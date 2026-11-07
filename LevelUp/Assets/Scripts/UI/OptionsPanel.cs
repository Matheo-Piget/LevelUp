using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Options (maquette "Options") : panneau 720px, volumes musique/SFX,
    /// contrôles segmentés (qualité, joueurs, bots, contrats), interrupteur
    /// daltonisme, bouton FERMER. Lié à <see cref="GameSettings"/> (persistant).
    /// </summary>
    public class OptionsPanel : MonoBehaviour
    {
        private const float PanelWidth = 720f;
        private const float ContentWidth = PanelWidth - 88f;

        private RectTransform? _panel;
        private CanvasGroup? _panelCanvasGroup;
        private GameObject? _veil;
        private bool _isVisible;

        public void Setup(RectTransform parent)
        {
            if (parent == null) return;
            GameSettings.Initialize();
            UISkin.EnsureFonts();

            // Voile : tap à l'extérieur = fermer
            _veil = new GameObject("OptionsVeil", typeof(RectTransform), typeof(Image), typeof(Button));
            _veil.transform.SetParent(parent, false);
            UISkin.Stretch((RectTransform)_veil.transform);
            _veil.GetComponent<Image>().color = new Color(0.04f, 0.055f, 0.07f, 0.86f);
            Button vBtn = _veil.GetComponent<Button>();
            vBtn.transition = Selectable.Transition.None;
            vBtn.onClick.AddListener(Hide);

            Image panel = UISkin.Rect(_veil.transform, "OptionsPanel", UISkin.Surface, 28f, raycast: true);
            _panel = panel.rectTransform;
            UISkin.Place(_panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(PanelWidth, 0f));
            UISkin.Outline(_panel, 28f, 1f, new Color(1f, 1f, 1f, 0.08f))
                .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UISkin.VCol(panel.gameObject, 28f, new RectOffset(44, 44, 40, 40), TextAnchor.UpperLeft);
            ContentSizeFitter fit = panel.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _panelCanvasGroup = panel.gameObject.AddComponent<CanvasGroup>();

            // En-tête : OPTIONS + fermer
            GameObject header = Row(panel.transform, "Header", 48f);
            TextMeshProUGUI title = UISkin.Text(header.transform, "Title", "OPTIONS", 34f, UISkin.TextColor,
                TextWeight.Display);
            title.characterSpacing = 2f;
            UISkin.Size(title.gameObject, ContentWidth - 48f - 16f);
            GameScreenLayout.IconButton(header.transform, "BtnX", UIIcon.Close, 20f, "Fermer", Hide);

            // Volumes
            GameObject volumes = Column(panel.transform, "Volumes", 18f);
            SliderRow(volumes.transform, "Musique", GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
            SliderRow(volumes.transform, "Effets SFX", GameSettings.SfxVolume, v => GameSettings.SfxVolume = v);

            // Choix segmentés
            ChoiceRow(panel.transform, "Qualité", new[] { "BAS", "MOYEN", "ELEVE" },
                () => GameSettings.QualityIndex, v => GameSettings.QualityIndex = v);
            ChoiceRow(panel.transform, "Joueurs", new[] { "2", "3", "4" },
                () => GameSettings.PlayerCount - 2, v => GameSettings.PlayerCount = v + 2);
            ChoiceRow(panel.transform, "Bots", new[] { "FACILE", "NORMAL", "EXPERT" },
                () => GameSettings.Difficulty, v => GameSettings.Difficulty = v);
            ChoiceRow(panel.transform, "Contrats", new[] { "FIXES", "VARIÉS" },
                () => GameSettings.RandomContracts ? 1 : 0, v => GameSettings.RandomContracts = v == 1);

            ColorblindCard(panel.transform);

            TextMeshProUGUI note = UISkin.Text(panel.transform, "MatchNote",
                "Joueurs, bots et contrats : appliqués à la prochaine partie", 13f, UISkin.TextMuted);
            UISkin.Size(note.gameObject, ContentWidth, 18f);

            Button close = UIFactory.CreateButton(panel.transform, "BtnClose", "FERMER",
                new Vector2(ContentWidth, 56f), Hide, primary: true);
            UISkin.Size(close.gameObject, ContentWidth, 56f);
            TextMeshProUGUI? closeLabel = close.GetComponentInChildren<TextMeshProUGUI>();
            if (closeLabel != null) closeLabel.fontSize = 18f;

            _panelCanvasGroup.alpha = 0f;
            _veil.SetActive(false);
        }

        // ─── Briques ────────────────────────────────────────────────────

        private static GameObject Row(Transform parent, string name, float height)
        {
            GameObject row = new(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            UISkin.HRow(row, 16f, null, TextAnchor.MiddleLeft);
            UISkin.Size(row, ContentWidth, height);
            return row;
        }

        private static GameObject Column(Transform parent, string name, float spacing)
        {
            GameObject col = new(name, typeof(RectTransform));
            col.transform.SetParent(parent, false);
            UISkin.VCol(col, spacing, null, TextAnchor.UpperLeft);
            UISkin.Size(col, ContentWidth);
            return col;
        }

        private static TextMeshProUGUI RowLabel(Transform row, string text)
        {
            TextMeshProUGUI lbl = UISkin.Text(row, "Label", text, 16f, UISkin.TextColor, TextWeight.SemiBold);
            UISkin.Size(lbl.gameObject, 160f);
            return lbl;
        }

        /// <summary>Label | piste 8px accent + poignée ronde | valeur %.</summary>
        private static void SliderRow(Transform parent, string label, float value, Action<float> onChanged)
        {
            GameObject row = Row(parent, label + "Row", 44f);
            RowLabel(row.transform, label);

            GameObject sliderObj = new("Slider", typeof(RectTransform));
            sliderObj.transform.SetParent(row.transform, false);
            UISkin.Size(sliderObj, ContentWidth - 160f - 52f - 32f, 44f);

            Image track = UISkin.Rect(sliderObj.transform, "Track", UISkin.SurfaceRaised, 4f);
            RectTransform trt = track.rectTransform;
            trt.anchorMin = new Vector2(0f, 0.5f);
            trt.anchorMax = new Vector2(1f, 0.5f);
            trt.sizeDelta = new Vector2(0f, 8f);

            GameObject fillArea = new("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fart = (RectTransform)fillArea.transform;
            fart.anchorMin = new Vector2(0f, 0.5f);
            fart.anchorMax = new Vector2(1f, 0.5f);
            fart.sizeDelta = new Vector2(0f, 8f);
            Image fill = UISkin.Rect(fillArea.transform, "Fill", UISkin.Accent, 4f);
            UISkin.Stretch(fill.rectTransform);

            // Zone de la poignée : bande de 24px centrée (le Slider étire la
            // poignée sur la hauteur de cette zone → rond de 24×24).
            GameObject handleArea = new("HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform hart = (RectTransform)handleArea.transform;
            hart.anchorMin = new Vector2(0f, 0.5f);
            hart.anchorMax = new Vector2(1f, 0.5f);
            hart.pivot = new Vector2(0.5f, 0.5f);
            hart.sizeDelta = new Vector2(-24f, 24f);
            hart.anchoredPosition = Vector2.zero;
            Image handle = UISkin.Rect(handleArea.transform, "Handle", Color.white, 12f, raycast: true);
            handle.rectTransform.sizeDelta = new Vector2(24f, 0f);

            // Zone de toucher pleine hauteur (44px) pour le tactile
            Image hit = sliderObj.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;

            TextMeshProUGUI valueText = UISkin.Text(row.transform, "Value", Pct(value), 15f, UISkin.TextMuted,
                TextWeight.SemiBold, TextAlignmentOptions.MidlineRight);
            UISkin.Size(valueText.gameObject, 52f);

            slider.onValueChanged.AddListener(v =>
            {
                valueText.text = Pct(v);
                onChanged(v);
            });
        }

        private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";

        /// <summary>Label | contrôle segmenté (fond #0F1419, actif champagne plein).</summary>
        private static void ChoiceRow(Transform parent, string label, string[] options,
            Func<int> getIndex, Action<int> setIndex)
        {
            GameObject row = Row(parent, label + "Row", 52f);
            RowLabel(row.transform, label);

            Image seg = UISkin.Rect(row.transform, "Segmented", UISkin.Base, 14f);
            UISkin.Size(seg.gameObject, ContentWidth - 160f - 16f, 52f);
            UISkin.HRow(seg.gameObject, 4f, new RectOffset(4, 4, 4, 4), TextAnchor.MiddleCenter);
            HorizontalLayoutGroup h = seg.GetComponent<HorizontalLayoutGroup>();
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            UISkin.Outline(seg.rectTransform, 14f, 1f, UISkin.Line).gameObject.AddComponent<LayoutElement>()
                .ignoreLayout = true;

            Image[] bgs = new Image[options.Length];
            TextMeshProUGUI[] labels = new TextMeshProUGUI[options.Length];

            void Refresh()
            {
                int active = getIndex();
                for (int i = 0; i < bgs.Length; i++)
                {
                    bool on = i == active;
                    bgs[i].color = on ? UISkin.Accent : new Color(0f, 0f, 0f, 0f);
                    labels[i].color = on ? UISkin.OnAccent : UISkin.TextMuted;
                }
            }

            for (int i = 0; i < options.Length; i++)
            {
                int idx = i;
                Image b = UISkin.Rect(seg.transform, $"Opt{i}", new Color(0f, 0f, 0f, 0f), 10f, raycast: true);
                UISkin.Size(b.gameObject, -1f, 44f, 1f);
                TextMeshProUGUI t = UISkin.Text(b.transform, "Label", options[i], 14f, UISkin.TextMuted,
                    TextWeight.Bold, TextAlignmentOptions.Center);
                t.characterSpacing = 6f;
                UISkin.Stretch(t.rectTransform);
                Button btn = b.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() =>
                {
                    setIndex(idx);
                    Refresh();
                });
                bgs[i] = b;
                labels[i] = t;
            }
            Refresh();
        }

        /// <summary>Carte "Mode daltonisme (icônes)" + interrupteur 60×34.</summary>
        private static void ColorblindCard(Transform parent)
        {
            Image card = UISkin.Rect(parent, "Colorblind", UISkin.Base, 16f);
            UISkin.Size(card.gameObject, ContentWidth, 76f);
            UISkin.HRow(card.gameObject, 16f, new RectOffset(18, 18, 14, 14), TextAnchor.MiddleLeft);
            UISkin.Outline(card.rectTransform, 16f, 1f, UISkin.Line).gameObject.AddComponent<LayoutElement>()
                .ignoreLayout = true;

            GameObject texts = new("Texts", typeof(RectTransform));
            texts.transform.SetParent(card.transform, false);
            UISkin.VCol(texts, 4f, null, TextAnchor.MiddleLeft);
            UISkin.Size(texts, ContentWidth - 36f - 60f - 16f);
            UISkin.Text(texts.transform, "Title", "Mode daltonisme (icônes)", 16f, UISkin.TextColor, TextWeight.SemiBold);

            GameObject legend = new("Legend", typeof(RectTransform));
            legend.transform.SetParent(texts.transform, false);
            UISkin.HRow(legend, 6f, null, TextAnchor.MiddleLeft);
            UISkin.Text(legend.transform, "Lbl", "Symbole", 13f, UISkin.TextMuted);
            CardColor[] order =
            {
                CardColor.Red, CardColor.Orange, CardColor.Yellow, CardColor.Green, CardColor.Blue, CardColor.Purple
            };
            foreach (CardColor c in order)
            {
                Image s = UISkin.SuitImage(legend.transform, c.ToString(), c, 13f, UISkin.TextMuted);
                UISkin.Size(s.gameObject, 13f, 13f);
            }
            UISkin.Text(legend.transform, "Lbl2", "sur chaque carte", 13f, UISkin.TextMuted);

            // Interrupteur
            Image track = UISkin.Rect(card.transform, "Switch", UISkin.SurfaceRaised, 17f, raycast: true);
            UISkin.Size(track.gameObject, 60f, 34f);
            Image knob = UISkin.Rect(track.transform, "Knob", Color.white, 13f);
            UISkin.Place(knob.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f),
                new Vector2(26f, 26f));

            void Apply(bool on, bool animate)
            {
                track.color = on ? UISkin.Accent : UISkin.SurfaceRaised;
                Vector2 target = new(on ? 30f : 4f, 0f);
                if (animate) UITween.MoveTo(track.gameObject, knob.rectTransform, target, 0.15f);
                else knob.rectTransform.anchoredPosition = target;
            }

            Button btn = track.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                GameSettings.ColorblindMode = !GameSettings.ColorblindMode;
                Apply(GameSettings.ColorblindMode, true);
            });
            Apply(GameSettings.ColorblindMode, false);
        }

        // ─── Affichage ──────────────────────────────────────────────────

        public void Show()
        {
            if (_panel == null || _panelCanvasGroup == null || _veil == null) return;
            if (_isVisible) return;
            _isVisible = true;

            _veil.SetActive(true);
            _veil.transform.SetAsLastSibling();
            _panel.localScale = Vector3.one * 0.94f;
            _panelCanvasGroup.alpha = 0f;
            UITween.ScaleTo(_panel.gameObject, _panel, Vector3.one, 0.22f);
            UITween.FadeTo(_panel.gameObject, _panelCanvasGroup, 1f, 0.18f);
        }

        public void Hide()
        {
            if (_panel == null || _panelCanvasGroup == null || _veil == null) return;
            if (!_isVisible) return;
            _isVisible = false;

            UITween.ScaleTo(_panel.gameObject, _panel, Vector3.one * 0.96f, 0.16f);
            UITween.FadeTo(_panel.gameObject, _panelCanvasGroup, 0f, 0.16f, () =>
            {
                if (_veil != null) _veil.SetActive(false);
            });
        }
    }
}
