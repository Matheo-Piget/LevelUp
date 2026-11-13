using LevelUp.Core;

namespace LevelUp.UI
{
    /// <summary>
    /// Messages affichés au joueur quand le Core refuse une action. Le Core ne
    /// porte qu'un motif (<see cref="CommandError"/>) : le ton (tutoiement) et
    /// la langue restent l'affaire de l'interface.
    /// </summary>
    public static class CommandErrorText
    {
        public static string For(CommandResult result) => For(result.Error);

        public static string For(CommandError error) => error switch
        {
            CommandError.NotYourTurn => "Ce n'est pas ton tour",
            CommandError.WrongPhase => "Pas à cette étape du tour",
            CommandError.EmptyDiscard => "Cette défausse est vide : touche la pioche",
            CommandError.ActionCardNotPickable => "Une carte « Passe » ou « +2 » ne se ramasse pas",
            CommandError.CardNotInHand => "Cette carte n'est plus dans ta main",
            CommandError.InvalidMeld => "Ces cartes ne forment pas une combinaison",
            CommandError.ContractMismatch => "Ces cartes ne forment pas le contrat",
            CommandError.AlreadyLaidDown => "Tu as déjà posé ton contrat cette manche",
            CommandError.MustLayDownFirst => "Pose d'abord ton contrat",
            CommandError.CardDoesNotFit => "Cette carte ne va pas sur cette combinaison",
            CommandError.NoRoundInProgress => "La manche est terminée",
            // Le scénario (tutoriel) affiche sa propre consigne : pas de message en plus.
            CommandError.Filtered => "",
            _ => "Action impossible"
        };
    }
}
