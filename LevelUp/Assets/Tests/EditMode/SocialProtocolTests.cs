using NUnit.Framework;
using LevelUp.Core;

namespace LevelUp.Tests
{
    /// <summary>Protocole social en ligne : réactions et vote de revanche (sans réseau).</summary>
    [TestFixture]
    public class SocialProtocolTests
    {
        [Test]
        public void Emote_roundTrips()
        {
            byte[] bytes = SocialProtocol.EncodeEmote(2, 3);
            Assert.IsTrue(SocialProtocol.TryDecodeEmote(bytes, 4, out int seat, out int id));
            Assert.AreEqual(2, seat);
            Assert.AreEqual(3, id);
        }

        [Test]
        public void Emote_rejectsBadBytes()
        {
            Assert.IsFalse(SocialProtocol.TryDecodeEmote(null, 4, out _, out _));
            Assert.IsFalse(SocialProtocol.TryDecodeEmote(new byte[] { 1 }, 4, out _, out _), "trop court");
            Assert.IsFalse(SocialProtocol.TryDecodeEmote(new byte[] { 1, 2, 3 }, 4, out _, out _), "trop long");
            Assert.IsFalse(SocialProtocol.TryDecodeEmote(new byte[] { 5, 0 }, 4, out _, out _), "siège hors table");
            Assert.IsFalse(SocialProtocol.TryDecodeEmote(new byte[] { 0, 200 }, 4, out _, out _), "réaction inconnue");
        }

        [Test]
        public void EmoteLimiter_blocksSpamPerSeat()
        {
            EmoteLimiter limiter = new();
            Assert.IsTrue(limiter.TryAccept(1, 10f));
            Assert.IsFalse(limiter.TryAccept(1, 11f), "même joueur, trop tôt");
            Assert.IsTrue(limiter.TryAccept(2, 11f), "un autre joueur n'est pas bloqué");
            Assert.IsTrue(limiter.TryAccept(1, 10f + SocialProtocol.EmoteCooldownSeconds));
        }

        [Test]
        public void Rematch_roundTrips_andRejectsVotesFromAbsentSeats()
        {
            Assert.IsTrue(SocialProtocol.TryDecodeRematch(SocialProtocol.EncodeRematch(0b001, 0b101), out int v, out int e));
            Assert.AreEqual(0b001, v);
            Assert.AreEqual(0b101, e);
            Assert.IsFalse(SocialProtocol.TryDecodeRematch(new byte[] { 0b010, 0b101 }, out _, out _));
            Assert.IsFalse(SocialProtocol.TryDecodeRematch(new byte[] { 1 }, out _, out _));
        }

        [Test]
        public void RematchVote_agreesWhenEveryPresentHumanVoted()
        {
            RematchVote vote = new();
            vote.SetExpected(0b101); // sièges 0 et 2 humains, siège 1 = bot
            Assert.IsFalse(vote.Vote(1), "un bot ne vote pas");
            Assert.IsTrue(vote.Vote(0));
            Assert.IsFalse(vote.Vote(0), "pas de double vote");
            Assert.IsFalse(vote.IsAgreed);
            Assert.IsTrue(vote.Vote(2));
            Assert.IsTrue(vote.IsAgreed);
        }

        [Test]
        public void RematchVote_aLeaverNoLongerBlocks_butOneAloneIsNotAnAgreement()
        {
            RematchVote vote = new();
            vote.SetExpected(0b111);
            vote.Vote(0);
            vote.Vote(1);
            Assert.IsFalse(vote.IsAgreed);
            vote.SetExpected(0b011); // le siège 2 est parti
            Assert.IsTrue(vote.IsAgreed);

            RematchVote alone = new();
            alone.SetExpected(0b001);
            alone.Vote(0);
            Assert.IsFalse(alone.IsAgreed, "seul, ce n'est pas un vote");
        }
    }
}
