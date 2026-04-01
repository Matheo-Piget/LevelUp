using UnityEngine;

namespace LevelUp.Utils
{
    /// <summary>
    /// Toutes les constantes globales du jeu Level Up.
    /// </summary>
    public static class Constants
    {
        // --- Deck ---
        public const int DefaultCardMin = 1;
        public const int DefaultCardMax = 18;
        public const int ColorsCount = 6;
        public const int DefaultDeckSize = 108;
        public const int CardsPerPlayerStart = 10;

        // --- Niveaux ---
        public const int MaxLevel = 8;

        // --- Couleurs de carte ---
        public static readonly Color CardRed    = new Color32(0xE8, 0x45, 0x45, 0xFF); // #E84545
        public static readonly Color CardBlue   = new Color32(0x45, 0x85, 0xE8, 0xFF); // #4585E8
        public static readonly Color CardGreen  = new Color32(0x45, 0xC8, 0x78, 0xFF); // #45C878
        public static readonly Color CardYellow = new Color32(0xF5, 0xC8, 0x42, 0xFF); // #F5C842
        public static readonly Color CardPurple = new Color32(0x9B, 0x59, 0xB6, 0xFF); // #9B59B6
        public static readonly Color CardOrange = new Color32(0xE8, 0x7D, 0x2A, 0xFF); // #E87D2A

        public static readonly Color BackgroundNavy = new Color32(0x1A, 0x1F, 0x3C, 0xFF); // #1A1F3C
        public static readonly Color CardBack       = new Color32(0x2C, 0x31, 0x50, 0xFF);

        // --- Animations ---
        public const float AnimDrawDuration    = 0.3f;
        public const float AnimPlayDuration    = 0.3f;
        public const float AnimDiscardDuration = 0.3f;

        // --- UI ---
        public const float CardAspectRatio = 2f / 3f; // largeur / hauteur
        public const float CardCornerRadius = 8f;

        // --- Action Cards ---
        public const int SkipCardValue      = -1;
        public const int Draw2CardValue     = -2;
        public const int WildCardValue      = -3;
        public const int WildDraw2CardValue = -4;

        /// <summary>
        /// Retourne la couleur Unity associée à un CardColor.
        /// </summary>
        public static Color GetColor(CardColor color)
        {
            return color switch
            {
                CardColor.Red    => CardRed,
                CardColor.Blue   => CardBlue,
                CardColor.Green  => CardGreen,
                CardColor.Yellow => CardYellow,
                CardColor.Purple => CardPurple,
                CardColor.Orange => CardOrange,
                _                => Color.white
            };
        }
    }

    /// <summary>
    /// Les 6 couleurs de cartes du jeu.
    /// </summary>
    public enum CardColor
    {
        Red,
        Blue,
        Green,
        Yellow,
        Purple,
        Orange,
        Wild // pour les cartes spéciales
    }

    /// <summary>
    /// Type de carte : normale ou action.
    /// </summary>
    public enum CardType
    {
        Normal,
        Skip,
        Draw2,
        Wild,
        WildDraw2
    }

    /// <summary>
    /// Les phases d'un tour de jeu.
    /// </summary>
    public enum TurnPhase
    {
        Draw,
        LayDown,
        AddToMelds,
        Discard
    }

    /// <summary>
    /// Les états globaux de la partie.
    /// </summary>
    public enum GameState
    {
        Setup,
        PlayerTurn,
        Validate,
        EndRound,
        GameOver
    }

    /// <summary>
    /// Le type de combinaison posée sur la table.
    /// </summary>
    public enum MeldType
    {
        Run,     // Suite (cartes consécutives)
        Set,     // Brelan / Carré (même valeur)
        Flush    // Flush (même couleur)
    }
}
