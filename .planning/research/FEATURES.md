# Feature Landscape

**Domain:** Barcode Packaging Box Management Web App
**Researched:** 2026-07-02

## Table Stakes

Features users expect. Missing = product feels incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Matricule Login | Opérateurs must sign in to associate their actions to the audit trail. | Low | Custom cookie authentication using employee ID (matricule) and password. |
| Box Creation | Needed to initiate the packaging process and establish expected quantities. | Low | Must prompt for type (Carton, Bois, Plastique), dimensions, and expected quantity. |
| Barcode scanning input (USB Keyboard Wedge) | Primary input mode on the packaging workstation. | Medium | Javascript listener captures rapid keypress events and submits via Fetch API. |
| Auto-Clôture (Auto-Completion) | Ensures operators don't have to manually click to finalize a box once it's full. | Low | Triggers when scanned count equals expected count. |
| Box Search & Direct Barcode Scan | Rapid lookups on homepage via USB scanner. | Low | Single barcode search field routing directly to the box detail/preparation view. |
| Virtual Barcode Scanner Simulator | Crucial for testing and development of wedge workflows without physical scanners. | Low | A simple, toggleable sidebar panel in UI to send mock scanned values. |

## Differentiators

Features that set product apart. Not expected, but valued.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Completed with Exception (Forced Completion) | Handles real-world exceptions where a box must ship under-filled. | Medium | Superviseur role required; prompts for mandatory reason; registers deviation. |
| Package Transfer, Retrait, and Disassociation | Corrects operator scanning errors without having to delete the entire box. | Medium | Recalculates remaining counts in a single database transaction. |
| Box Block/Quarantine | Prevents scanning or modification of boxes flagged with defects. | Medium | Puts box in read-only quarantine state until explicitly unblocked. |
| Structured Audit Trail | Essential for compliance. Track previous vs. new values for corrections. | High | EF Core interceptor automatically serializes old/new values to database on change. |

## Anti-Features

Features to explicitly NOT build.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| External ERP/MES Connection | Out of scope for MVP; increases deployment complexity. | Run as an autonomous standalone system in SQL Server. |
| Automated industrial label printing | Material/driver dependencies. | Generate standard browser-printable pages with HTML/CSS barcodes. |
| Detailed Excel exports | Simple audit search screen is sufficient for MVP. | Rely on the audit log UI screen with status/date filters. |

## Feature Dependencies

```
Matricule Login & Role Setup → Box Creation
Box Creation (Barcode Value) → Package Scanning
Package Scanning (Unique Constraint) → Package Transfer & Retrait
```

## MVP Recommendation

Prioritize:
1. **User Authentication & Role Mapping** (Matricule logins for Opérateur, Superviseur, Admin).
2. **Box Creation & Direct Scan lookup** (Carton/Bois/Plastique with unique number/barcode).
3. **Wedge Barcode Scan listener & Concurrency guard** (Fetch API scans protected by SQL unique constraint on PackageBarcode).
4. **Audit Logs & Interceptor** (Immutable table tracking events, values, and reasons).
5. **Virtual Scanner Simulator** (Bootstrap panel to test keyboard wedge behavior).

Defer:
- Excel exports: Defer, search filters on audit screen are enough for MVP verification.
- Hardware printer integration: Defer, print via standard browser print commands.

## Sources

- Motherson Box Management Functional Specification (Cahier des charges v1.3).
