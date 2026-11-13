# Graph Report - LevelUp  (2026-11-13)

## Corpus Check
- 113 files · ~117,628 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 247 file(s) not represented in the graph (top: .meta 178, .asset 34, .shader 13)
- WARNING: 247 of 369 files seen (67%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 2669 nodes · 8393 edges · 151 communities (74 shown, 77 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 829 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `69e2a093`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- LevelUp.Utils
- CardModel
- Image
- TextMeshProUGUI
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
- GameCommandExecutor.Execute
- with
- dependencies
- com.unity.modules.physics@1.0.0
- com.unity.modules.jsonserialize@1.0.0
- com.unity.ext.nunit
- com.unity.modules.imgui@1.0.0
- IGameCommand
- com.unity.nuget.newtonsoft-json
- dependencies
- HUDView
- com.unity.modules.unitywebrequest@1.0.0
- packages-lock.json
- Test
- GameBootstrapper
- OnlineSession
- BotBrain
- com.unity.2d.tilemap
- MeldGroupView
- com.unity.ugui@1.0.0
- AIPlayer
- CoachAdvisor
- EventBus.Publish
- LevelRequirement
- com.unity.modules.androidjni@1.0.0
- com.unity.modules.unitywebrequest@1.0.0
- MatchRules
- CLAUDE.md - Brief d'execution optimise pour Claude
- com.unity.inputsystem
- PassAndPlayController
- GameSettings
- UISkin
- com.unity.2d.spriteshape
- com.unity.nuget.mono-cecil@1.10.1
- GameObject
- TurnManager
- com.unity.modules.unitywebrequesttexture@1.0.0
- MainMenuController
- EventBus.Subscribe
- GameManager
- GameInputController
- CardOrigins.Of
- RoundSummaryView
- CommandError
- RectTransform
- MatchRulesTests
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
- UIIcon
- DiscardPileView
- UIInputs.TextField
- SocialProtocol
- TurnTimer
- OnlinePanel
- DrawFromDeckCommand
- SelectionStatusView
- BotSimulationTests.PlayGame
- DevAutoScreenshot.Start
- MatchRules
- TurnStartedEvent
- DailyPanel
- PlayerSetup
- RectTransform
- IReadOnlyList
- PlayerModel
- GameSession.GetLocalPlayer
- PauseMenuController
- RulesPanel
- CardColor
- Kind
- RematchVote
- GameOverCelebration
- MonoBehaviour
- Haptics
- UISkin.Text
- EventBus
- PanelState
- GameCommandExecutor
- GameSession.GetLocalPlayer
- SpecialCardsMode
- DailyPanel

## God Nodes (most connected - your core abstractions)
1. `OnlineSession` - 93 edges
2. `GameInputController` - 92 edges
3. `GameBootstrapper` - 90 edges
4. `LevelUp.Core` - 76 edges
5. `HandView` - 71 edges
6. `GameManager` - 67 edges
7. `GameScreenLayout` - 65 edges
8. `LevelUp.UI` - 53 edges
9. `UISkin` - 52 edges
10. `PlayerModel` - 51 edges

## Surprising Connections (you probably didn't know these)
- `3.6 TurnBannerView (bannière temporaire "Tour de X")` --references--> `TurnStartedEvent`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/Core/GameEvents.cs
- `3.5.a HandContainer (enfant de PlayerHand)` --references--> `HandView`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `Regles (Core)` --references--> `SaveGame`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/Core/SaveGame.cs
- `Ce qui tourne vraiment` --references--> `RoundSummaryView`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/UI/RoundSummaryView.cs
- `2) Contexte technique du repo (a respecter)` --references--> `GameInputController`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/UI/GameInputController.cs

## Import Cycles
- None detected.

## Communities (151 total, 77 thin omitted)

### Community 1 - "LevelUp.Utils"
Cohesion: 0.06
Nodes (8): LevelUp.Core, LevelUp.Tests, LevelUp.Utils, LevelUp.EditorTools, LevelUp.UI, LevelUp.AI, LevelUp.Network, UIInputBlocker

### Community 2 - "CardModel"
Cohesion: 0.13
Nodes (4): CardExtensions, LevelRequirement, LevelValidator, 15.4 Helpers STATIQUES (aucun GameObject)

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
Cohesion: 0.12
Nodes (11): Expect, AddToMeld, Discard, Done, DrawDeck, DrawDiscard, LayDown, Step (+3 more)

### Community 10 - "AudioManager"
Cohesion: 0.15
Nodes (3): AudioManager, Instance, Generator

### Community 11 - "com.unity.render-pipelines.core"
Cohesion: 0.07
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 12 - "NetworkManager"
Cohesion: 0.13
Nodes (8): NetworkManager, IsPassScreenActive, NetworkMode, PassAndPlay, SinglePlayer, 15.1 Scripts à placer MANUELLEMENT dans la scène `Game.unity`, Scripts racine (GameObjects vides, pas sous Canvas), Scripts UI (enfants du Canvas)

### Community 13 - "Guide de layout Unity — LevelUp"
Cohesion: 0.06
Nodes (32): 0. Pré-requis : Canvas Scaler, 10. Méthode pour ajuster visuellement dans Unity, 11. Test et validation, 12. Troubleshooting, 13. Ajustements mobiles (optionnel), 14. Couleurs de référence (cohérence visuelle), 15.3 Scripts sur PREFABS (pas dans la scène), 15.5 Hiérarchie finale complète (copier-coller mental) (+24 more)

### Community 14 - "com.unity.mathematics@1.2.6"
Cohesion: 0.08
Nodes (28): dependencies, depth, source, url, version, dependencies, depth, source (+20 more)

### Community 15 - "GameCommandExecutor.Execute"
Cohesion: 0.22
Nodes (7): CommandResult, DrawnCard, Error, Message, RoundEnded, Success, WinnerOverride

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

### Community 22 - "IGameCommand"
Cohesion: 0.11
Nodes (19): Regles (Core), GameCommandCodec, AddToMeldCommand, Card, MeldIndex, MeldOwnerIndex, PlayerIndex, DiscardCommand (+11 more)

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
Cohesion: 0.10
Nodes (10): CurrentLobby, DeviceToken, IsActive, IsSessionHost, JoinCode, LastJoinCode, LobbyHumanCount, VersionTag (+2 more)

### Community 31 - "BotBrain"
Cohesion: 0.16
Nodes (3): BotBrain, Difficulty, Me

### Community 32 - "com.unity.2d.tilemap"
Cohesion: 0.12
Nodes (18): dependencies, depth, dependencies, depth, source, url, version, source (+10 more)

### Community 33 - "MeldGroupView"
Cohesion: 0.13
Nodes (5): MeldGroupView, MeldType, OwnerIndex, TableView, ContentScale

### Community 34 - "com.unity.ugui@1.0.0"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, url, version, depth, source, version (+9 more)

### Community 39 - "EventBus.Publish"
Cohesion: 0.13
Nodes (8): ActionCardHandler, DeckManager, CardsPerPlayer, DiscardPileCount, DrawPileCount, PresetOrder, DeckChangedEvent, SkipStackingTests

### Community 40 - "LevelRequirement"
Cohesion: 0.15
Nodes (6): ContractCatalog, ContractPlan, IsRandom, ContractProgress, Entry, IsMet

### Community 44 - "MatchRules"
Cohesion: 0.09
Nodes (16): MatchRules, CatchUp, Draw2Count, FinalLevelRequired, GoOutBonus, HandSize, HasTurnTimer, IsClassic (+8 more)

### Community 45 - "CLAUDE.md - Brief d'execution optimise pour Claude"
Cohesion: 0.08
Nodes (22): 10) Carte du code (a lire AVANT d'explorer : evite de tout relire), 1) Role de Claude dans ce projet, 3) Objectif produit global, 4) Backlog priorise (spec fonctionnelle), 5) Contraintes de mise en oeuvre, 6) Definition of Done (DoD), 7) Format de reponse attendu de Claude, 8) Regles de priorisation quand il y a ambiguite (+14 more)

### Community 46 - "com.unity.inputsystem"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.inputsystem

### Community 47 - "PassAndPlayController"
Cohesion: 0.25
Nodes (3): PassAndPlayController, Active, HidesDeal

### Community 48 - "GameSettings"
Cohesion: 0.06
Nodes (32): GameSettings, BotDelayFactor, BotSpeed, CardBack, ColorblindMode, Difficulty, Felt, HandSize (+24 more)

### Community 49 - "UISkin"
Cohesion: 0.06
Nodes (12): UIFactory, CircleSprite, RingSprite, RoundedSprite, SmallPillSprite, SoftShadowSprite, UISkin, ActionBand (+4 more)

### Community 50 - "com.unity.2d.spriteshape"
Cohesion: 0.14
Nodes (14): dependencies, depth, source, url, version, dependencies, depth, source (+6 more)

### Community 51 - "com.unity.nuget.mono-cecil@1.10.1"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, url, version, dependencies, depth, source (+5 more)

### Community 53 - "TurnManager"
Cohesion: 0.18
Nodes (5): TurnManager, CurrentPhase, CurrentPlayer, CurrentPlayerIndex, TurnsThisRound

### Community 56 - "MainMenuController"
Cohesion: 0.11
Nodes (8): MainMenuController, IsVisible, MatchSetup, Root, MatchSetupPanel, IsVisible, ProfileDialog, IsVisible

### Community 58 - "GameManager"
Cohesion: 0.10
Nodes (15): GameStartedEvent, GameManager, AutoStartNextRound, CommandFilter, Config, DeckManager, Executor, LastRoundWinner (+7 more)

### Community 59 - "GameInputController"
Cohesion: 0.09
Nodes (7): GameInputController, ActionsLocked, CurrentPlayerIndex, DpiScale, DragUpThreshold, IsLocalTurn, ReorderDragThreshold

### Community 60 - "CardOrigins.Of"
Cohesion: 0.43
Nodes (3): CardOrigin, CardOrigins, Provider

### Community 61 - "RoundSummaryView"
Cohesion: 0.18
Nodes (3): RoundSummaryView, IsVisible, ReplayAvailable

### Community 62 - "CommandError"
Cohesion: 0.12
Nodes (16): CommandError, ActionCardNotPickable, AlreadyLaidDown, CardDoesNotFit, CardNotInHand, ContractMismatch, EmptyDiscard, Filtered (+8 more)

### Community 65 - "RectTransform"
Cohesion: 0.14
Nodes (4): AnimationController, IsAnimating, PileSlot, EmoteView

### Community 68 - "CardView"
Cohesion: 0.09
Nodes (9): CardFlights, CardView, CardModel, IsFaceUp, IsHovered, IsSelected, RectTransform, CardViewPool (+1 more)

### Community 101 - "PointerInput"
Cohesion: 0.14
Nodes (6): FrameBudget, Enabled, PointerInput, LastActivityFrame, Override, PointerState

### Community 104 - "UIIcon"
Cohesion: 0.15
Nodes (13): UIIcon, Cards, Check, Clock, Close, Copy, Edit, Lock (+5 more)

### Community 105 - "DiscardPileView"
Cohesion: 0.18
Nodes (4): DiscardPileView, Compact, PileCount, PileSpacing

### Community 106 - "UIInputs.TextField"
Cohesion: 0.16
Nodes (7): UIInputs, TextWeight, Bold, Display, Medium, Regular, SemiBold

### Community 109 - "OnlinePanel"
Cohesion: 0.11
Nodes (5): En ligne (`Scripts/Network`), LobbyViewModel, OnlinePanel, IsVisible, LastCode

### Community 110 - "DrawFromDeckCommand"
Cohesion: 0.19
Nodes (7): DrawFromDeckCommand, PlayerIndex, SkipPhaseCommand, PhaseToSkip, PlayerIndex, ContractAndReplayTests, CoreGuardsTests

### Community 115 - "DevAutoScreenshot.Start"
Cohesion: 0.14
Nodes (3): DevAutoScreenshot, TargetChooser, IsOpen

### Community 116 - "MatchRules"
Cohesion: 0.16
Nodes (4): LobbyInfo, LobbyMember, OnlineResumeData, OnlineStartData

### Community 117 - "TurnStartedEvent"
Cohesion: 0.19
Nodes (16): ActionCardPlayedEvent, CardAddedToMeldEvent, CardDiscardedEvent, CardDrawnEvent, ForcedDrawEvent, GameOverEvent, HandChangedEvent, InvalidActionEvent (+8 more)

### Community 118 - "DailyPanel"
Cohesion: 0.18
Nodes (4): DailyDeal, Rules, Streak, TodayKey

### Community 119 - "PlayerSetup"
Cohesion: 0.24
Nodes (5): SavedGame, SaveGame, Exists, FilePath, PathOverride

### Community 120 - "RectTransform"
Cohesion: 0.10
Nodes (5): UI / DA "Salon", ContractRow, GameScreenLayout, Coach, Root

### Community 122 - "PlayerModel"
Cohesion: 0.12
Nodes (13): PlayerModel, CurrentLevel, Hand, HasLaidDownThisRound, Index, IsAI, IsHandEmpty, IsSkipped (+5 more)

### Community 124 - "GameSession.GetLocalPlayer"
Cohesion: 0.17
Nodes (6): PlayerProfile, GamesPlayed, GamesWon, HasCustomName, Name, RoundsWon

### Community 125 - "PauseMenuController"
Cohesion: 0.11
Nodes (9): Ce qui tourne vraiment, AppLifecycle, OpponentMovesView, OptionsPanel, PauseMenuController, IsPaused, PauseAllowed, UIBackStack (+1 more)

### Community 127 - "RulesPanel"
Cohesion: 0.31
Nodes (4): RulesPanel, IsVisible, TutorialAvailable, TutorialRequested

### Community 128 - "CardColor"
Cohesion: 0.05
Nodes (33): CardModel, IsAction, IsWild, CardColor, Blue, Green, Orange, Purple (+25 more)

### Community 129 - "Kind"
Cohesion: 0.25
Nodes (8): Kind, AddToMeld, Discard, DrawFromDeck, DrawFromDiscard, LayDown, SkipPhase, StartNextRound

### Community 131 - "RematchVote"
Cohesion: 0.16
Nodes (5): EmoteLimiter, RematchVote, ExpectedMask, IsAgreed, VotesMask

### Community 132 - "GameOverCelebration"
Cohesion: 0.17
Nodes (7): AnimatedBackground, GameOverCelebration, PlayerNames, Players, ReplayIsVote, RoundsPlayed, 15.2 Scripts AUTO-GÉNÉRÉS au runtime (ne pas les ajouter à la main)

### Community 133 - "MonoBehaviour"
Cohesion: 0.13
Nodes (6): ConnectionOverlay, IsVisible, LayDownChooser, IsOpen, SafeAreaFitter, TweenRunner

### Community 136 - "EventBus"
Cohesion: 0.25
Nodes (4): 2) Contexte technique du repo (a respecter), EventBus, Muted, Subscription

### Community 137 - "PanelState"
Cohesion: 0.33
Nodes (6): PanelState, Busy, ClientLobby, Home, HostLobby, Join

### Community 139 - "GameSession.GetLocalPlayer"
Cohesion: 0.15
Nodes (6): GameSession, Contracts, IsHost, IsOnline, LocalPlayerIndex, Rules

### Community 145 - "SpecialCardsMode"
Cohesion: 0.21
Nodes (5): SpecialCardsMode, All, Jokers, None, MatchRulesText

### Community 146 - "DailyPanel"
Cohesion: 0.24
Nodes (3): ConfirmDialog, DailyPanel, IsVisible

## Knowledge Gaps
- **291 isolated node(s):** `PathOverride`, `FilePath`, `Exists`, `IsVisible`, `ActionsLocked` (+286 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 719 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **77 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `OnlineSession` connect `OnlineSession` to `LevelUp.Utils`, `CardModel`, `RematchVote`, `FastBufferReader`, `MonoBehaviour`, `SocialProtocol`, `OnlinePanel`, `MatchRules`, `GameBootstrapper.HandleResumeSolo`, `OnlineSession.HostAsync`, `GameInputController`, `GameManager`?**
  _High betweenness centrality (0.052) - this node is a cross-community bridge._
- **What connects `PathOverride`, `FilePath`, `Exists` to the rest of the system?**
  _291 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Vector2` be split into smaller, more focused modules?**
  _Cohesion score 0.12773109243697478 - nodes in this community are weakly interconnected._
- **Why does `GameBootstrapper` connect `GameBootstrapper` to `LevelUp.Utils`, `CardModel`, `Image`, `MonoBehaviour`, `IEnumerator`, `GameSession.GetLocalPlayer`, `GameBootstrapper.HandleResumeSolo`, `IGameCommand`, `OnlineSession`, `PassAndPlayController`, `GameObject`, `CardOrigins.Of`, `RoundSummaryView`, `GameManager`, `CardView`, `BalatroEffects`, `OnlinePanel`, `RoundStartedEvent`, `MatchRules`, `DailyPanel`, `PlayerSetup`, `RectTransform`, `GameSession.GetLocalPlayer`?**
  _High betweenness centrality (0.043) - this node is a cross-community bridge._
- **Should `LevelUp.Utils` be split into smaller, more focused modules?**
  _Cohesion score 0.06210964607911173 - nodes in this community are weakly interconnected._
- **Why does `HandView` connect `HandView` to `Vector2`, `LevelUp.Utils`, `CardModel`, `TextMeshProUGUI`, `MonoBehaviour`, `UISkin.Text`, `EventBus`, `IEnumerator`, `NetworkManager`, `Guide de layout Unity — LevelUp`, `GameBootstrapper.HandleResumeSolo`, `AIPlayer`, `GameInputController.ApplyPreferredSort`, `GameObject`, `GameInputController`, `CardOrigins.Of`, `RectTransform`, `CardView`, `SelectionStatusView`, `DevAutoScreenshot.Start`, `TurnStartedEvent`, `IReadOnlyList`, `PauseMenuController`?**
  _High betweenness centrality (0.040) - this node is a cross-community bridge._
- **Should `CardModel` be split into smaller, more focused modules?**
  _Cohesion score 0.1289198606271777 - nodes in this community are weakly interconnected._