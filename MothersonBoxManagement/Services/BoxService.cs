using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Data.Dtos;
using MothersonBoxManagement.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MothersonBoxManagement.Services;

public class BoxService : IBoxService
{
    private readonly ApplicationDbContext _context;

    public BoxService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BoxDetailsDto> CreateBoxAsync(CreateBoxDto dto, int userId, CancellationToken cancellationToken = default)
    {
        string boxIdentifier = "";
        bool exists = true;
        int retries = 0;
        while (exists && retries < 10)
        {
            boxIdentifier = $"BOX-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(0, 16777216):X6}";
            exists = await _context.Boxes.AnyAsync(b => b.BoxNumber == boxIdentifier || b.BarcodeValue == boxIdentifier, cancellationToken);
            retries++;
        }
        if (exists)
        {
            throw new InvalidOperationException("Failed to generate a unique box identifier after 10 attempts.");
        }

        var box = new Box
        {
            BoxNumber = boxIdentifier,
            BarcodeValue = boxIdentifier,
            Type = dto.Type,
            Height = dto.Height,
            Width = dto.Width,
            Depth = dto.Depth,
            ExpectedQuantity = dto.ExpectedQuantity,
            CurrentQuantity = 0,
            Status = BoxStatus.Open,
            CreatedByUserId = userId,
            CreatedAt = DateTime.Now
        };

        _context.Boxes.Add(box);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetBoxByIdAsync(box.Id, cancellationToken)
            ?? throw new InvalidOperationException("Box was not persisted.");
    }

    public async Task<List<BoxListItemDto>> GetOpenBoxesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Boxes
            .Where(b => b.Status == BoxStatus.Open)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BoxListItemDto
            {
                Id = b.Id,
                BoxNumber = b.BoxNumber,
                BarcodeValue = b.BarcodeValue,
                Type = b.Type,
                ExpectedQuantity = b.ExpectedQuantity,
                CurrentQuantity = b.CurrentQuantity,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                CreatedByMatricule = b.CreatedBy.Matricule,
                LastUpdatedAt = b.UpdatedAt ?? b.CreatedAt,
                LastUserMatricule = b.LastModifiedBy != null ? b.LastModifiedBy.Matricule : b.CreatedBy.Matricule
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BoxDetailsDto?> GetBoxByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return await _context.Boxes
            .Include(b => b.CreatedBy)
            .Include(b => b.Packages)
                .ThenInclude(p => p.ScannedBy)
            .Where(b => b.BarcodeValue == barcode)
            .Select(MapToDetailsDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BoxDetailsDto?> GetBoxByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Boxes
            .Include(b => b.CreatedBy)
            .Include(b => b.Packages)
                .ThenInclude(p => p.ScannedBy)
            .Where(b => b.Id == id)
            .Select(MapToDetailsDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ScanResult> ScanPackageAsync(int boxId, string barcode, int userId, CancellationToken cancellationToken = default)
    {
        barcode = barcode.Trim();

        if (barcode.StartsWith("BOX-", StringComparison.OrdinalIgnoreCase))
            return new ScanResult { Success = false, Message = "Les codes-barres de box ne peuvent pas être scannés comme paquets." };

        for (int attempt = 0; attempt < 3; attempt++)
        {
            var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(cancellationToken)
                : null;
            try
            {
                var isDuplicate = await _context.BoxPackages
                    .AnyAsync(bp => bp.PackageBarcode == barcode, cancellationToken);

                if (isDuplicate)
                    return new ScanResult { Success = false, Message = "Ce code-barres paquet a déjà été scanné." };

                var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, cancellationToken);

                if (box is null)
                    return new ScanResult { Success = false, Message = "Box introuvable." };

                if (box.Status != BoxStatus.Open)
                    return new ScanResult { Success = false, Message = "Cette box n'est pas ouverte aux scans." };

                if (box.CurrentQuantity >= box.ExpectedQuantity)
                    return new ScanResult { Success = false, Message = "La quantité attendue est déjà atteinte." };

                var package = new BoxPackage
                {
                    BoxId = boxId,
                    PackageBarcode = barcode,
                    ScannedByUserId = userId,
                    ScannedAt = DateTime.Now
                };

                _context.BoxPackages.Add(package);
                box.CurrentQuantity++;
                box.UpdatedAt = DateTime.Now;
                box.LastModifiedByUserId = userId;

                if (box.CurrentQuantity >= box.ExpectedQuantity)
                {
                    box.Status = BoxStatus.Completed;
                    box.ClosedAt = DateTime.Now;
                    box.ClosedByUserId = userId;
                }

                await _context.SaveChangesAsync(cancellationToken);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                var updatedBox = await GetBoxByIdAsync(boxId, cancellationToken);
                var msg = box.Status == BoxStatus.Completed
                    ? "Scan réussi ! Box complétée automatiquement."
                    : $"Scan réussi ! {box.CurrentQuantity}/{box.ExpectedQuantity} paquets.";

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
                return new ScanResult { Success = false, Message = "Ce code-barres paquet a déjà été scanné." };
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        return new ScanResult { Success = false, Message = "Conflit de concurrence. Veuillez réessayer." };
    }

    private async Task<T> ExecuteWithConcurrencyRetryAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(cancellationToken)
                : null;
            try
            {
                var result = await action();
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                return result;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                _context.ChangeTracker.Clear();
            }
            catch (Exception)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                throw;
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }
        throw new InvalidOperationException("Conflit de concurrence. Veuillez réessayer.");
    }

    public async Task<BoxDetailsDto> CancelBoxAsync(int boxId, string reason, int userId, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, ct);
            if (box == null)
                throw new KeyNotFoundException($"Box with ID {boxId} not found.");

            if (box.Status != BoxStatus.Open)
                throw new InvalidOperationException("Only open boxes can be cancelled.");

            box.Status = BoxStatus.Cancelled;
            box.ExceptionReason = reason;
            box.LastModifiedByUserId = userId;
            box.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(boxId, ct)
                ?? throw new InvalidOperationException("Box was not persisted.");
        }, ct);
    }

    public async Task<BoxDetailsDto> ForceCloseBoxAsync(int boxId, string reason, int userId, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, ct);
            if (box == null)
                throw new KeyNotFoundException($"Box with ID {boxId} not found.");

            if (box.Status != BoxStatus.Open)
                throw new InvalidOperationException("Only open boxes can be force closed.");

            box.Status = BoxStatus.CompletedWithException;
            box.ExceptionReason = reason;
            box.LastModifiedByUserId = userId;
            box.UpdatedAt = DateTime.Now;
            box.ClosedAt = DateTime.Now;
            box.ClosedByUserId = userId;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(boxId, ct)
                ?? throw new InvalidOperationException("Box was not persisted.");
        }, ct);
    }

    public async Task<BoxDetailsDto> UpdateExpectedQuantityAsync(int boxId, int newQuantity, string reason, int userId, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, ct);
            if (box == null)
                throw new KeyNotFoundException($"Box with ID {boxId} not found.");

            if (box.Status != BoxStatus.Open)
                throw new InvalidOperationException("Expected quantity can only be updated for open boxes.");

            if (newQuantity <= 0)
                throw new ArgumentException("Expected quantity must be greater than zero.", nameof(newQuantity));

            if (newQuantity < box.CurrentQuantity)
                throw new InvalidOperationException("Expected quantity cannot be less than the current quantity of packages in the box.");

            box.ExpectedQuantity = newQuantity;
            box.ExceptionReason = reason;
            box.LastModifiedByUserId = userId;
            box.UpdatedAt = DateTime.Now;

            if (box.CurrentQuantity >= box.ExpectedQuantity)
            {
                box.Status = BoxStatus.Completed;
                box.ClosedAt = DateTime.Now;
                box.ClosedByUserId = userId;
            }

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(boxId, ct)
                ?? throw new InvalidOperationException("Box was not persisted.");
        }, ct);
    }

    public async Task<BoxDetailsDto> BlockBoxAsync(int boxId, string reason, int userId, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, ct);
            if (box == null)
                throw new KeyNotFoundException($"Box with ID {boxId} not found.");

            if (box.Status != BoxStatus.Open)
                throw new InvalidOperationException("Only open boxes can be blocked.");

            box.Status = BoxStatus.Blocked;
            box.ExceptionReason = reason;
            box.LastModifiedByUserId = userId;
            box.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(boxId, ct)
                ?? throw new InvalidOperationException("Box was not persisted.");
        }, ct);
    }

    public async Task<BoxDetailsDto> UnblockBoxAsync(int boxId, string reason, int userId, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, ct);
            if (box == null)
                throw new KeyNotFoundException($"Box with ID {boxId} not found.");

            if (box.Status != BoxStatus.Blocked)
                throw new InvalidOperationException("Only blocked boxes can be unblocked.");

            box.Status = BoxStatus.Open;
            box.ExceptionReason = reason;
            box.LastModifiedByUserId = userId;
            box.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(boxId, ct)
                ?? throw new InvalidOperationException("Box was not persisted.");
        }, ct);
    }

    public async Task<BoxDetailsDto> BlockPackageAsync(int packageId, string reason, int userId, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var package = await _context.BoxPackages
                .Include(p => p.Box)
                .FirstOrDefaultAsync(p => p.Id == packageId, ct);

            if (package == null)
                throw new KeyNotFoundException($"Package with ID {packageId} not found.");

            if (package.Box.Status == BoxStatus.Cancelled || package.Box.Status == BoxStatus.Archived)
                throw new InvalidOperationException("Cannot modify packages in a cancelled or archived box.");

            package.IsBlocked = true;
            package.BlockReason = reason;
            package.Box.UpdatedAt = DateTime.Now;
            package.Box.LastModifiedByUserId = userId;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(package.BoxId, ct)
                ?? throw new InvalidOperationException("Box was not persisted.");
        }, ct);
    }

    public async Task<BoxDetailsDto> UnblockPackageAsync(int packageId, string reason, int userId, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var package = await _context.BoxPackages
                .Include(p => p.Box)
                .FirstOrDefaultAsync(p => p.Id == packageId, ct);

            if (package == null)
                throw new KeyNotFoundException($"Package with ID {packageId} not found.");

            if (package.Box.Status == BoxStatus.Cancelled || package.Box.Status == BoxStatus.Archived)
                throw new InvalidOperationException("Cannot modify packages in a cancelled or archived box.");

            package.IsBlocked = false;
            package.BlockReason = reason;
            package.Box.UpdatedAt = DateTime.Now;
            package.Box.LastModifiedByUserId = userId;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(package.BoxId, ct)
                ?? throw new InvalidOperationException("Box was not persisted.");
        }, ct);
    }

    public async Task<BoxDetailsDto> TransferPackageAsync(int packageId, int destinationBoxId, string reason, int userId, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var package = await _context.BoxPackages
                .FirstOrDefaultAsync(p => p.Id == packageId, ct);

            if (package == null)
                throw new KeyNotFoundException($"Package with ID {packageId} not found.");

            int sourceBoxId = package.BoxId;

            if (sourceBoxId == destinationBoxId)
                throw new InvalidOperationException("Source and destination boxes are the same.");

            var sourceBox = await _context.Boxes
                .FirstOrDefaultAsync(b => b.Id == sourceBoxId, ct);

            var destinationBox = await _context.Boxes
                .FirstOrDefaultAsync(b => b.Id == destinationBoxId, ct);

            if (sourceBox == null)
                throw new KeyNotFoundException($"Source box with ID {sourceBoxId} not found.");

            if (destinationBox == null)
                throw new KeyNotFoundException($"Destination box with ID {destinationBoxId} not found.");

            if (sourceBox.Status != BoxStatus.Open)
                throw new InvalidOperationException("Source box is not open.");

            if (destinationBox.Status != BoxStatus.Open)
                throw new InvalidOperationException("Destination box is not open.");

            if (destinationBox.CurrentQuantity >= destinationBox.ExpectedQuantity)
                throw new InvalidOperationException("Destination box is full.");

            package.BoxId = destinationBoxId;

            sourceBox.CurrentQuantity--;
            sourceBox.UpdatedAt = DateTime.Now;
            sourceBox.LastModifiedByUserId = userId;
            sourceBox.ExceptionReason = reason;

            destinationBox.CurrentQuantity++;
            destinationBox.UpdatedAt = DateTime.Now;
            destinationBox.LastModifiedByUserId = userId;
            destinationBox.ExceptionReason = reason;

            if (destinationBox.CurrentQuantity >= destinationBox.ExpectedQuantity)
            {
                destinationBox.Status = BoxStatus.Completed;
                destinationBox.ClosedAt = DateTime.Now;
                destinationBox.ClosedByUserId = userId;
            }

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(sourceBoxId, ct)
                ?? throw new InvalidOperationException("Box was not persisted.");
        }, ct);
    }

    public async Task<BoxDetailsDto> RetraitPackageAsync(int packageId, string reason, int userId, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var package = await _context.BoxPackages
                .Include(p => p.Box)
                .FirstOrDefaultAsync(p => p.Id == packageId, ct);

            if (package == null)
                throw new KeyNotFoundException($"Package with ID {packageId} not found.");

            var box = package.Box;

            if (box.Status != BoxStatus.Open)
                throw new InvalidOperationException("Cannot withdraw a package from a box that is not open.");

            _context.BoxPackages.Remove(package);

            box.CurrentQuantity--;
            box.UpdatedAt = DateTime.Now;
            box.LastModifiedByUserId = userId;
            box.ExceptionReason = reason;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(box.Id, ct)
                ?? throw new InvalidOperationException("Box was not persisted.");
        }, ct);
    }

    private static System.Linq.Expressions.Expression<System.Func<Box, BoxDetailsDto>> MapToDetailsDto()
    {
        return b => new BoxDetailsDto
        {
            Id = b.Id,
            BoxNumber = b.BoxNumber,
            BarcodeValue = b.BarcodeValue,
            Type = b.Type,
            Height = b.Height,
            Width = b.Width,
            Depth = b.Depth,
            ExpectedQuantity = b.ExpectedQuantity,
            CurrentQuantity = b.CurrentQuantity,
            Status = b.Status,
            CreatedByMatricule = b.CreatedBy.Matricule,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt,
            ClosedAt = b.ClosedAt,
            ExceptionReason = b.ExceptionReason,
            Packages = b.Packages.Select(p => new PackageItemDto
            {
                Id = p.Id,
                PackageBarcode = p.PackageBarcode,
                ScannedAt = p.ScannedAt,
                ScannedByMatricule = p.ScannedBy.Matricule,
                IsBlocked = p.IsBlocked,
                BlockReason = p.BlockReason
            }).ToList()
        };
    }
}
