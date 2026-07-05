# Feature Landscape

**Domain:** Barcode Packaging Box Management Web App
**Researched:** 2026-07-02

## Table Stakes

Features users expect. Missing them makes the product feel incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Matricule Login | Operators must sign in so their actions can be linked to the audit trail. | Low | Custom cookie authentication using employee ID (matricule) and password. |
| Box Creation | Needed to start the packaging process and define expected quantities. | Low | Must prompt for type (Carton, Bois, Plastique), dimensions, and expected quantity. |
| Barcode Scanning Input (USB Keyboard Wedge) | Primary input mode on the packaging workstation. | Medium | JavaScript listener captures rapid keypress events and submits them through the Fetch API. |
| Auto-Completion | Ensures operators do not need to manually finalize a box once it is full. | Low | Triggers when the scanned count matches the expected count. |
| Box Search & Direct Barcode Scan | Enables fast lookups from the home page via USB scanner. | Low | Single barcode search field routes directly to the box detail or preparation view. |
| Virtual Barcode Scanner Simulator | Important for testing and development of wedge workflows without physical scanners. | Low | Simple toggleable sidebar panel in the UI to send mock scanned values. |

## Differentiators

Features that set the product apart. Not always expected, but valuable.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Completed with Exception (Forced Completion) | Handles real-world cases where a box must ship under-filled. | Medium | Supervisor role required; asks for a mandatory reason and records the deviation. |
| Package Transfer, Removal, and Disassociation | Corrects operator scanning mistakes without deleting the entire box. | Medium | Recalculates remaining counts in a single database transaction. |
| Box Block/Quarantine | Prevents scanning or modification of boxes flagged with defects. | Medium | Puts the box in a read-only quarantine state until it is explicitly unblocked. |
| Structured Audit Trail | Essential for compliance. Tracks previous and new values for corrections. | High | EF Core interceptor automatically serializes old/new values to the database on change. |

## Anti-Features

Features we explicitly do NOT want to build.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| External ERP/MES Connection | Out of scope for the MVP and increases deployment complexity. | Run as a standalone autonomous system on SQL Server. |
| Automated industrial label printing | Adds material and driver dependencies. | Generate standard browser-printable pages with HTML/CSS barcodes. |
| Detailed Excel exports | A simple audit search screen is enough for the MVP. | Rely on the audit log UI with status/date filters. |

## Feature Dependencies

```text
Matricule Login & Role Setup -> Box Creation
Box Creation (Barcode Value) -> Package Scanning
Package Scanning (Unique Constraint) -> Package Transfer & Removal
```

## MVP Recommendation

Prioritize:
1. **User Authentication & Role Mapping** (matricule logins for Operator, Supervisor, Admin).
2. **Box Creation & Direct Scan Lookup** (Carton/Bois/Plastique with unique number/barcode).
3. **Wedge Barcode Scan Listener & Concurrency Guard** (Fetch API scans protected by the SQL unique constraint on `PackageBarcode`).
4. **Audit Logs & Interceptor** (immutable table tracking events, values, and reasons).
5. **Virtual Scanner Simulator** (Bootstrap panel to test keyboard wedge behavior).

Defer:
- Excel exports: defer, because audit screen search filters are enough for MVP verification.
- Hardware printer integration: defer, because printing through standard browser print commands is enough for now.

## Sources

- Motherson Box Management Functional Specification (v1.3).
