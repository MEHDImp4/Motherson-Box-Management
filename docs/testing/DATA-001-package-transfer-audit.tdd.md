# Preuves TDD — DATA-001

## Parcours utilisateur

En tant que responsable de traçabilité, je veux qu'un transfert de package indique sans ambiguïté le package, la box source, la box destination, le motif, l'utilisateur et le poste afin de pouvoir reconstruire une correction industrielle.

## RED

L'ancien test recherchait seulement le barcode dans n'importe quel `DetailsJson` et retrouvait l'ancien événement `PackageScanned`. Il a été remplacé par une vérification stricte d'un unique `ActionType == "PackageTransferred"`.

Commande ciblée :

```text
dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj -c Release --filter FullyQualifiedName~TransferPackage_SuccessfulTransfer_UpdatesQuantitiesAndCreatesAuditLog
```

Résultat avant correction : 0/1 réussi, `Sequence contains no elements`.

## GREEN

`BoxService.TransferPackageAsync` ajoute maintenant un `BoxAuditLog` avant le `SaveChanges` qui persiste le déplacement et les compteurs. L'événement contient `PackageTransferred`, barcode, IDs et numéros des deux box, motif, utilisateur, poste et horodatage UTC.

- Test ciblé : 1/1 réussi.
- Suite complète : 144/144 réussis.
- Build Release : 0 warning, 0 erreur.

| Garantie | Test | Type | Résultat |
|---|---|---|---|
| Exactement un événement métier de transfert existe | test strict de transfert | intégration InMemory | PASS |
| Source et destination sont identifiables | `PreviousValue`, `NewValue`, `DetailsJson` | intégration InMemory | PASS |
| Package, motif, acteur et poste sont conservés | assertions dédiées | intégration InMemory | PASS |

## Couverture et limites

Couverture globale après correction : 20,29 % lignes, 57,96 % branches, sous l'objectif de 80 %. L'atomicité doit encore être démontrée sur SQL Server réel dans `QA-001` ; le provider InMemory confirme le contenu fonctionnel mais pas le rollback moteur.

Aucun commit checkpoint n'a été créé afin de ne pas mélanger les nombreux changements utilisateur préexistants dans `BoxService.cs` et le fichier de tests.
