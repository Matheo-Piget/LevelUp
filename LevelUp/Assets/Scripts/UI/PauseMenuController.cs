using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace LevelUp.UI
{
    /// <summary>
    /// Menu pause (style maquette : panneau #161B22 radius 28, liseré fin,
    /// titre serif, une action principale champagne).
    /// S'ouvre via les boutons pause/réglages de la top bar, ESC ou le bouton
    /// Retour Android. Fige le temps hors ligne.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        private RectTransform? _root;
        private CanvasGroup? _rootCanvasGroup;
        private RectTransform? _panel;
        private OptionsPanel? _optionsPanel;
        private RulesPanel? _rulesPanel;
        private readonly List<GameObject> _pauseButtons = new();
        private bool _isPaused;
        // La pause n'a de sens qu'en jeu : verrouillée sur le menu principal.
        private bool _pauseAllowed = true;

        /// <summary>Déclenché quand le joueur choisit "Menu principal".</summary>
        public event Action? OnMainMenuRequested;

        public bool IsPaused => _isPaused;

        public void Setup(Canvas canvas)
        {
            if (canvas == null) return;

            GameObject rootObj = new("PauseMenuOverlay", typeof(RectTransform), typeof(CanvasGroup));
            rootObj.transform.SetParent(canvas.transform, false);
            rootObj.transform.SetAsLastSibling();

            _root = rootObj.GetComponent<RectTransform>();
            UISkin.Stretch(_root);

            _rootCanvasGroup = rootObj.GetComponent<CanvasGroup>();
            _rootCanvasGroup.alpha = 0f;
            _rootCanvasGroup.blocksRaycasts = false;

            // Voile : tap en dehors du panneau = reprendre (essentiel en tactile)
            GameObject veil = new("Veil", typeof(RectTransform), typeof(Image), typeof(Button));
            veil.transform.SetParent(_root, false);
            UISkin.Stretch((RectTransform)veil.transform);
            veil.GetComponent<Image>().color = new Color(0.04f, 0.055f, 0.07f, 0.82f);
            Button veilBtn = veil.GetComponent<Button>();
            veilBtn.transition = Selectable.Transition.None;
            veilBtn.onClick.AddListener(Hide);

            Image panel = UISkin.Rect(_root, "PausePanel", UISkin.Surface, 28f, raycast: true);
            _panel = panel.rectTransform;
            PortraitFit.Wrap(_panel);
            UISkin.Place(_panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(480f, 514f));
            UISkin.Outline(_panel, 28f, 1f, new Color(1f, 1f, 1f, 0.08f));

            TextMeshProUGUI title = UISkin.Text(_panel, "Title", "PAUSE", 34f, UISkin.TextColor, TextWeight.Display,
                TextAlignmentOptions.MidlineLeft);
            title.characterSpacing = 2f;
            UISkin.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(44f, -40f),
                new Vector2(300f, 48f));

            Button close = GameScreenLayout.IconButton(_panel, "BtnClose", UIIcon.Close, 20f, "Fermer", Hide);
            UISkin.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-44f, -40f), new Vector2(48f, 48f));

            GameObject col = new("Buttons", typeof(RectTransform));
            col.transform.SetParent(_panel, false);
            UISkin.Place((RectTransform)col.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 44f), new Vector2(392f, 310f));
            VerticalLayoutGroup vlg = col.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            UIFactory.CreateButton(col.transform, "BtnResume", "REPRENDRE", new Vector2(392f, 64f), Hide,
                primary: true);
            UIFactory.CreateButton(col.transform, "BtnOptions", "OPTIONS", new Vector2(392f, 60f), ShowOptions);
            UIFactory.CreateButton(col.transform, "BtnRules", "COMMENT JOUER", new Vector2(392f, 60f), ShowRules);
            UIFactory.CreateButton(col.transform, "BtnMenu", "MENU PRINCIPAL", new Vector2(392f, 60f),
                HandleMainMenu, subtle: true);

            _root.gameObject.SetActive(false);
        }

        /// <summary>
        /// Enregistre un bouton externe (top bar) qui n'est visible qu'en jeu.
        /// </summary>
        public void RegisterPauseButton(GameObject button)
        {
            if (!_pauseButtons.Contains(button)) _pauseButtons.Add(button);
            button.SetActive(_pauseAllowed);
        }

        /// <summary>Affiche/masque les boutons pause (masqués sur le menu principal).</summary>
        public void SetPauseButtonVisible(bool visible)
        {
            _pauseAllowed = visible;
            foreach (GameObject b in _pauseButtons)
            {
                if (b != null) b.SetActive(visible);
            }
        }

        /// <summary>Ouvre le panneau d'options par-dessus la pause.</summary>
        public void ShowOptions()
        {
            if (_optionsPanel == null && _root != null)
            {
                _optionsPanel = _root.gameObject.AddComponent<OptionsPanel>();
                _optionsPanel.Setup(_root);
            }
            _optionsPanel?.Show();
        }

        /// <summary>Ouvre « Comment jouer » par-dessus la pause.</summary>
        public void ShowRules()
        {
            if (_rulesPanel == null && _root != null)
            {
                _rulesPanel = _root.gameObject.AddComponent<RulesPanel>();
                _rulesPanel.Setup(_root);
            }
            _rulesPanel?.Show();
        }

        /// <summary>Vrai si la pause peut s'ouvrir (en jeu).</summary>
        public bool PauseAllowed => _pauseAllowed;

        private void HandleMainMenu()
        {
            // Hide() gère _isPaused et Time.timeScale — à faire avant l'événement.
            Hide();
            OnMainMenuRequested?.Invoke();
        }

        public void Toggle()
        {
            if (_isPaused) Hide();
            else Show();
        }

        public void Show()
        {
            if (!_pauseAllowed || _isPaused || _root == null || _rootCanvasGroup == null || _panel == null) return;

            _isPaused = true;
            UIBackStack.Push(this, Hide);
            // En ligne, le temps ne se fige pas : la partie continue chez les autres.
            if (!LevelUp.Core.GameSession.IsOnline)
            {
                Time.timeScale = 0f;
            }
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _rootCanvasGroup.blocksRaycasts = true;
            _rootCanvasGroup.alpha = 0f;

            _panel.localScale = Vector3.one * 0.92f;
            UITween.ScaleTo(_root.gameObject, _panel, Vector3.one, 0.22f);
            UITween.FadeTo(_root.gameObject, _rootCanvasGroup, 1f, 0.18f);
        }

        public void Hide()
        {
            if (!_isPaused || _root == null || _rootCanvasGroup == null || _panel == null) return;

            _isPaused = false;
            UIBackStack.Remove(this);
            Time.timeScale = 1f;
            _rootCanvasGroup.blocksRaycasts = false;

            UITween.ScaleTo(_root.gameObject, _panel, Vector3.one * 0.95f, 0.16f);
            UITween.FadeTo(_root.gameObject, _rootCanvasGroup, 0f, 0.16f, () =>
            {
                if (_root != null) _root.gameObject.SetActive(false);
            });
        }
    }
}
