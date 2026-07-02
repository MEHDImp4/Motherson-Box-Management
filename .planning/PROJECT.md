# Motherson Box Management

## What This Is

Motherson Box Management is a web application designed for packaging operators, supervisors, and administrators in the P3 zone to prepare, track, and guarantee the traceability of packaging boxes. The application uses barcodes to record and control cable packages added to each box in real-time, functioning autonomously without connection to external ERP or MES systems for its MVP version.

## Core Value

Ensure absolute traceability of packaging boxes and guarantee that no cable package is ever scanned or assigned to more than one box.

## Requirements

### Validated

- [Phase 1] Connect with a unique ID (matricule) and password for roles: Operator, Supervisor, and Administrator.
- [Phase 2] Create boxes specifying their type (Carton, Bois, Plastique), dimensions (Height, Width, Depth), and expected quantity of packages.
- [Phase 2] Generate unique box numbers and barcode values distinguishable from package formats.
- [Phase 2] Direct access to a box's details and preparation screen by scanning its barcode from the homepage.

### Active

- [ ] Scan cable package barcodes via a USB scanner (simulating keyboard input) to associate them with the open box, performing real-time uniqueness and quantity limits checks.
- [ ] Automatically close a box (set status to Completed) when the expected package count is reached.
- [ ] Allow supervisors and administrators to perform exceptional operations: cancel a box, close with an exception (Completed with deviation), block/unblock a box or package, and transfer packages between boxes.
- [ ] Retain an immutable audit trail of all sensitive operations (creation, scan, exception, transfer, blocking, etc.) tracking previous/new values, reasons, and workstation names.
- [ ] Provide a dedicated virtual barcode scanner simulator in the UI to facilitate testing without physical hardware.

### Out of Scope

- [ ] Automated industrial label printing — Deferred to future milestone, pending hardware specifications validation.
- [ ] Detailed Excel reports exports — Deferred to future milestone, not needed for the core MVP traceability.
- [ ] ERP / MES / External database synchronization — Excluded from MVP to maintain a simple, autonomous system.

## Context

The packaging operators in the P3 zone physical layout need a simple and reliable tool. There is currently no external database system tracking the detailed cables structure inside boxes, so the MVP will manage package scans independently of external referentials. Concurrency control is critical since multiple operators might scan packages simultaneously; the database design must prevent double-allocations using transaction boundaries and SQL unique constraints.

## Constraints

- **Tech Stack**: ASP.NET Core MVC, Entity Framework Core, SQL Server — Mandated by technical target specification.
- **UI Design**: Standard Bootstrap layout typical of corporate intranet portals — Selected by the user for corporate environment alignment.
- **Database Architecture**: Single table for Boxes and single table for BoxPackages, avoiding dynamic table creation per box to ensure ease of audit and search.
- **Traceability**: An immutable database journal (BoxAuditLogs) must be written for all operations, with no edit or delete access from the UI.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Standard Bootstrap Layout | User requested standard corporate layout typical of internal portals. | — Pending |
| Virtual Scanner Simulator | User selected virtual scanner panel to ease development and testing of barcode scanner workflows. | — Pending |
| Seed Test Users | Pre-seed Operator, Supervisor, and Admin accounts in database initialization for verification. | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd:complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-07-02 after initialization*
