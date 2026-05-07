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
    }
}
