using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Main du joueur style Balatro : éventail avec arc prononcé,
    /// hover lift smooth, drag-to-reorder, idle breathing, smooth lerp.
    /// </summary>
    public class HandView : MonoBehaviour
    {
        [SerializeField] private RectTransform? _handContainer;
        [SerializeField] private GameObject? _cardPrefab;
        [SerializeField] private float _cardSpacing = 58f;
        [SerializeField] private float _maxHandWidth = 900f;
        [SerializeField] private AnimationController? _animController;

        private readonly List<CardView> _cardViews = new();
        private readonly List<CardView> _selectedCards = new();
        private readonly List<Vector2> _targetPositions = new();
        private readonly List<float> _targetRotations = new();
        private readonly List<Vector3> _targetScales = new();
        private CardView? _draggedCard;
        private CardView? _hoveredCard;
        private Vector2 _dragOffset;
        private int _playerIndex;
        private bool _animatingDraw;
        private int _hoveredIndex = -1;

        // Drag-to-reorder
        private bool _isReorderDrag;
        private int _draggedOriginalIndex = -1;
        private int _reorderInsertIndex = -1;

        // Vrai quand toutes les cartes sont à leur place : plus rien à écrire.
        private bool _settled;

        // Deal cascade
        private bool _animatingDeal;

        /// <summary>Prefab de carte (partagé avec les cartes « en vol »).</summary>
        public GameObject? CardPrefab => _cardPrefab;

        /// <summary>Carte unique sélectionnée (pour défausse).</summary>
        public CardView? SelectedCard => _selectedCards.Count == 1 ? _selectedCards[0] : null;

        /// <summary>Liste de toutes les cartes sélectionnées (pour pose de niveau).</summary>
        public IReadOnlyList<CardView> SelectedCards => _selectedCards;

        /// <summary>Nombre de cartes dans la main.</summary>
        public int CardCount => _cardViews.Count;

        /// <summary>Indique si la main est en cours de deal (cascade animation).</summary>
        public bool IsAnimatingDeal => _animatingDeal;

        /// <summary>Événement déclenché quand une carte est sélectionnée.</summary>
        public event System.Action<CardModel>? OnCardSelected;

        /// <summary>Événement déclenché quand une carte est drag & drop sur une cible.</summary>
        public event System.Action<CardModel, Vector2>? OnCardDropped;

        /// <summary>Événement déclenché quand les cartes sont réordonnées.</summary>
        public event System.Action<int, int>? OnCardsReordered;

        /// <summary>Événement déclenché quand la sélection (ajout/retrait/clear) change.</summary>
        public event System.Action? OnSelectionChanged;

        // Pending refresh : si un HandChangedEvent arrive pendant une animation,
        // on note qu'il faut rafraîchir une fois l'animation finie pour éviter
        // toute désynchronisation entre la main du modèle et la vue.
        private List<CardModel>? _pendingHand;

        // Boutons de tri posés en scène (mis en cache : le layout les déplace).
        private Button? _sortValueButton;
        private Button? _sortColorButton;

        private void Awake()
        {
            // Canvas imbriqué : la main bouge chaque frame (lerp, breathing, hover).
            // L'isoler dans son propre Canvas évite de re-mesher tout le HUD à
            // chaque frame — seul ce sous-canvas est rebuild. Gain majeur mobile.
            if (_handContainer != null && _handContainer.GetComponent<Canvas>() == null)
            {
                _handContainer.gameObject.AddComponent<Canvas>();
            }

            _sortValueButton = RestyleSortButton("SortButton", "Valeur", UIIcon.SortValue);
            _sortColorButton = RestyleSortButton("SortButtonColour", "Couleur", null);
        }

        /// <summary>Espacement et largeur max de l'éventail (posés par le layout d'écran).</summary>
        public void SetLayout(float cardSpacing, float maxWidth)
        {
            _cardSpacing = cardSpacing;
            _maxHandWidth = maxWidth;
            RecalculateTargets();
        }

        /// <summary>
        /// Déplace les deux boutons de tri dans le contrôle segmenté de la
        /// maquette ("TRIER LA MAIN" en bas à droite).
        /// </summary>
        public void MoveSortButtonsInto(RectTransform segmented)
        {
            if (_sortValueButton != null) _sortValueButton.transform.SetParent(segmented, false);
            if (_sortColorButton != null) _sortColorButton.transform.SetParent(segmented, false);
            SetActiveSort(true, highlight: false);
        }

        /// <summary>
        /// Met en évidence le dernier tri appliqué (champagne plein) : le joueur voit
        /// d'un coup d'œil dans quel ordre est sa main.
        /// </summary>
        public void SetActiveSort(bool byValue) => SetActiveSort(byValue, highlight: true);

        /// <summary>Surbrillance du tri mémorisé : 0 = ordre libre (aucun), 1 = valeur, 2 = couleur.</summary>
        public void SetSortMode(int mode) => SetActiveSort(mode == 1, highlight: mode != 0);

        private void SetActiveSort(bool byValue, bool highlight)
        {
            ApplySortButtonState(_sortValueButton, highlight && byValue);
            ApplySortButtonState(_sortColorButton, highlight && !byValue);
        }

        private static void ApplySortButtonState(Button? btn, bool active)
        {
            if (btn == null) return;
            Image? img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? UISkin.Accent : new Color(0f, 0f, 0f, 0f);
            TextMeshProUGUI? label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.color = active ? UISkin.OnAccent : UISkin.TextColor;
            Transform? icon = btn.transform.Find("Icon");
            if (icon != null && icon.TryGetComponent(out Image iconImg) && iconImg.sprite != null
                && iconImg.sprite.name.StartsWith("Icon_"))
            {
                iconImg.color = active ? UISkin.OnAccent : UISkin.TextColor;
            }
        }

        /// <summary>
        /// Bouton de tri de la maquette : 48px de haut, radius 12, icône + libellé.
        /// "Valeur" : icône de tri ; "Couleur" : 4 pastilles de couleur.
        /// </summary>
        private Button? RestyleSortButton(string childName, string text, UIIcon? icon)
        {
            Transform? t = transform.Find(childName);
            if (t == null) return null;

            Image? img = t.GetComponent<Image>();
            if (img != null)
            {
                UISkin.SetRounded(img, 12f);
                img.color = new Color(0f, 0f, 0f, 0f);
            }

            Button? btn = t.GetComponent<Button>();
            if (btn != null)
            {
                ColorBlock colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.25f, 1f);
                colors.pressedColor = new Color(0.8f, 0.8f, 0.85f, 1f);
                colors.selectedColor = Color.white;
                btn.colors = colors;
            }

            UISkin.HRow(t.gameObject, 8f, new RectOffset(18, 18, 0, 0), TextAnchor.MiddleCenter);
            UISkin.Size(t.gameObject, -1f, 48f);

            if (icon.HasValue)
            {
                Image ic = UISkin.IconImage(t, "Icon", icon.Value, 18f, UISkin.TextColor);
                UISkin.Size(ic.gameObject, 18f, 18f);
                ic.transform.SetAsFirstSibling();
            }
            else
            {
                // Pastilles rouge/bleu/vert/jaune en carré 2×2
                GameObject dots = new("Icon", typeof(RectTransform));
                dots.transform.SetParent(t, false);
                dots.transform.SetAsFirstSibling();
                UISkin.Size(dots, 18f, 18f);
                CardColor[] cs = { CardColor.Red, CardColor.Blue, CardColor.Green, CardColor.Yellow };
                for (int i = 0; i < 4; i++)
                {
                    Image d = UISkin.Rect(dots.transform, $"D{i}", Constants.GetColor(cs[i]), 3.5f);
                    UISkin.Place(d.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2((i % 2) * 10f + 0.5f, -(i / 2) * 10f - 0.5f), new Vector2(7f, 7f));
                }
            }

            TextMeshProUGUI? label = t.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = text;
                UISkin.ApplyFont(label, TextWeight.SemiBold);
                label.color = UISkin.TextColor;
                label.fontSize = 15f;
                label.enableWordWrapping = false;
                label.alignment = TextAlignmentOptions.MidlineLeft;
            }
            return btn;
        }


        private void OnEnable()
        {
            EventBus.Subscribe<HandChangedEvent>(OnHandChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<HandChangedEvent>(OnHandChanged);
        }

        private void Update()
        {
            UpdateHover();
            if (!_settled) LerpCardsToTarget();
        }

        /// <summary>
        /// Détecte le hover en continu.
        /// </summary>
        private void UpdateHover()
        {
            if (_draggedCard != null || _handContainer == null) return;

            Vector2 mousePos = Vector2.zero;
            bool hasPointer = false;

            // Tactile prioritaire (cohérent avec GameInputController) : un doigt
            // posé gagne sur un éventuel périphérique Mouse fantôme Android.
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                mousePos = Touchscreen.current.primaryTouch.position.ReadValue();
                hasPointer = true;
            }
            else if (Mouse.current != null)
            {
                mousePos = Mouse.current.position.ReadValue();
                hasPointer = true;
            }

            if (!hasPointer)
            {
                SetHoveredCard(-1);
                return;
            }

            int newHovered = GetCardIndexAtPosition(mousePos);
            SetHoveredCard(newHovered);
        }

        private void SetHoveredCard(int index)
        {
            if (index == _hoveredIndex) return;

            if (_hoveredIndex >= 0 && _hoveredIndex < _cardViews.Count)
            {
                _cardViews[_hoveredIndex].SetHovered(false);
            }

            _hoveredIndex = index;
            _hoveredCard = (index >= 0 && index < _cardViews.Count) ? _cardViews[index] : null;

            if (_hoveredCard != null)
            {
                _hoveredCard.SetHovered(true);
            }

            RecalculateTargets();
        }

        /// <summary>
        /// Rapproche chaque carte de sa cible (amortissement exponentiel). Une
        /// fois toutes les cartes arrivées, plus aucune écriture : le canvas de
        /// la main ne se redessine plus tant que rien ne bouge (batterie mobile).
        /// </summary>
        private void LerpCardsToTarget()
        {
            // Amortissement exponentiel : convergence identique à 30 et 144 fps
            // (un Lerp(a, b, deltaTime * k) brut est dépendant du framerate).
            float dt = 1f - Mathf.Exp(-Constants.CardLerpSpeed * Time.deltaTime);
            bool allSettled = !_animatingDeal && !_animatingDraw && _draggedCard == null;

            for (int i = 0; i < _cardViews.Count; i++)
            {
                CardView card = _cardViews[i];
                if (card == _draggedCard) continue;
                if (i >= _targetPositions.Count) break;

                RectTransform rt = card.RectTransform;
                Vector2 targetPos = _targetPositions[i];
                Vector3 targetScale = _targetScales[i];
                float targetAngle = _targetRotations[i];

                float currentAngle = rt.localEulerAngles.z;
                if (currentAngle > 180f) currentAngle -= 360f;

                bool arrived = (rt.anchoredPosition - targetPos).sqrMagnitude < 0.04f
                               && Mathf.Abs(currentAngle - targetAngle) < 0.05f
                               && Mathf.Abs(rt.localScale.x - targetScale.x) < 0.0015f;
                if (arrived)
                {
                    if (rt.anchoredPosition != targetPos) rt.anchoredPosition = targetPos;
                }
                else
                {
                    allSettled = false;
                    rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, targetPos, dt);
                    rt.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(currentAngle, targetAngle, dt));
                    rt.localScale = Vector3.Lerp(rt.localScale, targetScale, dt);
                }

                // Z-order : ne toucher à la hiérarchie que si elle change vraiment.
                // SetSiblingIndex force un rebuild complet de la géométrie du Canvas,
                // l'appeler chaque frame ruinait le batching.
                int desiredIndex = (i == _hoveredIndex)
                    ? rt.parent.childCount - 1
                    : i;
                if (rt.GetSiblingIndex() != desiredIndex)
                {
                    rt.SetSiblingIndex(desiredIndex);
                }
            }

            _settled = allSettled;
        }

        /// <summary>
        /// Calcule les positions cibles avec hover/selection lift et reorder gap.
        /// </summary>
        private void RecalculateTargets()
        {
            _settled = false;
            int count = _cardViews.Count;
            _targetPositions.Clear();
            _targetRotations.Clear();
            _targetScales.Clear();

            if (count == 0) return;

            float spacing = _cardSpacing;
            float totalWidth = (count - 1) * spacing;

            if (totalWidth > _maxHandWidth && count > 1)
            {
                spacing = _maxHandWidth / (count - 1);
                totalWidth = _maxHandWidth;
            }

            float startX = -totalWidth / 2f;
            float fanAngle = Constants.HandFanAngle;
            float angleStep = count > 1 ? fanAngle / (count - 1) : 0f;
            float startAngle = fanAngle / 2f;
            float arcHeight = Constants.HandArcHeight;

            for (int i = 0; i < count; i++)
            {
                CardView card = _cardViews[i];

                float x = startX + i * spacing;
                float angle = startAngle - i * angleStep;

                // Reorder gap : décaler les cartes pour montrer le point d'insertion
                if (_isReorderDrag && _reorderInsertIndex >= 0 && card != _draggedCard)
                {
                    int visualIndex = i;
                    if (visualIndex >= _reorderInsertIndex && visualIndex != _draggedOriginalIndex)
                    {
                        x += spacing * 0.5f;
                    }
                    if (visualIndex < _reorderInsertIndex && visualIndex != _draggedOriginalIndex)
                    {
                        x -= spacing * 0.1f;
                    }
                }

                // Arc parabolique
                float normalizedPos = count > 1 ? (float)i / (count - 1) - 0.5f : 0f;
                float y = -(normalizedPos * normalizedPos) * arcHeight;

                float scale = 1f;

                // Hover lift
                if (i == _hoveredIndex && !card.IsSelected && card != _draggedCard)
                {
                    y += Constants.HoverLiftY;
                    angle = 0f;
                    scale = Constants.HoverScale;
                }
                // Selection lift
                else if (card.IsSelected && card != _draggedCard)
                {
                    y += Constants.SelectLiftY;
                    scale = 1.08f;
                }

                // Voisins du hover
                if (_hoveredIndex >= 0 && i != _hoveredIndex && !card.IsSelected && card != _draggedCard)
                {
                    int dist = Mathf.Abs(i - _hoveredIndex);
                    if (dist == 1)
                    {
                        y += 8f;
                        scale = 1.02f;
                    }
                }

                _targetPositions.Add(new Vector2(x, y));
                _targetRotations.Add(angle);
                _targetScales.Add(Vector3.one * scale);
            }
        }

        /// <summary>
        /// Définit l'index du joueur local.
        /// </summary>
        public void SetPlayerIndex(int index)
        {
            _playerIndex = index;
        }

        /// <summary>
        /// Permet à l'InputController de verrouiller le flag d'animation
        /// AVANT d'exécuter une commande de pioche, pour que le HandChangedEvent
        /// soit mis en file d'attente au lieu de rebuild la main instantanément.
        /// </summary>
        public void SetAnimatingDraw(bool animating)
        {
            _animatingDraw = animating;
            if (!animating)
            {
                FlushPendingHand();
            }
        }

        private void OnHandChanged(HandChangedEvent evt)
        {
            if (evt.PlayerIndex != _playerIndex) return;
            if (_animatingDraw || _animatingDeal)
            {
                // On garde une référence à la dernière main pour rafraîchir
                // une fois l'animation terminée. Évite toute désync.
                _pendingHand = new List<CardModel>(evt.NewHand);
                return;
            }
            RefreshHand(evt.NewHand);
        }

        /// <summary>
        /// Si une mise à jour était en attente pendant une animation, l'applique.
        /// Appelée à la fin des animations de deal et de draw.
        /// </summary>
        private void FlushPendingHand()
        {
            if (_pendingHand == null) return;
            List<CardModel> hand = _pendingHand;
            _pendingHand = null;
            RefreshHand(hand);
        }

        /// <summary>
        /// Reconstruit la main. Si <paramref name="dealAnimation"/> est vrai, anime
        /// un cascade deal. Recycle les <see cref="CardView"/> existantes plutôt que
        /// de tout détruire/réinstancier — bénéfice direct sur le GC à chaque action
        /// (pioche, défausse, pose, ajout meld).
        /// </summary>
        public void RefreshHand(IReadOnlyList<CardModel> cards, bool dealAnimation = false)
        {
            _selectedCards.Clear();
            _hoveredIndex = -1;
            _hoveredCard = null;
            _draggedCard = null;

            if (_cardPrefab == null || _handContainer == null) return;

            // Chaque carte garde SA vue (retrouvée par identifiant) : après une
            // défausse ou une pose, les cartes restantes glissent vers leur nouvelle
            // place au lieu de changer de contenu sur place (« téléportation »).
            _viewById.Clear();
            _spareViews.Clear();
            foreach (CardView cv in _cardViews)
            {
                if (cv == null) continue;
                if (!_viewById.ContainsKey(cv.CardModel.Id)) _viewById[cv.CardModel.Id] = cv;
                else _spareViews.Add(cv);
            }

            _nextViews.Clear();
            _freshViews.Clear();
            for (int i = 0; i < cards.Count; i++)
            {
                if (!dealAnimation && _viewById.TryGetValue(cards[i].Id, out CardView kept))
                {
                    _viewById.Remove(cards[i].Id);
                    kept.Setup(cards[i], faceUp: true); // réinitialise sélection / survol
                    _nextViews.Add(kept);
                    continue;
                }
                _nextViews.Add(null!); // complété ci-dessous
            }

            // Vues sans carte : recyclées pour les nouvelles cartes, sinon détruites.
            foreach (CardView leftover in _viewById.Values) _spareViews.Add(leftover);
            int spare = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                if (_nextViews[i] != null) continue;
                CardView view;
                if (spare < _spareViews.Count)
                {
                    view = _spareViews[spare++];
                }
                else
                {
                    GameObject cardObj = Instantiate(_cardPrefab, _handContainer);
                    view = cardObj.GetComponent<CardView>();
                }
                view.Setup(cards[i], faceUp: true);
                _nextViews[i] = view;
                _freshViews.Add(view);
            }
            for (int i = spare; i < _spareViews.Count; i++)
            {
                if (_spareViews[i] != null) Destroy(_spareViews[i].gameObject);
            }

            _cardViews.Clear();
            _cardViews.AddRange(_nextViews);
            RecalculateTargets();

            if (dealAnimation && _animController != null)
            {
                StartCoroutine(DealCascadeCoroutine());
            }
            else if (!_hasLaidOut)
            {
                ApplyTargetsInstantly();
            }
            else
            {
                // Les cartes conservées glissent (LerpCardsToTarget) ; une carte
                // apparue hors pioche (+2 subi, reprise) se pose directement.
                for (int i = 0; i < _cardViews.Count && i < _targetPositions.Count; i++)
                {
                    if (!_freshViews.Contains(_cardViews[i])) continue;
                    RectTransform rt = _cardViews[i].RectTransform;
                    rt.anchoredPosition = _targetPositions[i];
                    rt.localRotation = Quaternion.Euler(0, 0, _targetRotations[i]);
                    rt.localScale = _targetScales[i];
                    _cardViews[i].SetAlpha(1f);
                }
            }
            _hasLaidOut = true;
        }

        // Tampons de RefreshHand (aucune allocation après le premier appel).
        private readonly Dictionary<int, CardView> _viewById = new();
        private readonly List<CardView> _spareViews = new();
        private readonly List<CardView> _nextViews = new();
        private readonly List<CardView> _freshViews = new();
        private bool _hasLaidOut;

        /// <summary>
        /// Deal cascade : cartes arrivent une par une depuis le deck.
        /// </summary>
        private System.Collections.IEnumerator DealCascadeCoroutine()
        {
            _animatingDeal = true;

            // Cacher toutes les cartes au début
            for (int i = 0; i < _cardViews.Count; i++)
            {
                _cardViews[i].SetAlpha(0f);
                _cardViews[i].RectTransform.localScale = Vector3.zero;
            }

            // Faire apparaître chaque carte avec un délai
            for (int i = 0; i < _cardViews.Count; i++)
            {
                CardView card = _cardViews[i];
                card.SetAlpha(1f);

                if (_animController != null)
                {
                    // Position au deck, puis anime vers la main
                    _animController.AnimateDrawToHand(card.RectTransform, null);
                }
                else
                {
                    if (i < _targetPositions.Count)
                    {
                        card.RectTransform.anchoredPosition = _targetPositions[i];
                        card.RectTransform.localScale = _targetScales[i];
                    }
                }

                yield return new WaitForSeconds(0.08f);
            }

            // Attendre que la dernière animation finisse
            yield return new WaitForSeconds(Constants.AnimDrawDuration);

            _animatingDeal = false;
            RecalculateTargets();
            FlushPendingHand();
        }

        /// <summary>Fin de l'animation d'une carte piochée (le tri mémorisé peut s'appliquer).</summary>
        public event System.Action? DrawAnimationFinished;

        /// <summary>Vrai pendant l'arrivée d'une carte piochée.</summary>
        public bool IsAnimatingDraw => _animatingDraw;

        /// <summary>
        /// Position et échelle monde d'une carte de la main (départ des
        /// trajectoires vers la table ou la défausse). Faux si absente.
        /// </summary>
        public bool TryGetCardOrigin(int cardId, out CardOrigin origin)
        {
            foreach (CardView cv in _cardViews)
            {
                if (cv == null || cv.CardModel.Id != cardId) continue;
                RectTransform rt = cv.RectTransform;
                origin = new CardOrigin(rt.position, rt.lossyScale.x);
                return true;
            }
            origin = default;
            return false;
        }

        /// <summary>
        /// Ajoute une carte avec animation depuis le paquet, ou depuis
        /// <paramref name="fromWorld"/> (carte ramassée sur une défausse).
        /// </summary>
        public void AddCardWithAnimation(CardModel card, Vector3? fromWorld = null)
        {
            if (_cardPrefab == null || _handContainer == null) return;

            _animatingDraw = true;

            GameObject cardObj = Instantiate(_cardPrefab, _handContainer);
            CardView cardView = cardObj.GetComponent<CardView>();
            if (cardView == null)
            {
                _animatingDraw = false;
                return;
            }

            cardView.Setup(card, true);
            _cardViews.Add(cardView);

            RecalculateTargets();

            if (_animController != null)
            {
                _animController.AnimateDrawToHand(cardView.RectTransform, () =>
                {
                    _animatingDraw = false;
                    FlushPendingHand();
                    DrawAnimationFinished?.Invoke();
                }, fromWorld);
            }
            else
            {
                _animatingDraw = false;
                FlushPendingHand();
                DrawAnimationFinished?.Invoke();
            }
        }

        private void ApplyTargetsInstantly()
        {
            for (int i = 0; i < _cardViews.Count && i < _targetPositions.Count; i++)
            {
                RectTransform rt = _cardViews[i].RectTransform;
                rt.anchoredPosition = _targetPositions[i];
                rt.localRotation = Quaternion.Euler(0, 0, _targetRotations[i]);
                rt.localScale = _targetScales[i];
                rt.SetSiblingIndex(i);
                _cardViews[i].SetAlpha(1f);
            }
        }

        public void ForceLayout()
        {
            RecalculateTargets();
        }

        /// <summary>
        /// Réordonne les CardView existantes pour matcher l'ordre du modèle
        /// (après un tri). Ne recrée rien — la sélection et les instances sont
        /// conservées, seul l'ordre de _cardViews change, puis Lerp anime.
        /// </summary>
        public void ApplySortedOrder(IReadOnlyList<CardModel> newOrder)
        {
            if (newOrder == null) return;

            // Si on est en plein drag, on ne perturbe pas le geste en cours.
            if (_draggedCard != null || _isReorderDrag)
            {
                return;
            }

            // Si le nombre ne correspond pas (desync rare), fallback rebuild propre.
            if (newOrder.Count != _cardViews.Count)
            {
                RefreshHand(newOrder);
                return;
            }

            List<CardView> sorted = new(newOrder.Count);
            for (int i = 0; i < newOrder.Count; i++)
            {
                CardModel target = newOrder[i];
                CardView? match = null;
                for (int j = 0; j < _cardViews.Count; j++)
                {
                    if (_cardViews[j].CardModel.Id == target.Id)
                    {
                        match = _cardViews[j];
                        break;
                    }
                }
                if (match == null)
                {
                    RefreshHand(newOrder);
                    return;
                }
                sorted.Add(match);
            }

            _cardViews.Clear();
            _cardViews.AddRange(sorted);
            // Le hover peut pointer vers un mauvais index après tri — on reset.
            _hoveredIndex = -1;
            _hoveredCard = null;
            RecalculateTargets();
        }

        // ═══════════════════════════════════════
        //  DRAG-TO-REORDER
        // ═══════════════════════════════════════

        /// <summary>
        /// Démarre un drag-to-reorder depuis la main.
        /// </summary>
        public void BeginReorderDrag(int cardIndex, Vector2 screenPosition)
        {
            if (cardIndex < 0 || cardIndex >= _cardViews.Count) return;

            _isReorderDrag = true;
            _draggedOriginalIndex = cardIndex;
            _reorderInsertIndex = cardIndex;
            _draggedCard = _cardViews[cardIndex];

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _handContainer, screenPosition, null, out _dragOffset);
            _dragOffset -= _draggedCard.RectTransform.anchoredPosition;

            _draggedCard.RectTransform.SetAsLastSibling();
            _draggedCard.SetAlpha(0.85f);
            _draggedCard.RectTransform.localScale = Vector3.one * 1.15f;
            _draggedCard.RectTransform.localRotation = Quaternion.identity;

            RecalculateTargets();
        }

        /// <summary>
        /// Continue un drag-to-reorder : calcule le point d'insertion.
        /// </summary>
        public void ContinueReorderDrag(Vector2 screenPosition)
        {
            if (_draggedCard == null || _handContainer == null || !_isReorderDrag) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _handContainer, screenPosition, null, out Vector2 localPoint))
            {
                _draggedCard.RectTransform.anchoredPosition = localPoint - _dragOffset;

                // Calculer le point d'insertion basé sur la position X
                int newInsert = CalculateInsertIndex(localPoint.x);
                if (newInsert != _reorderInsertIndex)
                {
                    _reorderInsertIndex = newInsert;
                    RecalculateTargets();
                }
            }
        }

        /// <summary>
        /// Termine le drag-to-reorder : applique le réordonnement.
        /// </summary>
        public void EndReorderDrag()
        {
            if (_draggedCard == null || !_isReorderDrag) return;

            _draggedCard.SetAlpha(1f);
            _draggedCard.RectTransform.localScale = Vector3.one;

            // Effectuer le réordonnement
            if (_reorderInsertIndex >= 0 && _reorderInsertIndex != _draggedOriginalIndex)
            {
                CardView movedCard = _cardViews[_draggedOriginalIndex];
                _cardViews.RemoveAt(_draggedOriginalIndex);

                int insertAt = _reorderInsertIndex;
                if (insertAt > _draggedOriginalIndex) insertAt--;
                insertAt = Mathf.Clamp(insertAt, 0, _cardViews.Count);

                _cardViews.Insert(insertAt, movedCard);

                // Notifier pour sync le model
                OnCardsReordered?.Invoke(_draggedOriginalIndex, insertAt);
            }

            _isReorderDrag = false;
            _draggedCard = null;
            _draggedOriginalIndex = -1;
            _reorderInsertIndex = -1;

            RecalculateTargets();
        }

        /// <summary>
        /// Calcule l'index d'insertion en fonction de la position X du drag.
        /// </summary>
        private int CalculateInsertIndex(float localX)
        {
            int count = _cardViews.Count;
            if (count <= 1) return 0;

            float spacing = _cardSpacing;
            float totalWidth = (count - 1) * spacing;

            if (totalWidth > _maxHandWidth && count > 1)
            {
                spacing = _maxHandWidth / (count - 1);
                totalWidth = _maxHandWidth;
            }

            float startX = -totalWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                float cardCenterX = startX + i * spacing;
                if (localX < cardCenterX)
                {
                    return i;
                }
            }

            return count - 1;
        }

        /// <summary>
        /// Indique si un drag-to-reorder est en cours.
        /// </summary>
        public bool IsReorderDragging => _isReorderDrag;

        // ═══════════════════════════════════════
        //  ACTION DRAG (défausse / add to meld)
        // ═══════════════════════════════════════

        public void BeginDrag(int cardIndex, Vector2 screenPosition)
        {
            if (cardIndex < 0 || cardIndex >= _cardViews.Count) return;

            _isReorderDrag = false;
            _draggedCard = _cardViews[cardIndex];

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _handContainer, screenPosition, null, out _dragOffset);
            _dragOffset -= _draggedCard.RectTransform.anchoredPosition;

            _draggedCard.RectTransform.SetAsLastSibling();
            _draggedCard.SetAlpha(0.9f);
            _draggedCard.RectTransform.localScale = Vector3.one * 1.1f;
        }

        public void ContinueDrag(Vector2 screenPosition)
        {
            if (_draggedCard == null || _handContainer == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _handContainer, screenPosition, null, out Vector2 localPoint))
            {
                RectTransform rt = _draggedCard.RectTransform;
                Vector2 target = localPoint - _dragOffset;
                // Léger tilt dans le sens du mouvement (la carte « traîne » derrière le doigt).
                float dx = target.x - rt.anchoredPosition.x;
                float tilt = GameSettings.ReducedMotion ? 0f : Mathf.Clamp(-dx * 0.5f, -12f, 12f);
                float current = rt.localEulerAngles.z;
                if (current > 180f) current -= 360f;
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(current, tilt, 0.25f));
                rt.anchoredPosition = target;
            }
        }

        public void EndDrag(Vector2 screenPosition)
        {
            if (_draggedCard == null) return;

            _draggedCard.SetAlpha(1f);
            _draggedCard.RectTransform.localScale = Vector3.one;
            OnCardDropped?.Invoke(_draggedCard.CardModel, screenPosition);
            _draggedCard = null;
            _isReorderDrag = false;

            RecalculateTargets();
        }

        // ═══════════════════════════════════════
        //  SELECTION
        // ═══════════════════════════════════════

        public void ToggleCardSelection(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= _cardViews.Count) return;

            CardView card = _cardViews[cardIndex];

            if (card.IsSelected)
            {
                card.SetSelected(false);
                _selectedCards.Remove(card);
            }
            else
            {
                card.SetSelected(true);
                _selectedCards.Add(card);

                // Pulse feedback
                if (_animController != null)
                {
                    _animController.AnimatePulse(card.RectTransform);
                }
            }

            RecalculateTargets();
            OnCardSelected?.Invoke(card.CardModel);
            OnSelectionChanged?.Invoke();
        }

        public void SelectSingleCard(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= _cardViews.Count) return;

            CardView clicked = _cardViews[cardIndex];

            if (_selectedCards.Count == 1 && _selectedCards[0] == clicked)
            {
                return;
            }

            DeselectAllInternal();
            clicked.SetSelected(true);
            _selectedCards.Add(clicked);
            RecalculateTargets();
            OnCardSelected?.Invoke(clicked.CardModel);
            OnSelectionChanged?.Invoke();
        }

        public bool IsDoubleClick(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= _cardViews.Count) return false;
            return _selectedCards.Count == 1 && _selectedCards[0] == _cardViews[cardIndex];
        }

        public CardView? GetCardViewAt(int index)
        {
            if (index < 0 || index >= _cardViews.Count) return null;
            return _cardViews[index];
        }

        public List<CardModel> GetSelectedCardModels()
        {
            List<CardModel> models = new();
            foreach (CardView cv in _selectedCards)
            {
                models.Add(cv.CardModel);
            }
            return models;
        }

        public int GetCardIndexAtPosition(Vector2 screenPosition)
        {
            for (int i = _cardViews.Count - 1; i >= 0; i--)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(
                        _cardViews[i].RectTransform, screenPosition, null))
                {
                    return i;
                }
            }
            return -1;
        }

        public void DeselectAll()
        {
            DeselectAllInternal();
            RecalculateTargets();
            OnSelectionChanged?.Invoke();
        }

        /// <summary>
        /// Remplace la sélection par ces cartes (aide "Préparer la pose").
        /// Les cartes absentes de la main sont ignorées.
        /// </summary>
        public void SelectCards(IReadOnlyList<CardModel> cards)
        {
            DeselectAllInternal();
            foreach (CardModel model in cards)
            {
                foreach (CardView cv in _cardViews)
                {
                    if (cv.CardModel == model && !cv.IsSelected)
                    {
                        cv.SetSelected(true);
                        _selectedCards.Add(cv);
                        break;
                    }
                }
            }
            RecalculateTargets();
            OnSelectionChanged?.Invoke();
        }

        /// <summary>Allume/éteint l'indice "jouable" sur chaque carte de la main.</summary>
        public void SetPlayableHints(System.Func<CardModel, bool>? isPlayable)
        {
            foreach (CardView cv in _cardViews)
            {
                cv.SetHint(isPlayable != null && isPlayable(cv.CardModel));
            }
        }

        private void DeselectAllInternal()
        {
            foreach (CardView cv in _selectedCards)
            {
                cv.SetSelected(false);
            }
            _selectedCards.Clear();
        }
    }
}
