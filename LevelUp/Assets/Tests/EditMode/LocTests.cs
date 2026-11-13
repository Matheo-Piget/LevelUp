using System.Collections.Generic;
using NUnit.Framework;
using LevelUp.Utils;

namespace LevelUp.Tests
{
    /// <summary>Traduction : textes entiers, textes composés, mots entiers seulement.</summary>
    [TestFixture]
    public class LocTests
    {
        private int _saved;

        [SetUp]
        public void SetUp()
        {
            _saved = Loc.Language;
            Loc.Language = Loc.English;
        }

        [TearDown]
        public void TearDown() => Loc.Language = _saved;

        [Test]
        public void French_isReturnedUntouched()
        {
            Loc.Language = Loc.French;
            Assert.AreEqual("Margot pose le niveau 3 !", Loc.T("Margot pose le niveau 3 !"));
        }

        [Test]
        public void ExactLabels_areTranslated()
        {
            Assert.AreEqual("PLAY", Loc.T("JOUER"));
            Assert.AreEqual("Color", Loc.T("Couleur"), "bouton de tri : couleur, pas « flush »");
            Assert.AreEqual("Discard", Loc.T("Défausser"));
        }

        [Test]
        public void ComposedTexts_areTranslatedPieceByPiece()
        {
            Assert.AreEqual("Margot lays down level 3!", Loc.T("Margot pose le niveau 3 !"));
            Assert.AreEqual("2 runs of 3", Loc.T("2 suites de 3"));
            Assert.AreEqual("1 flush of 5 + 1 run of 3", Loc.T("1 couleur de 5 + 1 suite de 3"));
            Assert.AreEqual("Round 4", Loc.T("Manche 4"));
            Assert.AreEqual("Léon · <color=#A7A198>Flush</color>", Loc.T("Léon · <color=#A7A198>Couleur</color>"));
            Assert.AreEqual("Hint: discard the 7 red", Loc.T("Conseil : défausse le 7 rouge"));
        }

        [Test]
        public void Fragments_onlyMatchWholeWords()
        {
            // « carte » ne doit pas transformer « cartes » en « cards » + « s », ni toucher un pseudo.
            Assert.AreEqual("3 cards", Loc.T("3 cartes"));
            Assert.AreEqual("Cartesien", Loc.T("Cartesien"));
        }

        [Test]
        public void UnknownText_staysAsIs()
        {
            Assert.AreEqual("K7Q2PX", Loc.T("K7Q2PX"));
            Assert.AreEqual("", Loc.T(""));
        }

        [Test]
        public void Table_hasNoEmptyEntries()
        {
            foreach (KeyValuePair<string, string> e in LocTable.Exact)
            {
                Assert.IsNotEmpty(e.Key);
                Assert.IsNotEmpty(e.Value, e.Key);
            }
            foreach (KeyValuePair<string, string> e in LocTable.Fragments)
            {
                Assert.IsNotEmpty(e.Key);
                Assert.IsNotEmpty(e.Value, e.Key);
            }
        }
    }
}
