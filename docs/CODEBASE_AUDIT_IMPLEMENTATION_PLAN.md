# Codebase Audit Implementation Plan

This plan is intentionally incremental. It preserves existing business behavior unless the audit finding specifically requires changing unsafe bootstrap, authorization, or persistence behavior.

## Commit 1: fix(authz): restrict sensitive box lifecycle actions

Objective: close `SEC-001` and prevent operator access to box creation/opening/printing/settings actions that are supervisor/admin-only or otherwise sensitive.

Findings addressed: `SEC-001`, `TEST-001`

Scope and likely files:

- `MothersonBoxManagement/Controllers/BoxController.cs`
- `MothersonBoxManagement/Security/AppRoles.cs`
- `MothersonBoxManagement.Tests/SecurityAuditTests.cs`
- `MothersonBoxManagement.Tests/BoxControllerTests.cs`

Implementation approach:

- Add a central role/policy helper for supervisor/admin roles.
- Apply it to `CreateFromTemplate`, `Open`, `Print`, and `Settings` if those actions are confirmed supervisor/admin-only.
- Leave read-only search/details available to authenticated roles unless product rules say otherwise.
- Add negative tests that an Operator cannot post to `/Box/CreateFromTemplate` or `/Box/Open`, and cannot reach print/settings if restricted.

Expected impact: closes the highest-impact role bypass without changing valid supervisor/admin workflows.

Risks and compatibility concerns: if operators intentionally need reprint access, split print into a separate permission rather than using the same policy as creation.

Required automated tests:

- Operator denied for sensitive actions.
- Supervisor/Admin allowed.
- Existing supervisor operations still pass.

Manual validation:

- Sign in as Operator and verify no UI path or direct POST can create/open/print boxes.
- Sign in as Supervisor and create/open/print a box.

Observability/rollout: log access-denied events if available.

Rollback: revert controller attributes/policies and tests.

Modifies business behavior: No, it enforces documented behavior.

## Commit 2: fix(auth): make seeded accounts development-only and rotate defaults

Objective: remove the shared documented password risk.

Findings addressed: `SEC-002`

Scope and likely files:

- `MothersonBoxManagement/Data/DbInitializer.cs`
- `MothersonBoxManagement/Program.cs`
- `README.md`
- `docs/GETTING-STARTED.md`
- `docs/CONFIGURATION.md`
- authentication tests

Implementation approach:

- Gate demo account seeding behind `IsDevelopment()` or an explicit `SeedDemoUsers=true` setting.
- Remove static password tables from general documentation.
- Prefer one of: environment-provided bootstrap password, generated one-time bootstrap secret printed only to local console in Development, or admin-created accounts.
- Add a first-login password-change flag if permanent seeded accounts remain.

Expected impact: production/staging cannot silently ship with known admin credentials.

Risks and compatibility concerns: local demos need updated setup steps.

Required automated tests:

- Development/test factory still has demo users when explicitly enabled.
- Production configuration does not seed demo users.

Manual validation:

- Start Development and confirm documented setup works.
- Start Production-like config against empty DB and confirm no default users are created unless explicitly requested.

Observability/rollout: document bootstrap owner and first-admin creation path.

Rollback: re-enable current seeding, but only for local demos.

Modifies business behavior: Yes, bootstrap account behavior changes.

## Commit 3: fix(data): preserve package association history instead of deleting rows

Objective: close the traceability gap from physical package deletion.

Findings addressed: `DB-001`

Scope and likely files:

- `MothersonBoxManagement/Entities/BoxPackage.cs`
- `MothersonBoxManagement/Data/ApplicationDbContext.cs`
- new EF Core migration
- `MothersonBoxManagement/Services/BoxService.cs`
- `MothersonBoxManagement/Data/Interceptors/AuditSaveChangesInterceptor.cs`
- `MothersonBoxManagement/Views/Box/Details.cshtml`
- service and integration tests
- `AGENT.md` migration/security/business-rule sections

Implementation approach:

- Add soft-state fields such as `IsRemoved`, `RemovedAt`, `RemovedByUserId`, `RemovalReason`, and optionally `OriginalBoxId`.
- Replace `_context.BoxPackages.Remove(package)` with state transitions.
- Decide whether released barcodes may be re-associated. If yes, the current unique index needs a filtered unique index for active associations only; if no, preserve the current global unique index and model disassociation as visibility/state only.
- Update audit logs to capture source/destination, old/new box IDs, reason, and package barcode without deleting the row.

Expected impact: preserves historical traceability and prevents silent loss of package evidence.

Risks and compatibility concerns: requires migration and UI filtering changes; uniqueness semantics must be explicitly confirmed.

Required automated tests:

- Retrait/disassociate no longer deletes rows.
- Audit log retains package barcode, old box, user, workstation, reason.
- Duplicate scan behavior matches chosen uniqueness rule.

Manual validation:

- Disassociate from cancelled box, then inspect details/audit history.
- Attempt to scan the same package again and verify intended outcome.

Observability/rollout: migration backup required before schema change.

Rollback: restore from DB backup or migration rollback if no production writes occurred after deploy.

Modifies business behavior: Potentially, depending on re-association semantics.

## Commit 4: fix(scan): tighten scan transaction and unique-constraint handling

Objective: make package scan atomic from read checks through insert/update.

Findings addressed: `DB-002`

Scope and likely files:

- `MothersonBoxManagement/Services/PackageScanService.cs`
- SQL Server-backed integration tests

Implementation approach:

- Start transaction before duplicate package lookup and box status/quantity checks.
- Use a consistent isolation level appropriate for SQL Server workload.
- Detect unique-constraint errors by SQL Server error number instead of matching `IX_BoxPackages_PackageBarcode` in exception text.
- Keep the current user-facing rejection behavior.

Expected impact: stronger concurrency behavior under simultaneous scans.

Risks and compatibility concerns: transaction isolation may increase lock contention; test against expected scanner rate.

Required automated tests:

- Two concurrent scans of the same barcode into different boxes produce one success and one clean duplicate rejection.
- Full box and closed box checks still reject.

Manual validation:

- Run concurrency test against SQL Server, not only InMemory.

Observability/rollout: add structured log event for duplicate/race rejection.

Rollback: revert service change if lock contention appears.

Modifies business behavior: No.

## Commit 5: fix(security): revoke cookies after account deactivation or password reset

Objective: enforce account state changes immediately.

Findings addressed: `SEC-004`

Scope and likely files:

- `MothersonBoxManagement/Entities/User.cs`
- `MothersonBoxManagement/Services/UserService.cs`
- `MothersonBoxManagement/Program.cs`
- new EF Core migration if adding a session/security stamp
- authentication/security tests

Implementation approach:

- Add `SecurityStamp` or `SessionVersion` to `User`.
- Include it as a claim at login.
- On cookie validation, query active user and stamp; reject principal when inactive or stale.
- Update stamp on password reset, role change, and deactivation.

Expected impact: deactivated/reset users lose access before cookie expiry.

Risks and compatibility concerns: one DB query per validation interval; configure a reasonable validation cadence.

Required automated tests:

- Deactivate signed-in user, then next request redirects.
- Reset password invalidates old cookie.

Manual validation:

- Sign in in two browsers; reset password in one; verify the other loses access.

Observability/rollout: log cookie rejection reason without secrets.

Rollback: remove validation hook; keep schema column harmless if necessary.

Modifies business behavior: No.

## Commit 6: fix(ops): split local and production Docker configuration

Objective: make the Docker path safe for production-like deployment.

Findings addressed: `OPS-001`, `OPS-004`

Scope and likely files:

- `Dockerfile`
- `docker-compose.yml`
- optional `docker-compose.prod.yml`
- `.env.example`
- `docs/CONFIGURATION.md`
- `README.md`

Implementation approach:

- Keep local Compose clearly marked Development.
- Add production profile/file with `ASPNETCORE_ENVIRONMENT=Production`.
- Use least-privilege SQL login for the app instead of `sa`.
- Pin image tags more narrowly; consider digest pinning.
- Add `USER` for the final app container if compatible with printing/runtime needs.
- Document production `AllowedHosts` override.

Expected impact: avoids accidental production use of local/demo settings.

Risks and compatibility concerns: SQL login provisioning must be scripted/documented.

Required automated tests:

- Docker build still succeeds.
- Optional Compose smoke test starts local stack.

Manual validation:

- Start local Compose and sign in.
- Start production profile with externally provisioned DB credentials.

Observability/rollout: include required environment validation in startup logs.

Rollback: use previous local compose for demos only.

Modifies business behavior: No.

## Commit 7: feat(ops): add health endpoint and CI gates

Objective: add repeatable build/test gates and runtime readiness checks.

Findings addressed: `OPS-002`, `TEST-003`

Scope and likely files:

- `MothersonBoxManagement/Program.cs`
- `.github/workflows/ci.yml` or the repository's chosen CI location
- tests/docs

Implementation approach:

- Add ASP.NET Core health checks for self and SQL Server.
- Map `/health` or `/healthz`.
- Add CI workflow running restore, build, and test.
- Optionally add a Docker smoke job later.

Expected impact: deployment and release readiness become verifiable.

Risks and compatibility concerns: health endpoint should not leak details.

Required automated tests:

- Health endpoint returns OK in test host.
- CI passes on pull requests/main.

Manual validation:

- Hit `/health` locally.

Observability/rollout: expose health endpoint to load balancer/monitoring.

Rollback: remove health mapping/workflow.

Modifies business behavior: No.

## Commit 8: fix(ops): move production migrations out of web startup

Objective: avoid uncontrolled schema changes during app startup.

Findings addressed: `OPS-003`

Scope and likely files:

- `MothersonBoxManagement/Program.cs`
- deployment docs/scripts
- tests

Implementation approach:

- Keep auto-migration only for Development/test, or guard with explicit config such as `Database:AutoMigrate=true`.
- Add documented `dotnet ef database update` or migration bundle flow for production.
- Preserve test factory setup by using explicit EnsureCreated/migrate in tests.

Expected impact: safer production startup and clearer rollback plan.

Risks and compatibility concerns: deploy pipeline must run migrations before app starts.

Required automated tests:

- Production environment does not call automatic migration path.
- Development/test still initialize correctly.

Manual validation:

- Run documented migration step against local SQL Server.

Observability/rollout: log pending migration state at startup if possible.

Rollback: re-enable guarded auto-migration for emergency local use only.

Modifies business behavior: No.

## Commit 9: fix(model): resolve dimension integer-vs-decimal rule drift

Objective: align schema, validation, docs, and tests around one dimension rule.

Findings addressed: `DB-003`

Scope and likely files:

- `Box.cs`, `BoxTemplate.cs`
- DTOs/view models
- `ApplicationDbContext.cs`
- EF migration if schema changes
- `AGENT.md`, docs/spec
- validation tests

Implementation approach:

- Confirm canonical rule. If whole centimeters remain required, change decimals to integers and reject fractional input.
- If decimal dimensions are intended, update AGENT.md/spec/tests and keep decimal schema.

Expected impact: removes ambiguity and prevents future migration churn.

Risks and compatibility concerns: integer migration can truncate/round existing decimal values; plan data conversion.

Required automated tests:

- Fractional dimension input rejected if integer rule.
- Valid integer dimensions accepted.

Manual validation:

- Create/edit template with boundary values.

Observability/rollout: migration notes must document data conversion.

Rollback: restore prior schema from backup if production decimals exist.

Modifies business behavior: Yes if current decimal acceptance is removed.

## Commit 10: perf(search): paginate and index high-volume audit/search views

Objective: reduce risk of slow pages as data grows.

Findings addressed: `PERF-001`

Scope and likely files:

- `BoxService.cs`
- `AuditController.cs`
- DTOs/view models/views
- `ApplicationDbContext.cs`
- migration for indexes if justified
- tests

Implementation approach:

- Add pagination to box search results.
- Add default audit date/page bounds.
- Add indexes for common filters after confirming expected query patterns.

Expected impact: predictable page load behavior under larger datasets.

Risks and compatibility concerns: UI changes for pagination.

Required automated tests:

- Search pagination returns expected counts/pages.
- Audit page clamps invalid page/page size.

Manual validation:

- Seed many rows and verify dashboard/search/audit responsiveness.

Observability/rollout: capture query timing in logs if available.

Rollback: remove indexes only if they harm writes measurably.

Modifies business behavior: Minor UI behavior change.

## Commit 11: test(db): add SQL Server-backed integrity tests

Objective: validate production-like constraints and migrations.

Findings addressed: `TEST-002`, supports `DB-001` and `DB-002`

Scope and likely files:

- test project
- optional Docker/Testcontainers setup
- docs

Implementation approach:

- Add a small suite that runs against SQL Server, guarded so normal unit tests remain fast.
- Cover package uniqueness, rowversion concurrency, migration application, and transaction retry behavior.

Expected impact: catches issues InMemory cannot represent.

Risks and compatibility concerns: slower tests and Docker/SQL dependency.

Required automated tests:

- The new SQL Server suite itself.

Manual validation:

- Run local SQL-backed test command from docs.

Observability/rollout: make this a required CI job only if infrastructure supports it.

Rollback: keep tests optional if CI cannot host SQL Server yet.

Modifies business behavior: No.

## Commit 12: chore(quality): centralize roles and pin package versions

Objective: reduce authorization drift and build drift.

Findings addressed: `ARCH-001`, `ARCH-002`

Scope and likely files:

- `AppRoles.cs`
- `Program.cs`
- controllers
- `.csproj` files
- tests/docs

Implementation approach:

- Add `Operator` constant and named authorization policies.
- Replace repeated role strings with constants/policies.
- Pin exact package versions and consider `packages.lock.json`.

Expected impact: easier reviews and reproducible restores.

Risks and compatibility concerns: role names must match existing database values.

Required automated tests:

- Existing authz tests pass.
- Restore/build/test green.

Manual validation:

- Sign in with all seeded roles.

Observability/rollout: none required.

Rollback: revert constants/pinning if restore breaks unexpectedly.

Modifies business behavior: No.

## Commit 13: harden(frontend): constrain dynamic HTML and sanitize browser-submitted labels

Objective: keep local workstation settings non-secret and reduce future DOM XSS risk.

Findings addressed: `FRONT-001`, `FRONT-002`

Scope and likely files:

- `wwwroot/js/scanner.js`
- `wwwroot/js/station-terminal.js`
- `WorkstationResolver.cs`
- views/tests

Implementation approach:

- Replace dynamic `innerHTML` with DOM construction where practical.
- Keep a single escape helper if templates remain.
- Clamp/sanitize station and printer names server-side for length/control characters.

Expected impact: small hardening without visible UI change.

Risks and compatibility concerns: scanner overlay markup may need visual regression check.

Required automated tests:

- Malicious-looking scanner messages are escaped.
- station/printer names are trimmed and length-limited.

Manual validation:

- Use scanner simulator and station settings.

Observability/rollout: none required.

Rollback: revert JS refactor if overlay regressions occur.

Modifies business behavior: No.

## Suggested Commit Order

1. `fix(authz): restrict sensitive box lifecycle actions`
2. `fix(auth): make seeded accounts development-only and rotate defaults`
3. `fix(data): preserve package association history instead of deleting rows`
4. `fix(scan): tighten scan transaction and unique-constraint handling`
5. `fix(security): revoke cookies after account deactivation or password reset`
6. `fix(ops): split local and production Docker configuration`
7. `feat(ops): add health endpoint and CI gates`
8. `fix(ops): move production migrations out of web startup`
9. `fix(model): resolve dimension integer-vs-decimal rule drift`
10. `perf(search): paginate and index high-volume audit/search views`
11. `test(db): add SQL Server-backed integrity tests`
12. `chore(quality): centralize roles and pin package versions`
13. `harden(frontend): constrain dynamic HTML and sanitize browser-submitted labels`
