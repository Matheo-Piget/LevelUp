using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Options de l'appareil, en deux colonnes :
    ///   SON & AFFICHAGE : volumes, qualité, fluidité, taille de l'interface, tapis, dos de cartes ;
    ///   JEU & ACCESSIBILITÉ : vitesse des bots, aides, vibrations, animations réduites, daltonisme.
    /// Les règles d'une partie se choisissent dans "Nouvelle partie".
    /// Les tapis et dos de cartes se débloquent avec les victoires.
    /// </summary>
    public class OptionsPanel : MonoBehaviour
    {
        private const float PanelWidth = 1120f;
        private const float Inner = PanelWidth - 88f;
        private const float ColumnWidth = (Inner - 40f) / 2f;
        private const float LabelWidth = 150f;
        private const float ControlWidth = ColumnWidth - LabelWidth - 16f;

        private RectTransform? _panel;
        private CanvasGroup? _panelCanvasGroup;
        private GameObject? _veil;
        private bool _isVisible;
        private readonly List<Action> _refreshers = new();

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
            UISkin.VCol(panel.gameObject, 22f, new RectOffset(44, 44, 34, 34), TextAnchor.UpperLeft);
            ContentSizeFitter fit = panel.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _panelCanvasGroup = panel.gameObject.AddComponent<CanvasGroup>();

            // En-tête : OPTIONS + fermer
            GameObject header = UIControls.Row(panel.transform, "Header", Inner, 48f);
            TextMeshProUGUI title = UISkin.Text(header.transform, "Title", "OPTIONS", 34f, UISkin.TextColor,
                TextWeight.Display);
            title.characterSpacing = 2f;
            UISkin.Size(title.gameObject, Inner - 48f - 16f);
            GameScreenLayout.IconButton(header.transform, "BtnX", UIIcon.Close, 20f, "Fermer", Hide);

            GameObject columns = UIControls.Row(panel.transform, "Columns", Inner, -1f, 40f);
            columns.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
            LayoutElement cle = columns.GetComponent<LayoutElement>();
            cle.minHeight = -1f;
            cle.preferredHeight = -1f;

            // ── Colonne gauche : son & affichage ──
            GameObject left = UIControls.Column(columns.transform, "Display", ColumnWidth, 12f);
            UIControls.SectionLabel(left.transform, "SON & AFFICHAGE", ColumnWidth);
            SliderRow(left.transform, "Musique", GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
            SliderRow(left.transform, "Effets", GameSettings.SfxVolume, v => GameSettings.SfxVolume = v);
            Choice(left.transform, "Qualité", new[] { "BAS", "MOYEN", "ÉLEVÉ" },
                () => GameSettings.QualityIndex, v => GameSettings.QualityIndex = v);
            Choice(left.transform, "Fluidité", new[] { "60 I/S", "120 I/S" },
                () => GameSettings.HighFrameRate ? 1 : 0, v => GameSettings.HighFrameRate = v == 1);
            Choice(left.transform, "Langue", new[] { "FRANÇAIS", "ENGLISH" },
                () => LevelUp.Utils.Loc.Language, v => LevelUp.Utils.Loc.Language = v);
            Choice(left.transform, "Interface", new[] { "NORMALE", "GRANDE" },
                () => GameSettings.UiScale, v => GameSettings.UiScale = v);
            CosmeticRow(left.transform, "Tapis", () => GameSettings.Felt, v => GameSettings.Felt = v,
                v => UISkin.FeltCenterFor(v));
            CosmeticRow(left.transform, "Dos de carte", () => GameSettings.CardBack, v => GameSettings.CardBack = v,
                UISkin.CardBackInk);

            // ── Colonne droite : jeu, aides (chacune au choix) & accessibilité ──
            // Les aides qui donnent une information de jeu sont coupées par défaut.
            GameObject right = UIControls.Column(columns.transform, "Game", ColumnWidth, 10f);
            UIControls.SectionLabel(right.transform, "JEU & AIDES", ColumnWidth);
            Choice(right.transform, "Vitesse bots", new[] { "POSÉE", "NORMALE", "RAPIDE" },
                () => GameSettings.BotSpeed, v => GameSettings.BotSpeed = v);
            Switch(right.transform, "HintSteps", "Consigne d'étape",
                "Rappel de ce qu'il reste à faire dans ton tour",
                () => GameSettings.HintSteps, v => GameSettings.HintSteps = v);
            Switch(right.transform, "HintContract", "Détection du contrat",
                "Progression, « contrat prêt », sélection validée",
                () => GameSettings.HintContract, v => GameSettings.HintContract = v);
            Switch(right.transform, "HintPlayable", "Cartes jouables",
                "« + » sur les cartes qui complètent la table",
                () => GameSettings.HintPlayable, v => GameSettings.HintPlayable = v);
            Switch(right.transform, "QuickDiscard", "Défausse en un toucher",
                "Sinon : toucher pour soulever, toucher encore pour jeter",
                () => GameSettings.QuickDiscard, v => GameSettings.QuickDiscard = v);
            Switch(right.transform, "HintCoach", "Bouton « Conseil »",
                "Ce que jouerait un bot Expert à ta place, sur demande",
                () => GameSettings.HintCoach, v => GameSettings.HintCoach = v);
            Switch(left.transform, "Haptics", "Vibrations",
                "Retour tactile sur les actions (téléphone)",
                () => GameSettings.Haptics, v => GameSettings.Haptics = v);
            Switch(right.transform, "Motion", "Animations réduites",
                "Ni flash ni éclats, trajets de cartes plus courts",
                () => GameSettings.ReducedMotion, v => GameSettings.ReducedMotion = v);
            _refreshers.Add(ColorblindCard(right.transform));

            Button close = UIFactory.CreateButton(panel.transform, "BtnClose", "FERMER",
                new Vector2(Inner, 56f), Hide, primary: true);
            UISkin.Size(close.gameObject, Inner, 56f);
            TextMeshProUGUI? closeLabel = close.GetComponentInChildren<TextMeshProUGUI>();
            if (closeLabel != null) closeLabel.fontSize = 18f;

            _panelCanvasGroup.alpha = 0f;
            _veil.SetActive(false);
        }

        // ─── Briques ────────────────────────────────────────────────────

        private void Choice(Transform parent, string label, string[] options, Func<int> get, Action<int> set)
        {
            GameObject row = UIControls.Row(parent, label + "Row", ColumnWidth, 50f);
            UIControls.RowLabel(row.transform, label, LabelWidth);
            _refreshers.Add(UIControls.Segmented(row.transform, "Segmented", options, ControlWidth, get, set, 50f));
        }

        private void Switch(Transform parent, string name, string title, string description,
            Func<bool> get, Action<bool> set)
        {
            _refreshers.Add(UIControls.SwitchCard(parent, name, title, description, ColumnWidth, get, set,
                height: SwitchHeight));
        }

        private const float SwitchHeight = 60f;

        /// <summary>Label | piste 8px accent + poignée ronde | valeur %.</summary>
        private static void SliderRow(Transform parent, string label, float value, Action<float> onChanged)
        {
            GameObject row = UIControls.Row(parent, label + "Row", ColumnWidth, 44f);
            UIControls.RowLabel(row.transform, label, LabelWidth);

            GameObject sliderObj = new("Slider", typeof(RectTransform));
            sliderObj.transform.SetParent(row.transform, false);
            UISkin.Size(sliderObj, ControlWidth - 52f - 16f, 44f);

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

        /// <summary>
        /// Trois pastilles de couleur (variantes) ; celles qui ne sont pas encore
        /// débloquées portent un cadenas et le nombre de victoires requis.
        /// </summary>
        private void CosmeticRow(Transform parent, string label, Func<int> get, Action<int> set,
            Func<int, Color> swatch)
        {
            GameObject row = UIControls.Row(parent, label + "Row", ColumnWidth, 50f);
            UIControls.RowLabel(row.transform, label, LabelWidth);

            GameObject group = new("Swatches", typeof(RectTransform));
            group.transform.SetParent(row.transform, false);
            UISkin.HRow(group, 10f, null, TextAnchor.MiddleLeft);
            UISkin.Size(group, ControlWidth, 50f);

            int count = GameSettings.CosmeticUnlockWins.Length;
            Image[] rings = new Image[count];
            GameObject[] locks = new GameObject[count];
            TextMeshProUGUI? note = null;

            void Refresh()
            {
                int active = get();
                for (int i = 0; i < count; i++)
                {
                    bool unlocked = GameSettings.IsCosmeticUnlocked(i);
                    rings[i].color = i == active ? UISkin.Accent : UISkin.Line;
                    UISkin.SetStroke(rings[i], 12f, i == active ? 2.5f : 1f);
                    locks[i].SetActive(!unlocked);
                }
                if (note != null)
                {
                    int wins = PlayerProfile.GamesWon;
                    int next = -1;
                    foreach (int need in GameSettings.CosmeticUnlockWins)
                    {
                        if (need > wins) { next = need; break; }
                    }
                    note.text = next < 0 ? "Tout est débloqué" : $"{wins}/{next} victoires";
                }
            }

            for (int i = 0; i < count; i++)
            {
                int idx = i;
                Image chip = UISkin.Rect(group.transform, $"Swatch{i}", swatch(i), 12f, raycast: true);
                UISkin.Size(chip.gameObject, 64f, 40f);
                rings[i] = UISkin.Outline(chip.rectTransform, 12f, 1f, UISkin.Line);
                UISkin.Stretch(rings[i].rectTransform, -3f);

                GameObject lockObj = new("Lock", typeof(RectTransform));
                lockObj.transform.SetParent(chip.transform, false);
                UISkin.Stretch((RectTransform)lockObj.transform);
                Image dim = lockObj.AddComponent<Image>();
                UISkin.SetRounded(dim, 12f);
                dim.color = UISkin.WithAlpha(UISkin.Ground, 0.65f);
                dim.raycastTarget = false;
                Image icon = UISkin.IconImage(lockObj.transform, "Icon", UIIcon.Lock, 16f, UISkin.TextColor);
                UISkin.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(16f, 16f));
                locks[i] = lockObj;

                Button btn = chip.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() =>
                {
                    if (!GameSettings.IsCosmeticUnlocked(idx)) return;
                    set(idx);
                    Refresh();
                });
            }

            note = UISkin.Text(group.transform, "Note", "", 13f, UISkin.TextMuted, TextWeight.Medium);
            UISkin.Size(note.gameObject, -1f, -1f, 1f);

            Refresh();
            _refreshers.Add(Refresh);
        }

        /// <summary>Carte "Mode daltonisme" + légende des symboles + interrupteur.</summary>
        private static Action ColorblindCard(Transform parent)
        {
            return UIControls.SwitchCard(parent, "Colorblind", "Symboles renforcés (daltonisme)", "", ColumnWidth,
                () => GameSettings.ColorblindMode, v => GameSettings.ColorblindMode = v, height: SwitchHeight, extra: texts =>
                {
                    GameObject legend = new("Legend", typeof(RectTransform));
                    legend.transform.SetParent(texts, false);
                    UISkin.HRow(legend, 6f, null, TextAnchor.MiddleLeft);
                    UISkin.Text(legend.transform, "Lbl", "Symbole", 13f, UISkin.TextMuted);
                    CardColor[] order =
                    {
                        CardColor.Red, CardColor.Orange, CardColor.Yellow, CardColor.Green, CardColor.Blue,
                        CardColor.Purple
                    };
                    foreach (CardColor c in order)
                    {
                        Image s = UISkin.SuitImage(legend.transform, c.ToString(), c, 13f, UISkin.TextMuted);
                        UISkin.Size(s.gameObject, 13f, 13f);
                    }
                    UISkin.Text(legend.transform, "Lbl2", "par couleur", 13f, UISkin.TextMuted);
                });
        }

        // ─── Affichage ──────────────────────────────────────────────────

        public void Show()
        {
            if (_panel == null || _panelCanvasGroup == null || _veil == null) return;
            if (_isVisible) return;
            _isVisible = true;
            UIBackStack.Push(this, Hide);
            foreach (Action refresh in _refreshers) refresh();

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
            UIBackStack.Remove(this);
            GameSettings.Flush();

            UITween.ScaleTo(_panel.gameObject, _panel, Vector3.one * 0.96f, 0.16f);
            UITween.FadeTo(_panel.gameObject, _panelCanvasGroup, 0f, 0.16f, () =>
            {
                if (_veil != null) _veil.SetActive(false);
            });
        }
    }
}
