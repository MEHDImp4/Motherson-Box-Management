using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Services;

public class PackageScanService : IPackageScanService
{
    internal const string SqlServerErrorNumberDataKey = "SqlServerErrorNumber";
    private readonly ApplicationDbContext _context;
    private readonly IBarcodeService _barcodeService;
    private readonly IAuditService _auditService;
    private readonly IBoxService _boxService;

    public PackageScanService(
        ApplicationDbContext context,
        IBarcodeService barcodeService,
        IAuditService auditService,
        IBoxService boxService)
    {
        _context = context;
        _barcodeService = barcodeService;
        _auditService = auditService;
        _boxService = boxService;
    }

    public async Task<ScanResult> ScanPackageAsync(int boxId, string barcode, int userId, string workstationName, CancellationToken cancellationToken = default, string? requestId = null)
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
                transaction = _context.Database.IsRelational() && _context.Database.CurrentTransaction is null
                    ? await _context.Database.BeginTransactionAsync(cancellationToken)
                    : null;

                var existingPackage = await _context.BoxPackages
                    .FirstOrDefaultAsync(
                        bp => bp.PackageBarcode == barcode && !bp.IsRemoved,
                        cancellationToken);

                if (existingPackage != null)
                {
                    if (!string.IsNullOrWhiteSpace(requestId) &&
                        existingPackage.BoxId == boxId &&
                        string.Equals(existingPackage.ScanRequestId, requestId, StringComparison.Ordinal))
                    {
                        var replayedBox = await _boxService.GetBoxByIdAsync(boxId, cancellationToken);
                        if (transaction is not null)
                            await transaction.CommitAsync(cancellationToken);
                        return new ScanResult
                        {
                            Success = true,
                            Message = "Scan already recorded; previous result restored.",
                            Box = replayedBox
                        };
                    }

                    if (existingPackage.IsBlocked)
                    {
                        var blockedResult = await RejectScanAsync(
                            boxId,
                            barcode,
                            $"Package barcode {barcode} is blocked/quarantined (reason: {existingPackage.BlockReason}).",
                            "This package is blocked and cannot be scanned. Contact a supervisor.",
                            userId,
                            workstationName,
                            cancellationToken);
                        if (transaction is not null)
                        {
                            await transaction.CommitAsync(cancellationToken);
                        }
                        return blockedResult;
                    }

                    var duplicateResult = await RejectScanAsync(
                        boxId,
                        barcode,
                        "Duplicate package barcode detected.",
                        "This package is already associated with a box.",
                        userId,
                        workstationName,
                        cancellationToken);
                    if (transaction is not null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }
                    return duplicateResult;
                }

                var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, cancellationToken);

                if (box is null)
                {
                    var missingBoxResult = await RejectScanAsync(
                        null,
                        barcode,
                        "Box not found.",
                        "Box not found.",
                        userId,
                        workstationName,
                        cancellationToken);
                    if (transaction is not null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }
                    return missingBoxResult;
                }

                if (box.Status != BoxStatus.Open)
                {
                    var closedBoxResult = await RejectScanAsync(
                        boxId,
                        barcode,
                        $"Box is not open (Status: {box.Status}).",
                        "This box cannot receive packages (closed, cancelled, or blocked).",
                        userId,
                        workstationName,
                        cancellationToken);
                    if (transaction is not null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }
                    return closedBoxResult;
                }

                var persistedValidQuantity = await _context.BoxPackages.CountAsync(
                    package => package.BoxId == boxId && !package.IsRemoved && !package.IsBlocked,
                    cancellationToken);
                if (persistedValidQuantity != box.CurrentQuantity)
                {
                    var inconsistentResult = await RejectScanAsync(
                        boxId,
                        barcode,
                        $"Quantity integrity mismatch: counter={box.CurrentQuantity}, packages={persistedValidQuantity}.",
                        "This box has a data consistency problem. Scanning is blocked; contact a supervisor.",
                        userId,
                        workstationName,
                        cancellationToken);
                    if (transaction is not null)
                        await transaction.CommitAsync(cancellationToken);
                    return inconsistentResult;
                }

                if (box.CurrentQuantity >= box.ExpectedQuantity)
                {
                    var fullBoxResult = await RejectScanAsync(
                        boxId,
                        barcode,
                        "Expected quantity already reached.",
                        "The expected quantity has already been reached. This box cannot receive any more packages.",
                        userId,
                        workstationName,
                        cancellationToken);
                    if (transaction is not null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }
                    return fullBoxResult;
                }
                var package = new BoxPackage
                {
                    BoxId = boxId,
                    PackageBarcode = barcode,
                    ScannedByUserId = userId,
                    ScannedAt = DateTime.UtcNow,
                    WorkstationName = workstationName,
                    ScanRequestId = string.IsNullOrWhiteSpace(requestId) ? null : requestId
                };

                _context.BoxPackages.Add(package);
                box.CurrentQuantity++;
                box.ModifiedAt = DateTime.UtcNow;
                box.LastModifiedByUserId = userId;

                if (box.CurrentQuantity >= box.ExpectedQuantity)
                {
                    box.Status = BoxStatus.Completed;
                    box.CompletionMode = BoxService.CompletionModeAutomatic;
                    box.CompletedAt = DateTime.UtcNow;
                    box.CompletedByUserId = userId;
                }

                await _context.SaveChangesAsync(cancellationToken);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                var updatedBox = await _boxService.GetBoxByIdAsync(boxId, cancellationToken);
                var msg = box.Status == BoxStatus.Completed
                    ? "Scan successful! Box automatically completed."
                    : $"Package successfully added to box {box.BoxNumber}. {box.CurrentQuantity}/{box.ExpectedQuantity} packages.";

                return new ScanResult { Success = true, Message = msg, Box = updatedBox };
            }
            catch (DbUpdateConcurrencyException)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                foreach (var entry in _context.ChangeTracker.Entries().ToList())
                    await entry.ReloadAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                _context.ChangeTracker.Clear();
                return await RejectScanAsync(
                    boxId,
                    barcode,
                    "Unique constraint concurrency conflict.",
                    "This package is already associated with a box (concurrency conflict).",
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

    internal static bool IsUniqueConstraintViolationNumber(int errorNumber)
        => errorNumber is 2601 or 2627;

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException &&
                IsUniqueConstraintViolationNumber(sqlException.Number))
            {
                return true;
            }

            if (current.Data[SqlServerErrorNumberDataKey] is int simulatedNumber &&
                IsUniqueConstraintViolationNumber(simulatedNumber))
            {
                return true;
            }
        }

        return false;
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
}
