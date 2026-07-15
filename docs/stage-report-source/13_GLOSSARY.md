# 13_GLOSSARY.md -- Terminologie

Ce glossaire définit les termes tels qu'ils sont utilisés dans le projet Motherson Box Management. Chaque définition correspond à l'usage réel dans le code source.

---

## A

**Active Template** -- Un `BoxTemplate` avec `IsActive = true`. Seuls les templates actifs apparaissent dans la page de sélection de templates et participent à la correspondance de préfixe pour l'auto-scan. Source : `BoxTemplateService.cs`

**Admin (français)** -- Variante française du rôle Administrateur, stockée sous forme `"Admin"` dans la base de données. Utilisé de manière interchangeable avec « Administrator » dans les vérifications d'autorisation. Source : `AppRoles.AdminFr`

**Administrator** -- Le rôle applicatif avec le plus haut niveau de privilèges. Peut gérer les utilisateurs, consulter tous les journaux d'audit, configurer les imprimantes et effectuer toutes les opérations de Superviseur. Source : `AppRoles.Administrator`

**Agent** -- Fait référence à l'application de bureau PrintAgent (`MothersonPrintAgent.exe`), une application de zone de notification Windows qui interroge l'API web pour les travaux d'impression. Source : `MothersonBoxManagement.PrintAgent/`

**AgentTokenHash** -- Hash SHA-256 du bearer token assigné à un PrintAgent appairé. Stocké dans `PrinterConfigurations.AgentTokenHash`. Le token brut n'est jamais stocké côté serveur. Source : `PrinterConfiguration.cs`

**Archive** -- Une opération du cycle de vie des boîtes qui déplace une boîte Completed ou CompletedWithException vers un statut historique en lecture seule (`BoxStatus.Archived`). Source : `BoxService.ArchiveBoxAsync`

**Auto-Complete** -- Transition automatique d'une boîte de Open à Completed lorsque `CurrentQuantity >= ExpectedQuantity`. Déclenchée par le service de scan après chaque association de colis réussie. Source : `PackageScanService.cs`

**Auto-Scan** -- Un mode de scan où le système crée automatiquement une boîte à partir d'un template lorsqu'un code-barres de colis correspond au `PackagePrefixPattern` du template. Combine la création de boîte, l'association de colis et la mise en file d'impression dans une seule opération atomique. Source : `BoxController.AutoScanPackage`

---

## B

**BarcodeConfiguration** -- Entité stockant les paramètres de génération de codes-barres : préfixe de boîte (défaut `BOX-`), format de date (défaut `yyyyMMdd`), longueur du suffixe aléatoire (défaut 6), préfixe de colis et longueur minimale de colis. Source : `Entities/BarcodeConfiguration.cs`

**BarcodeValue** -- La chaîne de code-barres scannable d'une boîte, typiquement identique à `BoxNumber`. Générée sous la forme `{BoxPrefix}{DatePattern}-{RandomHex}` (ex. `BOX-20260715-A3F2B1`). Source : `BarcodeService.cs`

**Bearer Token** -- Token d'authentification utilisé par l'API REST du PrintAgent. Obtenu lors de l'appairage, stocké chiffré par DPAPI sur le poste de travail client. Source : `PrintAgentApiController.cs`

**Block** -- Une opération de quarantaine qui empêche une boîte ou un colis de participer à des scans ultérieurs. Les boîtes bloquées rejettent toutes les associations de colis. Les colis bloqués sont exclus du compteur actif. Source : `BoxService.BlockBoxAsync`, `BoxService.BlockPackageAsync`

**Box** -- L'entité métier centrale représentant une boîte d'emballage physique. Contient des colis identifiés par des codes-barres. Suit une machine à états définie (Created -> Open -> Completed/Cancelled/Blocked). Source : `Entities/Box.cs`

**BoxAuditLog** -- Enregistrement immuable et en ajout uniquement de chaque opération effectuée sur les boîtes et les colis. Généré automatiquement par l'`AuditSaveChangesInterceptor`. Source : `Entities/BoxAuditLog.cs`

**BoxNumber** -- L'identifiant d'affichage lisible d'une boîte, typiquement identique à `BarcodeValue`. Source : `Entities/Box.cs`

**BoxPackage** -- Entité d'association liant un colis de câble (identifié par `PackageBarcode`) à une boîte. Supporte la suppression logique via `IsRemoved`. Source : `Entities/BoxPackage.cs`

**BoxPrintJob** -- Entité suivant une demande d'étiquette d'impression à travers son cycle de vie : Pending -> Claimed -> Printed/Failed. Supporte la réservation basée sur un bail et la logique de réessai. Source : `Entities/BoxPrintJob.cs`

**BoxStatus** -- Énumération définissant les 7 états du cycle de vie d'une boîte : Open (0), Completed (1), CompletedWithException (2), Cancelled (3), Archived (4), Blocked (5), Created (6). Source : `Entities/BoxStatus.cs`

**BoxTemplate** -- Configuration réutilisable pour la création de boîtes standardisées. Définit le type, les dimensions, la quantité attendue et un optionnel motif de préfixe de colis. Source : `Entities/BoxTemplate.cs`

**BoxType** -- Énumération définissant les 3 types de matériau : Cardboard (0), Wood (1), Plastic (2). Source : `Entities/BoxType.cs`

---

## C

**Cancel** -- Une opération de superviseur qui arrête définitivement une boîte (`BoxStatus.Cancelled`). Les colis restent associés pour préserver l'historique. Nécessite un motif. Source : `BoxOperationsController.CancelBox`

**Claim** -- L'action par laquelle un PrintAgent réserve un travail d'impression en attente pour un traitement exclusif. Utilise un token de bail pour empêcher la double réservation. Source : `PrintAgentApiController.Next`

**CompletionMode** -- Champ sur Box indiquant comment la boîte a été complétée : `Automatic` (quantité atteinte) ou `Forced` (exception superviseur). Source : `Entities/Box.cs`

**Concurrency Token** -- Champ `RowVersion` (type SQL Server `rowversion`) sur Box, BoxPrintJob et PasswordResetRequest. Utilisé pour la concurrence optimiste afin de détecter les modifications simultanées. Source : `ApplicationDbContext.cs`

**CurrentQuantity** -- Compteur dénormalisé sur Box suivant le nombre de colis actifs (non supprimés, non bloqués). Incrémenté/décrémenté automatiquement par la couche service. Source : `Entities/Box.cs`

---

## D

**Dashboard** -- La page d'accueil principale après connexion (`/Dashboard`). Affiche les boîtes actives, les boîtes en attente, la saisie de scan et l'identité du poste. Source : `DashboardController.cs`

**DPAPI** -- Windows Data Protection API (`CryptProtectData`/`CryptUnprotectData`). Utilisé par le PrintAgent pour chiffrer le bearer token au repos sur le poste de travail. Source : `Dpapi.cs`

**Double-Scan** -- Le flux de scan manuel : scanner un code-barres de colis, puis scanner un code-barres de boîte dans les 30 secondes pour créer l'association. Utilisé lorsqu'aucun préfixe de template ne correspond. Source : `scanner.js`

---

## E

**EF Core** -- Entity Framework Core 8.0.28, l'ORM utilisé pour l'accès à la base de données. Code-first avec migrations. Source : `MothersonBoxManagement.csproj`

**ExpectedQuantity** -- Le nombre cible de colis qu'une boîte doit contenir. Lorsque `CurrentQuantity >= ExpectedQuantity`, la boîte se complète automatiquement. Source : `Entities/Box.cs`

---

## F

**Filtered Unique Index** -- Un index unique SQL Server avec une clause WHERE. Utilisé pour `PackageBarcode` (WHERE `IsRemoved = 0`) et `PackagePrefixPattern` (WHERE `IsActive = 1 AND PackagePrefixPattern IS NOT NULL`). Source : Migrations

**Force Close** -- Une opération de superviseur qui ferme une boîte ouverte avant d'atteindre la quantité attendue (`BoxStatus.CompletedWithException`). Nécessite un motif. Source : `BoxOperationsController.ForceCloseBox`

**French Variants** -- Chaînes de rôle stockées en français (`"Superviseur"`, `"Admin"`) en plus de l'anglais (`"Supervisor"`, `"Administrator"`). Les deux variantes sont vérifiées lors de l'autorisation. Source : `AppRoles.cs`

---

## G

**GSD Core** -- Méthodologie « Get Stuff Done » utilisée pour la planification du projet. Phases, plans, exigences et jalons suivis dans le répertoire `.planning/`. Source : `.planning/`

---

## H

**HAS_BOX** -- État du scanner indiquant le mode sticky. Une fois qu'une boîte est sélectionnée, tous les scans de colis suivants vont vers cette boîte sans rescanner le code-barres de la boîte. Source : `scanner.js`

**Health Check** -- Points d'accès (`/health/live`, `/health`, `/health/ready`) pour surveiller la disponibilité de l'application et de la base de données. Source : `Program.cs`

---

## I

**Idempotency** -- La propriété selon laquelle la répétition de la même opération de scan produit le même résultat. Implémentée via `ScanRequestId` (UUID) sur `BoxPackage`. Source : `PackageScanService.cs`

**Interceptor** -- `SaveChangesInterceptor` EF Core qui génère automatiquement des entrées de journal d'audit à chaque sauvegarde en base de données. Source : `AuditSaveChangesInterceptor.cs`

**IsActive** -- Booléen sur User, BoxTemplate et PrinterConfiguration pour la suppression logique/désactivation. Source : Plusieurs entités

**IsRemoved** -- Booléen sur BoxPackage pour la suppression logique. Les colis supprimés sont exclus du compteur actif et du filtre d'index unique. Source : `Entities/BoxPackage.cs`

---

## K

**Keyboard Wedge** -- Le mode de scan où un lecteur de codes-barres USB agit comme un clavier, tapant des caractères suivis de Entrée. L'application capture cela via un champ de saisie caché. Source : `scanner.js`

---

## L

**Lease Token** -- Un token à durée limitée assigné lorsqu'un PrintAgent revendique un travail. Empêche la double réservation et valide les rapports de complétion/échec. Source : `BoxPrintJob.LeaseTokenHash`

**Lockout** -- Suspension temporaire de compte après trop de tentatives de connexion échouées. En mémoire, configurable (défaut : 5 tentatives / 15 minutes). Source : `LoginLockoutService.cs`

**Longest Prefix Wins** -- Stratégie de correspondance de template pour l'auto-scan : lorsque plusieurs templates ont des motifs de préfixe qui correspondent à un code-barres de colis, le template avec le motif le plus long est sélectionné. Source : `BoxTemplateService.FindTemplateByPackageBarcodeAsync`

---

## M

**Matricule** -- Identifiant alphanumérique unique pour chaque utilisateur (3 à 20 caractères). Utilisé comme identifiant de connexion. Source : `Entities/User.cs`

**Material Design 3** -- La philosophie du système de design appliquée à l'interface utilisateur. Implémentée via des propriétés CSS personnalisées dans `site.css`. Source : `DESIGN.md`

**MustChangePassword** -- Claim dans le cookie d'authentification indiquant que l'utilisateur est en session de récupération et doit changer son mot de passe. Force la redirection vers `/Account/ChangePassword`. Source : `Program.cs` lignes 101-112

---

## O

**Operator** -- Le rôle avec le moins de privilèges. Peut scanner des colis dans des boîtes, consulter les détails, rechercher, utiliser les templates approuvés et configurer les paramètres du poste de travail. Ne peut pas effectuer d'opérations d'exception. Source : `AppRoles.Operator`

---

## P

**PackageBarcode** -- L'identifiant unique d'un colis de câble, scanné à partir d'un code-barres physique. Unique au niveau du système (index unique filtré, IsRemoved = 0). Source : `Entities/BoxPackage.cs`

**PackagePrefixPattern** -- Champ optionnel sur BoxTemplate (chiffres uniquement, 50 caractères max). Lorsqu'un code-barres de colis scanné commence par ce préfixe, le système crée automatiquement une boîte à partir du template. Source : `Entities/BoxTemplate.cs`

**Pairing Code** -- Un code alphanumérique de 12 caractères généré par un administrateur pour appairer un PrintAgent avec le serveur. Valable 10 minutes, usage unique. Source : `PrintAgentService.cs`

**Password Reset Request** -- Entité suivant la demande de récupération de mot de passe d'un employé. Cycle de vie : Pending -> Approved -> InProgress -> Consumed (ou Expired). Source : `Entities/PasswordResetRequest.cs`

**PrintAgent** -- Application de zone de notification Windows qui interroge l'API web pour les travaux d'impression et imprime des étiquettes sur les imprimantes connectées localement. Source : `MothersonBoxManagement.PrintAgent/`

**PrintClient** -- Page d'impression côté navigateur (`/Box/PrintClient/{barcode}`). Accessible à tous les rôles authentifiés. Utilise `window.print()` pour la sortie d'étiquette. Source : `PrintController.PrintClient`

**PrinterConfiguration** -- Entité représentant la configuration d'impression d'un poste de travail : code, nom de machine, nom d'imprimante, hash du token agent, imprimantes disponibles, statut. Source : `Entities/PrinterConfiguration.cs`

---

## Q

**QR Code** -- Code-barres bidimensionnel généré pour chaque étiquette de boîte. Contient le `BarcodeValue`. Généré avec la bibliothèque QRCoder au niveau ECC M. Source : `QrCodeService.cs`

---

## R

**Rate Limiting** -- Limitation de débit des requêtes configurée avec 3 politiques : login (TokenBucket, 5/min), scan (FixedWindow, 60/min), global (FixedWindow, 200/min). Source : `RateLimitingConfiguration.cs`

**Recovery Login** -- Un flux de connexion sans mot de passe après approbation par l'administrateur d'une demande de réinitialisation de mot de passe. Crée une session temporaire (15 minutes) avec le claim `MustChangePassword`. Source : `AccountController.RecoveryLogin`

**RequestId** -- UUID généré par opération de scan par le frontend. Utilisé comme clé d'idempotence pour empêcher les soumissions de scan en double. Source : `scanner.js`, `PackageScanService.cs`

**Retrait** -- Terme français pour la suppression/dissociation d'un colis d'une boîte. Utilisé comme nom d'action dans le code source. Source : `BoxOperationsController.RetraitPackage`

**RowVersion** -- SQL Server `rowversion` (8 octets binaires) utilisé comme jeton de concurrence optimiste. Mis à jour automatiquement par SQL Server à chaque modification de ligne. Source : `ApplicationDbContext.cs`

---

## S

**ScanResult** -- DTO retourné par le service de scan contenant `Success` (booléen), `Message` (chaîne) et optionnellement `Box` (BoxDetailsDto). Source : `Dtos/ScanResult.cs`

**Scanner Simulator** -- Un panneau flottant dans l'interface du Dashboard simulant les entrées d'un scanner USB pour tester sans matériel physique. Activé via `window.simulateScan()`. Source : `Dashboard/Index.cshtml`

**SecurityStamp** -- Chaîne aléatoire (GUID sans tirets) stockée sur User et dans le cookie d'authentification. Régénérée à chaque changement de crédentiel/profil. Validée à chaque requête pour détecter les sessions obsolètes. Source : `Entities/User.cs`, `AuthenticationConfiguration.cs`

**Soft Delete** -- Patron où les enregistrements sont marqués comme supprimés (`IsActive = false` ou `IsRemoved = true`) plutôt que physiquement retirés de la base de données. Utilisé pour Users, BoxPackages, BoxTemplates et PrinterConfigurations. Source : Plusieurs entités

**Sticky Mode** -- État du scanner (`HAS_BOX`) où les scans de colis suivants sont automatiquement associés à la boîte courante sans rescanner le code-barres de la boîte. Se termine à la complétion de la boîte ou au scan d'un autre code-barres BOX. Source : `scanner.js`

**Supervisor** -- Rôle de niveau intermédiaire avec des permissions de gestion du cycle de vie des boîtes : annuler, forcer la fermeture, bloquer/débloquer, archiver, transférer des colis, gérer les templates. Source : `AppRoles.Supervisor`

**Superviseur** -- Variante française de Supervisor. Source : `AppRoles.SupervisorFr`

**SYSTEM Principal** -- Utilisateur d'audit interne (Id = -1, Matricule = « SYSTEM », IsActive = false). Utilisé pour l'attribution d'audit automatisée lorsqu'aucun contexte HTTP n'est disponible. Ne peut être modifié ni supprimé. Source : `DbInitializer.cs`, migration `AddSystemAuditPrincipal`

---

## T

**Template** -- Voir BoxTemplate.

**Transfer** -- Une opération de superviseur qui déplace un colis d'une boîte ouverte vers une autre boîte ouverte. Ajuste les quantités sur les deux boîtes et peut auto-compléter la destination. Nécessite un motif. Source : `BoxService.TransferPackageAsync`

---

## U

**Unblock** -- Opération qui supprime une quarantaine d'une boîte ou d'un colis bloqué, restaurant son statut actif. Source : `BoxService.UnblockBoxAsync`, `BoxService.UnblockPackageAsync`

**Unique Constraint** -- Index SQL Server empêchant les valeurs en double. Le plus critique est sur `BoxPackages.PackageBarcode` (filtré : `IsRemoved = 0`). Source : Migrations

---

## V

**ViewModel** -- Classe C# dans le répertoire `Models/` utilisée exclusivement pour le transfert de données entre les vues Razor et les contrôleurs. N'expose jamais directement les entités EF Core. Source : `MothersonBoxManagement/Models/`

---

## W

**WebApplicationFactory** -- Infrastructure de test ASP.NET Core qui crée un serveur de test avec une base de données en mémoire. Utilisée comme principal patron de test d'intégration. Source : `CustomWebApplicationFactory.cs`

**Workstation** -- Un terminal physique (PC d'usine) qui accède à l'application via le navigateur. Identifié par un nom stocké dans le localStorage du navigateur et transmis avec chaque opération. Source : `station-terminal.js`, `WorkstationResolver.cs`

**WorkstationResolver** -- Service qui résout le nom du poste de travail actuel en utilisant une chaîne de repli : valeur fournie par le client -> valeur de configuration -> `Environment.MachineName`. Source : `WorkstationResolver.cs`

---

## Z

**ZPL** -- Zebra Programming Language, un langage de commande pour les imprimantes à étiquettes thermiques Zebra. Le PrintAgent peut générer des commandes ZPL-II pour la communication directe avec l'imprimante. Source : `ZplLabelBuilder.cs`
