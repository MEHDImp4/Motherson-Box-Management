---
phase: 1
slug: database-authentication
wave: 1
depends_on: []
requirements: [AUTH-01, AUTH-02, AUTH-03, BOX-05]
files_modified:
  # Project scaffold
  - MothersonBoxManagement/MothersonBoxManagement.csproj
  - MothersonBoxManagement/Program.cs
  - MothersonBoxManagement/appsettings.json
  - MothersonBoxManagement/appsettings.Development.json
  # Entities
  - MothersonBoxManagement/Entities/User.cs
  - MothersonBoxManagement/Entities/Box.cs
  - MothersonBoxManagement/Entities/BoxPackage.cs
  - MothersonBoxManagement/Entities/BoxAuditLog.cs
  - MothersonBoxManagement/Entities/BoxType.cs
  - MothersonBoxManagement/Entities/BoxStatus.cs
  # Data layer
  - MothersonBoxManagement/Data/ApplicationDbContext.cs
  - MothersonBoxManagement/Data/DbInitializer.cs
  - MothersonBoxManagement/Migrations/ (generated)
  # Services
  - MothersonBoxManagement/Services/IAuthenticationService.cs
  - MothersonBoxManagement/Services/AuthenticationService.cs
  # ViewModels
  - MothersonBoxManagement/ViewModels/LoginViewModel.cs
  # Controllers
  - MothersonBoxManagement/Controllers/AccountController.cs
  - MothersonBoxManagement/Controllers/HomeController.cs
  # Views
  - MothersonBoxManagement/Views/Account/Login.cshtml
  - MothersonBoxManagement/Views/Home/Index.cshtml
  - MothersonBoxManagement/Views/Shared/_Layout.cshtml
  - MothersonBoxManagement/Views/Shared/_LoginPartial.cshtml
  # Static
  - MothersonBoxManagement/wwwroot/css/site.css
  # Tests
  - MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj
  - MothersonBoxManagement.Tests/CustomWebApplicationFactory.cs
  - MothersonBoxManagement.Tests/AccountControllerTests.cs
autonomous: true
---

# Phase 1: Database & Authentication — Plan

**As a** packaging zone user, **I want to** log in with my matricule and password, **so that** I can access the application with my role-based permissions.

## Tasks

<task id="01-01-01">
<objective>Create the ASP.NET Core MVC project with all entities, EF Core DbContext, initial migration, seed data, and verify the database is created with seeded users — the first working end-to-end DB path.</objective>
<read_first>
- .planning/phases/01-database-authentication/01-RESEARCH.md (NuGet packages, entity schema, Fluent API constraints, Docker SQL setup)
- .planning/phases/01-database-authentication/01-CONTEXT.md (decisions D-04, D-07 for connection string and auto-migration)
- GEMINI.md (naming conventions, nullable reference types, EF Core rules)
</read_first>
<action>
1. Run `dotnet new mvc -n MothersonBoxManagement --no-https` inside the workspace root to scaffold the MVC project.
2. Add NuGet packages to MothersonBoxManagement.csproj: Microsoft.EntityFrameworkCore.SqlServer 8.0.*, Microsoft.EntityFrameworkCore.Design 8.0.* (PrivateAssets=all).
3. Enable `<Nullable>enable</Nullable>` in the .csproj if not already present.
4. Create enum `BoxType` in Entities/BoxType.cs with values: Carton, Bois, Plastique.
5. Create enum `BoxStatus` in Entities/BoxStatus.cs with values: Open, Completed, CompletedWithException, Cancelled, Archived, Blocked.
6. Create entity class `User` in Entities/User.cs with properties: int Id, string Matricule, string PasswordHash, string Role, bool IsActive. All string properties non-nullable.
7. Create entity class `Box` in Entities/Box.cs with properties: int Id, string BoxNumber, string BarcodeValue, BoxType Type, double Height, double Width, double Depth, int ExpectedQuantity, int CurrentQuantity, BoxStatus Status, int CreatedByUserId, int? LastModifiedByUserId, int? ClosedByUserId, DateTime CreatedAt, DateTime? UpdatedAt, DateTime? ClosedAt, byte[] RowVersion. Add navigation properties: User CreatedBy, User? LastModifiedBy, User? ClosedBy, ICollection<BoxPackage> Packages.
8. Create entity class `BoxPackage` in Entities/BoxPackage.cs with properties: int Id, int BoxId, string PackageBarcode, int ScannedByUserId, DateTime ScannedAt. Add navigation properties: Box Box, User ScannedBy.
9. Create entity class `BoxAuditLog` in Entities/BoxAuditLog.cs with properties: int Id, int? BoxId, string ActionType, int UserId, DateTime Timestamp, string? WorkstationName, string? DetailsJson. Add navigation properties: Box? Box, User User.
10. Create Data/ApplicationDbContext.cs inheriting DbContext. Register DbSet<User>, DbSet<Box>, DbSet<BoxPackage>, DbSet<BoxAuditLog>. In OnModelCreating, configure via Fluent API: User.Matricule unique index, Box.BoxNumber unique index, Box.BarcodeValue unique index, Box.RowVersion as IsRowVersion(), BoxPackage.PackageBarcode unique index, cascade delete restrictions on Box FK relationships to User.
11. Create Data/DbInitializer.cs with a static async method SeedAsync(ApplicationDbContext context) that checks if any User exists; if not, uses PasswordHasher<User> to hash "Motherson2026!" and inserts three users: { Matricule: "OP001", Role: "Operator" }, { Matricule: "SP001", Role: "Supervisor" }, { Matricule: "AD001", Role: "Administrator" }, all with IsActive = true.
12. Configure appsettings.json with ConnectionStrings:DefaultConnection using placeholder `Server=localhost,1433;Database=MothersonBoxDb;User Id=sa;Password=YOUR_PASSWORD_HERE;TrustServerCertificate=True;`. Create appsettings.Development.json (git-ignored) with the real SA password.
13. In Program.cs, register ApplicationDbContext with UseSqlServer and the DefaultConnection string. After building the app and before app.Run(), call context.Database.Migrate() and DbInitializer.SeedAsync(context) using a scoped service provider.
14. Run `dotnet ef migrations add InitialSchema --project MothersonBoxManagement` to generate the initial migration.
15. Verify: `dotnet build` exits 0, and running the app creates the MothersonBoxDb database with Users table containing 3 seeded rows.
</action>
<acceptance_criteria>
- `dotnet build` exits 0 with no errors
- File MothersonBoxManagement/Entities/User.cs contains class `User` with property `Matricule`
- File MothersonBoxManagement/Entities/Box.cs contains property `RowVersion` of type `byte[]`
- File MothersonBoxManagement/Data/ApplicationDbContext.cs contains `.HasIndex(bp => bp.PackageBarcode).IsUnique()`
- File MothersonBoxManagement/Data/DbInitializer.cs contains `PasswordHasher<User>`
- MothersonBoxManagement/Migrations/ directory contains at least one migration file with `InitialSchema` in its name
- `dotnet run --project MothersonBoxManagement` starts without exceptions (database created and seeded)
</acceptance_criteria>
<verify>
dotnet build MothersonBoxManagement/MothersonBoxManagement.csproj
</verify>
</task>

<task id="01-01-02">
<objective>Wire Cookie Authentication middleware and the AuthenticationService so that a user can be authenticated against the database — the auth pipeline is functional end-to-end.</objective>
<read_first>
- MothersonBoxManagement/Program.cs (current middleware pipeline state from task 01-01-01)
- .planning/phases/01-database-authentication/01-RESEARCH.md (Section 4: cookie config, claim schema)
- .planning/phases/01-database-authentication/01-CONTEXT.md (decisions D-05, D-06, D-08 for cookie policy)
</read_first>
<action>
1. Create Services/IAuthenticationService.cs with method signature: Task<User?> ValidateCredentialsAsync(string matricule, string password, CancellationToken cancellationToken).
2. Create Services/AuthenticationService.cs implementing IAuthenticationService. Constructor-inject ApplicationDbContext and IPasswordHasher<User>. In ValidateCredentialsAsync: query the User by Matricule (case-insensitive, IsActive == true), verify password using _passwordHasher.VerifyHashedPassword, return User on success or null on failure.
3. In Program.cs, register services: builder.Services.AddScoped<IAuthenticationService, AuthenticationService>() and builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>().
4. In Program.cs, configure authentication: builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => { options.Cookie.Name = "Motherson.BoxManagement.Auth"; options.Cookie.HttpOnly = true; options.Cookie.SameSite = SameSiteMode.Lax; options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; options.SlidingExpiration = true; options.ExpireTimeSpan = TimeSpan.FromMinutes(60); options.LoginPath = "/Account/Login"; options.AccessDeniedPath = "/Account/Login"; }).
5. In the middleware pipeline, add app.UseAuthentication() immediately before app.UseAuthorization().
</action>
<acceptance_criteria>
- `dotnet build` exits 0
- File MothersonBoxManagement/Services/AuthenticationService.cs contains `VerifyHashedPassword`
- File MothersonBoxManagement/Program.cs contains `Motherson.BoxManagement.Auth`
- File MothersonBoxManagement/Program.cs contains `app.UseAuthentication()` appearing before `app.UseAuthorization()`
</acceptance_criteria>
<verify>
dotnet build MothersonBoxManagement/MothersonBoxManagement.csproj
</verify>
</task>

<task id="01-01-03">
<objective>Build the Login UI and AccountController so a user can type their matricule and password, submit the form, authenticate against the database, and be redirected to the home page — the first complete user-facing interaction.</objective>
<read_first>
- MothersonBoxManagement/Services/IAuthenticationService.cs (method signature from task 01-01-02)
- .planning/phases/01-database-authentication/01-UI-SPEC.md (color palette, typography, copywriting contract)
- .planning/phases/01-database-authentication/01-CONTEXT.md (decision D-03 for matricule validation)
</read_first>
<action>
1. Create ViewModels/LoginViewModel.cs with properties: string Matricule ([Required], [RegularExpression(@"^[a-zA-Z0-9]{3,20}$", ErrorMessage)]), string Password ([Required]), string? ReturnUrl. Add DataAnnotations for validation.
2. Create Controllers/AccountController.cs. Constructor-inject IAuthenticationService. Add [HttpGet] Login(string? returnUrl) action returning View with new LoginViewModel { ReturnUrl = returnUrl }. Add [HttpPost] Login(LoginViewModel model) action: if ModelState is invalid, return View(model). Call _authenticationService.ValidateCredentialsAsync. On null result, add ModelError with message "Invalid matricule or password." and return View. On success, create ClaimsIdentity with claims: ClaimTypes.NameIdentifier = user.Id.ToString(), ClaimTypes.Name = user.Matricule, ClaimTypes.Role = user.Role, custom "Matricule" claim = user.Matricule. Sign in with HttpContext.SignInAsync using AuthenticationProperties { IsPersistent = false }. Redirect to returnUrl or "/" default.
3. Add [HttpGet] Logout action: call HttpContext.SignOutAsync and redirect to /Account/Login.
4. Create Views/Account/Login.cshtml as a Razor view with @model LoginViewModel. Use Bootstrap 5.3 CDN. Build a centered card (max-width 420px) with background #ffffff on page background #f8fafc. Include Inter font from Google Fonts. Form posts to /Account/Login with asp-for bindings for Matricule (text input, placeholder "Matricule") and Password (password input). Submit button text "Se connecter" with background-color #0f52ba. Display validation summary and asp-validation-for spans. Error text color #df2c3f.
5. Update Views/Shared/_Layout.cshtml to include Bootstrap 5.3 CSS CDN, Inter font, and a minimal navbar with the application name "Motherson Box Management". Add a _LoginPartial partial rendering logout link when user is authenticated.
6. Create Views/Shared/_LoginPartial.cshtml showing the logged-in user's matricule and a "Sign out" link to /Account/Logout when authenticated, or nothing when anonymous.
7. Update wwwroot/css/site.css with body background-color #f8fafc, font-family Inter, and accent color variables.
</action>
<acceptance_criteria>
- `dotnet build` exits 0
- File MothersonBoxManagement/ViewModels/LoginViewModel.cs contains `[RegularExpression(@"^[a-zA-Z0-9]{3,20}$"`
- File MothersonBoxManagement/Controllers/AccountController.cs contains `"Invalid matricule or password."`
- File MothersonBoxManagement/Controllers/AccountController.cs contains `IsPersistent = false`
- File MothersonBoxManagement/Views/Account/Login.cshtml contains `Se connecter`
- Navigating to /Account/Login in a browser displays a centered login card with matricule and password fields
- Submitting OP001 / Motherson2026! redirects to the home page
- Submitting invalid credentials shows "Invalid matricule or password."
</acceptance_criteria>
<verify>
dotnet build MothersonBoxManagement/MothersonBoxManagement.csproj
dotnet run --project MothersonBoxManagement --urls "http://localhost:5000"
</verify>
</task>

<task id="01-01-04">
<objective>Apply role-based authorization on the HomeController so that authenticated users land on their home page and unauthenticated users are redirected to login — proving AUTH-03 and BOX-05 claim propagation.</objective>
<read_first>
- MothersonBoxManagement/Controllers/AccountController.cs (claim creation from task 01-01-03)
- .planning/REQUIREMENTS.md (AUTH-03 role enforcement, BOX-05 creator identity)
- GEMINI.md (section 3: Authorize filters requirement)
</read_first>
<action>
1. Update Controllers/HomeController.cs: add [Authorize] attribute to the class. In the Index action, read User.FindFirst("Matricule")?.Value and User.FindFirst(ClaimTypes.Role)?.Value, pass them to ViewBag or a simple ViewModel. This proves BOX-05 readiness: the user identity context is available for future box creation.
2. Update Views/Home/Index.cshtml to display a welcome message including the user's matricule and role: "Bienvenue, {Matricule} ({Role})". Include a placeholder message: "Le tableau de bord sera disponible dans la phase suivante."
3. In Program.cs, ensure the default route maps to Home/Index. The [Authorize] attribute on HomeController will redirect unauthenticated users to /Account/Login automatically via the cookie configuration's LoginPath.
4. Verify the full flow: unauthenticated visit to / redirects to /Account/Login. Login with OP001 redirects to / showing "Bienvenue, OP001 (Operator)". Login with AD001 shows "Bienvenue, AD001 (Administrator)".
</action>
<acceptance_criteria>
- `dotnet build` exits 0
- File MothersonBoxManagement/Controllers/HomeController.cs contains `[Authorize]`
- File MothersonBoxManagement/Views/Home/Index.cshtml contains `Matricule` and `Role` display
- Unauthenticated GET / returns 302 redirect to /Account/Login
- Authenticated OP001 GET / displays "Bienvenue, OP001 (Operator)"
- Authenticated AD001 GET / displays "Bienvenue, AD001 (Administrator)"
</acceptance_criteria>
<verify>
dotnet build MothersonBoxManagement/MothersonBoxManagement.csproj
</verify>
</task>

<task id="01-01-05">
<objective>Create the test project with integration tests verifying login success, login failure (generic error), matricule validation, and authorization redirect — locking in AUTH-01, AUTH-02, AUTH-03 guarantees.</objective>
<read_first>
- .planning/phases/01-database-authentication/01-VALIDATION.md (test infrastructure, per-task verification map)
- MothersonBoxManagement/Controllers/AccountController.cs (login actions from task 01-01-03)
- MothersonBoxManagement/Data/ApplicationDbContext.cs (DbContext for in-memory test setup)
</read_first>
<action>
1. Run `dotnet new xunit -n MothersonBoxManagement.Tests` in the workspace root.
2. Add project reference to MothersonBoxManagement. Add NuGet: Microsoft.AspNetCore.Mvc.Testing 8.0.*.
3. Create CustomWebApplicationFactory.cs inheriting WebApplicationFactory<Program>. Override ConfigureWebHost to replace the SQL Server DbContext registration with UseInMemoryDatabase("TestDb") and ensure seeding runs. Make Program class accessible by adding `[assembly: InternalsVisibleTo("MothersonBoxManagement.Tests")]` or a partial Program class in the main project.
4. Create AccountControllerTests.cs with test methods:
   a. DisplayLogin_ReturnsLoginPage — GET /Account/Login returns 200 with "Se connecter" in response body.
   b. InvalidLogin_ShowsGenericError — POST /Account/Login with { Matricule: "INVALID", Password: "wrong" } returns 200 with "Invalid matricule or password." in response body.
   c. ValidLogin_RedirectsToHome — POST /Account/Login with { Matricule: "OP001", Password: "Motherson2026!" } returns 302 redirect to /.
   d. UnauthenticatedAccess_RedirectsToLogin — GET / without auth cookie returns 302 redirect containing "/Account/Login".
   e. MatriculeValidation_RejectsInvalidFormat — POST /Account/Login with { Matricule: "A!", Password: "test" } returns 200 with validation error (ModelState invalid).
5. Add a solution file `dotnet new sln -n MothersonBoxManagement` and add both projects to it for unified `dotnet test` execution.
</action>
<acceptance_criteria>
- `dotnet build` exits 0 for the entire solution
- `dotnet test` reports 5 passing tests, 0 failures
- Test DisplayLogin_ReturnsLoginPage verifies 200 status and "Se connecter" presence
- Test InvalidLogin_ShowsGenericError verifies "Invalid matricule or password." presence
- Test ValidLogin_RedirectsToHome verifies 302 redirect
- Test UnauthenticatedAccess_RedirectsToLogin verifies redirect to /Account/Login
- Test MatriculeValidation_RejectsInvalidFormat verifies validation rejection
</acceptance_criteria>
<verify>
dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj
</verify>
</task>

## must_haves

truths:
  - Navigating to / without authentication redirects to /Account/Login with 302 status
  - Submitting OP001 / Motherson2026! on /Account/Login authenticates and redirects to /
  - Submitting invalid credentials on /Account/Login displays "Invalid matricule or password."
  - The authentication cookie is named "Motherson.BoxManagement.Auth" with HttpOnly=true and SameSite=Lax
  - The authentication cookie is session-only (IsPersistent=false), destroyed when the browser closes
  - Cookie sliding expiration is 60 minutes
  - Database table Users contains 3 seeded rows: OP001 (Operator), SP001 (Supervisor), AD001 (Administrator)
  - All passwords are stored as PBKDF2 hashes via PasswordHasher<User>, never in plaintext
  - BoxPackages.PackageBarcode has a SQL unique index configured via EF Core Fluent API
  - Box entity has a RowVersion concurrency token configured via IsRowVersion()
  - User.Matricule has a unique index
  - Box.BoxNumber and Box.BarcodeValue each have unique indexes
  - ClaimTypes.Role is set during login to enable [Authorize(Roles = "...")] enforcement
  - A custom "Matricule" claim is set during login for identity context propagation (BOX-05)
  - HomeController has [Authorize] attribute, proving role-based access enforcement (AUTH-03)
  - `dotnet build` exits 0 for the full solution
  - `dotnet test` reports 5 passing tests with 0 failures

## Artifacts this phase produces

### Enums
- `BoxType` — Carton, Bois, Plastique
- `BoxStatus` — Open, Completed, CompletedWithException, Cancelled, Archived, Blocked

### Entities
- `User` — Id, Matricule (unique), PasswordHash, Role, IsActive
- `Box` — Id, BoxNumber (unique), BarcodeValue (unique), Type, Height, Width, Depth, ExpectedQuantity, CurrentQuantity, Status, CreatedByUserId, LastModifiedByUserId, ClosedByUserId, CreatedAt, UpdatedAt, ClosedAt, RowVersion
- `BoxPackage` — Id, BoxId, PackageBarcode (unique), ScannedByUserId, ScannedAt
- `BoxAuditLog` — Id, BoxId, ActionType, UserId, Timestamp, WorkstationName, DetailsJson

### Data Layer
- `ApplicationDbContext` — EF Core DbContext with Fluent API configurations
- `DbInitializer` — Static seeder with PasswordHasher<User> for 3 default users
- `InitialSchema` migration — Full schema creation

### Services
- `IAuthenticationService` — ValidateCredentialsAsync contract
- `AuthenticationService` — Database-backed credential validation with PBKDF2 verification

### ViewModels
- `LoginViewModel` — Matricule (regex validated), Password, ReturnUrl

### Controllers
- `AccountController` — Login (GET/POST), Logout (GET)
- `HomeController` — Index (authorized, displays matricule + role)

### Views
- `Views/Account/Login.cshtml` — Bootstrap 5.3 centered login card
- `Views/Home/Index.cshtml` — Welcome page with matricule and role
- `Views/Shared/_Layout.cshtml` — Master layout with navbar
- `Views/Shared/_LoginPartial.cshtml` — Auth-aware nav partial

### Tests
- `CustomWebApplicationFactory` — In-memory DB test fixture
- `AccountControllerTests` — 5 integration tests covering AUTH-01, AUTH-02, AUTH-03

### Configuration
- `appsettings.json` — Connection string placeholder
- `appsettings.Development.json` — Local SQL Server Docker connection (git-ignored)
- `wwwroot/css/site.css` — Brand styles (Inter font, #f8fafc background, #0f52ba accent)
