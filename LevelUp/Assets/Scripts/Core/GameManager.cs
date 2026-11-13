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
        private MatchRules _rules = MatchRules.Classic;

        /// <summary>
        /// Version des règles : à incrémenter dès qu'une règle ou le format d'une
        /// commande change. Deux appareils (en ligne) ou une sauvegarde d'une autre
        /// version ne produiraient pas la même partie en rejouant les commandes.
        /// v6 : cartes « Passe » / « +2 » non ramassables et exclues des combinaisons,
        /// départage de la pioche épuisée.
        /// v5 : fin de manche explicite (StartNextRoundCommand), sortie par pose,
        /// phases Piocher/Défausser non passables, points de pénalité.
        /// </summary>
        public const int RulesVersion = 6;

        /// <summary>
        /// Vrai (défaut, tests et simulations) : la manche suivante démarre dans la
        /// foulée de la fin de manche. Faux (le jeu réel) : la partie attend une
        /// <see cref="StartNextRoundCommand"/> en <see cref="GameState.EndRound"/>,
        /// le temps d'afficher le récap. Doit être identique sur tous les appareils
        /// d'une partie en ligne (réglé une fois par le bootstrapper).
        /// </summary>
        public bool AutoStartNextRound { get; set; } = true;

        /// <summary>État actuel de la partie.</summary>
        public GameState State => _state;

        /// <summary>Règles de la partie en cours (immuables jusqu'à la prochaine partie).</summary>
        public MatchRules Rules => _rules;

        /// <summary>Gagnant de la dernière manche terminée (-1 avant la première).</summary>
        public int LastRoundWinner { get; private set; } = -1;

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
                setups.Add(new PlayerSetup($"Joueur {i + 1}", false));
            }
            for (int i = 0; i < aiPlayers; i++)
            {
                setups.Add(new PlayerSetup($"Bot {i + 1}", true));
            }

            StartGame(setups, System.Environment.TickCount, new MatchRules(randomContracts));
        }

        /// <summary>
        /// Compat : partie aux règles classiques, contrats fixes ou variés.
        /// </summary>
        public void StartGame(IReadOnlyList<PlayerSetup> setups, int seed, bool randomContracts = false)
        {
            StartGame(setups, seed, new MatchRules(randomContracts));
        }

        /// <summary>
        /// Initialise et démarre une partie avec une liste de sièges explicite, un
        /// seed de mélange et des règles. Même (setups, seed, règles) ⇒ même partie
        /// sur tous les appareils : c'est le point d'entrée du multijoueur en ligne.
        /// </summary>
        public void StartGame(IReadOnlyList<PlayerSetup> setups, int seed, MatchRules rules)
        {
            if (_config == null)
            {
                Debug.LogError("GameConfig is not assigned!");
                return;
            }

            _state = GameState.Setup;
            _players.Clear();
            _roundNumber = 0;
            LastRoundWinner = -1;
            _rules = rules ?? MatchRules.Classic;
            GameSession.Rules = _rules;
            GameSession.Contracts = _rules.RandomContracts
                ? ContractPlan.CreateRandom(seed)
                : ContractPlan.Classic;

            int totalPlayers = setups.Count;
            for (int i = 0; i < totalPlayers; i++)
            {
                _players.Add(new PlayerModel(i, setups[i].Name, setups[i].IsAI));
            }

            _deckManager = new DeckManager(_config, seed, _rules);
            _actionHandler = new ActionCardHandler(_deckManager, _players);
            _turnManager = new TurnManager(_players, _actionHandler);
            _executor = new GameCommandExecutor(
                _players, _deckManager, _turnManager, _actionHandler, _config, _rules);

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

            // Variante « coup de pouce » : une carte de plus pour qui piétine.
            if (_rules.CatchUp)
            {
                foreach (PlayerModel player in _players)
                {
                    if (player.RoundsAtLevel < MatchRules.CatchUpAfterRounds) continue;
                    CardModel? extra = _deckManager.DrawFromPile();
                    if (extra.HasValue) player.AddToHand(extra.Value);
                }
            }

            EventBus.Publish(new RoundStartedEvent { RoundNumber = _roundNumber });

            _turnManager!.StartRound(_roundStarterIndex);
        }

        /// <summary>
        /// Appelé quand un joueur termine son tour en vidant sa main.
        /// Gère la fin du round et la progression des niveaux.
        /// </summary>
        internal void OnRoundEnd(int winnerIndex, bool deckExhausted = false, bool turnLimitReached = false)
        {
            _state = GameState.EndRound;
            LastRoundWinner = winnerIndex;

            PlayerModel winner = _players[winnerIndex];

            // Instantané avant la montée des niveaux (récap) + points de pénalité :
            // les cartes restées en main comptent contre leur joueur.
            int count = _players.Count;
            int[] levelsBefore = new int[count];
            bool[] laidDown = new bool[count];
            int[] cardsLeft = new int[count];
            int[] roundPenalty = new int[count];
            for (int i = 0; i < count; i++)
            {
                PlayerModel p = _players[i];
                levelsBefore[i] = p.CurrentLevel;
                laidDown[i] = p.HasLaidDownThisRound;
                cardsLeft[i] = p.Hand.Count;
                roundPenalty[i] = p.HandPenalty();
                p.PenaltyPoints += roundPenalty[i];
            }

            EventBus.Publish(new RoundEndedEvent { WinnerIndex = winnerIndex });

            // Le gagnant du round saute un niveau (bonus de sortie, réglable).
            // Manche arrêtée par la limite de tours : personne n'est sorti, donc
            // pas de bonus — le « gagnant » ne monte que s'il avait posé.
            int winnerBefore = winner.CurrentLevel;
            int gain = turnLimitReached
                ? (winner.HasLaidDownThisRound ? 1 : 0)
                : (winner.HasLaidDownThisRound ? _rules.GoOutBonus : 1);
            winner.CurrentLevel += gain;
            // Variante : le bonus ne fait pas sauter le dernier niveau.
            if (_rules.FinalLevelRequired && winnerBefore < _rules.LevelsToWin
                && winner.CurrentLevel > _rules.LevelsToWin)
            {
                winner.CurrentLevel = _rules.LevelsToWin;
            }
            if (winner.CurrentLevel != winnerBefore)
            {
                EventBus.Publish(new LevelCompletedEvent
                {
                    PlayerIndex = winnerIndex,
                    Level = winner.CurrentLevel - 1
                });
            }

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

            // Fin de partie : quelqu'un a dépassé le niveau final de la partie.
            // Plusieurs dans le même cas : le plus avancé gagne, et à égalité
            // le gagnant de la manche (c'est lui qui l'a terminée).
            int champion = -1;
            foreach (PlayerModel player in _players)
            {
                if (player.CurrentLevel <= _rules.LevelsToWin) continue;
                if (champion < 0)
                {
                    champion = player.Index;
                    continue;
                }
                PlayerModel best = _players[champion];
                if (player.CurrentLevel > best.CurrentLevel
                    || (player.CurrentLevel == best.CurrentLevel && player.Index == winnerIndex))
                {
                    champion = player.Index;
                }
            }

            int[] levelsAfter = new int[count];
            for (int i = 0; i < count; i++)
            {
                levelsAfter[i] = _players[i].CurrentLevel;
                // Suivi pour la variante « coup de pouce ».
                _players[i].RoundsAtLevel = levelsAfter[i] == levelsBefore[i] ? _players[i].RoundsAtLevel + 1 : 0;
            }

            bool gameOver = champion >= 0;
            EventBus.Publish(new RoundSummaryEvent
            {
                RoundNumber = _roundNumber,
                WinnerIndex = winnerIndex,
                DeckExhausted = deckExhausted,
                TurnLimitReached = turnLimitReached,
                AwaitingNextRound = !gameOver && !AutoStartNextRound,
                GameOver = gameOver,
                LevelsBefore = levelsBefore,
                LevelsAfter = levelsAfter,
                LaidDown = laidDown,
                CardsLeft = cardsLeft,
                RoundPenalty = roundPenalty
            });

            if (gameOver)
            {
                _state = GameState.GameOver;
                EventBus.Publish(new GameOverEvent { WinnerIndex = champion });
                return;
            }

            // Prochain round : tout de suite (tests, simulations) ou sur
            // StartNextRoundCommand, une fois le récap lu (jeu réel).
            _roundStarterIndex = (winnerIndex + 1) % _players.Count;
            if (AutoStartNextRound) StartNewRound();
        }

        /// <summary>
        /// Abandonne la partie en cours (retour au menu) : plus aucune commande
        /// n'est acceptée et rien ne tourne en arrière-plan (bots, minuteur,
        /// input) jusqu'au prochain <see cref="StartGame(IReadOnlyList{PlayerSetup}, int, MatchRules)"/>.
        /// </summary>
        public void AbandonGame()
        {
            _state = GameState.Setup;
        }

        /// <summary>
        /// Point d'entrée UNIQUE pour toute action de jeu.
        /// Valide, exécute, publie les événements, et gère la fin de round automatiquement.
        /// IA et humain passent par ce chemin — aucune exception.
        /// </summary>
        public CommandResult ExecuteCommand(IGameCommand command)
        {
            // Les événements partent PENDANT l'exécution : un abonné qui
            // rejouerait une commande depuis son handler serait notifié avant la
            // commande en cours (sauvegarde et relais réseau dans le désordre).
            // Une commande se joue donc toujours hors d'un handler d'événement.
            if (_executing)
            {
                Debug.LogError($"[GameManager] Commande ré-entrante refusée : {command?.GetType().Name}");
                return CommandResult.Failure("Commande déjà en cours");
            }

            _executing = true;
            try
            {
                return ExecuteCommandCore(command);
            }
            finally
            {
                _executing = false;
            }
        }

        private bool _executing;

        private CommandResult ExecuteCommandCore(IGameCommand command)
        {
            if (_executor == null)
                return CommandResult.Failure("Game not initialized");

            if (command is StartNextRoundCommand next)
                return ExecuteStartNextRound(next);

            // Hors manche (récap affiché, partie finie ou abandonnée) : rien ne
            // se joue — une commande réseau tardive ne peut pas rejouer une fin
            // de manche ni agir sur une partie terminée.
            if (_state != GameState.PlayerTurn)
                return CommandResult.Failure(CommandError.NoRoundInProgress, "Aucune manche en cours");

            CommandResult result = _executor.Execute(command);

            if (result.RoundEnded)
            {
                // Le gagnant par défaut est le joueur courant (cas standard : main vide
                // après défausse). Si l'exécuteur fournit un override (deck épuisé),
                // c'est lui qui prime.
                int winner = result.WinnerOverride ?? _turnManager!.CurrentPlayerIndex;
                bool limit = result.WinnerOverride.HasValue && _turnManager!.TurnLimitReached(_rules, _players.Count);
                OnRoundEnd(winner, result.WinnerOverride.HasValue && !limit, limit);
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
        /// Démarre la manche suivante après un récap. Refusée hors récap ou si
        /// elle vise une manche déjà dépassée (commande en double).
        /// </summary>
        private CommandResult ExecuteStartNextRound(StartNextRoundCommand command)
        {
            if (_state != GameState.EndRound)
                return CommandResult.Failure("Aucun récap en cours");
            if (command.FinishedRound != _roundNumber)
                return CommandResult.Failure("Manche déjà lancée");
            if (command.PlayerIndex < 0 || command.PlayerIndex >= _players.Count)
                return CommandResult.Failure("Joueur invalide");

            StartNewRound();

            CommandResult result = CommandResult.Ok();
            if (!_rebuilding) CommandExecuted?.Invoke(command, result);
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
            return RebuildFromHistory(setups, seed, new MatchRules(randomContracts), history);
        }

        /// <inheritdoc cref="RebuildFromHistory(IReadOnlyList{PlayerSetup}, int, bool, IReadOnlyList{IGameCommand})"/>
        public bool RebuildFromHistory(IReadOnlyList<PlayerSetup> setups, int seed,
            MatchRules rules, IReadOnlyList<IGameCommand> history)
        {
            bool wasMuted = EventBus.Muted;
            EventBus.Muted = true;
            _rebuilding = true;
            try
            {
                StartGame(setups, seed, rules);
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
                    h = h * 31 + _turnManager.TurnsThisRound;
                }

                foreach (PlayerModel p in _players)
                {
                    h = h * 31 + p.CurrentLevel;
                    h = h * 31 + (p.HasLaidDownThisRound ? 1 : 0);
                    h = h * 31 + p.SkipCount;
                    h = h * 31 + p.PenaltyPoints;
                    h = h * 31 + p.RoundsAtLevel;

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
