---
title: Testing
description: Test strategy and current validation baseline
---

# Testing

The `v1` release is backed by an xUnit test suite using `WebApplicationFactory` and EF Core InMemory support for isolated application tests.

## Current baseline

- `182/182` tests passing (2 SQL Server integration tests conditionally skipped)
- `dotnet build` passing (0 warnings, 0 errors)
- `dotnet test` passing

## Main commands

```powershell
dotnet test
```

```powershell
dotnet test --filter "FullyQualifiedName~ConcurrencyTests"
```

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

## Main coverage areas

- authentication, role access, and login lockout
- dashboard, box lookup, and template selection
- package scanning (auto-scan, manual double-scan, sticky mode)
- duplicate prevention and idempotency
- optimistic concurrency and retry logic
- supervisor exception flows (cancel, force-close, block, transfer, remove)
- audit log access, filtering, and structure
- security and validation behavior
- end-to-end lifecycle scenarios
- password recovery workflow
- print agent pairing, heartbeat, and job lifecycle
- workstation resolver and station identity
- QR code and barcode generation
- SQL Server constraint validation

## Important note for local validation

Run `dotnet build` and `dotnet test` serially. Running them in parallel can temporarily lock `MvcTestingAppManifest.json` in the test project.

## Test accounts

The test harness mirrors the seeded local users:

| Matricule | Role |
| --- | --- |
| `OP001` | Operator |
| `SP001` | Supervisor |
| `AD001` | Administrator |

## Suggested release check

```powershell
dotnet restore
dotnet build
dotnet test
```
