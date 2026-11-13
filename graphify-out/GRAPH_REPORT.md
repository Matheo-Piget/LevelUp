# Graph Report - LevelUp  (2026-11-13)

## Corpus Check
- 112 files · ~116,590 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 246 file(s) not represented in the graph (top: .meta 177, .asset 34, .shader 13)
- WARNING: 246 of 367 files seen (67%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 2660 nodes · 8345 edges · 148 communities (70 shown, 78 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 822 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `4e66d39d`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Vector2
- LevelUp.Utils
- CardModel
- Image
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
- TableView
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
- MatchRulesTests
- AIPlayer
- List
- DeckManager
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
- Expect
- com.unity.modules.unitywebrequesttexture@1.0.0
- MainMenuController
- EventBus.Subscribe
- GameManager
- CardOrigins.Of
- RoundSummaryView
- CommandError
- GameInputController.ExecuteAddToMeld
- Action
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
- EmoteLimiter
- SocialProtocol
- GameBootstrapper.EnsureGameSystems
- OnlinePanel
- GameManager.ExecuteCommand
- SelectionStatusView
- RoundStartedEvent
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
- CardColor
- Kind
- RematchVote
- GameOverCelebration
- MonoBehaviour
- Transform
- PanelState
- GameSession
- LobbyViewModel
- PointerInput
- SpecialCardsMode
- ConfirmDialog

## God Nodes (most connected - your core abstractions)
1. `OnlineSession` - 93 edges
2. `GameInputController` - 92 edges
3. `GameBootstrapper` - 84 edges
4. `LevelUp.Core` - 75 edges
5. `HandView` - 71 edges
6. `GameScreenLayout` - 67 edges
7. `GameManager` - 67 edges
8. `PlayerModel` - 53 edges
9. `LevelUp.UI` - 52 edges
10. `UISkin` - 52 edges

## Surprising Connections (you probably didn't know these)
- `5. Références à assigner sur GameBootstrapper` --references--> `GameBootstrapper`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/GameBootstrapper.cs
- `3.6 TurnBannerView (bannière temporaire "Tour de X")` --references--> `TurnStartedEvent`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/Core/GameEvents.cs
- `3.5.a HandContainer (enfant de PlayerHand)` --references--> `HandView`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `Regles (Core)` --references--> `GameSession`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/Core/GameSession.cs
- `Ce qui tourne vraiment` --references--> `GameBootstrapper`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/GameBootstrapper.cs

## Import Cycles
- None detected.

## Communities (148 total, 78 thin omitted)

### Community 0 - "Vector2"
Cohesion: 0.09
Nodes (7): GameInputController, ActionsLocked, CurrentPlayerIndex, DpiScale, DragUpThreshold, IsLocalTurn, ReorderDragThreshold

### Community 1 - "LevelUp.Utils"
Cohesion: 0.05
Nodes (19): LevelUp.Core, LevelUp.Tests, LevelUp.Utils, LevelUp.EditorTools, LevelUp.UI, LevelUp.AI, LevelUp.Network, BotPersonas (+11 more)

### Community 4 - "Image"
Cohesion: 0.17
Nodes (4): PileSlot, ContractRow, StepPill, PlayerPill

### Community 5 - "HandView"
Cohesion: 0.09
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
Cohesion: 0.12
Nodes (13): NetworkManager, IsPassScreenActive, NetworkMode, PassAndPlay, SinglePlayer, 15.1 Scripts à placer MANUELLEMENT dans la scène `Game.unity`, 15.3 Scripts sur PREFABS (pas dans la scène), 15.5 Hiérarchie finale complète (copier-coller mental) (+5 more)

### Community 13 - "Guide de layout Unity — LevelUp"
Cohesion: 0.07
Nodes (27): 0. Pré-requis : Canvas Scaler, 10. Méthode pour ajuster visuellement dans Unity, 11. Test et validation, 12. Troubleshooting, 13. Ajustements mobiles (optionnel), 14. Couleurs de référence (cohérence visuelle), 1. Vue d'ensemble du layout, 2. Hiérarchie cible (+19 more)

### Community 14 - "com.unity.mathematics@1.2.6"
Cohesion: 0.08
Nodes (28): dependencies, depth, source, url, version, dependencies, depth, source (+20 more)

### Community 15 - "PlayerModel"
Cohesion: 0.12
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
Cohesion: 0.09
Nodes (14): LayDownLevelCommand, Melds, PlayerIndex, Meld, Cards, OwnerIndex, Type, BotBrainTests (+6 more)

### Community 30 - "OnlineSession"
Cohesion: 0.10
Nodes (10): CurrentLobby, DeviceToken, IsActive, IsSessionHost, JoinCode, LastJoinCode, LobbyHumanCount, VersionTag (+2 more)

### Community 31 - "BotBrain"
Cohesion: 0.14
Nodes (3): BotBrain, Difficulty, Me

### Community 32 - "com.unity.2d.tilemap"
Cohesion: 0.12
Nodes (18): dependencies, depth, dependencies, depth, source, url, version, source (+10 more)

### Community 33 - "MeldGroupView"
Cohesion: 0.17
Nodes (4): LevelRequirement, MeldGroupView, MeldType, OwnerIndex

### Community 34 - "com.unity.ugui@1.0.0"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, url, version, depth, source, version (+9 more)

### Community 39 - "DeckManager"
Cohesion: 0.07
Nodes (14): DeckManager, CardsPerPlayer, DiscardPileCount, DrawPileCount, PresetOrder, GameConfig, CardMaxValue, CardMinValue (+6 more)

### Community 40 - "LevelRequirement"
Cohesion: 0.16
Nodes (6): ContractCatalog, ContractPlan, IsRandom, ContractProgress, Entry, IsMet

### Community 44 - "MatchRules"
Cohesion: 0.07
Nodes (17): MatchRules, CatchUp, Draw2Count, FinalLevelRequired, GoOutBonus, HandSize, HasTurnTimer, IsClassic (+9 more)

### Community 45 - "CLAUDE.md - Brief d'execution optimise pour Claude"
Cohesion: 0.08
Nodes (22): 10) Carte du code (a lire AVANT d'explorer : evite de tout relire), 1) Role de Claude dans ce projet, 3) Objectif produit global, 4) Backlog priorise (spec fonctionnelle), 5) Contraintes de mise en oeuvre, 6) Definition of Done (DoD), 7) Format de reponse attendu de Claude, 8) Regles de priorisation quand il y a ambiguite (+14 more)

### Community 46 - "com.unity.inputsystem"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.inputsystem

### Community 47 - "PassAndPlayController"
Cohesion: 0.31
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
Cohesion: 0.14
Nodes (5): EmoteView, TweenRunner, UITween, Runner, 15.4 Helpers STATIQUES (aucun GameObject)

### Community 53 - "Expect"
Cohesion: 0.29
Nodes (7): Expect, AddToMeld, Discard, Done, DrawDeck, DrawDiscard, LayDown

### Community 56 - "MainMenuController"
Cohesion: 0.10
Nodes (11): Ce qui tourne vraiment, LayDownChooser, IsOpen, MainMenuController, IsVisible, MatchSetup, Root, ProfileDialog (+3 more)

### Community 57 - "EventBus.Subscribe"
Cohesion: 0.18
Nodes (6): 2) Contexte technique du repo (a respecter), EventBus, Muted, Subscription, EventBusTests, PingEvent

### Community 58 - "GameManager"
Cohesion: 0.11
Nodes (13): GameManager, AutoStartNextRound, CommandFilter, Config, DeckManager, Executor, LastRoundWinner, NextDeckOrder (+5 more)

### Community 60 - "CardOrigins.Of"
Cohesion: 0.43
Nodes (3): CardOrigin, CardOrigins, Provider

### Community 61 - "RoundSummaryView"
Cohesion: 0.19
Nodes (5): GameOverEvent, GameStartedEvent, RoundSummaryEvent, RoundSummaryView, IsVisible

### Community 62 - "CommandError"
Cohesion: 0.12
Nodes (16): CommandError, ActionCardNotPickable, AlreadyLaidDown, CardDoesNotFit, CardNotInHand, ContractMismatch, EmptyDiscard, Filtered (+8 more)

### Community 68 - "CardView"
Cohesion: 0.10
Nodes (9): CardFlights, CardView, CardModel, IsFaceUp, IsHovered, IsSelected, RectTransform, CardViewPool (+1 more)

### Community 104 - "UIIcon"
Cohesion: 0.15
Nodes (13): UIIcon, Cards, Check, Clock, Close, Copy, Edit, Lock (+5 more)

### Community 105 - "DiscardPileView"
Cohesion: 0.18
Nodes (4): DiscardPileView, Compact, PileCount, PileSpacing

### Community 109 - "OnlinePanel"
Cohesion: 0.12
Nodes (10): OnlinePanel, IsVisible, LastCode, UIInputs, TextWeight, Bold, Display, Medium (+2 more)

### Community 110 - "GameManager.ExecuteCommand"
Cohesion: 0.09
Nodes (26): Regles (Core), GameCommandCodec, AddToMeldCommand, Card, MeldIndex, MeldOwnerIndex, PlayerIndex, DiscardCommand (+18 more)

### Community 116 - "MatchRules"
Cohesion: 0.15
Nodes (5): En ligne (`Scripts/Network`), LobbyInfo, LobbyMember, OnlineResumeData, OnlineStartData

### Community 117 - "TurnStartedEvent"
Cohesion: 0.22
Nodes (12): ActionCardPlayedEvent, CardAddedToMeldEvent, CardDiscardedEvent, ForcedDrawEvent, HandChangedEvent, LevelCompletedEvent, LevelLaidDownEvent, PlayerSkippedEvent (+4 more)

### Community 118 - "DailyPanel"
Cohesion: 0.18
Nodes (6): DailyDeal, Rules, Streak, TodayKey, DailyPanel, IsVisible

### Community 119 - "PlayerSetup"
Cohesion: 0.24
Nodes (6): PlayerSetup, SavedGame, SaveGame, Exists, FilePath, PathOverride

### Community 120 - "RectTransform"
Cohesion: 0.07
Nodes (7): UI / DA "Salon", AnimatedBackground, AnimationController, IsAnimating, GameScreenLayout, Coach, Root

### Community 122 - "PlayerModel"
Cohesion: 0.09
Nodes (15): ActionCardHandler, PlayerModel, CurrentLevel, Hand, HasLaidDownThisRound, Index, IsAI, IsHandEmpty (+7 more)

### Community 124 - "GameSession.GetLocalPlayer"
Cohesion: 0.15
Nodes (6): PlayerProfile, GamesPlayed, GamesWon, HasCustomName, Name, RoundsWon

### Community 125 - "PauseMenuController"
Cohesion: 0.14
Nodes (6): AppLifecycle, PauseMenuController, IsPaused, PauseAllowed, UIBackStack, HasOpenPanel

### Community 128 - "CardColor"
Cohesion: 0.07
Nodes (23): CardModel, IsAction, IsWild, CardColor, Blue, Green, Orange, Purple (+15 more)

### Community 129 - "Kind"
Cohesion: 0.25
Nodes (8): Kind, AddToMeld, Discard, DrawFromDeck, DrawFromDiscard, LayDown, SkipPhase, StartNextRound

### Community 131 - "RematchVote"
Cohesion: 0.24
Nodes (4): RematchVote, ExpectedMask, IsAgreed, VotesMask

### Community 132 - "GameOverCelebration"
Cohesion: 0.19
Nodes (5): GameOverCelebration, PlayerNames, Players, ReplayIsVote, RoundsPlayed

### Community 133 - "MonoBehaviour"
Cohesion: 0.16
Nodes (5): ConnectionOverlay, IsVisible, TextMirror, SafeAreaFitter, UIInputBlocker

### Community 135 - "Transform"
Cohesion: 0.16
Nodes (6): OptionsPanel, RulesPanel, IsVisible, TutorialAvailable, TutorialRequested, UIControls

### Community 137 - "PanelState"
Cohesion: 0.33
Nodes (6): PanelState, Busy, ClientLobby, Home, HostLobby, Join

### Community 139 - "GameSession"
Cohesion: 0.25
Nodes (6): GameSession, Contracts, IsHost, IsOnline, LocalPlayerIndex, Rules

### Community 142 - "PointerInput"
Cohesion: 0.39
Nodes (4): PointerInput, LastActivityFrame, Override, PointerState

### Community 145 - "SpecialCardsMode"
Cohesion: 0.21
Nodes (5): SpecialCardsMode, All, Jokers, None, MatchRulesText

## Knowledge Gaps
- **291 isolated node(s):** `IsOnline`, `IsHost`, `LocalPlayerIndex`, `Contracts`, `Rules` (+286 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 718 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **78 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `OnlineSession` connect `OnlineSession` to `Vector2`, `LevelUp.Utils`, `CardModel`, `RematchVote`, `FastBufferReader`, `List`, `MonoBehaviour`, `EmoteLimiter`, `SocialProtocol`, `OnlinePanel`, `MatchRules`, `GameBootstrapper.HandleResumeSolo`, `OnlineSession.HostAsync`, `GameManager`?**
  _High betweenness centrality (0.066) - this node is a cross-community bridge._
- **What connects `IsOnline`, `IsHost`, `LocalPlayerIndex` to the rest of the system?**
  _291 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Vector2` be split into smaller, more focused modules?**
  _Cohesion score 0.09343200740055504 - nodes in this community are weakly interconnected._
- **Why does `GameInputController` connect `Vector2` to `LevelUp.Utils`, `Image`, `HandView`, `MonoBehaviour`, `Transform`, `NetworkManager`, `TableView`, `HUDView`, `List`, `GameInputController.ApplyPreferredSort`, `CLAUDE.md - Brief d'execution optimise pour Claude`, `MainMenuController`, `EventBus.Subscribe`, `GameManager`, `HandView.DeselectAll`, `GameInputController.ExecuteAddToMeld`, `Action`, `CardView`, `LevelProgressView`, `DiscardPileView`, `SelectionStatusView`, `TurnStartedEvent`, `RectTransform`?**
  _High betweenness centrality (0.051) - this node is a cross-community bridge._
- **Should `LevelUp.Utils` be split into smaller, more focused modules?**
  _Cohesion score 0.0524904214559387 - nodes in this community are weakly interconnected._
- **Why does `HandView` connect `HandView` to `Vector2`, `LevelUp.Utils`, `CardModel`, `UISkin.Text`, `Image`, `MonoBehaviour`, `IEnumerator`, `NetworkManager`, `Guide de layout Unity — LevelUp`, `GameBootstrapper.HandleResumeSolo`, `List`, `GameInputController.ApplyPreferredSort`, `GameObject`, `MainMenuController`, `EventBus.Subscribe`, `HandView.DeselectAll`, `CardOrigins.Of`, `GameInputController.ExecuteAddToMeld`, `CardView`, `SelectionStatusView`, `TurnStartedEvent`, `RectTransform`, `IReadOnlyList`?**
  _High betweenness centrality (0.045) - this node is a cross-community bridge._
- **Should `HandView` be split into smaller, more focused modules?**
  _Cohesion score 0.09302325581395349 - nodes in this community are weakly interconnected._