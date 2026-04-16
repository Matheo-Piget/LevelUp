using System.Collections.Generic;
using UnityEngine;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Machine à états principale du jeu : Setup → PlayerTurn → EndRound → GameOver.
    /// Orchestre les systèmes et expose le <see cref="GameCommandExecutor"/>
    /// comme point d'entrée unique pour toutes les actions.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameConfig? _config;

        private GameState _state = GameState.Setup;
        private List<PlayerModel> _players = new();
        private DeckManager? _deckManager;
        private TurnManager? _turnManager;
        private ActionCardHandler? _actionHandler;
        private GameCommandExecutor? _executor;
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

        /// <summary>L'exécuteur de commandes — point d'entrée pour toute action de jeu.</summary>
        public GameCommandExecutor? Executor => _executor;

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

            int totalPlayers = humanPlayers + aiPlayers;
            for (int i = 0; i < humanPlayers; i++)
            {
                _players.Add(new PlayerModel(i, $"Player {i + 1}", false));
            }
            for (int i = 0; i < aiPlayers; i++)
            {
                _players.Add(new PlayerModel(humanPlayers + i, $"Bot {i + 1}", true));
            }

            _deckManager = new DeckManager(_config);
            _actionHandler = new ActionCardHandler(_deckManager, _players);
            _turnManager = new TurnManager(_players, _actionHandler);
            _executor = new GameCommandExecutor(
                _players, _deckManager, _turnManager, _actionHandler, _config);

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

            foreach (PlayerModel player in _players)
            {
                player.ResetForNewRound();
            }

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
                winner.CurrentLevel += 2;
            }
            else
            {
                winner.CurrentLevel += 1;
            }
            EventBus.Publish(new LevelCompletedEvent
            {
                PlayerIndex = winnerIndex,
                Level = winner.CurrentLevel - 1
            });

            // Les autres joueurs ayant posé leur niveau avancent d'un niveau
            foreach (PlayerModel player in _players)
            {
                if (player.Index == winnerIndex) continue;

                if (player.HasLaidDownThisRound)
                {
                    player.CurrentLevel += 1;
                    EventBus.Publish(new LevelCompletedEvent
                    {
                        PlayerIndex = player.Index,
                        Level = player.CurrentLevel - 1
                    });
                }
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
        /// Point d'entrée UNIQUE pour toute action de jeu.
        /// Valide, exécute, publie les événements, et gère la fin de round automatiquement.
        /// IA et humain passent par ce chemin — aucune exception.
        /// </summary>
        public CommandResult ExecuteCommand(IGameCommand command)
        {
            if (_executor == null)
                return CommandResult.Failure("Game not initialized");

            CommandResult result = _executor.Execute(command);

            if (result.RoundEnded)
            {
                OnRoundEnd(_turnManager!.CurrentPlayerIndex);
            }

            return result;
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
