using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LevelUp.UI
{
    /// <summary>
    /// Petite fenêtre de confirmation à la DA (titre, message, ANNULER / action).
    /// Sert avant une action qui efface quelque chose (remplacer la partie
    /// sauvegardée). Toucher à côté ou Retour = annuler.
    /// </summary>
    public class ConfirmDialog : MonoBehaviour
    {
        private const float Width = 620f;
        private const float Inner = Width - 88f;

        private GameObject? _veil;
        private RectTransform? _panel;
        private CanvasGroup? _cg;
        private TextMeshProUGUI? _title;
        private TextMeshProUGUI? _message;
        private TextMeshProUGUI? _confirmLabel;
        private Action? _onConfirm;
        private Action? _onCancel;
        private bool _visible;

        public void Setup(Transform parent)
        {
            UISkin.EnsureFonts();

            _veil = new GameObject("ConfirmVeil", typeof(RectTransform), typeof(Image), typeof(Button));
            _veil.transform.SetParent(parent, false);
            UISkin.Stretch((RectTransform)_veil.transform);
            _veil.GetComponent<Image>().color = new Color(0.04f, 0.055f, 0.07f, 0.86f);
            Button vBtn = _veil.GetComponent<Button>();
            vBtn.transition = Selectable.Transition.None;
            vBtn.onClick.AddListener(Cancel);

            Image panel = UISkin.Rect(_veil.transform, "ConfirmPanel", UISkin.Surface, 28f, raycast: true);
            _panel = panel.rectTransform;
            PortraitFit.Wrap(_panel);
            UISkin.Place(_panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width, 0f));
            UISkin.Outline(_panel, 28f, 1f, new Color(1f, 1f, 1f, 0.08f))
                .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UISkin.VCol(panel.gameObject, 16f, new RectOffset(44, 44, 34, 30), TextAnchor.UpperLeft);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _cg = panel.gameObject.AddComponent<CanvasGroup>();

            _title = UISkin.Text(panel.transform, "Title", "", 30f, UISkin.TextColor, TextWeight.Display);
            UISkin.Size(_title.gameObject, Inner, 40f);
            _message = UIControls.Paragraph(panel.transform, "Message", "", Inner, 48f, 16f);

            GameObject footer = UIControls.Row(panel.transform, "Footer", Inner, 60f, 14f);
            footer.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            Button cancel = UIFactory.CreateButton(footer.transform, "BtnConfirmCancel", "ANNULER",
                new Vector2(180f, 56f), Cancel, subtle: true);
            UISkin.Size(cancel.gameObject, 180f, 56f);
            Button ok = UIFactory.CreateButton(footer.transform, "BtnConfirmOk", "", new Vector2(240f, 60f), Confirm,
                primary: true);
            UISkin.Size(ok.gameObject, 240f, 60f);
            _confirmLabel = ok.GetComponentInChildren<TextMeshProUGUI>();

            _cg.alpha = 0f;
            _veil.SetActive(false);
        }

        public void Show(string title, string message, string confirm, Action onConfirm, Action? onCancel = null)
        {
            if (_veil == null || _panel == null || _cg == null) return;
            if (_title != null) _title.text = title;
            if (_message != null) _message.text = message;
            if (_confirmLabel != null) _confirmLabel.text = confirm;
            _onConfirm = onConfirm;
            _onCancel = onCancel;

            _visible = true;
            _veil.SetActive(true);
            _veil.transform.SetAsLastSibling();
            _panel.localScale = Vector3.one * 0.95f;
            UITween.ScaleTo(_panel.gameObject, _panel, Vector3.one, 0.2f);
            UITween.FadeTo(_panel.gameObject, _cg, 1f, 0.16f);
            UIBackStack.Push(this, Cancel);
        }

        private void Confirm()
        {
            Action? action = _onConfirm;
            Close();
            action?.Invoke();
        }

        private void Cancel()
        {
            Action? action = _onCancel;
            Close();
            action?.Invoke();
        }

        private void Close()
        {
            if (!_visible || _veil == null || _panel == null || _cg == null) return;
            _visible = false;
            _onConfirm = null;
            _onCancel = null;
            UIBackStack.Remove(this);
            UITween.FadeTo(_panel.gameObject, _cg, 0f, 0.14f, () =>
            {
                if (_veil != null && !_visible) _veil.SetActive(false);
            });
        }
    }
}
