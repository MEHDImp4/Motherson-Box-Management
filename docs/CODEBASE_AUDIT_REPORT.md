# Codebase Audit Report

Audit date: 2026-07-08  
Repository state audited: current working tree on `main`, not the clean `origin/main` tag. `git status --short --branch` showed many modified, deleted, and untracked files; findings therefore apply to the local working tree unless noted.

## Executive Summary

Overall security level: High  
Overall performance level: Medium  
Overall maintainability level: Medium  
Overall production-readiness level: Medium

Finding counts: Critical 0, High 2, Medium 7, Low 5, Info 3

Top risks and production blockers:

- `SEC-001`: any authenticated user can call box creation/open/print routes that are not role-restricted in `BoxController`.
- `SEC-002`: seeded operator, supervisor, and administrator accounts use one documented default password.
- `DB-001`: package removal/disassociation physically deletes `BoxPackages` rows, conflicting with traceability expectations.
- `OPS-001`: Docker Compose runs the web app in `Development` and uses SQL Server `sa`, so it is not production-ready.
- `OPS-002`: there is no CI workflow in the repository and no health/readiness endpoint.

Audit limitations:

- No external vulnerability database or package advisory scan was run because the audit stayed local/read-only.
- No deployed environment, production database, printer, barcode scanner, CI run, or backup/restore procedure was available.
- The worktree is dirty, so line evidence may differ from `origin/main` or tag `v1`.
- The audit created only this report and `CODEBASE_AUDIT_IMPLEMENTATION_PLAN.md`; no application source files were modified.

## Architecture Map

Technologies detected:

- ASP.NET Core MVC on .NET 8.0 (`MothersonBoxManagement/MothersonBoxManagement.csproj`).
- EF Core SQL Server with migrations (`Microsoft.EntityFrameworkCore.SqlServer`, `ApplicationDbContext`).
- Cookie authentication with role claims (`Program.cs`, `AccountController.cs`).
- Razor views, Bootstrap-like custom CSS, JavaScript scanner and station-terminal scripts.
- Docker Compose with SQL Server 2022 and a web container.
- xUnit integration/unit tests using `Microsoft.AspNetCore.Mvc.Testing` and EF Core InMemory.

Main modules:

- Frontend: Razor views under `MothersonBoxManagement/Views`, static assets in `wwwroot`.
- Backend/API layer: MVC controllers under `Controllers`.
- Business services: `BoxService`, `PackageScanService`, `BoxTemplateService`, `UserService`, `AuthenticationService`, print/QR/workstation services.
- Database layer: EF entities under `Entities`, DTO projections under `Data/Dtos`, migrations under `Migrations`.
- Auth flow: `AccountController.Login` validates matricule/password through `AuthenticationService`, signs a cookie with name, role, full name, and matricule claims.
- Authorization flow: controller-level `[Authorize]` and role-specific `[Authorize(Roles = ...)]` on users, audit, box templates, and supervisor operations.
- Primary data flows: template -> box creation/opening -> QR/print job -> package association -> audit log; supervisor exception operations update box/package state with reasons.
- External integrations: SQL Server, local printer subsystem (`System.Drawing.Printing`), QR generation via QRCoder.
- Deployment flow: Dockerfile publishes the MVC app; Compose starts SQL Server and web app; `Program.cs` auto-applies migrations and seeds users at startup.
- High-risk areas: box creation/open/print, seeded accounts, package removal/disassociation, scan concurrency, audit logging, Docker deployment defaults.

## Specialist Reviewer Summaries

Security reviewer:

- Scope: authentication, sessions, authorization, CSRF, rate limiting, frontend XSS patterns, secrets, dependency/supply chain.
- Files reviewed: `Program.cs`, all controllers, `AuthenticationService.cs`, `UserService.cs`, `DbInitializer.cs`, `SecurityAuditTests.cs`, Razor views, JS, configs, Docker files.
- Entry-point tracing performed for `POST /Account/Login`, `POST /Box/CreateFromTemplate`, `POST /Box/AssociatePackage`, supervisor operation routes, and user-management routes.
- Confirmed findings: `SEC-001`, `SEC-002`, `SEC-003`, `SEC-004`, `SEC-005`.
- False positives/rejected: SQL injection from EF LINQ queries, open redirect in login, unsafe scanner `innerHTML` as exploitable XSS.

Database security and integrity reviewer:

- Scope: EF schema, migrations, constraints, transactions, concurrency, deletes, indexes.
- Files reviewed: `ApplicationDbContext.cs`, entities, migrations, `BoxService.cs`, `PackageScanService.cs`, audit interceptor.
- Confirmed findings: `DB-001`, `DB-002`, `DB-003`.
- Deferred: real SQL execution plans and backup/restore testing need a production-like database.

Backend performance and reliability reviewer:

- Scope: query limits, transaction placement, startup, printing behavior, error handling.
- Files reviewed: services, controllers, `Program.cs`, tests.
- Confirmed findings: `PERF-001`, `PERF-002`, `REL-001`.

Frontend performance and security reviewer:

- Scope: JS unsafe rendering, local storage, page workflow, client-side validation.
- Files reviewed: `scanner.js`, `station-terminal.js`, `Login.cshtml`, `Details.cshtml`, `Settings.cshtml`, layout.
- Confirmed findings: `FRONT-001`, `FRONT-002`.
- Rejected: scanner overlay XSS was not confirmed because messages are escaped before insertion.

Architecture and code-quality reviewer:

- Scope: responsibility boundaries, role constants, docs drift, package pinning, dirty tree/reproducibility.
- Files reviewed: controllers, services, docs, csproj files, planning docs.
- Confirmed findings: `ARCH-001`, `ARCH-002`, `ARCH-003`.

DevOps and production-readiness reviewer:

- Scope: Docker, environment, startup validation, migrations, health checks, CI/CD, container hardening.
- Files reviewed: Dockerfile, Compose, appsettings, docs, `.gitignore`, `.dockerignore`.
- Confirmed findings: `OPS-001`, `OPS-002`, `OPS-003`, `OPS-004`.

Testing and reliability reviewer:

- Scope: existing tests and high-value gaps.
- Evidence: `dotnet build --no-restore` passed with 0 warnings/errors; `dotnet test --no-build` passed 107 tests.
- Files reviewed: all test files, especially `SecurityAuditTests.cs`, `ConcurrencyTests.cs`, controller tests, service exception tests.
- Confirmed findings: `TEST-001`, `TEST-002`, `TEST-003`.

## Master Findings Table

| ID | Severity | Category | Validation Status | File(s) | Description | Impact | Evidence | Recommended Fix | Effort |
| -- | -------- | -------- | ----------------- | ------- | ----------- | ------ | -------- | --------------- | ------ |
| SEC-001 | High | Authorization | Confirmed by multiple agents | `MothersonBoxManagement/Controllers/BoxController.cs`, `MothersonBoxManagement/Services/BoxTemplateService.cs` | Box creation/open/print/settings actions require only authentication, not Supervisor/Admin roles. | Operators can create/open boxes and print labels, bypassing the documented role boundary. | `BoxController` has class `[Authorize]` only at line 12; `CreateFromTemplate` at lines 65-73 calls `CreateBoxFromTemplateAsync`; service creates `Status = BoxStatus.Open` at line 143. | Add explicit role attributes/policies to sensitive actions; add negative operator tests. | Small |
| SEC-002 | High | Authentication | Confirmed by multiple agents | `DbInitializer.cs`, `README.md`, `docs/GETTING-STARTED.md` | Seeded Operator, Supervisor, and Administrator accounts share one documented default password. | Anyone with repo/docs can sign in if the app is reachable and accounts remain active. | `DbInitializer.cs` line 12 defines the default password and lines 16-18 seed OP/SP/AD users; README lines 57-61 and docs lines 120-124 publish credentials. | Restrict seeding to Development/demo only; force first-login rotation or random per-install bootstrap password; remove public password tables. | Medium |
| SEC-003 | Medium | Secrets and Logging | Confirmed by one agent | `BoxController.cs`, `PrintJobService.cs`, `BoxPrintJob` | Raw printer exception messages are persisted to `BoxPrintJobs.ErrorMessage`. | Printer driver/network errors can persist internal host, share, or driver details. | `BoxController.cs` lines 84 and 133 pass `ex.Message`; `ApplicationDbContext.cs` line 102 stores up to 500 chars. | Store sanitized user-facing failure categories; log detailed exception only to protected logs. | Small |
| SEC-004 | Medium | Session Security | Confirmed by one agent | `Program.cs` | Cookies are persistent for 60 minutes with sliding expiration but there is no revocation/security-stamp check after password reset or deactivation. | A deactivated or reset user can keep using an already-issued cookie until it expires. | Cookie configured at `Program.cs` lines 74-89; `UserService.DeleteUserAsync` and `ResetPasswordAsync` update DB only. | Add cookie validation event checking `IsActive` and a session version/security stamp. | Medium |
| SEC-005 | Low | Account Enumeration | Confirmed by one agent | `AccountController.cs` | Login reveals remaining-attempt count and lockout state for a submitted matricule. | Helps attackers tune brute-force attempts against known matricules. | `AccountController.cs` lines 35-53 vary messages by lockout/remaining attempts. | Use a generic error externally; log detailed lockout state server-side. | Small |
| DB-001 | High | Data Integrity | Confirmed by multiple agents | `BoxService.cs`, `AuditSaveChangesInterceptor.cs` | Retrait/disassociate physically delete `BoxPackage` rows. | Package history and uniqueness evidence are lost; removed barcodes can be re-scanned into other boxes. | `BoxService.cs` lines 520-536 and 550-566 call `_context.BoxPackages.Remove(package)`; audit records only previous barcode, not full immutable row. | Replace deletes with soft-disassociation/status fields and preserve original rows; optionally use a historical association table. | Medium |
| DB-002 | Medium | Transactions and Concurrency | Confirmed by one agent | `PackageScanService.cs` | Duplicate package existence check and box state checks happen before transaction start. | Race windows are left to the unique index and can produce fragile behavior under load. | `PackageScanService.cs` lines 46-91 run checks; transaction starts at line 112. | Start transaction before read checks; use appropriate isolation or retry around the full unit. | Medium |
| DB-003 | Medium | Schema/Business Drift | Confirmed by one agent | `ApplicationDbContext.cs`, `Box.cs`, `BoxTemplateViewModel.cs`, migrations, `AGENT.md` | Current code stores box dimensions as decimals, while repo rules still state whole-centimeter integers. | Business validation and documentation conflict; fractional dimensions can be accepted. | Decimal entity fields at `Box.cs` line 9 and `BoxTemplate.cs` line 9; decimal columns at `ApplicationDbContext.cs` lines 35-37 and 112-114; `BoxTemplateViewModel` allows `0.01` at lines 19, 23, 27. | Decide canonical rule; either update docs/specs/tests or enforce integer centimeters in model/schema. | Medium |
| PERF-001 | Medium | Backend Performance | Confirmed by one agent | `BoxService.cs`, `AuditController.cs` | Search and audit endpoints can run broad counts/lists without strong date or page defaults. | Large tables can slow operator dashboards and audit views. | `SearchBoxesAsync` returns `ToListAsync` without pagination; `AuditController` counts all matching rows before paginating. | Add pagination/default date windows to box search; add indexes for common audit filters. | Medium |
| PERF-002 | Low | Backend Reliability | Confirmed by one agent | `BoxController.cs`, `PrinterService.cs` | Printing is executed synchronously in HTTP requests. | Printer delays can block box creation/opening workflows. | `BoxController.cs` lines 79 and 128 call `_printerService.PrintLabel`; `PrinterService.PrintLabel` calls `printDoc.Print()`. | Queue print jobs or make printing optional/retriable; keep request path fast. | Medium |
| FRONT-001 | Low | Frontend Security | Confirmed by one agent | `Login.cshtml`, `scanner.js` | The frontend uses `innerHTML` for static SVG/template insertion. Current dynamic messages appear escaped, but the pattern is risky. | Future changes could introduce DOM XSS if unescaped input reaches these helpers. | `Login.cshtml` line 59 and `scanner.js` lines 42-62 use `innerHTML`; `scanner.js` line 188 escapes message content. | Use DOM APIs for dynamic nodes; keep escaping tests. | Small |
| FRONT-002 | Info | Frontend Data Handling | Confirmed by one agent | `station-terminal.js`, `Settings.cshtml`, layout | Station and printer names are stored in browser `localStorage`. | Acceptable for non-secret workstation labels, but not centrally managed or tamper-resistant. | `station-terminal.js` lines 16-36; `Settings.cshtml` text describes localStorage. | Keep non-secret only; validate/sanitize submitted station/printer names server-side. | Small |
| ARCH-001 | Medium | Maintainability | Confirmed by one agent | `AppRoles.cs`, controllers/tests/docs | Role names have English and French variants, while `Operator` is not centralized in `AppRoles`. | Authorization policies can drift and tests may miss role mismatch. | `AppRoles.cs` only defines Supervisor/Admin variants; controllers repeat role lists. | Define all roles once and use named policies. | Small |
| ARCH-002 | Low | Supply Chain | Confirmed by one agent | `.csproj` files | Several package references use floating `8.0.*` versions. | Reproducible builds can change after restore. | App csproj lines 15 and 19; test csproj lines 14-15. | Pin exact versions and use a lock file or central package management. | Small |
| ARCH-003 | Info | Reproducibility | Confirmed by one agent | Git worktree | Current audit ran against a dirty worktree with many modified/deleted/untracked files. | Findings may not reproduce from clone/tag. | `git status --short --branch` showed many local changes and untracked migrations/services/views. | Commit or stash coherent change sets before release audits. | Small |
| OPS-001 | High | Production Readiness | Confirmed by multiple agents | `docker-compose.yml`, `Dockerfile` | Compose runs the web app in `Development` and connects as SQL Server `sa`; containers lack non-root user hardening. | Production deployment could expose dev behavior and over-privileged DB access. | `docker-compose.yml` line 22 sets Development; line 24 uses `User Id=sa`; Dockerfile has no `USER`; SQL image is `2022-latest` at line 3. | Create production compose/profile with non-sa DB login, pinned image digest/version, `Production`, non-root user where possible. | Medium |
| OPS-002 | Medium | CI/CD and Health | Confirmed by one agent | Repository root, `Program.cs` | No `.github` workflow was found and no health/readiness endpoint is mapped. | Build/test gates and runtime dependency checks are manual. | `.github` file search returned no workflows; `Program.cs` maps only MVC route and no health checks. | Add CI for restore/build/test and `/health` endpoint checking DB connectivity. | Medium |
| OPS-003 | Medium | Deployment Safety | Confirmed by one agent | `Program.cs` | Migrations run automatically at startup. | Multiple app instances or failed migrations can block startup or change production schema unexpectedly. | `Program.cs` line 134 calls `db.Database.MigrateAsync()`. | Use a controlled migration job or deployment step with rollback notes. | Medium |
| OPS-004 | Low | Host Configuration | Confirmed by one agent | `appsettings.json`, docs | `AllowedHosts` is `localhost`, which is safer than `*` but conflicts with container/server IP deployment unless overridden. | Misconfiguration can cause outages when deployed behind a hostname/IP. | `appsettings.json` line 8. | Document required production override and validate startup config. | Small |
| TEST-001 | Medium | Security Tests | Confirmed by one agent | `SecurityAuditTests.cs`, `BoxControllerTests.cs` | Tests cover supervisor operation denial but not operator denial for `POST /Box/CreateFromTemplate`, `POST /Box/Open`, or `GET /Box/Print`. | The `SEC-001` regression can pass the suite. | `SecurityAuditTests.cs` endpoint list omits CreateFromTemplate/Open/Print; `BoxControllerTests.cs` positive creation test uses supervisor. | Add negative role-boundary tests for every sensitive route. | Small |
| TEST-002 | Medium | Integration Tests | Confirmed by one agent | Tests | Current tests use InMemory for many paths, which does not enforce SQL Server uniqueness/isolation semantics. | Concurrency and unique-index behavior can differ from production SQL Server. | Test project references EF InMemory; build/test passed 107 tests. | Add a small SQL Server/Testcontainers suite for package uniqueness, rowversion, migrations, and transaction behavior. | Medium |
| TEST-003 | Low | Operational Tests | Confirmed by one agent | Docker/docs | No automated Docker startup, migration, or health validation test was found. | Release packaging can regress outside unit/integration tests. | No CI workflow; Docker Compose only manually documented. | Add smoke test that starts compose, checks `/Account/Login` and `/health`, then tears down. | Medium |

## Detailed Findings

### 1. Security

#### Authentication and Session Security

`SEC-002` High: shared documented seeded credentials  
Validation status and reviewers: Confirmed by multiple agents (Security, DevOps).  
Paths and locations: `DbInitializer.cs` lines 12, 16-18, 26; `README.md` lines 57-61; `docs/GETTING-STARTED.md` lines 120-124.  
Entry point and sensitive action: `/Account/Login` -> `AuthenticationService.ValidateCredentialsAsync` -> seeded Administrator/Supervisor/Operator accounts.  
Attack scenario: a person with repository/docs access signs in as `AD001`, `SP001`, or `OP001` if those seeded accounts exist in a reachable environment.  
Impact: unauthorized access, including administrator actions if bootstrap users are left active.  
Remediation: seed only in Development/demo, randomize bootstrap credentials, force password change, and remove static password tables from general docs.  
Validation after remediation: test that Production startup does not create default accounts; test first-login rotation or bootstrap secret flow.  
Effort: Medium. Business behavior change: Yes, login bootstrap behavior changes.

`SEC-004` Medium: no cookie revocation after account changes  
Validation status and reviewers: Confirmed by one agent.  
Paths and locations: `Program.cs` lines 74-89; `UserService.cs` reset/deactivation methods.  
Failure scenario: an admin deactivates or resets a user, but an existing browser cookie remains accepted until expiry.  
Impact: delayed enforcement of account disable/reset.  
Remediation: add `CookieAuthenticationEvents.OnValidatePrincipal` to check user active status and a session/security-stamp value.  
Validation after remediation: integration test signs in, deactivates user, then verifies next request signs out or redirects.  
Effort: Medium. Business behavior change: No, it enforces expected security state sooner.

`SEC-005` Low: login messages expose remaining-attempt state  
Validation status and reviewers: Confirmed by one agent.  
Evidence: `AccountController.cs` lines 35-53.  
Impact: minor enumeration/brute-force tuning help.  
Remediation: use one generic user-facing failure message and keep details in logs/metrics.  
Validation after remediation: invalid-user and invalid-password responses have indistinguishable user-facing text.  
Effort: Small. Business behavior change: Minor UX copy change.

#### Authorization

`SEC-001` High: sensitive box actions only require authentication  
Validation status and reviewers: Confirmed by multiple agents (Security, Testing).  
Paths and locations: `BoxController.cs` class `[Authorize]` line 12; `CreateFromTemplate` lines 65-73; `Open` line 116; `Print` line 149; `Settings` line 211; `BoxTemplateService.cs` lines 120 and 143.  
Entry point and sensitive action: `POST /Box/CreateFromTemplate` -> authenticated user ID from cookie -> `CreateBoxFromTemplateAsync` -> new open box persisted and label printed.  
Attack scenario: an Operator posts to `/Box/CreateFromTemplate` with a valid template ID and creates an open box, despite AGENT.md stating operators cannot create boxes.  
Impact: role-boundary bypass, invalid traceability workflows, unauthorized label printing.  
Remediation: add `[Authorize(Roles = ...)]` or named policies to create/open/print/settings as intended; add route-level tests.  
Validation after remediation: operator receives access denied for create/open/print/settings; supervisor/admin still succeed.  
Effort: Small. Business behavior change: No, it aligns with documented behavior.

#### API and Backend Security

No SQL injection was confirmed. EF LINQ is used for reviewed queries, and no `FromSql`/raw SQL patterns were found in the searched application paths.

`SEC-003` Medium: printer exception messages persisted  
Validation status and reviewers: Confirmed by one agent.  
Evidence: `BoxController.cs` lines 84 and 133 store `ex.Message`; print-job error column configured at `ApplicationDbContext.cs` line 102.  
Failure scenario: printer/driver/network exception includes internal share, host, or driver details and is stored in the database.  
Impact: sensitive operational data exposure to anyone who can view print jobs or database rows.  
Remediation: store sanitized error code/category; log raw exception only to protected application logs.  
Validation after remediation: simulate printer failure and verify persisted row contains no exception internals.  
Effort: Small. Business behavior change: No.

#### Frontend Security

`FRONT-001` Low: `innerHTML` pattern should remain constrained  
Validation status and reviewers: Confirmed by one agent.  
Evidence: `scanner.js` uses `innerHTML` for overlay templates and also escapes messages; `Login.cshtml` uses `innerHTML` for static SVG toggle icons.  
Attack scenario: future change passes unescaped barcode or server message into template HTML.  
Impact: potential DOM XSS if escaping is removed or bypassed.  
Remediation: prefer DOM node creation or central escaping tests.  
Validation: add a scanner message test with `<script>`-like input and assert escaped rendering.  
Effort: Small. Business behavior change: No.

#### File Handling

No upload/download file handling flows were found. Printing uses generated QR image bytes and local printer APIs; no user-uploaded files are processed.

#### Secrets

No committed real secret was printed or confirmed. `.env` is ignored and not tracked. `appsettings.Development.json` is tracked with a placeholder password; `.env.example` contains a placeholder. The material credential issue is the documented shared seed password in `SEC-002`.

#### Dependencies and Supply Chain

`ARCH-002` Low: floating package versions  
Validation status: Confirmed by one agent.  
Evidence: `MothersonBoxManagement.csproj` lines 15 and 19 use `8.0.*`; test project lines 14-15 do the same.  
Impact: restores can drift unexpectedly.  
Remediation: pin exact versions and optionally use package lock files.  
Effort: Small. Business behavior change: No.

#### Docker, Deployment, and CI/CD Security

Covered in `OPS-001`, `OPS-002`, and `OPS-003`.

### 2. Database Security and Integrity

#### Schema and Constraints

Confirmed strengths:

- `Users.Matricule`, `Boxes.BoxNumber`, `Boxes.BarcodeValue`, and `BoxPackages.PackageBarcode` have unique indexes in `ApplicationDbContext.cs`.
- `Boxes.RowVersion` is configured with `.IsRowVersion()`.
- Foreign keys use `DeleteBehavior.Restrict`.

`DB-003` Medium: dimension model drift  
Validation status: Confirmed by one agent.  
Evidence: decimals in entity/view model/schema; earlier integer migration and AGENT.md still state whole-centimeter dimensions.  
Impact: confusion and acceptance of fractional dimensions.  
Remediation: decide whether decimals are now intended; update docs and tests, or revert to integer validation/schema.  
Effort: Medium.

#### Query Safety

No raw SQL concatenation was confirmed. Query safety for injection is good in reviewed paths.

#### Transactions and Concurrency

`DB-002` Medium: scan transaction starts after preliminary reads  
Validation status: Confirmed by one agent.  
Evidence: duplicate/box checks occur before `BeginTransactionAsync` in `PackageScanService.cs`; unique-index exception handling depends on provider message text.  
Impact: fragile race behavior and provider-specific duplicate detection.  
Remediation: wrap the full check/insert/update in one transaction and detect SQL Server unique-constraint numbers rather than string fragments.  
Effort: Medium.

#### Migration Safety

`OPS-003` covers automatic runtime migration risk. Migration files exist for the current snapshot, but AGENT.md migration status is stale relative to newer migrations in the working tree.

#### Indexing and Slow-Query Risks

`PERF-001` covers unpaginated box search and audit filtering concerns.

#### Sensitive-Data Storage

Password hashes use `IPasswordHasher<User>`. Print failure messages may store sensitive operational data (`SEC-003`).

### 3. Performance

`PERF-001` Medium: broad search/audit queries  
Validation status: Confirmed by one agent.  
Evidence: `BoxService.SearchBoxesAsync` materializes all matches; audit view counts matching records and then pages.  
Failure scenario: audit and box tables grow during plant operation, causing slow pages.  
Remediation: add pagination to box search, default date windows for audit, and indexes on audit timestamp/action/box/user where workload confirms need.  
Effort: Medium.

`PERF-002` Low: synchronous printing in request path  
Validation status: Confirmed by one agent.  
Evidence: controller directly calls `PrintLabel`; `PrinterService` calls `printDoc.Print()`.  
Failure scenario: printer latency or failure blocks operator workflow.  
Remediation: persist print jobs and process asynchronously or allow delayed retry.  
Effort: Medium.

### 4. Architecture and Code Quality

`ARCH-001` Medium: role constants and policies are incomplete/inconsistent  
Validation status: Confirmed by one agent.  
Evidence: `AppRoles.cs` lacks Operator and includes English/French variants; controllers repeat role lists.  
Impact: future authorization drift.  
Remediation: centralize all roles and named policies.  
Effort: Small.

`ARCH-003` Info: dirty worktree audit reproducibility risk  
Validation status: Confirmed by one agent.  
Evidence: git status showed many modified/deleted/untracked files.  
Impact: reviewers cannot reproduce from clean clone.  
Remediation: commit coherent changes or run release audits on a clean branch.  
Effort: Small.

### 5. Reliability and Production Readiness

`OPS-001` High: Docker configuration is development/over-privileged  
Validation status and reviewers: Confirmed by multiple agents (DevOps, Security).  
Evidence: Compose sets `ASPNETCORE_ENVIRONMENT=Development`, uses SQL `sa`, SQL image tag is `2022-latest`, Dockerfile has no non-root `USER`.  
Impact: unsafe production posture if reused outside local demo.  
Remediation: separate production deployment config; least-privilege DB user; pin images; set `Production`; add health check; avoid running as root where possible.  
Effort: Medium.

`OPS-002` Medium: no CI workflow or health endpoint  
Validation status: Confirmed by one agent.  
Evidence: no `.github` workflows found; `Program.cs` has no health checks.  
Impact: release gates and runtime readiness are manual.  
Remediation: add CI restore/build/test and `/health` endpoint checking database connectivity.  
Effort: Medium.

`OPS-003` Medium: startup auto-migrations  
Validation status: Confirmed by one agent.  
Evidence: `Program.cs` calls `MigrateAsync()` during app startup.  
Impact: schema changes can block startup or run concurrently in scaled deployments.  
Remediation: run migrations as a controlled deployment job.  
Effort: Medium.

`OPS-004` Low: host config must be overridden for deployment  
Validation status: Confirmed by one agent.  
Evidence: `AllowedHosts` is `localhost`.  
Impact: service may reject expected host headers if not configured.  
Remediation: document and validate production `AllowedHosts`.  
Effort: Small.

### 6. Testing Gaps

Current validation:

- `dotnet build --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test --no-build`: passed, 107 tests.

`TEST-001` Medium: missing negative authorization tests for sensitive `BoxController` actions  
Validation status: Confirmed by one agent.  
Evidence: security tests check supervisor operation routes but omit `CreateFromTemplate`, `Open`, `Print`, and `Settings`.  
Impact: `SEC-001` was not caught.  
Remediation: add per-route role tests.  
Effort: Small.

`TEST-002` Medium: SQL Server-specific integrity tests are missing  
Validation status: Confirmed by one agent.  
Evidence: test project uses EF InMemory; some concurrency tests exist, but InMemory does not prove SQL Server unique-index/isolation behavior.  
Impact: duplicate scan races can differ from production.  
Remediation: add SQL Server-backed integration tests for uniqueness, rowversion, migrations, transaction behavior.  
Effort: Medium.

`TEST-003` Low: no Docker/operational smoke tests  
Validation status: Confirmed by one agent.  
Evidence: no CI workflow; Compose is manual.  
Impact: deployment packaging regressions can slip.  
Remediation: add optional local/CI smoke test.  
Effort: Medium.

## Rejected or Deferred Findings

| Finding candidate | Status | Why rejected or deferred | Evidence reviewed | Additional evidence needed |
| --- | --- | --- | --- | --- |
| SQL injection in search/filter endpoints | Rejected as a false positive | Reviewed paths use EF LINQ with parameterized queries; no raw SQL patterns found. | `BoxService`, `AuditController`, `rg` search for raw SQL APIs. | SQL profiler evidence if raw SQL is introduced later. |
| Login open redirect | Rejected as a false positive | Return URL is checked with `Url.IsLocalUrl`. | `AccountController.Login`. | None. |
| Scanner overlay exploitable XSS | Suspected but not proven | `innerHTML` exists, but dynamic scanner messages are escaped before insertion. | `scanner.js`, `Login.cshtml`. | Browser-level malicious payload test. |
| Broken supervisor forms due `asp-controller="Box"` | Rejected as a false positive | Attribute routes under `/Box/...` are present on `BoxOperationsController`; HTTP tests call those paths successfully. | `Details.cshtml`, `BoxOperationsController`, tests. | Rendered form snapshot if tag-helper behavior changes. |
| Dependency vulnerabilities | Deferred because evidence was insufficient | Local package list inspected, but no advisory scan was run. | `.csproj` files. | Approved `dotnet list package --vulnerable` or equivalent with network access. |
| Production backup/restore readiness | Deferred because evidence was insufficient | No backup runbook or production DB available. | Docs and Docker files. | Backup policy, restore drill output, SQL Server retention config. |

## Prioritized Action Plan

### 1. Immediate fixes - Critical and High

- Fix `SEC-001` by adding role restrictions to `CreateFromTemplate`, `Open`, `Print`, and settings routes. Complexity: Small. Risk if not fixed: operators can bypass box creation/opening policy. Affected files: `BoxController.cs`, tests. Business behavior change: No, aligns behavior with existing rules.
- Fix `SEC-002` by making seed accounts Development-only and removing static password documentation. Complexity: Medium. Risk if not fixed: known administrator/supervisor login. Affected files: `DbInitializer.cs`, `Program.cs`, README/docs, tests. Business behavior change: Yes for bootstrap.
- Fix `DB-001` by replacing physical package delete with auditable soft-disassociation. Complexity: Medium. Risk if not fixed: traceability history and package uniqueness can be lost. Affected files: entity, migration, `BoxService`, audit view/tests. Business behavior change: Minimal but DB-visible.
- Fix `OPS-001` before any production deployment. Complexity: Medium. Risk if not fixed: dev-mode and over-privileged DB deployment. Affected files: Dockerfile, Compose, docs. Business behavior change: No for app workflows.

### 2. Short-term fixes - Important Medium items

- Add session revocation validation after user deactivation/password reset (`SEC-004`).
- Wrap scan checks and insert/update in one transaction and improve unique-constraint handling (`DB-002`).
- Resolve dimension integer-vs-decimal drift (`DB-003`).
- Add pagination/default ranges for box search and audit filters (`PERF-001`).
- Add CI and health endpoint (`OPS-002`).
- Move migrations out of app startup for production (`OPS-003`).
- Add missing authorization and SQL Server-backed tests (`TEST-001`, `TEST-002`).

### 3. Medium-term improvements - Architecture and Performance

- Centralize roles and authorization policies (`ARCH-001`).
- Move printing behind an asynchronous job or retryable workflow (`PERF-002`).
- Pin package versions and optionally introduce package lock files (`ARCH-002`).
- Add audit indexes after measuring query plans on expected data volumes.

### 4. Nice-to-have improvements - Cleanup and Polish

- Keep `innerHTML` use constrained or replace with DOM creation helpers (`FRONT-001`).
- Sanitize and validate non-secret station/printer names submitted from browser localStorage (`FRONT-002`).
- Keep worktree clean before release audits (`ARCH-003`).
