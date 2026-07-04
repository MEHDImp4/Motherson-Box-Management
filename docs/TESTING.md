---
title: Testing
description: Guide complet de la stratégie de tests, de l'exécution et de la rédaction de tests pour Motherson Box Management
---

# TESTING.md — Stratégie et guide de tests

## Vue d'ensemble

Le projet **Motherson Box Management** utilise une stratégie de tests à plusieurs niveaux pour garantir la fiabilité des opérations métier critiques : création de boxes, scan de packages, gestion des exceptions superviseur, et concurrence d'accès. L'ensemble du corpus de tests repose sur **xUnit** avec une base de données **InMemory** (EF Core) pour isoler les tests de toute instance SQL Server réelle.

## Framework et dépendances

| Package | Version | Rôle |
| :--- | :--- | :--- |
| `xunit` | 2.5.3 | Framework de tests |
| `xunit.runner.visualstudio` | 2.5.3 | Adaptateur Visual Studio / `dotnet test` |
| `Microsoft.AspNetCore.Mvc.Testing` | 8.0.* | `WebApplicationFactory` pour les tests d'intégration HTTP |
| `Microsoft.EntityFrameworkCore.InMemory` | 8.0.* | Provider InMemory pour isoler les tests de la base SQL Server |
| `coverlet.collector` | 6.0.0 | Collecteur de couverture de code |
| `Microsoft.NET.Test.Sdk` | 17.8.0 | SDK de test .NET |

## Exécuter les tests

### Tous les tests

```bash
dotnet test
```

### Tests d'un fichier spécifique

```bash
dotnet test --filter "FullyQualifiedName~BoxControllerTests"
```

### Tests par classe

```bash
dotnet test --filter "FullyQualifiedName~ConcurrencyTests"
```

### Tests par nom

```bash
dotnet test --filter "DisplayName~ScanDuplicatePackage_Rejected"
```

### Avec couverture de code

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Watch mode (développement)

```bash
dotnet test --watch
```

## Structure du projet de tests

```
MothersonBoxManagement.Tests/
├── MothersonBoxManagement.Tests.csproj   # Projet xUnit, cible net8.0
├── CustomWebApplicationFactory.cs        # Factory de test HTTP personnalisée
├── TestAuthHelper.cs                     # Helper d'authentification pour les tests
├── BoxControllerTests.cs                 # Tests du contrôleur Box (CRUD, préparation, scan)
├── ScanControllerTests.cs                # Tests du scan de packages (valide, dupliqué, auto-complétion)
├── AuditControllerTests.cs               # Tests du journal d'audit (accès, filtres, pagination)
├── AccountControllerTests.cs             # Tests de connexion et d'authentification
├── HomeControllerTests.cs                # Tests du contrôleur Home (tableau de bord, recherche)
├── SecurityAuditTests.cs                 # Tests de sécurité (autorisations, validation, gestion des erreurs)
├── ConcurrencyTests.cs                   # Tests de concurrence optimiste et d'unicité
├── E2ELifecycleTests.cs                  # Tests end-to-end du cycle de vie complet d'une box
├── SupervisorExceptionsControllerTests.cs # Tests des actions superviseur (annulation, transfert, blocage)
├── BoxServiceExceptionTests.cs           # Tests unitaires de la couche service (transfert, annulation, blocage)
└── AdditionalTests.cs                    # Tests de validation (ViewModels, hashage, statuts, codes-barres)
```

## Helpers de test

### CustomWebApplicationFactory

Fichier : `CustomWebApplicationFactory.cs`

Cette factory remplace la base de données SQL Server par une base **InMemory** et configure trois utilisateurs de test :

| Matricule | Rôle | Mot de passe |
| :--- | :--- | :--- |
| `OP001` | Operator | `Motherson2026!` |
| `SP001` | Supervisor | `Motherson2026!` |
| `AD001` | Administrator | `Motherson2026!` |

Elle inclut également :

- **`FakeAntiforgery`** : Bypass du jeton CSRF pour simplifier les requêtes POST de test.
- **`E2EUniqueConstraintSimulatingInterceptor`** : Intercepteur EF Core qui simule la contrainte d'unicité sur `PackageBarcode` (l'InMemory ne gère pas les index uniques nativement). Il maintient un `HashSet` de codes-barres déjà vus et lève une `DbUpdateException` en cas de doublon.

Chaque test obtient une base de données unique (GUID aléatoire) via `_dbName = Guid.NewGuid().ToString()`, garantissant l'isolation entre tests.

### TestAuthHelper

Fichier : `TestAuthHelper.cs`

Helper statique pour authentifier un client de test :

```csharp
var client = await TestAuthHelper.CreateAuthenticatedClient(factory, "OP001", "Motherson2026!");
```

La méthode `CreateAuthenticatedClient` effectue un POST vers `/Account/Login` avec les identifiants fournis et retourne un `HttpClient` authentifié avec le cookie de session. Elle désactive la redirection automatique (`AllowAutoRedirect = false`) pour pouvoir inspecter les réponses de redirection.

Elle fournit également une extension `ToJsonContent<T>` pour sérialiser des objets en JSON pour les requêtes AJAX.

## Couverture des tests

### AccountControllerTests

Tests de l'authentification et du compteur de connexion :

- Affichage de la page de connexion
- Connexion avec identifiants invalides (message d'erreur générique)
- Connexion réussie pour chaque rôle (Operator, Supervisor, Administrator)
- Redirection vers `/Account/Login` pour les utilisateurs non authentifiés
- Validation du format du matricule (3-20 caractères, alphanumérique)

### BoxControllerTests

Tests du contrôleur principal de gestion des boxes :

- **Création** : formulaire GET, POST avec données valides (redirection vers Préparation), POST avec dimensions invalides (retour au formulaire)
- **Détails** : affichage d'une box existante, 404 pour une box inexistante
- **Tableau de bord** : affichage des boxes actives, autofocus du champ scanner
- **Scan depuis l'accueil** : redirection vers Préparation (box ouverte) ou Détails (box fermée)
- **Avertissement package** : scan d'un code-barres package sur l'écran d'accueil affiche un avertissement
- **Préparation** : accès à la page de scan pour une box ouverte, redirection pour une box fermée

### ScanControllerTests

Tests du parcours de scan de packages :

- **Scan valide** : package ajouté avec succès, redirigé vers les détails
- **Scan dupliqué** : même code-barres rejeté avec message contenant "déjà"
- **Scan code box** : code-barres BOX scanné comme package rejeté
- **Auto-complétion** : scan du dernier package change automatiquement le statut à `Completed`
- **Barcode vide / trop court** : rejet avec message de validation
- **Scan sur box complète** : rejet avec message "n'est pas ouverte aux scans"
- **Scan sur box inexistante** : 404
- **ScanAjax** : retour JSON avec succès/échec, vérification de l'audit log
- **Capacité atteinte** : scan supplémentaire rejeté après que `ExpectedQuantity` est atteinte
- **Index unique** : vérification que le modèle EF Core configure un index unique sur `PackageBarcode`

### AuditControllerTests

Tests du journal d'audit :

- **Accès anonyme** : redirection vers connexion
- **Accès Opérateur** : redirection (accès refusé)
- **Accès Superviseur** : affichage du journal
- **Filtres** : par `boxId`, par `actionType`, par plage de dates
- **Pagination** : navigation entre pages avec 25 entrées

### SecurityAuditTests

Tests de sécurité et autorisations :

- **Opérateur vs Endpoints Superviseur** : vérifie que l'Opérateur est redirigé vers `/Account/Login` pour toutes les actions supervisées (CancelBox, ForceCloseBox, BlockBox, UnblockBox, TransferPackage, RetraitPackage, etc.)
- **Superviseur vs Endpoints Superviseur** : vérifie que le Superviseur accède aux endpoints (redirection vers Détails, pas vers Login)
- **Validation des dimensions** : rejet des valeurs nulles ou négatives avec messages explicites
- **Barcode court** : rejet des codes-barres de moins de 3 caractères
- **Gestionnaire d'erreurs global** : en mode Production, les exceptions non gérées retournent un message générique ("Une erreur inattendue s'est produite") sans exposer les détails de la base de données

### ConcurrencyTests

Tests de concurrence et d'unicité :

- **Retry sur conflit de concurrence** : simulation d'une `DbUpdateConcurrencyException` via un intercepteur EF Core, vérifie que le scan réussit après retry
- **Scan concurrent du même package** : 8 tâches parallèles scannent le même code-barres, exactement 1 réussit et 7 échouent avec le message "Ce code-barres paquet a déjà été scanné."

### E2ELifecycleTests

Tests end-to-end du cycle de vie complet :

- **Cycle complet** : création (Operator) → scan de 2 packages (Operator) → blocage (Supervisor) → scan rejeté → déblocage (Supervisor) → transfert de package (Supervisor) → clôture avec exception (Supervisor) → vérification de l'état et des logs d'audit
- **Doublon E2E** : scan d'un même package sur la même box puis sur une autre box, les deux échouent
- **Codes-barres invalides** : scan d'un code box comme package, scan d'un barcode trop court
- **Accès non autorisé** : client anonyme redirigé vers Login, Opérateur redirigé pour les actions Superviseur
- **Scan simultané** : 5 clients HTTP parallèles scannent le même package, exactement 1 réussit

### SupervisorExceptionsControllerTests

Tests des actions exceptionnelles du Superviseur :

- **Opérateur vs actions Superviseur** : accès refusé à CancelBox
- **Annulation** : statut `Cancelled` avec raison enregistrée
- **Clôture avec écart** : statut `CompletedWithException` avec raison
- **Blocage/Déblocage** : transitions `Open → Blocked → Open` avec raison
- **Modification quantité attendue** : mise à jour de `ExpectedQuantity` avec raison
- **Retrait de package** : suppression du package et décrémentation de `CurrentQuantity`
- **Blocage/Déblocage package** : propriété `IsBlocked` et `BlockReason` mises à jour
- **Transfert de package** : quantités source/destination mises à jour, raison propagée
- **Journal d'audit** : vérifie la structure JSON (`ChangedProperties` sans `OriginalValues`/`CurrentValues`), sérialisation des enums en chaînes

### BoxServiceExceptionTests

Tests unitaires de la couche service (`IBoxService`) :

- **Transfert** : quantités source/destination mises à jour, raison propagée aux deux boxes, audit log créé
- **Annulation** : statut `Cancelled`, raison enregistrée, double annulation échoue
- **Blocage/Déblocage** : transitions de statut correctes avec raison

### AdditionalTests

Tests paramétrés (`[Theory]`/`[InlineData]`) :

- **Validation CreateBoxViewModel** : 13 cas de test couvrant toutes les combinaisons de dimensions valides/invalides
- **Validation LoginViewModel** : 11 cas de test pour le matricule et le mot de passe
- **Hashage de mot de passe** : 10 cas de test vérifiant `PasswordHasher<User>` (hash + vérification)
- **Validation code-barres** : 10 cas de test pour la distinction box/package et les formats acceptés
- **Énumération BoxStatus** : vérifie que chaque valeur d'enum est correctement nommée

## Rédiger de nouveaux tests

### Conventions de nommage

Les noms de tests suivent le pattern `MethodOrScenario_Condition_ExpectedResult` :

```
CreateBox_Post_ValidData_RedirectsToPrepare
ScanDuplicatePackage_Rejected
Operator_CannotAccess_SupervisorEndpoints
```

### Pattern de test d'intégration HTTP

```csharp
public class MyControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MyControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> LoginAsync(string matricule = "OP001", string password = "Motherson2026!")
    {
        return await TestAuthHelper.CreateAuthenticatedClient(_factory, matricule, password);
    }

    [Fact]
    public async Task MyAction_ValidInput_ReturnsOk()
    {
        // Arrange
        var client = await LoginAsync();

        // Act
        var response = await client.GetAsync("/MyController/MyAction");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Expected content", content);
    }
}
```

### Pattern de test de service (sans HTTP)

```csharp
[Fact]
public async Task MyService_DoSomething_ReturnsExpectedResult()
{
    using var scope = _factory.Services.CreateScope();
    var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Arrange - données de test directement en base
    var user = db.Users.First(u => u.Matricule == "OP001");
    var boxDto = new CreateBoxDto { /* ... */ };
    var box = await boxService.CreateBoxAsync(boxDto, user.Id);

    // Act
    var result = await boxService.SomeMethodAsync(box.Id, "param", user.Id);

    // Assert
    Assert.True(result.Success);
}
```

### Pattern de test paramétré

```csharp
[Theory]
[InlineData("PKG-VALID", true, null)]
[InlineData("BOX-123456", false, "Les codes-barres de box ne peuvent pas être scannés comme paquets.")]
public async Task BarcodeValidation_Theory(string barcode, bool expectedSuccess, string? expectedMessage)
{
    using var scope = _factory.Services.CreateScope();
    var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
    // ... Arrange, Act, Assert
}
```

### Règles importantes

1. **Isolation** : chaque test utilise une base InMemory unique (via le GUID dans `CustomWebApplicationFactory`). Ne jamais partager d'état entre tests.
2. **Authentification** : toujours authentifier le client via `LoginAsync()` ou `TestAuthHelper.CreateAuthenticatedClient()` avant de tester une action protégée.
3. **Assertions de contenu** : pour les messages d'erreur français, utiliser `StringComparison.OrdinalIgnoreCase` et `WebUtility.HtmlDecode()` si le contenu est encodé HTML.
4. **Transactions** : les tests accédant directement à la base via des scopes (`_factory.Services.CreateScope()`) doivent disposer correctement les scopes.
5. **Concurrence** : pour les tests de concurrence, chaque tâche parallèle doit créer son propre scope et son propre `DbContext` pour simuler des connexions distinctes.
6. **Désactivation de la parallélisation** : `BoxServiceExceptionTests.cs` désactive la parallélisation via `[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]` car certains tests modifient l'état global de la base InMemory.

## Intégration CI

<!-- VERIFY: Aucun fichier de workflow GitHub Actions n'a été trouvé dans le dépôt. L'intégration CI n'est pas encore configurée. -->

Aucun pipeline CI/CD n'est actuellement configuré. Pour mettre en place l'exécution automatique des tests, créer un fichier `.github/workflows/test.yml` :

```yaml
name: Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --verbosity normal
```

## Couverture de code

Le projet inclut `coverlet.collector` (v6.0.0) pour la collecte de couverture. Pour générer un rapport :

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

Les résultats seront disponibles dans `./coverage/*/coverage.cobertura.xml`. Aucun seuil de couverture minimum n'est actuellement configuré pour bloquer le build.
