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
        private readonly GameConfig _config;
        private readonly MatchRules _rules;

        // RNG dédié (jamais UnityEngine.Random) : avec un seed partagé, tous les
        // appareils d'une partie en ligne produisent exactement les mêmes mélanges,
        // round après round — condition du modèle "simulation miroir".
        private readonly System.Random _rng;

        /// <summary>Nombre de cartes restantes dans la pioche.</summary>
        public int DrawPileCount => _drawPile.Count;

        /// <summary>Nombre de piles de défausse (une par joueur).</summary>
        public int DiscardPileCount => _discardPiles.Count;

        /// <summary>
        /// Constructeur avec config ScriptableObject. Un seed identique garantit
        /// des mélanges identiques (requis pour le multijoueur en ligne). Les
        /// règles de partie fixent la taille de main et les cartes spéciales
        /// (aucune par défaut : paquet classique).
        /// </summary>
        public DeckManager(GameConfig config, int? seed = null, MatchRules? rules = null)
        {
            _config = config;
            _rules = rules ?? MatchRules.Classic;
            _rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        /// <summary>Cartes distribuées par joueur : règle de la partie, sinon la config.</summary>
        public int CardsPerPlayer => _rules.HandSize > 0 ? _rules.HandSize : _config.CardsPerPlayer;

        /// <summary>
        /// Crée un deck complet basé sur la configuration et le mélange.
        /// </summary>
        public void CreateAndShuffle(int playerCount)
        {
            _drawPile.Clear();
            _discardPiles.Clear();
            _drawPile.AddRange(BuildComposition(_config, _rules));

            Shuffle();

            // Créer les piles de défausse (une par joueur)
            for (int i = 0; i < playerCount; i++)
            {
                _discardPiles.Add(new List<CardModel>());
            }
        }

        /// <summary>
        /// Composition complète du paquet (non mélangée, ids 0..N-1 dans l'ordre) :
        /// cartes numérotées selon la config, puis cartes spéciales si les règles
        /// les activent. Publique pour que l'IA connaisse le paquet sans le toucher.
        /// </summary>
        public static List<CardModel> BuildComposition(GameConfig config, MatchRules? rules = null)
        {
            MatchRules r = rules ?? MatchRules.Classic;
            List<CardModel> cards = new(config.DeckSize + r.JokerCount + r.SkipCount + r.Draw2Count);
            int nextId = 0;

            CardColor[] colors = { CardColor.Red, CardColor.Blue, CardColor.Green,
                                   CardColor.Yellow, CardColor.Purple, CardColor.Orange };

            int cardsPerColor = config.DeckSize / colors.Length;
            int valRange = config.CardMaxValue - config.CardMinValue + 1;

            foreach (CardColor color in colors)
            {
                for (int i = 0; i < cardsPerColor; i++)
                {
                    int value = config.CardMinValue + (i % valRange);
                    cards.Add(new CardModel(nextId++, value, color));
                }
            }

            // Compléter si le deck n'est pas parfaitement divisible
            while (cards.Count < config.DeckSize)
            {
                CardColor color = colors[cards.Count % colors.Length];
                int value = config.CardMinValue + (cards.Count % valRange);
                cards.Add(new CardModel(nextId++, value, color));
            }

            // Cartes spéciales : uniquement si la partie les active (option).
            for (int i = 0; i < r.JokerCount; i++) cards.Add(new CardModel(nextId++, CardType.Wild));
            for (int i = 0; i < r.SkipCount; i++) cards.Add(new CardModel(nextId++, CardType.Skip));
            for (int i = 0; i < r.Draw2Count; i++) cards.Add(new CardModel(nextId++, CardType.Draw2));

            return cards;
        }

        /// <summary>
        /// Mélange le deck en utilisant l'algorithme Fisher-Yates.
        /// </summary>
        public void Shuffle()
        {
            for (int i = _drawPile.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(0, i + 1);
                (_drawPile[i], _drawPile[j]) = (_drawPile[j], _drawPile[i]);
            }
        }

        /// <summary>
        /// Distribue les cartes de départ à tous les joueurs.
        /// </summary>
        public List<List<CardModel>> Deal(int playerCount)
        {
            List<List<CardModel>> hands = new();
            int perPlayer = CardsPerPlayer;

            for (int p = 0; p < playerCount; p++)
            {
                List<CardModel> hand = new();
                for (int c = 0; c < perPlayer; c++)
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

        /// <summary>Nombre de cartes dans la défausse d'un joueur (0 si index invalide).</summary>
        public int GetDiscardCount(int discardPileIndex)
        {
            if (discardPileIndex < 0 || discardPileIndex >= _discardPiles.Count) return 0;
            return _discardPiles[discardPileIndex].Count;
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
