# Graph Report - LevelUp  (2026-11-06)

## Corpus Check
- 73 files · ~75,095 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 209 file(s) not represented in the graph (top: .meta 140, .asset 34, .shader 13)
- WARNING: 209 of 291 files seen (72%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 1899 nodes · 5420 edges · 116 communities (56 shown, 60 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 430 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `efcf7853`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- LevelUp.Utils
- CardModel
- GameBootstrapper
- OptionsPanel.Setup
- RectTransform
- manifest.json
- com.unity.2d.common@8.1.0
- CardView
- GameEvents.cs
- com.unity.render-pipelines.core
- TurnStartedEvent
- LevelProgressView
- com.unity.mathematics@1.2.6
- GameCommandExecutor
- OnlinePanel
- dependencies
- com.unity.modules.physics@1.0.0
- com.unity.modules.jsonserialize@1.0.0
- com.unity.ext.nunit
- com.unity.modules.imgui@1.0.0
- GameManager
- com.unity.nuget.newtonsoft-json
- dependencies
- CardColor
- com.unity.modules.unitywebrequest@1.0.0
- packages-lock.json
- Color
- DiscardPileView
- OnlineSession
- DeckView
- com.unity.2d.tilemap
- CmdKind
- com.unity.ugui@1.0.0
- DeckManager
- AIPlayer
- DevAutoScreenshot
- .ExecuteCommand
- ActionCardHandler
- CardModel
- com.unity.modules.androidjni@1.0.0
- com.unity.modules.unitywebrequest@1.0.0
- EventBus
- PauseMenuController
- CLAUDE.md - Brief d'execution optimise pour Claude
- com.unity.inputsystem
- MonoBehaviour
- GameSettings
- UISkin
- com.unity.2d.spriteshape
- com.unity.nuget.mono-cecil@1.10.1
- MainMenuController
- Scripts UI (enfants du Canvas)
- com.unity.modules.unitywebrequesttexture@1.0.0
- Test
- HandView
- EventBus.Subscribe
- GameOverCelebration
- AndroidBuilder
- IGameCommand
- Image
- GameInputController
- 3. Configuration détaillée par GameObject
- GameScreenLayout
- com.unity.collab-proxy@2.12.4
- com.unity.feature.2d@2.0.1
- com.unity.ide.rider@3.0.36
- com.unity.ide.visualstudio@2.0.22
- com.unity.inputsystem@1.7.0
- com.unity.modules.ai@1.0.0
- com.unity.modules.animation@1.0.0
- com.unity.modules.assetbundle@1.0.0
- com.unity.modules.audio@1.0.0
- com.unity.modules.cloth@1.0.0
- com.unity.modules.director@1.0.0
- com.unity.modules.imgui@1.0.0
- com.unity.modules.particlesystem@1.0.0
- com.unity.modules.physics@1.0.0
- com.unity.modules.physics2d@1.0.0
- com.unity.modules.screencapture@1.0.0
- com.unity.modules.terrain@1.0.0
- com.unity.modules.terrainphysics@1.0.0
- com.unity.modules.ui@1.0.0
- com.unity.modules.uielements@1.0.0
- com.unity.modules.unitywebrequestassetbundle@1.0.0
- com.unity.modules.unitywebrequestaudio@1.0.0
- com.unity.modules.unitywebrequestwww@1.0.0
- com.unity.modules.vehicles@1.0.0
- com.unity.modules.vr@1.0.0
- com.unity.modules.wind@1.0.0
- com.unity.netcode.gameobjects@1.12.0
- com.unity.services.authentication@2.7.4
- com.unity.services.relay@1.1.1
- com.unity.test-framework@1.1.33
- com.unity.timeline@1.7.7
- README.md
- FastBufferReader
- GameSession
- 4) Backlog priorise (spec fonctionnelle)
- TableView

## God Nodes (most connected - your core abstractions)
1. `CardModel` - 70 edges
2. `HandView` - 68 edges
3. `GameInputController` - 65 edges
4. `OnlineSession` - 64 edges
5. `GameBootstrapper` - 58 edges
6. `LevelUp.Utils` - 51 edges
7. `CardView` - 48 edges
8. `GameScreenLayout` - 47 edges
9. `LevelUp.Core` - 45 edges
10. `UISkin` - 45 edges

## Surprising Connections (you probably didn't know these)
- `5. Références à assigner sur GameBootstrapper` --references--> `GameBootstrapper`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/GameBootstrapper.cs
- `9) Prochaines taches recommandees (ordre d'execution)` --references--> `HandView`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `3.5.a HandContainer (enfant de PlayerHand)` --references--> `HandView`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `3.6 TurnBannerView (bannière temporaire "Tour de X")` --references--> `TurnStartedEvent`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/Core/GameEvents.cs
- `5) Contraintes de mise en oeuvre` --references--> `LevelValidator`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/Core/LevelValidator.cs

## Import Cycles
- None detected.

## Communities (116 total, 60 thin omitted)

### Community 1 - "LevelUp.Utils"
Cohesion: 0.06
Nodes (22): LevelUp.Core, LevelUp.Tests, LevelUp.Utils, LevelUp.EditorTools, LevelUp.UI, LevelUp.AI, LevelUp.Network, NetworkMode (+14 more)

### Community 2 - "CardModel"
Cohesion: 0.13
Nodes (5): CardExtensions, CardModel, IsAction, IsWild, HandChangedEvent

### Community 4 - "OptionsPanel.Setup"
Cohesion: 0.17
Nodes (3): LayDownChooser, IsOpen, OptionsPanel

### Community 5 - "RectTransform"
Cohesion: 0.10
Nodes (5): AnimationController, IsAnimating, PileSlot, UITween, Runner

### Community 6 - "manifest.json"
Cohesion: 0.17
Nodes (11): com.unity.modules.jsonserialize@1.0.0, com.unity.modules.umbra@1.0.0, com.unity.render-pipelines.universal@14.0.11, com.unity.textmeshpro@3.0.9, com.unity.visualscripting@1.9.4, com.unity.modules.jsonserialize, com.unity.modules.umbra, com.unity.modules.unityanalytics (+3 more)

### Community 7 - "com.unity.2d.common@8.1.0"
Cohesion: 0.05
Nodes (44): dependencies, depth, source, url, version, dependencies, depth, source (+36 more)

### Community 8 - "CardView"
Cohesion: 0.15
Nodes (6): CardView, CardModel, IsFaceUp, IsHovered, IsSelected, RectTransform

### Community 9 - "GameEvents.cs"
Cohesion: 0.15
Nodes (12): ActionCardPlayedEvent, CardAddedToMeldEvent, CardDiscardedEvent, CardDrawnEvent, DeckExhaustedEvent, GameOverEvent, InvalidActionEvent, LevelCompletedEvent (+4 more)

### Community 11 - "com.unity.render-pipelines.core"
Cohesion: 0.07
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 12 - "TurnStartedEvent"
Cohesion: 0.10
Nodes (5): AIThinkingEvent, TurnStartedEvent, NetworkManager, IsPassScreenActive, PlayerTurnGlow

### Community 13 - "LevelProgressView"
Cohesion: 0.12
Nodes (15): 0. Pré-requis : Canvas Scaler, 10. Méthode pour ajuster visuellement dans Unity, 11. Test et validation, 12. Troubleshooting, 13. Ajustements mobiles (optionnel), 14. Couleurs de référence (cohérence visuelle), 1. Vue d'ensemble du layout, 2. Hiérarchie cible (+7 more)

### Community 14 - "com.unity.mathematics@1.2.6"
Cohesion: 0.08
Nodes (28): dependencies, depth, source, url, version, dependencies, depth, source (+20 more)

### Community 15 - "GameCommandExecutor"
Cohesion: 0.14
Nodes (13): GameCommandExecutor, CommandResult, DrawnCard, Message, RoundEnded, Success, WinnerOverride, TurnPhaseChangedEvent (+5 more)

### Community 16 - "OnlinePanel"
Cohesion: 0.20
Nodes (3): OnlinePanel, IsVisible, LastCode

### Community 17 - "dependencies"
Cohesion: 0.17
Nodes (12): dependencies, com.unity.modules.imageconversion@1.0.0, com.unity.modules.tilemap@1.0.0, com.unity.modules.unityanalytics@1.0.0, com.unity.modules.video@1.0.0, com.unity.modules.xr@1.0.0, com.unity.ugui@1.0.0, com.unity.modules.imageconversion (+4 more)

### Community 18 - "com.unity.modules.physics@1.0.0"
Cohesion: 0.07
Nodes (27): dependencies, depth, source, version, dependencies, depth, source, version (+19 more)

### Community 19 - "com.unity.modules.jsonserialize@1.0.0"
Cohesion: 0.08
Nodes (28): dependencies, depth, source, version, dependencies, depth, source, version (+20 more)

### Community 20 - "com.unity.ext.nunit"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, url, version, dependencies, depth, source (+18 more)

### Community 21 - "com.unity.modules.imgui@1.0.0"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, url, version, dependencies, depth, source (+18 more)

### Community 22 - "GameManager"
Cohesion: 0.09
Nodes (19): GameManager, Config, DeckManager, Executor, Players, RoundNumber, State, TurnManager (+11 more)

### Community 23 - "com.unity.nuget.newtonsoft-json"
Cohesion: 0.06
Nodes (34): dependencies, depth, source, version, dependencies, depth, source, url (+26 more)

### Community 24 - "dependencies"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, url, version, dependencies, depth, source (+18 more)

### Community 25 - "CardColor"
Cohesion: 0.11
Nodes (15): CardColor, Blue, Green, Orange, Purple, Red, Wild, Yellow (+7 more)

### Community 26 - "com.unity.modules.unitywebrequest@1.0.0"
Cohesion: 0.05
Nodes (49): dependencies, depth, source, version, dependencies, depth, source, version (+41 more)

### Community 27 - "packages-lock.json"
Cohesion: 0.08
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 28 - "Color"
Cohesion: 0.09
Nodes (3): BalatroEffects, RoundTransitionView, TurnBannerView

### Community 29 - "DiscardPileView"
Cohesion: 0.15
Nodes (6): DiscardPileView, PileCount, PileSpacing, 15.1 Scripts à placer MANUELLEMENT dans la scène `Game.unity`, Scripts racine (GameObjects vides, pas sous Canvas), Scripts UI (enfants du Canvas)

### Community 30 - "OnlineSession"
Cohesion: 0.10
Nodes (8): OnlineSession, DeviceToken, IsActive, IsSessionHost, JoinCode, LastJoinCode, LobbyHumanCount, VersionTag

### Community 32 - "com.unity.2d.tilemap"
Cohesion: 0.12
Nodes (18): dependencies, depth, dependencies, depth, source, url, version, source (+10 more)

### Community 33 - "CmdKind"
Cohesion: 0.22
Nodes (8): CmdKind, AddToMeld, Discard, DrawFromDeck, DrawFromDiscard, LayDown, SkipPhase, NetCommandCodec

### Community 34 - "com.unity.ugui@1.0.0"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, url, version, depth, source, version (+9 more)

### Community 35 - "DeckManager"
Cohesion: 0.10
Nodes (16): DeckManager, DiscardPileCount, DrawPileCount, GameConfig, CardMaxValue, CardMinValue, CardsPerPlayer, DeckSize (+8 more)

### Community 36 - "AIPlayer"
Cohesion: 0.20
Nodes (6): AIDifficulty, Easy, Hard, Normal, AIPlayer, PlayerIndex

### Community 39 - "ActionCardHandler"
Cohesion: 0.23
Nodes (4): ActionCardHandler, ForcedDrawEvent, PlayerSkippedEvent, SkipStackingTests

### Community 40 - "CardModel"
Cohesion: 0.08
Nodes (14): ContractCatalog, ContractPlan, IsRandom, ContractProgress, Entry, IsMet, LevelDefinition, MeldRequirement (+6 more)

### Community 43 - "EventBus"
Cohesion: 0.22
Nodes (4): 2) Contexte technique du repo (a respecter), EventBus, Muted, Subscription

### Community 44 - "PauseMenuController"
Cohesion: 0.27
Nodes (3): PauseMenuController, IsPaused, 15.2 Scripts AUTO-GÉNÉRÉS au runtime (ne pas les ajouter à la main)

### Community 45 - "CLAUDE.md - Brief d'execution optimise pour Claude"
Cohesion: 0.20
Nodes (9): 1) Role de Claude dans ce projet, 3) Objectif produit global, 5) Contraintes de mise en oeuvre, 6) Definition of Done (DoD), 7) Format de reponse attendu de Claude, 8) Regles de priorisation quand il y a ambiguite, 9) Prochaines taches recommandees (ordre d'execution), CLAUDE.md - Brief d'execution optimise pour Claude (+1 more)

### Community 46 - "com.unity.inputsystem"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.inputsystem

### Community 47 - "MonoBehaviour"
Cohesion: 0.17
Nodes (4): ConnectionOverlay, IsVisible, TextMirror, SafeAreaFitter

### Community 48 - "GameSettings"
Cohesion: 0.16
Nodes (8): GameSettings, ColorblindMode, Difficulty, MusicVolume, PlayerCount, QualityIndex, RandomContracts, SfxVolume

### Community 49 - "UISkin"
Cohesion: 0.05
Nodes (23): AnimatedBackground, UIFactory, CircleSprite, RingSprite, RoundedSprite, SmallPillSprite, SoftShadowSprite, UIIcon (+15 more)

### Community 50 - "com.unity.2d.spriteshape"
Cohesion: 0.14
Nodes (14): dependencies, depth, source, url, version, dependencies, depth, source (+6 more)

### Community 51 - "com.unity.nuget.mono-cecil@1.10.1"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, url, version, dependencies, depth, source (+5 more)

### Community 52 - "MainMenuController"
Cohesion: 0.19
Nodes (3): MainMenuController, IsVisible, Root

### Community 53 - "Scripts UI (enfants du Canvas)"
Cohesion: 0.29
Nodes (7): TweenRunner, 15.3 Scripts sur PREFABS (pas dans la scène), 15.4 Helpers STATIQUES (aucun GameObject), 15.5 Hiérarchie finale complète (copier-coller mental), 15.6 Toutes les références Inspector (checklist drag & drop), 15.7 Résumé en 3 lignes, 15. Mapping complet script → GameObject

### Community 55 - "Test"
Cohesion: 0.10
Nodes (13): LayDownLevelCommand, Melds, PlayerIndex, Meld, Cards, OwnerIndex, Type, CardExtensionsTests (+5 more)

### Community 56 - "HandView"
Cohesion: 0.12
Nodes (6): HandView, CardCount, IsAnimatingDeal, IsReorderDragging, SelectedCard, SelectedCards

### Community 60 - "IGameCommand"
Cohesion: 0.13
Nodes (20): AddToMeldCommand, Card, MeldIndex, MeldOwnerIndex, PlayerIndex, DiscardCommand, Card, PlayerIndex (+12 more)

### Community 62 - "Image"
Cohesion: 0.19
Nodes (4): ContractRow, StepPill, PlayerPill, UIInputBlocker

### Community 65 - "GameInputController"
Cohesion: 0.09
Nodes (7): GameInputController, ActionsLocked, CurrentPlayerIndex, DpiScale, DragUpThreshold, IsLocalTurn, ReorderDragThreshold

### Community 66 - "3. Configuration détaillée par GameObject"
Cohesion: 0.17
Nodes (12): 3.1 HUD (barre du haut), 3.2 DiscardPileView (défausses de tous les joueurs), 3.3 LevelProgress (colonne gauche), 3.4.a DeckArea (pile de pioche), 3.4.b DiscardArea (défausse unique au centre), 3.4.c Table / TableContainer (melds du joueur local), 3.4 PlayZone (conteneur central), 3.5.a HandContainer (enfant de PlayerHand) (+4 more)

### Community 68 - "GameScreenLayout"
Cohesion: 0.05
Nodes (4): GameScreenLayout, HUDView, LevelProgressView, SelectionStatusView

### Community 102 - "GameSession"
Cohesion: 0.29
Nodes (5): GameSession, Contracts, IsHost, IsOnline, LocalPlayerIndex

### Community 103 - "4) Backlog priorise (spec fonctionnelle)"
Cohesion: 0.40
Nodes (5): 4) Backlog priorise (spec fonctionnelle), A. UX & Gameplay (zero friction) - PRIORITE P0, B. Direction visuelle & juice - PRIORITE P1, C. Accessibilite & responsive - PRIORITE P1, D. Menus & retention - PRIORITE P2

### Community 104 - "TableView"
Cohesion: 0.16
Nodes (4): MeldGroupView, MeldType, OwnerIndex, TableView

## Knowledge Gaps
- **164 isolated node(s):** `IsRandom`, `IsMet`, `CardModel`, `IsFaceUp`, `IsSelected` (+159 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 541 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **60 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `GameInputController` connect `GameInputController` to `Vector2`, `LevelUp.Utils`, `CardModel`, `GameBootstrapper`, `GameScreenLayout`, `OptionsPanel.Setup`, `.ExecuteCommand`, `RectTransform`, `CardView`, `TableView`, `EventBus`, `GameInputController.SortCurrentPlayerHand`, `MonoBehaviour`, `GameScreenLayout.Build`, `HandView`, `Color`, `DiscardPileView`, `Image`?**
  _High betweenness centrality (0.058) - this node is a cross-community bridge._
- **What connects `IsRandom`, `IsMet`, `CardModel` to the rest of the system?**
  _164 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `LevelUp.Utils` be split into smaller, more focused modules?**
  _Cohesion score 0.05871559633027523 - nodes in this community are weakly interconnected._
- **Why does `GameBootstrapper` connect `GameBootstrapper` to `LevelUp.Utils`, `GameInputController`, `CardModel`, `GameScreenLayout`, `.ExecuteCommand`, `TableView`, `GameBootstrapper.HandleResume`, `PauseMenuController`, `LevelProgressView`, `MonoBehaviour`, `OnlinePanel`, `DiscardPileView`, `MainMenuController`, `HandView`, `Color`, `GameScreenLayout.Build`, `OnlineSession`?**
  _High betweenness centrality (0.052) - this node is a cross-community bridge._
- **Should `CardModel` be split into smaller, more focused modules?**
  _Cohesion score 0.13257575757575757 - nodes in this community are weakly interconnected._
- **Why does `HandView` connect `HandView` to `Vector2`, `LevelUp.Utils`, `CardModel`, `GameBootstrapper`, `RectTransform`, `CardView`, `GameBootstrapper.HandleResume`, `Color`, `DiscardPileView`, `CardModel`, `EventBus`, `CLAUDE.md - Brief d'execution optimise pour Claude`, `MonoBehaviour`, `GameScreenLayout.Build`, `Image`, `GameInputController`, `3. Configuration détaillée par GameObject`, `GameScreenLayout`, `GameInputController.SortCurrentPlayerHand`?**
  _High betweenness centrality (0.050) - this node is a cross-community bridge._
- **Should `GameBootstrapper` be split into smaller, more focused modules?**
  _Cohesion score 0.11333333333333333 - nodes in this community are weakly interconnected._