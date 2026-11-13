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
    /// Parties complètes entre bots, sans UI ni délais (même enchaînement que
    /// AIPlayer) : aucune partie ne doit se bloquer, quelles que soient les
    /// cartes spéciales, et un bot Expert doit battre nettement un bot Facile.
    /// Tout est déterministe (seeds fixes) : pas de test "chanceux".
    /// </summary>
    [TestFixture]
    public class BotSimulationTests
    {
        private readonly List<GameObject> _objects = new();
        private GameConfig _config = null!;

        [SetUp]
        public void SetUp()
        {
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
            GameSession.Contracts = ContractPlan.Classic;
            GameSession.Rules = MatchRules.Classic;
            EventBus.Clear();
        }

        private GameManager CreateGameManager()
        {
            GameObject go = new("GM_Sim");
            _objects.Add(go);
            GameManager gm = go.AddComponent<GameManager>();
            typeof(GameManager)
                .GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(gm, _config);
            return gm;
        }

        /// <summary>Joue une partie entière ; retourne le vainqueur (-1 si bloquée ou trop longue).</summary>
        private int PlayGame(AIDifficulty[] seats, int seed, MatchRules rules, out int rounds)
        {
            EventBus.Clear();
            GameManager gm = CreateGameManager();
            PlayerSetup[] setups = new PlayerSetup[seats.Length];
            for (int i = 0; i < seats.Length; i++) setups[i] = new PlayerSetup($"Bot {i + 1}", true);
            gm.StartGame(setups, seed, rules);

            BotBrain[] brains = new BotBrain[seats.Length];
            for (int i = 0; i < seats.Length; i++) brains[i] = new BotBrain(gm, i, seats[i], seed * 7 + i);

            int winner = -1;
            EventBus.Subscribe<GameOverEvent>(e => winner = e.WinnerIndex);
            EventBus.Subscribe<RoundStartedEvent>(_ => { foreach (BotBrain b in brains) b.OnRoundStarted(); });
            EventBus.Subscribe<CardDiscardedEvent>(e => { foreach (BotBrain b in brains) b.OnCardDiscarded(e.PlayerIndex, e.Card); });
            EventBus.Subscribe<CardDrawnEvent>(e => { foreach (BotBrain b in brains) b.OnCardDrawn(e.PlayerIndex, e.Card, e.FromDiscard); });

            for (int turn = 0; turn < 4000 && gm.State != GameState.GameOver; turn++)
            {
                int before = gm.ComputeStateHash();
                _lastGameTurns = turn + 1;
                PlayTurn(gm, brains[gm.TurnManager!.CurrentPlayerIndex]);
                if (gm.State != GameState.GameOver && gm.ComputeStateHash() == before)
                {
                    rounds = gm.RoundNumber;
                    return -1; // tour sans aucun effet : partie bloquée
                }
            }

            rounds = gm.RoundNumber;
            return gm.State == GameState.GameOver ? winner : -1;
        }

        /// <summary>Même enchaînement que AIPlayer.PlayTurnCoroutine, sans délais.</summary>
        private static void PlayTurn(GameManager gm, BotBrain brain)
        {
            TurnManager tm = gm.TurnManager!;
            int seat = tm.CurrentPlayerIndex;
            int round = gm.RoundNumber;
            bool Mine() => gm.State == GameState.PlayerTurn && gm.RoundNumber == round && tm.CurrentPlayerIndex == seat;

            if (Mine() && tm.CurrentPhase == TurnPhase.Draw)
            {
                int pile = brain.ChooseDrawPile();
                if (pile < 0 || !gm.ExecuteCommand(new DrawFromDiscardCommand(seat, pile)).Success)
                {
                    gm.ExecuteCommand(new DrawFromDeckCommand(seat));
                }
            }

            if (Mine() && tm.CurrentPhase == TurnPhase.LayDown)
            {
                List<Meld>? melds = brain.ChooseLayDown();
                bool laid = false;
                if (melds != null)
                {
                    List<Meld> own = new();
                    foreach (Meld m in melds) own.Add(new Meld(m.Type, new List<CardModel>(m.Cards), seat));
                    laid = gm.ExecuteCommand(new LayDownLevelCommand(seat, own)).Success;
                }
                if (!laid && Mine() && tm.CurrentPhase == TurnPhase.LayDown)
                {
                    gm.ExecuteCommand(new SkipPhaseCommand(seat, TurnPhase.LayDown));
                }
            }

            int guard = 0;
            while (Mine() && tm.CurrentPhase == TurnPhase.AddToMelds && guard++ < 40)
            {
                if (!brain.TryChooseAddToMeld(out CardModel card, out int owner, out int meld)
                    || !gm.ExecuteCommand(new AddToMeldCommand(seat, card, owner, meld)).Success)
                {
                    if (Mine()) gm.ExecuteCommand(new SkipPhaseCommand(seat, TurnPhase.AddToMelds));
                    break;
                }
            }

            if (Mine() && tm.CurrentPhase == TurnPhase.Discard)
            {
                if (!gm.ExecuteCommand(brain.ChooseDiscard()).Success)
                {
                    IGameCommand? fallback = TurnAutoPlay.NextCommand(gm);
                    if (fallback != null) gm.ExecuteCommand(fallback);
                }
            }
        }

        [TestCase(SpecialCardsMode.None)]
        [TestCase(SpecialCardsMode.Jokers)]
        [TestCase(SpecialCardsMode.All)]
        public void BotGames_alwaysFinish(SpecialCardsMode special)
        {
            AIDifficulty[] seats = { AIDifficulty.Hard, AIDifficulty.Normal, AIDifficulty.Easy, AIDifficulty.Hard };
            for (int seed = 1; seed <= 3; seed++)
            {
                MatchRules rules = new(seed % 2 == 0, special, 10, 3);
                int winner = PlayGame(seats, seed * 101, rules, out int rounds);
                Assert.GreaterOrEqual(winner, 0, $"Partie bloquée ({special}, seed {seed}, manche {rounds})");
            }
        }

        private int _lastGameTurns;

        /// <summary>
        /// Mesure (pas une vérification) : manches et tours d'une partie de 4 bots
        /// Normal selon le nombre de niveaux — sert à afficher une durée estimée.
        /// </summary>
        [Test]
        public void GameLength_report()
        {
            AIDifficulty[] seats = { AIDifficulty.Normal, AIDifficulty.Normal, AIDifficulty.Normal, AIDifficulty.Normal };
            foreach (int levels in new[] { 4, 6, 8 })
            {
                int totalRounds = 0, totalTurns = 0, games = 0;
                for (int seed = 1; seed <= 8; seed++)
                {
                    int winner = PlayGame(seats, seed * 313, new MatchRules(levelsToWin: levels), out int rounds);
                    if (winner < 0) continue;
                    games++;
                    totalRounds += rounds;
                    totalTurns += _lastGameTurns;
                }
                Debug.Log($"[GameLength] niveaux={levels} parties={games} " +
                          $"manches={totalRounds / (float)Mathf.Max(1, games):0.0} " +
                          $"tours={totalTurns / (float)Mathf.Max(1, games):0}");
                Assert.Greater(games, 0);
            }
        }

        [Test]
        public void ExpertBots_beatEasyBots()
        {
            // Sièges alternés pour neutraliser l'avantage de jouer en premier.
            AIDifficulty[] seats = { AIDifficulty.Hard, AIDifficulty.Easy, AIDifficulty.Hard, AIDifficulty.Easy };
            int expertWins = 0, games = 0, totalRounds = 0;
            for (int seed = 1; seed <= 12; seed++)
            {
                int winner = PlayGame(seats, seed * 977, new MatchRules(levelsToWin: 3), out int rounds);
                Assert.GreaterOrEqual(winner, 0, $"Partie bloquée (seed {seed})");
                games++;
                totalRounds += rounds;
                if (seats[winner] == AIDifficulty.Hard) expertWins++;
            }

            Debug.Log($"[BotSimulation] Expert {expertWins}/{games} victoires contre Facile " +
                      $"({totalRounds / (float)games:0.0} manches par partie)");
            Assert.GreaterOrEqual(expertWins, 8, "Les bots Expert doivent dominer les bots Facile");
        }
    }
}
