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
        private bool _modelAssigned;
        private RectTransform? _rectTransform;
        private bool _isFaceUp = true;
        private bool _isSelected;
        private bool _isHovered;
        private Image? _glowOverlay;
        private Image? _outerGlow;
        private Image? _gradientOverlay;
        private TextMeshProUGUI? _suitIcon;
        private float _glowAlpha;
        private float _targetGlowAlpha;
        private static Sprite? _radialSprite;

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
            CreateGradientOverlay();
            CreateSuitIcon();
        }

        private void OnEnable()
        {
            GameSettings.SettingsChanged += OnSettingsChanged;
        }

        private void OnDisable()
        {
            GameSettings.SettingsChanged -= OnSettingsChanged;
        }

        private void OnSettingsChanged()
        {
            if (_modelAssigned) UpdateSuitIcon();
        }

        private void Update()
        {
            _glowAlpha = Mathf.Lerp(_glowAlpha, _targetGlowAlpha, Time.deltaTime * 15f);

            if (_glowOverlay != null)
            {
                Color c = _glowOverlay.color;
                c.a = _glowAlpha;
                _glowOverlay.color = c;
            }

            if (_outerGlow != null)
            {
                Color c = _outerGlow.color;
                c.a = _glowAlpha * 0.5f;
                _outerGlow.color = c;
            }
        }

        /// <summary>
        /// Crée 2 couches de glow derrière la carte pour un vrai effet néon doux.
        /// </summary>
        private void CreateGlowOverlay()
        {
            if (_border == null) return;

            EnsureRadialSprite();

            // Glow extérieur large et doux
            GameObject outerObj = new("OuterGlow", typeof(RectTransform), typeof(Image));
            outerObj.transform.SetParent(transform, false);
            outerObj.transform.SetAsFirstSibling();

            RectTransform outerRt = outerObj.GetComponent<RectTransform>();
            outerRt.anchorMin = Vector2.zero;
            outerRt.anchorMax = Vector2.one;
            outerRt.sizeDelta = new Vector2(80f, 80f);
            outerRt.anchoredPosition = Vector2.zero;

            _outerGlow = outerObj.GetComponent<Image>();
            _outerGlow.sprite = _radialSprite;
            _outerGlow.color = new Color(1f, 1f, 1f, 0f);
            _outerGlow.raycastTarget = false;
            _outerGlow.type = Image.Type.Simple;

            // Glow proche, plus net
            GameObject glowObj = new("GlowOverlay", typeof(RectTransform), typeof(Image));
            glowObj.transform.SetParent(transform, false);
            glowObj.transform.SetSiblingIndex(1);

            RectTransform rt = glowObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(28f, 28f);
            rt.anchoredPosition = Vector2.zero;

            _glowOverlay = glowObj.GetComponent<Image>();
            _glowOverlay.sprite = _radialSprite;
            _glowOverlay.color = new Color(1f, 1f, 1f, 0f);
            _glowOverlay.raycastTarget = false;
            _glowOverlay.type = Image.Type.Simple;
        }

        /// <summary>
        /// Crée un symbole unicode (♥ ♦ ♣ ♠ ★ ●) centré sur la face, visible uniquement
        /// en mode daltonisme. Permet d'identifier la couleur sans se fier à la teinte.
        /// </summary>
        private void CreateSuitIcon()
        {
            if (_cardFront == null) return;

            GameObject iconObj = new("SuitIcon", typeof(RectTransform));
            iconObj.transform.SetParent(_cardFront.transform, false);
            iconObj.transform.SetAsLastSibling();

            _suitIcon = iconObj.AddComponent<TextMeshProUGUI>();
            _suitIcon.alignment = TextAlignmentOptions.Center;
            _suitIcon.fontSize = 36f;
            _suitIcon.fontStyle = FontStyles.Bold;
            _suitIcon.raycastTarget = false;
            _suitIcon.color = new Color(1f, 1f, 1f, 0f);

            RectTransform rt = _suitIcon.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(60f, 40f);
            rt.anchoredPosition = new Vector2(0f, 18f);
        }

        /// <summary>
        /// Met à jour l'icône de daltonisme selon le paramètre global et la couleur.
        /// </summary>
        private void UpdateSuitIcon()
        {
            if (_suitIcon == null) return;
            if (!_isFaceUp)
            {
                _suitIcon.text = string.Empty;
                return;
            }

            if (!GameSettings.ColorblindMode || _cardModel.IsAction)
            {
                _suitIcon.text = string.Empty;
                return;
            }

            _suitIcon.text = GetSuitSymbol(_cardModel.Color);
            Color c = Constants.GetColor(_cardModel.Color);
            _suitIcon.color = Color.Lerp(c, Color.white, 0.4f);
            _suitIcon.outlineWidth = 0.2f;
            _suitIcon.outlineColor = new Color32(0, 0, 0, 200);
        }

        private static string GetSuitSymbol(CardColor color)
        {
            return color switch
            {
                CardColor.Red    => "\u2665", // ♥
                CardColor.Blue   => "\u25C6", // ◆
                CardColor.Green  => "\u2663", // ♣
                CardColor.Yellow => "\u2605", // ★
                CardColor.Purple => "\u2660", // ♠
                CardColor.Orange => "\u25B2", // ▲
                _                => string.Empty
            };
        }

        /// <summary>
        /// Ajoute un overlay de gradient subtil sur la face de la carte (haut clair, bas sombre).
        /// Donne du volume sans shader.
        /// </summary>
        private void CreateGradientOverlay()
        {
            if (_cardFront == null) return;

            GameObject gradObj = new("GradientOverlay", typeof(RectTransform), typeof(Image));
            gradObj.transform.SetParent(_cardFront.transform, false);
            gradObj.transform.SetAsLastSibling();

            RectTransform rt = gradObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            _gradientOverlay = gradObj.GetComponent<Image>();
            _gradientOverlay.sprite = CreateVerticalGradient();
            _gradientOverlay.color = new Color(1f, 1f, 1f, 0.18f);
            _gradientOverlay.raycastTarget = false;
            _gradientOverlay.type = Image.Type.Simple;
        }

        private static void EnsureRadialSprite()
        {
            if (_radialSprite != null) return;

            const int size = 64;
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float center = (size - 1) * 0.5f;
            float maxDist = center;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / maxDist;
                    float dy = (y - center) / maxDist;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            _radialSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateVerticalGradient()
        {
            const int width = 4;
            const int height = 32;
            Texture2D tex = new(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                // Haut = blanc transparent, bas = noir transparent
                float topAlpha = Mathf.Lerp(0.0f, 0.35f, Mathf.Pow(t, 1.5f));
                float c = Mathf.Lerp(0.05f, 1f, t);
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = new Color(c, c, c, topAlpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Configure l'affichage de la carte à partir du modèle.
        /// </summary>
        public void Setup(CardModel card, bool faceUp = true)
        {
            _cardModel = card;
            _modelAssigned = true;
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

            // Fond sombre avec un léger tint de la couleur de la carte
            if (_background != null)
            {
                Color tinted = Color.Lerp(Constants.CardFaceColor, cardColor, 0.08f);
                _background.color = tinted;
            }

            // Bordure colorée vive et un peu plus épaisse visuellement
            if (_border != null) _border.color = cardColor;

            // Bande de couleur sur le côté — plus lumineuse
            if (_colorBand != null)
            {
                Color brightBand = Color.Lerp(cardColor, Color.white, 0.15f);
                _colorBand.color = brightBand;
            }

            // Ombre plus prononcée
            if (_shadowImage != null) _shadowImage.color = new Color(0f, 0f, 0f, 0.6f);

            // Glow overlay prend la couleur de la carte
            if (_glowOverlay != null)
            {
                Color glowColor = cardColor;
                glowColor.a = 0f;
                _glowOverlay.color = glowColor;
            }
            if (_outerGlow != null)
            {
                Color outerColor = cardColor;
                outerColor.a = 0f;
                _outerGlow.color = outerColor;
            }

            string displayText = GetDisplayText();

            // Texte principal — blanc pur, bien gros pour lisibilité instantanée
            if (_valueText != null)
            {
                _valueText.text = displayText;
                _valueText.color = Color.white;
                _valueText.fontSize = _cardModel.IsAction ? 30f : 60f;
                _valueText.fontStyle = FontStyles.Bold;
                _valueText.outlineWidth = 0.15f;
                _valueText.outlineColor = new Color32(0, 0, 0, 120);
            }

            // Coins — couleur de la carte, plus gros et lumineux
            if (_cornerValueTopLeft != null)
            {
                _cornerValueTopLeft.text = displayText;
                _cornerValueTopLeft.color = Color.Lerp(cardColor, Color.white, 0.25f);
                _cornerValueTopLeft.fontStyle = FontStyles.Bold;
                _cornerValueTopLeft.fontSize = 18f;
            }

            if (_cornerValueBottomRight != null)
            {
                _cornerValueBottomRight.text = displayText;
                _cornerValueBottomRight.color = Color.Lerp(cardColor, Color.white, 0.25f);
                _cornerValueBottomRight.fontStyle = FontStyles.Bold;
                _cornerValueBottomRight.fontSize = 18f;
            }

            UpdateSuitIcon();
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

        private Color GetActionCardColor() => Constants.GetActionColor(_cardModel.Type);

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
                _targetGlowAlpha = 0.85f;
            }
            else
            {
                if (_border != null)
                {
                    _border.color = cardColor;
                }
                _targetGlowAlpha = _isHovered ? 0.5f : 0f;
            }

            ApplyGlowColor(cardColor);
        }

        /// <summary>
        /// Marque la carte comme survolée — glow léger.
        /// </summary>
        public void SetHovered(bool hovered)
        {
            if (_isHovered == hovered) return;
            _isHovered = hovered;

            Color cardColor = _cardModel.IsAction
                ? GetActionCardColor()
                : Constants.GetColor(_cardModel.Color);

            if (!_isSelected)
            {
                _targetGlowAlpha = hovered ? 0.5f : 0f;

                if (_border != null)
                {
                    _border.color = hovered
                        ? Color.Lerp(cardColor, Color.white, 0.25f)
                        : cardColor;
                }
            }

            ApplyGlowColor(cardColor);
        }

        /// <summary>
        /// Synchronise la teinte de l'effet glow avec la couleur courante de la carte.
        /// L'alpha est mis à jour en continu via Update.
        /// </summary>
        private void ApplyGlowColor(Color cardColor)
        {
            if (_glowOverlay != null)
            {
                Color c = _glowOverlay.color;
                _glowOverlay.color = new Color(cardColor.r, cardColor.g, cardColor.b, c.a);
            }
            if (_outerGlow != null)
            {
                Color c = _outerGlow.color;
                _outerGlow.color = new Color(cardColor.r, cardColor.g, cardColor.b, c.a);
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
