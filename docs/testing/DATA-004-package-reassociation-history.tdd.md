# Preuves TDD — DATA-004

## Invariant retenu

Un barcode package ne peut avoir qu'une association active, mais peut conserver plusieurs associations historiques retirées. Une désassociation approuvée libère donc le barcode pour une nouvelle box sans effacer la ligne précédente.

## RED

`DisassociatedPackage_CanBeReassociatedWhileRetainingHistory` créait une association, annulait la box, désassociait le package puis tentait de le scanner dans une autre box. Avant correction, `reassociation.Success` était `false`.

## GREEN

- `PackageScanService` ne considère comme doublon qu'une ligne `!IsRemoved`.
- La recherche package/box ignore également les associations retirées.
- EF configure `IX_BoxPackages_PackageBarcode` unique avec filtre `[IsRemoved] = 0`.
- La migration `20260712200405_AllowHistoricalPackageReassociation` remplace l'index global par l'index filtré.
- Le rollback refuse explicitement de restaurer l'unicité globale si des réassociations historiques rendent ce downgrade destructif.
- Le simulateur InMemory suit la même notion d'association active.

Résultats : test ciblé 1/1, suite complète 148/148, build Release 0 warning/0 erreur. Le script SQL généré contient `CREATE UNIQUE INDEX ... WHERE [IsRemoved] = 0`.

| Garantie | Résultat |
|---|---|
| Une association active bloque toujours un doublon | PASS via tests existants |
| Une association retirée libère le barcode | PASS |
| L'ancien historique est conservé avec sa box source | PASS |
| La nouvelle ligne active référence la destination | PASS |

Couverture globale : 18,89 % lignes, 59,44 % branches, sous 80 %. L'application de la migration et une course de deux réassociations simultanées sur SQL Server restent rattachées à `QA-001`.

Aucun commit checkpoint n'a été créé afin de préserver les modifications utilisateur préexistantes des mêmes fichiers.
