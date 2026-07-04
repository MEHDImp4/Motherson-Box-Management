# Phase 1: Database & Authentication - Context

**Gathered:** 2026-07-02
**Status:** Ready for planning

<domain>
## Phase Boundary

This phase delivers the initial ASP.NET Core MVC skeleton, Entity Framework Core DbContext configurations, SQL Server database schema creation/migrations, user role-based seeding, and custom Cookie Authentication using matricule claims.

</domain>

<decisions>
## Implementation Decisions

### Authentication & Password Security
- **D-01:** ASP.NET Core `PasswordHasher<User>` (PBKDF2) will be used to hash and verify user passwords.
- **D-02:** Pre-configured default users (`OP001` (Operator), `SP001` (Supervisor), `AD001` (Admin) with default password `Motherson2026!`) will be seeded in the database initializer.
- **D-03:** Alphanumeric check (length 3-20) in the ViewModel will validate input matricule formats before running database queries.
- **D-04:** EF Core database migrations and seeding will run automatically during application startup.

### Cookie Lifecycle on Workstations
- **D-05:** Sliding Expiration (60 minutes) will be used for authentication cookies, resetting the session window automatically if the user is active.
- **D-06:** Session-only cookies (not persistent) will be configured, ensuring that closing the browser instantly destroys the session to prevent session hijacking on shared factory workstation terminals.

### Database Connection & Environment Configuration
- **D-07:** The connection string will be configured in `appsettings.json` (LocalDB default) and overridden locally via `appsettings.Development.json` (git-ignored) for security and flexibility.
- **D-08:** A custom Cookie Name `Motherson.BoxManagement.Auth` will be defined with `HttpOnly` and `SameAsRequest` secure settings.

### the agent's Discretion
- None — all decisions for Phase 1 have been explicitly aligned with the user.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project Framework & Goals
- [.planning/PROJECT.md](file:///.planning/PROJECT.md) — Main constraints, conventions, and stacks.
- [.planning/ROADMAP.md](file:///.planning/ROADMAP.md) — Milestone details, phase boundaries, and success criteria.
- [.planning/REQUIREMENTS.md](file:///.planning/REQUIREMENTS.md) — AUTH-01, AUTH-02, AUTH-03, and BOX-05 requirements definitions.
- [GEMINI.md](file:///GEMINI.md) — Coding conventions, nullables policies, EF Core unique indexes, and transaction requirements.

### Technical Research References
- [.planning/research/STACK.md](file:///.planning/research/STACK.md) — Recommended dependencies and library configurations.
- [.planning/research/ARCHITECTURE.md](file:///.planning/research/ARCHITECTURE.md) — Folder layout structures and component boundaries.
- [.planning/research/PITFALLS.md](file:///.planning/research/PITFALLS.md) — Concurrency, double-scan races, and validation warnings.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- None — starting with an empty workspace root. The project skeleton will be generated in this phase.

### Established Patterns
- Standard ASP.NET Core MVC 8.0 directory layout (`/Controllers`, `/Views`, `/Models`, `/Data`, `/Entities`, `/Services`).
- Cookie-based claims authentication using standard MVC routing.

### Integration Points
- `/Account/Login` and `/Account/Logout` controllers for authentication.
- Application DB startup migration hook inside `Program.cs`.

</code_context>

<specifics>
## Specific Ideas
- The application must be configured to run as a local web server (Kestrel) viewed in browser tabs, using session-only cookies to handle local workstation operator transitions.

</specifics>

<deferred>
## Deferred Ideas
- None — discussion stayed within Phase 1 scope.

</deferred>

---

*Phase: 01-database-authentication*
*Context gathered: 2026-07-02*
