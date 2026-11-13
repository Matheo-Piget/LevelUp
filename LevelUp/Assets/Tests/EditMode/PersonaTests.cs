using System.Collections.Generic;
using NUnit.Framework;
using LevelUp.AI;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.Tests
{
    [TestFixture]
    public class PersonaTests
    {
        [Test]
        public void Names_areDistinct_andSkipThePlayersName()
        {
            List<string> names = BotPersonas.Names(3, new List<string> { "Léon" });
            Assert.AreEqual(3, names.Count);
            CollectionAssert.DoesNotContain(names, "Léon");
            CollectionAssert.AllItemsAreUnique(names);
        }

        [Test]
        public void Names_fallBackToNumberedBots_whenThePoolIsExhausted()
        {
            List<string> names = BotPersonas.Names(8);
            Assert.AreEqual(8, names.Count);
            CollectionAssert.AllItemsAreUnique(names);
            StringAssert.StartsWith("Bot ", names[7]);
        }

        [Test]
        public void UnknownName_isABalancedBot()
        {
            BotStyle style = BotPersonas.For("Bot 2");
            Assert.AreEqual(1f, style.DangerFactor);
            Assert.AreEqual(0f, style.ActionBonus);
            Assert.AreNotEqual(1f, BotPersonas.For("Margot").DangerFactor);
        }

        [Test]
        public void Coach_describesCardsInPlainFrench()
        {
            Assert.AreEqual("7 rouge", CoachAdvisor.Describe(new CardModel(1, 7, CardColor.Red)));
            Assert.AreEqual("joker", CoachAdvisor.Describe(new CardModel(2, CardType.Wild)));
            Assert.AreEqual("« Passe »", CoachAdvisor.Describe(new CardModel(3, CardType.Skip)));
        }
    }
}
