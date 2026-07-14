# Politique métier et conservation

## Retrait et réassociation d’un package

- Un package retiré reste dans `BoxPackages` avec `IsRemoved = true`; cet enregistrement constitue l’historique et ne doit pas être supprimé.
- Un package retiré ne contribue plus à `CurrentQuantity` et peut être associé ultérieurement à une autre box.
- Une seule association active (`IsRemoved = false`) est autorisée par code package. La base impose cet invariant par index unique filtré.
- Un package bloqué reste associé mais est placé en quarantaine: il ne contribue pas à la quantité valide jusqu’à son déblocage.
- Retrait, transfert, blocage, déblocage et réassociation doivent conserver l’acteur, l’horodatage UTC, le poste et la raison dans l’audit.

## Idempotence des scans

Le terminal transmet un `requestId` UUID par scan. Un rejeu réseau avec le même identifiant et la même box retourne le premier résultat sans créer une seconde association. Un nouvel acte opérateur produit un nouvel identifiant.

## Conservation minimale proposée

- Audit métier et historique des associations: durée définie par l’exigence qualité/traçabilité du site; aucune purge automatique n’est activée par défaut.
- Logs applicatifs: rotation locale Docker activée; export vers le collecteur du site à configurer selon la politique SSI.
- Sauvegardes SQL: conservation et chiffrement selon la politique IT du site; chaque changement doit faire l’objet d’un test de restauration.

Les durées réglementaires finales et les habilitations de purge doivent être approuvées par Qualité, Production et IT avant mise en service.
