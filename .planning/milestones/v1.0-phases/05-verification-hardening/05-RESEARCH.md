# Phase 5 Research: Verification & Hardening

## Concurrency & Scanning Architecture

### 1. Concurrency Control in `ScanPackageAsync`
The system manages concurrent package scans and changes to box states using a combination of **database-level transactions**, **EF Core optimistic concurrency control**, and a **retry loop pattern**.

- **Optimistic Concurrency with `RowVersion`:**
  - The `Box` entity contains a `RowVersion` byte array (`byte[]`), which is configured in `ApplicationDbContext.cs` via:
    ```csharp
    builder.Entity<Box>().Property(b => b.RowVersion).IsRowVersion();
    ```
  - In a SQL Server relational database, the `RowVersion` column is mapped to a `rowversion` database type. The database automatically generates and increments this value whenever the row is updated.
  - When EF Core reads a `Box` record, it loads the current `RowVersion`. During `SaveChangesAsync()`, EF Core checks if the database row still has the same `RowVersion` value. If another user modified the row in the meantime, the database update fails to affect any rows, and EF Core throws a `DbUpdateConcurrencyException`.

- **The Concurrency Retry Loop:**
  - In [BoxService.cs](file:///C:/Users/mehdi/Documents/PFA%20MotherSon/Motherson_Box_Management/MothersonBoxManagement/Services/BoxService.cs#L107-L194) the scan operation is wrapped in a loop that attempts the transaction up to 3 times.
  - If a `DbUpdateConcurrencyException` occurs, the catch block intercepts it:
    ```csharp
    catch (DbUpdateConcurrencyException)
    {
        _context.ChangeTracker.Entries().ToList().ForEach(e => e.Reload());
    }
    ```
  - `Reload()` refreshes the entity's property values from the database, updating the `RowVersion` and quantities in memory. The loop then continues to the next attempt, retrying the scan with the updated state.

- **Uniqueness Constraint and Duplicate Checks:**
  - To prevent a package barcode from being assigned to more than one box, the `BoxPackage` entity has a unique index constraint configured on `PackageBarcode`:
    ```csharp
    builder.Entity<BoxPackage>().HasIndex(bp => bp.PackageBarcode).IsUnique();
    ```
  - If two threads scan the same package barcode concurrently, they might both pass the initial `AnyAsync(bp => bp.PackageBarcode == barcode)` check. However, when writing to the database, the transaction that commits second will fail the unique constraint.
  - In a relational database, this throws a `DbUpdateException` wrapping a unique index violation. The service catches this and rolls back:
    ```csharp
    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_BoxPackages_PackageBarcode") == true)
    {
        if (transaction is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        return new ScanResult { Success = false, Message = "Ce code-barres paquet a déjà été scanné." };
    }
    ```

### 2. Simulating Concurrency in Tests
- **EF Core InMemory Database Limitations:**
  - The default test suite uses `UseInMemoryDatabase("TestDb")`. The InMemory provider does **not** enforce unique constraints nor support database-level concurrency tokens. This means a default multi-threaded test running on the InMemory DB will not trigger `DbUpdateConcurrencyException` or unique constraint violations naturally.
- **Testing Mitigation Strategy:**
  - To test the retry loop and constraint handling without a full SQL Server instance, we will use **EF Core SaveChanges Interceptors** in our tests:
    - **`ConcurrencySimulatingInterceptor`:** Intercepts `SavingChangesAsync` and throws a `DbUpdateConcurrencyException` a specified number of times to verify that the retry loop reloads and succeeds.
    - **`UniqueConstraintSimulatingInterceptor`:** Intercepts saves to track unique barcodes and throws a mock `DbUpdateException` containing the `"IX_BoxPackages_PackageBarcode"` string if a duplicate barcode write is attempted.

---

## Global Exception Handler & Security Hardening

### 1. HTTP Request Pipeline Configuration
The HTTP request pipeline in [Program.cs](file:///C:/Users/mehdi/Documents/PFA%20MotherSon/Motherson_Box_Management/MothersonBoxManagement/Program.cs) is configured to handle routing, static files, security middleware, and authentication/authorization.

- **Global Exception Handling:**
  - The global exception handling middleware is registered in `Program.cs` on lines 42-45:
    ```csharp
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
    }
    ```
  - When an unhandled exception occurs in a non-Development environment, the `ExceptionHandlerMiddleware` intercepts it, logs the full exception on the server, and internally redirects the request to the `/Home/Error` action.

- **Suppressing Internal Details:**
  - The `/Home/Error` endpoint points to `HomeController.Error` in [HomeController.cs](file:///C:/Users/mehdi/Documents/PFA%20MotherSon/Motherson_Box_Management/MothersonBoxManagement/Controllers/HomeController.cs#L91-L105).
  - This action returns an `ErrorViewModel` containing only the `RequestId` and `StatusCode`. It does **not** capture or expose the exception details, stack traces, database schemas, or connection strings.
  - The user is rendered a clean French UI view ([Error.cshtml](file:///C:/Users/mehdi/Documents/PFA%20MotherSon/Motherson_Box_Management/MothersonBoxManagement/Views/Shared/Error.cshtml)) showing a generic, user-friendly error message:
    > "Une erreur inattendue s'est produite lors du traitement de votre demande."
  - This completely suppresses SQL errors, stack traces, and database connection details from reaching the client, mitigating information leakage security risks.

---

## Test Architecture & Verification Plans

To thoroughly test concurrency, authorization bypasses, and the complete E2E lifecycle, we will introduce a new set of integration tests using `CustomWebApplicationFactory`.

### 1. Concurrent Scan Test
- **File:** `MothersonBoxManagement.Tests/ConcurrencyTests.cs`
- **Objective:** Verify that concurrent scans of the same package or concurrent modifications on the same box are resolved correctly.
- **Scenarios:**
  - **`ScanPackage_RetryOnConcurrencyConflict_Succeeds`:** Register `ConcurrencySimulatingInterceptor` to fail the first save with `DbUpdateConcurrencyException`. Assert that `ScanPackageAsync` successfully retries, reloads, and saves the package.
  - **`ScanPackage_DuplicateBarcodeConcurrent_ExactlyOneSucceeds`:** Fire multiple parallel calls to `/Box/ScanAjax` or `ScanPackageAsync` using `Task.WhenAll`. Register `UniqueConstraintSimulatingInterceptor` to throw unique constraint violations on duplicate barcodes. Assert that exactly one task succeeds (returns success `true`) and the remaining tasks fail (return duplicate package message).

### 2. Security Audit Tests
- **File:** `MothersonBoxManagement.Tests/SecurityAuditTests.cs`
- **Objective:** Verify that roles are restricted correctly and that input validation rejects malformed values.
- **Scenarios:**
  - **`Operator_CannotAccess_SupervisorEndpoints`:** Using `TestAuthHelper.CreateAuthenticatedClient`, authenticate as `OP001` (Operator). Post requests to supervisor actions:
    - `/Box/CancelBox`
    - `/Box/ForceCloseBox`
    - `/Box/ModifyExpectedQuantity`
    - `/Box/BlockBox` / `/Box/UnblockBox`
    - `/Box/BlockPackage` / `/Box/UnblockPackage`
    - `/Box/TransferPackage`
    - `/Box/RetraitPackage`
    - Assert that all requests result in `HttpStatusCode.Redirect` redirecting to the login page (configured as `AccessDeniedPath`).
  - **`Supervisor_CanAccess_SupervisorEndpoints`:** Authenticate as `SP001` (Supervisor). Post to the same endpoints and verify they succeed or redirect to details with success.
  - **`InputValidation_RejectsInvalidBoxDimensions`:** Post a box creation request to `/Box/Create` with negative/zero dimensions (e.g., `Height = -5`, `Width = 0`). Verify that the controller returns the View with model state errors, blocking database write.
  - **`InputValidation_RejectsShortBarcode`:** Post a scan request with a package barcode shorter than 3 characters (e.g., `"12"`). Assert it redirects with the validation error: *"Le code-barres doit contenir au moins 3 caractères."*

### 3. E2E Lifecycle Integration Test
- **File:** `MothersonBoxManagement.Tests/E2ELifecycleTests.cs`
- **Objective:** Verify a complete box lifecycle workflow, ensuring it completes successfully and leaves a robust, unalterable trail in `BoxAuditLogs`.
- **Steps:**
  1. **Authentication:** Authenticate `SP001` (Supervisor) and `OP001` (Operator) clients.
  2. **Creation:** Operator creates a box `BOX-E2E-TEST` (ExpectedQuantity = 3, Carton).
  3. **Scanning:** Operator scans packages `PKG-E2E-001` and `PKG-E2E-002` successfully.
  4. **Exception - Block:** Supervisor blocks the box with reason `"Contrôle qualité en cours"`.
  5. **Block Validation:** Operator attempts to scan `PKG-E2E-003` to the blocked box. Verify the scan is rejected with the message containing `"n'est pas ouverte aux scans"` or similar.
  6. **Exception - Unblock:** Supervisor unblocks the box with reason `"Libération après inspection"`.
  7. **Exception - Transfer:** Supervisor transfers `PKG-E2E-002` to another open box.
  8. **Exception - Force Close:** Supervisor force-closes the box with reason `"Fin de poste - Clôture anticipée"`.
  9. **Final Verification:**
     - Assert that the box status is updated to `CompletedWithException` and `ExceptionReason` is `"Fin de poste - Clôture anticipée"`.
     - Fetch the entries from `BoxAuditLogs` for this box. Assert that all operations are recorded in order (Insert, PackageScan, Update) with matching user IDs, correct `ActionType` values, workstation name, and detailed JSON diffs containing the supervisor reasons.

### 4. Verification Commands
Run the verification suite locally:
```powershell
dotnet restore
dotnet build
dotnet test
```
