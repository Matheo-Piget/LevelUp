using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.Tests
{
    /// <summary>
    /// Vérifie la garde anti-triche d'<see cref="GameCommandExecutor.Execute(IGameCommand)"/>
    /// pour les LayDownLevelCommand : un client malicieux ne peut pas faire poser
    /// des melds invalides même si sa main entière serait validable.
    /// </summary>
    [TestFixture]
    public class GameCommandExecutorTests
    {
        private GameConfig _config = null!;
        private List<PlayerModel> _players = null!;
        private DeckManager _deck = null!;
        private TurnManager _turn = null!;
        private ActionCardHandler _action = null!;
        private GameCommandExecutor _executor = null!;

        [SetUp]
        public void SetUp()
        {
            CardFactory.Reset();
            EventBus.Clear();

            _config = ScriptableObject.CreateInstance<GameConfig>();
            _players = new List<PlayerModel>
            {
                new(0, "P0", false),
                new(1, "P1", false),
            };
            _deck = new DeckManager(_config);
            _deck.CreateAndShuffle(_players.Count);
            _action = new ActionCardHandler(_deck, _players);
            _turn = new TurnManager(_players, _action);
            _turn.StartRound(0);
            _executor = new GameCommandExecutor(_players, _deck, _turn, _action, _config);
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null) Object.DestroyImmediate(_config);
            EventBus.Clear();
        }

        /// <summary>Force la phase à LayDown sans passer par Draw (raccourci de test).</summary>
        private void ForceLayDownPhase()
        {
            // Simuler une pioche pour passer Draw → LayDown.
            _executor.Execute(new DrawFromDeckCommand(0));
            // Maintenant on est en LayDown (ou AddToMelds si déjà posé, mais ici non).
            Assert.AreEqual(TurnPhase.LayDown, _turn.CurrentPhase);
        }

        [Test]
        public void LayDown_validMeldsForLevel1_succeeds()
        {
            // Donner une main niveau 1 (deux runs de 3).
            CardModel[] hand =
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(5),
                CardFactory.Y(8), CardFactory.P(9), CardFactory.O(10),
            };
            foreach (CardModel c in hand) _players[0].AddToHand(c);

            ForceLayDownPhase();

            List<Meld> melds = new()
            {
                new Meld(MeldType.Run, new List<CardModel> { hand[0], hand[1], hand[2] }, 0),
                new Meld(MeldType.Run, new List<CardModel> { hand[3], hand[4], hand[5] }, 0),
            };

            CommandResult result = _executor.Execute(new LayDownLevelCommand(0, melds));
            Assert.IsTrue(result.Success, "Le LayDown valide doit passer : " + result.Message);
            Assert.IsTrue(_players[0].HasLaidDownThisRound);
        }

        [Test]
        public void LayDown_meldNotMatchingLevel_fails()
        {
            // Niveau 1 attend 2 runs, le client envoie 2 sets — doit être rejeté.
            CardModel[] hand =
            {
                CardFactory.R(3), CardFactory.B(3), CardFactory.G(3),
                CardFactory.Y(8), CardFactory.P(8), CardFactory.O(8),
            };
            foreach (CardModel c in hand) _players[0].AddToHand(c);

            ForceLayDownPhase();

            List<Meld> melds = new()
            {
                new Meld(MeldType.Set, new List<CardModel> { hand[0], hand[1], hand[2] }, 0),
                new Meld(MeldType.Set, new List<CardModel> { hand[3], hand[4], hand[5] }, 0),
            };

            CommandResult result = _executor.Execute(new LayDownLevelCommand(0, melds));
            Assert.IsFalse(result.Success,
                "2 sets ne doivent pas valider niveau 1 (2 runs requis)");
            Assert.IsFalse(_players[0].HasLaidDownThisRound);
        }

        [Test]
        public void LayDown_structurallyInvalidMeld_fails()
        {
            // Main qui pourrait techniquement valider niveau 1 ailleurs… mais on
            // envoie un Run cassé (3, 4, 9) — le validateur du Run doit rejeter.
            CardModel[] hand =
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(9),
                CardFactory.Y(8), CardFactory.P(9), CardFactory.O(10),
            };
            foreach (CardModel c in hand) _players[0].AddToHand(c);

            ForceLayDownPhase();

            List<Meld> melds = new()
            {
                new Meld(MeldType.Run, new List<CardModel> { hand[0], hand[1], hand[2] }, 0),
                new Meld(MeldType.Run, new List<CardModel> { hand[3], hand[4], hand[5] }, 0),
            };

            CommandResult result = _executor.Execute(new LayDownLevelCommand(0, melds));
            Assert.IsFalse(result.Success, "Le run [3,4,9] doit être rejeté structurellement");
            Assert.IsFalse(_players[0].HasLaidDownThisRound);
        }

        [Test]
        public void LayDown_cardNotInHand_fails()
        {
            // Le client envoie une carte qu'il n'a pas en main.
            CardModel[] hand =
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(5),
                CardFactory.Y(8), CardFactory.P(9), CardFactory.O(10),
            };
            foreach (CardModel c in hand) _players[0].AddToHand(c);

            ForceLayDownPhase();

            CardModel intruder = CardFactory.R(99);   // pas dans la main
            List<Meld> melds = new()
            {
                new Meld(MeldType.Run, new List<CardModel> { hand[0], hand[1], hand[2] }, 0),
                new Meld(MeldType.Run, new List<CardModel> { hand[3], hand[4], intruder }, 0),
            };

            CommandResult result = _executor.Execute(new LayDownLevelCommand(0, melds));
            Assert.IsFalse(result.Success, "Carte étrangère doit être rejetée");
        }

        [Test]
        public void LayDown_cardUsedTwice_fails()
        {
            // Le client tente d'utiliser deux fois la même carte dans deux melds.
            CardModel[] hand =
            {
                CardFactory.R(3), CardFactory.B(4), CardFactory.G(5),
                CardFactory.Y(3), CardFactory.P(4),
            };
            foreach (CardModel c in hand) _players[0].AddToHand(c);

            ForceLayDownPhase();

            // Le run [3,4,5] et un autre run [3,4,5] qui réutilise la même G(5).
            List<Meld> melds = new()
            {
                new Meld(MeldType.Run, new List<CardModel> { hand[0], hand[1], hand[2] }, 0),
                new Meld(MeldType.Run, new List<CardModel> { hand[3], hand[4], hand[2] }, 0),
            };

            CommandResult result = _executor.Execute(new LayDownLevelCommand(0, melds));
            Assert.IsFalse(result.Success, "Réutiliser la même carte doit être rejeté");
        }

        [Test]
        public void LayDown_emptyMelds_fails()
        {
            ForceLayDownPhase();
            CommandResult result = _executor.Execute(
                new LayDownLevelCommand(0, new List<Meld>()));
            Assert.IsFalse(result.Success);
        }

        // ──────────────────────────────────────────────
        //  Deck épuisé — fin de round forcée
        // ──────────────────────────────────────────────

        [Test]
        public void DrawFromDeck_deckFullyEmpty_forcesRoundEndWithSmallestHand()
        {
            // Vider la pioche : on tire jusqu'à null.
            int safety = 200;
            while (safety-- > 0 && _deck.DrawFromPile().HasValue) { /* drain */ }

            // Donner aux joueurs des mains de tailles différentes.
            // P0 a 5 cartes, P1 en a 2 → P1 doit gagner.
            for (int i = 0; i < 5; i++) _players[0].AddToHand(CardFactory.R(i + 1));
            for (int i = 0; i < 2; i++) _players[1].AddToHand(CardFactory.B(i + 1));

            // Tour de P0, phase Draw (StartRound l'a fait au SetUp).
            // P0 tente de piocher : la pile est vide, aucune défausse à recycler.
            CommandResult result = _executor.Execute(new DrawFromDeckCommand(0));

            Assert.IsTrue(result.Success, "Doit retourner success (pas Failure)");
            Assert.IsTrue(result.RoundEnded, "Le round doit se terminer");
            Assert.AreEqual(1, result.WinnerOverride,
                "Le gagnant override doit être P1 (main la plus courte)");
        }

        [Test]
        public void DrawFromDeck_deckEmpty_tieGoesToLowestPenaltyNotSeatZero()
        {
            int safety = 200;
            while (safety-- > 0 && _deck.DrawFromPile().HasValue) { /* drain */ }

            // Même nombre de cartes : P0 a des cartes hautes (10 pts), P1 des basses (5 pts).
            _players[0].AddToHand(CardFactory.R(12));
            _players[0].AddToHand(CardFactory.R(13));
            _players[1].AddToHand(CardFactory.B(2));
            _players[1].AddToHand(CardFactory.B(3));

            CommandResult result = _executor.Execute(new DrawFromDeckCommand(0));
            Assert.AreEqual(1, result.WinnerOverride, "À égalité de cartes, le moins de points l'emporte");
        }

        // ──────────────────────────────────────────────
        //  Cartes action : ni ramassées, ni posées
        // ──────────────────────────────────────────────

        [Test]
        public void DrawFromDiscard_actionCardOnTop_isRefused()
        {
            _deck.Discard(1, CardFactory.Skip());
            CommandResult result = _executor.Execute(new DrawFromDiscardCommand(0, 1));
            Assert.IsFalse(result.Success);
            Assert.AreEqual(CommandError.ActionCardNotPickable, result.Error);
            Assert.AreEqual(1, _deck.GetDiscardCount(1), "La carte reste sur la défausse");
            Assert.AreEqual(TurnPhase.Draw, _turn.CurrentPhase);
        }

        [Test]
        public void DrawFromDiscard_jokerOnTop_isAllowed()
        {
            _deck.Discard(1, CardFactory.Wild());
            Assert.IsTrue(_executor.Execute(new DrawFromDiscardCommand(0, 1)).Success);
        }

        [Test]
        public void ActionCards_neverFitInAMeld()
        {
            CardModel skip = CardFactory.Skip();
            CardModel draw2 = CardFactory.Draw2();
            List<CardModel> run = new() { CardFactory.R(5), CardFactory.B(6), skip };
            Assert.IsFalse(run.IsValidRun(), "Une carte Passe ne complète pas une suite");
            Assert.IsFalse(new List<CardModel> { CardFactory.R(7), CardFactory.B(7), draw2 }.IsValidSet());
            Assert.IsFalse(new List<CardModel> { CardFactory.R(7), CardFactory.R(9), skip }.IsValidFlush());
            Assert.IsFalse(new List<CardModel> { skip, draw2 }.IsValidSet(), "Deux cartes action ne font pas une paire");

            Meld meld = new(MeldType.Run, new List<CardModel> { CardFactory.G(5), CardFactory.G(6), CardFactory.G(7) }, 0);
            Assert.IsFalse(meld.CanAddCard(skip));
            Assert.IsFalse(meld.TryAddCard(draw2));
            Assert.IsTrue(meld.CanAddCard(CardFactory.Wild()), "Un joker, lui, complète");
            Assert.IsTrue(meld.CanAddCard(CardFactory.Y(8)));
            Assert.IsFalse(meld.CanAddCard(CardFactory.Y(6)), "Doublon de valeur refusé");
        }

        [Test]
        public void Failure_carriesAnErrorCode()
        {
            CommandResult wrongTurn = _executor.Execute(new DrawFromDeckCommand(1));
            Assert.AreEqual(CommandError.NotYourTurn, wrongTurn.Error);
            CommandResult wrongPhase = _executor.Execute(new DiscardCommand(0, CardFactory.R(1)));
            Assert.AreEqual(CommandError.WrongPhase, wrongPhase.Error);
        }
    }
}
