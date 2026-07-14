# Plan de remédiation

## Vague 0 — Blocages absolus

| Ordre | IDs | Action | Dépendances | Effort | Validation |
|---:|---|---|---|---|---|
| 1 | SEC-002 | Expurger `PasswordHash`, évaluer/purger l'historique exposé | Politique sécurité | F | Tests création/reset: aucune valeur sensible |
| 2 | SEC-003 | Rendre le lockout atomique | SQL Server | M | Tentatives concurrentes, seuil jamais contourné |
| 3 | SEC-001 | Neutraliser seed connu et faire tourner secrets utilisés | Gestion secrets | F | Production refuse seed; scan local propre |
| 4 | OPS-002 | Décider architecture d'impression (Windows ou service réseau) | IT usine/imprimante | É | Étiquette réelle, erreur/reprise/audit validés |
| 5 | OPS-001 | Aligner healthcheck par profil | Choix hébergement | F | Compose local et prod `healthy` |
| 6 | QA-001 | Exécuter migrations, contraintes et concurrence sur SQL identique cible | Instance SQL éphémère/copie | M | Zéro skip, rapport schéma/index/contraintes |
| 7 | OPS-004 | Exercer backup, restore, rollback et pannes | Stockage/DBA | É | RPO proposé ≤15 min, RTO proposé ≤2 h, à approuver métier |
| 8 | QA-002 | Recette E2E navigateur/scanner/imprimante | Matériel et comptes test | É | Matrice critique réussie, preuves/captures |

## Vague 1 — Sécurisation préproduction

- `SEC-004`: partitionner et tester le rate limit au pic.
- `FUNC-001`: interdire ou prioriser les préfixes actifs ambigus.
- `DATA-001`/`DATA-002`: contrôle de réconciliation et réponse opérateur explicite après conflit persistant.
- `UX-001..003`: focus, feedback persistant et états réseau confirmés.
- `OPS-005`/`OPS-007`/`OPS-008`: SBOM/scan, observabilité/alertes, revue TLS-DNS-NTP-firewall-secrets.

Validation: tests automatisés ciblés, smoke de l'artefact immuable, alerte provoquée et approbation IT/Sécurité/Production.

## Vague 2 — Stabilisation

- `ARCH-001`/`ARCH-003`: documentation réelle et extraction de l'orchestrateur auto-scan.
- Test de charge: scan API p95 cible ≤300 ms et feedback opérateur ≤500 ms sur LAN; aucune double association/perte.
- Mesurer CPU/RAM/SQL, profondeur/âge de file d'impression et saturation disque.

## Vague 3 — Améliorations

- `FUNC-003`, `API-001`, `ARCH-002`, `UX-004`, `QA-005`, `QA-007`.
- Validation par non-régression, collecte de couverture correcte, tests clavier/NVDA/axe et recette avec gants.

Chaque vague doit être réalisée sur un lot logique minimal, avec tests, revue du diff et mise à jour de `08-FINDINGS-REGISTER.md`. La fermeture d'un constat exige une preuve, pas seulement une modification.
