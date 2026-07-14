# 06 — Préparation infrastructure et déploiement

**Verdict : 62/100 — déploiement pilote seulement après correction des P1 et exercice sur la cible.** La publication .NET Release fonctionne et le manifeste de production impose TLS, hôte autorisé, compte SQL applicatif et désactive migration/seed automatiques. Le chemin conteneur local, l'impression réelle et le rollback restent insuffisamment prouvés.

## Résultats exécutés le 13 juillet 2026

| Commande | Résultat exact |
| --- | --- |
| `dotnet build Motherson_Box_Management.sln -c Release --no-restore` | **PASS**, 0 warning, 0 erreur, 11,23 s. |
| `dotnet publish ... -c Release --no-build -o %TEMP%\\motherson-publish-audit` | **PASS**, DLL publiée. |
| `docker compose config --quiet` | **FAIL attendu de configuration**, variable `MOTHERSON_ALLOWED_HOSTS` absente. La commande combinée n'a donc pas atteint la seconde validation. |
| Construction/démarrage Docker, migration SQL, TLS, healthcheck et impression | **Non exécutés / non vérifiés** dans cet environnement. |

## Constats

| ID | Prio. | Nature | Preuve et risque |
| --- | --- | --- | --- |
| OPS-001 | P1 | Défaut manifeste | Le `Dockerfile` vérifie toujours `https://localhost:8443/health/ready`, tandis que le compose local configure `ASPNETCORE_ENVIRONMENT=Development` et publie `8080:8080` sans certificat/écoute HTTPS. Le conteneur local peut servir HTTP mais être déclaré unhealthy. Créer des healthchecks distincts ou aligner protocole/port. |
| OPS-002 | P1 | Architecture incompatible | Sous Linux, `Program.cs` enregistre `FakePrintSpoolerService`; l'image finale est Linux. Le déploiement conteneur ne peut donc pas prouver l'impression RAW Windows réelle. Choisir hôte Windows, service d'impression dédié ou mécanisme réseau supporté, puis tester le spooler cible. |
| OPS-003 | P1 | Procédure manquante | `Database__AutoMigrate=false` en production est prudent, mais aucun job de migration versionné avec pré-check, sauvegarde, validation et retour arrière n'a été exécuté dans cet audit. Bloquer le déploiement tant qu'une répétition sur copie représentative n'est pas réussie. |
| OPS-004 | P1 | Reprise non prouvée | Des scripts SQL backup/restore existent sous `ops/sql`, mais aucun résultat récent de backup avec checksum, `RESTORE VERIFYONLY`, restauration chronométrée ou RPO/RTO accepté n'a été fourni. |
| OPS-005 | P2 | Supply chain | Images épinglées à des versions (`sdk:8.0.422`, `aspnet:8.0.28`, SQL CU15) mais pas à des digests; dépendances NuGet sans gate de vulnérabilités/SBOM visible dans CI. Ajouter scan, SBOM et politique de mise à jour. |
| OPS-006 | P2 | CI incomplète | CI Windows/Linux exécute restore/build/test et Docker build Linux, mais ne publie pas d'artefact signé, ne valide pas le compose production, les migrations, le démarrage/healthcheck, ni l'impression. |
| OPS-007 | P2 | Observabilité limitée | Logs console UTC et rotation `local` (compose prod) sont des points positifs; aucune collecte centrale, métrique, corrélation métier, seuil/alerte ou test de saturation disque n'est démontré. |
| OPS-008 | P2 | Topologie cible non vérifiée | Certificat, DNS, pare-feu, NTP, proxy, compte de service, droits SQL, stockage des secrets, capacité disque/mémoire et disponibilité SQL ne peuvent pas être déduits du dépôt. |

## Forces

- Exécution finale non-root (`USER $APP_UID`), health endpoints live/ready, readiness avec timeout DB.
- Production exige TLS, `AllowedHosts`, identifiants DB et certificat; seed et migration automatiques désactivés.
- Connexion de production documente un compte applicatif plutôt que `sa`; logs Docker rotatifs configurés.
- CI multi-OS et Docker build Linux présents.

## Gate de mise en production

- [ ] Décider et éprouver l'architecture d'impression réelle; aucun `FakePrintSpoolerService` en production métier.
- [ ] Corriger le healthcheck local et démontrer `docker compose up` healthy.
- [ ] Construire l'image, relever son digest/SBOM et démarrer exactement l'artefact promu.
- [ ] Appliquer migrations sur copie, réaliser backup/restore chronométré, documenter rollback applicatif + DB.
- [ ] Vérifier TLS/chaîne/renouvellement, DNS, pare-feu, secrets et moindre privilège SQL.
- [ ] Smoke test: readiness, login par rôle, scan/doublon, audit, impression et redémarrage worker.
- [ ] Centraliser logs/métriques et provoquer une alerte test; surveiller file d'impression, DB et espace disque.

**Séparation des preuves :** OPS-001/002/003 sont déductibles du code/manifeste. La capacité, l'impression physique, la sécurité réseau et la reprise sont **non vérifiées sur l'environnement cible**; elles ne constituent pas un échec observé, mais empêchent un GO confiant.
