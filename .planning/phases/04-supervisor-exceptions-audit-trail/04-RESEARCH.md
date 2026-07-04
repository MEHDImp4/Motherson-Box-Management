# Phase 4: Supervisor Exceptions & Audit Trail - Research

## Technical Architecture & Code Patterns

### 1. EF Core `SaveChangesInterceptor` Implementation
- **Current Approach:** `BoxService.ScanPackageAsync` manually adds a `BoxAuditLog` record to `_context.BoxAuditLogs` within the same transaction. This is error-prone for exceptions (e.g., transfers, modifications) because developers might forget to add the log.
- **Proposed Approach:** Implement an `ISaveChangesInterceptor` (or derive from `SaveChangesInterceptor`).
  - Override `SavingChangesAsync`.
  - Iterate through `eventData.Context.ChangeTracker.Entries()`.
  - Filter for relevant entities (`Box`, `BoxPackage`).
  - Extract `OriginalValues` and `CurrentValues` to create a JSON diff.
  - Automatically append a `BoxAuditLog` entity for each tracked change.
  - Inject `IConfiguration` to retrieve `WorkstationName` or `IHttpContextAccessor` to get workstation info and the current user's ID (or pass the User ID down to the context if not available).
- **Location:** Create `MothersonBoxManagement/Data/Interceptors/AuditTrailInterceptor.cs`. Register it in `Program.cs` via `options.AddInterceptors(...)` during DbContext setup.

### 2. Audit Log Schema
- **Current state:** `BoxAuditLog` already exists with `Id`, `BoxId`, `ActionType`, `UserId`, `Timestamp`, `WorkstationName`, and `DetailsJson`.
- **Validation:** This schema is sufficient to store full state diffs in `DetailsJson`.
- **Migrations:** No migration is needed for `BoxAuditLog`.

### 3. Supervisor Exception Service Methods
- **Current Patterns:** `IBoxService` and `BoxService` use async methods returning a DTO or `ScanResult`. They use `CancellationToken`. They handle transactions using `BeginTransactionAsync`.
- **Proposed Methods for `IBoxService`:**
  - `Task<OperationResult> ModifyExpectedQuantityAsync(int boxId, int newQuantity, string reason, int userId, CancellationToken ct);`
  - `Task<OperationResult> CancelBoxAsync(int boxId, string reason, int userId, CancellationToken ct);`
  - `Task<OperationResult> ForceCloseBoxAsync(int boxId, string reason, int userId, CancellationToken ct);`
  - `Task<OperationResult> BlockBoxAsync(int boxId, string reason, int userId, CancellationToken ct);`
  - `Task<OperationResult> UnblockBoxAsync(int boxId, string reason, int userId, CancellationToken ct);`
  - `Task<OperationResult> TransferPackageAsync(int packageId, int targetBoxId, string reason, int userId, CancellationToken ct);`
  - `Task<OperationResult> RetraitPackageAsync(int packageId, string reason, int userId, CancellationToken ct);`
  - `Task<OperationResult> BlockPackageAsync(int packageId, string reason, int userId, CancellationToken ct);`
  - `Task<OperationResult> UnblockPackageAsync(int packageId, string reason, int userId, CancellationToken ct);`

### 4. Migration Strategy for Packages
- **Current state:** `BoxPackage` entity lacks a blocked status. We need a way to block packages.
- **Proposed change:** Add `public bool IsBlocked { get; set; }` and `public string? BlockReason { get; set; }` to `BoxPackage.cs`. Add a migration.

### 5. UI Integration
- **Box Details (`Views/Box/Details.cshtml`):** Add Exception buttons (Modify, Cancel, Force Close, Block) that appear for Superviseur/Admin. Use an inline form or Bootstrap collapse to show the reason input field.
- **Package List in Details:** Add Transfer/Retrait/Block buttons per package row.
- **Audit Logs (`Views/Audit/Index.cshtml`):** Create an `AuditController` and an `Index` view. Fetch logs using EF Core pagination (e.g., `.Skip().Take()`).

### 6. Transaction & Concurrency Patterns
- **Concurrency:** Exception operations must wrap changes in a try-catch for `DbUpdateConcurrencyException`, reloading entries, and retrying if necessary (as seen in `ScanPackageAsync`).
- **Transactions:** Use `_context.Database.BeginTransactionAsync(cancellationToken)` for any operation that spans multiple entities (e.g., Transferring a package).

### 7. Authorization
- Use `[Authorize(Roles = "Superviseur,Admin")]` on the exception endpoints in `BoxController` (or a dedicated `ExceptionsController`).

## Summary of Findings
- **Risk:** Implementing the `SaveChangesInterceptor` will double-log `PackageScan` if we don't remove the manual `BoxAuditLog.Add` from `ScanPackageAsync`.
- **Risk:** EF Core Interceptors might run into issues with DI scoping (e.g., injecting `IHttpContextAccessor` into a singleton interceptor). The interceptor must be registered correctly (scoped, or resolve services dynamically).
- The transition from manual audit logging to interceptor-based logging is the highest technical risk and must be done atomically.

## RESEARCH COMPLETE
