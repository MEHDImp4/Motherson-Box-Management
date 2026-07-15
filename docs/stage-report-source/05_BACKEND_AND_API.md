# 05_BACKEND_AND_API.md -- Analyse du backend et de l'API

## 1. Inventaire des controleurs

### 1.1 AccountController

**Fichier :** `MothersonBoxManagement/Controllers/AccountController.cs`
**Responsabilite :** Flux d'authentification (connexion, deconnexion, recuperation de mot de passe)
**Dependances :** `IUserAuthenticationService`, `ILoginLockoutService`, `IPasswordRecoveryService`

| Action | HTTP | Auth | But |
|--------|------|------|-----|
| `Login(string?)` | GET | Anonyme | Afficher le formulaire de connexion |
| `Login(LoginViewModel, CancellationToken)` | POST | Anonyme | Authentifier l'utilisateur |
| `RequestPasswordReset(bool)` | GET | Anonyme | Afficher le formulaire de demande de reinitialisation |
| `RequestPasswordReset(PasswordResetRequestViewModel, CancellationToken)` | POST | Anonyme | Soumettre la demande de reinitialisation |
| `RecoveryLogin()` | GET | Anonyme | Afficher le formulaire de connexion de recuperation |
| `RecoveryLogin(RecoveryLoginViewModel, CancellationToken)` | POST | Anonyme | Demarrer la session de recuperation |
| `ChangePassword()` | GET | Autorise | Afficher le changement de mot de passe force |
| `ChangePassword(ForcedPasswordChangeViewModel, CancellationToken)` | POST | Autorise | Completer le changement de mot de passe |
| `Logout()` | POST | Autorise + CSRF | Deconnexion |

### 1.2 AuditController

**Fichier :** `MothersonBoxManagement/Controllers/AuditController.cs`
**Responsabilite :** Visualisation en lecture seule du journal d'audit
**Dependances :** `IAuditService`
**Autorisation :** `[Authorize(Roles = SupervisorFr, AdminFr, Supervisor, Administrator)]`

| Action | HTTP | But |
|--------|------|-----|
| `Index(boxId?, actionType?, fromDate?, toDate?, page, pageSize)` | GET | Journal d'audit pagine et filtre |

### 1.3 BoxController

**Fichier :** `MothersonBoxManagement/Controllers/BoxController.cs`
**Responsabilite :** Cycle de vie principal des boites -- liste, recherche, creation, scan, parametres
**Dependances :** `IBoxService`, `IBoxTemplateService`, `IPackageScanService`, `IBarcodeService`, `IWorkstationResolver`, `IQrCodeService`, `IUserService`, `ApplicationDbContext`

| Action | HTTP | Route | Auth | But |
|--------|------|-------|------|-----|
| `Index(BoxSearchFilterDto)` | GET | `/Box` | Autorise | Rechercher/lister les boites |
| `CreateFromTemplate(templateId, workstationName)` | POST | `/Box/CreateFromTemplate` | Autorise + CSRF | Creer une boite a partir d'un modele |
| `Details(barcode)` | GET | `/Box/Details/{barcode}` | Autorise | Voir les details de la boite |
| `Open(boxId, boxBarcode)` | POST | `/Box/Open` | Superviseur/Admin + CSRF | Ouvrir une boite Created |
| `AutoScanPackage(packageBarcode, workstationName?, requestId?)` | POST | `/Box/AutoScanPackage` | Autorise + CSRF + Limite | Auto-creation de boite + scan de colis |
| `AssociatePackage(packageBarcode, boxBarcode, workstationName?, requestId?)` | POST | `/Box/AssociatePackage` | Autorise + CSRF + Limite | Scanner un colis dans une boite existante |
| `Settings()` | GET | `/Box/Settings` | Autorise | Parametres du poste de travail |
| `SaveBarcodeConfig(...)` | POST | `/Box/SaveBarcodeConfig` | Admin + CSRF | Enregistrer la configuration des codes-barres |
| `SearchPackageIndex(query?)` | GET | `/Box/SearchPackage` | Autorise | Page de recherche de colis |
| `SearchPackage(barcode)` | POST | `/Box/SearchPackage` | Autorise + CSRF | Rechercher une boite par colis |

### 1.4 BoxOperationsController

**Fichier :** `MothersonBoxManagement/Controllers/BoxOperationsController.cs`
**Responsabilite :** Mutations de boites et colis par le Superviseur/Admin
**Autorisation :** `[Authorize]` + `[Authorize(Roles = SupervisorFr, AdminFr, Supervisor, Administrator)]`

| Action | HTTP | Route | But |
|--------|------|-------|-----|
| `CancelBox(...)` | POST | `/Box/CancelBox` | Annuler une boite (motif obligatoire) |
| `ForceCloseBox(...)` | POST | `/Box/ForceCloseBox` | Fermeture forcee avec exception (motif obligatoire) |
| `ModifyExpectedQuantity(...)` | POST | `/Box/ModifyExpectedQuantity` | Modifier la quantite attendue (motif obligatoire) |
| `BlockBox(...)` | POST | `/Box/BlockBox` | Mettre en quarantaine une boite (motif obligatoire) |
| `UnblockBox(...)` | POST | `/Box/UnblockBox` | Lever la quarantaine (motif obligatoire) |
| `ArchiveBox(...)` | POST | `/Box/ArchiveBox` | Archiver une boite terminee (motif obligatoire) |
| `BlockPackage(...)` | POST | `/Box/BlockPackage` | Bloquer un colis (motif obligatoire) |
| `UnblockPackage(...)` | POST | `/Box/UnblockPackage` | Debloquer un colis (motif obligatoire) |
| `TransferPackage(...)` | POST | `/Box/TransferPackage` | Transferer un colis entre boites (motif obligatoire) |
| `RetraitPackage(...)` | POST | `/Box/RetraitPackage` | Retirer un colis de la boite (motif obligatoire) |
| `DisassociatePackage(...)` | POST | `/Box/DisassociatePackage` | Liberer un colis d'une boite annulee (motif obligatoire) |

### 1.5 BoxTemplateController

**Fichier :** `MothersonBoxManagement/Controllers/BoxTemplateController.cs`
**Responsabilite :** CRUD des modeles
**Autorisation :** `[Authorize(Roles = Supervisor, SupervisorFr, Administrator, AdminFr)]`

| Action | HTTP | But |
|--------|------|-----|
| `Index(page, pageSize)` | GET | Liste paginee des modeles |
| `Create()` | GET | Formulaire de creation de modele |
| `Create(BoxTemplateViewModel)` | POST | Enregistrer un nouveau modele |
| `Edit(id)` | GET | Formulaire d'edition de modele |
| `Edit(id, BoxTemplateViewModel)` | POST | Mettre a jour le modele |
| `Deactivate(id)` | POST | Desactiver le modele (soft-delete) |

### 1.6 DashboardController

**Fichier :** `MothersonBoxManagement/Controllers/DashboardController.cs`
**Responsabilite :** Tableau de bord principal, selection de modeles, page d'erreur
**Dependances :** `IBoxService`, `IBarcodeService`, `IBoxTemplateService`

| Action | HTTP | Auth | But |
|--------|------|------|-----|
| `Index(page, pageSize)` | GET | Autorise | Tableau de bord avec boites ouvertes/creees |
| `Templates()` | GET | Autorise | Page de selection de modeles |
| `Index(HomeViewModel)` | POST | Autorise + CSRF | Recherche de code-barres depuis le tableau de bord |
| `Error(statusCode?)` | GET | Anonyme | Page d'erreur |

### 1.7 PrintAgentApiController (REST API)

**Fichier :** `MothersonBoxManagement/Controllers/PrintAgentApiController.cs`
**Responsabilite :** REST API pour l'agent d'impression de bureau
**Autorisation :** Bearer token (personnalise, via `IPrintAgentService.AuthenticateAsync`)
**Limitation de debit :** `[EnableRateLimiting("print-agent")]`

| Action | HTTP | Route | Auth | But |
|--------|------|-------|------|-----|
| `Pair(PairAgentRequest)` | POST | `/api/print-agent/pair` | Code d'appariement | Apparier l'agent au poste de travail |
| `Heartbeat(HeartbeatRequest)` | POST | `/api/print-agent/heartbeat` | Bearer token | Signaler l'etat de l'agent |
| `Next()` | GET | `/api/print-agent/jobs/next` | Bearer token | Reclamer le prochain travail d'impression |
| `Complete(jobId, LeaseRequest)` | POST | `/api/print-agent/jobs/{jobId}/complete` | Bearer token | Marquer le travail comme termine |
| `Fail(jobId, FailJobRequest)` | POST | `/api/print-agent/jobs/{jobId}/fail` | Bearer token | Marquer le travail comme echoue |

### 1.8 PrintAgentController (MVC)

**Fichier :** `MothersonBoxManagement/Controllers/PrintAgentController.cs`
**Responsabilite :** Interface de gestion de la flotte d'agents d'impression
**Autorisation :** Diverse (Authentifie, Admin, Superviseur/Admin)

| Action | HTTP | Auth | But |
|--------|------|------|-----|
| `Status(workstationName)` | GET | Autorise | Statut du poste (JSON) |
| `History(workstationName, page, pageSize)` | GET | Admin | Historique des travaux d'impression |
| `Fleet()` | GET | Admin | Vue d'ensemble de la flotte (JSON) |
| `Workstations()` | GET | Admin | Vue de gestion des postes |
| `ConfigureWorkstation(id, ...)` | POST | Admin + CSRF | Configurer le poste |
| `PairingCode(workstationName)` | POST | Superviseur/Admin + CSRF | Generer un code d'appariement |
| `Configure(workstationName, printerName, printMode)` | POST | Superviseur/Admin + CSRF | Configurer l'imprimante |
| `Revoke(workstationName)` | POST | Superviseur/Admin + CSRF | Revoquer le jeton de l'agent |
| `Retry(jobId)` | POST | Admin + CSRF | Reessayer un travail d'impression echoue |
| `Download()` | GET | Superviseur/Admin | Telecharger l'installateur de l'agent |

### 1.9 PrintController

**Fichier :** `MothersonBoxManagement/Controllers/PrintController.cs`
**Responsabilite :** Impression d'etiquettes

| Action | HTTP | Auth | But |
|--------|------|------|-----|
| `Print(barcode)` | GET | Superviseur/Admin | Vue d'impression d'etiquette cote serveur |
| `PrintClient(barcode, autoPrint?, returnUrl?)` | GET | Autorise | Impression navigateur avec auto-impression |
| `Queue(barcode, workstationName?)` | POST | Autorise + CSRF | Mettre en file d'attente vers le poste |

### 1.10 UsersController

**Fichier :** `MothersonBoxManagement/Controllers/UsersController.cs`
**Responsabilite :** Gestion des utilisateurs
**Autorisation :** `[Authorize(Roles = AdminFr, Administrator)]`

| Action | HTTP | But |
|--------|------|-----|
| `Index()` | GET | Lister tous les utilisateurs |
| `Create()` | GET | Formulaire de creation d'utilisateur |
| `Create(CreateUserViewModel)` | POST | Creer un utilisateur |
| `Edit(id)` | GET | Formulaire d'edition d'utilisateur |
| `Edit(EditUserViewModel)` | POST | Mettre a jour l'utilisateur |
| `ResetPassword(id)` | GET | Formulaire de reinitialisation de mot de passe |
| `ResetPassword(ResetPasswordViewModel)` | POST | Reinitialiser le mot de passe |
| `PasswordResetRequests()` | GET | Demandes de reinitialisation en attente |
| `ApprovePasswordReset(requestId)` | POST | Approuver la demande de reinitialisation |
| `Deactivate(id)` | POST | Desactiver l'utilisateur |

## 2. Inventaire des services

### 2.1 BoxService (651 lignes, 4 interfaces)

**Fichier :** `MothersonBoxManagement/Services/BoxService.cs`
**Interfaces :** `IBoxService`, `IBoxQueryService`, `IBoxLifecycleService`, `IBoxPackageService`
**Dependances :** `ApplicationDbContext`, `IBarcodeService`

Methodes principales :
- `CreateBoxAsync` -- Creer une boite avec un code-barres genere
- `GetOpenBoxesAsync` / `GetCreatedBoxesAsync` -- Lister les boites par statut
- `SearchBoxesAsync` -- Recherche multi-filtres avec pagination
- `GetBoxByBarcodeAsync` / `GetBoxByIdAsync` -- Details complets de la boite
- `FindBoxByPackageBarcodeAsync` -- Trouver la boite contenant un colis
- `OpenBoxAsync` -- Transition Created -> Open
- `CancelBoxAsync` / `ForceCloseBoxAsync` -- Operations d'exception
- `BlockBoxAsync` / `UnblockBoxAsync` -- Operations de quarantaine
- `ArchiveBoxAsync` -- Archiver les boites terminees
- `UpdateExpectedQuantityAsync` -- Modifier la quantite avec auto-completion
- `TransferPackageAsync` -- Deplacer un colis entre boites
- `RetraitPackageAsync` / `DisassociatePackageAsync` -- Suppression de colis
- `BlockPackageAsync` / `UnblockPackageAsync` -- Quarantaine de colis

**Regles metier :** Reprise par concurrence optimiste (3 tentatives), application de la machine a etats, integrite des quantites, auto-completion, aucune suppression physique.

### 2.2 PackageScanService

**Fichier :** `MothersonBoxManagement/Services/PackageScanService.cs`
**Dependances :** `ApplicationDbContext`, `IBarcodeService`, `IAuditService`, `IBoxService`

Methode principale : `ScanPackageAsync(boxId, barcode, userId, workstationName, cancellationToken, requestId?)`

**Regles metier :** Idempotence via requestId, detection de doublons, verification de colis bloque, verification du statut de la boite, verification de quantite, reprise par concurrence (3 tentatives), gestion de la contrainte d'unicite (erreur SQL 2601/2627), auto-completion, journalisation d'audit pour les rejets.

### 2.3 Autres services

| Service | Fichier | Responsabilite principale |
|---------|---------|--------------------------|
| `BoxTemplateService` | `BoxTemplateService.cs` | CRUD des modeles, creation de boites a partir d'un modele, correspondance de prefixe (le plus long gagne) |
| `AuthenticationService` | `AuthenticationService.cs` | Validation des identifiants (retourne null en cas d'echec, pas d'enumeration) |
| `UserService` | `UserService.cs` | CRUD des utilisateurs, protection du principal SYSTEM, regeneration du jeton de securite |
| `AuditService` | `AuditService.cs` | Journalisation des rejets, requetes d'audit paginees |
| `BarcodeService` | `BarcodeService.cs` | Generation de codes-barres (BOX-YYYYMMDD-XXXXXX), validation du format, classification boite vs. colis |
| `QrCodeService` | `QrCodeService.cs` | Generation de QR codes en PNG (bibliotheque QRCoder, niveau ECC M) |
| `WorkstationResolver` | `WorkstationResolver.cs` | Chaine de resolution du nom du poste (client -> config -> MachineName) |
| `LoginLockoutService` | `LoginLockoutService.cs` | Verrouillage contre la force brute (ConcurrentDictionary, SemaphoreSlim par matricule, transactions serialisables) |
| `LoginLockoutCleanupService` | `LoginLockoutCleanupService.cs` | Nettoyage en arriere-plan des verrouillages expires (toutes les 5 minutes) |
| `PasswordRecoveryService` | `PasswordRecoveryService.cs` | Flux de reinitialisation de mot de passe (demande -> approbation -> debut -> completion) |
| `PrintAgentService` | `PrintAgentService.cs` | Gestion des travaux d'impression, appariement, authentification |

## 3. Inventaire complet de l'API

### Points d'entree MVC (Auth par cookie)

| Methode | Point d'entree | Controleur | Auth | Roles | But |
|---------|---------------|------------|------|-------|-----|
| GET | `/Account/Login` | Account | Non | -- | Formulaire de connexion |
| POST | `/Account/Login` | Account | Non | -- | Authentifier |
| GET | `/Account/RequestPasswordReset` | Account | Non | -- | Formulaire de demande de reinitialisation |
| POST | `/Account/RequestPasswordReset` | Account | Non | -- | Soumettre la demande |
| GET | `/Account/RecoveryLogin` | Account | Non | -- | Formulaire de connexion de recuperation |
| POST | `/Account/RecoveryLogin` | Account | Non | -- | Demarrer la recuperation |
| GET | `/Account/ChangePassword` | Account | Oui | Tout | Formulaire de changement de mot de passe |
| POST | `/Account/ChangePassword` | Account | Oui | Tout | Completer le changement |
| POST | `/Account/Logout` | Account | Oui | Tout | Deconnexion |
| GET | `/Audit` | Audit | Oui | Superviseur/Admin | Journal d'audit |
| GET | `/Box` | Box | Oui | Tout | Recherche de boites |
| POST | `/Box/CreateFromTemplate` | Box | Oui | Tout | Creer a partir d'un modele |
| GET | `/Box/Details/{barcode}` | Box | Oui | Tout | Details de la boite |
| POST | `/Box/Open` | Box | Oui | Superviseur/Admin | Ouvrir une boite |
| POST | `/Box/AutoScanPackage` | Box | Oui | Tout | Auto-scan |
| POST | `/Box/AssociatePackage` | Box | Oui | Tout | Association manuelle |
| GET | `/Box/Settings` | Box | Oui | Tout | Parametres |
| POST | `/Box/SaveBarcodeConfig` | Box | Oui | Admin | Enregistrer la config des codes-barres |
| GET | `/Box/SearchPackage` | Box | Oui | Tout | Recherche de colis |
| POST | `/Box/SearchPackage` | Box | Oui | Tout | Executer la recherche |
| POST | `/Box/CancelBox` | BoxOps | Oui | Superviseur/Admin | Annuler |
| POST | `/Box/ForceCloseBox` | BoxOps | Oui | Superviseur/Admin | Fermeture forcee |
| POST | `/Box/ModifyExpectedQuantity` | BoxOps | Oui | Superviseur/Admin | Modifier la quantite |
| POST | `/Box/BlockBox` | BoxOps | Oui | Superviseur/Admin | Bloquer |
| POST | `/Box/UnblockBox` | BoxOps | Oui | Superviseur/Admin | Debloquer |
| POST | `/Box/ArchiveBox` | BoxOps | Oui | Superviseur/Admin | Archiver |
| POST | `/Box/BlockPackage` | BoxOps | Oui | Superviseur/Admin | Bloquer un colis |
| POST | `/Box/UnblockPackage` | BoxOps | Oui | Superviseur/Admin | Debloquer un colis |
| POST | `/Box/TransferPackage` | BoxOps | Oui | Superviseur/Admin | Transferer |
| POST | `/Box/RetraitPackage` | BoxOps | Oui | Superviseur/Admin | Retirer |
| POST | `/Box/DisassociatePackage` | BoxOps | Oui | Superviseur/Admin | Dissocier |
| GET | `/BoxTemplate` | Template | Oui | Superviseur/Admin | Liste des modeles |
| GET | `/BoxTemplate/Create` | Template | Oui | Superviseur/Admin | Formulaire de creation |
| POST | `/BoxTemplate/Create` | Template | Oui | Superviseur/Admin | Enregistrer le modele |
| GET | `/BoxTemplate/Edit/{id}` | Template | Oui | Superviseur/Admin | Formulaire d'edition |
| POST | `/BoxTemplate/Edit/{id}` | Template | Oui | Superviseur/Admin | Mettre a jour le modele |
| POST | `/BoxTemplate/Deactivate/{id}` | Template | Oui | Superviseur/Admin | Desactiver |
| GET | `/Dashboard` | Dashboard | Oui | Tout | Tableau de bord |
| GET | `/Dashboard/Templates` | Dashboard | Oui | Tout | Selection de modeles |
| POST | `/Dashboard` | Dashboard | Oui | Tout | Recherche de codes-barres |
| GET | `/Dashboard/Error` | Dashboard | Non | -- | Page d'erreur |
| GET | `/PrintAgent/Status` | PrintAgent | Oui | Tout | Statut (JSON) |
| GET | `/PrintAgent/History` | PrintAgent | Oui | Admin | Historique |
| GET | `/PrintAgent/Fleet` | PrintAgent | Oui | Admin | Flotte (JSON) |
| GET | `/PrintAgent/Workstations` | PrintAgent | Oui | Admin | Vue de la flotte |
| POST | `/PrintAgent/Workstations/{id}/Configure` | PrintAgent | Oui | Admin | Configurer |
| POST | `/PrintAgent/PairingCode` | PrintAgent | Oui | Superviseur/Admin | Code d'appariement |
| POST | `/PrintAgent/Configure` | PrintAgent | Oui | Superviseur/Admin | Configurer l'imprimante |
| POST | `/PrintAgent/Revoke` | PrintAgent | Oui | Superviseur/Admin | Revoquer |
| POST | `/PrintAgent/Retry/{jobId}` | PrintAgent | Oui | Admin | Reessayer le travail |
| GET | `/Downloads/PrintAgent` | PrintAgent | Oui | Superviseur/Admin | Telecharger l'agent |
| GET | `/Box/Print/{barcode}` | Print | Oui | Superviseur/Admin | Impression serveur |
| GET | `/Box/PrintClient/{barcode}` | Print | Oui | Tout | Impression navigateur |
| POST | `/Print/Queue/{barcode}` | Print | Oui | Tout | File d'attente d'impression |
| GET | `/Users` | Users | Oui | Admin | Liste des utilisateurs |
| GET | `/Users/Create` | Users | Oui | Admin | Formulaire de creation |
| POST | `/Users/Create` | Users | Oui | Admin | Creer un utilisateur |
| GET | `/Users/Edit/{id}` | Users | Oui | Admin | Formulaire d'edition |
| POST | `/Users/Edit/{id}` | Users | Oui | Admin | Mettre a jour l'utilisateur |
| GET | `/Users/ResetPassword/{id}` | Users | Oui | Admin | Formulaire de reinitialisation |
| POST | `/Users/ResetPassword/{id}` | Users | Oui | Admin | Reinitialiser le mot de passe |
| GET | `/Users/PasswordResetRequests` | Users | Oui | Admin | Demandes en attente |
| POST | `/Users/ApprovePasswordReset` | Users | Oui | Admin | Approuver la demande |
| POST | `/Users/Deactivate/{id}` | Users | Oui | Admin | Desactiver |

### Points d'entree REST API (Bearer Token)

| Methode | Point d'entree | Controleur | Auth | But |
|---------|---------------|------------|------|-----|
| POST | `/api/print-agent/pair` | PrintAgentApi | Code d'appariement | Apparier l'agent |
| POST | `/api/print-agent/heartbeat` | PrintAgentApi | Bearer token | Signaler l'etat |
| GET | `/api/print-agent/jobs/next` | PrintAgentApi | Bearer token | Reclamer un travail |
| POST | `/api/print-agent/jobs/{jobId}/complete` | PrintAgentApi | Bearer token | Terminer le travail |
| POST | `/api/print-agent/jobs/{jobId}/fail` | PrintAgentApi | Bearer token | Echouer le travail |

### Points d'entree de sante (Anonyme)

| Methode | Point d'entree | But |
|---------|---------------|-----|
| GET | `/health/live` | Verification de vivacite |
| GET | `/health` | Verification de disponibilite (connexion DB) |
| GET | `/health/ready` | Verification de disponibilite (alias) |

**Source :** Tous les fichiers de controleurs, `Program.cs` lines 120-136
