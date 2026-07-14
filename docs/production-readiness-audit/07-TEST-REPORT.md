# 07 — Rapport QA, performance et fiabilité

**Verdict QA : build et suite locale verts, mais gate production incomplet.** Le checkout courant compile et ses tests automatisés passent; couverture, SQL Server réel, navigateur, charge, panne et matériels industriels ne sont pas validés dans cette exécution.

## Exécution reproductible du 13 juillet 2026

| Gate | Commande | Résultat |
| --- | --- | --- |
| Build Release | `dotnet build Motherson_Box_Management.sln -c Release --no-restore` | **PASS** — 0 warning, 0 erreur; 11,23 s. |
| Tests | `dotnet test ... -c Release --no-build --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"` | **PASS** — 156 total, 154 réussis, 0 échec, 2 ignorés; 25,0667 s. |
| Couverture | `coverage.cobertura.xml` de cette exécution | **INVALIDE/INEXPLOITABLE** — `line-rate=0`, 0/13 856 lignes et 0/1 818 branches. La collecte lancée avec `--no-build` n'a pas instrumenté l'assembly; ne pas présenter 0 % comme couverture réelle ni revendiquer un seuil. |
| Publication | `dotnet publish ... -c Release --no-build` | **PASS** — artefact produit dans un dossier temporaire. |
| Compose sans secrets | `docker compose config --quiet` | **FAIL attendu** — `MOTHERSON_ALLOWED_HOSTS` requis et absent. Cela prouve le fail-fast de cette variable, pas le démarrage production. |
| Vulnérabilités NuGet | `dotnet list ... package --vulnerable --include-transitive` | Application: aucune signalée. Tests: `System.Net.Http` 4.3.0 et `System.Text.RegularExpressions` 4.3.0, transitifs, sévérité High. |
| Paquets obsolètes | `dotnet list ... package --outdated` | Mises à jour majeures disponibles (EF/ASP.NET 10, test SDK/xUnit/coverlet); information de maintenance, pas recommandation de saut majeur immédiat. |

Les deux tests ignorés sont les scénarios SQL conditionnels; aucune variable/instance SQL de test n'était disponible dans cette exécution. Les logs `fail` visibles pendant certains tests proviennent de scénarios injectant volontairement des exceptions et n'ont pas fait échouer la suite.

## Évaluation des tests

### Couverture démontrée

- Contrôleurs et autorisations, erreurs et validation.
- Services métier, concurrence/idempotence simulée, contraintes décrites par le modèle.
- Audit et principal système, verrouillage/authentification.
- Génération ZPL, gestion de file d'impression et faux spooler.
- Flux MVC importants via `WebApplicationFactory` et EF InMemory.

### Lacunes prioritaires

| ID | Prio. | Lacune / risque | Gate proposé |
| --- | --- | --- | --- |
| QA-001 | P1 | Les 2 tests SQL réels sont ignorés; EF InMemory ne prouve pas migrations, transactions, verrouillages, collations ni codes d'erreur du provider. | CI éphémère SQL Server: migration depuis vide et N-1, contraintes, concurrence réelle et rollback. Zéro skip sur branche release. |
| QA-002 | P1 | Aucun E2E navigateur/scanner/imprimante n'a été exécuté. | Playwright sur Chrome/Edge avec rôles, scan HID simulé, erreur réseau, impression et axe; recette matérielle séparée. |
| QA-003 | P1 | Aucune mesure de charge/latence, file d'impression ou consommation. | Test au volume usine et pic: p50/p95/p99, taux d'erreur, débit scans/s, CPU/RAM/DB et profondeur/âge de file. |
| QA-004 | P1 | Pannes non injectées: perte réseau avant/après commit, SQL indisponible, redémarrage worker, spooler bloqué, disque plein. | Tests de reprise démontrant absence de double association/impression, retry borné et état opérateur récupérable. |
| QA-005 | P2 | Couverture non mesurée; aucun seuil CI. | Relancer sans `--no-build` ou avec instrumentation explicite, publier Cobertura, fixer progressivement un seuil sur code métier critique (cible recommandée 80 %, à valider). |
| QA-006 | P2 | CI ne traite ni analyse statique/vulnérabilités, ni compose prod, migration, healthcheck et smoke de l'image. | Ajouter gates Release reproductibles et conserver rapports/artefacts. |
| QA-007 | P2 | Les workers hébergés démarrent pendant de nombreux tests, allongeant/bruitant les logs. | Neutraliser explicitement les workers hors tests qui les ciblent; garder une suite d'intégration dédiée au cycle de vie. |

## Objectifs de performance proposés (non mesurés)

- Retour visuel opérateur après scan ≤ 500 ms et API scan p95 ≤ 300 ms sur LAN cible.
- 0 double association et 0 perte sous rafale nominale/pic, retry réseau et redémarrage.
- Readiness répond en < 3 s (timeout code actuel) et bascule 503 lorsque SQL est indisponible.
- File d'impression: âge et profondeur plafonnés avec alerte; reprise sans double impression selon règle métier à formaliser.

Ces valeurs sont des critères de recette proposés, pas des résultats.

## Décision QA

Le build et la régression locale sont **PASS**. Le candidat est **NO-GO production générale** tant que QA-001 à QA-004 ne sont pas exécutés avec succès sur un environnement représentatif. Un pilote contrôlé peut être envisagé seulement après SQL réel, smoke de l'image, correction du healthcheck, impression physique et plan de rollback éprouvé.
