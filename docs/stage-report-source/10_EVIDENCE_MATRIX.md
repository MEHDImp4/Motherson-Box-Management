# 10_EVIDENCE_MATRIX.md -- Correspondance Affirmation-Preuve

Ce fichier met en correspondance chaque affirmation significative avec sa source dans le code source. Chaque entree comprend un niveau de confiance.

## Legende de confiance

- :green_circle: **Confirme** -- Directement observe dans le code, la configuration, la migration ou le test
- :blue_circle: **Fortement probable** -- Deduit de plusieurs patterns de code coherents
- :yellow_circle: **Partiellement verifie** -- Des preuves existent mais sont incompletes
- :orange_circle: **Non verifie** -- Impossible a confirmer uniquement a partir du code source
- :red_circle: **Contradictoire** -- Plusieurs sources donnent des informations conflictuelles

---

## Architecture et Technologie

| ID | Affirmation | Type | Preuve | Confiance |
|----|-------------|------|--------|-----------|
| E-001 | Le framework est ASP.NET Core MVC .NET 8.0 | code | `MothersonBoxManagement.csproj` ligne 4 : `<TargetFramework>net8.0</TargetFramework>` | :green_circle: |
| E-002 | La base de donnees est SQL Server 2022 | config | `docker-compose.yml` ligne 3 : `mcr.microsoft.com/mssql/server:2022-CU15-ubuntu-22.04` | :green_circle: |
| E-003 | L'ORM est EF Core 8.0.28 | code | `MothersonBoxManagement.csproj` ligne 19 : `Microsoft.EntityFrameworkCore.SqlServer` Version 8.0.28 | :green_circle: |
| E-004 | Le framework CSS est Bootstrap 5.3 | code | `_Layout.cshtml` lien CDN vers `bootstrap@5.3.3` | :green_circle: |
| E-005 | La bibliotheque QR code est QRCoder 1.8.0 | code | `MothersonBoxManagement.csproj` ligne 20 : `<PackageReference Include="QRCoder" Version="1.8.0" />` | :green_circle: |
| E-006 | Program.cs fait 140 lignes | code | `MothersonBoxManagement/Program.cs` | :green_circle: |
| E-007 | La solution contient 4 projets | code | `Motherson_Box_Management.sln` | :green_circle: |

## Modele de donnees

| ID | Affirmation | Type | Preuve | Confiance |
|----|-------------|------|--------|-----------|
| E-010 | 11 DbSets dans ApplicationDbContext | code | `ApplicationDbContext.cs` | :green_circle: |
| E-011 | 23 migrations EF Core existent | code | Repertoire `MothersonBoxManagement/Migrations/` | :green_circle: |
| E-012 | PackageBarcode a un index unique filtre | migration | Migration `AllowHistoricalPackageReassociation` | :green_circle: |
| E-013 | Box.RowVersion est un rowversion SQL Server | code | `ApplicationDbContext.cs` `.IsRowVersion()` | :green_circle: |
| E-014 | 3 contraintes CHECK sur la table Boxes | migration | Migration `AddCriticalBoxCheckConstraints` | :green_circle: |
| E-015 | BoxStatus a 7 valeurs (0-6) | code | `Entities/BoxStatus.cs` | :green_circle: |
| E-016 | BoxType a 3 valeurs (0-2) | code | `Entities/BoxType.cs` | :green_circle: |
| E-017 | Toutes les relations FK utilisent Restrict pour la suppression | code | `ApplicationDbContext.cs` Fluent API | :green_circle: |
| E-018 | Le principal SYSTEM a Id=-1 | migration | Migration `AddSystemAuditPrincipal` | :green_circle: |
| E-019 | ScanRequestId assure l'idempotence | migration | Migration `BoundStringsAndScanIdempotency` | :green_circle: |

## Authentification et Securite

| ID | Affirmation | Type | Preuve | Confiance |
|----|-------------|------|--------|-----------|
| E-020 | Authentification par cookie (pas ASP.NET Identity) | code | `AuthenticationConfiguration.cs` | :green_circle: |
| E-021 | Mots de passe haches avec IPasswordHasher<User> | code | `UserService.cs`, `AuthenticationService.cs` | :green_circle: |
| E-022 | SecurityStamp regenere au changement de mot de passe | code | `UserService.cs` `ResetPasswordAsync` | :green_circle: |
| E-023 | Verrouillage de connexion apres 5 tentatives (15 min) | code | `LoginLockoutService.cs`, `appsettings.json` | :green_circle: |
| E-024 | Limitation de debit : login (5/min), scan (60/min), global (200/min) | code | `RateLimitingConfiguration.cs` | :green_circle: |
| E-025 | CSRF sur toutes les actions POST | code | `[ValidateAntiForgeryToken]` sur toutes les methodes POST | :green_circle: |
| E-026 | Middleware d'en-tetes de securite | code | `SecurityHeadersConfiguration.cs` | :green_circle: |
| E-027 | La deconnexion est un POST avec CSRF | code | `AccountController.cs` `[HttpPost]` + `[ValidateAntiForgeryToken]` | :green_circle: |
| E-028 | Auto-desactivation empechee | code | `UsersController.Deactivate` verifie `GetCurrentUserId()` | :green_circle: |

## Logique metier

| ID | Affirmation | Type | Preuve | Confiance |
|----|-------------|------|--------|-----------|
| E-030 | Unicite du colis appliquee au niveau SQL | migration | Index unique filtre `AllowHistoricalPackageReassociation` | :green_circle: |
| E-031 | Auto-completion lorsque CurrentQuantity >= ExpectedQuantity | code | `PackageScanService.cs`, `BoxService.cs` | :green_circle: |
| E-032 | Repetition en concurrence optimiste (3 tentatives) | code | `BoxService.cs` `ExecuteWithConcurrencyRetryAsync` | :green_circle: |
| E-033 | Journalisation d'audit via l'intercepteur SaveChanges | code | `AuditSaveChangesInterceptor.cs` | :green_circle: |
| E-034 | Suppression logique sur les colis (IsRemoved), jamais de suppression physique | code | `BoxPackage.cs` champ `IsRemoved` | :green_circle: |
| E-035 | Mode sticky dans le scanner (etat HAS_BOX) | code | `scanner.js` machine a etats | :green_circle: |
| E-036 | Scan automatique avec correspondance de prefixe (le plus long gagne) | code | `BoxTemplateService.FindTemplateByPackageBarcodeAsync` | :green_circle: |
| E-037 | Toutes les operations de superviseur necessitent une raison | code | `BoxOperationsController.cs` validation de raison | :green_circle: |
| E-038 | Les operateurs ne peuvent pas creer de boites manuellement | code | `BoxController.CreateFromTemplate` necessite un modele | :green_circle: |
| E-039 | DateTime.UtcNow utilise de maniere coherente | code | Services et intercepteur | :green_circle: |

## Infrastructure

| ID | Affirmation | Type | Preuve | Confiance |
|----|-------------|------|--------|-----------|
| E-040 | Build Docker multi-etape (SDK 8.0.422 -> ASP.NET 8.0.28) | config | `Dockerfile` | :green_circle: |
| E-041 | Points de terminaison de sante : /health/live, /health, /health/ready | code | `Program.cs` lignes 120-136 | :green_circle: |
| E-042 | Le workflow CI existe (.github/workflows/ci.yml) | config | `.github/workflows/ci.yml` | :green_circle: |
| E-043 | Protection des donnees avec stockage de cles sur systeme de fichiers | code | `Program.cs` lignes 26-38 | :green_circle: |
| E-044 | Le compose de production utilise HTTPS sur le port 8443 | config | `docker-compose.prod.yml` | :green_circle: |
| E-045 | Auto-migration desactivee en production | code | `DatabaseInitializationExtensions.cs` | :green_circle: |

## Tests

| ID | Affirmation | Type | Preuve | Confiance |
|----|-------------|------|--------|-----------|
| E-050 | ~130 methodes de test reparties dans 22 fichiers | code | Liste du repertoire du projet de test | :green_circle: |
| E-051 | WebApplicationFactory avec BD InMemory | code | `CustomWebApplicationFactory.cs` | :green_circle: |
| E-052 | Intercepteurs personnalises simulent des comportements SQL Server | code | Fichiers de test avec classes d'intercepteurs | :green_circle: |
| E-053 | Tests SQL Server conditionnels ([SqlServerFact]) | code | `SqlServerIntegrationTests.cs` | :green_circle: |
| E-054 | Parallelisation des tests desactivee | code | `[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]` | :green_circle: |
| E-055 | Derniere verification : 162 tests reussis (2026-07-13) | execution | `docs/production-readiness-audit/07-TEST-REPORT.md` | :green_circle: |
| E-056 | Couverture : 16,69% des lignes, 58,39% des branches | execution | `docs/production-readiness-audit/07-TEST-REPORT.md` | :green_circle: |

## Roles et Permissions

| ID | Affirmation | Type | Preuve | Confiance |
|----|-------------|------|--------|-----------|
| E-060 | 3 roles applicatifs + 1 role systeme | code | `Security/AppRoles.cs` | :green_circle: |
| E-061 | Variantes francaises de roles supportees (Superviseur, Admin) | code | `AppRoles.SupervisorFr`, `AppRoles.AdminFr` | :green_circle: |
| E-062 | Les operateurs peuvent creer des boites a partir de modeles approuves | code | `BoxController.CreateFromTemplate` `[Authorize]` (tout role) | :green_circle: |
| E-063 | Les operateurs ne peuvent pas acceder a CancelBox, ForceCloseBox, etc. | code | `BoxOperationsController` `[Authorize(Roles = Supervisor/Admin)]` | :green_circle: |
| E-064 | Seul l'Admin peut gerer les utilisateurs | code | `UsersController` `[Authorize(Roles = AdminFr, Administrator)]` | :green_circle: |

## Print Agent

| ID | Affirmation | Type | Preuve | Confiance |
|----|-------------|------|--------|-----------|
| E-070 | PrintAgent est une application de barre des taches WinForms | code | `MothersonBoxManagement.PrintAgent.csproj` `<UseWindowsForms>true</UseWindowsForms>` | :green_circle: |
| E-071 | Deux modes d'impression : ZPL et Windows GDI+ | code | `LabelPrinter.cs` | :green_circle: |
| E-072 | Stockage de token chiffre par DPAPI | code | `Dpapi.cs` | :green_circle: |
| E-073 | Authentification par Bearer token pour l'API | code | `PrintAgentApiController.cs` `AuthenticateAsync` | :green_circle: |
| E-074 | Code d'appairage : 12 caracteres, expiration 10 min, usage unique | code | `PrintAgentService.cs` | :green_circle: |
| E-075 | Heartbeat toutes les 30 secondes | code | `PrintAgentContext.cs` | :green_circle: |
| E-076 | Backoff exponentiel (plafond 2s-30s) | code | `AgentContracts.cs` `AgentBackoff` | :green_circle: |

## Problemes connus

| ID | Affirmation | Type | Preuve | Confiance |
|----|-------------|------|--------|-----------|
| E-080 | ARCHITECTURE.md est desynchronise du code | documentation | `docs/production-readiness-audit/01-PROJECT-UNDERSTANDING.md` | :green_circle: |
| E-081 | Le nombre de tests dans TESTING.md est obsolete (indique 129) | documentation | `docs/TESTING.md` vs production-readiness-audit | :red_circle: |
| E-082 | PrintAgent sera utilise en production (pas du code mort) | saisie humaine | Reponse de l'interne | :green_circle: |
| E-083 | La barre de progression de STATE.md affiche 80% mais les metadonnees indiquent 100% | documentation | `.planning/STATE.md` | :red_circle: |
| E-084 | Mot de passe seed statique toujours dans DbInitializer | code | `DbInitializer.cs` | :green_circle: |

## Informations fournies par l'interne (saisie humaine)

| ID | Affirmation | Type | Preuve | Confiance |
|----|-------------|------|--------|-----------|
| E-090 | Le stagiaire est le developpeur unique du projet | saisie humaine | Reponse de l'interne | :green_circle: |
| E-091 | Duree du stage : 1 mois (juillet 2026) | saisie humaine | Reponse de l'interne | :green_circle: |
| E-092 | Encadrants : Aymane et Ayoub | saisie humaine | Reponse de l'interne | :green_circle: |
| E-093 | Entreprise : Motherson PKC, Kenitra (Maroc) | saisie humaine | Reponse de l'interne | :green_circle: |
| E-094 | 2-3 terminaux en production | saisie humaine | Reponse de l'interne | :green_circle: |
| E-095 | Volume estime : ~10 cartons/jour | saisie humaine | Reponse de l'interne (approximatif) | :yellow_circle: |
| E-096 | Pas encore de deploiement en production | saisie humaine | Reponse de l'interne | :green_circle: |
| E-097 | Scanners USB testes sur le materiel cible | saisie humaine | Reponse de l'interne | :green_circle: |
| E-098 | Impression physique non encore testee | saisie humaine | Reponse de l'interne | :green_circle: |
| E-099 | Difficulte technique principale : systeme d'impression | saisie humaine | Reponse de l'interne | :green_circle: |
| E-100 | Perspectives v2 : Integration ERP, export Excel/PDF | saisie humaine | Reponse de l'interne | :green_circle: |
