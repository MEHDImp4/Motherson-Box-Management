# Phase 2: Box Lifecycle & Home Lookup - Context

**Gathered:** 2026-07-02
**Status:** Ready for planning

<domain>
## Phase Boundary

This phase delivers box creation (type, dimensions, expected package count), automatic unique box number and barcode generation, a dashboard showing all open boxes with progress indicators, a box detail view displaying properties/packages/progress/logs, and a homepage barcode search field that redirects to the preparation screen (open boxes) or read-only detail view (closed/completed/cancelled boxes).

Requirements covered: BOX-01, BOX-02, BOX-03, BOX-04, BOX-05, BOX-07, BOX-08, HOME-01, HOME-02, HOME-03, HOME-04. BOX-06 is deferred to Phase 3 because automatic completion depends on package scan execution.

</domain>

<decisions>
## Implementation Decisions

### Box Number & Barcode Generation
- **D-01:** Box numbers follow the format `BOX-YYYYMMDD-XXXXXX` where `XXXXXX` is a 6-character random hexadecimal string. This provides date grouping while avoiding sequential prediction.
- **D-02:** The box barcode value IS the box number — a single identifier used for both display and scanning. No separate barcode code.
- **D-03:** Box barcodes are distinguished from package barcodes by the `BOX` prefix inherent in the number format. No additional prefix scheme needed. Phase 3 (SCAN-06) will use a simple `StartsWith("BOX")` check.

### Dashboard Layout & Progress Display
- **D-04:** Dashboard displays open boxes in a standard Bootstrap `<table>` with columns: box number, type, progress bar (scanned/expected), last update timestamp, and last user.
- **D-05:** Dashboard auto-refreshes via JavaScript interval (periodic page or AJAX refresh). No SignalR/WebSocket for MVP.
- **D-06:** Default sorting is newest first (by creation date descending).
- **D-07:** No pagination — all open boxes shown on a single page. Factory floor expected to have <100 open boxes simultaneously.

### Box Creation Form & Validation
- **D-08:** All authenticated roles (Operator, Supervisor, Admin) can create boxes. Role-based restrictions apply only to exceptions and modifications (Phase 4).
- **D-09:** Box types are a hardcoded C# enum: `BoxType { Carton, Bois, Plastique }`. Type-safe and covers the 3 known types from requirements.
- **D-10:** Box dimensions (Height, Width, Depth) validated as positive integers only (> 0) via ViewModel validation. No min/max upper constraints — factory operators know their box sizes.
- **D-11:** Box creation lives on a dedicated page `/Box/Create` with a full form (type dropdown, 3 dimension fields, expected quantity input). Standard MVC form pattern.

### Homepage Scan & Redirect Behavior
- **D-12:** The barcode input field on the homepage auto-focuses on page load. Operator can scan immediately without clicking.
- **D-13:** Unknown/invalid barcode → inline red error message below the input field: "No box found with this barcode." Input stays focused for retry.
- **D-14:** Open box → redirects to preparation screen (package scanning page). Closed/Completed/Cancelled box → redirects to read-only detail view. Two distinct page targets.
- **D-15:** Package barcode typed in box search → inline red warning: "This is a package barcode, not a box barcode. Use the preparation screen to scan packages." Input stays focused.

### Agent's Discretion
- None — all decisions for Phase 2 have been explicitly aligned with the user.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project Framework & Goals
- `.planning/PROJECT.md` — Core value, constraints, tech stack (ASP.NET Core MVC, EF Core, SQL Server), and key decisions.
- `.planning/ROADMAP.md` — Phase 2 goal, success criteria, and requirements mapping.
- `.planning/REQUIREMENTS.md` — BOX-01 through BOX-08 and HOME-01 through HOME-04 requirement definitions.
- `GEMINI.md` — Coding conventions (PascalCase, thin controllers, service layer, async/await, ViewModel-only views), EF Core unique indexes, transaction requirements, and security rules.

### Prior Phase Context
- `.planning/phases/01-database-authentication/01-CONTEXT.md` — Phase 1 decisions on auth cookies (D-05, D-06), auto-migrations (D-04), seeded users (D-02), password hashing (D-01), and connection config (D-07, D-08).

### Technical Research References
- `.planning/research/STACK.md` — Recommended dependencies and library configurations.
- `.planning/research/ARCHITECTURE.md` — Folder layout structures and component boundaries.
- `.planning/research/PITFALLS.md` — Concurrency, double-scan races, and validation warnings.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Phase 1 will have created the ASP.NET Core MVC skeleton, EF Core DbContext, User entity, and Cookie Authentication middleware. Phase 2 builds on top of this foundation.
- Existing `Box` and `BoxPackages` entity definitions from Phase 1 migration (if already scaffolded) can be extended.

### Established Patterns
- Standard ASP.NET Core MVC 8.0 directory layout (`/Controllers`, `/Views`, `/Models`, `/Data`, `/Entities`, `/Services`).
- Cookie-based claims authentication using standard MVC routing.
- ViewModel pattern for all form data (entities never exposed directly to views).
- Service layer for business logic (controllers stay thin).

### Integration Points
- Box CRUD controllers integrate with the existing authentication middleware and role claims.
- Homepage controller adds a barcode lookup action that queries `BoxDbContext`.
- Dashboard controller queries open boxes from the same `BoxDbContext`.
- Box entity needs `RowVersion` for optimistic concurrency (per GEMINI.md).

</code_context>

<specifics>
## Specific Ideas
- Auto-refresh on the dashboard should use a configurable interval (e.g., 30 seconds) to keep the operator view current without manual reload.
- The box number format `BOX-YYYYMMDD-XXXXXX` with hex random part provides both human readability (date context) and collision resistance.

</specifics>

<deferred>
## Deferred Ideas
- None — discussion stayed within Phase 2 scope.

</deferred>

---

*Phase: 02-box-lifecycle-home-lookup*
*Context gathered: 2026-07-02*
