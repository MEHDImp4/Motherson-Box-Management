# 12_REPORT_MATERIALS.md -- Matériaux pour le futur rapport de stage

Ce fichier inventorie les matériaux disponibles pour la rédaction du rapport de stage. Il ne rédige pas le rapport lui-même.

---

## 1. Disponibles pour l'introduction

| Matériau | Source | Statut |
|----------|--------|--------|
| Nom du projet et version | `MothersonBoxManagement.csproj` | Vérifié |
| Problématique métier (traçabilité, unicité) | `AGENT.md`, `.planning/PROJECT.md` | Vérifié |
| Utilisateurs cibles (3 rôles) | Code, `AGENT.md` | Vérifié |
| Stack technologique | `.csproj`, `docker-compose.yml` | Vérifié |
| Chronologie de développement (5 phases, 2026-07-02 au 2026-07-05) | `.planning/MILESTONES.md`, git log | Vérifié |
| Définition du périmètre MVP | `AGENT.md` lignes 9-16 | Vérifié |
| Nom de l'entreprise : Motherson | Fichiers projet | Vérifié |
| Département : zone d'emballage P3 | `AGENT.md`, `README.md` | Vérifié |
| Contributions spécifiques du stagiaire | **Non présent dans le code source** | Nécessite une saisie humaine |
| Contexte et localisation de l'entreprise | **Non présent dans le code source** | Nécessite une saisie humaine |

---

## 2. Disponibles pour le contexte technique

| Matériau | Source | Statut |
|----------|--------|--------|
| Architecture ASP.NET Core MVC | Structure du code, `Program.cs` | Vérifié |
| EF Core Code First avec 23 migrations | Répertoire `Migrations/` | Vérifié |
| SQL Server 2022 avec contraintes CHECK | Migrations, `docker-compose.yml` | Vérifié |
| Authentification par cookie avec security stamps | `AuthenticationConfiguration.cs` | Vérifié |
| Autorisation basée sur les rôles (3 rôles + variantes françaises) | `AppRoles.cs`, attributs des contrôleurs | Vérifié |
| Journal d'audit immuable via intercepteur | `AuditSaveChangesInterceptor.cs` | Vérifié |
| Concurrence optimiste (RowVersion) | `Box.cs`, `ApplicationDbContext.cs` | Vérifié |
| Limitation de débit (3 politiques) | `RateLimitingConfiguration.cs` | Vérifié |
| Déploiement Docker (dev + prod) | `docker-compose.yml`, `docker-compose.prod.yml` | Vérifié |
| Pipeline CI/CD | `.github/workflows/ci.yml` | Vérifié |
| Système de design (Material Design 3) | `DESIGN.md`, `site.css` | Vérifié |

---

## 3. Disponibles pour la description des fonctionnalités

| Matériau | Source | Statut |
|----------|--------|--------|
| Machine à états du cycle de vie des boîtes (7 états) | `BoxStatus.cs`, `AGENT.md` | Vérifié |
| Machine à états du scanner (3 états) | `scanner.js` | Vérifié |
| Application de l'unicité des colis | `PackageScanService.cs`, migrations | Vérifié |
| Auto-scan avec correspondance de préfixe | `BoxTemplateService.cs`, `BoxController.cs` | Vérifié |
| Mode sticky pour le scan continu | `scanner.js` | Vérifié |
| Opérations d'exception superviseur (11 actions) | `BoxOperationsController.cs` | Vérifié |
| Workflow de récupération de mot de passe | `PasswordRecoveryService.cs` | Vérifié |
| Système d'agent d'impression (appairage, heartbeat, file d'attente d'impression) | Fichiers du projet PrintAgent | Vérifié |
| Gestion des utilisateurs (CRUD + réinitialisation de mot de passe) | `UsersController.cs` | Vérifié |

---

## 4. Diagrammes disponibles

| Diagramme | Type | Emplacement | Statut |
|-----------|------|-------------|--------|
| Machine à états des boîtes | Mermaid stateDiagram | `AGENT.md` lignes 115-127 | Prêt à copier |
| Modèle ER | Mermaid erDiagram | `06_DATA_MODEL.md` | Prêt à copier |
| Composants d'architecture | Mermaid graph | `03_ARCHITECTURE.md` | Prêt à copier |
| Topologie de déploiement | Mermaid graph | `08_INFRASTRUCTURE_AND_DEPLOYMENT.md` | Prêt à copier |
| Diagramme de séquence du scan | Mermaid sequenceDiagram | `03_ARCHITECTURE.md` | Prêt à copier |
| Machine à états du scanner | Mermaid stateDiagram | `04_FRONTEND.md` | Prêt à copier |

---

## 5. Captures d'écran à réaliser

| Écran | Objectif dans le rapport | Route | Données de démo nécessaires | Statut |
|-------|--------------------------|-------|----------------------------|--------|
| Page de connexion | UX d'authentification | `/Account/Login` | Aucune | À capturer |
| Tableau de bord | Vue d'ensemble de l'interface principale | `/Dashboard` | Boîtes actives avec progression | À capturer |
| Sélection de modèle | Création basée sur des modèles | `/Dashboard/Templates` | Modèles actifs | À capturer |
| Recherche de boîtes | Recherche et filtrage | `/Box` | Plusieurs boîtes avec divers statuts | À capturer |
| Détails de boîte (ouverte) | Interface de scan de colis | `/Box/Details/{barcode}` | Boîte ouverte avec colis | À capturer |
| Détails de boîte (superviseur) | Opérations d'exception | `/Box/Details/{barcode}` | Superviseur connecté | À capturer |
| Journal d'audit | Traçabilité | `/Audit` | Entrées d'audit | À capturer |
| Recherche de colis | Suivi des colis | `/Box/SearchPackage` | Colis trouvé | À capturer |
| Paramètres / Agent d'impression | Configuration du poste de travail | `/Box/Settings` | Agent configuré | À capturer |
| Gestion des utilisateurs | CRUD administrateur | `/Users` | Plusieurs utilisateurs | À capturer |
| Page d'erreur | Gestion des erreurs | `/Dashboard/Error` | Déclencher 404 ou 500 | À capturer |
| Vue mobile | Design responsive | N'importe quelle page sur viewport mobile | -- | À capturer |
| Overlay scanner (succès) | Retour de scan | Scanner un colis | -- | À capturer |
| Overlay scanner (erreur) | Retour d'erreur | Scanner un code-barres invalide | -- | À capturer |

---

## 6. Résultats de tests disponibles

| Métrique | Valeur | Source | Date |
|----------|--------|--------|------|
| Résultat du build | 0 avertissements, 0 erreurs | production-readiness-audit | 2026-07-13 |
| Résultat des tests | 162 passants, 0 échecs | production-readiness-audit | 2026-07-13 |
| Tests SQL Server | 2 passants | production-readiness-audit | 2026-07-13 |
| Vulnérabilités NuGet | 0 | production-readiness-audit | 2026-07-13 |
| Couverture (lignes) | 16,69 % | production-readiness-audit | 2026-07-13 |
| Couverture (branches) | 58,39 % | production-readiness-audit | 2026-07-13 |
| Image Docker | Build réussi et health check passé | production-readiness-audit | 2026-07-13 |
| Sauvegarde/restauration | 1,64s réussi | production-readiness-audit | 2026-07-13 |

> Note : Ces résultats proviennent de l'audit de préparation à la production. Les modifications non commitées actuelles peuvent les affecter. Un `dotnet build` et `dotnet test` frais doivent être exécutés avant de finaliser le rapport.

---

## 7. Difficultés et défis techniques (observables depuis le code source)

| Difficulté | Preuve | Source |
|------------|--------|--------|
| Dimensions décimales vs. entières | L'historique des migrations montre une évolution float -> int -> decimal(10,2) | Migrations |
| Unicité des colis avec suppression logique | Nécessité d'un index unique filtré pour permettre la réassociation | Migration `AllowHistoricalPackageReassociation` |
| Consolidation du journal d'audit | Multiples itérations avant l'approche par intercepteur uniquement | ADR dans AGENT.md |
| Identité de poste sur serveur partagé | Solution de contournement via localStorage navigateur car le serveur ne peut pas détecter le nom d'hôte client | ADR dans AGENT.md |
| Agent d'impression vs. impression navigateur | Architecture modifiée de côté serveur à navigateur uniquement | Documents TDD |
| Complexité des contrôleurs | BoxController scindé en BoxController + BoxOperationsController | ADR dans AGENT.md |
| Remédiation de sécurité | 13 constatations traitées dans l'audit du code source | CODEBASE_AUDIT_IMPLEMENTATION_PLAN.md |
| Préparation à la production | Verdict NO-GO avec 8 bloqueurs P1 | production-readiness-audit |

---

## 8. Limitations (observables)

| Limitation | Preuve |
|------------|--------|
| Pas d'intégration ERP/MES | AGENT.md : « autonome, pas de synchronisation externe » |
| Pas encore de déploiement en production | En local Docker uniquement |
| Impression physique non encore testée sur matériel cible | Fourni par l'interne |
| Pas de test de charge | production-readiness-audit |
| Pas de test E2E navigateur | production-readiness-audit |
| Faible couverture de tests (16,69 %) | production-readiness-audit |
| Pas de test d'accessibilité | production-readiness-audit |
| Pas d'archivage automatisé | AGENT.md : « Définir la fréquence d'archivage » point ouvert |
| Pas d'expiration de mot de passe | AGENT.md VULN-09 |

---

## 9. Perspectives (mentionnées dans le code source et saisie humaine)

| Perspective | Source |
|-------------|--------|
| v2 : Intégration ERP | **Saisie humaine** de l'interne |
| v2 : Export Excel/PDF | **Saisie humaine** de l'interne |
| v2 : Intégration d'impression thermique | `.planning/REQUIREMENTS.md` (PRNT-01, reporté) |
| Expiration/rotation des mots de passe | AGENT.md VULN-09 |
| Limitation de débit par IP | production-readiness-audit SEC-004 |
| Analyse SAST/DAST/SCA | production-readiness-audit |
| Supervision et alertes | production-readiness-audit |
| Archivage automatisé | AGENT.md point ouvert |

---

## 10. Éléments nécessitant une explication humaine

| Élément | Pourquoi |
|---------|----------|
| Pourquoi ASP.NET Core MVC a été choisi par rapport à d'autres frameworks | Non documenté dans le code source |
| Pourquoi le projet utilise une authentification par cookie au lieu de JWT/OAuth | L'ADR indique « plus léger » mais la justification complète est inconnue |
| Ce que le stagiaire a appris de ce projet | Nécessite une saisie humaine supplémentaire |
| Difficultés personnelles rencontrées au-delà du système d'impression | Nécessite une saisie humaine supplémentaire |
| Comment l'application a été reçue par les utilisateurs finaux | Non présent dans le code source |
| Ce que le stagiaire ferait différemment | Non présent dans le code source |
