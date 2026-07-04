---
phase: 1
slug: database-authentication
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-07-02
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit / dotnet test |
| **Config file** | MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj |
| **Quick run command** | `dotnet test --filter Category=Unit` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~5 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter Category=Unit`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 10 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 01-01-01 | 01 | 0 | AUTH-01 | — | N/A | integration | `dotnet test --filter DisplayLogin` | ❌ W0 | ⬜ pending |
| 01-01-02 | 01 | 1 | AUTH-01 | — | Alphanumeric validation (3-20 chars) | unit | `dotnet test --filter MatriculeValidation` | ❌ W0 | ⬜ pending |
| 01-01-03 | 01 | 1 | AUTH-02 | T-01-02 | Generic error message for invalid login | integration | `dotnet test --filter InvalidLogin` | ❌ W0 | ⬜ pending |
| 01-01-04 | 01 | 1 | AUTH-03 | T-01-03 | Role-based authorization cookie check | integration | `dotnet test --filter AuthorizedRoutes` | ❌ W0 | ⬜ pending |
| 01-01-05 | 01 | 1 | BOX-05 | — | Ensure identity claims map to created box fields | unit | `dotnet test --filter UserClaimsMapping` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj` — test project setup
- [ ] `MothersonBoxManagement.Tests/AccountControllerTests.cs` — stubs for login validation
- [ ] `MothersonBoxManagement.Tests/CustomWebApplicationFactory.cs` — shared fixtures

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Browser session termination on close | AUTH-02 | Requires browser-level cookie persistence checks | 1. Open app, log in. 2. Close browser. 3. Reopen browser to app home, verify user is redirected to login. |

*If none: "All phase behaviors have automated verification."*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
