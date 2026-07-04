---
status: complete
phase: 02-box-lifecycle
source:
  - .planning/phases/02-box-lifecycle/02-01-SUMMARY.md
  - .planning/phases/02-box-lifecycle/02-02-SUMMARY.md
  - .planning/phases/02-box-lifecycle/02-03-SUMMARY.md
  - .planning/phases/02-box-lifecycle/02-04-SUMMARY.md
started: 2026-07-02T15:37:24+01:00
updated: 2026-07-02T16:10:40+01:00
---

## Current Test

[testing complete]

## Tests

### 1. Cold Start Smoke Test
expected: |
  Kill any running server/service. Clear ephemeral state (temp DBs, caches, lock files). Start the application from scratch. Server boots without errors, any seed/migration completes, and a primary query (health check, homepage load, or basic API call) returns live data.
result: pass

### 2. Login and Open Dashboard
expected: |
  Navigate to the login page, authenticate as Operator (matricule: 100001, password: Password123), and confirm you land on the dashboard showing the welcome message, the barcode search field, and a table/placeholder for open boxes.
result: pass

### 3. Box Creation Form Validation
expected: |
  Navigate to `/Box/Create` and fill the form with invalid box dimensions (e.g. Height = -5, Width = 1.5, Depth = 0) or Expected Quantity = 0. Verify that form validation rejects the values and displays appropriate error messages.
result: pass

### 4. Successful Box Creation
expected: |
  On `/Box/Create`, enter valid values (Type = Carton, Height = 30, Width = 20, Depth = 15, ExpectedQuantity = 5) and click "Create". Verify you are redirected to the box details page (`/Box/Details/{id}`).
result: pass

### 5. Verify Generated BOX Barcode & Metadata
expected: |
  On the box details page, verify the page displays the generated box number matching `BOX-YYYYMMDD-XXXXXX` (where XXXXXX is a 6-character uppercase hex string), the dimensions (30x20x15 cm), status "Open", and the creator operator matricule "100001".
result: pass

### 6. Home Dashboard Table & Auto-Refresh
expected: |
  Go to the homepage `/`. Verify that the open boxes table lists the newly created box at the top, displaying the correct dimensions, progression `0 / 5`, last update timestamp, and operator matricule "100001".
result: pass

### 7. Barcode Search: Open Box Redirects to Prepare
expected: |
  Type or scan the newly generated box barcode on the homepage and submit. Verify you are redirected to `/Box/Prepare/{id}` (the scanning preparation view).
result: pass

### 8. Barcode Search: Package Barcode Warning
expected: |
  Return to the homepage. Type a package barcode (e.g., `PKG-123456`) in the box search field and click search. Verify that an inline warning displays: "This is a package barcode, not a box barcode. Use the preparation screen to scan packages." and the input remains focused.
result: pass

### 9. Barcode Search: Unknown Box Barcode Error
expected: |
  Saisir an unknown box barcode (e.g., `BOX-00000000-000000`) on the homepage search field and click search. Verify that an inline error displays: "No box found with this barcode." and the input remains focused.
result: pass

### 10. Convert Height, Width, and Depth to integer values
expected: |
  Convert Height, Width, and Depth to integer values in Box entity, DTOs, CreateBoxViewModel, and Create.cshtml inputs.
result: pass
source: automated
coverage_id: D1

### 11. Generate and review EF Core migration UseIntegerBoxDimensions
expected: |
  Generate and review EF Core migration UseIntegerBoxDimensions to modify columns in SQL Server.
result: pass
source: automated
coverage_id: D2

### 12. Implement BOX-YYYYMMDD-XXXXXX pattern for BoxNumber and BarcodeValue (equal) with collision checks
expected: |
  Implement BOX-YYYYMMDD-XXXXXX pattern for BoxNumber and BarcodeValue (equal) with collision checks.
result: pass
source: automated
coverage_id: D1

### 13. Store box creator metadata (CreatedByUserId and CreatedAt) and expose in DTO
expected: |
  Store box creator metadata (CreatedByUserId and CreatedAt) and expose in DTO.
result: pass
source: automated
coverage_id: D2

### 14. Implement GET /Box/Prepare/{id} which renders only for Open boxes, and redirects non-open boxes to details
expected: |
  Implement GET /Box/Prepare/{id} which renders only for Open boxes, and redirects non-open boxes to details.
result: pass
source: automated
coverage_id: D3

### 15. Display all open boxes in a Bootstrap table, newest first, with progress and creator/operator matricules
expected: |
  Display all open boxes in a Bootstrap table, newest first, with progress and creator/operator matricules.
result: pass
source: automated
coverage_id: D1

### 16. Autofocus box lookup input on the homepage
expected: |
  Autofocus box lookup input on the homepage.
result: pass
source: automated
coverage_id: D2

### 17. Redirect known open box barcodes to /Box/Prepare/{id}
expected: |
  Redirect known open box barcodes to /Box/Prepare/{id}.
result: pass
source: automated
coverage_id: D3

### 18. Redirect known non-open box barcodes to /Box/Details/{id}
expected: |
  Redirect known non-open box barcodes to /Box/Details/{id}.
result: pass
source: automated
coverage_id: D4

### 19. Reject package barcodes typed in the lookup input and display the D-15 warning
expected: |
  Reject package barcodes typed in the lookup input and display the D-15 warning.
result: pass
source: automated
coverage_id: D5

### 20. Verify that all Phase 2 requirements (except BOX-06) have automated coverage and pass consistently
expected: |
  Verify that all Phase 2 requirements (except BOX-06) have automated coverage and pass consistently.
result: pass
source: automated
coverage_id: D1

### 21. Update AGENT.md memory log to document integer dimensions, new barcode semantics, routing rules, and EF Core migrations
expected: |
  Update AGENT.md memory log to document integer dimensions, new barcode semantics, routing rules, and EF Core migrations.
result: pass
source: automated
coverage_id: D2

### 22. Mark tasks TSK-007 through TSK-010 as Done in TODO.md
expected: |
  Mark tasks TSK-007 through TSK-010 as Done in TODO.md.
result: pass
source: automated
coverage_id: D3

## Summary

total: 22
passed: 22
issues: 0
pending: 0
skipped: 0

## Gaps

[none yet]
