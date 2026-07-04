using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using MothersonBoxManagement.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MothersonBoxManagement.Data.Interceptors;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    private void AuditChanges(DbContext context)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        int? userIdVal = null;

        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int id))
            {
                userIdVal = id;
            }
        }

        if (userIdVal == null)
        {
            // Fallback for non-HTTP context / background tasks / seeding
            var systemUser = context.Set<User>().AsNoTracking().FirstOrDefault(u => u.Matricule == "AD001" || u.Role == "Administrator");
            if (systemUser != null)
            {
                userIdVal = systemUser.Id;
            }
            else
            {
                var firstUser = context.Set<User>().AsNoTracking().FirstOrDefault();
                if (firstUser != null)
                {
                    userIdVal = firstUser.Id;
                }
            }
        }

        if (userIdVal == null)
        {
            return;
        }

        string workstationName = _configuration["WorkstationName"] ?? "DEFAULT-STATION";

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not BoxAuditLog &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        var auditLogs = new List<BoxAuditLog>();

        foreach (var entry in entries)
        {
            int? boxId = null;
            Box? boxNavigation = null;

            if (entry.Entity is Box box)
            {
                if (entry.State == EntityState.Added)
                {
                    boxNavigation = box;
                }
                else
                {
                    boxId = box.Id;
                }
            }
            else if (entry.Entity is BoxPackage package)
            {
                boxId = package.BoxId;
            }

            string actionType = entry.State switch
            {
                EntityState.Added => entry.Entity is BoxPackage ? "PackageScan" : "Insert",
                EntityState.Modified => "Update",
                EntityState.Deleted => "Delete",
                _ => entry.State.ToString()
            };

            var originalValues = new Dictionary<string, object?>();
            var currentValues = new Dictionary<string, object?>();
            var changedProperties = new Dictionary<string, object?>();

            if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                foreach (var property in entry.OriginalValues.Properties)
                {
                    originalValues[property.Name] = entry.OriginalValues[property];
                }
            }

            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                foreach (var property in entry.CurrentValues.Properties)
                {
                    currentValues[property.Name] = entry.CurrentValues[property];
                }
            }

            if (entry.State == EntityState.Added)
            {
                foreach (var property in entry.CurrentValues.Properties)
                {
                    changedProperties[property.Name] = entry.CurrentValues[property];
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                foreach (var property in entry.OriginalValues.Properties)
                {
                    changedProperties[property.Name] = entry.OriginalValues[property];
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                foreach (var property in entry.OriginalValues.Properties)
                {
                    var originalValue = entry.OriginalValues[property];
                    var currentValue = entry.CurrentValues[property];
                    if (!Equals(originalValue, currentValue))
                    {
                        changedProperties[property.Name] = new { Old = originalValue, New = currentValue };
                    }
                }
            }

            var details = new
            {
                EntityName = entry.Entity.GetType().Name,
                State = entry.State.ToString(),
                ChangedProperties = changedProperties.Count > 0 ? changedProperties : null
            };

            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            string detailsJson = JsonSerializer.Serialize(details, options);

            var auditLog = new BoxAuditLog
            {
                ActionType = actionType,
                UserId = userIdVal.Value,
                Timestamp = DateTime.Now,
                WorkstationName = workstationName,
                DetailsJson = detailsJson
            };

            if (boxNavigation != null)
            {
                auditLog.Box = boxNavigation;
            }
            else if (boxId != null)
            {
                auditLog.BoxId = boxId;
            }

            auditLogs.Add(auditLog);
        }

        if (auditLogs.Count > 0)
        {
            context.AddRange(auditLogs);
        }
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
