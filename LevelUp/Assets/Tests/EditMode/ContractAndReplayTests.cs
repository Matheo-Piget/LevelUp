using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.Tests
{
    /// <summary>
    /// Couvre le catalogue de contrats (textes, variantes, déterminisme du tirage),
    /// l'aide de progression, et la reconstruction d'une partie par rejeu de son
    /// historique — base de la reconnexion / resynchronisation en ligne.
    /// </summary>
    [TestFixture]
    public class ContractAndReplayTests
    {
        private readonly List<GameObject> _objects = new();
        private GameConfig _config = null!;

        [SetUp]
        public void SetUp()
        {
            CardFactory.Reset();
            EventBus.Clear();
            GameSession.Contracts = ContractPlan.Classic;
            _config = ScriptableObject.CreateInstance<GameConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects) Object.DestroyImmediate(go);
            _objects.Clear();
            Object.DestroyImmediate(_config);
            GameSession.Contracts = ContractPlan.Classic;
            EventBus.Muted = false;
            EventBus.Clear();
        }

        // ──────────────────────────────────────────────
        //  Catalogue
        // ──────────────────────────────────────────────

        [Test]
        public void EveryLevel_hasAtLeastOneVariant()
        {
            for (int level = 1; level <= Constants.MaxLevel; level++)
            {
                Assert.Greater(ContractCatalog.VariantCount(level), 0, $"niveau {level}");
                Assert.IsNotEmpty(ContractCatalog.Get(level, 0), $"niveau {level}");
            }
        }

        [Test]
        public void Describe_groupsIdenticalRequirements()
        {
            Assert.AreEqual("2 suites de 3", ContractCatalog.DescribeLevel(1));
            Assert.AreEqual("1 brelan + 1 paire", ContractCatalog.DescribeLevel(2));
            Assert.AreEqual("1 flush de 5 + 1 suite de 3", ContractCatalog.DescribeLevel(4));
            Assert.AreEqual("2 brelans", ContractCatalog.DescribeLevel(7));
        }

        [Test]
        public void RandomPlan_isDeterministicForASeed()
        {
            ContractPlan a = ContractPlan.CreateRandom(4242);
            ContractPlan b = ContractPlan.CreateRandom(4242);
            for (int level = 1; level <= Constants.MaxLevel; level++)
            {
                Assert.AreEqual(ContractCatalog.Describe(a.GetRequirements(level)),
                    ContractCatalog.Describe(b.GetRequirements(level)), $"niveau {level}");
            }
        }

        [Test]
        public void RandomPlan_eventuallyDiffersFromClassic()
        {
            bool differs = false;
            for (int seed = 0; seed < 20 && !differs; seed++)
            {
                ContractPlan plan = ContractPlan.CreateRandom(seed);
                for (int level = 1; level <= Constants.MaxLevel; level++)
                {
                    if (ContractCatalog.Describe(plan.GetRequirements(level))
                        != ContractCatalog.Describe(ContractPlan.Classic.GetRequirements(level)))
                    {
                        differs = true;
                        break;
                    }
                }
            }
            Assert.IsTrue(differs);
        }

        [Test]
        public void Validator_followsActivePlan()
        {
            // Variante 1 du niveau 3 = 1 suite de 5 (au lieu de suite de 3 + brelan)
            List<CardModel> runOf5 = new()
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(5), CardFactory.Y(6), CardFactory.P(7),
            };
            Assert.IsFalse(LevelValidator.IsLevelComplete(runOf5, 3, null, out _));

            ContractPlan? withRun5 = null;
            for (int seed = 0; seed < 200 && withRun5 == null; seed++)
            {
                ContractPlan plan = ContractPlan.CreateRandom(seed);
                if (ContractCatalog.Describe(plan.GetRequirements(3)) == "1 suite de 5") withRun5 = plan;
            }
            Assert.IsNotNull(withRun5);

            GameSession.Contracts = withRun5!;
            Assert.IsTrue(LevelValidator.IsLevelComplete(runOf5, 3, null, out _));
        }

        // ──────────────────────────────────────────────
        //  Progression
        // ──────────────────────────────────────────────

        [Test]
        public void Progress_reportsPartialRequirements()
        {
            // Niveau 3 classique = suite de 3 + brelan
            List<CardModel> hand = new()
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(9), CardFactory.Y(9), CardFactory.P(15),
            };
            List<ContractProgress.Entry> entries =
                ContractProgress.Evaluate(hand, GameSession.Contracts.GetRequirements(3));

            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual(MeldType.Run, entries[0].Requirement.Type);
            Assert.AreEqual(2, entries[0].Have);
            Assert.AreEqual(MeldType.Set, entries[1].Requirement.Type);
            Assert.AreEqual(2, entries[1].Have);
            Assert.IsFalse(entries[0].IsMet);
        }

        // ──────────────────────────────────────────────
        //  Rejeu d'historique (reprise en ligne)
        // ──────────────────────────────────────────────

        private GameManager CreateGameManager()
        {
            GameObject go = new("GM_Test");
            _objects.Add(go);
            GameManager gm = go.AddComponent<GameManager>();
            typeof(GameManager)
                .GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(gm, _config);
            return gm;
        }

        private static readonly PlayerSetup[] TwoPlayers =
        {
            new("A", false),
            new("B", false),
        };

        /// <summary>Joue N tours simples (pioche, passe la pose, défausse) en notant les commandes.</summary>
        private static List<IGameCommand> PlayTurns(GameManager gm, int turns)
        {
            List<IGameCommand> history = new();
            for (int t = 0; t < turns; t++)
            {
                int p = gm.TurnManager!.CurrentPlayerIndex;
                IGameCommand[] steps =
                {
                    new DrawFromDeckCommand(p),
                    new SkipPhaseCommand(p, TurnPhase.LayDown),
                };
                foreach (IGameCommand step in steps)
                {
                    Assert.IsTrue(gm.ExecuteCommand(step).Success);
                    history.Add(step);
                }
                IGameCommand discard = new DiscardCommand(p, gm.Players[p].Hand[0], -1);
                Assert.IsTrue(gm.ExecuteCommand(discard).Success);
                history.Add(discard);
            }
            return history;
        }

        [Test]
        public void Rebuild_fromHistory_reachesSameState()
        {
            GameManager original = CreateGameManager();
            original.StartGame(TwoPlayers, 1234, randomContracts: true);
            List<IGameCommand> history = PlayTurns(original, 6);

            GameManager mirror = CreateGameManager();
            bool ok = mirror.RebuildFromHistory(TwoPlayers, 1234, true, history);

            Assert.IsTrue(ok);
            Assert.AreEqual(original.ComputeStateHash(), mirror.ComputeStateHash());
            Assert.IsFalse(EventBus.Muted, "Le bus doit être réactivé après le rejeu");
        }

        [Test]
        public void Rebuild_doesNotRaiseCommandExecuted()
        {
            GameManager original = CreateGameManager();
            original.StartGame(TwoPlayers, 99);
            List<IGameCommand> history = PlayTurns(original, 2);

            GameManager mirror = CreateGameManager();
            int relayed = 0;
            mirror.CommandExecuted += (_, _) => relayed++;
            mirror.RebuildFromHistory(TwoPlayers, 99, false, history);

            Assert.AreEqual(0, relayed, "Un rejeu ne doit jamais être relayé sur le réseau");
        }

        [Test]
        public void StateHash_ignoresLocalHandOrder_butSeesRealChanges()
        {
            GameManager gm = CreateGameManager();
            gm.StartGame(TwoPlayers, 7);
            int before = gm.ComputeStateHash();

            gm.Players[0].ReorderHand(0, gm.Players[0].Hand.Count - 1);
            Assert.AreEqual(before, gm.ComputeStateHash(), "Le tri de main est local");

            gm.ExecuteCommand(new DrawFromDeckCommand(gm.TurnManager!.CurrentPlayerIndex));
            Assert.AreNotEqual(before, gm.ComputeStateHash());
        }

        [Test]
        public void DifferentSeed_givesDifferentState()
        {
            GameManager a = CreateGameManager();
            a.StartGame(TwoPlayers, 1);
            GameManager b = CreateGameManager();
            b.StartGame(TwoPlayers, 2);
            Assert.AreNotEqual(a.ComputeStateHash(), b.ComputeStateHash());
        }
    }
}
