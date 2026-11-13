using System.Collections.Generic;

namespace LevelUp.Core
{
    /// <summary>
    /// Petits messages « sociaux » de la partie en ligne — réactions (emotes) et
    /// vote de revanche. Pur C#, sans dépendance réseau : encodage, décodage
    /// borné et règles (anti-spam, décompte des votes) sont testés tels quels ;
    /// la couche réseau ne fait que transporter ces octets.
    /// </summary>
    public static class SocialProtocol
    {
        /// <summary>Réactions prédéfinies (aucun texte libre : rien à modérer).</summary>
        public static readonly string[] Emotes =
        {
            "Bien joué !", "Merci", "Oups…", "Dépêche-toi !", "Aïe", "Bravo"
        };

        public const int MaxSeats = 8;

        /// <summary>Délai minimum entre deux réactions d'un même joueur.</summary>
        public const float EmoteCooldownSeconds = 3f;

        public static bool IsValidEmote(int id) => id >= 0 && id < Emotes.Length;

        // ─── Emote : [siège, identifiant] ────────────────────────────────

        public static byte[] EncodeEmote(int seat, int emoteId) => new[] { (byte)seat, (byte)emoteId };

        public static bool TryDecodeEmote(byte[]? bytes, int playerCount, out int seat, out int emoteId)
        {
            seat = -1;
            emoteId = -1;
            if (bytes == null || bytes.Length != 2) return false;
            seat = bytes[0];
            emoteId = bytes[1];
            return seat < playerCount && seat < MaxSeats && IsValidEmote(emoteId);
        }

        // ─── Revanche : [masque des sièges qui ont voté, masque des sièges attendus] ───

        public static byte[] EncodeRematch(int votesMask, int expectedMask) =>
            new[] { (byte)votesMask, (byte)expectedMask };

        public static bool TryDecodeRematch(byte[]? bytes, out int votesMask, out int expectedMask)
        {
            votesMask = 0;
            expectedMask = 0;
            if (bytes == null || bytes.Length != 2) return false;
            votesMask = bytes[0];
            expectedMask = bytes[1];
            // Un vote ne peut venir que d'un siège attendu.
            return (votesMask & ~expectedMask) == 0;
        }

        public static int CountBits(int mask)
        {
            int n = 0;
            for (int i = 0; i < MaxSeats; i++)
            {
                if ((mask & (1 << i)) != 0) n++;
            }
            return n;
        }
    }

    /// <summary>Anti-spam des réactions : une par joueur toutes les <see cref="SocialProtocol.EmoteCooldownSeconds"/> s.</summary>
    public sealed class EmoteLimiter
    {
        private readonly Dictionary<int, float> _last = new();

        public bool TryAccept(int seat, float now)
        {
            if (_last.TryGetValue(seat, out float last) && now - last < SocialProtocol.EmoteCooldownSeconds)
                return false;
            _last[seat] = now;
            return true;
        }

        public void Clear() => _last.Clear();
    }

    /// <summary>
    /// Vote de revanche (tenu par l'hôte) : chaque humain connecté peut voter ;
    /// quand tous ont voté, la revanche part. Un joueur qui quitte ne bloque plus.
    /// </summary>
    public sealed class RematchVote
    {
        public int VotesMask { get; private set; }
        public int ExpectedMask { get; private set; }

        /// <summary>Sièges humains présents (les autres bits sont effacés des votes).</summary>
        public void SetExpected(int expectedMask)
        {
            ExpectedMask = expectedMask;
            VotesMask &= expectedMask;
        }

        /// <summary>Enregistre un vote ; faux s'il vient d'un siège non attendu ou déjà compté.</summary>
        public bool Vote(int seat)
        {
            int bit = 1 << seat;
            if (seat < 0 || seat >= SocialProtocol.MaxSeats || (ExpectedMask & bit) == 0 || (VotesMask & bit) != 0)
                return false;
            VotesMask |= bit;
            return true;
        }

        /// <summary>Vrai quand tous les humains présents ont voté (au moins deux).</summary>
        public bool IsAgreed => ExpectedMask != 0 && VotesMask == ExpectedMask
                                && SocialProtocol.CountBits(ExpectedMask) >= 2;

        public void Reset()
        {
            VotesMask = 0;
        }
    }
}
