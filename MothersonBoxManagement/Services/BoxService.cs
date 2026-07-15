using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Services;

public class BoxService : IBoxService, IBoxQueryService, IBoxLifecycleService, IBoxPackageService
{
    public const string CompletionModeForced = "Forced";
    public const string CompletionModeAutomatic = "Automatic";

    private static readonly Expression<Func<Box, BoxListItemDto>> ToListItemDtoExpr = b => new BoxListItemDto
    {
        Id = b.Id,
        BoxNumber = b.BoxNumber,
        BarcodeValue = b.BarcodeValue,
        Type = b.Type,
        ExpectedQuantity = b.ExpectedQuantity,
        CurrentQuantity = b.CurrentQuantity,
        Status = b.Status,
        CreatedAt = b.CreatedAt,
        ModifiedAt = b.ModifiedAt,
        CreatedByMatricule = b.CreatedBy.Matricule,
        LastModifiedAt = b.ModifiedAt ?? b.CreatedAt,
        LastUserMatricule = b.LastModifiedBy != null ? b.LastModifiedBy.Matricule : b.CreatedBy.Matricule
    };

    private readonly ApplicationDbContext _context;
    private readonly IBarcodeService _barcodeService;

    public BoxService(
        ApplicationDbContext context,
        IBarcodeService barcodeService)
    {
        _context = context;
        _barcodeService = barcodeService;
    }

    public async Task<BoxDetailsDto> CreateBoxAsync(CreateBoxDto dto, int userId, CancellationToken cancellationToken = default)
    {
        var boxIdentifier = await _barcodeService.GenerateUniqueBoxBarcodeAsync(cancellationToken);

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
            Status = BoxStatus.Created,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Boxes.Add(box);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetBoxByIdAsync(box.Id, cancellationToken)
            ?? throw new InvalidOperationException("The box could not be saved.");
    }

    public async Task<List<BoxListItemDto>> GetOpenBoxesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Boxes
            .Where(b => b.Status == BoxStatus.Open)
            .OrderByDescending(b => b.CreatedAt)
            .Take(200)
            .Select(ToListItemDtoExpr)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BoxListItemDto>> GetCreatedBoxesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Boxes
            .Where(b => b.Status == BoxStatus.Created)
            .OrderByDescending(b => b.CreatedAt)
            .Take(200)
            .Select(ToListItemDtoExpr)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<BoxListItemDto>> GetOpenBoxesPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Boxes.Where(b => b.Status == BoxStatus.Open);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToListItemDtoExpr)
            .ToListAsync(cancellationToken);
        return new PagedResult<BoxListItemDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<PagedResult<BoxListItemDto>> GetCreatedBoxesPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Boxes.Where(b => b.Status == BoxStatus.Created);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToListItemDtoExpr)
            .ToListAsync(cancellationToken);
        return new PagedResult<BoxListItemDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<List<BoxListItemDto>> SearchBoxesAsync(BoxSearchFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Boxes
            .AsQueryable();

        if (filter.Status.HasValue)
        {
            query = query.Where(b => b.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.BoxNumber))
        {
            string cleanNumber = filter.BoxNumber.Trim();
            query = query.Where(b => b.BoxNumber.Contains(cleanNumber) || b.BarcodeValue.Contains(cleanNumber));
        }

        if (filter.CreatedByUserId.HasValue)
        {
            query = query.Where(b => b.CreatedByUserId == filter.CreatedByUserId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(b => b.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            var endOfDay = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(b => b.CreatedAt <= endOfDay);
        }

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToListItemDtoExpr)
            .ToListAsync(cancellationToken);
    }

    public async Task<BoxDetailsDto?> GetBoxByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return await _context.Boxes
            .AsNoTracking()
            .Include(b => b.CreatedBy)
            .Include(b => b.Packages)
                .ThenInclude(p => p.ScannedBy)
            .Where(b => b.BarcodeValue == barcode)
            .Select(BoxMapper.ToDetailsDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BoxDetailsDto?> GetBoxByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Boxes
            .AsNoTracking()
            .Include(b => b.CreatedBy)
            .Include(b => b.Packages)
                .ThenInclude(p => p.ScannedBy)
            .Where(b => b.Id == id)
            .Select(BoxMapper.ToDetailsDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BoxDetailsDto?> FindBoxByPackageBarcodeAsync(string packageBarcode, CancellationToken cancellationToken = default)
    {
        var package = await _context.BoxPackages
            .FirstOrDefaultAsync(
                bp => bp.PackageBarcode == packageBarcode && !bp.IsRemoved,
                cancellationToken);

        if (package is null)
            return null;

        return await GetBoxByIdAsync(package.BoxId, cancellationToken);
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
        throw new InvalidOperationException("Concurrency conflict. Please try again.");
    }

    public async Task<BoxDetailsDto> OpenBoxAsync(int boxId, int userId, string workstationName, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, ct);
            if (box == null)
                throw new KeyNotFoundException($"Box with ID {boxId} was not found.");

            if (box.Status != BoxStatus.Created)
                throw new InvalidOperationException("Only boxes with 'Created' status can be opened.");

            box.Status = BoxStatus.Open;
            box.LastModifiedByUserId = userId;
            box.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(boxId, ct)
                ?? throw new InvalidOperationException("The box could not be saved.");
        }, ct);
    }

    public async Task<BoxDetailsDto> CancelBoxAsync(int boxId, string reason, int userId, string workstationName, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, ct);
            if (box == null)
                throw new KeyNotFoundException($"Box with ID {boxId} was not found.");

            if (box.Status != BoxStatus.Open && box.Status != BoxStatus.Created)
                throw new InvalidOperationException("Only open or created boxes can be cancelled.");

            box.Status = BoxStatus.Cancelled;
            box.ExceptionReason = reason;
            box.LastModifiedByUserId = userId;
            box.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(boxId, ct)
                ?? throw new InvalidOperationException("The box could not be saved.");
        }, ct);
    }

    public async Task<BoxDetailsDto> ForceCloseBoxAsync(int boxId, string reason, int userId, string workstationName, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, ct);
            if (box == null)
                throw new KeyNotFoundException($"Box with ID {boxId} was not found.");

            if (box.Status != BoxStatus.Open)
                throw new InvalidOperationException("Only open boxes can be force closed.");

            var expectedQuantity = box.ExpectedQuantity;
            var currentQuantity = box.CurrentQuantity;

            if (currentQuantity >= expectedQuantity)
                throw new InvalidOperationException("Force close is only allowed when the expected quantity has not been reached.");

            box.Status = BoxStatus.CompletedWithException;
            box.ExceptionReason = reason;
            box.CompletionMode = CompletionModeForced;
            box.LastModifiedByUserId = userId;
            box.ModifiedAt = DateTime.UtcNow;
            box.CompletedAt = DateTime.UtcNow;
            box.CompletedByUserId = userId;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(boxId, ct)
                ?? throw new InvalidOperationException("The box could not be saved.");
        }, ct);
    }

    public async Task<BoxDetailsDto> UpdateExpectedQuantityAsync(int boxId, int newQuantity, string reason, int userId, string workstationName, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, ct);
            if (box == null)
                throw new KeyNotFoundException($"Box with ID {boxId} was not found.");

            if (box.Status != BoxStatus.Open)
                throw new InvalidOperationException("Expected quantity can only be changed for open boxes.");

            if (newQuantity <= 0)
                throw new ArgumentException("Expected quantity must be greater than zero.", nameof(newQuantity));

            if (newQuantity < box.CurrentQuantity)
                throw new InvalidOperationException("Expected quantity cannot be lower than the current number of packages in the box.");

            var oldQuantity = box.ExpectedQuantity;

            box.ExpectedQuantity = newQuantity;
            box.ExceptionReason = reason;
            box.LastModifiedByUserId = userId;
            box.ModifiedAt = DateTime.UtcNow;

            if (box.CurrentQuantity >= box.ExpectedQuantity)
            {
                box.Status = BoxStatus.Completed;
                box.CompletionMode = CompletionModeAutomatic;
                box.CompletedAt = DateTime.UtcNow;
                box.CompletedByUserId = userId;
            }

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(boxId, ct)
                ?? throw new InvalidOperationException("The box could not be saved.");
        }, ct);
    }

    public async Task<BoxDetailsDto> BlockBoxAsync(int boxId, string reason, int userId, string workstationName, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, ct);
            if (box == null)
                throw new KeyNotFoundException($"Box with ID {boxId} was not found.");

            if (box.Status != BoxStatus.Open)
                throw new InvalidOperationException("Only open boxes can be blocked.");

            box.Status = BoxStatus.Blocked;
            box.BlockReason = reason;
            box.BlockedByUserId = userId;
            box.BlockedAt = DateTime.UtcNow;
            box.LastModifiedByUserId = userId;
            box.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(boxId, ct)
                ?? throw new InvalidOperationException("The box could not be saved.");
        }, ct);
    }

    public async Task<BoxDetailsDto> UnblockBoxAsync(int boxId, string reason, int userId, string workstationName, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, ct);
            if (box == null)
                throw new KeyNotFoundException($"Box with ID {boxId} was not found.");

            if (box.Status != BoxStatus.Blocked)
                throw new InvalidOperationException("Only blocked boxes can be unblocked.");

            box.Status = BoxStatus.Open;
            box.BlockReason = null;
            box.BlockedByUserId = null;
            box.BlockedAt = null;
            box.LastModifiedByUserId = userId;
            box.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(boxId, ct)
                ?? throw new InvalidOperationException("The box could not be saved.");
        }, ct);
    }

    public async Task<BoxDetailsDto> ArchiveBoxAsync(int boxId, string reason, int userId, string workstationName, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, ct);
            if (box == null)
                throw new KeyNotFoundException($"Box with ID {boxId} was not found.");

            if (box.Status != BoxStatus.Completed && box.Status != BoxStatus.CompletedWithException)
                throw new InvalidOperationException("Only completed boxes (Completed or CompletedWithException) can be archived.");

            box.Status = BoxStatus.Archived;
            box.ExceptionReason = string.IsNullOrEmpty(box.ExceptionReason) ? reason : box.ExceptionReason;
            box.LastModifiedByUserId = userId;
            box.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(boxId, ct)
                ?? throw new InvalidOperationException("The box could not be saved.");
        }, ct);
    }

    public async Task<BoxDetailsDto> BlockPackageAsync(int packageId, string reason, int userId, string workstationName, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var package = await _context.BoxPackages
                .Include(p => p.Box)
                .FirstOrDefaultAsync(p => p.Id == packageId, ct);

            if (package == null)
                throw new KeyNotFoundException($"Package with ID {packageId} was not found.");

            if (package.Box.Status == BoxStatus.Cancelled || package.Box.Status == BoxStatus.Archived)
                throw new InvalidOperationException("Packages in a cancelled or archived box cannot be changed.");

            if (package.IsRemoved)
                throw new InvalidOperationException("A removed package association cannot be blocked.");

            if (package.IsBlocked)
                throw new InvalidOperationException("This package is already blocked.");

            package.IsBlocked = true;
            package.BlockReason = reason;
            package.Box.CurrentQuantity = Math.Max(0, package.Box.CurrentQuantity - 1);

            if (package.Box.Status == BoxStatus.Completed || package.Box.Status == BoxStatus.CompletedWithException)
            {
                package.Box.Status = BoxStatus.Open;
                package.Box.CompletionMode = null;
                package.Box.CompletedAt = null;
                package.Box.CompletedByUserId = null;
            }

            package.Box.ModifiedAt = DateTime.UtcNow;
            package.Box.LastModifiedByUserId = userId;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(package.BoxId, ct)
                ?? throw new InvalidOperationException("The box could not be saved.");
        }, ct);
    }

    public async Task<BoxDetailsDto> UnblockPackageAsync(int packageId, string reason, int userId, string workstationName, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var package = await _context.BoxPackages
                .Include(p => p.Box)
                .FirstOrDefaultAsync(p => p.Id == packageId, ct);

            if (package == null)
                throw new KeyNotFoundException($"Package with ID {packageId} was not found.");

            if (package.Box.Status == BoxStatus.Cancelled || package.Box.Status == BoxStatus.Archived)
                throw new InvalidOperationException("Packages in a cancelled or archived box cannot be changed.");

            if (package.IsRemoved)
                throw new InvalidOperationException("A removed package association cannot be unblocked.");

            if (!package.IsBlocked)
                throw new InvalidOperationException("This package is not blocked.");

            if (package.Box.CurrentQuantity >= package.Box.ExpectedQuantity)
                throw new InvalidOperationException("Unblocking this package would exceed the expected quantity.");

            package.IsBlocked = false;
            package.BlockReason = reason;
            package.Box.CurrentQuantity++;

            if (package.Box.Status == BoxStatus.Open &&
                package.Box.CurrentQuantity >= package.Box.ExpectedQuantity)
            {
                package.Box.Status = BoxStatus.Completed;
                package.Box.CompletionMode = CompletionModeAutomatic;
                package.Box.CompletedAt = DateTime.UtcNow;
                package.Box.CompletedByUserId = userId;
            }

            package.Box.ModifiedAt = DateTime.UtcNow;
            package.Box.LastModifiedByUserId = userId;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(package.BoxId, ct)
                ?? throw new InvalidOperationException("The box could not be saved.");
        }, ct);
    }

    public async Task<BoxDetailsDto> TransferPackageAsync(int packageId, int destinationBoxId, string reason, int userId, string workstationName, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var package = await _context.BoxPackages
                .FirstOrDefaultAsync(p => p.Id == packageId, ct);

            if (package == null)
                throw new KeyNotFoundException($"Package with ID {packageId} was not found.");

            if (package.IsBlocked)
                throw new InvalidOperationException("A blocked package cannot be transferred. Unblock it first.");

            if (package.IsRemoved)
                throw new InvalidOperationException("A removed package association cannot be transferred.");

            int sourceBoxId = package.BoxId;

            if (sourceBoxId == destinationBoxId)
                throw new InvalidOperationException("The source and destination boxes are the same.");

            var boxIds = new[] { sourceBoxId, destinationBoxId };
            var boxes = await _context.Boxes
                .Where(b => boxIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, ct);

            if (!boxes.TryGetValue(sourceBoxId, out var sourceBox))
                throw new KeyNotFoundException($"Source box with ID {sourceBoxId} was not found.");

            if (!boxes.TryGetValue(destinationBoxId, out var destinationBox))
                throw new KeyNotFoundException($"Destination box with ID {destinationBoxId} was not found.");

            if (sourceBox.Status != BoxStatus.Open)
                throw new InvalidOperationException("The source box is not open.");

            if (destinationBox.Status != BoxStatus.Open)
                throw new InvalidOperationException("The destination box is not open.");

            if (destinationBox.CurrentQuantity >= destinationBox.ExpectedQuantity)
                throw new InvalidOperationException("The destination box is full.");

            package.BoxId = destinationBoxId;

            sourceBox.CurrentQuantity--;
            sourceBox.ModifiedAt = DateTime.UtcNow;
            sourceBox.LastModifiedByUserId = userId;
            sourceBox.ExceptionReason = reason;

            destinationBox.CurrentQuantity++;
            destinationBox.ModifiedAt = DateTime.UtcNow;
            destinationBox.LastModifiedByUserId = userId;
            destinationBox.ExceptionReason = reason;

            if (destinationBox.CurrentQuantity >= destinationBox.ExpectedQuantity)
            {
                destinationBox.Status = BoxStatus.Completed;
                destinationBox.CompletionMode = CompletionModeAutomatic;
                destinationBox.CompletedAt = DateTime.UtcNow;
                destinationBox.CompletedByUserId = userId;
            }

            _context.BoxAuditLogs.Add(new BoxAuditLog
            {
                BoxId = sourceBoxId,
                PackageBarcode = package.PackageBarcode,
                ActionType = "PackageTransferred",
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                WorkstationName = workstationName,
                PreviousValue = sourceBoxId.ToString(),
                NewValue = destinationBoxId.ToString(),
                Reason = reason,
                Description = $"Package transferred from box {sourceBox.BoxNumber} to box {destinationBox.BoxNumber}.",
                DetailsJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    PackageBarcode = package.PackageBarcode,
                    SourceBoxId = sourceBoxId,
                    SourceBoxNumber = sourceBox.BoxNumber,
                    DestinationBoxId = destinationBoxId,
                    DestinationBoxNumber = destinationBox.BoxNumber,
                    Reason = reason,
                    UserId = userId,
                    WorkstationName = workstationName
                })
            });

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(sourceBoxId, ct)
                ?? throw new InvalidOperationException("The box could not be saved.");
        }, ct);
    }

    public async Task<BoxDetailsDto> RetraitPackageAsync(int packageId, string reason, int userId, string workstationName, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var package = await _context.BoxPackages
                .Include(p => p.Box)
                .FirstOrDefaultAsync(p => p.Id == packageId, ct);

            if (package == null)
                throw new KeyNotFoundException($"Package with ID {packageId} was not found.");

            var box = package.Box;

            if (box.Status != BoxStatus.Open && box.Status != BoxStatus.Cancelled)
                throw new InvalidOperationException("A package can only be removed from an open or cancelled box.");

            if (package.IsRemoved)
                throw new InvalidOperationException("This package association has already been removed.");

            package.IsRemoved = true;
            package.RemovedAt = DateTime.UtcNow;
            package.RemovedByUserId = userId;
            package.RemovalReason = reason;

            box.CurrentQuantity = Math.Max(0, box.CurrentQuantity - 1);
            box.ModifiedAt = DateTime.UtcNow;
            box.LastModifiedByUserId = userId;
            box.ExceptionReason = reason;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(box.Id, ct)
                ?? throw new InvalidOperationException("The box could not be saved.");
        }, ct);
    }

    public async Task<BoxDetailsDto> DisassociatePackageAsync(int packageId, string reason, int userId, string workstationName, CancellationToken ct = default)
    {
        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var package = await _context.BoxPackages
                .Include(p => p.Box)
                .FirstOrDefaultAsync(p => p.Id == packageId, ct);

            if (package == null)
                throw new KeyNotFoundException($"Package with ID {packageId} was not found.");

            var box = package.Box;

            if (box.Status != BoxStatus.Cancelled)
                throw new InvalidOperationException("Disassociation is only allowed for packages in a cancelled box.");

            if (package.IsRemoved)
                throw new InvalidOperationException("This package association has already been removed.");

            package.IsRemoved = true;
            package.RemovedAt = DateTime.UtcNow;
            package.RemovedByUserId = userId;
            package.RemovalReason = reason;

            box.CurrentQuantity = Math.Max(0, box.CurrentQuantity - 1);
            box.ModifiedAt = DateTime.UtcNow;
            box.LastModifiedByUserId = userId;
            box.ExceptionReason = reason;

            await _context.SaveChangesAsync(ct);

            return await GetBoxByIdAsync(box.Id, ct)
                ?? throw new InvalidOperationException("The box could not be saved.");
        }, ct);
    }

}
