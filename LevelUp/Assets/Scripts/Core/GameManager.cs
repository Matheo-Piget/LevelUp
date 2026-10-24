using System.Collections.Generic;
using UnityEngine;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Description d'un siège au démarrage d'une partie : nom + humain/IA.
    /// Utilisé par le multijoueur en ligne pour que tous les appareils
    /// construisent exactement la même liste de joueurs.
    /// </summary>
    public readonly struct PlayerSetup
    {
        public readonly string Name;
        public readonly bool IsAI;

        public PlayerSetup(string name, bool isAI)
        {
            Name = name;
            IsAI = isAI;
        }
    }

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
        private bool _rebuilding;

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
        /// Publié après chaque commande exécutée AVEC SUCCÈS (fin de round incluse).
        /// C'est le point d'accroche du multijoueur : la couche réseau relaie la
        /// commande validée aux autres appareils, qui la rejouent à l'identique.
        /// </summary>
        public event System.Action<IGameCommand, CommandResult>? CommandExecuted;

        /// <summary>
        /// Initialise et démarre une nouvelle partie (solo : humains puis bots).
        /// </summary>
        public void StartGame(int humanPlayers, int aiPlayers, bool randomContracts = false)
        {
            List<PlayerSetup> setups = new();
            for (int i = 0; i < humanPlayers; i++)
            {
                setups.Add(new PlayerSetup($"Player {i + 1}", false));
            }
            for (int i = 0; i < aiPlayers; i++)
            {
                setups.Add(new PlayerSetup($"Bot {i + 1}", true));
            }

            StartGame(setups, System.Environment.TickCount, randomContracts);
        }

        /// <summary>
        /// Initialise et démarre une partie avec une liste de sièges explicite et
        /// un seed de mélange. Même (setups, seed, mode de contrats) ⇒ même partie
        /// sur tous les appareils : c'est le point d'entrée du multijoueur en ligne.
        /// </summary>
        public void StartGame(IReadOnlyList<PlayerSetup> setups, int seed, bool randomContracts = false)
        {
            if (_config == null)
            {
                Debug.LogError("GameConfig is not assigned!");
                return;
            }

            _state = GameState.Setup;
            _players.Clear();
            _roundNumber = 0;
            GameSession.Contracts = randomContracts
                ? ContractPlan.CreateRandom(seed)
                : ContractPlan.Classic;

            int totalPlayers = setups.Count;
            for (int i = 0; i < totalPlayers; i++)
            {
                _players.Add(new PlayerModel(i, setups[i].Name, setups[i].IsAI));
            }

            _deckManager = new DeckManager(_config, seed);
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
                // Le gagnant par défaut est le joueur courant (cas standard : main vide
                // après défausse). Si l'exécuteur fournit un override (deck épuisé),
                // c'est lui qui prime.
                int winner = result.WinnerOverride ?? _turnManager!.CurrentPlayerIndex;
                OnRoundEnd(winner);
            }

            // Notifier APRÈS le traitement complet (fin de round comprise) : le
            // relais réseau n'envoie que des commandes intégralement appliquées.
            if (result.Success && !_rebuilding)
            {
                CommandExecuted?.Invoke(command, result);
            }

            return result;
        }

        /// <summary>
        /// Reconstruit une partie en rejouant son historique complet (même sièges,
        /// même seed, mêmes commandes ⇒ même état, cf. simulation miroir).
        /// Silencieux : aucun événement publié, aucune commande relayée — l'appelant
        /// resynchronise ensuite les vues. Retourne false si une commande échoue.
        /// </summary>
        public bool RebuildFromHistory(IReadOnlyList<PlayerSetup> setups, int seed,
            bool randomContracts, IReadOnlyList<IGameCommand> history)
        {
            bool wasMuted = EventBus.Muted;
            EventBus.Muted = true;
            _rebuilding = true;
            try
            {
                StartGame(setups, seed, randomContracts);
                foreach (IGameCommand command in history)
                {
                    if (!ExecuteCommand(command).Success) return false;
                }
                return true;
            }
            finally
            {
                _rebuilding = false;
                EventBus.Muted = wasMuted;
            }
        }

        /// <summary>
        /// Convertit un joueur humain en bot (déconnexion en ligne). L'appelant
        /// est responsable d'attacher un <c>AIPlayer</c> côté hôte.
        /// </summary>
        public void ConvertPlayerToAI(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _players.Count) return;
            _players[playerIndex].IsAI = true;
        }

        /// <summary>
        /// Rend un siège à son joueur humain (reconnexion en ligne après qu'un
        /// bot a pris le relais). L'appelant retire l'<c>AIPlayer</c> côté hôte.
        /// </summary>
        public void ConvertAIToPlayer(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _players.Count) return;
            _players[playerIndex].IsAI = false;
        }

        /// <summary>
        /// Empreinte de l'état de jeu partagé, pour détecter une désynchronisation
        /// entre appareils. Indépendante de l'ordre des cartes en main (le tri de
        /// la main est local à chaque appareil) ; inclut tout ce qui influe sur
        /// les règles : tour, phase, niveaux, mains, combinaisons, défausses, pioche.
        /// </summary>
        public int ComputeStateHash()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + _roundNumber;
                h = h * 31 + (int)_state;
                if (_turnManager != null)
                {
                    h = h * 31 + _turnManager.CurrentPlayerIndex;
                    h = h * 31 + (int)_turnManager.CurrentPhase;
                }

                foreach (PlayerModel p in _players)
                {
                    h = h * 31 + p.CurrentLevel;
                    h = h * 31 + (p.HasLaidDownThisRound ? 1 : 0);
                    h = h * 31 + p.SkipCount;

                    // Somme commutative : l'ordre local de la main n'influe pas.
                    int handSum = 0;
                    foreach (CardModel c in p.Hand) handSum += (c.Id + 1) * -1640531535; // Knuth
                    h = h * 31 + handSum;
                    h = h * 31 + p.Hand.Count;

                    foreach (Meld m in p.LaidMelds)
                    {
                        h = h * 31 + (int)m.Type;
                        foreach (CardModel c in m.Cards) h = h * 31 + c.Id;
                    }
                }

                if (_deckManager != null)
                {
                    h = h * 31 + _deckManager.DrawPileCount;
                    for (int i = 0; i < _deckManager.DiscardPileCount; i++)
                    {
                        h = h * 31 + _deckManager.GetDiscardCount(i);
                        CardModel? top = _deckManager.PeekDiscard(i);
                        h = h * 31 + (top.HasValue ? top.Value.Id : -1);
                    }
                }
                return h;
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
            // Pas de EventBus.Clear() ici : les subscribers UI gèrent leur cycle via
            // OnEnable/OnDisable. Vider le bus à la destruction de GameManager
            // désynchroniserait les vues toujours vivantes (ex: au restart sans
            // changement de scène). Clear ne doit être invoqué qu'à la transition
            // de scène effective.
        }
    }
}
