# 02 — Audit fonctionnel, métier et concurrence

## Verdict fonctionnel

Les règles cœur sont présentes et cohérentes : unicité des cartons et paquets actifs, contrôle de capacité, complétion automatique, reprise par un autre opérateur, opérations d'exception réservées aux superviseurs, audit du transfert, exclusion des paquets bloqués de la quantité valide, retrait logique puis réassociation, et rejeu idempotent avec `requestId`.

La préparation au déploiement reste conditionnée à deux clarifications fonctionnelles et à l'exécution des tests SQL Server optionnels. Aucun défaut critique ou élevé n'a été démontré dans ce volet.

## Matrice des règles métier

| Règle | État | Preuves |
|---|---|---|
| Carton et code-barres carton uniques | Conforme | `ApplicationDbContext.cs:63-65` |
| Un seul rattachement actif par paquet | Conforme | index filtré `ApplicationDbContext.cs:97-109`; gestion 2601/2627 `PackageScanService.cs:204-218,232-233` |
| Réassociation après retrait | Conforme | retrait logique `BoxService.cs:579-612`; index limité à `IsRemoved = 0` |
| Carton ouvert seulement pour scanner | Conforme | `PackageScanService.cs:125-156` |
| Capacité et complétion automatiques | Conforme | `PackageScanService.cs:142-191`; CHECK `ApplicationDbContext.cs:52-60` |
| Package bloqué exclu de la quantité valide | Conforme | décrément/réouverture `BoxService.cs:397-436`; réincrément contrôlé `BoxService.cs:439-482` |
| Annulation / blocage / archivage / fermeture forcée | Conforme au code | gardes `BoxService.cs:230-394`; autorisation `BoxOperationsController.cs:10-11` |
| Transfert atomique et tracé | Conforme au code | compteurs + journal explicite `BoxService.cs:485-576`; suppression du doublon d'intercepteur `AuditSaveChangesInterceptor.cs:190-192` |
| Auto-création + premier scan atomiques | Conforme sur fournisseur relationnel | transaction/rollback `BoxController.cs:195-267`; test de non-persistance `AutoScanPackageTests.cs:198-246` |
| Rejeu d'un scan après réponse perdue | Conforme si `requestId` stable | `PackageScanService.cs:57-71`; index `ApplicationDbContext.cs:103-106` |
| Reprise d'un carton par un autre opérateur | Autorisée implicitement | aucun verrou de possession; attribution de `LastModifiedByUserId` à chaque mutation |

## Analyse des scénarios de concurrence

### Deux opérateurs scannent le même paquet

La prélecture seule ne suffit pas, mais l'index SQL unique filtré constitue l'arbitre final. Les erreurs SQL Server 2601/2627 sont converties en refus métier (`ApplicationDbContext.cs:107-109`; `PackageScanService.cs:204-218,232-233`). Le test réel lance deux contextes concurrents et exige exactement un succès (`SqlServerIntegrationTests.cs:81-135`).

### Deux opérateurs ajoutent les derniers paquets d'un carton

Chaque mutation incrémente une entité `Box` protégée par `rowversion`; un conflit provoque rollback, rechargement et jusqu'à trois tentatives (`ApplicationDbContext.cs:65`; `PackageScanService.cs:43-50,195-203,227-229`). Le résultat attendu est une quantité bornée par la contrainte SQL et un refus lorsque la capacité est atteinte.

### Timeout ou réponse HTTP perdue après commit

Le navigateur peut rejouer avec le même `requestId`. Si le même paquet, le même carton et la même clé sont retrouvés, le service retourne le résultat précédent comme succès (`PackageScanService.cs:57-71`). Sans clé stable fournie par le client, le rejeu reste correctement refusé comme doublon, mais ne peut pas être distingué d'un doublon réel.

### Auto-création concurrente

Chaque requête crée potentiellement son propre carton, mais l'unicité du paquet actif fait échouer l'un des premiers scans. Sur SQL Server, la transaction englobante ramène alors le carton et son éventuel job d'impression (`BoxController.cs:195-226,254-267`). Ce scénario exact n'est pas couvert par un test SQL concurrent dédié.

## Constats résiduels

### FUNC-001 — Sélection ambiguë pour deux modèles actifs de préfixe équivalent — Moyenne

Le modèle choisi est celui dont le préfixe correspondant est le plus long. En cas de deux modèles actifs portant le même préfixe, ou de préfixes distincts de même longueur correspondant au même code, aucun second critère stable n'est appliqué (`BoxTemplateService.cs:166-175`). La base impose l'unicité du nom, pas celle du préfixe (`ApplicationDbContext.cs:185`), et création/mise à jour n'empêchent pas le chevauchement (`BoxTemplateService.cs:62-104`). Un scan peut donc auto-créer un type de carton dépendant de l'ordre de retour SQL.

**Recommandation :** faire valider la règle par le métier puis soit interdire les préfixes actifs ambigus, soit définir une priorité explicite persistée. Ajouter un test de deux modèles de longueur égale.

### FUNC-002 — Validation SQL réelle facultative dans la baseline — Moyenne

Les deux tests qui prouvent les contraintes et le double scan sur SQL Server sont automatiquement ignorés lorsque `MOTHERSON_TEST_SQL_CONNECTION` n'est pas défini (`SqlServerIntegrationTests.cs:11-17`). Les tests InMemory ne reproduisent ni `rowversion`, ni les contraintes, ni exactement les transactions SQL Server. Une exécution de tests peut donc être verte sans avoir validé l'arbitre principal des conflits de production.

**Recommandation :** rendre cette suite obligatoire dans le pipeline de release et archiver son résultat comme preuve Go/No-Go.

### FUNC-003 — Échec d'impression masqué pendant l'auto-création — Faible

La création du job d'impression est entourée d'un `catch` vide et le flux continue (`BoxController.cs:204-215`). Le carton et son paquet peuvent être validés sans qu'un avertissement explicite soit retourné à l'opérateur. Cela peut générer un carton physiquement non étiqueté alors que l'opération logique est réussie.

**Recommandation :** confirmer si l'impression est bloquante ou non pour l'atelier; au minimum retourner un avertissement visible et auditer l'échec avec un identifiant de corrélation.

## Scénarios non vérifiés

- perte réseau pendant un scan et rejeu réel depuis le navigateur;
- expiration de session entre saisie et soumission;
- redémarrage applicatif/SQL pendant une transaction;
- concurrence auto-création sur le même paquet avec SQL Server;
- multi-onglets, saturation du rate limiter et charge de pointe réelle;
- comportement avec scanner, poste et imprimante industriels réels.

Ces éléments nécessitent une recette intégrée; ils ne peuvent pas être conclus à partir du seul code source.
