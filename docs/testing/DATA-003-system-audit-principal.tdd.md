# Preuves TDD — DATA-003

## Parcours utilisateur

En tant que responsable d'audit, je veux que les opérations automatiques soient attribuées à un principal système distinct afin qu'aucune action machine ne soit imputée à tort à un administrateur humain.

## RED

1. `AuditWithoutHttpContext_IsAttributedToInactiveSystemPrincipal` attendait SYSTEM mais recevait AD001 : attendu 4, réel 3.
2. `SystemAuditPrincipal_CannotBeModifiedOrDeleted` attendait un refus de modification, mais aucune exception n'était levée.

## GREEN

- `SystemPrincipal` réserve l'ID `-1`, le matricule `SYSTEM` et le rôle `System`.
- EF seed crée cet utilisateur inactif avec mot de passe de connexion désactivé.
- La migration `20260712150705_AddSystemAuditPrincipal` insère la ligne avec `IDENTITY_INSERT`.
- L'intercepteur utilise exclusivement ce principal hors contexte authentifié et échoue explicitement s'il manque.
- `UserService` masque SYSTEM des listes et interdit lecture administrative directe, modification, reset et suppression ; le matricule est réservé.

Résultats : tests ciblés 2/2, suite complète 146/146, build Release sans warning ni erreur. Le script SQL temporaire a été généré et contient l'insertion SYSTEM ainsi que l'entrée d'historique EF.

| Garantie | Test | Résultat |
|---|---|---|
| Une action sans HttpContext n'est jamais attribuée à AD001 | `AuditWithoutHttpContext_IsAttributedToInactiveSystemPrincipal` | PASS |
| SYSTEM reste inactif et distinct d'un humain | même test | PASS |
| SYSTEM ne peut être modifié, réinitialisé ou supprimé | `SystemAuditPrincipal_CannotBeModifiedOrDeleted` | PASS |

## Couverture et limites

Couverture globale : 19,21 % lignes et 57,84 % branches, sous 80 %. La migration a été compilée et son SQL généré, mais son application sur une vraie base SQL Server reste **Non vérifiée** car aucune instance n'était accessible.

Aucun commit checkpoint n'a été créé afin de ne pas mélanger les modifications utilisateur préexistantes des mêmes fichiers.
