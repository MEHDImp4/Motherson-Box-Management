# Registre consolidé des constats ouverts

## Statut de remédiation actuel

| ID | Statut | Preuve de fermeture ou reste à faire |
|---|---|---|
| SEC-001 | Fermé | Secrets de seed externes, distincts; Production refuse `SeedDemoUsers`; base test recréée |
| SEC-002 | Fermé | Allowlist audit + test de reset sans `PasswordHash` |
| SEC-003 | Fermé applicativement | Section critique sérialisable + gate par matricule + test concurrent; SQL réel à conserver en CI |
| OPS-001 | Fermé | Healthcheck Development HTTP/8080; Production HTTPS/8443 |
| OPS-002 | Architecture remplacée | Agent Windows retiré; impression exclusivement via le navigateur, à valider sur le poste et l’imprimante cibles |
| QA-001 | Fermé | 2/2 tests SQL Server 2022 réels, zéro skip dans l’exécution dédiée |
| OPS-004 | Fermé sur test, cible ouverte | Backup checksum + restore réussis en 1,64 s; exercice infrastructure cible requis |
| QA-002 | Ouvert | Recette navigateur/scanner/imprimante physique non réalisable localement |
| OPS-005 | Fermé | Dépendances de tests mises à jour; aucun paquet vulnérable signalé; gates CI ajoutés |
| QA-005 | Ouvert | Couverture 16,69 % lignes / 58,39 % branches, objectif 80 % non atteint |

**P1 ouverts avant production usine: 2 gates liées à OPS-002 et QA-002.**

La relecture contradictoire a supprimé le faux positif initial sur l'absence de retry des opérations superviseur: `BoxService.ExecuteWithConcurrencyRetryAsync` effectue bien transaction, rollback et trois tentatives.

| ID | Domaine | Sévérité | Titre | Preuve | Impact | Recommandation | Effort | Bloquant |
|---|---|---:|---|---|---|---|---:|---|
| SEC-001 | Auth | P1 | Comptes démo/admin à secret constant | `DbInitializer.cs:12-29` | Accès privilégié si mauvais profil | Seed à usage unique, secret externe, rotation | F | Oui |
| SEC-002 | Audit | P1 | `PasswordHash` sérialisé | `AuditSaveChangesInterceptor.cs:284-329`; `User.cs:8` | Attaque hors ligne/fuite sensible | Allowlist/redaction + purge et test | F | Oui |
| SEC-003 | Auth | P1 | Lockout non atomique | `LoginLockoutService.cs:55-81` | Contournement sous concurrence | UPDATE atomique/rowversion + test SQL | M | Oui |
| OPS-001 | Déploiement | P1 | Healthcheck image/compose incohérent | `Dockerfile:15-27`; `docker-compose.yml:29-33` | Conteneur déclaré unhealthy | Aligner protocole/port par profil | F | Oui |
| OPS-002 | Impression | P1 | Faux spooler dans image Linux | `Program.cs:50-53`; image Linux | Aucune impression usine réelle | Choisir Windows/service réseau et tester | É | Oui |
| QA-001 | SQL/QA | P1 | Gate SQL/migrations/concurrence ignoré | `SqlServerIntegrationTests.cs:11-17` | Garanties DB non prouvées | CI SQL obligatoire, zéro skip release | M | Oui |
| OPS-004 | Reprise | P1 | Restore/rollback/pannes non éprouvés | `ops/sql/*`; aucune preuve cible | Perte de données/arrêt prolongé | Exercice chronométré + RPO/RTO | É | Oui |
| QA-002 | E2E | P1 | Scanner/navigateur/imprimante non testés | Aucun résultat Playwright/matériel | Blocage ou erreur opérateur | Recette E2E et matériel | É | Oui |
| SEC-004 | Disponibilité | P2 | Limiteur global partagé | `RateLimitingConfiguration.cs:38-43` | DoS collectif involontaire | Partitionner par identité/IP/route | M | Non |
| FUNC-001 | Métier | P2 | Préfixes de modèles ambigus | `BoxTemplateService.cs:166-175` | Mauvais type de box | Unicité/priorité explicite | M | Non |
| DATA-001 | Données | P2 | Compteur dénormalisé sans réconciliation | `Box.CurrentQuantity`; mutations services | Drift hors voies nominales | Job/contrôle de cohérence | M | Non |
| UX-001 | UX | P2 | Focus scanner global | `scanner.js` | Saisie supervision perturbée | Suspendre sur contrôles/modales; test clavier | M | Non |
| UX-002 | A11y | P2 | Feedback fugace/non démontré `aria-live` | `scanner.js` | Message manqué | Erreur persistante et région live | M | Non |
| UX-003 | Réseau/UX | P2 | Rejeu sous réponse perdue non validé E2E | `scanner.js`; `ScanRequestId` | État opérateur ambigu | États confirmé/incertain + test perte réponse | M | Non |
| OPS-005 | Supply/CI | P2 | Pas de digest/SBOM et transitifs vulnérables dans tests | Docker/CI; `dotnet list package --vulnerable` | Dérive/vulnérabilité de chaîne de test | Mettre à jour dépendances, gates et artefact promu | M | Non |
| OPS-007 | Observabilité | P2 | Pas de collecte/alertes centrales | `Program.cs`; compose prod | Incident non détecté/corrélé | Centraliser métriques/logs et tester alerte | M | Non |
| OPS-008 | Infrastructure | P2 | Topologie cible non vérifiée | Certificat/DNS/NTP/firewall/secrets absents | Déploiement non maîtrisé | Revue infra signée | M | Non |
| ARCH-001 | Documentation | P2 | Architecture obsolète | `docs/ARCHITECTURE.md:31-149` | Exploitation induite en erreur | Synchroniser routes/modèle/flux | F | Non |
| ARCH-003 | Architecture | P2 | Transaction auto-scan dans contrôleur | `BoxController.cs:181-267` | Divergence future | Extraire service applicatif | M | Non |
| DATA-002 | Concurrence UX | P2 | Message générique après retries épuisés | `BoxService.cs:165-205`; contrôleur | Reprise opérateur ambiguë | Conflit explicite, audit, état rechargé | M | Non |
| FUNC-003 | Impression | P3 | Échec de job avalé | `BoxController.cs:204-215` | Box sans étiquette sans alerte | Avertir/auditer | F | Non |
| API-001 | Résilience | P3 | `CancellationToken` absent au lockout | `LoginLockoutService.cs` | Travail DB après abandon | Propager token | F | Non |
| ARCH-002 | Code | P3 | `BoxService` central de 651 lignes | `BoxService.cs` | Rayon d'impact élevé | Extraire progressivement | M | Non |
| UX-004 | UX/A11y | P3 | Cibles, son, langue et modales à homogénéiser | vues/CSS/JS | Confort/accessibilité | Design tokens + recette WCAG | M | Non |
| QA-005 | Couverture | P3 | Couverture inexploitable | Cobertura 0 non instrumenté | Angle mort métrique | Collecte instrumentée + seuil progressif | F | Non |
| QA-007 | Tests | P3 | Workers bruyants en tests | logs suite locale | Lenteur/bruit | Désactiver hors suites dédiées | F | Non |

Total dédupliqué: **0 P0, 8 P1, 12 P2, 6 P3**.
