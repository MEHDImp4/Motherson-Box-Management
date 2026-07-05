# AGENTS.md - Instructions for the Codex AI Agent

This file defines the strict development rules and guidance that the Codex agent must follow when working on the **Motherson Box Management** project.

## 1. C# and ASP.NET Core MVC Code Conventions
* **Naming style:**
  * `PascalCase` for classes, methods, properties, enums, interfaces, and structs.
  * `camelCase` for local variables and parameters.
  * `_camelCase` for private readonly fields.
* **Nullables:** Respect *Nullable Reference Types*. No nullable warning should be left unresolved.
* **Separation of responsibilities:**
  * Keep MVC controllers extremely thin, limited to routing, ViewModel binding, and state validation.
  * Put all business logic, calculations, state checks, and data orchestration into separate, testable business services.
  * Use dedicated ViewModels only for data flowing to and from Razor views. **Never expose EF Core entities directly to forms or external contracts.**
* **Asynchrony:** Always use `async` / `await` for I/O operations (database access, files). Pass through `CancellationToken`. Blocking the thread with `.Result` or `.Wait()` is strictly forbidden.
* **Dependency Injection:** Use native constructor-based dependency injection. Do not use a Service Locator.

## 2. Data Integrity and EF Core
* **Schema changes:** Any change to entities or entity configuration must have an explicitly named EF Core migration.
* **Immutable migrations:** Never manually edit or delete a migration that has already been validated and shared.
* **Documentation update:** After every database migration, immediately update the "Migration Status" section in `AGENT.md`.
* **SQL uniqueness guarantee:** Keep the global uniqueness constraint on `BoxPackages.PackageBarcode` at SQL Server level through EF Core Fluent API. Application checks alone are not enough.
* **SQL transactions:** Wrap package scans, transfers, and complex multi-table or multi-row operations in explicit SQL transactions to guarantee atomicity.
* **Optimistic concurrency:** Use `RowVersion` (or another concurrency token) on boxes to prevent simultaneous overwrites by two operators.
* **No physical deletion:** Never physically delete boxes or audit records from the UI. Any removal or disassociation must keep a clear historical trace.

## 3. Security and Authorization
* **Secrets and keys:** Never commit, display, or log secrets, plaintext passwords, or real connection strings in source code or logs. Use only standard placeholders (for example `ConnectionStrings__DefaultConnection`).
* **Passwords:** Store passwords only as secure cryptographic hashes (for example with `IPasswordHasher` from ASP.NET Core).
* **Access control:** Apply role-based authorization filters (`[Authorize(Roles = "...")]`) on all sensitive MVC actions.
* **Safe logging:** Do not log sensitive information (passwords, confidential personal data), and never show internal database details in user-facing error messages.
* **Security changes require documentation:** Any security-related change (authentication, authorization, rate limiting, headers, HTTPS, lockout, CSRF protection, etc.) **must** be immediately reflected in `AGENT.md` section 23 (Security Audit Status). This includes new security services, middleware, configuration, and architecture decisions.

## 4. Required Workflow (GSD Core)
Before making any change:
1. Check the GSD Core planning files in `.planning/` (`PROJECT.md`, `ROADMAP.md`) to confirm the active phase.
2. Read `AGENT.md` to understand the business and technical rules for the current scope.
3. Find or create the task in `TODO.md` and set its status to `In progress`.
4. Review the existing code and migrations.

During development:
1. Make small, targeted, incremental changes.
2. Implement business logic in isolated services and write matching tests.
3. Update `TODO.md` at each major step.
4. Update `AGENT.md` whenever a change touches the data model, migration status, business rules, routes, or NuGet dependencies.

Before considering the work complete:
1. Run the required validation commands:
   ```bash
   dotnet restore
   ```
   ```bash
   dotnet build
   ```
   ```bash
   dotnet test
   ```
2. Make sure the build and all tests pass without major warnings or errors.
3. Verify that the documentation (`AGENT.md` and `TODO.md`) is up to date.
4. Set the task status to `Completed` in `TODO.md`.

## 5. Git and Commits
* Never commit if the build or tests fail.
* Never use `git add .` without carefully inspecting the included files.
* Write commits using **Conventional Commits** (for example `feat(...)`, `fix(...)`, `db(migration)...`, `docs(agent)...`).
* If local permissions or the GSD Core workflow prevent automatic commit creation, provide the exact Git command to run.
