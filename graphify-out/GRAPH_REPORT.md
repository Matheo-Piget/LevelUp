# Graph Report - LevelUp  (2026-11-13)

## Corpus Check
- 118 files · ~120,588 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 250 file(s) not represented in the graph (top: .meta 181, .asset 34, .shader 13)
- WARNING: 250 of 377 files seen (66%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 2713 nodes · 8597 edges · 153 communities (82 shown, 71 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 858 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `a12bab0c`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- LevelUp.Utils
- CardModel
- Image
- HandView
- manifest.json
- com.unity.2d.common@8.1.0
- TutorialDirector
- CardFactory.G
- AudioManager
- com.unity.render-pipelines.core
- NetworkManager
- Guide de layout Unity — LevelUp
- com.unity.mathematics@1.2.6
- EventBus.Publish
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
- EventBus.Clear
- AIPlayer
- CoachAdvisor
- SkipStackingTests
- LevelRequirement
- com.unity.modules.androidjni@1.0.0
- com.unity.modules.unitywebrequest@1.0.0
- MatchRules
- IEnumerator
- com.unity.inputsystem
- PassAndPlayController
- GameSettings
- UISkin
- com.unity.2d.spriteshape
- com.unity.nuget.mono-cecil@1.10.1
- GameObject
- GameConfig
- com.unity.modules.unitywebrequesttexture@1.0.0
- MainMenuController
- EventBus.Subscribe
- GameManager
- GameInputController
- RoundSummaryView
- CommandError
- GameCommandExecutor.Execute
- Action
- MatchRulesTests
- FastBufferReader
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
- CardModel
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
- Color
- Kind
- ChallengeDirector
- RematchVote
- RectTransform
- MonoBehaviour
- BotPersonas.Names
- UISkin.Text
- GameInputController.ExecuteAddToMeld
- PanelState
- DeckManager
- GameSession.GetLocalPlayer
- ContractAndReplayTests
- ProfileDialog
- SpecialCardsMode
- UITween.FadeTo
- quick-compile.sh
- run-harness.sh

## God Nodes (most connected - your core abstractions)
1. `OnlineSession` - 95 edges
2. `GameInputController` - 92 edges
3. `GameBootstrapper` - 82 edges
4. `LevelUp.Core` - 79 edges
5. `GameManager` - 74 edges
6. `HandView` - 71 edges
7. `GameScreenLayout` - 65 edges
8. `LevelUp.UI` - 54 edges
9. `UISkin` - 53 edges
10. `PlayerModel` - 51 edges

## Surprising Connections (you probably didn't know these)
- `3.6 TurnBannerView (bannière temporaire "Tour de X")` --references--> `TurnStartedEvent`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/Core/GameEvents.cs
- `3.5.a HandContainer (enfant de PlayerHand)` --references--> `HandView`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `2) Contexte technique du repo (a respecter)` --references--> `TurnManager`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/Core/TurnManager.cs
- `2) Contexte technique du repo (a respecter)` --references--> `GameInputController`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/UI/GameInputController.cs
- `2) Contexte technique du repo (a respecter)` --references--> `HandView`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/UI/HandView.cs

## Import Cycles
- None detected.

## Communities (153 total, 71 thin omitted)

### Community 1 - "LevelUp.Utils"
Cohesion: 0.05
Nodes (18): LevelUp.Core, LevelUp.Tests, LevelUp.Utils, LevelUp.EditorTools, LevelUp.UI, LevelUp.AI, LevelUp.Network, UIInputBlocker (+10 more)

### Community 2 - "CardModel"
Cohesion: 0.13
Nodes (4): CardExtensions, LevelRequirement, LevelValidator, 15.4 Helpers STATIQUES (aucun GameObject)

### Community 4 - "Image"
Cohesion: 0.18
Nodes (4): ContractRow, StepPill, TextMirror, PlayerPill

### Community 5 - "HandView"
Cohesion: 0.12
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

### Community 9 - "CardFactory.G"
Cohesion: 0.22
Nodes (3): BotBrainTests, LevelValidatorTests, CardFactory

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
Cohesion: 0.06
Nodes (32): 0. Pré-requis : Canvas Scaler, 10. Méthode pour ajuster visuellement dans Unity, 11. Test et validation, 12. Troubleshooting, 13. Ajustements mobiles (optionnel), 14. Couleurs de référence (cohérence visuelle), 15.3 Scripts sur PREFABS (pas dans la scène), 15.5 Hiérarchie finale complète (copier-coller mental) (+24 more)

### Community 14 - "com.unity.mathematics@1.2.6"
Cohesion: 0.08
Nodes (28): dependencies, depth, source, url, version, dependencies, depth, source (+20 more)

### Community 15 - "EventBus.Publish"
Cohesion: 0.09
Nodes (14): GameCommandExecutor, CommandResult, DrawnCard, Error, Message, RoundEnded, Success, WinnerOverride (+6 more)

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
Cohesion: 0.10
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
Cohesion: 0.15
Nodes (3): CardExtensionsTests, LayDownOptionsTests, MeldTests

### Community 30 - "OnlineSession"
Cohesion: 0.09
Nodes (10): CurrentLobby, DeviceToken, IsActive, IsSessionHost, JoinCode, LastJoinCode, LobbyHumanCount, VersionTag (+2 more)

### Community 31 - "BotBrain"
Cohesion: 0.13
Nodes (3): BotBrain, Difficulty, Me

### Community 32 - "com.unity.2d.tilemap"
Cohesion: 0.12
Nodes (18): dependencies, depth, dependencies, depth, source, url, version, source (+10 more)

### Community 33 - "MeldGroupView"
Cohesion: 0.09
Nodes (8): CardOrigin, CardOrigins, Provider, MeldGroupView, MeldType, OwnerIndex, TableView, ContentScale

### Community 34 - "com.unity.ugui@1.0.0"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, url, version, depth, source, version (+9 more)

### Community 35 - "EventBus.Clear"
Cohesion: 0.09
Nodes (7): GameConfig, CardMaxValue, CardMinValue, CardsPerPlayer, DeckSize, MaxPlayers, MinPlayers

### Community 40 - "LevelRequirement"
Cohesion: 0.16
Nodes (6): ContractCatalog, ContractPlan, IsRandom, ContractProgress, Entry, IsMet

### Community 44 - "MatchRules"
Cohesion: 0.09
Nodes (16): MatchRules, CatchUp, Draw2Count, FinalLevelRequired, GoOutBonus, HandSize, HasTurnTimer, IsClassic (+8 more)

### Community 45 - "IEnumerator"
Cohesion: 0.06
Nodes (23): 10) Carte du code (a lire AVANT d'explorer : evite de tout relire), 1) Role de Claude dans ce projet, 3) Objectif produit global, 4) Backlog priorise (spec fonctionnelle), 5) Contraintes de mise en oeuvre, 6) Definition of Done (DoD), 7) Format de reponse attendu de Claude, 8) Regles de priorisation quand il y a ambiguite (+15 more)

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

### Community 52 - "GameObject"
Cohesion: 0.15
Nodes (3): EmoteView, UITween, Runner

### Community 53 - "GameConfig"
Cohesion: 0.14
Nodes (3): Challenge, ChallengeGenerator, ChallengeGeneratorTests

### Community 56 - "MainMenuController"
Cohesion: 0.13
Nodes (5): MainMenuController, IsVisible, MatchSetup, Root, 15.2 Scripts AUTO-GÉNÉRÉS au runtime (ne pas les ajouter à la main)

### Community 57 - "EventBus.Subscribe"
Cohesion: 0.19
Nodes (5): EventBus, Muted, Subscription, EventBusTests, PingEvent

### Community 58 - "GameManager"
Cohesion: 0.10
Nodes (14): GameManager, AutoStartNextRound, CommandFilter, Config, DeckManager, Executor, LastRoundWinner, NextContracts (+6 more)

### Community 59 - "GameInputController"
Cohesion: 0.10
Nodes (7): GameInputController, ActionsLocked, CurrentPlayerIndex, DpiScale, DragUpThreshold, IsLocalTurn, ReorderDragThreshold

### Community 61 - "RoundSummaryView"
Cohesion: 0.16
Nodes (4): RoundSummaryView, IsVisible, ReplayAvailable, Suppressed

### Community 62 - "CommandError"
Cohesion: 0.12
Nodes (16): CommandError, ActionCardNotPickable, AlreadyLaidDown, CardDoesNotFit, CardNotInHand, ContractMismatch, EmptyDiscard, Filtered (+8 more)

### Community 63 - "GameCommandExecutor.Execute"
Cohesion: 0.27
Nodes (4): LayDownLevelCommand, Melds, PlayerIndex, GameCommandExecutorTests

### Community 65 - "Action"
Cohesion: 0.16
Nodes (4): AnimationController, IsAnimating, MatchSetupPanel, IsVisible

### Community 68 - "CardView"
Cohesion: 0.09
Nodes (9): 2) Contexte technique du repo (a respecter), CardView, CardModel, IsFaceUp, IsHovered, IsSelected, RectTransform, CardViewPool (+1 more)

### Community 101 - "PointerInput"
Cohesion: 0.13
Nodes (7): 9) Prochaines taches recommandees (ordre d'execution), FrameBudget, Enabled, PointerInput, LastActivityFrame, Override, PointerState

### Community 103 - "BalatroEffects"
Cohesion: 0.16
Nodes (4): BalatroEffects, 15.1 Scripts à placer MANUELLEMENT dans la scène `Game.unity`, Scripts racine (GameObjects vides, pas sous Canvas), Scripts UI (enfants du Canvas)

### Community 104 - "UIIcon"
Cohesion: 0.15
Nodes (13): UIIcon, Cards, Check, Clock, Close, Copy, Edit, Lock (+5 more)

### Community 105 - "DiscardPileView"
Cohesion: 0.17
Nodes (5): DiscardPileView, Compact, PileCount, PileSpacing, PileSlot

### Community 106 - "UIInputs.TextField"
Cohesion: 0.16
Nodes (8): UI / DA "Salon", UIInputs, TextWeight, Bold, Display, Medium, Regular, SemiBold

### Community 107 - "SocialProtocol"
Cohesion: 0.16
Nodes (3): EmoteLimiter, SocialProtocol, SocialProtocolTests

### Community 108 - "TurnTimer"
Cohesion: 0.24
Nodes (3): GameOverEvent, RoundEndedEvent, TurnTimer

### Community 109 - "OnlinePanel"
Cohesion: 0.18
Nodes (6): En ligne (`Scripts/Network`), LobbySeatView, LobbyViewModel, OnlinePanel, IsVisible, LastCode

### Community 110 - "DrawFromDeckCommand"
Cohesion: 0.24
Nodes (6): DrawFromDeckCommand, PlayerIndex, SkipPhaseCommand, PhaseToSkip, PlayerIndex, CoreGuardsTests

### Community 115 - "CardModel"
Cohesion: 0.14
Nodes (9): CardModel, IsAction, IsWild, CardType, Draw2, Normal, Skip, Wild (+1 more)

### Community 117 - "TurnStartedEvent"
Cohesion: 0.21
Nodes (14): ActionCardPlayedEvent, CardAddedToMeldEvent, CardDiscardedEvent, CardDrawnEvent, ForcedDrawEvent, GameStartedEvent, HandChangedEvent, LevelCompletedEvent (+6 more)

### Community 118 - "DailyPanel"
Cohesion: 0.18
Nodes (4): DailyDeal, Rules, Streak, TodayKey

### Community 119 - "PlayerSetup"
Cohesion: 0.19
Nodes (7): SavedGame, SaveGame, Exists, FilePath, PathOverride, GameBootstrapper, OnlineStartData

### Community 120 - "RectTransform"
Cohesion: 0.11
Nodes (3): GameScreenLayout, Coach, Root

### Community 122 - "PlayerModel"
Cohesion: 0.11
Nodes (17): Meld, Cards, OwnerIndex, Type, PlayerModel, CurrentLevel, Hand, HasLaidDownThisRound (+9 more)

### Community 124 - "GameSession.GetLocalPlayer"
Cohesion: 0.22
Nodes (6): PlayerProfile, GamesPlayed, GamesWon, HasCustomName, Name, RoundsWon

### Community 125 - "PauseMenuController"
Cohesion: 0.16
Nodes (5): AppLifecycle, OptionsPanel, PauseMenuController, IsPaused, PauseAllowed

### Community 127 - "RulesPanel"
Cohesion: 0.32
Nodes (4): RulesPanel, IsVisible, TutorialAvailable, TutorialRequested

### Community 128 - "Color"
Cohesion: 0.14
Nodes (14): CardColor, Blue, Green, Orange, Purple, Red, Wild, Yellow (+6 more)

### Community 129 - "Kind"
Cohesion: 0.25
Nodes (8): Kind, AddToMeld, Discard, DrawFromDeck, DrawFromDiscard, LayDown, SkipPhase, StartNextRound

### Community 130 - "ChallengeDirector"
Cohesion: 0.24
Nodes (4): ChallengeDirector, IsRunning, Rules, Solved

### Community 131 - "RematchVote"
Cohesion: 0.19
Nodes (4): RematchVote, ExpectedMask, IsAgreed, VotesMask

### Community 132 - "RectTransform"
Cohesion: 0.11
Nodes (7): AnimatedBackground, GameOverCelebration, PlayerNames, Players, ReplayIsVote, RoundsPlayed, SafeAreaFitter

### Community 133 - "MonoBehaviour"
Cohesion: 0.12
Nodes (8): Ce qui tourne vraiment, CardFlights, ConnectionOverlay, IsVisible, OpponentMovesView, TargetChooser, IsOpen, TweenRunner

### Community 134 - "BotPersonas.Names"
Cohesion: 0.25
Nodes (3): BotPersonas, BotStyle, PersonaTests

### Community 137 - "PanelState"
Cohesion: 0.33
Nodes (6): PanelState, Busy, ClientLobby, Home, HostLobby, Join

### Community 138 - "DeckManager"
Cohesion: 0.25
Nodes (5): DeckManager, CardsPerPlayer, DiscardPileCount, DrawPileCount, PresetOrder

### Community 139 - "GameSession.GetLocalPlayer"
Cohesion: 0.12
Nodes (6): GameSession, Contracts, IsHost, IsOnline, LocalPlayerIndex, Rules

### Community 145 - "SpecialCardsMode"
Cohesion: 0.23
Nodes (5): SpecialCardsMode, All, Jokers, None, MatchRulesText

### Community 146 - "UITween.FadeTo"
Cohesion: 0.12
Nodes (7): ConfirmDialog, DailyPanel, IsVisible, LayDownChooser, IsOpen, UIBackStack, HasOpenPanel

## Knowledge Gaps
- **296 isolated node(s):** `IsRunning`, `Solved`, `Rules`, `IsRandom`, `IsMet` (+291 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 727 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **71 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `OnlineSession` connect `OnlineSession` to `LevelUp.Utils`, `CardModel`, `RematchVote`, `FastBufferReader`, `PointerInput`, `MonoBehaviour`, `GameSession.GetLocalPlayer`, `SocialProtocol`, `OnlineSession.ApproveConnection`, `MatchRules`, `GameConfig`, `GameBootstrapper.HandleResumeSolo`, `OnlineSession.HostAsync`, `PlayerSetup`?**
  _High betweenness centrality (0.077) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `OnlineSession` (e.g. with `9) Prochaines taches recommandees (ordre d'execution)` and `Ce qui tourne vraiment`) actually correct?**
  _`OnlineSession` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `IsRunning`, `Solved`, `Rules` to the rest of the system?**
  _296 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Vector2` be split into smaller, more focused modules?**
  _Cohesion score 0.14516129032258066 - nodes in this community are weakly interconnected._
- **Why does `GameManager` connect `GameManager` to `LevelUp.Utils`, `ChallengeDirector`, `CardModel`, `MonoBehaviour`, `UISkin.Text`, `TutorialDirector`, `CardFactory.G`, `AudioManager`, `ContractAndReplayTests`, `NetworkManager`, `Guide de layout Unity — LevelUp`, `EventBus.Publish`, `GameBootstrapper.HandleResumeSolo`, `GameBootstrapper`, `BotBrain`, `EventBus.Clear`, `AIPlayer`, `LevelRequirement`, `MatchRules`, `IEnumerator`, `GameConfig`, `GameInputController`, `UIFactory.CreateButton`, `MatchRulesTests`, `BalatroEffects`, `DiscardPileView`, `TurnTimer`, `DrawFromDeckCommand`, `SelectionStatusView`, `BotSimulationTests.PlayGame`, `TurnStartedEvent`, `PlayerSetup`, `IReadOnlyList`?**
  _High betweenness centrality (0.043) - this node is a cross-community bridge._
- **Should `LevelUp.Utils` be split into smaller, more focused modules?**
  _Cohesion score 0.05311713726586525 - nodes in this community are weakly interconnected._
- **Why does `HandView` connect `HandView` to `Vector2`, `LevelUp.Utils`, `CardModel`, `Image`, `MonoBehaviour`, `RectTransform`, `GameInputController.ExecuteAddToMeld`, `Guide de layout Unity — LevelUp`, `GameBootstrapper.HandleResumeSolo`, `MeldGroupView`, `GameInputController.ApplyPreferredSort`, `IEnumerator`, `GameObject`, `EventBus.Subscribe`, `GameInputController`, `UIFactory.CreateButton`, `Action`, `CardView`, `BalatroEffects`, `SelectionStatusView`, `RoundStartedEvent`, `TurnStartedEvent`, `IReadOnlyList`?**
  _High betweenness centrality (0.042) - this node is a cross-community bridge._