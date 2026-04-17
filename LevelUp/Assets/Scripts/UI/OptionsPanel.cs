using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Panel d'options : volume musique/SFX, qualité graphique, daltonisme.
    /// Lie ses sliders/toggles à <see cref="GameSettings"/> qui persiste les valeurs.
    /// </summary>
    public class OptionsPanel : MonoBehaviour
    {
        private RectTransform? _panel;
        private CanvasGroup? _panelCanvasGroup;
        private bool _isVisible;

        /// <summary>
        /// Construit le panel sous le parent donné. Masqué par défaut, Show() pour afficher.
        /// </summary>
        public void Setup(RectTransform parent)
        {
            if (parent == null) return;
            GameSettings.Initialize();

            // Veil sombre qui capture les clics en arrière
            GameObject veil = new("OptionsVeil", typeof(RectTransform), typeof(Image), typeof(Button));
            veil.transform.SetParent(parent, false);
            RectTransform vrt = veil.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.sizeDelta = Vector2.zero;
            Image vImg = veil.GetComponent<Image>();
            vImg.color = new Color(0f, 0f, 0f, 0.65f);
            Button vBtn = veil.GetComponent<Button>();
            vBtn.transition = Selectable.Transition.None;
            vBtn.onClick.AddListener(Hide);

            // Panel central — hauteur augmentée pour laisser de la place au bouton FERMER
            // sans qu'il chevauche les sliders/toggles au-dessus.
            GameObject panelObj = UIFactory.CreatePanel(veil.transform, "OptionsPanel",
                Constants.PanelBackground, new Vector2(580f, 640f),
                withBorder: true, borderColor: Constants.CardPurple);
            _panel = panelObj.GetComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.anchoredPosition = Vector2.zero;
            _panelCanvasGroup = panelObj.AddComponent<CanvasGroup>();
            // Bloque tous les clics qui ne touchent pas les contrôles (pour ne pas
            // que le veil derrière ferme le panel quand on clique entre deux éléments).
            Image panelBlocker = panelObj.GetComponent<Image>();
            if (panelBlocker != null) panelBlocker.raycastTarget = true;

            UIFactory.AddDropShadow(_panel, 10f, 0.5f);

            // Titre
            TextMeshProUGUI title = UIFactory.CreateText(_panel, "Title", "OPTIONS", 38f,
                Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform trt = title.rectTransform;
            trt.anchorMin = new Vector2(0.5f, 1f);
            trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(500f, 60f);
            trt.anchoredPosition = new Vector2(0f, -30f);
            title.characterSpacing = 14f;
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(
                Color.Lerp(Constants.CardPurple, Color.white, 0.3f),
                Color.Lerp(Constants.CardPurple, Color.white, 0.3f),
                Color.Lerp(Constants.CardBlue, Color.white, 0.2f),
                Color.Lerp(Constants.CardBlue, Color.white, 0.2f));

            // Séparateur
            GameObject sep = new("Separator", typeof(RectTransform), typeof(Image));
            sep.transform.SetParent(_panel, false);
            RectTransform srt = sep.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 1f);
            srt.anchorMax = new Vector2(0.5f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.sizeDelta = new Vector2(460f, 2f);
            srt.anchoredPosition = new Vector2(0f, -95f);
            Image sImg = sep.GetComponent<Image>();
            sImg.color = new Color(Constants.CardPurple.r, Constants.CardPurple.g, Constants.CardPurple.b, 0.5f);
            sImg.raycastTarget = false;

            // Contenu en VerticalLayout
            GameObject content = new("Content", typeof(RectTransform));
            content.transform.SetParent(_panel, false);
            RectTransform crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 1f);
            crt.anchorMax = new Vector2(0.5f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = new Vector2(500f, 340f);
            crt.anchoredPosition = new Vector2(0f, -120f);

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            // Sliders volume
            UIFactory.CreateLabeledSlider(content.transform, "MusicVol", "Musique",
                0f, 1f, GameSettings.MusicVolume, 500f,
                v => GameSettings.MusicVolume = v, Constants.CardBlue);

            UIFactory.CreateLabeledSlider(content.transform, "SfxVol", "Effets SFX",
                0f, 1f, GameSettings.SfxVolume, 500f,
                v => GameSettings.SfxVolume = v, Constants.CardGreen);

            // Qualité : 3 boutons radio
            CreateQualityRow(content.transform);

            // Toggle daltonisme
            UIFactory.CreateLabeledToggle(content.transform, "Colorblind",
                "Mode daltonisme (icônes)", GameSettings.ColorblindMode, 500f,
                v => GameSettings.ColorblindMode = v, Constants.CardOrange);

            // Bouton fermer ancré en bas du panel — SetAsLastSibling() pour être
            // sûr d'être au-dessus du contenu et de recevoir les clics.
            Button closeBtn = UIFactory.CreateButton(_panel, "BtnClose", "FERMER",
                new Vector2(240f, 60f), Hide, Constants.CardRed);
            RectTransform closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 30f);
            closeBtn.transform.SetAsLastSibling();

            // Masqué par défaut
            _panel.localScale = Vector3.zero;
            _panelCanvasGroup.alpha = 0f;
            veil.SetActive(false);
        }

        private void CreateQualityRow(Transform parent)
        {
            GameObject row = new("QualityRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rrt = row.GetComponent<RectTransform>();
            rrt.sizeDelta = new Vector2(460f, 60f);

            TextMeshProUGUI lbl = UIFactory.CreateText(row.transform, "Label", "Qualité", 20f,
                Constants.TextPrimary, TextAlignmentOptions.Left, FontStyles.Bold);
            RectTransform lrt = lbl.rectTransform;
            lrt.anchorMin = new Vector2(0f, 0.5f);
            lrt.anchorMax = new Vector2(0f, 0.5f);
            lrt.pivot = new Vector2(0f, 0.5f);
            lrt.sizeDelta = new Vector2(160f, 40f);
            lrt.anchoredPosition = new Vector2(4f, 0f);

            string[] labels = { "BAS", "MOYEN", "ELEVE" };
            Button[] buttons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                Button btn = UIFactory.CreateButton(row.transform, $"Q{i}", labels[i],
                    new Vector2(90f, 44f), () => SelectQuality(idx, buttons), Constants.CardPurple);
                RectTransform brt = btn.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0f, 0.5f);
                brt.anchorMax = new Vector2(0f, 0.5f);
                brt.pivot = new Vector2(0f, 0.5f);
                brt.anchoredPosition = new Vector2(170f + i * 98f, 0f);
                buttons[i] = btn;
            }

            RefreshQualityButtons(buttons);
        }

        private void SelectQuality(int index, Button[] buttons)
        {
            GameSettings.QualityIndex = index;
            RefreshQualityButtons(buttons);
        }

        private void RefreshQualityButtons(Button[] buttons)
        {
            int active = GameSettings.QualityIndex;
            for (int i = 0; i < buttons.Length; i++)
            {
                Image? bg = buttons[i].GetComponent<Image>();
                if (bg == null) continue;
                bg.color = (i == active)
                    ? Color.Lerp(Constants.PanelHighlight, Constants.CardPurple, 0.45f)
                    : Constants.PanelHighlight;
            }
        }

        /// <summary>
        /// Affiche le panel avec une animation de pop-in.
        /// </summary>
        public void Show()
        {
            if (_panel == null || _panelCanvasGroup == null) return;
            if (_isVisible) return;

            _isVisible = true;

            // Activer le veil parent
            Transform? veil = _panel.parent;
            if (veil != null) veil.gameObject.SetActive(true);

            _panel.localScale = Vector3.zero;
            _panelCanvasGroup.alpha = 0f;
            UITween.ScaleTo(_panel.gameObject, _panel, Vector3.one, 0.35f);
            UITween.FadeTo(_panel.gameObject, _panelCanvasGroup, 1f, 0.25f);
        }

        /// <summary>
        /// Masque le panel avec une animation.
        /// </summary>
        public void Hide()
        {
            if (_panel == null || _panelCanvasGroup == null) return;
            if (!_isVisible) return;

            _isVisible = false;

            UITween.ScaleTo(_panel.gameObject, _panel, Vector3.zero, 0.22f);
            UITween.FadeTo(_panel.gameObject, _panelCanvasGroup, 0f, 0.2f, () =>
            {
                Transform? veil = _panel.parent;
                if (veil != null) veil.gameObject.SetActive(false);
            });
        }
    }
}
