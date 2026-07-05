using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Data.Dtos;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Services;

public class PackageScanService : IPackageScanService
{
    private readonly ApplicationDbContext _context;
    private readonly IBarcodeService _barcodeService;
    private readonly IAuditService _auditService;

    public PackageScanService(
        ApplicationDbContext context,
        IBarcodeService barcodeService,
        IAuditService auditService)
    {
        _context = context;
        _barcodeService = barcodeService;
        _auditService = auditService;
    }

    public async Task<ScanResult> ScanPackageAsync(int boxId, string barcode, int userId, string workstationName, CancellationToken cancellationToken = default)
    {
        barcode = barcode.Trim();

        try
        {
            _barcodeService.ValidateBarcodeFormat(barcode, expectBox: false);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return await RejectScanAsync(boxId, barcode, ex.Message, ex.Message, userId, workstationName, cancellationToken);
        }

        for (int attempt = 0; attempt < 3; attempt++)
        {
            IDbContextTransaction? transaction = null;
            try
            {
                var isDuplicate = await _context.BoxPackages
                    .AnyAsync(bp => bp.PackageBarcode == barcode, cancellationToken);

                if (isDuplicate)
                {
                    return await RejectScanAsync(
                        boxId,
                        barcode,
                        "This package barcode has already been scanned (duplicate).",
                        "This package barcode has already been scanned.",
                        userId,
                        workstationName,
                        cancellationToken);
                }

                var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, cancellationToken);

                if (box is null)
                {
                    return await RejectScanAsync(
                        null,
                        barcode,
                        "Box not found.",
                        "Box not found.",
                        userId,
                        workstationName,
                        cancellationToken);
                }

                if (box.Status != BoxStatus.Open)
                {
                    return await RejectScanAsync(
                        boxId,
                        barcode,
                        $"Box is not open (Status: {box.Status}).",
                        "This box is not open for scanning.",
                        userId,
                        workstationName,
                        cancellationToken);
                }

                if (box.CurrentQuantity >= box.ExpectedQuantity)
                {
                    return await RejectScanAsync(
                        boxId,
                        barcode,
                        "Expected quantity already reached.",
                        "The expected quantity has already been reached.",
                        userId,
                        workstationName,
                        cancellationToken);
                }

                transaction = _context.Database.IsRelational()
                    ? await _context.Database.BeginTransactionAsync(cancellationToken)
                    : null;

                var package = new BoxPackage
                {
                    BoxId = boxId,
                    PackageBarcode = barcode,
                    ScannedByUserId = userId,
                    ScannedAt = DateTime.UtcNow,
                    WorkstationName = workstationName
                };

                _context.BoxPackages.Add(package);
                box.CurrentQuantity++;
                box.ModifiedAt = DateTime.UtcNow;
                box.LastModifiedByUserId = userId;

                if (box.CurrentQuantity >= box.ExpectedQuantity)
                {
                    box.Status = BoxStatus.Completed;
                    box.CompletionMode = "Automatic";
                    box.CompletedAt = DateTime.UtcNow;
                    box.CompletedByUserId = userId;
                }

                await _context.SaveChangesAsync(cancellationToken);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                var updatedBox = await GetBoxByIdAsync(boxId, cancellationToken);
                var msg = box.Status == BoxStatus.Completed
                    ? "Scan successful! Box completed automatically."
                    : $"Scan successful! {box.CurrentQuantity}/{box.ExpectedQuantity} packages.";

                return new ScanResult { Success = true, Message = msg, Box = updatedBox };
            }
            catch (DbUpdateConcurrencyException)
            {
                _context.ChangeTracker.Entries().ToList().ForEach(e => e.Reload());
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_BoxPackages_PackageBarcode") == true)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                _context.ChangeTracker.Clear();
                return await RejectScanAsync(
                    boxId,
                    barcode,
                    "Unique concurrency conflict.",
                    "This package barcode has already been scanned.",
                    userId,
                    workstationName,
                    cancellationToken);
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        return new ScanResult { Success = false, Message = "Concurrency conflict. Please try again." };
    }

    private async Task<ScanResult> RejectScanAsync(
        int? boxId,
        string barcode,
        string reason,
        string message,
        int userId,
        string workstationName,
        CancellationToken cancellationToken)
    {
        await _auditService.LogScanRejectionAsync(boxId, barcode, reason, userId, workstationName, cancellationToken);
        return new ScanResult
        {
            Success = false,
            Message = message
        };
    }

    private async Task<BoxDetailsDto?> GetBoxByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Boxes
            .Include(b => b.CreatedBy)
            .Include(b => b.Packages)
                .ThenInclude(p => p.ScannedBy)
            .Where(b => b.Id == id)
            .Select(BoxMapper.ToDetailsDto())
            .FirstOrDefaultAsync(ct);
    }
}
