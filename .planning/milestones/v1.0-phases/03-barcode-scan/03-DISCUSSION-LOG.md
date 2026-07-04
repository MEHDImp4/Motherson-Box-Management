# Phase 3: Barcode Scan Integration - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-02
**Phase:** 3-barcode-scan
**Areas discussed:** Scan input UX flow, Simulator panel features, Test coverage gaps, Package barcode format

---

## Scan input UX flow

### Auto-focus after scan

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-focus + clear (Recommended) | Keep autofocus on input, clear after redirect | ✓ |
| Stay as-is | Redirect back with TempData banner, autofocus on page load | |
| AJAX no redirect | Submit via JavaScript, update page without full reload | |

**User's choice:** Auto-focus + clear
**Notes:** Operator can scan continuously without clicking.

### Visual feedback style

| Option | Description | Selected |
|--------|-------------|----------|
| Banner + flash highlight (Recommended) | Green/red alert banner + brief highlight on progress bar | ✓ |
| Banner only | Current: green success or red error banner with dismiss button | |
| Toast notification | Small popup/toast in corner | |

**User's choice:** Banner + flash highlight
**Notes:** Combines existing banner pattern with subtle progress bar animation.

### USB scanner keyboard handling

| Option | Description | Selected |
|--------|-------------|----------|
| Enter auto-submit (Recommended) | Form submits automatically on Enter | ✓ |
| Manual submit | User clicks Scanner button after scanning | |
| Debounced auto-submit | Wait 300ms after last keystroke, then auto-submit | |

**User's choice:** Enter auto-submit
**Notes:** USB scanners send barcode + Enter key. No button click needed.

### Box auto-close behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Disable input + show message (Recommended) | Grey out scan input, show "Box completed" | ✓ |
| Hide the scan form | Remove scan form entirely when Completed | |
| Keep active, reject on submit | Let user keep scanning, show error on each attempt | |

**User's choice:** Disable input + show message
**Notes:** Operator sees the box is full without losing context.

### Input validation

| Option | Description | Selected |
|--------|-------------|----------|
| No validation (Recommended) | Accept any input, validate at service layer | |
| Min length check | Reject barcodes shorter than 3 characters | ✓ |
| Format pattern | Require barcodes to match a pattern like PKG-* | |

**User's choice:** Min length check
**Notes:** Prevents accidental empty/near-empty submits from USB scanner initialization noise.

### Progress bar animation

| Option | Description | Selected |
|--------|-------------|----------|
| Instant update (Recommended) | Progress bar fills immediately after redirect | |
| Animated transition | CSS transition on width change | ✓ |
| You decide | Let agent choose based on Bootstrap style | |

**User's choice:** Animated transition
**Notes:** Subtle CSS transition, no animation library required.

---

## Simulator panel features

### Batch scanning

| Option | Description | Selected |
|--------|-------------|----------|
| Batch with count input (Recommended) | Auto-scan N barcodes sequentially with delay | ✓ |
| Single scan only | One barcode per click | |
| Configurable delay | Batch scan with user-settable delay between scans | |

**User's choice:** Batch with count input
**Notes:** Count field already exists (1-10). Should auto-scan sequentially.

### Barcode generation pattern

| Option | Description | Selected |
|--------|-------------|----------|
| Prefix + timestamp (Recommended) | PKG-TEST-{timestamp} | ✓ |
| Prefix + sequential number | PKG-TEST-001, PKG-TEST-002... | |
| Random alphanumeric | PKG-{random8chars} | |

**User's choice:** Prefix + timestamp
**Notes:** Keeps current behavior. Simple, unique, easy to identify as test data.

### Panel toggle method

| Option | Description | Selected |
|--------|-------------|----------|
| Query param toggle (Recommended) | ?simulator=1 in URL | |
| Button toggle (JS) | Click button to show/hide without reload | ✓ |
| Always visible for Operators | Show by default for all users | |

**User's choice:** Button toggle (JS)
**Notes:** More polished UX. No page reload needed.

### Panel position

| Option | Description | Selected |
|--------|-------------|----------|
| Below scan form (Recommended) | Inside scan card, below input | ✓ |
| Sidebar panel | Fixed panel on right side | |
| Separate tab/section | Collapsible accordion section | |

**User's choice:** Below scan form
**Notes:** Keeps scanning workflow together. Current position works well.

---

## Test coverage gaps

### Non-Open box scan test

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, add test (Recommended) | Test scanning on Completed/Cancelled box | ✓ |
| Skip | Defer to Phase 5 | |
| Add for all non-Open statuses | Test Completed, Cancelled, Blocked separately | |

**User's choice:** Yes, add test
**Notes:** Covers SCAN-04 requirement.

### Concurrent scan test

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, add test (Recommended) | Parallel HTTP requests simulating concurrent scans | ✓ |
| Skip for now | Defer to Phase 5 concurrency testing | |
| Add as integration test | Use parallel HTTP requests | |

**User's choice:** Yes, add test
**Notes:** Prepares for Phase 5 success criteria.

### Full box scan test

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, add test (Recommended) | Test scanning after quantity is met | ✓ |
| Skip | Auto-complete test implicitly covers this | |
| Combine with non-Open test | Test both scenarios in one method | |

**User's choice:** Yes, add test
**Notes:** Covers SCAN-04 requirement explicitly.

---

## Package barcode format

### Package barcode prefix

| Option | Description | Selected |
|--------|-------------|----------|
| Freeform (Recommended) | Accept any string as package barcode | ✓ |
| PKG- prefix required | Require PKG- prefix on all package barcodes | |
| Configurable prefix | Allow admins to set package prefix | |

**User's choice:** Freeform
**Notes:** Real-world scanners read whatever is on the label. BOX- prefix check is sufficient.

### BOX- prefix check scope

| Option | Description | Selected |
|--------|-------------|----------|
| Exact prefix only (Recommended) | startsWith('BOX-', OrdinalIgnoreCase) | ✓ |
| Contains 'BOX' anywhere | Block any barcode containing 'BOX' substring | |
| Regex pattern | ^BOX-\d{8}-[A-F0-9]{6}$ | |

**User's choice:** Exact prefix only
**Notes:** Clean, predictable. BOX123 without dash is NOT blocked.

### Box barcode error message

| Option | Description | Selected |
|--------|-------------|----------|
| English message (Recommended) | "Box barcodes cannot be scanned as packages." | ✓ |
| Short message | "Invalid box barcode for this action." | |
| Bilingual | Show both French and English | |

**User's choice:** French message
**Notes:** Consistent with UI language convention.

---

## the agent's Discretion

No areas marked as agent discretion — all decisions were explicitly made by the user.

## Deferred Ideas

None — discussion stayed within phase scope.
