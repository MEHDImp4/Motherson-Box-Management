# Preuves TDD — FUNC-002

## Invariant retenu

Un package bloqué est en quarantaine et ne compte pas dans la quantité valide. Si son retrait rend une box complétée incomplète, la box redevient ouverte. Son déblocage réintègre exactement une unité et peut compléter automatiquement la box.

## RED

Test : `BlockPackage_ExcludesItFromValidQuantity_AndUnblockRestoresCompletion`.

Après correction d'une assertion DTO invalide, le RED métier était : quantité attendue après blocage `0`, quantité réelle `1`.

## GREEN

- Blocage : refuse package retiré/déjà bloqué, décrémente sans valeur négative, rouvre une box complétée et efface ses métadonnées de complétion.
- Déblocage : refuse package retiré/non bloqué et dépassement, incrémente une seule fois, complète automatiquement à la cible avec l'acteur superviseur.

Résultats : test ciblé 1/1, suite complète 147/147, build Release 0 warning/0 erreur.

| Garantie | Résultat |
|---|---|
| Un package quarantainé ne compte plus | PASS |
| Une box complétée redevient Open si elle n'atteint plus la cible | PASS |
| Le déblocage restaure quantité et complétion | PASS |
| Doubles blocages/déblocages sont refusés | PASS |
| L'acteur du déblocage devient l'acteur de complétion | PASS |

Couverture globale : 19,88 % lignes, 59,28 % branches, sous 80 %. Les courses SQL Server réelles sur blocage/déblocage restent couvertes par la limite générale `QA-001`.

Aucun commit checkpoint n'a été créé pour préserver les modifications utilisateur préexistantes des mêmes fichiers.
