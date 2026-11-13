using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>Cartes spéciales mélangées au paquet (désactivées par défaut).</summary>
    public enum SpecialCardsMode : byte
    {
        /// <summary>Paquet classique : uniquement les 108 cartes numérotées.</summary>
        None = 0,

        /// <summary>+ jokers (remplacent n'importe quelle carte dans une combinaison).</summary>
        Jokers = 1,

        /// <summary>+ jokers, cartes « Passe » et « +2 » à jouer contre un adversaire.</summary>
        All = 2
    }

    /// <summary>
    /// Règles d'une partie, fixées AVANT le lancement : par le joueur en solo,
    /// par l'hôte en ligne (envoyées dans le message de départ, donc identiques
    /// sur tous les appareils — condition de la simulation miroir).
    /// Immuable : une partie ne change jamais de règles en cours de route.
    /// </summary>
    public sealed class MatchRules
    {
        public const int DefaultHandSize = 10;
        public const int MinHandSize = 6;
        public const int MaxHandSize = 12;
        public const int MinLevelsToWin = 2;
        public const int MaxTurnTimerSeconds = 300;

        /// <summary>Jokers ajoutés au paquet dès que les cartes spéciales sont activées.</summary>
        public const int JokersInDeck = 8;

        /// <summary>Cartes « Passe » et « +2 » ajoutées en mode <see cref="SpecialCardsMode.All"/>.</summary>
        public const int ActionsPerTypeInDeck = 4;

        /// <summary>Règles d'origine du jeu (contrats fixes, paquet classique, 10 cartes, 8 niveaux).</summary>
        public static readonly MatchRules Classic = new();

        /// <summary>Chaque niveau tire un contrat parmi des variantes de difficulté équivalente.</summary>
        public bool RandomContracts { get; }

        /// <summary>Cartes spéciales mélangées au paquet.</summary>
        public SpecialCardsMode SpecialCards { get; }

        /// <summary>Nombre de cartes distribuées à chaque joueur en début de manche.</summary>
        public int HandSize { get; }

        /// <summary>Niveau final : la partie est gagnée en terminant ce niveau.</summary>
        public int LevelsToWin { get; }

        /// <summary>Temps maximum par tour en secondes (0 = sans limite).</summary>
        public int TurnTimerSeconds { get; }

        // ─── Variantes (toutes à leur valeur classique par défaut) ───────

        /// <summary>Niveaux gagnés par le joueur qui sort après avoir posé (classique : 2 ; variante : 1).</summary>
        public int GoOutBonus { get; }

        /// <summary>
        /// Vrai : le bonus de sortie ne fait pas sauter le dernier niveau — son
        /// contrat doit être posé pour gagner (classique : faux).
        /// </summary>
        public bool FinalLevelRequired { get; }

        /// <summary>
        /// Nombre maximum de tours par joueur dans une manche (0 = sans limite,
        /// classique). À la limite, la manche s'arrête : la main la plus légère l'emporte.
        /// </summary>
        public int RoundTurnLimit { get; }

        /// <summary>
        /// Coup de pouce : un joueur resté deux manches de suite au même niveau
        /// reçoit une carte de plus à la donne (classique : faux).
        /// </summary>
        public bool CatchUp { get; }

        public const int MaxRoundTurnLimit = 60;

        /// <summary>Manches sans monter de niveau avant le coup de pouce.</summary>
        public const int CatchUpAfterRounds = 2;

        public MatchRules(bool randomContracts = false, SpecialCardsMode specialCards = SpecialCardsMode.None,
            int handSize = DefaultHandSize, int levelsToWin = Constants.MaxLevel, int turnTimerSeconds = 0,
            int goOutBonus = 2, bool finalLevelRequired = false, int roundTurnLimit = 0, bool catchUp = false)
        {
            RandomContracts = randomContracts;
            SpecialCards = specialCards > SpecialCardsMode.All ? SpecialCardsMode.None : specialCards;
            HandSize = Clamp(handSize, MinHandSize, MaxHandSize);
            LevelsToWin = Clamp(levelsToWin, MinLevelsToWin, Constants.MaxLevel);
            TurnTimerSeconds = Clamp(turnTimerSeconds, 0, MaxTurnTimerSeconds);
            GoOutBonus = Clamp(goOutBonus, 1, 2);
            FinalLevelRequired = finalLevelRequired;
            RoundTurnLimit = Clamp(roundTurnLimit, 0, MaxRoundTurnLimit);
            CatchUp = catchUp;
        }

        /// <summary>Écrit les règles (sauvegarde, réseau) — même ordre que <see cref="Read"/>.</summary>
        public void Write(System.IO.BinaryWriter w)
        {
            w.Write(RandomContracts);
            w.Write((byte)SpecialCards);
            w.Write(HandSize);
            w.Write(LevelsToWin);
            w.Write(TurnTimerSeconds);
            w.Write((byte)GoOutBonus);
            w.Write(FinalLevelRequired);
            w.Write((byte)RoundTurnLimit);
            w.Write(CatchUp);
        }

        /// <summary>Relit des règles ; chaque valeur est bornée par le constructeur.</summary>
        public static MatchRules Read(System.IO.BinaryReader r)
        {
            bool random = r.ReadBoolean();
            byte special = r.ReadByte();
            int hand = r.ReadInt32();
            int levels = r.ReadInt32();
            int timer = r.ReadInt32();
            int bonus = r.ReadByte();
            bool final = r.ReadBoolean();
            int limit = r.ReadByte();
            bool catchUp = r.ReadBoolean();
            return new MatchRules(random, (SpecialCardsMode)special, hand, levels, timer, bonus, final, limit, catchUp);
        }

        /// <summary>Règles sérialisées (réseau : envoyées comme un bloc d'octets).</summary>
        public byte[] ToBytes()
        {
            using System.IO.MemoryStream stream = new(24);
            using (System.IO.BinaryWriter w = new(stream)) Write(w);
            return stream.ToArray();
        }

        /// <summary>Relit un bloc d'octets ; null s'il est tronqué ou trop long.</summary>
        public static MatchRules? FromBytes(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > 64) return null;
            try
            {
                using System.IO.MemoryStream stream = new(bytes, false);
                using System.IO.BinaryReader r = new(stream);
                MatchRules rules = Read(r);
                return stream.Position == stream.Length ? rules : null;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        /// <summary>Nombre de jokers dans le paquet.</summary>
        public int JokerCount => SpecialCards == SpecialCardsMode.None ? 0 : JokersInDeck;

        /// <summary>Nombre de cartes « Passe » dans le paquet.</summary>
        public int SkipCount => SpecialCards == SpecialCardsMode.All ? ActionsPerTypeInDeck : 0;

        /// <summary>Nombre de cartes « +2 » dans le paquet.</summary>
        public int Draw2Count => SpecialCards == SpecialCardsMode.All ? ActionsPerTypeInDeck : 0;

        /// <summary>Vrai si un minuteur limite la durée des tours.</summary>
        public bool HasTurnTimer => TurnTimerSeconds > 0;

        /// <summary>Vrai si ces règles sont exactement celles du jeu d'origine.</summary>
        public bool IsClassic =>
            !RandomContracts && SpecialCards == SpecialCardsMode.None && HandSize == DefaultHandSize
            && LevelsToWin == Constants.MaxLevel && TurnTimerSeconds == 0
            && GoOutBonus == 2 && !FinalLevelRequired && RoundTurnLimit == 0 && !CatchUp;

        private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;

        public override bool Equals(object? obj)
        {
            return obj is MatchRules o
                   && o.RandomContracts == RandomContracts
                   && o.SpecialCards == SpecialCards
                   && o.HandSize == HandSize
                   && o.LevelsToWin == LevelsToWin
                   && o.TurnTimerSeconds == TurnTimerSeconds
                   && o.GoOutBonus == GoOutBonus
                   && o.FinalLevelRequired == FinalLevelRequired
                   && o.RoundTurnLimit == RoundTurnLimit
                   && o.CatchUp == CatchUp;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int h = RandomContracts ? 1 : 0;
                h = h * 31 + (int)SpecialCards;
                h = h * 31 + HandSize;
                h = h * 31 + LevelsToWin;
                h = h * 31 + TurnTimerSeconds;
                h = h * 31 + GoOutBonus;
                h = h * 31 + (FinalLevelRequired ? 1 : 0);
                h = h * 31 + RoundTurnLimit;
                h = h * 31 + (CatchUp ? 1 : 0);
                return h;
            }
        }

        public override string ToString() =>
            $"[contrats {(RandomContracts ? "variés" : "fixes")}, spéciales {SpecialCards}, " +
            $"main {HandSize}, {LevelsToWin} niveaux, minuteur {TurnTimerSeconds}s]";
    }
}
