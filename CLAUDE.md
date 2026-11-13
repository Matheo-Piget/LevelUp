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
Deja livres : snap magnetique + snap-back, tri valeur/couleur anime, daltonisme (symboles), options,
pseudo + salon en ligne, ecran "Nouvelle partie", minuteur, recap de manche, classement final, bots v2,
audio procedural + vibrations, fin de manche explicite, sauvegarde/reprise solo, donne du jour, "Comment jouer",
interface "grande" (telephones), cosmetiques (tapis / dos), points de penalite, bouton Retour Android.
1. Mise en page telephone dediee (au-dela du facteur 1.3 de `GameSettings.UiScaleFactor`) : cibles >= 7 mm.
2. Pass-and-play local (plusieurs humains sur un appareil) : `NetworkManager` n'est qu'un stub.
3. Tutoriel jouable (donne scriptee) ; decoupage de `GameInputController` (etat par phase) et d'`OnlineSession`.
4. `InputSystemUIInputModule` a la place de `StandaloneInputModule` (a verifier sur appareil).

## 10) Carte du code (a lire AVANT d'explorer : evite de tout relire)

### Ce qui tourne vraiment
- Scene `Assets/Scenes/Game.unity` (seule scene) : `GameBootstrapper`, `GameManager`, `HandView`, `TableView`,
  `HUDView`, `LevelProgressView`, `DiscardPileView`, `GameInputController`, `AnimationController`, `BalatroEffects`,
  `NetworkManager` (stub pass-and-play, inactif). `CardView` est dans `Assets/Prefab/Card.prefab`.
- Crees au runtime par `GameBootstrapper` (AddComponent) : `MainMenuController`, `PauseMenuController`,
  `GameOverCelebration`, `GameScreenLayout`, `AnimatedBackground`, `SelectionStatusView` (+ bouton POSER),
  `RoundSummaryView`, `TurnTimer`, `AIPlayer` (un par bot), `OnlineSession`, `OnlinePanel`, `ConnectionOverlay`,
  `AudioManager` (sons synthetises au demarrage, aucun fichier audio), `AppLifecycle` (Retour/Echap, sauvegarde
  des reglages en pause), `CardFlights` + `OpponentMovesView` (cartes en vol des adversaires).
  Crees par d'autres vues : `OptionsPanel`, `MatchSetupPanel`, `ProfileDialog`, `RulesPanel` (menu / pause) ;
  `LayDownChooser`, `TargetChooser` (input).
- Fenetres : chacune s'inscrit dans `UIBackStack` (Push a l'ouverture, Remove a la fermeture) pour le bouton Retour.
- Trajets de cartes : `CardOrigins.Provider` (main du joueur local, sinon pastille du joueur) ; `ReducedMotion` coupe.
- Aides de jeu = interrupteurs separes (`GameSettings`) : `HintSteps` (consigne d'etape, ON), `HintContract`
  (progression du contrat, "contrat pret", pose auto sans selection, OFF), `HintPlayable` (cartes "+", aimant
  cible, pose d'un toucher, OFF), `QuickDiscard` (OFF = toucher pour soulever, retoucher pour defausser).
  Toute nouvelle aide qui revele une information de jeu doit etre derriere un de ces reglages, coupee par defaut.
- `BalatroEffects` : textes flottants et particules d'ambiance desactives (consts `FloatingTexts`/`AmbientParticles`).

### Regles (Core)
- Toute action = commande -> `GameManager.ExecuteCommand` -> `GameCommandExecutor`. IA, UI, minuteur et reseau
  passent tous par la. Fin de manche : main vide apres defausse, apres ajout de la derniere carte OU apres une
  pose qui vide la main. Commandes refusees hors `GameState.PlayerTurn` ; Piocher/Defausser non passables ;
  les cartes d'une commande sont retrouvees dans la main par Id (les autres champs ne font pas foi).
- Fin de manche explicite : `GameManager.AutoStartNextRound` (vrai dans les tests, FAUX en jeu) -> la partie
  reste en `EndRound` (table et mains intactes), `RoundSummaryEvent` porte le bilan, `StartNextRoundCommand`
  lance la manche suivante (en ligne : seul l'hote). Penalites : `PlayerModel.PenaltyPoints` (cartes restees).
- `GameManager.RulesVersion` : a INCREMENTER si une regle ou le format d'une commande change (sauvegarde + reseau).
- Serialisation : `GameCommandCodec` (Core, BinaryWriter, lecture bornee et sure) pour le reseau ET `SaveGame`
  (partie solo sauvegardee commande par commande dans persistentDataPath, reprise par `RebuildFromHistory`).
- `MatchRules` (immuable, par partie) : contrats fixes/varies, cartes speciales (aucune par defaut / jokers /
  jokers + Passe + +2), taille de main, niveau final, minuteur. `GameSession` (statique) expose `Rules`,
  `Contracts`, `IsOnline`, `LocalPlayerIndex`. Paquet : `DeckManager.BuildComposition` = 6 couleurs x 1..18
  (chaque couple valeur/couleur existe UNE fois) + speciales si l'option est active.
- Contrats : `ContractCatalog` (variantes par niveau) + `LevelValidator` (recherche exhaustive des poses).
- Fin de partie : depasser `Rules.LevelsToWin` ; egalite -> le gagnant de la manche.
- `PlayerProfile` (Core, PlayerPrefs) : pseudo nettoye (`Sanitize`, max 14 car., pas de `| < >`) + stats.

### IA (`Scripts/AI`)
- `BotBrain` (pur C#, teste) : manque de cartes pour le contrat (glouton sur toutes les permutations) + "outs"
  (cartes invisibles utiles) ; danger d'une defausse (carte jouable sur la table quand un adversaire a pose,
  ramassages adverses en Expert). `AIPlayer` = pilote (delais, garde "toujours mon tour").
- Difficulte = `AIDifficulty` Easy/Normal/Hard (UI : FACILE/NORMAL/EXPERT). Le bot ne voit que l'info publique.

### En ligne (`Scripts/Network`)
- Simulation miroir : meme seed + memes sieges + memes `MatchRules` => meme partie ; chaque commande validee est
  relayee. L'hote = siege 0, les bots ne tournent QUE chez l'hote. `ProtocolVersion` (OnlineSession) a
  INCREMENTER des qu'un format de message change (v5 = GameCommandCodec + fin de manche explicite) ; le tag de
  version inclut aussi `GameManager.RulesVersion`. Les messages hote -> clients sont ignores s'ils ne viennent
  pas du serveur (`IsFromHost`), toutes les tailles lues sont bornees.
- Connexion : payload `LU|version|jeton|pseudo`. Salon : message `LU_LOBBY` (membres + places + regles) diffuse
  par l'hote, `OnlinePanel` ne depend pas du reseau (le bootstrapper convertit `LobbyInfo` -> `LobbyViewModel`).

### UI / DA "Salon"
- 100 % procedural (aucun prefab hors Card). Jetons et helpers : `UISkin` (couleurs, polices Inter + DM Serif
  Display, sprites arrondis, icones rasterisees), `UIControls` (lignes, segmente, interrupteur), `UIInputs`
  (champs TMP), `UIFactory.CreateButton` (primaire champagne / secondaire / discret).
- Reference 1920x1080 divisee par `GameSettings.UiScaleFactor` (1.3 en interface "grande", defaut sur ecran
  < 8 pouces) (`GameScreenLayout.ConfigureScaler`), UI de jeu sous la safe area (`SafeAreaFitter.Wrap`) ;
  `GameScreenLayout` replace/restyle les objets de scene (les valeurs serialisees en scene sont ecrasees en code).
- Reglages : `GameSettings` (UI, PlayerPrefs `levelup.*`) = preferences appareil + reglages de la prochaine partie
  (`BuildMatchRules()`). Options = appareil seulement ; "Nouvelle partie" = regles.

### Verifier sans appareil (editeur Unity FERME, sinon le batchmode echoue)
- Compiler + tests EditMode : `Unity.exe -batchmode -projectPath <repo>\LevelUp -runTests -testPlatform EditMode
  -testResults <xml> -logFile <log>` (Unity 2022.3.62f3, chemin dans la memoire `levelup-tooling`).
- Visuel : build Windows (`-executeMethod LevelUp.EditorTools.AndroidBuilder.BuildWindows -quit`) puis
  `LevelUp.exe -screen-fullscreen 0 -screen-width 1600 -screen-height 720 -autoshot <dossier>`
  (`DevAutoScreenshot` : menu, profil, options, en ligne, nouvelle partie, jeu, table, pose, cibles, recap,
  pause, fin de partie). APK : `AndroidBuilder.BuildApk`.
- Les `.meta` des nouveaux scripts sont generes par Unity : les commiter.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- When graphify-out/graph.json exists, run `graphify query "<question>"` before your first grep, search or source-file read for a codebase question, and put this rule in every subagent prompt that explores code. Read raw files only for lines the graph or an earlier read already located. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
- A committed graphify-out/graph.json is rewritten whole on every build, so a large diff to it in a pull request is expected. Do not flag it as churn or suggest skipping the graph rebuild or its hooks.
