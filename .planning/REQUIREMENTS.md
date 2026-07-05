# Requirements: Motherson Box Management

**Defined:** 2026-07-02
**Core Value:** Ensure absolute traceability of packaging boxes and guarantee that no cable package is ever scanned or assigned to more than one box.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Authentication & Accounts

- [x] **AUTH-01**: User can log in with a unique matricule and password.
- [x] **AUTH-02**: System must reject invalid login credentials with a generic message to prevent username brute-forcing.
- [x] **AUTH-03**: Role-based access mapping must be enforced for Operator, Supervisor, and Admin roles.

### Box Lifecycle

- [x] **BOX-01**: User can create a new box by selecting Carton, Bois, or Plastique type.
- [x] **BOX-02**: User must enter box dimensions (Height, Width, Depth) as positive values in cm.
- [x] **BOX-03**: User must define the expected packages quantity as an integer strictly greater than zero.
- [x] **BOX-04**: System must generate a unique box number (e.g., BOX-YYYYMMDD-XXXXXX) and unique barcode value.
- [x] **BOX-05**: Box creation registers the creator's matricule and timestamp.
- [x] **BOX-06**: Box status automatically transitions to Completed when the scanned quantity equals the expected quantity.
- [x] **BOX-07**: Box detail screen displays properties, associated packages, progress statistics, and historical logs.
- [x] **BOX-08**: Dashboard displays all open boxes with progress bars, quantities, last update timestamp, and last user.

### Barcode Scan

- [x] **SCAN-01**: System processes scanned package codes from USB keyboard wedge readers (emulating keys followed by Enter).
- [x] **SCAN-02**: System associates scanned package barcode with the selected open box.
- [x] **SCAN-03**: System blocks duplicate scans of the same package barcode globally (via SQL unique constraint index).
- [x] **SCAN-04**: System rejects scans when expected quantity is already met or the box is not in "Open" state.
- [x] **SCAN-05**: Scan interface provides immediate, clear success and error messages (e.g., duplicate package, invalid format, box blocked).
- [x] **SCAN-06**: System distinguishes box barcodes from package barcodes (using prefix check rules) and blocks box codes in the package scan view.

### Homepage Direct Scan

- [x] **HOME-01**: Homepage features a dedicated box barcode input field.
- [x] **HOME-02**: Scan of a valid open box barcode from homepage directly opens the package scanning screen.
- [x] **HOME-03**: Scan of a closed, cancelled, or archived box barcode opens its detail screen in read-only mode.
- [x] **HOME-04**: Input of package barcodes in box search displays an explicit warning.

### Audit Trail & Logs

- [x] **AUDIT-01**: Immutable database log table (BoxAuditLogs) captures all database writes.
- [x] **AUDIT-02**: Log records type of action, timestamp, user matricule, and workstation IP / name.
- [x] **AUDIT-03**: Modification logs store previous and new values in JSON format.
- [x] **AUDIT-04**: Log entries are non-editable and non-deletable from the application UI.

### Exceptions & Transfers

- [x] **EXC-01**: Expected quantity cannot be modified by Operator after the first scan; modifications are restricted to Supervisors/Admins with a mandatory reason.
- [x] **EXC-02**: Supervisors/Admins can cancel a box or force-complete it (Completed with exception) with a mandatory justification.
- [x] **EXC-03**: Supervisors/Admins can perform package retraits, transfers, or disassociations with a mandatory reason.
- [x] **EXC-04**: Supervisors/Admins can quarantine (Block) a box or package to halt scans, and unblock it with a justification.

### Virtual Scanner Panel

- [x] **SIM-01**: UI features a developer sidebar/panel allowing virtual input generation to simulate scanner keystrokes.

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Printing & Exports

- **PRNT-01**: Direct integration with industrial thermal label printers for barcode labels.
- **EXPT-01**: Export audit logs and dashboard lists to downloadable Excel sheets.

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| ERP / MES Synchronization | Out of scope for Standalone MVP to simplify local workstation deployment. |
| Automated physical print drivers | Physical material variations defer this driver dependency. |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| AUTH-01 | Phase 1 | Complete |
| AUTH-02 | Phase 1 | Complete |
| AUTH-03 | Phase 1 | Complete |
| BOX-01 | Phase 2 | Complete |
| BOX-02 | Phase 2 | Complete |
| BOX-03 | Phase 2 | Complete |
| BOX-04 | Phase 2 | Complete |
| BOX-05 | Phase 2 | Complete |
| BOX-06 | Phase 3 | Complete |
| BOX-07 | Phase 2 | Complete |
| BOX-08 | Phase 2 | Complete |
| SCAN-01 | Phase 3 | Complete |
| SCAN-02 | Phase 3 | Complete |
| SCAN-03 | Phase 3 | Complete |
| SCAN-04 | Phase 3 | Complete |
| SCAN-05 | Phase 3 | Complete |
| SCAN-06 | Phase 3 | Complete |
| SIM-01 | Phase 3 | Complete |
| HOME-01 | Phase 2 | Complete |
| HOME-02 | Phase 2 | Complete |
| HOME-03 | Phase 2 | Complete |
| HOME-04 | Phase 2 | Complete |
| AUDIT-01 | Phase 4 | Complete |
| AUDIT-02 | Phase 4 | Complete |
| AUDIT-03 | Phase 4 | Complete |
| AUDIT-04 | Phase 4 | Complete |
| EXC-01 | Phase 4 | Complete |
| EXC-02 | Phase 4 | Complete |
| EXC-03 | Phase 4 | Complete |
| EXC-04 | Phase 4 | Complete |

**Coverage:**

- v1 requirements: 30 total
- Mapped to phases: 30
- Unmapped: 0 ✓

---
*Requirements defined: 2026-07-02*
*Last updated: 2026-07-05 after V1 release readiness review*
