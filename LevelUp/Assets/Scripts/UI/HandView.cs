using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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

        // Idle breathing
        private float _breathTime;

        // Deal cascade
        private bool _animatingDeal;

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
            _breathTime += Time.deltaTime;
            UpdateHover();
            LerpCardsToTarget();
        }

        /// <summary>
        /// Détecte le hover en continu.
        /// </summary>
        private void UpdateHover()
        {
            if (_draggedCard != null || _handContainer == null) return;

            Vector2 mousePos = Vector2.zero;
            bool hasPointer = false;

            if (Mouse.current != null)
            {
                mousePos = Mouse.current.position.ReadValue();
                hasPointer = true;
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                mousePos = Touchscreen.current.primaryTouch.position.ReadValue();
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
        /// Lerp smooth + idle breathing.
        /// </summary>
        private void LerpCardsToTarget()
        {
            float speed = Constants.CardLerpSpeed;
            float dt = Time.deltaTime * speed;

            for (int i = 0; i < _cardViews.Count; i++)
            {
                CardView card = _cardViews[i];
                if (card == _draggedCard) continue;
                if (i >= _targetPositions.Count) break;

                RectTransform rt = card.RectTransform;

                // Idle breathing : sin wave légère par carte
                float breathOffset = Mathf.Sin(_breathTime * 1.8f + i * 0.6f) * 2.5f;
                float breathScale = 1f + Mathf.Sin(_breathTime * 1.2f + i * 0.4f) * 0.005f;

                Vector2 targetPos = _targetPositions[i];
                // N'appliquer le breathing que si pas hovered/selected
                if (i != _hoveredIndex && !card.IsSelected)
                {
                    targetPos.y += breathOffset;
                }

                // Position
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, targetPos, dt);

                // Rotation
                float currentAngle = rt.localEulerAngles.z;
                if (currentAngle > 180f) currentAngle -= 360f;
                float newAngle = Mathf.Lerp(currentAngle, _targetRotations[i], dt);
                rt.localRotation = Quaternion.Euler(0, 0, newAngle);

                // Scale (avec breathing subtil)
                Vector3 targetScale = _targetScales[i];
                if (i != _hoveredIndex && !card.IsSelected)
                {
                    targetScale *= breathScale;
                }
                rt.localScale = Vector3.Lerp(rt.localScale, targetScale, dt);

                // Z-order
                if (i == _hoveredIndex)
                {
                    rt.SetAsLastSibling();
                }
                else
                {
                    rt.SetSiblingIndex(i);
                }
            }
        }

        /// <summary>
        /// Calcule les positions cibles avec hover/selection lift et reorder gap.
        /// </summary>
        private void RecalculateTargets()
        {
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
        /// Reconstruit la main. Si dealAnimation=true, anime un cascade deal.
        /// </summary>
        public void RefreshHand(IReadOnlyList<CardModel> cards, bool dealAnimation = false)
        {
            foreach (CardView view in _cardViews)
            {
                if (view != null) Destroy(view.gameObject);
            }
            _cardViews.Clear();
            _selectedCards.Clear();
            _hoveredIndex = -1;
            _hoveredCard = null;
            _draggedCard = null;

            if (_cardPrefab == null || _handContainer == null) return;

            for (int i = 0; i < cards.Count; i++)
            {
                GameObject cardObj = Instantiate(_cardPrefab, _handContainer);
                CardView cardView = cardObj.GetComponent<CardView>();
                if (cardView != null)
                {
                    cardView.Setup(cards[i], true);
                    _cardViews.Add(cardView);
                }
            }

            RecalculateTargets();

            if (dealAnimation && _animController != null)
            {
                StartCoroutine(DealCascadeCoroutine());
            }
            else
            {
                ApplyTargetsInstantly();
            }
        }

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

        /// <summary>
        /// Ajoute une carte avec animation depuis le deck.
        /// </summary>
        public void AddCardWithAnimation(CardModel card)
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
                });
            }
            else
            {
                _animatingDraw = false;
                FlushPendingHand();
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
                _draggedCard.RectTransform.anchoredPosition = localPoint - _dragOffset;
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
