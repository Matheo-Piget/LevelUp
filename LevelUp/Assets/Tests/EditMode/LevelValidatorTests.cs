using System.Collections.Generic;
using NUnit.Framework;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.Tests
{
    /// <summary>
    /// Couvre <see cref="LevelValidator"/> sur les 8 niveaux par défaut + cas limites
    /// (wilds, hand insuffisante, mismatch). Couvre aussi <see cref="LevelValidator.MeldsSatisfyLevel"/>
    /// utilisé par le ExecuteLayDown pour rejeter les melds trichés.
    /// </summary>
    [TestFixture]
    public class LevelValidatorTests
    {
        [SetUp]
        public void SetUp() => CardFactory.Reset();

        // ──────────────────────────────────────────────
        //  IsLevelComplete — niveaux par défaut
        // ──────────────────────────────────────────────

        [Test]
        public void Level1_twoRunsOf3_validates()
        {
            // Niveau 1 = 2 suites de 3
            List<CardModel> hand = new()
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(5),
                CardFactory.Y(8), CardFactory.P(9), CardFactory.O(10),
            };
            Assert.IsTrue(LevelValidator.IsLevelComplete(hand, 1, null, out List<Meld> melds));
            Assert.AreEqual(2, melds.Count);
        }

        [Test]
        public void Level3_twoSets_validates()
        {
            List<CardModel> hand = new()
            {
                CardFactory.R(3), CardFactory.B(3), CardFactory.G(3),
                CardFactory.Y(8), CardFactory.P(8), CardFactory.O(8),
            };
            Assert.IsTrue(LevelValidator.IsLevelComplete(hand, 3, null, out List<Meld> melds));
            Assert.AreEqual(2, melds.Count);
            Assert.AreEqual(MeldType.Set, melds[0].Type);
            Assert.AreEqual(MeldType.Set, melds[1].Type);
        }

        [Test]
        public void Level5_flushOf5_validates()
        {
            List<CardModel> hand = new()
            {
                CardFactory.R(2), CardFactory.R(5), CardFactory.R(7), CardFactory.R(11), CardFactory.R(14),
            };
            Assert.IsTrue(LevelValidator.IsLevelComplete(hand, 5, null, out List<Meld> melds));
            Assert.AreEqual(1, melds.Count);
            Assert.AreEqual(MeldType.Flush, melds[0].Type);
        }

        [Test]
        public void Level1_withWildBridgingGap_validates()
        {
            // 3, W, 5 + 8, 9, 10 → niveau 1 valide
            List<CardModel> hand = new()
            {
                CardFactory.R(3), CardFactory.Wild(), CardFactory.G(5),
                CardFactory.Y(8), CardFactory.P(9), CardFactory.O(10),
            };
            Assert.IsTrue(LevelValidator.IsLevelComplete(hand, 1, null, out List<Meld> _));
        }

        [Test]
        public void Level1_handTooShort_fails()
        {
            List<CardModel> hand = new()
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(5),
            };
            Assert.IsFalse(LevelValidator.IsLevelComplete(hand, 1, null, out List<Meld> _));
        }

        [Test]
        public void Level7_quartetAndPair_validates()
        {
            // 4 cartes de valeur 5 + 2 cartes de valeur 9
            List<CardModel> hand = new()
            {
                CardFactory.R(5), CardFactory.B(5), CardFactory.G(5), CardFactory.Y(5),
                CardFactory.P(9), CardFactory.O(9),
            };
            Assert.IsTrue(LevelValidator.IsLevelComplete(hand, 7, null, out List<Meld> _));
        }

        [Test]
        public void Level8_flushOf7WithWilds_validates()
        {
            // 5 cartes rouges + 2 wilds = flush de 7
            List<CardModel> hand = new()
            {
                CardFactory.R(2), CardFactory.R(4), CardFactory.R(7),
                CardFactory.R(11), CardFactory.R(14),
                CardFactory.Wild(), CardFactory.Wild(),
            };
            Assert.IsTrue(LevelValidator.IsLevelComplete(hand, 8, null, out List<Meld> _));
        }

        // ──────────────────────────────────────────────
        //  MeldsSatisfyLevel — la garde anti-triche
        // ──────────────────────────────────────────────

        [Test]
        public void MeldsSatisfyLevel_validLevel1_returnsTrue()
        {
            List<Meld> melds = new()
            {
                new Meld(MeldType.Run, new List<CardModel>
                    { CardFactory.R(3), CardFactory.B(4), CardFactory.G(5) }, ownerIndex: 0),
                new Meld(MeldType.Run, new List<CardModel>
                    { CardFactory.Y(8), CardFactory.P(9), CardFactory.O(10) }, ownerIndex: 0),
            };
            Assert.IsTrue(LevelValidator.MeldsSatisfyLevel(melds, 1, null));
        }

        [Test]
        public void MeldsSatisfyLevel_wrongTypeForLevel_returnsFalse()
        {
            // Niveau 1 = 2 suites, on envoie 2 brelans → rejet
            List<Meld> melds = new()
            {
                new Meld(MeldType.Set, new List<CardModel>
                    { CardFactory.R(3), CardFactory.B(3), CardFactory.G(3) }, ownerIndex: 0),
                new Meld(MeldType.Set, new List<CardModel>
                    { CardFactory.Y(8), CardFactory.P(8), CardFactory.O(8) }, ownerIndex: 0),
            };
            Assert.IsFalse(LevelValidator.MeldsSatisfyLevel(melds, 1, null));
        }

        [Test]
        public void MeldsSatisfyLevel_wrongCountForLevel_returnsFalse()
        {
            // Niveau 1 attend 2 melds, on en envoie 1.
            List<Meld> melds = new()
            {
                new Meld(MeldType.Run, new List<CardModel>
                    { CardFactory.R(3), CardFactory.B(4), CardFactory.G(5) }, ownerIndex: 0),
            };
            Assert.IsFalse(LevelValidator.MeldsSatisfyLevel(melds, 1, null));
        }

        [Test]
        public void MeldsSatisfyLevel_runTooShort_returnsFalse()
        {
            // Niveau 6 = 1 run de 5, on envoie un run de 4
            List<Meld> melds = new()
            {
                new Meld(MeldType.Run, new List<CardModel>
                    { CardFactory.R(3), CardFactory.B(4), CardFactory.G(5), CardFactory.Y(6) },
                    ownerIndex: 0),
            };
            Assert.IsFalse(LevelValidator.MeldsSatisfyLevel(melds, 6, null));
        }

        [Test]
        public void MeldsSatisfyLevel_orderIndependent()
        {
            // Niveau 2 = 1 run de 3 + 1 brelan, peu importe l'ordre des melds.
            List<Meld> melds = new()
            {
                new Meld(MeldType.Set, new List<CardModel>
                    { CardFactory.R(3), CardFactory.B(3), CardFactory.G(3) }, ownerIndex: 0),
                new Meld(MeldType.Run, new List<CardModel>
                    { CardFactory.Y(8), CardFactory.P(9), CardFactory.O(10) }, ownerIndex: 0),
            };
            Assert.IsTrue(LevelValidator.MeldsSatisfyLevel(melds, 2, null));
        }
    }
}
