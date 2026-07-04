# UI Screen Inventory - Motherson Box Management

This inventory lists all discovered screens, routes, modals, states, and role-specific UI elements in the application.

## 1. Global Application Shell & States

### Layout File
* **File**: `Views/Shared/_Layout.cshtml`
* **Purpose**: Base layout providing HTML shell, fonts, styles, scripts, CSRF validation tokens, and navigation.
* **Redesign Direction**: Rebuild with a desktop-first responsive sidebar for workstations. Ensure role-adapted link visibility.

### Common Layout Elements
* **Motherson Logo**: `~/images/mothersson-logo.png`
* **Navigation Links**:
  * **Dashboard** (All roles): Link to `/Dashboard/Index`
  * **Search** (All roles): Link to `/Box/Index`
  * **New Box** (All roles): Link to `/Box/Create`
  * **Audit Log** (Supervisor/Admin): Link to `/Audit/Index`
  * **Users** (Admin): Link to `/Users/Index`
  * **User Information & Sign out**: Displays user's matricule and "Sign out" link (`_LoginPartial.cshtml`).

---

## 2. Page & Screen Inventory

### 2.1 Login Page
* **Route**: `/Account/Login` (GET, POST)
* **Controller Action**: `AccountController.Login`
* **View**: `Views/Account/Login.cshtml`
* **Roles**: Anonymous (unauthenticated users)
* **Form Inputs**:
  * `Matricule` (Employee ID) - Text input, autofocus
  * `Password` - Password input
* **Feedback States**:
  * Validation error for empty fields
  * Incorrect credentials error (alert banner)
* **Redesign Direction**: Structured, high-contrast container with visible inputs, logo, and a clear test account summary helper.

### 2.2 Home Dashboard
* **Route**: `/` or `/Dashboard/Index` (GET, POST)
* **Controller Action**: `DashboardController.Index`
* **View**: `Views/Dashboard/Index.cshtml`
* **Roles**: All authenticated roles (Operator, Supervisor, Administrator)
* **Features**:
  * **Scan Box Field**: Large, focused barcode field to scan/search box barcodes.
  * **Add Box Button**: Prominent primary button to create a new box.
  * **Active Boxes Table**: List of current open boxes showing number, type, progress (bar + text), last modified date, last operator matricule, and action links ("Prepare", "Details").
* **Feedback States**:
  * Error messages (e.g., box not found, invalid barcode)
  * Warning messages (e.g., package barcode scanned on box lookup field)
  * Empty state (if no open boxes exist)
* **Refresh Loop**: Reloads the dashboard dynamically if the barcode input is inactive and empty, ensuring live terminal visibility.

### 2.3 Box Search & Tracking List
* **Route**: `/Box/Index` (GET)
* **Controller Action**: `BoxController.Index`
* **View**: `Views/Box/Index.cshtml`
* **Roles**: All authenticated roles
* **Form Inputs (Filters)**:
  * Box number/Barcode search
  * Status (All, Open, Completed, CompletedWithException, Cancelled, Blocked, Archived)
  * Created By (Matricule dropdown)
  * From Date
  * To Date
* **Features**:
  * Search results table listing box information and progress.
  * "+ New Box" primary action.
* **Feedback States**:
  * Empty state for no filter matches.

### 2.4 Create Box
* **Route**: `/Box/Create` (GET, POST)
* **Controller Action**: `BoxController.Create`
* **View**: `Views/Box/Create.cshtml`
* **Roles**: All authenticated roles
* **Form Inputs**:
  * Box Type (Cardboard, Wood, Plastic dropdown)
  * Height, Width, Depth (strictly positive whole integers in cm)
  * Expected Quantity (strictly positive integer)
* **Redesign Direction**: Card-based form with clear visual selectors for box types.

### 2.5 Box Preparation & Scanning Screen
* **Route**: `/Box/Prepare` (redirected via lookup, GET)
* **Controller Action**: `BoxController.Prepare`
* **View**: `Views/Box/Prepare.cshtml`
* **Roles**: All authenticated roles
* **Features**:
  * Box Metadata panel (Number, Barcode, Type, Dimensions, Creator)
  * Large progress indicator (scanned vs expected packages)
  * **Package Scan Input**: Main interactive text field to scan a package barcode.
  * **Scan Simulator**: Interactive test panel (`_SimulatorPanel.cshtml`) to generate and inject dummy package scans via AJAX.
  * **Recent Scans Table**: Live list of scanned packages inside this box.
* **AJAX Endpoint**: `/Box/ScanAjax` (POST)
  * Intercepted by `scanner.js`
  * Emits Web Audio API sound confirmation (high beep for success, low beep for error)
  * Refreshes the recent scans table and progress bar dynamically without page reload
* **Feedback States**:
  * Card body flashes green for successful scans
  * Card body flashes red for validation errors (e.g., duplicate packages, format issues)
  * Alerts for errors and successes.

### 2.6 Box Details (Open, Completed, Blocked, Cancelled, Archived)
* **Route**: `/Box/Details` (GET)
* **Controller Action**: `BoxController.Details`
* **View**: `Views/Box/Details.cshtml`
* **Roles**: All authenticated roles (with Supervisor/Admin action permissions)
* **Features**:
  * Detailed box status banner (color-coded, accessible textual representation)
  * Box metrics (scanned, expected, remaining, dimensions, timestamps)
  * Scanned packages table
  * Link to full audit log for this specific box
* **Supervisor/Admin Action Modals**:
  * **Edit Qty**: Modify expected packages count (reason required)
  * **Cancel**: Permanently cancel the box (reason required)
  * **Force close**: Complete box with exceptions (reason required)
  * **Block**: Temporarily quarantine the box (reason required)
  * **Unblock**: Remove quarantine (reason required)
  * **Archive**: Move completed box to archives (reason required)
  * **Transfer Package**: Move package to another open box (reason required)
  * **Remove Package (Retrait)**: Unlink a package (reason required)
  * **Block Package**: Quarantine a single package (reason required)
  * **Unblock Package**: Un-quarantine a single package (reason required)
  * **Disassociate Package**: Release package from a cancelled box (reason required)

### 2.7 User Management
* **Route**: `/Users/Index` (GET)
* **Controller Action**: `UsersController.Index`
* **View**: `Views/Users/Index.cshtml`
* **Roles**: Administrator only
* **Sub-Screens**:
  * **Create User**: `/Users/Create` (GET, POST)
  * **Edit User**: `/Users/Edit/{id}` (GET, POST) - Edit details, role, and active/inactive status
  * **Reset Password**: `/Users/ResetPassword/{id}` (GET, POST) - Change password
  * **Deactivate User**: Form POST inside user index row

### 2.8 Audit Log Tracking
* **Route**: `/Audit/Index` (GET)
* **Controller Action**: `AuditController.Index`
* **View**: `Views/Audit/Index.cshtml`
* **Roles**: Supervisors and Administrators
* **Form Filters**:
  * Box ID
  * Action Type dropdown
  * From Date
  * To Date
* **Features**:
  * Full, paginated audit list.
  * Action category badge.
  * User, Workstation, and Box references.
  * **Details JSON Toggle**: Expandable `<details>` toggle formatting full changes JSON (using custom format utility).

### 2.9 Error, Forbidden, and Not Found States
* **Route**: `/Dashboard/Error` or status-code driven
* **Controller Action**: `DashboardController.Error`
* **View**: `Views/Shared/Error.cshtml`
* **Roles**: All
* **States**:
  * 404 Page Not Found (clear header, path suggestions, back link)
  * 500 Unexpected Application Error (Request ID displayed, safe logging)
  * Forbidden (handled by standard Cookie Auth redirects)

---

## 3. Keyboard / Wedge Barcode Scanner Integration

### JavaScript wedge scanner listener
* **File**: `wwwroot/js/scanner.js`
* **Purpose**: Listens to global document keydown events. It collects characters entered in rapid succession (wedge scanner input with <50ms delay) and sends the buffer as an AJAX request to `/Box/ScanAjax` when `Enter` is detected.
* **Visual / Auditory Feedback**:
  * Sound indicators (800Hz / 150ms success beep, 300Hz / 300ms error beep).
  * Page flash indicators (`.scan-flash-success` / `.scan-flash-error`).
  * Live table prepends and progress updates.
