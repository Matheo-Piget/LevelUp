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

        // --- Couleurs de carte (palette à hue + luminance variées pour max de distinction) ---
        // - Red : red-600 (deep crimson, lightness ~47%)
        // - Orange : orange-500 (vivid orange, lightness ~53%)
        // - Yellow : yellow-300 (très clair, lightness ~65%) → contraste fort vs orange/red
        // - Green : green-500 true green (lightness ~50%) → différencié de blue/cyan
        // - Blue : blue-500 (lightness ~60%)
        // - Purple : violet-500 (lightness ~65%) → différencié de blue
        public static readonly Color CardRed    = new Color32(0xDC, 0x26, 0x26, 0xFF); // #DC2626 (red-600)
        public static readonly Color CardOrange = new Color32(0xF9, 0x73, 0x16, 0xFF); // #F97316 (orange-500)
        public static readonly Color CardYellow = new Color32(0xFD, 0xE0, 0x47, 0xFF); // #FDE047 (yellow-300)
        public static readonly Color CardGreen  = new Color32(0x22, 0xC5, 0x5E, 0xFF); // #22C55E (green-500)
        public static readonly Color CardBlue   = new Color32(0x3B, 0x82, 0xF6, 0xFF); // #3B82F6 (blue-500)
        public static readonly Color CardPurple = new Color32(0x8B, 0x5C, 0xF6, 0xFF); // #8B5CF6 (violet-500)

        // Versions assombries pour le bas du dégradé vertical (Tailwind 700/600).
        public static readonly Color CardRedDark    = new Color32(0xB9, 0x1C, 0x1C, 0xFF); // red-700
        public static readonly Color CardOrangeDark = new Color32(0xC2, 0x41, 0x0C, 0xFF); // orange-700
        public static readonly Color CardYellowDark = new Color32(0xEA, 0xB3, 0x08, 0xFF); // yellow-500
        public static readonly Color CardGreenDark  = new Color32(0x15, 0x80, 0x3D, 0xFF); // green-700
        public static readonly Color CardBlueDark   = new Color32(0x1D, 0x4E, 0xD8, 0xFF); // blue-700
        public static readonly Color CardPurpleDark = new Color32(0x6D, 0x28, 0xD9, 0xFF); // violet-700

        // --- Background & surfaces (palette neutre : fond uni, indigo en accent) ---
        public static readonly Color BackgroundDark   = new Color32(0x0F, 0x14, 0x19, 0xFF); // #0F1419 – fond principal
        public static readonly Color BackgroundNavy   = new Color32(0x0F, 0x14, 0x19, 0xFF); // alias
        public static readonly Color BackgroundDeep   = new Color32(0x0A, 0x0E, 0x12, 0xFF); // #0A0E12 – vignette/périphérie
        public static readonly Color CardFaceColor    = new Color32(0x1A, 0x21, 0x2B, 0xFF); // surface neutre pour les cartes
        public static readonly Color CardBack         = new Color32(0x16, 0x1B, 0x22, 0xFF); // #161B22
        public static readonly Color CardBackPattern  = new Color32(0x1F, 0x26, 0x30, 0xFF);

        // --- UI surfaces (hiérarchie de profondeur, neutres) ---
        public static readonly Color SurfaceBase      = new Color32(0x0F, 0x14, 0x19, 0xFF); // fond appli
        public static readonly Color SurfaceA         = new Color32(0x16, 0x1B, 0x22, 0xFF); // topbar / sidebar
        public static readonly Color SurfaceB         = new Color32(0x13, 0x18, 0x20, 0xFF); // panneau secondaire
        public static readonly Color SurfaceC         = new Color32(0x1F, 0x26, 0x30, 0xFF); // élément élevé / hover
        public static readonly Color PanelBackground  = new Color32(0x16, 0x1B, 0x22, 0xF2); // alias → SurfaceA
        public static readonly Color PanelBorder      = new Color32(0xFF, 0xFF, 0xFF, 0x1F); // rgba(255,255,255,0.12)
        public static readonly Color PanelBorderSoft  = new Color32(0xFF, 0xFF, 0xFF, 0x0F); // rgba(255,255,255,0.06)
        public static readonly Color PanelHighlight   = new Color32(0x1F, 0x26, 0x30, 0xFF); // hover
        public static readonly Color GlassTint        = new Color32(0xFF, 0xFF, 0xFF, 0x07); // surface 2 : 0.02

        // --- Texte (3 niveaux de contraste) ---
        public static readonly Color TextPrimary      = new Color32(0xE5, 0xE7, 0xEB, 0xFF); // #E5E7EB
        public static readonly Color TextSecondary    = new Color32(0x9C, 0xA3, 0xAF, 0xFF); // #9CA3AF
        public static readonly Color TextMuted        = new Color32(0x6B, 0x72, 0x80, 0xFF); // #6B7280
        public static readonly Color TextAccent       = new Color32(0x81, 0x8C, 0xF8, 0xFF); // indigo light pour titres accentués

        // --- Accents (une seule couleur d'interaction : indigo) ---
        public static readonly Color AccentPrimary    = new Color32(0x63, 0x66, 0xF1, 0xFF); // #6366F1 indigo
        public static readonly Color AccentLight      = new Color32(0x81, 0x8C, 0xF8, 0xFF); // #818CF8 indigo light
        public static readonly Color AccentLighter    = new Color32(0xC7, 0xD2, 0xFE, 0xFF); // #C7D2FE indigo-200 (texte sur pill)
        public static readonly Color AccentDeep       = new Color32(0x43, 0x38, 0xCA, 0xFF); // #4338CA indigo-700 (gradient bas)
        public static readonly Color AccentDim        = new Color32(0x63, 0x66, 0xF1, 0x33); // indigo translucide

        // Tokens pill indigo (turn badge) :
        //   bg = rgba(99,102,241, 0.12)  ≈ alpha 0x1F
        //   border = rgba(99,102,241, 0.35) ≈ alpha 0x59
        //   active row bg = rgba(99,102,241, 0.08) ≈ alpha 0x14
        //   active row border = rgba(129,140,248, 0.40) ≈ alpha 0x66
        public static readonly Color PillIndigoBg     = new Color32(0x63, 0x66, 0xF1, 0x1F);
        public static readonly Color PillIndigoBorder = new Color32(0x63, 0x66, 0xF1, 0x59);
        public static readonly Color ActiveRowBg      = new Color32(0x63, 0x66, 0xF1, 0x14);
        public static readonly Color ActiveRowBorder  = new Color32(0x81, 0x8C, 0xF8, 0x66);
        public static readonly Color AccentGold       = new Color32(0x63, 0x66, 0xF1, 0xFF); // alias → indigo (compat code)
        public static readonly Color AccentMagenta    = new Color32(0x63, 0x66, 0xF1, 0xFF); // alias → indigo
        public static readonly Color AccentCyan       = new Color32(0x81, 0x8C, 0xF8, 0xFF); // alias → indigo light

        // --- Phases : neutres, on s'appuie sur le hint discret en bas ---
        public static readonly Color PhaseDraw        = new Color32(0x81, 0x8C, 0xF8, 0xFF); // indigo light
        public static readonly Color PhaseLayDown     = new Color32(0x81, 0x8C, 0xF8, 0xFF);
        public static readonly Color PhaseAddToMelds  = new Color32(0x81, 0x8C, 0xF8, 0xFF);
        public static readonly Color PhaseDiscard     = new Color32(0x81, 0x8C, 0xF8, 0xFF);
        public static readonly Color Success          = new Color32(0x4D, 0xFF, 0x91, 0xFF);
        public static readonly Color Danger           = new Color32(0xFF, 0x4D, 0x6A, 0xFF);
        public static readonly Color Warning          = new Color32(0xFB, 0xBF, 0x24, 0xFF);

        // --- Glow / effets (toujours accent indigo) ---
        public static readonly Color GlowWhite        = new Color32(0xFF, 0xFF, 0xFF, 0x33);
        public static readonly Color GlowSelect       = new Color32(0xFF, 0xFF, 0xFF, 0x66);
        public static readonly Color GlowGold         = new Color32(0x63, 0x66, 0xF1, 0x55); // alias → glow indigo

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
        /// Retourne la version assombrie (Tailwind 600) d'une couleur de carte —
        /// utilisée pour le bas du dégradé vertical sur la face de carte.
        /// </summary>
        public static Color GetDarkColor(CardColor color)
        {
            return color switch
            {
                CardColor.Red    => CardRedDark,
                CardColor.Blue   => CardBlueDark,
                CardColor.Green  => CardGreenDark,
                CardColor.Yellow => CardYellowDark,
                CardColor.Purple => CardPurpleDark,
                CardColor.Orange => CardOrangeDark,
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
