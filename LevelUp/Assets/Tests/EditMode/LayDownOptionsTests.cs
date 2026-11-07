using System.Collections.Generic;
using NUnit.Framework;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.Tests
{
    /// <summary>
    /// Régressions de la recherche de pose : une carte disputée entre deux
    /// combinaisons (brelan / couleur / suite) ne doit plus faire échouer la
    /// pose, et toutes les répartitions possibles doivent être proposées au
    /// joueur. Les règles (validation des combinaisons) ne changent pas.
    /// </summary>
    [TestFixture]
    public class LayDownOptionsTests
    {
        [SetUp]
        public void SetUp()
        {
            CardFactory.Reset();
            GameSession.Contracts = ContractPlan.Classic;
        }

        [Test]
        public void Level4_moreFlushCardsThanNeeded_stillValidates()
        {
            // Niveau 4 = couleur de 5 + suite de 3. Six rouges : avant, le groupe
            // rouge (plus grand que 5) était ignoré et la pose échouait.
            List<CardModel> hand = new()
            {
                CardFactory.R(2), CardFactory.R(5), CardFactory.R(9), CardFactory.R(11), CardFactory.R(13),
                CardFactory.R(7), CardFactory.B(6), CardFactory.G(8),
            };
            Assert.IsTrue(LevelValidator.IsLevelComplete(hand, 4, null, out List<Meld> melds));
            Assert.AreEqual(2, melds.Count);
        }

        [Test]
        public void Level4_redSevenCanGoToRunOrFlush_offersBothChoices()
        {
            // Le 7 rouge peut compléter la suite 6-7-8 OU la couleur rouge :
            // le joueur doit pouvoir choisir.
            List<CardModel> hand = new()
            {
                CardFactory.R(2), CardFactory.R(5), CardFactory.R(9), CardFactory.R(11), CardFactory.R(13),
                CardFactory.R(7), CardFactory.B(6), CardFactory.Y(7), CardFactory.G(8),
            };
            List<List<Meld>> options = LevelValidator.FindLayDownOptions(hand, 4, null);
            Assert.GreaterOrEqual(options.Count, 2);

            bool redSevenInRun = false, redSevenInFlush = false;
            foreach (List<Meld> option in options)
            {
                foreach (Meld meld in option)
                {
                    foreach (CardModel c in meld.Cards)
                    {
                        if (c.Value != 7 || c.Color != CardColor.Red) continue;
                        if (meld.Type == MeldType.Run) redSevenInRun = true;
                        if (meld.Type == MeldType.Flush) redSevenInFlush = true;
                    }
                }
            }
            Assert.IsTrue(redSevenInRun, "une option doit mettre le 7 rouge dans la suite");
            Assert.IsTrue(redSevenInFlush, "une option doit mettre le 7 rouge dans la couleur");
        }

        [Test]
        public void Level7_fourOfAKind_forTwoSets_offersEveryChoice()
        {
            // Niveau 7 = 2 brelans. Quatre 7 : avant, le groupe était ignoré.
            List<CardModel> hand = new()
            {
                CardFactory.R(7), CardFactory.B(7), CardFactory.G(7), CardFactory.Y(7),
                CardFactory.R(9), CardFactory.B(9), CardFactory.G(9),
            };
            Assert.IsTrue(LevelValidator.IsLevelComplete(hand, 7, null, out List<Meld> _));

            List<List<Meld>> options = LevelValidator.FindLayDownOptions(hand, 7, null);
            // 4 façons de choisir le 7 qui reste en main.
            Assert.AreEqual(4, options.Count);
        }

        [Test]
        public void EveryOption_isStructurallyValid_andMatchesLevel()
        {
            List<CardModel> hand = new()
            {
                CardFactory.R(2), CardFactory.R(5), CardFactory.R(9), CardFactory.R(11), CardFactory.R(13),
                CardFactory.R(7), CardFactory.B(6), CardFactory.Y(7), CardFactory.G(8), CardFactory.Wild(),
            };
            List<List<Meld>> options = LevelValidator.FindLayDownOptions(hand, 4, null, 32);
            Assert.Greater(options.Count, 0);

            foreach (List<Meld> option in options)
            {
                Assert.IsTrue(LevelValidator.MeldsSatisfyLevel(option, 4, null));
                HashSet<int> used = new();
                foreach (Meld meld in option)
                {
                    bool valid = meld.Type switch
                    {
                        MeldType.Run => CardExtensions.IsValidRun(meld.Cards),
                        MeldType.Set => CardExtensions.IsValidSet(meld.Cards),
                        MeldType.Flush => CardExtensions.IsValidFlush(meld.Cards),
                        _ => false
                    };
                    Assert.IsTrue(valid, $"combinaison invalide : {meld.Type}");
                    foreach (CardModel c in meld.Cards) Assert.IsTrue(used.Add(c.Id), "carte utilisée deux fois");
                }
            }
        }

        [Test]
        public void IdenticalCards_doNotCreateDuplicateOptions()
        {
            // Deux 7 rouges identiques : les poses qui ne diffèrent que par
            // l'exemplaire utilisé ne comptent qu'une fois.
            List<CardModel> hand = new()
            {
                CardFactory.R(7), CardFactory.R(7), CardFactory.B(7), CardFactory.G(7),
                CardFactory.R(9), CardFactory.B(9), CardFactory.G(9),
            };
            List<List<Meld>> options = LevelValidator.FindLayDownOptions(hand, 7, null);
            // 7 restant en main : rouge, bleu ou vert → 3 poses distinctes.
            Assert.AreEqual(3, options.Count);
        }
    }
}
