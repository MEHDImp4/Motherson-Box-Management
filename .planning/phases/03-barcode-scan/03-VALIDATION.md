---
phase: 3
slug: barcode-scan
status: draft
nyquist_compliant: true
wave_0_complete: false
created: 2026-07-03
---

# Phase 3 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.x + WebApplicationFactory |
| **Config file** | MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj |
| **Quick run command** | `dotnet test` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~5 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 03-01-01 | 01 | 1 | SCAN-01, SCAN-03 | — | Concurrency retry, uniqueness constraints | integration | `dotnet test` | ✅ | ⬜ pending |
| 03-01-02 | 01 | 1 | G-08 | — | Use PrepareViewModel in GET | unit | `dotnet test` | ✅ | ⬜ pending |
| 03-01-03 | 01 | 1 | G-03, G-04 | — | CSRF token checked on POST | integration | `dotnet test` | ✅ | ⬜ pending |
| 03-01-04 | 01 | 1 | SCAN-02 | — | ScanAjax returns JSON | integration | `dotnet test` | ✅ | ⬜ pending |
| 03-02-01 | 02 | 2 | SCAN-01, SCAN-02 | — | Keystroke listener and AJAX scan | manual | — | ✅ | ⬜ pending |
| 03-02-02 | 02 | 2 | SCAN-02 | — | Prepare view updated with IDs | manual | — | ✅ | ⬜ pending |
| 03-02-03 | 02 | 2 | G-06 | — | Pulse success and input focus CSS | manual | — | ✅ | ⬜ pending |
| 03-03-01 | 03 | 3 | SIM-01 | — | Simulator AJAX + CSRF batch | manual | — | ✅ | ⬜ pending |
| 03-03-02 | 03 | 3 | SCAN-03, SCAN-04 | — | Completed box rejection + JSON response tests | integration | `dotnet test` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Audio beep feedback (success/error) | SCAN-05 | Web Audio API cannot be assert-tested in xUnit headless browser | Scan package, listen for high/low beep |
| Barcode scanner keystroke detection | SCAN-01 | USB wedge emulator is physical | Focus anywhere on page, scan package with hardware wedge |
| Progress bar transition animation | D-06 | CSS layout transition | Check visual fill animation on successful scan |
| Simulator batch UI results | SIM-01 | Multi-fetch visual badge updates | Run simulator with count 3, check badges appear inline |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 5s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
