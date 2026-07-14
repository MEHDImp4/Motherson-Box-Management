<!-- generated-by: gsd-doc-writer -->
# Development Guide

This project targets `.NET 8`, `ASP.NET Core MVC`, `EF Core`, and `SQL Server`.

## Daily setup

```powershell
dotnet restore
dotnet build
dotnet test
```

You can work either:

- locally against SQL Server / LocalDB
- through `docker compose up --build`

## Structure

```text
MothersonBoxManagement/
|-- Controllers/     # thin MVC controllers
|-- Data/            # DbContext, DTOs, interceptor, seeding
|-- Entities/        # EF Core entities
|-- Security/        # role constants and auth helpers
|-- Services/        # business logic
|-- ViewModels/      # UI contracts
|-- Views/           # Razor pages
`-- wwwroot/         # static assets
```

## Development rules

- Keep controllers thin.
- Put business logic in services.
- Use `async/await` for I/O.
- Respect nullable reference types.
- Add explicit EF Core migrations for schema changes.
- Update `AGENT.md` and `TODO.md` when behavior or structure changes.

## Database workflow

The application migrates the database automatically on startup in `Development`. Outside `Development`, startup migration requires `Database__AutoMigrate=true`; otherwise run migrations explicitly:

```powershell
dotnet ef database update --project MothersonBoxManagement --startup-project MothersonBoxManagement
```

## Docker workflow

Files added for V1 packaging:

- `Dockerfile`
- `docker-compose.yml`
- `.env.example`
- `.dockerignore`

These files provide a consistent local release environment without changing the application startup path.

## Release workflow

Before a release:

1. `dotnet restore`
2. `dotnet build`
3. `dotnet test`
4. update docs and planning records
5. create the release commit
6. tag the release
7. push branch and tag

## Known build note

If `dotnet build` and `dotnet test` are launched at the same time, `Microsoft.AspNetCore.Mvc.Testing` may briefly lock `MvcTestingAppManifest.json`. Run them serially for reliable release validation.
