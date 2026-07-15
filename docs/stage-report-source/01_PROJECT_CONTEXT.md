# 01_PROJECT_CONTEXT.md -- Contexte du projet et objectifs metier

## 1. Identite du projet

- **Nom :** Motherson Box Management
- **Version :** v1.0.0 (MVP)
- **Type :** Application web interne (autonome, sans integrations externes)
- **Framework :** ASP.NET Core MVC (.NET 8.0)
- **Langage :** C# 12 avec Nullable Reference Types active

**Source :** `MothersonBoxManagement/MothersonBoxManagement.csproj` (Version 1.0.0, TargetFramework net8.0)

## 2. Probleme metier

Dans la zone de conditionnement P3 de l'usine Motherson, les lots de cables doivent etre groupes dans des cartons physiques pour l'expedition. Sans systeme de tracabilite, il existe un risque que :

- Le meme lot de cables soit affecte a plusieurs cartons (erreur de duplication)
- Aucun historique n'existe de qui a scane quoi, quand, et depuis quel poste de travail
- Les superviseurs ne puissent pas retracer les problemes de qualite a des cartons ou lots specifiques
- Les operations exceptionnelles (annulations, transferts, fermetures forcees) se fassent sans responsabilite

**Source :** `AGENT.md` lignes 4-7, `.planning/PROJECT.md`

## 3. Valeur metier principale

Le systeme garantit une **tracabilite absolue** des cartons de conditionnement et applique une **contrainte d'unicite globale** : aucun lot de cables ne peut etre scanne ou affecte a plus d'un carton dans l'ensemble du systeme.

Cette contrainte est appliquee a deux niveaux :
1. **Niveau SQL Server :** Un index unique filtre sur `BoxPackages.PackageBarcode` (WHERE `IsRemoved = 0`) empeche les doublons au niveau de la base de donnees
2. **Niveau applicatif :** `PackageScanService` verifie les doublons avant insertion et gere les erreurs de contrainte unique SQL (numeros d'erreur 2601, 2627)

**Source :** `AGENT.md` ligne 7, `MothersonBoxManagement/Services/PackageScanService.cs`, `MothersonBoxManagement/Data/ApplicationDbContext.cs`

## 4. Utilisateurs cibles

| Role | Description | Identification |
|------|-------------|----------------|
| **Operateur** | Ouvrier de l'usine qui scanne les lots dans les cartons | Matricule + mot de passe |
| **Superviseur** | Chef d'equipe qui gere les cartons, les templates et les exceptions | Matricule + mot de passe |
| **Administrateur (IT)** | Gestionnaire systeme qui controle les utilisateurs, les templates et l'acces complet a l'audit | Matricule + mot de passe |

Tous les utilisateurs s'authentifient avec un **matricule** unique (identifiant alphanumerique, 3-20 caracteres) et un mot de passe. Il n'y a pas d'inscription publique ; les comptes sont crees par les Administrateurs.

**Source :** `AGENT.md` lignes 77-82, `MothersonBoxManagement/Security/AppRoles.cs`

## 5. Contexte metier

### 5.1 Entreprise

- **Nom de l'entite :** Motherson PKC
- **Adresse :** 8J22+RJ, Ameur Seflia, Kenitra (Maroc)
- **Departement :** Zone de conditionnement P3

> **Source :** Informations fournies par l'interne.

### 5.2 Environnement operationnel

- L'application tourne sur un **serveur partage** auquel accedent plusieurs terminaux d'usine via navigateur
- Chaque terminal physique stocke son **nom de poste** dans le localStorage du navigateur et le transmet avec chaque operation de scan/audit
- Des scanners a code-barres USB (type clavier) sont utilises pour le scan des lots
- L'application est concue pour un **usage intranet uniquement** (non accessible depuis Internet)

**Source :** `AGENT.md` lignes 15-16, `AGENT.md` ligne 259 (ADR : Identite de poste cote navigateur)

### 5.3 Limites du perimetre

**Dans le perimetre (v1 MVP) :**
- Fonctionnement autonome (pas de connexion ERP/MES)
- Decouverte des lots par scan (pas d'import de donnees de reference)
- Application de l'unicite globale des lots
- Interface en anglais
- Simulateur de scanner integre pour les tests sans materiel
- Identite de poste basee sur le navigateur

**Hors perimetre :**
- Synchronisation ERP ou MES externe
- Impression d'etiquettes (l'integration imprimante thermique a ete construite puis remplacee par l'impression navigateur)
- Export Excel
- Pilotes d'impression physique automatises

**Source :** `AGENT.md` lignes 9-16, `.planning/REQUIREMENTS.md`

## 6. Chronologie de developpement

| Date | Jalon |
|------|-------|
| 02/07/2026 | Phase 1 : Schema de base de donnees et authentification |
| 02/07/2026 | Phase 2 : Cycle de vie des cartons et recherche depuis l'accueil |
| 03/07/2026 | Phase 3 : Integration scan de codes-barres et simulateur |
| 04/07/2026 | Phase 4 : Exceptions superviseur et piste d'audit |
| 04/07/2026 | Phase 5 : Verification et durcissement |
| 05/07/2026 | Passe de release v1.0 (Docker, docs, marqueur de version) |
| 05/07/2026 | Audit de securite et remediation |
| 06/07/2026 | Identite de poste locale au navigateur |
| 08/07/2026 | Audit de la codebase et plan d'implementation |
| 09/07/2026 | Reorganisation de la codebase, auto-scan, mode collant |
| 12/07/2026 | Durcissement de la base de donnees (contraintes CHECK, idempotence) |
| 13/07/2026 | Audit de preparation a la production |
| 14/07/2026 | Administration a distance des postes de travail |

**Source :** `AGENT.md` lignes 358-366, `.planning/MILESTONES.md`, historique Git

## 7. Role de l'interne

- **Statut :** Developpeur unique du projet (seul contributeur au code)
- **Duree du stage :** 1 mois, tout le mois de juillet 2026
- **Encadrants :** Aymane et Ayoub (noms fournis par l'interne, roles precis non verifies dans la codebase)

> **Note :** L'historique Git et le fichier TODO.md attribuent toutes les taches a "Agent". Les commits ont ete generes avec l'assistance d'outils d'IA. Les contributions exactes de l'interne par rapport a l'IA ne peuvent pas etre distinguees a partir de la seule codebase.

## 8. Informations complementaires (saisie humaine)

- **Nombre de terminaux en production :** 2-3 postes de travail
- **Volume quotidien estime :** ~10 cartons/jour (approximatif)
- **Etat du deploiement :** Pas encore en production, execution en local Docker uniquement
- **Difficulte technique principale :** Le systeme d'impression
- **Perspectives v2 :** Integration ERP, export Excel/PDF

## 9. Informations non verifiees

- La signification precise du nom de departement "P3" (mentionne dans les docs mais non defini)
- Les types exacts de cables ou produits conditionnes
- L'infrastructure IT interne de l'entreprise au-dela de ce qui est configure dans Docker

> **Note :** Ces elements necessiteront une contribution humaine pour le rapport de stage.
