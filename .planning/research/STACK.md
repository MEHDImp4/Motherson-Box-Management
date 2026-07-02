# Technology Stack

**Project:** Motherson Box Management
**Researched:** 2026-07-02

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

```bash
# EF Core CLI Tools (Global)
dotnet tool install --global dotnet-ef

# EF Core Packages
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
```

## Sources

- [Microsoft EF Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Cookie Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie)
