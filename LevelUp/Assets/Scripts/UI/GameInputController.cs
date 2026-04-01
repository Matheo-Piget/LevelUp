using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Contrôleur d'entrée principal du jeu.
    /// Gère : pioche (clic ou drag depuis le deck), multi-sélection pour poser le niveau,
    /// défausse (clic ou drag), et ajout aux combinaisons.
    /// </summary>
    public class GameInputController : MonoBehaviour
    {
        [SerializeField] private HandView? _handView;
        [SerializeField] private TableView? _tableView;
        [SerializeField] private HUDView? _hudView;
        [SerializeField] private AnimationController? _animController;
        [SerializeField] private RectTransform? _deckHitArea;
        [SerializeField] private RectTransform?[]? _discardHitAreas;
        [SerializeField] private RectTransform? _tableHitArea;

        private GameManager? _gameManager;
        private bool _isDragging;
        private bool _isDraggingFromDeck;
        private bool _inputEnabled = true;

        /// <summary>
        /// Initialise avec la référence au GameManager.
        /// </summary>
        public void Initialize(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        /// <summary>
        /// Active/désactive les inputs.
        /// </summary>
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

        /// <summary>
        /// Gère les entrées souris et tactile.
        /// </summary>
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
                // Clic sur le deck → pioche avec animation
                if (HitsRect(_deckHitArea, pos))
                {
                    DrawFromDeckWithAnimation();
                    return;
                }

                // Clic sur une défausse
                if (_discardHitAreas != null)
                {
                    for (int i = 0; i < _discardHitAreas.Length; i++)
                    {
                        if (HitsRect(_discardHitAreas[i], pos))
                        {
                            DrawFromDiscardWithAnimation(i);
                            return;
                        }
                    }
                }
                return;
            }

            // ── Phase LAY DOWN : multi-sélection de cartes ──
            if (phase == TurnPhase.LayDown && _handView != null)
            {
                int idx = _handView.GetCardIndexAtPosition(pos);
                if (idx >= 0)
                {
                    _handView.ToggleCardSelection(idx);
                    return;
                }

                // Clic sur la table → tenter de poser le niveau avec les cartes sélectionnées
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
                    // Double-clic sur la même carte → défausser directement
                    if (_handView.IsDoubleClick(idx))
                    {
                        DiscardCard(_handView.SelectedCard!.CardModel);
                        return;
                    }

                    _handView.SelectSingleCard(idx);
                    _isDragging = true;
                    _handView.BeginDrag(idx, pos);
                    return;
                }
            }
        }

        // ──────────────────────────────────────────────
        //  POINTER DRAG / UP
        // ──────────────────────────────────────────────

        private void OnPointerDrag(Vector2 pos)
        {
            _handView?.ContinueDrag(pos);
        }

        private void OnPointerUp(Vector2 pos)
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;

            if (_gameManager?.TurnManager == null || _handView == null)
            {
                _handView?.EndDrag(pos);
                return;
            }

            TurnManager tm = _gameManager.TurnManager;
            CardView? dragged = _handView.SelectedCard;

            if (dragged == null)
            {
                _handView.EndDrag(pos);
                return;
            }

            CardModel card = dragged.CardModel;

            // Phase Discard : drop sur la zone de défausse ou n'importe où au-dessus de la main
            if (tm.CurrentPhase == TurnPhase.Discard)
            {
                bool droppedOnValidZone = IsAboveHand(pos);

                if (_discardHitAreas != null)
                {
                    foreach (RectTransform? area in _discardHitAreas)
                    {
                        if (HitsRect(area, pos)) { droppedOnValidZone = true; break; }
                    }
                }

                if (droppedOnValidZone)
                {
                    _handView.EndDrag(pos);
                    _handView.DeselectAll();
                    DiscardCard(card);
                    return;
                }
            }

            // Phase AddToMelds : drop sur une combinaison
            if (tm.CurrentPhase == TurnPhase.AddToMelds && _tableView != null)
            {
                if (_tableView.GetMeldAtPosition(pos, out int ownerIdx, out int meldIdx))
                {
                    _handView.EndDrag(pos);
                    _handView.DeselectAll();
                    tm.AddToMeld(card, ownerIdx, meldIdx);
                    return;
                }
            }

            // Drop invalide → retour à la position
            _handView.EndDrag(pos);
        }

        // ──────────────────────────────────────────────
        //  ACTIONS DE JEU
        // ──────────────────────────────────────────────

        /// <summary>
        /// Pioche depuis le deck avec animation.
        /// </summary>
        private void DrawFromDeckWithAnimation()
        {
            if (_gameManager?.TurnManager == null || _handView == null) return;

            TurnManager tm = _gameManager.TurnManager;

            // Piocher la carte dans le modèle
            CardModel? drawn = _gameManager.DeckManager?.DrawFromPile();
            if (!drawn.HasValue) return;

            CardModel card = drawn.Value;
            tm.CurrentPlayer.AddToHand(card);

            // Animer visuellement
            _handView.AddCardWithAnimation(card);

            // Publier l'événement et avancer la phase
            EventBus.Publish(new CardDrawnEvent
            {
                PlayerIndex = tm.CurrentPlayerIndex,
                Card = card,
                FromDiscard = false
            });

            tm.AdvanceFromDraw();

            _hudView?.ShowStatus("Carte piochée !");
        }

        /// <summary>
        /// Pioche depuis une défausse avec animation.
        /// </summary>
        private void DrawFromDiscardWithAnimation(int pileIndex)
        {
            if (_gameManager?.TurnManager == null || _handView == null) return;

            TurnManager tm = _gameManager.TurnManager;

            CardModel? drawn = _gameManager.DeckManager?.DrawFromDiscard(pileIndex);
            if (!drawn.HasValue)
            {
                _hudView?.ShowStatus("Défausse vide !");
                return;
            }

            CardModel card = drawn.Value;
            tm.CurrentPlayer.AddToHand(card);

            _handView.AddCardWithAnimation(card);

            EventBus.Publish(new CardDrawnEvent
            {
                PlayerIndex = tm.CurrentPlayerIndex,
                Card = card,
                FromDiscard = true
            });

            tm.AdvanceFromDraw();
        }

        /// <summary>
        /// Tente de poser le niveau avec les cartes sélectionnées.
        /// </summary>
        private void TryLayDownSelectedCards()
        {
            if (_gameManager?.TurnManager == null || _handView == null) return;

            TurnManager tm = _gameManager.TurnManager;
            PlayerModel player = tm.CurrentPlayer;
            List<CardModel> selected = _handView.GetSelectedCardModels();

            if (selected.Count == 0)
            {
                _hudView?.ShowStatus("Sélectionnez des cartes d'abord !");
                return;
            }

            // Vérifier si les cartes sélectionnées valident le niveau
            if (LevelValidator.IsLevelComplete(player.Hand, player.CurrentLevel,
                    _gameManager.Config, out List<Meld> melds))
            {
                List<Meld> playerMelds = new();
                foreach (Meld m in melds)
                {
                    playerMelds.Add(new Meld(m.Type, m.Cards, player.Index));
                }

                _handView.DeselectAll();
                tm.TryLayDownLevel(playerMelds);
                _hudView?.ShowStatus($"Niveau {player.CurrentLevel} posé !");
            }
            else
            {
                _hudView?.ShowStatus("Ces cartes ne valident pas le niveau !");

                // Shake les cartes sélectionnées pour feedback visuel
                if (_animController != null)
                {
                    foreach (CardView cv in _handView.SelectedCards)
                    {
                        _animController.AnimateShake(cv.RectTransform);
                    }
                }
            }
        }

        /// <summary>
        /// Défausse une carte.
        /// </summary>
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

        /// <summary>
        /// Vérifie si une position est au-dessus de la zone de main.
        /// </summary>
        private bool IsAboveHand(Vector2 screenPos)
        {
            if (_handView == null) return false;
            RectTransform handRect = _handView.GetComponent<RectTransform>();
            if (handRect == null) return false;

            // Position est au-dessus du haut de la main
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handRect, screenPos, null, out Vector2 local);
            return local.y > handRect.rect.height * 0.3f;
        }

        /// <summary>
        /// Vérifie si une position touche un RectTransform donné.
        /// </summary>
        private static bool HitsRect(RectTransform? rect, Vector2 screenPos)
        {
            return rect != null &&
                   RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, null);
        }

        // ──────────────────────────────────────────────
        //  BOUTONS UI
        // ──────────────────────────────────────────────

        /// <summary>
        /// Bouton "Poser le niveau" — utilise la validation automatique.
        /// </summary>
        public void OnLayDownButton()
        {
            if (_gameManager?.TurnManager == null) return;
            if (_gameManager.TurnManager.CurrentPhase != TurnPhase.LayDown) return;

            TryLayDownSelectedCards();
        }

        /// <summary>
        /// Bouton "Passer" — saute la phase de pose du niveau.
        /// </summary>
        public void OnSkipLayDownButton()
        {
            _handView?.DeselectAll();
            _gameManager?.TurnManager?.SkipLayDown();
        }

        /// <summary>
        /// Bouton "Terminer" — saute la phase d'ajout aux combinaisons.
        /// </summary>
        public void OnSkipAddToMeldsButton()
        {
            _handView?.DeselectAll();
            _gameManager?.TurnManager?.SkipAddToMelds();
        }
    }
}
