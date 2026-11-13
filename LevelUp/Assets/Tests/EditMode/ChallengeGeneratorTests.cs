using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using LevelUp.Core;

namespace LevelUp.Tests
{
    /// <summary>Mode Défi : chaque défi généré est faisable, mais pas déjà gagné.</summary>
    [TestFixture]
    public class ChallengeGeneratorTests
    {
        private GameConfig _config = null!;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<GameConfig>();

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            GameSession.Contracts = ContractPlan.Classic;
        }

        private static List<CardModel> Hand(Challenge c)
        {
            // Les HandSize + Draws dernières cartes du paquet : la main (dessus), puis les pioches.
            List<CardModel> hand = new();
            for (int i = 0; i < c.HandSize; i++) hand.Add(c.DeckOrder[c.DeckOrder.Count - 1 - i]);
            return hand;
        }

        [Test]
        public void EveryChallenge_isSolvable_butNotAlreadySolved([Values(0, 1, 2)] int difficulty)
        {
            for (int seed = 1; seed <= 60; seed++)
            {
                Challenge c = ChallengeGenerator.Create(_config, seed, difficulty);
                GameSession.Contracts = ContractPlan.Custom(c.Contract);

                List<CardModel> hand = Hand(c);
                Assert.AreEqual(ChallengeGenerator.HandSize, hand.Count);
                Assert.AreEqual(ChallengeGenerator.Draws, c.Upcoming.Count);
                Assert.IsFalse(LevelValidator.IsLevelComplete(hand, 1, _config, out _),
                    $"seed {seed} : la main de départ pose déjà le contrat");

                List<CardModel> all = new(hand);
                all.AddRange(c.Upcoming);
                Assert.IsTrue(LevelValidator.IsLevelComplete(all, 1, _config, out _),
                    $"seed {seed} : aucune solution avec la main et les pioches");
            }
        }

        [Test]
        public void SameSeed_givesTheSameChallenge()
        {
            Challenge a = ChallengeGenerator.Create(_config, 77, 1);
            Challenge b = ChallengeGenerator.Create(_config, 77, 1);
            Assert.AreEqual(a.DeckOrder.Count, b.DeckOrder.Count);
            for (int i = 0; i < a.DeckOrder.Count; i++) Assert.AreEqual(a.DeckOrder[i].Id, b.DeckOrder[i].Id);
        }

        [Test]
        public void Deck_hasEveryCardExactlyOnce()
        {
            Challenge c = ChallengeGenerator.Create(_config, 5, 2);
            HashSet<int> ids = new();
            foreach (CardModel card in c.DeckOrder) Assert.IsTrue(ids.Add(card.Id), "carte en double");
            Assert.AreEqual(_config.DeckSize, ids.Count);
        }
    }
}
