using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Menu pause overlay — s'ouvre/ferme avec ESC pendant le jeu.
    /// Met le jeu en pause (Time.timeScale = 0) et propose Reprendre / Options / Menu principal.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        private Canvas? _canvas;
        private RectTransform? _root;
        private CanvasGroup? _rootCanvasGroup;
        private RectTransform? _panel;
        private OptionsPanel? _optionsPanel;
        private Button? _pauseButton;
        private bool _isPaused;
        // La pause n'a de sens qu'en jeu : verrouillée sur le menu principal
        // (sinon ESC rouvrirait l'overlay par-dessus le menu).
        private bool _pauseAllowed = true;

        /// <summary>Déclenché quand le joueur choisit "Menu principal".</summary>
        public event Action? OnMainMenuRequested;

        /// <summary>Indique si la pause est active.</summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Construit le menu pause sous le canvas. Masqué au démarrage.
        /// </summary>
        public void Setup(Canvas canvas)
        {
            if (canvas == null) return;
            _canvas = canvas;

            GameObject rootObj = new("PauseMenuOverlay", typeof(RectTransform), typeof(CanvasGroup));
            rootObj.transform.SetParent(canvas.transform, false);
            rootObj.transform.SetAsLastSibling();

            _root = rootObj.GetComponent<RectTransform>();
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.sizeDelta = Vector2.zero;

            _rootCanvasGroup = rootObj.GetComponent<CanvasGroup>();
            _rootCanvasGroup.alpha = 0f;
            _rootCanvasGroup.blocksRaycasts = false;

            // Veil
            GameObject veil = new("Veil", typeof(RectTransform), typeof(Image), typeof(Button));
            veil.transform.SetParent(_root, false);
            RectTransform vrt = veil.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.sizeDelta = Vector2.zero;
            Image vImg = veil.GetComponent<Image>();
            vImg.color = new Color(0f, 0f, 0f, 0.7f);
            Button veilBtn = veil.GetComponent<Button>();
            veilBtn.transition = Selectable.Transition.None;
            // Tap en dehors du panneau = reprendre (essentiel en tactile)
            veilBtn.onClick.AddListener(Hide);

            // Panel
            GameObject panelObj = UIFactory.CreatePanel(_root, "PausePanel",
                Constants.PanelBackground, new Vector2(420f, 520f),
                withBorder: true, borderColor: Constants.CardYellow);
            _panel = panelObj.GetComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.anchoredPosition = Vector2.zero;

            UIFactory.AddDropShadow(_panel, 10f, 0.5f);

            // Titre
            TextMeshProUGUI title = UIFactory.CreateText(_panel, "Title", "PAUSE", 48f,
                Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform trt = title.rectTransform;
            trt.anchorMin = new Vector2(0.5f, 1f);
            trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(360f, 70f);
            trt.anchoredPosition = new Vector2(0f, -40f);
            title.characterSpacing = 18f;
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(
                Color.Lerp(Constants.CardYellow, Color.white, 0.3f),
                Color.Lerp(Constants.CardYellow, Color.white, 0.3f),
                Color.Lerp(Constants.CardOrange, Color.white, 0.2f),
                Color.Lerp(Constants.CardOrange, Color.white, 0.2f));

            // Colonne de boutons
            GameObject col = new("Buttons", typeof(RectTransform));
            col.transform.SetParent(_panel, false);
            RectTransform crt = col.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(320f, 260f);
            crt.anchoredPosition = new Vector2(0f, -40f);

            VerticalLayoutGroup vlg = col.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            UIFactory.CreateButton(col.transform, "BtnResume", "REPRENDRE",
                new Vector2(300f, 64f), Hide, Constants.CardGreen);

            UIFactory.CreateButton(col.transform, "BtnOptions", "OPTIONS",
                new Vector2(300f, 64f), OpenOptions, Constants.CardPurple);

            UIFactory.CreateButton(col.transform, "BtnMenu", "MENU PRINCIPAL",
                new Vector2(300f, 64f), HandleMainMenu, Constants.CardRed);

            _root.gameObject.SetActive(false);

            CreatePauseButton(canvas);
        }

        /// <summary>
        /// Bouton pause flottant en haut à droite — indispensable en tactile
        /// (ESC / bouton Retour Android restent actifs en parallèle).
        /// Placé sous la safe area pour éviter punch-hole et coins arrondis.
        /// </summary>
        private void CreatePauseButton(Canvas canvas)
        {
            Transform parent = canvas.transform;
            SafeAreaFitter? fitter = canvas.GetComponentInChildren<SafeAreaFitter>();
            if (fitter != null) parent = fitter.transform;

            _pauseButton = UIFactory.CreateButton(parent, "BtnPause", "II",
                new Vector2(64f, 64f), Toggle, Constants.CardYellow);

            RectTransform rt = _pauseButton.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -20f);
            _pauseButton.transform.SetAsLastSibling();

            TextMeshProUGUI label = _pauseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.fontSize = 24f;
                label.characterSpacing = 8f;
            }
        }

        /// <summary>
        /// Affiche/masque le bouton pause flottant (masqué sur le menu principal).
        /// </summary>
        public void SetPauseButtonVisible(bool visible)
        {
            _pauseAllowed = visible;
            if (_pauseButton != null) _pauseButton.gameObject.SetActive(visible);
        }

        private void OpenOptions()
        {
            if (_optionsPanel == null && _root != null)
            {
                _optionsPanel = _root.gameObject.AddComponent<OptionsPanel>();
                _optionsPanel.Setup(_root);
            }
            _optionsPanel?.Show();
        }

        private void HandleMainMenu()
        {
            // Hide() gère _isPaused et Time.timeScale — le faire avant nous
            // ferait tomber sur sa garde d'idempotence et l'overlay resterait
            // affiché par-dessus le menu principal.
            Hide();
            OnMainMenuRequested?.Invoke();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        /// <summary>
        /// Bascule pause/reprise.
        /// </summary>
        public void Toggle()
        {
            if (_isPaused) Hide();
            else Show();
        }

        /// <summary>
        /// Affiche le menu pause et gèle le temps.
        /// </summary>
        public void Show()
        {
            if (!_pauseAllowed || _isPaused || _root == null || _rootCanvasGroup == null || _panel == null) return;

            _isPaused = true;
            // En ligne, le temps ne se fige pas : la partie continue chez les
            // autres joueurs et les commandes réseau doivent être rejouées.
            if (!LevelUp.Core.GameSession.IsOnline)
            {
                Time.timeScale = 0f;
            }
            _root.gameObject.SetActive(true);
            _rootCanvasGroup.blocksRaycasts = true;
            _rootCanvasGroup.alpha = 0f;

            _panel.localScale = Vector3.zero;
            UITween.ScaleTo(_root.gameObject, _panel, Vector3.one, 0.35f);
            UITween.FadeTo(_root.gameObject, _rootCanvasGroup, 1f, 0.25f);
        }

        /// <summary>
        /// Masque le menu pause et reprend le temps.
        /// </summary>
        public void Hide()
        {
            if (!_isPaused || _root == null || _rootCanvasGroup == null || _panel == null) return;

            _isPaused = false;
            Time.timeScale = 1f;
            _rootCanvasGroup.blocksRaycasts = false;

            UITween.ScaleTo(_root.gameObject, _panel, Vector3.zero, 0.22f);
            UITween.FadeTo(_root.gameObject, _rootCanvasGroup, 0f, 0.22f, () =>
            {
                if (_root != null) _root.gameObject.SetActive(false);
            });
        }
    }
}
