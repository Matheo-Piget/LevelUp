# Graph Report - LevelUp  (2026-11-13)

## Corpus Check
- 124 files · ~127,760 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 259 file(s) not represented in the graph (top: .meta 190, .asset 34, .shader 13)
- WARNING: 259 of 394 files seen (66%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 2765 nodes · 8727 edges · 155 communities (77 shown, 78 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 862 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `32b2b8a3`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- unityengine
- CardModel
- IconBuilder.Generate
- HandView
- manifest.json
- com.unity.2d.common@8.1.0
- TutorialDirector
- LevelUp.Core
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
- Regles (Core)
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
- LevelUp.Utils
- GameManager.StartGame
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
- Action
- ChallengeGenerator.Build
- com.unity.modules.unitywebrequesttexture@1.0.0
- MainMenuController
- DeckManager
- GameManager
- GameInputController
- CLAUDE.md - Brief d'execution optimise pour Claude
- RoundSummaryView
- CommandError
- Constants.cs
- RectTransform
- 3. Configuration détaillée par GameObject
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
- LevelProgressView
- BalatroEffects
- UIIcon
- DiscardPileView
- TextWeight
- SocialProtocol
- AndroidBuilder.cs
- OnlinePanel
- OnlineSession.cs
- SelectionStatusView
- RoundStartedEvent
- BotSimulationTests.PlayGame
- CardModel
- MatchRules
- TurnStartedEvent
- DailyPanel
- IGameCommand
- PlayerModel
- CardColor
- GameSession.GetLocalPlayer
- Loc
- EventBus.Subscribe
- FrameBudget.Ping
- 15.4 Helpers STATIQUES (aucun GameObject)
- ChallengeDirector
- RematchVote
- CanvasGroup
- CardType
- BotPersonas.Names
- Transform
- PanelState
- MatchRulesTests
- TurnPhase
- Constants
- GameSession
- SpecialCardsMode
- Kind
- quick-compile.sh
- run-harness.sh

## God Nodes (most connected - your core abstractions)
1. `OnlineSession` - 95 edges
2. `GameInputController` - 90 edges
3. `GameBootstrapper` - 83 edges
4. `LevelUp.Core` - 79 edges
5. `HandView` - 69 edges
6. `GameManager` - 69 edges
7. `GameScreenLayout` - 67 edges
8. `LevelUp.UI` - 56 edges
9. `UISkin` - 53 edges
10. `BotBrain` - 51 edges

## Surprising Connections (you probably didn't know these)
- `3.6 TurnBannerView (bannière temporaire "Tour de X")` --references--> `TurnStartedEvent`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/Core/GameEvents.cs
- `3.5.a HandContainer (enfant de PlayerHand)` --references--> `HandView`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `Verifier sans appareil (editeur Unity FERME, sinon le batchmode echoue)` --references--> `DevAutoScreenshot`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/DevAutoScreenshot.cs
- `Ce qui tourne vraiment` --references--> `OptionsPanel`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/UI/OptionsPanel.cs
- `15.2 Scripts AUTO-GÉNÉRÉS au runtime (ne pas les ajouter à la main)` --references--> `OptionsPanel`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/OptionsPanel.cs

## Import Cycles
- None detected.

## Communities (155 total, 78 thin omitted)

### Community 2 - "CardModel"
Cohesion: 0.13
Nodes (5): CardExtensions, Challenge, LevelRequirement, LevelValidator, LocTable

### Community 5 - "HandView"
Cohesion: 0.10
Nodes (8): HandView, CardCount, CardPrefab, IsAnimatingDeal, IsAnimatingDraw, IsReorderDragging, SelectedCard, SelectedCards

### Community 6 - "manifest.json"
Cohesion: 0.17
Nodes (11): com.unity.modules.jsonserialize@1.0.0, com.unity.modules.umbra@1.0.0, com.unity.render-pipelines.universal@14.0.11, com.unity.textmeshpro@3.0.9, com.unity.visualscripting@1.9.4, com.unity.modules.jsonserialize, com.unity.modules.umbra, com.unity.modules.unityanalytics (+3 more)

### Community 7 - "com.unity.2d.common@8.1.0"
Cohesion: 0.05
Nodes (44): dependencies, depth, source, url, version, dependencies, depth, source (+36 more)

### Community 8 - "TutorialDirector"
Cohesion: 0.11
Nodes (11): Expect, AddToMeld, Discard, Done, DrawDeck, DrawDiscard, LayDown, Step (+3 more)

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
Cohesion: 0.12
Nodes (15): 0. Pré-requis : Canvas Scaler, 10. Méthode pour ajuster visuellement dans Unity, 11. Test et validation, 12. Troubleshooting, 13. Ajustements mobiles (optionnel), 14. Couleurs de référence (cohérence visuelle), 1. Vue d'ensemble du layout, 2. Hiérarchie cible (+7 more)

### Community 14 - "com.unity.mathematics@1.2.6"
Cohesion: 0.08
Nodes (28): dependencies, depth, source, url, version, dependencies, depth, source (+20 more)

### Community 15 - "EventBus.Publish"
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

### Community 22 - "Regles (Core)"
Cohesion: 0.13
Nodes (15): Regles (Core), GameCommandCodec, AddToMeldCommand, Card, MeldIndex, MeldOwnerIndex, PlayerIndex, DrawFromDiscardCommand (+7 more)

### Community 23 - "com.unity.nuget.newtonsoft-json"
Cohesion: 0.06
Nodes (34): dependencies, depth, source, version, dependencies, depth, source, url (+26 more)

### Community 24 - "dependencies"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, url, version, dependencies, depth, source (+18 more)

### Community 25 - "HUDView"
Cohesion: 0.19
Nodes (4): HUDView, 15.1 Scripts à placer MANUELLEMENT dans la scène `Game.unity`, Scripts racine (GameObjects vides, pas sous Canvas), Scripts UI (enfants du Canvas)

### Community 26 - "com.unity.modules.unitywebrequest@1.0.0"
Cohesion: 0.05
Nodes (49): dependencies, depth, source, version, dependencies, depth, source, version (+41 more)

### Community 27 - "packages-lock.json"
Cohesion: 0.08
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 28 - "Test"
Cohesion: 0.06
Nodes (22): LayDownLevelCommand, Melds, PlayerIndex, GameConfig, CardMaxValue, CardMinValue, CardsPerPlayer, DeckSize (+14 more)

### Community 30 - "OnlineSession"
Cohesion: 0.12
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

### Community 39 - "GameManager.StartGame"
Cohesion: 0.15
Nodes (11): DiscardCommand, Card, PlayerIndex, TargetPlayerIndex, DrawFromDeckCommand, PlayerIndex, SkipPhaseCommand, PhaseToSkip (+3 more)

### Community 40 - "LevelRequirement"
Cohesion: 0.12
Nodes (9): ContractCatalog, ContractPlan, IsRandom, ContractProgress, Entry, IsMet, Achievements, Entry (+1 more)

### Community 44 - "MatchRules"
Cohesion: 0.09
Nodes (16): MatchRules, CatchUp, Draw2Count, FinalLevelRequired, GoOutBonus, HandSize, HasTurnTimer, IsClassic (+8 more)

### Community 46 - "com.unity.inputsystem"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.inputsystem

### Community 47 - "PassAndPlayController"
Cohesion: 0.33
Nodes (4): 9) Prochaines taches recommandees (ordre d'execution), PassAndPlayController, Active, HidesDeal

### Community 48 - "GameSettings"
Cohesion: 0.06
Nodes (32): GameSettings, BotDelayFactor, BotSpeed, CardBack, ColorblindMode, Difficulty, Felt, HandSize (+24 more)

### Community 49 - "UISkin"
Cohesion: 0.05
Nodes (14): LayDownChooser, IsOpen, UIFactory, CircleSprite, RingSprite, RoundedSprite, SmallPillSprite, SoftShadowSprite (+6 more)

### Community 50 - "com.unity.2d.spriteshape"
Cohesion: 0.14
Nodes (14): dependencies, depth, source, url, version, dependencies, depth, source (+6 more)

### Community 51 - "com.unity.nuget.mono-cecil@1.10.1"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, url, version, dependencies, depth, source (+5 more)

### Community 52 - "Action"
Cohesion: 0.05
Nodes (23): Ce qui tourne vraiment, AppLifecycle, ConfirmDialog, DailyPanel, IsVisible, EmoteView, MatchSetupPanel, IsVisible (+15 more)

### Community 56 - "MainMenuController"
Cohesion: 0.11
Nodes (4): MainMenuController, IsVisible, MatchSetup, Root

### Community 57 - "DeckManager"
Cohesion: 0.15
Nodes (8): ActionCardHandler, DeckManager, CardsPerPlayer, DiscardPileCount, DrawPileCount, PresetOrder, DeckChangedEvent, SkipStackingTests

### Community 58 - "GameManager"
Cohesion: 0.10
Nodes (15): GameManager, AutoStartNextRound, CommandFilter, Config, DeckManager, Executor, LastRoundWinner, NextContracts (+7 more)

### Community 59 - "GameInputController"
Cohesion: 0.09
Nodes (8): InvalidActionEvent, GameInputController, ActionsLocked, CurrentPlayerIndex, DpiScale, DragUpThreshold, IsLocalTurn, ReorderDragThreshold

### Community 60 - "CLAUDE.md - Brief d'execution optimise pour Claude"
Cohesion: 0.10
Nodes (20): 10) Carte du code (a lire AVANT d'explorer : evite de tout relire), 1) Role de Claude dans ce projet, 3) Objectif produit global, 4) Backlog priorise (spec fonctionnelle), 5) Contraintes de mise en oeuvre, 6) Definition of Done (DoD), 7) Format de reponse attendu de Claude, 8) Regles de priorisation quand il y a ambiguite (+12 more)

### Community 61 - "RoundSummaryView"
Cohesion: 0.16
Nodes (4): RoundSummaryView, IsVisible, ReplayAvailable, Suppressed

### Community 62 - "CommandError"
Cohesion: 0.12
Nodes (16): CommandError, ActionCardNotPickable, AlreadyLaidDown, CardDoesNotFit, CardNotInHand, ContractMismatch, EmptyDiscard, Filtered (+8 more)

### Community 63 - "Constants.cs"
Cohesion: 0.18
Nodes (10): GameState, EndRound, GameOver, PlayerTurn, Setup, Validate, MeldType, Flush (+2 more)

### Community 65 - "RectTransform"
Cohesion: 0.06
Nodes (16): UI / DA "Salon", AnimatedBackground, AnimationController, IsAnimating, ConnectionOverlay, IsVisible, PileSlot, ContractRow (+8 more)

### Community 66 - "3. Configuration détaillée par GameObject"
Cohesion: 0.17
Nodes (12): 3.1 HUD (barre du haut), 3.2 DiscardPileView (défausses de tous les joueurs), 3.3 LevelProgress (colonne gauche), 3.4.a DeckArea (pile de pioche), 3.4.b DiscardArea (défausse unique au centre), 3.4.c Table / TableContainer (melds du joueur local), 3.4 PlayZone (conteneur central), 3.5.a HandContainer (enfant de PlayerHand) (+4 more)

### Community 68 - "CardView"
Cohesion: 0.11
Nodes (9): CardFlights, CardView, CardModel, IsFaceUp, IsHovered, IsSelected, RectTransform, CardViewPool (+1 more)

### Community 104 - "UIIcon"
Cohesion: 0.15
Nodes (13): UIIcon, Cards, Check, Clock, Close, Copy, Edit, Lock (+5 more)

### Community 105 - "DiscardPileView"
Cohesion: 0.13
Nodes (5): DiscardPileView, Compact, PileCount, PileSpacing, OpponentMovesView

### Community 106 - "TextWeight"
Cohesion: 0.33
Nodes (6): TextWeight, Bold, Display, Medium, Regular, SemiBold

### Community 107 - "SocialProtocol"
Cohesion: 0.16
Nodes (3): EmoteLimiter, SocialProtocol, SocialProtocolTests

### Community 109 - "OnlinePanel"
Cohesion: 0.14
Nodes (6): En ligne (`Scripts/Network`), LobbyViewModel, OnlinePanel, IsVisible, LastCode, UIInputs

### Community 110 - "OnlineSession.cs"
Cohesion: 0.17
Nodes (3): LevelUp.Network, OnlineResumeData, OnlineStartData

### Community 115 - "CardModel"
Cohesion: 0.29
Nodes (3): CardModel, IsAction, IsWild

### Community 117 - "TurnStartedEvent"
Cohesion: 0.20
Nodes (16): ActionCardPlayedEvent, CardAddedToMeldEvent, CardDiscardedEvent, CardDrawnEvent, ForcedDrawEvent, GameOverEvent, GameStartedEvent, HandChangedEvent (+8 more)

### Community 118 - "DailyPanel"
Cohesion: 0.18
Nodes (4): DailyDeal, Rules, Streak, TodayKey

### Community 119 - "IGameCommand"
Cohesion: 0.14
Nodes (6): SavedGame, SaveGame, Exists, FilePath, PathOverride, GameBootstrapper

### Community 121 - "PlayerModel"
Cohesion: 0.12
Nodes (14): PlayerModel, CurrentLevel, Hand, HasLaidDownThisRound, Index, IsAI, IsHandEmpty, IsSkipped (+6 more)

### Community 122 - "CardColor"
Cohesion: 0.25
Nodes (8): CardColor, Blue, Green, Orange, Purple, Red, Wild, Yellow

### Community 124 - "GameSession.GetLocalPlayer"
Cohesion: 0.13
Nodes (6): PlayerProfile, GamesPlayed, GamesWon, HasCustomName, Name, RoundsWon

### Community 125 - "Loc"
Cohesion: 0.38
Nodes (3): Loc, IsFrench, Language

### Community 127 - "EventBus.Subscribe"
Cohesion: 0.23
Nodes (6): 2) Contexte technique du repo (a respecter), EventBus, Muted, Subscription, EventBusTests, PingEvent

### Community 128 - "FrameBudget.Ping"
Cohesion: 0.09
Nodes (8): FrameBudget, Enabled, Entry, LocDriver, PointerInput, LastActivityFrame, Override, PointerState

### Community 129 - "15.4 Helpers STATIQUES (aucun GameObject)"
Cohesion: 0.29
Nodes (7): TweenRunner, 15.3 Scripts sur PREFABS (pas dans la scène), 15.4 Helpers STATIQUES (aucun GameObject), 15.5 Hiérarchie finale complète (copier-coller mental), 15.6 Toutes les références Inspector (checklist drag & drop), 15.7 Résumé en 3 lignes, 15. Mapping complet script → GameObject

### Community 130 - "ChallengeDirector"
Cohesion: 0.16
Nodes (4): ChallengeDirector, IsRunning, Rules, Solved

### Community 131 - "RematchVote"
Cohesion: 0.21
Nodes (4): RematchVote, ExpectedMask, IsAgreed, VotesMask

### Community 132 - "CanvasGroup"
Cohesion: 0.21
Nodes (5): GameOverCelebration, PlayerNames, Players, ReplayIsVote, RoundsPlayed

### Community 133 - "CardType"
Cohesion: 0.29
Nodes (6): CardType, Draw2, Normal, Skip, Wild, WildDraw2

### Community 134 - "BotPersonas.Names"
Cohesion: 0.25
Nodes (3): BotPersonas, BotStyle, PersonaTests

### Community 137 - "PanelState"
Cohesion: 0.33
Nodes (6): PanelState, Busy, ClientLobby, Home, HostLobby, Join

### Community 140 - "TurnPhase"
Cohesion: 0.33
Nodes (5): TurnPhase, AddToMelds, Discard, Draw, LayDown

### Community 143 - "GameSession"
Cohesion: 0.25
Nodes (6): GameSession, Contracts, IsHost, IsOnline, LocalPlayerIndex, Rules

### Community 145 - "SpecialCardsMode"
Cohesion: 0.23
Nodes (5): SpecialCardsMode, All, Jokers, None, MatchRulesText

### Community 146 - "Kind"
Cohesion: 0.25
Nodes (8): Kind, AddToMeld, Discard, DrawFromDeck, DrawFromDiscard, LayDown, SkipPhase, StartNextRound

## Knowledge Gaps
- **299 isolated node(s):** `IsVisible`, `IsVisible`, `TutorialRequested`, `TutorialAvailable`, `RoundedSprite` (+294 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 732 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **78 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `GameManager` connect `GameManager` to `15.4 Helpers STATIQUES (aucun GameObject)`, `ChallengeDirector`, `CardModel`, `Transform`, `LevelUp.Core`, `MatchRulesTests`, `AudioManager`, `NetworkManager`, `EventBus.Publish`, `HUDView`, `Test`, `GameBootstrapper`, `EventBus.Clear`, `AIPlayer`, `GameManager.StartGame`, `LevelRequirement`, `MatchRules`, `IEnumerator`, `Action`, `DeckManager`, `GameInputController`, `LevelProgressView`, `DiscardPileView`, `BotSimulationTests.PlayGame`, `TurnStartedEvent`, `IGameCommand`, `PlayerModel`, `GameSession.GetLocalPlayer`?**
  _High betweenness centrality (0.052) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `OnlineSession` (e.g. with `9) Prochaines taches recommandees (ordre d'execution)` and `Ce qui tourne vraiment`) actually correct?**
  _`OnlineSession` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `IsVisible`, `IsVisible`, `TutorialRequested` to the rest of the system?**
  _299 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Vector2` be split into smaller, more focused modules?**
  _Cohesion score 0.14516129032258066 - nodes in this community are weakly interconnected._
- **Why does `OnlineSession` connect `OnlineSession` to `CardModel`, `RematchVote`, `FastBufferReader`, `TutorialDirector`, `SocialProtocol`, `OnlinePanel`, `OnlineSession.cs`, `PassAndPlayController`, `Action`, `ChallengeGenerator.Build`, `MatchRules`, `GameBootstrapper.HandleResumeSolo`, `OnlineSession.HostAsync`, `Regles (Core)`, `IGameCommand`?**
  _High betweenness centrality (0.047) - this node is a cross-community bridge._
- **Should `unityengine` be split into smaller, more focused modules?**
  _Cohesion score 0.12857142857142856 - nodes in this community are weakly interconnected._
- **Why does `CardView` connect `CardView` to `FrameBudget.Ping`, `unityengine`, `CardModel`, `UISkin.Text`, `RectTransform`, `HandView`, `Vector2`, `15.4 Helpers STATIQUES (aucun GameObject)`, `IEnumerator`, `UISkin`, `Action`, `GameInputController`, `EventBus.Subscribe`?**
  _High betweenness centrality (0.046) - this node is a cross-community bridge._