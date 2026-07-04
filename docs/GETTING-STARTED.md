---
title: Guide de démarrage
description: Installation et configuration du projet Motherson Box Management pour les nouveaux développeurs
---

# Guide de démarrage rapide

Ce guide vous accompagne depuis le clonage du dépôt jusqu'à une application fonctionnelle sur votre poste de développement.

## Prérequis

| Outil | Version requise | Vérification |
|:------|:----------------|:-------------|
| **.NET SDK** | `8.0` ou supérieur | `dotnet --version` |
| **SQL Server** | 2022 ou supérieur (LocalDB accepté) | `sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT @@VERSION"` |
| **Git** | Dernière version stable | `git --version` |
| **IDE** | Visual Studio 2022, VS Code, ou Rider | — |

> **Note :** LocalDB (`(localdb)\mssqllocaldb`) est inclus avec Visual Studio et suffit pour le développement local.

## Étapes d'installation

### 1. Cloner le dépôt

```powershell
git clone <URL_DU_DEPOT>
cd Motherson_Box_Management
```

### 2. Restaurer les dépendances

```powershell
dotnet restore
```

### 3. Configurer la chaîne de connexion

Le fichier `MothersonBoxManagement/appsettings.Development.json` contient déjà une chaîne de connexion par défaut pour un environnement LocalDB :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=MothersonBoxDb;User Id=sa;Password=Motherson2026!;TrustServerCertificate=True;"
  }
}
```

Si vous utilisez **LocalDB** au lieu d'une instance SQL Server, remplacez la chaîne par :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MothersonBoxDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 4. Appliquer les migrations et initialiser la base de données

```powershell
dotnet ef database update --project MothersonBoxManagement --startup-project MothersonBoxManagement
```

> **Alternative :** L'application applique automatiquement les migrations au démarrage via `Program.cs` (méthode `MigrateAsync`). L'étape manuelle est donc facultative si la base de données SQL Server est accessible.

### 5. Lancer l'application

```powershell
dotnet run --project MothersonBoxManagement
```

L'application démarre sur `http://localhost:5169` (port défini dans `Properties/launchSettings.json`).

### 6. Se connecter

Ouvrez `http://localhost:5169` dans votre navigateur. L'écran de connexion s'affiche.

Des comptes de test sont automatiquement créés lors de la première initialisation de la base de données :

| Matricule | Rôle | Mot de passe |
|:----------|:-----|:-------------|
| `OP001` | Opérateur | `Motherson2026!` |
| `SP001` | Superviseur | `Motherson2026!` |
| `AD001` | Administrateur | `Motherson2026!` |

> **Note :** Ces comptes sont créés une seule fois par `DbInitializer.SeedAsync`. Les comptes existants ne sont pas écrasés lors des redémarrages.

## Vérifier que l'installation fonctionne

1. **Connexion :** Connectez-vous avec le matricule `OP001` et le mot de passe `Motherson2026!`.
2. **Tableau de bord :** Vous atterrissez sur la page d'accueil (`/` ou `/Home/Index`) affichant le champ « Scanner une box » et la liste des boxes actives.
3. **Création d'une box :** Naviguez vers `/Box/Create`, remplissez le formulaire (type, dimensions en cm entiers, quantité attendue), et soumettez. La box apparaît avec le statut `Open`.
4. **Scan de package :** Sur la page `/Box/Prepare/{id}`, utilisez le simulateur de scanner virtuel pour scanner un code-barres au format `PKG-XXXXXX`.
5. **Déconnexion :** Cliquez sur « Déconnexion » (`/Account/Logout`).

Si ces étapes aboutissent, l'installation est fonctionnelle.

## Aperçu de la structure du projet

```
Motherson_Box_Management/
├── MothersonBoxManagement/          # Projet web principal (ASP.NET Core MVC)
│   ├── Controllers/                  # Contrôleurs MVC (Account, Box, Home, Audit)
│   ├── Entities/                     # Entités métier (User, Box, BoxPackage, BoxAuditLog)
│   ├── Data/                         # DbContext, intercepteurs d'audit, DTOs, initialisation
│   │   ├── Interceptors/             # AuditSaveChangesInterceptor (log append-only)
│   │   └── Dtos/                     # Objets de transfert de données
│   ├── Models/                       # ViewModels pour les vues Razor
│   ├── ViewModels/                   # ViewModels supplémentaires
│   ├── Services/                     # Logique métier (IBoxService, IAuthenticationService)
│   ├── Views/                        # Pages Razor (Account, Box, Home, Audit, Shared)
│   ├── Migrations/                   # Migrations EF Core
│   ├── wwwroot/                      # Fichiers statiques (CSS, JS, lib)
│   ├── Properties/                   # launchSettings.json
│   ├── Program.cs                    # Point d'entrée et configuration des services
│   └── appsettings*.json             # Configuration par environnement
├── MothersonBoxManagement.Tests/    # Tests unitaires et d'intégration (xUnit)
├── docs/                             # Documentation du projet
├── .planning/                        # Fichiers de planification GSD Core
├── AGENT.md                          # Mémoire persistante du projet (source d'autorité)
├── Motherson_Box_Management.sln      # Solution Visual Studio
└── README.md
```

### Rôle de chaque dossier clé

| Dossier | Responsabilité |
|:--------|:---------------|
| `Controllers/` | Contrôleurs MVC minces — routage, liaison de ViewModels, validation d'état. La logique métier est déléguée aux services. |
| `Entities/` | Entités métier pures mappées en base (User, Box, BoxPackage, BoxAuditLog). Jamais exposées directement aux vues. |
| `Data/` | `ApplicationDbContext`, configurations EF Core, intercepteurs d'audit, DTOs et `DbInitializer` (seed des comptes). |
| `Services/` | Services métier autonomes contenant toute la logique, les validations et les transactions SQL. |
| `Views/` | Pages Razor structurées avec Bootstrap. |
| `wwwroot/` | Scripts JS (simulateur de scanner USB), styles CSS, fichiers statiques. |
| `MothersonBoxManagement.Tests/` | Tests xUnit avec base InMemory et `WebApplicationFactory` pour les tests d'intégration. |

## Problèmes fréquents lors de la première configuration

### Erreur de connexion à la base de données

**Symptôme :** `A network-related or instance-specific error occurred while establishing a connection to SQL Server.`

**Solutions :**
- Vérifiez que SQL Server (ou LocalDB) est en cours d'exécution.
- Si vous utilisez LocalDB, assurez-vous qu'il est installé : `sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT 1"`.
- Vérifiez la chaîne de connexion dans `appsettings.Development.json`.

### Erreur de migration EF Core

**Symptôme :** `No EF Core migration metadata was found in assembly 'MothersonBoxManagement'.`

**Solutions :**
- Restaurez les dépendances : `dotnet restore`.
- Vérifiez que le `--project` pointe vers le projet contenant les migrations (`MothersonBoxManagement`).
- Si la base existe déjà avec un schéma différent, supprimez-la et relancez `dotnet ef database update`.

### Port déjà utilisé

**Symptôme :** `Error: listen EADDRINUSE: address already in use :::5169`

**Solution :** Modifiez le port dans `Properties/launchSettings.json` ou arrêtez le processus qui utilise le port :
```powershell
netstat -ano | findstr :5169
taskkill /PID <PID> /F
```

### Le simulateur de scanner ne fonctionne pas

**Symptôme :** Le scan virtuel ne déclenche aucune action sur la page.

**Solutions :**
- Assurez-vous d'être sur la page `/Box/Prepare/{id}` (pas la page d'accueil).
- Le simulateur est un panel JavaScript dans l'UI — vérifiez que JavaScript est activé dans le navigateur.
- Consultez la console du navigateur (F12) pour identifier les erreurs JS.

### Comptes de test déjà existants

**Symptôme :** L'initialisation ne crée pas de nouveaux comptes (c'est normal).

**Explication :** `DbInitializer.SeedAsync` vérifie l'existence de chaque matricule avant de créer le compte. Les comptes existants ne sont jamais écrasés.

## Étapes suivantes

- [Architecture du projet](./ARCHITECTURE.md) — Vue d'ensemble technique de l'application
- [Configuration](./CONFIGURATION.md) — Variables d'environnement et paramètres
- `AGENT.md` — Source d'autorité pour les règles métier, le modèle de données et les conventions
