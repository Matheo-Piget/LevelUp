# Graph Report - LevelUp  (2026-11-12)

## Corpus Check
- 94 files · ~102,474 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 230 file(s) not represented in the graph (top: .meta 161, .asset 34, .shader 13)
- WARNING: 230 of 333 files seen (69%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 2367 nodes · 7395 edges · 134 communities (63 shown, 71 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 694 edges (avg confidence: 0.87)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `254094d9`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- LevelUp.Utils
- CardModel
- UISkin.Text
- RectTransform
- RectTransform
- manifest.json
- com.unity.2d.common@8.1.0
- GameSession.IsLocalHuman
- AudioManager
- com.unity.render-pipelines.core
- TurnStartedEvent
- LevelProgressView
- com.unity.mathematics@1.2.6
- GameManager.ExecuteCommand
- Ce qui tourne vraiment
- dependencies
- com.unity.modules.physics@1.0.0
- com.unity.modules.jsonserialize@1.0.0
- com.unity.ext.nunit
- com.unity.modules.imgui@1.0.0
- EventBus.Subscribe
- com.unity.nuget.newtonsoft-json
- dependencies
- HUDView
- com.unity.modules.unitywebrequest@1.0.0
- packages-lock.json
- TextMeshProUGUI
- FastBufferReader
- OnlineSession
- BotBrain
- com.unity.2d.tilemap
- IGameCommand
- com.unity.ugui@1.0.0
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
- UITween.FadeTo
- GameManager
- com.unity.modules.unitywebrequesttexture@1.0.0
- Test
- HandView
- EventBus.Subscribe
- CanvasGroup
- 10) Carte du code (a lire AVANT d'explorer : evite de tout relire)
- PlayerSetup
- MainMenuController
- Color
- Transform
- GameInputController
- GameObject
- TurnManager
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
- GameBootstrapper
- LevelProgressView
- BalatroEffects
- CardView
- PlayerModel
- RoundSummaryView
- CardDrawnEvent
- SelectionStatusView
- MatchRulesTests
- GameEvents.cs
- DailyDeal
- MonoBehaviour
- GameScreenLayout
- GameConfig
- CardType
- PlayerProfile
- UIIcon
- TurnTimer
- Haptics
- UI / DA "Salon"
- Kind
- RulesPanel.Setup
- IReadOnlyList
- PanelState

## God Nodes (most connected - your core abstractions)
1. `GameInputController` - 91 edges
2. `OnlineSession` - 78 edges
3. `GameBootstrapper` - 75 edges
4. `HandView` - 73 edges
5. `LevelUp.Core` - 64 edges
6. `GameScreenLayout` - 62 edges
7. `GameManager` - 61 edges
8. `UISkin` - 52 edges
9. `BotBrain` - 48 edges
10. `PlayerModel` - 47 edges

## Surprising Connections (you probably didn't know these)
- `5. Références à assigner sur GameBootstrapper` --references--> `GameBootstrapper`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/GameBootstrapper.cs
- `3.5.a HandContainer (enfant de PlayerHand)` --references--> `HandView`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `3.6 TurnBannerView (bannière temporaire "Tour de X")` --references--> `TurnStartedEvent`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/Core/GameEvents.cs
- `5) Contraintes de mise en oeuvre` --references--> `LevelValidator`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/Core/LevelValidator.cs
- `Regles (Core)` --references--> `LevelValidator`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/Core/LevelValidator.cs

## Import Cycles
- None detected.

## Communities (134 total, 71 thin omitted)

### Community 1 - "LevelUp.Utils"
Cohesion: 0.06
Nodes (18): LevelUp.Core, LevelUp.Tests, LevelUp.Utils, LevelUp.EditorTools, LevelUp.UI, LevelUp.AI, LevelUp.Network, MeldRequirement (+10 more)

### Community 2 - "CardModel"
Cohesion: 0.10
Nodes (12): CardExtensions, CardModel, IsAction, IsWild, CardColor, Blue, Green, Orange (+4 more)

### Community 3 - "UISkin.Text"
Cohesion: 0.13
Nodes (4): OnlinePanel, IsVisible, LastCode, UIInputs

### Community 5 - "RectTransform"
Cohesion: 0.16
Nodes (4): AnimationController, IsAnimating, MatchSetupPanel, IsVisible

### Community 6 - "manifest.json"
Cohesion: 0.17
Nodes (11): com.unity.modules.jsonserialize@1.0.0, com.unity.modules.umbra@1.0.0, com.unity.render-pipelines.universal@14.0.11, com.unity.textmeshpro@3.0.9, com.unity.visualscripting@1.9.4, com.unity.modules.jsonserialize, com.unity.modules.umbra, com.unity.modules.unityanalytics (+3 more)

### Community 7 - "com.unity.2d.common@8.1.0"
Cohesion: 0.05
Nodes (44): dependencies, depth, source, url, version, dependencies, depth, source (+36 more)

### Community 8 - "GameSession.IsLocalHuman"
Cohesion: 0.12
Nodes (6): GameSession, Contracts, IsHost, IsOnline, LocalPlayerIndex, Rules

### Community 10 - "AudioManager"
Cohesion: 0.15
Nodes (3): AudioManager, Instance, Generator

### Community 11 - "com.unity.render-pipelines.core"
Cohesion: 0.07
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 12 - "TurnStartedEvent"
Cohesion: 0.10
Nodes (13): NetworkManager, IsPassScreenActive, NetworkMode, PassAndPlay, SinglePlayer, 15.1 Scripts à placer MANUELLEMENT dans la scène `Game.unity`, 15.3 Scripts sur PREFABS (pas dans la scène), 15.5 Hiérarchie finale complète (copier-coller mental) (+5 more)

### Community 13 - "LevelProgressView"
Cohesion: 0.07
Nodes (27): 0. Pré-requis : Canvas Scaler, 10. Méthode pour ajuster visuellement dans Unity, 11. Test et validation, 12. Troubleshooting, 13. Ajustements mobiles (optionnel), 14. Couleurs de référence (cohérence visuelle), 1. Vue d'ensemble du layout, 2. Hiérarchie cible (+19 more)

### Community 14 - "com.unity.mathematics@1.2.6"
Cohesion: 0.08
Nodes (28): dependencies, depth, source, url, version, dependencies, depth, source (+20 more)

### Community 15 - "GameManager.ExecuteCommand"
Cohesion: 0.18
Nodes (9): Regles (Core), GameCommandExecutor, CommandResult, DrawnCard, Message, RoundEnded, Success, WinnerOverride (+1 more)

### Community 16 - "Ce qui tourne vraiment"
Cohesion: 0.13
Nodes (7): Ce qui tourne vraiment, AppLifecycle, PauseMenuController, IsPaused, PauseAllowed, UIBackStack, HasOpenPanel

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

### Community 28 - "TextMeshProUGUI"
Cohesion: 0.10
Nodes (8): PileSlot, ContractRow, StepPill, TextMirror, LayDownChooser, IsOpen, TargetChooser, IsOpen

### Community 30 - "OnlineSession"
Cohesion: 0.09
Nodes (9): OnlineSession, CurrentLobby, DeviceToken, IsActive, IsSessionHost, JoinCode, LastJoinCode, LobbyHumanCount (+1 more)

### Community 31 - "BotBrain"
Cohesion: 0.11
Nodes (7): BotBrain, Difficulty, Me, DeckManager, CardsPerPlayer, DiscardPileCount, DrawPileCount

### Community 32 - "com.unity.2d.tilemap"
Cohesion: 0.12
Nodes (18): dependencies, depth, dependencies, depth, source, url, version, source (+10 more)

### Community 33 - "IGameCommand"
Cohesion: 0.09
Nodes (18): GameCommandCodec, AddToMeldCommand, Card, MeldIndex, MeldOwnerIndex, PlayerIndex, DiscardCommand, Card (+10 more)

### Community 34 - "com.unity.ugui@1.0.0"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, url, version, depth, source, version (+9 more)

### Community 36 - "AIPlayer"
Cohesion: 0.12
Nodes (8): IA (`Scripts/AI`), AIDifficulty, Easy, Hard, Normal, AIPlayer, PlayerIndex, AIThinkingEvent

### Community 38 - "GameManager.StartGame"
Cohesion: 0.18
Nodes (7): DrawFromDeckCommand, PlayerIndex, SkipPhaseCommand, PhaseToSkip, PlayerIndex, ContractAndReplayTests, CoreGuardsTests

### Community 40 - "LevelRequirement"
Cohesion: 0.17
Nodes (6): ContractCatalog, ContractPlan, IsRandom, ContractProgress, Entry, IsMet

### Community 44 - "MatchRules"
Cohesion: 0.07
Nodes (17): MatchRules, Draw2Count, HandSize, HasTurnTimer, IsClassic, JokerCount, LevelsToWin, RandomContracts (+9 more)

### Community 45 - "CLAUDE.md - Brief d'execution optimise pour Claude"
Cohesion: 0.13
Nodes (14): 1) Role de Claude dans ce projet, 3) Objectif produit global, 4) Backlog priorise (spec fonctionnelle), 5) Contraintes de mise en oeuvre, 6) Definition of Done (DoD), 7) Format de reponse attendu de Claude, 8) Regles de priorisation quand il y a ambiguite, 9) Prochaines taches recommandees (ordre d'execution) (+6 more)

### Community 46 - "com.unity.inputsystem"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.inputsystem

### Community 47 - "CardModel"
Cohesion: 0.23
Nodes (3): LevelDefinition, LevelRequirement, LevelValidator

### Community 48 - "GameSettings"
Cohesion: 0.07
Nodes (26): GameSettings, BotDelayFactor, BotSpeed, CardBack, ColorblindMode, Difficulty, Felt, HandSize (+18 more)

### Community 49 - "UISkin"
Cohesion: 0.05
Nodes (19): AnimatedBackground, UIFactory, CircleSprite, RingSprite, RoundedSprite, SmallPillSprite, SoftShadowSprite, TextWeight (+11 more)

### Community 50 - "com.unity.2d.spriteshape"
Cohesion: 0.14
Nodes (14): dependencies, depth, source, url, version, dependencies, depth, source (+6 more)

### Community 51 - "com.unity.nuget.mono-cecil@1.10.1"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, url, version, dependencies, depth, source (+5 more)

### Community 53 - "GameManager"
Cohesion: 0.14
Nodes (11): GameManager, AutoStartNextRound, Config, DeckManager, Executor, LastRoundWinner, Players, RoundNumber (+3 more)

### Community 55 - "Test"
Cohesion: 0.08
Nodes (14): LayDownLevelCommand, Melds, PlayerIndex, Meld, Cards, OwnerIndex, Type, BotBrainTests (+6 more)

### Community 56 - "HandView"
Cohesion: 0.11
Nodes (8): HandView, CardCount, CardPrefab, IsAnimatingDeal, IsAnimatingDraw, IsReorderDragging, SelectedCard, SelectedCards

### Community 58 - "CanvasGroup"
Cohesion: 0.19
Nodes (4): GameOverCelebration, PlayerNames, Players, RoundsPlayed

### Community 59 - "10) Carte du code (a lire AVANT d'explorer : evite de tout relire)"
Cohesion: 0.21
Nodes (6): 10) Carte du code (a lire AVANT d'explorer : evite de tout relire), En ligne (`Scripts/Network`), Verifier sans appareil (editeur Unity FERME, sinon le batchmode echoue), AndroidBuilder, LobbyInfo, LobbyMember

### Community 60 - "PlayerSetup"
Cohesion: 0.16
Nodes (7): PlayerSetup, SavedGame, SaveGame, Exists, FilePath, PathOverride, OnlineStartData

### Community 61 - "MainMenuController"
Cohesion: 0.14
Nodes (4): MainMenuController, IsVisible, MatchSetup, Root

### Community 65 - "GameInputController"
Cohesion: 0.09
Nodes (7): GameInputController, ActionsLocked, CurrentPlayerIndex, DpiScale, DragUpThreshold, IsLocalTurn, ReorderDragThreshold

### Community 66 - "GameObject"
Cohesion: 0.15
Nodes (4): TweenRunner, UITween, Runner, 15.4 Helpers STATIQUES (aucun GameObject)

### Community 68 - "TurnManager"
Cohesion: 0.12
Nodes (12): 2) Contexte technique du repo (a respecter), TurnManager, CurrentPhase, CurrentPlayer, CurrentPlayerIndex, TurnPhase, AddToMelds, Discard (+4 more)

### Community 101 - "GameBootstrapper"
Cohesion: 0.10
Nodes (4): GameBootstrapper, LobbySeatView, LobbyViewModel, 15.2 Scripts AUTO-GÉNÉRÉS au runtime (ne pas les ajouter à la main)

### Community 104 - "CardView"
Cohesion: 0.05
Nodes (17): CardFlights, CardOrigin, CardOrigins, Provider, CardView, CardModel, IsFaceUp, IsHovered (+9 more)

### Community 105 - "PlayerModel"
Cohesion: 0.13
Nodes (12): PlayerModel, CurrentLevel, Hand, HasLaidDownThisRound, Index, IsAI, IsHandEmpty, IsSkipped (+4 more)

### Community 109 - "CardDrawnEvent"
Cohesion: 0.19
Nodes (4): CardDiscardedEvent, CardDrawnEvent, DeckChangedEvent, OpponentMovesView

### Community 117 - "GameEvents.cs"
Cohesion: 0.19
Nodes (15): ActionCardPlayedEvent, CardAddedToMeldEvent, ForcedDrawEvent, GameOverEvent, GameStartedEvent, HandChangedEvent, InvalidActionEvent, LevelCompletedEvent (+7 more)

### Community 118 - "DailyDeal"
Cohesion: 0.31
Nodes (3): DailyDeal, Rules, TodayKey

### Community 119 - "MonoBehaviour"
Cohesion: 0.24
Nodes (4): ConfirmDialog, ConnectionOverlay, IsVisible, UIInputBlocker

### Community 121 - "GameConfig"
Cohesion: 0.22
Nodes (8): GameConfig, CardMaxValue, CardMinValue, CardsPerPlayer, DeckSize, LevelDefinitions, MaxPlayers, MinPlayers

### Community 122 - "CardType"
Cohesion: 0.29
Nodes (6): CardType, Draw2, Normal, Skip, Wild, WildDraw2

### Community 124 - "PlayerProfile"
Cohesion: 0.18
Nodes (6): PlayerProfile, GamesPlayed, GamesWon, HasCustomName, Name, RoundsWon

### Community 125 - "UIIcon"
Cohesion: 0.15
Nodes (13): UIIcon, Cards, Check, Clock, Close, Copy, Edit, Lock (+5 more)

### Community 129 - "Kind"
Cohesion: 0.25
Nodes (8): Kind, AddToMeld, Discard, DrawFromDeck, DrawFromDiscard, LayDown, SkipPhase, StartNextRound

### Community 137 - "PanelState"
Cohesion: 0.33
Nodes (6): PanelState, Busy, ClientLobby, Home, HostLobby, Join

## Knowledge Gaps
- **239 isolated node(s):** `Instance`, `DpiScale`, `ReorderDragThreshold`, `DragUpThreshold`, `ActionsLocked` (+234 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 634 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **71 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `OnlineSession` connect `OnlineSession` to `LevelUp.Utils`, `CardModel`, `UISkin.Text`, `IGameCommand`, `CLAUDE.md - Brief d'execution optimise pour Claude`, `CardModel`, `Ce qui tourne vraiment`, `GameManager`, `MonoBehaviour`, `10) Carte du code (a lire AVANT d'explorer : evite de tout relire)`, `PlayerSetup`, `FastBufferReader`?**
  _High betweenness centrality (0.069) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `GameInputController` (e.g. with `2) Contexte technique du repo (a respecter)` and `9) Prochaines taches recommandees (ordre d'execution)`) actually correct?**
  _`GameInputController` has 4 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Instance`, `DpiScale`, `ReorderDragThreshold` to the rest of the system?**
  _239 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Vector2` be split into smaller, more focused modules?**
  _Cohesion score 0.14461538461538462 - nodes in this community are weakly interconnected._
- **Why does `Ce qui tourne vraiment` connect `Ce qui tourne vraiment` to `RulesPanel.Setup`, `UISkin.Text`, `RectTransform`, `AudioManager`, `TurnStartedEvent`, `HUDView`, `TextMeshProUGUI`, `OnlineSession`, `AIPlayer`, `GameSettings`, `UISkin`, `UITween.FadeTo`, `GameManager`, `HandView`, `CanvasGroup`, `10) Carte du code (a lire AVANT d'explorer : evite de tout relire)`, `MainMenuController`, `Transform`, `GameInputController`, `GameBootstrapper`, `LevelProgressView`, `BalatroEffects`, `CardView`, `RoundSummaryView`, `CardDrawnEvent`, `SelectionStatusView`, `MonoBehaviour`, `GameScreenLayout`?**
  _High betweenness centrality (0.056) - this node is a cross-community bridge._
- **Should `LevelUp.Utils` be split into smaller, more focused modules?**
  _Cohesion score 0.05824271966004249 - nodes in this community are weakly interconnected._
- **Why does `HandView` connect `HandView` to `Vector2`, `LevelUp.Utils`, `CardModel`, `RectTransform`, `IReadOnlyList`, `GameSession.IsLocalHuman`, `GameInputController.ExecuteLayDown`, `TurnStartedEvent`, `LevelProgressView`, `Ce qui tourne vraiment`, `EventBus.Subscribe`, `TextMeshProUGUI`, `BotBrain`, `CardModel`, `Transform`, `GameInputController`, `GameObject`, `TurnManager`, `GameBootstrapper`, `BalatroEffects`, `GameSession.GetLocalPlayer`, `SelectionStatusView`, `GameInputController.OnPointerUp`, `GameBootstrapper`, `MonoBehaviour`?**
  _High betweenness centrality (0.043) - this node is a cross-community bridge._