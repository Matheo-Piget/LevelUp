using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Contrôleur d'entrée principal. Gère pioche, multi-sélection, défausse,
    /// ajout aux combinaisons, et drag-to-reorder de la main.
    /// </summary>
    public class GameInputController : MonoBehaviour
    {
        [SerializeField] private HandView? _handView;
        [SerializeField] private TableView? _tableView;
        [SerializeField] private HUDView? _hudView;
        [SerializeField] private AnimationController? _animController;
        [SerializeField] private DiscardPileView? _discardPileView;
        [SerializeField] private RectTransform? _deckHitArea;
        [SerializeField] private RectTransform? _tableHitArea;

        private GameManager? _gameManager;
        private bool _isDragging;
        private bool _inputEnabled = true;
        private Vector2 _dragStartPos;
        private float _dragStartTime;
        private int _dragCardIndex = -1;

        // Reorder drag tracking
        private bool _isReorderCandidate;
        private const float ReorderDragThreshold = 15f;
        private const float DragUpThreshold = 30f;

        /// <summary>
        /// Initialise avec la référence au GameManager.
        /// </summary>
        public void Initialize(GameManager gameManager)
        {
            _gameManager = gameManager;

            // Connecter l'event de réordonnement
            if (_handView != null)
            {
                _handView.OnCardsReordered += OnCardsReordered;
            }
        }

        private void OnDestroy()
        {
            if (_handView != null)
            {
                _handView.OnCardsReordered -= OnCardsReordered;
            }
        }

        /// <summary>
        /// Synchronise le modèle quand les cartes sont réordonnées dans la vue.
        /// </summary>
        private void OnCardsReordered(int fromIndex, int toIndex)
        {
            if (_gameManager?.TurnManager == null) return;
            PlayerModel player = _gameManager.TurnManager.CurrentPlayer;
            if (!player.IsAI)
            {
                player.ReorderHand(fromIndex, toIndex);
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
        }

        private void Update()
        {
            if (!_inputEnabled || _gameManager == null) return;
            if (_gameManager.State != GameState.PlayerTurn) return;
            if (_gameManager.TurnManager == null) return;
            if (_animController != null && _animController.IsAnimating) return;

            PlayerModel currentPlayer = _gameManager.TurnManager.CurrentPlayer;
            if (currentPlayer.IsAI) return;

            HandleInput();
        }

        private void HandleInput()
        {
            Vector2 position;
            bool pressed, held, released;

            if (Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue();
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
                held = Mouse.current.leftButton.isPressed;
                released = Mouse.current.leftButton.wasReleasedThisFrame;
            }
            else if (Touchscreen.current != null)
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
                pressed = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
                held = Touchscreen.current.primaryTouch.press.isPressed;
                released = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            }
            else
            {
                return;
            }

            if (pressed) OnPointerDown(position);
            else if (held && _isDragging) OnPointerDrag(position);
            else if (released) OnPointerUp(position);
        }

        // ──────────────────────────────────────────────
        //  POINTER DOWN
        // ──────────────────────────────────────────────

        private void OnPointerDown(Vector2 pos)
        {
            if (_gameManager?.TurnManager == null) return;
            TurnManager tm = _gameManager.TurnManager;
            TurnPhase phase = tm.CurrentPhase;

            // ── Phase DRAW ──
            if (phase == TurnPhase.Draw)
            {
                if (HitsRect(_deckHitArea, pos))
                {
                    DrawFromDeckWithAnimation();
                    return;
                }

                // Clic sur une pile de défausse (via DiscardPileView)
                if (_discardPileView != null && _discardPileView.GetPileAtPosition(pos, out int pileIdx))
                {
                    DrawFromDiscardWithAnimation(pileIdx);
                    return;
                }

                // Permettre le reorder pendant la phase Draw
                if (_handView != null)
                {
                    int idx = _handView.GetCardIndexAtPosition(pos);
                    if (idx >= 0)
                    {
                        _dragStartPos = pos;
                        _dragStartTime = Time.time;
                        _dragCardIndex = idx;
                        _isReorderCandidate = true;
                        _isDragging = true;
                    }
                }
                return;
            }

            // ── Phase LAY DOWN : multi-sélection + reorder possible ──
            if (phase == TurnPhase.LayDown && _handView != null)
            {
                int idx = _handView.GetCardIndexAtPosition(pos);
                if (idx >= 0)
                {
                    // Start tracking pour distinguer click vs drag (reorder)
                    _dragStartPos = pos;
                    _dragStartTime = Time.time;
                    _dragCardIndex = idx;
                    _isReorderCandidate = true;
                    _isDragging = true;
                    return;
                }

                if (HitsRect(_tableHitArea, pos) || IsAboveHand(pos))
                {
                    TryLayDownSelectedCards();
                    return;
                }
                return;
            }

            // ── Phase ADD TO MELDS ──
            if (phase == TurnPhase.AddToMelds && _handView != null)
            {
                int idx = _handView.GetCardIndexAtPosition(pos);
                if (idx >= 0)
                {
                    _handView.SelectSingleCard(idx);
                    _isDragging = true;
                    _isReorderCandidate = false;
                    _dragCardIndex = idx;
                    _dragStartPos = pos;
                    _handView.BeginDrag(idx, pos);
                    return;
                }
                return;
            }

            // ── Phase DISCARD ──
            if (phase == TurnPhase.Discard && _handView != null)
            {
                int idx = _handView.GetCardIndexAtPosition(pos);
                if (idx >= 0)
                {
                    if (_handView.IsDoubleClick(idx))
                    {
                        DiscardCard(_handView.SelectedCard!.CardModel);
                        return;
                    }

                    _handView.SelectSingleCard(idx);
                    _isDragging = true;
                    _isReorderCandidate = true;
                    _dragCardIndex = idx;
                    _dragStartPos = pos;
                    _dragStartTime = Time.time;
                    return;
                }
            }
        }

        // ──────────────────────────────────────────────
        //  POINTER DRAG
        // ──────────────────────────────────────────────

        private void OnPointerDrag(Vector2 pos)
        {
            if (_handView == null) return;

            TurnPhase phase = _gameManager?.TurnManager?.CurrentPhase ?? TurnPhase.Draw;
            float dragDist = Vector2.Distance(pos, _dragStartPos);

            // Phase DRAW : reorder seulement (pas d'action drag)
            if (phase == TurnPhase.Draw && _isReorderCandidate)
            {
                if (dragDist > ReorderDragThreshold)
                {
                    if (!_handView.IsReorderDragging)
                    {
                        _handView.BeginReorderDrag(_dragCardIndex, _dragStartPos);
                    }
                    _handView.ContinueReorderDrag(pos);
                }
                return;
            }

            // Phase LAY DOWN : drag horizontal = reorder, drag vers le haut = défausse
            if (phase == TurnPhase.LayDown && _isReorderCandidate)
            {
                if (dragDist > ReorderDragThreshold)
                {
                    // Si déjà en mode reorder, on y reste
                    if (_handView.IsReorderDragging)
                    {
                        _handView.ContinueReorderDrag(pos);
                        return;
                    }

                    bool draggingUp = (pos.y - _dragStartPos.y) > DragUpThreshold;

                    if (draggingUp)
                    {
                        // Action drag : intention de défausser → on isole la carte tirée
                        _isReorderCandidate = false;
                        _handView.SelectSingleCard(_dragCardIndex);
                        _handView.BeginDrag(_dragCardIndex, _dragStartPos);
                        _handView.ContinueDrag(pos);
                    }
                    else
                    {
                        // Reorder drag (horizontal)
                        _handView.BeginReorderDrag(_dragCardIndex, _dragStartPos);
                        _handView.ContinueReorderDrag(pos);
                    }
                }
                return;
            }

            // Phase DISCARD : décide entre reorder et action drag
            if (phase == TurnPhase.Discard && _isReorderCandidate)
            {
                if (dragDist > ReorderDragThreshold)
                {
                    // Si on drag vers le haut → action drag (défausse)
                    bool draggingUp = (pos.y - _dragStartPos.y) > DragUpThreshold;

                    if (draggingUp)
                    {
                        _isReorderCandidate = false;
                        _handView.BeginDrag(_dragCardIndex, _dragStartPos);
                        _handView.ContinueDrag(pos);
                    }
                    else
                    {
                        // Reorder drag (horizontal)
                        if (!_handView.IsReorderDragging)
                        {
                            _handView.BeginReorderDrag(_dragCardIndex, _dragStartPos);
                        }
                        _handView.ContinueReorderDrag(pos);
                    }
                }
                return;
            }

            // Action drag normal (AddToMelds ou Discard confirmé)
            if (_handView.IsReorderDragging)
            {
                _handView.ContinueReorderDrag(pos);
            }
            else
            {
                _handView.ContinueDrag(pos);
            }
        }

        // ──────────────────────────────────────────────
        //  POINTER UP
        // ──────────────────────────────────────────────

        private void OnPointerUp(Vector2 pos)
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;

            if (_gameManager?.TurnManager == null || _handView == null)
            {
                FinishDrag(pos);
                return;
            }

            TurnManager tm = _gameManager.TurnManager;

            // Si c'était un reorder drag, terminer le réordonnement
            if (_handView.IsReorderDragging)
            {
                _handView.EndReorderDrag();
                _isReorderCandidate = false;
                _dragCardIndex = -1;
                return;
            }

            float dragDist = Vector2.Distance(pos, _dragStartPos);

            // Phase LAY DOWN : si c'était juste un click (pas un drag), toggle selection
            if (tm.CurrentPhase == TurnPhase.LayDown && dragDist < ReorderDragThreshold)
            {
                _handView.ToggleCardSelection(_dragCardIndex);
                _isReorderCandidate = false;
                _dragCardIndex = -1;
                return;
            }

            // Phase DISCARD : un simple clic sur une carte la défausse directement
            // (plus besoin de double-clic ou drag — zéro friction)
            if (tm.CurrentPhase == TurnPhase.Discard && _isReorderCandidate && dragDist < ReorderDragThreshold)
            {
                CardView? clicked = _dragCardIndex >= 0
                    ? _handView.GetCardViewAt(_dragCardIndex)
                    : null;
                _isReorderCandidate = false;
                _dragCardIndex = -1;

                if (clicked != null)
                {
                    CardModel toDiscard = clicked.CardModel;
                    _handView.DeselectAll();
                    DiscardCard(toDiscard);
                }
                return;
            }

            CardView? dragged = _handView.SelectedCard;

            if (dragged == null)
            {
                FinishDrag(pos);
                return;
            }

            CardModel card = dragged.CardModel;

            // Phase Discard : drop sur zone de défausse ou au-dessus de la main
            if (tm.CurrentPhase == TurnPhase.Discard)
            {
                if (IsDropOnDiscardZone(pos))
                {
                    _handView.EndDrag(pos);
                    _handView.DeselectAll();
                    DiscardCard(card);
                    return;
                }
            }

            // Phase LayDown : drop sur défausse → skip LayDown puis défausser (fin de tour)
            if (tm.CurrentPhase == TurnPhase.LayDown)
            {
                if (IsDropOnDiscardZone(pos))
                {
                    _handView.EndDrag(pos);
                    _handView.DeselectAll();
                    tm.SkipLayDown();
                    DiscardCard(card);
                    return;
                }
            }

            // Phase AddToMelds : drop sur une combinaison ou défausse
            if (tm.CurrentPhase == TurnPhase.AddToMelds)
            {
                if (_tableView != null &&
                    _tableView.GetMeldAtPosition(pos, out int ownerIdx, out int meldIdx))
                {
                    _handView.EndDrag(pos);
                    _handView.DeselectAll();
                    if (!tm.AddToMeld(card, ownerIdx, meldIdx))
                    {
                        _hudView?.ShowStatus("Cette carte ne peut pas être ajoutée ici");
                        if (_animController != null)
                        {
                            _animController.AnimateShake(dragged.RectTransform);
                        }
                    }
                    return;
                }

                // Strict : uniquement la pile de défausse compte (évite les drops manqués
                // sur les combinaisons qui partiraient en défausse accidentellement)
                if (IsDropOnDiscardPile(pos))
                {
                    _handView.EndDrag(pos);
                    _handView.DeselectAll();
                    tm.SkipAddToMelds();
                    DiscardCard(card);
                    return;
                }
            }

            FinishDrag(pos);
        }

        /// <summary>
        /// Vrai si la position correspond à la zone de défausse (au-dessus de la main
        /// ou directement sur une pile de défausse). Lenient : utile en LayDown/Discard.
        /// </summary>
        private bool IsDropOnDiscardZone(Vector2 pos)
        {
            if (IsAboveHand(pos)) return true;
            return IsDropOnDiscardPile(pos);
        }

        /// <summary>
        /// Vrai uniquement si le drop est directement sur une pile de défausse.
        /// </summary>
        private bool IsDropOnDiscardPile(Vector2 pos)
        {
            return _discardPileView != null &&
                   _discardPileView.GetPileAtPosition(pos, out int _);
        }

        private void FinishDrag(Vector2 pos)
        {
            if (_handView != null)
            {
                if (_handView.IsReorderDragging)
                {
                    _handView.EndReorderDrag();
                }
                else
                {
                    _handView.EndDrag(pos);
                }
            }
            _isReorderCandidate = false;
            _dragCardIndex = -1;
        }

        // ──────────────────────────────────────────────
        //  ACTIONS DE JEU
        // ──────────────────────────────────────────────

        private void DrawFromDeckWithAnimation()
        {
            if (_gameManager?.TurnManager == null || _handView == null) return;

            TurnManager tm = _gameManager.TurnManager;

            CardModel? drawn = _gameManager.DeckManager?.DrawFromPile();
            if (!drawn.HasValue) return;

            CardModel card = drawn.Value;

            // ORDRE IMPORTANT : lancer l'animation AVANT AddToHand, pour que
            // HandView soit en mode _animatingDraw quand HandChangedEvent arrive
            // (sinon la main serait rebuild instantanément → carte en doublon).
            _handView.AddCardWithAnimation(card);
            tm.CurrentPlayer.AddToHand(card);

            EventBus.Publish(new CardDrawnEvent
            {
                PlayerIndex = tm.CurrentPlayerIndex,
                Card = card,
                FromDiscard = false,
                DiscardPileIndex = -1
            });

            tm.AdvanceFromDraw();
        }

        private void DrawFromDiscardWithAnimation(int pileIndex)
        {
            if (_gameManager?.TurnManager == null || _handView == null) return;

            TurnManager tm = _gameManager.TurnManager;

            CardModel? drawn = _gameManager.DeckManager?.DrawFromDiscard(pileIndex);
            if (!drawn.HasValue)
            {
                _hudView?.ShowStatus("Defausse vide !");
                if (_animController != null && _deckHitArea != null)
                {
                    _animController.AnimateShake(_deckHitArea);
                }
                return;
            }

            CardModel card = drawn.Value;

            _handView.AddCardWithAnimation(card);
            tm.CurrentPlayer.AddToHand(card);

            EventBus.Publish(new CardDrawnEvent
            {
                PlayerIndex = tm.CurrentPlayerIndex,
                Card = card,
                FromDiscard = true,
                DiscardPileIndex = pileIndex
            });

            tm.AdvanceFromDraw();
        }

        private void TryLayDownSelectedCards()
        {
            if (_gameManager?.TurnManager == null || _handView == null) return;

            TurnManager tm = _gameManager.TurnManager;
            PlayerModel player = tm.CurrentPlayer;
            List<CardModel> selected = _handView.GetSelectedCardModels();

            // Si le joueur a fait une sélection, on valide CETTE sélection.
            // Sinon, on essaye une auto-détection sur toute la main (mode rapide).
            List<CardModel> source = selected.Count > 0 ? selected : player.Hand;
            bool isAutoMode = selected.Count == 0;

            if (LevelValidator.IsLevelComplete(source, player.CurrentLevel,
                    _gameManager.Config, out List<Meld> melds))
            {
                List<Meld> playerMelds = new();
                foreach (Meld m in melds)
                {
                    playerMelds.Add(new Meld(m.Type, m.Cards, player.Index));
                }

                _handView.DeselectAll();
                tm.TryLayDownLevel(playerMelds);
                _hudView?.ShowStatus($"NIVEAU {player.CurrentLevel} !");
            }
            else
            {
                _hudView?.ShowStatus(isAutoMode
                    ? "Aucune combinaison valide dans la main"
                    : "Combinaison invalide !");

                if (_animController != null && !isAutoMode)
                {
                    foreach (CardView cv in _handView.SelectedCards)
                    {
                        _animController.AnimateShake(cv.RectTransform);
                    }
                }
            }
        }

        private void DiscardCard(CardModel card)
        {
            if (_gameManager?.TurnManager == null) return;

            TurnManager tm = _gameManager.TurnManager;

            int targetIndex = -1;
            if (card.IsAction)
            {
                targetIndex = (tm.CurrentPlayerIndex + 1) % _gameManager.Players.Count;
            }

            bool roundEnded = tm.Discard(card, targetIndex);
            if (roundEnded)
            {
                _gameManager.OnRoundEnd(tm.CurrentPlayerIndex);
            }
        }

        private bool IsAboveHand(Vector2 screenPos)
        {
            if (_handView == null) return false;
            RectTransform handRect = _handView.GetComponent<RectTransform>();
            if (handRect == null) return false;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handRect, screenPos, null, out Vector2 local);
            // Tout drag relâché au-dessus du milieu de la zone main compte comme défausse,
            // ça rend le geste beaucoup plus tolérant et évite les "rate" frustrants.
            return local.y > -handRect.rect.height * 0.1f;
        }

        private static bool HitsRect(RectTransform? rect, Vector2 screenPos)
        {
            return rect != null &&
                   RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, null);
        }

        // ──────────────────────────────────────────────
        //  BOUTONS UI
        // ──────────────────────────────────────────────

        public void OnLayDownButton()
        {
            if (_gameManager?.TurnManager == null) return;
            if (_gameManager.TurnManager.CurrentPhase != TurnPhase.LayDown) return;
            TryLayDownSelectedCards();
        }

        public void OnSkipLayDownButton()
        {
            _handView?.DeselectAll();
            _gameManager?.TurnManager?.SkipLayDown();
        }

        public void OnSkipAddToMeldsButton()
        {
            _handView?.DeselectAll();
            _gameManager?.TurnManager?.SkipAddToMelds();
        }
    }
}
