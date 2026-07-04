# Roadmap: Motherson Box Management

## Overview

This roadmap lays out the path to build a standalone, secure web application for packaging box barcode tracking and auditing. We structure the project as a Vertical MVP slice-by-slice, starting with database and role-based authentication setups. Next, we build the box lifecycle management and dashboard, followed by barcode keyboard wedge scanning event listeners and a virtual test simulator. Finally, we implement supervisor exceptions (corrections, cancels, blocks, and transfers) with a robust EF Core audit trail interceptor.

## Phases

Phase Numbering:

- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Database & Authentication** - Project setup, EF Core migrations, and role-based Cookie Authentication. (completed 2026-07-02)
- [x] **Phase 2: Box Lifecycle & Home Lookup** - Box creation, dashboard, status tracking, and homepage search lookup. (completed 2026-07-02)
- [x] **Phase 3: Barcode Scan Integration & Simulator** - Keyboard wedge scanning listeners, uniqueness constraints, and a mock test panel. (completed 2026-07-03)
- [x] **Phase 4: Supervisor Exceptions & Audit Trail** - SaveChangesInterceptor audit trail logging and supervisor quarantine, block, cancellation, and transfer workflows. (completed 2026-07-04)
- [ ] **Phase 5: Verification & Hardening** - Integration verification, double-scan concurrency testing, and security hardening.

## Phase Details

### Phase 1: Database & Authentication

**Goal**: Set up the ASP.NET Core MVC skeleton, SQL Server schema migrations, and user authentication with matricule role-based access.
**Mode**: mvp
**Depends on**: Nothing (first phase)
**Requirements**: AUTH-01, AUTH-02, AUTH-03, BOX-05
**Success Criteria** (what must be TRUE):

  1. User can navigate to the application and view a login page.
  2. User can log in with a seeded matricule (Opérateur, Superviseur, Admin) and be routed to their respective homepage.
  3. User session persists across browser refresh.
  4. Invalid credentials display a generic login error message.

**Plans**: 1/1 plans complete
**UI hint**: yes

### Phase 2: Box Lifecycle & Home Lookup

**Goal**: As a Operator, I want to manage the box lifecycle by creating new packaging boxes and looking them up on the home dashboard, so that I can prepare them for scanning cable packages.
**Mode**: mvp
**Depends on**: Phase 1
**Requirements**: BOX-01, BOX-02, BOX-03, BOX-04, BOX-05, BOX-07, BOX-08, HOME-01, HOME-02, HOME-03, HOME-04
**Success Criteria** (what must be TRUE):

  1. Authorized user can create a box by inputting type, dimensions, and expected packages count.
  2. System generates a unique box number and barcode.
  3. User can view all open boxes with real-time status and progress indicators on the dashboard.
  4. User can scan/type a box barcode on the homepage and be immediately redirected to its details view (or preparation page if open).
  5. Scans of non-open boxes display them in read-only mode.

**Plans**: 4/4 plans complete
Plans:

- [ ] 01-PLAN.md

- [x] 02-01-PLAN.md — Workflow status gate and integer box creation schema.
- [x] 02-02-PLAN.md — Box identity, creator metadata, details, and prepare route.
- [x] 02-03-PLAN.md — Home dashboard table and barcode lookup redirects.
- [x] 02-04-PLAN.md — Regression coverage, documentation, and TODO completion.

**UI hint**: yes

### Phase 3: Barcode Scan Integration & Simulator

**Goal**: Implement the scanner listener on the preparation screen, execute package scanning with uniqueness validation, and provide a virtual barcode simulator panel for testing.
**Mode**: mvp
**Depends on**: Phase 2
**Requirements**: BOX-06, SCAN-01, SCAN-02, SCAN-03, SCAN-04, SCAN-05, SCAN-06, SIM-01
**Success Criteria** (what must be TRUE):

  1. Operator can scan package barcodes via a USB scanner (or virtual simulator panel) to associate them with the open box.
  2. Scan interface displays immediate feedback (green success banner or red warning description).
  3. Duplicate scans of a package code are blocked globally with an explicit warning.
  4. Scans of a box barcode in the package screen are blocked.
  5. Box status automatically transitions to "Completed" when expected quantity is met.

**Plans**: 3/3 plans complete
Plans:

- [x] 03-01-PLAN.md — Keyboard wedge barcode listener and scan screen
- [x] 03-02-PLAN.md — Uniqueness constraints, format checks, and status auto-complete
- [x] 03-03-PLAN.md — Simulator panel upgrade with AJAX/CSRF and integration tests

**UI hint**: yes

### Phase 4: Supervisor Exceptions & Audit Trail

**Goal**: Implement immutable audit logging for all database write actions and enable supervisor exceptions (forced closure, cancel, block, package transfers/retraits).
**Mode**: mvp
**Depends on**: Phase 3
**Requirements**: AUDIT-01, AUDIT-02, AUDIT-03, AUDIT-04, EXC-01, EXC-02, EXC-03, EXC-04
**Success Criteria** (what must be TRUE):

  1. Supervisor or Admin can modify the expected quantity of a box with a mandatory reason.
  2. Supervisor or Admin can cancel a box or force-close it with a deviation, typing a mandatory justification.
  3. Supervisor or Admin can transfer packages between open boxes or perform a retrait.
  4. Supervisor can quarantine (Block) a box or package to halt scans, and unblock it.
  5. Audit log view displays an unmodifiable list of actions, users, and previous/new values in JSON format.

**Plans**: 4/4 plans complete
Plans:

- [x] 04-01-PLAN.md — Database Updates & Audit Interceptor
- [x] 04-02-PLAN.md — Supervisor Exception Service Methods
- [x] 04-03-PLAN.md — UI Updates & Audit Log View
- [x] 04-04-PLAN.md — Gap closure: fix transfer dropdown role check + audit log ChangedProperties

**UI hint**: yes

### Phase 5: Verification & Hardening

**Goal**: Finalize system integration, perform concurrent scanning simulation testing, and execute final security audit checks.
**Depends on**: Phase 4
**Requirements**: None
**Success Criteria** (what must be TRUE):

  1. Verification of concurrent scans of the same barcode block one scan and succeed on the other.
  2. Security check verifies that error logs contain no sensitive internal database details.

**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Database & Auth Setup | 1/1 | Complete   | 2026-07-02 |
| 2. Box Lifecycle & Home Lookup | 4/4 | Complete   | 2026-07-02 |
| 3. Barcode Scan Integration | 3/3 | Complete   | 2026-07-03 |
| 4. Supervisor Exceptions | 3/3 | Complete   | 2026-07-04 |
| 5. Verification & Hardening | 0/1 | Not started | - |
