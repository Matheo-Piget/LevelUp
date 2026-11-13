using System.Collections.Generic;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>Événement déclenché quand la partie démarre.</summary>
    public struct GameStartedEvent
    {
        public int PlayerCount;
    }

    /// <summary>Événement déclenché quand un nouveau round commence.</summary>
    public struct RoundStartedEvent
    {
        public int RoundNumber;
    }

    /// <summary>Événement déclenché quand c'est au tour d'un joueur.</summary>
    public struct TurnStartedEvent
    {
        public int PlayerIndex;
        public TurnPhase Phase;
        public int PlayerLevel;
        public bool HasLaidDown;
    }

    /// <summary>Événement déclenché quand la phase du tour change.</summary>
    public struct TurnPhaseChangedEvent
    {
        public int PlayerIndex;
        public TurnPhase NewPhase;
    }

    /// <summary>Événement déclenché quand un joueur pioche une carte.</summary>
    public struct CardDrawnEvent
    {
        public int PlayerIndex;
        public CardModel Card;
        public bool FromDiscard;
        /// <summary>Index de la pile de défausse piochée (-1 si pioche depuis le deck).</summary>
        public int DiscardPileIndex;
    }

    /// <summary>Événement déclenché quand un joueur défausse une carte.</summary>
    public struct CardDiscardedEvent
    {
        public int PlayerIndex;
        public CardModel Card;
    }

    /// <summary>Événement déclenché quand un joueur pose son niveau.</summary>
    public struct LevelLaidDownEvent
    {
        public int PlayerIndex;
        public int Level;
        public List<List<CardModel>> Melds;
        /// <summary>Type de chaque combinaison (même ordre que <see cref="Melds"/>) ; null = à déduire.</summary>
        public List<MeldType>? MeldTypes;
    }

    /// <summary>Événement déclenché quand un joueur ajoute une carte à une combinaison.</summary>
    public struct CardAddedToMeldEvent
    {
        public int PlayerIndex;
        public int MeldOwnerIndex;
        public int MeldIndex;
        public CardModel Card;
    }

    /// <summary>Événement déclenché quand un joueur complète un niveau.</summary>
    public struct LevelCompletedEvent
    {
        public int PlayerIndex;
        public int Level;
    }

    /// <summary>Événement déclenché quand un round se termine.</summary>
    public struct RoundEndedEvent
    {
        public int WinnerIndex;
    }

    /// <summary>
    /// Bilan d'une manche, publié après la montée des niveaux (et avant le
    /// <see cref="GameOverEvent"/> éventuel). Tableaux indexés par joueur.
    /// Quand <see cref="AwaitingNextRound"/> est vrai, la partie reste en
    /// <see cref="GameState.EndRound"/> — mains et table intactes — jusqu'à une
    /// <see cref="StartNextRoundCommand"/> : l'UI affiche le récap entre-temps.
    /// </summary>
    public struct RoundSummaryEvent
    {
        public int RoundNumber;
        public int WinnerIndex;
        public bool DeckExhausted;
        public bool AwaitingNextRound;
        public bool GameOver;
        public int[] LevelsBefore;
        public int[] LevelsAfter;
        public bool[] LaidDown;
        public int[] CardsLeft;
        public int[] RoundPenalty;
    }

    /// <summary>Événement déclenché quand la partie est terminée.</summary>
    public struct GameOverEvent
    {
        public int WinnerIndex;
    }

    /// <summary>Événement déclenché quand un joueur est sauté (Skip).</summary>
    public struct PlayerSkippedEvent
    {
        public int PlayerIndex;
    }

    /// <summary>Événement déclenché quand un joueur doit piocher des cartes supplémentaires (Draw2).</summary>
    public struct ForcedDrawEvent
    {
        public int PlayerIndex;
        public int CardCount;
    }

    /// <summary>Événement déclenché quand la main d'un joueur change.</summary>
    public struct HandChangedEvent
    {
        public int PlayerIndex;
        public List<CardModel> NewHand;
    }

    /// <summary>Événement déclenché quand le deck change.</summary>
    public struct DeckChangedEvent
    {
        public int CardsRemaining;
    }

    /// <summary>Événement déclenché pour une action carte spéciale.</summary>
    public struct ActionCardPlayedEvent
    {
        public int PlayerIndex;
        public CardModel Card;
        public int TargetPlayerIndex;
    }

    /// <summary>Événement déclenché quand l'IA commence/arrête de réfléchir.</summary>
    public struct AIThinkingEvent
    {
        public int PlayerIndex;
        public bool IsThinking;
    }

    /// <summary>
    /// Événement déclenché quand le joueur tente une action invalide
    /// (drop raté, pioche refusée, combinaison invalide). Permet aux systèmes
    /// de feedback (audio, haptique…) de réagir sans couplage avec l'input.
    /// </summary>
    public struct InvalidActionEvent
    {
    }

    /// <summary>
    /// Événement déclenché quand la pioche est définitivement épuisée (drawPile vide
    /// ET aucune défausse recyclable). Le round se termine, le joueur avec la main
    /// la plus courte est désigné gagnant. L'UI peut afficher un message dédié
    /// ("Pioche épuisée — manche au plus court"). Suivi immédiatement de
    /// <see cref="RoundEndedEvent"/>.
    /// </summary>
    public struct DeckExhaustedEvent
    {
        public int WinnerIndex;
    }
}
