using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LevelUp.UI;
using LevelUp.AI;
using LevelUp.Network;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Point d'entrée de la scène de jeu.
    /// Câble tous les systèmes, applique le style visuel, et lance les parties :
    /// solo (nouvelle, reprise de sauvegarde, donne du jour) et en ligne.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private GameManager? _gameManager;

        [Header("UI")]
        [SerializeField] private HandView? _handView;
        [SerializeField] private TableView? _tableView;
        [SerializeField] private HUDView? _hudView;
        [SerializeField] private LevelProgressView? _levelProgressView;
        [SerializeField] private AnimationController? _animController;
        [SerializeField] private GameInputController? _inputController;
        [SerializeField] private BalatroEffects? _balatroEffects;
        [SerializeField] private DiscardPileView? _discardPileView;

        [Header("Visual")]
        [SerializeField] private Canvas? _mainCanvas;

        [Header("Network")]
        [SerializeField] private NetworkManager? _networkManager;

        [Header("Game Settings")]
        [SerializeField] private int _humanPlayerCount = 1;
        [SerializeField] private int _aiPlayerCount = 3;

        private readonly List<AIPlayer> _aiPlayers = new();
        private MainMenuController? _mainMenu;
        private PauseMenuController? _pauseMenu;
        private GameOverCelebration? _gameOverCelebration;
        private OnlineSession? _onlineSession;
        private OnlinePanel? _onlinePanel;
        private bool _gameStarted;
        private bool _systemsInitialized;
        private bool _syncingViews;
        private ConnectionOverlay? _connectionOverlay;
        private GameScreenLayout? _screenLayout;
        private RoundSummaryView? _roundSummary;
        private TurnTimer? _turnTimer;
        private AppLifecycle? _lifecycle;
        private CardFlights? _flights;
        private ConfirmDialog? _confirm;

        // Partie solo sauvegardée au fil de l'eau (fermer l'appli ne perd rien).
        private bool _saveActive;
        private int _soloMode;      // 0 = libre, 1 = donne du jour
        private int _dailyKey;
        private int _soloDifficulty;

        private void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            GameSettings.Initialize();

            // Jeu réel : la manche suivante attend la fin du récap (StartNextRoundCommand).
            // Identique sur tous les appareils d'une partie en ligne.
            if (_gameManager != null) _gameManager.AutoStartNextRound = false;

            InitializeVisuals();

            gameObject.AddComponent<AudioManager>().Initialize(_gameManager);
            _lifecycle = gameObject.AddComponent<AppLifecycle>();
            _lifecycle.Initialize(() => _gameStarted && (_mainMenu == null || !_mainMenu.IsVisible));

            ShowMainMenu();
            StartCoroutine(PrewarmCards());
        }

        /// <summary>
        /// Pendant le menu, crée quelques cartes d'avance (2 par image) : la première
        /// donne et les premières poses ne paient plus leur création en plein jeu.
        /// </summary>
        private IEnumerator PrewarmCards()
        {
            GameObject? prefab = _handView != null ? _handView.CardPrefab : null;
            if (prefab == null) yield break;
            yield return null;
            for (int target = 2; target <= 28; target += 2)
            {
                CardViewPool.Prewarm(prefab, target);
                yield return null;
            }
        }

        // ────────────────────────────────────────────────────
        //  MENU
        // ────────────────────────────────────────────────────

        /// <summary>Affiche le menu principal (REPRENDRE si une partie est sauvegardée).</summary>
        private void ShowMainMenu()
        {
            if (_mainCanvas == null)
            {
                // Fallback : démarre direct si pas de canvas
                StartNewSoloGame();
                return;
            }

            if (_mainMenu == null)
            {
                _mainMenu = gameObject.AddComponent<MainMenuController>();
                _mainMenu.OnPlayClicked += RequestNewSoloGame;
                _mainMenu.OnOnlineClicked += HandleMenuOnline;
                _mainMenu.OnResumeClicked += HandleResumeSolo;
                _mainMenu.OnDailyClicked += HandleDaily;
                _mainMenu.Setup(_mainCanvas);
            }
            else
            {
                _mainMenu.Show();
            }
            RefreshMenuStatus();
        }

        private void RefreshMenuStatus()
        {
            if (_mainMenu == null) return;
            SavedGame? saved = SaveGame.Load();
            if (saved == null)
            {
                _mainMenu.SetResume(null);
            }
            else
            {
                string what = saved.Mode == 1 ? "donne du jour" : "partie";
                _mainMenu.SetResume($"{what} · manche {RoundOf(saved)}");
            }
            _mainMenu.SetDailyStatus(DailyDeal.Status());
        }

        private void InitializePauseAndGameOver()
        {
            if (_mainCanvas == null) return;

            if (_pauseMenu == null)
            {
                _pauseMenu = gameObject.AddComponent<PauseMenuController>();
                _pauseMenu.OnMainMenuRequested += HandleReturnToMainMenu;
                _pauseMenu.Setup(_mainCanvas);
                _screenLayout?.BindPauseMenu(_pauseMenu);
                _lifecycle?.BindPause(_pauseMenu);
            }

            if (_gameOverCelebration == null)
            {
                _gameOverCelebration = gameObject.AddComponent<GameOverCelebration>();
                _gameOverCelebration.OnReplayClicked += HandleReplay;
                _gameOverCelebration.OnMainMenuClicked += HandleReturnToMainMenu;
                _gameOverCelebration.Setup(_mainCanvas);
                if (_gameManager != null)
                {
                    List<string> names = new();
                    foreach (PlayerModel p in _gameManager.Players) names.Add(p.Name);
                    BindGameOverScreen(names);
                }
            }
        }

        /// <summary>
        /// Retour au menu : la partie est ABANDONNÉE (plus rien ne tourne derrière
        /// le menu : ni bots, ni minuteur, ni input). Une partie solo reste
        /// sauvegardée et se reprend avec REPRENDRE.
        /// </summary>
        private void HandleReturnToMainMenu()
        {
            Time.timeScale = 1f;
            _pauseMenu?.SetPauseButtonVisible(false);

            if (GameSession.IsOnline)
            {
                _onlineSession?.Leave();
                GameSession.Reset();
            }

            CleanupAIPlayers();
            _gameManager?.AbandonGame();
            _saveActive = false;
            _gameStarted = false;
            ShowMainMenu();
        }

        private void HandleReplay()
        {
            Time.timeScale = 1f;

            if (GameSession.IsOnline)
            {
                if (GameSession.IsHost)
                {
                    // L'hôte relance : nouveau seed diffusé à tous les appareils
                    HandleOnlineLaunch();
                }
                else
                {
                    // Un client ne peut pas relancer la partie : retour menu
                    HandleReturnToMainMenu();
                }
                return;
            }

            StartNewSoloGame();
        }

        private void CleanupAIPlayers()
        {
            foreach (AIPlayer ai in _aiPlayers)
            {
                if (ai != null) Destroy(ai);
            }
            _aiPlayers.Clear();
        }

        // ────────────────────────────────────────────────────
        //  PARTIES SOLO
        // ────────────────────────────────────────────────────

        /// <summary>
        /// JOUER depuis le menu : si une partie est sauvegardée, on demande avant
        /// de l'écraser (annuler ramène au menu, la partie reste à reprendre).
        /// </summary>
        private void RequestNewSoloGame()
        {
            ConfirmReplaceSave(StartNewSoloGame, ShowMainMenu);
        }

        private void ConfirmReplaceSave(System.Action proceed, System.Action? cancel)
        {
            SavedGame? saved = SaveGame.Load();
            if (saved == null || _mainCanvas == null)
            {
                proceed();
                return;
            }

            if (_confirm == null)
            {
                _confirm = gameObject.AddComponent<ConfirmDialog>();
                _confirm.Setup(_mainCanvas.transform);
            }
            string what = saved.Mode == 1 ? "La donne du jour" : "Une partie";
            _confirm.Show("Partie en cours",
                $"{what} est sauvegardée (manche {RoundOf(saved)}). La nouvelle partie la remplacera.",
                "REMPLACER", proceed, cancel);
        }

        private static int RoundOf(SavedGame saved)
        {
            int round = 1;
            foreach (IGameCommand c in saved.History)
            {
                if (c is StartNextRoundCommand) round++;
            }
            return round;
        }

        /// <summary>Nouvelle partie solo avec les réglages de "Nouvelle partie".</summary>
        private void StartNewSoloGame()
        {
            _aiPlayerCount = Mathf.Max(1, GameSettings.PlayerCount - _humanPlayerCount);
            StartSoloGame(BuildSoloSetups(), System.Environment.TickCount, GameSettings.BuildMatchRules(),
                GameSettings.Difficulty, mode: 0, dailyKey: 0);
        }

        /// <summary>Donne du jour : même partie pour tout le monde aujourd'hui.</summary>
        private void HandleDaily()
        {
            int key = DailyDeal.TodayKey;
            void Launch()
            {
                StartSoloGame(DailyDeal.Seats(), DailyDeal.SeedFor(key), DailyDeal.Rules, DailyDeal.Difficulty,
                    mode: 1, dailyKey: key);
            }
            void HideAndLaunch()
            {
                if (_mainMenu != null) _mainMenu.Hide(Launch);
                else Launch();
            }
            ConfirmReplaceSave(HideAndLaunch, null);
        }

        private void StartSoloGame(List<PlayerSetup> setups, int seed, MatchRules rules, int difficulty,
            int mode, int dailyKey)
        {
            if (_gameManager == null)
            {
                Debug.LogError("GameManager is not assigned to GameBootstrapper!");
                return;
            }

            GameSession.Reset();
            EnsureGameSystems();
            CleanupAIPlayers();
            _handView?.SetPlayerIndex(0);

            _soloMode = mode;
            _dailyKey = dailyKey;
            _soloDifficulty = difficulty;
            _gameManager.StartGame(setups, seed, rules);
            _saveActive = SaveGame.Begin(setups, seed, rules, difficulty, mode, dailyKey);

            CreateAIPlayers(difficulty, mode == 1 ? seed : (int?)null);
            RefreshPlayerViews();
            InitializePauseAndGameOver();
            _pauseMenu?.SetPauseButtonVisible(true);
            _gameStarted = true;

            if (_handView != null) StartCoroutine(FirstDealCascade());
        }

        /// <summary>REPRENDRE : reconstruit la partie sauvegardée en rejouant ses commandes.</summary>
        private void HandleResumeSolo()
        {
            if (_gameManager == null) return;
            SavedGame? saved = SaveGame.Load();
            if (saved == null)
            {
                RefreshMenuStatus();
                return;
            }

            void Resume()
            {
                GameSession.Reset();
                EnsureGameSystems();
                CleanupAIPlayers();
                _handView?.SetPlayerIndex(0);

                bool ok = _gameManager.RebuildFromHistory(saved.Players, saved.Seed, saved.Rules, saved.History);
                if (!ok || _gameManager.State == GameState.GameOver || _gameManager.State == GameState.Setup)
                {
                    Debug.LogWarning("[Save] Sauvegarde incohérente : nouvelle partie");
                    SaveGame.Delete();
                    StartNewSoloGame();
                    _hudView?.ShowStatus("Sauvegarde illisible : nouvelle partie");
                    return;
                }

                _soloMode = saved.Mode;
                _dailyKey = saved.DailyKey;
                _soloDifficulty = saved.BotDifficulty;
                _saveActive = true;

                RefreshPlayerViews();
                InitializePauseAndGameOver();
                _pauseMenu?.SetPauseButtonVisible(true);
                _gameStarted = true;
                SyncViewsFromModel();

                CreateAIPlayers(saved.BotDifficulty, saved.Mode == 1 ? saved.Seed : (int?)null);

                // Sauvegardée pendant un récap : on enchaîne directement sur la manche suivante.
                if (_gameManager.State == GameState.EndRound)
                {
                    _gameManager.ExecuteCommand(new StartNextRoundCommand(0, _gameManager.RoundNumber));
                }
                foreach (AIPlayer ai in _aiPlayers) ai.PlayTurnIfCurrent();
                _hudView?.ShowStatus($"Reprise · manche {_gameManager.RoundNumber}");
            }

            if (_mainMenu != null) _mainMenu.Hide(Resume);
            else Resume();
        }

        /// <summary>Sièges d'une partie solo : le joueur (son pseudo) puis les bots.</summary>
        private List<PlayerSetup> BuildSoloSetups()
        {
            List<PlayerSetup> setups = new() { new PlayerSetup(PlayerProfile.Name, false) };
            for (int i = 1; i < _humanPlayerCount; i++)
            {
                setups.Add(new PlayerSetup($"Joueur {i + 1}", false));
            }
            for (int i = 0; i < _aiPlayerCount; i++)
            {
                setups.Add(new PlayerSetup($"Bot {i + 1}", true));
            }
            return setups;
        }

        private void CreateAIPlayers(int difficulty, int? seed = null)
        {
            if (_gameManager == null) return;

            for (int i = 0; i < _gameManager.Players.Count; i++)
            {
                if (!_gameManager.Players[i].IsAI) continue;
                AIPlayer ai = gameObject.AddComponent<AIPlayer>();
                ai.Initialize(_gameManager, i, (AIDifficulty)difficulty, GameSettings.BotDelayFactor, seed);
                _aiPlayers.Add(ai);
            }
        }

        /// <summary>
        /// (Re)construit les vues dépendantes du nombre de joueurs et de leurs noms.
        /// </summary>
        private void RefreshPlayerViews()
        {
            if (_gameManager == null) return;

            List<string> names = new();
            foreach (PlayerModel p in _gameManager.Players)
            {
                names.Add(p.Name);
            }

            _levelProgressView?.Initialize(_gameManager.Players.Count, names);
            _levelProgressView?.BindPlayers(_gameManager.Players);
            _hudView?.SetPlayerNames(names);
            _tableView?.SetPlayerNames(names, GameSession.LocalPlayerIndex);
            BindGameOverScreen(names);
            _roundSummary?.Bind(_gameManager.Players);
            _discardPileView?.Initialize(
                _gameManager.Players.Count, names, _gameManager.DeckManager);
        }

        private void BindGameOverScreen(List<string> names)
        {
            if (_gameOverCelebration == null || _gameManager == null) return;
            GameManager gm = _gameManager;
            _gameOverCelebration.PlayerNames = names;
            _gameOverCelebration.Players = gm.Players;
            _gameOverCelebration.RoundsPlayed = () => gm.RoundNumber;
        }

        /// <summary>
        /// Style visuel : caméra sombre, mise en page, récap de manche, fond, cartes en vol.
        /// </summary>
        private void InitializeVisuals()
        {
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = Constants.BackgroundDark;
            }

            if (_mainCanvas != null)
            {
                // Polices de la DA (Inter / DM Serif Display) avant toute création d'UI.
                UISkin.EnsureFonts();

                // L'UI gameplay posée en scène passe sous un conteneur calé sur
                // la safe area (punch-hole, barre de gestes). Le fond et les menus
                // créés ensuite restent full bleed sur tout l'écran.
                RectTransform safeRoot = SafeAreaFitter.Wrap(_mainCanvas);

                _screenLayout = gameObject.AddComponent<GameScreenLayout>();
                _screenLayout.Build(_mainCanvas, safeRoot, _gameManager);
                _screenLayout.BindInput(_inputController);

                // Récap de fin de manche, au centre du tapis.
                _roundSummary = gameObject.AddComponent<RoundSummaryView>();
                _roundSummary.Setup(safeRoot);
                _roundSummary.ContinueRequested += HandleContinue;

                AnimatedBackground animBg = gameObject.AddComponent<AnimatedBackground>();
                animBg.Setup(_mainCanvas);

                // Cartes "en vol" (pioches adverses), au-dessus du jeu, sous les menus.
                _flights = gameObject.AddComponent<CardFlights>();
                _flights.Setup(_mainCanvas, _handView != null ? _handView.CardPrefab : null);
            }
        }

        /// <summary>
        /// Câblage one-shot des systèmes partagés (input, vues, événements) —
        /// commun aux parties solo et en ligne.
        /// </summary>
        private void EnsureGameSystems()
        {
            if (_systemsInitialized || _gameManager == null) return;
            _systemsInitialized = true;

            if (_inputController != null)
            {
                _inputController.Initialize(_gameManager);
            }

            if (_networkManager != null)
            {
                _networkManager.Initialize(_gameManager, _humanPlayerCount);
            }

            _discardPileView?.BindGame(_gameManager);

            // Indicateur de validité de sélection + bouton POSER
            if (_handView != null && _mainCanvas != null)
            {
                SelectionStatusView statusView = gameObject.AddComponent<SelectionStatusView>();
                statusView.Setup(_mainCanvas, _gameManager, _handView, _inputController);
            }

            // Minuteur de tour (règle de partie, inactif sans minuteur)
            _turnTimer = gameObject.AddComponent<TurnTimer>();
            _turnTimer.Initialize(_gameManager, _inputController, _levelProgressView, _hudView);

            // D'où partent les cartes : la main (joueur local) ou la pastille du joueur.
            CardOrigins.Provider = CardOriginOf;
            if (_flights != null && _mainCanvas != null)
            {
                OpponentMovesView moves = gameObject.AddComponent<OpponentMovesView>();
                moves.Initialize(_gameManager, _flights, GameScreenLayout.Find(_mainCanvas.transform, "DeckArea"),
                    _discardPileView, _levelProgressView);
            }

            // Sauvegarde solo : chaque commande réussie est ajoutée au fichier.
            _gameManager.CommandExecuted += OnCommandExecuted;

            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Subscribe<RoundEndedEvent>(OnRoundEndedStats);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        private CardOrigin? CardOriginOf(int playerIndex, CardModel card)
        {
            if (_gameManager == null || playerIndex < 0 || playerIndex >= _gameManager.Players.Count) return null;
            if (GameSession.IsLocalHuman(_gameManager.Players[playerIndex])
                && _handView != null && _handView.TryGetCardOrigin(card.Id, out CardOrigin fromHand))
            {
                return fromHand;
            }
            if (_levelProgressView != null && _levelProgressView.TryGetPillOrigin(playerIndex, out CardOrigin pill))
            {
                return pill;
            }
            return null;
        }

        private void OnCommandExecuted(IGameCommand command, CommandResult result)
        {
            if (_saveActive && !GameSession.IsOnline) SaveGame.Append(command);
        }

        /// <summary>CONTINUER sur le récap (ou suite automatique) : manche suivante.</summary>
        private void HandleContinue(int finishedRound)
        {
            if (_gameManager == null || _gameManager.State != GameState.EndRound) return;
            if (GameSession.IsOnline && !GameSession.IsHost) return;
            int seat = GameSession.IsOnline ? GameSession.LocalPlayerIndex : 0;
            _gameManager.ExecuteCommand(new StartNextRoundCommand(seat, finishedRound));
        }

        private void OnRoundEndedStats(RoundEndedEvent evt)
        {
            PlayerModel? local = GameSession.GetLocalPlayer(_gameManager);
            if (local != null && local.Index == evt.WinnerIndex) PlayerProfile.RecordRoundWon();
        }

        private void OnGameOver(GameOverEvent evt)
        {
            PlayerModel? local = GameSession.GetLocalPlayer(_gameManager);
            bool won = local != null && local.Index == evt.WinnerIndex;
            if (local != null) PlayerProfile.RecordGame(won);

            if (!GameSession.IsOnline)
            {
                if (_soloMode == 1 && _gameManager != null) DailyDeal.Record(_dailyKey, won, _gameManager.RoundNumber);
                if (_saveActive) SaveGame.Delete();
                _saveActive = false;
            }
        }

        /// <summary>
        /// Deal cascade pour le premier round (les cartes sont déjà dans la main).
        /// </summary>
        private IEnumerator FirstDealCascade()
        {
            // Attendre une frame pour que tout soit initialisé
            yield return null;

            // La main affichée est TOUJOURS celle du joueur local — en ligne le
            // premier joueur du round n'est pas forcément celui de cet appareil.
            PlayerModel? player = GameSession.GetLocalPlayer(_gameManager);
            if (player != null && _handView != null)
            {
                _inputController?.ApplyPreferredSort(player);
                _handView.RefreshHand(player.Hand, dealAnimation: true);
            }
        }

        /// <summary>
        /// Au début de chaque nouveau round, deal cascade.
        /// </summary>
        private void OnRoundStarted(RoundStartedEvent evt)
        {
            if (evt.RoundNumber <= 1) return; // Premier round géré par FirstDealCascade
            if (_syncingViews) return;        // Reprise : main posée sans cascade

            if (_handView != null && _gameManager != null)
            {
                PlayerModel? local = GameSession.GetLocalPlayer(_gameManager);
                if (local != null)
                {
                    StartCoroutine(DealAfterDelay(local));
                }
            }
        }

        private IEnumerator DealAfterDelay(PlayerModel player)
        {
            yield return null; // Attendre que les cartes soient distribuées
            _inputController?.ApplyPreferredSort(player);
            _handView?.RefreshHand(player.Hand, dealAnimation: true);
        }

        // ────────────────────────────────────────────────────
        //  MULTIJOUEUR EN LIGNE
        // ────────────────────────────────────────────────────

        /// <summary>Bouton EN LIGNE du menu : ouvre le lobby.</summary>
        private void HandleMenuOnline()
        {
            EnsureOnlineSession();

            if (_onlinePanel == null && _mainMenu?.Root != null)
            {
                _onlinePanel = gameObject.AddComponent<OnlinePanel>();
                _onlinePanel.Setup(_mainMenu.Root);
                _onlinePanel.CreateRequested += HandleOnlineCreate;
                _onlinePanel.JoinRequested += HandleOnlineJoin;
                _onlinePanel.LaunchRequested += HandleOnlineLaunch;
                _onlinePanel.SettingsRequested += HandleOnlineSettings;
                _onlinePanel.Closed += HandleOnlineClosed;
            }

            if (_onlinePanel != null) _onlinePanel.LastCode = OnlineSession.LastJoinCode;

            _onlinePanel?.Show();
        }

        private void EnsureOnlineSession()
        {
            if (_onlineSession != null) return;

            _onlineSession = gameObject.AddComponent<OnlineSession>();
            _onlineSession.LobbyChanged += HandleLobbyChanged;
            _onlineSession.ConnectedToHost += () =>
            {
                _onlinePanel?.ShowClientWaiting();
                if (_onlineSession?.CurrentLobby != null) HandleLobbyChanged(_onlineSession.CurrentLobby);
            };
            _onlineSession.MatchStartReceived += LaunchOnlineMatch;
            _onlineSession.PlayerReplacedByAI += HandlePlayerReplacedByAI;
            _onlineSession.SessionEnded += HandleSessionEnded;
            _onlineSession.Desynchronized += msg => _hudView?.ShowStatus(msg);
            _onlineSession.ResumeReceived += HandleResume;
            _onlineSession.PlayerRestoredToHuman += HandlePlayerRestoredToHuman;
            _onlineSession.Notice += msg => _hudView?.ShowStatus(msg);
            _onlineSession.Reconnecting += msg => GetConnectionOverlay()?.Show(msg);
            _onlineSession.Reconnected += () =>
                GetConnectionOverlay()?.Show("Reconnecté !\nReprise de la partie…");
        }

        private ConnectionOverlay? GetConnectionOverlay()
        {
            if (_connectionOverlay == null && _mainCanvas != null)
            {
                _connectionOverlay = gameObject.AddComponent<ConnectionOverlay>();
                _connectionOverlay.Setup(_mainCanvas);
            }
            return _connectionOverlay;
        }

        /// <summary>Héberge une partie : alloue le relay et affiche le code.</summary>
        private async void HandleOnlineCreate()
        {
            if (_onlineSession == null || _onlinePanel == null) return;

            _onlinePanel.ShowBusy("Création de la partie…");
            try
            {
                string code = await _onlineSession.HostAsync();
                _onlineSession.SetLobbySettings(GameSettings.PlayerCount, GameSettings.BuildMatchRules(),
                    GameSettings.Difficulty);
                _onlinePanel.ShowHostLobby(code);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Online] Échec création : {e}");
                _onlinePanel.ShowHome();
                _onlinePanel.SetStatus(OnlineSession.DescribeError(e));
            }
        }

        /// <summary>Rejoint la partie d'un ami via son code.</summary>
        private async void HandleOnlineJoin(string code)
        {
            if (_onlineSession == null || _onlinePanel == null) return;

            _onlinePanel.ShowBusy("Connexion…");
            try
            {
                await _onlineSession.JoinAsync(code);
                // La suite (ShowClientWaiting) arrive via ConnectedToHost
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Online] Échec join : {e}");
                _onlinePanel.ShowJoinEntry();
                _onlinePanel.SetStatus(OnlineSession.DescribeError(e));
            }
        }

        /// <summary>Salon mis à jour (hôte ou client) : liste des joueurs + règles.</summary>
        private void HandleLobbyChanged(LobbyInfo info)
        {
            if (_onlinePanel == null) return;
            LobbyViewModel view = new()
            {
                Seats = info.Seats,
                BotDifficulty = info.BotDifficulty,
                Rules = info.Rules
            };
            foreach (LobbyMember m in info.Members)
            {
                view.Members.Add(new LobbySeatView(m.Name, m.IsHost, m.ClientId == info.LocalClientId));
            }
            _onlinePanel.UpdateLobby(view);
        }

        /// <summary>Hôte : réglages de la partie (même panneau qu'en solo), diffusés au salon.</summary>
        private void HandleOnlineSettings()
        {
            MatchSetupPanel? setup = _mainMenu?.MatchSetup;
            if (setup == null || _onlineSession == null) return;
            setup.ShowOnlineHost(() =>
            {
                _onlineSession?.SetLobbySettings(GameSettings.PlayerCount, GameSettings.BuildMatchRules(),
                    GameSettings.Difficulty);
            });
        }

        /// <summary>Hôte : fige les sièges et lance la partie partout.</summary>
        private void HandleOnlineLaunch()
        {
            if (_onlineSession == null) return;

            try
            {
                OnlineStartData data =
                    _onlineSession.BuildAndBroadcastStart(GameSettings.PlayerCount,
                        GameSettings.BuildMatchRules());
                LaunchOnlineMatch(data);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Online] Échec lancement : {e}");
                _onlinePanel?.SetStatus("Impossible de lancer la partie");
            }
        }

        /// <summary>Fermeture du lobby avant lancement = quitter la session.</summary>
        private void HandleOnlineClosed()
        {
            if (!GameSession.IsOnline)
            {
                _onlineSession?.Leave();
            }
        }

        /// <summary>
        /// Démarre la partie en ligne sur CET appareil (hôte comme client) :
        /// même liste de sièges + même seed = simulation identique partout.
        /// </summary>
        private void LaunchOnlineMatch(OnlineStartData data)
        {
            if (_gameManager == null || _onlineSession == null) return;

            GameSession.IsOnline = true;
            GameSession.IsHost = _onlineSession.IsSessionHost;
            GameSession.LocalPlayerIndex = data.LocalSeat;
            _saveActive = false; // une partie en ligne ne remplace pas la sauvegarde solo

            _onlinePanel?.HideSilent();
            _mainMenu?.Hide();
            _connectionOverlay?.Hide();

            EnsureGameSystems();
            CleanupAIPlayers();

            _handView?.SetPlayerIndex(data.LocalSeat);
            _gameManager.StartGame(data.Players, data.Seed, data.Rules ?? MatchRules.Classic);
            _onlineSession.BindGameManager(_gameManager);

            // Les bots ne tournent QUE chez l'hôte ; leurs commandes sont relayées
            if (GameSession.IsHost)
            {
                CreateAIPlayers(GameSettings.Difficulty);
            }

            RefreshPlayerViews();
            InitializePauseAndGameOver();
            _pauseMenu?.SetPauseButtonVisible(true);
            _gameStarted = true;

            if (_handView != null)
            {
                StartCoroutine(FirstDealCascade());
            }
        }

        /// <summary>
        /// Un humain a quitté la partie en ligne : son siège devient un bot
        /// (côté hôte, un AIPlayer prend la relève immédiatement).
        /// </summary>
        private void HandlePlayerReplacedByAI(int seat)
        {
            if (_gameManager == null || seat < 0 || seat >= _gameManager.Players.Count) return;

            string name = _gameManager.Players[seat].Name;
            _gameManager.ConvertPlayerToAI(seat);
            _hudView?.ShowStatus($"{name} a quitté — un bot le remplace");

            if (GameSession.IsOnline && GameSession.IsHost)
            {
                AIPlayer ai = gameObject.AddComponent<AIPlayer>();
                ai.Initialize(_gameManager, seat, (AIDifficulty)GameSettings.Difficulty, GameSettings.BotDelayFactor);
                _aiPlayers.Add(ai);
                ai.PlayTurnIfCurrent();
            }
        }

        /// <summary>
        /// Un joueur revenu reprend son siège : le bot qui jouait pour lui
        /// (côté hôte) s'arrête immédiatement, même au milieu du tour.
        /// </summary>
        private void HandlePlayerRestoredToHuman(int seat)
        {
            if (_gameManager == null || seat < 0 || seat >= _gameManager.Players.Count) return;

            _gameManager.ConvertAIToPlayer(seat);
            for (int i = _aiPlayers.Count - 1; i >= 0; i--)
            {
                AIPlayer ai = _aiPlayers[i];
                if (ai != null && ai.PlayerIndex != seat) continue;
                if (ai != null) Destroy(ai);
                _aiPlayers.RemoveAt(i);
            }
        }

        /// <summary>
        /// Client : reconstruit la partie à partir de l'historique envoyé par
        /// l'hôte (reconnexion ou désync corrigée), puis recale toutes les vues
        /// d'un bloc sur le modèle.
        /// </summary>
        private void HandleResume(OnlineResumeData data)
        {
            if (_gameManager == null || _onlineSession == null) return;

            GameSession.IsOnline = true;
            GameSession.IsHost = false;
            GameSession.LocalPlayerIndex = data.Start.LocalSeat;
            _saveActive = false;

            _onlinePanel?.HideSilent();
            _mainMenu?.Hide();

            EnsureGameSystems();
            CleanupAIPlayers();
            _handView?.SetPlayerIndex(data.Start.LocalSeat);

            bool ok = _gameManager.RebuildFromHistory(data.Start.Players, data.Start.Seed,
                data.Start.Rules ?? MatchRules.Classic, data.History);
            int hash = _gameManager.ComputeStateHash();
            if (!ok || hash != data.ExpectedHash)
            {
                Debug.LogError($"[Online] Reprise incohérente (ok={ok}, hash {hash} ≠ {data.ExpectedHash})");
                _hudView?.ShowError("Reprise imparfaite — si ça bloque, relance la partie");
            }

            _onlineSession.BindGameManager(_gameManager);
            RefreshPlayerViews();
            InitializePauseAndGameOver();
            _pauseMenu?.SetPauseButtonVisible(true);
            _gameStarted = true;

            SyncViewsFromModel();
            _connectionOverlay?.Hide();
        }

        /// <summary>
        /// Recale toutes les vues sur l'état courant du modèle, sans rejouer
        /// l'historique visuellement : événements "d'état" + resync dédiées.
        /// </summary>
        private void SyncViewsFromModel()
        {
            if (_gameManager == null) return;
            TurnManager? tm = _gameManager.TurnManager;
            DeckManager? deck = _gameManager.DeckManager;
            if (tm == null || deck == null) return;

            _syncingViews = true;
            try
            {
                EventBus.Publish(new GameStartedEvent { PlayerCount = _gameManager.Players.Count });
                EventBus.Publish(new RoundStartedEvent { RoundNumber = _gameManager.RoundNumber });

                _tableView?.SyncFromModel(_gameManager.Players);
                _discardPileView?.SyncFromModel();
                foreach (PlayerModel p in _gameManager.Players)
                {
                    _levelProgressView?.UpdatePlayerLevel(p.Index, p.CurrentLevel);
                }

                EventBus.Publish(new DeckChangedEvent { CardsRemaining = deck.DrawPileCount });

                if (_gameManager.State == GameState.PlayerTurn)
                {
                    PlayerModel current = tm.CurrentPlayer;
                    EventBus.Publish(new TurnStartedEvent
                    {
                        PlayerIndex = current.Index,
                        Phase = tm.CurrentPhase,
                        PlayerLevel = current.CurrentLevel,
                        HasLaidDown = current.HasLaidDownThisRound
                    });
                    EventBus.Publish(new TurnPhaseChangedEvent
                    {
                        PlayerIndex = current.Index,
                        NewPhase = tm.CurrentPhase
                    });
                }

                PlayerModel? local = GameSession.GetLocalPlayer(_gameManager);
                if (local != null)
                {
                    _inputController?.ApplyPreferredSort(local);
                    _handView?.RefreshHand(local.Hand, dealAnimation: false);
                }
            }
            finally
            {
                _syncingViews = false;
            }
        }

        /// <summary>Session réseau terminée (hôte parti, connexion perdue…).</summary>
        private void HandleSessionEnded(string reason)
        {
            _connectionOverlay?.Hide();
            if (!GameSession.IsOnline)
            {
                // Encore dans le lobby : afficher l'erreur sans quitter le menu
                _onlineSession?.Leave();
                if (_onlinePanel != null && _onlinePanel.IsVisible)
                {
                    _onlinePanel.ShowHome();
                    _onlinePanel.SetStatus(reason);
                }
                return;
            }

            _hudView?.ShowStatus(reason);
            HandleReturnToMainMenu();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Unsubscribe<RoundEndedEvent>(OnRoundEndedStats);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            if (_gameManager != null) _gameManager.CommandExecuted -= OnCommandExecuted;
            CardOrigins.Provider = null;
            _onlineSession?.Leave();

            foreach (AIPlayer ai in _aiPlayers)
            {
                if (ai != null) Destroy(ai);
            }
            _aiPlayers.Clear();
        }
    }
}
