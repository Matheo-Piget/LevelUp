using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

        // Geste commencé sur un bouton / un élément d'interface : ignoré
        // jusqu'au relâchement (sinon "Pause" en phase Poser = clic table).
        private bool _gestureBlocked;
        private static readonly List<RaycastResult> RaycastBuffer = new();

        // "Choisis ta pose" (plusieurs répartitions valides du contrat)
        private LayDownChooser? _chooser;

        // Cible d'une carte « Passe » / « +2 » (3 joueurs ou plus)
        private TargetChooser? _targetChooser;

        // Fin de tour automatique en cours (minuteur écoulé)
        private bool _autoFinishing;

        // Indices "carte jouable" (phase Compléter) à recalculer
        private bool _hintsDirty = true;

        // Carte saisie pendant un drag d'action (peut faire partie d'une sélection multiple).
        private CardView? _actionCard;

        // Combinaisons acceptant la carte saisie (calculées une fois au début du drag).
        private readonly HashSet<(int Owner, int Meld)> _validTargets = new();
        private System.Func<int, int, bool>? _acceptsTarget;

        // Hors de son tour : seul le rangement de la main est permis.
        private bool _reorderOnly;

        // Pile touchée en phase Piocher (paquet = DeckPress, défausse = index), -1 sinon.
        private const int DeckPress = -2;
        private int _pressedPile = -1;

        // Appui commencé sur la table (étape Poser) : la pose part au relâchement,
        // comme la pioche — un doigt qui effleure la table ne pose plus rien.
        private bool _pressedTable;

        // Appui commencé sur la zone des défausses avec une carte sélectionnée :
        // au relâchement, cette carte est défaussée (toucher la carte, puis la défausse).
        private bool _pressedDiscardZone;

        // Action demandée pendant une animation qui verrouille le jeu (carte
        // piochée en vol) : rejouée dès la fin de l'animation au lieu d'être
        // perdue. Une seule en attente — la dernière demandée gagne.
        private System.Action? _queuedAction;
        private int _queuedPlayer = -1;
        private int _queuedRound = -1;
        private float _queuedUntil;
        private const float QueuedActionLifetime = 1f;

        // Phase Défausser : la carte touchée était déjà soulevée (second toucher).
        private bool _tapWasSelected;

        // Contrat faisable avec la main locale (calculé une fois par état de main).
        private int _contractKey = int.MinValue;
        private bool _contractReady;

        // Tri mémorisé à réappliquer (après une pioche, une donne, un +2…).
        private bool _sortDirty;
        private int _deckBackVariant = -1;
        private RectTransform? _deckTop;

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

            _acceptsTarget = (owner, meld) => _validTargets.Contains((owner, meld));

            if (_handView != null)
            {
                _handView.OnCardsReordered += OnCardsReordered;
                _handView.DrawAnimationFinished += MarkSortDirty;
                _handView.SetSortMode(GameSettings.HandSort);
            }

            RestyleDeckPanel();
            EventBus.Subscribe<HandChangedEvent>(OnLocalHandChanged);

            // Indices "jouable" : recalculés quand la phase, la main ou la table changent.
            EventBus.Subscribe<TurnPhaseChangedEvent>(MarkHintsDirty);
            EventBus.Subscribe<TurnStartedEvent>(MarkHintsDirty);
            EventBus.Subscribe<HandChangedEvent>(MarkHintsDirty);
            EventBus.Subscribe<CardAddedToMeldEvent>(MarkHintsDirty);
            EventBus.Subscribe<LevelLaidDownEvent>(MarkHintsDirty);
            EventBus.Subscribe<RoundStartedEvent>(MarkHintsDirty);
            GameSettings.SettingsChanged += OnSettingsChanged;
        }

        private void MarkHintsDirty<T>(T _) => _hintsDirty = true;

        private void OnSettingsChanged()
        {
            _hintsDirty = true;
            // Dos de carte changé dans les options : la pioche suit.
            if (_deckTop != null && _deckBackVariant != GameSettings.CardBack)
            {
                _deckBackVariant = GameSettings.CardBack;
                UISkin.BuildCardBack(_deckTop, 14f, _deckBackVariant);
            }
        }

        private void MarkSortDirty() => _sortDirty = true;

        private void OnLocalHandChanged(HandChangedEvent evt)
        {
            if (evt.PlayerIndex == GameSession.LocalPlayerIndex) _sortDirty = true;
        }

        /// <summary>
        /// Trie la main du joueur local selon le tri mémorisé (avant une donne :
        /// les cartes arrivent déjà rangées). Cosmétique, aucune règle touchée.
        /// </summary>
        public void ApplyPreferredSort(PlayerModel? player)
        {
            if (player == null) return;
            System.Comparison<CardModel>? comparator = SortComparator();
            if (comparator != null) player.SortHand(comparator);
        }

        private static System.Comparison<CardModel>? SortComparator() => GameSettings.HandSort switch
        {
            1 => PlayerModel.CompareByValue,
            2 => PlayerModel.CompareByColor,
            _ => null
        };

        /// <summary>Réapplique le tri mémorisé dès que la main est au repos (glissement animé).</summary>
        private void ApplyPendingSort()
        {
            if (!_sortDirty || _handView == null) return;
            if (_isDragging || _handView.IsReorderDragging || _handView.IsAnimatingDeal
                || _handView.IsAnimatingDraw) return;
            _sortDirty = false;

            System.Comparison<CardModel>? comparator = SortComparator();
            PlayerModel? player = GameSession.GetLocalPlayer(_gameManager);
            if (comparator == null || player == null) return;
            player.SortHand(comparator);
            _handView.ApplySortedOrder(player.Hand);
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

            // Pioche = pile de dos "Indigo" (maquette) : deux cartes décalées
            // derrière pour l'épaisseur, la carte du dessus porte le dos complet.
            for (int i = 2; i >= 1; i--)
            {
                Image layer = UISkin.Rect(_deckHitArea, $"DeckStack_{i}",
                    i == 2 ? UISkin.Hex(0x10151B) : UISkin.Hex(0x131920), 14f);
                RectTransform lrt = layer.rectTransform;
                UISkin.Stretch(lrt);
                lrt.anchoredPosition = new Vector2(4f * i, -4f * i);
                layer.transform.SetAsFirstSibling();
                UISkin.Outline(lrt, 14f, 1f, new Color(1f, 1f, 1f, 0.08f));
            }

            GameObject top = new("DeckTop", typeof(RectTransform), typeof(Image));
            top.transform.SetParent(_deckHitArea, false);
            UISkin.Stretch((RectTransform)top.transform);
            top.GetComponent<Image>().raycastTarget = false;
            _deckTop = (RectTransform)top.transform;
            _deckBackVariant = GameSettings.CardBack;
            UISkin.BuildCardBack(_deckTop, 14f, _deckBackVariant);

            // Le panneau de scène reste la zone de clic, sans visuel propre.
            Image? img = _deckHitArea.GetComponent<Image>();
            if (img != null)
            {
                UISkin.SetRounded(img, 14f);
                img.color = new Color(0f, 0f, 0f, 0f);
            }

            // Le libellé "Pioche · N" est porté par le layout sous la pile.
            TextMeshProUGUI? label = _deckHitArea.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null && label.transform.parent == _deckHitArea) label.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_handView != null)
            {
                _handView.OnCardsReordered -= OnCardsReordered;
                _handView.DrawAnimationFinished -= MarkSortDirty;
            }
            EventBus.Unsubscribe<HandChangedEvent>(OnLocalHandChanged);
            EventBus.Unsubscribe<TurnPhaseChangedEvent>(MarkHintsDirty);
            EventBus.Unsubscribe<TurnStartedEvent>(MarkHintsDirty);
            EventBus.Unsubscribe<HandChangedEvent>(MarkHintsDirty);
            EventBus.Unsubscribe<CardAddedToMeldEvent>(MarkHintsDirty);
            EventBus.Unsubscribe<LevelLaidDownEvent>(MarkHintsDirty);
            EventBus.Unsubscribe<RoundStartedEvent>(MarkHintsDirty);
            GameSettings.SettingsChanged -= OnSettingsChanged;
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

            // Rangement à la main : le tri automatique s'efface (ordre libre).
            if (GameSettings.HandSort != 0)
            {
                GameSettings.HandSort = 0;
                _handView?.SetSortMode(0);
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
        }

        private void Update()
        {
            if (!_inputEnabled || _gameManager == null) return;
            if (_gameManager.TurnManager == null) return;
            bool playing = _gameManager.State == GameState.PlayerTurn;
            if (!playing && _gameManager.State != GameState.EndRound) return;
            // Seul le deal cascade (début de round) bloque tout l'input : les
            // positions de toutes les cartes sont pilotées par les coroutines.
            // Les autres animations ne verrouillent que les actions mutantes
            // (voir ActionsLocked) — sélection et reorder restent disponibles.
            if (_handView != null && _handView.IsAnimatingDeal) return;

            // Choix de pose / de cible ouvert, ou fin de tour automatique : rien d'autre.
            if (LayDownChooser.IsOpen || TargetChooser.IsOpen || _autoFinishing) return;

            // En ligne : cet appareil ne joue QUE son propre siège. Hors de son
            // tour (ou pendant le récap), on peut quand même ranger sa main.
            PlayerModel currentPlayer = _gameManager.TurnManager.CurrentPlayer;
            bool myTurn = playing && GameSession.IsLocalHuman(currentPlayer);
            if (!myTurn && GameSession.GetLocalPlayer(_gameManager) == null) return;

            // Changement de mode en plein geste (le tour arrive) : on termine
            // proprement le geste précédent avant de passer à l'autre mode.
            if (_reorderOnly == myTurn && _isDragging) CancelGesture();
            _reorderOnly = !myTurn;

            HandleInput();
        }

        private void LateUpdate()
        {
            FlushQueuedAction();
            if (_hintsDirty) RefreshPlayableHints();
            ApplyPendingSort();
        }

        /// <summary>
        /// Vrai si une animation verrouille les actions : <paramref name="retry"/>
        /// est alors mis en attente et rejoué dès qu'elle se termine (même tour,
        /// même manche). L'appelant n'a plus qu'à sortir.
        /// </summary>
        private bool Deferred(System.Action retry)
        {
            if (!ActionsLocked) return false;
            if (_gameManager?.TurnManager == null) return true;
            _queuedAction = retry;
            _queuedPlayer = CurrentPlayerIndex;
            _queuedRound = _gameManager.RoundNumber;
            _queuedUntil = Time.unscaledTime + QueuedActionLifetime;
            return true;
        }

        private void FlushQueuedAction()
        {
            if (_queuedAction == null) return;
            if (ActionsLocked)
            {
                // Animation anormalement longue : l'action est abandonnée plutôt
                // que de partir à un moment où le joueur ne l'attend plus.
                if (Time.unscaledTime > _queuedUntil) _queuedAction = null;
                return;
            }

            System.Action action = _queuedAction;
            _queuedAction = null;
            if (_gameManager?.TurnManager == null || _gameManager.State != GameState.PlayerTurn) return;
            if (!IsLocalTurn || CurrentPlayerIndex != _queuedPlayer || _gameManager.RoundNumber != _queuedRound) return;
            if (LayDownChooser.IsOpen || TargetChooser.IsOpen || _autoFinishing) return;
            action();
        }

        /// <summary>
        /// « Défausser » (étapes) : passe la pose et les ajouts pour aller
        /// directement à la défausse. La carte se choisit ensuite d'un toucher.
        /// </summary>
        public void SkipToDiscard()
        {
            if (_gameManager?.TurnManager == null || !IsLocalTurn) return;
            if (_gameManager.State != GameState.PlayerTurn) return;
            TurnPhase phase = _gameManager.TurnManager.CurrentPhase;
            if (phase != TurnPhase.LayDown && phase != TurnPhase.AddToMelds) return;
            if (Deferred(SkipToDiscard)) return;

            CancelGesture();
            _handView?.DeselectAll();
            int safety = 3;
            while (_gameManager.TurnManager.CurrentPhase != TurnPhase.Discard && safety-- > 0)
            {
                TurnPhase p = _gameManager.TurnManager.CurrentPhase;
                if (p != TurnPhase.LayDown && p != TurnPhase.AddToMelds) break;
                ExecuteSkipPhase(p);
            }
        }

        /// <summary>
        /// Phase "Compléter" du joueur local : signale les cartes de la main qui
        /// peuvent compléter une combinaison posée (même test que la commande
        /// AddToMeld : Meld.CanAddCard). Hors de cette phase, aucun indice.
        /// </summary>
        private void RefreshPlayableHints()
        {
            if (_handView == null || _handView.IsAnimatingDeal) return;
            _hintsDirty = false;

            TurnManager? tm = _gameManager?.TurnManager;
            bool active = tm != null
                          && GameSettings.HintPlayable
                          && _gameManager!.State == GameState.PlayerTurn
                          && tm.CurrentPhase == TurnPhase.AddToMelds
                          && GameSession.IsLocalHuman(tm.CurrentPlayer);
            if (!active || _gameManager == null)
            {
                _handView.SetPlayableHints(null);
                return;
            }

            IReadOnlyList<PlayerModel> players = _gameManager.Players;
            bool Playable(CardModel card)
            {
                foreach (PlayerModel p in players)
                {
                    foreach (Meld meld in p.LaidMelds)
                    {
                        if (meld.CanAddCard(card)) return true;
                    }
                }
                return false;
            }

            // Aide « cartes jouables » : si rien ne complète la table, l'étape
            // est passée toute seule (annoncée), le joueur n'a plus qu'à défausser.
            bool any = false;
            foreach (CardModel card in tm!.CurrentPlayer.Hand)
            {
                if (Playable(card)) { any = true; break; }
            }
            if (!any && !_autoFinishing && !_isDragging && tm.CurrentPlayer.Hand.Count > 0)
            {
                _handView.SetPlayableHints(null);
                _handView.DeselectAll();
                ExecuteSkipPhase(TurnPhase.AddToMelds);
                _hudView?.ShowStatus("Rien à compléter : défausse une carte");
                return;
            }

            _handView.SetPlayableHints(Playable);
        }

        /// <summary>
        /// Vrai si le point touche un bouton (Selectable) ou un élément marqué
        /// <see cref="UIInputBlocker"/> : ce n'est alors pas un geste de jeu.
        /// </summary>
        private static bool IsPointerOverBlockingUI(Vector2 screenPos)
        {
            EventSystem? es = EventSystem.current;
            if (es == null) return false;

            PointerEventData data = new(es) { position = screenPos };
            RaycastBuffer.Clear();
            es.RaycastAll(data, RaycastBuffer);
            foreach (RaycastResult hit in RaycastBuffer)
            {
                if (hit.gameObject == null) continue;
                if (hit.gameObject.GetComponentInParent<Selectable>() != null) return true;
                if (hit.gameObject.GetComponentInParent<UIInputBlocker>() != null) return true;
            }
            return false;
        }

        private void HandleInput()
        {
            // Doigt ou souris (ou source scriptée du harnais) : une seule lecture.
            if (!PointerInput.TryRead(out PointerState pointer)) return;
            Vector2 position = pointer.Position;
            bool pressed = pointer.Pressed;
            bool held = pointer.Held;
            bool released = pointer.Released;

            if (pressed)
            {
                // Un appui sur un bouton (pause, réglages, tri, menus…) ne doit
                // jamais déclencher d'action de jeu derrière lui.
                _gestureBlocked = IsPointerOverBlockingUI(position);
                if (!_gestureBlocked) OnPointerDown(position);
            }
            else if (_gestureBlocked)
            {
                if (released) _gestureBlocked = false;
            }
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
            _actionCard = null;

            // ── Hors de son tour : ranger sa main, rien d'autre ──
            if (_reorderOnly)
            {
                int idx = _handView != null ? _handView.GetCardIndexAtPosition(pos) : -1;
                if (idx >= 0)
                {
                    _dragStartPos = pos;
                    _dragCardIndex = idx;
                    _isReorderCandidate = true;
                    _isDragging = true;
                }
                return;
            }

            // ── Phase DRAW ──
            // La pioche se déclenche au RELÂCHEMENT, si le doigt est resté sur la
            // même pile : un geste commencé à côté qui l'effleure ne pioche plus.
            if (phase == TurnPhase.Draw)
            {
                if (HitsRect(_deckHitArea, pos))
                {
                    _pressedPile = DeckPress;
                    _isDragging = true;
                    _dragStartPos = pos;
                    return;
                }

                if (_discardPileView != null && _discardPileView.GetPileAtPosition(pos, out int pileIdx))
                {
                    _pressedPile = pileIdx;
                    _isDragging = true;
                    _dragStartPos = pos;
                    return;
                }

                // Reorder pendant la phase Draw
                if (_handView != null)
                {
                    int idx = _handView.GetCardIndexAtPosition(pos);
                    if (idx >= 0)
                    {
                        _dragStartPos = pos;
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
                    _dragCardIndex = idx;
                    _isReorderCandidate = true;
                    _isDragging = true;
                    return;
                }

                // Une carte sélectionnée, puis la défausse : elle est jetée au relâchement.
                if (TryPressDiscardZone(pos)) return;

                // Toucher la table (et seulement la table) pose le niveau, au
                // RELÂCHEMENT si le doigt y est resté : la pose est irréversible,
                // un geste qui effleure la table ne doit pas la déclencher.
                if (HitsRect(_tableHitArea, pos))
                {
                    _pressedTable = true;
                    _isDragging = true;
                    _dragStartPos = pos;
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
                    _actionCard = _handView.GetCardViewAt(idx);
                    ComputeValidTargets(_actionCard != null ? _actionCard.CardModel : (CardModel?)null);
                    _handView.BeginDrag(idx, pos);
                    return;
                }
                TryPressDiscardZone(pos);
                return;
            }

            // ── Phase DISCARD ──
            if (phase == TurnPhase.Discard && _handView != null)
            {
                int idx = _handView.GetCardIndexAtPosition(pos);
                if (idx >= 0)
                {
                    // Second toucher sur la carte déjà soulevée = défausse (au relâchement).
                    _tapWasSelected = _handView.IsDoubleClick(idx);
                    _handView.SelectSingleCard(idx);
                    _isDragging = true;
                    _isReorderCandidate = true;
                    _dragCardIndex = idx;
                    _dragStartPos = pos;
                    return;
                }
                TryPressDiscardZone(pos);
            }
        }

        /// <summary>
        /// Appui sur la zone des défausses alors qu'une seule carte est
        /// sélectionnée : geste « je touche la carte, puis la défausse ». Rien
        /// n'est joué ici — la défausse part au relâchement, au même endroit.
        /// </summary>
        private bool TryPressDiscardZone(Vector2 pos)
        {
            if (_handView == null || _handView.SelectedCard == null) return false;
            if (!IsDropOnDiscardPile(pos)) return false;
            _pressedDiscardZone = true;
            _isDragging = true;
            _dragStartPos = pos;
            return true;
        }

        // ──────────────────────────────────────────────
        //  POINTER DRAG
        // ──────────────────────────────────────────────

        private void OnPointerDrag(Vector2 pos)
        {
            if (_handView == null) return;

            TurnPhase phase = _gameManager?.TurnManager?.CurrentPhase ?? TurnPhase.Draw;
            float dragDist = Vector2.Distance(pos, _dragStartPos);

            // Phase DRAW (ou hors de son tour) : reorder seulement
            if ((phase == TurnPhase.Draw || _reorderOnly) && _isReorderCandidate)
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
            CardView? card = _handView!.GetCardViewAt(_dragCardIndex);
            // En phase Poser, glisser une carte déjà sélectionnée garde toute la
            // sélection (on emmène le contrat vers la table) ; sinon elle seule.
            if (select && (card == null || !card.IsSelected)) _handView.SelectSingleCard(_dragCardIndex);
            _actionCard = card;
            _handView.BeginDrag(_dragCardIndex, _dragStartPos);
            _handView.ContinueDrag(pos);
        }

        /// <summary>Combinaisons de la table qui acceptent la carte (même test que la commande).</summary>
        private void ComputeValidTargets(CardModel? card)
        {
            _validTargets.Clear();
            if (!card.HasValue || _gameManager == null) return;
            foreach (PlayerModel p in _gameManager.Players)
            {
                for (int m = 0; m < p.LaidMelds.Count; m++)
                {
                    if (p.LaidMelds[m].CanAddCard(card.Value)) _validTargets.Add((p.Index, m));
                }
            }
        }

        /// <summary>
        /// Snap magnétique : surligne le meld le plus proche du curseur (rayon
        /// généreux) et attire doucement la carte vers son centre. Au release,
        /// la cible mémorisée gagne même si on rate le rect de quelques pixels.
        /// Seules les combinaisons qui acceptent la carte attirent.
        /// </summary>
        private Vector2 UpdateMeldMagnet(Vector2 pos)
        {
            if (_tableView == null) return pos;

            if (_tableView.TryGetNearestMeld(pos, MagnetRadius,
                    out int owner, out int meld, out Vector2 center,
                    GameSettings.HintPlayable ? _acceptsTarget : null))
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

            // Pioche : relâché sur la même pile qu'à l'appui.
            if (_pressedPile != -1)
            {
                int pressed = _pressedPile;
                _pressedPile = -1;
                if (phase != TurnPhase.Draw || _reorderOnly) return;
                if (pressed == DeckPress)
                {
                    if (HitsRect(_deckHitArea, pos)) ExecuteDrawFromDeck();
                }
                else if (_discardPileView != null && _discardPileView.GetPileAtPosition(pos, out int releasedPile)
                         && releasedPile == pressed)
                {
                    ExecuteDrawFromDiscard(pressed);
                }
                return;
            }

            // Pose : relâché sur la table, là où l'appui a commencé.
            if (_pressedTable)
            {
                _pressedTable = false;
                if (phase == TurnPhase.LayDown && !_reorderOnly && HitsRect(_tableHitArea, pos)) ExecuteLayDown();
                return;
            }

            // Carte sélectionnée puis défausse touchée : la carte est jetée.
            if (_pressedDiscardZone)
            {
                _pressedDiscardZone = false;
                CardView? chosen = _handView.SelectedCard;
                if (_reorderOnly || chosen == null || !IsDropOnDiscardPile(pos)) return;

                CardModel toDiscard = chosen.CardModel;
                _handView.DeselectAll();
                if (phase == TurnPhase.Discard) ExecuteDiscard(toDiscard);
                else if (phase == TurnPhase.LayDown || phase == TurnPhase.AddToMelds) AdvanceToDiscardAndDiscard(toDiscard);
                return;
            }

            // Reorder drag terminé
            if (_handView.IsReorderDragging)
            {
                _handView.EndReorderDrag();
                _isReorderCandidate = false;
                _dragCardIndex = -1;
                return;
            }

            float dragDist = Vector2.Distance(pos, _dragStartPos);

            // Hors de son tour : un simple toucher ne fait rien.
            if (_reorderOnly)
            {
                FinishDrag(pos);
                return;
            }

            // Phase LAY DOWN : simple clic = toggle selection
            if (phase == TurnPhase.LayDown && dragDist < ReorderDragThreshold)
            {
                _handView.ToggleCardSelection(_dragCardIndex);
                _isReorderCandidate = false;
                _dragCardIndex = -1;
                return;
            }

            // Phase DISCARD : toucher = soulever la carte, second toucher = défausser
            // (ou défausse directe si l'option « Défausse en un toucher » est active).
            if (phase == TurnPhase.Discard && _isReorderCandidate && dragDist < ReorderDragThreshold)
            {
                CardView? clicked = _dragCardIndex >= 0
                    ? _handView.GetCardViewAt(_dragCardIndex)
                    : null;
                _isReorderCandidate = false;
                _dragCardIndex = -1;

                if (clicked != null && (GameSettings.QuickDiscard || _tapWasSelected))
                {
                    _handView.DeselectAll();
                    ExecuteDiscard(clicked.CardModel);
                }
                _tapWasSelected = false;
                return;
            }

            CardView? dragged = _actionCard != null ? _actionCard : _handView.SelectedCard;
            _actionCard = null;
            if (dragged == null)
            {
                FinishDrag(pos);
                return;
            }

            CardModel card = dragged.CardModel;

            // Phase AddToMelds : simple toucher sur une carte → posée directement
            // si une seule combinaison l'accepte (sinon on indique quoi faire).
            if (phase == TurnPhase.AddToMelds && dragDist < ReorderDragThreshold)
            {
                FinishDrag(pos);
                // Sans l'aide « cartes jouables », le jeu ne dit pas où va la carte.
                if (GameSettings.HintPlayable) TapToAddToMeld(card, dragged);
                return;
            }

            // Phase Discard : drop sur zone de défausse (la seule action possible
            // à cette étape : la bande au-dessus de la main compte aussi).
            if (phase == TurnPhase.Discard && IsDropOnDiscardZone(pos))
            {
                _handView.EndDrag(pos);
                _handView.DeselectAll();
                ExecuteDiscard(card);
                return;
            }

            // Phase LayDown :
            //  - drop sur une défausse → avance jusqu'à Discard puis défausse ;
            //  - drop sur la table → pose (la carte glissée rejoint la sélection) ;
            //  - ailleurs → retour en main. Plus de défausse « au-dessus de la
            //    main » : glisser vers la table pour poser ne jette plus de carte.
            if (phase == TurnPhase.LayDown)
            {
                if (IsDropOnDiscardPile(pos))
                {
                    _handView.EndDrag(pos);
                    _handView.DeselectAll();
                    AdvanceToDiscardAndDiscard(card);
                    return;
                }
                if (HitsRect(_tableHitArea, pos))
                {
                    _handView.EndDrag(pos);
                    // Une seule carte glissée : on cherche le contrat dans toute la main.
                    if (_handView.SelectedCards.Count <= 1) _handView.DeselectAll();
                    ExecuteLayDown();
                    return;
                }
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
            if (Deferred(() => AdvanceToDiscardAndDiscard(card))) return;

            // Carte « Passe » / « +2 » : la cible se choisit AVANT de quitter
            // l'étape — annuler le choix ne doit rien avoir changé au tour.
            if (card.IsAction && !card.IsWild && _gameManager.Players.Count > 2)
            {
                TargetChooser? chooser = GetTargetChooser();
                if (chooser != null)
                {
                    int player = CurrentPlayerIndex;
                    int round = _gameManager.RoundNumber;
                    chooser.Show(card.Type, _gameManager.Players, player, target =>
                    {
                        if (_gameManager?.TurnManager == null || !IsLocalTurn) return;
                        if (CurrentPlayerIndex != player || _gameManager.RoundNumber != round) return;
                        if (SkipUntilDiscard()) DiscardTo(card, target);
                    });
                    return;
                }
            }

            if (SkipUntilDiscard()) ExecuteDiscard(card);
        }

        /// <summary>Passe « Poser » et « Compléter » jusqu'à la défausse ; vrai si on y est.</summary>
        private bool SkipUntilDiscard()
        {
            if (_gameManager?.TurnManager == null) return false;
            int safety = 4;
            while (_gameManager.TurnManager.CurrentPhase != TurnPhase.Discard && safety-- > 0)
            {
                TurnPhase p = _gameManager.TurnManager.CurrentPhase;
                if (p != TurnPhase.LayDown && p != TurnPhase.AddToMelds) return false;
                ExecuteSkipPhase(p);
            }
            return _gameManager.TurnManager.CurrentPhase == TurnPhase.Discard;
        }

        /// <summary>
        /// Phase Compléter, simple toucher sur une carte : posée tout de suite si
        /// une seule combinaison l'accepte ; sinon on dit quoi faire.
        /// </summary>
        private void TapToAddToMeld(CardModel card, CardView view)
        {
            if (_validTargets.Count == 1)
            {
                foreach ((int owner, int meld) in _validTargets)
                {
                    _handView?.DeselectAll();
                    ExecuteAddToMeld(card, owner, meld, view);
                    return;
                }
            }

            _hudView?.ShowStatus(_validTargets.Count > 1
                ? "Plusieurs combinaisons possibles : glisse la carte sur celle de ton choix"
                : "Cette carte ne complète aucune combinaison — glisse-la sur ta défausse pour finir");
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
            _actionCard = null;
            _pressedPile = -1;
            _pressedTable = false;
            _pressedDiscardZone = false;
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
            if (Deferred(ExecuteDrawFromDeck)) return;

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
                _hudView?.ShowError(CommandErrorText.For(result));
            }
        }

        private void ExecuteDrawFromDiscard(int pileIndex)
        {
            if (_gameManager == null || _handView == null) return;
            if (Deferred(() => ExecuteDrawFromDiscard(pileIndex))) return;

            _handView.SetAnimatingDraw(true);

            RectTransform? pile = _discardPileView != null ? _discardPileView.GetPileRect(pileIndex) : null;
            Vector3? from = pile != null ? pile.position : (Vector3?)null;

            CommandResult result = _gameManager.ExecuteCommand(
                new DrawFromDiscardCommand(CurrentPlayerIndex, pileIndex));

            if (result.Success && result.DrawnCard.HasValue)
            {
                // La carte part de la défausse ramassée, pas du paquet.
                _handView.AddCardWithAnimation(result.DrawnCard.Value, from);
            }
            else
            {
                _handView.SetAnimatingDraw(false);
                _hudView?.ShowError(CommandErrorText.For(result));
                if (_animController != null && pile != null)
                {
                    _animController.AnimateShake(pile);
                }
                EventBus.Publish(new InvalidActionEvent());
            }
        }

        private void ExecuteLayDown()
        {
            if (_gameManager == null || _handView == null) return;
            if (Deferred(ExecuteLayDown)) return;

            PlayerModel player = _gameManager.TurnManager!.CurrentPlayer;
            List<CardModel> selected = _handView.GetSelectedCardModels();

            // Sans l'aide « détection du contrat », le jeu ne cherche pas le
            // contrat dans la main : c'est au joueur de sélectionner ses cartes.
            if (selected.Count == 0 && !GameSettings.HintContract)
            {
                _hudView?.ShowStatus("Sélectionne les cartes de ton contrat, puis « Poser »");
                return;
            }

            // Sélection explicite ou auto-détection sur toute la main
            IReadOnlyList<CardModel> source = selected.Count > 0
                ? selected
                : player.Hand;
            bool isAutoMode = selected.Count == 0;

            // Toutes les répartitions valides (5 = 4 affichées + "il en existe d'autres").
            List<List<Meld>> options = LevelValidator.FindLayDownOptions(source, player.CurrentLevel,
                _gameManager.Config, 5);

            if (options.Count == 1)
            {
                // Une seule façon de poser : pose directe, zéro friction.
                LayDownWith(options[0]);
            }
            else if (options.Count > 1)
            {
                // Plusieurs répartitions (ex. 7 rouge dans le brelan OU dans la
                // couleur) : c'est au joueur de choisir.
                LayDownChooser? chooser = GetChooser();
                if (chooser == null)
                {
                    LayDownWith(options[0]);
                    return;
                }
                int turnPlayer = CurrentPlayerIndex;
                chooser.Show(options, options.Count > 4, melds =>
                {
                    // Le tour a pu changer pendant le choix (en ligne) : on revérifie.
                    if (_gameManager?.TurnManager == null) return;
                    if (_gameManager.TurnManager.CurrentPhase != TurnPhase.LayDown) return;
                    if (CurrentPlayerIndex != turnPlayer || !IsLocalTurn) return;
                    LayDownWith(melds);
                });
            }
            else
            {
                _hudView?.ShowError(isAutoMode
                    ? "Ta main ne remplit pas encore le contrat"
                    : "Ces cartes ne forment pas le contrat");

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

        /// <summary>Pose le contrat avec la répartition donnée (commande validée par le Core).</summary>
        private void LayDownWith(List<Meld> melds)
        {
            if (_gameManager?.TurnManager == null || _handView == null) return;
            if (Deferred(() => LayDownWith(melds))) return;

            PlayerModel player = _gameManager.TurnManager.CurrentPlayer;
            List<Meld> playerMelds = new();
            foreach (Meld m in melds)
            {
                playerMelds.Add(new Meld(m.Type, new List<CardModel>(m.Cards), player.Index));
            }

            _handView.DeselectAll();

            CommandResult result = _gameManager.ExecuteCommand(
                new LayDownLevelCommand(CurrentPlayerIndex, playerMelds));

            // Succès : annoncé par le HUD (LevelLaidDownEvent), pas de toast en double.
            if (!result.Success) _hudView?.ShowError(CommandErrorText.For(result));
        }

        private LayDownChooser? GetChooser()
        {
            if (_chooser != null) return _chooser;
            Canvas? canvas = _handView != null ? _handView.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return null;
            canvas = canvas.rootCanvas;
            _chooser = gameObject.AddComponent<LayDownChooser>();
            _chooser.Setup(canvas);
            return _chooser;
        }

        /// <summary>
        /// "Préparer la pose" : sélectionne dans la main une combinaison qui
        /// remplit le contrat (le joueur n'a plus qu'à toucher la table, ou à
        /// ajuster sa sélection). Ne joue rien à sa place.
        /// </summary>
        public bool SelectSuggestedLayDown()
        {
            if (_gameManager?.TurnManager == null || _handView == null) return false;
            if (!IsLocalTurn || _gameManager.TurnManager.CurrentPhase != TurnPhase.LayDown) return false;

            PlayerModel player = _gameManager.TurnManager.CurrentPlayer;
            if (player.HasLaidDownThisRound) return false;

            List<List<Meld>> options = LevelValidator.FindLayDownOptions(player.Hand, player.CurrentLevel,
                _gameManager.Config, 1);
            if (options.Count == 0) return false;

            List<CardModel> cards = new();
            foreach (Meld meld in options[0]) cards.AddRange(meld.Cards);
            _handView.SelectCards(cards);
            return true;
        }

        /// <summary>Vrai si le joueur local peut poser son contrat avec sa main actuelle.</summary>
        public bool CanLocalPlayerLayDown()
        {
            if (!GameSettings.HintContract) return false;
            if (_gameManager?.TurnManager == null) return false;
            if (!IsLocalTurn || _gameManager.TurnManager.CurrentPhase != TurnPhase.LayDown) return false;
            PlayerModel player = _gameManager.TurnManager.CurrentPlayer;
            if (player.HasLaidDownThisRound) return false;
            return LocalHandMeetsContract(player);
        }

        /// <summary>
        /// La main du joueur remplit-elle son contrat ? Calculé une seule fois par
        /// état de main (le contrat, la carte contrat et le panneau de sélection
        /// le demandent tous à chaque changement) — aucune aide affichée ici.
        /// </summary>
        public bool LocalHandMeetsContract(PlayerModel player)
        {
            if (_gameManager == null) return false;
            int key;
            unchecked
            {
                key = _gameManager.RoundNumber * 7919 + player.CurrentLevel * 131 + player.Hand.Count;
                foreach (CardModel c in player.Hand) key += (c.Id + 1) * -1640531535;
            }
            if (key != _contractKey)
            {
                _contractKey = key;
                _contractReady = LevelValidator.IsLevelComplete(player.Hand, player.CurrentLevel,
                    _gameManager.Config, out _);
            }
            return _contractReady;
        }

        private void ExecuteAddToMeld(CardModel card, int ownerIdx, int meldIdx, CardView cardView)
        {
            if (_gameManager == null) return;
            if (Deferred(() => ExecuteAddToMeld(card, ownerIdx, meldIdx, cardView))) return;

            CommandResult result = _gameManager.ExecuteCommand(
                new AddToMeldCommand(CurrentPlayerIndex, card, ownerIdx, meldIdx));

            if (!result.Success)
            {
                _hudView?.ShowError(CommandErrorText.For(result));
                if (_animController != null)
                {
                    _animController.AnimatePulse(cardView.RectTransform);
                }
                EventBus.Publish(new InvalidActionEvent());
            }
        }

        private void ExecuteSkipPhase(TurnPhase phaseToSkip)
        {
            _gameManager?.ExecuteCommand(
                new SkipPhaseCommand(CurrentPlayerIndex, phaseToSkip));
        }

        /// <summary>
        /// Défausse une carte. Carte « Passe » / « +2 » : il faut une cible —
        /// l'adversaire à deux joueurs, sinon le joueur la choisit (panneau).
        /// Un joker défaussé n'a pas d'effet (aucune cible).
        /// </summary>
        private void ExecuteDiscard(CardModel card)
        {
            if (_gameManager == null) return;
            if (Deferred(() => ExecuteDiscard(card))) return;

            int player = CurrentPlayerIndex;
            if (card.IsAction && !card.IsWild)
            {
                if (_gameManager.Players.Count > 2)
                {
                    TargetChooser? chooser = GetTargetChooser();
                    if (chooser != null)
                    {
                        chooser.Show(card.Type, _gameManager.Players, player, target =>
                        {
                            // Le tour a pu changer pendant le choix (en ligne / minuteur).
                            if (_gameManager?.TurnManager == null) return;
                            if (CurrentPlayerIndex != player || !IsLocalTurn) return;
                            if (_gameManager.TurnManager.CurrentPhase != TurnPhase.Discard) return;
                            DiscardTo(card, target);
                        });
                        return;
                    }
                }
                DiscardTo(card, TurnAutoPlay.DefaultTarget(player, _gameManager.Players));
                return;
            }

            DiscardTo(card, -1);
        }

        private void DiscardTo(CardModel card, int targetIndex)
        {
            if (_gameManager == null) return;
            if (Deferred(() => DiscardTo(card, targetIndex))) return;

            // ExecuteCommand gère automatiquement OnRoundEnd si la main est vide
            _gameManager.ExecuteCommand(
                new DiscardCommand(CurrentPlayerIndex, card, targetIndex));
        }

        private TargetChooser? GetTargetChooser()
        {
            if (_targetChooser != null) return _targetChooser;
            Canvas? canvas = _handView != null ? _handView.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return null;
            _targetChooser = gameObject.AddComponent<TargetChooser>();
            _targetChooser.Setup(canvas.rootCanvas);
            return _targetChooser;
        }

        // ──────────────────────────────────────────────
        //  FIN DE TOUR AUTOMATIQUE (minuteur)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Minuteur écoulé : termine le tour du joueur local le plus simplement
        /// possible (pioche, passe la pose et les ajouts, défausse la carte la
        /// moins utile). Chaque étape est une commande normale (relayée en ligne).
        /// </summary>
        public void AutoFinishTurn()
        {
            if (_gameManager?.TurnManager == null || !IsLocalTurn || _autoFinishing) return;
            StartCoroutine(AutoFinishRoutine());
        }

        private IEnumerator AutoFinishRoutine()
        {
            _autoFinishing = true;
            CancelGesture();
            if (_chooser != null) _chooser.Hide();
            if (_targetChooser != null) _targetChooser.Hide();

            int round = _gameManager!.RoundNumber;
            int player = CurrentPlayerIndex;

            for (int step = 0; step < 8; step++)
            {
                // Laisser finir les animations qui verrouillent les actions.
                float waited = 0f;
                while ((ActionsLocked || (_handView != null && _handView.IsAnimatingDeal)) && waited < 2.5f)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (_gameManager?.TurnManager == null || _gameManager.State != GameState.PlayerTurn) break;
                if (_gameManager.RoundNumber != round || CurrentPlayerIndex != player || !IsLocalTurn) break;

                IGameCommand? command = TurnAutoPlay.NextCommand(_gameManager);
                if (command == null) break;

                if (command is DrawFromDeckCommand)
                {
                    ExecuteDrawFromDeck();
                }
                else if (command is DiscardCommand discard)
                {
                    _handView?.DeselectAll();
                    DiscardTo(discard.Card, discard.TargetPlayerIndex);
                }
                else
                {
                    _handView?.DeselectAll();
                    _gameManager.ExecuteCommand(command);
                }
                yield return null;
            }

            _autoFinishing = false;
        }

        /// <summary>Abandonne le geste en cours (drag, reorder) : la carte revient dans la main.</summary>
        private void CancelGesture()
        {
            if (_isDragging || (_handView != null && _handView.IsReorderDragging))
            {
                _isDragging = false;
                FinishDrag(_dragStartPos);
            }
            ClearMagnet();
            _gestureBlocked = false;
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

        /// <summary>Tri de la main par valeur croissante (actions en fin), mémorisé.</summary>
        public void OnSortByValueButton()
        {
            if (!SortCurrentPlayerHand(PlayerModel.CompareByValue)) return;
            GameSettings.HandSort = 1;
            _handView?.SetSortMode(1);
            AudioManager.Instance?.PlayClick();
        }

        /// <summary>Tri de la main par couleur (tiebreak par valeur), mémorisé.</summary>
        public void OnSortByColorButton()
        {
            if (!SortCurrentPlayerHand(PlayerModel.CompareByColor)) return;
            GameSettings.HandSort = 2;
            _handView?.SetSortMode(2);
            AudioManager.Instance?.PlayClick();
        }

        private bool SortCurrentPlayerHand(System.Comparison<CardModel> comparator)
        {
            if (_gameManager?.TurnManager == null || _handView == null) return false;
            if (_isDragging) return false; // ne pas perturber un geste en cours

            // Trie la main du joueur LOCAL (celle que la HandView affiche) —
            // cosmétique, donc autorisé même pendant le tour d'un autre joueur.
            PlayerModel? player = GameSession.GetLocalPlayer(_gameManager);
            if (player == null) return false;

            player.SortHand(comparator);
            _handView.ApplySortedOrder(player.Hand);
            return true;
        }
    }
}
