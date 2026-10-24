using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Voile plein écran "Reconnexion…" pendant une coupure réseau en ligne.
    /// Bloque les entrées (aucune action ne doit partir pendant la reprise)
    /// et anime un indicateur pour montrer que l'appli n'est pas figée.
    /// </summary>
    public class ConnectionOverlay : MonoBehaviour
    {
        private GameObject? _root;
        private TextMeshProUGUI? _text;
        private RectTransform? _spinner;

        public bool IsVisible => _root != null && _root.activeSelf;

        public void Setup(Canvas canvas)
        {
            if (canvas == null || _root != null) return;

            _root = new GameObject("ConnectionOverlay", typeof(RectTransform), typeof(Image));
            _root.transform.SetParent(canvas.transform, false);
            RectTransform rt = _root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            Image veil = _root.GetComponent<Image>();
            veil.color = new Color(0f, 0f, 0f, 0.72f);
            veil.raycastTarget = true; // avale les clics/touchers

            GameObject spinnerObj = new("Spinner", typeof(RectTransform), typeof(Image));
            spinnerObj.transform.SetParent(_root.transform, false);
            _spinner = spinnerObj.GetComponent<RectTransform>();
            _spinner.sizeDelta = new Vector2(64f, 64f);
            _spinner.anchoredPosition = new Vector2(0f, 70f);
            Image spinImg = spinnerObj.GetComponent<Image>();
            spinImg.sprite = UIFactory.RoundedSprite;
            spinImg.type = Image.Type.Sliced;
            spinImg.color = Constants.AccentLight;
            spinImg.raycastTarget = false;

            _text = UIFactory.CreateText(_root.transform, "Text", "", 30f,
                Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            _text.rectTransform.sizeDelta = new Vector2(900f, 140f);
            _text.rectTransform.anchoredPosition = new Vector2(0f, -40f);

            _root.SetActive(false);
        }

        public void Show(string message)
        {
            if (_root == null) return;
            if (_text != null) _text.text = message;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void Update()
        {
            if (_spinner != null && _root != null && _root.activeSelf)
            {
                _spinner.Rotate(0f, 0f, -180f * Time.unscaledDeltaTime);
            }
        }
    }
}
