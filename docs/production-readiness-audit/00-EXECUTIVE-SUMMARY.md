# Synthèse exécutive — audit du 13 juillet 2026

## État après remédiation du 13 juillet 2026

**Les blocages applicatifs SEC-001, SEC-002, SEC-003, OPS-001 et QA-001 sont fermés par code et tests.** L’agent d’impression Windows a été retiré à la demande du projet; l’étiquette est désormais imprimée uniquement via le navigateur. Le backup/restore Docker de test est réussi en 1,64 s. La décision reste **NO-GO usine** tant que la recette scanner et l’impression navigateur sur le matériel cible ne sont pas validées.

Preuves nouvelles: build Release 0 warning/0 erreur; 162 tests locaux réussis; 2/2 tests SQL Server réels réussis sans skip; publications Linux et win-x64 réussies; image Development healthy; aucun paquet NuGet vulnérable signalé. Couverture mesurée: 16,69 % lignes et 58,39 % branches, sous l’objectif de 80 %.

## Décision

**NO-GO pour un déploiement général sur le réseau usine.** Les défauts applicatifs P1 sont corrigés, mais les deux gates matérielles/environnementales restent ouvertes.

**Confiance globale : moyenne à élevée sur le code statique et les tests locaux; faible sur l'environnement industriel cible.** L'audit porte sur un checkout très modifié et non validé par rapport à `main`. Aucun code n'a été corrigé pendant l'audit.

## Périmètre et preuves

- ASP.NET Core MVC .NET 8, EF Core/SQL Server, Razor/JavaScript scanner, impression ZPL et conteneurs.
- `dotnet build -c Release`: réussi, 0 warning/0 erreur.
- `dotnet test -c Release --no-build`: 154 réussis, 2 tests SQL Server ignorés, 0 échec.
- `dotnet publish -c Release --no-build`: réussi.
- `dotnet list package --vulnerable --include-transitive`: application sans paquet vulnérable signalé; projet de tests avec deux transitifs anciens classés High (`System.Net.Http` et `System.Text.RegularExpressions` 4.3.0). Ils ne sont pas des dépendances directes du runtime applicatif.
- Couverture: **Non vérifiée** (fichier produit inexploitable, instrumentation absente).
- Semgrep, CodeQL, Gitleaks, ZAP, navigateur, axe/Lighthouse, charge et matériel usine: **Non vérifiés**.

## Registre synthétique

| Sévérité | Nombre ouvert | Effet |
|---|---:|---|
| P0 | 0 | Aucun défaut critique confirmé |
| P1 | 8 | Bloque la production générale |
| P2 | 12 | Conditions de stabilisation/acceptation |
| P3 | 6 | Maintenabilité et confort |

## Huit blocages absolus

1. `SEC-002`: l'audit générique sérialise `PasswordHash`.
2. `SEC-003`: verrouillage de connexion read-modify-write non atomique.
3. `SEC-001`: comptes de démonstration, dont administrateur, à secret constant activables par mauvaise configuration.
4. `OPS-001`: healthcheck HTTPS de l'image incompatible avec le compose local HTTP.
5. `OPS-002`: l'image Linux injecte le faux spooler; impression réelle non disponible dans cette architecture.
6. `QA-001`: migrations, contraintes et concurrence SQL Server réelles non exécutées dans cette passe; deux tests ignorés.
7. `OPS-004`: sauvegarde, restauration chronométrée, rollback et reprise après panne non prouvés sur cible.
8. `QA-002`: aucune recette E2E navigateur/scanner/imprimante physique.

## Points forts

Authentification cookie avec hash Identity, autorisations serveur, anti-CSRF, CSP/HSTS, configuration Production fail-fast, TLS obligatoire en Production, index uniques/`rowversion`/idempotence de scan, audit des transferts, health endpoints, image non-root et suite locale verte.

## Tableau de notation

| Domaine | Note /5 | Confiance | Justification |
|---|---:|---|---|
| Architecture | 3.5 | Élevée | Couches claires; orchestration auto-scan dans contrôleur et docs obsolètes |
| Qualité du code | 3.5 | Élevée | Build propre; service central volumineux |
| Fonctionnalités | 3.5 | Moyenne | Parcours présents; terrain non exercé |
| Règles métier | 3.5 | Moyenne | Invariants principaux; préfixes ambigus |
| Sécurité | 2.0 | Élevée | Bon socle, fuite de hash dans audit |
| Authentification | 2.0 | Élevée | Hash/cookies corrects; seed et lockout à corriger |
| Autorisations | 4.0 | Élevée | Rôles appliqués côté serveur sur endpoints inspectés |
| Intégrité des données | 3.0 | Moyenne | Contraintes présentes; SQL réel et réconciliation requis |
| Concurrence | 3.0 | Moyenne | Retry/idempotence présents; gate SQL ignoré |
| Traçabilité | 2.5 | Élevée | Audit riche, mais fuite `PasswordHash` |
| UI/UX | 2.5 | Faible | Inspection statique seulement, feedback scanner à valider |
| Accessibilité | 2.0 | Faible | Aucun axe/NVDA/terrain |
| Performance | 1.5 | Faible | Aucune charge mesurée |
| Résilience | 1.5 | Faible | Pannes/restauration non exercées |
| Tests | 3.0 | Élevée localement | 154 pass; SQL/E2E/charge/pannes absents |
| Déploiement | 2.0 | Moyenne | Publication réussie; healthcheck/impression incompatibles |
| Sauvegarde/restauration | 1.5 | Faible | Scripts présents, exercice cible absent |
| Observabilité | 2.5 | Moyenne | Logs UTC/rotation; pas de centralisation/alertes |
| Maintenabilité | 3.0 | Élevée | Structure lisible; dette de couplage/documentation |
| Documentation | 2.5 | Élevée | Runbooks présents mais architecture désynchronisée |

La moyenne indicative est **2,7/5**; elle ne neutralise aucun P1.

## Recommandation finale

Traiter la Vague 0, produire les preuves SQL/restauration/E2E matériel, puis refaire un audit de fermeture sur un commit candidat immuable. Ne pas corriger automatiquement sans instruction explicite.
