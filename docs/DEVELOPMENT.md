<!-- generated-by: gsd-doc-writer -->
# DEVELOPMENT.md — Guide de développement

Guide complet pour les contributeurs du projet **Motherson Box Management**.

---

## Configuration locale

### Prérequis

- **Runtime :** .NET 8.0 SDK (cible `net8.0`)
- **Base de données :** SQL Server 2022 (ou LocalDB pour le développement)
- **IDE :** Visual Studio 2022, Visual Studio Code avec l'extension C#, ou JetBrains Rider

### Installation

```powershell
git clone <URL_DU_DEPOT>
cd Motherson_Box_Management
dotnet restore
```

### Variables d'environnement

Configurer `appsettings.Development.json` ou utiliser le Secret Manager :

```powershell
dotnet user-secrets set "ConnectionStrings__DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=MothersonBoxManagement;Trusted_Connection=True;MultipleActiveResultSets=true" --project MothersonBoxManagement
dotnet user-secrets set "Authentication__CookieName" "Motherson.BoxManagement.Auth" --project MothersonBoxManagement
dotnet user-secrets set "Authentication__ExpireTimeSpanMinutes" "60" --project MothersonBoxManagement
```

> Ne jamais commiter de secrets de production dans le dépôt.

### Première exécution

```powershell
dotnet run --project MothersonBoxManagement
```

L'application démarre sur `https://localhost:XXXX` ou `http://localhost:XXXX`. La base de données est automatiquement migrée au premier lancement via `DbInitializer.SeedAsync()`.

---

## Structure du projet

```
Motherson_Box_Management/
├── MothersonBoxManagement/           # Projet web ASP.NET Core MVC
│   ├── Controllers/                  # Contrôleurs MVC minces (routage + liaison de ViewModels)
│   ├── Models/                       # ViewModels (pas d'entités EF Core directement)
│   ├── ViewModels/                   # ViewModels supplémentaires
│   ├── Entities/                     # Entités métier mappées en base (Box, BoxPackage, User, etc.)
│   ├── Services/                     # Services métier autonomes (IBoxService, IUserAuthenticationService)
│   ├── Data/
│   │   ├── ApplicationDbContext.cs   # Contexte EF Core avec Fluent API
│   │   ├── DbInitializer.cs         # Seed initial (utilisateur admin par défaut)
│   │   ├── Dtos/                     # Objets de transfert entre contrôleurs et services
│   │   └── Interceptors/            # AuditSaveChangesInterceptor (audit append-only)
│   ├── Migrations/                   # Migrations EF Core
│   ├── Views/                        # Pages Razor (Bootstrap 5.3)
│   ├── wwwroot/                      # Fichiers statiques (JS, CSS, images)
│   ├── Program.cs                    # Point d'entrée et configuration des services
│   └── MothersonBoxManagement.csproj
├── MothersonBoxManagement.Tests/     # Projet de tests
├── docs/                             # Documentation
├── .planning/                        # Artefacts de planification GSD Core
├── TODO.md                           # Suivi des tâches
├── AGENT.md                          # Mémoire persistante du projet
└── Motherson_Box_Management.sln      # Solution Visual Studio
```

---

## Conventions de code C# et ASP.NET Core MVC

### Nommage

| Élément | Convention | Exemple |
|---------|-----------|---------|
| Classes, interfaces, enums, structs | `PascalCase` | `BoxService`, `IBoxService`, `BoxStatus` |
| Méthodes publiques | `PascalCase` | `CreateBoxAsync()`, `ScanPackageAsync()` |
| Propriétés publiques | `PascalCase` | `BoxNumber`, `BarcodeValue`, `CurrentQuantity` |
| Variables locales et paramètres | `camelCase` | `userId`, `cancellationToken`, `boxNumber` |
| Champs privés readonly | `_camelCase` | `_boxService`, `_applicationDbContext` |

### Nullables Reference Types

Activation obligatoire dans le `.csproj` :

```xml
<Nullable>enable</Nullable>
```

Tout warning nullable doit être résolu proprement — interdiction de supprimer les alertes avec le null-forging `!` de manière abusive.

### Asynchronisme

- Utiliser `async`/`await` pour **toutes** les opérations d'E/S (accès DB, lectures de fichiers).
- Propager les `CancellationToken` dans les chaînes d'appels asynchrones.
- Interdiction stricte de `.Result` ou `.Wait()` (risque de deadlock).

### Injection de dépendances

Injection de dépendances par constructeur native d'ASP.NET Core. L'anti-pattern *Service Locator* est interdit.

**Exemple (depuis `BoxController.cs`) :**

```csharp
[Authorize]
public class BoxController : Controller
{
    private readonly IBoxService _boxService;

    public BoxController(IBoxService boxService)
    {
        _boxService = boxService;
    }
}
```

**Enregistrement dans `Program.cs` :**

```csharp
builder.Services.AddScoped<IBoxService, BoxService>();
```

### Séparation des responsabilités

| Couche | Responsabilité | Contenu |
|--------|---------------|---------|
| **Controllers** | Routage, liaison de ViewModels, validation d'état | `[Authorize]`, validation ModelState, redirection |
| **Services** | Toute la logique métier, calculs, transactions SQL | `IBoxService`, `IUserAuthenticationService` |
| **ViewModels** | Transfert de données vers et depuis les vues Razor | `CreateBoxViewModel`, `LoginViewModel`, `PrepareViewModel` |
| **Entities** | Entités pures mappées en base de données | `Box`, `BoxPackage`, `User`, `BoxAuditLog` |

> **Règle absolue :** Ne jamais exposer les entités EF Core directement aux formulaires ou contrats externes. Utiliser exclusivement des ViewModels dédiés.

### Sécurité

- Mots de passe : hash cryptographique uniquement (`IPasswordHasher<User>`)
- Filtres d'autorisation par rôle : `[Authorize(Roles = "...")]` sur toutes les actions sensibles
- CSRF : token antiforgery avec en-tête `X-CSRF-TOKEN`
- Ne jamais loguer de mots de passe ou de données sensibles
- Ne jamais exposer les détails internes de la base de données dans les messages d'erreur utilisateur

---

## Comment ajouter un nouveau controller

1. **Créer l'interface et la classe de service** dans `Services/` :
   ```csharp
   // Services/IMyService.cs
   public interface IMyService
   {
       Task<MyResult> DoSomethingAsync(int id, CancellationToken cancellationToken);
   }

   // Services/MyService.cs
   public class MyService : IMyService
   {
       private readonly ApplicationDbContext _dbContext;

       public MyService(ApplicationDbContext dbContext)
       {
           _dbContext = dbContext;
       }

       public async Task<MyResult> DoSomethingAsync(int id, CancellationToken cancellationToken)
       {
           // Logique métier ici
       }
   }
   ```

2. **Enregistrer le service** dans `Program.cs` :
   ```csharp
   builder.Services.AddScoped<IMyService, MyService>();
   ```

3. **Créer les ViewModels** dans `ViewModels/` :
   ```csharp
   public class MyViewModel
   {
       [Required]
       public string Name { get; set; } = string.Empty;
   }
   ```

4. **Créer le controller** dans `Controllers/` :
   ```csharp
   [Authorize]
   public class MyController : Controller
   {
       private readonly IMyService _myService;

       public MyController(IMyService myService)
       {
           _myService = myService;
       }

       [HttpGet]
       public IActionResult Index()
       {
           return View(new MyViewModel());
       }

       [HttpPost]
       [ValidateAntiForgeryToken]
       public async Task<IActionResult> Index(MyViewModel model, CancellationToken cancellationToken)
       {
           if (!ModelState.IsValid)
               return View(model);

           var result = await _myService.DoSomethingAsync(model.Id, cancellationToken);
           return RedirectToAction("Details", new { id = result.Id });
       }
   }
   ```

5. **Créer les vues Razor** dans `Views/My/`

---

## Comment ajouter une nouvelle migration EF Core

1. **Modifier les entités** dans `Entities/` ou la configuration Fluent API dans `ApplicationDbContext.cs`.

2. **Générer la migration :**
   ```powershell
   dotnet ef migrations add <NomMigration> --project MothersonBoxManagement --startup-project MothersonBoxManagement
   ```

3. **Appliquer la migration localement :**
   ```powershell
   dotnet ef database update --project MothersonBoxManagement --startup-project MothersonBoxManagement
   ```

4. **Mettre à jour la section « État des migrations »** dans `AGENT.md`.

> **Règle :** Ne jamais modifier manuellement ou supprimer une migration déjà validée et partagée.

### Règles pour les migrations

- Toute modification d'entité ou de configuration d'entité doit faire l'objet d'une migration nommée explicitement.
- Utiliser des noms explicites et descriptifs (ex. `AddPackageBarcodeIndex`, `ConvertBoxDimensionsToInt`).
- La contrainte d'unicité de `BoxPackages.PackageBarcode` doit être maintenue via Fluent API :
  ```csharp
  entity.HasIndex(bp => bp.PackageBarcode).IsUnique();
  ```
- Le `RowVersion` des boxes doit être configuré comme jeton de concurrence :
  ```csharp
  entity.Property(b => b.RowVersion).IsRowVersion();
  ```

---

## Comment ajouter une nouvelle entité

1. **Créer la classe d'entité** dans `Entities/` :
   ```csharp
   namespace MothersonBoxManagement.Entities;

   public class MyEntity
   {
       public int Id { get; set; }
       public string Name { get; set; } = string.Empty;
       // Propriétés de navigation, etc.
   }
   ```

2. **Ajouter le `DbSet`** dans `ApplicationDbContext.cs` :
   ```csharp
   public DbSet<MyEntity> MyEntities => Set<MyEntity>();
   ```

3. **Configurer la relation** dans `OnModelCreating` avec Fluent API si nécessaire.

4. **Générer la migration** (voir section précédente).

5. **Mettre à jour `AGENT.md`** : ajouter l'entité au tableau du §8 (Modèle de données).

---

## Débogage

### Points de débogage utiles

| Fichier | Intérêt |
|---------|---------|
| `Program.cs` | Configuration des services, pipeline middleware, connexion DB |
| `Data/ApplicationDbContext.cs` | Configurations Fluent API, relations, index |
| `Data/Interceptors/AuditSaveChangesInterceptor.cs` | Intercepteur d'audit (log automatique des opérations) |
| `Data/DbInitializer.cs` | Données de seed (utilisateurs initiaux) |
| `Services/BoxService.cs` | Logique métier centrale (création, scan, transfert, annulation) |

### Scénarios de debug fréquents

**Erreur de concurrence (`DbUpdateConcurrencyException`) :**
- Deux opérateurs modifient la même box simultanément.
- Vérifier que `RowVersion` est correctement configuré dans `ApplicationDbContext.cs`.

**Erreur d'unicité sur `PackageBarcode` :**
- Tentative d'associer un package déjà scanné à une autre box.
- Vérifier la contrainte unique SQL dans la migration et dans Fluent API.

**Erreur d'autorisation (`403 Forbidden`) :**
- L'utilisateur n'a pas le rôle requis (`Operator`, `Supervisor`, `Administrator`).
- Vérifier les attributs `[Authorize(Roles = "...")]` sur l'action.

**Audit non enregistré :**
- Vérifier que `AuditSaveChangesInterceptor` est enregistré dans le pipeline EF Core (`Program.cs`).
- Vérifier que l'intercepteur est bien injecté dans `AddDbContext`.

### Logs et diagnostic

En mode développement, les détails d'erreur sont affichés dans la page d'erreur. En production, les erreurs sont redirigées vers `/Home/Error` sans exposer les détails internes.

---

## Configuration de l'IDE

### Visual Studio 2022

1. Ouvrir `MothersonBoxManagement.sln`
2. Installer le workload « Développement web et cloud .NET » si nécessaire
3. Activer les analyseurs de code :
   - **Analyseur Roslyn** pour les warnings nullable
   - **Code style** : PascalCase pour les types, camelCase pour les variables
4. Activer « Traiter les warnings comme des erreurs » dans les propriétés du projet (optionnel, recommandé)

### Visual Studio Code

1. Installer les extensions :
   - **C# for Visual Studio Code** (Microsoft)
   - **.NET Extension Pack**
   - **EditorConfig** (si un `.editorconfig` est ajouté)
2. Ouvrir le dossier racine du dépôt

### JetBrains Rider

1. Ouvrir le fichier `MothersonBoxManagement.sln`
2. Rider détecte automatiquement les conventions C# du projet

---

## Workflow GSD Core (cycle de travail obligatoire)

### Avant toute modification

1. Consulter les fichiers de planification dans `.planning/` (`PROJECT.md`, `ROADMAP.md`) pour identifier la phase active.
2. Lire `AGENT.md` pour maîtriser les règles métier liées au périmètre.
3. Repérer ou créer la tâche dans `TODO.md` et passer son statut à `En cours`.
4. Examiner le code et les migrations existantes.

### Pendant le développement

1. Effectuer des modifications petites, ciblées et incrémentales.
2. Implémenter la logique métier dans des services isolés et écrire les tests correspondants.
3. Mettre à jour `TODO.md` à chaque étape majeure.
4. Mettre à jour `AGENT.md` dès qu'une modification touche :
   - Le modèle de données (entités, relations, contraintes)
   - Le statut des migrations
   - Les règles métier
   - Les routes MVC
   - Les dépendances NuGet

### Avant de considérer le travail terminé

Exécuter les commandes de validation obligatoires :

```powershell
dotnet restore
```

```powershell
dotnet build
```

```powershell
dotnet test
```

Vérifier que :
- Le build et tous les tests passent sans warning majeur ni erreur.
- La documentation (`AGENT.md` et `TODO.md`) est à jour.
- La tâche est passée à `Terminé` dans `TODO.md`.

---

## Git et commits

- **Ne jamais commiter** si le build ou les tests échouent.
- **Ne jamais utiliser** `git add .` sans inspecter rigoureusement les fichiers inclus.
- Rédiger les commits en respectant les **Conventional Commits** :
  - `feat(nom-module) :` — Nouvelle fonctionnalité
  - `fix(nom-module) :` — Correction de bug
  - `db(migration) :` — Ajout ou modification de migration EF Core
  - `docs(agent) :` — Mise à jour de la documentation
  - `refactor(nom-module) :` — Refactoring sans changement de comportement
  - `test(nom-module) :` — Ajout ou modification de tests
  - `chore :` — Tâches de maintenance (dépendances, configuration)

### Règles pour les commits

- Un commit = une unité logique de travail.
- Le message doit être clair et concis (une ligne de titre, optionnellement un paragraphe de description).
- Ne jamais commiter de secrets, mots de passe ou chaînes de connexion réelles.

---

## Exécution des tests

```powershell
dotnet test
```

Le projet de tests (`MothersonBoxManagement.Tests/`) contient des tests unitaires, d'intégration et de bout en bout couvrant :

| Fichier de test | Périmètre |
|-----------------|-----------|
| `BoxControllerTests.cs` | Actions du BoxController |
| `ScanControllerTests.cs` | Logique de scan de packages |
| `BoxServiceExceptionTests.cs` | Gestion des exceptions métier |
| `ConcurrencyTests.cs` | Concurrence optimiste (RowVersion) |
| `SecurityAuditTests.cs` | Audit et sécurité |
| `E2ELifecycleTests.cs` | Scénarios bout en bout complets |
| `AuditControllerTests.cs` | Actions du AuditController |
| `AccountControllerTests.cs` | Authentification et comptes |
| `SupervisorExceptionsControllerTests.cs` | Actions exceptionnelles supervisur |

---

## Checklists de modification

### Avant modification

- [ ] Consulter `.planning/PROJECT.md` et `.planning/ROADMAP.md`
- [ ] Lire `AGENT.md` pour les règles métier
- [ ] Créer ou mettre à jour la tâche dans `TODO.md` (statut : `En cours`)
- [ ] Vérifier que le workspace est propre (`git status`)

### Après modification

- [ ] `dotnet build` — zéro erreur, zéro warning majeur
- [ ] `dotnet test` — tous les tests passent
- [ ] Migration EF Core générée et appliquée (si modification de schéma)
- [ ] `AGENT.md` mis à jour (si entité, relation, migration, route ou package NuGet modifié)
- [ ] `TODO.md` mis à jour (tâche à `Terminé`)
- [ ] Commit rédigé en Conventional Commits
