# Research Summary: Motherson Box Management

**Domain:** Barcode Packaging Box Management Web App
**Researched:** 2026-07-02
**Overall confidence:** HIGH

## Executive Summary

The Motherson Box Management application is an autonomous web application designed to track and secure the packaging process of cable packages into boxes. Built with **ASP.NET Core MVC 8.0**, **Entity Framework Core 8.0**, and **SQL Server 2022**, it focuses on absolute traceability and double-scan prevention.

Key technical requirements include using custom Cookie Authentication instead of full ASP.NET Core Identity to align with a simple employee matricule login. For traceability, an EF Core `ISaveChangesInterceptor` will intercept and record all database updates to an immutable `BoxAuditLogs` table, saving old and new values in JSON format. Concurrency control is achieved using SQL Server's unique constraint indexes on `BoxPackages.PackageBarcode` to prevent double-allocations at database level, and optimistic concurrency (`rowversion` / `Timestamp`) on the `Boxes` table to handle simultaneous updates.

To ensure smooth testing during development without physical hardware, the UI will feature a dedicated Virtual Barcode Scanner Simulator panel. The design will follow standard Bootstrap 5.3 layouts for corporate intranet portals.

## Key Findings

**Stack:** ASP.NET Core MVC 8.0, EF Core 8.0, SQL Server 2022, Cookie Authentication, Bootstrap 5.3.
**Architecture:** Controller -> Service Layer -> DbContext -> ISaveChangesInterceptor -> SQL Server.
**Critical pitfall:** Double-scan concurrency race condition resolved via unique index on PackageBarcode and DbUpdateConcurrencyException handling.

## Implications for Roadmap

Based on research, suggested phase structure:

1. **Database & Auth Setup** - Set up the ASP.NET Core MVC structure, SQL Server schema with Entity Framework Core migrations, and Role-Based Cookie Authentication using custom claims.
   - Addresses: Connect with unique ID, Seed test accounts, DbContext setup.
   - Avoids: Mixing system users with complex Identity schemas.

2. **Box Lifecycle & Home Lookup** - Implement Box entity creation (Carton/Bois/Plastique) with dimensions, status transitions, and the homepage direct lookup by scan.
   - Addresses: Create boxes, states management (Open, Completed, Cancelled, Archived, CompletedWithException, Blocked).
   - Avoids: Mixing scan contexts by separating home search scanner input from packaging scanning views.

3. **Barcode Scan Wedge Integration & Simulator** - Build the package scanning screen with Javascript keypress listeners, AJAX Fetch API endpoints, and a dedicated virtual scanner panel for mock tests.
   - Addresses: Scanning package barcodes, unique constraint checks, virtual scanner simulator.
   - Avoids: Focus loss in keyboard wedge mode by using a rapid-input speed listener.

4. **Supervisor Exceptions & Audit Trail** - Implement the EF Core SaveChanges interceptor, audit logs visual logs screen, and exception workflows (cancel, force completion, transfers, quarantine blocks).
   - Addresses: Exceptional operations, package transfers, immutable audit trail.
   - Avoids: Data loss from browser tab closes by saving scanned packages immediately.

5. **Verification & Hardening** - Final integration testing, concurrency testing (simulating dual scans), and security checks.
   - Addresses: Concurrency guard validation, final user-acceptance testing.
   - Avoids: Concurrent write race conditions.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Target technologies are mature, standard, and highly supported in EF Core and SQL Server. |
| Features | HIGH | The spec details exact MVP rules, status mappings, and roles. |
| Architecture | HIGH | Standard MVC pattern fits perfectly; interceptors provide clean audit trail isolation. |
| Pitfalls | HIGH | Concurrency race conditions and keyboard wedge issues are well-documented and mitigated. |

## Gaps to Address

- The exact barcode prefix format used by physical scanners in P3 should be verified during phase 3 to align the regex check rules.
- Workstation name detection might return local gateway IPs (e.g. `::1` or load-balancer IPs) depending on hosting setup.
