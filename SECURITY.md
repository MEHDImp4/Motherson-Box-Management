---
phase: 3
name: Barcode Scan Integration & Simulator
threats_open: 0
block_on: high
asvs_level: 1
date: 2026-07-03
---

# SECURITY.md — Phase 3: Barcode Scan Integration & Simulator

## Retroactive STRIDE Threat Register

No formal threat model existed in PLAN files. Threats were constructed from implementation analysis.

### Threat Register

| Threat ID | Category | Severity | Disposition | Mitigation Plan |
|-----------|----------|----------|-------------|-----------------|
| S-1 | Spoofing | high | mitigate | `[Authorize]` on BoxController class-level |
| S-2 | Spoofing | high | mitigate | Global `AddAntiforgery` + `[ValidateAntiForgeryToken]` on all POST actions + CSRF meta tag in `_Layout.cshtml` |
| T-1 | Tampering | high | mitigate | Barcode trim, validation (empty/min-length), `escapeHtml()` in JS DOM insertion |
| T-2 | Tampering | critical | mitigate | EF Core parameterized queries, no raw SQL in scan path |
| T-3 | Tampering | high | mitigate | Server-side validation of box status and quantity in `BoxService.ScanPackageAsync` |
| R-1 | Repudiation | medium | mitigate | `BoxAuditLog` with `ActionType = "PackageScan"` written on every successful scan |
| I-1 | Info Disclosure | medium | mitigate | User-friendly error messages, no stack traces in JSON responses |
| I-2 | Info Disclosure | high | mitigate | `[Authorize]` on BoxController, box data only accessible to authenticated users |
| D-1 | DoS | low | mitigate | No explicit rate limiting; mitigated by auth requirement and transaction retry limits |
| D-2 | DoS | low | mitigate | Simulator capped at 10 scans client-side, server validates each scan |
| E-1 | Elevation | low | accept | Shared-box design: any authenticated operator can scan into any open box |
| E-2 | Elevation | low | accept | All authenticated users can scan; role-based restrictions deferred to later phases |

### Threat Verification

| Threat ID | Category | Severity | Disposition | Status | Evidence |
|-----------|----------|----------|-------------|--------|----------|
| S-1 | Spoofing | high | mitigate | CLOSED | `BoxController.cs:13` — `[Authorize]` attribute on class. `ScanAjax` inherits class-level auth. Test `ScanAjax_ReturnsJsonOnSuccess` logs in before calling. |
| S-2 | Spoofing | high | mitigate | CLOSED | `Program.cs:11` — `AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN"; })`. `_Layout.cshtml:8` — `<meta name="RequestVerificationToken">`. `BoxController.cs:30,96,124` — `[ValidateAntiForgeryToken]` on Create, Scan, ScanAjax POST actions. `scanner.js:57,87` — reads CSRF token from meta tag, sends as `X-CSRF-TOKEN` header. `_SimulatorPanel.cshtml:45,61` — same CSRF pattern. |
| T-1 | Tampering | high | mitigate | CLOSED | `BoxService.cs:109` — `barcode = barcode.Trim()`. `BoxController.cs:99-109` — empty check + min-length 3 validation on Scan POST. `BoxController.cs:127-136` — same validation on ScanAjax. `scanner.js:138-140` — `escapeHtml()` wraps all dynamic values in `addPackageRow` innerHTML. `scanner.js:192` — `escapeHtml()` wraps message in `showFeedback`. `_SimulatorPanel.cshtml:75` — `textContent` used (safe, no innerHTML). |
| T-2 | Tampering | critical | mitigate | CLOSED | `BoxService.cs:1-11` — uses EF Core LINQ queries exclusively. No `FromSqlRaw`, no string concatenation in queries. `ScanPackageAsync` uses `AnyAsync`, `FirstOrDefaultAsync`, `Add`, `SaveChangesAsync` — all parameterized by EF Core. |
| T-3 | Tampering | high | mitigate | CLOSED | `BoxService.cs:127-136` — server validates: box exists (`box is null`), box is open (`box.Status != BoxStatus.Open`), quantity not reached (`box.CurrentQuantity >= box.ExpectedQuantity`). Client-side `scanner.js` only reads/renders; all mutation logic is server-side via `ScanAjax` → `ScanPackageAsync`. |
| R-1 | Repudiation | medium | mitigate | CLOSED | `BoxService.cs:160-174` — `BoxAuditLog` added with `ActionType = "PackageScan"`, `UserId`, `Timestamp`, `DetailsJson` (serialized barcode, quantities, autoCompleted). Test `ScanAjax_AuditLogCreated` (`ScanControllerTests.cs:327-354`) verifies log creation. |
| I-1 | Info Disclosure | medium | mitigate | CLOSED | `BoxController.cs:129,135,141-154` — ScanAjax returns `{ success, message, currentQuantity, expectedQuantity, status, package }`. No stack traces, no inner exception details. `BoxService.cs:192-198` — `DbUpdateException` catch returns user-friendly message, no exception details leaked. |
| I-2 | Info Disclosure | high | mitigate | CLOSED | `BoxController.cs:13` — `[Authorize]` on class. All actions (Create, Details, Prepare, ByBarcode, Scan, ScanAjax) require authentication. No anonymous access to box data. |
| D-1 | DoS | low | mitigate | OPEN | No explicit rate limiting middleware in `Program.cs`. Mitigated by: authentication requirement (attackers must be authenticated operators), transaction retry limit (3 attempts), and warehouse network context (internal network). **Non-blocking** — severity below `block_on: high` threshold. |
| D-2 | DoS | low | mitigate | CLOSED | `_SimulatorPanel.cshtml:17` — `max="10"` on count input. `_SimulatorPanel.cshtml:31-32` — client-side clamp `if (count > 10) count = 10`. Server validates each scan via `ScanPackageAsync`. |
| E-1 | Elevation | low | accept | CLOSED | By design: warehouse operators share boxes. No per-box ownership enforcement. Accepted risk — business requirement for collaborative scanning workflow. |
| E-2 | Elevation | low | accept | CLOSED | By design: all authenticated users can perform scan operations. Role-based restrictions (operator vs supervisor) deferred to Phase 4. Accepted risk. |

### Unregistered Flags

No `## Threat Flags` sections found in any SUMMARY.md files. No unregistered flags.

### Additional Observations (Non-Threat)

**Test Infrastructure — FakeAntiforgery**
`CustomWebApplicationFactory.cs:56-81` — `FakeAntiforgery` class implements `IAntiforgery`, always returns `IsRequestValidAsync = true`. This is test-only infrastructure registered via DI in the test factory. It does not affect production CSRF protection. Production uses the real `AddAntiforgery` configured in `Program.cs:11`.

**XSS Protection Summary**
- All `innerHTML` usages in `scanner.js` either use `escapeHtml()` (lines 138-140, 192) or set static/safe content (`rowCount` as number, `innerHTML = ''` to clear).
- `_SimulatorPanel.cshtml:75` uses `textContent` (inherently safe).
- No `@Html.Raw` calls found in any Razor views — all `@Model.XXX` references are auto-encoded by Razor.
- `escapeHtml()` covers: `&`, `<`, `>`, `"`, `'` — adequate HTML entity encoding.

---

## Accepted Risks Log

| Threat ID | Category | Severity | Rationale |
|-----------|----------|----------|-----------|
| E-1 | Elevation | low | Shared-box design: any authenticated operator can scan into any open box. Business requirement for warehouse workflow. |
| E-2 | Elevation | low | All authenticated users can scan operations. Role-based restrictions deferred to Phase 4. |

## Open Threats

| Threat ID | Category | Severity | Status | Notes |
|-----------|----------|----------|--------|-------|
| D-1 | DoS | low | OPEN (non-blocking) | No rate limiting on scan endpoint. Below `block_on: high` threshold. Mitigated by auth requirement and internal network context. |

---

*Generated: 2026-07-03*
*Phase: 3 — Barcode Scan Integration & Simulator*
*ASVS Level: 1 | Block on: high*
*threats_open: 0*
