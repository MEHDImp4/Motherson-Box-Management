---
title: Configuration
description: Configuration guide for local, Docker, and release-ready development
---

# Configuration

This document describes the current `v1` configuration model for Motherson Box Management.

## Main configuration sources

Configuration is resolved from:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. environment variables

## Core environment variables

| Variable | Required | Description |
| --- | --- | --- |
| `ConnectionStrings__DefaultConnection` | Yes | SQL Server connection string |
| `WorkstationName` | No | Workstation name written into audit logs |
| `ASPNETCORE_ENVIRONMENT` | No | `Development`, `Staging`, or `Production` |

## Docker environment file

For Docker-based local startup, copy `.env.example` to `.env` and set:

| Variable | Description |
| --- | --- |
| `MSSQL_SA_PASSWORD` | SQL Server `sa` password used by the container |
| `MOTHERSON_DB_NAME` | Database name |
| `MOTHERSON_APP_PORT` | Host port mapped to the app |
| `MOTHERSON_WORKSTATION` | Workstation name exposed to the app |

## Connection string examples

### Docker / local SQL Server

```text
Server=localhost,1433;Database=MothersonBoxDb;User Id=sa;Password=YOUR_PASSWORD_HERE;TrustServerCertificate=True;
```

### Docker internal app-to-db connection

```text
Server=sqlserver,1433;Database=MothersonBoxDb;User Id=sa;Password=YOUR_PASSWORD_HERE;TrustServerCertificate=True;
```

### LocalDB

```text
Server=(localdb)\\mssqllocaldb;Database=MothersonBoxDb;Trusted_Connection=True;MultipleActiveResultSets=true
```

## Authentication configuration

Cookie authentication is configured in `Program.cs` with:

- `Cookie.Name = Motherson.BoxManagement.Auth`
- `HttpOnly = true`
- `SameSite = Lax`
- `SlidingExpiration = true`
- 60-minute inactivity timeout

`CookieSecurePolicy` is environment-aware:

- `SameAsRequest` in development
- `Always` outside development

## Database startup behavior

At startup the app:

- uses SQL Server via EF Core
- applies migrations with `Database.MigrateAsync()`
- runs `DbInitializer.SeedAsync()` to create development accounts if missing

## Seeded accounts

The following users are inserted when absent:

| Matricule | Role |
| --- | --- |
| `OP001` | Operator |
| `SP001` | Supervisor |
| `AD001` | Administrator |

The default test password is `Motherson2026!`, hashed through `PasswordHasher<User>`.

## Secrets policy

- Never commit real passwords.
- Keep `appsettings.json` free of production secrets.
- Prefer environment variables or `dotnet user-secrets`.
- `.env` is ignored by Git; use `.env.example` as the checked-in template.

## Release metadata

The web project now embeds release metadata through `MothersonBoxManagement.csproj`:

- `Version = 1.0.0`
- `AssemblyVersion = 1.0.0.0`
- `FileVersion = 1.0.0.0`
- `InformationalVersion = v1`

The UI reads the informational version and renders a subtle release marker in the authenticated shell.

## Validation and rate limiting

Built-in protections currently configured in `Program.cs`:

- login rate limiting
- scan rate limiting
- global rate limiting
- security headers
- conditional HTTPS redirection
- HSTS in non-development environments

## Ports

| Mode | Port |
| --- | --- |
| Local ASP.NET Core profile | Usually `5169` |
| Docker app host port | `8080` by default |
| SQL Server | `1433` |

## Related files

- `MothersonBoxManagement/appsettings.json`
- `MothersonBoxManagement/appsettings.Development.json`
- `.env.example`
- `docker-compose.yml`
- `Dockerfile`
