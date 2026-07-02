# AGENTS.md — Instructions pour l'Agent IA Codex

Ce fichier définit les directives et les règles de développement strictes que l'agent Codex doit suivre lors de ses interventions sur le projet **Motherson Box Management**.

## 1. Conventions de code C# et ASP.NET Core MVC
* **Style de nommage :**
  * `PascalCase` pour les classes, méthodes, propriétés, enums, interfaces, structs.
  * `camelCase` pour les variables locales et paramètres.
  * `_camelCase` pour les champs privés en lecture seule (private readonly).
* **Nullables :** Respecter l'activation des *Nullable Reference Types*. Aucun warning de nullable ne doit être laissé sans correction.
* **Séparation des responsabilités :**
  * Garder les contrôleurs MVC extrêmement minces (uniquement routage, liaison de ViewModels et validation d'état).
  * Placer toute la logique métier, les calculs, les vérifications d'état et l'orchestration des données dans des services métier séparés et testables.
  * Utiliser exclusivement des ViewModels dédiés pour le transfert de données vers et depuis les vues Razor. **Ne jamais exposer les entités EF Core directement aux formulaires ou contrats externes.**
* **Asynchronisme :** Utiliser systématiquement `async` / `await` pour les opérations d'E/S (accès base de données, fichiers). Propager les objets `CancellationToken`. Interdiction stricte de bloquer le thread avec `.Result` ou `.Wait()`.
* **Injection de Dépendances :** Utiliser l'injection de dépendances native par constructeur. Ne pas utiliser de Service Locator.

## 2. Intégrité des données et EF Core
* **Modifications de schéma :** Toute modification des entités ou des configurations d'entités doit faire l'objet d'une migration EF Core nommée explicitement.
* **Migrations immuables :** Ne jamais modifier manuellement ou supprimer une migration qui a déjà été validée et partagée.
* **Mise à jour documentaire :** Après chaque migration de base de données, mettre immédiatement à jour la section "État des migrations" dans `AGENT.md`.
* **Garantie d'unicité SQL :** Maintenir la contrainte d'unicité globale de `BoxPackages.PackageBarcode` au niveau SQL Server (index unique configuré via EF Core Fluent API). Le contrôle applicatif seul ne suffit pas.
* **Transactions SQL :** Envelopper les scans de packages, transferts et opérations complexes impliquant plusieurs tables ou lignes dans des transactions SQL explicites pour garantir l'atomicité.
* **Concurrence optimiste :** Utiliser `RowVersion` (ou un jeton de concurrence) sur les boxes pour empêcher les écrasements simultanés par deux opérateurs.
* **Pas de suppression physique :** Ne jamais supprimer physiquement de boxes ou d'enregistrements d'audit depuis l'interface utilisateur. Tout retrait ou désaffectation doit conserver une trace historique claire (audit append-only).

## 3. Sécurité et autorisations
* **Secrets et clés :** Interdiction absolue de commiter, d'afficher ou de consigner des secrets, mots de passe en clair ou chaînes de connexion réelles dans le code source ou dans les logs. Utiliser uniquement des placeholders standard (ex. `ConnectionStrings__DefaultConnection`).
* **Mots de passe :** Stocker les mots de passe uniquement sous forme de hash cryptographique sécurisé (ex. en utilisant `IPasswordHasher` d'ASP.NET Core).
* **Contrôles d'accès :** Appliquer des filtres d'autorisation par rôle (`[Authorize(Roles = "...")]`) sur toutes les actions MVC sensibles.
* **Log sécurisé :** Ne pas loguer d'informations sensibles (mots de passe, données personnelles confidentielles) et ne jamais inclure de détails internes de base de données dans les messages d'erreur affichés aux utilisateurs.

## 4. Cycle de travail obligatoire (GSD Core)
Avant toute modification :
1. Consulter les fichiers de planification GSD Core dans `.planning/` (`PROJECT.md`, `ROADMAP.md`) pour valider la phase active.
2. Lire `AGENT.md` pour maîtriser les règles métier et techniques associées au périmètre.
3. Repérer ou créer la tâche dans `TODO.md` et passer son statut à `En cours`.
4. Examiner le code et les migrations existantes.

Pendant le développement :
1. Effectuer des modifications petites, ciblées et incrémentales.
2. Implémenter la logique métier dans des services isolés et écrire les tests correspondants.
3. Mettre à jour `TODO.md` à chaque étape majeure.
4. Mettre à jour `AGENT.md` dès qu'une modification touche le modèle de données, le statut des migrations, les règles métier, les routes ou les dépendances NuGet.

Avant de considérer le travail terminé :
1. Exécuter les commandes de validation obligatoires :
   ```bash
   dotnet restore
   ```
   ```bash
   dotnet build
   ```
   ```bash
   dotnet test
   ```
2. S'assurer que le build et tous les tests passent sans warning majeur ni erreur.
3. Vérifier que la documentation (`AGENT.md` et `TODO.md`) est à jour.
4. Passer le statut de la tâche à `Terminé` dans `TODO.md`.

## 5. Git et commits
* Ne jamais commiter si le build ou les tests échouent.
* Ne jamais utiliser `git add .` sans inspecter rigoureusement les fichiers inclus.
* Rédiger les commits en respectant les **Conventional Commits** (ex. `feat(...)`, `fix(...)`, `db(migration)...`, `docs(agent)...`).
* Si les droits locaux ou le workflow GSD Core empêchent le commit automatique, fournir la commande Git exacte à exécuter.
