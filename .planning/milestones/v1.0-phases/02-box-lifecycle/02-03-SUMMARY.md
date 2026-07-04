---
phase: 02-box-lifecycle
plan: "03"
subsystem: ui
tags: [mvc, viewmodel, dashboard, lookup]

requires:
  - phase: 02-box-lifecycle
    provides: [box-prepare-route, read-only-box-details]
provides:
  - typed-home-dashboard
  - status-aware-barcode-lookup
  - package-barcode-warning
  - javascript-auto-refresh
affects:
  - 02-04-PLAN.md

tech-stack:
  added: []
  patterns:
    - "Typed ViewModels: replace ViewBag dynamic fields with strongly-typed view properties"
    - "Autofocus scans: keep homepage inputs focused automatically"
    - "Interactive periodic page refresh via JS interval"

key-files:
  created: []
  modified:
    - "MothersonBoxManagement/ViewModels/HomeViewModel.cs"
    - "MothersonBoxManagement/Data/Dtos/BoxListItemDto.cs"
    - "MothersonBoxManagement/Services/BoxService.cs"
    - "MothersonBoxManagement/Controllers/HomeController.cs"
    - "MothersonBoxManagement/Views/Home/Index.cshtml"
    - "MothersonBoxManagement.Tests/BoxControllerTests.cs"

key-decisions:
  - "Decided to replace card layout with a structured Bootstrap table to display open boxes, which provides better scanning and comparison options for operators."
  - "Decided to reject package barcodes immediately in the search input without querying the database to save resources, displaying warning D-15."

requirements-completed:
  - BOX-08
  - HOME-01
  - HOME-02
  - HOME-03
  - HOME-04

coverage:
  - id: D1
    description: "Display all open boxes in a Bootstrap table, newest first, with progress and creator/operator matricules."
    requirement: "BOX-08"
    verification:
      - kind: unit
        ref: "MothersonBoxManagement.Tests/BoxControllerTests.cs#Homepage_Authenticated_ShowsDashboard"
        status: pass
    human_judgment: false
  - id: D2
    description: "Autofocus box lookup input on the homepage."
    requirement: "HOME-01"
    verification:
      - kind: unit
        ref: "MothersonBoxManagement.Tests/BoxControllerTests.cs#Homepage_Autofocus_Exists"
        status: pass
    human_judgment: false
  - id: D3
    description: "Redirect known open box barcodes to /Box/Prepare/{id}."
    requirement: "HOME-02"
    verification:
      - kind: unit
        ref: "MothersonBoxManagement.Tests/BoxControllerTests.cs#Homepage_Lookup_OpenBox_RedirectsToPrepare"
        status: pass
    human_judgment: false
  - id: D4
    description: "Redirect known non-open box barcodes to /Box/Details/{id}."
    requirement: "HOME-03"
    verification:
      - kind: unit
        ref: "MothersonBoxManagement.Tests/BoxControllerTests.cs#Homepage_Lookup_NonOpenBox_RedirectsToDetails"
        status: pass
    human_judgment: false
  - id: D5
    description: "Reject package barcodes typed in the lookup input and display the D-15 warning."
    requirement: "HOME-04"
    verification:
      - kind: unit
        ref: "MothersonBoxManagement.Tests/BoxControllerTests.cs#Homepage_Lookup_PackageBarcode_Warning"
        status: pass
    human_judgment: false

duration: 15 min
completed: 2026-07-02
status: complete
---

# Phase 2 Plan 3: Home Dashboard and Barcode Lookup Summary

**Implemented the typed home dashboard table with progress indicators, status-aware barcode lookup redirects, inline warnings for package barcodes, and JavaScript interval auto-refresh.**

## Performance

- **Duration:** 15 min
- **Started:** 2026-07-02T14:32:00Z
- **Completed:** 2026-07-02T14:33:00Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments

- Updated the main homepage dashboard to use `HomeViewModel` for strongly-typed data binding instead of `ViewBag`.
- Changed the homepage open boxes layout from card listings to a responsive Bootstrap table showing progress bars, last update timestamps, and last operators.
- Added barcode classification logic: inputs not starting with `BOX-` trigger the D-15 warning message without hitting the database, while unknown `BOX-` inputs yield the D-13 error message.
- Configured automatic redirect rules: open boxes go to `/Box/Prepare/{id}` and closed/completed boxes go to `/Box/Details/{id}`.
- Added a 30-second JavaScript page reload script that triggers only when the search input field is not active/dirty.
- Created and successfully verified unit tests for autofocus, open/closed redirects, and package warning messages.

## Task Commits

Each task was committed atomically:

1. **Task 1: Add focused home dashboard and lookup tests** - `[pending]` (test)
2. **Task 2: Implement typed home dashboard and status-aware barcode lookup** - `[pending]` (feat)

## Files Created/Modified

- `MothersonBoxManagement/ViewModels/HomeViewModel.cs` - Added typed properties
- `MothersonBoxManagement/Data/Dtos/BoxListItemDto.cs` - Added `LastUpdatedAt` and `LastUserMatricule`
- `MothersonBoxManagement/Services/BoxService.cs` - Updated `GetOpenBoxesAsync` projection
- `MothersonBoxManagement/Controllers/HomeController.cs` - Updated Index actions and lookup rules
- `MothersonBoxManagement/Views/Home/Index.cshtml` - Replaced card deck with table, added auto-refresh script
- `MothersonBoxManagement.Tests/BoxControllerTests.cs` - Added autofocus, redirect, and warning tests

## Decisions Made

- Standardized on English error messages as specified in the context keys ("No box found with this barcode." and "This is a package barcode, not a box barcode. Use the preparation screen to scan packages.").

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## Next Phase Readiness

- Ready for Plan 4 (Regression coverage, documentation, and TODO completion).
