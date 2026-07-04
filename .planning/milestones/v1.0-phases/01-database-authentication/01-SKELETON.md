# Walking Skeleton — Motherson Box Management

**Phase:** 1
**Generated:** 2026-07-02

## Capability Proven End-to-End

> A packaging zone user can navigate to the application, see a login page, enter their matricule and password, authenticate against a SQL Server database with hashed credentials, receive a role-bearing session cookie, and land on an authorized home page displaying their identity — or be shown a generic error on failure.

## Architectural Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Framework | ASP.NET Core MVC 8.0 | Mandated by project constraints; mature, well-supported for intranet apps |
| Data layer | Entity Framework Core 8.0 + SQL Server 2022 (Docker) | EF Core Fluent API enables unique indexes, concurrency tokens, and migration-based schema management |
| Auth | Custom Cookie Authentication (not ASP.NET Identity) | Simpler for matricule-based login on shared factory workstations; session-only cookies for security |
| Password storage | PasswordHasher\<User\> (PBKDF2) | ASP.NET Core built-in, cryptographically secure, no external dependency |
| Cookie policy | HttpOnly, SameSite=Lax, SameAsRequest, Sliding 60min, IsPersistent=false | Prevents XSS cookie theft, CSRF mitigation, auto-logout on browser close for shared terminals |
| Deployment target | Local Kestrel dev server (dotnet run) | MVP phase — no production deployment yet |
| Directory layout | /Entities, /Data, /Services, /ViewModels, /Controllers, /Views | Standard ASP.NET Core MVC convention per GEMINI.md |
| UI framework | Bootstrap 5.3 CDN + Inter font + custom accent #0f52ba | Corporate intranet aesthetic per UI-SPEC |
| Test framework | xUnit + Microsoft.AspNetCore.Mvc.Testing | Standard .NET integration testing with WebApplicationFactory |
| SQL environment | Docker container mcr.microsoft.com/mssql/server:2022-latest | Local machine lacks SQL Server; Docker is already installed |

## Stack Touched in Phase 1

- [x] Project scaffold — `dotnet new mvc`, .csproj NuGet packages, solution file
- [x] Routing — AccountController (Login/Logout), HomeController (Index), default route
- [x] Database — EF Core DbContext, Fluent API (unique indexes, RowVersion), InitialSchema migration, seed data
- [x] UI — Login.cshtml (Bootstrap 5.3 card), Home/Index.cshtml (welcome page), _Layout.cshtml, _LoginPartial.cshtml
- [x] Auth middleware — Cookie authentication pipeline, claims identity, role-based [Authorize]
- [ ] Deployment / local run — `dotnet run` on Kestrel (dev only, no production deploy in this phase)

## Out of Scope

| Item | Deferred To |
|---|---|
| Box creation, dashboard, lifecycle management | Phase 2 |
| Barcode scanning (USB wedge + simulator) | Phase 3 |
| Supervisor exceptions, audit trail interceptor | Phase 4 |
| Concurrency stress testing, security hardening | Phase 5 |
| Password change / reset flow | Future milestone |
| HTTPS / TLS configuration | Production deployment |
| Role-specific home pages (separate views per role) | Phase 2 (dashboard) |
| User CRUD management UI | Future milestone |

## Subsequent Slice Plan

| Phase | Slice | Builds On |
|---|---|---|
| Phase 2 | Box creation form → DB write → dashboard read → barcode lookup redirect | Phase 1 skeleton (auth, entities, DbContext) |
| Phase 3 | Package scan input → uniqueness check → box quantity update → status transition | Phase 2 (box entity, preparation screen) |
| Phase 4 | Supervisor exception actions → audit trail SaveChangesInterceptor → log viewer | Phase 3 (scan operations, box state machine) |
| Phase 5 | Concurrent scan simulation → double-allocation test → error log security audit | Phase 4 (all features complete) |
