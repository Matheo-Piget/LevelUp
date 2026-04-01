using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Affichage visuel d'une carte : couleur, valeur, animations.
    /// Attach ce script à un prefab de carte avec Image + TextMeshPro.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CardView : MonoBehaviour
    {
        [Header("Card Faces")]
        [SerializeField] private GameObject? _cardFront;
        [SerializeField] private GameObject? _cardBack;

        [Header("Visuals")]
        [SerializeField] private Image? _background;
        [SerializeField] private Image? _border;
        [SerializeField] private Image? _colorBand;
        [SerializeField] private Image? _shadowImage;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI? _valueText;
        [SerializeField] private TextMeshProUGUI? _cornerValueTopLeft;
        [SerializeField] private TextMeshProUGUI? _cornerValueBottomRight;

        [Header("Interaction")]
        [SerializeField] private CanvasGroup? _canvasGroup;

        private CardModel _cardModel;
        private RectTransform? _rectTransform;
        private bool _isFaceUp = true;
        private bool _isSelected;
        private float _baseY;

        /// <summary>Le modèle de carte associé.</summary>
        public CardModel CardModel => _cardModel;

        /// <summary>Indique si la carte est face visible.</summary>
        public bool IsFaceUp => _isFaceUp;

        /// <summary>Indique si la carte est sélectionnée.</summary>
        public bool IsSelected => _isSelected;

        /// <summary>RectTransform de la carte.</summary>
        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
                return _rectTransform;
            }
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Configure l'affichage de la carte à partir du modèle.
        /// </summary>
        public void Setup(CardModel card, bool faceUp = true)
        {
            _cardModel = card;
            _isFaceUp = faceUp;
            UpdateVisuals();
        }

        /// <summary>
        /// Met à jour tous les éléments visuels selon le modèle de carte.
        /// </summary>
        private void UpdateVisuals()
        {
            if (_cardFront != null) _cardFront.SetActive(_isFaceUp);
            if (_cardBack != null) _cardBack.SetActive(!_isFaceUp);

            if (!_isFaceUp) return;

            Color cardColor = _cardModel.IsAction
                ? GetActionCardColor()
                : Constants.GetColor(_cardModel.Color);

            // Fond blanc de la carte
            if (_background != null)
            {
                _background.color = Color.white;
            }

            // Bordure colorée (bande latérale ou contour)
            if (_border != null)
            {
                _border.color = cardColor;
            }

            // Bande de couleur (indicateur visuel fort)
            if (_colorBand != null)
            {
                _colorBand.color = cardColor;
            }

            // Ombre
            if (_shadowImage != null)
            {
                _shadowImage.color = new Color(0f, 0f, 0f, 0.25f);
            }

            // Texte principal
            string displayText = GetDisplayText();

            if (_valueText != null)
            {
                _valueText.text = displayText;
                _valueText.color = cardColor;
                _valueText.fontSize = _cardModel.IsAction ? 28f : 52f;
            }

            if (_cornerValueTopLeft != null)
            {
                _cornerValueTopLeft.text = displayText;
                _cornerValueTopLeft.color = cardColor;
            }

            if (_cornerValueBottomRight != null)
            {
                _cornerValueBottomRight.text = displayText;
                _cornerValueBottomRight.color = cardColor;
            }
        }

        /// <summary>
        /// Retourne le texte à afficher selon le type de carte.
        /// </summary>
        private string GetDisplayText()
        {
            return _cardModel.Type switch
            {
                CardType.Normal    => _cardModel.Value.ToString(),
                CardType.Skip      => "SKIP",
                CardType.Draw2     => "+2",
                CardType.Wild      => "W",
                CardType.WildDraw2 => "W+2",
                _                  => "?"
            };
        }

        /// <summary>
        /// Retourne la couleur pour les cartes action.
        /// </summary>
        private Color GetActionCardColor()
        {
            return _cardModel.Type switch
            {
                CardType.Skip      => new Color32(0xFF, 0x45, 0x45, 0xFF),
                CardType.Draw2     => new Color32(0xFF, 0x85, 0x00, 0xFF),
                CardType.Wild      => new Color32(0x00, 0xCC, 0x66, 0xFF),
                CardType.WildDraw2 => new Color32(0xCC, 0x00, 0xFF, 0xFF),
                _                  => Color.white
            };
        }

        /// <summary>
        /// Retourne la carte face visible ou face cachée.
        /// </summary>
        public void SetFaceUp(bool faceUp)
        {
            _isFaceUp = faceUp;
            UpdateVisuals();
        }

        /// <summary>
        /// Sélectionne ou désélectionne la carte.
        /// La carte monte et reçoit un outline lumineux quand sélectionnée.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            Vector2 pos = RectTransform.anchoredPosition;

            if (selected)
            {
                _baseY = pos.y;
                RectTransform.anchoredPosition = new Vector2(pos.x, _baseY + 30f);

                // Highlight : border plus claire
                if (_border != null)
                {
                    Color c = _border.color;
                    _border.color = new Color(
                        Mathf.Min(c.r + 0.3f, 1f),
                        Mathf.Min(c.g + 0.3f, 1f),
                        Mathf.Min(c.b + 0.3f, 1f),
                        1f);
                }
            }
            else
            {
                RectTransform.anchoredPosition = new Vector2(pos.x, _baseY);

                // Restaurer la couleur normale
                if (_border != null)
                {
                    Color cardColor = _cardModel.IsAction
                        ? GetActionCardColor()
                        : Constants.GetColor(_cardModel.Color);
                    _border.color = cardColor;
                }
            }
        }

        /// <summary>
        /// Définit l'opacité de la carte.
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }
        }

        /// <summary>
        /// Active/désactive l'interactivité de la carte.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = interactable;
                _canvasGroup.blocksRaycasts = interactable;
            }
        }
    }
}
