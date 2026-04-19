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
    ///
    /// Principe fondamental : AUCUNE mutation directe du modèle ou du TurnManager.
    /// Toute action de jeu passe par <see cref="GameManager.ExecuteCommand"/>.
    /// Ce contrôleur ne lit que l'état courant (phase, joueur, main) pour
    /// décider quelles commandes envoyer.
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
        /// ReorderHand est public et cosmétique — seul accès direct autorisé.
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
            TurnPhase phase = _gameManager.TurnManager.CurrentPhase;

            // ── Phase DRAW ──
            if (phase == TurnPhase.Draw)
            {
                if (HitsRect(_deckHitArea, pos))
                {
                    ExecuteDrawFromDeck();
                    return;
                }

                if (_discardPileView != null && _discardPileView.GetPileAtPosition(pos, out int pileIdx))
                {
                    ExecuteDrawFromDiscard(pileIdx);
                    return;
                }

                // Reorder pendant la phase Draw
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

            // ── Phase LAY DOWN : multi-sélection + reorder ──
            if (phase == TurnPhase.LayDown && _handView != null)
            {
                int idx = _handView.GetCardIndexAtPosition(pos);
                if (idx >= 0)
                {
                    _dragStartPos = pos;
                    _dragStartTime = Time.time;
                    _dragCardIndex = idx;
                    _isReorderCandidate = true;
                    _isDragging = true;
                    return;
                }

                if (HitsRect(_tableHitArea, pos) || IsAboveHand(pos))
                {
                    ExecuteLayDown();
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
                        ExecuteDiscard(_handView.SelectedCard!.CardModel);
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

            // Phase DRAW : reorder seulement
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

            // Phase LAY DOWN : horizontal = reorder, vers le haut = action drag
            if (phase == TurnPhase.LayDown && _isReorderCandidate)
            {
                if (dragDist > ReorderDragThreshold)
                {
                    if (_handView.IsReorderDragging)
                    {
                        _handView.ContinueReorderDrag(pos);
                        return;
                    }

                    bool draggingUp = (pos.y - _dragStartPos.y) > DragUpThreshold;
                    if (draggingUp)
                    {
                        _isReorderCandidate = false;
                        _handView.SelectSingleCard(_dragCardIndex);
                        _handView.BeginDrag(_dragCardIndex, _dragStartPos);
                        _handView.ContinueDrag(pos);
                    }
                    else
                    {
                        _handView.BeginReorderDrag(_dragCardIndex, _dragStartPos);
                        _handView.ContinueReorderDrag(pos);
                    }
                }
                return;
            }

            // Phase DISCARD : horizontal = reorder, vers le haut = action drag
            if (phase == TurnPhase.Discard && _isReorderCandidate)
            {
                if (dragDist > ReorderDragThreshold)
                {
                    bool draggingUp = (pos.y - _dragStartPos.y) > DragUpThreshold;
                    if (draggingUp)
                    {
                        _isReorderCandidate = false;
                        _handView.BeginDrag(_dragCardIndex, _dragStartPos);
                        _handView.ContinueDrag(pos);
                    }
                    else
                    {
                        if (!_handView.IsReorderDragging)
                        {
                            _handView.BeginReorderDrag(_dragCardIndex, _dragStartPos);
                        }
                        _handView.ContinueReorderDrag(pos);
                    }
                }
                return;
            }

            // Action drag en cours (AddToMelds ou confirmé)
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
            if (!_isDragging) return;
            _isDragging = false;

            if (_gameManager?.TurnManager == null || _handView == null)
            {
                FinishDrag(pos);
                return;
            }

            TurnPhase phase = _gameManager.TurnManager.CurrentPhase;

            // Reorder drag terminé
            if (_handView.IsReorderDragging)
            {
                _handView.EndReorderDrag();
                _isReorderCandidate = false;
                _dragCardIndex = -1;
                return;
            }

            float dragDist = Vector2.Distance(pos, _dragStartPos);

            // Phase LAY DOWN : simple clic = toggle selection
            if (phase == TurnPhase.LayDown && dragDist < ReorderDragThreshold)
            {
                _handView.ToggleCardSelection(_dragCardIndex);
                _isReorderCandidate = false;
                _dragCardIndex = -1;
                return;
            }

            // Phase DISCARD : simple clic = défausse directe (zéro friction)
            if (phase == TurnPhase.Discard && _isReorderCandidate && dragDist < ReorderDragThreshold)
            {
                CardView? clicked = _dragCardIndex >= 0
                    ? _handView.GetCardViewAt(_dragCardIndex)
                    : null;
                _isReorderCandidate = false;
                _dragCardIndex = -1;

                if (clicked != null)
                {
                    _handView.DeselectAll();
                    ExecuteDiscard(clicked.CardModel);
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

            // Phase Discard : drop sur zone de défausse
            if (phase == TurnPhase.Discard && IsDropOnDiscardZone(pos))
            {
                _handView.EndDrag(pos);
                _handView.DeselectAll();
                ExecuteDiscard(card);
                return;
            }

            // Phase LayDown : drop sur défausse → skip + défausser
            if (phase == TurnPhase.LayDown && IsDropOnDiscardZone(pos))
            {
                _handView.EndDrag(pos);
                _handView.DeselectAll();
                ExecuteSkipPhase(TurnPhase.LayDown);
                ExecuteDiscard(card);
                return;
            }

            // Phase AddToMelds : drop sur combinaison ou défausse
            if (phase == TurnPhase.AddToMelds)
            {
                if (_tableView != null &&
                    _tableView.GetMeldAtPosition(pos, out int ownerIdx, out int meldIdx))
                {
                    _handView.EndDrag(pos);
                    _handView.DeselectAll();
                    ExecuteAddToMeld(card, ownerIdx, meldIdx, dragged);
                    return;
                }

                if (IsDropOnDiscardZone(pos))
                {
                    _handView.EndDrag(pos);
                    _handView.DeselectAll();
                    ExecuteSkipPhase(TurnPhase.AddToMelds);
                    ExecuteDiscard(card);
                    return;
                }
            }

            FinishDrag(pos);
        }

        private bool IsDropOnDiscardZone(Vector2 pos)
        {
            return IsAboveHand(pos) || IsDropOnDiscardPile(pos);
        }

        /// <summary>
        /// Drop sur la défausse = sur une pile précise OU dans la bande large
        /// qui contient toutes les piles (zone généreuse, zéro friction).
        /// </summary>
        private bool IsDropOnDiscardPile(Vector2 pos)
        {
            if (_discardPileView == null) return false;
            if (_discardPileView.GetPileAtPosition(pos, out int _)) return true;
            return _discardPileView.IsInsideDiscardArea(pos);
        }

        private void FinishDrag(Vector2 pos)
        {
            if (_handView != null)
            {
                if (_handView.IsReorderDragging)
                    _handView.EndReorderDrag();
                else
                    _handView.EndDrag(pos);
            }
            _isReorderCandidate = false;
            _dragCardIndex = -1;
        }

        // ──────────────────────────────────────────────
        //  COMMANDES DE JEU (via GameManager.ExecuteCommand)
        // ──────────────────────────────────────────────

        private int CurrentPlayerIndex =>
            _gameManager!.TurnManager!.CurrentPlayerIndex;

        private void ExecuteDrawFromDeck()
        {
            if (_gameManager == null || _handView == null) return;

            // Verrouiller l'animation AVANT l'exécution pour que
            // HandChangedEvent soit mis en file d'attente
            _handView.SetAnimatingDraw(true);

            CommandResult result = _gameManager.ExecuteCommand(
                new DrawFromDeckCommand(CurrentPlayerIndex));

            if (result.Success && result.DrawnCard.HasValue)
            {
                _handView.AddCardWithAnimation(result.DrawnCard.Value);
            }
            else
            {
                _handView.SetAnimatingDraw(false);
                _hudView?.ShowStatus(result.Message);
            }
        }

        private void ExecuteDrawFromDiscard(int pileIndex)
        {
            if (_gameManager == null || _handView == null) return;

            _handView.SetAnimatingDraw(true);

            CommandResult result = _gameManager.ExecuteCommand(
                new DrawFromDiscardCommand(CurrentPlayerIndex, pileIndex));

            if (result.Success && result.DrawnCard.HasValue)
            {
                _handView.AddCardWithAnimation(result.DrawnCard.Value);
            }
            else
            {
                _handView.SetAnimatingDraw(false);
                _hudView?.ShowStatus(result.Message);
                if (_animController != null && _deckHitArea != null)
                {
                    _animController.AnimateShake(_deckHitArea);
                }
            }
        }

        private void ExecuteLayDown()
        {
            if (_gameManager == null || _handView == null) return;

            PlayerModel player = _gameManager.TurnManager!.CurrentPlayer;
            List<CardModel> selected = _handView.GetSelectedCardModels();

            // Sélection explicite ou auto-détection sur toute la main
            IReadOnlyList<CardModel> source = selected.Count > 0
                ? selected
                : player.Hand;
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

                CommandResult result = _gameManager.ExecuteCommand(
                    new LayDownLevelCommand(CurrentPlayerIndex, playerMelds));

                if (result.Success)
                {
                    _hudView?.ShowStatus($"NIVEAU {player.CurrentLevel} !");
                }
                else
                {
                    _hudView?.ShowStatus(result.Message);
                }
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

        private void ExecuteAddToMeld(CardModel card, int ownerIdx, int meldIdx, CardView cardView)
        {
            if (_gameManager == null) return;

            CommandResult result = _gameManager.ExecuteCommand(
                new AddToMeldCommand(CurrentPlayerIndex, card, ownerIdx, meldIdx));

            if (!result.Success)
            {
                _hudView?.ShowStatus(result.Message);
                if (_animController != null)
                {
                    _animController.AnimateShake(cardView.RectTransform);
                }
            }
        }

        private void ExecuteSkipPhase(TurnPhase phaseToSkip)
        {
            _gameManager?.ExecuteCommand(
                new SkipPhaseCommand(CurrentPlayerIndex, phaseToSkip));
        }

        private void ExecuteDiscard(CardModel card)
        {
            if (_gameManager == null) return;

            int targetIndex = -1;
            if (card.IsAction)
            {
                targetIndex = (CurrentPlayerIndex + 1) % _gameManager.Players.Count;
            }

            // ExecuteCommand gère automatiquement OnRoundEnd si la main est vide
            _gameManager.ExecuteCommand(
                new DiscardCommand(CurrentPlayerIndex, card, targetIndex));
        }

        // ──────────────────────────────────────────────
        //  HELPERS
        // ──────────────────────────────────────────────

        private bool IsAboveHand(Vector2 screenPos)
        {
            if (_handView == null) return false;
            RectTransform handRect = _handView.GetComponent<RectTransform>();
            if (handRect == null) return false;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handRect, screenPos, null, out Vector2 local);
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
            ExecuteLayDown();
        }

        public void OnSkipLayDownButton()
        {
            _handView?.DeselectAll();
            ExecuteSkipPhase(TurnPhase.LayDown);
        }

        public void OnSkipAddToMeldsButton()
        {
            _handView?.DeselectAll();
            ExecuteSkipPhase(TurnPhase.AddToMelds);
        }
    }
}
