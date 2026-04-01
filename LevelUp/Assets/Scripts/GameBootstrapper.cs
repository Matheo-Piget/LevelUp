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
    /// Câble tous les systèmes ensemble et lance la partie.
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

        [Header("Network")]
        [SerializeField] private NetworkManager? _networkManager;

        [Header("Game Settings")]
        [SerializeField] private int _humanPlayerCount = 1;
        [SerializeField] private int _aiPlayerCount = 3;

        private readonly List<AIPlayer> _aiPlayers = new();

        private void Start()
        {
            InitializeGame();
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

            // Initialiser le background
            Camera.main!.backgroundColor = Constants.BackgroundNavy;

            // Initialiser les vues
            if (_handView != null)
            {
                _handView.SetPlayerIndex(0); // Joueur local = index 0
            }

            if (_inputController != null)
            {
                _inputController.Initialize(_gameManager);
            }

            // Initialiser le network
            if (_networkManager != null)
            {
                _networkManager.Initialize(_gameManager, _humanPlayerCount);
            }

            // Démarrer la partie
            _gameManager.StartGame(_humanPlayerCount, _aiPlayerCount);

            // Créer les AIPlayers après que les joueurs soient créés
            for (int i = 0; i < _gameManager.Players.Count; i++)
            {
                if (_gameManager.Players[i].IsAI)
                {
                    AIPlayer ai = gameObject.AddComponent<AIPlayer>();
                    ai.Initialize(_gameManager, i);
                    _aiPlayers.Add(ai);
                }
            }

            // Initialiser la progression des niveaux
            if (_levelProgressView != null)
            {
                List<string> names = new();
                foreach (PlayerModel p in _gameManager.Players)
                {
                    names.Add(p.Name);
                }
                _levelProgressView.Initialize(_gameManager.Players.Count, names);
            }
        }

        private void OnDestroy()
        {
            foreach (AIPlayer ai in _aiPlayers)
            {
                if (ai != null) Destroy(ai);
            }
            _aiPlayers.Clear();
        }
    }
}
