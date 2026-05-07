using System.Collections.Generic;
using NUnit.Framework;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.Tests
{
    /// <summary>
    /// Vérifie le comportement de <see cref="Meld.TryAddCard"/> et <see cref="Meld.CanAddCard"/>
    /// pour l'ajout de cartes à une combinaison existante (phase AddToMelds).
    /// </summary>
    [TestFixture]
    public class MeldTests
    {
        [SetUp]
        public void SetUp() => CardFactory.Reset();

        [Test]
        public void Run_extendByOne_succeeds()
        {
            Meld meld = new(MeldType.Run, new List<CardModel>
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(5)
            }, 0);

            Assert.IsTrue(meld.TryAddCard(CardFactory.Y(6)), "Doit accepter une carte qui prolonge le run");
            Assert.AreEqual(4, meld.Cards.Count);
        }

        [Test]
        public void Run_addInvalidValue_fails()
        {
            Meld meld = new(MeldType.Run, new List<CardModel>
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(5)
            }, 0);

            Assert.IsFalse(meld.TryAddCard(CardFactory.Y(10)), "Carte trop loin doit être rejetée");
            Assert.AreEqual(3, meld.Cards.Count);
        }

        [Test]
        public void Set_addSameValue_succeeds()
        {
            Meld meld = new(MeldType.Set, new List<CardModel>
            {
                CardFactory.R(7), CardFactory.B(7), CardFactory.G(7)
            }, 0);

            Assert.IsTrue(meld.TryAddCard(CardFactory.Y(7)));
            Assert.AreEqual(4, meld.Cards.Count);
        }

        [Test]
        public void Set_addDifferentValue_fails()
        {
            Meld meld = new(MeldType.Set, new List<CardModel>
            {
                CardFactory.R(7), CardFactory.B(7), CardFactory.G(7)
            }, 0);

            Assert.IsFalse(meld.TryAddCard(CardFactory.Y(8)));
        }

        [Test]
        public void Flush_addSameColor_succeeds()
        {
            Meld meld = new(MeldType.Flush, new List<CardModel>
            {
                CardFactory.R(2), CardFactory.R(5), CardFactory.R(7), CardFactory.R(11), CardFactory.R(14)
            }, 0);

            Assert.IsTrue(meld.TryAddCard(CardFactory.R(9)));
        }

        [Test]
        public void Flush_addDifferentColor_fails()
        {
            Meld meld = new(MeldType.Flush, new List<CardModel>
            {
                CardFactory.R(2), CardFactory.R(5), CardFactory.R(7), CardFactory.R(11), CardFactory.R(14)
            }, 0);

            Assert.IsFalse(meld.TryAddCard(CardFactory.B(9)));
        }

        [Test]
        public void CanAddCard_doesNotMutateMeld()
        {
            Meld meld = new(MeldType.Run, new List<CardModel>
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(5)
            }, 0);

            int countBefore = meld.Cards.Count;
            bool canAdd = meld.CanAddCard(CardFactory.Y(6));

            Assert.IsTrue(canAdd);
            Assert.AreEqual(countBefore, meld.Cards.Count, "CanAddCard ne doit jamais muter");
        }
    }
}
