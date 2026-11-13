# Graph Report - LevelUp  (2026-11-13)

## Corpus Check
- 99 files · ~106,728 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 235 file(s) not represented in the graph (top: .meta 166, .asset 34, .shader 13)
- WARNING: 235 of 343 files seen (69%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 2442 nodes · 7681 edges · 153 communities (71 shown, 82 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 749 edges (avg confidence: 0.87)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `128a0f18`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- LevelUp.Utils
- CardModel
- UISkin.Size
- HandView
- manifest.json
- com.unity.2d.common@8.1.0
- GameSession
- IEnumerator
- AudioManager
- com.unity.render-pipelines.core
- NetworkManager
- Guide de layout Unity — LevelUp
- com.unity.mathematics@1.2.6
- PlayerModel
- MonoBehaviour
- dependencies
- com.unity.modules.physics@1.0.0
- com.unity.modules.jsonserialize@1.0.0
- com.unity.ext.nunit
- com.unity.modules.imgui@1.0.0
- com.unity.nuget.newtonsoft-json
- dependencies
- HUDView
- com.unity.modules.unitywebrequest@1.0.0
- packages-lock.json
- CardFactory.B
- MatchRulesTests
- OnlineSession
- BotBrain
- com.unity.2d.tilemap
- GameScreenLayout.RefreshContract
- com.unity.ugui@1.0.0
- EventBus.Clear
- AIPlayer
- DevAutoScreenshot
- GameManager.ExecuteCommand
- DeckManager
- LevelRequirement
- com.unity.modules.androidjni@1.0.0
- com.unity.modules.unitywebrequest@1.0.0
- MatchRules
- CLAUDE.md - Brief d'execution optimise pour Claude
- com.unity.inputsystem
- ContractAndReplayTests
- GameSettings
- UISkin
- com.unity.2d.spriteshape
- com.unity.nuget.mono-cecil@1.10.1
- 3. Configuration détaillée par GameObject
- CanvasGroup
- com.unity.modules.unitywebrequesttexture@1.0.0
- Test
- EventBus.Subscribe
- GameInputController.ExecuteAddToMeld
- AndroidBuilder
- GameSession.IsLocalHuman
- MainMenuController
- CommandError
- Transform
- GameInputController
- GameObject
- EventBus.Subscribe
- CardView
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
- AnimationController
- LevelProgressView
- BalatroEffects
- MeldGroupView
- DiscardPileView
- RoundSummaryView
- TextWeight
- GameOverEvent
- Meld
- SelectionStatusView
- IGameCommand
- 15. Mapping complet script → GameObject
- TurnStartedEvent
- DailyDeal
- PlayerSetup
- RectTransform
- ConfirmDialog
- IReadOnlyList
- Regles (Core)
- UIIcon
- PointerInput
- MatchSetupPanel
- CardColor
- Kind
- BotSimulationTests.PlayGame
- GameManager
- SafeAreaFitter
- 4) Backlog priorise (spec fonctionnelle)
- PanelState
- NetworkMode
- GameBootstrapper

## God Nodes (most connected - your core abstractions)
1. `GameInputController` - 93 edges
2. `OnlineSession` - 78 edges
3. `GameBootstrapper` - 76 edges
4. `HandView` - 72 edges
5. `GameManager` - 69 edges
6. `LevelUp.Core` - 66 edges
7. `GameScreenLayout` - 63 edges
8. `PlayerModel` - 57 edges
9. `UISkin` - 52 edges
10. `DiscardPileView` - 50 edges

## Surprising Connections (you probably didn't know these)
- `3.5.a HandContainer (enfant de PlayerHand)` --references--> `HandView`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `3.6 TurnBannerView (bannière temporaire "Tour de X")` --references--> `TurnStartedEvent`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/Core/GameEvents.cs
- `5. Références à assigner sur GameBootstrapper` --references--> `GameBootstrapper`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/GameBootstrapper.cs
- `Ce qui tourne vraiment` --references--> `DiscardPileView`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/UI/DiscardPileView.cs
- `Scripts UI (enfants du Canvas)` --references--> `DiscardPileView`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/DiscardPileView.cs

## Import Cycles
- None detected.

## Communities (153 total, 82 thin omitted)

### Community 1 - "LevelUp.Utils"
Cohesion: 0.07
Nodes (7): LevelUp.Core, LevelUp.Tests, LevelUp.Utils, LevelUp.EditorTools, LevelUp.UI, LevelUp.AI, LevelUp.Network

### Community 2 - "CardModel"
Cohesion: 0.13
Nodes (3): CardExtensions, LevelValidator, 15.4 Helpers STATIQUES (aucun GameObject)

### Community 4 - "UISkin.Size"
Cohesion: 0.19
Nodes (4): OnlinePanel, IsVisible, LastCode, UIInputs

### Community 5 - "HandView"
Cohesion: 0.12
Nodes (8): HandView, CardCount, CardPrefab, IsAnimatingDeal, IsAnimatingDraw, IsReorderDragging, SelectedCard, SelectedCards

### Community 6 - "manifest.json"
Cohesion: 0.17
Nodes (11): com.unity.modules.jsonserialize@1.0.0, com.unity.modules.umbra@1.0.0, com.unity.render-pipelines.universal@14.0.11, com.unity.textmeshpro@3.0.9, com.unity.visualscripting@1.9.4, com.unity.modules.jsonserialize, com.unity.modules.umbra, com.unity.modules.unityanalytics (+3 more)

### Community 7 - "com.unity.2d.common@8.1.0"
Cohesion: 0.05
Nodes (44): dependencies, depth, source, url, version, dependencies, depth, source (+36 more)

### Community 8 - "GameSession"
Cohesion: 0.17
Nodes (6): GameSession, Contracts, IsHost, IsOnline, LocalPlayerIndex, Rules

### Community 10 - "AudioManager"
Cohesion: 0.12
Nodes (4): AudioManager, Instance, Generator, Haptics

### Community 11 - "com.unity.render-pipelines.core"
Cohesion: 0.07
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 12 - "NetworkManager"
Cohesion: 0.29
Nodes (3): 9) Prochaines taches recommandees (ordre d'execution), NetworkManager, IsPassScreenActive

### Community 13 - "Guide de layout Unity — LevelUp"
Cohesion: 0.12
Nodes (15): 0. Pré-requis : Canvas Scaler, 10. Méthode pour ajuster visuellement dans Unity, 11. Test et validation, 12. Troubleshooting, 13. Ajustements mobiles (optionnel), 14. Couleurs de référence (cohérence visuelle), 1. Vue d'ensemble du layout, 2. Hiérarchie cible (+7 more)

### Community 14 - "com.unity.mathematics@1.2.6"
Cohesion: 0.08
Nodes (28): dependencies, depth, source, url, version, dependencies, depth, source (+20 more)

### Community 15 - "PlayerModel"
Cohesion: 0.08
Nodes (24): GameCommandExecutor, CommandResult, DrawnCard, Error, Message, RoundEnded, Success, WinnerOverride (+16 more)

### Community 16 - "MonoBehaviour"
Cohesion: 0.11
Nodes (6): AppLifecycle, PauseMenuController, IsPaused, PauseAllowed, UIBackStack, HasOpenPanel

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

### Community 23 - "com.unity.nuget.newtonsoft-json"
Cohesion: 0.06
Nodes (34): dependencies, depth, source, version, dependencies, depth, source, url (+26 more)

### Community 24 - "dependencies"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, url, version, dependencies, depth, source (+18 more)

### Community 25 - "HUDView"
Cohesion: 0.21
Nodes (4): ActionCardPlayedEvent, ForcedDrawEvent, PlayerSkippedEvent, HUDView

### Community 26 - "com.unity.modules.unitywebrequest@1.0.0"
Cohesion: 0.05
Nodes (49): dependencies, depth, source, version, dependencies, depth, source, version (+41 more)

### Community 27 - "packages-lock.json"
Cohesion: 0.08
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 28 - "CardFactory.B"
Cohesion: 0.19
Nodes (4): BotBrainTests, LayDownOptionsTests, LevelValidatorTests, CardFactory

### Community 30 - "OnlineSession"
Cohesion: 0.06
Nodes (11): LobbyInfo, LobbyMember, OnlineSession, CurrentLobby, DeviceToken, IsActive, IsSessionHost, JoinCode (+3 more)

### Community 31 - "BotBrain"
Cohesion: 0.15
Nodes (3): BotBrain, Difficulty, Me

### Community 32 - "com.unity.2d.tilemap"
Cohesion: 0.12
Nodes (18): dependencies, depth, dependencies, depth, source, url, version, source (+10 more)

### Community 34 - "com.unity.ugui@1.0.0"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, url, version, depth, source, version (+9 more)

### Community 36 - "AIPlayer"
Cohesion: 0.11
Nodes (12): IA (`Scripts/AI`), AIDifficulty, Easy, Hard, Normal, AIPlayer, PlayerIndex, AddToMeldCommand (+4 more)

### Community 38 - "GameManager.ExecuteCommand"
Cohesion: 0.19
Nodes (9): DrawFromDeckCommand, PlayerIndex, DrawFromDiscardCommand, DiscardPileIndex, PlayerIndex, SkipPhaseCommand, PhaseToSkip, PlayerIndex (+1 more)

### Community 39 - "DeckManager"
Cohesion: 0.08
Nodes (13): ActionCardHandler, DeckManager, CardsPerPlayer, DiscardPileCount, DrawPileCount, GameConfig, CardMaxValue, CardMinValue (+5 more)

### Community 40 - "LevelRequirement"
Cohesion: 0.16
Nodes (6): ContractCatalog, ContractPlan, IsRandom, ContractProgress, Entry, IsMet

### Community 44 - "MatchRules"
Cohesion: 0.08
Nodes (20): 10) Carte du code (a lire AVANT d'explorer : evite de tout relire), En ligne (`Scripts/Network`), Regles (Core), Verifier sans appareil (editeur Unity FERME, sinon le batchmode echoue), MatchRules, Draw2Count, HandSize, HasTurnTimer (+12 more)

### Community 45 - "CLAUDE.md - Brief d'execution optimise pour Claude"
Cohesion: 0.22
Nodes (8): 1) Role de Claude dans ce projet, 3) Objectif produit global, 5) Contraintes de mise en oeuvre, 6) Definition of Done (DoD), 7) Format de reponse attendu de Claude, 8) Regles de priorisation quand il y a ambiguite, CLAUDE.md - Brief d'execution optimise pour Claude, graphify

### Community 46 - "com.unity.inputsystem"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.inputsystem

### Community 48 - "GameSettings"
Cohesion: 0.07
Nodes (26): GameSettings, BotDelayFactor, BotSpeed, CardBack, ColorblindMode, Difficulty, Felt, HandSize (+18 more)

### Community 49 - "UISkin"
Cohesion: 0.08
Nodes (6): UISkin, ActionBand, BodyFont, DisplayFont, DotPattern, Radial

### Community 50 - "com.unity.2d.spriteshape"
Cohesion: 0.14
Nodes (14): dependencies, depth, source, url, version, dependencies, depth, source (+6 more)

### Community 51 - "com.unity.nuget.mono-cecil@1.10.1"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, url, version, dependencies, depth, source (+5 more)

### Community 52 - "3. Configuration détaillée par GameObject"
Cohesion: 0.17
Nodes (12): 3.1 HUD (barre du haut), 3.2 DiscardPileView (défausses de tous les joueurs), 3.3 LevelProgress (colonne gauche), 3.4.a DeckArea (pile de pioche), 3.4.b DiscardArea (défausse unique au centre), 3.4.c Table / TableContainer (melds du joueur local), 3.4 PlayZone (conteneur central), 3.5.a HandContainer (enfant de PlayerHand) (+4 more)

### Community 53 - "CanvasGroup"
Cohesion: 0.10
Nodes (11): Ce qui tourne vraiment, ConnectionOverlay, IsVisible, GameOverCelebration, PlayerNames, Players, RoundsPlayed, LayDownChooser (+3 more)

### Community 60 - "GameSession.IsLocalHuman"
Cohesion: 0.48
Nodes (3): CardOrigin, CardOrigins, Provider

### Community 61 - "MainMenuController"
Cohesion: 0.11
Nodes (8): MainMenuController, IsVisible, MatchSetup, Root, ProfileDialog, IsVisible, RulesPanel, IsVisible

### Community 62 - "CommandError"
Cohesion: 0.12
Nodes (15): CommandError, ActionCardNotPickable, AlreadyLaidDown, CardDoesNotFit, CardNotInHand, ContractMismatch, EmptyDiscard, InvalidMeld (+7 more)

### Community 65 - "GameInputController"
Cohesion: 0.10
Nodes (7): GameInputController, ActionsLocked, CurrentPlayerIndex, DpiScale, DragUpThreshold, IsLocalTurn, ReorderDragThreshold

### Community 67 - "EventBus.Subscribe"
Cohesion: 0.14
Nodes (6): GameOverEvent, GameStartedEvent, RoundEndedEvent, EventBus, Muted, Subscription

### Community 68 - "CardView"
Cohesion: 0.09
Nodes (10): 2) Contexte technique du repo (a respecter), CardFlights, CardView, CardModel, IsFaceUp, IsHovered, IsSelected, RectTransform (+2 more)

### Community 101 - "AnimationController"
Cohesion: 0.12
Nodes (4): AnimationController, IsAnimating, FrameBudget, Enabled

### Community 104 - "MeldGroupView"
Cohesion: 0.10
Nodes (6): LevelRequirement, MeldGroupView, MeldType, OwnerIndex, TableView, ContentScale

### Community 105 - "DiscardPileView"
Cohesion: 0.18
Nodes (4): DiscardPileView, Compact, PileCount, PileSpacing

### Community 107 - "TextWeight"
Cohesion: 0.25
Nodes (6): TextWeight, Bold, Display, Medium, Regular, SemiBold

### Community 109 - "Meld"
Cohesion: 0.20
Nodes (8): LayDownLevelCommand, Melds, PlayerIndex, Meld, Cards, OwnerIndex, Type, GameCommandExecutorTests

### Community 113 - "IGameCommand"
Cohesion: 0.12
Nodes (10): GameCommandCodec, DiscardCommand, Card, PlayerIndex, TargetPlayerIndex, IGameCommand, PlayerIndex, StartNextRoundCommand (+2 more)

### Community 116 - "15. Mapping complet script → GameObject"
Cohesion: 0.25
Nodes (8): 15.1 Scripts à placer MANUELLEMENT dans la scène `Game.unity`, 15.3 Scripts sur PREFABS (pas dans la scène), 15.5 Hiérarchie finale complète (copier-coller mental), 15.6 Toutes les références Inspector (checklist drag & drop), 15.7 Résumé en 3 lignes, 15. Mapping complet script → GameObject, Scripts racine (GameObjects vides, pas sous Canvas), Scripts UI (enfants du Canvas)

### Community 117 - "TurnStartedEvent"
Cohesion: 0.25
Nodes (7): CardAddedToMeldEvent, HandChangedEvent, LevelCompletedEvent, LevelLaidDownEvent, RoundSummaryEvent, TurnPhaseChangedEvent, TurnStartedEvent

### Community 118 - "DailyDeal"
Cohesion: 0.31
Nodes (3): DailyDeal, Rules, TodayKey

### Community 119 - "PlayerSetup"
Cohesion: 0.21
Nodes (8): PlayerSetup, SavedGame, SaveGame, Exists, FilePath, PathOverride, OnlineResumeData, OnlineStartData

### Community 120 - "RectTransform"
Cohesion: 0.07
Nodes (14): UI / DA "Salon", AnimatedBackground, PileSlot, ContractRow, StepPill, TextMirror, PlayerPill, UIFactory (+6 more)

### Community 121 - "ConfirmDialog"
Cohesion: 0.38
Nodes (3): ConfirmDialog, UIInputBlocker, TweenRunner

### Community 124 - "Regles (Core)"
Cohesion: 0.18
Nodes (6): PlayerProfile, GamesPlayed, GamesWon, HasCustomName, Name, RoundsWon

### Community 125 - "UIIcon"
Cohesion: 0.15
Nodes (13): UIIcon, Cards, Check, Clock, Close, Copy, Edit, Lock (+5 more)

### Community 126 - "PointerInput"
Cohesion: 0.33
Nodes (4): PointerInput, LastActivityFrame, Override, PointerState

### Community 128 - "CardColor"
Cohesion: 0.05
Nodes (33): CardModel, IsAction, IsWild, CardColor, Blue, Green, Orange, Purple (+25 more)

### Community 129 - "Kind"
Cohesion: 0.25
Nodes (8): Kind, AddToMeld, Discard, DrawFromDeck, DrawFromDiscard, LayDown, SkipPhase, StartNextRound

### Community 132 - "BotSimulationTests.PlayGame"
Cohesion: 0.14
Nodes (5): CardDiscardedEvent, CardDrawnEvent, DeckChangedEvent, RoundStartedEvent, OpponentMovesView

### Community 133 - "GameManager"
Cohesion: 0.15
Nodes (11): GameManager, AutoStartNextRound, Config, DeckManager, Executor, LastRoundWinner, Players, RoundNumber (+3 more)

### Community 136 - "4) Backlog priorise (spec fonctionnelle)"
Cohesion: 0.40
Nodes (5): 4) Backlog priorise (spec fonctionnelle), A. UX & Gameplay (zero friction) - PRIORITE P0, B. Direction visuelle & juice - PRIORITE P1, C. Accessibilite & responsive - PRIORITE P1, D. Menus & retention - PRIORITE P2

### Community 137 - "PanelState"
Cohesion: 0.33
Nodes (6): PanelState, Busy, ClientLobby, Home, HostLobby, Join

### Community 140 - "NetworkMode"
Cohesion: 0.50
Nodes (3): NetworkMode, PassAndPlay, SinglePlayer

### Community 149 - "GameBootstrapper"
Cohesion: 0.11
Nodes (3): GameBootstrapper, LobbySeatView, LobbyViewModel

## Knowledge Gaps
- **257 isolated node(s):** `PileSpacing`, `Compact`, `PileCount`, `Root`, `ContentScale` (+252 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 663 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **82 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `OnlineSession` connect `OnlineSession` to `LevelUp.Utils`, `CardModel`, `GameManager`, `NetworkManager`, `MatchRules`, `IGameCommand`, `GameBootstrapper.HandleResumeSolo`, `CanvasGroup`, `PlayerSetup`, `ConfirmDialog`?**
  _High betweenness centrality (0.056) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `GameInputController` (e.g. with `2) Contexte technique du repo (a respecter)` and `9) Prochaines taches recommandees (ordre d'execution)`) actually correct?**
  _`GameInputController` has 4 INFERRED edges - model-reasoned connections that need verification._
- **What connects `PileSpacing`, `Compact`, `PileCount` to the rest of the system?**
  _257 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Vector2` be split into smaller, more focused modules?**
  _Cohesion score 0.1319073083778966 - nodes in this community are weakly interconnected._
- **Why does `GameManager` connect `GameManager` to `LevelUp.Utils`, `CardModel`, `BotSimulationTests.PlayGame`, `GameSession`, `AudioManager`, `NetworkManager`, `PlayerModel`, `GameBootstrapper`, `CardFactory.B`, `MatchRulesTests`, `OnlineSession`, `BotBrain`, `GameScreenLayout.RefreshContract`, `EventBus.Clear`, `AIPlayer`, `DevAutoScreenshot`, `GameManager.ExecuteCommand`, `DeckManager`, `ContractAndReplayTests`, `CanvasGroup`, `GameInputController`, `EventBus.Subscribe`, `CardView`, `DiscardPileView`, `GameOverEvent`, `SelectionStatusView`, `GameBootstrapper.HandleResumeSolo`, `15. Mapping complet script → GameObject`, `TurnStartedEvent`, `PlayerSetup`, `ConfirmDialog`, `IReadOnlyList`?**
  _High betweenness centrality (0.048) - this node is a cross-community bridge._
- **Should `LevelUp.Utils` be split into smaller, more focused modules?**
  _Cohesion score 0.06679960119641076 - nodes in this community are weakly interconnected._
- **Why does `HandView` connect `HandView` to `Vector2`, `LevelUp.Utils`, `CardModel`, `UISkin.Text`, `GameBootstrapper`, `3. Configuration détaillée par GameObject`, `CanvasGroup`, `GameInputController.ApplyPreferredSort`, `GameInputController.ExecuteAddToMeld`, `GameSession.IsLocalHuman`, `GameInputController`, `GameObject`, `EventBus.Subscribe`, `CardView`, `AnimationController`, `SelectionStatusView`, `GameBootstrapper.HandleResumeSolo`, `15. Mapping complet script → GameObject`, `TurnStartedEvent`, `RectTransform`, `ConfirmDialog`, `IReadOnlyList`?**
  _High betweenness centrality (0.046) - this node is a cross-community bridge._