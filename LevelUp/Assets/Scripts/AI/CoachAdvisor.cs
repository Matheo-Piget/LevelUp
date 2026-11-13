using System.Collections.Generic;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.AI
{
    /// <summary>
    /// Conseil à la demande pour le joueur local : ce que jouerait un bot Expert
    /// à sa place, avec la même information que lui (sa main et ce qui est
    /// public). Aide optionnelle, coupée par défaut — ne joue jamais rien.
    /// </summary>
    public sealed class CoachAdvisor
    {
        private readonly GameManager _gm;
        private BotBrain? _brain;
        private int _seat = -1;

        public CoachAdvisor(GameManager gameManager)
        {
            _gm = gameManager;
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Subscribe<CardDiscardedEvent>(OnCardDiscarded);
            EventBus.Subscribe<CardDrawnEvent>(OnCardDrawn);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Unsubscribe<CardDiscardedEvent>(OnCardDiscarded);
            EventBus.Unsubscribe<CardDrawnEvent>(OnCardDrawn);
        }

        private void OnRoundStarted(RoundStartedEvent evt) => _brain?.OnRoundStarted();
        private void OnCardDiscarded(CardDiscardedEvent evt) => _brain?.OnCardDiscarded(evt.PlayerIndex, evt.Card);
        private void OnCardDrawn(CardDrawnEvent evt) => _brain?.OnCardDrawn(evt.PlayerIndex, evt.Card, evt.FromDiscard);

        private BotBrain BrainFor(int seat)
        {
            if (_brain == null || _seat != seat)
            {
                _seat = seat;
                _brain = new BotBrain(_gm, seat, AIDifficulty.Hard, 12345);
            }
            return _brain;
        }

        /// <summary>Conseil pour l'étape en cours du joueur courant, ou null hors de son tour.</summary>
        public string? Advise()
        {
            TurnManager? tm = _gm.TurnManager;
            if (tm == null || _gm.State != GameState.PlayerTurn) return null;
            PlayerModel me = tm.CurrentPlayer;
            if (!GameSession.IsLocalHuman(me) || me.Hand.Count == 0) return null;
            BotBrain brain = BrainFor(me.Index);

            switch (tm.CurrentPhase)
            {
                case TurnPhase.Draw:
                {
                    int pile = brain.ChooseDrawPile();
                    CardModel? top = pile >= 0 ? _gm.DeckManager?.PeekDiscard(pile) : null;
                    return top.HasValue
                        ? $"Conseil : prends le {Describe(top.Value)} de {OwnerName(pile, me.Index)}"
                        : "Conseil : pioche dans le paquet";
                }

                case TurnPhase.LayDown:
                    return brain.ChooseLayDown() != null
                        ? "Conseil : ta main remplit le contrat, pose-le"
                        : $"Conseil : rien à poser, défausse le {Describe(brain.ChooseDiscard().Card)}";

                case TurnPhase.AddToMelds:
                    if (brain.TryChooseAddToMeld(out CardModel card, out int owner, out int _))
                    {
                        return $"Conseil : ajoute le {Describe(card)} sur une combinaison de {OwnerName(owner, me.Index)}";
                    }
                    return $"Conseil : rien à compléter, défausse le {Describe(brain.ChooseDiscard().Card)}";

                case TurnPhase.Discard:
                    return $"Conseil : défausse le {Describe(brain.ChooseDiscard().Card)}";

                default:
                    return null;
            }
        }

        private string OwnerName(int seat, int me)
        {
            if (seat == me) return "ta défausse";
            IReadOnlyList<PlayerModel> players = _gm.Players;
            return seat >= 0 && seat < players.Count ? players[seat].Name : "un joueur";
        }

        /// <summary>"7 rouge", "joker", "Passe", "+2".</summary>
        public static string Describe(CardModel card)
        {
            if (card.IsWild) return "joker";
            if (card.Type == CardType.Skip) return "« Passe »";
            if (card.Type == CardType.Draw2) return "« +2 »";
            string color = card.Color switch
            {
                CardColor.Red => "rouge",
                CardColor.Blue => "bleu",
                CardColor.Green => "vert",
                CardColor.Yellow => "jaune",
                CardColor.Purple => "violet",
                CardColor.Orange => "orange",
                _ => ""
            };
            return $"{card.Value} {color}";
        }
    }
}
