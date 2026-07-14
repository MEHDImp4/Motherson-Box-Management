# 04 — Audit données, EF Core, atomicité et concurrence

## Conclusion

Les anciennes lacunes sur les contraintes CHECK, la réassociation historique, le principal SYSTEM, les codes SQL 2601/2627, les tailles de chaînes et l’idempotence de scan sont corrigées dans l’état actuel. Les 26 tests sécurité/concurrence/contraintes ciblés réussissent en Release. La garantie la plus critique — double scan du même package — repose désormais sur une transaction, `RowVersion`, retries et index uniques filtrés.

La préparation données reste **conditionnelle** : cohérence du compteur dénormalisé, concurrence des opérations superviseur et preuve SQL réelle exhaustive doivent être fermées avant GO.

## Protections confirmées

- Unicité box/barcode : `ApplicationDbContext.cs:63-65`.
- Unicité d’un package actif et réassociation après soft-delete : `ApplicationDbContext.cs:97-109`.
- Idempotence par `ScanRequestId` unique filtré : `ApplicationDbContext.cs:103-106`, `PackageScanService.cs:59-71`.
- Contraintes SQL quantité/dimensions : `ApplicationDbContext.cs:50-61` et migration `20260712201848_AddCriticalBoxCheckConstraints.cs`.
- Concurrence optimiste de Box : `ApplicationDbContext.cs:65`, `Entities/Box.cs:26`.
- Scan atomique : transaction, contrôle du statut/capacité, insertion + incrément + complétion dans le même `SaveChanges`, retry de concurrence et traitement 2601/2627 : `PackageScanService.cs:43-50,106-186,195-218,232-252`.
- FKs principales en `DeleteBehavior.Restrict` : `ApplicationDbContext.cs:76-124`.
- Bornes de chaînes et décimaux explicites : `ApplicationDbContext.cs:31-34,66-74,99-103`.

## [DATA-001] Le compteur de box n’est pas garanti égal aux packages actifs

**Sévérité :** P1  
**Domaine :** intégrité / traçabilité  
**Statut :** confirmé  
**Emplacement :** `Entities/Box.cs:12-14`; `ApplicationDbContext.cs:55-57`; mises à jour applicatives `PackageScanService.cs:168-179`, `BoxService.cs:485-644`  
**Preuve :** la base garantit seulement `0 <= CurrentQuantity <= ExpectedQuantity`. Elle ne garantit pas `CurrentQuantity = COUNT(BoxPackages WHERE BoxId=... AND IsRemoved=0)`. Le compteur est maintenu par plusieurs chemins applicatifs. Une opération SQL manuelle, un ancien binaire, un import ou un défaut futur peut produire une fausse complétion tout en respectant les CHECK.  
**Scénario de reproduction :** dans une base de test, modifier `CurrentQuantity` sans modifier les packages actifs, puis lire la box ; la contrainte SQL accepte la ligne si elle reste dans la plage.  
**Comportement observé :** deux sources de vérité peuvent diverger.  
**Comportement attendu :** une source de vérité unique ou une détection/réconciliation obligatoire et supervisée.  
**Impact industriel :** box déclarée complète avec contenu incorrect, fausse traçabilité et expédition erronée.  
**Recommandation :** calculer le compteur depuis les packages lorsque possible, ou encapsuler toute mutation dans une procédure/transaction et ajouter un contrôle de cohérence périodique bloquant avec alerte. Interdire les droits SQL directs d’écriture aux opérateurs/app hors procédures prévues.  
**Critère d’acceptation :** test d’intégrité détectant toute divergence ; job de contrôle et alerte démontrés ; aucune voie applicative ne modifie l’un sans l’autre.  
**Effort relatif :** moyen  
**Bloquant pour le déploiement :** oui

## [DATA-002] Message de conflit superviseur insuffisamment explicite après épuisement des reprises

**Sévérité :** P2
**Domaine :** concurrence / API  
**Statut :** confirmé  
**Emplacement :** `Services/BoxService.cs:230-644`; gestion contrôleur générique `Controllers/BoxOperationsController.cs:37-58`  
**Preuve :** `ExecuteWithConcurrencyRetryAsync` englobe les opérations dans une transaction, intercepte `DbUpdateConcurrencyException`, annule, vide le tracker et recommence jusqu’à trois fois (`BoxService.cs:165-205`). Les opérations superviseur l’utilisent. Après épuisement, le contrôleur conserve toutefois un message générique après redirection, sans réponse de conflit dédiée ni version client.  
**Scénario de reproduction :** charger la même box dans deux contextes SQL, faire terminer/modifier/transférer simultanément, puis soumettre les deux commandes.  
**Comportement observé :** les conflits transitoires sont repris jusqu’à trois fois ; un conflit persistant peut finir comme erreur générique et la reprise opérateur n’est pas contractualisée.  
**Comportement attendu :** conflit 409 ou message métier précis, état rechargé, action sûre à répéter, audit du rejet.  
**Impact industriel :** opérateur incertain pouvant répéter une commande, perte de temps et risque de décision incohérente autour du dernier package/transfert.  
**Recommandation :** définir l’idempotence de chaque commande, accepter un token/version, distinguer `DbUpdateConcurrencyException`, recharger l’état et journaliser le conflit ; ne retry automatiquement que les opérations prouvées sûres.  
**Critère d’acceptation :** tests SQL synchronisés pour deux clôtures, dernier scan vs modification, double clic, timeout/rejeu et transfert concurrent ; état final unique, audit complet et aucune 500.  
**Effort relatif :** moyen  
**Bloquant pour le déploiement :** non isolément

## [DATA-003] Validation SQL Server réelle incomplète dans cette exécution

**Sévérité :** P2  
**Domaine :** migrations / preuve  
**Statut :** non vérifié  
**Emplacement :** `Migrations/20260712201848_AddCriticalBoxCheckConstraints.cs`; `Migrations/20260712205044_BoundStringsAndScanIdempotency.cs`; `MothersonBoxManagement.Tests/SqlServerIntegrationTests.cs`  
**Preuve :** les tests ciblés passent, mais certains tests SQL Server d’intégration dépendent d’une instance/variable externe et la commande exécutée n’établit pas que toutes les migrations ont été appliquées à une copie représentative de Production.  
**Scénario de reproduction :** restaurer une sauvegarde anonymisée, appliquer les migrations, exécuter la suite SQL et les scénarios concurrents avec barrières.  
**Comportement observé :** cohérence du modèle/migrations prouvée statiquement ; drift et comportement sur données réelles non prouvés.  
**Comportement attendu :** migration répétable, contrôles présents dans `sys.*`, absence de drift et rollback/restauration testés.  
**Impact industriel :** le code peut être correct alors que la base déployée manque d’un index/contrainte critique.  
**Recommandation :** ajouter un gate préproduction qui vérifie migrations, contraintes, index filtrés et modèle sur SQL Server identique à Production.  
**Critère d’acceptation :** rapport SQL signé listant migration courante, contraintes/index attendus, tests concurrents réussis et restauration validée.  
**Effort relatif :** moyen  
**Bloquant pour le déploiement :** oui tant que non vérifié

## [API-001] Annulation non propagée dans le service de verrouillage

**Sévérité :** P2  
**Domaine :** backend / résilience  
**Statut :** confirmé  
**Emplacement :** `Services/LoginLockoutService.cs:7-13,35-121`; appel `AccountController.cs:38-58`  
**Preuve :** toutes les méthodes du verrouillage omettent `CancellationToken` et les requêtes/SaveChanges associés ne reçoivent pas le token HTTP.  
**Scénario de reproduction :** interrompre une connexion pendant une latence SQL artificielle ; le travail DB continue après abandon de la requête.  
**Comportement observé :** annulation non propagée.  
**Comportement attendu :** toute I/O liée à la requête accepte et propage le token.  
**Impact industriel :** accumulation de travail inutile et épuisement de connexions lors d’une panne réseau/DB.  
**Recommandation :** ajouter le token à l’interface, aux appels EF et au contrôleur.  
**Critère d’acceptation :** test avec commande SQL retardée et requête annulée prouvant l’annulation rapide et l’absence de mutation partielle.  
**Effort relatif :** faible  
**Bloquant pour le déploiement :** non

## Atomicité — verdict par scénario prioritaire

| Scénario | Verdict actuel | Preuve / réserve |
|---|---|---|
| Deux scans du même package | Protégé | index unique actif + 2601/2627 + transaction (`PackageScanService.cs:43-218`) |
| Rejeu du même scan après timeout | Protégé si `requestId` est réutilisé | index unique + restauration du résultat (`:59-71`); génération/stockage client à valider en E2E |
| Deux derniers packages différents | Protégé par `RowVersion` avec retry | contrainte de plage + retry ; test SQL réel de charge requis |
| Deux clôtures superviseur | Rejet technique probable, UX/idempotence non définies | DATA-002 |
| Dernier scan vs modification quantité | Conflit EF attendu, résultat métier non contractualisé | DATA-002 |
| Transfert simultané | Transaction applicative présente, preuve concurrente SQL requise | `BoxService.cs:485-579` |

## Non vérifié

- Schéma et migration réellement présents sur la future base Production ; drift.
- Isolation configurée côté SQL Server, deadlocks sous charge, volumétrie et plans d’exécution.
- RPO/RTO validés métier, chiffrement des sauvegardes et test de restauration.
- Précision/synchronisation UTC entre hôtes et politique de rétention/tamper-evidence des audits.
