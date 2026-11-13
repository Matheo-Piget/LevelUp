using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.Tests
{
    /// <summary>
    /// Gardes du Core (v5 des règles) : sortie par pose, phases non passables,
    /// commandes hors manche, fin de manche explicite, pénalités, codec des
    /// commandes et sauvegarde de partie.
    /// </summary>
    [TestFixture]
    public class CoreGuardsTests
    {
        private static readonly PlayerSetup[] ThreePlayers =
        {
            new("A", false), new("B", true), new("C", true)
        };

        private readonly List<GameObject> _objects = new();
        private GameConfig _config = null!;

        [SetUp]
        public void SetUp()
        {
            CardFactory.ResetTo(5000);
            EventBus.Clear();
            GameSession.Contracts = ContractPlan.Classic;
            GameSession.Rules = MatchRules.Classic;
            _config = ScriptableObject.CreateInstance<GameConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects) Object.DestroyImmediate(go);
            _objects.Clear();
            Object.DestroyImmediate(_config);
            SaveGame.PathOverride = null;
            GameSession.Contracts = ContractPlan.Classic;
            GameSession.Rules = MatchRules.Classic;
            EventBus.Clear();
        }

        private GameManager CreateGameManager()
        {
            GameObject go = new("GM_Guards");
            _objects.Add(go);
            GameManager gm = go.AddComponent<GameManager>();
            typeof(GameManager)
                .GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(gm, _config);
            return gm;
        }

        private (List<PlayerModel> players, TurnManager turn, GameCommandExecutor exec) CreateExecutor()
        {
            List<PlayerModel> players = new() { new PlayerModel(0, "A", false), new PlayerModel(1, "B", false) };
            DeckManager deck = new(_config, 3);
            deck.CreateAndShuffle(players.Count);
            ActionCardHandler handler = new(deck, players);
            TurnManager turn = new(players, handler);
            turn.StartRound(0);
            return (players, turn, new GameCommandExecutor(players, deck, turn, handler, _config));
        }

        // ──────────────────────────────────────────────
        //  Règles
        // ──────────────────────────────────────────────

        [Test]
        public void LayDown_usingWholeHand_endsTheRound()
        {
            (List<PlayerModel> players, TurnManager turn, GameCommandExecutor exec) = CreateExecutor();
            CardModel[] hand =
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(5),
                CardFactory.Y(8), CardFactory.P(9), CardFactory.O(10)
            };
            foreach (CardModel c in hand) players[0].AddToHand(c);
            turn.AdvanceToPhase(TurnPhase.LayDown);

            List<Meld> melds = new()
            {
                new Meld(MeldType.Run, new List<CardModel> { hand[0], hand[1], hand[2] }, 0),
                new Meld(MeldType.Run, new List<CardModel> { hand[3], hand[4], hand[5] }, 0)
            };
            CommandResult result = exec.Execute(new LayDownLevelCommand(0, melds));

            Assert.IsTrue(result.Success, result.Message);
            Assert.IsTrue(result.RoundEnded, "Main vidée par la pose : le joueur sort");
        }

        [TestCase(TurnPhase.Draw)]
        [TestCase(TurnPhase.Discard)]
        public void SkipPhase_drawAndDiscard_areRefused(TurnPhase phase)
        {
            (List<PlayerModel> players, TurnManager turn, GameCommandExecutor exec) = CreateExecutor();
            players[0].AddToHand(CardFactory.R(5));
            turn.AdvanceToPhase(phase);

            Assert.IsFalse(exec.Execute(new SkipPhaseCommand(0, phase)).Success);
            Assert.AreEqual(phase, turn.CurrentPhase);
        }

        [Test]
        public void Discard_usesTheRealCard_notTheAnnouncedFields()
        {
            (List<PlayerModel> players, TurnManager turn, GameCommandExecutor exec) = CreateExecutor();
            CardModel real = CardFactory.R(5);
            players[0].AddToHand(real);
            players[0].AddToHand(CardFactory.B(7));
            turn.AdvanceToPhase(TurnPhase.Discard);

            CardModel? discarded = null;
            EventBus.Subscribe<CardDiscardedEvent>(e => discarded = e.Card);
            // Même identifiant, valeur et couleur trafiquées.
            CardModel forged = new(real.Id, 18, CardColor.Blue);
            Assert.IsTrue(exec.Execute(new DiscardCommand(0, forged)).Success);

            Assert.IsTrue(discarded.HasValue);
            Assert.AreEqual(5, discarded!.Value.Value);
            Assert.AreEqual(CardColor.Red, discarded.Value.Color);
        }

        [Test]
        public void Commands_areRefused_outsideARound()
        {
            GameManager gm = CreateGameManager();
            gm.StartGame(ThreePlayers, 11, MatchRules.Classic);
            gm.AbandonGame();

            CommandResult result = gm.ExecuteCommand(new DrawFromDeckCommand(gm.TurnManager!.CurrentPlayerIndex));
            Assert.IsFalse(result.Success);
        }

        [Test]
        public void RoundEnd_waitsForStartNextRound_whenNotAutomatic()
        {
            GameManager gm = CreateGameManager();
            gm.AutoStartNextRound = false;
            gm.StartGame(ThreePlayers, 7, MatchRules.Classic);

            RoundSummaryEvent? summary = null;
            EventBus.Subscribe<RoundSummaryEvent>(e => summary = e);
            gm.Players[1].HasLaidDownThisRound = true;
            gm.OnRoundEnd(1);

            Assert.AreEqual(GameState.EndRound, gm.State);
            Assert.AreEqual(1, gm.RoundNumber);
            Assert.IsTrue(summary.HasValue);
            Assert.IsTrue(summary!.Value.AwaitingNextRound);
            Assert.AreEqual(3, summary.Value.LevelsAfter[1], "Vainqueur posé : +2");
            Assert.IsFalse(gm.ExecuteCommand(new DrawFromDeckCommand(gm.TurnManager!.CurrentPlayerIndex)).Success,
                "Rien ne se joue pendant le récap");

            Assert.IsFalse(gm.ExecuteCommand(new StartNextRoundCommand(0, 5)).Success, "Mauvaise manche");
            Assert.IsTrue(gm.ExecuteCommand(new StartNextRoundCommand(0, 1)).Success);
            Assert.AreEqual(2, gm.RoundNumber);
            Assert.AreEqual(GameState.PlayerTurn, gm.State);
            Assert.IsFalse(gm.ExecuteCommand(new StartNextRoundCommand(0, 1)).Success, "Pas deux fois");
        }

        [Test]
        public void RoundEnd_addsPenaltyForCardsLeftInHand()
        {
            GameManager gm = CreateGameManager();
            gm.StartGame(ThreePlayers, 7, MatchRules.Classic);
            int expected = gm.Players[2].HandPenalty();
            Assert.Greater(expected, 0);

            gm.OnRoundEnd(0);

            Assert.AreEqual(expected, gm.Players[2].PenaltyPoints);
        }

        [Test]
        public void PenaltyValues()
        {
            Assert.AreEqual(5, PlayerModel.PenaltyFor(CardFactory.R(9)));
            Assert.AreEqual(10, PlayerModel.PenaltyFor(CardFactory.R(10)));
            Assert.AreEqual(15, PlayerModel.PenaltyFor(CardFactory.Skip()));
            Assert.AreEqual(25, PlayerModel.PenaltyFor(CardFactory.Wild()));
        }

        // ──────────────────────────────────────────────
        //  Codec
        // ──────────────────────────────────────────────

        [Test]
        public void Codec_roundTripsEveryCommand()
        {
            List<Meld> melds = new()
            {
                new Meld(MeldType.Run, new List<CardModel> { CardFactory.R(3), CardFactory.Wild(), CardFactory.G(5) }, 2),
                new Meld(MeldType.Set, new List<CardModel> { CardFactory.Y(8), CardFactory.P(8) }, 2)
            };
            IGameCommand[] commands =
            {
                new DrawFromDeckCommand(1),
                new DrawFromDiscardCommand(2, 3),
                new LayDownLevelCommand(2, melds),
                new AddToMeldCommand(1, CardFactory.O(12), 0, 1),
                new SkipPhaseCommand(3, TurnPhase.AddToMelds),
                new DiscardCommand(0, CardFactory.Draw2(), 2),
                new StartNextRoundCommand(0, 4)
            };

            foreach (IGameCommand command in commands)
            {
                byte[] bytes = GameCommandCodec.ToBytes(command);
                IGameCommand? back = GameCommandCodec.FromBytes(bytes);
                Assert.IsNotNull(back, command.GetType().Name);
                Assert.AreEqual(command.GetType(), back!.GetType());
                CollectionAssert.AreEqual(bytes, GameCommandCodec.ToBytes(back), command.GetType().Name);
            }
        }

        [Test]
        public void Codec_rejectsGarbage()
        {
            Assert.IsNull(GameCommandCodec.FromBytes(null));
            Assert.IsNull(GameCommandCodec.FromBytes(new byte[0]));
            Assert.IsNull(GameCommandCodec.FromBytes(new byte[] { 99, 0, 0, 0, 0 }), "Type inconnu");
            Assert.IsNull(GameCommandCodec.FromBytes(new byte[] { 1, 0, 0 }), "Tronqué");

            byte[] ok = GameCommandCodec.ToBytes(new DrawFromDeckCommand(1));
            byte[] extra = new byte[ok.Length + 1];
            ok.CopyTo(extra, 0);
            Assert.IsNull(GameCommandCodec.FromBytes(extra), "Octets en trop");

            byte[] huge = new byte[GameCommandCodec.MaxCommandBytes + 1];
            Assert.IsNull(GameCommandCodec.FromBytes(huge));
        }

        // ──────────────────────────────────────────────
        //  Sauvegarde
        // ──────────────────────────────────────────────

        [Test]
        public void SaveGame_rebuildsTheSameGame()
        {
            SaveGame.PathOverride = Path.Combine(Application.temporaryCachePath, "levelup-test.sav");
            MatchRules rules = new(true, SpecialCardsMode.Jokers, 10, 6, 0);

            GameManager original = CreateGameManager();
            original.AutoStartNextRound = false;
            original.StartGame(ThreePlayers, 4242, rules);
            Assert.IsTrue(SaveGame.Begin(ThreePlayers, 4242, rules, 2));
            original.CommandExecuted += (cmd, _) => SaveGame.Append(cmd);

            for (int t = 0; t < 5; t++)
            {
                int p = original.TurnManager!.CurrentPlayerIndex;
                Assert.IsTrue(original.ExecuteCommand(new DrawFromDeckCommand(p)).Success);
                Assert.IsTrue(original.ExecuteCommand(new SkipPhaseCommand(p, TurnPhase.LayDown)).Success);
                CardModel card = original.Players[p].Hand[0];
                Assert.IsTrue(original.ExecuteCommand(new DiscardCommand(p, card, -1)).Success);
            }

            SavedGame? saved = SaveGame.Load();
            Assert.IsNotNull(saved);
            Assert.AreEqual(15, saved!.History.Count);
            Assert.AreEqual(rules, saved.Rules);
            Assert.AreEqual(2, saved.BotDifficulty);

            GameManager mirror = CreateGameManager();
            mirror.AutoStartNextRound = false;
            Assert.IsTrue(mirror.RebuildFromHistory(saved.Players, saved.Seed, saved.Rules, saved.History));
            Assert.AreEqual(original.ComputeStateHash(), mirror.ComputeStateHash());

            SaveGame.Delete();
            Assert.IsNull(SaveGame.Load());
        }

        /// <summary>
        /// Parties complètes de bots en mode « jeu réel » (récap entre les manches,
        /// StartNextRoundCommand) : elles se terminent toujours, et l'historique
        /// rejoué (= reprise d'une sauvegarde / reconnexion) redonne le même état.
        /// </summary>
        [TestCase(1, SpecialCardsMode.None)]
        [TestCase(2, SpecialCardsMode.All)]
        [TestCase(3, SpecialCardsMode.Jokers)]
        public void ExplicitRoundFlow_fullGame_finishesAndReplays(int seed, SpecialCardsMode special)
        {
            MatchRules rules = new(seed % 2 == 0, special, seed == 3 ? 8 : 10, 3);
            PlayerSetup[] seats = { new("A", true), new("B", true), new("C", true) };
            GameManager gm = CreateGameManager();
            gm.AutoStartNextRound = false;
            gm.StartGame(seats, seed * 313, rules);

            List<IGameCommand> history = new();
            gm.CommandExecuted += (cmd, _) => history.Add(cmd);

            LevelUp.AI.BotBrain[] brains = new LevelUp.AI.BotBrain[seats.Length];
            for (int i = 0; i < seats.Length; i++)
                brains[i] = new LevelUp.AI.BotBrain(gm, i, LevelUp.AI.AIDifficulty.Normal, seed * 7 + i);

            int summaries = 0;
            EventBus.Subscribe<RoundSummaryEvent>(_ => summaries++);

            for (int step = 0; step < 6000 && gm.State != GameState.GameOver; step++)
            {
                if (gm.State == GameState.EndRound)
                {
                    Assert.IsTrue(gm.ExecuteCommand(new StartNextRoundCommand(0, gm.RoundNumber)).Success);
                    continue;
                }

                TurnManager tm = gm.TurnManager!;
                int seat = tm.CurrentPlayerIndex;
                LevelUp.AI.BotBrain brain = brains[seat];
                int before = gm.ComputeStateHash();

                switch (tm.CurrentPhase)
                {
                    case TurnPhase.Draw:
                        int pile = brain.ChooseDrawPile();
                        if (pile < 0 || !gm.ExecuteCommand(new DrawFromDiscardCommand(seat, pile)).Success)
                            gm.ExecuteCommand(new DrawFromDeckCommand(seat));
                        break;
                    case TurnPhase.LayDown:
                        List<Meld>? melds = brain.ChooseLayDown();
                        if (melds == null || !gm.ExecuteCommand(new LayDownLevelCommand(seat, melds)).Success)
                            gm.ExecuteCommand(new SkipPhaseCommand(seat, TurnPhase.LayDown));
                        break;
                    case TurnPhase.AddToMelds:
                        if (!brain.TryChooseAddToMeld(out CardModel card, out int owner, out int meld)
                            || !gm.ExecuteCommand(new AddToMeldCommand(seat, card, owner, meld)).Success)
                            gm.ExecuteCommand(new SkipPhaseCommand(seat, TurnPhase.AddToMelds));
                        break;
                    case TurnPhase.Discard:
                        if (!gm.ExecuteCommand(brain.ChooseDiscard()).Success)
                        {
                            IGameCommand? fallback = TurnAutoPlay.NextCommand(gm);
                            if (fallback != null) gm.ExecuteCommand(fallback);
                        }
                        break;
                }

                Assert.AreNotEqual(before, gm.ComputeStateHash(),
                    $"Partie bloquée (seed {seed}, manche {gm.RoundNumber}, phase {tm.CurrentPhase})");
            }

            Assert.AreEqual(GameState.GameOver, gm.State, "La partie doit se terminer");
            Assert.AreEqual(gm.RoundNumber, summaries, "Un récap par manche");

            GameManager mirror = CreateGameManager();
            mirror.AutoStartNextRound = false;
            Assert.IsTrue(mirror.RebuildFromHistory(seats, seed * 313, rules, history));
            Assert.AreEqual(gm.ComputeStateHash(), mirror.ComputeStateHash());
            Assert.AreEqual(GameState.GameOver, mirror.State);
        }

        [Test]
        public void SaveGame_ignoresATruncatedTail()
        {
            string path = Path.Combine(Application.temporaryCachePath, "levelup-trunc.sav");
            SaveGame.PathOverride = path;
            Assert.IsTrue(SaveGame.Begin(ThreePlayers, 1, MatchRules.Classic, 1));
            SaveGame.Append(new DrawFromDeckCommand(0));
            SaveGame.Append(new SkipPhaseCommand(0, TurnPhase.LayDown));

            byte[] data = File.ReadAllBytes(path);
            System.Array.Resize(ref data, data.Length - 3); // coupe la dernière commande
            File.WriteAllBytes(path, data);

            SavedGame? saved = SaveGame.Load();
            Assert.IsNotNull(saved);
            Assert.AreEqual(1, saved!.History.Count);

            SaveGame.Append(new SkipPhaseCommand(0, TurnPhase.LayDown));
            Assert.AreEqual(2, SaveGame.Load()!.History.Count, "Le fichier a été réparé");
            SaveGame.Delete();
        }

        [Test]
        public void ExecuteCommand_fromAnEventHandler_isRefused()
        {
            GameManager gm = CreateGameManager();
            gm.StartGame(ThreePlayers, 42, MatchRules.Classic);

            CommandResult? nested = null;
            EventBus.Subscribe<CardDrawnEvent>(_ =>
                nested = gm.ExecuteCommand(new SkipPhaseCommand(0, TurnPhase.LayDown)));

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("ré-entrante"));
            CommandResult outer = gm.ExecuteCommand(new DrawFromDeckCommand(0));

            Assert.IsTrue(outer.Success);
            Assert.IsTrue(nested.HasValue && !nested.Value.Success, "La commande imbriquée doit être refusée");
            Assert.AreEqual(TurnPhase.LayDown, gm.TurnManager!.CurrentPhase, "La phase ne doit pas avoir été sautée");
        }
    }
}
