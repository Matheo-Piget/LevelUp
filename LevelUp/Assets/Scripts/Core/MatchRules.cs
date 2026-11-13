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

        public MatchRules(bool randomContracts = false, SpecialCardsMode specialCards = SpecialCardsMode.None,
            int handSize = DefaultHandSize, int levelsToWin = Constants.MaxLevel, int turnTimerSeconds = 0)
        {
            RandomContracts = randomContracts;
            SpecialCards = specialCards > SpecialCardsMode.All ? SpecialCardsMode.None : specialCards;
            HandSize = Clamp(handSize, MinHandSize, MaxHandSize);
            LevelsToWin = Clamp(levelsToWin, MinLevelsToWin, Constants.MaxLevel);
            TurnTimerSeconds = Clamp(turnTimerSeconds, 0, MaxTurnTimerSeconds);
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
            && LevelsToWin == Constants.MaxLevel && TurnTimerSeconds == 0;

        private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;

        public override bool Equals(object? obj)
        {
            return obj is MatchRules o
                   && o.RandomContracts == RandomContracts
                   && o.SpecialCards == SpecialCards
                   && o.HandSize == HandSize
                   && o.LevelsToWin == LevelsToWin
                   && o.TurnTimerSeconds == TurnTimerSeconds;
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
                return h;
            }
        }

        public override string ToString() =>
            $"[contrats {(RandomContracts ? "variés" : "fixes")}, spéciales {SpecialCards}, " +
            $"main {HandSize}, {LevelsToWin} niveaux, minuteur {TurnTimerSeconds}s]";
    }
}
