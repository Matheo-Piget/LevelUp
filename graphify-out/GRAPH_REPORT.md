# Graph Report - LevelUp  (2026-11-13)

## Corpus Check
- 94 files · ~102,474 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 230 file(s) not represented in the graph (top: .meta 161, .asset 34, .shader 13)
- WARNING: 230 of 333 files seen (69%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 2365 nodes · 7428 edges · 171 communities (70 shown, 101 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 727 edges (avg confidence: 0.87)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `cb69c229`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- LevelUp.Utils
- CardModel
- Image
- AnimationController
- manifest.json
- com.unity.2d.common@8.1.0
- GameSession
- AudioManager
- com.unity.render-pipelines.core
- TurnStartedEvent
- LevelProgressView
- com.unity.mathematics@1.2.6
- PlayerModel
- AppLifecycle
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
- Ce qui tourne vraiment
- LobbyInfo
- OnlineSession
- BotBrain
- com.unity.2d.tilemap
- IGameCommand
- com.unity.ugui@1.0.0
- DeckManager
- AIPlayer
- DevAutoScreenshot
- GameManager.StartGame
- ActionCardHandler
- LevelRequirement
- com.unity.modules.androidjni@1.0.0
- com.unity.modules.unitywebrequest@1.0.0
- MatchRules
- CLAUDE.md - Brief d'execution optimise pour Claude
- com.unity.inputsystem
- CardModel
- GameSettings
- UISkin
- com.unity.2d.spriteshape
- com.unity.nuget.mono-cecil@1.10.1
- GameManager
- com.unity.modules.unitywebrequesttexture@1.0.0
- Test
- HandView
- EventBus.Subscribe
- CanvasGroup
- AndroidBuilder
- PlayerSetup
- MainMenuController
- GameObject
- Transform
- GameInputController
- GameObject
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
- OnlinePanel
- LevelProgressView
- BalatroEffects
- MeldGroupView
- DiscardPileView
- RoundSummaryView
- Meld
- SelectionStatusView
- Regles (Core)
- GameBootstrapper
- TurnStartedEvent
- DailyDeal
- MonoBehaviour
- RectTransform
- ContractAndReplayTests
- CardType
- PlayerProfile
- UIIcon
- BotBrainTests
- MatchSetupPanel
- CardColor
- Kind
- IReadOnlyList
- PanelState

## God Nodes (most connected - your core abstractions)
1. `GameInputController` - 91 edges
2. `OnlineSession` - 80 edges
3. `GameBootstrapper` - 75 edges
4. `HandView` - 73 edges
5. `GameManager` - 72 edges
6. `LevelUp.Core` - 64 edges
7. `GameScreenLayout` - 62 edges
8. `PlayerModel` - 60 edges
9. `CardView` - 54 edges
10. `MatchRules` - 52 edges

## Surprising Connections (you probably didn't know these)
- `3.6 TurnBannerView (bannière temporaire "Tour de X")` --references--> `TurnStartedEvent`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/Core/GameEvents.cs
- `5. Références à assigner sur GameBootstrapper` --references--> `GameBootstrapper`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/GameBootstrapper.cs
- `3.5.a HandContainer (enfant de PlayerHand)` --references--> `HandView`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `2) Contexte technique du repo (a respecter)` --references--> `TurnManager`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/Core/TurnManager.cs
- `2) Contexte technique du repo (a respecter)` --references--> `CardView`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/UI/CardView.cs

## Import Cycles
- None detected.

## Communities (171 total, 101 thin omitted)

### Community 1 - "LevelUp.Utils"
Cohesion: 0.06
Nodes (21): LevelUp.Core, LevelUp.Tests, LevelUp.Utils, LevelUp.EditorTools, LevelUp.UI, LevelUp.AI, LevelUp.Network, MeldRequirement (+13 more)

### Community 2 - "CardModel"
Cohesion: 0.13
Nodes (6): CardExtensions, CardModel, IsAction, IsWild, LevelDefinition, LevelRequirement

### Community 5 - "AnimationController"
Cohesion: 0.15
Nodes (7): AnimationController, IsAnimating, 15.3 Scripts sur PREFABS (pas dans la scène), 15.5 Hiérarchie finale complète (copier-coller mental), 15.6 Toutes les références Inspector (checklist drag & drop), 15.7 Résumé en 3 lignes, 15. Mapping complet script → GameObject

### Community 6 - "manifest.json"
Cohesion: 0.17
Nodes (11): com.unity.modules.jsonserialize@1.0.0, com.unity.modules.umbra@1.0.0, com.unity.render-pipelines.universal@14.0.11, com.unity.textmeshpro@3.0.9, com.unity.visualscripting@1.9.4, com.unity.modules.jsonserialize, com.unity.modules.umbra, com.unity.modules.unityanalytics (+3 more)

### Community 7 - "com.unity.2d.common@8.1.0"
Cohesion: 0.05
Nodes (44): dependencies, depth, source, url, version, dependencies, depth, source (+36 more)

### Community 8 - "GameSession"
Cohesion: 0.25
Nodes (6): GameSession, Contracts, IsHost, IsOnline, LocalPlayerIndex, Rules

### Community 10 - "AudioManager"
Cohesion: 0.12
Nodes (4): AudioManager, Instance, Generator, Haptics

### Community 11 - "com.unity.render-pipelines.core"
Cohesion: 0.07
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 12 - "TurnStartedEvent"
Cohesion: 0.20
Nodes (5): NetworkManager, IsPassScreenActive, 15.1 Scripts à placer MANUELLEMENT dans la scène `Game.unity`, Scripts racine (GameObjects vides, pas sous Canvas), Scripts UI (enfants du Canvas)

### Community 13 - "LevelProgressView"
Cohesion: 0.07
Nodes (27): 0. Pré-requis : Canvas Scaler, 10. Méthode pour ajuster visuellement dans Unity, 11. Test et validation, 12. Troubleshooting, 13. Ajustements mobiles (optionnel), 14. Couleurs de référence (cohérence visuelle), 1. Vue d'ensemble du layout, 2. Hiérarchie cible (+19 more)

### Community 14 - "com.unity.mathematics@1.2.6"
Cohesion: 0.08
Nodes (28): dependencies, depth, source, url, version, dependencies, depth, source (+20 more)

### Community 15 - "PlayerModel"
Cohesion: 0.07
Nodes (28): GameCommandExecutor, CommandResult, DrawnCard, Message, RoundEnded, Success, WinnerOverride, PlayerModel (+20 more)

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

### Community 26 - "com.unity.modules.unitywebrequest@1.0.0"
Cohesion: 0.05
Nodes (49): dependencies, depth, source, version, dependencies, depth, source, version (+41 more)

### Community 27 - "packages-lock.json"
Cohesion: 0.08
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 28 - "Ce qui tourne vraiment"
Cohesion: 0.14
Nodes (7): Ce qui tourne vraiment, ConnectionOverlay, IsVisible, TargetChooser, IsOpen, UIBackStack, HasOpenPanel

### Community 29 - "LobbyInfo"
Cohesion: 0.27
Nodes (5): En ligne (`Scripts/Network`), LobbyInfo, LobbyMember, LobbySeatView, LobbyViewModel

### Community 30 - "OnlineSession"
Cohesion: 0.08
Nodes (9): OnlineSession, CurrentLobby, DeviceToken, IsActive, IsSessionHost, JoinCode, LastJoinCode, LobbyHumanCount (+1 more)

### Community 31 - "BotBrain"
Cohesion: 0.13
Nodes (3): BotBrain, Difficulty, Me

### Community 32 - "com.unity.2d.tilemap"
Cohesion: 0.12
Nodes (18): dependencies, depth, dependencies, depth, source, url, version, source (+10 more)

### Community 33 - "IGameCommand"
Cohesion: 0.19
Nodes (4): GameCommandCodec, IGameCommand, PlayerIndex, OnlineResumeData

### Community 34 - "com.unity.ugui@1.0.0"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, url, version, depth, source, version (+9 more)

### Community 35 - "DeckManager"
Cohesion: 0.08
Nodes (6): DeckManager, CardsPerPlayer, DiscardPileCount, DrawPileCount, BotSimulationTests, MatchRulesTests

### Community 36 - "AIPlayer"
Cohesion: 0.12
Nodes (10): IA (`Scripts/AI`), AIDifficulty, Easy, Hard, Normal, AIPlayer, PlayerIndex, DrawFromDiscardCommand (+2 more)

### Community 38 - "GameManager.StartGame"
Cohesion: 0.20
Nodes (6): DrawFromDeckCommand, PlayerIndex, SkipPhaseCommand, PhaseToSkip, PlayerIndex, CoreGuardsTests

### Community 39 - "ActionCardHandler"
Cohesion: 0.12
Nodes (10): ActionCardHandler, GameConfig, CardMaxValue, CardMinValue, CardsPerPlayer, DeckSize, LevelDefinitions, MaxPlayers (+2 more)

### Community 40 - "LevelRequirement"
Cohesion: 0.17
Nodes (6): ContractCatalog, ContractPlan, IsRandom, ContractProgress, Entry, IsMet

### Community 44 - "MatchRules"
Cohesion: 0.09
Nodes (16): MatchRules, Draw2Count, HandSize, HasTurnTimer, IsClassic, JokerCount, LevelsToWin, RandomContracts (+8 more)

### Community 45 - "CLAUDE.md - Brief d'execution optimise pour Claude"
Cohesion: 0.09
Nodes (18): 1) Role de Claude dans ce projet, 2) Contexte technique du repo (a respecter), 3) Objectif produit global, 4) Backlog priorise (spec fonctionnelle), 5) Contraintes de mise en oeuvre, 6) Definition of Done (DoD), 7) Format de reponse attendu de Claude, 8) Regles de priorisation quand il y a ambiguite (+10 more)

### Community 46 - "com.unity.inputsystem"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.inputsystem

### Community 48 - "GameSettings"
Cohesion: 0.07
Nodes (26): GameSettings, BotDelayFactor, BotSpeed, CardBack, ColorblindMode, Difficulty, Felt, HandSize (+18 more)

### Community 49 - "UISkin"
Cohesion: 0.08
Nodes (7): UISkin, ActionBand, BodyFont, DisplayFont, DotPattern, Radial, Constants

### Community 50 - "com.unity.2d.spriteshape"
Cohesion: 0.14
Nodes (14): dependencies, depth, source, url, version, dependencies, depth, source (+6 more)

### Community 51 - "com.unity.nuget.mono-cecil@1.10.1"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, url, version, dependencies, depth, source (+5 more)

### Community 53 - "GameManager"
Cohesion: 0.15
Nodes (9): GameManager, AutoStartNextRound, Config, Executor, LastRoundWinner, Players, RoundNumber, Rules (+1 more)

### Community 55 - "Test"
Cohesion: 0.13
Nodes (5): CardExtensionsTests, LayDownOptionsTests, LevelValidatorTests, MeldTests, CardFactory

### Community 56 - "HandView"
Cohesion: 0.11
Nodes (8): HandView, CardCount, CardPrefab, IsAnimatingDeal, IsAnimatingDraw, IsReorderDragging, SelectedCard, SelectedCards

### Community 57 - "EventBus.Subscribe"
Cohesion: 0.27
Nodes (4): DeckManager, TurnManager, EventBusTests, PingEvent

### Community 58 - "CanvasGroup"
Cohesion: 0.14
Nodes (6): GameOverCelebration, PlayerNames, Players, RoundsPlayed, LayDownChooser, IsOpen

### Community 60 - "PlayerSetup"
Cohesion: 0.17
Nodes (7): PlayerSetup, SavedGame, SaveGame, Exists, FilePath, PathOverride, OnlineStartData

### Community 61 - "MainMenuController"
Cohesion: 0.09
Nodes (13): MainMenuController, IsVisible, MatchSetup, Root, ProfileDialog, IsVisible, UIInputs, TextWeight (+5 more)

### Community 62 - "GameObject"
Cohesion: 0.10
Nodes (7): PileSlot, UIFactory, CircleSprite, RingSprite, RoundedSprite, SmallPillSprite, SoftShadowSprite

### Community 63 - "Transform"
Cohesion: 0.13
Nodes (7): OptionsPanel, PauseMenuController, IsPaused, PauseAllowed, RulesPanel, IsVisible, UIControls

### Community 65 - "GameInputController"
Cohesion: 0.11
Nodes (7): GameInputController, ActionsLocked, CurrentPlayerIndex, DpiScale, DragUpThreshold, IsLocalTurn, ReorderDragThreshold

### Community 66 - "GameObject"
Cohesion: 0.15
Nodes (4): TweenRunner, UITween, Runner, 15.4 Helpers STATIQUES (aucun GameObject)

### Community 68 - "CardView"
Cohesion: 0.11
Nodes (7): CardFlights, CardView, CardModel, IsFaceUp, IsHovered, IsSelected, RectTransform

### Community 101 - "OnlinePanel"
Cohesion: 0.14
Nodes (3): OnlinePanel, IsVisible, LastCode

### Community 102 - "LevelProgressView"
Cohesion: 0.12
Nodes (3): LevelProgressView, OpponentMovesView, TurnTimer

### Community 104 - "MeldGroupView"
Cohesion: 0.12
Nodes (7): CardOrigin, CardOrigins, Provider, MeldGroupView, MeldType, OwnerIndex, TableView

### Community 105 - "DiscardPileView"
Cohesion: 0.18
Nodes (3): DiscardPileView, PileCount, PileSpacing

### Community 106 - "RoundSummaryView"
Cohesion: 0.14
Nodes (5): GameOverEvent, GameStartedEvent, RoundSummaryEvent, RoundSummaryView, IsVisible

### Community 109 - "Meld"
Cohesion: 0.19
Nodes (9): LayDownLevelCommand, Melds, PlayerIndex, DeckExhaustedEvent, Meld, Cards, OwnerIndex, Type (+1 more)

### Community 113 - "Regles (Core)"
Cohesion: 0.13
Nodes (11): 10) Carte du code (a lire AVANT d'explorer : evite de tout relire), Regles (Core), Verifier sans appareil (editeur Unity FERME, sinon le batchmode echoue), AddToMeldCommand, Card, MeldIndex, MeldOwnerIndex, PlayerIndex (+3 more)

### Community 117 - "TurnStartedEvent"
Cohesion: 0.12
Nodes (16): ActionCardPlayedEvent, AIThinkingEvent, CardAddedToMeldEvent, CardDiscardedEvent, CardDrawnEvent, DeckChangedEvent, ForcedDrawEvent, HandChangedEvent (+8 more)

### Community 118 - "DailyDeal"
Cohesion: 0.31
Nodes (3): DailyDeal, Rules, TodayKey

### Community 119 - "MonoBehaviour"
Cohesion: 0.17
Nodes (4): ConfirmDialog, TextMirror, SafeAreaFitter, UIInputBlocker

### Community 120 - "RectTransform"
Cohesion: 0.09
Nodes (6): UI / DA "Salon", AnimatedBackground, ContractRow, GameScreenLayout, Root, StepPill

### Community 122 - "CardType"
Cohesion: 0.29
Nodes (6): CardType, Draw2, Normal, Skip, Wild, WildDraw2

### Community 124 - "PlayerProfile"
Cohesion: 0.18
Nodes (6): PlayerProfile, GamesPlayed, GamesWon, HasCustomName, Name, RoundsWon

### Community 125 - "UIIcon"
Cohesion: 0.15
Nodes (13): UIIcon, Cards, Check, Clock, Close, Copy, Edit, Lock (+5 more)

### Community 128 - "CardColor"
Cohesion: 0.25
Nodes (8): CardColor, Blue, Green, Orange, Purple, Red, Wild, Yellow

### Community 129 - "Kind"
Cohesion: 0.25
Nodes (8): Kind, AddToMeld, Discard, DrawFromDeck, DrawFromDiscard, LayDown, SkipPhase, StartNextRound

### Community 133 - "IReadOnlyList"
Cohesion: 0.33
Nodes (5): DiscardCommand, Card, PlayerIndex, TargetPlayerIndex, TurnAutoPlay

### Community 137 - "PanelState"
Cohesion: 0.33
Nodes (6): PanelState, Busy, ClientLobby, Home, HostLobby, Join

## Knowledge Gaps
- **239 isolated node(s):** `Easy`, `Normal`, `Hard`, `PlayerIndex`, `Difficulty` (+234 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 667 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **101 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `OnlineSession` connect `OnlineSession` to `LevelUp.Utils`, `CardModel`, `GameInputController`, `IGameCommand`, `OnlinePanel`, `MatchRules`, `CLAUDE.md - Brief d'execution optimise pour Claude`, `PlayerSetup`, `GameBootstrapper`, `OnlineSession.HostAsync`, `GameManager`, `MonoBehaviour`, `Ce qui tourne vraiment`, `LobbyInfo`?**
  _High betweenness centrality (0.051) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `GameInputController` (e.g. with `2) Contexte technique du repo (a respecter)` and `9) Prochaines taches recommandees (ordre d'execution)`) actually correct?**
  _`GameInputController` has 4 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Easy`, `Normal`, `Hard` to the rest of the system?**
  _239 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Vector2` be split into smaller, more focused modules?**
  _Cohesion score 0.1471264367816092 - nodes in this community are weakly interconnected._
- **Why does `HandView` connect `HandView` to `Vector2`, `LevelUp.Utils`, `CardModel`, `UISkin.Text`, `Image`, `IReadOnlyList`, `AnimationController`, `GameInputController.ExecuteLayDown`, `TurnStartedEvent`, `LevelProgressView`, `Ce qui tourne vraiment`, `CLAUDE.md - Brief d'execution optimise pour Claude`, `CardModel`, `GameObject`, `GameInputController`, `CardView`, `MeldGroupView`, `GameSession.GetLocalPlayer`, `SelectionStatusView`, `GameBootstrapper`, `TurnStartedEvent`, `MonoBehaviour`, `RectTransform`?**
  _High betweenness centrality (0.043) - this node is a cross-community bridge._
- **Should `LevelUp.Utils` be split into smaller, more focused modules?**
  _Cohesion score 0.057455708661417325 - nodes in this community are weakly interconnected._
- **Why does `GameInputController` connect `GameInputController` to `Vector2`, `LevelUp.Utils`, `CardModel`, `Image`, `AnimationController`, `GameInputController.ExecuteLayDown`, `TurnStartedEvent`, `HUDView`, `Ce qui tourne vraiment`, `GameManager.StartGame`, `CLAUDE.md - Brief d'execution optimise pour Claude`, `GameManager`, `HandView`, `CanvasGroup`, `Transform`, `CardView`, `LevelProgressView`, `MeldGroupView`, `DiscardPileView`, `GameSession.GetLocalPlayer`, `Meld`, `SelectionStatusView`, `Regles (Core)`, `GameBootstrapper`, `TurnStartedEvent`, `MonoBehaviour`, `RectTransform`?**
  _High betweenness centrality (0.042) - this node is a cross-community bridge._