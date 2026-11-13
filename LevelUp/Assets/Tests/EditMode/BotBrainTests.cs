using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using LevelUp.AI;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.Tests
{
    /// <summary>
    /// Décisions des bots : estimation du manque pour le contrat, refus de
    /// "nourrir" la table quand un adversaire a posé, ramassage utile d'une
    /// défausse, jokers gardés, cible des cartes action.
    /// </summary>
    [TestFixture]
    public class BotBrainTests
    {
        private readonly List<GameObject> _objects = new();
        private GameConfig _config = null!;
        private GameManager _gm = null!;

        private static readonly PlayerSetup[] ThreeSeats =
        {
            new("Bot", true),
            new("Alice", false),
            new("Bob", false),
        };

        [SetUp]
        public void SetUp()
        {
            CardFactory.ResetTo(20000);
            EventBus.Clear();
            GameSession.Contracts = ContractPlan.Classic;
            GameSession.Rules = MatchRules.Classic;
            _config = ScriptableObject.CreateInstance<GameConfig>();

            GameObject go = new("GM_Bot");
            _objects.Add(go);
            _gm = go.AddComponent<GameManager>();
            typeof(GameManager)
                .GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(_gm, _config);
            _gm.StartGame(ThreeSeats, 99, MatchRules.Classic);

            // Mains vidées : chaque test pose exactement ses cartes.
            foreach (PlayerModel p in _gm.Players) p.ResetForNewRound();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects) Object.DestroyImmediate(go);
            _objects.Clear();
            Object.DestroyImmediate(_config);
            GameSession.Contracts = ContractPlan.Classic;
            GameSession.Rules = MatchRules.Classic;
            EventBus.Clear();
        }

        private BotBrain Brain(AIDifficulty difficulty = AIDifficulty.Normal) => new(_gm, 0, difficulty, 1234);

        private void Give(int player, params CardModel[] cards)
        {
            foreach (CardModel c in cards) _gm.Players[player].AddToHand(c);
        }

        /// <summary>Alice (siège 1) a posé une suite 5-6-7 et n'a plus que 2 cartes.</summary>
        private void AliceLaidDownRun()
        {
            PlayerModel alice = _gm.Players[1];
            alice.HasLaidDownThisRound = true;
            alice.AddMeld(new Meld(MeldType.Run,
                new List<CardModel> { CardFactory.B(5), CardFactory.G(6), CardFactory.Y(7) }, 1));
            Give(1, CardFactory.O(14), CardFactory.P(2));
        }

        [Test]
        public void Missing_isZeroForACompleteContract()
        {
            // Niveau 1 classique : 2 suites de 3
            List<CardModel> hand = new()
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(5),
                CardFactory.Y(9), CardFactory.P(10), CardFactory.O(11), CardFactory.R(15)
            };
            Assert.AreEqual(0, Brain().MissingFor(hand, 1));

            hand.RemoveAt(2);
            Assert.AreEqual(1, Brain().MissingFor(hand, 1));
        }

        [Test]
        public void Missing_triesEveryRequirementOrder()
        {
            // Niveau 4 classique : couleur de 5 + suite de 3. Prendre la suite
            // d'abord (1-2-3 rouges) casserait la couleur : il faut l'autre ordre.
            List<CardModel> hand = new()
            {
                CardFactory.R(1), CardFactory.R(2), CardFactory.R(3), CardFactory.R(7), CardFactory.R(9),
                CardFactory.B(4), CardFactory.G(5), CardFactory.Y(6)
            };
            Assert.AreEqual(0, Brain().MissingFor(hand, 4));
        }

        [Test]
        public void Jokers_fillTheGaps()
        {
            List<CardModel> hand = new()
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.Wild(),
                CardFactory.Y(9), CardFactory.P(10), CardFactory.O(11)
            };
            Assert.AreEqual(0, Brain().MissingFor(hand, 1));
        }

        [Test]
        public void Danger_onlyWhenAnOpponentCanPlayTheCard()
        {
            BotBrain brain = Brain();
            CardModel eight = CardFactory.R(8);
            CardModel twelve = CardFactory.R(12);

            AliceLaidDownRun();
            Assert.Greater(brain.Danger(eight), 100f, "Le 8 complète la suite d'Alice, qui a posé");
            Assert.AreEqual(0f, brain.Danger(twelve));

            _gm.Players[1].HasLaidDownThisRound = false;
            Assert.AreEqual(0f, brain.Danger(eight), "Personne ne peut l'ajouter tant qu'il n'a pas posé");
        }

        [Test]
        public void Discard_avoidsFeedingTheTable()
        {
            AliceLaidDownRun();

            // Contrat complet + deux cartes en trop : un 8 (jouable chez Alice) et un 1 isolé.
            // (Suites 13-15 et 10-12 : le 8 ne peut remplacer aucune de leurs cartes.)
            Give(0, CardFactory.R(13), CardFactory.B(14), CardFactory.G(15),
                CardFactory.Y(10), CardFactory.P(11), CardFactory.O(12),
                CardFactory.R(8), CardFactory.G(1));

            DiscardCommand discard = Brain().ChooseDiscard();
            Assert.AreEqual(1, discard.Card.Value, "Le 8 irait directement sur la suite d'Alice");
        }

        [Test]
        public void Discard_keepsJokers()
        {
            Give(0, CardFactory.Wild(), CardFactory.R(3), CardFactory.B(4), CardFactory.G(5),
                CardFactory.Y(9), CardFactory.P(10), CardFactory.O(11));

            DiscardCommand discard = Brain(AIDifficulty.Hard).ChooseDiscard();
            Assert.IsFalse(discard.Card.IsWild);
        }

        [Test]
        public void Discard_keepsCardsThatBuildTheContract()
        {
            // Il manque un 5 pour la première suite : le 17 isolé part, pas le 3 ou le 4.
            Give(0, CardFactory.R(3), CardFactory.B(4),
                CardFactory.Y(9), CardFactory.P(10), CardFactory.O(11), CardFactory.G(17));

            DiscardCommand discard = Brain(AIDifficulty.Hard).ChooseDiscard();
            Assert.AreEqual(17, discard.Card.Value);
        }

        [Test]
        public void Draw_takesTheDiscardThatCompletesTheContract()
        {
            Give(0, CardFactory.R(3), CardFactory.B(4),
                CardFactory.Y(9), CardFactory.P(10), CardFactory.O(11), CardFactory.G(17));
            _gm.DeckManager!.Discard(2, CardFactory.G(5));
            _gm.DeckManager.Discard(1, CardFactory.P(16));

            Assert.AreEqual(2, Brain().ChooseDrawPile(), "Le 5 vert termine la suite 3-4-5");
            Assert.AreEqual(2, Brain(AIDifficulty.Hard).ChooseDrawPile());
        }

        [Test]
        public void Draw_prefersTheDeckWhenNothingHelps()
        {
            Give(0, CardFactory.R(3), CardFactory.B(4),
                CardFactory.Y(9), CardFactory.P(10), CardFactory.O(11), CardFactory.G(17));
            _gm.DeckManager!.Discard(1, CardFactory.P(14));

            Assert.AreEqual(-1, Brain().ChooseDrawPile());
        }

        [Test]
        public void AfterLayingDown_takesAPlayableDiscard()
        {
            AliceLaidDownRun();
            PlayerModel bot = _gm.Players[0];
            bot.HasLaidDownThisRound = true;
            Give(0, CardFactory.R(17), CardFactory.B(12));
            _gm.DeckManager!.Discard(2, CardFactory.R(4));

            Assert.AreEqual(2, Brain().ChooseDrawPile(), "Le 4 se pose tout de suite sur la suite 5-6-7");
        }

        [Test]
        public void AddToMeld_findsAPlayableCard()
        {
            AliceLaidDownRun();
            PlayerModel bot = _gm.Players[0];
            bot.HasLaidDownThisRound = true;
            CardModel eight = CardFactory.R(8);
            Give(0, CardFactory.B(15), eight);

            Assert.IsTrue(Brain().TryChooseAddToMeld(out CardModel card, out int owner, out int meld));
            Assert.AreEqual(eight, card);
            Assert.AreEqual(1, owner);
            Assert.AreEqual(0, meld);
        }

        [Test]
        public void Target_isTheOpponentAboutToGoOut()
        {
            AliceLaidDownRun();
            Give(2, CardFactory.R(1), CardFactory.R(2), CardFactory.R(3), CardFactory.R(4),
                CardFactory.R(5), CardFactory.R(6), CardFactory.R(7), CardFactory.R(8), CardFactory.R(9));

            Assert.AreEqual(1, Brain().ChooseTarget(CardType.Skip));
            Assert.AreEqual(1, Brain(AIDifficulty.Hard).ChooseTarget(CardType.Draw2));
        }

        [Test]
        public void ActionCard_isPlayedOnAnUrgentThreat()
        {
            AliceLaidDownRun();
            Give(0, CardFactory.R(13), CardFactory.B(14), CardFactory.G(15),
                CardFactory.Y(9), CardFactory.P(10), CardFactory.O(11),
                CardFactory.G(1), CardFactory.Skip());

            DiscardCommand discard = Brain(AIDifficulty.Hard).ChooseDiscard();
            Assert.AreEqual(CardType.Skip, discard.Card.Type, "Alice n'a plus que 2 cartes : on la bloque");
            Assert.AreEqual(1, discard.TargetPlayerIndex);
        }
    }
}
