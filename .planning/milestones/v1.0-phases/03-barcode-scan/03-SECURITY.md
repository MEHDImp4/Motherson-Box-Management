---
phase: 3
slug: barcode-scan
status: verified
threats_open: 0
asvs_level: 1
created: 2026-07-03
---

# Phase 3 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Client ↔ Server | Browser sends scan requests, server processes | Package barcodes, box identifiers |
| Server ↔ Database | EF Core persists scan data | Box quantities, package records |
| User ↔ UI | Scanner input captures barcodes | Raw barcode keystrokes |

---

## Threat Register

| Threat ID | Category | Component | Severity | Disposition | Mitigation | Status |
|-----------|----------|-----------|----------|-------------|------------|--------|
| S-1 | Spoofing | BoxController | high | mitigate | `[Authorize]` on class, all actions require auth | closed |
| S-2 | Spoofing | CSRF | high | mitigate | `AddAntiforgery` + CSRF meta tag + `[ValidateAntiForgeryToken]` on all POSTs | closed |
| T-1 | Tampering | Input | high | mitigate | Server-side trim + validation, client `escapeHtml()` | closed |
| T-2 | Tampering | Database | critical | mitigate | EF Core LINQ only, no raw SQL, parameterized queries | closed |
| T-3 | Tampering | Scan | high | mitigate | Server validates box status + quantity before scan | closed |
| R-1 | Repudiation | Audit | medium | mitigate | `BoxAuditLog` with `ActionType = "PackageScan"` on every scan | closed |
| I-1 | Info Disclosure | Error | medium | mitigate | User-friendly JSON messages, no stack traces | closed |
| I-2 | Info Disclosure | Auth | high | mitigate | `[Authorize]` on controller, all actions require auth | closed |
| D-1 | DoS | Rate | low | mitigate | No rate limiting — mitigated by auth + internal network | open — below threshold |
| D-2 | DoS | Simulator | low | mitigate | Max 10 scans in simulator, server validates each | closed |
| E-1 | Elevation | Design | low | accept | Shared-box design, business requirement | closed |
| E-2 | Elevation | Roles | low | accept | All users can scan; role restrictions deferred to Phase 4 | closed |

*Status: open · closed · open — below high threshold (non-blocking)*
*Severity: critical > high > medium > low — only open threats at or above workflow.security_block_on count toward threats_open*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-01 | E-1 | Shared-box design is a business requirement — all operators work on the same boxes | Security Auditor | 2026-07-03 |
| AR-02 | E-2 | Role-based scan restrictions deferred to Phase 4 (Supervisor Exceptions) | Security Auditor | 2026-07-03 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-07-03 | 12 | 11 | 1 (non-blocking) | gsd-security-auditor (retroactive-STRIDE) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-07-03
