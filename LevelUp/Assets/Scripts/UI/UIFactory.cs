using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Fabrique d'éléments UI stylés (boutons, panels, sliders, toggles, textes).
    /// Centralise le look neon sombre du jeu pour garder une cohérence visuelle
    /// entre menus, pause, options et game over — sans aucun asset externe.
    /// </summary>
    public static class UIFactory
    {
        private static Sprite? _roundedSprite;
        private static Sprite? _softShadowSprite;
        private static Sprite? _ringSprite;

        /// <summary>
        /// Sprite carré aux coins arrondis (procédural, caché pour réutilisation).
        /// </summary>
        public static Sprite RoundedSprite
        {
            get
            {
                if (_roundedSprite == null) _roundedSprite = CreateRoundedSprite(64, 18);
                return _roundedSprite;
            }
        }

        /// <summary>
        /// Sprite d'ombre douce (radial blur).
        /// </summary>
        public static Sprite SoftShadowSprite
        {
            get
            {
                if (_softShadowSprite == null) _softShadowSprite = CreateSoftShadowSprite(96);
                return _softShadowSprite;
            }
        }

        /// <summary>
        /// Sprite d'anneau pour bordures glow.
        /// </summary>
        public static Sprite RingSprite
        {
            get
            {
                if (_ringSprite == null) _ringSprite = CreateRingSprite(96, 18);
                return _ringSprite;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  PANEL
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Crée un panel stylé (fond sombre, coins arrondis, bordure optionnelle).
        /// </summary>
        public static GameObject CreatePanel(Transform parent, string name, Color color,
            Vector2 size, bool withBorder = false, Color? borderColor = null)
        {
            GameObject panel = new(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image img = panel.GetComponent<Image>();
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
            img.color = color;

            // withBorder / borderColor conservés pour compat API mais plus rendus :
            // le design s'appuie désormais uniquement sur des surfaces arrondies sans contour.
            _ = withBorder; _ = borderColor;

            return panel;
        }

        /// <summary>
        /// Ajoute une ombre portée soft sous l'élément (pour donner de la profondeur).
        /// </summary>
        public static void AddDropShadow(RectTransform target, float offset = 8f, float alpha = 0.45f)
        {
            if (target == null) return;
            GameObject shadow = new("DropShadow", typeof(RectTransform), typeof(Image));
            shadow.transform.SetParent(target, false);
            shadow.transform.SetAsFirstSibling();

            RectTransform srt = shadow.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.sizeDelta = new Vector2(24f, 24f);
            srt.anchoredPosition = new Vector2(0f, -offset);

            Image simg = shadow.GetComponent<Image>();
            simg.sprite = SoftShadowSprite;
            simg.type = Image.Type.Simple;
            simg.color = new Color(0f, 0f, 0f, alpha);
            simg.raycastTarget = false;
        }

        // ═══════════════════════════════════════════════════════════
        //  TEXT
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Crée un TextMeshProUGUI stylé.
        /// </summary>
        public static TextMeshProUGUI CreateText(Transform parent, string name, string content,
            float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center,
            FontStyles style = FontStyles.Bold)
        {
            GameObject textObj = new(name, typeof(RectTransform));
            textObj.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;

            return tmp;
        }

        // ═══════════════════════════════════════════════════════════
        //  BUTTON
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Crée un bouton stylé avec hover/press animations et feedback lumineux.
        /// </summary>
        public static Button CreateButton(Transform parent, string name, string label,
            Vector2 size, Action onClick, Color? accentColor = null)
        {
            Color accent = accentColor ?? Constants.CardBlue;

            GameObject btnObj = new(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
            img.color = Constants.PanelHighlight;

            // Pas de bordure dure — la teinte + le glow au hover suffisent.

            // Glow extérieur (cache, apparaît au hover)
            GameObject glow = new("Glow", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            glow.transform.SetParent(btnObj.transform, false);
            glow.transform.SetAsFirstSibling();
            RectTransform grt = glow.GetComponent<RectTransform>();
            grt.anchorMin = Vector2.zero;
            grt.anchorMax = Vector2.one;
            grt.sizeDelta = new Vector2(40f, 40f);
            grt.anchoredPosition = Vector2.zero;
            Image gimg = glow.GetComponent<Image>();
            gimg.sprite = SoftShadowSprite;
            gimg.color = new Color(accent.r, accent.g, accent.b, 0.55f);
            gimg.raycastTarget = false;
            CanvasGroup gcg = glow.GetComponent<CanvasGroup>();
            gcg.alpha = 0f;
            gcg.blocksRaycasts = false;

            // Label
            CreateText(btnObj.transform, "Label", label, 26f, Constants.TextPrimary,
                TextAlignmentOptions.Center, FontStyles.Bold);

            // Button
            Button btn = btnObj.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            // Hover / press feedback via event triggers
            EventTrigger trigger = btnObj.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerEnter, _ =>
            {
                img.color = Color.Lerp(Constants.PanelHighlight, accent, 0.32f);
                UITween.FadeTo(btnObj, gcg, 1f, 0.18f);
                UITween.ScaleTo(btnObj, rt, Vector3.one * 1.04f, 0.16f);
            });
            AddTrigger(trigger, EventTriggerType.PointerExit, _ =>
            {
                img.color = Constants.PanelHighlight;
                UITween.FadeTo(btnObj, gcg, 0f, 0.25f);
                UITween.ScaleTo(btnObj, rt, Vector3.one, 0.16f);
            });
            AddTrigger(trigger, EventTriggerType.PointerDown, _ =>
            {
                UITween.ScaleTo(btnObj, rt, Vector3.one * 0.96f, 0.08f);
            });
            AddTrigger(trigger, EventTriggerType.PointerUp, _ =>
            {
                UITween.ScaleTo(btnObj, rt, Vector3.one * 1.04f, 0.12f);
            });

            return btn;
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> callback)
        {
            EventTrigger.Entry entry = new() { eventID = type };
            entry.callback.AddListener(new UnityEngine.Events.UnityAction<BaseEventData>(callback));
            trigger.triggers.Add(entry);
        }

        // ═══════════════════════════════════════════════════════════
        //  SLIDER
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Crée un slider stylé avec label. Le conteneur retourné mesure width x 60.
        /// </summary>
        public static GameObject CreateLabeledSlider(Transform parent, string name, string label,
            float min, float max, float current, float width, Action<float> onValueChanged,
            Color? accentColor = null)
        {
            Color accent = accentColor ?? Constants.CardGreen;

            GameObject container = new(name, typeof(RectTransform));
            container.transform.SetParent(parent, false);
            RectTransform crt = container.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(width, 60f);

            // Label à gauche
            TextMeshProUGUI lbl = CreateText(container.transform, "Label", label, 20f,
                Constants.TextPrimary, TextAlignmentOptions.Left, FontStyles.Bold);
            RectTransform lrt = lbl.rectTransform;
            lrt.anchorMin = new Vector2(0f, 0.5f);
            lrt.anchorMax = new Vector2(0f, 0.5f);
            lrt.pivot = new Vector2(0f, 0.5f);
            lrt.sizeDelta = new Vector2(width * 0.35f, 40f);
            lrt.anchoredPosition = new Vector2(4f, 0f);

            // Valeur à droite
            TextMeshProUGUI valueLabel = CreateText(container.transform, "Value",
                Mathf.RoundToInt(current * 100f) + "%", 18f, accent,
                TextAlignmentOptions.Right, FontStyles.Bold);
            RectTransform vrt = valueLabel.rectTransform;
            vrt.anchorMin = new Vector2(1f, 0.5f);
            vrt.anchorMax = new Vector2(1f, 0.5f);
            vrt.pivot = new Vector2(1f, 0.5f);
            vrt.sizeDelta = new Vector2(60f, 40f);
            vrt.anchoredPosition = new Vector2(-4f, 0f);

            // Slider area au milieu
            GameObject sliderObj = new("Slider", typeof(RectTransform));
            sliderObj.transform.SetParent(container.transform, false);
            RectTransform srt = sliderObj.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0.5f);
            srt.anchorMax = new Vector2(1f, 0.5f);
            srt.sizeDelta = new Vector2(-width * 0.35f - 80f, 22f);
            srt.anchoredPosition = new Vector2((width * 0.35f - 70f) * 0.5f + 20f, 0f);

            Slider slider = sliderObj.AddComponent<Slider>();

            // Background
            GameObject bg = new("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            Image bgImg = bg.GetComponent<Image>();
            bgImg.sprite = RoundedSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Constants.PanelBorder;

            // Fill
            GameObject fillArea = new("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.sizeDelta = new Vector2(-16f, 0f);
            faRt.anchoredPosition = Vector2.zero;

            GameObject fill = new("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fill.GetComponent<Image>();
            fillImg.sprite = RoundedSprite;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = accent;

            // Handle
            GameObject handleArea = new("HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform haRt = handleArea.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.sizeDelta = new Vector2(-16f, 0f);

            GameObject handle = new("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform hRt = handle.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(22f, 30f);
            Image hImg = handle.GetComponent<Image>();
            hImg.sprite = RoundedSprite;
            hImg.type = Image.Type.Sliced;
            hImg.color = Color.Lerp(accent, Color.white, 0.4f);

            slider.fillRect = fillRt;
            slider.handleRect = hRt;
            slider.targetGraphic = hImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = current;
            slider.onValueChanged.AddListener(v =>
            {
                valueLabel.text = Mathf.RoundToInt(v * 100f) + "%";
                onValueChanged?.Invoke(v);
            });

            return container;
        }

        // ═══════════════════════════════════════════════════════════
        //  TOGGLE
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Crée un toggle stylé avec label.
        /// </summary>
        public static GameObject CreateLabeledToggle(Transform parent, string name, string label,
            bool current, float width, Action<bool> onValueChanged, Color? accentColor = null)
        {
            Color accent = accentColor ?? Constants.CardPurple;

            GameObject container = new(name, typeof(RectTransform));
            container.transform.SetParent(parent, false);
            RectTransform crt = container.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(width, 48f);

            TextMeshProUGUI lbl = CreateText(container.transform, "Label", label, 20f,
                Constants.TextPrimary, TextAlignmentOptions.Left, FontStyles.Bold);
            RectTransform lrt = lbl.rectTransform;
            lrt.anchorMin = new Vector2(0f, 0.5f);
            lrt.anchorMax = new Vector2(0f, 0.5f);
            lrt.pivot = new Vector2(0f, 0.5f);
            lrt.sizeDelta = new Vector2(width - 80f, 40f);
            lrt.anchoredPosition = new Vector2(4f, 0f);

            // Toggle switch (rectangle qui bascule)
            GameObject swObj = new("Switch", typeof(RectTransform), typeof(Image), typeof(Toggle));
            swObj.transform.SetParent(container.transform, false);
            RectTransform srt = swObj.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(1f, 0.5f);
            srt.anchorMax = new Vector2(1f, 0.5f);
            srt.pivot = new Vector2(1f, 0.5f);
            srt.sizeDelta = new Vector2(60f, 30f);
            srt.anchoredPosition = new Vector2(-4f, 0f);

            Image bg = swObj.GetComponent<Image>();
            bg.sprite = RoundedSprite;
            bg.type = Image.Type.Sliced;
            bg.color = current ? accent : Constants.PanelBorder;

            // Knob
            GameObject knob = new("Knob", typeof(RectTransform), typeof(Image));
            knob.transform.SetParent(swObj.transform, false);
            RectTransform krt = knob.GetComponent<RectTransform>();
            krt.anchorMin = new Vector2(0f, 0.5f);
            krt.anchorMax = new Vector2(0f, 0.5f);
            krt.pivot = new Vector2(0.5f, 0.5f);
            krt.sizeDelta = new Vector2(24f, 24f);
            krt.anchoredPosition = new Vector2(current ? 46f : 14f, 0f);
            Image kimg = knob.GetComponent<Image>();
            kimg.sprite = RoundedSprite;
            kimg.type = Image.Type.Sliced;
            kimg.color = Color.white;

            Toggle toggle = swObj.GetComponent<Toggle>();
            toggle.isOn = current;
            toggle.transition = Selectable.Transition.None;
            toggle.targetGraphic = bg;
            toggle.onValueChanged.AddListener(v =>
            {
                bg.color = v ? accent : Constants.PanelBorder;
                UITween.MoveTo(swObj, krt, new Vector2(v ? 46f : 14f, 0f), 0.18f);
                onValueChanged?.Invoke(v);
            });

            return container;
        }

        // ═══════════════════════════════════════════════════════════
        //  PROCEDURAL SPRITES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Sprite carré avec coins arrondis (alpha mask), pour panels/boutons.
        /// </summary>
        private static Sprite CreateRoundedSprite(int size, int radius)
        {
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float a = 1f;
                    int ex = Mathf.Max(0, radius - Mathf.Min(x, size - 1 - x));
                    int ey = Mathf.Max(0, radius - Mathf.Min(y, size - 1 - y));
                    if (ex > 0 && ey > 0)
                    {
                        float d = Mathf.Sqrt(ex * ex + ey * ey);
                        a = Mathf.Clamp01(1f - (d - radius + 1f));
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            Vector4 border = new(radius, radius, radius, radius);
            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>
        /// Sprite radial doux (alpha décroît du centre vers les bords).
        /// </summary>
        private static Sprite CreateSoftShadowSprite(int size)
        {
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float center = (size - 1) * 0.5f;
            float maxDist = center;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / maxDist;
                    float dy = (y - center) / maxDist;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Sprite d'anneau (contour arrondi) — pour les bordures de boutons/panels.
        /// </summary>
        private static Sprite CreateRingSprite(int size, int radius)
        {
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            int thickness = 3;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float a = 0f;
                    int minDistEdge = Mathf.Min(x, size - 1 - x, y, size - 1 - y);
                    int ex = Mathf.Max(0, radius - Mathf.Min(x, size - 1 - x));
                    int ey = Mathf.Max(0, radius - Mathf.Min(y, size - 1 - y));

                    if (ex > 0 && ey > 0)
                    {
                        float d = Mathf.Sqrt(ex * ex + ey * ey);
                        float outer = Mathf.Clamp01(1f - (d - radius + 1f));
                        float inner = Mathf.Clamp01(1f - (d - radius + 1f + thickness));
                        a = Mathf.Clamp01(outer - inner);
                    }
                    else if (minDistEdge < thickness)
                    {
                        a = 1f;
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            Vector4 border = new(radius, radius, radius, radius);
            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
    }
}
