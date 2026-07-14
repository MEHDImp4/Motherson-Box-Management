# Preuves TDD — DATA-005

## Parcours

En tant qu'opérateur, je veux qu'un double scan concurrent soit reconnu comme doublon indépendamment de la langue ou du format du message SQL Server, sans masquer les autres erreurs de base.

## RED

Le test paramétré `UniqueConstraintClassification_UsesSqlServerErrorNumbers` ne compilait pas : `PackageScanService` ne possédait aucune classification numérique. Le code de production utilisait `InnerException.Message.Contains("IX_BoxPackages_PackageBarcode")`.

## GREEN

- La chaîne d'exceptions est parcourue jusqu'à `Microsoft.Data.SqlClient.SqlException`.
- Seuls les numéros SQL Server 2601 et 2627 sont classés comme violation d'unicité.
- Les numéros 1205 (deadlock) et 0 ne sont pas classés comme doublons.
- Les simulateurs InMemory utilisent une métadonnée entière `SqlServerErrorNumber`; aucun texte ne décide du traitement.

Résultats : classification et concurrence 5/5, suite complète 152/152, build Release 0 warning/0 erreur.

| Numéro | Sens | Résultat |
|---:|---|---|
| 2601 | index unique dupliqué | reconnu |
| 2627 | contrainte unique/PK dupliquée | reconnu |
| 1205 | deadlock | non reconnu |
| 0 | inconnu | non reconnu |

Couverture globale : 18,85 % lignes, 59,31 % branches, sous 80 %. Une exception réelle issue de SQL Server doit encore être observée dans `QA-001` pour valider l'intégration provider de bout en bout.

Aucun commit checkpoint n'a été créé afin de préserver les changements utilisateur préexistants.
