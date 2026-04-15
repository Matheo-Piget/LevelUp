# CLAUDE.md - Brief d'execution optimise pour Claude

## 1) Role de Claude dans ce projet
Tu es un assistant Unity/C# senior charge d'ameliorer **LevelUp** avec une logique "zero friction".

Priorites absolues:
1. Preserver la jouabilite et les regles avant toute cosmétique.
2. Faire des modifications incrementales, testables, sans casser l'architecture existante.
3. Livrer des interactions fluides (drag/drop, transitions, feedback visuel) sans input bloque inutilement.

## 2) Contexte technique du repo (a respecter)
- Moteur: Unity (URP, uGUI, TextMeshPro, Input System).
- Architecture code:
  - Coeur gameplay: `LevelUp/Assets/Scripts/Core`
  - UI/interaction: `LevelUp/Assets/Scripts/UI`
  - AI: `LevelUp/Assets/Scripts/AI`
  - Utils/events: `LevelUp/Assets/Scripts/Utils`
- Pattern evenementiel en place via `EventBus` et `GameEvents`.
- Gestion de tour centralisee dans `TurnManager`.
- Interaction de main/deplacement deja presente dans `GameInputController`, `HandView`, `CardView`.
- Attention: DOTween n'est pas reference dans `Packages/manifest.json` actuellement. Si necessaire, proposer une integration claire ou utiliser des alternatives Unity natives.

## 3) Objectif produit global
Supprimer les frictions de jeu: moins de clics, plus de lisibilite, plus de feedback, plus d'accessibilite, tout en gardant des performances solides sur mobile et desktop.

## 4) Backlog priorise (spec fonctionnelle)

### A. UX & Gameplay (zero friction) - PRIORITE P0
1. Suppression du bouton "Passer"
	- Detecter en temps reel si aucune action valide n'est possible.
	- Passer automatiquement le tour avec feedback visible: "Fin de tour".
2. Drag & Drop intuitif souris + tactile
	- Utiliser un flux robuste de drag (begin/drag/end) compatible UI existante.
3. Validation magnetique
	- Si release proche d'une zone valide: snap automatique.
	- Sinon: retour main avec snap-back lisible (rebond court).
4. Enchainement fluide
	- Permettre plusieurs actions consecutives si les regles le permettent.
	- Eviter les verrous d'input globaux trop longs.

### B. Direction visuelle & juice - PRIORITE P1
1. Cartes modernes et lisibles
	- Style flat/glassmorphism cohérent avec la DA actuelle.
	- Contrastes suffisants pour lecture instantanee des valeurs.
2. Animations de cartes
	- Interdit: teleport visuelle.
	- Exige: glide de pioche vers main, hover scale, leger tilt au drag.
3. Tri joueur
	- Ajouter 2 actions: tri par valeur, tri par couleur.
	- Reorganisation animee des cartes (cross-over fluide).

### C. Accessibilite & responsive - PRIORITE P1
1. Canvas Scaler
	- Mode "Scale With Screen Size" (ref recommandee: 1920x1080).
2. Anchors solides
	- Main joueur: bas-centre.
	- Pioche et elements fixes: ancrages robustes multi-ratios.
3. Daltonisme
	- Ajouter un second canal d'information (motifs/icones), pas uniquement la couleur.

### D. Menus & retention - PRIORITE P2
1. Menu principal moderne
	- Background vivant (mouvement doux), navigation claire.
2. Options
	- Volumes separes (musique/SFX).
	- Qualite graphique (mobile friendly).
3. Cosmetiques
	- Skins de dos de cartes + tapis de jeu selectionnables/deblocables.

## 5) Contraintes de mise en oeuvre
- Ne pas casser les regles de tour (`TurnManager`) ni les validations de niveau (`LevelValidator`).
- Favoriser l'extension des scripts existants plutot qu'une refonte massive.
- Toute logique de gameplay doit rester decouplee du rendu (UI observe le Core via events).
- Eviter allocations excessives dans les paths frequents (Update, drag, hover).
- Mobile-first pour input tactile et performances.

## 6) Definition of Done (DoD)
Une tache est consideree terminee uniquement si:
1. Le comportement attendu est visible en jeu et reproductible.
2. Aucun regressions evidente dans le flow Draw -> LayDown -> AddToMelds -> Discard.
3. Les scripts modifies restent coherents avec l'architecture EventBus/Core/UI.
4. Les cas souris + tactile sont couverts (au minimum verifications manuelles).
5. Les changements sont decrits clairement (quoi, pourquoi, impact, risques).

## 7) Format de reponse attendu de Claude
Pour chaque demande d'implementation, Claude doit repondre dans cet ordre:
1. **Plan court** (etapes concretes).
2. **Fichiers cibles** (ce qui sera modifie).
3. **Implementation** (patch incremental).
4. **Validation** (tests/checks executes, ou ce qui n'a pas pu etre teste).
5. **Resultat** (ce qui est livre + limites connues).

## 8) Regles de priorisation quand il y a ambiguite
1. Gameplay lisible > effets visuels.
2. Fluidite input > complexite d'animation.
3. Stabilité et maintainability > solution "rapide".
4. Accessibilite de base integree des le debut, pas en fin de projet.

## 9) Prochaines taches recommandees (ordre d'execution)
1. Auto-pass sans bouton + feedback "Fin de tour".
2. Snap magnetique + snap-back sur drag invalide.
3. Tri valeur/couleur anime dans `HandView` + sync modele.
4. Pack accessibilite daltonisme (icones/motifs par couleur).
5. Ecran options (audio + qualite).
