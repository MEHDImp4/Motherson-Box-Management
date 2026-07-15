# 11_GAPS_CONTRADICTIONS_AND_UNKNOWNS.md -- Écarts, Contradictions et Inconnues

Ce fichier centralise toutes les contradictions entre la documentation et le code, les informations manquantes et les éléments nécessitant une confirmation humaine.

---

## 1. Contradictions entre la documentation et le code

### C-001 : Écart sur le nombre de tests

**Question :** Quel est le nombre réel et actuel de tests ?

| Source | Nombre | Date |
|--------|--------|------|
| `docs/TESTING.md` | 129 | Création du document |
| `README.md` | 129 | 2026-07-05 |
| `AGENT.md` ligne 359 | 129/129 | 2026-07-05 |
| `AGENT.md` ligne 361 | 134/134 | 2026-07-06 |
| `AGENT.md` ligne 363 | 118/118 | 2026-07-08 |
| `AGENT.md` ligne 365 | 118/118 | 2026-07-09 |
| Documents TDD | 144-172 | Diverses dates |
| production-readiness-audit | 162 | 2026-07-13 |

**Confirme :** Le nombre de tests a évolué au fil du temps. Le décompte vérifié le plus récent est de **162 tests passants** issu de l'audit de préparation à la production (2026-07-13). Cependant, des modifications non commitées ont pu ajouter des tests supplémentaires (les fichiers PrintAgent et récupération de mot de passe sont modifiés).

**Impact :** Le README et TESTING.md sont obsolètes. Le rapport doit utiliser le dernier décompte vérifié ou relancer les tests.

---

### C-002 : ARCHITECTURE.md désynchronisé

**Question :** ARCHITECTURE.md décrit-il fidèlement l'architecture actuelle ?

**Fichiers consultés :** `docs/ARCHITECTURE.md`, `docs/production-readiness-audit/01-PROJECT-UNDERSTANDING.md`

**Ce qui est confirmé :**
- ARCHITECTURE.md décrit des routes `/Box/Prepare` et `/Box/ScanAjax` qui n'existent plus
- Le code actuel utilise `/Box/AutoScanPackage` et `/Box/AssociatePackage`
- ARCHITECTURE.md décrit une contrainte d'unicité globale sur PackageBarcode ; l'implémentation réelle utilise un index unique filtré (WHERE IsRemoved = 0)
- ARCHITECTURE.md décrit un agent d'impression Windows dans le schéma de topologie ; l'agent d'impression a été remplacé par une impression exclusivement navigateur

**Impact :** Le document d'architecture ne peut pas être utilisé tel quel pour le rapport. Le code fait foi.

---

### C-003 : Ambiguïté sur le statut de l'agent d'impression

**Question :** L'agent d'impression (Print Agent) fait-il encore partie du système ?

**Fichiers consultés :** `Motherson_Box_Management.sln`, `docs/testing/browser-only-printing.tdd.md`, `docs/testing/local-print-agent.tdd.md`, `.github/workflows/ci.yml`

**Ce qui est confirmé :**
- Les projets PrintAgent et PrintAgent.Core sont toujours dans le fichier solution
- Le `browser-only-printing.tdd.md` supplante le `local-print-agent.tdd.md`
- Le workflow CI contient toujours un job `package-print-agent`
- Les contrôleurs PrintAgentApiController et PrintAgentController existent toujours dans l'application web
- Les entités `PrinterConfiguration`, `BoxPrintJob` et `PrintAgentPairingCode` sont toujours dans le modèle de base de données
- La vue `Box/Settings.cshtml` affiche toujours le statut et la configuration de l'agent d'impression

**Resolution (saisie humaine) :** L'agent d'impression **sera utilise en production**. Il ne s'agit pas de code mort. Le document `browser-only-printing.tdd.md` represente un fallback navigateur, pas un remplacement complet de l'agent. L'architecture reelle est donc : agent d'impression sur les postes de travail + impression navigateur comme fallback.

**Impact :** Le rapport doit presenter l'agent d'impression comme une fonctionnalite productionnelle du systeme, avec l'impression navigateur en mode degrade.

---

### C-004 : Incohérence interne de STATE.md

**Question :** L'état du projet est-il correctement capturé ?

**Fichiers consultés :** `.planning/STATE.md`

**Ce qui est confirmé :**
- `current_phase_name` indique « supervisor-exceptions-audit-trail » (Phase 4)
- `current_phase` est `05` (Phase 5)
- La barre de progression affiche 80 % mais les métadonnées indiquent `percent: 100`
- Le tableau des métriques de performance affiche 0 plans pour les phases 1, 2, 3 et 5

**Impact :** STATE.md a été partiellement auto-généré et n'a pas été entièrement réconcilié. Non critique pour le rapport puisque ROADMAP.md et MILESTONES.md sont cohérents.

---

### C-005 : Placeholder non coché dans ROADMAP.md

**Question :** La Phase 2 est-elle terminée ?

**Fichiers consultés :** `.planning/ROADMAP.md`, `.planning/milestones/v1.0-ROADMAP.md`

**Ce qui est confirmé :**
- La Phase 2 liste `- [ ] 01-PLAN.md` comme non coché
- Les quatre sous-plans (02-01 à 02-04) sont tous cochés
- La Phase 2 est marquée comme terminée dans MILESTONES.md

**Impact :** Incohérence mineure de documentation. La Phase 2 est clairement terminée au vu de toutes les autres preuves.

---

## 2. Informations manquantes

### M-001 : Confirmation du format exact des codes-barres

**Question :** Les préfixes de codes-barres exacts (BOX- et PKG-) sont-ils confirmés avec les équipes de production ?

**Fichiers consultés :** `AGENT.md` ligne 270

**Ce qui est confirmé :** Le code utilise `BOX-` comme préfixe de boîte (configurable via `BarcodeConfigurations`). Les codes-barres des colis sont validés par « ne commençant pas par le préfixe BOX- ». Le préfixe `PKG-` mentionné dans AGENT.md n'est pas appliqué dans le code.

**Impact :** Le rapport doit noter que le format exact des codes-barres est configurable et que le préfixe PKG- est une convention, pas une contrainte.

---

### M-002 : Volume de données en production

**Question :** Combien de boîtes/colis sont attendus en production ?

**Fichiers consultés :** `AGENT.md` ligne 272

**Ce qui est confirmé :** Le code ne dispose d'aucune automatisation d'archivage. AGENT.md mentionne « Définir la fréquence d'archivage des boîtes pour éviter le ralentissement de BoxPackages au fil du temps » comme point ouvert.

**Impact :** Les performances sous des volumes de données de production sont inconnues.

---

### M-003 : Compatibilité matérielle du scanner USB

**Question :** Le scanner USB (cunéiforme clavier) a-t-il été testé sur les terminaux cibles ?

**Fichiers consultés :** `AGENT.md` ligne 271

**Ce qui est confirmé :** Le fichier scanner.js suppose un comportement de cunéiforme clavier (champ de saisie caché, détection de la touche Entrée).

**Resolution (saisie humaine) :** Les scanners USB ont ete testes sur les terminaux de l'usine et fonctionnent correctement. L'impression physique (agent + imprimante) n'a pas encore ete testee sur le materiel cible.

---

### M-004 : Nombre de terminaux en production

**Question :** Combien de terminaux accèderont à l'application simultanément ?

**Fichiers consultés :** Aucune preuve dans le code source.

**Ce qui est confirmé :** L'application supporte plusieurs terminaux via une identité de poste locale au navigateur. Aucun test de charge n'a été effectué.

**Impact :** Le comportement concurrentiel sous charge de production est inconnu.

---

### M-005 : Contexte du stage

**Question :** Quel est le rôle du stagiaire, la durée et les contributions spécifiques ?

**Fichiers consultés :** Aucune preuve dans le code source. L'historique Git montre « Agent » comme responsable des tâches dans TODO.md.

**Ce qui est confirmé :** Toutes les tâches de TODO.md sont attribuées à « Agent ». Les auteurs des commits Git devraient être vérifiés pour distinguer les contributions humaines de celles générées par l'IA.

**Impact :** Le rapport ne doit pas attribuer du code généré par l'IA au stagiaire sans vérification. Les contributions doivent être confirmées par le stagiaire.

---

### M-006 : Détails de l'entreprise et de l'usine

**Question :** Quel est le nom complet de l'entreprise, le lieu de l'usine et la structure du département ?

**Fichiers consultés :** Aucune preuve dans le code source au-delà de « Motherson » et « P3 ».

**Ce qui est confirmé :** Le nom du projet est « Motherson Box Management » et il cible la « zone d'emballage P3 » de « l'usine Motherson ».

**Impact :** L'introduction du rapport nécessitera un contexte d'entreprise fourni par l'humain.

---

## 3. Zones fonctionnelles ambiguës

### A-001 : Emplacement de l'orchestration d'AutoScanPackage

**Question :** L'orchestration de l'auto-scan doit-elle être dans le contrôleur ou dans un service ?

**Fichiers consultés :** `docs/production-readiness-audit/01-PROJECT-UNDERSTANDING.md`, `BoxController.cs`

**Ce qui est confirmé :** L'action `AutoScanPackage` dans `BoxController` contient de la logique d'orchestration (gestion des transactions, annulation) que l'audit de préparation à la production signale comme appartenant à une couche service.

**Impact :** Il s'agit d'un problème architectural connu mais qui n'affecte pas la fonctionnalité.

---

### A-002 : Cohérence de CurrentQuantity

**Question :** CurrentQuantity est-il toujours cohérent avec le nombre réel de colis actifs ?

**Fichiers consultés :** `docs/production-readiness-audit/04-DATA-AND-DATABASE-AUDIT.md`

**Ce qui est confirmé :** CurrentQuantity est un compteur dénormalisé incrémenté/décrémenté par la couche service. L'audit de préparation à la production signale qu'il n'est pas garanti d'être cohérent avec un COUNT des colis actifs (non supprimés, non bloqués).

**Impact :** Problème potentiel d'intégrité des données dans des cas limites.

---

### A-003 : Échec d'impression lors de la création automatique

**Question :** Que se passe-t-il si la mise en file d'attente d'impression échoue lors de la création automatique de boîte par auto-scan ?

**Fichiers consultés :** `docs/production-readiness-audit/02-FUNCTIONAL-AUDIT.md`

**Ce qui est confirmé :** FUNC-003 dans l'audit note que l'échec d'impression lors de la création automatique est silencieusement ignoré. La boîte et le colis sont tout de même créés.

**Impact :** Sévérité faible. L'étiquette peut être réimprimée manuellement.

---

## 4. Éléments nécessitant une confirmation humaine

| # | Question | Pourquoi c'est important | Statut |
|---|----------|--------------------------|--------|
| H-001 | Quel est le rôle spécifique du stagiaire et sa période de contribution ? | Requis pour l'introduction du rapport | **Repondu** : Developpeur unique, 1 mois (juillet 2026), encadrants Aymane et Ayoub |
| H-002 | Quel est le nom complet de l'entreprise et le lieu de l'usine ? | Requis pour le contexte du rapport | **Repondu** : Motherson PKC, 8J22+RJ, Ameur Seflia, Kenitra (Maroc) |
| H-003 | Combien de terminaux utiliseront le système en production ? | Requis pour la discussion architecturale | **Repondu** : 2-3 postes de travail |
| H-004 | L'agent d'impression est-il une fonctionnalité actuelle ou abandonnée ? | Requis pour la description du périmètre | **Repondu** : Sera utilise en production |
| H-005 | Les scanners USB ont-ils été testés sur le matériel cible ? | Requis pour la discussion de validation | **Repondu** : Scanners testes et fonctionnels, impression non encore testee |
| H-006 | Quelle a été la difficulté technique la plus challengeante rencontrée ? | Requis pour la section réflexion du rapport | **Repondu** : Le systeme d'impression |
| H-007 | Quelles améliorations sont prévues pour la v2 ? | Requis pour la conclusion/perspectives | **Repondu** : Integration ERP, export Excel/PDF |
| H-008 | Quels retours les superviseurs/administrateurs ont-ils donnés lors des tests ? | Requis pour la discussion de l'acceptation utilisateur | **Repondu** : Pas encore de retour |
| H-009 | L'application a-t-elle été déployée dans un environnement proche de la production ? | Requis pour la discussion du déploiement | **Repondu** : Pas encore, en local Docker uniquement |
| H-010 | Quel est le volume quotidien attendu de boîtes/colis ? | Requis pour la discussion des performances | **Repondu** : Estimation ~10 cartons/jour (approximatif) |
