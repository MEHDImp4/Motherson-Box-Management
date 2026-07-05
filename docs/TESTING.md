---
title: Testing
description: Test strategy and current validation baseline
---

# Testing

The `v1` release is backed by an xUnit test suite using `WebApplicationFactory` and EF Core InMemory support for isolated application tests.

## Current baseline

- `129/129` tests passing
- `dotnet build` passing
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

- authentication and role access
- dashboard and box lookup
- package scanning
- duplicate prevention
- optimistic concurrency
- supervisor exception flows
- audit log access and structure
- security and validation behavior
- end-to-end lifecycle scenarios

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
