# Graph Report - LevelUp  (2026-11-13)

## Corpus Check
- 106 files · ~111,221 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 240 file(s) not represented in the graph (top: .meta 171, .asset 34, .shader 13)
- WARNING: 240 of 355 files seen (68%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 2546 nodes · 7961 edges · 139 communities (63 shown, 76 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 777 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `33ef9cd6`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- LevelUp.Utils
- CardModel
- UISkin.Text
- Image
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
- with
- dependencies
- com.unity.modules.physics@1.0.0
- com.unity.modules.jsonserialize@1.0.0
- com.unity.ext.nunit
- com.unity.modules.imgui@1.0.0
- RoundStartedEvent
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
- DevAutoScreenshot
- DrawFromDeckCommand
- DeckManager
- LevelRequirement
- com.unity.modules.androidjni@1.0.0
- com.unity.modules.unitywebrequest@1.0.0
- MatchRules
- CLAUDE.md - Brief d'execution optimise pour Claude
- com.unity.inputsystem
- MainMenuController
- GameSettings
- UISkin
- com.unity.2d.spriteshape
- com.unity.nuget.mono-cecil@1.10.1
- PlayerProfile.Sanitize
- RectTransform
- com.unity.modules.unitywebrequesttexture@1.0.0
- EventBus.Subscribe
- GameManager
- AndroidBuilder
- CardOrigins.Of
- LevelValidator
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
- PointerInput
- LevelProgressView
- BalatroEffects
- TableView
- DiscardPileView
- GameManager.StartGame
- SpecialCardsMode
- GameBootstrapper.EnsureGameSystems
- ContractAndReplayTests
- SelectionStatusView
- FrameBudget.Ping
- CoreGuardsTests
- ContractProgress.Evaluate
- TurnStartedEvent
- DailyPanel
- PlayerSetup
- GameScreenLayout
- IReadOnlyList
- PlayerModel
- Regles (Core)
- CardColor
- Kind
- PanelState
- GameBootstrapper

## God Nodes (most connected - your core abstractions)
1. `GameInputController` - 92 edges
2. `GameBootstrapper` - 78 edges
3. `OnlineSession` - 78 edges
4. `HandView` - 71 edges
5. `LevelUp.Core` - 69 edges
6. `GameScreenLayout` - 66 edges
7. `GameManager` - 64 edges
8. `MatchRules` - 64 edges
9. `PlayerModel` - 58 edges
10. `UISkin` - 52 edges

## Surprising Connections (you probably didn't know these)
- `5. Références à assigner sur GameBootstrapper` --references--> `GameBootstrapper`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/GameBootstrapper.cs
- `3.6 TurnBannerView (bannière temporaire "Tour de X")` --references--> `TurnStartedEvent`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/Core/GameEvents.cs
- `3.5.a HandContainer (enfant de PlayerHand)` --references--> `HandView`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `Ce qui tourne vraiment` --references--> `AIPlayer`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/AI/AIPlayer.cs
- `15.2 Scripts AUTO-GÉNÉRÉS au runtime (ne pas les ajouter à la main)` --references--> `AIPlayer`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/AI/AIPlayer.cs

## Import Cycles
- None detected.

## Communities (139 total, 76 thin omitted)

### Community 1 - "LevelUp.Utils"
Cohesion: 0.05
Nodes (23): LevelUp.Core, LevelUp.Tests, LevelUp.Utils, LevelUp.EditorTools, LevelUp.UI, LevelUp.AI, LevelUp.Network, TextWeight (+15 more)

### Community 3 - "UISkin.Text"
Cohesion: 0.12
Nodes (7): LobbySeatView, LobbyViewModel, OnlinePanel, IsVisible, LastCode, UIControls, UIInputs

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
Cohesion: 0.15
Nodes (6): GameSession, Contracts, IsHost, IsOnline, LocalPlayerIndex, Rules

### Community 10 - "AudioManager"
Cohesion: 0.12
Nodes (4): AudioManager, Instance, Generator, Haptics

### Community 11 - "com.unity.render-pipelines.core"
Cohesion: 0.07
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 12 - "NetworkManager"
Cohesion: 0.14
Nodes (8): NetworkManager, IsPassScreenActive, NetworkMode, PassAndPlay, SinglePlayer, 15.1 Scripts à placer MANUELLEMENT dans la scène `Game.unity`, Scripts racine (GameObjects vides, pas sous Canvas), Scripts UI (enfants du Canvas)

### Community 13 - "Guide de layout Unity — LevelUp"
Cohesion: 0.06
Nodes (32): 0. Pré-requis : Canvas Scaler, 10. Méthode pour ajuster visuellement dans Unity, 11. Test et validation, 12. Troubleshooting, 13. Ajustements mobiles (optionnel), 14. Couleurs de référence (cohérence visuelle), 15.3 Scripts sur PREFABS (pas dans la scène), 15.5 Hiérarchie finale complète (copier-coller mental) (+24 more)

### Community 14 - "com.unity.mathematics@1.2.6"
Cohesion: 0.08
Nodes (28): dependencies, depth, source, url, version, dependencies, depth, source (+20 more)

### Community 15 - "PlayerModel"
Cohesion: 0.11
Nodes (13): GameCommandExecutor, CommandResult, DrawnCard, Error, Message, RoundEnded, Success, WinnerOverride (+5 more)

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
Cohesion: 0.06
Nodes (24): BotPersonas, BotStyle, LayDownLevelCommand, Melds, PlayerIndex, GameConfig, CardMaxValue, CardMinValue (+16 more)

### Community 30 - "OnlineSession"
Cohesion: 0.13
Nodes (9): OnlineSession, CurrentLobby, DeviceToken, IsActive, IsSessionHost, JoinCode, LastJoinCode, LobbyHumanCount (+1 more)

### Community 31 - "BotBrain"
Cohesion: 0.16
Nodes (3): BotBrain, Difficulty, Me

### Community 32 - "com.unity.2d.tilemap"
Cohesion: 0.12
Nodes (18): dependencies, depth, dependencies, depth, source, url, version, source (+10 more)

### Community 33 - "MeldGroupView"
Cohesion: 0.16
Nodes (3): MeldGroupView, MeldType, OwnerIndex

### Community 34 - "com.unity.ugui@1.0.0"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, url, version, depth, source, version (+9 more)

### Community 36 - "AIPlayer"
Cohesion: 0.13
Nodes (11): 10) Carte du code (a lire AVANT d'explorer : evite de tout relire), IA (`Scripts/AI`), Regles (Core), Verifier sans appareil (editeur Unity FERME, sinon le batchmode echoue), AIDifficulty, Easy, Hard, Normal (+3 more)

### Community 38 - "DrawFromDeckCommand"
Cohesion: 0.11
Nodes (22): AddToMeldCommand, Card, MeldIndex, MeldOwnerIndex, PlayerIndex, DiscardCommand, Card, PlayerIndex (+14 more)

### Community 39 - "DeckManager"
Cohesion: 0.14
Nodes (7): ActionCardHandler, DeckManager, CardsPerPlayer, DiscardPileCount, DrawPileCount, DeckChangedEvent, SkipStackingTests

### Community 40 - "LevelRequirement"
Cohesion: 0.20
Nodes (3): ContractCatalog, ContractPlan, IsRandom

### Community 44 - "MatchRules"
Cohesion: 0.09
Nodes (16): MatchRules, CatchUp, Draw2Count, FinalLevelRequired, GoOutBonus, HandSize, HasTurnTimer, IsClassic (+8 more)

### Community 45 - "CLAUDE.md - Brief d'execution optimise pour Claude"
Cohesion: 0.13
Nodes (14): 1) Role de Claude dans ce projet, 3) Objectif produit global, 4) Backlog priorise (spec fonctionnelle), 5) Contraintes de mise en oeuvre, 6) Definition of Done (DoD), 7) Format de reponse attendu de Claude, 8) Regles de priorisation quand il y a ambiguite, 9) Prochaines taches recommandees (ordre d'execution) (+6 more)

### Community 46 - "com.unity.inputsystem"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.inputsystem

### Community 47 - "MainMenuController"
Cohesion: 0.14
Nodes (6): MainMenuController, IsVisible, MatchSetup, Root, RulesPanel, IsVisible

### Community 48 - "GameSettings"
Cohesion: 0.06
Nodes (31): GameSettings, BotDelayFactor, BotSpeed, CardBack, ColorblindMode, Difficulty, Felt, HandSize (+23 more)

### Community 49 - "UISkin"
Cohesion: 0.06
Nodes (25): UIFactory, CircleSprite, RingSprite, RoundedSprite, SmallPillSprite, SoftShadowSprite, UIIcon, Cards (+17 more)

### Community 50 - "com.unity.2d.spriteshape"
Cohesion: 0.14
Nodes (14): dependencies, depth, source, url, version, dependencies, depth, source (+6 more)

### Community 51 - "com.unity.nuget.mono-cecil@1.10.1"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, url, version, dependencies, depth, source (+5 more)

### Community 52 - "PlayerProfile.Sanitize"
Cohesion: 0.15
Nodes (3): En ligne (`Scripts/Network`), LobbyInfo, LobbyMember

### Community 53 - "RectTransform"
Cohesion: 0.07
Nodes (8): AnimatedBackground, AnimationController, IsAnimating, GameOverCelebration, PlayerNames, Players, RoundsPlayed, SafeAreaFitter

### Community 57 - "EventBus.Subscribe"
Cohesion: 0.23
Nodes (6): 2) Contexte technique du repo (a respecter), EventBus, Muted, Subscription, EventBusTests, PingEvent

### Community 58 - "GameManager"
Cohesion: 0.15
Nodes (11): GameManager, AutoStartNextRound, Config, DeckManager, Executor, LastRoundWinner, Players, RoundNumber (+3 more)

### Community 60 - "CardOrigins.Of"
Cohesion: 0.36
Nodes (3): CardOrigin, CardOrigins, Provider

### Community 62 - "CommandError"
Cohesion: 0.14
Nodes (14): CommandError, ActionCardNotPickable, AlreadyLaidDown, CardDoesNotFit, CardNotInHand, ContractMismatch, EmptyDiscard, InvalidMeld (+6 more)

### Community 63 - "TextMeshProUGUI"
Cohesion: 0.14
Nodes (4): ConnectionOverlay, IsVisible, PileSlot, TextMirror

### Community 65 - "GameInputController"
Cohesion: 0.10
Nodes (7): GameInputController, ActionsLocked, CurrentPlayerIndex, DpiScale, DragUpThreshold, IsLocalTurn, ReorderDragThreshold

### Community 66 - "GameObject"
Cohesion: 0.05
Nodes (22): Ce qui tourne vraiment, AppLifecycle, ConfirmDialog, LayDownChooser, IsOpen, MatchSetupPanel, IsVisible, OptionsPanel (+14 more)

### Community 68 - "CardView"
Cohesion: 0.08
Nodes (10): CardFlights, CardView, CardModel, IsFaceUp, IsHovered, IsSelected, RectTransform, CardViewPool (+2 more)

### Community 101 - "PointerInput"
Cohesion: 0.39
Nodes (4): PointerInput, LastActivityFrame, Override, PointerState

### Community 105 - "DiscardPileView"
Cohesion: 0.17
Nodes (4): DiscardPileView, Compact, PileCount, PileSpacing

### Community 107 - "SpecialCardsMode"
Cohesion: 0.24
Nodes (5): SpecialCardsMode, All, Jokers, None, MatchRulesText

### Community 116 - "ContractProgress.Evaluate"
Cohesion: 0.47
Nodes (3): ContractProgress, Entry, IsMet

### Community 117 - "TurnStartedEvent"
Cohesion: 0.16
Nodes (15): ActionCardPlayedEvent, CardAddedToMeldEvent, CardDiscardedEvent, ForcedDrawEvent, GameOverEvent, GameStartedEvent, HandChangedEvent, InvalidActionEvent (+7 more)

### Community 118 - "DailyPanel"
Cohesion: 0.14
Nodes (6): DailyDeal, Rules, Streak, TodayKey, DailyPanel, IsVisible

### Community 119 - "PlayerSetup"
Cohesion: 0.12
Nodes (9): GameCommandCodec, PlayerSetup, SavedGame, SaveGame, Exists, FilePath, PathOverride, OnlineResumeData (+1 more)

### Community 120 - "GameScreenLayout"
Cohesion: 0.13
Nodes (5): UI / DA "Salon", ContractRow, GameScreenLayout, Coach, Root

### Community 122 - "PlayerModel"
Cohesion: 0.09
Nodes (15): PlayerModel, CurrentLevel, Hand, HasLaidDownThisRound, Index, IsAI, IsHandEmpty, IsSkipped (+7 more)

### Community 124 - "Regles (Core)"
Cohesion: 0.18
Nodes (6): PlayerProfile, GamesPlayed, GamesWon, HasCustomName, Name, RoundsWon

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
- **271 isolated node(s):** `Easy`, `Normal`, `Hard`, `PlayerIndex`, `Difficulty` (+266 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 697 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **76 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `OnlineSession` connect `OnlineSession` to `LevelUp.Utils`, `GameObject`, `CardModel`, `FastBufferReader`, `MatchRules`, `CLAUDE.md - Brief d'execution optimise pour Claude`, `GameBootstrapper.HandleOnlineCreate`, `GameBootstrapper.HandleResumeSolo`, `PlayerProfile.Sanitize`, `PlayerSetup`, `OnlineSession.HostAsync`, `GameManager`, `LevelValidator`?**
  _High betweenness centrality (0.063) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `GameInputController` (e.g. with `2) Contexte technique du repo (a respecter)` and `9) Prochaines taches recommandees (ordre d'execution)`) actually correct?**
  _`GameInputController` has 4 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Easy`, `Normal`, `Hard` to the rest of the system?**
  _271 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Vector2` be split into smaller, more focused modules?**
  _Cohesion score 0.14516129032258066 - nodes in this community are weakly interconnected._
- **Why does `HandView` connect `HandView` to `Vector2`, `LevelUp.Utils`, `CardModel`, `UISkin.Text`, `Image`, `IEnumerator`, `NetworkManager`, `Guide de layout Unity — LevelUp`, `RectTransform`, `GameInputController.ApplyPreferredSort`, `EventBus.Subscribe`, `CardOrigins.Of`, `TextMeshProUGUI`, `GameInputController`, `GameObject`, `CardView`, `SelectionStatusView`, `GameBootstrapper.HandleResumeSolo`, `TurnStartedEvent`, `IReadOnlyList`?**
  _High betweenness centrality (0.047) - this node is a cross-community bridge._
- **Should `LevelUp.Utils` be split into smaller, more focused modules?**
  _Cohesion score 0.05167173252279635 - nodes in this community are weakly interconnected._
- **Why does `Ce qui tourne vraiment` connect `GameObject` to `UISkin.Text`, `HandView`, `AudioManager`, `NetworkManager`, `GameBootstrapper`, `HUDView`, `OnlineSession`, `AIPlayer`, `MainMenuController`, `GameSettings`, `RectTransform`, `GameManager`, `TextMeshProUGUI`, `GameInputController`, `CardView`, `LevelProgressView`, `BalatroEffects`, `TableView`, `DiscardPileView`, `GameBootstrapper.EnsureGameSystems`, `SelectionStatusView`, `GameScreenLayout`, `PlayerModel`?**
  _High betweenness centrality (0.044) - this node is a cross-community bridge._