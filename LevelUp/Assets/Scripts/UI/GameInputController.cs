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

        // Seuils de geste calibrés pour un écran desktop (~96 dpi). Sur mobile
        // haute densité (S25 ≈ 416 dpi), 15 px font moins d'1 mm : on convertit
        // en distance physique via le ratio dpi pour garder le même ressenti
        // au doigt qu'à la souris.
        private const float BaseReorderDragThreshold = 15f;
        private const float BaseDragUpThreshold = 30f;

        private static float _dpiScale = -1f;
        private static float DpiScale
        {
            get
            {
                if (_dpiScale < 0f)
                {
                    float dpi = Screen.dpi;
                    _dpiScale = dpi > 0f ? Mathf.Clamp(dpi / 96f, 1f, 4f) : 1f;
                }
                return _dpiScale;
            }
        }

        private static float ReorderDragThreshold => BaseReorderDragThreshold * DpiScale;
        private static float DragUpThreshold => BaseDragUpThreshold * DpiScale;

        // Snap magnétique (phase AddToMelds) : meld le plus proche pendant le
        // drag. En unités canvas (TryGetNearestMeld mesure dans le local space
        // des melds) : déjà indépendant de la résolution, pas de scale dpi.
        private const float MagnetRadius = 90f;
        private bool _hasMagnetTarget;
        private int _magnetOwner = -1;
        private int _magnetMeld = -1;

        private RectTransform? _handRect;

        /// <summary>
        /// Vrai pendant les animations de cartes (pioche, pose, défausse).
        /// On ne bloque QUE les actions qui mutent le jeu — hover, sélection
        /// et reorder restent fluides (zéro verrou d'input global).
        /// </summary>
        private bool ActionsLocked =>
            _animController != null && _animController.IsAnimating;

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

            RestyleDeckPanel();
        }

        /// <summary>
        /// Habille le panneau de pioche posé en scène (rect brut) en dos de
        /// carte : coins arrondis, couleur CardBack, épaisseur de pile
        /// simulée et label discret. La DeckView n'étant pas câblée en scène,
        /// c'est ici (seul détenteur de la référence) qu'on le style.
        /// </summary>
        private void RestyleDeckPanel()
        {
            if (_deckHitArea == null) return;

            Image? img = _deckHitArea.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = UIFactory.RoundedSprite;
                img.type = Image.Type.Sliced;
                img.color = Constants.CardBack;
            }

            // Liseré discret pour détacher la pioche du fond sombre
            GameObject borderObj = new("DeckBorder", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(_deckHitArea, false);
            RectTransform brt = borderObj.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.sizeDelta = new Vector2(4f, 4f);
            Image borderImg = borderObj.GetComponent<Image>();
            borderImg.sprite = UIFactory.RoundedSprite;
            borderImg.type = Image.Type.Sliced;
            borderImg.fillCenter = false;
            borderImg.color = Constants.PanelBorder;
            borderImg.raycastTarget = false;

            // Épaisseur de pile : deux couches décalées derrière
            for (int i = 1; i <= 2; i++)
            {
                GameObject stack = new($"DeckStack_{i}", typeof(RectTransform), typeof(Image));
                stack.transform.SetParent(_deckHitArea, false);
                stack.transform.SetAsFirstSibling();
                RectTransform srt = stack.GetComponent<RectTransform>();
                srt.anchorMin = Vector2.zero;
                srt.anchorMax = Vector2.one;
                srt.sizeDelta = Vector2.zero;
                srt.anchoredPosition = new Vector2(-3f * i, 3f * i);
                Image simg = stack.GetComponent<Image>();
                simg.sprite = UIFactory.RoundedSprite;
                simg.type = Image.Type.Sliced;
                float darken = 1f - 0.25f * i;
                simg.color = new Color(
                    Constants.CardBack.r * darken,
                    Constants.CardBack.g * darken,
                    Constants.CardBack.b * darken, 1f);
                simg.raycastTarget = false;
            }

            // Label "PIOCHE" : petit, espacé, sous une icône de dos de carte
            TextMeshProUGUI? label = _deckHitArea.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = "PIOCHE";
                label.fontSize = 16f;
                label.fontStyle = FontStyles.Bold;
                label.color = Constants.TextSecondary;
                label.characterSpacing = 10f;
                label.alignment = TextAlignmentOptions.Center;
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
            // La HandView affiche toujours la main du joueur LOCAL : le reorder
            // (cosmétique) s'applique à lui, jamais au joueur courant distant/IA.
            PlayerModel? player = GameSession.GetLocalPlayer(_gameManager);
            player?.ReorderHand(fromIndex, toIndex);
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
            // Seul le deal cascade (début de round) bloque tout l'input : les
            // positions de toutes les cartes sont pilotées par les coroutines.
            // Les autres animations ne verrouillent que les actions mutantes
            // (voir ActionsLocked) — sélection et reorder restent disponibles.
            if (_handView != null && _handView.IsAnimatingDeal) return;

            // En ligne : cet appareil ne joue QUE son propre siège. Hors ligne,
            // IsLocalHuman se réduit à !IsAI (comportement solo inchangé).
            PlayerModel currentPlayer = _gameManager.TurnManager.CurrentPlayer;
            if (!GameSession.IsLocalHuman(currentPlayer)) return;

            HandleInput();
        }

        private void HandleInput()
        {
            Vector2 position;
            bool pressed, held, released;

            // Tactile prioritaire dès qu'un doigt est actif : certains Android
            // exposent un périphérique Mouse fantôme qui masquerait le touch
            // si on testait la souris en premier.
            var touch = Touchscreen.current?.primaryTouch;
            bool touchActive = touch != null &&
                (touch.press.isPressed || touch.press.wasReleasedThisFrame);

            if (touchActive)
            {
                position = touch!.position.ReadValue();
                pressed = touch.press.wasPressedThisFrame;
                held = touch.press.isPressed;
                released = touch.press.wasReleasedThisFrame;
            }
            else if (Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue();
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
                held = Mouse.current.leftButton.isPressed;
                released = Mouse.current.leftButton.wasReleasedThisFrame;
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

            // Phases LAY DOWN / DISCARD : horizontal = reorder, vers le haut =
            // action drag. Geste identique, seule différence : LayDown sélectionne
            // la carte au moment de la bascule (Discard l'a déjà fait au press).
            if ((phase == TurnPhase.LayDown || phase == TurnPhase.Discard) && _isReorderCandidate)
            {
                if (dragDist > ReorderDragThreshold)
                {
                    HandleHandCardDrag(pos, selectOnPromote: phase == TurnPhase.LayDown);
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
                // Snap magnétique : en AddToMelds, le meld le plus proche est
                // surligné et attire doucement la carte (spec UX A.3).
                if (phase == TurnPhase.AddToMelds)
                {
                    pos = UpdateMeldMagnet(pos);
                }
                _handView.ContinueDrag(pos);
            }
        }

        /// <summary>
        /// Geste partagé LayDown/Discard : drag horizontal = reorder de la main,
        /// drag vers le haut (ou sortie au-dessus de la main) = action drag.
        /// Bascule reorder → action en cours de geste si le curseur monte.
        /// </summary>
        private void HandleHandCardDrag(Vector2 pos, bool selectOnPromote)
        {
            bool draggingUp = (pos.y - _dragStartPos.y) > DragUpThreshold || IsAboveHand(pos);

            if (_handView!.IsReorderDragging)
            {
                if (draggingUp)
                {
                    _handView.EndReorderDrag();
                    PromoteToActionDrag(pos, selectOnPromote);
                }
                else
                {
                    _handView.ContinueReorderDrag(pos);
                }
                return;
            }

            if (draggingUp)
            {
                PromoteToActionDrag(pos, selectOnPromote);
            }
            else
            {
                _handView.BeginReorderDrag(_dragCardIndex, _dragStartPos);
                _handView.ContinueReorderDrag(pos);
            }
        }

        private void PromoteToActionDrag(Vector2 pos, bool select)
        {
            _isReorderCandidate = false;
            if (select) _handView!.SelectSingleCard(_dragCardIndex);
            _handView!.BeginDrag(_dragCardIndex, _dragStartPos);
            _handView.ContinueDrag(pos);
        }

        /// <summary>
        /// Snap magnétique : surligne le meld le plus proche du curseur (rayon
        /// généreux) et attire doucement la carte vers son centre. Au release,
        /// la cible mémorisée gagne même si on rate le rect de quelques pixels.
        /// </summary>
        private Vector2 UpdateMeldMagnet(Vector2 pos)
        {
            if (_tableView == null) return pos;

            if (_tableView.TryGetNearestMeld(pos, MagnetRadius,
                    out int owner, out int meld, out Vector2 center))
            {
                if (!_hasMagnetTarget || owner != _magnetOwner || meld != _magnetMeld)
                {
                    _tableView.ClearAllHovers();
                    _tableView.SetMeldHover(owner, meld, true);
                    _hasMagnetTarget = true;
                    _magnetOwner = owner;
                    _magnetMeld = meld;
                }
                // Attraction douce vers le centre du meld, le curseur garde la main.
                return Vector2.Lerp(pos, center, 0.35f);
            }

            ClearMagnet();
            return pos;
        }

        private void ClearMagnet()
        {
            if (_hasMagnetTarget) _tableView?.ClearAllHovers();
            _hasMagnetTarget = false;
            _magnetOwner = -1;
            _magnetMeld = -1;
        }

        // ──────────────────────────────────────────────
        //  POINTER UP
        // ──────────────────────────────────────────────

        private void OnPointerUp(Vector2 pos)
        {
            if (!_isDragging) return;
            _isDragging = false;

            // Capture puis reset de la cible magnétique (et de son highlight) —
            // elle est consommée plus bas par la branche AddToMelds.
            bool hadMagnetTarget = _hasMagnetTarget;
            int magnetOwner = _magnetOwner;
            int magnetMeld = _magnetMeld;
            ClearMagnet();

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

            // Phase LayDown : drop sur défausse → avance jusqu'à Discard puis défausse.
            // Si le joueur a déjà posé son niveau ce round, LayDown avance vers
            // AddToMelds (pas Discard) — on doit donc enchaîner les skips.
            if (phase == TurnPhase.LayDown && IsDropOnDiscardZone(pos))
            {
                _handView.EndDrag(pos);
                _handView.DeselectAll();
                AdvanceToDiscardAndDiscard(card);
                return;
            }

            // Phase AddToMelds :
            // 1. Cible magnétique active (meld surligné pendant le drag) → ajout,
            //    même si le release rate le rect de quelques pixels.
            // 2. Sinon drop précis sur une combinaison → ajout.
            // 3. Drop précis sur une pile de défausse → skip + discard.
            // 4. Ailleurs (y compris au-dessus de la main dans le vide) → snap-back.
            //    On n'utilise PAS IsDropOnDiscardZone ici car IsAboveHand est une
            //    bande très large qui recouvre l'aire des melds adverses et ferait
            //    défausser par erreur dès qu'on rate le meld de quelques pixels.
            if (phase == TurnPhase.AddToMelds)
            {
                int ownerIdx = magnetOwner;
                int meldIdx = magnetMeld;
                bool onMeld = hadMagnetTarget;

                if (!onMeld && _tableView != null &&
                    _tableView.GetMeldAtPosition(pos, out int o, out int m))
                {
                    onMeld = true;
                    ownerIdx = o;
                    meldIdx = m;
                }

                if (onMeld)
                {
                    _handView.EndDrag(pos);
                    _handView.DeselectAll();
                    ExecuteAddToMeld(card, ownerIdx, meldIdx, dragged);
                    return;
                }

                if (IsDropOnDiscardPile(pos))
                {
                    _handView.EndDrag(pos);
                    _handView.DeselectAll();
                    AdvanceToDiscardAndDiscard(card);
                    return;
                }
            }

            // Drop ailleurs : la carte va lerper vers sa position d'origine via
            // HandView.LerpCardsToTarget. On ajoute un shake léger sur la carte
            // pour signaler "drop invalide" (snap-back animé, spec UX A.3).
            OnInvalidDrop(dragged, pos);
        }

        /// <summary>
        /// Drop ailleurs que sur une zone valide : termine le drag (la carte va
        /// snap-back vers sa position d'origine via HandView.LerpCardsToTarget)
        /// et superpose un léger pulse de scale pour rendre l'échec lisible.
        /// On évite AnimateShake qui mute anchoredPosition et entrerait en conflit
        /// avec le lerp de la main.
        /// </summary>
        private void OnInvalidDrop(CardView dragged, Vector2 pos)
        {
            FinishDrag(pos);
            if (dragged != null && _animController != null)
            {
                _animController.AnimatePulse(dragged.RectTransform);
            }
            EventBus.Publish(new InvalidActionEvent());
        }

        /// <summary>
        /// Enchaîne les SkipPhase nécessaires pour atteindre la phase Discard,
        /// puis défausse. Gère le cas où le joueur a déjà posé son niveau
        /// (LayDown → AddToMelds → Discard).
        /// </summary>
        private void AdvanceToDiscardAndDiscard(CardModel card)
        {
            if (_gameManager?.TurnManager == null) return;
            // Guard ici (et pas seulement dans ExecuteDiscard) : sinon on
            // skipperait des phases sans pouvoir défausser derrière.
            if (ActionsLocked) return;

            int safety = 4;
            while (_gameManager.TurnManager.CurrentPhase != TurnPhase.Discard && safety-- > 0)
            {
                TurnPhase p = _gameManager.TurnManager.CurrentPhase;
                if (p == TurnPhase.Draw) return; // impossible normalement, abandon
                ExecuteSkipPhase(p);
            }

            if (_gameManager.TurnManager.CurrentPhase == TurnPhase.Discard)
            {
                ExecuteDiscard(card);
            }
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
            ClearMagnet();
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
            if (ActionsLocked) return;

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
            if (ActionsLocked) return;

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
                EventBus.Publish(new InvalidActionEvent());
            }
        }

        private void ExecuteLayDown()
        {
            if (_gameManager == null || _handView == null) return;
            if (ActionsLocked) return;

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
                if (!isAutoMode) EventBus.Publish(new InvalidActionEvent());
            }
        }

        private void ExecuteAddToMeld(CardModel card, int ownerIdx, int meldIdx, CardView cardView)
        {
            if (_gameManager == null) return;
            if (ActionsLocked) return;

            CommandResult result = _gameManager.ExecuteCommand(
                new AddToMeldCommand(CurrentPlayerIndex, card, ownerIdx, meldIdx));

            if (!result.Success)
            {
                _hudView?.ShowStatus(result.Message);
                if (_animController != null)
                {
                    _animController.AnimateShake(cardView.RectTransform);
                }
                EventBus.Publish(new InvalidActionEvent());
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
            if (ActionsLocked) return;

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

        /// <summary>
        /// Vrai si le curseur est au-dessus (ou dans la zone haute) de la main.
        /// Indépendant du pivot du RectTransform : on compare à rect.yMax.
        /// </summary>
        private bool IsAboveHand(Vector2 screenPos)
        {
            if (_handView == null) return false;
            // GetComponent caché : IsAboveHand est appelé à chaque frame de drag.
            if (_handRect == null) _handRect = _handView.GetComponent<RectTransform>();
            RectTransform handRect = _handRect;
            if (handRect == null) return false;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handRect, screenPos, null, out Vector2 local);
            // Dans l'espace local du rect, yMax est le bord supérieur (quel que soit le pivot).
            // On tolère une petite marge (20% du haut du rect) pour un seuil plus indulgent.
            float topThreshold = handRect.rect.yMax - handRect.rect.height * 0.2f;
            return local.y > topThreshold;
        }

        private static bool HitsRect(RectTransform? rect, Vector2 screenPos)
        {
            return rect != null &&
                   RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, null);
        }

        // ──────────────────────────────────────────────
        //  BOUTONS UI
        // ──────────────────────────────────────────────

        /// <summary>
        /// Vrai si le joueur courant est un humain jouable depuis cet appareil.
        /// Garde-fou des boutons publics : en ligne, un client ne doit pas pouvoir
        /// émettre une commande pendant le tour d'un autre joueur.
        /// </summary>
        private bool IsLocalTurn =>
            _gameManager?.TurnManager != null &&
            GameSession.IsLocalHuman(_gameManager.TurnManager.CurrentPlayer);

        public void OnLayDownButton()
        {
            if (_gameManager?.TurnManager == null) return;
            if (!IsLocalTurn) return;
            if (_gameManager.TurnManager.CurrentPhase != TurnPhase.LayDown) return;
            ExecuteLayDown();
        }

        public void OnSkipLayDownButton()
        {
            if (!IsLocalTurn) return;
            _handView?.DeselectAll();
            ExecuteSkipPhase(TurnPhase.LayDown);
        }

        public void OnSkipAddToMeldsButton()
        {
            if (!IsLocalTurn) return;
            _handView?.DeselectAll();
            ExecuteSkipPhase(TurnPhase.AddToMelds);
        }

        /// <summary>Tri de la main par valeur croissante (actions en fin).</summary>
        public void OnSortByValueButton()
        {
            SortCurrentPlayerHand(PlayerModel.CompareByValue);
        }

        /// <summary>Tri de la main par couleur (tiebreak par valeur).</summary>
        public void OnSortByColorButton()
        {
            SortCurrentPlayerHand(PlayerModel.CompareByColor);
        }

        private void SortCurrentPlayerHand(System.Comparison<CardModel> comparator)
        {
            if (_gameManager?.TurnManager == null || _handView == null) return;
            if (_isDragging) return; // ne pas perturber un geste en cours

            // Trie la main du joueur LOCAL (celle que la HandView affiche) —
            // cosmétique, donc autorisé même pendant le tour d'un autre joueur.
            PlayerModel? player = GameSession.GetLocalPlayer(_gameManager);
            if (player == null) return;

            player.SortHand(comparator);
            _handView.ApplySortedOrder(player.Hand);
        }
    }
}
