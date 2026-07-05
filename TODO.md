# TODO.md - Motherson Box Management Task Board

## Priority Legend
* **P0** = Blocks the MVP, security, or data integrity
* **P1** = Essential MVP feature
* **P2** = Important improvement but not blocking
* **P3** = Future improvement or technical debt

## Task Table

| ID | GSD Reference | Feature / Task | Related Specification Requirements | Priority | Status | Owner | Last Updated | Blocker / Notes | Technical Reference |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **TSK-001** | Phase 1 | Initialize the MVC solution (.NET 8.0, folder structure) | - | P0 | `Completed` | Agent | 2026-07-02 | None | `Program.cs`, `.csproj` |
| **TSK-002** | Phase 1 | Database schema (EF Core entities, context, configurations) | DONNEES-01 | P0 | `Completed` | Agent | 2026-07-02 | None | `ApplicationDbContext.cs` |
| **TSK-003** | Phase 1 | SQL uniqueness constraint on `BoxPackages.PackageBarcode` | SCAN-03 | P0 | `Completed` | Agent | 2026-07-02 | Key to data integrity | EF Core Fluent API |
| **TSK-004** | Phase 1 | Initial EF Core migrations and seed script (users and roles) | DECISION-03 | P0 | `Completed` | Agent | 2026-07-02 | Must pre-seed test roles and users | `/Migrations` |
| **TSK-005** | Phase 1 | Matricule / password authentication (Cookie Auth) | AUTH-01, AUTH-02 | P0 | `Completed` | Agent | 2026-07-02 | Persistent session required | `AccountController.cs` |
| **TSK-006** | Phase 1 | Role-based access control (Operator, Supervisor, Admin) | AUTH-03 | P0 | `Completed` | Agent | 2026-07-02 | MVC authorization filters | `[Authorize(Roles = "...")]` |
| **TSK-007** | Phase 2 / Plan 02-01 | Box creation (type, dimensions, expected quantity) | BOX-01 | P1 | `Completed` | Agent | 2026-07-02 | Validated by build and integration tests | `BoxController.cs` |
| **TSK-008** | Phase 2 / Plan 02-02 | Generate unique box numbers and barcodes | BOX-02, BOX-03 | P1 | `Completed` | Agent | 2026-07-02 | Validated by unit tests and retry generation | `IBoxService` |
| **TSK-009** | Phase 2 / Plan 02-03 | Open boxes dashboard and progress display | BOX-07, BOX-08 | P1 | `Completed` | Agent | 2026-07-02 | Validated by unit and UI integration tests | `HomeController.cs` |
| **TSK-010** | Phase 2 / Plan 02-03 | Direct box access by scan on the home page | HOME-01, HOME-02 | P1 | `Completed` | Agent | 2026-07-02 | Validated by lookup and redirect tests | Separate scan field |
| **TSK-011** | Phase 3 | Box preparation screen and virtual scan simulator | SIM-01 | P1 | `Completed` | Agent | 2026-07-03 | Simulation panel in the UI | `Views/Box/Prepare.cshtml` |
| **TSK-012** | Phase 3 | Scan and assign cable packages (USB wedge / JS listener) | SCAN-01, SCAN-02 | P1 | `Completed` | Agent | 2026-07-03 | Must intercept input | Scanner JS script |
| **TSK-013** | Phase 3 | Uniqueness validation and barcode format distinction | SCAN-04, SCAN-05 | P0 | `Completed` | Agent | 2026-07-03 | Reject box codes during package scan and vice versa | `IScanService` |
| **TSK-014** | Phase 3 | Automatic box closure (expected quantity reached) | SCAN-06 | P1 | `Completed` | Agent | 2026-07-03 | Transition to `Completed` status | `IScanService` / `Box` |
| **TSK-015** | Phase 3 | Handle concurrent scans and optimistic concurrency | - | P0 | `Completed` | Agent | 2026-07-03 | Handle `DbUpdateConcurrencyException` | EF Core `RowVersion` |
| **TSK-016** | Phase 4 | Immutable append-only audit log | AUDIT-01, AUDIT-02 | P0 | `Completed` | Agent | 2026-07-04 | `SaveChangesInterceptor` for EF Core | `BoxAuditLogInterceptor` |
| **TSK-017** | Phase 4 | Resume an open box by another operator | BOX-04 | P1 | `Completed` | Agent | 2026-07-04 | Keep original creator | `IBoxService` |
| **TSK-018** | Phase 4 | Exceptional close with deviation (Supervisor only + reason) | EXC-02 | P1 | `Completed` | Agent | 2026-07-04 | `CompletedWithException` + `CompletionMode=Forced` | `BoxController.cs` |
| **TSK-019** | Phase 4 | Cancel a box (Supervisor only + reason) | EXC-01 | P1 | `Completed` | Agent | 2026-07-04 | `Cancelled` status | `BoxController.cs` |
| **TSK-020** | Phase 4 | Block and unblock boxes / packages (quarantine) | EXC-04 | P1 | `Completed` | Agent | 2026-07-04 | `Blocked` status | `BoxController.cs` |
| **TSK-021** | Phase 4 | Controlled removal, transfer, and disassociation of packages | EXC-03 | P1 | `Completed` | Agent | 2026-07-04 | Traceability with required reason | `IBoxService` |
| **TSK-022** | Phase 4 | Multi-criteria search and filters (status, number, date, user) | - | P2 | `Completed` | Agent | 2026-07-04 | For supervisors and admins | `BoxController.cs` |
| **TSK-023** | Phase 5 | Business unit and integration tests | - | P1 | `Completed` | Agent | 2026-07-04 | None | Test project |
| **TSK-024** | Phase 5 | Security audit (check secrets, logs, and SQL injection) | - | P0 | `Completed` | Agent | 2026-07-04 | None | Static code analysis |
| **TSK-025** | Phase 5 | Prepare local deployment configuration (IIS / Kestrel) | - | P2 | `Completed` | Agent | 2026-07-04 | None | `appsettings.json` |
| **TSK-026** | Phase 5 | Additional E2E test scenarios (concurrency, duplicates, access rights) | SCAN-03 | P1 | `Completed` | Agent | 2026-07-04 | None | `E2ELifecycleTests.cs` |
| **TSK-027** | Phase 5 | Integration tests (HomeController, AuditController) and validation theories (125 tests) | - | P1 | `Completed` | Agent | 2026-07-04 | None | `AuditControllerTests.cs`, `HomeControllerTests.cs`, `AdditionalTests.cs` |
| **TSK-028** | Wave 1 | Multi-criteria box search (Requirement F-20) | F-20 | P1 | `Completed` | Agent | 2026-07-04 | None | `BoxController.cs`, `Index.cshtml` |
| **TSK-029** | Wave 1 | Disassociate packages from a cancelled box (F-28, F-30, RG-21) | F-28, F-30, RG-21 | P1 | `Completed` | Agent | 2026-07-04 | None | `BoxService.cs`, `Details.cshtml` |
| **TSK-030** | Wave 2 | Complete audit and specific action types (F-21, 7.5, 7.6) | F-21, 7.5, 7.6 | P1 | `Completed` | Agent | 2026-07-04 | None | `AuditSaveChangesInterceptor.cs` |
| **TSK-031** | Wave 2 | French localization of error and warning messages (A-10) | A-10 | P2 | `Completed` | Agent | 2026-07-04 | None | `BoxService.cs`, `HomeController.cs` |
| **TSK-032** | Wave 3 | CDC v1.5 compliance for scan rejections, transfer audit, and validation tests | F-21, F-27, RG-20 | P0 | `Completed` | Agent | 2026-07-04 | `dotnet test` green (129/129) after fixing business scan rejections and related traceability | `PackageScanService.cs`, `AuditService.cs`, `AuditSaveChangesInterceptor.cs` |
| **TSK-033** | Out of phase / Documentation | Fix and realign the LaTeX specification (cover version + PDF pagination) | - | P2 | `Completed` | Agent | 2026-07-04 | Rework completed with improved cover, table of contents, and typographic hierarchy | `Cahier_des_charges_Motherson_Box_Management.tex` |
| **TSK-034** | Out of phase / Translation | Translate French UI text, comments, tests, and readable documentation into simple English | - | P2 | `Completed` | Agent | 2026-07-04 | Completed remaining validation and JS translation passes | UI views, controllers, services, docs |
| **TSK-035** | Out of phase / UI Redesign | Completely redesign and rebuild the visual interface of the entire application from end to end | UI specifications | P0 | `Completed` | Agent | 2026-07-04 | Redesigning layout, views, css, and js | UI views, controllers, css, js |
| **TSK-036** | Out of phase / Code Quality | Code quality audit: security hardening, controller split, shared services, audit consolidation | - | P2 | `Completed` | Agent | 2026-07-05 | Security: removed hardcoded password, fixed exception leakage, added null-safe claims, authorization on logout. Code quality: AppRoles constants, WorkstationResolver, BoxMapper DTO, BoxController split into BoxController + BoxOperationsController. Audit: consolidated to interceptor-only (removed 15+ explicit AuditService calls). Performance: removed redundant Include, cleaned dead code. DateTime.UtcNow everywhere. | `Security/AppRoles.cs`, `Services/WorkstationResolver.cs`, `Data/Dtos/BoxMapper.cs`, `Controllers/BoxOperationsController.cs` |
| **TSK-037** | Milestone v1 / Release | Finalize V1 release packaging, Docker onboarding, documentation, version marker, and Git release | Release V1 | P1 | `Completed` | Agent | 2026-07-05 | Release metadata, Docker files, README/docs refresh, GSD milestone alignment, and green validation complete; ready for commit/tag/push | `README.md`, `docs/*`, `.planning/*`, release files |


## Maintenance Rules
1. **GSD Core link:** Every task must be tied to the active phase or matching GSD Core plan.
2. **Task lifecycle:**
   * Set to `In progress` as soon as work starts.
   * If something blocks the task, set it to `Blocked` and describe the blocker in the "Blocker / Notes" column.
   * Set it to `In review` when the code is written but not fully validated.
   * Only set it to `Completed` after:
     * Build validation (`dotnet build` with no errors and no major warnings).
     * Unit tests pass (`dotnet test` green).
     * Manual screen-flow validation.
     * Documentation is updated in `AGENT.md` (schema, ADR, migrations, and so on).
3. **Synchronization:** The statuses in this file must exactly reflect the real project state.
4. **Business integrity:** Never force the `Completed` status if business or security behavior has not been formally tested and proven.
