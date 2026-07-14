---
title: Configuration
description: Configuration guide for local, Docker, and release-ready development
---

# Configuration

## Security-sensitive configuration

- Demo users require `DemoUsers:OP001:Password`, `DemoUsers:SP001:Password`, and `DemoUsers:AD001:Password`. Values must be distinct, at least 16 characters, and stored in user-secrets or an ignored `.env` file.
- `SeedDemoUsers=true` is rejected in Production.
- Direct label printing uses a workstation-scoped Windows agent. The server stores no agent secret in clear text and accepts only outbound HTTPS polling from the workstation.
- Production Data Protection keys are persisted outside the container and encrypted with a dedicated PFX.
- The application connection uses a least-privilege SQL login; `sa` is limited to disposable Development environments.

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
| `Database__AutoMigrate` | No | Set to `true` only when the app should run EF Core migrations at startup outside `Development` |
| `SeedDemoUsers` | No | Set to `true` only for controlled non-production demo environments |
| `AllowedHosts` | Yes outside local dev | Host allow-list for ASP.NET Core |

## Local print agent

1. An administrator sets the browser-local workstation name in `/Box/Settings`.
2. Download `MothersonPrintAgentSetup.exe` from the same page and run it. It installs under `%LocalAppData%\Motherson\PrintAgent` and registers an `HKCU` startup entry without elevation.
3. Generate the 12-character pairing code in Settings, then enter the application HTTPS URL and code in the agent window within 10 minutes.
4. After the first heartbeat, select one of the printers reported by Windows and choose `Windows driver` or `Raw ZPL`.

The fixed label profile is 100 × 100 mm, 203 dpi, one copy. The agent accepts HTTP only for loopback development; shared deployments must use HTTPS.

Run `./ops/deploy/build-print-agent.ps1` to build the agent. The script writes the EXE and its SHA-256 metadata into `MothersonBoxManagement/App_Data/Downloads`, and the web publish embeds both files under `App_Data/Downloads`. No code-signing certificate or signing secret is required.

## Docker environment file

For Docker-based local startup, copy `.env.example` to `.env` and set:

| Variable | Description |
| --- | --- |
| `MSSQL_SA_PASSWORD` | SQL Server `sa` password used by the container |
| `MOTHERSON_DB_NAME` | Database name |
| `MOTHERSON_APP_PORT` | Host port mapped to the app |
| `MOTHERSON_SQL_PORT` | Host port mapped to SQL Server; use `11433` if local `1433` is already occupied |
| `MOTHERSON_WORKSTATION` | Workstation name exposed to the app |

For production-style Docker startup, use `docker-compose.prod.yml` and provide:

| Variable | Description |
| --- | --- |
| `MOTHERSON_ALLOWED_HOSTS` | Allowed host names for the deployed app |
| `MOTHERSON_DB_HOST` | SQL Server host |
| `MOTHERSON_DB_PORT` | SQL Server port, defaults to `1433` |
| `MOTHERSON_DB_NAME` | Production database name |
| `MOTHERSON_DB_APP_USER` | Least-privilege application SQL login |
| `MOTHERSON_DB_APP_PASSWORD` | Password for the application SQL login |
| `MOTHERSON_DB_TRUST_CERTIFICATE` | `False` by default; use `True` only for controlled internal certificates |

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
- applies migrations with `Database.MigrateAsync()` only in `Development` or when `Database__AutoMigrate=true`
- runs `DbInitializer.SeedAsync()` only in `Development` or when `SeedDemoUsers=true`
- exposes `/health` for anonymous infrastructure health checks

## Development seed accounts

The following users are inserted when absent only in `Development` or when `SeedDemoUsers=true` is explicitly configured:

| Matricule | Role |
| --- | --- |
| `OP001` | Operator |
| `SP001` | Supervisor |
| `AD001` | Administrator |

Seed credentials are for local development and automated tests only. Do not enable demo seeding in production.

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
| SQL Server | `11433` by default in `.env.example`, or `1433` when `MOTHERSON_SQL_PORT` is not set |

## Related files

- `MothersonBoxManagement/appsettings.json`
- `MothersonBoxManagement/appsettings.Development.json`
- `.env.example`
- `docker-compose.yml`
- `Dockerfile`
