using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
  /// <summary>
  /// Piles de défausse de tous les joueurs (une par joueur), sur le tapis.
  ///
  /// Chaque pile : la carte du dessus (face visible), et dessous "Nom · N".
  /// L'anneau champagne suit la phase du joueur actif :
  ///   - Piocher (tour local) : anneau doux sur les piles qu'on peut ramasser ;
  ///   - autres phases : anneau sur la pile du joueur actif (où ira sa défausse).
  /// Une carte défaussée glisse depuis la main (joueur local) ou la pastille
  /// du joueur. Une seule CardView par pile, réutilisée (aucune création en jeu).
  /// </summary>
  public class DiscardPileView : MonoBehaviour
  {
    [SerializeField] private RectTransform? _container;
    [SerializeField] private GameObject? _cardPrefab;
    [SerializeField] private AnimationController? _animController;
    [SerializeField] private float _pileSpacing = 150f;

    // Carte du dessus d'une pile : 120×180 × 0.9 (maquette : piles ≈ cartes en main).
    private const float PileCardScale = 0.9f;

    /// <summary>Écart horizontal entre les piles (posé par le layout de l'écran).</summary>
    public float PileSpacing
    {
      get => _pileSpacing;
      set => _pileSpacing = value;
    }

    private readonly List<PileSlot> _pileSlots = new();
    private int _activePlayerIndex = -1;
    private TurnPhase _phase = TurnPhase.Draw;
    private DeckManager? _deckManager;
    private GameManager? _gameManager;

    private void OnEnable()
    {
      EventBus.Subscribe<CardDiscardedEvent>(OnCardDiscarded);
      EventBus.Subscribe<CardDrawnEvent>(OnCardDrawn);
      EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
      EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
      EventBus.Subscribe<TurnPhaseChangedEvent>(OnPhaseChanged);
      EventBus.Subscribe<RoundSummaryEvent>(OnRoundSummary);
      EventBus.Subscribe<DeckChangedEvent>(OnDeckChanged);
    }

    private int _lastDeckCount = -1;

    // Pioche qui remonte = défausses remélangées : compteurs à recaler.
    private void OnDeckChanged(DeckChangedEvent evt)
    {
      bool recycled = _lastDeckCount >= 0 && evt.CardsRemaining > _lastDeckCount;
      _lastDeckCount = evt.CardsRemaining;
      if (!recycled) return;
      foreach (PileSlot slot in _pileSlots)
      {
        UpdateCountLabel(slot);
        UpdateEmptyState(slot);
      }
    }

    private void OnDisable()
    {
      EventBus.Unsubscribe<CardDiscardedEvent>(OnCardDiscarded);
      EventBus.Unsubscribe<CardDrawnEvent>(OnCardDrawn);
      EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
      EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
      EventBus.Unsubscribe<TurnPhaseChangedEvent>(OnPhaseChanged);
      EventBus.Unsubscribe<RoundSummaryEvent>(OnRoundSummary);
      EventBus.Unsubscribe<DeckChangedEvent>(OnDeckChanged);
    }

    /// <summary>Lie la partie (joueur local, pour l'anneau d'invite).</summary>
    public void BindGame(GameManager gameManager) => _gameManager = gameManager;

    public void Initialize(int playerCount, List<string> playerNames, DeckManager? deckManager = null)
    {
      ClearSlots();
      _deckManager = deckManager;

      if (_container == null) return;

      float totalWidth = (playerCount - 1) * _pileSpacing;
      float startX = -totalWidth / 2f;

      for (int i = 0; i < playerCount; i++)
      {
        _pileSlots.Add(CreatePileSlot(i, playerNames[i], startX + i * _pileSpacing));
      }

      _phase = TurnPhase.Draw;
      SetActivePlayer(0);
    }

    private PileSlot CreatePileSlot(int playerIndex, string playerName, float xPos)
    {
      GameObject slotObj = new($"DiscardSlot_P{playerIndex}", typeof(RectTransform));
      slotObj.transform.SetParent(_container, false);
      RectTransform slotRt = slotObj.GetComponent<RectTransform>();
      slotRt.anchoredPosition = new Vector2(xPos, 0);
      slotRt.sizeDelta = new Vector2(132f, 200f);

      // Zone "carte" (hit-test et ancrage de la carte), 108×162.
      GameObject cardZone = new("CardZone", typeof(RectTransform), typeof(Image));
      cardZone.transform.SetParent(slotObj.transform, false);
      RectTransform zoneRt = cardZone.GetComponent<RectTransform>();
      zoneRt.sizeDelta = new Vector2(108f, 162f);
      zoneRt.anchoredPosition = new Vector2(0f, 16f);

      // Pile vide : emplacement en creux (aplat très léger), caché sous une carte.
      Image zoneImg = cardZone.GetComponent<Image>();
      UISkin.SetRounded(zoneImg, 13f);
      zoneImg.color = new Color(1f, 1f, 1f, 0.03f);
      zoneImg.raycastTarget = true;

      Image ring = UISkin.Outline(zoneRt, 15f, 2f, new Color(0f, 0f, 0f, 0f), "ActiveRing");
      UISkin.Stretch(ring.rectTransform, -5f);

      GameObject cardAnchor = new("CardAnchor", typeof(RectTransform));
      cardAnchor.transform.SetParent(cardZone.transform, false);
      RectTransform anchorRt = cardAnchor.GetComponent<RectTransform>();
      anchorRt.anchorMin = new Vector2(0.5f, 0.5f);
      anchorRt.anchorMax = new Vector2(0.5f, 0.5f);
      anchorRt.sizeDelta = Vector2.zero;
      anchorRt.anchoredPosition = Vector2.zero;
      // Le ring passe devant la carte
      ring.transform.SetAsLastSibling();

      TextMeshProUGUI labelText = UISkin.Text(slotObj.transform, "Label", "", 13f, UISkin.TextMuted,
          TextWeight.SemiBold, TextAlignmentOptions.Center);
      RectTransform labelRt = labelText.rectTransform;
      labelRt.sizeDelta = new Vector2(150f, 18f);
      labelRt.anchoredPosition = new Vector2(0f, -86f);
      labelText.richText = true;
      labelText.overflowMode = TextOverflowModes.Ellipsis;

      PileSlot slot = new()
      {
        PlayerIndex = playerIndex,
        PlayerName = playerName,
        Root = slotObj,
        CardZoneRect = zoneRt,
        EmptyStateImage = zoneImg,
        Ring = ring,
        LabelText = labelText,
        CardAnchor = cardAnchor
      };

      UpdateCountLabel(slot);
      return slot;
    }

    // ─── État affiché ────────────────────────────────────────────────

    private void UpdateCountLabel(PileSlot slot)
    {
      // Le modèle fait foi : un remélange des défausses dans la pioche vide les
      // piles sans passer par CardDiscarded / CardDrawn.
      if (_deckManager != null) slot.CardCount = _deckManager.GetDiscardCount(slot.PlayerIndex);
      string nameColor = slot.PlayerIndex == _activePlayerIndex ? "#EEE9E0" : "#A7A198";
      slot.LabelText.text = slot.CardCount > 0
          ? $"<color={nameColor}>{slot.PlayerName}</color><color=#A7A198> · {slot.CardCount}</color>"
          : $"<color={nameColor}>{slot.PlayerName}</color>";
    }

    private static void UpdateEmptyState(PileSlot slot)
    {
      if (slot.EmptyStateImage == null) return;
      Color c = slot.EmptyStateImage.color;
      slot.EmptyStateImage.color = new Color(c.r, c.g, c.b, slot.CardCount > 0 ? 0f : 0.03f);
    }

    public void SetActivePlayer(int playerIndex)
    {
      _activePlayerIndex = playerIndex;
      RefreshRings();
    }

    private bool LocalDrawPhase()
    {
      if (_phase != TurnPhase.Draw || _gameManager?.TurnManager == null) return false;
      if (_gameManager.State != GameState.PlayerTurn) return false;
      return GameSession.IsLocalHuman(_gameManager.TurnManager.CurrentPlayer);
    }

    private void RefreshRings()
    {
      bool localDraw = LocalDrawPhase();
      foreach (PileSlot slot in _pileSlots)
      {
        Color ring;
        if (localDraw)
        {
          // On peut ramasser n'importe quelle carte visible : invitation discrète.
          ring = slot.TopCardView != null && slot.TopCardView.gameObject.activeSelf
              ? UISkin.WithAlpha(UISkin.AccentLight, 0.45f)
              : new Color(0f, 0f, 0f, 0f);
        }
        else
        {
          // Ailleurs : la pile où le joueur actif défaussera.
          ring = slot.PlayerIndex == _activePlayerIndex && _phase != TurnPhase.Draw
              ? UISkin.WithAlpha(UISkin.AccentLight, 0.7f)
              : new Color(0f, 0f, 0f, 0f);
        }
        if (slot.Ring != null) slot.Ring.color = ring;
        UpdateCountLabel(slot);
      }
    }

    // ─── Événements ──────────────────────────────────────────────────

    private void OnTurnStarted(TurnStartedEvent evt)
    {
      _phase = evt.Phase;
      SetActivePlayer(evt.PlayerIndex);
    }

    private void OnPhaseChanged(TurnPhaseChangedEvent evt)
    {
      _phase = evt.NewPhase;
      RefreshRings();
    }

    private void OnRoundSummary(RoundSummaryEvent evt)
    {
      _activePlayerIndex = -1;
      _phase = TurnPhase.Draw;
      RefreshRings();
    }

    private void OnCardDrawn(CardDrawnEvent evt)
    {
      if (!evt.FromDiscard) return;
      if (evt.DiscardPileIndex < 0 || evt.DiscardPileIndex >= _pileSlots.Count) return;

      PileSlot slot = _pileSlots[evt.DiscardPileIndex];
      slot.CardCount = Mathf.Max(0, slot.CardCount - 1);

      CardModel? newTop = _deckManager?.PeekDiscard(evt.DiscardPileIndex);
      ShowTop(slot, newTop);
      UpdateCountLabel(slot);
      UpdateEmptyState(slot);
      RefreshRings();
    }

    private void OnCardDiscarded(CardDiscardedEvent evt)
    {
      if (evt.PlayerIndex < 0 || evt.PlayerIndex >= _pileSlots.Count) return;

      PileSlot slot = _pileSlots[evt.PlayerIndex];
      slot.CardCount++;
      CardView? view = ShowTop(slot, evt.Card);
      if (view != null) StartCoroutine(AnimateCardArrival(view, CardOrigins.Of(evt.PlayerIndex, evt.Card)));

      UpdateCountLabel(slot);
      UpdateEmptyState(slot);
      RefreshRings();
    }

    /// <summary>Affiche la carte du dessus (même CardView réutilisée) ou rien si la pile est vide.</summary>
    private CardView? ShowTop(PileSlot slot, CardModel? card)
    {
      if (!card.HasValue)
      {
        if (slot.TopCardView != null) slot.TopCardView.gameObject.SetActive(false);
        return null;
      }

      if (slot.TopCardView == null)
      {
        if (_cardPrefab == null) return null;
        GameObject cardObj = Instantiate(_cardPrefab, slot.CardAnchor.transform);
        slot.TopCardView = cardObj.GetComponent<CardView>();
        if (slot.TopCardView == null) return null;
        slot.TopCardView.SetInteractable(true);
      }

      CardView view = slot.TopCardView;
      view.gameObject.SetActive(true);
      view.Setup(card.Value, true);
      view.SetAlpha(1f);
      RectTransform rt = view.RectTransform;
      rt.anchoredPosition = Vector2.zero;
      rt.localScale = Vector3.one * PileCardScale;
      rt.localRotation = Quaternion.identity;
      return view;
    }

    private IEnumerator AnimateCardArrival(CardView card, CardOrigin? origin)
    {
      if (card == null) yield break;
      RectTransform rt = card.RectTransform;
      Transform parent = rt.parent;

      Vector2 targetPos = Vector2.zero;
      Vector3 targetScale = Vector3.one * PileCardScale;
      Vector2 startPos;
      Vector3 startScale;
      if (origin.HasValue)
      {
        Vector3 local = parent.InverseTransformPoint(origin.Value.Position);
        startPos = new Vector2(local.x, local.y);
        startScale = Vector3.one * (origin.Value.Scale / Mathf.Max(0.0001f, parent.lossyScale.x));
      }
      else
      {
        startPos = Vector2.up * 100f;
        startScale = targetScale * 1.3f;
      }

      float startRot = Random.Range(-10f, 10f);
      rt.anchoredPosition = startPos;
      rt.localScale = startScale;
      rt.localRotation = Quaternion.Euler(0, 0, startRot);

      float duration = GameSettings.ReducedMotion ? 0.12f : origin.HasValue ? 0.32f : 0.25f;
      float elapsed = 0f;
      while (elapsed < duration)
      {
        if (card == null || rt == null) yield break;
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float eased = UITween.EaseOutCubic(t);
        rt.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, eased);
        rt.localScale = Vector3.Lerp(startScale, targetScale, eased);
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(startRot, 0f, eased));
        yield return null;
      }

      if (card == null || rt == null) yield break;
      rt.anchoredPosition = targetPos;
      rt.localScale = targetScale;
      rt.localRotation = Quaternion.identity;
      if (_animController != null) _animController.AnimatePulse(rt);
    }

    /// <summary>
    /// Recale toutes les piles sur le modèle (compteurs + carte du dessus),
    /// sans animation — reprise d'une partie (en ligne ou sauvegardée).
    /// </summary>
    public void SyncFromModel()
    {
      if (_deckManager == null) return;
      for (int i = 0; i < _pileSlots.Count; i++)
      {
        PileSlot slot = _pileSlots[i];
        slot.CardCount = _deckManager.GetDiscardCount(i);
        ShowTop(slot, _deckManager.PeekDiscard(i));
        UpdateCountLabel(slot);
        UpdateEmptyState(slot);
      }
      RefreshRings();
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
      foreach (PileSlot slot in _pileSlots)
      {
        ShowTop(slot, null);
        slot.CardCount = 0;
        UpdateCountLabel(slot);
        UpdateEmptyState(slot);
      }
    }

    public RectTransform? GetPileRect(int playerIndex)
    {
      if (playerIndex < 0 || playerIndex >= _pileSlots.Count) return null;
      return _pileSlots[playerIndex].CardZoneRect;
    }

    public int PileCount => _pileSlots.Count;

    public bool GetPileAtPosition(Vector2 screenPosition, out int pileIndex)
    {
      pileIndex = -1;
      for (int i = 0; i < _pileSlots.Count; i++)
      {
        if (RectTransformUtility.RectangleContainsScreenPoint(
            _pileSlots[i].CardZoneRect, screenPosition, null))
        {
          pileIndex = i;
          return true;
        }
      }
      return false;
    }

    public bool IsInsideDiscardArea(Vector2 screenPosition)
    {
      if (_container == null) return false;
      return RectTransformUtility.RectangleContainsScreenPoint(_container, screenPosition, null);
    }

    private void ClearSlots()
    {
      foreach (PileSlot slot in _pileSlots)
      {
        if (slot.Root != null) Destroy(slot.Root);
      }
      _pileSlots.Clear();
    }

    private class PileSlot
    {
      public int PlayerIndex;
      public string PlayerName = "";
      public GameObject Root = null!;
      public RectTransform CardZoneRect = null!;
      public Image? EmptyStateImage;
      public Image? Ring;
      public TextMeshProUGUI LabelText = null!;
      public GameObject CardAnchor = null!;
      public CardView? TopCardView;
      public int CardCount;
    }
  }
}
