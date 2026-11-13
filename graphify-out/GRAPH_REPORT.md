# Graph Report - LevelUp  (2026-11-13)

## Corpus Check
- 107 files · ~113,319 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 241 file(s) not represented in the graph (top: .meta 172, .asset 34, .shader 13)
- WARNING: 241 of 357 files seen (68%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 2588 nodes · 8098 edges · 140 communities (67 shown, 73 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 787 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `b16cfdf5`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- LevelUp.Utils
- CardModel
- UISkin.Text
- AnimationController
- HandView
- manifest.json
- com.unity.2d.common@8.1.0
- TutorialDirector
- IEnumerator
- AudioManager
- com.unity.render-pipelines.core
- NetworkManager
- Guide de layout Unity — LevelUp
- com.unity.mathematics@1.2.6
- PlayerModel
- with
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
- Test
- BotSimulationTests.PlayGame
- OnlineSession
- BotBrain
- com.unity.2d.tilemap
- MeldGroupView
- com.unity.ugui@1.0.0
- AIPlayer
- TurnManager
- DrawFromDeckCommand
- DeckManager
- LevelRequirement
- com.unity.modules.androidjni@1.0.0
- com.unity.modules.unitywebrequest@1.0.0
- MatchRules
- CLAUDE.md - Brief d'execution optimise pour Claude
- com.unity.inputsystem
- TurnTimer
- GameSettings
- UISkin
- com.unity.2d.spriteshape
- com.unity.nuget.mono-cecil@1.10.1
- 10) Carte du code (a lire AVANT d'explorer : evite de tout relire)
- RectTransform
- com.unity.modules.unitywebrequesttexture@1.0.0
- EventBus.Subscribe
- GameManager
- AndroidBuilder
- CardOrigins.Of
- RoundSummaryView
- CommandError
- TextMeshProUGUI
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
- FrameBudget.Ping
- LevelProgressView
- BalatroEffects
- UIIcon
- DiscardPileView
- GameManager.StartGame
- IGameCommand
- GameBootstrapper.EnsureGameSystems
- OnlinePanel
- GameManager.ExecuteCommand
- SelectionStatusView
- CoachAdvisor
- SkipStackingTests
- GameInputController.ExecuteAddToMeld
- OnlineStartData
- TurnStartedEvent
- DailyPanel
- PlayerSetup
- GameScreenLayout
- IReadOnlyList
- PlayerModel
- GameSession.GetLocalPlayer
- Expect
- CardColor
- Kind
- PanelState
- GameBootstrapper

## God Nodes (most connected - your core abstractions)
1. `GameInputController` - 92 edges
2. `GameBootstrapper` - 80 edges
3. `OnlineSession` - 78 edges
4. `GameManager` - 73 edges
5. `HandView` - 71 edges
6. `LevelUp.Core` - 70 edges
7. `GameScreenLayout` - 64 edges
8. `MatchRules` - 56 edges
9. `PlayerModel` - 55 edges
10. `UISkin` - 52 edges

## Surprising Connections (you probably didn't know these)
- `5. Références à assigner sur GameBootstrapper` --references--> `GameBootstrapper`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/GameBootstrapper.cs
- `3.6 TurnBannerView (bannière temporaire "Tour de X")` --references--> `TurnStartedEvent`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/Core/GameEvents.cs
- `3.5.a HandContainer (enfant de PlayerHand)` --references--> `HandView`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `Regles (Core)` --references--> `StartNextRoundCommand`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/Core/Commands/GameCommands.cs
- `Ce qui tourne vraiment` --references--> `GameManager`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/Core/GameManager.cs

## Import Cycles
- None detected.

## Communities (140 total, 73 thin omitted)

### Community 1 - "LevelUp.Utils"
Cohesion: 0.05
Nodes (26): LevelUp.Core, LevelUp.Tests, LevelUp.Utils, LevelUp.EditorTools, LevelUp.UI, LevelUp.AI, LevelUp.Network, LobbySeatView (+18 more)

### Community 2 - "CardModel"
Cohesion: 0.12
Nodes (3): CardExtensions, LevelRequirement, LevelValidator

### Community 4 - "AnimationController"
Cohesion: 0.14
Nodes (10): AnimationController, IsAnimating, 15.1 Scripts à placer MANUELLEMENT dans la scène `Game.unity`, 15.3 Scripts sur PREFABS (pas dans la scène), 15.5 Hiérarchie finale complète (copier-coller mental), 15.6 Toutes les références Inspector (checklist drag & drop), 15.7 Résumé en 3 lignes, 15. Mapping complet script → GameObject (+2 more)

### Community 5 - "HandView"
Cohesion: 0.11
Nodes (8): HandView, CardCount, CardPrefab, IsAnimatingDeal, IsAnimatingDraw, IsReorderDragging, SelectedCard, SelectedCards

### Community 6 - "manifest.json"
Cohesion: 0.17
Nodes (11): com.unity.modules.jsonserialize@1.0.0, com.unity.modules.umbra@1.0.0, com.unity.render-pipelines.universal@14.0.11, com.unity.textmeshpro@3.0.9, com.unity.visualscripting@1.9.4, com.unity.modules.jsonserialize, com.unity.modules.umbra, com.unity.modules.unityanalytics (+3 more)

### Community 7 - "com.unity.2d.common@8.1.0"
Cohesion: 0.05
Nodes (44): dependencies, depth, source, url, version, dependencies, depth, source (+36 more)

### Community 8 - "TutorialDirector"
Cohesion: 0.17
Nodes (4): Step, TutorialDirector, IsRunning, Rules

### Community 10 - "AudioManager"
Cohesion: 0.12
Nodes (4): AudioManager, Instance, Generator, Haptics

### Community 11 - "com.unity.render-pipelines.core"
Cohesion: 0.07
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 12 - "NetworkManager"
Cohesion: 0.17
Nodes (5): NetworkManager, IsPassScreenActive, NetworkMode, PassAndPlay, SinglePlayer

### Community 13 - "Guide de layout Unity — LevelUp"
Cohesion: 0.07
Nodes (27): 0. Pré-requis : Canvas Scaler, 10. Méthode pour ajuster visuellement dans Unity, 11. Test et validation, 12. Troubleshooting, 13. Ajustements mobiles (optionnel), 14. Couleurs de référence (cohérence visuelle), 1. Vue d'ensemble du layout, 2. Hiérarchie cible (+19 more)

### Community 14 - "com.unity.mathematics@1.2.6"
Cohesion: 0.08
Nodes (28): dependencies, depth, source, url, version, dependencies, depth, source (+20 more)

### Community 15 - "PlayerModel"
Cohesion: 0.17
Nodes (8): GameCommandExecutor, CommandResult, DrawnCard, Error, Message, RoundEnded, Success, WinnerOverride

### Community 16 - "with"
Cohesion: 0.06
Nodes (30): env, runs-on, steps, timeout-minutes, UNITY_EMAIL, UNITY_LICENSE, UNITY_PASSWORD, jobs (+22 more)

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

### Community 28 - "Test"
Cohesion: 0.07
Nodes (24): BotPersonas, BotStyle, LayDownLevelCommand, Melds, PlayerIndex, GameConfig, CardMaxValue, CardMinValue (+16 more)

### Community 30 - "OnlineSession"
Cohesion: 0.12
Nodes (9): OnlineSession, CurrentLobby, DeviceToken, IsActive, IsSessionHost, JoinCode, LastJoinCode, LobbyHumanCount (+1 more)

### Community 31 - "BotBrain"
Cohesion: 0.16
Nodes (3): BotBrain, Difficulty, Me

### Community 32 - "com.unity.2d.tilemap"
Cohesion: 0.12
Nodes (18): dependencies, depth, dependencies, depth, source, url, version, source (+10 more)

### Community 33 - "MeldGroupView"
Cohesion: 0.10
Nodes (5): MeldGroupView, MeldType, OwnerIndex, TableView, ContentScale

### Community 34 - "com.unity.ugui@1.0.0"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, url, version, depth, source, version (+9 more)

### Community 36 - "AIPlayer"
Cohesion: 0.13
Nodes (7): IA (`Scripts/AI`), AIDifficulty, Easy, Hard, Normal, AIPlayer, PlayerIndex

### Community 37 - "TurnManager"
Cohesion: 0.15
Nodes (5): TurnManager, CurrentPhase, CurrentPlayer, CurrentPlayerIndex, TurnsThisRound

### Community 38 - "DrawFromDeckCommand"
Cohesion: 0.11
Nodes (18): GameCommandCodec, AddToMeldCommand, Card, MeldIndex, MeldOwnerIndex, PlayerIndex, DiscardCommand, Card (+10 more)

### Community 39 - "DeckManager"
Cohesion: 0.20
Nodes (6): DeckManager, CardsPerPlayer, DiscardPileCount, DrawPileCount, PresetOrder, DeckChangedEvent

### Community 40 - "LevelRequirement"
Cohesion: 0.16
Nodes (6): ContractCatalog, ContractPlan, IsRandom, ContractProgress, Entry, IsMet

### Community 44 - "MatchRules"
Cohesion: 0.07
Nodes (21): MatchRules, CatchUp, Draw2Count, FinalLevelRequired, GoOutBonus, HandSize, HasTurnTimer, IsClassic (+13 more)

### Community 45 - "CLAUDE.md - Brief d'execution optimise pour Claude"
Cohesion: 0.13
Nodes (14): 1) Role de Claude dans ce projet, 3) Objectif produit global, 4) Backlog priorise (spec fonctionnelle), 5) Contraintes de mise en oeuvre, 6) Definition of Done (DoD), 7) Format de reponse attendu de Claude, 8) Regles de priorisation quand il y a ambiguite, 9) Prochaines taches recommandees (ordre d'execution) (+6 more)

### Community 46 - "com.unity.inputsystem"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.inputsystem

### Community 48 - "GameSettings"
Cohesion: 0.06
Nodes (31): GameSettings, BotDelayFactor, BotSpeed, CardBack, ColorblindMode, Difficulty, Felt, HandSize (+23 more)

### Community 49 - "UISkin"
Cohesion: 0.06
Nodes (12): UIFactory, CircleSprite, RingSprite, RoundedSprite, SmallPillSprite, SoftShadowSprite, UISkin, ActionBand (+4 more)

### Community 50 - "com.unity.2d.spriteshape"
Cohesion: 0.14
Nodes (14): dependencies, depth, source, url, version, dependencies, depth, source (+6 more)

### Community 51 - "com.unity.nuget.mono-cecil@1.10.1"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, url, version, dependencies, depth, source (+5 more)

### Community 52 - "10) Carte du code (a lire AVANT d'explorer : evite de tout relire)"
Cohesion: 0.29
Nodes (5): 10) Carte du code (a lire AVANT d'explorer : evite de tout relire), En ligne (`Scripts/Network`), Verifier sans appareil (editeur Unity FERME, sinon le batchmode echoue), LobbyInfo, LobbyMember

### Community 53 - "RectTransform"
Cohesion: 0.07
Nodes (11): AnimatedBackground, GameOverCelebration, PlayerNames, Players, RoundsPlayed, MainMenuController, IsVisible, MatchSetup (+3 more)

### Community 57 - "EventBus.Subscribe"
Cohesion: 0.23
Nodes (6): EventBus, Muted, Subscription, EventBusTests, PingEvent, 15.4 Helpers STATIQUES (aucun GameObject)

### Community 58 - "GameManager"
Cohesion: 0.10
Nodes (13): GameManager, AutoStartNextRound, CommandFilter, Config, DeckManager, Executor, LastRoundWinner, NextDeckOrder (+5 more)

### Community 60 - "CardOrigins.Of"
Cohesion: 0.43
Nodes (3): CardOrigin, CardOrigins, Provider

### Community 62 - "CommandError"
Cohesion: 0.12
Nodes (16): CommandError, ActionCardNotPickable, AlreadyLaidDown, CardDoesNotFit, CardNotInHand, ContractMismatch, EmptyDiscard, Filtered (+8 more)

### Community 63 - "TextMeshProUGUI"
Cohesion: 0.19
Nodes (6): ConnectionOverlay, IsVisible, PileSlot, ContractRow, TextMirror, PlayerPill

### Community 65 - "GameInputController"
Cohesion: 0.10
Nodes (7): GameInputController, ActionsLocked, CurrentPlayerIndex, DpiScale, DragUpThreshold, IsLocalTurn, ReorderDragThreshold

### Community 66 - "GameObject"
Cohesion: 0.04
Nodes (26): Ce qui tourne vraiment, AppLifecycle, ConfirmDialog, DailyPanel, IsVisible, LayDownChooser, IsOpen, MatchSetupPanel (+18 more)

### Community 68 - "CardView"
Cohesion: 0.11
Nodes (10): 2) Contexte technique du repo (a respecter), CardFlights, CardView, CardModel, IsFaceUp, IsHovered, IsSelected, RectTransform (+2 more)

### Community 101 - "FrameBudget.Ping"
Cohesion: 0.13
Nodes (6): FrameBudget, Enabled, PointerInput, LastActivityFrame, Override, PointerState

### Community 104 - "UIIcon"
Cohesion: 0.15
Nodes (13): UIIcon, Cards, Check, Clock, Close, Copy, Edit, Lock (+5 more)

### Community 105 - "DiscardPileView"
Cohesion: 0.18
Nodes (4): DiscardPileView, Compact, PileCount, PileSpacing

### Community 109 - "OnlinePanel"
Cohesion: 0.13
Nodes (5): UI / DA "Salon", OnlinePanel, IsVisible, LastCode, UIInputs

### Community 110 - "GameManager.ExecuteCommand"
Cohesion: 0.14
Nodes (7): DrawFromDeckCommand, PlayerIndex, SkipPhaseCommand, PhaseToSkip, PlayerIndex, ContractAndReplayTests, CoreGuardsTests

### Community 111 - "SelectionStatusView"
Cohesion: 0.17
Nodes (4): HandChangedEvent, TurnPhaseChangedEvent, TurnStartedEvent, SelectionStatusView

### Community 117 - "TurnStartedEvent"
Cohesion: 0.22
Nodes (11): ActionCardPlayedEvent, CardAddedToMeldEvent, CardDiscardedEvent, ForcedDrawEvent, GameOverEvent, GameStartedEvent, LevelCompletedEvent, LevelLaidDownEvent (+3 more)

### Community 118 - "DailyPanel"
Cohesion: 0.23
Nodes (4): DailyDeal, Rules, Streak, TodayKey

### Community 119 - "PlayerSetup"
Cohesion: 0.25
Nodes (7): Regles (Core), PlayerSetup, SavedGame, SaveGame, Exists, FilePath, PathOverride

### Community 120 - "GameScreenLayout"
Cohesion: 0.15
Nodes (3): GameScreenLayout, Coach, Root

### Community 122 - "PlayerModel"
Cohesion: 0.12
Nodes (13): PlayerModel, CurrentLevel, Hand, HasLaidDownThisRound, Index, IsAI, IsHandEmpty, IsSkipped (+5 more)

### Community 124 - "GameSession.GetLocalPlayer"
Cohesion: 0.08
Nodes (12): GameSession, Contracts, IsHost, IsOnline, LocalPlayerIndex, Rules, PlayerProfile, GamesPlayed (+4 more)

### Community 125 - "Expect"
Cohesion: 0.29
Nodes (7): Expect, AddToMeld, Discard, Done, DrawDeck, DrawDiscard, LayDown

### Community 128 - "CardColor"
Cohesion: 0.07
Nodes (23): CardModel, IsAction, IsWild, CardColor, Blue, Green, Orange, Purple (+15 more)

### Community 129 - "Kind"
Cohesion: 0.25
Nodes (8): Kind, AddToMeld, Discard, DrawFromDeck, DrawFromDiscard, LayDown, SkipPhase, StartNextRound

### Community 137 - "PanelState"
Cohesion: 0.33
Nodes (6): PanelState, Busy, ClientLobby, Home, HostLobby, Join

## Knowledge Gaps
- **284 isolated node(s):** `PlayerIndex`, `PlayerIndex`, `PlayerIndex`, `DiscardPileIndex`, `PlayerIndex` (+279 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 712 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **73 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `GameManager` connect `GameManager` to `LevelUp.Utils`, `CardModel`, `UISkin.Text`, `AnimationController`, `TutorialDirector`, `IEnumerator`, `AudioManager`, `NetworkManager`, `PlayerModel`, `GameBootstrapper`, `HUDView`, `Test`, `BotSimulationTests.PlayGame`, `OnlineSession`, `MatchRulesTests`, `AIPlayer`, `TurnManager`, `DeckManager`, `MatchRules`, `TurnTimer`, `GameInputController`, `GameObject`, `GameBootstrapper.EnsureGameSystems`, `GameManager.ExecuteCommand`, `SelectionStatusView`, `PlayerSetup`, `IReadOnlyList`, `GameSession.GetLocalPlayer`?**
  _High betweenness centrality (0.065) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `GameInputController` (e.g. with `2) Contexte technique du repo (a respecter)` and `9) Prochaines taches recommandees (ordre d'execution)`) actually correct?**
  _`GameInputController` has 4 INFERRED edges - model-reasoned connections that need verification._
- **What connects `PlayerIndex`, `PlayerIndex`, `PlayerIndex` to the rest of the system?**
  _284 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Vector2` be split into smaller, more focused modules?**
  _Cohesion score 0.1319073083778966 - nodes in this community are weakly interconnected._
- **Why does `Ce qui tourne vraiment` connect `GameObject` to `AnimationController`, `HandView`, `AudioManager`, `NetworkManager`, `GameBootstrapper`, `HUDView`, `OnlineSession`, `MeldGroupView`, `AIPlayer`, `GameSettings`, `10) Carte du code (a lire AVANT d'explorer : evite de tout relire)`, `RectTransform`, `GameManager`, `RoundSummaryView`, `TextMeshProUGUI`, `GameInputController`, `CardView`, `LevelProgressView`, `BalatroEffects`, `DiscardPileView`, `GameBootstrapper.EnsureGameSystems`, `OnlinePanel`, `SelectionStatusView`, `GameScreenLayout`?**
  _High betweenness centrality (0.051) - this node is a cross-community bridge._
- **Should `LevelUp.Utils` be split into smaller, more focused modules?**
  _Cohesion score 0.05009578544061303 - nodes in this community are weakly interconnected._
- **Why does `GameInputController` connect `GameInputController` to `Vector2`, `LevelUp.Utils`, `CardModel`, `UISkin.Text`, `AnimationController`, `HandView`, `HUDView`, `MeldGroupView`, `CLAUDE.md - Brief d'execution optimise pour Claude`, `TurnTimer`, `RectTransform`, `GameInputController.ApplyPreferredSort`, `GameManager`, `GameObject`, `CardView`, `DiscardPileView`, `SelectionStatusView`, `GameInputController.ExecuteAddToMeld`, `TurnStartedEvent`, `GameSession.GetLocalPlayer`?**
  _High betweenness centrality (0.051) - this node is a cross-community bridge._