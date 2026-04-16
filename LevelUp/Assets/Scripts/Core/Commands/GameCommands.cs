using System.Collections.Generic;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Interface de base pour toutes les commandes de jeu.
    /// Chaque action (pioche, défausse, pose, etc.) est une commande validée
    /// et exécutée par le <see cref="GameCommandExecutor"/>.
    /// Pattern Command : IA et humain passent par le même chemin.
    /// </summary>
    public interface IGameCommand
    {
        int PlayerIndex { get; }
    }

    /// <summary>Pioche une carte depuis le deck.</summary>
    public readonly struct DrawFromDeckCommand : IGameCommand
    {
        public int PlayerIndex { get; }
        public DrawFromDeckCommand(int playerIndex) => PlayerIndex = playerIndex;
    }

    /// <summary>Pioche une carte depuis une pile de défausse.</summary>
    public readonly struct DrawFromDiscardCommand : IGameCommand
    {
        public int PlayerIndex { get; }
        public int DiscardPileIndex { get; }

        public DrawFromDiscardCommand(int playerIndex, int discardPileIndex)
        {
            PlayerIndex = playerIndex;
            DiscardPileIndex = discardPileIndex;
        }
    }

    /// <summary>Pose le niveau avec les combinaisons validées.</summary>
    public readonly struct LayDownLevelCommand : IGameCommand
    {
        public int PlayerIndex { get; }
        public List<Meld> Melds { get; }

        public LayDownLevelCommand(int playerIndex, List<Meld> melds)
        {
            PlayerIndex = playerIndex;
            Melds = melds;
        }
    }

    /// <summary>Ajoute une carte à une combinaison existante sur la table.</summary>
    public readonly struct AddToMeldCommand : IGameCommand
    {
        public int PlayerIndex { get; }
        public CardModel Card { get; }
        public int MeldOwnerIndex { get; }
        public int MeldIndex { get; }

        public AddToMeldCommand(int playerIndex, CardModel card, int meldOwnerIndex, int meldIndex)
        {
            PlayerIndex = playerIndex;
            Card = card;
            MeldOwnerIndex = meldOwnerIndex;
            MeldIndex = meldIndex;
        }
    }

    /// <summary>Passe la phase courante (LayDown ou AddToMelds).</summary>
    public readonly struct SkipPhaseCommand : IGameCommand
    {
        public int PlayerIndex { get; }
        public TurnPhase PhaseToSkip { get; }

        public SkipPhaseCommand(int playerIndex, TurnPhase phaseToSkip)
        {
            PlayerIndex = playerIndex;
            PhaseToSkip = phaseToSkip;
        }
    }

    /// <summary>Défausse une carte (dernière action du tour).</summary>
    public readonly struct DiscardCommand : IGameCommand
    {
        public int PlayerIndex { get; }
        public CardModel Card { get; }
        public int TargetPlayerIndex { get; }

        public DiscardCommand(int playerIndex, CardModel card, int targetPlayerIndex = -1)
        {
            PlayerIndex = playerIndex;
            Card = card;
            TargetPlayerIndex = targetPlayerIndex;
        }
    }

    /// <summary>
    /// Résultat d'exécution d'une commande.
    /// Porte le succès/échec, un message, et des données optionnelles.
    /// </summary>
    public readonly struct CommandResult
    {
        public bool Success { get; }
        public string Message { get; }
        public bool RoundEnded { get; }
        public CardModel? DrawnCard { get; }

        private CommandResult(bool success, string message, bool roundEnded, CardModel? drawnCard)
        {
            Success = success;
            Message = message;
            RoundEnded = roundEnded;
            DrawnCard = drawnCard;
        }

        public static CommandResult Ok(string message = "")
            => new(true, message, false, null);

        public static CommandResult OkWithCard(CardModel card, string message = "")
            => new(true, message, false, card);

        public static CommandResult RoundComplete(string message = "")
            => new(true, message, true, null);

        public static CommandResult Failure(string reason)
            => new(false, reason, false, null);
    }
}
