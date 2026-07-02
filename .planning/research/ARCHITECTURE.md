# Architecture Patterns

**Domain:** Barcode Packaging Box Management Web App
**Researched:** 2026-07-02

## Recommended Architecture

The application will follow a clean, standard **ASP.NET Core MVC** architecture:

```
[Browser / UI] 
  │  ▲ (HTML/CSS/JS + Bootstrap 5.3)
  ▼  │
[MVC Controllers]
  │  ▲ (Filters for authorization: Operator/Supervisor/Admin)
  ▼  │
[Application Services / EF Core DbContext]
  │  ▲ (DbUpdateConcurrencyException Handling & Transactions)
  ▼  │
[EF Core ISaveChangesInterceptor] 
  │  ▲ (Automatic serialization of audit logs)
  ▼  │
[SQL Server Database] (Tables: Users, Boxes, BoxPackages, BoxAuditLogs)
```

### Component Boundaries

| Component | Responsibility | Communicates With |
|-----------|---------------|-------------------|
| `AccountController` | Handles matricule login, sets cookie authentication claims. | `UserService`, `Views` |
| `BoxController` | Manages box creation, search, detail view, and direct scanner lookup. | `BoxService`, `Views` |
| `ScanController` | Endpoint for AJAX package scan requests. Handles scan success/failure events. | `BoxService`, `Js Barcode Handler` |
| `MothersonDbContext` | Database context. Configures unique indexes on `BarcodeValue` and `PackageBarcode`. | `SQL Server` |
| `AuditSaveChangesInterceptor` | Scans the tracker before save to write entries to `BoxAuditLogs`. | `MothersonDbContext` |

### Data Flow

#### Package Scanning Flow:
1. **Physical Scan / Virtual Simulator Scan** types barcode + triggers `Enter`.
2. **Javascript Event Listener** captures the string, intercepts default submit, and performs a `POST` to `/Scan/Process`.
3. **Scan Controller** starts a database transaction.
4. **BoxService** performs uniqueness check, status verification, and adds the package.
5. **EF Core** invokes `AuditSaveChangesInterceptor` to log `PackageScanned`.
6. **Transaction commits**. If a duplicate package is scanned concurrently, the unique index on `BoxPackages.PackageBarcode` throws a `DbUpdateException` (violating unique constraint), rollback is executed, and client receives a friendly error.

## Patterns to Follow

### Pattern 1: EF Core SaveChanges Interceptor for Auditing
**What:** Intercepts database writes to write history.
**When:** Whenever entity state changes.
**Example:**
```csharp
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var matricule = _httpContextAccessor.HttpContext?.User?.FindFirst("Matricule")?.Value ?? "System";
        var workstation = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Modified)
            {
                // Capture old and new values, write to BoxAuditLogs
            }
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

### Pattern 2: Optimistic Concurrency with RowVersion
**What:** Uses `rowversion` column to prevent concurrent write collisions on `Boxes` expected or current quantities.
**When:** When updating `Boxes` status or quantities.
**Example:**
```csharp
public class Box
{
    public int Id { get; set; }
    public string BoxNumber { get; set; }
    public int CurrentQuantity { get; set; }
    public string Status { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; }
}
```

## Anti-Patterns to Avoid

### Anti-Pattern 1: Database Table Per Box
**What:** Creating a dynamic SQL table every time a box is initialized in the system.
**Why bad:** Destroys indexing, makes cross-box searches and global uniqueness constraints on `PackageBarcode` nearly impossible, and leads to schema drift and database bloat.
**Instead:** Rely on a unified relational schema: `Boxes` table holds box details, and `BoxPackages` table holds package associations with a foreign key to `Boxes.Id` and a unique constraint on `PackageBarcode`.

### Anti-Pattern 2: Mixing Scanner Inputs and Keyboard Inputs on the Same Text Field
**What:** Having a single input box for both box scan search and package scanner inputs without context differentiation.
**Why bad:** Scans meant for box search will register as package allocations, causing invalid scans or accidental packaging errors.
**Instead:** Keep the homepage direct box scanner input element completely separated from the box package preparation scanning page.

## Sources

- ASP.NET Core MVC Architecture Best Practices.
- Entity Framework Core Interceptors Reference.
