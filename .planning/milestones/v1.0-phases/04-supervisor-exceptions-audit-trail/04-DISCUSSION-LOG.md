# Phase 4: Supervisor Exceptions & Audit Trail - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-04
**Phase:** 04-supervisor-exceptions-audit-trail
**Areas discussed:** Exception workflow UX, Audit trail mechanism, Transfer/retrait mechanics, Block/unblock scope

---

## Exception Workflow UX

### Access Point

| Option | Description | Selected |
|--------|-------------|----------|
| Buttons on Details page | Add action buttons directly on the Box Details view. Supervisor sees the box state and acts immediately. | ✓ |
| Separate admin pages | Dedicated pages for each action (e.g., /Box/Cancel/123). More form space but requires navigation. | |
| Modal dialogs | Bootstrap modal popups triggered from Details page. Reason field in modal. | |

**User's choice:** Buttons on Details page
**Notes:** Keeps context in one place, minimal navigation.

### Reason Input

| Option | Description | Selected |
|--------|-------------|----------|
| Inline text input | Clicking 'Cancel' reveals a text input + confirm/cancel buttons directly below the button. | ✓ |
| Prompt dialog (window.prompt) | Simple browser prompt asking for reason text. | |
| Separate form page | Redirect to a dedicated form page with a textarea and submit button. | |

**User's choice:** Inline text input
**Notes:** Lightweight, stays in context.

### Role Access

| Option | Description | Selected |
|--------|-------------|----------|
| Same actions for both | Both Supervisors and Admins see all exception buttons. | ✓ |
| Admin-only force-close | Supervisors can cancel, block, transfer, retrait. Force-close is Admin-only. | |

**User's choice:** Same actions for both
**Notes:** Role filtering via Authorize attributes on service methods.

### Transfer Destination Selection

| Option | Description | Selected |
|--------|-------------|----------|
| Dropdown of open boxes | A dropdown lists all open boxes (box number + type). | ✓ |
| Scan destination barcode | Supervisor scans/types the destination box barcode. | |
| Table with radio buttons | Table of open boxes with radio buttons for selection. | |

**User's choice:** Dropdown of open boxes
**Notes:** Simple and fast.

---

## Audit Trail Mechanism

### Logging Approach

| Option | Description | Selected |
|--------|-------------|----------|
| SaveChangesInterceptor | EF Core interceptor automatically logs all Create/Update/Delete operations. | ✓ |
| Manual logging (current pattern) | Each service method manually writes BoxAuditLog entries. | |

**User's choice:** SaveChangesInterceptor
**Notes:** Catches everything, replaces manual pattern in ScanPackageAsync.

### Capture Detail Level

| Option | Description | Selected |
|--------|-------------|----------|
| Full state diffs | Capture all entity changes with before/after values in JSON. | ✓ |
| Action + entity only | Capture only the action type, entity ID, and user. | |

**User's choice:** Full state diffs
**Notes:** Complete traceability.

### Workstation Tracking

| Option | Description | Selected |
|--------|-------------|----------|
| IP address from request | Use HttpContext.Connection.RemoteIpAddress. | |
| Config-based workstation name | Each workstation has a config file or env var with its name. | ✓ |
| User-entered workstation | User manually types their workstation name during login. | |

**User's choice:** Config-based workstation name
**Notes:** Simple, consistent, no user friction.

### Audit Log Display

| Option | Description | Selected |
|--------|-------------|----------|
| Separate audit log page | A dedicated page listing all audit logs with filtering. | ✓ |
| Embedded in Box Details | Audit logs shown inline at the bottom of the Box Details view. | |
| Both | Both a global audit page AND inline logs on Box Details. | |

**User's choice:** Separate audit log page
**Notes:** Keeps Box Details focused, supports filtering.

---

## Transfer/Removal Mechanics

### Package Record Handling

| Option | Description | Selected |
|--------|-------------|----------|
| Move the BoxPackage record | Package moves from source box to destination box. | ✓ |
| Copy + mark source as transferred | Package stays in source but marked 'transferred'. New record in destination. | |

**User's choice:** Move the BoxPackage record
**Notes:** Single atomic transaction, no data duplication.

### Removal Behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Delete the record | The BoxPackage record is deleted entirely. | ✓ |
| Soft-delete (IsRetired flag) | The BoxPackage record is soft-deleted. | |

**User's choice:** Delete the record
**Notes:** Package barcode freed, can be re-scanned.

### Post-Removal Re-scan

| Option | Description | Selected |
|--------|-------------|----------|
| Allow re-scan | Same barcode can be scanned again into any open box. | ✓ |
| Block permanently | Once retracted, barcode is permanently blocked. | |

**User's choice:** Allow re-scan
**Notes:** Physical reality — retracted package can be reassigned.

### Transfer UI Complexity

| Option | Description | Selected |
|--------|-------------|----------|
| Single form with dropdown + reason | Dropdown of open boxes, package list, reason textarea. | ✓ |
| Multi-step wizard | Step 1: select destination. Step 2: confirm. Step 3: reason. | |

**User's choice:** Single form with dropdown + reason
**Notes:** Minimal and focused.

---

## Block/Unblock Scope

### Block Granularity

| Option | Description | Selected |
|--------|-------------|----------|
| Both box and package blocking | Supervisor can block a box OR a specific package. | ✓ |
| Box blocking only | Only box-level blocking exists. | |

**User's choice:** Both box and package blocking
**Notes:** Two distinct block types.

### Post-Unblock Behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Allow re-scan after unblock | Unblocked package can be scanned into any open box. | ✓ |
| Permanent quarantine after unblock | Once blocked and unblocked, package is permanently quarantined. | |

**User's choice:** Allow re-scan after unblock
**Notes:** No permanent quarantine.

### Block UI Location

| Option | Description | Selected |
|--------|-------------|----------|
| Buttons on Details page | Block/Unblock buttons on Box Details for box and package levels. | ✓ |
| Separate admin pages | Dedicated pages for block/unblock actions. | |

**User's choice:** Buttons on Details page
**Notes:** Consistent with Exception workflow UX.

### Visual Indicator

| Option | Description | Selected |
|--------|-------------|----------|
| Red badge/icon | Blocked boxes/packages get a red badge or icon. | ✓ |
| Greyed out with strikethrough | Blocked items shown with strikethrough text and grey background. | |
| Red badge + greyed out | Both — red badge AND greyed out row. | |

**User's choice:** Red badge/icon
**Notes:** Clear visual distinction.

---

## Agent's Discretion

No areas marked as agent discretion — all decisions were explicitly made by the user.

## Deferred Ideas

None — discussion stayed within phase scope.
