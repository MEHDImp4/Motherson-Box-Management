<!-- GSD:project-start source:PROJECT.md -->

## Project

**Motherson Box Management**

Motherson Box Management is a web application designed for packaging operators, supervisors, and administrators in the P3 zone to prepare, track, and guarantee the traceability of packaging boxes. The application uses barcodes to record and control cable packages added to each box in real-time, functioning autonomously without connection to external ERP or MES systems for its MVP version.

**Core Value:** Ensure absolute traceability of packaging boxes and guarantee that no cable package is ever scanned or assigned to more than one box.

### Constraints

- **Tech Stack**: ASP.NET Core MVC, Entity Framework Core, SQL Server — Mandated by technical target specification.
- **UI Design**: Standard Bootstrap layout typical of corporate intranet portals — Selected by the user for corporate environment alignment.
- **Database Architecture**: Single table for Boxes and single table for BoxPackages, avoiding dynamic table creation per box to ensure ease of audit and search.
- **Traceability**: An immutable database journal (BoxAuditLogs) must be written for all operations, with no edit or delete access from the UI.

<!-- GSD:project-end -->

<!-- GSD:stack-start source:research/STACK.md -->

## Technology Stack

## Recommended Stack

### Core Framework

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| ASP.NET Core MVC | 8.0 | Web Application Architecture | Standard web framework providing MVC pattern, robust performance, and easy integration with corporate layouts. |

### Database

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| SQL Server | 2022 | Centralized relational storage | Mandated target database. Provides excellent concurrency control, transactional integrity, and native support for EF Core features (like `rowversion`). |
| Entity Framework Core | 8.0 | Object-Relational Mapper (ORM) | Simplifies SQL operations, handles migrations, and supports custom SaveChanges interceptors for auditing. |

### Infrastructure

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Local IIS / Kestrel | 8.0 | Web Application Hosting | Built-in hosting model for local factory workstation environment deployment. |

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| ASP.NET Core Cookie Authentication | Built-in | User session management | Handles role-based access control (Operator, Supervisor, Admin) via custom matricule claims. |
| System.Text.Json | Built-in | Serializing Audit Trail values | Used inside the EF Core SaveChanges Interceptor to store entity property changes as JSON strings in `BoxAuditLogs`. |
| Bootstrap | 5.3 | Responsive frontend layout | Mandated standard intranet portal portal styling. |

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| ORM | EF Core 8.0 | Dapper | EF Core provides a cleaner interceptor mechanism for audit logs and native support for concurrency tracking (`rowversion` and DbUpdateConcurrencyException). |
| Authentication | Cookie Auth | ASP.NET Core Identity | ASP.NET Core Identity is geared towards email/username registration with a complex database schema (users, roles, claims, tokens), whereas the MVP calls for simple matricule-based credentials validateable against a single `Users` table. |

## Installation

# EF Core CLI Tools (Global)

# EF Core Packages

## Sources

- [Microsoft EF Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Cookie Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie)

<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->

## Conventions

Conventions not yet established. Will populate as patterns emerge during development.
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->

## Architecture

Architecture not yet mapped. Follow existing patterns found in the codebase.
<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->

## Project Skills

No project skills found. Add skills to any of: `.claude/skills/`, `.agents/skills/`, `.cursor/skills/`, `.github/skills/`, or `.codex/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->

## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:

- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->

<!-- GSD:profile-start -->

## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
