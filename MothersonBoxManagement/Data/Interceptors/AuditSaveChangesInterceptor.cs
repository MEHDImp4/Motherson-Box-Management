using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Services;
using MothersonBoxManagement.Security;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MothersonBoxManagement.Data.Interceptors;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> AuditablePropertyAllowlist = new(StringComparer.Ordinal)
    {
        "Id", "Matricule", "FullName", "Role", "IsActive",
        "BoxNumber", "BarcodeValue", "Type", "Height", "Width", "Depth",
        "ExpectedQuantity", "CurrentQuantity", "Status", "CompletionMode",
        "ExceptionReason", "BlockReason", "CreatedByUserId", "LastModifiedByUserId",
        "CompletedByUserId", "BlockedByUserId", "CreatedAt", "ModifiedAt", "CompletedAt",
        "PackageBarcode", "BoxId", "ScannedByUserId", "ScannedAt", "WorkstationName",
        "IsBlocked", "IsRemoved", "BlockReason", "RemovalReason", "RemovedByUserId",
        "RemovedAt", "ScanRequestId", "Name", "Description", "PackagePrefixPattern",
        "PrinterName", "PrinterUncPath", "PayloadType", "Status", "AttemptCount",
        "ErrorMessage", "RequestedByUserId", "RequestedAt", "PrintedAt", "ReprintReason",
        "FailedAttempts", "LockoutEnd", "LastAttemptAt", "Code", "PcName"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWorkstationResolver _workstationResolver;

    public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor, IWorkstationResolver workstationResolver)
    {
        _httpContextAccessor = httpContextAccessor;
        _workstationResolver = workstationResolver;
    }

    private void AuditChanges(DbContext context)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userIdVal = ResolveUserId(context, httpContext);

        if (userIdVal == null)
            return;

        string? clientWorkstationName = null;
        if (httpContext?.Request.HasFormContentType == true &&
            httpContext.Request.Form.TryGetValue("workstationName", out var workstationValues))
        {
            clientWorkstationName = workstationValues.FirstOrDefault();
        }

        string workstationName = _workstationResolver.Resolve(clientWorkstationName);

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not BoxAuditLog &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        var auditLogs = new List<BoxAuditLog>();

        foreach (var entry in entries)
        {
            var actionType = DetermineActionType(context, entry);
            if (actionType == null)
                continue;

            var (previousValue, newValue, reason, packageBarcode) = ExtractAuditValues(entry);

            var detailsJson = BuildDetailsJson(entry);

            var auditLog = new BoxAuditLog
            {
                ActionType = actionType,
                UserId = userIdVal.Value,
                Timestamp = DateTime.UtcNow,
                WorkstationName = workstationName,
                PreviousValue = previousValue,
                NewValue = newValue,
                Reason = reason,
                PackageBarcode = packageBarcode,
                Description = GetDescription(actionType),
                DetailsJson = detailsJson
            };

            if (entry.Entity is Box box && entry.State == EntityState.Added)
            {
                auditLog.Box = box;
            }
            else if (entry.Entity is Box)
            {
                auditLog.BoxId = ((Box)entry.Entity).Id;
            }
            else if (entry.Entity is BoxPackage package)
            {
                auditLog.BoxId = package.BoxId;
            }

            auditLogs.Add(auditLog);
        }

        if (auditLogs.Count > 0)
        {
            context.AddRange(auditLogs);
        }
    }

    private static int? ResolveUserId(DbContext context, HttpContext? httpContext)
    {
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int id))
                return id;
        }

        var systemUser = context.Set<User>().AsNoTracking()
            .FirstOrDefault(u => u.Id == SystemPrincipal.UserId
                && u.Matricule == SystemPrincipal.Matricule
                && !u.IsActive);

        if (systemUser != null)
            return systemUser.Id;

        throw new InvalidOperationException(
            "The inactive SYSTEM audit principal is missing. Refusing to attribute an automated action to a human user.");
    }

    private static string? DetermineActionType(DbContext context, EntityEntry entry)
    {
        if (entry.Entity is Box)
            return DetermineBoxActionType(entry);

        if (entry.Entity is BoxPackage pkg)
            return DeterminePackageActionType(context, entry, pkg);

        return entry.State switch
        {
            EntityState.Added => "Insert",
            EntityState.Modified => "Update",
            EntityState.Deleted => "Delete",
            _ => entry.State.ToString()
        };
    }

    private static string? DetermineBoxActionType(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
            return "BoxCreated";

        if (entry.State == EntityState.Deleted)
            return "BoxDeleted";

        if (entry.State == EntityState.Modified)
        {
            var statusProp = entry.Property("Status");
            var expectedQtyProp = entry.Property("ExpectedQuantity");

            if (statusProp.IsModified)
            {
                var oldStatus = (BoxStatus)statusProp.OriginalValue!;
                var newStatus = (BoxStatus)statusProp.CurrentValue!;
                return newStatus switch
                {
                    BoxStatus.Cancelled => "BoxCancelled",
                    BoxStatus.Completed => "BoxCompletedAuto",
                    BoxStatus.CompletedWithException => "BoxCompletedWithException",
                    BoxStatus.Blocked => "BoxBlocked",
                    BoxStatus.Open when oldStatus == BoxStatus.Blocked => "BoxUnblocked",
                    BoxStatus.Open when oldStatus == BoxStatus.Created => "BoxOpened",
                    _ => "BoxUpdated"
                };
            }

            if (expectedQtyProp.IsModified)
                return "ExpectedQuantityUpdated";

            return "BoxUpdated";
        }

        return null;
    }

    private static string? DeterminePackageActionType(DbContext context, EntityEntry entry, BoxPackage pkg)
    {
        if (entry.State == EntityState.Added)
            return "PackageScanned";

        if (entry.State == EntityState.Modified)
        {
            var isBlockedProp = entry.Property("IsBlocked");
            if (isBlockedProp.IsModified)
            {
                var isNowBlocked = (bool)isBlockedProp.CurrentValue!;
                return isNowBlocked ? "PackageBlocked" : "PackageUnblocked";
            }

            var boxIdProp = entry.Property("BoxId");
            if (boxIdProp.IsModified)
                return null; // skip transfer entries, handled by TransferPackageAsync

            var isRemovedProp = entry.Property("IsRemoved");
            if (isRemovedProp.IsModified && (bool)isRemovedProp.CurrentValue! == true)
            {
                var associatedBox = pkg.Box ?? context.Set<Box>().Find(pkg.BoxId);
                return associatedBox != null && associatedBox.Status == BoxStatus.Cancelled
                    ? "PackageDisassociated"
                    : "PackageRemoved";
            }

            return "PackageUpdated";
        }

        if (entry.State == EntityState.Deleted)
        {
            var associatedBox = pkg.Box ?? context.Set<Box>().Find(pkg.BoxId);
            return associatedBox != null && associatedBox.Status == BoxStatus.Cancelled
                ? "PackageDisassociated"
                : "PackageRemoved";
        }

        return null;
    }

    private static (string? previousValue, string? newValue, string? reason, string? packageBarcode) ExtractAuditValues(EntityEntry entry)
    {
        string? previousValue = null;
        string? newValue = null;
        string? reason = null;
        string? packageBarcode = null;

        if (entry.Entity is Box auditBox)
        {
            if (entry.State == EntityState.Modified)
            {
                var statusProp = entry.Property("Status");
                if (statusProp.IsModified)
                {
                    previousValue = statusProp.OriginalValue?.ToString();
                    newValue = statusProp.CurrentValue?.ToString();
                }
                var qtyProp = entry.Property("ExpectedQuantity");
                if (qtyProp.IsModified)
                {
                    previousValue = qtyProp.OriginalValue?.ToString();
                    newValue = qtyProp.CurrentValue?.ToString();
                }
            }
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                reason = auditBox.ExceptionReason ?? auditBox.BlockReason;
            }
        }
        else if (entry.Entity is BoxPackage auditPkg)
        {
            packageBarcode = auditPkg.PackageBarcode;
            if (entry.State == EntityState.Added)
            {
                newValue = auditPkg.PackageBarcode;
            }
            else if (entry.State == EntityState.Modified)
            {
                var boxIdProp = entry.Property("BoxId");
                if (boxIdProp.IsModified)
                {
                    previousValue = boxIdProp.OriginalValue?.ToString();
                    newValue = boxIdProp.CurrentValue?.ToString();
                }
                var isBlockedProp = entry.Property("IsBlocked");
                if (isBlockedProp.IsModified)
                {
                    previousValue = isBlockedProp.OriginalValue?.ToString();
                    newValue = isBlockedProp.CurrentValue?.ToString();
                }
                var isRemovedProp = entry.Property("IsRemoved");
                if (isRemovedProp.IsModified)
                {
                    previousValue = isRemovedProp.OriginalValue?.ToString();
                    newValue = isRemovedProp.CurrentValue?.ToString();
                    reason = auditPkg.RemovalReason;
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                previousValue = auditPkg.PackageBarcode;
            }
        }

        return (previousValue, newValue, reason, packageBarcode);
    }

    private static string BuildDetailsJson(EntityEntry entry)
    {
        var changedProperties = ExtractChangedProperties(entry);

        var details = new
        {
            EntityName = entry.Entity.GetType().Name,
            State = entry.State.ToString(),
            ChangedProperties = changedProperties.Count > 0 ? changedProperties : null
        };

        return JsonSerializer.Serialize(details, JsonOptions);
    }

    private static Dictionary<string, object?> ExtractChangedProperties(EntityEntry entry)
    {
        var changed = new Dictionary<string, object?>();

        if (entry.State == EntityState.Added)
        {
            foreach (var property in entry.CurrentValues.Properties)
            {
                if (AuditablePropertyAllowlist.Contains(property.Name))
                    changed[property.Name] = entry.CurrentValues[property];
            }
        }
        else if (entry.State == EntityState.Deleted)
        {
            foreach (var property in entry.OriginalValues.Properties)
            {
                if (AuditablePropertyAllowlist.Contains(property.Name))
                    changed[property.Name] = entry.OriginalValues[property];
            }
        }
        else if (entry.State == EntityState.Modified)
        {
            foreach (var property in entry.OriginalValues.Properties)
            {
                if (!AuditablePropertyAllowlist.Contains(property.Name))
                    continue;
                var originalValue = entry.OriginalValues[property];
                var currentValue = entry.CurrentValues[property];
                if (!Equals(originalValue, currentValue))
                {
                    changed[property.Name] = new { Old = originalValue, New = currentValue };
                }
            }
        }

        return changed;
    }

    private static string GetDescription(string actionType)
    {
        return actionType switch
        {
            "BoxCreated" => "New box created.",
            "BoxOpened" => "Box opened and QR code generated.",
            "BoxCancelled" => "Box cancelled.",
            "BoxCompletedAuto" => "Box completed automatically.",
            "BoxCompletedWithException" => "Box closed with an exception.",
            "BoxBlocked" => "Box blocked.",
            "BoxUnblocked" => "Box unblocked.",
            "BoxUpdated" => "Box information updated.",
            "BoxDeleted" => "Box deleted.",
            "PackageScanned" => "Package scanned and assigned to a box.",
            "PackageBlocked" => "Package blocked.",
            "PackageUnblocked" => "Package unblocked.",
            "PackageRemoved" => "Package removed from the box.",
            "PackageDisassociated" => "Package disassociated from a cancelled box.",
            "ExpectedQuantityUpdated" => "Expected quantity updated.",
            _ => $"Action {actionType} recorded."
        };
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context != null)
        {
            AuditChanges(eventData.Context);
        }
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
        {
            AuditChanges(eventData.Context);
        }
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
