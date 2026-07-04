# Phase 3: Barcode Scan Integration - Context

**Gathered:** 2026-07-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Implement the scanner listener on the preparation screen, execute package scanning with uniqueness validation, and provide a virtual barcode simulator panel for testing. This phase delivers the core scanning workflow: scan a package barcode, associate it with the open box, enforce global uniqueness, auto-close when full, and provide a simulator for testing without physical hardware.

</domain>

<decisions>
## Implementation Decisions

### Scan Input UX Flow
- **D-01:** After a successful scan, the input field must auto-focus and clear for the next scan. Operator can scan continuously without clicking.
- **D-02:** Visual feedback uses green/red Bootstrap alert banner at top (existing pattern) plus a brief CSS animated highlight on the progress bar.
- **D-03:** The scan input auto-submits on Enter keypress. USB scanners send barcode + Enter — no button click needed.
- **D-04:** When a box reaches expected quantity and auto-closes, the scan input is disabled (greyed out) with a "Box completed" message displayed.
- **D-05:** Scan input has a minimum length check (3 characters) before submitting. Prevents accidental empty/near-empty submits.
- **D-06:** Progress bar uses CSS transition on width change for animated fill. No animation library required.

### Simulator Panel Features
- **D-07:** Simulator supports batch scanning with a count input (1-10). Auto-scans N barcodes sequentially with a small delay between each.
- **D-08:** Test barcodes use prefix + timestamp format (e.g., `PKG-TEST-{timestamp}`). Keeps current behavior — simple, unique, easy to identify as test data.
- **D-09:** Simulator panel toggles via a JavaScript button (no page reload). Replaces current query param `?simulator=1` approach.
- **D-10:** Simulator panel stays below the scan form on the preparation page. Keeps scanning workflow together.

### Test Coverage
- **D-11:** Add integration test for scanning on a non-Open box (Completed/Cancelled). Covers SCAN-04 requirement.
- **D-12:** Add integration test for concurrent scans of the same barcode (race condition). Prepares for Phase 5 concurrency verification.
- **D-13:** Add integration test for scanning when quantity is already met (box at max). Covers SCAN-04 requirement.

### Package Barcode Format
- **D-14:** Package barcodes stay freeform — accept any string. Real-world scanners read whatever is on the label. BOX- prefix check is sufficient for box/package separation.
- **D-15:** BOX- prefix check uses exact prefix match with case-insensitive comparison. `BOX123` without dash is NOT blocked — only `BOX-` prefix triggers rejection.
- **D-16:** Error message for box barcodes scanned in package view is: "Box barcodes cannot be scanned as packages."

### the agent's Discretion
- No areas marked as agent discretion — all decisions were explicitly made by the user.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements & Specs
- `.planning/REQUIREMENTS.md` — SCAN-01 through SCAN-06, SIM-01, BOX-06 requirements for this phase
- `.planning/ROADMAP.md` §Phase 3 — Goal, success criteria, and plan status

### Existing Implementation
- `MothersonBoxManagement/Services/IBoxService.cs` — ScanPackageAsync interface (already defined)
- `MothersonBoxManagement/Services/BoxService.cs` — ScanPackageAsync implementation (scan logic, BOX- rejection, duplicate check, auto-close)
- `MothersonBoxManagement/Controllers/BoxController.cs` — Scan POST action (already implemented)
- `MothersonBoxManagement/Views/Box/Prepare.cshtml` — Preparation screen with scan input form
- `MothersonBoxManagement/Views/Box/_SimulatorPanel.cshtml` — Virtual scanner panel partial
- `MothersonBoxManagement/Data/Dtos/ScanResult.cs` — Scan result DTO
- `MothersonBoxManagement/Entities/BoxPackage.cs` — Package entity with unique index on PackageBarcode
- `MothersonBoxManagement.Tests/ScanControllerTests.cs` — 5 existing integration tests

### Architecture
- `MothersonBoxManagement/Data/ApplicationDbContext.cs` — EF Core config with unique indexes on Box.BarcodeValue, Box.BoxNumber, BoxPackage.PackageBarcode

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `BoxService.ScanPackageAsync`: Full scan logic already implemented — BOX- prefix rejection, duplicate detection via DB query, auto-close when CurrentQuantity >= ExpectedQuantity. Returns `ScanResult` with success flag, message, and updated box.
- `BoxController.Scan` POST action: Accepts boxId, boxBarcode, barcode. Calls ScanPackageAsync, sets TempData for success/error, redirects back to Prepare view.
- `_SimulatorPanel.cshtml`: Generates test barcodes with prefix + Date.now(), submits via hidden form to the same Scan endpoint.
- `ScanResult` DTO: Contains `Success`, `Message`, and `Box` (nullable BoxDetailsDto).

### Established Patterns
- **TempData for flash messages**: Success/error messages stored in TempData, displayed as Bootstrap alerts with dismiss buttons in Razor views.
- **French UI**: All user-facing strings in French (error messages, labels, buttons).
- **Bootstrap layout**: Standard Bootstrap cards, progress bars, tables, alerts.
- **Service layer pattern**: IBoxService/BoxService with async methods, CancellationToken propagation, DTO-based returns.
- **Controller thin layer**: Controllers only handle HTTP concerns, delegate to services.

### Integration Points
- `Prepare.cshtml` is the scan screen — new code connects here (auto-focus JS, disable-on-complete logic, animated progress bar).
- `_SimulatorPanel.cshtml` needs refactoring for JS toggle and batch scanning.
- `BoxController.Scan` POST action is the endpoint — no new endpoints needed.
- `BoxService.ScanPackageAsync` is the service method — no new service methods needed.

</code_context>

<specifics>
## Specific Ideas

- Scan input must feel continuous — operator scans, sees feedback, input is ready for next scan immediately.
- Simulator should feel like a real scanner — batch mode with sequential scans simulates scanning multiple packages.
- Progress bar animation should be subtle — CSS transition, not flashy.
- Minimum length check (3 chars) prevents accidental submits from USB scanner initialization noise.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 3-Barcode Scan Integration*
*Context gathered: 2026-07-02*
