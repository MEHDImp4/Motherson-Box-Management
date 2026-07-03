# Phase 4: Supervisor Exceptions & Audit Trail - Context

**Gathered:** 2026-07-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Implement immutable audit logging for all database write actions via an EF Core SaveChangesInterceptor, and enable supervisor exception workflows: cancel a box, force-close with deviation, block/unblock boxes and packages, and transfer/retrait packages between open boxes. This phase delivers the complete supervisor control layer on top of the existing box lifecycle and scanning system.

Requirements covered: AUDIT-01, AUDIT-02, AUDIT-03, AUDIT-04, EXC-01, EXC-02, EXC-03, EXC-04.

</domain>

<decisions>
## Implementation Decisions

### Exception Workflow UX
- **D-01:** Exception actions (cancel, force-close, block, transfer, retrait) are accessed via buttons directly on the Box Details page. Supervisor sees the box state and acts immediately — no navigation away.
- **D-02:** Mandatory reason is provided via an inline text input that appears below the button when clicked. Confirm/cancel buttons appear alongside. Lightweight, stays in context, no modal or redirect needed.
- **D-03:** Both Supervisors and Admins see all exception buttons. Same actions for both roles. Role filtering enforced via `[Authorize(Roles = "...")]` on service methods.
- **D-04:** For package transfers, the destination box is selected via a dropdown listing all open boxes (box number + type). Simple and fast — no barcode scanning required.

### Audit Trail Mechanism
- **D-05:** Use an EF Core `SaveChangesInterceptor` to automatically log all Create/Update/Delete operations. Replaces the current manual logging pattern in `ScanPackageAsync`. Catches all writes — no operations can slip through.
- **D-06:** The interceptor captures full state diffs — before/after values in JSON format for each changed property. More data but complete traceability.
- **D-07:** Workstation name is config-based — each workstation has a name in `appsettings.json` or environment variable (e.g., `WorkstationName: "P3-STATION-01"`). Injected via `IConfiguration`, consistent across all operations.
- **D-08:** Audit logs are viewable on a dedicated `/Audit/Index` page accessible from the navigation menu. Page supports filtering by box, user, and date range. Not embedded in Box Details — keeps the details view focused.

### Transfer/Retrait Mechanics
- **D-09:** Transfer moves the `BoxPackage` record from source box to destination box. Source `CurrentQuantity` decrements, destination increments. Package record updated with new `BoxId`. Single atomic transaction.
- **D-10:** Retrait deletes the `BoxPackage` record entirely from the database. Box `CurrentQuantity` decrements. Package barcode is freed and can be re-scanned into any open box.
- **D-11:** After a retrait, the same package barcode can be scanned again into any open box. Physical reality — a retracted package can be reassigned.
- **D-12:** Transfer form is a single view with dropdown of open boxes, package list to select, and a reason textarea. Submit button confirms. No multi-step wizard.

### Block/Unblock Scope
- **D-13:** Supervisors can block both boxes (halts all scans to the box) AND individual packages (prevents the package from being scanned elsewhere). Two distinct block types.
- **D-14:** When a blocked package is unblocked, it can be scanned into any open box again. No permanent quarantine after unblock.
- **D-15:** Block/Unblock buttons appear on the Box Details page — for box-level blocking in the status area, and for package-level blocking in the package list row. Consistent with Exception workflow UX.
- **D-16:** Blocked status is indicated with a red Bootstrap badge/icon. Clear visual distinction from Open/Completed states.

### Agent's Discretion
- No areas marked as agent discretion — all decisions were explicitly made by the user.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements & Specs
- `.planning/REQUIREMENTS.md` — AUDIT-01 through AUDIT-04, EXC-01 through EXC-04 requirements
- `.planning/ROADMAP.md` §Phase 4 — Goal, success criteria, and plan status

### Existing Implementation
- `MothersonBoxManagement/Entities/Box.cs` — Box entity with RowVersion, Status, ExpectedQuantity, CurrentQuantity
- `MothersonBoxManagement/Entities/BoxAuditLog.cs` — Audit log entity (Id, BoxId, ActionType, UserId, Timestamp, WorkstationName, DetailsJson)
- `MothersonBoxManagement/Entities/BoxPackage.cs` — Package entity with unique index on PackageBarcode
- `MothersonBoxManagement/Entities/BoxStatus.cs` — Status enum (Open, Completed, CompletedWithException, Cancelled, Archived, Blocked)
- `MothersonBoxManagement/Services/IBoxService.cs` — Current service interface
- `MothersonBoxManagement/Services/BoxService.cs` — ScanPackageAsync with manual audit logging pattern
- `MothersonBoxManagement/Controllers/BoxController.cs` — Existing controller with Scan/ScanAjax actions
- `MothersonBoxManagement/Data/ApplicationDbContext.cs` — EF Core DbContext with entity configurations

### Architecture & Conventions
- `GEMINI.md` — Coding conventions (PascalCase, thin controllers, service layer, async/await, ViewModel-only views), EF Core unique indexes, transaction requirements, security rules

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `BoxService.ScanPackageAsync`: Full scan logic with retry loop, transaction handling, optimistic concurrency catch. Pattern for new service methods.
- `BoxAuditLog` entity: Already defined with Id, BoxId, ActionType, UserId, Timestamp, WorkstationName, DetailsJson. Can be extended or used as-is.
- `BoxStatus` enum: Already includes `Blocked` and `CompletedWithException` values. No new enum members needed.
- `ApplicationDbContext`: Already has `DbSet<BoxAuditLog>` and entity configurations. SaveChangesInterceptor hooks into this.
- `BoxController.ScanAjax`: JSON response pattern for AJAX operations — reusable for exception actions.

### Established Patterns
- **Service layer pattern**: IBoxService/BoxService with async methods, CancellationToken propagation, DTO-based returns.
- **Transaction handling**: `BeginTransactionAsync` / `CommitAsync` / `RollbackAsync` with retry loop for concurrency.
- **TempData flash messages**: Success/error stored in TempData, displayed as Bootstrap alerts.
- **ViewModel pattern**: Dedicated ViewModels for all form data — entities never exposed to views.
- **French UI**: All user-facing strings in French.

### Integration Points
- `BoxDetailsDto` needs to show block status and exception action buttons.
- `BoxController` needs new actions: Cancel, ForceClose, Block, Unblock, Transfer, Retrait.
- `IBoxService`/`BoxService` needs new methods for each exception operation.
- `ApplicationDbContext` needs a `SaveChangesInterceptor` for automatic audit logging.
- Navigation menu (`_Layout.cshtml`) needs an Audit Log link.

</code_context>

<specifics>
## Specific Ideas

- Exception buttons on Details page should be visually distinct (e.g., danger/warning Bootstrap button classes) to signal destructive actions.
- The inline reason input should auto-focus when revealed, with a character count or minimum length hint.
- Blocked boxes should still be viewable in the dashboard but with a red badge — not hidden.
- Transfer dropdown should show box number + type + current quantity to give the supervisor context for destination choice.
- Audit log page should support pagination — don't load all logs at once.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 04-supervisor-exceptions-audit-trail*
*Context gathered: 2026-07-04*
