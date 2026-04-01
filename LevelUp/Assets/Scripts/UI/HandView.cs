using System.Collections.Generic;
using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Gère l'affichage de la main du joueur local : positionnement en éventail,
    /// multi-sélection, drag & drop.
    /// </summary>
    public class HandView : MonoBehaviour
    {
        [SerializeField] private RectTransform? _handContainer;
        [SerializeField] private GameObject? _cardPrefab;
        [SerializeField] private float _cardSpacing = 65f;
        [SerializeField] private float _maxHandWidth = 900f;
        [SerializeField] private float _fanAngle = 4f;
        [SerializeField] private AnimationController? _animController;

        private readonly List<CardView> _cardViews = new();
        private readonly List<CardView> _selectedCards = new();
        private CardView? _draggedCard;
        private Vector2 _dragOffset;
        private int _playerIndex;
        private bool _animatingDraw;

        /// <summary>Carte unique sélectionnée (pour défausse).</summary>
        public CardView? SelectedCard => _selectedCards.Count == 1 ? _selectedCards[0] : null;

        /// <summary>Liste de toutes les cartes sélectionnées (pour pose de niveau).</summary>
        public IReadOnlyList<CardView> SelectedCards => _selectedCards;

        /// <summary>Nombre de cartes dans la main.</summary>
        public int CardCount => _cardViews.Count;

        /// <summary>Événement déclenché quand une carte est sélectionnée.</summary>
        public event System.Action<CardModel>? OnCardSelected;

        /// <summary>Événement déclenché quand une carte est drag & drop sur une cible.</summary>
        public event System.Action<CardModel, Vector2>? OnCardDropped;

        private void OnEnable()
        {
            EventBus.Subscribe<HandChangedEvent>(OnHandChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<HandChangedEvent>(OnHandChanged);
        }

        /// <summary>
        /// Définit l'index du joueur local dont on affiche la main.
        /// </summary>
        public void SetPlayerIndex(int index)
        {
            _playerIndex = index;
        }

        /// <summary>
        /// Appelé quand la main du joueur change.
        /// </summary>
        private void OnHandChanged(HandChangedEvent evt)
        {
            if (evt.PlayerIndex != _playerIndex) return;

            // Si on est en train d'animer une pioche, ne pas reconstruire toute la main
            // (on l'a déjà ajoutée visuellement)
            if (_animatingDraw) return;

            RefreshHand(evt.NewHand);
        }

        /// <summary>
        /// Reconstruit l'affichage complet de la main.
        /// </summary>
        public void RefreshHand(List<CardModel> cards)
        {
            foreach (CardView view in _cardViews)
            {
                if (view != null) Destroy(view.gameObject);
            }
            _cardViews.Clear();
            _selectedCards.Clear();

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

            LayoutCards();
        }

        /// <summary>
        /// Ajoute une carte à la main avec animation depuis le deck.
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

            // D'abord positionner les cartes existantes pour calculer la destination
            LayoutCards();

            // Lancer l'animation depuis le deck
            if (_animController != null)
            {
                _animController.AnimateDrawToHand(cardView.RectTransform, () =>
                {
                    _animatingDraw = false;
                });
            }
            else
            {
                _animatingDraw = false;
            }
        }

        /// <summary>
        /// Positionne les cartes en éventail dans la main.
        /// </summary>
        private void LayoutCards()
        {
            int count = _cardViews.Count;
            if (count == 0) return;

            float spacing = _cardSpacing;
            float totalWidth = (count - 1) * spacing;

            if (totalWidth > _maxHandWidth && count > 1)
            {
                spacing = _maxHandWidth / (count - 1);
                totalWidth = _maxHandWidth;
            }

            float startX = -totalWidth / 2f;
            float angleStep = count > 1 ? _fanAngle / (count - 1) : 0f;
            float startAngle = _fanAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                CardView card = _cardViews[i];
                if (card.IsSelected) continue; // ne pas repositionner les cartes sélectionnées en Y

                RectTransform rt = card.RectTransform;
                float x = startX + i * spacing;
                float angle = startAngle - i * angleStep;

                // Arc léger
                float normalizedPos = count > 1 ? (float)i / (count - 1) - 0.5f : 0f;
                float y = -(normalizedPos * normalizedPos) * 25f;

                rt.anchoredPosition = new Vector2(x, y);
                rt.localRotation = Quaternion.Euler(0, 0, angle);
                rt.SetSiblingIndex(i);
            }
        }

        /// <summary>
        /// Repositionne toutes les cartes (y compris les sélectionnées) en X uniquement.
        /// Utilisé après un changement de nombre de cartes.
        /// </summary>
        public void ForceLayout()
        {
            int count = _cardViews.Count;
            if (count == 0) return;

            float spacing = _cardSpacing;
            float totalWidth = (count - 1) * spacing;

            if (totalWidth > _maxHandWidth && count > 1)
            {
                spacing = _maxHandWidth / (count - 1);
                totalWidth = _maxHandWidth;
            }

            float startX = -totalWidth / 2f;
            float angleStep = count > 1 ? _fanAngle / (count - 1) : 0f;
            float startAngle = _fanAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                CardView card = _cardViews[i];
                RectTransform rt = card.RectTransform;
                float x = startX + i * spacing;
                float angle = startAngle - i * angleStep;
                float normalizedPos = count > 1 ? (float)i / (count - 1) - 0.5f : 0f;
                float y = -(normalizedPos * normalizedPos) * 25f;

                if (card.IsSelected) y += 30f;

                rt.anchoredPosition = new Vector2(x, y);
                rt.localRotation = Quaternion.Euler(0, 0, angle);
                rt.SetSiblingIndex(i);
            }
        }

        /// <summary>
        /// Toggle la sélection d'une carte (multi-sélection pour pose de niveau).
        /// </summary>
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
            }

            OnCardSelected?.Invoke(card.CardModel);
        }

        /// <summary>
        /// Sélectionne une seule carte (pour la défausse). Désélectionne les autres.
        /// </summary>
        public void SelectSingleCard(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= _cardViews.Count) return;

            CardView clicked = _cardViews[cardIndex];

            // Si on re-clique sur la seule carte sélectionnée, c'est un double-clic
            if (_selectedCards.Count == 1 && _selectedCards[0] == clicked)
            {
                return; // le caller gère le double-clic
            }

            DeselectAll();
            clicked.SetSelected(true);
            _selectedCards.Add(clicked);
            OnCardSelected?.Invoke(clicked.CardModel);
        }

        /// <summary>
        /// Vérifie si un clic est un double-clic sur la carte déjà sélectionnée.
        /// </summary>
        public bool IsDoubleClick(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= _cardViews.Count) return false;
            return _selectedCards.Count == 1 && _selectedCards[0] == _cardViews[cardIndex];
        }

        /// <summary>
        /// Retourne la CardView à un index donné.
        /// </summary>
        public CardView? GetCardViewAt(int index)
        {
            if (index < 0 || index >= _cardViews.Count) return null;
            return _cardViews[index];
        }

        /// <summary>
        /// Retourne les CardModel de toutes les cartes sélectionnées.
        /// </summary>
        public List<CardModel> GetSelectedCardModels()
        {
            List<CardModel> models = new();
            foreach (CardView cv in _selectedCards)
            {
                models.Add(cv.CardModel);
            }
            return models;
        }

        /// <summary>
        /// Démarre le drag d'une carte.
        /// </summary>
        public void BeginDrag(int cardIndex, Vector2 screenPosition)
        {
            if (cardIndex < 0 || cardIndex >= _cardViews.Count) return;

            _draggedCard = _cardViews[cardIndex];

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _handContainer, screenPosition, null, out _dragOffset);

            _dragOffset -= _draggedCard.RectTransform.anchoredPosition;
            _draggedCard.RectTransform.SetAsLastSibling();
            _draggedCard.SetAlpha(0.85f);
        }

        /// <summary>
        /// Continue le drag d'une carte.
        /// </summary>
        public void ContinueDrag(Vector2 screenPosition)
        {
            if (_draggedCard == null || _handContainer == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _handContainer, screenPosition, null, out Vector2 localPoint))
            {
                _draggedCard.RectTransform.anchoredPosition = localPoint - _dragOffset;
            }
        }

        /// <summary>
        /// Termine le drag d'une carte.
        /// </summary>
        public void EndDrag(Vector2 screenPosition)
        {
            if (_draggedCard == null) return;

            _draggedCard.SetAlpha(1f);
            OnCardDropped?.Invoke(_draggedCard.CardModel, screenPosition);
            _draggedCard = null;

            ForceLayout();
        }

        /// <summary>
        /// Retourne l'index d'une carte sous une position écran donnée.
        /// </summary>
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

        /// <summary>
        /// Désélectionne toutes les cartes.
        /// </summary>
        public void DeselectAll()
        {
            foreach (CardView cv in _selectedCards)
            {
                cv.SetSelected(false);
            }
            _selectedCards.Clear();
        }
    }
}
