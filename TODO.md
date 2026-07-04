# TODO.md — Tableau de pilotage Motherson Box Management

## Légende des priorités
* **P0** = Bloque le MVP, la sécurité ou l'intégrité des données
* **P1** = Fonctionnalité MVP essentielle
* **P2** = Amélioration importante mais non bloquante
* **P3** = Amélioration future ou dette technique

## Tableau des tâches

| ID | Référence GSD | Fonctionnalité / tâche | Exigences CDC liées | Priorité | Statut | Responsable | Dernière mise à jour | Blocage / notes | Référence technique |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **TSK-001** | Phase 1 | Initialisation de la solution MVC (.NET 8.0, structure des dossiers) | - | P0 | `Terminé` | Agent | 2026-07-02 | Aucun | `Program.cs`, `.csproj` |
| **TSK-002** | Phase 1 | Schéma de base de données (Entités EF Core, Context, configurations) | DONNÉES-01 | P0 | `Terminé` | Agent | 2026-07-02 | Aucun | `ApplicationDbContext.cs` |
| **TSK-003** | Phase 1 | Contrainte d'unicité SQL sur `BoxPackages.PackageBarcode` | SCAN-03 | P0 | `Terminé` | Agent | 2026-07-02 | Clé pour l'intégrité des données | EF Core Fluent API |
| **TSK-004** | Phase 1 | Migrations EF Core initiales et script de Seed (Utilisateurs et Rôles) | DECISION-03 | P0 | `Terminé` | Agent | 2026-07-02 | Doit pré-seed les rôles et users de test | `/Migrations` |
| **TSK-005** | Phase 1 | Authentification par matricule / mot de passe (Cookie Auth) | AUTH-01, AUTH-02 | P0 | `Terminé` | Agent | 2026-07-02 | Session persistante requise | `AccountController.cs` |
| **TSK-006** | Phase 1 | Gestion et contrôle d'accès par rôles (Opérateur, Superviseur, Admin) | AUTH-03 | P0 | `Terminé` | Agent | 2026-07-02 | Filtres d'autorisation MVC | `[Authorize(Roles = "...")]` |
| **TSK-007** | Phase 2 / Plan 02-01 | Création de box (Type, Dimensions, Qté attendue) | BOX-01 | P1 | `Terminé` | Agent | 2026-07-02 | Validé par tests de compilation et d'intégration | `BoxController.cs` |
| **TSK-008** | Phase 2 / Plan 02-02 | Génération des numéros et codes-barres uniques de boxes | BOX-02, BOX-03 | P1 | `Terminé` | Agent | 2026-07-02 | Validé par tests unitaires et génération avec retry | `IBoxService` |
| **TSK-009** | Phase 2 / Plan 02-03 | Tableau de bord des boxes ouvertes et progression | BOX-07, BOX-08 | P1 | `Terminé` | Agent | 2026-07-02 | Validé par tests unitaires et intégration UI | `HomeController.cs` |
| **TSK-010** | Phase 2 / Plan 02-03 | Accès direct par scan de box sur la page d'accueil | HOME-01, HOME-02 | P1 | `Terminé` | Agent | 2026-07-02 | Validé par tests de lookup et de redirection | Champ de scan distinct |
| **TSK-011** | Phase 3 | Écran de préparation de box et simulateur de scan virtuel | SIM-01 | P1 | `Terminé` | Agent | 2026-07-03 | Panel de simulation dans l'UI | `Views/Box/Prepare.cshtml` |
| **TSK-012** | Phase 3 | Scan et affectation des packages de câbles (USB Wedge / JS listener) | SCAN-01, SCAN-02 | P1 | `Terminé` | Agent | 2026-07-03 | Doit intercepter la saisie | Script JS scanner |
| **TSK-013** | Phase 3 | Validation d'unicité et distinction des formats de codes-barres | SCAN-04, SCAN-05 | P0 | `Terminé` | Agent | 2026-07-03 | Rejeter codes boxes sur scan package et vice versa | `IScanService` |
| **TSK-014** | Phase 3 | Clôture automatique de box (quantité attendue atteinte) | SCAN-06 | P1 | `Terminé` | Agent | 2026-07-03 | Transition vers statut "Completed" | `IScanService` / `Box` |
| **TSK-015** | Phase 3 | Gestion des scans concurrents et de la concurrence optimiste | - | P0 | `Terminé` | Agent | 2026-07-03 | Gérer DbUpdateConcurrencyException | EF Core `RowVersion` |
| **TSK-016** | Phase 4 | Journal d'audit append-only immuable | AUDIT-01, AUDIT-02 | P0 | `Terminé` | Agent | 2026-07-04 | SaveChangesInterceptor pour EF Core | `BoxAuditLogInterceptor` |
| **TSK-017** | Phase 4 | Reprise de box ouverte par un autre opérateur | BOX-04 | P1 | `Terminé` | Agent | 2026-07-04 | Conserver créateur initial | `IBoxService` |
| **TSK-018** | Phase 4 | Clôture exceptionnelle avec écart (Superviseur uniquement + motif) | EXC-02 | P1 | `Terminé` | Agent | 2026-07-04 | CompletedWithException + CompletionMode=Forced | `BoxController.cs` |
| **TSK-019** | Phase 4 | Annulation de box (Superviseur uniquement + motif) | EXC-01 | P1 | `Terminé` | Agent | 2026-07-04 | Statut Cancelled | `BoxController.cs` |
| **TSK-020** | Phase 4 | Blocage et déblocage de boxes / packages (Quarantaine) | EXC-04 | P1 | `Terminé` | Agent | 2026-07-04 | Statut Blocked | `BoxController.cs` |
| **TSK-021** | Phase 4 | Retrait, transfert et désaffectation contrôlés de packages | EXC-03 | P1 | `Terminé` | Agent | 2026-07-04 | Traçabilité avec motif obligatoire | `IBoxService` |
| **TSK-022** | Phase 4 | Recherche multicritères et filtres (Statut, numéro, date, user) | - | P2 | `Terminé` | Agent | 2026-07-04 | Pour superviseurs et admins | `BoxController.cs` |
| **TSK-023** | Phase 5 | Tests unitaires et d'intégration métier | - | P1 | `Terminé` | Agent | 2026-07-04 | Aucun | Projet de tests |
| **TSK-024** | Phase 5 | Audit de sécurité (Vérification secrets, logs et injection SQL) | - | P0 | `Terminé` | Agent | 2026-07-04 | Aucun | Analyse statique de code |
| **TSK-025** | Phase 5 | Préparation de la configuration de déploiement local (IIS / Kestrel) | - | P2 | `Terminé` | Agent | 2026-07-04 | Aucun | `appsettings.json` |

## Règles de maintenance
1. **Lien GSD Core :** Associer impérativement chaque tâche à la phase active ou au plan GSD Core correspondant.
2. **Cycle de Vie des Tâches :**
   * Passer à `En cours` dès le début du travail sur la tâche.
   * Si un élément bloque la tâche, la passer en `Bloqué` et commenter le blocage dans la colonne "Blocage / notes".
   * Passer à `En revue` lorsque le code est écrit mais non validé globalement.
   * Ne passer à `Terminé` qu'après :
     * Validation du build (`dotnet build` sans erreur ni warning majeur).
     * Exécution des tests unitaires (`dotnet test` au vert).
     * Validation manuelle des parcours d'écrans.
     * Mise à jour de la documentation dans `AGENT.md` (schéma, ADR, migrations, etc.).
3. **Synchronisation :** Les statuts de ce fichier doivent être le miroir exact de l'état d'avancement réel du projet.
4. **Intégrité métier :** Ne jamais forcer le statut `Terminé` si un comportement métier ou de sécurité n'a pas été formellement testé et prouvé.
