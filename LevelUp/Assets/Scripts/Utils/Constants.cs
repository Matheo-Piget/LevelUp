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

        // --- Couleurs de carte : identité « Salon » ---
        // Teintes riches mais adoucies (moins néon), luminances bien séparées.
        // Encre blanche sur rouge / bleu / violet, encre sombre sur orange /
        // jaune / vert (UISkin.InkFor) : valeurs lisibles en grand (≥ 3:1, 4.3+ ici).
        public static readonly Color CardRed    = new Color32(0xCF, 0x50, 0x49, 0xFF); // #CF5049 brique
        public static readonly Color CardOrange = new Color32(0xDE, 0x8A, 0x3E, 0xFF); // #DE8A3E ambre
        public static readonly Color CardYellow = new Color32(0xE4, 0xC1, 0x5A, 0xFF); // #E4C15A safran
        public static readonly Color CardGreen  = new Color32(0x45, 0xA2, 0x71, 0xFF); // #45A271 jade
        public static readonly Color CardBlue   = new Color32(0x3D, 0x79, 0xC2, 0xFF); // #3D79C2 cobalt doux
        public static readonly Color CardPurple = new Color32(0x85, 0x62, 0xB5, 0xFF); // #8562B5 prune

        // Versions assombries (ombres, contours) de chaque couleur.
        public static readonly Color CardRedDark    = new Color32(0xA9, 0x3C, 0x37, 0xFF);
        public static readonly Color CardOrangeDark = new Color32(0xB6, 0x6B, 0x2A, 0xFF);
        public static readonly Color CardYellowDark = new Color32(0xBE, 0x9C, 0x3B, 0xFF);
        public static readonly Color CardGreenDark  = new Color32(0x32, 0x7F, 0x57, 0xFF);
        public static readonly Color CardBlueDark   = new Color32(0x2D, 0x5D, 0x99, 0xFF);
        public static readonly Color CardPurpleDark = new Color32(0x67, 0x48, 0x91, 0xFF);

        // --- Fond & surfaces (graphite chaud ; accent champagne) ---
        public static readonly Color BackgroundDark   = new Color32(0x11, 0x15, 0x16, 0xFF); // #111516 – fond principal
        public static readonly Color BackgroundNavy   = new Color32(0x11, 0x15, 0x16, 0xFF); // alias
        public static readonly Color BackgroundDeep   = new Color32(0x0D, 0x10, 0x11, 0xFF); // #0D1011 – périphérie
        public static readonly Color CardFaceColor    = new Color32(0x1C, 0x22, 0x24, 0xFF); // surface neutre des cartes
        public static readonly Color CardBack         = new Color32(0x17, 0x1B, 0x1C, 0xFF); // #171B1C
        public static readonly Color CardBackPattern  = new Color32(0x22, 0x28, 0x2A, 0xFF);

        // --- UI surfaces (hiérarchie de profondeur, neutres) ---
        public static readonly Color SurfaceBase      = new Color32(0x11, 0x15, 0x16, 0xFF); // fond appli
        public static readonly Color SurfaceA         = new Color32(0x17, 0x1B, 0x1C, 0xFF); // topbar / panneaux
        public static readonly Color SurfaceB         = new Color32(0x14, 0x18, 0x19, 0xFF); // panneau secondaire
        public static readonly Color SurfaceC         = new Color32(0x22, 0x28, 0x2A, 0xFF); // élément élevé / hover
        public static readonly Color PanelBackground  = new Color32(0x17, 0x1B, 0x1C, 0xF2); // alias → SurfaceA
        public static readonly Color PanelBorder      = new Color32(0xFF, 0xFF, 0xFF, 0x1F); // rgba(255,255,255,0.12)
        public static readonly Color PanelBorderSoft  = new Color32(0xFF, 0xFF, 0xFF, 0x0F); // rgba(255,255,255,0.06)
        public static readonly Color PanelHighlight   = new Color32(0x22, 0x28, 0x2A, 0xFF); // hover
        public static readonly Color GlassTint        = new Color32(0xFF, 0xFF, 0xFF, 0x09); // verre léger

        // --- Texte (3 niveaux de contraste, crème chaud) ---
        public static readonly Color TextPrimary      = new Color32(0xEE, 0xE9, 0xE0, 0xFF); // #EEE9E0
        public static readonly Color TextSecondary    = new Color32(0xA7, 0xA1, 0x98, 0xFF); // #A7A198
        public static readonly Color TextMuted        = new Color32(0x7E, 0x7A, 0x73, 0xFF); // #7E7A73
        public static readonly Color TextAccent       = new Color32(0xE6, 0xCB, 0x92, 0xFF); // champagne clair

        // --- Accent unique d'interaction : champagne ---
        public static readonly Color AccentPrimary    = new Color32(0xD8, 0xB6, 0x6E, 0xFF); // #D8B66E
        public static readonly Color AccentLight      = new Color32(0xE6, 0xCB, 0x92, 0xFF); // #E6CB92
        public static readonly Color AccentLighter    = new Color32(0xF2, 0xE3, 0xC2, 0xFF); // #F2E3C2 (texte sur pastille)
        public static readonly Color AccentDeep       = new Color32(0xA8, 0x8A, 0x4C, 0xFF); // #A88A4C
        public static readonly Color AccentDim        = new Color32(0xD8, 0xB6, 0x6E, 0x33); // champagne translucide

        // Tokens pastille d'accent (noms historiques conservés pour compat) :
        //   bg = 12 %, bordure = 35 %, ligne active = 8 %, bordure active = 40 %.
        public static readonly Color PillIndigoBg     = new Color32(0xD8, 0xB6, 0x6E, 0x1F);
        public static readonly Color PillIndigoBorder = new Color32(0xD8, 0xB6, 0x6E, 0x59);
        public static readonly Color ActiveRowBg      = new Color32(0xD8, 0xB6, 0x6E, 0x14);
        public static readonly Color ActiveRowBorder  = new Color32(0xE6, 0xCB, 0x92, 0x66);
        public static readonly Color AccentGold       = new Color32(0xD8, 0xB6, 0x6E, 0xFF); // alias → accent
        public static readonly Color AccentMagenta    = new Color32(0xD8, 0xB6, 0x6E, 0xFF); // alias → accent
        public static readonly Color AccentCyan       = new Color32(0xE6, 0xCB, 0x92, 0xFF); // alias → accent clair

        // --- Phases : neutres, on s'appuie sur le hint discret en bas ---
        public static readonly Color PhaseDraw        = new Color32(0xE6, 0xCB, 0x92, 0xFF);
        public static readonly Color PhaseLayDown     = new Color32(0xE6, 0xCB, 0x92, 0xFF);
        public static readonly Color PhaseAddToMelds  = new Color32(0xE6, 0xCB, 0x92, 0xFF);
        public static readonly Color PhaseDiscard     = new Color32(0xE6, 0xCB, 0x92, 0xFF);
        public static readonly Color Success          = new Color32(0x5F, 0xB9, 0x8A, 0xFF); // vert sauge
        public static readonly Color Danger           = new Color32(0xD9, 0x6C, 0x5F, 0xFF); // corail doux
        public static readonly Color Warning          = new Color32(0xE6, 0xCB, 0x92, 0xFF); // champagne

        // --- Glow / effets (accent champagne) ---
        public static readonly Color GlowWhite        = new Color32(0xFF, 0xFF, 0xFF, 0x33);
        public static readonly Color GlowSelect       = new Color32(0xFF, 0xFF, 0xFF, 0x66);
        public static readonly Color GlowGold         = new Color32(0xD8, 0xB6, 0x6E, 0x55); // glow accent

        // --- Animations ---
        public const float AnimDrawDuration    = 0.35f;
        public const float AnimPlayDuration    = 0.3f;
        public const float AnimDiscardDuration = 0.25f;
        public const float AnimHoverDuration   = 0.12f;
        public const float AnimBounceDuration  = 0.4f;
        public const float AnimCascadeDelay    = 0.06f;

        // --- Hand / hover ---
        public const float HoverLiftY       = 22f;  // maquette : survol y −8 à −20
        public const float HoverScale       = 1.06f; // maquette : survol scale 1.06
        public const float SelectLiftY      = 20f;  // maquette : sélection y −20
        public const float HandArcHeight    = 55f;
        public const float HandFanAngle     = 24f;  // maquette : ±3° par carte
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

        /// <summary>
        /// Encre (texte/symboles) la plus lisible sur un fond donné : blanc ou
        /// quasi-noir, choisi par ratio de contraste WCAG. Garantit la lecture de
        /// la valeur sur toutes les couleurs (orange/jaune/vert → encre sombre).
        /// </summary>
        public static Color GetInkColor(Color background)
        {
            float lum = RelativeLuminance(background);
            float contrastWhite = 1.05f / (lum + 0.05f);
            float contrastDark = (lum + 0.05f) / (RelativeLuminance(InkDark) + 0.05f);
            return contrastWhite >= contrastDark ? Color.white : InkDark;
        }

        /// <summary>Encre sombre neutre utilisée sur les cartes claires.</summary>
        public static readonly Color InkDark = new Color32(0x14, 0x11, 0x0C, 0xFF);

        private static float RelativeLuminance(Color c)
        {
            return 0.2126f * Linearize(c.r) + 0.7152f * Linearize(c.g) + 0.0722f * Linearize(c.b);
        }

        private static float Linearize(float v)
        {
            return v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
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
