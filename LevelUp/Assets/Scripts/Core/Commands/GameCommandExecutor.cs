using System.Collections.Generic;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Point d'entrée UNIQUE pour toutes les actions de jeu.
    /// Valide l'état courant, exécute la logique métier, publie les événements.
    ///
    /// Principe fondamental : IA et humain passent par le même chemin.
    /// Plus aucune logique de jeu ne doit exister dans l'UI ou l'IA —
    /// elles créent des commandes, l'exécuteur fait le reste.
    /// </summary>
    public class GameCommandExecutor
    {
        private readonly List<PlayerModel> _players;
        private readonly DeckManager _deckManager;
        private readonly TurnManager _turnManager;
        private readonly ActionCardHandler _actionHandler;
        private readonly GameConfig _config;
        private readonly MatchRules _rules;

        public GameCommandExecutor(
            List<PlayerModel> players,
            DeckManager deckManager,
            TurnManager turnManager,
            ActionCardHandler actionHandler,
            GameConfig config,
            MatchRules? rules = null)
        {
            _players = players;
            _deckManager = deckManager;
            _turnManager = turnManager;
            _actionHandler = actionHandler;
            _config = config;
            _rules = rules ?? MatchRules.Classic;
        }

        /// <summary>
        /// Exécute une commande de jeu après validation complète.
        /// </summary>
        public CommandResult Execute(IGameCommand command)
        {
            if (command.PlayerIndex != _turnManager.CurrentPlayerIndex)
                return CommandResult.Failure(CommandError.NotYourTurn, "Ce n'est pas le tour de ce joueur");

            return command switch
            {
                DrawFromDeckCommand => ExecuteDrawFromDeck(),
                DrawFromDiscardCommand cmd => ExecuteDrawFromDiscard(cmd.DiscardPileIndex),
                LayDownLevelCommand cmd => ExecuteLayDown(cmd.Melds),
                AddToMeldCommand cmd => ExecuteAddToMeld(cmd.Card, cmd.MeldOwnerIndex, cmd.MeldIndex),
                SkipPhaseCommand cmd => ExecuteSkipPhase(cmd.PhaseToSkip),
                DiscardCommand cmd => ExecuteDiscard(cmd.Card, cmd.TargetPlayerIndex),
                // StartNextRoundCommand est traitée par GameManager (hors tour).
                _ => CommandResult.Failure("Commande inconnue")
            };
        }

        // ────────────────────────────────────────────────────
        //  DRAW
        // ────────────────────────────────────────────────────

        private CommandResult ExecuteDrawFromDeck()
        {
            if (_turnManager.CurrentPhase != TurnPhase.Draw)
                return CommandResult.Failure(CommandError.WrongPhase, "Phase incorrecte pour piocher");

            CardModel? card = _deckManager.DrawFromPile();
            if (!card.HasValue)
            {
                // Deck définitivement vide même après recyclage (toutes les défausses
                // ont au plus une carte, donc rien à recycler). Le round se termine
                // forcément : on désigne comme gagnant le joueur avec la main la
                // plus courte (variante "match au plus court").
                int forcedWinner = FindPlayerWithSmallestHand();
                return CommandResult.RoundCompleteWithWinner(
                    forcedWinner, "Pioche épuisée");
            }

            PlayerModel player = _turnManager.CurrentPlayer;
            player.AddToHand(card.Value);

            EventBus.Publish(new CardDrawnEvent
            {
                PlayerIndex = _turnManager.CurrentPlayerIndex,
                Card = card.Value,
                FromDiscard = false,
                DiscardPileIndex = -1
            });

            PublishHandChanged(player);
            _turnManager.AdvanceToPhase(NextPhaseAfterDraw(player));

            return CommandResult.OkWithCard(card.Value);
        }

        /// <summary>
        /// Retourne l'index du joueur avec le moins de cartes en main.
        /// En cas d'égalité, retourne le premier (ordre des joueurs).
        /// </summary>
        private int FindPlayerWithSmallestHand()
        {
            // Départage : moins de cartes, puis moins de points en main, puis le
            // plus proche APRÈS le joueur courant (et non le siège 0, qui
            // favorisait toujours l'humain en solo et l'hôte en ligne).
            int count = _players.Count;
            int best = -1;
            for (int step = 1; step <= count; step++)
            {
                int i = (_turnManager.CurrentPlayerIndex + step) % count;
                if (best < 0) { best = i; continue; }
                PlayerModel p = _players[i], b = _players[best];
                if (p.Hand.Count < b.Hand.Count
                    || (p.Hand.Count == b.Hand.Count && p.HandPenalty() < b.HandPenalty()))
                {
                    best = i;
                }
            }
            return best < 0 ? 0 : best;
        }

        /// <summary>Vrai si la carte du dessus d'une défausse peut être ramassée.</summary>
        public static bool CanPickFromDiscard(CardModel card) => !card.IsAction || card.IsWild;

        private CommandResult ExecuteDrawFromDiscard(int discardPileIndex)
        {
            if (_turnManager.CurrentPhase != TurnPhase.Draw)
                return CommandResult.Failure(CommandError.WrongPhase, "Phase incorrecte pour piocher");

            // Une carte « Passe » / « +2 » défaussée a été jouée : personne ne la
            // reprend (sinon elle tournerait de main en main). Un joker, oui.
            CardModel? top = _deckManager.PeekDiscard(discardPileIndex);
            if (top.HasValue && !CanPickFromDiscard(top.Value))
                return CommandResult.Failure(CommandError.ActionCardNotPickable,
                    "Une carte action ne se ramasse pas dans une défausse");

            CardModel? card = _deckManager.DrawFromDiscard(discardPileIndex);
            if (!card.HasValue)
                return CommandResult.Failure(CommandError.EmptyDiscard, "Défausse vide");

            PlayerModel player = _turnManager.CurrentPlayer;
            player.AddToHand(card.Value);

            EventBus.Publish(new CardDrawnEvent
            {
                PlayerIndex = _turnManager.CurrentPlayerIndex,
                Card = card.Value,
                FromDiscard = true,
                DiscardPileIndex = discardPileIndex
            });

            PublishHandChanged(player);
            _turnManager.AdvanceToPhase(NextPhaseAfterDraw(player));

            return CommandResult.OkWithCard(card.Value);
        }

        /// <summary>
        /// Phase à enchaîner après une pioche.
        /// Si le joueur a déjà posé son niveau ce round, LayDown est inutile
        /// (il ne peut pas reposer) → on saute directement à AddToMelds pour
        /// lui permettre de compléter les combinaisons existantes.
        /// </summary>
        private static TurnPhase NextPhaseAfterDraw(PlayerModel player)
        {
            return player.HasLaidDownThisRound
                ? TurnPhase.AddToMelds
                : TurnPhase.LayDown;
        }

        // ────────────────────────────────────────────────────
        //  LAY DOWN
        // ────────────────────────────────────────────────────

        private CommandResult ExecuteLayDown(List<Meld> melds)
        {
            if (_turnManager.CurrentPhase != TurnPhase.LayDown)
                return CommandResult.Failure(CommandError.WrongPhase, "Phase incorrecte pour poser");

            PlayerModel player = _turnManager.CurrentPlayer;
            if (player.HasLaidDownThisRound)
                return CommandResult.Failure(CommandError.AlreadyLaidDown, "Niveau déjà posé ce round");

            if (melds == null || melds.Count == 0)
                return CommandResult.Failure("Aucune combinaison fournie");

            // 1. Toutes les cartes doivent être en main, une seule fois chacune.
            //    On repart des VRAIES cartes de la main (retrouvées par identifiant) :
            //    la valeur et la couleur annoncées par la commande ne font pas foi.
            List<CardModel> allCards = new();
            List<Meld> proposed = melds;
            melds = new List<Meld>(proposed.Count);
            foreach (Meld meld in proposed)
            {
                if (meld == null || meld.Cards == null)
                    return CommandResult.Failure(CommandError.InvalidMeld, "Combinaison invalide");

                List<CardModel> realCards = new(meld.Cards.Count);
                foreach (CardModel announced in meld.Cards)
                {
                    if (!player.TryGetHandCard(announced.Id, out CardModel card))
                        return CommandResult.Failure(CommandError.CardNotInHand, "Carte introuvable dans la main");
                    if (allCards.Contains(card))
                        return CommandResult.Failure("Une carte ne peut pas être utilisée deux fois");
                    allCards.Add(card);
                    realCards.Add(card);
                }
                melds.Add(new Meld(meld.Type, realCards, player.Index));
            }

            // 2. Chaque meld doit être structurellement valide.
            foreach (Meld meld in melds)
            {
                bool structurallyValid = meld.Type switch
                {
                    MeldType.Run   => CardExtensions.IsValidRun(meld.Cards),
                    MeldType.Set   => CardExtensions.IsValidSet(meld.Cards),
                    MeldType.Flush => CardExtensions.IsValidFlush(meld.Cards),
                    _              => false
                };
                if (!structurallyValid)
                    return CommandResult.Failure(CommandError.InvalidMeld, "Combinaison invalide");
            }

            // 3. La structure des melds doit correspondre aux exigences du niveau.
            if (!LevelValidator.MeldsSatisfyLevel(melds, player.CurrentLevel, _config))
                return CommandResult.Failure(CommandError.ContractMismatch, "Combinaisons ne correspondent pas au niveau");

            player.RemoveFromHand(allCards);

            foreach (Meld meld in melds)
            {
                Meld playerMeld = new(meld.Type, meld.Cards, player.Index);
                player.AddMeld(playerMeld);
            }

            player.HasLaidDownThisRound = true;

            List<List<CardModel>> meldCards = new();
            List<MeldType> meldTypes = new();
            foreach (Meld m in melds)
            {
                meldCards.Add(new List<CardModel>(m.Cards));
                meldTypes.Add(m.Type);
            }

            EventBus.Publish(new LevelLaidDownEvent
            {
                PlayerIndex = _turnManager.CurrentPlayerIndex,
                Level = player.CurrentLevel,
                Melds = meldCards,
                MeldTypes = meldTypes
            });

            PublishHandChanged(player);
            _turnManager.AdvanceToPhase(TurnPhase.AddToMelds);

            // Contrat qui utilise toute la main (ex. main de 8 + contrat de 9
            // cartes) : le joueur sort sans défausser. Sans ça il resterait en
            // phase Compléter sans carte, et le tour ne passerait plus jamais.
            if (player.IsHandEmpty)
                return CommandResult.RoundComplete("Main vide");

            return CommandResult.Ok($"Niveau {player.CurrentLevel} posé !");
        }

        // ────────────────────────────────────────────────────
        //  ADD TO MELD
        // ────────────────────────────────────────────────────

        private CommandResult ExecuteAddToMeld(CardModel card, int meldOwnerIndex, int meldIndex)
        {
            if (_turnManager.CurrentPhase != TurnPhase.AddToMelds)
                return CommandResult.Failure(CommandError.WrongPhase, "Phase incorrecte");

            PlayerModel player = _turnManager.CurrentPlayer;
            if (!player.HasLaidDownThisRound)
                return CommandResult.Failure(CommandError.MustLayDownFirst, "Le niveau doit être posé avant de compléter");

            if (meldOwnerIndex < 0 || meldOwnerIndex >= _players.Count)
                return CommandResult.Failure("Joueur cible invalide");

            PlayerModel meldOwner = _players[meldOwnerIndex];
            if (meldIndex < 0 || meldIndex >= meldOwner.LaidMelds.Count)
                return CommandResult.Failure(CommandError.InvalidMeld, "Combinaison invalide");

            // La vraie carte de la main (l'identifiant fait foi, pas les champs reçus).
            if (!player.TryGetHandCard(card.Id, out card))
                return CommandResult.Failure(CommandError.CardNotInHand, "Carte introuvable dans la main");

            Meld meld = meldOwner.GetMeld(meldIndex);
            if (!meld.TryAddCard(card))
                return CommandResult.Failure(CommandError.CardDoesNotFit, "Cette carte ne peut pas être ajoutée ici");

            player.RemoveFromHand(card);

            EventBus.Publish(new CardAddedToMeldEvent
            {
                PlayerIndex = _turnManager.CurrentPlayerIndex,
                MeldOwnerIndex = meldOwnerIndex,
                MeldIndex = meldIndex,
                Card = card
            });

            PublishHandChanged(player);

            // Dernière carte posée sur la table : le joueur sort sans défausser
            // (sinon il resterait bloqué en phase Défausse, main vide).
            if (player.IsHandEmpty)
                return CommandResult.RoundComplete("Main vide");

            return CommandResult.Ok();
        }

        // ────────────────────────────────────────────────────
        //  SKIP PHASE
        // ────────────────────────────────────────────────────

        private CommandResult ExecuteSkipPhase(TurnPhase phaseToSkip)
        {
            // Seules « Poser » et « Compléter » se passent : la pioche et la
            // défausse sont obligatoires (sinon un client modifié pourrait
            // vider sa main sans jamais piocher).
            if (phaseToSkip != TurnPhase.LayDown && phaseToSkip != TurnPhase.AddToMelds)
                return CommandResult.Failure("Cette phase ne se passe pas");

            if (_turnManager.CurrentPhase != phaseToSkip)
                return CommandResult.Failure(CommandError.WrongPhase, "Phase incorrecte");

            _turnManager.AdvancePhase();
            return CommandResult.Ok();
        }

        // ────────────────────────────────────────────────────
        //  DISCARD
        // ────────────────────────────────────────────────────

        private CommandResult ExecuteDiscard(CardModel card, int targetPlayerIndex)
        {
            if (_turnManager.CurrentPhase != TurnPhase.Discard)
                return CommandResult.Failure(CommandError.WrongPhase, "Phase incorrecte pour défausser");

            PlayerModel player = _turnManager.CurrentPlayer;
            if (!player.TryGetHandCard(card.Id, out card))
                return CommandResult.Failure(CommandError.CardNotInHand, "Carte introuvable dans la main");

            player.RemoveFromHand(card);

            if (card.IsAction && targetPlayerIndex >= 0)
            {
                _actionHandler.HandleActionCard(
                    _turnManager.CurrentPlayerIndex, card, targetPlayerIndex);
            }

            _deckManager.Discard(_turnManager.CurrentPlayerIndex, card);

            EventBus.Publish(new CardDiscardedEvent
            {
                PlayerIndex = _turnManager.CurrentPlayerIndex,
                Card = card
            });

            PublishHandChanged(player);

            if (player.IsHandEmpty)
                return CommandResult.RoundComplete();

            // Variante « limite de tours » : la manche s'arrête, la main la plus légère l'emporte.
            _turnManager.CountTurn();
            if (_turnManager.TurnLimitReached(_rules, _players.Count))
                return CommandResult.RoundCompleteWithWinner(FindPlayerWithSmallestHand(), "Limite de tours");

            _turnManager.NextTurn();
            return CommandResult.Ok();
        }

        // ────────────────────────────────────────────────────
        //  HELPERS
        // ────────────────────────────────────────────────────

        private static void PublishHandChanged(PlayerModel player)
        {
            EventBus.Publish(new HandChangedEvent
            {
                PlayerIndex = player.Index,
                NewHand = new List<CardModel>(player.Hand)
            });
        }
    }
}
