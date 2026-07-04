<!-- generated-by: gsd-doc-writer -->
# Motherson Box Management — Guide d'utilisation

**Motherson Box Management** est une application web interne conçue pour la zone de packaging P3 de l'usine Motherson. Elle permet de suivre, préparer et auditer de manière autonome des boxes de packaging contenant des packages de câbles identifiés par codes-barres.

---

## Table des matières
1. [Fonctionnalités principales](#fonctionnalités-principales)
2. [Prérequis et installation](#prérequis-et-installation)
3. [Comptes d'accès (Seeded Accounts)](#comptes-daccès-seeded-accounts)
4. [Guide d'utilisation de l'application](#guide-dutilisation-de-lapplication)
   - [1. Authentification](#1-authentification)
   - [2. Tableau de bord & Recherche](#2-tableau-de-bord--recherche)
   - [3. Création d'une Box](#3-création-dune-box)
   - [4. Scan de packages & Simulateur virtuel](#4-scan-de-packages--simulateur-virtuel)
   - [5. Gestion des exceptions (Superviseur)](#5-gestion-des-exceptions-superviseur)
   - [6. Journal d'audit](#6-journal-daudit)
5. [Commandes de développement et tests](#commandes-de-développement-et-tests)
6. [Technologies utilisées](#technologies-utilisées)

---

## Fonctionnalités principales

- **Gestion autonome** : Fonctionne de manière isolée sans dépendances directes avec l'ERP ou le MES.
- **Règle d'unicité stricte** : Un code-barres de package ne peut être scanné et associé qu'à **une seule box active** dans le système SQL Server.
- **Différenciation des formats** : Les formats de codes-barres boxes (`BOX-YYYYMMDD-XXXXXX`) et packages (`PKG-...`) sont validés pour éviter toute erreur opérationnelle.
- **Journal d'audit immuable** : Historique complet en mode *append-only* (impossible à modifier ou supprimer).
- **Simulateur de scan** : Panel virtuel intégré pour tester les flux et comportements du scanner de codes-barres sans matériel physique.

---

## Prérequis et installation

### Prérequis
- [SDK .NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (ex: LocalDB ou SQL Server Express local / conteneur Docker)

### Installation et configuration de la base de données
1. **Cloner le dépôt** et ouvrir un terminal dans le répertoire racine du projet.
2. **Restaurer les dépendances** :
   ```powershell
   dotnet restore
   ```
3. **Configurer la chaîne de connexion** dans le fichier `MothersonBoxManagement/appsettings.Development.json` (ou via `dotnet user-secrets`) :
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost,1433;Database=MothersonBoxDb;User Id=sa;Password=VotreMotDePasse;TrustServerCertificate=True;"
   }
   ```
4. **Appliquer les migrations EF Core** (la base de données et les tables seront automatiquement créées et alimentées au premier démarrage) :
   ```powershell
   dotnet ef database update --project MothersonBoxManagement
   ```
5. **Lancer l'application** :
   ```powershell
   dotnet run --project MothersonBoxManagement
   ```

---

## Comptes d'accès (Seeded Accounts)

Au premier démarrage, la base de données est automatiquement alimentée (seeded) avec les comptes de test suivants :

| Rôle | Matricule (Identifiant) | Mot de passe | Description des droits |
| :--- | :--- | :--- | :--- |
| **Opérateur** | `OP001` | `Motherson2026!` | Créer des boxes, scanner des packages, reprendre des boxes ouvertes. |
| **Superviseur** | `SP001` | `Motherson2026!` | *Tous les droits Opérateur* + actions exceptionnelles (annulation, forçage, retrait, transfert de package, blocage). |
| **Administrateur** | `AD001` | `Motherson2026!` | *Tous les droits Superviseur* + gestion des utilisateurs et consultation du journal d'audit complet. |

> [!CAUTION]
> Ces comptes sont destinés au développement et aux tests uniquement. Ne jamais les utiliser en production.

---

## Guide d'utilisation de l'application

### 1. Authentification
Rendez-vous sur `http://localhost:5000` (ou l'URL affichée par Kestrel).
Saisissez votre matricule de test (ex. `OP001` ou `SP001`) et le mot de passe `Motherson2026!`. L'application utilise une authentification par cookie sécurisée.

### 2. Tableau de bord & Recherche
Une fois connecté, vous arrivez sur le **Tableau de Bord** :
- **Recherche/Scan rapide de Box** : Placez votre curseur dans le champ de recherche principal et scannez/saisissez le code-barres d'une box.
  - S'il s'agit d'une box **ouverte** (`Open`), vous êtes redirigé vers l'écran de scan.
  - S'il s'agit d'une box **fermée**, vous accédez à sa vue de détail en lecture seule.
- **Liste des Boxes actives** : Affiche les boxes en cours de traitement avec un indicateur graphique de progression (quantité courante / quantité attendue).

### 3. Création d'une Box
1. Cliquez sur **Créer une Box** dans le menu ou le tableau de bord.
2. Sélectionnez le **Type de Box** (Carton, Bois, Plastique) et saisissez les dimensions (en centimètres, entiers strictement positifs).
3. Renseignez la **Quantité attendue** (nombre de packages de câbles que la box doit contenir).
4. Validez. Le système génère automatiquement un identifiant unique (ex: `BOX-20260702-000001`) et un code-barres associé au format valide.

### 4. Scan de packages & Simulateur virtuel
Depuis l'écran de préparation d'une box ouverte :
- **Scan physique** : Pointez le scanner de codes-barres (configuré en mode clavier/keyboard wedge) et scannez le code-barres d'un package de câbles (ex. `PKG-XXXXXXXX-XXXXXX`). L'application intercepte automatiquement la saisie et valide le code.
- **Simulateur de Scan Virtuel** : Si vous ne disposez pas de scanner physique :
  1. Utilisez le panel de simulation intégré sur le côté droit de l'écran.
  2. Saisissez ou générez un code-barres de test au format valide (`PKG-...`).
  3. Cliquez sur **Simuler le scan** pour envoyer l'événement à la page.
- **Clôture automatique** : Dès que la quantité scannée atteint la quantité attendue, la box passe automatiquement au statut `Completed` (Fermée) et se verrouille pour tout scan ultérieur.

> [!IMPORTANT]
> Un code-barres de package déjà affecté à une box ne peut pas être scanné pour une autre box. Le système renverra un message d'erreur rouge explicite et la transaction sera annulée.

### 5. Gestion des exceptions (Superviseur)
Les comptes ayant le rôle de **Superviseur** ou d'**Administrateur** peuvent effectuer les actions correctives suivantes sur les boxes depuis leur vue de détail :
- **Clôture forcée avec écart** : Clôturer la box immédiatement avant d'atteindre le nombre prévu (statut `CompletedWithException`). Un motif d'écart justificatif est requis.
- **Annuler une Box** : Annule la box (statut `Cancelled`). Les packages associés y restent rattachés sauf action explicite de retrait.
- **Retrait / Transfert de package** : Retirer un package spécifique d'une box ou le transférer vers une autre box ouverte pour correction (motif obligatoire).
- **Mise en quarantaine (Blocage)** : Bloquer temporairement une box (statut `Blocked`) ou un package pour suspendre les scans ou l'expédition.

### 6. Journal d'audit
Toute action sensible (scan de package, modification de quantité attendue, annulation, transfert, blocage) engendre automatiquement une ligne d'audit dans la table `BoxAuditLogs`. Cette table est en écriture seule et sert de base de contrôle pour les audits IT et qualité.

---

## Commandes de développement et tests

Voici les commandes principales à exécuter depuis la racine de la solution :

### Compiler le projet
```powershell
dotnet build
```

### Exécuter les tests unitaires et d'intégration
```powershell
dotnet test
```

### Exécuter l'application localement
```powershell
dotnet run --project MothersonBoxManagement
```

### Restaurer les dépendances
```powershell
dotnet restore
```

### Ajouter une migration EF Core
```powershell
dotnet ef migrations add <NomMigration> --project MothersonBoxManagement
```

### Appliquer les migrations
```powershell
dotnet ef database update --project MothersonBoxManagement
```

---

## Technologies utilisées

| Composant | Technologie | Version |
| :--- | :--- | :--- |
| Framework | ASP.NET Core MVC | 8.0 |
| ORM | Entity Framework Core | 8.0 |
| Base de données | SQL Server | 2022+ |
| Framework CSS | Bootstrap | 5.3 |
| Authentification | Cookie Authentication (ASP.NET Core natif) | - |
