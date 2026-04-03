using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Affichage visuel d'une carte style Balatro : fond sombre, accents néon,
    /// glow sur hover/sélection. Position et scale gérés par HandView.
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
        private bool _isHovered;
        private Image? _glowOverlay;
        private float _glowAlpha;
        private float _targetGlowAlpha;

        /// <summary>Le modèle de carte associé.</summary>
        public CardModel CardModel => _cardModel;

        /// <summary>Indique si la carte est face visible.</summary>
        public bool IsFaceUp => _isFaceUp;

        /// <summary>Indique si la carte est sélectionnée.</summary>
        public bool IsSelected => _isSelected;

        /// <summary>Indique si la carte est survolée.</summary>
        public bool IsHovered => _isHovered;

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
            CreateGlowOverlay();
        }

        private void Update()
        {
            // Smooth glow transition
            if (_glowOverlay != null)
            {
                _glowAlpha = Mathf.Lerp(_glowAlpha, _targetGlowAlpha, Time.deltaTime * 15f);
                Color c = _glowOverlay.color;
                c.a = _glowAlpha;
                _glowOverlay.color = c;
            }
        }

        /// <summary>
        /// Crée un overlay de glow derrière la carte pour l'effet néon.
        /// </summary>
        private void CreateGlowOverlay()
        {
            if (_border == null) return;

            GameObject glowObj = new GameObject("GlowOverlay", typeof(RectTransform), typeof(Image));
            glowObj.transform.SetParent(transform, false);
            glowObj.transform.SetAsFirstSibling();

            RectTransform rt = glowObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(8f, 8f); // slightly larger than card
            rt.anchoredPosition = Vector2.zero;

            _glowOverlay = glowObj.GetComponent<Image>();
            _glowOverlay.color = new Color(1f, 1f, 1f, 0f);
            _glowOverlay.raycastTarget = false;
        }

        /// <summary>
        /// Configure l'affichage de la carte à partir du modèle.
        /// </summary>
        public void Setup(CardModel card, bool faceUp = true)
        {
            _cardModel = card;
            _isFaceUp = faceUp;
            _isSelected = false;
            _isHovered = false;
            _targetGlowAlpha = 0f;
            _glowAlpha = 0f;
            UpdateVisuals();
        }

        /// <summary>
        /// Met à jour tous les éléments visuels — style Balatro : fond sombre, accents vifs.
        /// </summary>
        private void UpdateVisuals()
        {
            if (_cardFront != null) _cardFront.SetActive(_isFaceUp);
            if (_cardBack != null) _cardBack.SetActive(!_isFaceUp);

            if (!_isFaceUp)
            {
                if (_glowOverlay != null) _glowOverlay.color = new Color(1f, 1f, 1f, 0f);
                return;
            }

            Color cardColor = _cardModel.IsAction
                ? GetActionCardColor()
                : Constants.GetColor(_cardModel.Color);

            // Fond sombre de la carte (pas blanc)
            if (_background != null) _background.color = Constants.CardFaceColor;

            // Bordure colorée vive
            if (_border != null) _border.color = cardColor;

            // Bande de couleur sur le côté
            if (_colorBand != null) _colorBand.color = cardColor;

            // Ombre plus prononcée
            if (_shadowImage != null) _shadowImage.color = new Color(0f, 0f, 0f, 0.5f);

            // Glow overlay prend la couleur de la carte
            if (_glowOverlay != null)
            {
                Color glowColor = cardColor;
                glowColor.a = 0f;
                _glowOverlay.color = glowColor;
            }

            string displayText = GetDisplayText();

            // Texte principal — blanc pour contraste sur fond sombre
            if (_valueText != null)
            {
                _valueText.text = displayText;
                _valueText.color = Constants.TextPrimary;
                _valueText.fontSize = _cardModel.IsAction ? 28f : 56f;
                _valueText.fontStyle = FontStyles.Bold;
            }

            // Coins — couleur de la carte pour un accent visuel
            if (_cornerValueTopLeft != null)
            {
                _cornerValueTopLeft.text = displayText;
                _cornerValueTopLeft.color = cardColor;
                _cornerValueTopLeft.fontStyle = FontStyles.Bold;
            }

            if (_cornerValueBottomRight != null)
            {
                _cornerValueBottomRight.text = displayText;
                _cornerValueBottomRight.color = cardColor;
                _cornerValueBottomRight.fontStyle = FontStyles.Bold;
            }
        }

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

        private Color GetActionCardColor()
        {
            return _cardModel.Type switch
            {
                CardType.Skip      => new Color32(0xFF, 0x4D, 0x6A, 0xFF), // rose vif
                CardType.Draw2     => new Color32(0xFF, 0x8C, 0x42, 0xFF), // orange vif
                CardType.Wild      => new Color32(0x4D, 0xFF, 0x91, 0xFF), // vert néon
                CardType.WildDraw2 => new Color32(0xBB, 0x6B, 0xFF, 0xFF), // violet néon
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
        /// Marque la carte comme sélectionnée — glow intense + bordure lumineuse.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            Color cardColor = _cardModel.IsAction
                ? GetActionCardColor()
                : Constants.GetColor(_cardModel.Color);

            if (selected)
            {
                // Bordure très lumineuse (blend vers blanc)
                if (_border != null)
                {
                    _border.color = Color.Lerp(cardColor, Color.white, 0.5f);
                }

                // Glow intense
                _targetGlowAlpha = 0.4f;
            }
            else
            {
                if (_border != null)
                {
                    _border.color = cardColor;
                }
                _targetGlowAlpha = _isHovered ? 0.2f : 0f;
            }
        }

        /// <summary>
        /// Marque la carte comme survolée — glow léger.
        /// </summary>
        public void SetHovered(bool hovered)
        {
            if (_isHovered == hovered) return;
            _isHovered = hovered;

            if (!_isSelected)
            {
                _targetGlowAlpha = hovered ? 0.2f : 0f;

                Color cardColor = _cardModel.IsAction
                    ? GetActionCardColor()
                    : Constants.GetColor(_cardModel.Color);

                if (_border != null)
                {
                    _border.color = hovered
                        ? Color.Lerp(cardColor, Color.white, 0.25f)
                        : cardColor;
                }
            }
        }

        /// <summary>
        /// Définit l'opacité de la carte.
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = alpha;
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
