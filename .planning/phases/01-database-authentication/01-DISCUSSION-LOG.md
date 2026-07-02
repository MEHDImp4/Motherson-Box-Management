# Phase 1: Database & Authentication - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-02
**Phase:** 1-Database & Authentication
**Areas discussed:** Authentication & Password Security, Cookie Lifecycle on Workstations, Database Connection & Environment Configuration

---

## Authentication & Password Security

| Option | Description | Selected |
|--------|-------------|----------|
| ASP.NET Core PasswordHasher | Uses PBKDF2, built-in, no external package required | ✓ |
| BCrypt.Net-Next | Standard, widely used .NET library, simple API | |
| You decide | Select the most appropriate standard hashing method | |

**User's choice:** ASP.NET Core PasswordHasher (Uses PBKDF2, built-in).

---

| Option | Description | Selected |
|--------|-------------|----------|
| Pre-configured default credentials | Matricules OP001/SP001/AD001, default password 'Motherson2026!' | ✓ |
| Custom seed credentials | Defined on the fly | |
| You decide | Set standard test credentials | |

**User's choice:** Pre-configured default credentials.

---

| Option | Description | Selected |
|--------|-------------|----------|
| Standard alphanumeric check | Length 3-20, letters and numbers check in ViewModel | ✓ |
| Strict regex validation | Matches specific Motherson matricule pattern | |
| You decide | Standard alphanumeric validation | |

**User's choice:** Standard alphanumeric check.

---

| Option | Description | Selected |
|--------|-------------|----------|
| Run automatically | Database migrations and seeding automatically during application startup | ✓ |
| Migration HasData | Seeding via EF Core ModelBuilder HasData | |
| You decide | Run automatically on startup | |

**User's choice:** Run database migrations and seeding automatically during application startup.

---

## Cookie Lifecycle on Workstations

| Option | Description | Selected |
|--------|-------------|----------|
| Sliding Expiration | Extends session automatically if user is active (60 mins) | ✓ |
| Absolute Expiration | Session strictly expires after 60 minutes, forcing logout | |
| You decide | Sliding Expiration | |

**User's choice:** Sliding Expiration (60 minutes).

---

| Option | Description | Selected |
|--------|-------------|----------|
| Session-only cookie | Clears immediately when browser closes (shared workstation safety) | ✓ |
| Persistent cookie | Survives browser close and remains valid up to expiration window | |
| You decide | Session-only cookie | |

**User's choice:** Session-only cookie (cleared on browser close).

*Note:* Clarified with user that ASP.NET Core MVC runs via browser on workstations, thus cookie authentication policies apply.

---

## Database Connection & Environment Configuration

| Option | Description | Selected |
|--------|-------------|----------|
| appsettings default + overrides | appsettings.json development default, appsettings.Development.json (git-ignored) for local overrides | ✓ |
| Strictly environment variables | Configured strictly via environment variables | |
| You decide | appsettings.json default + overrides | |

**User's choice:** appsettings default + git-ignored local overrides.

---

| Option | Description | Selected |
|--------|-------------|----------|
| Custom Cookie Name | Custom name 'Motherson.BoxManagement.Auth' with HttpOnly and SameAsRequest security settings | ✓ |
| ASP.NET Core default name | Rely on ASP.NET Core default cookie naming | |
| You decide | Custom Cookie Name | |

**User's choice:** Custom Cookie Name with HttpOnly and SameAsRequest.

---

## the agent's Discretion
- None — all choices were explicitly confirmed by the user.

## Deferred Ideas
- None — discussion stayed within Phase 1 scope.
