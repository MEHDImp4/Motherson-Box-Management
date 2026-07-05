# Motherson Box Management

`v1` of **Motherson Box Management** is a standalone ASP.NET Core MVC application for the P3 packaging zone. It lets operators create boxes, scan cable packages, enforce one-package-per-system uniqueness, and keep a full audit trail for supervisor actions.

## V1 scope

- Role-based access for `Operator`, `Supervisor`, and `Administrator`
- Box creation with unique `BOX-YYYYMMDD-XXXXXX` identifiers
- Live package scanning with duplicate prevention and automatic completion
- Supervisor exception flows: cancel, force close, block/unblock, remove, transfer
- Immutable audit log
- Seeded development accounts
- Local startup through either Docker or a direct .NET + SQL Server setup

## Quick start with Docker

1. Copy the environment template:

```powershell
Copy-Item .env.example .env
```

2. Edit `.env` and set a strong SQL password in `MSSQL_SA_PASSWORD`.
3. Start the full stack:

```powershell
docker compose up --build
```

4. Open the app at [http://localhost:8080](http://localhost:8080).

The application runs EF Core migrations automatically on startup and seeds the test users if they do not already exist.

## Quick start without Docker

1. Install:
   - `.NET 8 SDK`
   - `SQL Server` or `LocalDB`
2. Configure the connection string with either:
   - `MothersonBoxManagement/appsettings.Development.json`
   - `dotnet user-secrets`
   - environment variable `ConnectionStrings__DefaultConnection`
3. Restore, build, and run:

```powershell
dotnet restore
dotnet build
dotnet run --project MothersonBoxManagement
```

4. Open the URL shown by ASP.NET Core, usually [http://localhost:5169](http://localhost:5169).

## Seeded test accounts

These accounts are created by `DbInitializer.SeedAsync()` on first startup:

| Role | Matricule | Password |
| --- | --- | --- |
| Operator | `OP001` | `Motherson2026!` |
| Supervisor | `SP001` | `Motherson2026!` |
| Administrator | `AD001` | `Motherson2026!` |

These credentials are for local development and testing only.

## Database creation and migrations

The app applies migrations automatically at startup:

- relational providers: `Database.MigrateAsync()`
- non-relational test providers: `Database.EnsureCreatedAsync()`

You can also run migrations manually:

```powershell
dotnet ef database update --project MothersonBoxManagement --startup-project MothersonBoxManagement
```

## Validation commands

Run these from the repository root:

```powershell
dotnet restore
dotnet build
dotnet test
```

Current release validation baseline:

- `dotnet build`: passes
- `dotnet test`: passes with `129/129` tests

## Release marker

The application now exposes a subtle `v1` marker in the authenticated shell so the deployed build can be visually identified without cluttering the UI.

## Repository structure

```text
Motherson_Box_Management/
|-- MothersonBoxManagement/          # ASP.NET Core MVC web app
|-- MothersonBoxManagement.Tests/    # xUnit test suite
|-- docs/                            # setup, configuration, architecture, testing
|-- .planning/                       # GSD planning and milestone artifacts
|-- Dockerfile                       # container image build
|-- docker-compose.yml               # local app + SQL Server stack
|-- .env.example                     # Docker environment template
|-- AGENT.md                         # persistent technical/project memory
|-- TODO.md                          # task tracking
```

## Important notes

- `PackageBarcode` remains globally unique at the SQL level.
- Box mutations rely on `RowVersion` optimistic concurrency.
- Sensitive supervisor actions require reasons and are audit logged.
- Real secrets and production connection strings must never be committed.

## Documentation

- [Getting Started](./docs/GETTING-STARTED.md)
- [Configuration](./docs/CONFIGURATION.md)
- [Development Guide](./docs/DEVELOPMENT.md)
- [Testing Guide](./docs/TESTING.md)
- [Architecture](./docs/ARCHITECTURE.md)
