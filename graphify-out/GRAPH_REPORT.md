# Graph Report - LevelUp  (2026-11-13)

## Corpus Check
- 120 files · ~121,660 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 254 file(s) not represented in the graph (top: .meta 185, .asset 34, .shader 13)
- WARNING: 254 of 384 files seen (66%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 2732 nodes · 8644 edges · 156 communities (78 shown, 78 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 859 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `705f428a`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- LevelUp.Utils
- CardModel
- Image
- IconBuilder.Generate
- HandView
- manifest.json
- com.unity.2d.common@8.1.0
- TutorialDirector
- CardFactory.P
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
- DrawFromDeckCommand
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
- DeckManager
- AIPlayer
- CoachAdvisor
- CLAUDE.md - Brief d'execution optimise pour Claude
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
- GameConfig
- com.unity.modules.unitywebrequesttexture@1.0.0
- MainMenuController
- EventBus.Subscribe
- GameManager
- GameInputController
- GameBootstrapper.LaunchOnlineMatch
- RoundSummaryView
- CommandError
- GameCommandExecutor.Execute
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
- PointerInput
- LevelProgressView
- BalatroEffects
- UIIcon
- DiscardPileView
- TextWeight
- SocialProtocol
- TurnTimer
- OnlinePanel
- OnlineSession.OnCommandMessage
- SelectionStatusView
- EventBus.Clear
- CardModel
- MatchRules
- TurnStartedEvent
- DailyPanel
- IGameCommand
- RectTransform
- IReadOnlyList
- PlayerModel
- GameSession.GetLocalPlayer
- Haptics
- EventBus
- CardColor
- CardDrawnEvent
- ChallengeDirector
- RematchVote
- CanvasGroup
- TargetChooser
- BotPersonas.Names
- UISkin.Text
- Expect
- PanelState
- CardType
- GameBootstrapper.StartSoloGame
- ContractAndReplayTests
- Achievements
- TurnPhase
- SpecialCardsMode
- 15.4 Helpers STATIQUES (aucun GameObject)
- quick-compile.sh
- run-harness.sh
- Constants

## God Nodes (most connected - your core abstractions)
1. `OnlineSession` - 95 edges
2. `GameInputController` - 90 edges
3. `GameBootstrapper` - 83 edges
4. `LevelUp.Core` - 80 edges
5. `HandView` - 69 edges
6. `GameManager` - 69 edges
7. `GameScreenLayout` - 67 edges
8. `LevelUp.UI` - 55 edges
9. `UISkin` - 53 edges
10. `BotBrain` - 51 edges

## Surprising Connections (you probably didn't know these)
- `3.6 TurnBannerView (bannière temporaire "Tour de X")` --references--> `TurnStartedEvent`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/Core/GameEvents.cs
- `3.5.a HandContainer (enfant de PlayerHand)` --references--> `HandView`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `9) Prochaines taches recommandees (ordre d'execution)` --references--> `TutorialDirector`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/TutorialDirector.cs
- `Ce qui tourne vraiment` --references--> `GameScreenLayout`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/UI/GameScreenLayout.cs
- `UI / DA "Salon"` --references--> `GameScreenLayout`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/UI/GameScreenLayout.cs

## Import Cycles
- None detected.

## Communities (156 total, 78 thin omitted)

### Community 1 - "LevelUp.Utils"
Cohesion: 0.05
Nodes (18): LevelUp.Core, LevelUp.Tests, LevelUp.Utils, LevelUp.EditorTools, LevelUp.UI, LevelUp.AI, LevelUp.Network, UIInputBlocker (+10 more)

### Community 2 - "CardModel"
Cohesion: 0.14
Nodes (3): CardExtensions, LevelRequirement, LevelValidator

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
Cohesion: 0.14
Nodes (4): Step, TutorialDirector, IsRunning, Rules

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

### Community 22 - "DrawFromDeckCommand"
Cohesion: 0.09
Nodes (26): AddToMeldCommand, Card, MeldIndex, MeldOwnerIndex, PlayerIndex, DiscardCommand, Card, PlayerIndex (+18 more)

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
Cohesion: 0.13
Nodes (5): CardExtensionsTests, LayDownOptionsTests, LevelValidatorTests, MeldTests, CardFactory

### Community 30 - "OnlineSession"
Cohesion: 0.11
Nodes (10): CurrentLobby, DeviceToken, IsActive, IsSessionHost, JoinCode, LastJoinCode, LobbyHumanCount, VersionTag (+2 more)

### Community 31 - "BotBrain"
Cohesion: 0.16
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

### Community 35 - "DeckManager"
Cohesion: 0.06
Nodes (15): ActionCardHandler, DeckManager, CardsPerPlayer, DiscardPileCount, DrawPileCount, PresetOrder, GameConfig, CardMaxValue (+7 more)

### Community 36 - "AIPlayer"
Cohesion: 0.14
Nodes (10): 10) Carte du code (a lire AVANT d'explorer : evite de tout relire), IA (`Scripts/AI`), Regles (Core), Verifier sans appareil (editeur Unity FERME, sinon le batchmode echoue), AIDifficulty, Easy, Hard, Normal (+2 more)

### Community 39 - "CLAUDE.md - Brief d'execution optimise pour Claude"
Cohesion: 0.14
Nodes (13): 1) Role de Claude dans ce projet, 3) Objectif produit global, 4) Backlog priorise (spec fonctionnelle), 5) Contraintes de mise en oeuvre, 6) Definition of Done (DoD), 7) Format de reponse attendu de Claude, 8) Regles de priorisation quand il y a ambiguite, A. UX & Gameplay (zero friction) - PRIORITE P0 (+5 more)

### Community 40 - "LevelRequirement"
Cohesion: 0.17
Nodes (6): ContractCatalog, ContractPlan, IsRandom, ContractProgress, Entry, IsMet

### Community 44 - "MatchRules"
Cohesion: 0.08
Nodes (16): MatchRules, CatchUp, Draw2Count, FinalLevelRequired, GoOutBonus, HandSize, HasTurnTimer, IsClassic (+8 more)

### Community 45 - "IEnumerator"
Cohesion: 0.07
Nodes (10): GameSession, Contracts, IsHost, IsOnline, LocalPlayerIndex, Rules, DevAutoScreenshot, DevInputTest (+2 more)

### Community 46 - "com.unity.inputsystem"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.inputsystem

### Community 47 - "PassAndPlayController"
Cohesion: 0.29
Nodes (3): PassAndPlayController, Active, HidesDeal

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
Nodes (20): Ce qui tourne vraiment, AppLifecycle, ConfirmDialog, ConnectionOverlay, IsVisible, DailyPanel, IsVisible, MatchSetupPanel (+12 more)

### Community 53 - "GameConfig"
Cohesion: 0.13
Nodes (4): Challenge, ChallengeGenerator, ChallengeGeneratorTests, MatchRulesTests

### Community 56 - "MainMenuController"
Cohesion: 0.10
Nodes (7): PileSlot, StepPill, TextMirror, MainMenuController, IsVisible, MatchSetup, Root

### Community 58 - "GameManager"
Cohesion: 0.12
Nodes (14): GameManager, AutoStartNextRound, CommandFilter, Config, DeckManager, Executor, LastRoundWinner, NextContracts (+6 more)

### Community 59 - "GameInputController"
Cohesion: 0.10
Nodes (8): InvalidActionEvent, GameInputController, ActionsLocked, CurrentPlayerIndex, DpiScale, DragUpThreshold, IsLocalTurn, ReorderDragThreshold

### Community 61 - "RoundSummaryView"
Cohesion: 0.18
Nodes (4): RoundSummaryView, IsVisible, ReplayAvailable, Suppressed

### Community 62 - "CommandError"
Cohesion: 0.12
Nodes (16): CommandError, ActionCardNotPickable, AlreadyLaidDown, CardDoesNotFit, CardNotInHand, ContractMismatch, EmptyDiscard, Filtered (+8 more)

### Community 65 - "RectTransform"
Cohesion: 0.11
Nodes (6): UI / DA "Salon", AnimatedBackground, AnimationController, IsAnimating, SafeAreaFitter, 15.2 Scripts AUTO-GÉNÉRÉS au runtime (ne pas les ajouter à la main)

### Community 66 - "3. Configuration détaillée par GameObject"
Cohesion: 0.17
Nodes (12): 3.1 HUD (barre du haut), 3.2 DiscardPileView (défausses de tous les joueurs), 3.3 LevelProgress (colonne gauche), 3.4.a DeckArea (pile de pioche), 3.4.b DiscardArea (défausse unique au centre), 3.4.c Table / TableContainer (melds du joueur local), 3.4 PlayZone (conteneur central), 3.5.a HandContainer (enfant de PlayerHand) (+4 more)

### Community 68 - "CardView"
Cohesion: 0.10
Nodes (11): 2) Contexte technique du repo (a respecter), 9) Prochaines taches recommandees (ordre d'execution), CardFlights, CardView, CardModel, IsFaceUp, IsHovered, IsSelected (+3 more)

### Community 101 - "PointerInput"
Cohesion: 0.48
Nodes (4): PointerInput, LastActivityFrame, Override, PointerState

### Community 104 - "UIIcon"
Cohesion: 0.15
Nodes (13): UIIcon, Cards, Check, Clock, Close, Copy, Edit, Lock (+5 more)

### Community 105 - "DiscardPileView"
Cohesion: 0.18
Nodes (4): DiscardPileView, Compact, PileCount, PileSpacing

### Community 106 - "TextWeight"
Cohesion: 0.33
Nodes (6): TextWeight, Bold, Display, Medium, Regular, SemiBold

### Community 109 - "OnlinePanel"
Cohesion: 0.18
Nodes (4): OnlinePanel, IsVisible, LastCode, UIInputs

### Community 115 - "CardModel"
Cohesion: 0.29
Nodes (3): CardModel, IsAction, IsWild

### Community 116 - "MatchRules"
Cohesion: 0.21
Nodes (4): En ligne (`Scripts/Network`), LobbyInfo, LobbyMember, OnlineStartData

### Community 117 - "TurnStartedEvent"
Cohesion: 0.19
Nodes (15): ActionCardPlayedEvent, CardAddedToMeldEvent, CardDiscardedEvent, ForcedDrawEvent, GameOverEvent, GameStartedEvent, HandChangedEvent, LevelCompletedEvent (+7 more)

### Community 118 - "DailyPanel"
Cohesion: 0.17
Nodes (4): DailyDeal, Rules, Streak, TodayKey

### Community 119 - "IGameCommand"
Cohesion: 0.12
Nodes (14): GameCommandCodec, Kind, AddToMeld, Discard, DrawFromDeck, DrawFromDiscard, LayDown, SkipPhase (+6 more)

### Community 120 - "RectTransform"
Cohesion: 0.15
Nodes (3): GameScreenLayout, Coach, Root

### Community 122 - "PlayerModel"
Cohesion: 0.09
Nodes (17): Meld, Cards, OwnerIndex, Type, PlayerModel, CurrentLevel, Hand, HasLaidDownThisRound (+9 more)

### Community 124 - "GameSession.GetLocalPlayer"
Cohesion: 0.13
Nodes (6): PlayerProfile, GamesPlayed, GamesWon, HasCustomName, Name, RoundsWon

### Community 127 - "EventBus"
Cohesion: 0.29
Nodes (3): EventBus, Muted, Subscription

### Community 128 - "CardColor"
Cohesion: 0.25
Nodes (8): CardColor, Blue, Green, Orange, Purple, Red, Wild, Yellow

### Community 130 - "ChallengeDirector"
Cohesion: 0.15
Nodes (4): ChallengeDirector, IsRunning, Rules, Solved

### Community 131 - "RematchVote"
Cohesion: 0.16
Nodes (5): EmoteLimiter, RematchVote, ExpectedMask, IsAgreed, VotesMask

### Community 132 - "CanvasGroup"
Cohesion: 0.13
Nodes (6): EmoteView, GameOverCelebration, PlayerNames, Players, ReplayIsVote, RoundsPlayed

### Community 134 - "BotPersonas.Names"
Cohesion: 0.25
Nodes (3): BotPersonas, BotStyle, PersonaTests

### Community 135 - "UISkin.Text"
Cohesion: 0.13
Nodes (5): RulesPanel, IsVisible, TutorialAvailable, TutorialRequested, UIControls

### Community 136 - "Expect"
Cohesion: 0.29
Nodes (7): Expect, AddToMeld, Discard, Done, DrawDeck, DrawDiscard, LayDown

### Community 137 - "PanelState"
Cohesion: 0.33
Nodes (6): PanelState, Busy, ClientLobby, Home, HostLobby, Join

### Community 138 - "CardType"
Cohesion: 0.29
Nodes (6): CardType, Draw2, Normal, Skip, Wild, WildDraw2

### Community 142 - "Achievements"
Cohesion: 0.40
Nodes (3): Achievements, Entry, Done

### Community 143 - "TurnPhase"
Cohesion: 0.33
Nodes (5): TurnPhase, AddToMelds, Discard, Draw, LayDown

### Community 145 - "SpecialCardsMode"
Cohesion: 0.24
Nodes (5): SpecialCardsMode, All, Jokers, None, MatchRulesText

### Community 146 - "15.4 Helpers STATIQUES (aucun GameObject)"
Cohesion: 0.33
Nodes (6): 15.3 Scripts sur PREFABS (pas dans la scène), 15.4 Helpers STATIQUES (aucun GameObject), 15.5 Hiérarchie finale complète (copier-coller mental), 15.6 Toutes les références Inspector (checklist drag & drop), 15.7 Résumé en 3 lignes, 15. Mapping complet script → GameObject

## Knowledge Gaps
- **297 isolated node(s):** `DrawDeck`, `LayDown`, `AddToMeld`, `Discard`, `DrawDiscard` (+292 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 726 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **78 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `OnlineSession` connect `OnlineSession` to `LevelUp.Utils`, `CardModel`, `RematchVote`, `CardView`, `FastBufferReader`, `TutorialDirector`, `SocialProtocol`, `OnlinePanel`, `OnlineSession.OnCommandMessage`, `Action`, `GameConfig`, `MatchRules`, `OnlineSession.HostAsync`, `IGameCommand`?**
  _High betweenness centrality (0.067) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `OnlineSession` (e.g. with `9) Prochaines taches recommandees (ordre d'execution)` and `Ce qui tourne vraiment`) actually correct?**
  _`OnlineSession` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `DrawDeck`, `LayDown`, `AddToMeld` to the rest of the system?**
  _297 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `LevelUp.Utils` be split into smaller, more focused modules?**
  _Cohesion score 0.052950910093767234 - nodes in this community are weakly interconnected._
- **Why does `HandView` connect `HandView` to `Vector2`, `LevelUp.Utils`, `CardModel`, `Image`, `UISkin.Text`, `NetworkManager`, `GameBootstrapper.HandleResumeSolo`, `MeldGroupView`, `GameInputController.ApplyPreferredSort`, `IEnumerator`, `Action`, `MainMenuController`, `EventBus.Subscribe`, `GameInputController`, `RectTransform`, `3. Configuration détaillée par GameObject`, `CardView`, `TurnStartedEvent`, `IReadOnlyList`?**
  _High betweenness centrality (0.046) - this node is a cross-community bridge._
- **Should `CardModel` be split into smaller, more focused modules?**
  _Cohesion score 0.1417004048582996 - nodes in this community are weakly interconnected._
- **Why does `GameManager` connect `GameManager` to `LevelUp.Utils`, `ChallengeDirector`, `CardModel`, `CardDrawnEvent`, `UISkin.Text`, `CardFactory.P`, `AudioManager`, `GameBootstrapper.StartSoloGame`, `ContractAndReplayTests`, `NetworkManager`, `EventBus.Publish`, `15.4 Helpers STATIQUES (aucun GameObject)`, `DrawFromDeckCommand`, `GameBootstrapper`, `DeckManager`, `AIPlayer`, `LevelRequirement`, `MatchRules`, `IEnumerator`, `Action`, `GameConfig`, `GameInputController`, `RoundSummaryView`, `GameCommandExecutor.Execute`, `DiscardPileView`, `TurnTimer`, `EventBus.Clear`, `MatchRules`, `TurnStartedEvent`, `IReadOnlyList`?**
  _High betweenness centrality (0.043) - this node is a cross-community bridge._