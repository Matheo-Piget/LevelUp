using System.Collections.Generic;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Gère l'état du tour : joueur actif, phase courante, transitions.
    ///
    /// Responsabilité unique : navigation dans le flow Draw → LayDown → AddToMelds → Discard.
    /// Les actions de jeu (pioche, défausse, etc.) sont dans <see cref="GameCommandExecutor"/>.
    /// Les méthodes de transition sont <c>internal</c> — seul l'exécuteur les appelle.
    /// </summary>
    public class TurnManager
    {
        private readonly List<PlayerModel> _players;
        private readonly ActionCardHandler _actionHandler;

        private int _currentPlayerIndex;
        private TurnPhase _currentPhase;

        /// <summary>Index du joueur actif.</summary>
        public int CurrentPlayerIndex => _currentPlayerIndex;

        /// <summary>Phase actuelle du tour.</summary>
        public TurnPhase CurrentPhase => _currentPhase;

        /// <summary>Joueur actif.</summary>
        public PlayerModel CurrentPlayer => _players[_currentPlayerIndex];

        public TurnManager(List<PlayerModel> players, ActionCardHandler actionHandler)
        {
            _players = players;
            _actionHandler = actionHandler;
        }

        /// <summary>
        /// Démarre le premier tour du round.
        /// </summary>
        public void StartRound(int startingPlayerIndex)
        {
            _currentPlayerIndex = startingPlayerIndex;
            _currentPhase = TurnPhase.Draw;

            EventBus.Publish(new TurnStartedEvent
            {
                PlayerIndex = _currentPlayerIndex,
                Phase = _currentPhase,
                PlayerLevel = CurrentPlayer.CurrentLevel,
                HasLaidDown = CurrentPlayer.HasLaidDownThisRound
            });
        }

        // ────────────────────────────────────────────────────
        //  TRANSITIONS (internal — appelées par le CommandExecutor)
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Avance directement à une phase spécifique.
        /// </summary>
        internal void AdvanceToPhase(TurnPhase phase)
        {
            _currentPhase = phase;
            EventBus.Publish(new TurnPhaseChangedEvent
            {
                PlayerIndex = _currentPlayerIndex,
                NewPhase = _currentPhase
            });
        }

        /// <summary>
        /// Avance à la phase suivante selon les règles :
        /// Draw → LayDown → AddToMelds (si posé) ou Discard → fin de tour.
        /// </summary>
        internal void AdvancePhase()
        {
            TurnPhase next = _currentPhase switch
            {
                TurnPhase.Draw => TurnPhase.LayDown,
                TurnPhase.LayDown => CurrentPlayer.HasLaidDownThisRound
                    ? TurnPhase.AddToMelds
                    : TurnPhase.Discard,
                TurnPhase.AddToMelds => TurnPhase.Discard,
                _ => TurnPhase.Discard
            };

            AdvanceToPhase(next);
        }

        /// <summary>
        /// Passe au tour du joueur suivant (saute les joueurs marqués Skip).
        /// </summary>
        internal void NextTurn()
        {
            _currentPlayerIndex = _actionHandler.GetNextPlayer(_currentPlayerIndex);
            _currentPhase = TurnPhase.Draw;

            EventBus.Publish(new TurnStartedEvent
            {
                PlayerIndex = _currentPlayerIndex,
                Phase = _currentPhase,
                PlayerLevel = CurrentPlayer.CurrentLevel,
                HasLaidDown = CurrentPlayer.HasLaidDownThisRound
            });
        }
    }
}
