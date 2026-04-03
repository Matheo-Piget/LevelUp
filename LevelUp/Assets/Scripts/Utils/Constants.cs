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

        // --- Couleurs de carte (saturées, style néon sur fond sombre) ---
        public static readonly Color CardRed    = new Color32(0xFF, 0x4D, 0x6A, 0xFF); // #FF4D6A
        public static readonly Color CardBlue   = new Color32(0x4D, 0x9A, 0xFF, 0xFF); // #4D9AFF
        public static readonly Color CardGreen  = new Color32(0x4D, 0xFF, 0x91, 0xFF); // #4DFF91
        public static readonly Color CardYellow = new Color32(0xFF, 0xD9, 0x4D, 0xFF); // #FFD94D
        public static readonly Color CardPurple = new Color32(0xBB, 0x6B, 0xFF, 0xFF); // #BB6BFF
        public static readonly Color CardOrange = new Color32(0xFF, 0x8C, 0x42, 0xFF); // #FF8C42

        // --- Background & surfaces ---
        public static readonly Color BackgroundDark   = new Color32(0x0F, 0x19, 0x23, 0xFF); // #0F1923
        public static readonly Color BackgroundNavy   = new Color32(0x0F, 0x19, 0x23, 0xFF); // alias
        public static readonly Color CardFaceColor    = new Color32(0x1C, 0x23, 0x33, 0xFF); // #1C2333
        public static readonly Color CardBack         = new Color32(0x15, 0x1D, 0x2B, 0xFF); // #151D2B
        public static readonly Color CardBackPattern  = new Color32(0x1F, 0x2A, 0x3E, 0xFF); // #1F2A3E

        // --- UI surfaces ---
        public static readonly Color PanelBackground  = new Color32(0x13, 0x1B, 0x28, 0xDD); // semi-transparent
        public static readonly Color PanelBorder      = new Color32(0x2A, 0x3A, 0x50, 0xFF); // #2A3A50
        public static readonly Color PanelHighlight   = new Color32(0x1E, 0x2D, 0x40, 0xFF); // #1E2D40

        // --- Texte ---
        public static readonly Color TextPrimary      = new Color32(0xE8, 0xED, 0xF2, 0xFF); // #E8EDF2
        public static readonly Color TextSecondary    = new Color32(0x7A, 0x8B, 0xA0, 0xFF); // #7A8BA0
        public static readonly Color TextAccent       = new Color32(0xFF, 0xD9, 0x4D, 0xFF); // jaune doré

        // --- Glow / effets ---
        public static readonly Color GlowWhite        = new Color32(0xFF, 0xFF, 0xFF, 0x40); // white glow
        public static readonly Color GlowSelect       = new Color32(0xFF, 0xFF, 0xFF, 0x60); // selection glow

        // --- Animations ---
        public const float AnimDrawDuration    = 0.35f;
        public const float AnimPlayDuration    = 0.3f;
        public const float AnimDiscardDuration = 0.25f;
        public const float AnimHoverDuration   = 0.12f;
        public const float AnimBounceDuration  = 0.4f;
        public const float AnimCascadeDelay    = 0.06f;

        // --- Hand / hover ---
        public const float HoverLiftY       = 45f;
        public const float HoverScale       = 1.15f;
        public const float SelectLiftY      = 35f;
        public const float HandArcHeight    = 55f;
        public const float HandFanAngle     = 5f;
        public const float CardLerpSpeed    = 12f;

        // --- UI ---
        public const float CardAspectRatio  = 2f / 3f;
        public const float CardCornerRadius = 12f;
        public const float PanelCornerRadius = 8f;

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

        /// <summary>
        /// Retourne une version glow (plus claire) d'une couleur de carte.
        /// </summary>
        public static Color GetGlowColor(CardColor color)
        {
            Color c = GetColor(color);
            return new Color(
                Mathf.Min(c.r + 0.35f, 1f),
                Mathf.Min(c.g + 0.35f, 1f),
                Mathf.Min(c.b + 0.35f, 1f),
                1f);
        }

        /// <summary>
        /// Retourne une version assombrie d'une couleur (pour le fond de carte).
        /// </summary>
        public static Color GetDimColor(CardColor color)
        {
            Color c = GetColor(color);
            return new Color(c.r * 0.3f, c.g * 0.3f, c.b * 0.3f, 1f);
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
