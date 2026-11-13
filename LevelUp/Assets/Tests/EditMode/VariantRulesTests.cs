using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.Tests
{
    /// <summary>
    /// Variantes de règles (toutes coupées par défaut) : bonus de sortie,
    /// dernier niveau obligatoire, limite de tours, coup de pouce.
    /// </summary>
    [TestFixture]
    public class VariantRulesTests
    {
        private static readonly PlayerSetup[] TwoPlayers = { new("A", false), new("B", false) };

        private readonly List<GameObject> _objects = new();
        private GameConfig _config = null!;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _config = ScriptableObject.CreateInstance<GameConfig>();
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

        private GameManager Start(MatchRules rules)
        {
            GameObject go = new("GM_Variants");
            _objects.Add(go);
            GameManager gm = go.AddComponent<GameManager>();
            typeof(GameManager).GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(gm, _config);
            gm.StartGame(TwoPlayers, 11, rules);
            return gm;
        }

        [Test]
        public void Classic_isStillTheDefault()
        {
            MatchRules rules = new();
            Assert.AreEqual(2, rules.GoOutBonus);
            Assert.IsFalse(rules.FinalLevelRequired);
            Assert.AreEqual(0, rules.RoundTurnLimit);
            Assert.IsFalse(rules.CatchUp);
            Assert.IsTrue(rules.IsClassic);
            Assert.IsFalse(new MatchRules(catchUp: true).IsClassic);
        }

        [Test]
        public void Rules_roundTripThroughBytes()
        {
            MatchRules rules = new(true, SpecialCardsMode.All, 8, 6, 30, goOutBonus: 1,
                finalLevelRequired: true, roundTurnLimit: 25, catchUp: true);
            Assert.AreEqual(rules, MatchRules.FromBytes(rules.ToBytes()));
            Assert.IsNull(MatchRules.FromBytes(new byte[] { 1, 2, 3 }), "Bloc tronqué refusé");
            Assert.IsNull(MatchRules.FromBytes(new byte[200]), "Bloc trop long refusé");
        }

        [Test]
        public void GoOutBonus_one_givesASingleLevel()
        {
            GameManager gm = Start(new MatchRules(goOutBonus: 1));
            gm.Players[0].HasLaidDownThisRound = true;
            gm.OnRoundEnd(0);
            Assert.AreEqual(2, gm.Players[0].CurrentLevel);
        }

        [Test]
        public void FinalLevelRequired_bonusStopsAtTheLastLevel()
        {
            GameManager gm = Start(new MatchRules(levelsToWin: 4, finalLevelRequired: true));
            gm.Players[0].CurrentLevel = 3;
            gm.Players[0].HasLaidDownThisRound = true;
            gm.OnRoundEnd(0);
            Assert.AreEqual(4, gm.Players[0].CurrentLevel, "Le bonus ne saute pas le dernier niveau");
            Assert.AreNotEqual(GameState.GameOver, gm.State);

            // Au dernier niveau, poser et sortir fait gagner.
            gm.Players[0].HasLaidDownThisRound = true;
            gm.OnRoundEnd(0);
            Assert.AreEqual(GameState.GameOver, gm.State);
        }

        [Test]
        public void TurnLimit_endsTheRoundWithoutGoOutBonus()
        {
            GameManager gm = Start(new MatchRules(roundTurnLimit: 1));
            for (int turn = 0; turn < 2; turn++)
            {
                int seat = gm.TurnManager!.CurrentPlayerIndex;
                Assert.IsTrue(gm.ExecuteCommand(new DrawFromDeckCommand(seat)).Success);
                Assert.IsTrue(gm.ExecuteCommand(new SkipPhaseCommand(seat, TurnPhase.LayDown)).Success);
                Assert.IsTrue(gm.ExecuteCommand(new DiscardCommand(seat, gm.Players[seat].Hand[0])).Success);
            }
            Assert.AreEqual(GameState.PlayerTurn, gm.State, "AutoStartNextRound : la manche suivante est lancée");
            Assert.AreEqual(2, gm.RoundNumber, "Un tour chacun, puis la manche s'arrête");
            Assert.AreEqual(1, gm.Players[0].CurrentLevel, "Personne n'a posé : personne ne monte");
            Assert.AreEqual(1, gm.Players[1].CurrentLevel);
        }

        [Test]
        public void CatchUp_dealsAnExtraCardAfterTwoRoundsAtTheSameLevel()
        {
            GameManager gm = Start(new MatchRules(catchUp: true));
            int normal = gm.Players[1].Hand.Count;

            // Le joueur 0 gagne deux manches de suite : le joueur 1 piétine.
            gm.Players[0].HasLaidDownThisRound = true;
            gm.OnRoundEnd(0);
            Assert.AreEqual(normal, gm.Players[1].Hand.Count, "Une seule manche sans monter : pas encore");
            gm.Players[0].HasLaidDownThisRound = true;
            gm.OnRoundEnd(0);

            Assert.AreEqual(normal + 1, gm.Players[1].Hand.Count, "Deux manches au même niveau : une carte de plus");
            Assert.AreEqual(normal, gm.Players[0].Hand.Count);
        }
    }
}
