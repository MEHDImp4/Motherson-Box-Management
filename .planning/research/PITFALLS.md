# Domain Pitfalls

**Domain:** Barcode Packaging Box Management Web App
**Researched:** 2026-07-02

## Critical Pitfalls

Mistakes that cause database issues, concurrency bugs, or physical scanning errors.

### Pitfall 1: Double-Scan Race Condition (Concurrent Scans)
**What goes wrong:** Two operators scan the exact same physical package code at two different workstations at the exact same millisecond. If the system only performs a logical check in C# (e.g., `_db.Packages.Any(p => p.Barcode == code)`), both scans might pass the validation before either is written, resulting in the same package being saved in two different boxes.
**Why it happens:** In-memory checks are subject to race conditions.
**Consequences:** Duplicate cable packages in different boxes, invalidating the primary core value of absolute traceability.
**Prevention:** 
1. Enforce a unique SQL index on `BoxPackages.PackageBarcode`.
2. Wrap scanning logic in a database transaction with a serializable or repeatabread isolation level, or rely on the unique database index throwing a duplicate key exception which is handled gracefully in the controller.

### Pitfall 2: Focus Loss in Keyboard Wedge Mode
**What goes wrong:** A physical USB scanner outputs character strokes into whatever field is currently focused. If the operator accidentally clicks outside the scanning text field, scanning a barcode will type characters into a search field, a password field, or trigger random browser hotkeys.
**Why it happens:** Keyboard wedge emulators have no native awareness of which input element should receive scanner text.
**Consequences:** Scans are lost, or worst, typed into the wrong screen context, frustrating operators.
**Prevention:** 
1. Inject a global document-level Javascript keypress listener that monitors input stream speed. If characters arrive with a delay < 30ms between strokes, treat it as a scanner burst and programmatically redirect the string to the hidden scan processor, regardless of where the browser focus currently is.
2. Provide visual cues (e.g., a green indicator for "Ready to scan") to show the listener is active.

### Pitfall 3: Barcode Format Overlap (Box vs. Package)
**What goes wrong:** An operator accidentally scans a Box barcode instead of a Cable Package barcode in the package preparation view.
**Why it happens:** Both barcodes look like standard strings to a simple keyboard wedge.
**Consequences:** A Box is added as a package inside another box, corrupting audit trails.
**Prevention:**
1. Establish a strict prefix/format validation rule. Box codes must begin with `BOX-` and package codes must begin with `PKG-` (or follow a distinct length/regex rule).
2. Validate the format on the backend before inserting, returning a custom error code.

## Moderate Pitfalls

### Pitfall 1: Concurrency Conflict on ExpectedQuantity Updates
**What goes wrong:** A supervisor updates a box's expected quantity at the same time an operator is scanning packages. The operator's scan automatically updates the box's current quantity, which can overwrite the supervisor's new expected quantity, or vice-versa.
**Why it happens:** Classic lost update problem on the `Boxes` table.
**Consequences:** Inconsistent expected vs. actual quantities, preventing auto-clôture or allowing overfill.
**Prevention:** Use SQL Server `rowversion` column with EF Core optimistic concurrency tracking (`[Timestamp]` attribute). Catch the `DbUpdateConcurrencyException` in both Controller actions and retry or notify user.

## Minor Pitfalls

### Pitfall 1: Browser Tab Closes Mid-Session
**What goes wrong:** Operator closes the browser tab or gets logged out due to inactivity while a box is half-filled.
**Why it happens:** Session timeout or accidental actions.
**Consequences:** The operator thinks the box data is lost because they didn't click "Save".
**Prevention:** Save every scanned package to the database immediately upon scan (AJAX POST) rather than storing the scanned package list in the user session memory. Since the box is in `Open` status in the DB, it can be resumed by anyone at any time.

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Database Setup | Missing Unique Constraints | Explicitly configure `HasIndex().IsUnique()` in Fluent API. |
| Authentication | Vague Auth Messages | Standard security practice: return generic "Invalid matricule or password" to prevent brute-forcing. |
| Scan Integration | Scanner Input Clears Too Slowly | Use Javascript `e.preventDefault()` to stop carriage return forms from reloading pages. |

## Sources

- Motherson Box Management Functional Specification (Cahier des charges v1.3).
- SQL Server Concurrency and Transaction Isolation Levels Guide.
