using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;

namespace LevelUp.UI
{
    /// <summary>
    /// Petite fenêtre "Ton pseudo" : saisie du nom affiché à la table et envoyé
    /// à l'hôte en ligne, avec le rappel des statistiques de l'appareil.
    /// </summary>
    public class ProfileDialog : MonoBehaviour
    {
        private const float PanelWidth = 560f;
        private const float Inner = PanelWidth - 88f;

        private GameObject? _veil;
        private RectTransform? _panel;
        private CanvasGroup? _cg;
        private TMP_InputField? _input;
        private TextMeshProUGUI? _stats;
        private TextMeshProUGUI? _achievements;
        private Action? _onSaved;
        private bool _isVisible;

        public bool IsVisible => _isVisible;

        public void Setup(RectTransform parent)
        {
            UISkin.EnsureFonts();

            _veil = new GameObject("ProfileVeil", typeof(RectTransform), typeof(Image), typeof(Button));
            _veil.transform.SetParent(parent, false);
            UISkin.Stretch((RectTransform)_veil.transform);
            _veil.GetComponent<Image>().color = new Color(0.04f, 0.055f, 0.07f, 0.86f);
            Button vBtn = _veil.GetComponent<Button>();
            vBtn.transition = Selectable.Transition.None;
            vBtn.onClick.AddListener(Hide);

            Image panel = UISkin.Rect(_veil.transform, "ProfilePanel", UISkin.Surface, 28f, raycast: true);
            _panel = panel.rectTransform;
            UISkin.Place(_panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f),
                new Vector2(PanelWidth, 0f));
            UISkin.Outline(_panel, 28f, 1f, new Color(1f, 1f, 1f, 0.08f))
                .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UISkin.VCol(panel.gameObject, 18f, new RectOffset(44, 44, 36, 34), TextAnchor.UpperLeft);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _cg = panel.gameObject.AddComponent<CanvasGroup>();

            GameObject header = UIControls.Row(panel.transform, "Header", Inner, 48f);
            TextMeshProUGUI title = UISkin.Text(header.transform, "Title", "TON PSEUDO", 30f, UISkin.TextColor,
                TextWeight.Display);
            title.characterSpacing = 2f;
            UISkin.Size(title.gameObject, Inner - 48f - 16f);
            GameScreenLayout.IconButton(header.transform, "BtnX", UIIcon.Close, 20f, "Fermer", Hide);

            UIControls.Paragraph(panel.transform, "Hint",
                "Affiché à la table et visible par tes amis quand tu joues en ligne.", Inner, 22f, 15f);

            _input = UIInputs.NameField(panel.transform, "NameInput", Inner, 64f);
            _input.onSubmit.AddListener(_ => Save());

            _stats = UISkin.Text(panel.transform, "Stats", "", 14f, UISkin.TextMuted, TextWeight.Medium);
            UISkin.Size(_stats.gameObject, Inner, 20f);
            _achievements = UIControls.Paragraph(panel.transform, "Achievements", "", Inner, 232f, 14f, UISkin.TextColor);
            _achievements.richText = true;
            _achievements.lineSpacing = 6f;

            GameObject footer = UIControls.Row(panel.transform, "Footer", Inner, 60f, 14f);
            footer.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            Button cancel = UIFactory.CreateButton(footer.transform, "BtnCancel", "ANNULER",
                new Vector2(170f, 56f), Hide, subtle: true);
            UISkin.Size(cancel.gameObject, 170f, 56f);
            Button save = UIFactory.CreateButton(footer.transform, "BtnSaveName", "ENREGISTRER",
                new Vector2(250f, 60f), Save, primary: true);
            UISkin.Size(save.gameObject, 250f, 60f);

            _cg.alpha = 0f;
            _veil.SetActive(false);
        }

        /// <summary>Ouvre la fenêtre, pré-remplie avec le pseudo actuel.</summary>
        public void Show(Action? onSaved = null)
        {
            if (_veil == null || _panel == null || _cg == null) return;
            _onSaved = onSaved;

            if (_input != null)
            {
                _input.text = PlayerProfile.HasCustomName ? PlayerProfile.Name : "";
            }
            if (_stats != null) _stats.text = StatsLine();
            if (_achievements != null) _achievements.text = Achievements.Describe();

            if (_isVisible) return;
            _isVisible = true;
            UIBackStack.Push(this, Hide);
            _veil.SetActive(true);
            _veil.transform.SetAsLastSibling();
            _panel.localScale = Vector3.one * 0.94f;
            UITween.ScaleTo(_panel.gameObject, _panel, Vector3.one, 0.2f);
            UITween.FadeTo(_panel.gameObject, _cg, 1f, 0.16f);

            // Focus direct : le clavier s'ouvre tout de suite sur mobile.
            _input?.ActivateInputField();
        }

        private void Save()
        {
            if (_input != null) PlayerProfile.Name = _input.text;
            Action? saved = _onSaved;
            Hide();
            saved?.Invoke();
        }

        public void Hide()
        {
            if (_veil == null || _panel == null || _cg == null || !_isVisible) return;
            _isVisible = false;
            UIBackStack.Remove(this);
            _input?.DeactivateInputField();
            UITween.ScaleTo(_panel.gameObject, _panel, Vector3.one * 0.96f, 0.14f);
            UITween.FadeTo(_panel.gameObject, _cg, 0f, 0.14f, () =>
            {
                if (_veil != null && !_isVisible) _veil.SetActive(false);
            });
        }

        /// <summary>"12 parties · 5 victoires · 31 manches gagnées" (ou invitation à jouer).</summary>
        public static string StatsLine()
        {
            int played = PlayerProfile.GamesPlayed;
            if (played == 0) return "Aucune partie terminée pour l'instant";
            int won = PlayerProfile.GamesWon;
            int rounds = PlayerProfile.RoundsWon;
            return $"{played} partie{(played > 1 ? "s" : "")} · {won} victoire{(won > 1 ? "s" : "")} · " +
                   $"{rounds} manche{(rounds > 1 ? "s" : "")} gagnée{(rounds > 1 ? "s" : "")}";
        }
    }
}
