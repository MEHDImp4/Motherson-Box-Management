using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Models;

namespace MothersonBoxManagement.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private const string ActionTypesCacheKey = "AuditActionTypes";

    public AuditService(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task LogScanRejectionAsync(int? boxId, string barcode, string reason, int userId, string workstationName, CancellationToken ct = default)
    {
        var log = new BoxAuditLog
        {
            BoxId = boxId,
            UserId = userId,
            ActionType = "PackageRejected",
            Timestamp = DateTime.UtcNow,
            WorkstationName = workstationName,
            PackageBarcode = barcode,
            Reason = reason,
            Description = $"Scan rejected for package {barcode}. Reason: {reason}"
        };

        _context.BoxAuditLogs.Add(log);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<AuditIndexViewModel> GetAuditLogsAsync(AuditFilterDto filter, CancellationToken ct = default)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : Math.Min(filter.PageSize, 100);

        var query = _context.BoxAuditLogs
            .Include(l => l.Box)
            .Include(l => l.User)
            .AsQueryable();

        if (filter.BoxId.HasValue)
            query = query.Where(l => l.BoxId == filter.BoxId.Value);

        if (!string.IsNullOrWhiteSpace(filter.ActionType))
            query = query.Where(l => l.ActionType == filter.ActionType);

        if (filter.FromDate.HasValue)
            query = query.Where(l => l.Timestamp >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
        {
            var endOfDay = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(l => l.Timestamp <= endOfDay);
        }

        var totalItems = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var actionTypes = await _cache.GetOrCreateAsync(ActionTypesCacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return _context.BoxAuditLogs
                .Select(l => l.ActionType)
                .Distinct()
                .ToListAsync();
        }) ?? new List<string>();

        return new AuditIndexViewModel
        {
            Items = items,
            ActionTypes = actionTypes,
            Filter = filter,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };
    }
}
