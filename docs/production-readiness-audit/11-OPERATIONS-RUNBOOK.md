# Runbook d'exploitation

## TLS et démarrage

Le profil Production refuse de démarrer sans connexion SQL, `AllowedHosts`, certificat PFX et mot de passe. Monter le PFX en lecture seule, publier uniquement le port HTTPS 443 et tester `/health/live` puis `/health/ready`.

## Migrations

1. Placer l'application en maintenance.
2. Sauvegarder et exécuter `RESTORE VERIFYONLY`.
3. Générer le script idempotent EF, le relire puis l'appliquer avec un compte DDL temporaire.
4. Démarrer la nouvelle version et exécuter les smoke tests.
5. En cas d'échec, restaurer le backup et redéployer l'artefact précédent. Ne pas tenter le downgrade de l'index package si des réassociations historiques existent.

## Sauvegarde

- RPO cible : 15 minutes ; full quotidien, journal toutes les 15 minutes si mode FULL.
- Rétention : 30 jours en stockage chiffré séparé, accès limité DBA.
- `ops/sql/backup.sql` réalise backup avec checksum et `RESTORE VERIFYONLY`.
- Superviser l'âge du dernier backup, le résultat SQL Agent et l'espace disque.

## Restauration

- RTO cible : 2 heures.
- Restaurer chaque trimestre sur une base isolée avec `ops/sql/restore.sql`.
- Vérifier migrations, nombre de box/packages/audits, login de test et scan contrôlé.
- Consigner durée, backup utilisé, opérateur et résultat.

## Supervision

- Liveness : `/health/live`.
- Readiness : `/health/ready`, timeout DB 3 secondes.
- Alerter après deux échecs consécutifs, disque > 80 %, absence de backup > 30 minutes et erreurs de scan/SQL.
