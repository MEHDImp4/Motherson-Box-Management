# Checklist Go/No-Go au 13 juillet 2026

> Mise à jour après remédiation: build et publication Linux, tests locaux, tests SQL réels, image Development, migration depuis zéro et backup/restore sont validés. L’agent Windows a été retiré au profit de l’impression navigateur; l’E2E navigateur/scanner, l’impression physique et la charge usine restent non validés, donc le verdict NO-GO usine est maintenu.

| Domaine | Contrôle | État |
|---|---|---|
| Build | Compilation Release, 0 warning/erreur | Validé |
| Publication | Publication Release locale | Validé |
| Tests | 154 tests locaux réussis | Validé |
| Tests | SQL Server réel/migrations/concurrence, zéro skip | Non validé |
| Tests | E2E navigateur/scanner/imprimante | Non validé |
| Tests | Charge, latence, volumétrie | Non vérifié |
| Sécurité | Aucun hash/secret dans audit | Non validé |
| Sécurité | Seed privilégié connu impossible en Production | Non validé |
| Auth | Hash Identity et cookie sécurisé | Validé |
| Auth | Lockout atomique sous concurrence | Non validé |
| Autorisations | Rôles contrôlés côté serveur | Validé |
| Données | Unicités, CHECK, `rowversion`, idempotence dans modèle | Validé statiquement |
| Données | Schéma réellement déployé conforme | Non vérifié |
| Traçabilité | Acteur/actions/transfert auditables | Validé partiellement |
| UI/UX | Focus, erreurs, perte réseau, tactile/gants | Non validé |
| Accessibilité | axe/NVDA/clavier/contraste | Non vérifié |
| Impression | Architecture réelle et matériel validés | Non validé |
| Conteneur | Healthcheck cohérent par profil | Non validé |
| Production | TLS et configuration fail-fast | Validé par code |
| Réseau | Certificat, DNS, firewall, NTP | Non vérifié |
| Secrets | Stockage/rotation/moindre privilège | Non vérifié |
| Sauvegarde | Automatisation, checksum, rétention, chiffrement | Non vérifié |
| Restauration | Restauration chronométrée et rollback | Non validé |
| Résilience | Réseau/SQL/spooler/disque/redémarrage | Non validé |
| Monitoring | Readiness/liveness disponibles | Validé |
| Monitoring | Centralisation, métriques et alertes testées | Non validé |
| Documentation | Procédure installation/exploitation | Validé partiellement |
| Documentation | Architecture à jour | Non validé |
| Organisation | Formation opérateurs | Non vérifié |
| Organisation | Support/astreinte/owner incident | Non vérifié |
| Métier | RPO/RTO et règles d'impression approuvés | Non vérifié |
| Métier | Validation Production/Qualité/IT | Non vérifié |

## Verdict

**NO-GO production générale.** Un nouveau passage Go/No-Go est requis après fermeture des huit P1. Aucun GO ne peut être prononcé tant qu'un P1 bloquant reste ouvert ou qu'une preuve critique d'environnement est absente.
