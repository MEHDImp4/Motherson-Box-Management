# Phase 5: Verification & Hardening - Context

**Gathered:** 2026-07-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Finalize system integration by writing comprehensive tests that verify concurrent scan blocking, security hardening of error responses, and a full lifecycle E2E test covering the complete box workflow from creation through supervisor exception. Apply security hardening measures including a global exception handler to suppress DB details in error pages.

</domain>

<decisions>
## Implementation Decisions

### Concurrency Test Strategy
- **D-01:** Use parallel xUnit tasks with existing `CustomWebApplicationFactory` to simulate concurrent scans. Launch N parallel `ScanPackageAsync` calls with the same barcode, assert exactly 1 succeeds and the rest get duplicate/optimistic concurrency errors. No additional test infrastructure needed.

### Security Audit Scope
- **D-02:** Security audit covers three areas: (1) Error log verification — ensure error responses don't leak DB details (connection strings, SQL errors, stack traces); (2) Authorization bypass checks — verify Operator role cannot trigger Supervisor/ Admin actions (cancel, force-close, block, transfer, retrait); (3) Input validation — verify all forms (box creation, exception reasons, transfers) reject malformed input.

### Verification Coverage
- **D-03:** Write a full lifecycle E2E integration test covering the complete workflow: create box → scan packages to fill → supervisor exception (cancel or force-close) → verify audit log entry exists with correct fields. This test validates all phases work together end-to-end.

### Hardening Measures
- **D-04:** Verify existing anti-CSRF tokens and HTTPS redirect middleware work correctly. Add a global exception handler (`app.UseExceptionHandler`) that returns a generic error page and suppresses DB details, stack traces, and internal paths from error responses.

### Agent's Discretion
- No areas marked as agent discretion — all decisions were explicitly made by the user.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements & Specs
- `.planning/REQUIREMENTS.md` — Full requirements (AUTH, BOX, SCAN, AUDIT, EXC, SIM)
- `.planning/ROADMAP.md` §Phase 5 — Goal, success criteria, and phase dependencies

### Existing Implementation
- `MothersonBoxManagement.Tests/CustomWebApplicationFactory.cs` — InMemory DB setup with `AuditSaveChangesInterceptor`, seed users, `FakeAntiforgery`
- `MothersonBoxManagement.Tests/ScanControllerTests.cs` — Existing scan integration tests
- `MothersonBoxManagement.Tests/BoxServiceExceptionTests.cs` — Existing exception service tests
- `MothersonBoxManagement.Tests/SupervisorExceptionsControllerTests.cs` — Existing supervisor controller tests
- `MothersonBoxManagement/Services/BoxService.cs` — `ScanPackageAsync` with retry loop, concurrency handling, `RowVersion` check
- `MothersonBoxManagement/Services/IBoxService.cs` — Service interface
- `MothersonBoxManagement/Controllers/BoxController.cs` — Box actions (Scan, Prepare, Details)
- `MothersonBoxManagement/Controllers/AuditController.cs` — Audit log viewer
- `MothersonBoxManagement/Data/ApplicationDbContext.cs` — EF Core DbContext with interceptor hooks

### Architecture & Conventions
- `AGENTS.md` — Coding conventions, EF Core rules, transaction requirements, security rules

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `CustomWebApplicationFactory`: Full InMemory DB test factory with seeded users (OP001, SP001, AD001), `FakeAntiforgery`, and `AuditSaveChangesInterceptor` — ready for new integration tests
- `BoxService.ScanPackageAsync`: Already has retry loop for optimistic concurrency, returns `ScanResult` with success/message/box — reuse for concurrency test assertions
- `TestAuthHelper`: Exists in test project for authentication simulation in tests

### Established Patterns
- **Integration test pattern**: `CustomWebApplicationFactory` + `HttpClient` with auth cookies + assert on response/content
- **Service layer testing**: Call service methods directly with `CancellationToken.None` for unit-level tests
- **French UI assertions**: All user-facing strings are French — test assertions must match French text

### Integration Points
- New concurrency test class: `MothersonBoxManagement.Tests/ConcurrencyTests.cs`
- New E2E lifecycle test: extend `ScanControllerTests.cs` or new `LifecycleTests.cs`
- Security audit tests: new `SecurityAuditTests.cs` or extend existing test files
- Global exception handler: `Program.cs` or `Startup.cs` — `app.UseExceptionHandler("/Home/Error")`

</code_context>

<specifics>
## Specific Ideas

- Concurrency test should use `Task.WhenAll` with 5-10 parallel scan tasks for realistic simulation
- E2E lifecycle test must verify audit log entry exists with correct ActionType, UserId, and non-empty DetailsJson
- Global exception handler must not expose `Development` environment details in non-Development mode
- Authorization tests should use `TestAuthHelper` to impersonate each role and attempt forbidden actions

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 05-verification-hardening*
*Context gathered: 2026-07-04*
