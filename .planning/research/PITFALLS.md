# Domain Pitfalls

**Domain:** Barcode Packaging Box Management Web App
**Researched:** 2026-07-02

## Critical Pitfalls

Mistakes that can cause database issues, concurrency bugs, or physical scanning errors.

### Pitfall 1: Double-Scan Race Condition (Concurrent Scans)
**What goes wrong:** Two operators scan the exact same physical package code at two different workstations at the exact same millisecond. If the system only performs a logical check in C# (for example `_db.Packages.Any(p => p.Barcode == code)`), both scans may pass validation before either write happens, so the same package can be saved in two different boxes.
**Why it happens:** In-memory checks are vulnerable to race conditions.
**Consequences:** Duplicate cable packages in different boxes, which breaks the core value of absolute traceability.
**Prevention:**
1. Enforce a unique SQL index on `BoxPackages.PackageBarcode`.
2. Wrap scan logic in a database transaction with a strict isolation level, or rely on the unique database index to throw a duplicate-key exception that the controller handles gracefully.

### Pitfall 2: Focus Loss in Keyboard Wedge Mode
**What goes wrong:** A physical USB scanner writes characters into whichever field currently has focus. If the operator accidentally clicks outside the scanning text field, scanning a barcode may type into a search field, a password field, or even trigger browser shortcuts.
**Why it happens:** Keyboard wedge scanners do not know which input element should receive the scan text.
**Consequences:** Scans are lost or typed into the wrong screen context, which frustrates operators.
**Prevention:**
1. Add a global document-level JavaScript keypress listener that monitors input speed. If characters arrive with less than 30 ms between keystrokes, treat the input as a scanner burst and redirect it to the hidden scan processor regardless of current browser focus.
2. Provide visual cues such as a green "Ready to scan" indicator so users know the listener is active.

### Pitfall 3: Barcode Format Overlap (Box vs. Package)
**What goes wrong:** An operator accidentally scans a box barcode instead of a cable package barcode in the package preparation view.
**Why it happens:** Both barcode types look like standard strings to a simple keyboard wedge scanner.
**Consequences:** A box gets added as a package inside another box, which corrupts the audit trail.
**Prevention:**
1. Enforce a strict prefix or format rule. Box codes must begin with `BOX-` and package codes must begin with `PKG-` (or follow another clearly separate regex/length rule).
2. Validate the format on the backend before inserting and return a custom error code when it is wrong.

## Moderate Pitfalls

### Pitfall 1: Concurrency Conflict on ExpectedQuantity Updates
**What goes wrong:** A supervisor updates a box's expected quantity at the same time an operator scans packages. The operator's scan also updates the box's current quantity, which can overwrite the supervisor's new expected quantity, or the reverse can happen.
**Why it happens:** This is a classic lost-update problem on the `Boxes` table.
**Consequences:** Expected and actual quantities become inconsistent, which can prevent auto-completion or allow overfill.
**Prevention:** Use a SQL Server `rowversion` column with EF Core optimistic concurrency tracking (`[Timestamp]`). Catch `DbUpdateConcurrencyException` in controller actions and either retry or notify the user.

## Minor Pitfalls

### Pitfall 1: Browser Tab Closes Mid-Session
**What goes wrong:** The operator closes the browser tab or gets logged out because of inactivity while a box is half-filled.
**Why it happens:** Session timeout or accidental user action.
**Consequences:** The operator may think the box data is lost because they did not click "Save".
**Prevention:** Save every scanned package immediately to the database through AJAX POST rather than storing the scan list in session memory. Because the box stays in `Open` status in the database, anyone can resume it at any time.

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Database Setup | Missing Unique Constraints | Explicitly configure `HasIndex().IsUnique()` in Fluent API. |
| Authentication | Vague Auth Messages | Return a generic "Invalid matricule or password" message to prevent brute-forcing. |
| Scan Integration | Scanner Input Clears Too Slowly | Use JavaScript `e.preventDefault()` to stop carriage-return form submissions from reloading the page. |

## Sources

- Motherson Box Management Functional Specification (v1.3).
- SQL Server Concurrency and Transaction Isolation Levels Guide.
