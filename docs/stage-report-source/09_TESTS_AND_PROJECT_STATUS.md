# 09_TESTS_AND_PROJECT_STATUS.md -- Tests, Qualite et Statut du Projet

## 1. Apercu de la suite de tests

| Aspect | Valeur |
|--------|--------|
| Framework | xUnit 2.9.3 |
| Runner | xunit.runner.visualstudio 3.1.5 |
| Integration | Microsoft.AspNetCore.Mvc.Testing 8.0.28 |
| Base de donnees | Microsoft.EntityFrameworkCore.InMemory 8.0.28 |
| Couverture | coverlet.collector 6.0.4 |
| Parallelisation | Desactivee (`DisableTestParallelization = true`) |

**Source :** `MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj`

## 2. Fichiers de test et couverture

| # | Fichier | Tests | Domaine couvert |
|---|---------|-------|-----------------|
| 1 | `AccountControllerTests.cs` | 7 | Connexion, deconnexion, verrouillage, redirections |
| 2 | `AdditionalTests.cs` | 5 Theories | Validation (matricule, mot de passe, dimensions, enums) |
| 3 | `AuditControllerTests.cs` | 6 | Acces audit, filtrage, pagination |
| 4 | `AutoScanPackageTests.cs` | 7 | Scan automatique, correspondance de prefixe, atomicite |
| 5 | `BoxControllerTests.cs` | 11 | CRUD boites, details, recherche, creation |
| 6 | `BoxServiceExceptionTests.cs` | 10 | Transfert, dissociation, reassociation |
| 7 | `ConcurrencyTests.cs` | 3 | Repetition en concurrence optimiste, gestion des doublons |
| 8 | `DashboardControllerTests.cs` | 10 | Rendu du tableau de bord, recherche code-barres, selection de modeles |
| 9 | `PasswordRecoveryFlowTests.cs` | 4 | Flux complet de recuperation |
| 10 | `PrintAgentAdminTests.cs` | 8 | Gestion de flotte admin, configuration |
| 11 | `PrintAgentApiTests.cs` | 2 | Appairage, heartbeat |
| 12 | `PrintAgentRenderingTests.cs` | 5 | Rendu ZPL, backoff, formatage de version |
| 13 | `PrintAgentWorkflowTests.cs` | 5 | Cycle de vie de travail, lease, retry |
| 14 | `SecurityAuditTests.cs` | 9 | Restrictions de role, validation des entrees, gestion des exceptions |
| 15 | `SecurityRemediationTests.cs` | 4 | Concurrence de verrouillage, seed demo, configuration production |
| 16 | `SqlConstraintTests.cs` | 4 | Contraintes CHECK, contraintes uniques |
| 17 | `SqlServerIntegrationTests.cs` | 2 | Vrai SQL Server (conditionnel, `[SqlServerFact]`) |
| 18 | `StickyModeTests.cs` | 3 | Mode sticky, scan automatique + manuel |
| 19 | `SupervisorExceptionsControllerTests.cs` | 8 | Annulation, fermeture forcee, blocage, modification de quantite |
| 20 | `TemplateRemediationTests.cs` | 1 | Normalisation de prefixe |
| 21 | `WebPrintingOnlyTests.cs` | 3 | Impression navigateur uniquement, pas d'impression serveur |
| 22 | `WorkstationResolverTests.cs` | 3 | Chaine de resolution, assainissement |

**Total :** ~130 methodes de test reparties dans 22 fichiers de test

**Source :** Repertoire `MothersonBoxManagement.Tests/`

## 3. Infrastructure de test

### 3.1 CustomWebApplicationFactory

- Etend `WebApplicationFactory<Program>`
- Remplace la BD reelle par EF Core InMemory (nom de base de donnees unique par instance)
- Seed 3 utilisateurs de demo (OP001, SP001, AD001)
- `FakeAntiforgery` personnalise qui valide toujours (permet les POST sans extraction de token)

**Source :** `MothersonBoxManagement.Tests/CustomWebApplicationFactory.cs`

### 3.2 TestAuthHelper

- Cree un `HttpClient` authentifie en postant les identifiants de connexion
- Capture le cookie d'authentification pour les requetes suivantes

**Source :** `MothersonBoxManagement.Tests/TestAuthHelper.cs`

### 3.3 Intercepteurs personnalises

| Intercepteur | Objectif |
|--------------|----------|
| `ConcurrencySimulatingInterceptor` | Simule `DbUpdateConcurrencyException` pour les tests de repetition |
| `UniqueConstraintSimulatingInterceptor` | Simule les violations d'index unique SQL Server |
| `E2EUniqueConstraintSimulatingInterceptor` | Version E2E avec support de reassociation |

**Source :** Fichiers de test

### 3.4 Tests SQL Server conditionnels

L'attribut `[SqlServerFact]` ignore les tests lorsque la variable d'environnement `MOTHERSON_TEST_SQL_CONNECTION` n'est pas definie. Permet les tests d'integration SQL Server reels dans le CI.

**Source :** `MothersonBoxManagement.Tests/SqlServerIntegrationTests.cs`

## 4. Categories de tests

### 4.1 Authentification et autorisation (26 tests)
- Rendu du formulaire de connexion, connexion valide/invalide pour les 3 roles
- Validation du format de matricule
- Redirections non authentifiees
- Acces aux points de terminaison base sur les roles (l'Operateur ne peut pas atteindre les points de terminaison Superviseur)
- Flux complet de recuperation de mot de passe
- Rejet de cookie d'utilisateur desactive
- Protection du principal SYSTEM

### 4.2 Cycle de vie des boites (21 tests)
- Creation de boites a partir de modeles
- Details de boite, gestion de boite inexistante
- Generation de numero de boite
- Annulation de boite, blocage/deblocage
- Transfert de colis, retrait, dissociation
- Reassociation de colis apres dissociation
- Recherche de boites avec filtres

### 4.3 Scan de colis (17 tests)
- Scan automatique avec correspondance de prefixe
- Le prefixe le plus long gagne
- Exclusion de modeles inactifs
- Gestion de code-barres vide
- Rejet du code-barres de boite en tant que colis
- Atomicite (l'echec annule tout)
- Comportement du mode sticky
- Completion de boite

### 4.4 Concurrency et integrite des donnees (10 tests)
- Repetition en concurrence optimiste
- Gestion des codes-barres en doublon
- Classification des erreurs SQL (2601, 2627, 1205)
- Validation des contraintes CHECK
- Application des contraintes uniques

### 4.5 Audit et conformite (6 tests)
- Controle d'acces a l'audit
- Filtrage et pagination
- Journalisation des rejets
- Principal SYSTEM pour les operations d'arriere-plan

### 4.6 Print Agent (20 tests)
- Appairage, heartbeat, cycle de vie de travail
- Rendu ZPL
- Controle d'acces a la gestion de flotte
- Verification de l'impression navigateur uniquement

### 4.7 Securite (13 tests)
- Validation des entrees
- Gestion des exceptions (pas de fuite de donnees sensibles)
- Concurrence de verrouillage
- Rejet du seed demo en production
- Accessibilite des points de terminaison de sante

### 4.8 Tableau de bord (10 tests)
- Rendu pour tous les roles
- Recherche de code-barres
- Selection de modeles
- Page d'erreur

## 5. Patterns de test

| Pattern | Utilisation |
|---------|-------------|
| `WebApplicationFactory<Program>` | Pattern principal de test d'integration (18 fichiers sur 22) |
| EF Core InMemory | Base de donnees de test (unique par factory) |
| `IClassFixture<CustomWebApplicationFactory>` | Factory partagee par classe de test |
| `TestAuthHelper` | Creation de client HTTP authentifie |
| Resolution directe de service | Contournement du pipeline HTTP pour les tests unitaires |
| `[Theory]` + `[InlineData]` | Regles de validation, valeurs d'enum, hachage de mot de passe |
| `[SqlServerFact]` | Tests SQL Server conditionnels |
| Intercepteurs personnalises | Simulation de comportements SQL Server dans InMemory |
| `FakeAntiforgery` | Saut du CSRF dans les tests |

## 6. Statut du build et des tests

### 6.1 Derniers resultats connus

| Metrique | Valeur | Source |
|----------|--------|--------|
| Build | 0 avertissements, 0 erreurs | production-readiness-audit (2026-07-13) |
| Tests | 162 reussis, 0 echoues | production-readiness-audit (2026-07-13) |
| Tests SQL Server | 2 reussis (aucun ignore) | production-readiness-audit (2026-07-13) |
| Vulnerabilites NuGet | 0 signalees | production-readiness-audit (2026-07-13) |
| Couverture (lignes) | 16,69% | production-readiness-audit (2026-07-13) |
| Couverture (branches) | 58,39% | production-readiness-audit (2026-07-13) |

> Note : Le nombre de tests a varie au cours de la vie du projet (129 -> 134 -> 144 -> 148 -> 152 -> 153 -> 162 -> 172). Le dernier nombre verifie par l'audit de preparation a la production est de 162 tests reussis.

**Source :** `docs/production-readiness-audit/07-TEST-REPORT.md`, `AGENT.md` lignes 358-366

### 6.2 Limitations de test connues

| Ecart | Description |
|-------|-------------|
| Pas de tests unitaires isoles | Tous les tests de service passent par le WebApplicationFactory complet |
| Pas de tests CRUD de gestion des utilisateurs | Creation/modification/suppression d'utilisateurs non directement testee |
| Pas de tests de statistiques du tableau de bord | Les KPI ne sont pas testes |
| Pas de cas limites de l'API PrintAgent | JSON malforme, leases invalides non testes |
| Pas de test fonctionnel ArchiveBox | Le point de terminaison existe mais n'est pas teste |
| Pas de flux d'impression de bout en bout | Scan-completion-impression complet non teste dans un seul test |
| Pas de creation concurrente de modeles | Tests de concurrence uniquement pour le scan |
| Pas de test de charge | Aucun benchmark de performance |
| Pas de test d'accessibilite | Conformite WCAG non verifiee |

**Source :** `docs/production-readiness-audit/07-TEST-REPORT.md`

## 7. Dette technique

| Element | Severite | Description |
|---------|----------|-------------|
| Couplage de BoxService | P3 | 651 lignes, 4 interfaces (couplage eleve) |
| Scan automatique dans le controleur | P3 | Logique d'orchestration dans BoxController.AutoScanPackage, pas dans le service |
| Docs d'architecture obsoletes | P2 | ARCHITECTURE.md desynchronise du code |
| PrintAgent dans la solution | P3 | Les projets PrintAgent sont toujours dans .sln mais la fonctionnalite a ete remplacee |
| Couverture de test | P3 | 16,69% des lignes (objectif : 80%) |
| Limiteur de debit global | P2 | Pourrait bloquer toute l'usine (necessite un partitionnement par IP) |
| Pas de SAST/DAST/SCA | P2 | Pas de scan de securite automatisé |
| Propagation de CancellationToken | P3 | Manquante dans certaines methods du service de verrouillage |

**Source :** `docs/production-readiness-audit/`, `AGENT.md` lignes 275-278

## 8. Resume du statut du projet

### 8.1 Statut des jalons

- **v1.0 MVP :** Complete (2026-07-05)
- **5 phases de developpement :** Toutes completes
- **13 plans :** Tous completes
- **50 taches dans TODO.md :** 48 completes, 2 en revue

### 8.2 Preparation a la production

**Verdict : NON-VALIDE pour le deploiement en usine** (au 2026-07-13)

**Valide :**
- Le build reussit (0 avertissements, 0 erreurs)
- 162 tests reussis
- Securite des cookies, roles cote serveur, modele de donnees statique
- TLS echec rapide, points de terminaison de sante
- L'image Docker se construit et passe la verification de sante
- Sauvegarde/restauration reussie (1,64s)
- Aucun paquet NuGet vulnerable

**Non valide :**
- Test E2E navigateur/scanner/imprimante sur le materiel cible
- Architecture d'impression sur le poste de travail cible
- Test de charge
- Accessibilite UI/UX (WCAG 2.2 AA)
- TLS/reseau sur l'infrastructure cible
- Gestion des secrets sur la cible
- Supervision et alertes

**Source :** `docs/production-readiness-audit/10-GO-NO-GO-CHECKLIST.md`
