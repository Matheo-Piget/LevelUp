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

        // --- Background & surfaces (palette Balatro-like : navy profond, tons plum) ---
        public static readonly Color BackgroundDark   = new Color32(0x0B, 0x12, 0x1C, 0xFF); // #0B121C – base plus profonde
        public static readonly Color BackgroundNavy   = new Color32(0x0B, 0x12, 0x1C, 0xFF); // alias
        public static readonly Color BackgroundDeep   = new Color32(0x06, 0x0A, 0x14, 0xFF); // #060A14 – vignette/périphérie
        public static readonly Color CardFaceColor    = new Color32(0x1C, 0x23, 0x33, 0xFF); // #1C2333
        public static readonly Color CardBack         = new Color32(0x15, 0x1D, 0x2B, 0xFF); // #151D2B
        public static readonly Color CardBackPattern  = new Color32(0x1F, 0x2A, 0x3E, 0xFF); // #1F2A3E

        // --- UI surfaces (hiérarchie de profondeur) ---
        public static readonly Color SurfaceBase      = new Color32(0x0F, 0x17, 0x22, 0xFF); // fond appli
        public static readonly Color SurfaceA         = new Color32(0x13, 0x1B, 0x28, 0xEE); // panneau principal
        public static readonly Color SurfaceB         = new Color32(0x19, 0x22, 0x32, 0xFF); // panneau secondaire
        public static readonly Color SurfaceC         = new Color32(0x20, 0x2B, 0x40, 0xFF); // élément élevé / hover
        public static readonly Color PanelBackground  = new Color32(0x13, 0x1B, 0x28, 0xEE); // alias → SurfaceA
        public static readonly Color PanelBorder      = new Color32(0x2A, 0x3A, 0x50, 0xFF); // #2A3A50
        public static readonly Color PanelBorderSoft  = new Color32(0x2A, 0x3A, 0x50, 0x80); // border discret
        public static readonly Color PanelHighlight   = new Color32(0x1E, 0x2D, 0x40, 0xFF); // #1E2D40
        public static readonly Color GlassTint        = new Color32(0xFF, 0xFF, 0xFF, 0x0D); // film lumineux

        // --- Texte ---
        public static readonly Color TextPrimary      = new Color32(0xF2, 0xF5, 0xFA, 0xFF); // #F2F5FA – plus lumineux
        public static readonly Color TextSecondary    = new Color32(0x9A, 0xAB, 0xC2, 0xFF); // #9AABC2 – meilleur contraste
        public static readonly Color TextMuted        = new Color32(0x5E, 0x6F, 0x87, 0xFF); // #5E6F87 – labels discrets
        public static readonly Color TextAccent       = new Color32(0xFF, 0xD9, 0x4D, 0xFF); // jaune doré

        // --- Accents & phases (sémantique) ---
        public static readonly Color AccentGold       = new Color32(0xFF, 0xD9, 0x4D, 0xFF);
        public static readonly Color AccentMagenta    = new Color32(0xFF, 0x5E, 0xA8, 0xFF);
        public static readonly Color AccentCyan       = new Color32(0x5E, 0xE8, 0xFF, 0xFF);
        public static readonly Color PhaseDraw        = new Color32(0x4D, 0x9A, 0xFF, 0xFF); // bleu (= CardBlue)
        public static readonly Color PhaseLayDown     = new Color32(0x4D, 0xFF, 0x91, 0xFF); // vert (= CardGreen)
        public static readonly Color PhaseAddToMelds  = new Color32(0xBB, 0x6B, 0xFF, 0xFF); // violet (= CardPurple)
        public static readonly Color PhaseDiscard     = new Color32(0xFF, 0x8C, 0x42, 0xFF); // orange (= CardOrange)
        public static readonly Color Success          = new Color32(0x4D, 0xFF, 0x91, 0xFF);
        public static readonly Color Danger           = new Color32(0xFF, 0x4D, 0x6A, 0xFF);
        public static readonly Color Warning          = new Color32(0xFF, 0xD9, 0x4D, 0xFF);

        // --- Glow / effets ---
        public static readonly Color GlowWhite        = new Color32(0xFF, 0xFF, 0xFF, 0x40); // white glow
        public static readonly Color GlowSelect       = new Color32(0xFF, 0xFF, 0xFF, 0x70); // selection glow – plus net
        public static readonly Color GlowGold         = new Color32(0xFF, 0xD9, 0x4D, 0x55); // glow accent doré

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

        /// <summary>Couleur dédiée à un CardType action (Skip/Draw2/Wild/WildDraw2).</summary>
        public static Color GetActionColor(CardType type)
        {
            return type switch
            {
                CardType.Skip      => CardRed,
                CardType.Draw2     => CardOrange,
                CardType.Wild      => CardGreen,
                CardType.WildDraw2 => CardPurple,
                _                  => Color.white
            };
        }

        /// <summary>Couleur sémantique associée à une phase de tour.</summary>
        public static Color GetPhaseColor(TurnPhase phase)
        {
            return phase switch
            {
                TurnPhase.Draw       => PhaseDraw,
                TurnPhase.LayDown    => PhaseLayDown,
                TurnPhase.AddToMelds => PhaseAddToMelds,
                TurnPhase.Discard    => PhaseDiscard,
                _                    => TextPrimary
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
