# 07_AUTH_AND_SECURITY.md -- Authentification, Autorisation et Securite

## 1. Mecanismes d'authentification

### 1.1 Authentification par cookie (MVC)

- **Schema :** `CookieAuthenticationDefaults.AuthenticationScheme`
- **Nom du cookie :** Configurable via `Authentication__CookieName` (par defaut : `Motherson.BoxManagement.Auth`)
- **Expiration :** Configurable via `Authentication__ExpireTimeSpanMinutes` (par defaut : 60 minutes)
- **HttpOnly :** Oui
- **SameSite :** Lax
- **Politique de securite :** `Always` en Production, `SameAsRequest` en Developpement
- **Expiration glissante :** Activee

**Claims stockes dans le cookie :**
- `ClaimTypes.NameIdentifier` (ID utilisateur)
- `ClaimTypes.Name` (Matricule)
- `ClaimTypes.Role` (Chaine de role)
- `FullName` (Nom d'affichage)
- `Matricule` (Duplique pour commodite)
- `SecurityStamp` (Validation de session)
- `MustChangePassword` (Indicateur de session de recuperation, optionnel)
- `PasswordResetRequestId` (Session de recuperation, optionnel)

**Source :** `MothersonBoxManagement/Configuration/AuthenticationConfiguration.cs`, `AccountController.cs`

### 1.2 Authentification par Bearer Token (API Print Agent)

- **Implementation personnalisee** (pas le bearer ASP.NET Identity)
- Token genere lors de l'appairage, stocke sous forme de hash sur le serveur (`PrinterConfiguration.AgentTokenHash`)
- Le client stocke le token chiffre par DPAPI dans `%LOCALAPPDATA%\Motherson\PrintAgent\agent.json`
- Valide via `IPrintAgentService.AuthenticateAsync` a chaque requete
- Token extrait de l'en-tete `Authorization: Bearer {token}`

**Source :** `PrintAgentApiController.cs`, `PrintAgentService.cs`

### 1.3 Stockage des mots de passe

- **Hasher :** `IPasswordHasher<User>` d'ASP.NET Core Identity
- **Algorithme :** PBKDF2 (format v3 par defaut d'ASP.NET Core)
- **Stockage :** Colonne `Users.PasswordHash` (`nvarchar(max)`)
- **Aucun mot de passe en clair** stocke ou journalise

**Source :** `MothersonBoxManagement/Services/AuthenticationService.cs`, `UserService.cs`

### 1.4 Tampon de securite (Security Stamp)

- Genere sous forme de `Guid.NewGuid().ToString("N")` a la creation de l'utilisateur
- Regenere a chaque : changement de mot de passe, mise a jour du profil, reinitialisation du mot de passe, desactivation de l'utilisateur
- Stocke en tant que claim dans le cookie d'authentification
- Valide a chaque requete par le middleware : si le tampon du cookie != tampon de la base de donnees, la session est rejetee
- Rejette egalement les utilisateurs inactifs (`IsActive = false`)

**Source :** `MothersonBoxManagement/Configuration/AuthenticationConfiguration.cs`, `Program.cs` lignes 101-112

## 2. Autorisation

### 2.1 Systeme de roles

| Constante | Valeur | Variante francaise | Utilisation |
|-----------|--------|-------------------|-------------|
| `AppRoles.Operator` | `"Operator"` | -- | Privilege le plus bas |
| `AppRoles.Supervisor` | `"Supervisor"` | `AppRoles.SupervisorFr` = `"Superviseur"` | Gestion du cycle de vie des boites |
| `AppRoles.Administrator` | `"Administrator"` | `AppRoles.AdminFr` = `"Admin"` | Acces complet |
| `AppRoles.AdministratorOnly` | `"Admin,Administrator"` | -- | Composite pour admin uniquement |
| `AppRoles.SupervisorOrAdministrator` | `"Superviseur,Admin,Supervisor,Administrator"` | -- | Composite pour superviseur+ |

**Source :** `MothersonBoxManagement/Security/AppRoles.cs`

### 2.2 Politiques d'autorisation

| Politique | Roles | Utilisation |
|-----------|-------|-------------|
| `SupervisorOrAdministrator` | Supervisor, Superviseur, Administrator, Admin | Operations sur les boites, impression, gestion des modeles |

**Source :** `Program.cs` lignes 40-44

### 2.3 Application du controle d'acces base sur les roles

- **Niveau controleur :** Attributs `[Authorize(Roles = "...")]` sur les controleurs et les actions
- **Niveau vue :** Verifications `User.IsInRole()` pour les elements UI conditionnels
- **Niveau service :** La logique metier ne reverifie pas les roles (fait confiance a la couche controleur)
- **Niveau middleware :** Le claim `MustChangePassword` force la redirection vers le changement de mot de passe

**Source :** Tous les fichiers de controleurs, `_Layout.cshtml`, `Program.cs` lignes 101-112

## 3. Mecanismes de securite

### 3.1 Verrouillage de connexion

- **Service :** `LoginLockoutService` (Singleton, `ConcurrentDictionary` en memoire)
- **Tentatives maximales :** 5 (configurable via `Security:LoginLockout:MaxAttempts`)
- **Duree de verrouillage :** 15 minutes (configurable via `Security:LoginLockout:LockoutMinutes`)
- **Controle de concurrence :** `SemaphoreSlim` par matricule pour eviter les conditions de course
- **Transactions serialisables :** `RecordFailedAttemptAsync` utilise `IsolationLevel.Serializable`
- **Nettoyage :** Service d'arriere-plan (`LoginLockoutCleanupService`) execute toutes les 5 minutes
- **Suivi :** Table `LoginAttempts` avec index unique sur Matricule

**Source :** `MothersonBoxManagement/Services/LoginLockoutService.cs`, `LoginLockoutCleanupService.cs`

### 3.2 Limitation de debit

Trois politiques de limitation de debit integrees a ASP.NET Core :

| Politique | Type | Limite | Objectif |
|-----------|------|--------|----------|
| `login` | Token Bucket | 5/minute par matricule | Prevenir le bourrage de credentials |
| `scan` | Fenetre fixe | 60/minute par utilisateur | Prevenir le flooding de scans |
| `global` | Fenetre fixe | 200/minute | Plafond global de requetes |
| `print-agent` | (configure) | -- | Protection de l'API print agent |

**Source :** `MothersonBoxManagement/Configuration/RateLimitingConfiguration.cs`

### 3.3 Protection CSRF

- **Configuration :** `builder.Services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN"; })`
- **Application :** `[ValidateAntiForgeryToken]` sur toutes les actions POST modifiant l'etat
- **Distribution du token :** Integre dans la balise `<meta name="csrf-token">` dans `_Layout.cshtml`
- **JavaScript :** Les appels `fetch()` incluent l'en-tete `X-CSRF-TOKEN` depuis la balise meta

**Source :** `Program.cs` ligne 39, toutes les actions POST des controleurs, `scanner.js`

### 3.4 En-tetes de securite

Le middleware ajoute a chaque reponse :
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `X-XSS-Protection: 0` (les navigateurs modernes n'ont pas besoin de filtres XSS)
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`

**Source :** `MothersonBoxManagement/Configuration/SecurityHeadersConfiguration.cs`

### 3.5 HTTPS

- **Conditionnel :** Applique uniquement lorsque le port HTTPS est configure (preserve la compatibilite des tests)
- **HSTS :** Applique dans les environnements autres que Developpement
- **Politique de securite du cookie :** `Always` en Production, `SameAsRequest` en Developpement

**Source :** `Program.cs` lignes 85-89

### 3.6 Protection des donnees

- Cles persistees sur le systeme de fichiers (`/keys` dans Docker)
- Production : cles protegees par un certificat X.509
- Nom de l'application : `MothersonBoxManagement`

**Source :** `Program.cs` lignes 26-38

### 3.7 Securite de la recuperation de mot de passe

- L'employe soumet une demande (matricule uniquement, pas d'enumeration)
- L'admin approuve (fenetre de 15 minutes)
- L'approbation invalide le mot de passe actuel (genere un hash aleatoire)
- L'employe se connecte sans mot de passe (session de recuperation)
- Changement de mot de passe force (minimum 12 caracteres)
- Tampon de securite regenere a la fin
- Les transactions serialisables evitent les conditions de course

**Source :** `MothersonBoxManagement/Services/PasswordRecoveryService.cs`

## 4. Resultats de l'audit de securite

### 4.1 Modele de menaces STRIDE de la phase 3 (docs/SECURITY.md)

11 menaces identifiees, 10 fermees, 1 ouverte (D-1 : DoS, faible severite, non bloquant).

### 4.2 Audit du code source (2026-07-08)

| ID | Severite | Constat | Statut |
|----|----------|---------|--------|
| SEC-001 | Haute | Creation/ouverture/impression de boite non restreinte par role | Corrige |
| SEC-002 | Haute | Mot de passe seed partage | Corrige (mots de passe externes distincts) |
| SEC-003 | Moyenne | PasswordHash dans les journaux d'audit | Corrige (liste autorisee + test) |
| SEC-004 | Moyenne | Pas de revoquation de cookie | Corrige (SecurityStamp) |
| VULN-01 | Critique | Mot de passe seed statique dans DbInitializer | Ouvert (necessite un flux de premiere connexion) |
| VULN-02 | Haute | Pas de protection contre la force brute | Corrige (LoginLockoutService) |
| VULN-09 | Basse | Pas d'expiration/rotation de mot de passe | Ouvert |

**Source :** `AGENT.md` lignes 302-341, `docs/CODEBASE_AUDIT_REPORT.md`

### 4.3 Audit de preparation a la production (2026-07-13)

| ID | Priorite | Constat | Statut |
|----|----------|---------|--------|
| SEC-001 | P1 | Identifiants de demo dans le depot | Ferme (externes, distincts) |
| SEC-002 | P1 | PasswordHash serialise dans les journaux d'audit | Ferme (liste autorisee + test) |
| SEC-003 | P1 | Verrouillage de connexion non atomique en concurrence | Ferme (section serialisable) |

**Source :** `docs/production-readiness-audit/03-SECURITY-AUDIT.md`

## 5. Matrice de permissions (detaillee)

| Ressource / Action | Operateur | Superviseur | Administrateur | Emplacement du controle |
|--------------------|-----------|-------------|----------------|------------------------|
| Connexion | Oui | Oui | Oui | AccountController (AllowAnonymous) |
| Voir le tableau de bord | Oui | Oui | Oui | DashboardController [Authorize] |
| Scanner une boite depuis le tableau de bord | Oui | Oui | Oui | DashboardController POST |
| Creer une boite a partir d'un modele | Oui | Oui | Oui | BoxController.CreateFromTemplate |
| Voir les details d'une boite | Oui | Oui | Oui | BoxController.Details |
| Rechercher des boites | Oui | Oui | Oui | BoxController.Index |
| Rechercher des colis | Oui | Oui | Oui | BoxController.SearchPackage |
| Configurer le poste de travail | Oui | Oui | Oui | BoxController.Settings |
| Impression navigateur | Oui | Oui | Oui | PrintController.PrintClient |
| Scan automatique de colis | Oui | Oui | Oui | BoxController.AutoScanPackage |
| Associer un colis | Oui | Oui | Oui | BoxController.AssociatePackage |
| Ouvrir une boite Creee | Non | Oui | Oui | BoxController.Open [SupervisorOrAdministrator] |
| Impression serveur | Non | Oui | Oui | PrintController.Print [SupervisorOrAdministrator] |
| CRUD modeles | Non | Oui | Oui | BoxTemplateController [Supervisor/Admin] |
| Annuler une boite | Non | Oui | Oui | BoxOperationsController [Supervisor/Admin] |
| Forcer la fermeture d'une boite | Non | Oui | Oui | BoxOperationsController [Supervisor/Admin] |
| Modifier la quantite | Non | Oui | Oui | BoxOperationsController [Supervisor/Admin] |
| Bloquer/debloquer une boite | Non | Oui | Oui | BoxOperationsController [Supervisor/Admin] |
| Archiver une boite | Non | Oui | Oui | BoxOperationsController [Supervisor/Admin] |
| Transferer un colis | Non | Oui | Oui | BoxOperationsController [Supervisor/Admin] |
| Retirer un colis | Non | Oui | Oui | BoxOperationsController [Supervisor/Admin] |
| Dissocier un colis | Non | Oui | Oui | BoxOperationsController [Supervisor/Admin] |
| Bloquer/debloquer un colis | Non | Oui | Oui | BoxOperationsController [Supervisor/Admin] |
| Voir le journal d'audit | Non | Oui | Oui | AuditController [Supervisor/Admin] |
| Generer un code d'appairage | Non | Oui | Oui | PrintAgentController [Supervisor/Admin] |
| Configurer l'imprimante | Non | Oui | Oui | PrintAgentController [Supervisor/Admin] |
| Revoquer un agent | Non | Oui | Oui | PrintAgentController [Supervisor/Admin] |
| Telecharger un agent | Non | Oui | Oui | PrintAgentController [Supervisor/Admin] |
| Voir la flotte | Non | Non | Oui | PrintAgentController [Admin] |
| Gerer les postes de travail | Non | Non | Oui | PrintAgentController [Admin] |
| Gerer les utilisateurs | Non | Non | Oui | UsersController [Admin] |
| Approuver la reinitialisation de mot de passe | Non | Non | Oui | UsersController [Admin] |
| Sauvegarder la config code-barres | Non | Non | Oui | BoxController.SaveBarcodeConfig [Admin] |

## 6. Limitations de securite connues

| Element | Risque | Statut |
|---------|--------|--------|
| Mot de passe seed statique dans DbInitializer | Critique (production) | Ouvert -- necessite un flux de premiere connexion |
| Pas d'expiration/rotation de mot de passe | Faible | Ouvert |
| Le limiteur de debit global pourrait bloquer toute l'usine | Moyen | Ouvert -- necessite un partitionnement par IP |
| Aucun scan SAST/DAST/SCA effectue | Moyen | Non traite |
| Aucun test de penetration | Moyen | Non traite |
| Couverture inferieure a l'objectif de 80% (16,69% des lignes) | Moyen | Ouvert |

**Source :** `AGENT.md` lignes 322-328, `docs/production-readiness-audit/03-SECURITY-AUDIT.md`
