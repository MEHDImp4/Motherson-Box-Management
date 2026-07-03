# Phase 2: Box Lifecycle & Home Lookup - Research

**Researched:** 2026-07-02  
**Domain:** ASP.NET Core MVC 8, EF Core, SQL Server, Bootstrap intranet UI  
**Confidence:** HIGH - repo-local evidence only, per network restriction

## User Constraints

Authoritative context source: `.planning/phases/02-box-lifecycle-home-lookup/02-CONTEXT.md`. [VERIFIED: repo file]

- Box number format is `BOX-YYYYMMDD-XXXXXX`, where `XXXXXX` is a 6-character random hexadecimal string. [VERIFIED: repo file]
- `BarcodeValue` must be exactly the same value as `BoxNumber`; do not use or preserve the stale `BX-` barcode prefix from `.planning/phases/02-box-lifecycle/02-PLAN.md`. [VERIFIED: repo file]
- Box/package distinction for Phase 2 home lookup is the `BOX` prefix. Phase 3 package scan rejection should use `StartsWith("BOX")`, not `StartsWith("BX-")`. [VERIFIED: repo file]
- Dashboard must show open boxes in a standard Bootstrap table with box number, type, progress, last update timestamp, and last user. [VERIFIED: repo file]
- Dashboard sorting is newest first by creation date descending; no pagination for MVP. [VERIFIED: repo file]
- Dashboard refresh may use a JavaScript interval; no SignalR/WebSocket. [VERIFIED: repo file]
- All authenticated roles can create boxes. Phase 4 owns exception-only role restrictions. [VERIFIED: repo file]
- Box types are the hardcoded enum values `Carton`, `Bois`, and `Plastique`. [VERIFIED: repo file]
- Dimensions are positive integers only in the context decision, but current code uses `double` with `0.01` validation; planner must choose whether to align code to integer decision or update the decision before execution. [VERIFIED: repo file + repo grep]
- Homepage barcode input must autofocus, keep focus on invalid input, warn on package barcode input, redirect open boxes to preparation, and redirect closed/cancelled/archived boxes to read-only details. [VERIFIED: repo file]

## Phase Requirements

| ID | Implementation Support |
|----|------------------------|
| BOX-01 | `CreateBoxViewModel.Type`, `BoxType`, `BoxController.Create`, and `BoxService.CreateBoxAsync`. [VERIFIED: repo grep] |
| BOX-02 | `CreateBoxViewModel` validates Height/Width/Depth as positive values; context says positive integers. [VERIFIED: repo grep] |
| BOX-03 | `CreateBoxViewModel.ExpectedQuantity` has `[Range(1, int.MaxValue)]`. [VERIFIED: repo grep] |
| BOX-04 | `BoxService.CreateBoxAsync` currently generates `BOX-YYYYMMDD-######` and incorrectly stores `BX-{boxNumber}` as barcode. [VERIFIED: repo grep] |
| BOX-05 | `Box.CreatedByUserId`, `CreatedAt`, and controller claim extraction are present. [VERIFIED: repo grep] |
| BOX-06 | Current scan code auto-completes boxes, but this belongs to Phase 3; Phase 2 should not deepen package scanning. [VERIFIED: repo grep] |
| BOX-07 | `BoxDetailsDto` and `Views/Box/Details.cshtml` show properties, packages, and progress; audit log display is not implemented yet and is Phase 4-bound. [VERIFIED: repo grep] |
| BOX-08 | `GetOpenBoxesAsync` filters `Open` and orders by `CreatedAt` descending; current UI renders cards, not the decided table. [VERIFIED: repo grep] |
| HOME-01 | `Views/Home/Index.cshtml` has a barcode input with `autofocus`. [VERIFIED: repo grep] |
| HOME-02 | Current home lookup always redirects to `Box/Details`; it must redirect open boxes to preparation when that route exists, or details with open scan area only if planner explicitly treats details as preparation for MVP. [VERIFIED: repo grep] |
| HOME-03 | Current lookup redirects all found boxes to details; read-only behavior depends on status because scan form only shows for `Open`. [VERIFIED: repo grep] |
| HOME-04 | Current home lookup does not explicitly detect package barcodes; add a package-format guard before DB lookup. [VERIFIED: repo grep] |

## Existing Code Patterns

- Project is an ASP.NET Core MVC app targeting `net8.0` with nullable enabled. [VERIFIED: `MothersonBoxManagement/MothersonBoxManagement.csproj`]
- EF Core SQL Server and EF Core Design are already referenced with `8.0.*` versions; no new NuGet package is needed for Phase 2. [VERIFIED: `MothersonBoxManagement/MothersonBoxManagement.csproj`]
- Controllers are constructor-injected and protected with `[Authorize]` for authenticated pages. [VERIFIED: `Controllers/BoxController.cs`, `Controllers/HomeController.cs`]
- Business logic belongs in `Services/BoxService.cs` behind `Services/IBoxService.cs`; controllers should remain thin and map ViewModels/claims to DTO calls. [VERIFIED: repo pattern + AGENTS.md]
- Views must use ViewModels/DTOs, not EF entities. Existing Phase 2 views use `CreateBoxViewModel` and `BoxDetailsDto`. [VERIFIED: repo grep]
- `ApplicationDbContext` already defines unique indexes for `Boxes.BoxNumber`, `Boxes.BarcodeValue`, `Users.Matricule`, and `BoxPackages.PackageBarcode`, plus `Box.RowVersion` as rowversion. [VERIFIED: `Data/ApplicationDbContext.cs`]
- Existing integration tests use xUnit, `WebApplicationFactory<Program>`, EF Core InMemory database, and real login via `/Account/Login`. [VERIFIED: `MothersonBoxManagement.Tests/CustomWebApplicationFactory.cs`, `BoxControllerTests.cs`]

## Required Files and Components

Use these existing files; do not create parallel duplicates. [VERIFIED: repo grep]

| Area | Files | Required Phase 2 Action |
|------|-------|-------------------------|
| Service | `Services/IBoxService.cs`, `Services/BoxService.cs` | Fix barcode generation to `BarcodeValue = BoxNumber`; use 6-character hex, not decimal `Random.Shared.Next(100000, 999999)`. Add collision retry around unique indexes. |
| Controller | `Controllers/HomeController.cs` | Replace `ViewBag` plumbing with `HomeViewModel` if changing view shape; add package-barcode warning and status-based redirect. |
| Controller | `Controllers/BoxController.cs` | Keep create/details thin; avoid adding business rules here. |
| ViewModels/DTOs | `ViewModels/CreateBoxViewModel.cs`, `Data/Dtos/*.cs` | Align dimensions to context decision: positive integers, or record a decision update if decimals are intentionally retained. Add dashboard fields for last update and last user. |
| Views | `Views/Home/Index.cshtml`, `Views/Box/Create.cshtml`, `Views/Box/Details.cshtml` | Convert dashboard cards to Bootstrap table; show decided columns; keep autofocus scan input. |
| Data | `Data/ApplicationDbContext.cs`, `Entities/Box.cs` | No schema migration required if only changing generated values and validation. Migration required only if dimension types change from `double` to `int`. |
| Tests | `MothersonBoxManagement.Tests/BoxControllerTests.cs` | Update `BX-` assertion; add home lookup/package warning/status redirect coverage. |
| Docs | `AGENT.md`, `TODO.md` | Update if routes, entity types, validation rules, or migration status change. [VERIFIED: AGENTS.md] |

## Implementation Guidance

### Box Number and Barcode

`BoxService.CreateBoxAsync` is the critical fix point. Current code:

```csharp
var boxNumber = $"BOX-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";
var barcode = $"BX-{boxNumber}";
```

Replace with a helper that generates `BOX-{date}-{sixHex}` and assigns both properties to that value. [VERIFIED: `Services/BoxService.cs`]

Recommended shape:

```csharp
private static string GenerateBoxNumber(DateTime utcNow)
{
    var suffix = Random.Shared.Next(0x000000, 0x1000000).ToString("X6");
    return $"BOX-{utcNow:yyyyMMdd}-{suffix}";
}
```

On create, retry a small fixed number of times if a generated `BoxNumber` or `BarcodeValue` collides with the unique database indexes. Do not rely on random uniqueness alone. [VERIFIED: `Data/ApplicationDbContext.cs`]

### Homepage Lookup

Home lookup should validate the scanned input before database lookup. [VERIFIED: context + current `HomeController.cs`]

- Empty input: inline error and keep focus.
- `StartsWith("BOX", OrdinalIgnoreCase)` false: show the package warning from context, not the generic "not found" message.
- Found `Open` box: redirect to the preparation target. If Phase 2 keeps details as the temporary preparation screen, document that choice and route to `Details`.
- Found non-open box: redirect to details in read-only mode.
- Unknown `BOX...` barcode: inline "No box found with this barcode."

### Dashboard

`GetOpenBoxesAsync` already filters `BoxStatus.Open` and sorts newest first. [VERIFIED: `Services/BoxService.cs`] The view does not match context because it renders cards and omits last user/last update. [VERIFIED: `Views/Home/Index.cshtml`]

Planner should add or adjust DTO fields:

- `LastUpdatedAt = UpdatedAt ?? CreatedAt`
- `LastUserMatricule = LastModifiedBy?.Matricule ?? CreatedBy.Matricule`

Use a Bootstrap `<table>` as decided, not cards. Add a simple meta refresh or JavaScript interval only if needed for the success criteria; keep it unobtrusive and configurable. [VERIFIED: context]

### Details and Read-Only Behavior

`Details.cshtml` only shows the package scan form when `Model.Status == Open`, which already supports read-only display for completed/cancelled/archived/blocked boxes. [VERIFIED: `Views/Box/Details.cshtml`] Phase 2 should not expand package scanning logic except where needed to remove `BX-` assumptions from shared barcode semantics. [VERIFIED: repo grep]

## Risk Notes

- High risk: stale plan and current code both preserve `BX-`; this directly contradicts authoritative Phase 2 context. Fix code and tests together. [VERIFIED: repo grep]
- High risk: `Random.Shared.Next(100000, 999999)` is decimal, not 6-character hexadecimal; it also excludes leading zeros. [VERIFIED: `Services/BoxService.cs`]
- Medium risk: current integration tests share one InMemory database name (`TestDb`), so data can leak across tests and hide ordering/lookup bugs. Use a unique database per factory/test run if tests become flaky. [VERIFIED: `CustomWebApplicationFactory.cs`]
- Medium risk: EF InMemory does not enforce SQL Server unique indexes or rowversion behavior; service collision tests should mock/check retry logic or use SQLite/SQL Server integration later. [VERIFIED: repo test setup]
- Medium risk: `HomeViewModel.cs` is incorrectly under the `MothersonBoxManagement.Controllers` namespace despite living in `ViewModels`; fix if it is used for typed home views. [VERIFIED: `ViewModels/HomeViewModel.cs`]
- Medium risk: `AGENT.md` still says barcode prefix examples may need validation; Phase 2 context now locks box barcode semantics, so docs should be updated after implementation. [VERIFIED: `AGENT.md` + context]
- Low risk: existing UI text is mostly French, but some required context messages are in English; keep final operator UI consistently French unless project owner requests English. [VERIFIED: repo views]

## Tests To Add or Update

Update `MothersonBoxManagement.Tests/BoxControllerTests.cs`: [VERIFIED: repo file]

- Replace `Assert.Contains("BX-", content)` with an assertion that the displayed barcode equals the `BOX-YYYYMMDD-XXXXXX` box number pattern.
- Add service or integration test: box creation stores `BoxNumber == BarcodeValue`.
- Add test: generated box number matches `^BOX-\d{8}-[0-9A-F]{6}$`.
- Add test: home lookup with known open box barcode redirects according to the Phase 2 preparation target.
- Add test: home lookup with a non-open box redirects to details and details does not show the scan form.
- Add test: home lookup with a package barcode such as `PKG-20260702-ABCDEF` returns the explicit package-warning message.
- Add test: homepage dashboard shows only open boxes and sorts newest first.
- If dimension type changes to `int`, add validation tests rejecting `30.5`, `0`, and negative values.

Run commands after implementation: [VERIFIED: AGENTS.md]

```powershell
dotnet restore
dotnet build
dotnet test
```

## Package Legitimacy Audit

No external package installation is required for Phase 2. Existing packages are `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.EntityFrameworkCore.Design` from the project file. [VERIFIED: `MothersonBoxManagement/MothersonBoxManagement.csproj`]

## Validation Architecture

Nyquist validation is enabled in `.planning/config.json`. [VERIFIED: repo file]

| Property | Value |
|----------|-------|
| Framework | xUnit with `Microsoft.AspNetCore.Mvc.Testing` patterns already present. [VERIFIED: tests] |
| Quick command | `dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj --filter BoxControllerTests` |
| Full command | `dotnet test` |
| Main gap | Tests currently assert the stale `BX-` prefix and do not cover package warning or status-based home redirect. [VERIFIED: `BoxControllerTests.cs`] |

## Security Domain

- V2 Authentication applies: Phase 2 routes are behind cookie authentication and `[Authorize]`; retain that. [VERIFIED: controllers]
- V4 Access Control applies: all authenticated roles may create boxes by decision; do not add exception-role restrictions in Phase 2. [VERIFIED: context]
- V5 Input Validation applies: keep server-side DataAnnotations and do not rely only on HTML `min` attributes. [VERIFIED: `CreateBoxViewModel.cs`]
- V6 Cryptography does not require new Phase 2 work; password hashing remains Phase 1/auth code. [VERIFIED: repo grep]
- Avoid logging raw secrets or database internals in user-visible errors. [VERIFIED: AGENTS.md]

## Sources

- `.planning/phases/02-box-lifecycle-home-lookup/02-CONTEXT.md`
- `.planning/PROJECT.md`
- `.planning/ROADMAP.md`
- `.planning/REQUIREMENTS.md`
- `AGENTS.md`
- `AGENT.md`
- `MothersonBoxManagement/Services/BoxService.cs`
- `MothersonBoxManagement/Controllers/HomeController.cs`
- `MothersonBoxManagement/Controllers/BoxController.cs`
- `MothersonBoxManagement/Data/ApplicationDbContext.cs`
- `MothersonBoxManagement/Views/Home/Index.cshtml`
- `MothersonBoxManagement/Views/Box/Details.cshtml`
- `MothersonBoxManagement.Tests/BoxControllerTests.cs`

## Open Questions (RESOLVED)

1. Dimensions must be changed from `double` to `int` to honor D-10 exactly. Phase 2 planning requires a `UseIntegerBoxDimensions` EF Core migration and integer validation across entity, DTO, ViewModel, and Razor inputs. [RESOLVED: checker feedback + context]
2. Phase 2 must create a distinct `Box/Prepare/{id}` route now. Homepage lookup redirects open boxes to `/Box/Prepare/{id}` and redirects non-open boxes to read-only `/Box/Details/{id}`. [RESOLVED: checker feedback + context]

## RESEARCH COMPLETE
