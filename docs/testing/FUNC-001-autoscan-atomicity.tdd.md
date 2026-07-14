# Preuves TDD — FUNC-001

## Parcours utilisateur

En tant qu'opérateur, je veux que l'auto-création d'une box et son premier scan forment une seule opération afin qu'un échec de scan ne laisse ni box vide ni travail d'impression orphelin.

## RED

Test ajouté : `AutoScanPackageTests.AutoScanPackage_FirstScanFails_DoesNotPersistBoxOrPrintJob`.

Commande :

```text
dotnet test MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj -c Release --filter FullyQualifiedName~AutoScanPackage_FirstScanFails_DoesNotPersistBoxOrPrintJob
```

Résultat avant correction : 0/1 réussi. `Assert.False()` échoue car une box est encore persistée après l'échec simulé du premier scan.

## GREEN

La requête ouvre désormais une transaction relationnelle avant la création ; création, travail d'impression et scan partagent cette transaction. `PackageScanService` réutilise la transaction ambiante. Pour EF InMemory, une compensation explicite supprime l'agrégat créé.

Résultats :

- test ciblé : 1/1 réussi ;
- tests auto-scan : 7/7 réussis ;
- suite complète : 144/144 réussis ;
- build Release : 0 warning, 0 erreur.

| Garantie | Test | Type | Résultat |
|---|---|---|---|
| Un premier scan refusé ne conserve aucune box | `AutoScanPackage_FirstScanFails_DoesNotPersistBoxOrPrintJob` | intégration InMemory | PASS |
| Un travail d'impression créé avant cet échec est supprimé | même test avec configuration imprimante active | intégration InMemory | PASS |
| Le parcours nominal continue à créer et associer | `AutoScanPackage_MatchingPrefix_CreatesBoxAndAssociates` | intégration InMemory | PASS |

## Couverture et limites

Commande : `dotnet test -c Release --collect:"XPlat Code Coverage"`.

Couverture globale : 20,11 % lignes, 57,96 % branches ; inférieure à l'objectif de 80 %. Une preuve transactionnelle sur SQL Server réel reste nécessaire dans `QA-001`, car EF InMemory n'implémente pas les transactions relationnelles.

Aucun commit checkpoint n'a été créé : le workspace contenait déjà de très nombreux changements utilisateur dans les mêmes fichiers. Les inclure dans un commit automatique aurait mélangé des travaux sans lien.
