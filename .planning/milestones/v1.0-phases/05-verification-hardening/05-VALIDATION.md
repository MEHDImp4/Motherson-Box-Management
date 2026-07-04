---
phase: 05
slug: verification-hardening
status: draft
nyquist_compliant: true
wave_0_complete: false
created: 2026-07-04
---

# Phase 05 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.x (.NET Core) |
| **Config file** | MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~ConcurrencyTests\|FullyQualifiedName~SecurityAuditTests\|FullyQualifiedName~E2ELifecycleTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~ConcurrencyTests\|FullyQualifiedName~SecurityAuditTests\|FullyQualifiedName~E2ELifecycleTests"`
- **After every plan wave:** Run `dotnet test`
- **Before \`/gsd-verify-work\`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 05-01-01 | 01 | 1 | None (SCAN-02/SCAN-03 verification) | T-05-01 | Concurrent scans fail cleanly except one | integration | `dotnet test --filter "FullyQualifiedName~ConcurrencyTests"` | ❌ W0 | ⬜ pending |
| 05-01-02 | 01 | 1 | EXC-01/02/03/04 | T-05-02 | Operators blocked from supervisor exception endpoints | integration | `dotnet test --filter "FullyQualifiedName~SecurityAuditTests.Operator_CannotAccess_SupervisorEndpoints"` | ❌ W0 | ⬜ pending |
| 05-01-03 | 01 | 1 | BOX-01 to EXC-04 | T-05-03 | Full E2E lifecycle completes and records audit logs | integration | `dotnet test --filter "FullyQualifiedName~E2ELifecycleTests"` | ❌ W0 | ⬜ pending |
| 05-01-04 | 01 | 1 | None (Hardening verification) | T-05-04 | Exception details suppressed in production | integration | `dotnet test --filter "FullyQualifiedName~SecurityAuditTests.GlobalExceptionHandler_SuppressesDetails"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `MothersonBoxManagement.Tests/ConcurrencyTests.cs` — stubs for concurrency tests
- [ ] `MothersonBoxManagement.Tests/SecurityAuditTests.cs` — stubs for security and hardening tests
- [ ] `MothersonBoxManagement.Tests/E2ELifecycleTests.cs` — stubs for E2E lifecycle tests

---

## Manual-Only Verifications

*All phase behaviors have automated verification.*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 15s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-07-04
