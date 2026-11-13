# Graph Report - LevelUp  (2026-11-13)

## Corpus Check
- 99 files · ~106,198 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 235 file(s) not represented in the graph (top: .meta 166, .asset 34, .shader 13)
- WARNING: 235 of 343 files seen (69%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 2437 nodes · 7666 edges · 157 communities (66 shown, 91 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 748 edges (avg confidence: 0.87)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `08dfb42c`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Vector2
- LevelUp.Utils
- CardModel
- UISkin.Text
- Image
- AnimationController
- manifest.json
- com.unity.2d.common@8.1.0
- GameSession
- IEnumerator
- AudioManager
- com.unity.render-pipelines.core
- NetworkManager
- LevelProgressView
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
- CardFactory.Y
- OnlineSession.BuildAndBroadcastStart
- OnlineSession
- BotBrain
- com.unity.2d.tilemap
- com.unity.ugui@1.0.0
- DeckManager
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
- Meld
- GameSettings
- UISkin
- com.unity.2d.spriteshape
- com.unity.nuget.mono-cecil@1.10.1
- UITween.FadeTo
- CanvasGroup
- com.unity.modules.unitywebrequesttexture@1.0.0
- Test
- EventBus.Subscribe
- GameOverCelebration
- AndroidBuilder
- GameSession.IsLocalHuman
- MainMenuController
- CommandError
- Transform
- GameInputController
- GameObject
- TurnManager
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
- MeldGroupView
- DiscardPileView
- RoundSummaryView
- GameOverEvent
- Meld
- 15.2 Scripts AUTO-GÉNÉRÉS au runtime (ne pas les ajouter à la main)
- SelectionStatusView
- FastBufferReader
- TurnStartedEvent
- DailyDeal
- PlayerSetup
- RectTransform
- ConfirmDialog
- RulesPanel
- Regles (Core)
- UIIcon
- PointerInput
- MatchSetupPanel
- CardColor
- Kind
- OpponentMovesView
- GameManager
- PanelState
- GameBootstrapper

## God Nodes (most connected - your core abstractions)
1. `GameInputController` - 97 edges
2. `OnlineSession` - 78 edges
3. `GameBootstrapper` - 76 edges
4. `HandView` - 76 edges
5. `GameManager` - 75 edges
6. `LevelUp.Core` - 66 edges
7. `GameScreenLayout` - 62 edges
8. `PlayerModel` - 59 edges
9. `CardView` - 55 edges
10. `UISkin` - 52 edges

## Surprising Connections (you probably didn't know these)
- `3.6 TurnBannerView (bannière temporaire "Tour de X")` --references--> `TurnStartedEvent`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/Core/GameEvents.cs
- `5. Références à assigner sur GameBootstrapper` --references--> `GameBootstrapper`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/GameBootstrapper.cs
- `3.5.a HandContainer (enfant de PlayerHand)` --references--> `HandView`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `Ce qui tourne vraiment` --references--> `AIPlayer`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/AI/AIPlayer.cs
- `15.2 Scripts AUTO-GÉNÉRÉS au runtime (ne pas les ajouter à la main)` --references--> `AIPlayer`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/AI/AIPlayer.cs

## Import Cycles
- None detected.

## Communities (157 total, 91 thin omitted)

### Community 0 - "Vector2"
Cohesion: 0.08
Nodes (8): HandView, CardCount, CardPrefab, IsAnimatingDeal, IsAnimatingDraw, IsReorderDragging, SelectedCard, SelectedCards

### Community 1 - "LevelUp.Utils"
Cohesion: 0.06
Nodes (10): LevelUp.Core, LevelUp.Tests, LevelUp.Utils, LevelUp.EditorTools, LevelUp.UI, LevelUp.AI, LevelUp.Network, NetworkMode (+2 more)

### Community 2 - "CardModel"
Cohesion: 0.13
Nodes (3): CardExtensions, LevelRequirement, LevelValidator

### Community 3 - "UISkin.Text"
Cohesion: 0.14
Nodes (4): OnlinePanel, IsVisible, LastCode, UIControls

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

### Community 12 - "NetworkManager"
Cohesion: 0.18
Nodes (5): NetworkManager, IsPassScreenActive, 15.1 Scripts à placer MANUELLEMENT dans la scène `Game.unity`, Scripts racine (GameObjects vides, pas sous Canvas), Scripts UI (enfants du Canvas)

### Community 13 - "LevelProgressView"
Cohesion: 0.06
Nodes (32): 0. Pré-requis : Canvas Scaler, 10. Méthode pour ajuster visuellement dans Unity, 11. Test et validation, 12. Troubleshooting, 13. Ajustements mobiles (optionnel), 14. Couleurs de référence (cohérence visuelle), 15.3 Scripts sur PREFABS (pas dans la scène), 15.5 Hiérarchie finale complète (copier-coller mental) (+24 more)

### Community 14 - "com.unity.mathematics@1.2.6"
Cohesion: 0.08
Nodes (28): dependencies, depth, source, url, version, dependencies, depth, source (+20 more)

### Community 15 - "PlayerModel"
Cohesion: 0.16
Nodes (8): GameCommandExecutor, CommandResult, DrawnCard, Error, Message, RoundEnded, Success, WinnerOverride

### Community 16 - "MonoBehaviour"
Cohesion: 0.09
Nodes (12): Ce qui tourne vraiment, AppLifecycle, ConnectionOverlay, IsVisible, PauseMenuController, IsPaused, PauseAllowed, TargetChooser (+4 more)

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

### Community 30 - "OnlineSession"
Cohesion: 0.09
Nodes (9): OnlineSession, CurrentLobby, DeviceToken, IsActive, IsSessionHost, JoinCode, LastJoinCode, LobbyHumanCount (+1 more)

### Community 31 - "BotBrain"
Cohesion: 0.14
Nodes (3): BotBrain, Difficulty, Me

### Community 32 - "com.unity.2d.tilemap"
Cohesion: 0.12
Nodes (18): dependencies, depth, dependencies, depth, source, url, version, source (+10 more)

### Community 34 - "com.unity.ugui@1.0.0"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, url, version, depth, source, version (+9 more)

### Community 36 - "AIPlayer"
Cohesion: 0.12
Nodes (7): IA (`Scripts/AI`), AIDifficulty, Easy, Hard, Normal, AIPlayer, PlayerIndex

### Community 38 - "GameManager.ExecuteCommand"
Cohesion: 0.07
Nodes (25): GameCommandCodec, AddToMeldCommand, Card, MeldIndex, MeldOwnerIndex, PlayerIndex, DiscardCommand, Card (+17 more)

### Community 39 - "DeckManager"
Cohesion: 0.15
Nodes (6): ActionCardHandler, DeckManager, CardsPerPlayer, DiscardPileCount, DrawPileCount, SkipStackingTests

### Community 40 - "LevelRequirement"
Cohesion: 0.17
Nodes (6): ContractCatalog, ContractPlan, IsRandom, ContractProgress, Entry, IsMet

### Community 44 - "MatchRules"
Cohesion: 0.10
Nodes (16): MatchRules, Draw2Count, HandSize, HasTurnTimer, IsClassic, JokerCount, LevelsToWin, RandomContracts (+8 more)

### Community 45 - "CLAUDE.md - Brief d'execution optimise pour Claude"
Cohesion: 0.14
Nodes (13): 1) Role de Claude dans ce projet, 3) Objectif produit global, 4) Backlog priorise (spec fonctionnelle), 6) Definition of Done (DoD), 7) Format de reponse attendu de Claude, 8) Regles de priorisation quand il y a ambiguite, 9) Prochaines taches recommandees (ordre d'execution), A. UX & Gameplay (zero friction) - PRIORITE P0 (+5 more)

### Community 46 - "com.unity.inputsystem"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.inputsystem

### Community 47 - "Meld"
Cohesion: 0.13
Nodes (12): GameConfig, CardMaxValue, CardMinValue, CardsPerPlayer, DeckSize, MaxPlayers, MinPlayers, Meld (+4 more)

### Community 48 - "GameSettings"
Cohesion: 0.07
Nodes (26): GameSettings, BotDelayFactor, BotSpeed, CardBack, ColorblindMode, Difficulty, Felt, HandSize (+18 more)

### Community 49 - "UISkin"
Cohesion: 0.07
Nodes (12): UIFactory, CircleSprite, RingSprite, RoundedSprite, SmallPillSprite, SoftShadowSprite, UISkin, ActionBand (+4 more)

### Community 50 - "com.unity.2d.spriteshape"
Cohesion: 0.14
Nodes (14): dependencies, depth, source, url, version, dependencies, depth, source (+6 more)

### Community 51 - "com.unity.nuget.mono-cecil@1.10.1"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, url, version, dependencies, depth, source (+5 more)

### Community 53 - "CanvasGroup"
Cohesion: 0.14
Nodes (4): PileSlot, TextMirror, LayDownChooser, IsOpen

### Community 55 - "Test"
Cohesion: 0.14
Nodes (3): CardExtensionsTests, LayDownOptionsTests, MeldTests

### Community 58 - "GameOverCelebration"
Cohesion: 0.24
Nodes (5): GameStartedEvent, GameOverCelebration, PlayerNames, Players, RoundsPlayed

### Community 60 - "GameSession.IsLocalHuman"
Cohesion: 0.39
Nodes (3): CardOrigin, CardOrigins, Provider

### Community 61 - "MainMenuController"
Cohesion: 0.15
Nodes (4): MainMenuController, IsVisible, MatchSetup, Root

### Community 62 - "CommandError"
Cohesion: 0.12
Nodes (15): CommandError, ActionCardNotPickable, AlreadyLaidDown, CardDoesNotFit, CardNotInHand, ContractMismatch, EmptyDiscard, InvalidMeld (+7 more)

### Community 63 - "Transform"
Cohesion: 0.12
Nodes (8): OptionsPanel, UIInputs, TextWeight, Bold, Display, Medium, Regular, SemiBold

### Community 65 - "GameInputController"
Cohesion: 0.09
Nodes (8): InvalidActionEvent, GameInputController, ActionsLocked, CurrentPlayerIndex, DpiScale, DragUpThreshold, IsLocalTurn, ReorderDragThreshold

### Community 66 - "GameObject"
Cohesion: 0.15
Nodes (4): TweenRunner, UITween, Runner, 15.4 Helpers STATIQUES (aucun GameObject)

### Community 67 - "TurnManager"
Cohesion: 0.17
Nodes (9): 2) Contexte technique du repo (a respecter), 5) Contraintes de mise en oeuvre, TurnManager, CurrentPhase, CurrentPlayer, CurrentPlayerIndex, EventBus, Muted (+1 more)

### Community 68 - "CardView"
Cohesion: 0.08
Nodes (9): CardFlights, CardView, CardModel, IsFaceUp, IsHovered, IsSelected, RectTransform, CardViewPool (+1 more)

### Community 104 - "MeldGroupView"
Cohesion: 0.10
Nodes (5): MeldGroupView, MeldType, OwnerIndex, TableView, ContentScale

### Community 105 - "DiscardPileView"
Cohesion: 0.21
Nodes (3): DiscardPileView, PileCount, PileSpacing

### Community 108 - "GameOverEvent"
Cohesion: 0.19
Nodes (3): GameOverEvent, RoundEndedEvent, TurnTimer

### Community 109 - "Meld"
Cohesion: 0.21
Nodes (4): LayDownLevelCommand, Melds, PlayerIndex, GameCommandExecutorTests

### Community 117 - "TurnStartedEvent"
Cohesion: 0.18
Nodes (14): ActionCardPlayedEvent, CardAddedToMeldEvent, CardDiscardedEvent, CardDrawnEvent, DeckChangedEvent, ForcedDrawEvent, HandChangedEvent, LevelCompletedEvent (+6 more)

### Community 118 - "DailyDeal"
Cohesion: 0.31
Nodes (3): DailyDeal, Rules, TodayKey

### Community 119 - "PlayerSetup"
Cohesion: 0.35
Nodes (6): PlayerSetup, SavedGame, SaveGame, Exists, FilePath, PathOverride

### Community 120 - "RectTransform"
Cohesion: 0.09
Nodes (7): UI / DA "Salon", AnimatedBackground, ContractRow, GameScreenLayout, Root, StepPill, SafeAreaFitter

### Community 124 - "Regles (Core)"
Cohesion: 0.18
Nodes (10): 10) Carte du code (a lire AVANT d'explorer : evite de tout relire), En ligne (`Scripts/Network`), Regles (Core), Verifier sans appareil (editeur Unity FERME, sinon le batchmode echoue), PlayerProfile, GamesPlayed, GamesWon, HasCustomName (+2 more)

### Community 125 - "UIIcon"
Cohesion: 0.15
Nodes (13): UIIcon, Cards, Check, Clock, Close, Copy, Edit, Lock (+5 more)

### Community 126 - "PointerInput"
Cohesion: 0.39
Nodes (4): PointerInput, LastActivityFrame, Override, PointerState

### Community 128 - "CardColor"
Cohesion: 0.05
Nodes (33): CardModel, IsAction, IsWild, CardColor, Blue, Green, Orange, Purple (+25 more)

### Community 129 - "Kind"
Cohesion: 0.25
Nodes (8): Kind, AddToMeld, Discard, DrawFromDeck, DrawFromDiscard, LayDown, SkipPhase, StartNextRound

### Community 133 - "GameManager"
Cohesion: 0.07
Nodes (24): GameManager, AutoStartNextRound, Config, DeckManager, Executor, LastRoundWinner, Players, RoundNumber (+16 more)

### Community 137 - "PanelState"
Cohesion: 0.33
Nodes (6): PanelState, Busy, ClientLobby, Home, HostLobby, Join

### Community 149 - "GameBootstrapper"
Cohesion: 0.12
Nodes (3): GameBootstrapper, LobbySeatView, LobbyViewModel

## Knowledge Gaps
- **256 isolated node(s):** `Easy`, `Normal`, `Hard`, `PlayerIndex`, `PlayerIndex` (+251 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 673 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **91 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `GameBootstrapper` connect `GameBootstrapper` to `Vector2`, `LevelUp.Utils`, `CardModel`, `GameBootstrapper.InitializePauseAndGameOver`, `UISkin.Text`, `GameManager`, `AnimationController`, `NetworkManager`, `LevelProgressView`, `MonoBehaviour`, `HUDView`, `OnlineSession`, `GameScreenLayout.RefreshContract`, `AIPlayer`, `DevAutoScreenshot`, `GameManager.ExecuteCommand`, `GameInputController.ApplyPreferredSort`, `GameSession.IsLocalHuman`, `GameInputController`, `CardView`, `MeldGroupView`, `DiscardPileView`, `GameOverEvent`, `15.2 Scripts AUTO-GÉNÉRÉS au runtime (ne pas les ajouter à la main)`, `GameBootstrapper.HandleResumeSolo`, `TurnStartedEvent`, `DailyDeal`, `RectTransform`?**
  _High betweenness centrality (0.048) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `GameInputController` (e.g. with `2) Contexte technique du repo (a respecter)` and `9) Prochaines taches recommandees (ordre d'execution)`) actually correct?**
  _`GameInputController` has 4 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Easy`, `Normal`, `Hard` to the rest of the system?**
  _256 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Vector2` be split into smaller, more focused modules?**
  _Cohesion score 0.08246753246753247 - nodes in this community are weakly interconnected._
- **Why does `Ce qui tourne vraiment` connect `MonoBehaviour` to `Vector2`, `UISkin.Text`, `OpponentMovesView`, `GameManager`, `AnimationController`, `AudioManager`, `NetworkManager`, `GameBootstrapper`, `HUDView`, `OnlineSession`, `AIPlayer`, `GameSettings`, `UITween.FadeTo`, `CanvasGroup`, `GameOverCelebration`, `MainMenuController`, `Transform`, `GameInputController`, `CardView`, `LevelProgressView`, `BalatroEffects`, `MeldGroupView`, `DiscardPileView`, `RoundSummaryView`, `SelectionStatusView`, `RectTransform`, `RulesPanel`, `Regles (Core)`, `MatchSetupPanel`?**
  _High betweenness centrality (0.039) - this node is a cross-community bridge._
- **Should `LevelUp.Utils` be split into smaller, more focused modules?**
  _Cohesion score 0.06304145008663202 - nodes in this community are weakly interconnected._
- **Why does `OnlineSession` connect `OnlineSession` to `LevelUp.Utils`, `CardModel`, `UISkin.Text`, `GameManager`, `MatchRules`, `CLAUDE.md - Brief d'execution optimise pour Claude`, `MonoBehaviour`, `FastBufferReader`, `GameBootstrapper.HandleResumeSolo`, `OnlineSession.HostAsync`, `OnlineSession.BuildAndBroadcastStart`?**
  _High betweenness centrality (0.039) - this node is a cross-community bridge._