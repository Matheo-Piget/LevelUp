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

        // ──────────────────────────────────────────────
        //  Tri post-ajout
        // ──────────────────────────────────────────────

        [Test]
        public void Run_addCard_sortsByValueAscending()
        {
            // Ajouter une carte plus petite : doit se retrouver en tête.
            Meld meld = new(MeldType.Run, new List<CardModel>
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(5)
            }, 0);

            CardModel small = CardFactory.Y(2);
            Assert.IsTrue(meld.TryAddCard(small));
            Assert.AreEqual(2, meld.Cards[0].Value);
            Assert.AreEqual(3, meld.Cards[1].Value);
            Assert.AreEqual(4, meld.Cards[2].Value);
            Assert.AreEqual(5, meld.Cards[3].Value);
        }

        [Test]
        public void Run_addWildToFillGap_wildPlacedInGap()
        {
            // Run [3, 4, _, 6] avec un wild qui comble le 5.
            Meld meld = new(MeldType.Run, new List<CardModel>
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(6)
            }, 0);

            CardModel wild = CardFactory.Wild();
            Assert.IsTrue(meld.TryAddCard(wild),
                "Le wild doit pouvoir compléter le run [3,4,_,6]");

            // Le wild doit être en position 2 (entre 4 et 6), pas en début.
            Assert.AreEqual(3, meld.Cards[0].Value);
            Assert.AreEqual(4, meld.Cards[1].Value);
            Assert.IsTrue(meld.Cards[2].IsWild,
                "Le wild doit être placé dans le trou, pas en tête");
            Assert.AreEqual(6, meld.Cards[3].Value);
        }

        [Test]
        public void Run_addWildAtEnd_wildPlacedAfterMax()
        {
            // [3, 4, 5] + wild → doit donner [3, 4, 5, W] (extension à la fin)
            Meld meld = new(MeldType.Run, new List<CardModel>
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(5)
            }, 0);

            Assert.IsTrue(meld.TryAddCard(CardFactory.Wild()));

            // Wild doit être au bout, pas en tête.
            Assert.AreEqual(3, meld.Cards[0].Value);
            Assert.AreEqual(4, meld.Cards[1].Value);
            Assert.AreEqual(5, meld.Cards[2].Value);
            Assert.IsTrue(meld.Cards[3].IsWild);
        }

        [Test]
        public void Set_addCard_doesNotReorderWild()
        {
            // Set qui contient déjà un wild placé après les normales.
            CardModel wild = CardFactory.Wild();
            CardModel n1 = CardFactory.R(7);
            CardModel n2 = CardFactory.B(7);
            Meld meld = new(MeldType.Set, new List<CardModel> { n1, n2, wild }, 0);

            CardModel n3 = CardFactory.G(7);
            Assert.IsTrue(meld.TryAddCard(n3));

            // Le wild ne doit pas remonter en tête juste parce qu'il a Value < 0.
            Assert.IsFalse(meld.Cards[0].IsWild,
                "Set : le wild ne doit pas être trié en tête");
        }

        [Test]
        public void Flush_addCard_doesNotReorderWild()
        {
            CardModel wild = CardFactory.Wild();
            Meld meld = new(MeldType.Flush, new List<CardModel>
            {
                CardFactory.R(2), CardFactory.R(5), CardFactory.R(7), wild, CardFactory.R(11)
            }, 0);

            Assert.IsTrue(meld.TryAddCard(CardFactory.R(14)));

            Assert.IsFalse(meld.Cards[0].IsWild,
                "Flush : le wild ne doit pas être trié en tête");
        }
    }
}
