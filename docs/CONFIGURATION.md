---
title: Configuration
description: Guide de configuration de l'application Motherson Box Management
---

<!-- generated-by: gsd-doc-writer -->

# Configuration — Motherson Box Management

Ce document décrit l'ensemble des paramètres de configuration de l'application, y compris les variables d'environnement, les fichiers de configuration, la gestion des secrets et les différences entre les environnements de développement et de production.

---

## Variables d'environnement

| Variable | Requis | Défaut | Description |
| :--- | :---: | :--- | :--- |
| `ConnectionStrings__DefaultConnection` | **Oui** | — | Chaîne de connexion SQL Server pour la base de données `MothersonBoxDb`. |
| `WorkstationName` | Non | `DEFAULT-STATION` | Nom de la station de travail, utilisé dans les logs d'audit (`BoxAuditLog.WorkstationName`). |
| `ASPNETCORE_ENVIRONMENT` | Non | `Production` | Environnement d'exécution ASP.NET Core (`Development`, `Staging`, `Production`). |

> **Note :** Les paramètres d'authentification (`Cookie.Name`, `ExpireTimeSpan`, etc.) sont **codés en dur** dans `Program.cs` et ne sont pas surchargeables via des variables d'environnement. Pour les modifier, il faut éditer le code source.

---

## Fichiers de configuration

### `appsettings.json` (base)

Fichier de configuration principal, valable pour tous les environnements. Contient les valeurs par défaut et les placeholders sécurisés.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "WorkstationName": "DEFAULT-STATION",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=MothersonBoxDb;User Id=sa;Password=YOUR_PASSWORD_HERE;TrustServerCertificate=True;"
  }
}
```

### `appsettings.Development.json` (développement)

Fichier de configuration spécifique à l'environnement de développement. Écrase les valeurs de `appsettings.json` uniquement lorsque `ASPNETCORE_ENVIRONMENT=Development`.

```json
{
  "WorkstationName": "DEV-STATION-01",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=MothersonBoxDb;User Id=sa;Password=Motherson2026!;TrustServerCertificate=True;"
  }
}
```

> **Note :** Ce fichier contient des identifiants de développement et ne doit pas être commité dans le contrôle de version avec des secrets réels.

---

## Chaîne de connexion à la base de données

### Format

La chaîne de connexion suit le format standard SQL Server :

```
Server=localhost,1433;Database=MothersonBoxDb;User Id=sa;Password=YOUR_PASSWORD_HERE;TrustServerCertificate=True;
```

### Paramètres clés

| Paramètre | Description |
| :--- | :--- |
| `Server` | Adresse et port du serveur SQL Server. En développement : `localhost,1433`. |
| `Database` | Nom de la base de données : `MothersonBoxDb`. |
| `User Id` | Identifiant SQL Server. En développement : `sa`. |
| `Password` | Mot de passe SQL Server. **Ne jamais stocker de mots de passe réels dans le code source.** |
| `TrustServerCertificate` | Accepte les certificats auto-signés en développement. En production, utiliser un certificat valide. |

### Configuration en développement (LocalDB)

Pour utiliser SQL Server LocalDB en développement, remplacer la chaîne par :

```
Server=(localdb)\\mssqllocaldb;Database=MothersonBoxManagement;Trusted_Connection=True;MultipleActiveResultSets=true
```

---

## Configuration de l'authentification

L'application utilise l'**authentification par cookie** native d'ASP.NET Core, configurée dans `Program.cs`.

### Paramètres du cookie

| Paramètre | Valeur | Description |
| :--- | :--- | :--- |
| `Cookie.Name` | `Motherson.BoxManagement.Auth` | Nom du cookie de session. |
| `Cookie.HttpOnly` | `true` | Le cookie n'est pas accessible via JavaScript (protection XSS). |
| `Cookie.SameSite` | `Lax` | Protection CSRF de base. |
| `Cookie.SecurePolicy` | `SameAsRequest` | Le cookie est sécurisé uniquement en HTTPS. |
| `SlidingExpiration` | `true` | La session est prolongée à chaque requête tant qu'elle est active. |
| `ExpireTimeSpan` | `60 minutes` | Durée maximale de la session sans activité. |
| `LoginPath` | `/Account/Login` | Redirection en cas d'accès non authentifié. |
| `AccessDeniedPath` | `/Account/Login` | Redirection en cas d'accès interdit (rôle insuffisant). |

### Configuration via variables d'environnement

Seule la chaîne de connexion peut être surchargée via des variables d'environnement :

```bash
# Windows
set ConnectionStrings__DefaultConnection=Server=localhost,1433;Database=MothersonBoxDb;User Id=sa;Password=YOUR_SECRET;TrustServerCertificate=True;

# Linux / macOS
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=MothersonBoxDb;User Id=sa;Password=YOUR_SECRET;TrustServerCertificate=True;"
```

> **Note :** Les paramètres d'authentification (nom du cookie, durée de session) sont codés en dur dans `Program.cs` et ne peuvent pas être surchargés via des variables d'environnement. Pour les modifier, il faut éditer le code source directement.

---

## Configuration Entity Framework Core

### Contexte de base de données

Le contexte `ApplicationDbContext` est enregistré dans le conteneur d'injection de dépendances avec :

```csharp
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
});
```

### Provider

- **Package :** `Microsoft.EntityFrameworkCore.SqlServer` (version `8.0.*`)
- **Base de données cible :** SQL Server 2022 (ou compatible)

### Tables

Le contexte expose les `DbSet` suivants :

| DbSet | Table SQL | Description |
| :--- | :--- | :--- |
| `Users` | `Users` | Utilateurs de l'application (opérateurs, superviseurs, administrateurs). |
| `Boxes` | `Boxes` | Boxes de packaging avec statut, dimensions et quantités. |
| `BoxPackages` | `BoxPackages` | Packages de câbles associés aux boxes. |
| `BoxAuditLogs` | `BoxAuditLogs` | Journal d'audit append-only de toutes les actions. |

### Index et contraintes

| Table | Colonne | Type | Description |
| :--- | :--- | :--- | :--- |
| `Users` | `Matricule` | Index unique | Unicité du matricule utilisateur. |
| `Boxes` | `BoxNumber` | Index unique | Unicité du numéro de box. |
| `Boxes` | `BarcodeValue` | Index unique | Unicité de la valeur code-barres de la box. |
| `Boxes` | `RowVersion` | Concurrency token | Concurrence optimiste (`IsRowVersion()`). |
| `BoxPackages` | `PackageBarcode` | Index unique | **Unicité globale** du code-barres package (interdit le double scan). |

### Intercepteur d'audit

Un intercepteur `AuditSaveChangesInterceptor` est ajouté au contexte EF Core pour journaliser automatiquement toutes les opérations d'écriture dans `BoxAuditLogs`. Cet intercepteur est enregistré en tant que `Scoped` :

```csharp
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
```

### Migration automatique au démarrage

L'application applique automatiquement les migrations au démarrage :

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();

    await DbInitializer.SeedAsync(db);
}
```

- Pour une base relationnelle (SQL Server) : `MigrateAsync()` applique les migrations en attente.
- Pour une base non relationnelle : `EnsureCreatedAsync()` crée le schéma.
- `DbInitializer.SeedAsync()` peuple la base avec les utilisateurs par défaut.

---

## Gestion des secrets

### Règle fondamentale

> **Ne jamais commiter de secrets de production (vrais mots de passe, vraies chaînes de connexion) dans le code source ou dans les dépôts Git.**

### Options de gestion

#### 1. `dotnet user-secrets` (développement local)

L'outil Secrets Manager de .NET permet de stocker les secrets localement sans les écrire dans des fichiers trackés :

```powershell
# Initialiser les secrets dans le projet
dotnet user-secrets init

# Définir la chaîne de connexion
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=MothersonBoxDb;User Id=sa;Password=YOUR_SECRET;TrustServerCertificate=True;"
```

> **Note :** Les paramètres d'authentification (nom du cookie, durée de session) sont codés en dur dans `Program.cs`. Les clés `Authentication:CookieName` et `Authentication:ExpireTimeSpanMinutes` n'auraient aucun effet si définies via user-secrets.

Les secrets sont stockés dans le profil utilisateur système et ne sont jamais dans le dépôt.

#### 2. Variables d'environnement (production)

En production, les secrets doivent être fournis via les variables d'environnement du serveur ou de la plateforme de dépôt. Les noms de variables utilisent le séparateur `__` (double underscore) pour les hiérarchies imbriquées :

```
ConnectionStrings__DefaultConnection
```

> **Note :** Les paramètres d'authentification sont codés en dur et ne sont pas configurables via des variables d'environnement.

#### 3. `appsettings.Development.json` (développement uniquement)

Ce fichier peut contenir des secrets de développement, mais doit être ajouté au `.gitignore` s'il contient des identifiants réels.

---

## Configuration du logging

### Niveaux de log par défaut

| Catégorie | Niveau | Description |
| :--- | :--- | :--- |
| `Default` | `Information` | Tous les logs de niveau Information et supérieur. |
| `Microsoft.AspNetCore` | `Warning` | Seuls les avertissements et erreurs du framework ASP.NET Core. |

### Configuration via environment

En développement, les logs détaillés sont activés automatiquement. En production, seuls les logs `Warning` et `Error` sont enregistrés par défaut.

---

## Configuration par environnement

### Développement (`Development`)

| Paramètre | Valeur |
| :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | `Development` |
| `WorkstationName` | `DEV-STATION-01` |
| Base de données | SQL Server local (`localhost,1433`) ou LocalDB |
| Authentification | Cookie avec durée de 60 minutes |
| Logs | Niveau `Information` pour toutes les catégories |
| Gestion des erreurs | Page d'erreur détaillée (développeur exception page) |

### Production (`Production`)

| Paramètre | Valeur |
| :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `WorkstationName` | Nom réel de la station (via variable d'environnement) |
| Base de données | SQL Server de production (via variable d'environnement) |
| Authentification | Cookie avec durée configurée (via variable d'environnement) |
| Logs | Niveau `Warning` pour le framework, configurable via `appsettings.Production.json` |
| Gestion des erreurs | `UseExceptionHandler("/Home/Error")` — aucune fuite d'information interne |

### Sécurité en production

En mode non-développement, l'application active :

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
```

Cela empêche l'affichage de détails d'erreur internes (schéma de base, chaîne de connexion, stack trace) aux utilisateurs finaux.

---

## Configuration réseau et ports

### Ports de développement

| Profil | URL | Description |
| :--- | :--- | :--- |
| `http` | `http://localhost:5169` | Profil Kestrel par défaut. |
| `IIS Express` | `http://localhost:27432` | Profil IIS Express (SSL désactivé). |

### Configuration IIS

```json
{
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:27432",
      "sslPort": 0
    }
  }
}
```

---

## Packages NuGet liés à la configuration

| Package | Version | Usage |
| :--- | :--- | :--- |
| `Microsoft.EntityFrameworkCore.SqlServer` | `8.0.*` | Provider SQL Server pour EF Core. |
| `Microsoft.EntityFrameworkCore.Design` | `8.0.*` | Outils de design pour les migrations EF Core. |

---

## Vérification de la configuration

Pour vérifier que la configuration est correctement chargée :

1. **Vérifier les variables d'environnement :**
   ```powershell
   $env:ConnectionStrings__DefaultConnection
   $env:Authentication__CookieName
   $env:Authentication__ExpireTimeSpanMinutes
   ```

2. **Vérifier les secrets :**
   ```powershell
   dotnet user-secrets list
   ```

3. **Tester la connexion à la base :**
   ```powershell
   dotnet ef database update --project MothersonBoxManagement --startup-project MothersonBoxManagement
   ```

---

## Notes importantes

1. **Le mot de passe SQL Server** dans `appsettings.Development.json` (`Motherson2026!`) est un identifiant de développement. Utiliser `dotnet user-secrets` ou des variables d'environnement pour les secrets réels.

2. **Le seeder** (`DbInitializer.SeedAsync`) crée automatiquement trois utilisateurs au premier démarrage avec le mot de passe par défaut `Motherson2026!` (hashé). Ce mot de passe doit être changé en production.

3. **La contrainte d'unicité** sur `BoxPackages.PackageBarcode` est gérée au niveau de la base de données (index unique EF Core) **et** au niveau applicatif (validation dans les services). Les deux niveaux sont nécessaires pour garantir l'intégrité.

4. **Le `RowVersion`** sur les boxes implémente la concurrence optimiste. Toute tentative de modification simultanée par deux opérateurs provoquera une exception de concurrence à gérer dans les services.
