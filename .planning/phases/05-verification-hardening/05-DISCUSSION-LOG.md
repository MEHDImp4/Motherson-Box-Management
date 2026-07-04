# Phase 5: Verification & Hardening - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-04
**Phase:** 5-verification-hardening
**Areas discussed:** Concurrency Test Strategy, Security Audit Scope, Verification Coverage, Hardening Measures

---

## Concurrency Test Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Parallel xUnit tasks | Launch N parallel tasks scanning same barcode, assert exactly 1 succeeds. Uses existing WebApplicationFactory. | ✓ |
| Delayed parallel tasks | Add explicit Thread.Sleep/Task.Delay to widen race window. More deterministic but slower. | |
| Parameterized concurrency suite | Dedicated test class with configurable parallelism (2, 5, 10). Parameterized for thoroughness. | |

**User's choice:** Parallel xUnit tasks (Recommended)
**Notes:** Reuses existing `CustomWebApplicationFactory` infrastructure — no new test helpers needed.

---

## Security Audit Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Error log audit only | Verify error responses/logs don't leak DB details (connection strings, SQL errors, stack traces). | |
| Error logs + auth + input | Error logs + authorization bypass checks + input validation on all forms. | ✓ |
| Comprehensive security audit | Full audit: error logs, auth bypass, input validation, CSRF, SQL injection, session fixation. | |

**User's choice:** Error logs + auth + input
**Notes:** Balanced scope — covers ROADMAP requirement (error logs) plus critical auth/input concerns without over-engineering.

---

## Verification Coverage

| Option | Description | Selected |
|--------|-------------|----------|
| Fill gaps only | Add only what's missing: concurrency test, security audit tests, critical uncovered paths. | |
| Fix regressions + add new | Run all existing tests, identify Phase 4 regressions, fix them, then add concurrency + security. | |
| Full lifecycle E2E test | Write end-to-end test: create box → scan packages → supervisor exception → verify audit log. | ✓ |

**User's choice:** Full lifecycle E2E test
**Notes:** Validates all phases work together — the ultimate integration verification.

---

## Hardening Measures

| Option | Description | Selected |
|--------|-------------|----------|
| Verify existing + global handler | Verify anti-CSRF/HTTPS, add global exception handler to suppress DB details in error pages. | ✓ |
| Verify + rate limit + CSP | All above + rate limiting on scan endpoint + Content-Security-Policy headers. | |
| Global exception handler only | Minimal: only add global exception handler returning generic error page. | |

**User's choice:** Verify existing + global handler (Recommended)
**Notes:** Pragmatic hardening — verifies what exists, adds the critical missing piece (global error handler).

---

## Agent's Discretion

No areas marked as agent discretion — all decisions were explicitly made by the user.

## Deferred Ideas

None — discussion stayed within phase scope.
