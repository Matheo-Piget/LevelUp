using System.Collections.Generic;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Fin de tour automatique (minuteur écoulé) : renvoie, phase par phase, la
    /// commande qui termine le tour le plus sobrement possible — pioche au deck,
    /// passe la pose et les ajouts, défausse la carte la moins utile. Ne joue
    /// jamais "à la place" du joueur au-delà de ça (pas de pose automatique).
    /// Lecture seule : les commandes passent ensuite par GameManager.ExecuteCommand.
    /// </summary>
    public static class TurnAutoPlay
    {
        /// <summary>Prochaine commande pour terminer le tour du joueur courant (null si rien à faire).</summary>
        public static IGameCommand? NextCommand(GameManager gameManager)
        {
            TurnManager? tm = gameManager.TurnManager;
            if (tm == null || gameManager.State != GameState.PlayerTurn) return null;

            PlayerModel player = tm.CurrentPlayer;
            return tm.CurrentPhase switch
            {
                TurnPhase.Draw => new DrawFromDeckCommand(player.Index),
                TurnPhase.LayDown => new SkipPhaseCommand(player.Index, TurnPhase.LayDown),
                TurnPhase.AddToMelds => new SkipPhaseCommand(player.Index, TurnPhase.AddToMelds),
                TurnPhase.Discard => player.Hand.Count == 0
                    ? null
                    : BuildDiscard(player, gameManager.Players),
                _ => null
            };
        }

        private static DiscardCommand BuildDiscard(PlayerModel player, IReadOnlyList<PlayerModel> players)
        {
            CardModel card = ChooseDiscard(player.Hand);
            int target = card.IsAction && !card.IsWild ? DefaultTarget(player.Index, players) : -1;
            return new DiscardCommand(player.Index, card, target);
        }

        /// <summary>
        /// Carte la moins utile de la main : peu de cartes de même valeur, de
        /// valeurs voisines ou de même couleur ; à égalité la plus haute. Les
        /// jokers ne sont défaussés qu'en dernier recours, les cartes « Passe »
        /// et « +2 » passent avant les cartes numérotées.
        /// </summary>
        public static CardModel ChooseDiscard(IReadOnlyList<CardModel> hand)
        {
            CardModel best = hand[0];
            float bestScore = float.MaxValue;

            foreach (CardModel card in hand)
            {
                float score;
                if (card.IsWild) score = 1000f;
                else if (card.IsAction) score = -1f;
                else score = Usefulness(card, hand);

                if (score < bestScore || (score == bestScore && card.Value > best.Value))
                {
                    bestScore = score;
                    best = card;
                }
            }
            return best;
        }

        private static float Usefulness(CardModel card, IReadOnlyList<CardModel> hand)
        {
            int sameValue = 0, neighbors = 0, sameColor = 0;
            foreach (CardModel other in hand)
            {
                if (other.Id == card.Id || other.Type != CardType.Normal) continue;
                if (other.Value == card.Value) sameValue++;
                int d = other.Value - card.Value;
                if (d == 1 || d == -1 || d == 2 || d == -2) neighbors++;
                if (other.Color == card.Color) sameColor++;
            }
            return sameValue * 3f + neighbors * 1.5f + sameColor * 0.5f;
        }

        /// <summary>Cible par défaut d'une carte « Passe » / « +2 » : le joueur suivant.</summary>
        public static int DefaultTarget(int playerIndex, IReadOnlyList<PlayerModel> players)
        {
            if (players.Count < 2) return -1;
            return (playerIndex + 1) % players.Count;
        }
    }
}
