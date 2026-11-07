using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;
using TMPro;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>Graisse de texte de la DA (Inter pour l'UI, DM Serif Display pour les titres/valeurs).</summary>
    public enum TextWeight
    {
        Regular,
        Medium,
        SemiBold,
        Bold,
        Display
    }

    /// <summary>Icônes vectorielles rasterisées en code (aucun asset externe).</summary>
    public enum UIIcon
    {
        Pause,
        Settings,
        Play,
        Check,
        Close,
        Lock,
        Cards,
        SortValue,
        Skip
    }

    /// <summary>
    /// Thème visuel unique du jeu, aligné sur les maquettes "LevelUp UI" :
    /// jetons de couleur, polices (Inter + DM Serif Display, chargées depuis
    /// Resources et converties en TMP dynamiques au runtime), sprites arrondis
    /// nets (générés en 2x), symboles de couleur et icônes.
    /// Tout est généré une fois puis mis en cache : zéro allocation en jeu.
    /// </summary>
    public static class UISkin
    {
        // ─── Couleurs : identité « Salon » (sobre, feutrée, détendue) ────
        // Graphite chaud, tapis vert profond, accent champagne, texte crème.
        public static readonly Color Ground = Hex(0x0D1011);
        public static readonly Color Base = Hex(0x111516);
        public static readonly Color Surface = Hex(0x171B1C);
        public static readonly Color SurfaceRaised = Hex(0x22282A);
        public static readonly Color CardFace = Hex(0x1C2224);
        public static readonly Color Felt = Hex(0x14211D);
        public static readonly Color FeltCenter = Hex(0x1B2D27);
        public static readonly Color TextColor = Hex(0xEEE9E0);
        public static readonly Color TextMuted = Hex(0xA7A198);
        /// <summary>Accent unique (champagne) : actions principales, sélection, progression.</summary>
        public static readonly Color Accent = Hex(0xD8B66E);
        public static readonly Color AccentLight = Hex(0xE6CB92);
        public static readonly Color AccentPale = Hex(0xF2E3C2);
        /// <summary>Encre sur fond accent (texte des boutons champagne).</summary>
        public static readonly Color OnAccent = Hex(0x1B160E);
        /// <summary>Anneau de sélection des cartes : crème, lisible sur toutes les couleurs.</summary>
        public static readonly Color SelectRing = Hex(0xF4EEE3);
        public static readonly Color Success = Hex(0x5FB98A);
        public static readonly Color Line = new(1f, 1f, 1f, 0.07f);
        public static readonly Color LineStrong = new(1f, 1f, 1f, 0.11f);
        public static readonly Color Glass = new(1f, 1f, 1f, 0.035f);
        public static readonly Color AccentWash = Hex(0xD8B66E, 0.12f);
        public static readonly Color AccentWashStrong = Hex(0xD8B66E, 0.20f);

        /// <summary>Teintes d'avatar par joueur (pastels chauds, lisibles sur fond sombre).</summary>
        public static readonly Color[] AvatarTints =
        {
            Hex(0xE9C9A0), Hex(0x9CC9B3), Hex(0xA9BEDC), Hex(0xD9A9A9), Hex(0xC5B3DA), Hex(0xE6D49A)
        };

        public static Color Hex(int rgb, float a = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);
        }

        public static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);

        // ─── Polices ─────────────────────────────────────────────────────
        private static bool _fontsLoaded;
        private static TMP_FontAsset? _body;
        private static TMP_FontAsset? _medium;
        private static TMP_FontAsset? _semi;
        private static TMP_FontAsset? _bold;
        private static TMP_FontAsset? _display;

        /// <summary>Inter Regular (bold = Inter Bold via la table de graisses).</summary>
        public static TMP_FontAsset? BodyFont { get { EnsureFonts(); return _body; } }

        /// <summary>DM Serif Display : titres, valeurs des cartes, chiffres (identité « Salon »).</summary>
        public static TMP_FontAsset? DisplayFont { get { EnsureFonts(); return _display; } }

        /// <summary>
        /// Charge les TTF de Resources/Fonts et crée les font assets TMP
        /// dynamiques. Remplace aussi la police par défaut de TMP : tout texte
        /// créé ensuite (prefabs, menus, overlays) hérite d'Inter.
        /// </summary>
        public static void EnsureFonts()
        {
            // (_body détruit = nouvelle session de Play sans domain reload : on recrée)
            if (_fontsLoaded && _body != null) return;
            _fontsLoaded = true;

            TMP_FontAsset? fallback = _originalDefaultFont != null
                ? _originalDefaultFont
                : TMP_Settings.defaultFontAsset;

            _body = Create("Fonts/Inter-Regular", fallback);
            _medium = Create("Fonts/Inter-SemiBold", fallback);
            _semi = _medium;
            _bold = Create("Fonts/Inter-Bold", fallback);
            _display = Create("Fonts/DMSerifDisplay-Regular", fallback);

            if (_body != null)
            {
                // Le style Bold de TMP pioche la vraie graisse (pas de faux gras).
                TMP_FontWeightPair[] table = _body.fontWeightTable;
                if (table != null && table.Length > 7)
                {
                    if (_semi != null) table[6].regularTypeface = _semi;
                    if (_bold != null) table[7].regularTypeface = _bold;
                }
                OverrideTmpDefault(_body);
            }
        }

        private static TMP_FontAsset? Create(string path, TMP_FontAsset? fallback)
        {
            Font? font = Resources.Load<Font>(path);
            if (font == null)
            {
                Debug.LogWarning($"[UISkin] Police introuvable : Resources/{path}");
                return null;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9,
                GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = font.name + " SDF (runtime)";
            if (fallback != null)
            {
                asset.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
                asset.fallbackFontAssetTable.Add(fallback);
            }
            return asset;
        }

        private static TMP_FontAsset? _originalDefaultFont;
        private static bool _restoreHooked;

        /// <summary>
        /// Remplace (en mémoire) la police par défaut de TMP. Dans l'éditeur, la
        /// valeur d'origine est remise à la sortie du Play : l'asset
        /// "TMP Settings" ne doit jamais référencer une police créée au runtime.
        /// </summary>
        private static void OverrideTmpDefault(TMP_FontAsset asset)
        {
            try
            {
                TMP_Settings settings = TMP_Settings.instance;
                FieldInfo? field = typeof(TMP_Settings).GetField("m_defaultFontAsset",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (settings == null || field == null) return;

                if (_originalDefaultFont == null)
                {
                    _originalDefaultFont = field.GetValue(settings) as TMP_FontAsset;
                }
                field.SetValue(settings, asset);

                if (!_restoreHooked)
                {
                    _restoreHooked = true;
                    Application.quitting += RestoreTmpDefault;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UISkin] Police par défaut TMP non remplacée : {e.Message}");
            }
        }

        private static void RestoreTmpDefault()
        {
            try
            {
                TMP_Settings settings = TMP_Settings.instance;
                FieldInfo? field = typeof(TMP_Settings).GetField("m_defaultFontAsset",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (settings != null && field != null && _originalDefaultFont != null)
                {
                    field.SetValue(settings, _originalDefaultFont);
                }
            }
            catch (Exception)
            {
                // Rien à faire : l'application se ferme.
            }
            _restoreHooked = false;
            Application.quitting -= RestoreTmpDefault;
        }

        /// <summary>Applique une graisse de la DA à un texte TMP existant.</summary>
        public static void ApplyFont(TMP_Text text, TextWeight weight)
        {
            EnsureFonts();
            TMP_FontAsset? asset = weight switch
            {
                TextWeight.Display => _display,
                TextWeight.Bold => _bold,
                TextWeight.SemiBold => _semi,
                TextWeight.Medium => _medium,
                _ => _body
            };
            if (asset == null) asset = _body;
            if (asset != null && text.font != asset) text.font = asset;
            // La graisse est portée par le font asset : on retire le faux gras.
            text.fontStyle &= ~FontStyles.Bold;
        }

        /// <summary>
        /// Passe tous les textes sous <paramref name="root"/> sur Inter (ceux qui
        /// étaient en gras passent en Inter SemiBold), sauf ceux déjà en DA.
        /// </summary>
        public static void ApplyFontsRecursive(Transform root)
        {
            EnsureFonts();
            if (_body == null) return;
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in texts)
            {
                if (t.font == _body || t.font == _semi || t.font == _bold || t.font == _display) continue;
                bool bold = (t.fontStyle & FontStyles.Bold) != 0;
                ApplyFont(t, bold ? TextWeight.SemiBold : TextWeight.Regular);
            }
        }

        // ─── Création d'éléments ─────────────────────────────────────────

        /// <summary>Crée un texte TMP à la DA.</summary>
        public static TextMeshProUGUI Text(Transform parent, string name, string content, float size,
            Color color, TextWeight weight = TextWeight.Regular,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            GameObject obj = new(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            ApplyFont(tmp, weight);
            tmp.text = content;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        /// <summary>Crée un rectangle arrondi plein.</summary>
        public static Image Rect(Transform parent, string name, Color color, float radius,
            bool raycast = false)
        {
            GameObject obj = new(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Image img = obj.GetComponent<Image>();
            SetRounded(img, radius);
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        /// <summary>Habille une Image existante en rectangle arrondi plein.</summary>
        public static void SetRounded(Image img, float radius)
        {
            if (radius <= 0.5f)
            {
                img.sprite = null;
                img.type = Image.Type.Simple;
                return;
            }
            img.sprite = Rounded(radius);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.fillCenter = true;
        }

        /// <summary>
        /// Ajoute un contour arrondi (anneau) en enfant, étiré sur le parent —
        /// l'équivalent du <c>box-shadow: inset 0 0 0 Npx</c> de la maquette.
        /// </summary>
        public static Image Outline(RectTransform target, float radius, float thickness, Color color,
            string name = "Outline")
        {
            GameObject obj = new(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(target, false);
            RectTransform rt = (RectTransform)obj.transform;
            Stretch(rt);
            Image img = obj.GetComponent<Image>();
            SetStroke(img, radius, thickness);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static void SetStroke(Image img, float radius, float thickness)
        {
            img.sprite = Stroke(radius, thickness);
            img.type = Image.Type.Sliced;
            img.fillCenter = true;
            img.pixelsPerUnitMultiplier = 1f;
        }

        /// <summary>Crée une image d'icône (teinte via color).</summary>
        public static Image IconImage(Transform parent, string name, UIIcon icon, float size, Color color)
        {
            GameObject obj = new(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            ((RectTransform)obj.transform).sizeDelta = new Vector2(size, size);
            Image img = obj.GetComponent<Image>();
            img.sprite = Icon(icon);
            img.color = color;
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        /// <summary>Crée l'image d'un symbole de couleur (♥ ▲ ★ ♣ ◆ ♠).</summary>
        public static Image SuitImage(Transform parent, string name, CardColor color, float size, Color tint)
        {
            GameObject obj = new(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            ((RectTransform)obj.transform).sizeDelta = new Vector2(size, size);
            Image img = obj.GetComponent<Image>();
            img.sprite = Suit(color);
            img.color = tint;
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        public static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        /// <summary>Ajoute un HorizontalLayoutGroup configuré (pas d'expansion forcée).</summary>
        public static HorizontalLayoutGroup HRow(GameObject obj, float spacing, RectOffset? padding = null,
            TextAnchor align = TextAnchor.MiddleLeft)
        {
            HorizontalLayoutGroup h = obj.GetComponent<HorizontalLayoutGroup>();
            if (h == null) h = obj.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = padding ?? new RectOffset(0, 0, 0, 0);
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return h;
        }

        public static VerticalLayoutGroup VCol(GameObject obj, float spacing, RectOffset? padding = null,
            TextAnchor align = TextAnchor.UpperLeft)
        {
            VerticalLayoutGroup v = obj.GetComponent<VerticalLayoutGroup>();
            if (v == null) v = obj.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding ?? new RectOffset(0, 0, 0, 0);
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = false;
            v.childForceExpandHeight = false;
            return v;
        }

        public static LayoutElement Size(GameObject obj, float w = -1f, float h = -1f, float flex = -1f)
        {
            LayoutElement le = obj.GetComponent<LayoutElement>();
            if (le == null) le = obj.AddComponent<LayoutElement>();
            if (w >= 0f) { le.preferredWidth = w; le.minWidth = w; }
            if (h >= 0f) { le.preferredHeight = h; le.minHeight = h; }
            if (flex >= 0f) le.flexibleWidth = flex;
            return le;
        }

        // ─── Sprites arrondis (générés en 2x pour rester nets sur mobile) ─
        private const float SpriteScale = 2f;
        private static readonly Dictionary<int, Sprite> RoundedCache = new();
        private static readonly Dictionary<long, Sprite> StrokeCache = new();

        /// <summary>Sprite 9-slice d'un rectangle arrondi de rayon donné (unités canvas).</summary>
        public static Sprite Rounded(float radius)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(radius * SpriteScale), 1, 128);
            if (RoundedCache.TryGetValue(r, out Sprite s) && s != null) return s;
            s = BuildRoundedSprite(r, -1f);
            RoundedCache[r] = s;
            return s;
        }

        /// <summary>Sprite 9-slice d'un contour arrondi (épaisseur en unités canvas).</summary>
        public static Sprite Stroke(float radius, float thickness)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(radius * SpriteScale), 1, 128);
            int t = Mathf.Max(1, Mathf.RoundToInt(thickness * SpriteScale));
            long key = ((long)r << 16) | (uint)t;
            if (StrokeCache.TryGetValue(key, out Sprite s) && s != null) return s;
            s = BuildRoundedSprite(r, t);
            StrokeCache[key] = s;
            return s;
        }

        private static Sprite BuildRoundedSprite(int r, float thickness)
        {
            int size = r * 2 + 4;
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = thickness < 0 ? $"Rounded_{r}" : $"Stroke_{r}_{thickness}"
            };
            Color32[] px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distance signée à un rectangle arrondi qui remplit la texture.
                    float px0 = Mathf.Abs(x + 0.5f - half) - (half - r);
                    float py0 = Mathf.Abs(y + 0.5f - half) - (half - r);
                    float ox = Mathf.Max(px0, 0f);
                    float oy = Mathf.Max(py0, 0f);
                    float outside = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(px0, py0), 0f) - r;
                    float a = Mathf.Clamp01(0.5f - outside);
                    if (thickness > 0f)
                    {
                        float inner = Mathf.Clamp01(0.5f - (outside + thickness));
                        a = Mathf.Clamp01(a - inner);
                    }
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            float b = r + 2;
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f * SpriteScale, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        // ─── Rasteriseur de formes (symboles, icônes) ────────────────────
        private static Sprite Raster(string name, int size, Func<float, float, bool> inside)
        {
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = name
            };
            Color32[] px = new Color32[size * size];
            const int ss = 4;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        for (int sx = 0; sx < ss; sx++)
                        {
                            // Coordonnées normalisées [-1,1], y vers le haut.
                            float u = ((x + (sx + 0.5f) / ss) / size) * 2f - 1f;
                            float v = ((y + (sy + 0.5f) / ss) / size) * 2f - 1f;
                            if (inside(u, v)) hits++;
                        }
                    }
                    byte a = (byte)(hits * 255 / (ss * ss));
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static bool InCircle(float x, float y, float cx, float cy, float r)
        {
            float dx = x - cx, dy = y - cy;
            return dx * dx + dy * dy <= r * r;
        }

        private static bool InPolygon(float x, float y, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > y) != (poly[j].y > y)) &&
                    (x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static bool NearSegment(float x, float y, float ax, float ay, float bx, float by, float w)
        {
            float vx = bx - ax, vy = by - ay;
            float t = Mathf.Clamp01(((x - ax) * vx + (y - ay) * vy) / (vx * vx + vy * vy));
            float dx = x - (ax + t * vx), dy = y - (ay + t * vy);
            return dx * dx + dy * dy <= w * w;
        }

        // Coordonnées : viewBox SVG 24×24 de la maquette → [-1,1] (y inversé).
        private static Vector2 V(float sx, float sy) => new(sx / 12f - 1f, 1f - sy / 12f);

        private static readonly Vector2[] StarPoly =
        {
            V(12f, 2.5f), V(14.8f, 8.8f), V(21.7f, 9.5f), V(16.5f, 14.1f), V(18f, 20.9f),
            V(12f, 17.4f), V(6f, 20.9f), V(7.5f, 14.1f), V(2.3f, 9.5f), V(9.2f, 8.8f)
        };
        private static readonly Vector2[] TrianglePoly = { V(12f, 3f), V(22f, 21f), V(2f, 21f) };
        private static readonly Vector2[] DiamondPoly = { V(12f, 2f), V(20f, 12f), V(12f, 22f), V(4f, 12f) };
        private static readonly Vector2[] HeartTip = { V(2.6f, 11.6f), V(21.4f, 11.6f), V(12f, 21.2f) };
        private static readonly Vector2[] SpadeBody = { V(12f, 2.5f), V(20.2f, 12.6f), V(3.8f, 12.6f) };
        private static readonly Vector2[] StemPoly = { V(10.7f, 15.5f), V(13.3f, 15.5f), V(14.6f, 21.5f), V(9.4f, 21.5f) };

        private static readonly Dictionary<CardColor, Sprite> SuitCache = new();

        /// <summary>Symbole de couleur, identique aux SVG de la maquette.</summary>
        public static Sprite Suit(CardColor color)
        {
            if (SuitCache.TryGetValue(color, out Sprite s) && s != null) return s;
            Func<float, float, bool> shape = color switch
            {
                CardColor.Red => (x, y) =>
                    InCircle(x, y, V(7f, 9f).x, V(7f, 9f).y, 0.40f) ||
                    InCircle(x, y, V(17f, 9f).x, V(17f, 9f).y, 0.40f) ||
                    InPolygon(x, y, HeartTip),
                CardColor.Orange => (x, y) => InPolygon(x, y, TrianglePoly),
                CardColor.Yellow => (x, y) => InPolygon(x, y, StarPoly),
                CardColor.Green => (x, y) =>
                    InCircle(x, y, V(12f, 6.9f).x, V(12f, 6.9f).y, 0.36f) ||
                    InCircle(x, y, V(7.6f, 12.6f).x, V(7.6f, 12.6f).y, 0.36f) ||
                    InCircle(x, y, V(16.4f, 12.6f).x, V(16.4f, 12.6f).y, 0.36f) ||
                    InPolygon(x, y, StemPoly),
                CardColor.Blue => (x, y) => InPolygon(x, y, DiamondPoly),
                CardColor.Purple => (x, y) =>
                    InPolygon(x, y, SpadeBody) ||
                    InCircle(x, y, V(8.4f, 13.4f).x, V(8.4f, 13.4f).y, 0.37f) ||
                    InCircle(x, y, V(15.6f, 13.4f).x, V(15.6f, 13.4f).y, 0.37f) ||
                    InPolygon(x, y, StemPoly),
                _ => (x, y) => false
            };
            s = Raster($"Suit_{color}", 128, shape);
            SuitCache[color] = s;
            return s;
        }

        private static readonly Dictionary<UIIcon, Sprite> IconCache = new();

        /// <summary>Icône au trait (stroke ~2px à 24px), identique aux SVG de la maquette.</summary>
        public static Sprite Icon(UIIcon icon)
        {
            if (IconCache.TryGetValue(icon, out Sprite s) && s != null) return s;
            const float w = 0.095f; // demi-épaisseur du trait (≈ 2.2px sur 24)
            Func<float, float, bool> shape = icon switch
            {
                UIIcon.Pause => (x, y) =>
                    (Mathf.Abs(x + 0.33f) <= 0.17f || Mathf.Abs(x - 0.33f) <= 0.17f) && Mathf.Abs(y) <= 0.58f,
                UIIcon.Play => (x, y) => InPolygon(x, y, new[] { V(8f, 5.5f), V(19f, 12f), V(8f, 18.5f) }),
                UIIcon.Check => (x, y) =>
                    NearSegment(x, y, V(5f, 12.5f).x, V(5f, 12.5f).y, V(9.5f, 17f).x, V(9.5f, 17f).y, 0.13f) ||
                    NearSegment(x, y, V(9.5f, 17f).x, V(9.5f, 17f).y, V(19f, 7.5f).x, V(19f, 7.5f).y, 0.13f),
                UIIcon.Close => (x, y) =>
                    NearSegment(x, y, -0.5f, -0.5f, 0.5f, 0.5f, w) || NearSegment(x, y, -0.5f, 0.5f, 0.5f, -0.5f, w),
                UIIcon.Skip => (x, y) =>
                {
                    float r = Mathf.Sqrt(x * x + y * y);
                    return Mathf.Abs(r - 0.75f) <= w || (r <= 0.75f && NearSegment(x, y, -0.53f, 0.53f, 0.53f, -0.53f, w));
                },
                UIIcon.Lock => (x, y) =>
                {
                    bool body = x >= -0.58f && x <= 0.58f && y >= -0.75f && y <= 0.08f;
                    float r = Mathf.Sqrt(x * x + (y - 0.08f) * (y - 0.08f));
                    bool shackle = y >= 0.08f && Mathf.Abs(r - 0.36f) <= w;
                    bool legs = (Mathf.Abs(x + 0.36f) <= w || Mathf.Abs(x - 0.36f) <= w) && y >= 0.0f && y <= 0.1f;
                    return body || shackle || legs;
                },
                UIIcon.Cards => (x, y) =>
                {
                    bool outer = x >= -0.6f && x <= 0.6f && y >= -0.78f && y <= 0.78f;
                    bool inner = x >= -0.6f + 2 * w && x <= 0.6f - 2 * w && y >= -0.78f + 2 * w && y <= 0.78f - 2 * w;
                    return outer && !inner;
                },
                UIIcon.SortValue => (x, y) =>
                    NearSegment(x, y, V(4f, 6f).x, V(4f, 6f).y, V(7f, 6f).x, V(7f, 6f).y, w) ||
                    NearSegment(x, y, V(4f, 12f).x, V(4f, 12f).y, V(10f, 12f).x, V(10f, 12f).y, w) ||
                    NearSegment(x, y, V(4f, 18f).x, V(4f, 18f).y, V(13f, 18f).x, V(13f, 18f).y, w) ||
                    NearSegment(x, y, V(17f, 4f).x, V(17f, 4f).y, V(17f, 20f).x, V(17f, 20f).y, w) ||
                    NearSegment(x, y, V(14f, 17f).x, V(14f, 17f).y, V(17f, 20f).x, V(17f, 20f).y, w) ||
                    NearSegment(x, y, V(20f, 17f).x, V(20f, 17f).y, V(17f, 20f).x, V(17f, 20f).y, w),
                UIIcon.Settings => (x, y) =>
                {
                    float r = Mathf.Sqrt(x * x + y * y);
                    if (r <= 0.26f) return false;
                    if (r <= 0.62f) return true;
                    if (r > 0.92f) return false;
                    float ang = Mathf.Atan2(y, x);
                    // 8 dents de ~22° chacune
                    float sector = Mathf.Repeat(ang + Mathf.PI / 8f, Mathf.PI / 4f) - Mathf.PI / 8f;
                    return Mathf.Abs(sector) <= 0.2f;
                },
                _ => (x, y) => false
            };
            s = Raster($"Icon_{icon}", 96, shape);
            IconCache[icon] = s;
            return s;
        }

        // ─── Cartes : couleurs d'encre et textures de dos ────────────────

        /// <summary>Encre de la maquette par couleur (blanc sur rouge/bleu/violet, sombre sinon).</summary>
        public static Color InkFor(CardColor color) => color switch
        {
            CardColor.Orange => Hex(0x1C0D02),
            CardColor.Yellow => Hex(0x1F1A02),
            CardColor.Green => Hex(0x04170B),
            _ => Color.white
        };

        private static Sprite? _actionBand;

        /// <summary>
        /// Bandeau 6 couleurs des cartes action (120×6, coins hauts arrondis
        /// comme la carte, radius 14). Pré-dessiné : aucun masque nécessaire,
        /// donc correct même quand la carte est inclinée dans l'éventail.
        /// </summary>
        public static Sprite ActionBand
        {
            get
            {
                if (_actionBand != null) return _actionBand;
                const int w = 240, h = 12; // 2x
                const float r = 28f;       // radius 14 en 2x
                Color[] cols =
                {
                    Constants.GetColor(CardColor.Red), Constants.GetColor(CardColor.Orange),
                    Constants.GetColor(CardColor.Yellow), Constants.GetColor(CardColor.Green),
                    Constants.GetColor(CardColor.Blue), Constants.GetColor(CardColor.Purple)
                };
                Texture2D tex = new(w, h, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "ActionBand"
                };
                Color32[] px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                {
                    // y = 0 en bas de la texture ; le haut de la carte est en y = h.
                    float fromTop = h - (y + 0.5f);
                    for (int x = 0; x < w; x++)
                    {
                        float cx = x + 0.5f;
                        float a = 1f;
                        float dx = cx < r ? r - cx : cx > w - r ? cx - (w - r) : 0f;
                        float dy = fromTop < r ? r - fromTop : 0f;
                        if (dx > 0f && dy > 0f)
                        {
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            a = Mathf.Clamp01(r - d + 0.5f);
                        }
                        Color c = cols[Mathf.Min(cols.Length - 1, x * cols.Length / w)];
                        px[y * w + x] = new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255),
                            (byte)(a * 255));
                    }
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                _actionBand = Sprite.Create(tex, new UnityEngine.Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
                return _actionBand;
            }
        }

        private static Sprite? _dotPattern;

        /// <summary>Motif de points du dos "Accent" (tuilable).</summary>
        public static Sprite DotPattern
        {
            get
            {
                if (_dotPattern != null) return _dotPattern;
                const int size = 20; // 10px canvas en 2x
                Texture2D tex = new(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Repeat,
                    name = "DotPattern"
                };
                Color32[] px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - 2.5f, dy = y + 0.5f - 2.5f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(3.0f - d);
                        px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                _dotPattern = Sprite.Create(tex, new UnityEngine.Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), 100f * SpriteScale, 0, SpriteMeshType.FullRect);
                return _dotPattern;
            }
        }

        private static Sprite? _vignette;

        /// <summary>Dégradé radial (centre opaque → bords transparents) pour le halo du tapis.</summary>
        public static Sprite Radial
        {
            get
            {
                if (_vignette != null) return _vignette;
                const int size = 128;
                Texture2D tex = new(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "Radial"
                };
                Color32[] px = new Color32[size * size];
                float c = (size - 1) * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x - c) / c, dy = (y - c) / c;
                        float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                        float a = 1f - Mathf.SmoothStep(0f, 1f, d);
                        px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                _vignette = Sprite.Create(tex, new UnityEngine.Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                return _vignette;
            }
        }

        /// <summary>
        /// Construit un dos de carte "Accent" (maquette) dans <paramref name="parent"/> :
        /// fond #161B22 + points accent, double liseré et monogramme LU.
        /// </summary>
        public static void BuildCardBack(RectTransform parent, float radius = 14f)
        {
            Image bg = parent.GetComponent<Image>();
            if (bg == null) bg = parent.gameObject.AddComponent<Image>();
            SetRounded(bg, radius);
            bg.color = Surface;

            // Les enfants "BackDeco" sont reconstruits si on rappelle la méthode.
            Transform? old = parent.Find("BackDeco");
            if (old != null) UnityEngine.Object.Destroy(old.gameObject);

            // Zone de points en retrait de 6px : reste dans les coins arrondis
            // sans masque (un masque ne suivrait pas une carte inclinée).
            GameObject deco = new("BackDeco", typeof(RectTransform));
            deco.transform.SetParent(parent, false);
            RectTransform drt = (RectTransform)deco.transform;
            Stretch(drt, 6f);

            GameObject dots = new("Dots", typeof(RectTransform), typeof(Image));
            dots.transform.SetParent(deco.transform, false);
            Stretch((RectTransform)dots.transform);
            Image dimg = dots.GetComponent<Image>();
            dimg.sprite = DotPattern;
            dimg.type = Image.Type.Tiled;
            dimg.pixelsPerUnitMultiplier = 1f;
            dimg.color = WithAlpha(AccentLight, 0.35f);
            dimg.raycastTarget = false;

            Outline(parent, radius, 1.5f, WithAlpha(AccentLight, 0.55f), "Edge");
            Image inner = Outline(parent, Mathf.Max(4f, radius - 5f), 1f, WithAlpha(AccentLight, 0.55f), "InnerEdge");
            Stretch(inner.rectTransform, 8f);

            Image mono = Rect(parent, "Monogram", Base, 12f);
            Place(mono.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(48f, 48f));
            Outline(mono.rectTransform, 12f, 1.5f, WithAlpha(AccentLight, 0.55f));
            TextMeshProUGUI lu = Text(mono.transform, "LU", "LU", 18f, AccentPale, TextWeight.Display,
                TextAlignmentOptions.Center);
            Stretch(lu.rectTransform);
        }
    }
}
