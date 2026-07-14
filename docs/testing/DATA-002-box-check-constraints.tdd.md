# Preuves TDD — DATA-002

## Parcours

En tant que responsable des données, je veux que SQL Server refuse toute box dont les quantités ou dimensions violent les invariants, même si l'écriture contourne l'application.

## RED

Le test `BoxModel_DefinesCriticalCheckConstraints` interroge le modèle EF design-time. Après correction du test pour éviter le modèle runtime optimisé, le RED valide était l'absence de `CK_Boxes_ExpectedQuantity_Positive`.

## GREEN

Contraintes ajoutées :

- `CK_Boxes_ExpectedQuantity_Positive` : `ExpectedQuantity > 0` ;
- `CK_Boxes_CurrentQuantity_Range` : `0 <= CurrentQuantity <= ExpectedQuantity` ;
- `CK_Boxes_Dimensions_Positive` : hauteur, largeur et profondeur strictement positives.

La migration `20260712201848_AddCriticalBoxCheckConstraints` exécute d'abord un préflight. Si des données existantes sont invalides, elle lève l'erreur 51001 avec un message explicite au lieu d'appliquer partiellement le schéma.

Résultats : test ciblé 1/1, suite complète 153/153, build Release 0 warning/0 erreur. Le script SQL généré contient le préflight et les trois `ALTER TABLE ... ADD CONSTRAINT ... CHECK`.

Couverture globale : 17,91 % lignes, 59,31 % branches, sous 80 %. L'application et le rejet effectif d'écritures invalides sur SQL Server réel restent à valider via `QA-001`.

Aucun commit checkpoint n'a été créé afin de préserver les modifications utilisateur préexistantes.
