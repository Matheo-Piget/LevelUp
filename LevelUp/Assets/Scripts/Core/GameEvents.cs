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
    }

    /// <summary>Événement déclenché quand un joueur ajoute une carte à une combinaison.</summary>
    public struct CardAddedToMeldEvent
    {
        public int PlayerIndex;
        public int MeldOwnerIndex;
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
}
