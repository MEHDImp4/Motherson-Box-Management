# 05 — Audit UI/UX et accessibilité industrielle

**Verdict : 58/100 — validation terrain requise avant déploiement opérateur.** L'interface présente une base cohérente (navigation latérale, états vides, validations, cibles fréquemment à 44–48 px), mais le flux scanner global et son dialogue dynamique ne satisfont pas encore une démonstration WCAG 2.2 AA ni une recette industrielle.

## Périmètre et méthode

Inspection statique des vues Razor, de `wwwroot/css/site.css`, `wwwroot/js/scanner.js` et des tests MVC. Aucun navigateur connecté, base active, scanner HID, écran tactile, gant, NVDA/JAWS, axe ou Lighthouse n'était disponible. Par conséquent, contraste réel, reflow 320 px/zoom 200–400 %, ordre de tabulation, lecteur d'écran et latence perçue sont **non vérifiés**, et non déclarés conformes.

## Constats prioritaires

| ID | Prio. | Preuve | Impact / action attendue |
| --- | --- | --- | --- |
| UX-001 | P1 | `scanner.js` installe un champ de scan global, reprend périodiquement le focus et pilote un overlay créé en JavaScript. | Risque de voler le focus pendant une saisie ou une action de supervision. Tester tout le parcours clavier; suspendre la capture dès qu'un contrôle interactif/modal est actif et annoncer explicitement le mode scanner. |
| UX-002 | P1 | Les overlays de scan sont construits dynamiquement; les messages succès/erreur disparaissent après environ 1 s. | Temps insuffisant pour certains opérateurs et annonces de lecteur d'écran non démontrées (WCAG 2.2.1, 4.1.3). Employer une région `aria-live`, laisser les erreurs persistantes jusqu'à acquittement et rendre la durée configurable. |
| UX-003 | P1 | Le traitement enchaîne des appels `fetch`; une erreur réseau déclenche immédiatement un second POST. Un `requestId` existe, mais le comportement de bout en bout sous réponse perdue n'a pas été testé ici. | Le retour opérateur peut être ambigu ou désordonné. Ajouter un test E2E réseau lent/perte de réponse et afficher un état « en cours / résultat confirmé / à réessayer ». |
| UX-004 | P2 | Plusieurs boutons utilisent `min-height: 36px` ou `40px` (par ex. actions dans `Views/Box/Details.cshtml`), alors que d'autres utilisent 44–48 px. | Cibles irrégulières pour tactile/gants (WCAG 2.5.8). Standardiser les actions opérateur à 48 px minimum, espace inclus, puis tester avec gants. |
| UX-005 | P2 | Les retours de scan combinent texte, couleur et sons, mais aucun réglage/mute ni alternative haptique/visuelle persistante n'est documenté. | En atelier bruyant, le son seul n'est pas fiable; pour les personnes sensibles, il doit être contrôlable. Ajouter un réglage poste et conserver une confirmation visuelle non fugace. |
| UX-006 | P2 | Des styles inline nombreux coexistent avec les tokens CSS. Les libellés sont majoritairement en anglais alors que des rôles français existent. | Cohérence et maintenabilité faibles; charge cognitive multilingue. Centraliser tailles/états et valider la langue de travail de l'usine. |
| UX-007 | P2 | Les modales Bootstrap ont `tabindex=-1` et bouton Close, mais aucun test automatisé ne prouve nom accessible, focus initial/restauré, Escape et piège de focus. | Conformité clavier/lecteur d'écran non démontrée (WCAG 2.1.1, 2.4.3, 4.1.2). Ajouter des tests axe/Playwright et une recette NVDA. |

## Points favorables vérifiés statiquement

- Images observées avec texte alternatif; boutons de fermeture et bascule du mot de passe nommés.
- Labels Razor associés aux champs et messages `asp-validation-for` présents sur les formulaires principaux.
- Plusieurs alertes utilisent `role="alert"`; la navigation paginée possède un `aria-label`.
- Les données injectées dans l'overlay scanner sont échappées avant insertion HTML.
- États vides, erreurs, progression et feedback multimodal sont prévus.

## Recette UI/UX obligatoire sur cible

1. Parcours complet uniquement au clavier et avec NVDA: connexion, création, scan, doublon, blocage, impression et retour après erreur.
2. axe/WAVE sans violation critique ou sérieuse; reflow à 320 px et zoom 200/400 % sans perte d'action.
3. Contrastes mesurés: texte normal ≥ 4,5:1, grand texte et composants ≥ 3:1; statut jamais transmis par la couleur seule.
4. Rafale de scans et réseau lent/perdu: aucune réponse hors ordre, double action ou focus volé.
5. Essai écran réel avec gants, bruit d'atelier et scanner physique; cibles critiques ≥ 48 px et retour perceptible en moins de 500 ms (objectif à faire valider par l'usine).

**Décision UX :** pas de déclaration WCAG AA ni d'aptitude poste industriel avant cette recette. Les défauts ci-dessus sont issus du code; l'efficacité tactile, scanner et lecteur d'écran reste un risque d'environnement cible.
