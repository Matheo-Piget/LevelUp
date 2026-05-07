using System.Collections.Generic;
using NUnit.Framework;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.Tests
{
    /// <summary>
    /// Vérifie les méthodes de validation Run/Set/Flush avec wilds dans toutes
    /// les positions (début, milieu, fin) et les rejets (doublons, couleurs mixtes).
    /// </summary>
    [TestFixture]
    public class CardExtensionsTests
    {
        [SetUp]
        public void SetUp() => CardFactory.Reset();

        // ──────────────────────────────────────────────
        //  RUN
        // ──────────────────────────────────────────────

        [Test]
        public void IsValidRun_consecutive_returnsTrue()
        {
            List<CardModel> cards = new() { CardFactory.R(3), CardFactory.B(4), CardFactory.G(5) };
            Assert.IsTrue(cards.IsValidRun());
        }

        [Test]
        public void IsValidRun_withWildInMiddle_returnsTrue()
        {
            List<CardModel> cards = new() { CardFactory.R(3), CardFactory.Wild(), CardFactory.G(5) };
            Assert.IsTrue(cards.IsValidRun());
        }

        [Test]
        public void IsValidRun_withWildAtEnd_returnsTrue()
        {
            List<CardModel> cards = new() { CardFactory.R(3), CardFactory.B(4), CardFactory.Wild() };
            Assert.IsTrue(cards.IsValidRun());
        }

        [Test]
        public void IsValidRun_withWildAtStart_returnsTrue()
        {
            List<CardModel> cards = new() { CardFactory.Wild(), CardFactory.B(4), CardFactory.G(5) };
            Assert.IsTrue(cards.IsValidRun());
        }

        [Test]
        public void IsValidRun_withTwoWilds_returnsTrue()
        {
            List<CardModel> cards = new() { CardFactory.R(3), CardFactory.Wild(), CardFactory.Wild() };
            Assert.IsTrue(cards.IsValidRun());
        }

        [Test]
        public void IsValidRun_duplicateValue_returnsFalse()
        {
            List<CardModel> cards = new() { CardFactory.R(3), CardFactory.B(3), CardFactory.G(5) };
            Assert.IsFalse(cards.IsValidRun());
        }

        [Test]
        public void IsValidRun_gapTooBigForWilds_returnsFalse()
        {
            List<CardModel> cards = new() { CardFactory.R(3), CardFactory.Wild(), CardFactory.G(7) };
            // Trou de 3 (4,5,6) avec un seul wild → invalide.
            Assert.IsFalse(cards.IsValidRun());
        }

        [Test]
        public void IsValidRun_singleCard_returnsFalse()
        {
            List<CardModel> cards = new() { CardFactory.R(3) };
            Assert.IsFalse(cards.IsValidRun());
        }

        // ──────────────────────────────────────────────
        //  SET
        // ──────────────────────────────────────────────

        [Test]
        public void IsValidSet_sameValueDifferentColors_returnsTrue()
        {
            List<CardModel> cards = new() { CardFactory.R(7), CardFactory.B(7), CardFactory.G(7) };
            Assert.IsTrue(cards.IsValidSet());
        }

        [Test]
        public void IsValidSet_sameValueWithWild_returnsTrue()
        {
            List<CardModel> cards = new() { CardFactory.R(7), CardFactory.Wild(), CardFactory.G(7) };
            Assert.IsTrue(cards.IsValidSet());
        }

        [Test]
        public void IsValidSet_differentValues_returnsFalse()
        {
            List<CardModel> cards = new() { CardFactory.R(7), CardFactory.B(8) };
            Assert.IsFalse(cards.IsValidSet());
        }

        [Test]
        public void IsValidSet_onlyWilds_returnsTrue()
        {
            List<CardModel> cards = new() { CardFactory.Wild(), CardFactory.Wild() };
            Assert.IsTrue(cards.IsValidSet());
        }

        // ──────────────────────────────────────────────
        //  FLUSH
        // ──────────────────────────────────────────────

        [Test]
        public void IsValidFlush_sameColor_returnsTrue()
        {
            List<CardModel> cards = new()
            {
                CardFactory.R(2), CardFactory.R(5), CardFactory.R(7), CardFactory.R(9), CardFactory.R(11)
            };
            Assert.IsTrue(cards.IsValidFlush());
        }

        [Test]
        public void IsValidFlush_sameColorWithWild_returnsTrue()
        {
            List<CardModel> cards = new()
            {
                CardFactory.R(2), CardFactory.Wild(), CardFactory.R(7), CardFactory.R(9), CardFactory.R(11)
            };
            Assert.IsTrue(cards.IsValidFlush());
        }

        [Test]
        public void IsValidFlush_mixedColors_returnsFalse()
        {
            List<CardModel> cards = new()
            {
                CardFactory.R(2), CardFactory.B(5), CardFactory.R(7), CardFactory.R(9), CardFactory.R(11)
            };
            Assert.IsFalse(cards.IsValidFlush());
        }
    }
}
