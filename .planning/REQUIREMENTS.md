# Requirements: Motherson Box Management

**Defined:** 2026-07-02
**Core Value:** Ensure absolute traceability of packaging boxes and guarantee that no cable package is ever scanned or assigned to more than one box.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Authentication & Accounts

- [ ] **AUTH-01**: User can log in with a unique matricule and password.
- [ ] **AUTH-02**: System must reject invalid login credentials with a generic message to prevent username brute-forcing.
- [ ] **AUTH-03**: Role-based access mapping must be enforced for Opérateur, Superviseur, and Admin roles.

### Box Lifecycle

- [ ] **BOX-01**: User can create a new box by selecting Carton, Bois, or Plastique type.
- [ ] **BOX-02**: User must enter box dimensions (Height, Width, Depth) as positive values in cm.
- [ ] **BOX-03**: User must define the expected packages quantity as an integer strictly greater than zero.
- [ ] **BOX-04**: System must generate a unique box number (e.g., BOX-YYYYMMDD-XXXXXX) and unique barcode value.
- [ ] **BOX-05**: Box creation registers the creator's matricule and timestamp.
- [ ] **BOX-06**: Box status automatically transitions to Completed when the scanned quantity equals the expected quantity.
- [ ] **BOX-07**: Box detail screen displays properties, associated packages, progress statistics, and historical logs.
- [ ] **BOX-08**: Dashboard displays all open boxes with progress bars, quantities, last update timestamp, and last user.

### Barcode Scan

- [ ] **SCAN-01**: System processes scanned package codes from USB keyboard wedge readers (emulating keys followed by Enter).
- [ ] **SCAN-02**: System associates scanned package barcode with the selected open box.
- [ ] **SCAN-03**: System blocks duplicate scans of the same package barcode globally (via SQL unique constraint index).
- [ ] **SCAN-04**: System rejects scans when expected quantity is already met or the box is not in "Open" state.
- [ ] **SCAN-05**: Scan interface provides immediate, clear success and error messages (e.g., duplicate package, invalid format, box blocked).
- [ ] **SCAN-06**: System distinguishes box barcodes from package barcodes (using prefix check rules) and blocks box codes in the package scan view.

### Homepage Direct Scan

- [ ] **HOME-01**: Homepage features a dedicated box barcode input field.
- [ ] **HOME-02**: Scan of a valid open box barcode from homepage directly opens the package scanning screen.
- [ ] **HOME-03**: Scan of a closed, cancelled, or archived box barcode opens its detail screen in read-only mode.
- [ ] **HOME-04**: Input of package barcodes in box search displays an explicit warning.

### Audit Trail & Logs

- [ ] **AUDIT-01**: Immutable database log table (BoxAuditLogs) captures all database writes.
- [ ] **AUDIT-02**: Log records type of action, timestamp, user matricule, and workstation IP / name.
- [ ] **AUDIT-03**: Modification logs store previous and new values in JSON format.
- [ ] **AUDIT-04**: Log entries are non-editable and non-deletable from the application UI.

### Exceptions & Transfers

- [ ] **EXC-01**: Expected quantity cannot be modified by Operator after the first scan; modifications are restricted to Supervisors/Admins with a mandatory reason.
- [ ] **EXC-02**: Supervisors/Admins can cancel a box or force-complete it (Completed with exception) with a mandatory justification.
- [ ] **EXC-03**: Supervisors/Admins can perform package retraits, transfers, or disassociations with a mandatory reason.
- [ ] **EXC-04**: Supervisors/Admins can quarantine (Block) a box or package to halt scans, and unblock it with a justification.

### Virtual Scanner Panel

- [ ] **SIM-01**: UI features a developer sidebar/panel allowing virtual input generation to simulate scanner keystrokes.

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
| AUTH-01 | Phase 1 | Pending |
| AUTH-02 | Phase 1 | Pending |
| AUTH-03 | Phase 1 | Pending |
| BOX-01 | Phase 2 | Pending |
| BOX-02 | Phase 2 | Pending |
| BOX-03 | Phase 2 | Pending |
| BOX-04 | Phase 2 | Pending |
| BOX-05 | Phase 2 | Pending |
| BOX-06 | Phase 2 | Pending |
| BOX-07 | Phase 2 | Pending |
| BOX-08 | Phase 2 | Pending |
| SCAN-01 | Phase 3 | Pending |
| SCAN-02 | Phase 3 | Pending |
| SCAN-03 | Phase 3 | Pending |
| SCAN-04 | Phase 3 | Pending |
| SCAN-05 | Phase 3 | Pending |
| SCAN-06 | Phase 3 | Pending |
| SIM-01 | Phase 3 | Pending |
| HOME-01 | Phase 2 | Pending |
| HOME-02 | Phase 2 | Pending |
| HOME-03 | Phase 2 | Pending |
| HOME-04 | Phase 2 | Pending |
| AUDIT-01 | Phase 4 | Pending |
| AUDIT-02 | Phase 4 | Pending |
| AUDIT-03 | Phase 4 | Pending |
| AUDIT-04 | Phase 4 | Pending |
| EXC-01 | Phase 4 | Pending |
| EXC-02 | Phase 4 | Pending |
| EXC-03 | Phase 4 | Pending |
| EXC-04 | Phase 4 | Pending |

**Coverage:**
- v1 requirements: 30 total
- Mapped to phases: 30
- Unmapped: 0 ✓

---
*Requirements defined: 2026-07-02*
*Last updated: 2026-07-02 after initial definition*
