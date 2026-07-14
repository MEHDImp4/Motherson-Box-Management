---
title: Getting Started
description: Setup and installation guide for local and Docker-based development
---

# Getting Started

This guide brings a new developer from clone to a running `v1` environment.

## Requirements

| Tool | Version | Check |
| --- | --- | --- |
| .NET SDK | 8.0+ | `dotnet --version` |
| Docker Desktop | Recent | `docker --version` |
| SQL Server or LocalDB | SQL Server 2022 compatible | `sqlcmd -?` |
| Git | Recent | `git --version` |

You can use either the Docker path or the direct local path below.

## Option 1: Run with Docker

### 1. Clone the repository

```powershell
git clone <REPOSITORY_URL>
cd Motherson_Box_Management
```

### 2. Create your local environment file

```powershell
Copy-Item .env.example .env
```

Set at least:

- `MSSQL_SA_PASSWORD`
- `MOTHERSON_APP_PORT` if `8080` is already used

### 3. Start the stack

```powershell
docker compose up --build
```

This starts:

- `sqlserver`: SQL Server 2022
- `web`: the ASP.NET Core MVC app

### 4. Open the application

Open [http://localhost:8080](http://localhost:8080), or the port you configured in `.env`.

### 5. What happens automatically

On startup, the app:

- connects to SQL Server
- applies EF Core migrations in `Development`
- seeds local test users through `DbInitializer.SeedAsync()` in `Development`

## Option 2: Run locally without Docker

### 1. Clone and restore

```powershell
git clone <REPOSITORY_URL>
cd Motherson_Box_Management
dotnet restore
```

### 2. Configure the connection string

Use one of these approaches:

#### `appsettings.Development.json`

```json
{
  "WorkstationName": "DEV-STATION-01",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=MothersonBoxDb;User Id=sa;Password=YOUR_PASSWORD_HERE;TrustServerCertificate=True;"
  }
}
```

#### LocalDB

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MothersonBoxDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

#### User Secrets

```powershell
dotnet user-secrets set "ConnectionStrings__DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=MothersonBoxDb;Trusted_Connection=True;MultipleActiveResultSets=true" --project MothersonBoxManagement
```

### 3. Build and run

```powershell
dotnet build
dotnet run --project MothersonBoxManagement
```

### 4. Open the application

Open the URL shown in the terminal, commonly [http://localhost:5169](http://localhost:5169).

## Seeded development accounts

The application creates these local accounts on first startup in `Development`:

| Matricule | Role |
| --- | --- |
| `OP001` | Operator |
| `SP001` | Supervisor |
| `AD001` | Administrator |

Existing users are not overwritten if they are already present in the database. Development seed credentials are local/test-only and must not be enabled in production.

## First verification checklist

1. Sign in with `OP001`.
2. Open the dashboard.
3. Create a box.
4. Open the prepare screen.
5. Simulate a `PKG-...` scan.
6. Confirm the shell shows the subtle `v1` release marker.

## Common issues

### SQL Server not reachable

- Verify the SQL Server container or local instance is running.
- Recheck `ConnectionStrings__DefaultConnection`.
- If using Docker, inspect `docker compose logs sqlserver`.

### Port already in use

- Change `MOTHERSON_APP_PORT` in `.env`, or
- change the ASP.NET Core local launch port.

### Migrations fail on startup

Run manually:

```powershell
dotnet ef database update --project MothersonBoxManagement --startup-project MothersonBoxManagement
```

### Test accounts do not appear

`DbInitializer.SeedAsync()` only runs in `Development` or when `SeedDemoUsers=true`, and only inserts missing accounts. If users already exist, the seed step will skip them.

## Project layout

```text
Motherson_Box_Management/
|-- MothersonBoxManagement/
|   |-- Controllers/
|   |-- Data/
|   |-- Entities/
|   |-- Security/
|   |-- Services/
|   |-- ViewModels/
|   |-- Views/
|   `-- wwwroot/
|-- MothersonBoxManagement.Tests/
|-- docs/
|-- .planning/
|-- Dockerfile
`-- docker-compose.yml
```
