using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Carte au style de la maquette "Système de cartes" :
    ///   - face pleine de sa couleur, encre blanche ou sombre (contraste ≥ 4.5:1)
    ///   - valeur centrale en serif (DM Serif Display), coins valeur + symbole (haut-gauche, bas-droite inversé)
    ///   - grand symbole en filigrane (second canal d'info, daltonisme)
    ///   - cartes action : surface neutre, bandeau 6 couleurs, pastille accent + libellé
    ///   - dos "Accent" : points, double liseré, monogramme LU
    /// États : survol (liseré clair + glow), sélection (anneau accent 3px + coche).
    /// Position/scale/rotation pilotés par HandView.
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

        private const float Radius = 14f;

        private CardModel _cardModel;
        private bool _modelAssigned;
        private RectTransform? _rectTransform;
        private bool _isFaceUp = true;
        private bool _isSelected;
        private bool _isHovered;
        private bool _built;

        // Éléments construits en code
        private Image? _face;
        private Image? _edge;
        private Image? _selectRing;
        private Image? _checkBadge;
        private Image? _hintRing;
        private Image? _hintBadge;
        private bool _isHinted;
        private Image? _watermark;
        private Image? _cornerSuitTL;
        private Image? _cornerSuitBR;
        private GameObject? _actionGroup;
        private Image? _actionBadgeIcon;
        private TextMeshProUGUI? _actionGlyph;
        private TextMeshProUGUI? _actionLabel;
        private Image? _glow;

        private float _glowAlpha;
        private float _targetGlowAlpha;
        private Color _glowColor = Color.white;
        private Color _ink = Color.white;

        private static readonly Color EdgeRest = new(1f, 1f, 1f, 0.22f);
        private static readonly Color EdgeHover = new(1f, 1f, 1f, 0.40f);

        public CardModel CardModel => _cardModel;
        public bool IsFaceUp => _isFaceUp;
        public bool IsSelected => _isSelected;
        public bool IsHovered => _isHovered;

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
            Build();
        }

        private void OnEnable() => GameSettings.SettingsChanged += OnSettingsChanged;
        private void OnDisable() => GameSettings.SettingsChanged -= OnSettingsChanged;

        // Réglages qui changent l'apparence d'une carte : seul un vrai changement
        // redessine (déplacer un curseur de volume ne touche plus aux cartes).
        private bool _builtColorblind;
        private int _builtBack;

        private void OnSettingsChanged()
        {
            if (_cardBack != null && _builtBack != GameSettings.CardBack)
            {
                _builtBack = GameSettings.CardBack;
                UISkin.BuildCardBack((RectTransform)_cardBack.transform, Radius, _builtBack);
            }
            if (_modelAssigned && _builtColorblind != GameSettings.ColorblindMode) UpdateVisuals();
        }

        private void Update()
        {
            // Converge puis s'arrête : pas de réécriture de couleur à chaque frame.
            if (_glowAlpha == _targetGlowAlpha) return;
            FrameBudget.Ping(0.2f);
            _glowAlpha = Mathf.Lerp(_glowAlpha, _targetGlowAlpha, 1f - Mathf.Exp(-15f * Time.deltaTime));
            if (Mathf.Abs(_glowAlpha - _targetGlowAlpha) < 0.003f) _glowAlpha = _targetGlowAlpha;
            if (_glow != null) _glow.color = UISkin.WithAlpha(_glowColor, _glowAlpha * 0.55f);
        }

        // ═══════════════════════════════════════════════════════════
        //  CONSTRUCTION
        // ═══════════════════════════════════════════════════════════

        private void Build()
        {
            if (_built) return;
            _built = true;
            UISkin.EnsureFonts();

            RectTransform root = RectTransform;

            // Glow derrière la carte (halo doux, invisible au repos → mesh cullé)
            GameObject glowObj = new("Glow", typeof(RectTransform), typeof(Image));
            glowObj.transform.SetParent(root, false);
            glowObj.transform.SetAsFirstSibling();
            RectTransform grt = (RectTransform)glowObj.transform;
            UISkin.Stretch(grt, -34f);
            _glow = glowObj.GetComponent<Image>();
            _glow.sprite = UISkin.Radial;
            _glow.color = new Color(1f, 1f, 1f, 0f);
            _glow.raycastTarget = false;
            _glow.canvasRenderer.cullTransparentMesh = true;

            // Ombre portée (0 10px 20px rgba(0,0,0,.4))
            if (_shadowImage != null)
            {
                UISkin.SetRounded(_shadowImage, Radius + 4f);
                RectTransform srt = _shadowImage.rectTransform;
                UISkin.Stretch(srt, -4f);
                srt.anchoredPosition = new Vector2(0f, -8f);
                _shadowImage.color = new Color(0f, 0f, 0f, 0.38f);
                _shadowImage.raycastTarget = false;
            }

            // Racine : zone de hit transparente (le visuel est porté par la face)
            Image? rootImg = GetComponent<Image>();
            if (rootImg != null)
            {
                UISkin.SetRounded(rootImg, Radius);
                rootImg.color = new Color(0f, 0f, 0f, 0f);
            }

            // Dos
            if (_cardBack != null)
            {
                _builtBack = GameSettings.CardBack;
                UISkin.BuildCardBack((RectTransform)_cardBack.transform, Radius, _builtBack);
            }

            if (_cardFront == null) return;
            RectTransform front = (RectTransform)_cardFront.transform;

            // Les décors du prefab d'origine ne font plus partie de la DA.
            if (_border != null) _border.enabled = false;
            if (_colorBand != null) _colorBand.enabled = false;
            Transform? indicator = front.Find("ColorIndicator");
            if (indicator != null) indicator.gameObject.SetActive(false);

            // Face
            if (_background != null && _background.transform.IsChildOf(front))
            {
                _face = _background;
            }
            else
            {
                if (_background != null && _background.gameObject != gameObject) _background.enabled = false;
                _face = UISkin.Rect(front, "Face", Color.white, Radius);
                UISkin.Stretch(_face.rectTransform);
            }
            UISkin.SetRounded(_face, Radius);
            _face.transform.SetAsFirstSibling();

            // Conteneur du contenu de face (filigrane, groupe action). Pas de masque :
            // tout est dessiné à l'intérieur de la carte, et un RectMask2D ne
            // suivrait pas l'inclinaison des cartes dans l'éventail.
            GameObject clip = new("Content", typeof(RectTransform));
            clip.transform.SetParent(front, false);
            clip.transform.SetSiblingIndex(_face.transform.GetSiblingIndex() + 1);
            UISkin.Stretch((RectTransform)clip.transform);

            _watermark = UISkin.SuitImage(clip.transform, "Watermark", CardColor.Red, 96f, Color.white);
            UISkin.Place(_watermark.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(96f, 96f));

            BuildActionGroup(clip.transform);

            // Valeur centrale
            if (_valueText != null)
            {
                _valueText.transform.SetAsLastSibling();
                UISkin.Stretch(_valueText.rectTransform);
                _valueText.rectTransform.anchoredPosition = Vector2.zero;
                UISkin.ApplyFont(_valueText, TextWeight.Display);
                _valueText.alignment = TextAlignmentOptions.Center;
                _valueText.enableWordWrapping = false;
                _valueText.characterSpacing = -3f;
                _valueText.raycastTarget = false;
            }

            // Coins : valeur + symbole
            _cornerSuitTL = BuildCorner(front, _cornerValueTopLeft, topLeft: true);
            _cornerSuitBR = BuildCorner(front, _cornerValueBottomRight, topLeft: false);

            // Liseré + anneau de sélection + coche
            _edge = UISkin.Outline(front, Radius, 1.5f, EdgeRest, "Edge");
            _edge.transform.SetAsLastSibling();

            _selectRing = UISkin.Outline(root, Radius + 3f, 3f, UISkin.SelectRing, "SelectRing");
            UISkin.Stretch(_selectRing.rectTransform, -3f);
            _selectRing.enabled = false;

            _checkBadge = UISkin.Rect(root, "CheckBadge", UISkin.Accent, 11f);
            // En haut au centre, à cheval sur le bord : dans l'éventail, le coin
            // haut-droit est recouvert par la carte voisine (la coche disparaissait).
            UISkin.Place(_checkBadge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 2f), new Vector2(22f, 22f));
            Image check = UISkin.IconImage(_checkBadge.transform, "Check", UIIcon.Check, 14f, UISkin.OnAccent);
            UISkin.Place(check.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(14f, 14f));
            _checkBadge.gameObject.SetActive(false);

            // Indice "jouable" (phase Compléter) : liseré champagne + pastille "+".
            _hintRing = UISkin.Outline(root, Radius + 2f, 2f, UISkin.WithAlpha(UISkin.Accent, 0.9f), "HintRing");
            UISkin.Stretch(_hintRing.rectTransform, -2f);
            _hintRing.enabled = false;

            _hintBadge = UISkin.Rect(root, "HintBadge", UISkin.Accent, 11f);
            UISkin.Place(_hintBadge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 2f), new Vector2(22f, 22f));
            TextMeshProUGUI plus = UISkin.Text(_hintBadge.transform, "Plus", "+", 18f, UISkin.OnAccent,
                TextWeight.Bold, TextAlignmentOptions.Center);
            UISkin.Stretch(plus.rectTransform);
            plus.rectTransform.anchoredPosition = new Vector2(0f, 1f);
            _hintBadge.gameObject.SetActive(false);
        }

        /// <summary>
        /// Indique que la carte peut compléter une combinaison posée (aide de
        /// lecture en phase "Compléter" — ne change rien aux règles).
        /// </summary>
        public void SetHint(bool hinted)
        {
            if (_isHinted == hinted) return;
            _isHinted = hinted;
            ApplyStateVisuals();
        }

        private static Image? BuildCorner(RectTransform front, TextMeshProUGUI? text, bool topLeft)
        {
            if (text == null) return null;

            GameObject corner = new(topLeft ? "CornerTL" : "CornerBR", typeof(RectTransform));
            corner.transform.SetParent(front, false);
            RectTransform crt = (RectTransform)corner.transform;
            Vector2 a = topLeft ? new Vector2(0f, 1f) : new Vector2(1f, 0f);
            UISkin.Place(crt, a, new Vector2(0.5f, 0.5f),
                topLeft ? new Vector2(22f, -26f) : new Vector2(-22f, 26f), new Vector2(36f, 44f));
            if (!topLeft) crt.localRotation = Quaternion.Euler(0f, 0f, 180f);

            text.transform.SetParent(corner.transform, false);
            UISkin.Place(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero,
                new Vector2(36f, 22f));
            text.rectTransform.localRotation = Quaternion.identity;
            UISkin.ApplyFont(text, TextWeight.Display);
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.fontSize = 20f;
            text.raycastTarget = false;

            Image suit = UISkin.SuitImage(corner.transform, "Suit", CardColor.Red, 15f, Color.white);
            UISkin.Place(suit.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -24f), new Vector2(15f, 15f));
            return suit;
        }

        private void BuildActionGroup(Transform parent)
        {
            _actionGroup = new GameObject("Action", typeof(RectTransform));
            _actionGroup.transform.SetParent(parent, false);
            UISkin.Stretch((RectTransform)_actionGroup.transform);

            // Bandeau 6 couleurs en haut, coins hauts arrondis (sprite pré-dessiné).
            GameObject band = new("Band", typeof(RectTransform), typeof(Image));
            band.transform.SetParent(_actionGroup.transform, false);
            RectTransform brt = (RectTransform)band.transform;
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(0f, 6f);
            brt.anchoredPosition = Vector2.zero;
            Image bandImg = band.GetComponent<Image>();
            bandImg.sprite = UISkin.ActionBand;
            bandImg.type = Image.Type.Simple;
            bandImg.raycastTarget = false;

            // Pastille centrale 60×60
            Image badge = UISkin.Rect(_actionGroup.transform, "Badge", UISkin.WithAlpha(UISkin.Accent, 0.16f), 16f);
            UISkin.Place(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 12f), new Vector2(60f, 60f));
            UISkin.Outline(badge.rectTransform, 16f, 1f, UISkin.WithAlpha(UISkin.AccentLight, 0.45f));

            _actionBadgeIcon = UISkin.IconImage(badge.transform, "Icon", UIIcon.Skip, 32f, UISkin.AccentPale);
            UISkin.Place(_actionBadgeIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(32f, 32f));

            _actionGlyph = UISkin.Text(badge.transform, "Glyph", "", 24f, UISkin.AccentPale,
                TextWeight.Display, TextAlignmentOptions.Center);
            UISkin.Stretch(_actionGlyph.rectTransform);

            _actionLabel = UISkin.Text(_actionGroup.transform, "Label", "", 14f, UISkin.TextColor,
                TextWeight.SemiBold, TextAlignmentOptions.Center);
            UISkin.Place(_actionLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -38f), new Vector2(110f, 20f));
        }

        // ═══════════════════════════════════════════════════════════
        //  MISE À JOUR
        // ═══════════════════════════════════════════════════════════

        /// <summary>Configure l'affichage de la carte à partir du modèle.</summary>
        public void Setup(CardModel card, bool faceUp = true)
        {
            Build();
            _cardModel = card;
            _modelAssigned = true;
            _isFaceUp = faceUp;
            _isSelected = false;
            _isHovered = false;
            _isHinted = false;
            _targetGlowAlpha = 0f;
            _glowAlpha = 0f;
            if (_glow != null) _glow.color = new Color(1f, 1f, 1f, 0f);
            UpdateVisuals();
            ApplyStateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_cardFront != null) _cardFront.SetActive(_isFaceUp);
            if (_cardBack != null) _cardBack.SetActive(!_isFaceUp);
            if (!_isFaceUp) return;

            bool action = _cardModel.IsAction;
            Color faceColor = action ? UISkin.CardFace : Constants.GetColor(_cardModel.Color);
            _ink = action ? UISkin.AccentPale : UISkin.InkFor(_cardModel.Color);
            _glowColor = action ? UISkin.AccentLight : faceColor;

            if (_face != null) _face.color = faceColor;

            if (_actionGroup != null) _actionGroup.SetActive(action);
            if (_watermark != null)
            {
                _watermark.gameObject.SetActive(!action);
                if (!action)
                {
                    _watermark.sprite = UISkin.Suit(_cardModel.Color);
                    // Mode daltonisme : filigrane renforcé.
                    _builtColorblind = GameSettings.ColorblindMode;
                    float a = _builtColorblind ? 0.30f : 0.16f;
                    _watermark.color = UISkin.WithAlpha(_ink, a);
                }
            }

            string value = action ? string.Empty : _cardModel.Value.ToString();

            if (_valueText != null)
            {
                _valueText.gameObject.SetActive(!action);
                _valueText.text = value;
                _valueText.color = _ink;
                _valueText.fontSize = _cardModel.Value >= 10 ? 56f : 62f;
            }

            SetCorner(_cornerValueTopLeft, _cornerSuitTL, value, action);
            SetCorner(_cornerValueBottomRight, _cornerSuitBR, value, action);

            if (action) SetupAction();
        }

        private void SetCorner(TextMeshProUGUI? text, Image? suit, string value, bool action)
        {
            if (text != null)
            {
                Transform holder = text.transform.parent;
                holder.gameObject.SetActive(!action);
                text.text = value;
                text.color = _ink;
            }
            if (suit != null && !action)
            {
                suit.sprite = UISkin.Suit(_cardModel.Color);
                suit.color = _ink;
            }
        }

        private void SetupAction()
        {
            if (_actionLabel == null || _actionGlyph == null || _actionBadgeIcon == null) return;

            bool skip = _cardModel.Type == CardType.Skip;
            _actionBadgeIcon.gameObject.SetActive(skip);
            _actionGlyph.gameObject.SetActive(!skip);

            _actionGlyph.text = _cardModel.Type switch
            {
                CardType.Draw2 => "+2",
                CardType.Wild => "J",
                CardType.WildDraw2 => "J+2",
                _ => ""
            };
            _actionGlyph.fontSize = _cardModel.Type == CardType.WildDraw2 ? 19f : _cardModel.Type == CardType.Wild ? 30f : 24f;

            _actionLabel.text = _cardModel.Type switch
            {
                CardType.Skip => "Passe",
                CardType.Draw2 => "Pioche 2",
                CardType.Wild => "Joker",
                CardType.WildDraw2 => "Joker +2",
                _ => ""
            };
        }

        /// <summary>Applique liseré / anneau / coche / glow selon survol et sélection.</summary>
        private void ApplyStateVisuals()
        {
            if (_selectRing != null) _selectRing.enabled = _isSelected && _isFaceUp;
            if (_checkBadge != null) _checkBadge.gameObject.SetActive(_isSelected && _isFaceUp);
            bool hint = _isHinted && _isFaceUp && !_isSelected;
            if (_hintRing != null) _hintRing.enabled = hint;
            if (_hintBadge != null) _hintBadge.gameObject.SetActive(hint);

            if (_edge != null)
            {
                Color rest = _cardModel.IsAction ? UISkin.WithAlpha(UISkin.AccentLight, 0.45f) : EdgeRest;
                _edge.color = _isHovered || _isSelected ? EdgeHover : rest;
            }

            if (_isSelected)
            {
                _glowColor = UISkin.Accent;
                _targetGlowAlpha = 1f;
            }
            else
            {
                _glowColor = _cardModel.IsAction ? UISkin.AccentLight : Constants.GetColor(_cardModel.Color);
                _targetGlowAlpha = _isHovered ? 0.7f : 0f;
            }
        }

        public void SetFaceUp(bool faceUp)
        {
            _isFaceUp = faceUp;
            UpdateVisuals();
            ApplyStateVisuals();
        }

        /// <summary>Sélection : anneau accent 3px + coche + halo accent.</summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            ApplyStateVisuals();
        }

        /// <summary>Survol : liseré plus clair + halo de la couleur de la carte.</summary>
        public void SetHovered(bool hovered)
        {
            if (_isHovered == hovered) return;
            _isHovered = hovered;
            ApplyStateVisuals();
        }

        public void SetAlpha(float alpha)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = alpha;
        }

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
