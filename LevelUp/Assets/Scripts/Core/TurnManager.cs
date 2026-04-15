using System.Collections.Generic;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Gère l'ordre des tours et les actions de chaque phase.
    /// </summary>
    public class TurnManager
    {
        private readonly List<PlayerModel> _players;
        private readonly DeckManager _deckManager;
        private readonly ActionCardHandler _actionHandler;
        private readonly GameConfig _config;

        private int _currentPlayerIndex;
        private TurnPhase _currentPhase;

        /// <summary>Index du joueur actif.</summary>
        public int CurrentPlayerIndex => _currentPlayerIndex;

        /// <summary>Phase actuelle du tour.</summary>
        public TurnPhase CurrentPhase => _currentPhase;

        /// <summary>Joueur actif.</summary>
        public PlayerModel CurrentPlayer => _players[_currentPlayerIndex];

        /// <summary>
        /// Constructeur avec toutes les dépendances.
        /// </summary>
        public TurnManager(List<PlayerModel> players, DeckManager deckManager,
            ActionCardHandler actionHandler, GameConfig config)
        {
            _players = players;
            _deckManager = deckManager;
            _actionHandler = actionHandler;
            _config = config;
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

        /// <summary>
        /// Le joueur pioche une carte du deck.
        /// </summary>
        public bool DrawFromDeck()
        {
            if (_currentPhase != TurnPhase.Draw) return false;

            CardModel? card = _deckManager.DrawFromPile();
            if (!card.HasValue) return false;

            CurrentPlayer.AddToHand(card.Value);

            EventBus.Publish(new CardDrawnEvent
            {
                PlayerIndex = _currentPlayerIndex,
                Card = card.Value,
                FromDiscard = false,
                DiscardPileIndex = -1
            });

            AdvanceToLayDown();
            return true;
        }

        /// <summary>
        /// Le joueur pioche une carte d'une pile de défausse.
        /// </summary>
        public bool DrawFromDiscard(int discardPileIndex)
        {
            if (_currentPhase != TurnPhase.Draw) return false;

            CardModel? card = _deckManager.DrawFromDiscard(discardPileIndex);
            if (!card.HasValue) return false;

            CurrentPlayer.AddToHand(card.Value);

            EventBus.Publish(new CardDrawnEvent
            {
                PlayerIndex = _currentPlayerIndex,
                Card = card.Value,
                FromDiscard = true,
                DiscardPileIndex = discardPileIndex
            });

            AdvanceToLayDown();
            return true;
        }

        /// <summary>
        /// Le joueur tente de poser son niveau.
        /// </summary>
        public bool TryLayDownLevel(List<Meld> melds)
        {
            if (_currentPhase != TurnPhase.LayDown) return false;
            if (CurrentPlayer.HasLaidDownThisRound) return false;

            // Vérifier que le niveau est valide
            List<CardModel> allCards = new();
            foreach (Meld meld in melds)
            {
                allCards.AddRange(meld.Cards);
            }

            if (!LevelValidator.IsLevelComplete(CurrentPlayer.Hand, CurrentPlayer.CurrentLevel,
                    _config, out List<Meld> _))
            {
                return false;
            }

            // Retirer les cartes de la main et poser les combinaisons
            CurrentPlayer.RemoveFromHand(allCards);

            foreach (Meld meld in melds)
            {
                Meld playerMeld = new(meld.Type, meld.Cards, _currentPlayerIndex);
                CurrentPlayer.LaidMelds.Add(playerMeld);
            }

            CurrentPlayer.HasLaidDownThisRound = true;

            List<List<CardModel>> meldCards = new();
            foreach (Meld m in melds)
            {
                meldCards.Add(m.Cards);
            }

            EventBus.Publish(new LevelLaidDownEvent
            {
                PlayerIndex = _currentPlayerIndex,
                Level = CurrentPlayer.CurrentLevel,
                Melds = meldCards
            });

            // Avancer vers AddToMelds puisque le niveau a été posé
            _currentPhase = TurnPhase.AddToMelds;
            EventBus.Publish(new TurnPhaseChangedEvent
            {
                PlayerIndex = _currentPlayerIndex,
                NewPhase = _currentPhase
            });

            return true;
        }

        /// <summary>
        /// Passe la phase de pose du niveau (le joueur ne veut/peut pas poser).
        /// </summary>
        public void SkipLayDown()
        {
            if (_currentPhase == TurnPhase.LayDown)
            {
                AdvancePhase();
            }
        }

        /// <summary>
        /// Le joueur ajoute une carte à une combinaison existante (la sienne ou celle d'un adversaire).
        /// </summary>
        public bool AddToMeld(CardModel card, int meldOwnerIndex, int meldIndex)
        {
            if (_currentPhase != TurnPhase.AddToMelds) return false;
            if (!CurrentPlayer.HasLaidDownThisRound) return false;

            PlayerModel meldOwner = _players[meldOwnerIndex];
            if (meldIndex < 0 || meldIndex >= meldOwner.LaidMelds.Count) return false;

            Meld meld = meldOwner.LaidMelds[meldIndex];

            if (!CurrentPlayer.Hand.Contains(card)) return false;

            if (meld.TryAddCard(card))
            {
                CurrentPlayer.RemoveFromHand(card);

                EventBus.Publish(new CardAddedToMeldEvent
                {
                    PlayerIndex = _currentPlayerIndex,
                    MeldOwnerIndex = meldOwnerIndex,
                    MeldIndex = meldIndex,
                    Card = card
                });

                return true;
            }

            return false;
        }

        /// <summary>
        /// Passe la phase d'ajout aux combinaisons.
        /// </summary>
        public void SkipAddToMelds()
        {
            if (_currentPhase == TurnPhase.AddToMelds)
            {
                AdvancePhase();
            }
        }

        /// <summary>
        /// Le joueur défausse une carte. C'est la dernière action du tour.
        /// Si la carte est une action, son effet est appliqué.
        /// Retourne true si le round est terminé (main vide).
        /// </summary>
        public bool Discard(CardModel card, int targetPlayerIndex = -1)
        {
            if (_currentPhase != TurnPhase.Discard) return false;
            if (!CurrentPlayer.Hand.Contains(card)) return false;

            CurrentPlayer.RemoveFromHand(card);

            if (card.IsAction && targetPlayerIndex >= 0)
            {
                _actionHandler.HandleActionCard(_currentPlayerIndex, card, targetPlayerIndex);
            }

            _deckManager.Discard(_currentPlayerIndex, card);

            EventBus.Publish(new CardDiscardedEvent
            {
                PlayerIndex = _currentPlayerIndex,
                Card = card
            });

            // Vérifier si le round est terminé
            if (CurrentPlayer.IsHandEmpty)
            {
                return true;
            }

            // Passer au joueur suivant
            NextTurn();
            return false;
        }

        /// <summary>
        /// Avance de la phase Draw à LayDown.
        /// Appelé par le GameInputController après une pioche gérée côté UI.
        /// </summary>
        public void AdvanceFromDraw()
        {
            if (_currentPhase == TurnPhase.Draw)
            {
                AdvanceToLayDown();
            }
        }

        /// <summary>
        /// Avance de la phase Draw à LayDown (interne).
        /// </summary>
        private void AdvanceToLayDown()
        {
            _currentPhase = TurnPhase.LayDown;
            EventBus.Publish(new TurnPhaseChangedEvent
            {
                PlayerIndex = _currentPlayerIndex,
                NewPhase = _currentPhase
            });
        }

        /// <summary>
        /// Avance à la phase suivante.
        /// </summary>
        private void AdvancePhase()
        {
            _currentPhase = _currentPhase switch
            {
                TurnPhase.Draw => TurnPhase.LayDown,
                TurnPhase.LayDown => CurrentPlayer.HasLaidDownThisRound
                    ? TurnPhase.AddToMelds
                    : TurnPhase.Discard,
                TurnPhase.AddToMelds => TurnPhase.Discard,
                _ => TurnPhase.Discard
            };

            EventBus.Publish(new TurnPhaseChangedEvent
            {
                PlayerIndex = _currentPlayerIndex,
                NewPhase = _currentPhase
            });
        }

        /// <summary>
        /// Passe au tour du joueur suivant.
        /// </summary>
        private void NextTurn()
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
