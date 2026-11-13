# Graph Report - LevelUp  (2026-11-13)

## Corpus Check
- 100 files · ~108,653 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 236 file(s) not represented in the graph (top: .meta 167, .asset 34, .shader 13)
- WARNING: 236 of 345 files seen (68%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 2476 nodes · 7791 edges · 138 communities (66 shown, 72 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 771 edges (avg confidence: 0.87)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `24c9e480`
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
- Test
- BotSimulationTests.PlayGame
- OnlineSession
- BotBrain
- com.unity.2d.tilemap
- MeldGroupView
- com.unity.ugui@1.0.0
- MatchRulesTests
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
- TextMirror
- GameSettings
- UISkin
- com.unity.2d.spriteshape
- com.unity.nuget.mono-cecil@1.10.1
- CanvasGroup
- com.unity.modules.unitywebrequesttexture@1.0.0
- EventBus.Subscribe
- GameInputController.ExecuteAddToMeld
- AndroidBuilder
- GameSession.IsLocalHuman
- CommandError
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
- FrameBudget.Ping
- MonoBehaviour
- BalatroEffects
- TableView
- DiscardPileView
- RoundSummaryView
- TextWeight
- GameOverEvent
- SelectionStatusView
- IGameCommand
- AnimationController
- TurnStartedEvent
- DailyDeal
- PlayerSetup
- RectTransform
- PlayerModel
- Regles (Core)
- UIIcon
- CardColor
- Kind
- SafeAreaFitter
- PanelState
- GameBootstrapper

## God Nodes (most connected - your core abstractions)
1. `GameInputController` - 93 edges
2. `OnlineSession` - 78 edges
3. `GameBootstrapper` - 76 edges
4. `HandView` - 72 edges
5. `GameManager` - 70 edges
6. `LevelUp.Core` - 67 edges
7. `MatchRules` - 66 edges
8. `GameScreenLayout` - 63 edges
9. `PlayerModel` - 60 edges
10. `UISkin` - 52 edges

## Surprising Connections (you probably didn't know these)
- `3.6 TurnBannerView (bannière temporaire "Tour de X")` --references--> `TurnStartedEvent`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/Core/GameEvents.cs
- `5. Références à assigner sur GameBootstrapper` --references--> `GameBootstrapper`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/GameBootstrapper.cs
- `3.5.a HandContainer (enfant de PlayerHand)` --references--> `HandView`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `Regles (Core)` --references--> `GameCommandExecutor`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/Core/Commands/GameCommandExecutor.cs
- `Regles (Core)` --references--> `RoundSummaryEvent`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/Core/GameEvents.cs

## Import Cycles
- None detected.

## Communities (138 total, 72 thin omitted)

### Community 1 - "LevelUp.Utils"
Cohesion: 0.05
Nodes (22): LevelUp.Core, LevelUp.Tests, LevelUp.Utils, LevelUp.EditorTools, LevelUp.UI, LevelUp.AI, LevelUp.Network, GameState (+14 more)

### Community 2 - "CardModel"
Cohesion: 0.12
Nodes (7): CardExtensions, LevelRequirement, LevelValidator, Meld, Cards, OwnerIndex, Type

### Community 4 - "UISkin.Size"
Cohesion: 0.16
Nodes (4): OnlinePanel, IsVisible, LastCode, UIInputs

### Community 5 - "HandView"
Cohesion: 0.10
Nodes (8): HandView, CardCount, CardPrefab, IsAnimatingDeal, IsAnimatingDraw, IsReorderDragging, SelectedCard, SelectedCards

### Community 6 - "manifest.json"
Cohesion: 0.17
Nodes (11): com.unity.modules.jsonserialize@1.0.0, com.unity.modules.umbra@1.0.0, com.unity.render-pipelines.universal@14.0.11, com.unity.textmeshpro@3.0.9, com.unity.visualscripting@1.9.4, com.unity.modules.jsonserialize, com.unity.modules.umbra, com.unity.modules.unityanalytics (+3 more)

### Community 7 - "com.unity.2d.common@8.1.0"
Cohesion: 0.05
Nodes (44): dependencies, depth, source, url, version, dependencies, depth, source (+36 more)

### Community 8 - "GameSession"
Cohesion: 0.22
Nodes (6): GameSession, Contracts, IsHost, IsOnline, LocalPlayerIndex, Rules

### Community 10 - "AudioManager"
Cohesion: 0.12
Nodes (4): AudioManager, Instance, Generator, Haptics

### Community 11 - "com.unity.render-pipelines.core"
Cohesion: 0.07
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 12 - "NetworkManager"
Cohesion: 0.18
Nodes (5): NetworkManager, IsPassScreenActive, NetworkMode, PassAndPlay, SinglePlayer

### Community 13 - "Guide de layout Unity — LevelUp"
Cohesion: 0.07
Nodes (27): 0. Pré-requis : Canvas Scaler, 10. Méthode pour ajuster visuellement dans Unity, 11. Test et validation, 12. Troubleshooting, 13. Ajustements mobiles (optionnel), 14. Couleurs de référence (cohérence visuelle), 1. Vue d'ensemble du layout, 2. Hiérarchie cible (+19 more)

### Community 14 - "com.unity.mathematics@1.2.6"
Cohesion: 0.08
Nodes (28): dependencies, depth, source, url, version, dependencies, depth, source (+20 more)

### Community 15 - "PlayerModel"
Cohesion: 0.10
Nodes (13): GameCommandExecutor, CommandResult, DrawnCard, Error, Message, RoundEnded, Success, WinnerOverride (+5 more)

### Community 16 - "AppLifecycle"
Cohesion: 0.20
Nodes (3): AppLifecycle, UIBackStack, HasOpenPanel

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
Nodes (13): DrawFromDiscardCommand, DiscardPileIndex, PlayerIndex, LayDownLevelCommand, Melds, PlayerIndex, BotBrainTests, CardExtensionsTests (+5 more)

### Community 30 - "OnlineSession"
Cohesion: 0.05
Nodes (13): LobbyInfo, LobbyMember, OnlineResumeData, OnlineSession, CurrentLobby, DeviceToken, IsActive, IsSessionHost (+5 more)

### Community 31 - "BotBrain"
Cohesion: 0.15
Nodes (3): BotBrain, Difficulty, Me

### Community 32 - "com.unity.2d.tilemap"
Cohesion: 0.12
Nodes (18): dependencies, depth, dependencies, depth, source, url, version, source (+10 more)

### Community 33 - "MeldGroupView"
Cohesion: 0.20
Nodes (3): MeldGroupView, MeldType, OwnerIndex

### Community 34 - "com.unity.ugui@1.0.0"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, url, version, depth, source, version (+9 more)

### Community 35 - "MatchRulesTests"
Cohesion: 0.08
Nodes (8): GameConfig, CardMaxValue, CardMinValue, CardsPerPlayer, DeckSize, MaxPlayers, MinPlayers, MatchRulesTests

### Community 36 - "AIPlayer"
Cohesion: 0.08
Nodes (18): IA (`Scripts/AI`), AIDifficulty, Easy, Hard, Normal, AIPlayer, PlayerIndex, GameManager (+10 more)

### Community 38 - "GameManager.ExecuteCommand"
Cohesion: 0.13
Nodes (11): DiscardCommand, Card, PlayerIndex, TargetPlayerIndex, DrawFromDeckCommand, PlayerIndex, SkipPhaseCommand, PhaseToSkip (+3 more)

### Community 39 - "DeckManager"
Cohesion: 0.15
Nodes (6): ActionCardHandler, DeckManager, CardsPerPlayer, DiscardPileCount, DrawPileCount, SkipStackingTests

### Community 40 - "LevelRequirement"
Cohesion: 0.14
Nodes (6): ContractCatalog, ContractPlan, IsRandom, ContractProgress, Entry, IsMet

### Community 44 - "MatchRules"
Cohesion: 0.06
Nodes (23): En ligne (`Scripts/Network`), MatchRules, CatchUp, Draw2Count, FinalLevelRequired, GoOutBonus, HandSize, HasTurnTimer (+15 more)

### Community 45 - "CLAUDE.md - Brief d'execution optimise pour Claude"
Cohesion: 0.13
Nodes (14): 1) Role de Claude dans ce projet, 3) Objectif produit global, 4) Backlog priorise (spec fonctionnelle), 5) Contraintes de mise en oeuvre, 6) Definition of Done (DoD), 7) Format de reponse attendu de Claude, 8) Regles de priorisation quand il y a ambiguite, 9) Prochaines taches recommandees (ordre d'execution) (+6 more)

### Community 46 - "com.unity.inputsystem"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.inputsystem

### Community 48 - "GameSettings"
Cohesion: 0.06
Nodes (30): GameSettings, BotDelayFactor, BotSpeed, CardBack, ColorblindMode, Difficulty, Felt, HandSize (+22 more)

### Community 49 - "UISkin"
Cohesion: 0.06
Nodes (13): UIFactory, CircleSprite, RingSprite, RoundedSprite, SmallPillSprite, SoftShadowSprite, UISkin, ActionBand (+5 more)

### Community 50 - "com.unity.2d.spriteshape"
Cohesion: 0.14
Nodes (14): dependencies, depth, source, url, version, dependencies, depth, source (+6 more)

### Community 51 - "com.unity.nuget.mono-cecil@1.10.1"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, url, version, dependencies, depth, source (+5 more)

### Community 53 - "CanvasGroup"
Cohesion: 0.08
Nodes (10): GameOverCelebration, PlayerNames, Players, RoundsPlayed, MainMenuController, IsVisible, MatchSetup, Root (+2 more)

### Community 57 - "EventBus.Subscribe"
Cohesion: 0.21
Nodes (5): EventBus, Muted, Subscription, EventBusTests, PingEvent

### Community 62 - "CommandError"
Cohesion: 0.12
Nodes (15): CommandError, ActionCardNotPickable, AlreadyLaidDown, CardDoesNotFit, CardNotInHand, ContractMismatch, EmptyDiscard, InvalidMeld (+7 more)

### Community 65 - "GameInputController"
Cohesion: 0.10
Nodes (7): GameInputController, ActionsLocked, CurrentPlayerIndex, DpiScale, DragUpThreshold, IsLocalTurn, ReorderDragThreshold

### Community 66 - "GameObject"
Cohesion: 0.06
Nodes (14): ConfirmDialog, LayDownChooser, IsOpen, PauseMenuController, IsPaused, PauseAllowed, ProfileDialog, IsVisible (+6 more)

### Community 68 - "CardView"
Cohesion: 0.11
Nodes (9): 2) Contexte technique du repo (a respecter), CardView, CardModel, IsFaceUp, IsHovered, IsSelected, RectTransform, CardViewPool (+1 more)

### Community 101 - "FrameBudget.Ping"
Cohesion: 0.13
Nodes (6): FrameBudget, Enabled, PointerInput, LastActivityFrame, Override, PointerState

### Community 102 - "MonoBehaviour"
Cohesion: 0.11
Nodes (9): Ce qui tourne vraiment, CardFlights, ConnectionOverlay, IsVisible, LevelProgressView, OpponentMovesView, TargetChooser, IsOpen (+1 more)

### Community 104 - "TableView"
Cohesion: 0.15
Nodes (4): CardOrigins, Provider, TableView, ContentScale

### Community 105 - "DiscardPileView"
Cohesion: 0.19
Nodes (4): DiscardPileView, Compact, PileCount, PileSpacing

### Community 106 - "RoundSummaryView"
Cohesion: 0.12
Nodes (6): GameOverEvent, GameStartedEvent, RoundEndedEvent, RoundSummaryEvent, RoundSummaryView, IsVisible

### Community 107 - "TextWeight"
Cohesion: 0.33
Nodes (6): TextWeight, Bold, Display, Medium, Regular, SemiBold

### Community 113 - "IGameCommand"
Cohesion: 0.13
Nodes (14): 10) Carte du code (a lire AVANT d'explorer : evite de tout relire), Regles (Core), Verifier sans appareil (editeur Unity FERME, sinon le batchmode echoue), GameCommandCodec, AddToMeldCommand, Card, MeldIndex, MeldOwnerIndex (+6 more)

### Community 116 - "AnimationController"
Cohesion: 0.14
Nodes (10): AnimationController, IsAnimating, 15.1 Scripts à placer MANUELLEMENT dans la scène `Game.unity`, 15.3 Scripts sur PREFABS (pas dans la scène), 15.5 Hiérarchie finale complète (copier-coller mental), 15.6 Toutes les références Inspector (checklist drag & drop), 15.7 Résumé en 3 lignes, 15. Mapping complet script → GameObject (+2 more)

### Community 117 - "TurnStartedEvent"
Cohesion: 0.18
Nodes (13): ActionCardPlayedEvent, CardAddedToMeldEvent, CardDiscardedEvent, CardDrawnEvent, DeckChangedEvent, ForcedDrawEvent, HandChangedEvent, LevelCompletedEvent (+5 more)

### Community 118 - "DailyDeal"
Cohesion: 0.31
Nodes (3): DailyDeal, Rules, TodayKey

### Community 119 - "PlayerSetup"
Cohesion: 0.30
Nodes (6): PlayerSetup, SavedGame, SaveGame, Exists, FilePath, PathOverride

### Community 120 - "RectTransform"
Cohesion: 0.07
Nodes (8): UI / DA "Salon", AnimatedBackground, PileSlot, ContractRow, GameScreenLayout, Root, StepPill, PlayerPill

### Community 122 - "PlayerModel"
Cohesion: 0.11
Nodes (14): PlayerModel, CurrentLevel, Hand, HasLaidDownThisRound, Index, IsAI, IsHandEmpty, IsSkipped (+6 more)

### Community 124 - "Regles (Core)"
Cohesion: 0.18
Nodes (6): PlayerProfile, GamesPlayed, GamesWon, HasCustomName, Name, RoundsWon

### Community 125 - "UIIcon"
Cohesion: 0.15
Nodes (13): UIIcon, Cards, Check, Clock, Close, Copy, Edit, Lock (+5 more)

### Community 128 - "CardColor"
Cohesion: 0.09
Nodes (17): CardModel, IsAction, IsWild, CardColor, Blue, Green, Orange, Purple (+9 more)

### Community 129 - "Kind"
Cohesion: 0.25
Nodes (8): Kind, AddToMeld, Discard, DrawFromDeck, DrawFromDiscard, LayDown, SkipPhase, StartNextRound

### Community 137 - "PanelState"
Cohesion: 0.33
Nodes (6): PanelState, Busy, ClientLobby, Home, HostLobby, Join

### Community 149 - "GameBootstrapper"
Cohesion: 0.09
Nodes (3): GameBootstrapper, LobbySeatView, 15.2 Scripts AUTO-GÉNÉRÉS au runtime (ne pas les ajouter à la main)

## Knowledge Gaps
- **267 isolated node(s):** `AutoStartNextRound`, `State`, `Rules`, `LastRoundWinner`, `Players` (+262 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 673 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **72 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `OnlineSession` connect `OnlineSession` to `LevelUp.Utils`, `CardModel`, `AIPlayer`, `MonoBehaviour`, `MatchRules`, `CLAUDE.md - Brief d'execution optimise pour Claude`, `BotBrain`?**
  _High betweenness centrality (0.043) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `GameInputController` (e.g. with `2) Contexte technique du repo (a respecter)` and `9) Prochaines taches recommandees (ordre d'execution)`) actually correct?**
  _`GameInputController` has 4 INFERRED edges - model-reasoned connections that need verification._
- **What connects `AutoStartNextRound`, `State`, `Rules` to the rest of the system?**
  _267 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Vector2` be split into smaller, more focused modules?**
  _Cohesion score 0.12605042016806722 - nodes in this community are weakly interconnected._
- **Why does `PlayerModel` connect `PlayerModel` to `GameInputController`, `LevelUp.Utils`, `CardModel`, `AIPlayer`, `MatchRulesTests`, `MonoBehaviour`, `DeckManager`, `LevelRequirement`, `UISkin.Text`, `RoundSummaryView`, `NetworkManager`, `PlayerModel`, `GameBootstrapper`, `GameInputController.ApplyPreferredSort`, `EventBus.Subscribe`, `Test`, `BotBrain`?**
  _High betweenness centrality (0.040) - this node is a cross-community bridge._
- **Should `LevelUp.Utils` be split into smaller, more focused modules?**
  _Cohesion score 0.0518194140339902 - nodes in this community are weakly interconnected._
- **Why does `GameManager` connect `AIPlayer` to `LevelUp.Utils`, `CardModel`, `AudioManager`, `NetworkManager`, `PlayerModel`, `GameBootstrapper`, `Test`, `BotSimulationTests.PlayGame`, `OnlineSession`, `BotBrain`, `MatchRulesTests`, `DevAutoScreenshot`, `GameManager.ExecuteCommand`, `MatchRules`, `GameInputController.ApplyPreferredSort`, `GameInputController`, `MonoBehaviour`, `DiscardPileView`, `RoundSummaryView`, `GameOverEvent`, `SelectionStatusView`, `AnimationController`, `TurnStartedEvent`, `PlayerSetup`, `PlayerModel`?**
  _High betweenness centrality (0.039) - this node is a cross-community bridge._