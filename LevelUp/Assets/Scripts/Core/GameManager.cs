using System.Collections.Generic;
using UnityEngine;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Machine à états principale du jeu : Setup → PlayerTurn → Validate → EndRound → GameOver.
    /// Point d'entrée du jeu, orchestre tous les systèmes.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameConfig? _config;

        private GameState _state = GameState.Setup;
        private List<PlayerModel> _players = new();
        private DeckManager? _deckManager;
        private TurnManager? _turnManager;
        private ActionCardHandler? _actionHandler;
        private int _roundNumber;
        private int _roundStarterIndex;

        /// <summary>État actuel de la partie.</summary>
        public GameState State => _state;

        /// <summary>Liste des joueurs.</summary>
        public IReadOnlyList<PlayerModel> Players => _players;

        /// <summary>Le TurnManager actif.</summary>
        public TurnManager? TurnManager => _turnManager;

        /// <summary>Le DeckManager actif.</summary>
        public DeckManager? DeckManager => _deckManager;

        /// <summary>La config du jeu.</summary>
        public GameConfig? Config => _config;

        /// <summary>Numéro du round actuel.</summary>
        public int RoundNumber => _roundNumber;

        /// <summary>
        /// Initialise et démarre une nouvelle partie.
        /// </summary>
        public void StartGame(int humanPlayers, int aiPlayers)
        {
            if (_config == null)
            {
                Debug.LogError("GameConfig is not assigned!");
                return;
            }

            _state = GameState.Setup;
            _players.Clear();
            _roundNumber = 0;

            // Créer les joueurs
            int totalPlayers = humanPlayers + aiPlayers;
            for (int i = 0; i < humanPlayers; i++)
            {
                _players.Add(new PlayerModel(i, $"Player {i + 1}", false));
            }
            for (int i = 0; i < aiPlayers; i++)
            {
                _players.Add(new PlayerModel(humanPlayers + i, $"Bot {i + 1}", true));
            }

            // Initialiser les systèmes
            _deckManager = new DeckManager(_config);
            _actionHandler = new ActionCardHandler(_deckManager, _players);
            _turnManager = new TurnManager(_players, _deckManager, _actionHandler, _config);

            _roundStarterIndex = 0;

            EventBus.Publish(new GameStartedEvent { PlayerCount = totalPlayers });

            StartNewRound();
        }

        /// <summary>
        /// Démarre un nouveau round : mélange, distribue, et lance le premier tour.
        /// </summary>
        private void StartNewRound()
        {
            _roundNumber++;
            _state = GameState.PlayerTurn;

            // Reset des joueurs pour le nouveau round
            foreach (PlayerModel player in _players)
            {
                player.ResetForNewRound();
            }

            // Créer et mélanger le deck, distribuer
            _deckManager!.CreateAndShuffle(_players.Count);
            List<List<CardModel>> hands = _deckManager.Deal(_players.Count);

            for (int i = 0; i < _players.Count; i++)
            {
                foreach (CardModel card in hands[i])
                {
                    _players[i].AddToHand(card);
                }
            }

            EventBus.Publish(new RoundStartedEvent { RoundNumber = _roundNumber });

            // Démarrer le premier tour
            _turnManager!.StartRound(_roundStarterIndex);
        }

        /// <summary>
        /// Appelé quand un joueur termine son tour en vidant sa main.
        /// Gère la fin du round et la progression des niveaux.
        /// </summary>
        public void OnRoundEnd(int winnerIndex)
        {
            _state = GameState.EndRound;

            PlayerModel winner = _players[winnerIndex];

            EventBus.Publish(new RoundEndedEvent { WinnerIndex = winnerIndex });

            // Le gagnant du round saute un niveau (bonus)
            if (winner.HasLaidDownThisRound)
            {
                winner.CurrentLevel += 2; // Saute un niveau
            }
            else
            {
                winner.CurrentLevel += 1;
            }

            // Les autres joueurs ayant posé leur niveau avancent d'un niveau
            foreach (PlayerModel player in _players)
            {
                if (player.Index == winnerIndex) continue;

                if (player.HasLaidDownThisRound)
                {
                    player.CurrentLevel += 1;
                }
                // Les joueurs qui n'ont pas posé restent au même niveau
            }

            // Vérifier si quelqu'un a gagné la partie
            foreach (PlayerModel player in _players)
            {
                if (player.CurrentLevel > Constants.MaxLevel)
                {
                    _state = GameState.GameOver;
                    EventBus.Publish(new GameOverEvent { WinnerIndex = player.Index });
                    return;
                }
            }

            // Prochain round
            _roundStarterIndex = (winnerIndex + 1) % _players.Count;
            StartNewRound();
        }

        /// <summary>
        /// Vérifie si le round vient de se terminer (appelé après chaque défausse).
        /// </summary>
        public void CheckRoundEnd()
        {
            if (_turnManager == null) return;

            PlayerModel current = _turnManager.CurrentPlayer;
            if (current.IsHandEmpty)
            {
                OnRoundEnd(current.Index);
            }
        }

        /// <summary>
        /// Retourne le joueur actif.
        /// </summary>
        public PlayerModel? GetCurrentPlayer()
        {
            return _turnManager?.CurrentPlayer;
        }

        private void OnDestroy()
        {
            EventBus.Clear();
        }
    }
}
