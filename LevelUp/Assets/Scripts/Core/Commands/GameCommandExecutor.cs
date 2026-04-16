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

        public GameCommandExecutor(
            List<PlayerModel> players,
            DeckManager deckManager,
            TurnManager turnManager,
            ActionCardHandler actionHandler,
            GameConfig config)
        {
            _players = players;
            _deckManager = deckManager;
            _turnManager = turnManager;
            _actionHandler = actionHandler;
            _config = config;
        }

        /// <summary>
        /// Exécute une commande de jeu après validation complète.
        /// </summary>
        public CommandResult Execute(IGameCommand command)
        {
            if (command.PlayerIndex != _turnManager.CurrentPlayerIndex)
                return CommandResult.Failure("Ce n'est pas votre tour");

            return command switch
            {
                DrawFromDeckCommand => ExecuteDrawFromDeck(),
                DrawFromDiscardCommand cmd => ExecuteDrawFromDiscard(cmd.DiscardPileIndex),
                LayDownLevelCommand cmd => ExecuteLayDown(cmd.Melds),
                AddToMeldCommand cmd => ExecuteAddToMeld(cmd.Card, cmd.MeldOwnerIndex, cmd.MeldIndex),
                SkipPhaseCommand cmd => ExecuteSkipPhase(cmd.PhaseToSkip),
                DiscardCommand cmd => ExecuteDiscard(cmd.Card, cmd.TargetPlayerIndex),
                _ => CommandResult.Failure("Commande inconnue")
            };
        }

        // ────────────────────────────────────────────────────
        //  DRAW
        // ────────────────────────────────────────────────────

        private CommandResult ExecuteDrawFromDeck()
        {
            if (_turnManager.CurrentPhase != TurnPhase.Draw)
                return CommandResult.Failure("Phase incorrecte pour piocher");

            CardModel? card = _deckManager.DrawFromPile();
            if (!card.HasValue)
                return CommandResult.Failure("La pioche est vide");

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
            _turnManager.AdvanceToPhase(TurnPhase.LayDown);

            return CommandResult.OkWithCard(card.Value);
        }

        private CommandResult ExecuteDrawFromDiscard(int discardPileIndex)
        {
            if (_turnManager.CurrentPhase != TurnPhase.Draw)
                return CommandResult.Failure("Phase incorrecte pour piocher");

            CardModel? card = _deckManager.DrawFromDiscard(discardPileIndex);
            if (!card.HasValue)
                return CommandResult.Failure("Défausse vide");

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
            _turnManager.AdvanceToPhase(TurnPhase.LayDown);

            return CommandResult.OkWithCard(card.Value);
        }

        // ────────────────────────────────────────────────────
        //  LAY DOWN
        // ────────────────────────────────────────────────────

        private CommandResult ExecuteLayDown(List<Meld> melds)
        {
            if (_turnManager.CurrentPhase != TurnPhase.LayDown)
                return CommandResult.Failure("Phase incorrecte pour poser");

            PlayerModel player = _turnManager.CurrentPlayer;
            if (player.HasLaidDownThisRound)
                return CommandResult.Failure("Niveau déjà posé ce round");

            if (!LevelValidator.IsLevelComplete(player.Hand, player.CurrentLevel,
                    _config, out List<Meld> _))
                return CommandResult.Failure("Combinaison invalide pour ce niveau");

            // Retirer les cartes des melds de la main
            List<CardModel> allCards = new();
            foreach (Meld meld in melds)
                allCards.AddRange(meld.Cards);

            player.RemoveFromHand(allCards);

            foreach (Meld meld in melds)
            {
                Meld playerMeld = new(meld.Type, meld.Cards, player.Index);
                player.AddMeld(playerMeld);
            }

            player.HasLaidDownThisRound = true;

            List<List<CardModel>> meldCards = new();
            foreach (Meld m in melds)
                meldCards.Add(m.Cards);

            EventBus.Publish(new LevelLaidDownEvent
            {
                PlayerIndex = _turnManager.CurrentPlayerIndex,
                Level = player.CurrentLevel,
                Melds = meldCards
            });

            PublishHandChanged(player);
            _turnManager.AdvanceToPhase(TurnPhase.AddToMelds);

            return CommandResult.Ok($"Niveau {player.CurrentLevel} posé !");
        }

        // ────────────────────────────────────────────────────
        //  ADD TO MELD
        // ────────────────────────────────────────────────────

        private CommandResult ExecuteAddToMeld(CardModel card, int meldOwnerIndex, int meldIndex)
        {
            if (_turnManager.CurrentPhase != TurnPhase.AddToMelds)
                return CommandResult.Failure("Phase incorrecte");

            PlayerModel player = _turnManager.CurrentPlayer;
            if (!player.HasLaidDownThisRound)
                return CommandResult.Failure("Vous devez d'abord poser votre niveau");

            if (meldOwnerIndex < 0 || meldOwnerIndex >= _players.Count)
                return CommandResult.Failure("Joueur cible invalide");

            PlayerModel meldOwner = _players[meldOwnerIndex];
            if (meldIndex < 0 || meldIndex >= meldOwner.LaidMelds.Count)
                return CommandResult.Failure("Combinaison invalide");

            if (!player.HandContains(card))
                return CommandResult.Failure("Carte introuvable dans la main");

            Meld meld = meldOwner.GetMeld(meldIndex);
            if (!meld.TryAddCard(card))
                return CommandResult.Failure("Cette carte ne peut pas être ajoutée ici");

            player.RemoveFromHand(card);

            EventBus.Publish(new CardAddedToMeldEvent
            {
                PlayerIndex = _turnManager.CurrentPlayerIndex,
                MeldOwnerIndex = meldOwnerIndex,
                MeldIndex = meldIndex,
                Card = card
            });

            PublishHandChanged(player);
            return CommandResult.Ok();
        }

        // ────────────────────────────────────────────────────
        //  SKIP PHASE
        // ────────────────────────────────────────────────────

        private CommandResult ExecuteSkipPhase(TurnPhase phaseToSkip)
        {
            if (_turnManager.CurrentPhase != phaseToSkip)
                return CommandResult.Failure("Phase incorrecte");

            _turnManager.AdvancePhase();
            return CommandResult.Ok();
        }

        // ────────────────────────────────────────────────────
        //  DISCARD
        // ────────────────────────────────────────────────────

        private CommandResult ExecuteDiscard(CardModel card, int targetPlayerIndex)
        {
            if (_turnManager.CurrentPhase != TurnPhase.Discard)
                return CommandResult.Failure("Phase incorrecte pour défausser");

            PlayerModel player = _turnManager.CurrentPlayer;
            if (!player.HandContains(card))
                return CommandResult.Failure("Carte introuvable dans la main");

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
