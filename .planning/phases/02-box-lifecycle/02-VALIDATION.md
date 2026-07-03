# Phase 2 Validation Architecture

**Phase:** 02-box-lifecycle  
**Created:** 2026-07-02  
**Nyquist:** Enabled  
**Scope:** BOX-01, BOX-02, BOX-03, BOX-04, BOX-05, BOX-07, BOX-08, HOME-01, HOME-02, HOME-03, HOME-04. BOX-06 remains Phase 3.

## Validation Strategy

Phase 2 validation uses xUnit integration tests through the existing `MothersonBoxManagement.Tests` project and focused CLI checks for EF Core migration visibility. Every executable plan includes at least one automated verification command.

## Requirement Mapping

| Requirement | Required proof | Primary automated check |
|-------------|----------------|--------------------------|
| BOX-01 | Operator, Supervisor, and Administrator can create boxes with Carton, Bois, or Plastique. | `dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj --filter "FullyQualifiedName~BoxControllerTests"` |
| BOX-02 | Height, Width, and Depth are positive integer centimeters; decimal, zero, and negative values are rejected. | `dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj --filter "FullyQualifiedName~BoxControllerTests"` plus `dotnet ef migrations list --project MothersonBoxManagement --startup-project MothersonBoxManagement` |
| BOX-03 | ExpectedQuantity is an integer strictly greater than zero. | `dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj --filter "FullyQualifiedName~BoxControllerTests"` |
| BOX-04 | BoxNumber and BarcodeValue are identical and match `BOX-YYYYMMDD-XXXXXX` with a 6-character uppercase hex suffix. | `dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj --filter "FullyQualifiedName~BoxControllerTests"` |
| BOX-05 | Creator user and creation timestamp are persisted and displayed/available in details data. | `dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj --filter "FullyQualifiedName~BoxControllerTests"` |
| BOX-07 | Details show properties, packages, progress, and a Phase 4-backed audit/history area when logs are absent. | `dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj --filter "FullyQualifiedName~BoxControllerTests"` |
| BOX-08 | Homepage dashboard shows open boxes only, newest first, with progress, last update, and last user in a Bootstrap table. | `dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj --filter "FullyQualifiedName~BoxControllerTests"` |
| HOME-01 | Homepage box barcode input has autofocus. | `dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj --filter "FullyQualifiedName~BoxControllerTests"` |
| HOME-02 | Known open box barcode redirects to `/Box/Prepare/{id}`. | `dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj --filter "FullyQualifiedName~BoxControllerTests"` |
| HOME-03 | Known non-open box barcode redirects to `/Box/Details/{id}`. | `dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj --filter "FullyQualifiedName~BoxControllerTests"` |
| HOME-04 | Package barcode input on homepage shows the explicit package warning and stays on homepage. | `dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj --filter "FullyQualifiedName~BoxControllerTests"` |

## Plan Verification Map

| Plan | Automated verification | Covers |
|------|------------------------|--------|
| 02-01 | `dotnet build MothersonBoxManagement/MothersonBoxManagement.csproj`; `dotnet ef migrations list --project MothersonBoxManagement --startup-project MothersonBoxManagement` | TODO start gate, BOX-01, BOX-02, BOX-03 |
| 02-02 | `dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj --filter "FullyQualifiedName~BoxControllerTests|FullyQualifiedName~ScanControllerTests"` | BOX-04, BOX-05, BOX-07, D-14 route target |
| 02-03 | `dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj --filter "FullyQualifiedName~BoxControllerTests"` | BOX-08, HOME-01, HOME-02, HOME-03, HOME-04 |
| 02-04 | `dotnet restore`; `dotnet build`; `dotnet test` | Full Phase 2 regression, docs, TODO close gate |

## Negative Scope

These items are intentionally not validated in Phase 2 because they belong to Phase 3 or later:

- BOX-06 auto-completion when scanned quantity equals expected quantity.
- USB keyboard wedge listener behavior.
- Virtual scanner simulator.
- Duplicate package scan transaction handling.
- SQL-level concurrent double-scan race testing.
- Immutable audit write interceptor.

## Acceptance Gate

Phase 2 execution is complete only when:

1. All four executable plans have summaries.
2. `dotnet restore`, `dotnet build`, and `dotnet test` pass from the repository root.
3. `TODO.md` marks TSK-007 through TSK-010 as `Terminé`.
4. `AGENT.md` records the Phase 2 barcode, route, dimension, and migration decisions.
