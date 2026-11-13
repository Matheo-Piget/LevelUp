using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.Tests
{
    /// <summary>
    /// Règles de partie (MatchRules) : composition du paquet selon les cartes
    /// spéciales, taille de main, niveau final, départage de fin de partie,
    /// sortie par ajout de la dernière carte, et pseudo du profil.
    /// </summary>
    [TestFixture]
    public class MatchRulesTests
    {
        private readonly List<GameObject> _objects = new();
        private GameConfig _config = null!;

        [SetUp]
        public void SetUp()
        {
            CardFactory.ResetTo(10000);
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
            EventBus.Muted = false;
            EventBus.Clear();
        }

        private GameManager CreateGameManager()
        {
            GameObject go = new("GM_Rules");
            _objects.Add(go);
            GameManager gm = go.AddComponent<GameManager>();
            typeof(GameManager)
                .GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(gm, _config);
            return gm;
        }

        private static readonly PlayerSetup[] ThreePlayers =
        {
            new("A", false),
            new("B", false),
            new("C", false),
        };

        // ──────────────────────────────────────────────
        //  Paquet
        // ──────────────────────────────────────────────

        private static (int normal, int wild, int skip, int draw2) Count(List<CardModel> cards)
        {
            int n = 0, w = 0, s = 0, d = 0;
            foreach (CardModel c in cards)
            {
                switch (c.Type)
                {
                    case CardType.Normal: n++; break;
                    case CardType.Wild: w++; break;
                    case CardType.Skip: s++; break;
                    case CardType.Draw2: d++; break;
                }
            }
            return (n, w, s, d);
        }

        [Test]
        public void ClassicDeck_hasOnlyNumberedCards()
        {
            var (normal, wild, skip, draw2) = Count(DeckManager.BuildComposition(_config, MatchRules.Classic));
            Assert.AreEqual(108, normal);
            Assert.AreEqual(0, wild + skip + draw2, "Aucune carte spéciale sans l'option");
        }

        [Test]
        public void JokersOption_addsJokersOnly()
        {
            MatchRules rules = new(specialCards: SpecialCardsMode.Jokers);
            var (normal, wild, skip, draw2) = Count(DeckManager.BuildComposition(_config, rules));
            Assert.AreEqual(108, normal);
            Assert.AreEqual(MatchRules.JokersInDeck, wild);
            Assert.AreEqual(0, skip + draw2);
        }

        [Test]
        public void AllSpecialCards_addsSkipAndDraw2()
        {
            MatchRules rules = new(specialCards: SpecialCardsMode.All);
            var (normal, wild, skip, draw2) = Count(DeckManager.BuildComposition(_config, rules));
            Assert.AreEqual(108, normal);
            Assert.AreEqual(MatchRules.JokersInDeck, wild);
            Assert.AreEqual(MatchRules.ActionsPerTypeInDeck, skip);
            Assert.AreEqual(MatchRules.ActionsPerTypeInDeck, draw2);
        }

        [Test]
        public void Composition_hasUniqueIds()
        {
            List<CardModel> cards = DeckManager.BuildComposition(_config, new MatchRules(specialCards: SpecialCardsMode.All));
            HashSet<int> ids = new();
            foreach (CardModel c in cards) Assert.IsTrue(ids.Add(c.Id), $"id {c.Id} en double");
        }

        [Test]
        public void Rules_areClamped()
        {
            MatchRules rules = new(handSize: 50, levelsToWin: 99, turnTimerSeconds: -5);
            Assert.AreEqual(MatchRules.MaxHandSize, rules.HandSize);
            Assert.AreEqual(Constants.MaxLevel, rules.LevelsToWin);
            Assert.AreEqual(0, rules.TurnTimerSeconds);
            Assert.IsTrue(MatchRules.Classic.IsClassic);
        }

        // ──────────────────────────────────────────────
        //  Partie
        // ──────────────────────────────────────────────

        [TestCase(8)]
        [TestCase(12)]
        public void StartGame_dealsRuleHandSize(int handSize)
        {
            GameManager gm = CreateGameManager();
            gm.StartGame(ThreePlayers, 42, new MatchRules(handSize: handSize));
            foreach (PlayerModel p in gm.Players) Assert.AreEqual(handSize, p.Hand.Count);
            Assert.AreEqual(handSize, GameSession.Rules.HandSize, "Les règles sont publiées pour l'UI");
        }

        [Test]
        public void GameEnds_afterFinalLevelOfTheRules()
        {
            GameManager gm = CreateGameManager();
            gm.StartGame(ThreePlayers, 7, new MatchRules(levelsToWin: 4));
            int winner = -1;
            EventBus.Subscribe<GameOverEvent>(e => winner = e.WinnerIndex);

            PlayerModel p0 = gm.Players[0];
            p0.CurrentLevel = 4;
            p0.HasLaidDownThisRound = true;
            gm.OnRoundEnd(0);

            Assert.AreEqual(GameState.GameOver, gm.State);
            Assert.AreEqual(0, winner);
        }

        [Test]
        public void GameContinues_belowFinalLevel()
        {
            GameManager gm = CreateGameManager();
            gm.StartGame(ThreePlayers, 7, new MatchRules(levelsToWin: 8));

            PlayerModel p0 = gm.Players[0];
            p0.CurrentLevel = 4;
            p0.HasLaidDownThisRound = true;
            gm.OnRoundEnd(0);

            Assert.AreNotEqual(GameState.GameOver, gm.State);
            Assert.AreEqual(2, gm.RoundNumber);
        }

        [Test]
        public void GameOverTie_goesToRoundWinner()
        {
            GameManager gm = CreateGameManager();
            gm.StartGame(ThreePlayers, 7, new MatchRules(levelsToWin: 4));
            int winner = -1;
            EventBus.Subscribe<GameOverEvent>(e => winner = e.WinnerIndex);

            // Joueur 0 : niveau 4 posé (+1 → 5). Joueur 2 gagne la manche niveau 3 posé (+2 → 5).
            gm.Players[0].CurrentLevel = 4;
            gm.Players[0].HasLaidDownThisRound = true;
            gm.Players[2].CurrentLevel = 3;
            gm.Players[2].HasLaidDownThisRound = true;
            gm.OnRoundEnd(2);

            Assert.AreEqual(GameState.GameOver, gm.State);
            Assert.AreEqual(2, winner, "À égalité, le gagnant de la manche l'emporte");
        }

        [Test]
        public void Rebuild_withSpecialRules_reachesSameState()
        {
            MatchRules rules = new(true, SpecialCardsMode.All, 12, 6, 30);
            GameManager original = CreateGameManager();
            original.StartGame(ThreePlayers, 2024, rules);

            List<IGameCommand> history = new();
            for (int t = 0; t < 6; t++)
            {
                int p = original.TurnManager!.CurrentPlayerIndex;
                IGameCommand draw = new DrawFromDeckCommand(p);
                Assert.IsTrue(original.ExecuteCommand(draw).Success);
                history.Add(draw);
                IGameCommand skip = new SkipPhaseCommand(p, TurnPhase.LayDown);
                Assert.IsTrue(original.ExecuteCommand(skip).Success);
                history.Add(skip);

                // Défausse une carte numérotée (pas d'effet de carte action ici).
                CardModel card = original.Players[p].Hand[0];
                foreach (CardModel c in original.Players[p].Hand)
                {
                    if (c.Type == CardType.Normal) { card = c; break; }
                }
                IGameCommand discard = new DiscardCommand(p, card, -1);
                Assert.IsTrue(original.ExecuteCommand(discard).Success);
                history.Add(discard);
            }

            GameManager mirror = CreateGameManager();
            Assert.IsTrue(mirror.RebuildFromHistory(ThreePlayers, 2024, rules, history));
            Assert.AreEqual(original.ComputeStateHash(), mirror.ComputeStateHash());
        }

        // ──────────────────────────────────────────────
        //  Cartes action et sortie par ajout
        // ──────────────────────────────────────────────

        [Test]
        public void Draw2_publishesHandChangedForTarget()
        {
            List<PlayerModel> players = new()
            {
                new PlayerModel(0, "A", false),
                new PlayerModel(1, "B", false),
            };
            DeckManager deck = new(_config, 3);
            deck.CreateAndShuffle(players.Count);
            ActionCardHandler handler = new(deck, players);

            int changedFor = -1;
            int newCount = -1;
            EventBus.Subscribe<HandChangedEvent>(e => { changedFor = e.PlayerIndex; newCount = e.NewHand.Count; });

            handler.HandleActionCard(0, CardFactory.Draw2(), 1);

            Assert.AreEqual(1, changedFor, "La main visée doit être rafraîchie");
            Assert.AreEqual(2, newCount);
            Assert.AreEqual(2, players[1].Hand.Count);
        }

        [Test]
        public void AddingLastCardToMeld_endsTheRound()
        {
            List<PlayerModel> players = new()
            {
                new PlayerModel(0, "A", false),
                new PlayerModel(1, "B", false),
            };
            DeckManager deck = new(_config, 3);
            deck.CreateAndShuffle(players.Count);
            ActionCardHandler handler = new(deck, players);
            TurnManager turn = new(players, handler);
            GameCommandExecutor executor = new(players, deck, turn, handler, _config);
            turn.StartRound(0);

            // Joueur 0 a posé une suite 4-5-6 et n'a plus qu'un 7 en main.
            PlayerModel p0 = players[0];
            p0.HasLaidDownThisRound = true;
            p0.AddMeld(new Meld(MeldType.Run, new List<CardModel> { CardFactory.R(4), CardFactory.B(5), CardFactory.G(6) }, 0));
            CardModel seven = CardFactory.Y(7);
            p0.AddToHand(seven);
            turn.AdvanceToPhase(TurnPhase.AddToMelds);

            CommandResult result = executor.Execute(new AddToMeldCommand(0, seven, 0, 0));

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.RoundEnded, "Main vide après un ajout : le joueur sort");
        }

        // ──────────────────────────────────────────────
        //  Pseudo
        // ──────────────────────────────────────────────

        [Test]
        public void Sanitize_removesProtocolAndRichTextCharacters()
        {
            Assert.AreEqual("Alexb", PlayerProfile.Sanitize("  Al|ex<b>  "));
            Assert.AreEqual("Jean Paul", PlayerProfile.Sanitize("Jean    Paul"));
            Assert.AreEqual("", PlayerProfile.Sanitize("   "));
            Assert.AreEqual("", PlayerProfile.Sanitize(null));
            Assert.AreEqual(PlayerProfile.MaxNameLength,
                PlayerProfile.Sanitize("UnPseudoBeaucoupTropLongPourLaTable").Length);
            Assert.AreEqual("Léa", PlayerProfile.Sanitize("Léa"), "Les accents sont gardés");
        }

        [Test]
        public void Initial_usesBotDigitOrFirstLetter()
        {
            Assert.AreEqual("3", PlayerProfile.Initial("Bot 3"));
            Assert.AreEqual("A", PlayerProfile.Initial("alex"));
            Assert.AreEqual("J", PlayerProfile.Initial("Joueur 2"));
            Assert.AreEqual("?", PlayerProfile.Initial(""));
        }

        // ──────────────────────────────────────────────
        //  Fin de tour automatique (minuteur)
        // ──────────────────────────────────────────────

        [Test]
        public void AutoPlay_neverDiscardsAJokerWhenAnotherCardExists()
        {
            List<CardModel> hand = new()
            {
                CardFactory.Wild(), CardFactory.R(5), CardFactory.R(6), CardFactory.B(17),
            };
            CardModel discard = TurnAutoPlay.ChooseDiscard(hand);
            Assert.IsFalse(discard.IsWild);
            Assert.AreEqual(17, discard.Value, "La carte isolée part en premier");
        }

        [Test]
        public void AutoPlay_followsThePhases()
        {
            GameManager gm = CreateGameManager();
            gm.StartGame(ThreePlayers, 11, MatchRules.Classic);
            int p = gm.TurnManager!.CurrentPlayerIndex;

            Assert.IsInstanceOf<DrawFromDeckCommand>(TurnAutoPlay.NextCommand(gm));
            Assert.IsTrue(gm.ExecuteCommand(TurnAutoPlay.NextCommand(gm)!).Success);
            Assert.IsInstanceOf<SkipPhaseCommand>(TurnAutoPlay.NextCommand(gm));
            Assert.IsTrue(gm.ExecuteCommand(TurnAutoPlay.NextCommand(gm)!).Success);
            Assert.IsInstanceOf<DiscardCommand>(TurnAutoPlay.NextCommand(gm));
            Assert.IsTrue(gm.ExecuteCommand(TurnAutoPlay.NextCommand(gm)!).Success);
            Assert.AreNotEqual(p, gm.TurnManager.CurrentPlayerIndex, "Le tour est passé au joueur suivant");
        }
    }
}
