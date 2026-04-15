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
        private bool _gameStarted;

        private void Start()
        {
            GameSettings.Initialize();
            InitializeVisuals();
            ShowMainMenu();
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
                return;
            }
            InitializeGame();
            InitializePauseAndGameOver();
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
            RestartGame();
            _gameStarted = false;
            ShowMainMenu();
        }

        private void HandleReplay()
        {
            Time.timeScale = 1f;
            RestartGame();
        }

        private void RestartGame()
        {
            // Nettoie les AIs précédents
            foreach (AIPlayer ai in _aiPlayers)
            {
                if (ai != null) Destroy(ai);
            }
            _aiPlayers.Clear();

            // Relance la partie
            if (_gameManager != null)
            {
                _gameManager.StartGame(_humanPlayerCount, _aiPlayerCount);

                for (int i = 0; i < _gameManager.Players.Count; i++)
                {
                    if (_gameManager.Players[i].IsAI)
                    {
                        AIPlayer ai = gameObject.AddComponent<AIPlayer>();
                        ai.Initialize(_gameManager, i);
                        _aiPlayers.Add(ai);
                    }
                }

                if (_handView != null)
                {
                    StartCoroutine(FirstDealCascade());
                }
            }
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
                // Fond animé avec blobs colorés qui dérivent
                AnimatedBackground animBg = gameObject.AddComponent<AnimatedBackground>();
                animBg.Setup(_mainCanvas);

                // Glow coloré sur les bords selon le joueur actif
                PlayerTurnGlow turnGlow = gameObject.AddComponent<PlayerTurnGlow>();
                turnGlow.Setup(_mainCanvas);
            }
        }

        /// <summary>
        /// Initialise tous les systèmes et démarre la partie.
        /// </summary>
        private void InitializeGame()
        {
            if (_gameManager == null)
            {
                Debug.LogError("GameManager is not assigned to GameBootstrapper!");
                return;
            }

            // Initialiser les vues
            if (_handView != null)
            {
                _handView.SetPlayerIndex(0);
            }

            if (_inputController != null)
            {
                _inputController.Initialize(_gameManager);
            }

            if (_networkManager != null)
            {
                _networkManager.Initialize(_gameManager, _humanPlayerCount);
            }

            // Démarrer la partie
            _gameManager.StartGame(_humanPlayerCount, _aiPlayerCount);

            // Créer les AIPlayers
            for (int i = 0; i < _gameManager.Players.Count; i++)
            {
                if (_gameManager.Players[i].IsAI)
                {
                    AIPlayer ai = gameObject.AddComponent<AIPlayer>();
                    ai.Initialize(_gameManager, i);
                    _aiPlayers.Add(ai);
                }
            }

            // Noms des joueurs
            List<string> names = new();
            foreach (PlayerModel p in _gameManager.Players)
            {
                names.Add(p.Name);
            }

            // Initialiser la progression des niveaux
            if (_levelProgressView != null)
            {
                _levelProgressView.Initialize(_gameManager.Players.Count, names);
            }

            // Initialiser les piles de défausse
            if (_discardPileView != null)
            {
                _discardPileView.Initialize(_gameManager.Players.Count, names);
            }

            // Indicateur de validité de sélection en temps réel
            if (_handView != null && _mainCanvas != null)
            {
                SelectionStatusView statusView = gameObject.AddComponent<SelectionStatusView>();
                statusView.Setup(_mainCanvas, _gameManager, _handView);
            }

            // Écouter le début de round pour le deal cascade
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);

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

            PlayerModel? player = _gameManager?.GetCurrentPlayer();
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

            if (_handView != null && _gameManager != null)
            {
                PlayerModel? local = null;
                foreach (PlayerModel p in _gameManager.Players)
                {
                    if (!p.IsAI) { local = p; break; }
                }

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

        private void OnDestroy()
        {
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);

            foreach (AIPlayer ai in _aiPlayers)
            {
                if (ai != null) Destroy(ai);
            }
            _aiPlayers.Clear();
        }
    }
}
