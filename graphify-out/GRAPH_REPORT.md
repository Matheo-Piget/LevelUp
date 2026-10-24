# Graph Report - levelup  (2026-10-24)

## Corpus Check
- 67 files · ~65,450 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 63 file(s) not represented in the graph (top: .asset 34, .shader 13, .asmdef 6)

## Summary
- 1679 nodes · 4602 edges · 101 communities (50 shown, 51 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 406 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `626529cc`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- HandView
- LevelUp.Utils
- Test
- GameBootstrapper
- .CreateButton
- AnimationController
- manifest.json
- com.unity.2d.common@8.1.0
- CardView
- HUDView
- CardModel
- com.unity.render-pipelines.core
- CanvasGroup
- LevelProgressView
- com.unity.mathematics@1.2.6
- GameCommandExecutor
- RectTransform
- dependencies
- com.unity.modules.physics@1.0.0
- com.unity.modules.jsonserialize@1.0.0
- com.unity.ext.nunit
- com.unity.modules.imgui@1.0.0
- PlayerModel
- com.unity.nuget.newtonsoft-json
- dependencies
- CardColor
- com.unity.modules.unitywebrequest@1.0.0
- packages-lock.json
- Color
- DiscardPileView
- OnlineSession
- GameInputController
- com.unity.2d.tilemap
- CmdKind
- com.unity.ugui@1.0.0
- com.unity.collections@1.1.0
- AIPlayer
- TableView
- .ExecuteCommand
- ActionCardHandler
- SelectionStatusView
- com.unity.modules.androidjni@1.0.0
- com.unity.modules.unitywebrequest@1.0.0
- DeckManager
- CLAUDE.md - Brief d'execution optimise pour Claude
- SafeAreaFitter
- GameSettings
- com.unity.2d.spriteshape
- com.unity.nuget.mono-cecil@1.10.1
- MonoBehaviour
- GameManager
- com.unity.modules.unitywebrequesttexture@1.0.0
- EventBus
- 15.4 Helpers STATIQUES (aucun GameObject)
- .Publish
- GameOverCelebration
- AndroidBuilder
- IGameCommand
- CardType
- .Evaluate
- com.unity.collab-proxy
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

## God Nodes (most connected - your core abstractions)
1. `CardModel` - 92 edges
2. `OnlineSession` - 66 edges
3. `HandView` - 58 edges
4. `GameBootstrapper` - 57 edges
5. `CardView` - 52 edges
6. `GameInputController` - 52 edges
7. `LevelUp.Utils` - 50 edges
8. `GameManager` - 47 edges
9. `PlayerModel` - 46 edges
10. `AnimationController` - 42 edges

## Surprising Connections (you probably didn't know these)
- `3.6 TurnBannerView (bannière temporaire "Tour de X")` --references--> `TurnStartedEvent`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/Core/GameEvents.cs
- `5. Références à assigner sur GameBootstrapper` --references--> `GameBootstrapper`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/GameBootstrapper.cs
- `9) Prochaines taches recommandees (ordre d'execution)` --references--> `HandView`  [INFERRED]
  CLAUDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `3.5.a HandContainer (enfant de PlayerHand)` --references--> `HandView`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/UI/HandView.cs
- `15.2 Scripts AUTO-GÉNÉRÉS au runtime (ne pas les ajouter à la main)` --references--> `AIPlayer`  [INFERRED]
  UNITY_LAYOUT_GUIDE.md → LevelUp/Assets/Scripts/AI/AIPlayer.cs

## Import Cycles
- None detected.

## Communities (101 total, 51 thin omitted)

### Community 0 - "HandView"
Cohesion: 0.14
Nodes (6): HandView, CardCount, IsAnimatingDeal, IsReorderDragging, SelectedCard, SelectedCards

### Community 1 - "LevelUp.Utils"
Cohesion: 0.08
Nodes (8): LevelUp.Core, LevelUp.Tests, LevelUp.Utils, LevelUp.EditorTools, LevelUp.UI, LevelUp.AI, LevelUp.Network, OnlineResumeData

### Community 2 - "Test"
Cohesion: 0.12
Nodes (12): LayDownLevelCommand, Melds, PlayerIndex, Meld, Cards, OwnerIndex, Type, CardExtensionsTests (+4 more)

### Community 3 - "GameBootstrapper"
Cohesion: 0.07
Nodes (12): GameSession, Contracts, IsHost, IsOnline, LocalPlayerIndex, GameBootstrapper, ConnectionOverlay, IsVisible (+4 more)

### Community 4 - ".CreateButton"
Cohesion: 0.10
Nodes (9): OptionsPanel, PauseMenuController, IsPaused, UIFactory, CircleSprite, RingSprite, RoundedSprite, SmallPillSprite (+1 more)

### Community 5 - "AnimationController"
Cohesion: 0.12
Nodes (4): AnimationController, IsAnimating, UITween, Runner

### Community 6 - "manifest.json"
Cohesion: 0.17
Nodes (11): com.unity.modules.jsonserialize@1.0.0, com.unity.modules.umbra@1.0.0, com.unity.render-pipelines.universal@14.0.11, com.unity.textmeshpro@3.0.9, com.unity.visualscripting@1.9.4, com.unity.modules.jsonserialize, com.unity.modules.umbra, com.unity.modules.unityanalytics (+3 more)

### Community 7 - "com.unity.2d.common@8.1.0"
Cohesion: 0.05
Nodes (44): dependencies, depth, source, url, version, dependencies, depth, source (+36 more)

### Community 8 - "CardView"
Cohesion: 0.14
Nodes (6): CardView, CardModel, IsFaceUp, IsHovered, IsSelected, RectTransform

### Community 9 - "HUDView"
Cohesion: 0.06
Nodes (21): ActionCardPlayedEvent, AIThinkingEvent, CardAddedToMeldEvent, CardDiscardedEvent, CardDrawnEvent, DeckChangedEvent, ForcedDrawEvent, GameOverEvent (+13 more)

### Community 10 - "CardModel"
Cohesion: 0.11
Nodes (8): CardExtensions, CardModel, IsAction, IsWild, LevelDefinition, MeldRequirement, LevelRequirement, LevelValidator

### Community 11 - "com.unity.render-pipelines.core"
Cohesion: 0.07
Nodes (29): depth, source, version, dependencies, depth, source, version, dependencies (+21 more)

### Community 13 - "LevelProgressView"
Cohesion: 0.05
Nodes (29): LevelProgressView, PlayerRow, 0. Pré-requis : Canvas Scaler, 10. Méthode pour ajuster visuellement dans Unity, 11. Test et validation, 12. Troubleshooting, 13. Ajustements mobiles (optionnel), 14. Couleurs de référence (cohérence visuelle) (+21 more)

### Community 14 - "com.unity.mathematics@1.2.6"
Cohesion: 0.10
Nodes (21): dependencies, depth, source, url, version, dependencies, depth, source (+13 more)

### Community 15 - "GameCommandExecutor"
Cohesion: 0.10
Nodes (17): GameCommandExecutor, CommandResult, DrawnCard, Message, RoundEnded, Success, WinnerOverride, DeckExhaustedEvent (+9 more)

### Community 16 - "RectTransform"
Cohesion: 0.11
Nodes (4): PileSlot, MainMenuController, IsVisible, Root

### Community 17 - "dependencies"
Cohesion: 0.17
Nodes (12): dependencies, com.unity.modules.imageconversion@1.0.0, com.unity.modules.tilemap@1.0.0, com.unity.modules.unityanalytics@1.0.0, com.unity.modules.video@1.0.0, com.unity.modules.xr@1.0.0, com.unity.ugui@1.0.0, com.unity.modules.imageconversion (+4 more)

### Community 18 - "com.unity.modules.physics@1.0.0"
Cohesion: 0.07
Nodes (28): dependencies, depth, source, version, dependencies, depth, source, version (+20 more)

### Community 19 - "com.unity.modules.jsonserialize@1.0.0"
Cohesion: 0.08
Nodes (28): dependencies, depth, source, version, dependencies, depth, source, version (+20 more)

### Community 20 - "com.unity.ext.nunit"
Cohesion: 0.14
Nodes (14): dependencies, depth, source, url, version, dependencies, depth, source (+6 more)

### Community 21 - "com.unity.modules.imgui@1.0.0"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, url, version, dependencies, depth, source (+18 more)

### Community 22 - "PlayerModel"
Cohesion: 0.14
Nodes (11): PlayerModel, CurrentLevel, Hand, HasLaidDownThisRound, Index, IsAI, IsHandEmpty, IsSkipped (+3 more)

### Community 23 - "com.unity.nuget.newtonsoft-json"
Cohesion: 0.06
Nodes (34): dependencies, depth, source, version, dependencies, depth, source, url (+26 more)

### Community 24 - "dependencies"
Cohesion: 0.06
Nodes (33): dependencies, depth, source, url, version, dependencies, depth, source (+25 more)

### Community 25 - "CardColor"
Cohesion: 0.23
Nodes (9): CardColor, Blue, Green, Orange, Purple, Red, Wild, Yellow (+1 more)

### Community 26 - "com.unity.modules.unitywebrequest@1.0.0"
Cohesion: 0.06
Nodes (42): dependencies, depth, source, version, dependencies, depth, source, version (+34 more)

### Community 27 - "packages-lock.json"
Cohesion: 0.08
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 30 - "OnlineSession"
Cohesion: 0.09
Nodes (8): OnlineSession, DeviceToken, IsActive, IsSessionHost, JoinCode, LastJoinCode, LobbyHumanCount, VersionTag

### Community 31 - "GameInputController"
Cohesion: 0.14
Nodes (7): GameInputController, ActionsLocked, CurrentPlayerIndex, DpiScale, DragUpThreshold, IsLocalTurn, ReorderDragThreshold

### Community 32 - "com.unity.2d.tilemap"
Cohesion: 0.12
Nodes (18): dependencies, depth, dependencies, depth, source, url, version, source (+10 more)

### Community 33 - "CmdKind"
Cohesion: 0.15
Nodes (8): CmdKind, AddToMeld, Discard, DrawFromDeck, DrawFromDiscard, LayDown, SkipPhase, NetCommandCodec

### Community 34 - "com.unity.ugui@1.0.0"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, url, version, depth, source, version (+9 more)

### Community 35 - "com.unity.collections@1.1.0"
Cohesion: 0.11
Nodes (19): dependencies, depth, source, url, version, dependencies, depth, source (+11 more)

### Community 36 - "AIPlayer"
Cohesion: 0.19
Nodes (6): AIDifficulty, Easy, Hard, Normal, AIPlayer, PlayerIndex

### Community 37 - "TableView"
Cohesion: 0.17
Nodes (8): MeldGroupView, MeldType, OwnerIndex, TableView, MeldType, Flush, Run, Set

### Community 38 - ".ExecuteCommand"
Cohesion: 0.29
Nodes (3): PlayerSetup, OnlineStartData, ContractAndReplayTests

### Community 40 - "SelectionStatusView"
Cohesion: 0.13
Nodes (4): ContractCatalog, ContractPlan, IsRandom, SelectionStatusView

### Community 44 - "DeckManager"
Cohesion: 0.11
Nodes (11): DeckManager, DiscardPileCount, DrawPileCount, GameConfig, CardMaxValue, CardMinValue, CardsPerPlayer, DeckSize (+3 more)

### Community 45 - "CLAUDE.md - Brief d'execution optimise pour Claude"
Cohesion: 0.13
Nodes (14): 1) Role de Claude dans ce projet, 3) Objectif produit global, 4) Backlog priorise (spec fonctionnelle), 5) Contraintes de mise en oeuvre, 6) Definition of Done (DoD), 7) Format de reponse attendu de Claude, 8) Regles de priorisation quand il y a ambiguite, 9) Prochaines taches recommandees (ordre d'execution) (+6 more)

### Community 48 - "GameSettings"
Cohesion: 0.16
Nodes (8): GameSettings, ColorblindMode, Difficulty, MusicVolume, PlayerCount, QualityIndex, RandomContracts, SfxVolume

### Community 50 - "com.unity.2d.spriteshape"
Cohesion: 0.14
Nodes (14): dependencies, depth, source, url, version, dependencies, depth, source (+6 more)

### Community 51 - "com.unity.nuget.mono-cecil@1.10.1"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, url, version, dependencies, depth, source (+5 more)

### Community 52 - "MonoBehaviour"
Cohesion: 0.16
Nodes (3): AnimatedBackground, PlayerTurnGlow, TweenRunner

### Community 53 - "GameManager"
Cohesion: 0.08
Nodes (19): GameManager, Config, DeckManager, Executor, Players, RoundNumber, State, TurnManager (+11 more)

### Community 55 - "EventBus"
Cohesion: 0.22
Nodes (4): 2) Contexte technique du repo (a respecter), EventBus, Muted, Subscription

### Community 56 - "15.4 Helpers STATIQUES (aucun GameObject)"
Cohesion: 0.22
Nodes (9): 15.1 Scripts à placer MANUELLEMENT dans la scène `Game.unity`, 15.3 Scripts sur PREFABS (pas dans la scène), 15.4 Helpers STATIQUES (aucun GameObject), 15.5 Hiérarchie finale complète (copier-coller mental), 15.6 Toutes les références Inspector (checklist drag & drop), 15.7 Résumé en 3 lignes, 15. Mapping complet script → GameObject, Scripts racine (GameObjects vides, pas sous Canvas) (+1 more)

### Community 60 - "IGameCommand"
Cohesion: 0.13
Nodes (19): AddToMeldCommand, Card, MeldIndex, MeldOwnerIndex, PlayerIndex, DiscardCommand, Card, PlayerIndex (+11 more)

### Community 61 - "CardType"
Cohesion: 0.29
Nodes (6): CardType, Draw2, Normal, Skip, Wild, WildDraw2

### Community 62 - ".Evaluate"
Cohesion: 0.47
Nodes (3): ContractProgress, Entry, IsMet

### Community 65 - "com.unity.collab-proxy"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.collab-proxy

## Knowledge Gaps
- **142 isolated node(s):** `Easy`, `Normal`, `Hard`, `PlayerIndex`, `IsWild` (+137 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 499 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **51 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `dependencies` connect `dependencies` to `com.unity.2d.tilemap`, `com.unity.collab-proxy`, `com.unity.ugui@1.0.0`, `com.unity.collections@1.1.0`, `com.unity.2d.common@8.1.0`, `com.unity.render-pipelines.core`, `com.unity.mathematics@1.2.6`, `com.unity.2d.spriteshape`, `com.unity.modules.physics@1.0.0`, `com.unity.ext.nunit`, `com.unity.modules.imgui@1.0.0`, `com.unity.modules.jsonserialize@1.0.0`, `com.unity.nuget.newtonsoft-json`, `com.unity.nuget.mono-cecil@1.10.1`, `com.unity.modules.unitywebrequest@1.0.0`, `packages-lock.json`?**
  _High betweenness centrality (0.065) - this node is a cross-community bridge._
- **What connects `Easy`, `Normal`, `Hard` to the rest of the system?**
  _142 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `HandView` be split into smaller, more focused modules?**
  _Cohesion score 0.13675213675213677 - nodes in this community are weakly interconnected._
- **Why does `CardModel` connect `CardModel` to `HandView`, `LevelUp.Utils`, `Test`, `CmdKind`, `AIPlayer`, `TableView`, `ActionCardHandler`, `CardView`, `HUDView`, `DeckManager`, `GameCommandExecutor`, `DiscardPileView`, `PlayerModel`, `CardColor`, `IGameCommand`, `CardType`, `.Evaluate`, `GameInputController`?**
  _High betweenness centrality (0.060) - this node is a cross-community bridge._
- **Should `LevelUp.Utils` be split into smaller, more focused modules?**
  _Cohesion score 0.08375350140056022 - nodes in this community are weakly interconnected._
- **Why does `OnlineSession` connect `OnlineSession` to `LevelUp.Utils`, `CmdKind`, `GameBootstrapper`, `.ApproveConnection`, `.ExecuteCommand`, `CardModel`, `.HostAsync`, `MonoBehaviour`, `GameManager`, `IGameCommand`?**
  _High betweenness centrality (0.048) - this node is a cross-community bridge._
- **Should `Test` be split into smaller, more focused modules?**
  _Cohesion score 0.12121212121212122 - nodes in this community are weakly interconnected._