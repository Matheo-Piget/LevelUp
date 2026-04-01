using System.Collections.Generic;
using LevelUp.Utils;
using UnityEngine;

namespace LevelUp.Core
{
    /// <summary>
    /// Gère le deck de cartes : création, mélange (Fisher-Yates), distribution, pioche et défausse.
    /// </summary>
    public class DeckManager
    {
        private readonly List<CardModel> _drawPile = new();
        private readonly List<List<CardModel>> _discardPiles = new();
        private int _nextCardId;
        private readonly GameConfig _config;

        /// <summary>Nombre de cartes restantes dans la pioche.</summary>
        public int DrawPileCount => _drawPile.Count;

        /// <summary>Nombre de piles de défausse (une par joueur).</summary>
        public int DiscardPileCount => _discardPiles.Count;

        /// <summary>
        /// Constructeur avec config ScriptableObject.
        /// </summary>
        public DeckManager(GameConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Crée un deck complet basé sur la configuration et le mélange.
        /// </summary>
        public void CreateAndShuffle(int playerCount)
        {
            _drawPile.Clear();
            _discardPiles.Clear();
            _nextCardId = 0;

            // Créer les cartes normales
            CardColor[] colors = { CardColor.Red, CardColor.Blue, CardColor.Green,
                                   CardColor.Yellow, CardColor.Purple, CardColor.Orange };

            int cardsPerColor = _config.DeckSize / colors.Length;
            int valRange = _config.CardMaxValue - _config.CardMinValue + 1;

            foreach (CardColor color in colors)
            {
                for (int i = 0; i < cardsPerColor; i++)
                {
                    int value = _config.CardMinValue + (i % valRange);
                    _drawPile.Add(new CardModel(_nextCardId++, value, color));
                }
            }

            // Compléter si le deck n'est pas parfaitement divisible
            while (_drawPile.Count < _config.DeckSize)
            {
                CardColor color = colors[_drawPile.Count % colors.Length];
                int value = _config.CardMinValue + (_drawPile.Count % valRange);
                _drawPile.Add(new CardModel(_nextCardId++, value, color));
            }

            Shuffle();

            // Créer les piles de défausse (une par joueur)
            for (int i = 0; i < playerCount; i++)
            {
                _discardPiles.Add(new List<CardModel>());
            }
        }

        /// <summary>
        /// Mélange le deck en utilisant l'algorithme Fisher-Yates.
        /// </summary>
        public void Shuffle()
        {
            for (int i = _drawPile.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_drawPile[i], _drawPile[j]) = (_drawPile[j], _drawPile[i]);
            }
        }

        /// <summary>
        /// Distribue les cartes de départ à tous les joueurs.
        /// </summary>
        public List<List<CardModel>> Deal(int playerCount)
        {
            List<List<CardModel>> hands = new();

            for (int p = 0; p < playerCount; p++)
            {
                List<CardModel> hand = new();
                for (int c = 0; c < _config.CardsPerPlayer; c++)
                {
                    if (_drawPile.Count > 0)
                    {
                        hand.Add(_drawPile[^1]);
                        _drawPile.RemoveAt(_drawPile.Count - 1);
                    }
                }
                hands.Add(hand);
            }

            EventBus.Publish(new DeckChangedEvent { CardsRemaining = _drawPile.Count });
            return hands;
        }

        /// <summary>
        /// Pioche une carte du dessus de la pioche.
        /// Si la pioche est vide, recycle les défausses.
        /// </summary>
        public CardModel? DrawFromPile()
        {
            if (_drawPile.Count == 0)
            {
                RecycleDiscards();
            }

            if (_drawPile.Count == 0)
            {
                return null;
            }

            CardModel card = _drawPile[^1];
            _drawPile.RemoveAt(_drawPile.Count - 1);
            EventBus.Publish(new DeckChangedEvent { CardsRemaining = _drawPile.Count });
            return card;
        }

        /// <summary>
        /// Pioche la carte du dessus de la défausse d'un joueur donné.
        /// </summary>
        public CardModel? DrawFromDiscard(int discardPileIndex)
        {
            if (discardPileIndex < 0 || discardPileIndex >= _discardPiles.Count)
                return null;

            List<CardModel> pile = _discardPiles[discardPileIndex];
            if (pile.Count == 0) return null;

            CardModel card = pile[^1];
            pile.RemoveAt(pile.Count - 1);
            return card;
        }

        /// <summary>
        /// Place une carte sur la défausse d'un joueur.
        /// </summary>
        public void Discard(int playerIndex, CardModel card)
        {
            if (playerIndex >= 0 && playerIndex < _discardPiles.Count)
            {
                _discardPiles[playerIndex].Add(card);
            }
        }

        /// <summary>
        /// Retourne la carte visible sur le dessus d'une pile de défausse.
        /// </summary>
        public CardModel? PeekDiscard(int discardPileIndex)
        {
            if (discardPileIndex < 0 || discardPileIndex >= _discardPiles.Count)
                return null;

            List<CardModel> pile = _discardPiles[discardPileIndex];
            return pile.Count > 0 ? pile[^1] : null;
        }

        /// <summary>
        /// Recycle toutes les défausses (sauf la carte du dessus de chacune) dans la pioche.
        /// </summary>
        private void RecycleDiscards()
        {
            foreach (List<CardModel> pile in _discardPiles)
            {
                if (pile.Count > 1)
                {
                    for (int i = 0; i < pile.Count - 1; i++)
                    {
                        _drawPile.Add(pile[i]);
                    }

                    CardModel top = pile[^1];
                    pile.Clear();
                    pile.Add(top);
                }
            }

            Shuffle();
            EventBus.Publish(new DeckChangedEvent { CardsRemaining = _drawPile.Count });
        }
    }
}
