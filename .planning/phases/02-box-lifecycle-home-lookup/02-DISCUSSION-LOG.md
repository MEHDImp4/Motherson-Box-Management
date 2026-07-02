# Phase 2: Box Lifecycle & Home Lookup - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-02
**Phase:** 02-box-lifecycle-home-lookup
**Areas discussed:** Box Number & Barcode Generation, Dashboard Layout & Progress Display, Box Creation Form & Validation, Homepage Scan & Redirect Behavior

---

## Box Number & Barcode Generation

### Q1: Box number format

| Option | Description | Selected |
|--------|-------------|----------|
| Date+sequential | BOX-YYYYMMDD-XXXXXX with zero-padded daily counter | |
| Timestamp+random | BOX-YYYYMMDD-XXXXXX with 6-char hex random | ✓ |
| Pure sequential | BOX-000001, BOX-000002 | |

**User's choice:** Timestamp+random — BOX-YYYYMMDD-A3F8B1 (date + 6-char hex). Avoids sequential prediction, still date-grouped.
**Notes:** None.

### Q2: Box vs package barcode distinction

| Option | Description | Selected |
|--------|-------------|----------|
| Prefix convention | Box barcodes always start with "BX" prefix | ✓ |
| Length-based | Different fixed lengths for box vs package | |
| Regex pattern | Regex patterns for each type | |

**User's choice:** Prefix convention — simple string check at scan time.
**Notes:** Combined with Q4 below — the "BOX" prefix from the number format itself serves as the distinguisher.

### Q3: Barcode value = box number?

| Option | Description | Selected |
|--------|-------------|----------|
| Same identifier | Box barcode = box number (e.g., BOX-20260702-000001 is both) | ✓ |
| Separate barcode | Human-readable number + separate barcode code | |

**User's choice:** Single identifier — box number IS the barcode value.
**Notes:** None.

### Q4: Prefix convention

| Option | Description | Selected |
|--------|-------------|----------|
| BOX prefix | The format BOX-YYYYMMDD-XXXXXX already starts with "BOX" | ✓ |
| BX short prefix | Use "BX" as a dedicated 2-char barcode prefix | |

**User's choice:** BOX prefix — inherent in the box number format.
**Notes:** None.

---

## Dashboard Layout & Progress Display

### Q1: Dashboard layout

| Option | Description | Selected |
|--------|-------------|----------|
| Bootstrap table | Standard table with progress bars, columns for all required data | ✓ |
| Card grid | Bootstrap cards with progress ring/bar inside | |
| Compact list | Minimal rows with inline progress text | |

**User's choice:** Bootstrap table with progress bars — fits corporate intranet style.
**Notes:** None.

### Q2: Refresh strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Manual reload | Page refresh or "Refresh" button | |
| Auto-refresh | Page auto-reloads every N seconds via JS interval | ✓ |
| Real-time push | SignalR WebSocket updates | |

**User's choice:** Auto-refresh — periodic JS interval.
**Notes:** None.

### Q3: Default sorting

| Option | Description | Selected |
|--------|-------------|----------|
| Newest first | By creation date descending | ✓ |
| Least progress first | Boxes closest to empty first | |
| Most progress first | Boxes closest to completion first | |

**User's choice:** Newest first (by creation date).
**Notes:** None.

### Q4: Pagination

| Option | Description | Selected |
|--------|-------------|----------|
| No pagination | All open boxes on one page | ✓ |
| Paginated | 20 per page | |
| Infinite scroll | Load more on scroll | |

**User's choice:** All open boxes on one page — factory expected <100 open boxes.
**Notes:** None.

---

## Box Creation Form & Validation

### Q1: Who can create boxes?

| Option | Description | Selected |
|--------|-------------|----------|
| All authenticated roles | Operator, Supervisor, Admin | ✓ |
| Supervisor and Admin only | Operators can only scan | |
| Admin only | Only Admin creates | |

**User's choice:** All authenticated roles — role restrictions apply only to exceptions in Phase 4.
**Notes:** None.

### Q2: Box type storage

| Option | Description | Selected |
|--------|-------------|----------|
| Hardcoded C# enum | `BoxType { Carton, Bois, Plastique }` | ✓ |
| Database lookup table | DB-configurable types | |
| Configurable enum + seed | Enum + DB table for extensibility | |

**User's choice:** Hardcoded C# enum — simple, type-safe, covers requirements.
**Notes:** None.

### Q3: Dimension validation

| Option | Description | Selected |
|--------|-------------|----------|
| Positive integers only | > 0, no min/max | ✓ |
| Min/Max range | 1–999 cm | |
| Decimal values | Allow fractional cm | |

**User's choice:** Positive integers only, no upper constraints.
**Notes:** None.

### Q4: Form location

| Option | Description | Selected |
|--------|-------------|----------|
| Dedicated creation page | /Box/Create with full form | ✓ |
| Modal dialog | Bootstrap modal on dashboard | |
| Sidebar panel | Slide-in panel from dashboard | |

**User's choice:** Dedicated /Box/Create page — standard MVC form pattern.
**Notes:** None.

---

## Homepage Scan & Redirect Behavior

### Q1: Auto-focus

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-focus on page load | Cursor placed in input automatically | ✓ |
| Manual focus | User must click the input field | |

**User's choice:** Auto-focus — operator scans immediately without clicking.
**Notes:** None.

### Q2: Unknown barcode handling

| Option | Description | Selected |
|--------|-------------|----------|
| Inline error | Red alert below input, stays focused | ✓ |
| Toast notification | Bootstrap toast in corner | |
| Modal dialog | Pop-up dialog with error | |

**User's choice:** Inline red error below input: "No box found with this barcode."
**Notes:** None.

### Q3: Redirect targets by status

| Option | Description | Selected |
|--------|-------------|----------|
| Two distinct pages | Open → preparation screen; Closed → read-only detail | ✓ |
| Same detail page | With "Start Scanning" button for open | |
| Detail + embedded scan | Embedded scan input for open boxes | |

**User's choice:** Two distinct pages — open goes to scanning, closed goes to read-only detail.
**Notes:** None.

### Q4: Package barcode in box search

| Option | Description | Selected |
|--------|-------------|----------|
| Inline warning | Red alert explaining it's a package code | ✓ |
| Redirect to scanning | Attempt to find which box it belongs to | |
| Block silently | Clear input, no message | |

**User's choice:** Inline red warning — "This is a package barcode, not a box barcode."
**Notes:** None.

---

## Agent's Discretion

None — all decisions were explicitly made by the user.

## Deferred Ideas

None — discussion stayed within Phase 2 scope.
