using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.Tests
{
    /// <summary>
    /// Vérifie que <see cref="ActionCardHandler.GetNextPlayer"/> consomme
    /// correctement les Skip empilés (un par un) et que deux Skip jouées
    /// contre le même joueur le font sauter deux tours.
    /// </summary>
    [TestFixture]
    public class SkipStackingTests
    {
        private List<PlayerModel> _players = null!;
        private DeckManager _deck = null!;
        private ActionCardHandler _handler = null!;
        private GameConfig _config = null!;

        [SetUp]
        public void SetUp()
        {
            CardFactory.Reset();
            _config = ScriptableObject.CreateInstance<GameConfig>();
            _players = new List<PlayerModel>
            {
                new(0, "P0", false),
                new(1, "P1", false),
                new(2, "P2", false),
                new(3, "P3", false),
            };
            _deck = new DeckManager(_config);
            _deck.CreateAndShuffle(_players.Count);
            _handler = new ActionCardHandler(_deck, _players);
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null) Object.DestroyImmediate(_config);
            EventBus.Clear();
        }

        [Test]
        public void NoSkip_advancesByOne()
        {
            int next = _handler.GetNextPlayer(0);
            Assert.AreEqual(1, next);
        }

        [Test]
        public void OneSkip_skipsOnePlayer()
        {
            _players[1].SkipCount = 1;
            int next = _handler.GetNextPlayer(0);
            Assert.AreEqual(2, next);
            Assert.AreEqual(0, _players[1].SkipCount, "Le compteur doit être consommé");
        }

        [Test]
        public void TwoSkipsOnSamePlayer_skipsTwoTurns()
        {
            // Deux Skip joués contre le joueur 1.
            _players[1].SkipCount = 2;

            // Tour suivant : on saute le 1 (consomme 1 → reste 1) et on passe au 2.
            int firstNext = _handler.GetNextPlayer(0);
            Assert.AreEqual(2, firstNext);
            Assert.AreEqual(1, _players[1].SkipCount, "Un seul Skip consommé pour ce tour");

            // Le tour suivant après le 2, le joueur 3 joue normalement.
            int secondNext = _handler.GetNextPlayer(2);
            Assert.AreEqual(3, secondNext);

            // Et au tour suivant après le 3, on tente de revenir au 0… mais avant
            // ça on retombe sur le 1 qui a encore 1 Skip → on le saute, retour au 2.
            // Or 2 vient de jouer, donc en pratique le flow normal fait : 3 → 0.
            int thirdNext = _handler.GetNextPlayer(3);
            Assert.AreEqual(0, thirdNext);

            // Et au prochain passage par 1, son dernier Skip se consomme.
            int fourthNext = _handler.GetNextPlayer(0);
            Assert.AreEqual(2, fourthNext);
            Assert.AreEqual(0, _players[1].SkipCount, "Tous les Skip consommés");
        }

        [Test]
        public void HandleSkip_incrementsCounter()
        {
            // Jouer une carte Skip ciblant le joueur 2 : son SkipCount monte.
            CardModel skip = CardFactory.Skip();
            _handler.HandleActionCard(playerIndex: 0, skip, targetPlayerIndex: 2);
            Assert.AreEqual(1, _players[2].SkipCount);

            // Une seconde Skip → empile à 2.
            CardModel skip2 = CardFactory.Skip();
            _handler.HandleActionCard(playerIndex: 0, skip2, targetPlayerIndex: 2);
            Assert.AreEqual(2, _players[2].SkipCount);
        }

        [Test]
        public void Skip_cannotTargetSelf()
        {
            CardModel skip = CardFactory.Skip();
            _handler.HandleActionCard(playerIndex: 0, skip, targetPlayerIndex: 0);
            Assert.AreEqual(0, _players[0].SkipCount,
                "Un joueur ne peut pas se Skip lui-même");
        }
    }
}
