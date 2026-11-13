using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Rend lisibles les pioches des autres joueurs : une carte vole du paquet
    /// (dos) ou de la défausse ramassée (face) jusqu'à la pastille du joueur.
    /// Les poses et ajouts sur la table, eux, partent de la pastille (TableView).
    /// </summary>
    public class OpponentMovesView : MonoBehaviour
    {
        private GameManager? _gameManager;
        private CardFlights? _flights;
        private RectTransform? _deck;
        private DiscardPileView? _piles;
        private LevelProgressView? _players;

        public void Initialize(GameManager gameManager, CardFlights flights, RectTransform? deck,
            DiscardPileView? piles, LevelProgressView? players)
        {
            _gameManager = gameManager;
            _flights = flights;
            _deck = deck;
            _piles = piles;
            _players = players;
        }

        private void OnEnable() => EventBus.Subscribe<CardDrawnEvent>(OnCardDrawn);
        private void OnDisable() => EventBus.Unsubscribe<CardDrawnEvent>(OnCardDrawn);

        private void OnCardDrawn(CardDrawnEvent evt)
        {
            if (_gameManager == null || _flights == null || _players == null) return;
            if (evt.PlayerIndex < 0 || evt.PlayerIndex >= _gameManager.Players.Count) return;
            if (GameSession.IsLocalHuman(_gameManager.Players[evt.PlayerIndex])) return;
            if (!_players.TryGetPillOrigin(evt.PlayerIndex, out CardOrigin target)) return;

            RectTransform? from = evt.FromDiscard && _piles != null
                ? _piles.GetPileRect(evt.DiscardPileIndex)
                : _deck;
            if (from == null) return;

            // Taille de départ : celle d'une carte posée sur le tapis.
            float startScale = from.lossyScale.x * 0.9f;
            _flights.Fly(evt.FromDiscard ? evt.Card : (CardModel?)null, from.position, target.Position,
                startScale, target.Scale);
        }
    }
}
