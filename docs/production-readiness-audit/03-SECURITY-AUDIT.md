# 03 — Audit sécurité

## Portée et conclusion

Audit statique en lecture seule de l’état de travail au 13 juillet 2026, complété par 26 tests ciblés (`SecurityAuditTests`, `AccountControllerTests`, `ConcurrencyTests`, `SqlConstraintTests`) exécutés en Release : **26 réussis, 0 échec**. Aucun secret réel n’est reproduit ici. Semgrep, CodeQL, Gitleaks, ZAP et test d’intrusion réseau : **Non vérifié**.

Conclusion sécurité : **non validée pour production** tant que SEC-001 à SEC-003 ne sont pas traités. Les protections structurantes sont présentes : authentification cookie, hachage ASP.NET Identity, invalidation par `SecurityStamp`, comptes inactifs refusés, anti-CSRF, rôles serveur, CSP, HSTS et limitation de débit.

## Contrôles confirmés

- Cookie `HttpOnly`, `SameSite=Lax`, `Secure=Always` hors développement, durée 60 minutes et validation en base à chaque principal : `Configuration/AuthenticationConfiguration.cs:13-45`.
- Hachage via `PasswordHasher<User>` : `Program.cs:64`, `Services/AuthenticationService.cs:20-29`, `Services/UserService.cs:65,103`.
- Anti-CSRF sur les POST inspectés et retour de connexion limité aux URL locales : `Controllers/AccountController.cs:29-32,80-81`.
- Autorisations serveur : `UsersController` réservé administrateur (`:10`), `AuditController` superviseur/admin (`:9`), opérations d’exception superviseur/admin (`BoxOperationsController.cs:10-12`).
- En-têtes : CSP, anti-framing, nosniff et Permissions-Policy : `Configuration/SecurityHeadersConfiguration.cs:7-16`.
- Aucun CORS n’est activé ; pour l’application MVC same-origin observée, ce n’est pas une lacune.

## [SEC-001] Identifiants de démonstration prévisibles conservés dans le dépôt

**Sévérité :** P1  
**Domaine :** secrets / authentification  
**Statut :** confirmé  
**Emplacement :** `Data/DbInitializer.cs:12-18,24-29`; `appsettings.Development.json:3-5`; `_set_secret.cmd:1`; `_start_sql.cmd:1`  
**Preuve :** l’initialiseur crée trois comptes actifs, dont un administrateur, avec le même mot de passe constant. Plusieurs fichiers de développement contiennent aussi un mot de passe SQL réutilisable et prévisible. La valeur n’est volontairement pas reproduite dans ce rapport. La configuration Production désactive le seed dans `docker-compose.prod.yml:10-11`, mais une erreur d’environnement ou l’usage du compose de développement sur le réseau suffit à exposer ces comptes.  
**Scénario de reproduction :** démarrer une base vide avec l’initialisation de démonstration activée, puis essayer les identifiants documentés/déductibles du dépôt sur les trois matricules seedés.  
**Comportement observé :** comptes actifs et secret partagé déterministe.  
**Comportement attendu :** aucun secret utilisable ni compte privilégié à mot de passe connu ne doit être créé depuis le code.  
**Impact industriel :** prise de contrôle administrateur si le mauvais profil est déployé ou si une base de recette est raccordée au réseau usine.  
**Recommandation :** supprimer tout secret utilisable des fichiers suivis ; rendre le seed de comptes explicite, Development-only et alimenté par secret externe aléatoire, ou le remplacer par une commande d’amorçage à usage unique imposant un changement de mot de passe. Faire tourner toute valeur déjà utilisée.  
**Critère d’acceptation :** recherche de secrets sans résultat exploitable ; démarrage Production impossible avec seed de démonstration ; test prouvant qu’aucun compte privilégié par défaut n’est créé.  
**Effort relatif :** faible  
**Bloquant pour le déploiement :** oui

## [SEC-002] Les journaux d’audit génériques capturent les hachages de mots de passe

**Sévérité :** P1  
**Domaine :** données sensibles / journalisation  
**Statut :** confirmé  
**Emplacement :** `Data/Interceptors/AuditSaveChangesInterceptor.cs:121-135,284-329`; `Entities/User.cs:8`; accès audit `Controllers/AuditController.cs:9-40`  
**Preuve :** toute entité ajoutée/modifiée non `BoxAuditLog` reçoit un audit générique. `ExtractChangedProperties` sérialise toutes les propriétés courantes/originales sans liste d’exclusion ; pour `User`, cela inclut `PasswordHash`. Un superviseur ou administrateur ayant accès à l’audit peut donc récupérer des hachages.  
**Scénario de reproduction :** créer un utilisateur ou réinitialiser son mot de passe, puis inspecter `BoxAuditLogs.DetailsJson` pour l’événement `Insert`/`Update`.  
**Comportement observé :** le hachage est sérialisé dans le journal d’audit.  
**Comportement attendu :** les secrets, mots de passe et hachages sont toujours expurgés ; seul le fait qu’un mot de passe a changé est journalisé.  
**Impact industriel :** fuite de matériel d’authentification, attaques hors ligne et augmentation de l’impact d’une compromission du rôle superviseur ou de la base.  
**Recommandation :** appliquer une liste d’autorisation de champs auditables ou exclure explicitement `PasswordHash` et tout futur champ secret avant sérialisation ; purger/faire tourner les mots de passe concernés selon l’exposition réelle.  
**Critère d’acceptation :** test créant et réinitialisant un utilisateur, puis assertion qu’aucun `DetailsJson`, log applicatif ou réponse ne contient le hachage.  
**Effort relatif :** faible  
**Bloquant pour le déploiement :** oui

## [SEC-003] Le verrouillage de connexion n’est pas atomique sous concurrence

**Sévérité :** P1  
**Domaine :** brute force / concurrence  
**Statut :** confirmé par analyse ; charge concurrente SQL réelle non vérifiée  
**Emplacement :** `Services/LoginLockoutService.cs:35-81`; index unique `Data/ApplicationDbContext.cs:199-202`  
**Preuve :** `RecordFailedAttemptAsync` effectue un read-modify-write sans transaction sérialisable, jeton de concurrence ni `UPDATE ... SET FailedAttempts = FailedAttempts + 1`. Deux échecs simultanés peuvent perdre une incrémentation ; deux premières tentatives simultanées peuvent aussi tenter deux INSERT sur l’index unique, dont une remonte en erreur non gérée. Les appels ne reçoivent pas de `CancellationToken`.  
**Scénario de reproduction :** sur SQL Server, envoyer plusieurs POST `/Account/Login` simultanés pour un matricule absent de `LoginAttempts`, puis comparer le nombre d’échecs au compteur et observer les réponses 500 éventuelles.  
**Comportement observé :** aucune garantie atomique dans le code.  
**Comportement attendu :** chaque échec accepté incrémente exactement une fois et ne provoque jamais de 500 ; le seuil est fiable multi-instance.  
**Impact industriel :** contournement partiel du verrouillage ou déni de service ciblé de la page de connexion.  
**Recommandation :** employer un UPSERT SQL atomique/transaction appropriée avec retry des conflits, ou un mécanisme Identity éprouvé ; normaliser le matricule et propager l’annulation.  
**Critère d’acceptation :** test SQL concurrent (au moins 20 requêtes synchronisées) démontrant compteur exact, verrouillage au seuil et aucune réponse 500, y compris sur deux instances applicatives.  
**Effort relatif :** moyen  
**Bloquant pour le déploiement :** oui

## [SEC-004] Limiteur global partagé pouvant bloquer toute l’usine

**Sévérité :** P2  
**Domaine :** disponibilité  
**Statut :** confirmé  
**Emplacement :** `Configuration/RateLimitingConfiguration.cs:38-43`; application globale `Program.cs:95-98`  
**Preuve :** toutes les routes MVC partagent une unique partition nommée `global`, limitée à 200 requêtes/minute pour l’ensemble des opérateurs.  
**Scénario de reproduction :** plusieurs postes dépassent ensemble 200 requêtes/minute ; les utilisateurs non fautifs reçoivent 429.  
**Comportement observé :** quota commun à toute l’usine et à toutes les routes.  
**Comportement attendu :** limites dimensionnées et partitionnées par utilisateur/IP/route avec capacité globale de protection supérieure au trafic nominal.  
**Impact industriel :** indisponibilité collective lors d’un pic légitime ou d’un seul poste bavard.  
**Recommandation :** partitionner, mesurer le trafic réel, réserver les endpoints de santé et superviser les 429.  
**Critère d’acceptation :** test de charge nominal + pic prouvant qu’un poste ne bloque pas les autres et que les scans restent dans le SLO.  
**Effort relatif :** faible  
**Bloquant pour le déploiement :** non, sous réserve d’un dimensionnement préproduction

## Éléments non vérifiés

- TLS réel, certificat et chaîne de confiance sur le réseau usine.
- Moindre privilège du compte SQL et ACL des journaux/sauvegardes.
- Scan SCA de dépendances et test dynamique authentifié.
- Rotation effective des secrets et absence de secrets dans l’historique Git.
- Résistance multi-instance et politique organisationnelle de mots de passe (le code impose seulement 8 caractères : `Models/UserViewModels.cs:32-35,63-66`).
