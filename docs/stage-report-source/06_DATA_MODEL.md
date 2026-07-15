# 06_DATA_MODEL.md -- Base de donnees et modele de donnees

## 1. Technologie de la base de donnees

- **Moteur :** SQL Server 2022-CU15
- **ORM :** Entity Framework Core 8.0.28 (Code First)
- **Fournisseur :** `Microsoft.EntityFrameworkCore.SqlServer`
- **Nom de la base de donnees :** `MothersonBoxDb` (configurable via `MOTHERSON_DB_NAME`)

**Source :** `MothersonBoxManagement.csproj`, `docker-compose.yml`

## 2. Inventaire des entites

### 2.1 User (table : `Users`)

| Propriete | Type | Contraintes | Description |
|-----------|------|-------------|-------------|
| `Id` | `int` (PK) | Identity, NOT NULL | Auto-increment |
| `Matricule` | `string` | `nvarchar(20)`, UNIQUE INDEX, NOT NULL | Identifiant de connexion |
| `FullName` | `string` | `nvarchar(200)`, NOT NULL | Nom d'affichage |
| `PasswordHash` | `string` | `nvarchar(max)`, NOT NULL | Hachage ASP.NET Core Identity |
| `Role` | `string` | `nvarchar(40)`, NOT NULL | Operator/Supervisor/Administrator/System |
| `IsActive` | `bool` | NOT NULL | Drapeau de suppression logique |
| `SecurityStamp` | `string` | `nvarchar(64)`, NOT NULL | Jeton d'invalidation de session |
| `CreatedAt` | `DateTime` | NOT NULL | Horodatage de creation |
| `UpdatedAt` | `DateTime?` | nullable | Horodatage de derniere mise a jour |

**Donnees initiales :** Principal SYSTEM (Id=-1, Matricule="SYSTEM", IsActive=false)
**Navigation :** CreatedBoxes, ModifiedBoxes, ScannedPackages, AuditLogs

**Source :** `MothersonBoxManagement/Entities/User.cs`, `ApplicationDbContext.cs`

### 2.2 Box (table : `Boxes`)

| Propriete | Type | Contraintes | Description |
|-----------|------|-------------|-------------|
| `Id` | `int` (PK) | Identity, NOT NULL | Auto-increment |
| `BoxNumber` | `string` | `nvarchar(80)`, UNIQUE INDEX, NOT NULL | Numero d'affichage |
| `BarcodeValue` | `string` | `nvarchar(100)`, UNIQUE INDEX, NOT NULL | Code-barres scannable |
| `Type` | `BoxType` (enum->int) | NOT NULL | Cardboard/Wood/Plastic |
| `Height` | `decimal` | `decimal(10,2)`, CHECK > 0 | Hauteur en cm |
| `Width` | `decimal` | `decimal(10,2)`, CHECK > 0 | Largeur en cm |
| `Depth` | `decimal` | `decimal(10,2)`, CHECK > 0 | Profondeur en cm |
| `ExpectedQuantity` | `int` | NOT NULL, CHECK > 0 | Nombre de colis cible |
| `CurrentQuantity` | `int` | NOT NULL, CHECK >= 0 AND <= Expected | Nombre actuel de colis |
| `Status` | `BoxStatus` (enum->int) | NOT NULL | Etat actuel |
| `CreatedByUserId` | `int` (FK) | NOT NULL | Createur |
| `LastModifiedByUserId` | `int?` (FK) | nullable | Dernier modificateur |
| `CompletedByUserId` | `int?` (FK) | nullable | Qui a complete |
| `BlockedByUserId` | `int?` (FK) | nullable | Qui a bloque |
| `CreatedAt` | `DateTime` | NOT NULL | Horodatage de creation |
| `ModifiedAt` | `DateTime?` | nullable | Derniere modification |
| `CompletedAt` | `DateTime?` | nullable | Horodatage de completion |
| `BlockedAt` | `DateTime?` | nullable | Horodatage de blocage |
| `CompletionMode` | `string?` | `nvarchar(40)` | Automatic/Forced |
| `ExceptionReason` | `string?` | `nvarchar(500)` | Motif de fermeture forcee |
| `BlockReason` | `string?` | `nvarchar(500)` | Motif de blocage |
| `RowVersion` | `byte[]` | ROWVERSION | Jeton de concurrence optimiste |

**Contraintes CHECK :**
- `CK_Boxes_ExpectedQuantity_Positive` : `[ExpectedQuantity] > 0`
- `CK_Boxes_CurrentQuantity_Range` : `[CurrentQuantity] >= 0 AND [CurrentQuantity] <= [ExpectedQuantity]`
- `CK_Boxes_Dimensions_Positive` : `[Height] > 0 AND [Width] > 0 AND [Depth] > 0`

**Relations :** CreatedBy, LastModifiedBy, CompletedBy, BlockedBy (toutes FK -> Users, suppression Restrict), Packages (1:N -> BoxPackages)

**Source :** `MothersonBoxManagement/Entities/Box.cs`, migrations

### 2.3 BoxPackage (table : `BoxPackages`)

| Propriete | Type | Contraintes | Description |
|-----------|------|-------------|-------------|
| `Id` | `int` (PK) | Identity, NOT NULL | Auto-increment |
| `BoxId` | `int` (FK) | NOT NULL | Boite parente |
| `PackageBarcode` | `string` | `nvarchar(100)`, INDEX UNIQUE FILTRE (IsRemoved=0) | Identifiant du colis |
| `ScannedByUserId` | `int` (FK) | NOT NULL | Scanner |
| `ScannedAt` | `DateTime` | NOT NULL | Horodatage du scan |
| `WorkstationName` | `string?` | `nvarchar(64)` | Nom du terminal |
| `IsBlocked` | `bool` | NOT NULL, default false | Drapeau de quarantaine |
| `BlockReason` | `string?` | `nvarchar(500)` | Motif de blocage |
| `IsRemoved` | `bool` | NOT NULL | Drapeau de suppression logique |
| `RemovedAt` | `DateTime?` | nullable | Horodatage de suppression |
| `RemovedByUserId` | `int?` (FK) | nullable | Qui a supprime |
| `RemovalReason` | `string?` | `nvarchar(500)` | Motif de suppression |
| `ScanRequestId` | `string?` | `nvarchar(64)`, INDEX UNIQUE FILTRE (IS NOT NULL) | Cle d'idempotence |

**Index principaux :**
- Index unique filtre sur `PackageBarcode` WHERE `IsRemoved = 0` (permet la reassociation apres suppression)
- Index unique filtre sur `ScanRequestId` WHERE `IS NOT NULL` (idempotence)

**Source :** `MothersonBoxManagement/Entities/BoxPackage.cs`, migrations

### 2.4 BoxAuditLog (table : `BoxAuditLogs`)

| Propriete | Type | Contraintes | Description |
|-----------|------|-------------|-------------|
| `Id` | `int` (PK) | Identity, NOT NULL | Auto-increment |
| `BoxId` | `int?` (FK) | nullable | Boite concernee |
| `PackageBarcode` | `string?` | `nvarchar(100)` | Colis concerne |
| `ActionType` | `string` | `nvarchar(80)`, NOT NULL | Type d'action |
| `UserId` | `int` (FK) | NOT NULL | Acteur |
| `Timestamp` | `DateTime` | NOT NULL | Horodatage de l'action |
| `WorkstationName` | `string?` | `nvarchar(64)` | Nom du terminal |
| `PreviousValue` | `string?` | `nvarchar(1000)` | Etat precedent |
| `NewValue` | `string?` | `nvarchar(1000)` | Etat nouveau |
| `Reason` | `string?` | `nvarchar(1000)` | Motif de l'action |
| `Description` | `string?` | `nvarchar(1000)` | Description humaine |
| `DetailsJson` | `string?` | `nvarchar(max)` | Diff JSON complet |

**Immutabilite :** Les enregistrements sont en append uniquement. Aucune modification ou suppression au niveau applicatif.

**Source :** `MothersonBoxManagement/Entities/BoxAuditLog.cs`, `AuditSaveChangesInterceptor.cs`

### 2.5 BoxTemplate (table : `BoxTemplates`)

| Propriete | Type | Contraintes | Description |
|-----------|------|-------------|-------------|
| `Id` | `int` (PK) | Identity, NOT NULL | Auto-increment |
| `Name` | `string` | `nvarchar(200)`, UNIQUE INDEX, NOT NULL | Nom du modele |
| `Description` | `string?` | `nvarchar(1000)` | Description |
| `Type` | `BoxType` (enum->int) | NOT NULL | Type de materiau |
| `Height/Width/Depth` | `decimal` | `decimal(10,2)`, NOT NULL | Dimensions en cm |
| `ExpectedQuantity` | `int` | NOT NULL | Quantite cible |
| `PackagePrefixPattern` | `string?` | `nvarchar(450)`, INDEX UNIQUE FILTRE (actif, non null) | Prefixe d'auto-scan |
| `IsActive` | `bool` | NOT NULL, default true | Drapeau actif |
| `CreatedByUserId` | `int` (FK) | NOT NULL | Createur |
| `CreatedAt` | `DateTime` | NOT NULL | Horodatage de creation |
| `UpdatedAt` | `DateTime?` | nullable | Horodatage de mise a jour |

**Source :** `MothersonBoxManagement/Entities/BoxTemplate.cs`, migrations

### 2.6 Autres entites

| Entite | Table | But | Champs cles |
|--------|-------|-----|-------------|
| `BoxPrintJob` | `BoxPrintJobs` | File d'attente des travaux d'impression | BoxId, PrinterName, Payload, Status, LeaseTokenHash, RowVersion |
| `PrinterConfiguration` | `PrinterConfigurations` | Configuration d'imprimante du poste | Code, PcName, PrinterName, AgentTokenHash, AvailablePrintersJson |
| `PrintAgentPairingCode` | `PrintAgentPairingCodes` | Codes d'appariement de l'agent | WorkstationId, CodeHash, ExpiresAt |
| `BarcodeConfiguration` | `BarcodeConfigurations` | Configuration de generation de codes-barres | BoxPrefix, BoxDatePattern, BoxRandomLength |
| `LoginAttempt` | `LoginAttempts` | Suivi des tentatives de connexion | Matricule, FailedAttempts, LockoutEnd |
| `PasswordResetRequest` | `PasswordResetRequests` | Flux de reinitialisation de mot de passe | UserId, Status, ApprovedByUserId, ExpiresAt, RowVersion |

**Source :** Repertoire `MothersonBoxManagement/Entities/`

## 3. Enumerations

### BoxStatus

| Valeur | Entier | Signification |
|--------|--------|---------------|
| `Open` | 0 | Accepte les scans |
| `Completed` | 1 | Auto-complete (plein) |
| `CompletedWithException` | 2 | Fermeture forcee par le superviseur |
| `Cancelled` | 3 | Annule par le superviseur |
| `Archived` | 4 | Historique |
| `Blocked` | 5 | En quarantaine |
| `Created` | 6 | Etat initial (pas encore ouvert) |

**Source :** `MothersonBoxManagement/Entities/BoxStatus.cs`

### BoxType

| Valeur | Entier | Signification |
|--------|--------|---------------|
| `Cardboard` | 0 | Boite en carton |
| `Wood` | 1 | Caisse en bois |
| `Plastic` | 2 | Conteneur en plastique |

**Source :** `MothersonBoxManagement/Entities/BoxType.cs`

## 4. Diagramme ER

```mermaid
erDiagram
    User {
        int Id PK
        string Matricule UK
        string FullName
        string PasswordHash
        string Role
        bool IsActive
        string SecurityStamp
        datetime CreatedAt
    }

    Box {
        int Id PK
        string BoxNumber UK
        string BarcodeValue UK
        BoxType Type
        decimal Height
        decimal Width
        decimal Depth
        int ExpectedQuantity
        int CurrentQuantity
        BoxStatus Status
        int CreatedByUserId FK
        byte[] RowVersion
    }

    BoxPackage {
        int Id PK
        int BoxId FK
        string PackageBarcode UK_filtered
        int ScannedByUserId FK
        bool IsRemoved
        string ScanRequestId UK_filtered
    }

    BoxAuditLog {
        int Id PK
        int BoxId FK_nullable
        string ActionType
        int UserId FK
        string DetailsJson
    }

    BoxTemplate {
        int Id PK
        string Name UK
        BoxType Type
        decimal Height
        decimal Width
        decimal Depth
        int ExpectedQuantity
        string PackagePrefixPattern UK_filtered
        bool IsActive
    }

    BoxPrintJob {
        int Id PK
        int BoxId FK
        string Status
        string LeaseTokenHash
        byte[] RowVersion
    }

    PrinterConfiguration {
        int Id PK
        string Code UK
        string PcName
        string AgentTokenHash UK_filtered
    }

    PasswordResetRequest {
        int Id PK
        int UserId FK
        string Status
        byte[] RowVersion
    }

    User ||--o{ Box : "creates/modifies/completes/blocks"
    Box ||--o{ BoxPackage : "contains"
    User ||--o{ BoxPackage : "scans/removes"
    Box ||--o{ BoxAuditLog : "audited"
    User ||--o{ BoxAuditLog : "performs"
    User ||--o{ BoxTemplate : "creates"
    Box ||--o{ BoxPrintJob : "prints"
    User ||--o{ PasswordResetRequest : "requests/approves"
    PrinterConfiguration ||--o{ BoxPrintJob : "prints_on"
```

## 5. Historique des migrations (23 migrations)

| # | Date | Migration | But |
|---|------|-----------|-----|
| 1 | 2026-07-02 | `InitialSchema` | Creation de Users, Boxes, BoxAuditLogs, BoxPackages |
| 2 | 2026-07-02 | `UseIntegerBoxDimensions` | Dimensions float -> int |
| 3 | 2026-07-04 | `AddBoxPackageBlocking` | IsBlocked, BlockReason sur BoxPackages |
| 4 | 2026-07-04 | `AddBoxExceptionReason` | ExceptionReason sur Boxes |
| 5 | 2026-07-04 | `RenameColumnsAndAddMissingFields` | Renommage ClosedBy->CompletedBy, ajout de champs |
| 6 | 2026-07-04 | `DimensionsDecimalAndAuditDescription` | Dimensions int -> decimal(10,2) |
| 7 | 2026-07-06 | `AddCdcFieldsAndPrintJobs` | Creation de la table BoxPrintJobs |
| 8 | 2026-07-06 | `CleanupDeadFields` | Suppression des colonnes inutilisees |
| 9 | 2026-07-06 | `RemoveRelatedBoxIdFromAuditLog` | Simplification de la FK du journal d'audit |
| 10 | 2026-07-07 | `AddBoxTemplate` | Creation de la table BoxTemplates |
| 11 | 2026-07-08 | `AuditRemediationSecurityAndTraceability` | SecurityStamp, champs de suppression logique |
| 12 | 2026-07-09 | `AddPackagePrefixPattern` | Prefixe d'auto-scan sur les modeles |
| 13 | 2026-07-09 | `AddLoginAttemptsTable` | Table de verrouillage de connexion |
| 14 | 2026-07-09 | `AddPrinterConfigurationAndExtendPrintJobs` | Configuration d'imprimante, travaux d'impression etendus |
| 15 | 2026-07-09 | `AddBarcodeConfiguration` | Configuration de generation de codes-barres |
| 16 | 2026-07-12 | `AddSystemAuditPrincipal` | Utilisateur SYSTEM (Id=-1) |
| 17 | 2026-07-12 | `AllowHistoricalPackageReassociation` | Index unique filtre |
| 18 | 2026-07-12 | `AddCriticalBoxCheckConstraints` | Contraintes CHECK pour quantites/dimensions |
| 19 | 2026-07-12 | `BoundStringsAndScanIdempotency` | Limites de chaines, ScanRequestId |
| 20 | 2026-07-13 | `ProductionRemediationActivePrefix` | Normalisation des prefixes, index unique filtre |
| 21 | 2026-07-13 | `AddPasswordResetWorkflow` | Table PasswordResetRequests |
| 22 | 2026-07-13 | `AddLocalPrintAgent` | Extension des travaux d'impression, config d'imprimante, codes d'appariement |
| 23 | 2026-07-14 | `AddRemoteWorkstationAdministration` | DisplayName, LastIpAddress sur PrinterConfigurations |

**Source :** Repertoire `MothersonBoxManagement/Migrations/`

## 6. Contraintes principales

### 6.1 Contraintes d'unicite

| Table | Colonne | Type | But |
|-------|---------|------|-----|
| `Users` | `Matricule` | Unique standard | Identifiant de connexion |
| `Boxes` | `BoxNumber` | Unique standard | Numero d'affichage |
| `Boxes` | `BarcodeValue` | Unique standard | Code-barres scannable |
| `BoxPackages` | `PackageBarcode` | Unique filtre (IsRemoved=0) | Unicite globale du colis |
| `BoxPackages` | `ScanRequestId` | Unique filtre (IS NOT NULL) | Idempotence |
| `BoxTemplates` | `Name` | Unique standard | Nom du modele |
| `BoxTemplates` | `PackagePrefixPattern` | Unique filtre (actif, non null) | Unicite du prefixe |
| `PrinterConfigurations` | `Code` | Unique standard | Code du poste |
| `PrinterConfigurations` | `AgentTokenHash` | Unique filtre (IS NOT NULL) | Jeton de l'agent |
| `PrintAgentPairingCodes` | `CodeHash` | Unique standard | Code d'appariement |
| `LoginAttempts` | `Matricule` | Unique standard | Suivi de verrouillage |

### 6.2 Contraintes CHECK

| Table | Contrainte | Regle |
|-------|-----------|-------|
| `Boxes` | `CK_Boxes_ExpectedQuantity_Positive` | `ExpectedQuantity > 0` |
| `Boxes` | `CK_Boxes_CurrentQuantity_Range` | `CurrentQuantity >= 0 AND CurrentQuantity <= ExpectedQuantity` |
| `Boxes` | `CK_Boxes_Dimensions_Positive` | `Height > 0 AND Width > 0 AND Depth > 0` |

### 6.3 Jetons de concurrence

| Table | Colonne | Type |
|-------|---------|------|
| `Boxes` | `RowVersion` | SQL Server rowversion |
| `BoxPrintJobs` | `RowVersion` | SQL Server rowversion |
| `PasswordResetRequests` | `RowVersion` | SQL Server rowversion |

### 6.4 Comportement de suppression

Toutes les relations FK utilisent `DeleteBehavior.Restrict` (aucune suppression en cascade) sauf `PrintAgentPairingCode -> PrinterConfiguration` qui utilise `Cascade`.

**Source :** Configuration Fluent API de `ApplicationDbContext.cs`
