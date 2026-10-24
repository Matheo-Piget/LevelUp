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
    /// Câble tous les systèmes, applique le style visuel Balatro, et lance la partie.
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
        [SerializeField] private TurnBannerView? _turnBannerView;

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

        private void Start()
        {
            ApplyDeviceSettings();
            GameSettings.Initialize();
            InitializeVisuals();
            ShowMainMenu();
        }

        /// <summary>
        /// Réglages device : demande 120 fps — sur Android, Swappy transmet la
        /// cible à l'OS qui bascule l'écran sur son mode le plus rapide (120 Hz
        /// sur Galaxy S25). Ne PAS lire Screen.currentResolution au démarrage :
        /// elle rapporte le mode courant (souvent 60 Hz tant qu'aucune app n'a
        /// demandé plus), pas le maximum de la dalle. Si l'écran est limité à
        /// 60 Hz, vsync plafonne sans autre effet. Empêche aussi la mise en
        /// veille pendant qu'on lit sa main.
        /// </summary>
        private static void ApplyDeviceSettings()
        {
            Application.targetFrameRate = 120;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        /// <summary>
        /// Affiche le menu principal. La partie démarre quand le joueur clique sur JOUER.
        /// </summary>
        private void ShowMainMenu()
        {
            if (_mainCanvas == null)
            {
                // Fallback : démarre direct si pas de canvas
                InitializeGame();
                return;
            }

            if (_mainMenu == null)
            {
                _mainMenu = gameObject.AddComponent<MainMenuController>();
                _mainMenu.OnPlayClicked += HandleMenuPlay;
                _mainMenu.OnOnlineClicked += HandleMenuOnline;
                _mainMenu.Setup(_mainCanvas);
            }
            else
            {
                _mainMenu.Show();
            }
        }

        private void HandleMenuPlay()
        {
            if (_gameStarted)
            {
                // Reprise depuis pause → rien à initialiser, juste masquer menu
                _pauseMenu?.SetPauseButtonVisible(true);
                return;
            }
            InitializeGame();
            InitializePauseAndGameOver();
            // Le bouton pause a pu être masqué par un précédent retour au menu.
            _pauseMenu?.SetPauseButtonVisible(true);
            _gameStarted = true;
        }

        private void InitializePauseAndGameOver()
        {
            if (_mainCanvas == null) return;

            if (_pauseMenu == null)
            {
                _pauseMenu = gameObject.AddComponent<PauseMenuController>();
                _pauseMenu.OnMainMenuRequested += HandleReturnToMainMenu;
                _pauseMenu.Setup(_mainCanvas);
            }

            if (_gameOverCelebration == null)
            {
                _gameOverCelebration = gameObject.AddComponent<GameOverCelebration>();
                _gameOverCelebration.OnReplayClicked += HandleReplay;
                _gameOverCelebration.OnMainMenuClicked += HandleReturnToMainMenu;
                _gameOverCelebration.Setup(_mainCanvas);
            }
        }

        private void HandleReturnToMainMenu()
        {
            // Réinitialise le temps et la partie, puis réaffiche le menu
            Time.timeScale = 1f;
            _pauseMenu?.SetPauseButtonVisible(false);

            // Quitter proprement la session en ligne le cas échéant
            if (GameSession.IsOnline)
            {
                _onlineSession?.Leave();
                GameSession.Reset();
            }

            RestartGame();
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

            RestartGame();
        }

        private void CleanupAIPlayers()
        {
            foreach (AIPlayer ai in _aiPlayers)
            {
                if (ai != null) Destroy(ai);
            }
            _aiPlayers.Clear();
        }

        private void RestartGame()
        {
            CleanupAIPlayers();

            // Relance une partie SOLO (post-replay ou retour de session en ligne)
            if (_gameManager != null)
            {
                _handView?.SetPlayerIndex(0);
                ApplyMatchSettings();
                _gameManager.StartGame(_humanPlayerCount, _aiPlayerCount, GameSettings.RandomContracts);
                CreateAIPlayers();

                // Le nombre de joueurs a pu changer dans les options entre
                // deux parties : les vues indexées par joueur sont reconstruites.
                RefreshPlayerViews();

                if (_handView != null)
                {
                    StartCoroutine(FirstDealCascade());
                }
            }
        }

        /// <summary>
        /// Applique les réglages de partie choisis dans les options
        /// (nombre de joueurs total → nombre de bots).
        /// </summary>
        private void ApplyMatchSettings()
        {
            _aiPlayerCount = Mathf.Max(1, GameSettings.PlayerCount - _humanPlayerCount);
        }

        private void CreateAIPlayers()
        {
            if (_gameManager == null) return;

            AIDifficulty difficulty = (AIDifficulty)GameSettings.Difficulty;
            for (int i = 0; i < _gameManager.Players.Count; i++)
            {
                if (_gameManager.Players[i].IsAI)
                {
                    AIPlayer ai = gameObject.AddComponent<AIPlayer>();
                    ai.Initialize(_gameManager, i, difficulty);
                    _aiPlayers.Add(ai);
                }
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
            _discardPileView?.Initialize(
                _gameManager.Players.Count, names, _gameManager.DeckManager);
        }

        /// <summary>
        /// Applique le style visuel Balatro : caméra sombre + fond animé + glow joueur.
        /// </summary>
        private void InitializeVisuals()
        {
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = Constants.BackgroundDark;
            }

            if (_mainCanvas != null)
            {
                // L'UI gameplay posée en scène passe sous un conteneur calé sur
                // la safe area (punch-hole du S25, barre de gestes). Le fond et
                // les menus créés ensuite restent full bleed sur tout l'écran.
                SafeAreaFitter.Wrap(_mainCanvas);

                // Fond animé avec blobs colorés qui dérivent
                AnimatedBackground animBg = gameObject.AddComponent<AnimatedBackground>();
                animBg.Setup(_mainCanvas);
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

            // Indicateur de validité de sélection en temps réel
            if (_handView != null && _mainCanvas != null)
            {
                SelectionStatusView statusView = gameObject.AddComponent<SelectionStatusView>();
                statusView.Setup(_mainCanvas, _gameManager, _handView);
            }

            // Écouter le début de round pour le deal cascade
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
        }

        /// <summary>
        /// Initialise tous les systèmes et démarre une partie SOLO (vs bots).
        /// </summary>
        private void InitializeGame()
        {
            if (_gameManager == null)
            {
                Debug.LogError("GameManager is not assigned to GameBootstrapper!");
                return;
            }

            GameSession.Reset();
            EnsureGameSystems();

            if (_handView != null)
            {
                _handView.SetPlayerIndex(0);
            }

            // Démarrer la partie avec les réglages choisis dans les options
            ApplyMatchSettings();
            _gameManager.StartGame(_humanPlayerCount, _aiPlayerCount, GameSettings.RandomContracts);
            CreateAIPlayers();

            // Vues indexées par joueur (progression, HUD, piles de défausse)
            RefreshPlayerViews();

            // Lancer le deal cascade pour le premier round
            if (_handView != null)
            {
                StartCoroutine(FirstDealCascade());
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
                _handView.RefreshHand(player.Hand, dealAnimation: true);
            }
        }

        /// <summary>
        /// Au début de chaque nouveau round, deal cascade.
        /// </summary>
        private void OnRoundStarted(RoundStartedEvent evt)
        {
            if (evt.RoundNumber <= 1) return; // Premier round géré par FirstDealCascade
            if (_syncingViews) return;        // Reprise en ligne : main posée sans cascade

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
                _onlinePanel.Closed += HandleOnlineClosed;
            }

            if (_onlinePanel != null) _onlinePanel.LastCode = OnlineSession.LastJoinCode;

            _onlinePanel?.Show();
        }

        private void EnsureOnlineSession()
        {
            if (_onlineSession != null) return;

            _onlineSession = gameObject.AddComponent<OnlineSession>();
            _onlineSession.LobbyCountChanged += count => _onlinePanel?.SetLobbyCount(count);
            _onlineSession.ConnectedToHost += () => _onlinePanel?.ShowClientWaiting();
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
                _onlinePanel.ShowHostLobby(code, _onlineSession.LobbyHumanCount);
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

        /// <summary>Hôte : fige les sièges et lance la partie partout.</summary>
        private void HandleOnlineLaunch()
        {
            if (_onlineSession == null) return;

            try
            {
                OnlineStartData data =
                    _onlineSession.BuildAndBroadcastStart(GameSettings.PlayerCount,
                        GameSettings.RandomContracts);
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

            _onlinePanel?.HideSilent();
            _mainMenu?.Hide();
            _connectionOverlay?.Hide();

            EnsureGameSystems();
            CleanupAIPlayers();

            _handView?.SetPlayerIndex(data.LocalSeat);
            _gameManager.StartGame(data.Players, data.Seed, data.RandomContracts);
            _onlineSession.BindGameManager(_gameManager);

            // Les bots ne tournent QUE chez l'hôte ; leurs commandes sont relayées
            if (GameSession.IsHost)
            {
                CreateAIPlayers();
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
                AIDifficulty difficulty = (AIDifficulty)GameSettings.Difficulty;
                AIPlayer ai = gameObject.AddComponent<AIPlayer>();
                ai.Initialize(_gameManager, seat, difficulty);
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

            _onlinePanel?.HideSilent();
            _mainMenu?.Hide();

            EnsureGameSystems();
            CleanupAIPlayers();
            _handView?.SetPlayerIndex(data.Start.LocalSeat);

            bool ok = _gameManager.RebuildFromHistory(data.Start.Players, data.Start.Seed,
                data.Start.RandomContracts, data.History);
            int hash = _gameManager.ComputeStateHash();
            if (!ok || hash != data.ExpectedHash)
            {
                Debug.LogError($"[Online] Reprise incohérente (ok={ok}, hash {hash} ≠ {data.ExpectedHash})");
                _hudView?.ShowStatus("Reprise imparfaite — si ça bloque, relance la partie");
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

                PlayerModel? local = GameSession.GetLocalPlayer(_gameManager);
                if (local != null) _handView?.RefreshHand(local.Hand, dealAnimation: false);
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
            _onlineSession?.Leave();

            foreach (AIPlayer ai in _aiPlayers)
            {
                if (ai != null) Destroy(ai);
            }
            _aiPlayers.Clear();
        }
    }
}
