# 02_FUNCTIONAL_SCOPE.md -- Perimetre fonctionnel et regles metier

## 1. Acteurs et roles

### 1.1 Definitions des roles

| Role | Constante | Variante francaise | Description |
|------|-----------|---------------------|-------------|
| Operateur | `Operator` | -- | Scanne les lots, consulte les cartons, utilise les templates approuves |
| Superviseur | `Supervisor` | `Superviseur` | Tous les droits Operateur + exceptions du cycle de vie des cartons + gestion des templates |
| Administrateur | `Administrator` | `Admin` | Tous les droits Superviseur + gestion des utilisateurs + acces complet a l'audit |
| Systeme | `System` | -- | Principal d'audit interne (ID = -1, pas un vrai utilisateur) |

**Source :** `MothersonBoxManagement/Security/AppRoles.cs`, `AGENT.md` lignes 77-82

### 1.2 Matrice des permissions

| Ressource / Action | Operateur | Superviseur | Administrateur |
|--------------------|-----------|-------------|----------------|
| Voir le tableau de bord | Oui | Oui | Oui |
| Scanner un code-barres carton depuis le tableau de bord | Oui | Oui | Oui |
| Creer un carton depuis un template approuve | Oui | Oui | Oui |
| Voir les details d'un carton | Oui | Oui | Oui |
| Rechercher des cartons | Oui | Oui | Oui |
| Rechercher des lots | Oui | Oui | Oui |
| Configurer les parametres du poste | Oui | Oui | Oui |
| Imprimer via navigateur (PrintClient) | Oui | Oui | Oui |
| Ouvrir un carton Cree | Non | Oui | Oui |
| Imprimer cote serveur (Print) | Non | Oui | Oui |
| Creer/modifier/desactiver les templates | Non | Oui | Oui |
| Annuler un carton | Non | Oui | Oui |
| Fermer un carton avec exception | Non | Oui | Oui |
| Modifier la quantite prevue | Non | Oui | Oui |
| Bloquer/debloquer un carton | Non | Oui | Oui |
| Archiver un carton | Non | Oui | Oui |
| Bloquer/debloquer un lot | Non | Oui | Oui |
| Transferer un lot | Non | Oui | Oui |
| Retirer un lot (retrait) | Non | Oui | Oui |
| Dissocier un lot | Non | Oui | Oui |
| Voir le journal d'audit | Non | Oui | Oui |
| Voir la flotte d'agents d'impression | Non | Non | Oui |
| Gerer les postes de travail | Non | Non | Oui |
| Gerer les utilisateurs | Non | Non | Oui |
| Approuver la reinitialisation de mot de passe | Non | Non | Oui |
| Sauvegarder la configuration des codes-barres | Non | Non | Oui |

**Source :** Attributs `[Authorize]` sur les 10 controleurs, `AGENT.md` lignes 77-82

## 2. Fonctionnalites principales

### 2.1 Authentification et gestion de compte

**Objectif :** Connexion securisee avec matricule + mot de passe, protection contre la force brute, et workflow de recuperation de mot de passe.

**Fonctionnalites :**
- Connexion matricule/mot de passe (authentification par cookie)
- Verrouillage apres 5 tentatives echouees (fenetre de 15 minutes, `ConcurrentDictionary` en memoire)
- Flux de recuperation : l'employe fait une demande -> l'admin approuve -> l'employe complete dans les 15 minutes
- Changement de mot de passe force lors de la connexion de recuperation (minimum 12 caracteres)
- Invalidation par tampon de securite (changer le mot de passe ou desactiver l'utilisateur invalide toutes les sessions existantes)
- Deconnexion en POST avec protection CSRF

**Fichiers cles :**
- `MothersonBoxManagement/Controllers/AccountController.cs`
- `MothersonBoxManagement/Services/AuthenticationService.cs`
- `MothersonBoxManagement/Services/LoginLockoutService.cs`
- `MothersonBoxManagement/Services/PasswordRecoveryService.cs`

**Source :** `AGENT.md` lignes 302-317

### 2.2 Gestion du cycle de vie des cartons

**Objectif :** Creer, suivre et gerer les cartons de conditionnement a travers une machine d'etat definie.

**Fonctionnalites :**
- Creation de cartons depuis des templates (identifiants uniques BOX-YYYYMMDD-XXXXXX)
- Ouverture de cartons (transition Cree -> Ouvert)
- Fermeture automatique quand CurrentQuantity atteint ExpectedQuantity
- Operations d'exception superviseur : annulation, fermeture forcee, blocage/deblocage, archivage
- Concurrency optimiste via `RowVersion` (empeche les ecritures simultanees)
- Recherche multi-criteres (code-barres, statut, createur, plage de dates) avec pagination

**Transitions d'etat :**
```
Cree -> Ouvert -> Complete (auto)
                -> CompleteAvecException (force par superviseur)
                -> Annule (superviseur)
                -> Bloque (superviseur) -> Ouvert (deblocage)
Complete -> Archive
CompleteAvecException -> Archive
```

**Fichiers cles :**
- `MothersonBoxManagement/Services/BoxService.cs` (651 lignes, 4 interfaces)
- `MothersonBoxManagement/Controllers/BoxController.cs`
- `MothersonBoxManagement/Controllers/BoxOperationsController.cs`

**Source :** `AGENT.md` lignes 112-131

### 2.3 Scan des lots

**Objectif :** Associer les lots de cables aux cartons via le scan de codes-barres avec application de l'unicite globale.

**Fonctionnalites :**
- **Auto-scan avec correspondance de prefixe :** Le scan d'un code-barres de lot verifie tous les templates actifs pour un `PackagePrefixPattern` correspondant. Le prefixe le plus long gagne. Si correspondance : creation automatique du carton + association du lot + file d'attente d'impression.
- **Double-scan manuel :** Si aucun template ne correspond, l'operateur scanne le lot puis scanne le carton dans les 30 secondes.
- **Mode collant :** Apres la premiere association, les scans de lots suivants vont au meme carton sans rescanner le carton. Sortie a la completion du carton ou au scan d'un autre code-barres BOX.
- **Idempotence :** `RequestId` (UUID) empeche les soumissions dupliquees du meme scan.
- **Retry de concurrence :** Jusqu'a 3 tentatives sur `DbUpdateConcurrencyException`.
- **Gestion de la contrainte unique :** Capture les erreurs SQL Server 2601/2627 (violations d'index unique).
- **Retour sonore :** Tons Web Audio API pour succes, erreur, timeout et completion du carton (fanfare de 4 notes).

**Verification de validation (dans l'ordre) :**
1. Validation du format (doit etre un code-barres de lot, pas un carton)
2. Verification du statut du carton (doit etre Ouvert)
3. Verification des doublons (contrainte unique globale)
4. Verification de la quantite (quantite prevue non atteinte)
5. Si tout est valide : insertion, incrementation de la quantite, journal d'audit, auto-completion si plein

**Fichiers cles :**
- `MothersonBoxManagement/Services/PackageScanService.cs`
- `MothersonBoxManagement/wwwroot/js/scanner.js` (438 lignes)
- `MothersonBoxManagement/Controllers/BoxController.cs` (AutoScanPackage, AssociatePackage)

**Source :** `AGENT.md` lignes 62-68, 133-178

### 2.4 Gestion des templates

**Objectif :** Configurations de cartons reutilisables pour une creation standardisee.

**Fonctionnalites :**
- Operations CRUD (Superviseur/Admin uniquement)
- Champs du template : Nom, Description, Type (Carton/Bois/Plastique), Dimensions (L x l x H cm), QuantitePrevue, PackagePrefixPattern (optionnel, chiffres uniquement)
- Application de l'unicite des prefixes (index unique filtre pour les templates actifs)
- Normalisation des prefixes (trim, majuscules)
- Desactivation logique (jamais de suppression physique)
- File d'attente d'impression automatique a la creation de carton depuis un template (si le poste a une imprimante configuree)

**Fichiers cles :**
- `MothersonBoxManagement/Services/BoxTemplateService.cs`
- `MothersonBoxManagement/Controllers/BoxTemplateController.cs`

**Source :** `AGENT.md` ligne 69, `BoxTemplateService.cs`

### 2.5 Piste d'audit

**Objectif :** Journal immuable et append-only de toutes les operations pour la tracabilite.

**Fonctionnalites :**
- Capture automatique via `AuditSaveChangesInterceptor` (intercepteur EF Core `SaveChanges`)
- Types d'actions : BoxCreated, BoxOpened, BoxCancelled, BoxCompletedAuto, BoxCompletedWithException, BoxBlocked, BoxUnblocked, BoxUpdated, BoxDeleted, ExpectedQuantityUpdated, PackageScanned, PackageBlocked, PackageUnblocked, PackageRemoved, PackageDisassociated, Insert, Update, Delete
- Capture : PreviousValue, NewValue, Reason, PackageBarcode, WorkstationName, DetailsJson (diff JSON complet)
- Visualiseur en lecture seule avec filtres (ID carton, type d'action, plage de dates) et pagination
- Rendu JSON structure dans l'UI (tableau des proprietes modifiees)
- Principal SYSTEM (ID = -1) pour les operations sans contexte HTTP

**Fichiers cles :**
- `MothersonBoxManagement/Data/Interceptors/AuditSaveChangesInterceptor.cs`
- `MothersonBoxManagement/Services/AuditService.cs`
- `MothersonBoxManagement/Controllers/AuditController.cs`

**Source :** `AGENT.md` ligne 74, `AuditSaveChangesInterceptor.cs`

### 2.6 Gestion des utilisateurs

**Objectif :** Cycle de vie des comptes utilisateurs controle par l'administrateur.

**Fonctionnalites :**
- Creer un utilisateur (matricule, nom complet, role, mot de passe)
- Modifier un utilisateur (nom complet, role, statut actif)
- Reinitialiser le mot de passe (initie par l'admin)
- Desactiver un utilisateur (suppression logique, empeche l'auto-desactivation)
- Workflow d'approbation de reinitialisation de mot de passe (l'employe fait une demande, l'admin approuve, fenetre de 15 minutes)
- Protection du principal SYSTEM (ne peut pas etre modifie ou supprime)
- Regeneration du tampon de securite a chaque changement de mot de passe/profil

**Fichiers cles :**
- `MothersonBoxManagement/Controllers/UsersController.cs`
- `MothersonBoxManagement/Services/UserService.cs`

**Source :** `UsersController.cs`, `UserService.cs`

### 2.7 Systeme d'agent d'impression

**Objectif :** Impression d'etiquettes distribuee sur les postes de travail de l'usine.

**Architecture :**
- **Cote serveur :** API REST (`PrintAgentApiController`) pour l'appairage, les heartbeats, le cycle de vie des travaux
- **Cote client :** Application Windows WinForms dans la barre des taches (`MothersonBoxManagement.PrintAgent`)
- **Bibliotheque partagee :** `MothersonBoxManagement.PrintAgent.Core` (contrats, constructeur ZPL, backoff)
- **Fallback navigateur :** Page `PrintClient` pour l'impression via navigateur sans agent

**Fichiers cles :**
- `MothersonBoxManagement/Controllers/PrintAgentApiController.cs`
- `MothersonBoxManagement/Controllers/PrintAgentController.cs`
- `MothersonBoxManagement/Controllers/PrintController.cs`
- `MothersonBoxManagement.PrintAgent/` (projet entier)
- `MothersonBoxManagement.PrintAgent.Core/` (projet entier)

**Source :** `PrintAgentApiController.cs`, `PrintAgentController.cs`, fichiers du projet PrintAgent

### 2.8 Tableau de bord

**Objectif :** Page d'accueil principale avec vue d'ensemble en temps reel.

**Fonctionnalites :**
- En-tete de bienvenue avec identite utilisateur et nom de poste
- Saisie de code-barres carton (redirige vers les details du carton)
- Tableau des cartons en attente (statut Cree, avec action Ouvrir)
- Tableau des cartons ouverts actifs avec barres de progression et pagination
- Page de selection de template (route dediee `/Dashboard/Templates`)
- Page d'erreur avec affichage du code de statut

**Fichiers cles :**
- `MothersonBoxManagement/Controllers/DashboardController.cs`
- `MothersonBoxManagement/Views/Dashboard/Index.cshtml` (374 lignes)
- `MothersonBoxManagement/Views/Dashboard/Templates.cshtml`

**Source :** `DashboardController.cs`, `Dashboard/Index.cshtml`

## 3. Resume des regles metier

| ID | Regle | Application |
|----|-------|-------------|
| RG-01 | Un lot ne peut appartenir qu'a un seul carton a la fois | Index unique SQL + verification applicative |
| RG-02 | Les codes-barres carton utilisent le format BOX-YYYYMMDD-XXXXXX | `BarcodeService.GenerateUniqueBoxBarcodeAsync` |
| RG-03 | Les codes-barres de lots ne doivent pas commencer par BOX- | `BarcodeService.IsPackageBarcode` |
| RG-04 | Seuls les cartons Ouverts acceptent les scans | `PackageScanService.ScanPackageAsync` |
| RG-05 | Auto-completion quand CurrentQuantity >= ExpectedQuantity | `BoxService` + `PackageScanService` |
| RG-06 | Les dimensions doivent etre des centimets entiers strictement positifs | Contrainte CHECK SQL + validation |
| RG-07 | ExpectedQuantity doit etre > 0 | Contrainte CHECK SQL + validation |
| RG-08 | CurrentQuantity doit etre >= 0 et <= ExpectedQuantity | Contrainte CHECK SQL |
| RG-09 | Les operations superviseur necessitent une raison | Validation du controleur |
| RG-10 | Les cartons ne sont jamais physiquement supprimes | Pattern d'archivage logique |
| RG-11 | Les lots sont logiquement retires (IsRemoved), jamais physiquement supprimes | Drapeau de suppression logique |
| RG-12 | Les journaux d'audit sont append-only, jamais modifies ou supprimes | Ecritures via intercepteur uniquement |
| RG-13 | Les operateurs ne peuvent pas creer de cartons manuellement ; uniquement depuis des templates approuves | Controle d'acces base sur les roles |
| RG-14 | La correspondance de prefixe template utilise la strategie du prefixe le plus long | `BoxTemplateService.FindTemplateByPackageBarcodeAsync` |
| RG-15 | La concurrence optimiste sur les cartons empeche les ecritures simultanees | `RowVersion` + logique de retry |
| RG-16 | Invalidation du tampon de securite lors des changements de mot de passe/profil | Middleware de validation des cookies |

**Source :** `AGENT.md` lignes 61-74, implementations des services

## 4. Matrice de statut des fonctionnalites

| Fonctionnalite | Frontend | Backend | Base de donnees | Tests | Statut |
|----------------|----------|---------|-----------------|-------|--------|
| Connexion/deconnexion | Account/Login.cshtml | AccountController | Table Users | AccountControllerTests | :green_circle: Fonctionnel |
| Verrouillage de connexion | Formulaire login | LoginLockoutService | Table LoginAttempts | SecurityAuditTests | :green_circle: Fonctionnel |
| Recuperation mot de passe | Account/RequestPasswordReset, RecoveryLogin, ChangePassword | PasswordRecoveryService | Table PasswordResetRequests | PasswordRecoveryFlowTests | :green_circle: Fonctionnel |
| Tableau de bord | Dashboard/Index.cshtml | DashboardController | -- | DashboardControllerTests | :green_circle: Fonctionnel |
| Selection de template | Dashboard/Templates.cshtml | DashboardController | Table BoxTemplates | DashboardControllerTests | :green_circle: Fonctionnel |
| Creation carton depuis template | Page templates | BoxController.CreateFromTemplate | Boxes + BoxTemplates | BoxControllerTests | :green_circle: Fonctionnel |
| Recherche de cartons | Box/Index.cshtml | BoxController.Index | Table Boxes | BoxControllerTests | :green_circle: Fonctionnel |
| Details du carton | Box/Details.cshtml | BoxController.Details | Boxes + BoxPackages | BoxControllerTests | :green_circle: Fonctionnel |
| Auto-scan de lot | scanner.js | BoxController.AutoScanPackage | Boxes + BoxPackages | AutoScanPackageTests | :green_circle: Fonctionnel |
| Association manuelle | scanner.js | BoxController.AssociatePackage | BoxPackages | StickyModeTests | :green_circle: Fonctionnel |
| Mode collant | scanner.js | -- (frontend uniquement) | -- | StickyModeTests | :green_circle: Fonctionnel |
| Annulation carton | Modal details | BoxOperationsController | Boxes | SupervisorExceptionsControllerTests | :green_circle: Fonctionnel |
| Fermeture forcee | Modal details | BoxOperationsController | Boxes | SupervisorExceptionsControllerTests | :green_circle: Fonctionnel |
| Blocage/deblocage carton | Modal details | BoxOperationsController | Boxes | SupervisorExceptionsControllerTests | :green_circle: Fonctionnel |
| Transfert de lot | Modal details | BoxOperationsController | BoxPackages | BoxServiceExceptionTests | :green_circle: Fonctionnel |
| Retrait de lot | Modal details | BoxOperationsController | BoxPackages | BoxServiceExceptionTests | :green_circle: Fonctionnel |
| Dissociation de lot | Modal details | BoxOperationsController | BoxPackages | BoxServiceExceptionTests | :green_circle: Fonctionnel |
| Visualiseur d'audit | Audit/Index.cshtml | AuditController | BoxAuditLogs | AuditControllerTests | :green_circle: Fonctionnel |
| Gestion utilisateurs | Users/*.cshtml | UsersController | Table Users | -- | :yellow_circle: Pas de tests dedies |
| CRUD templates | BoxTemplate/*.cshtml | BoxTemplateController | BoxTemplates | TemplateRemediationTests | :green_circle: Fonctionnel |
| Appairage agent impression | Page Settings | PrintAgentApiController | PrinterConfigurations | PrintAgentApiTests | :green_circle: Fonctionnel |
| Flotte agent impression | PrintAgent/Workstations | PrintAgentController | PrinterConfigurations | PrintAgentAdminTests | :green_circle: Fonctionnel |
| Impression navigateur | Box/Print.cshtml | PrintController | -- | WebPrintingOnlyTests | :green_circle: Fonctionnel |
| Identite de poste | station-terminal.js | WorkstationResolver | -- | WorkstationResolverTests | :green_circle: Fonctionnel |
| Config codes-barres | Box/Settings.cshtml | BoxController | BarcodeConfigurations | -- | :yellow_circle: Pas de tests dedies |
| Simulateur de scan | Dashboard JS inline | -- | -- | -- | :yellow_circle: Manuel uniquement |
