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
  /// Affiche les piles de défausse de tous les joueurs.
  ///
  /// Direction visuelle : minimaliste, beaucoup d'air. Chaque pile a :
  ///   - un fin liseré coloré (3px) en haut → identité du joueur, sans crier
  ///   - la carte du dessus (face visible) flottant au centre
  ///   - sous la pile : nom du joueur + compteur "· N" en typo discrète
  ///   - pile vide : pas de fond plein, juste un contour pointillé très léger
  ///
  /// Le joueur actif est signalé par un halo indigo très doux derrière la pile
  /// + son nom passe en blanc — cohérent avec la sidebar.
  /// </summary>
  public class DiscardPileView : MonoBehaviour
  {
    [SerializeField] private RectTransform? _container;
    [SerializeField] private GameObject? _cardPrefab;
    [SerializeField] private AnimationController? _animController;
    [SerializeField] private float _pileSpacing = 150f;

    private readonly List<PileSlot> _pileSlots = new();
    private int _playerCount;
    private int _activePlayerIndex = -1;
    private DeckManager? _deckManager;

    // Couleurs joueur saturées : utilisées UNIQUEMENT pour le liseré du dessus,
    // jamais en aplat. Cohérent avec la sidebar où on les utilise pour l'avatar.
    private static readonly Color[] PlayerAccentColors =
    {
            Constants.CardBlue,
            Constants.CardRed,
            Constants.CardGreen,
            Constants.CardPurple,
            Constants.CardOrange,
            Constants.CardYellow
        };

    // Sprite de contour pointillé pour les piles vides — généré une fois, partagé.
    private static Sprite? _dashedBorderSprite;

    private void OnEnable()
    {
      EventBus.Subscribe<CardDiscardedEvent>(OnCardDiscarded);
      EventBus.Subscribe<CardDrawnEvent>(OnCardDrawn);
      EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
      EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
      EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
    }

    private void OnDisable()
    {
      EventBus.Unsubscribe<CardDiscardedEvent>(OnCardDiscarded);
      EventBus.Unsubscribe<CardDrawnEvent>(OnCardDrawn);
      EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
      EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
      EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
    }

    private void OnCardDrawn(CardDrawnEvent evt)
    {
      if (!evt.FromDiscard) return;
      if (evt.DiscardPileIndex < 0 || evt.DiscardPileIndex >= _pileSlots.Count) return;

      PileSlot slot = _pileSlots[evt.DiscardPileIndex];

      if (slot.TopCardView != null)
      {
        Destroy(slot.TopCardView.gameObject);
        slot.TopCardView = null;
      }

      slot.CardCount = Mathf.Max(0, slot.CardCount - 1);
      UpdateCountLabel(slot);
      UpdateEmptyState(slot);

      if (_deckManager == null || _cardPrefab == null) return;

      CardModel? newTop = _deckManager.PeekDiscard(evt.DiscardPileIndex);
      if (!newTop.HasValue) return;

      GameObject cardObj = Instantiate(_cardPrefab, slot.CardAnchor.transform);
      CardView cardView = cardObj.GetComponent<CardView>();
      if (cardView == null) return;

      cardView.Setup(newTop.Value, true);
      cardView.SetInteractable(true);
      cardView.RectTransform.localScale = Vector3.one * 0.65f;
      cardView.RectTransform.anchoredPosition = Vector2.zero;
      slot.TopCardView = cardView;
    }

    private void OnGameStarted(GameStartedEvent evt)
    {
      _playerCount = evt.PlayerCount;
    }

    private void OnTurnStarted(TurnStartedEvent evt)
    {
      SetActivePlayer(evt.PlayerIndex);
    }

    public void Initialize(int playerCount, List<string> playerNames, DeckManager? deckManager = null)
    {
      ClearSlots();
      _playerCount = playerCount;
      _deckManager = deckManager;

      if (_container == null) return;

      float totalWidth = (playerCount - 1) * _pileSpacing;
      float startX = -totalWidth / 2f;

      for (int i = 0; i < playerCount; i++)
      {
        PileSlot slot = CreatePileSlot(i, playerNames[i], startX + i * _pileSpacing);
        _pileSlots.Add(slot);
      }

      SetActivePlayer(0);
    }

    private PileSlot CreatePileSlot(int playerIndex, string playerName, float xPos)
    {
      // ─── Conteneur du slot ─────────────────────────────────────────────
      GameObject slotObj = new($"DiscardSlot_P{playerIndex}", typeof(RectTransform));
      slotObj.transform.SetParent(_container, false);

      RectTransform slotRt = slotObj.GetComponent<RectTransform>();
      slotRt.anchoredPosition = new Vector2(xPos, 0);
      slotRt.sizeDelta = new Vector2(140f, 210f); // un peu plus haut pour loger le label en dessous

      Color accentColor = playerIndex < PlayerAccentColors.Length
          ? PlayerAccentColors[playerIndex]
          : Constants.TextPrimary;

      // ─── Halo "joueur actif" (derrière tout, invisible par défaut) ────
      // Très doux, pas un cadre. Sert juste à signaler "c'est ici qu'on défausse".
      GameObject activeGlow = new("ActiveGlow", typeof(RectTransform), typeof(Image));
      activeGlow.transform.SetParent(slotObj.transform, false);
      RectTransform glowRt = activeGlow.GetComponent<RectTransform>();
      glowRt.sizeDelta = new Vector2(150f, 195f);
      glowRt.anchoredPosition = new Vector2(0f, 12f); // centré sur la zone carte, pas le label
      Image glowImg = activeGlow.GetComponent<Image>();
      glowImg.sprite = UIFactory.SoftShadowSprite;
      glowImg.color = new Color(0f, 0f, 0f, 0f);
      glowImg.raycastTarget = false;

      // ─── Zone "carte" (hit-test pour le drop, et ancrage de la carte) ─
      // C'est la zone visuelle de la pile : 120×165, centrée vers le haut du slot.
      GameObject cardZone = new("CardZone", typeof(RectTransform), typeof(Image));
      cardZone.transform.SetParent(slotObj.transform, false);

      RectTransform zoneRt = cardZone.GetComponent<RectTransform>();
      zoneRt.sizeDelta = new Vector2(120f, 165f);
      zoneRt.anchoredPosition = new Vector2(0f, 12f); // décalé vers le haut (label en dessous)

      // Fond de pile vide : contour pointillé fin, pas de remplissage.
      // Dès qu'une carte arrive on cache ce fond.
      Image zoneImg = cardZone.GetComponent<Image>();
      zoneImg.sprite = GetDashedBorderSprite();
      zoneImg.type = Image.Type.Sliced;
      zoneImg.color = new Color(1f, 1f, 1f, 0.10f);
      zoneImg.raycastTarget = true;

      // ─── Liseré coloré 3px en haut de la zone carte ───────────────────
      // C'est l'identité visuelle du joueur — fine, latérale, pas envahissante.
      GameObject accentBar = new("AccentBar", typeof(RectTransform), typeof(Image));
      accentBar.transform.SetParent(cardZone.transform, false);
      RectTransform barRt = accentBar.GetComponent<RectTransform>();
      barRt.anchorMin = new Vector2(0f, 1f);
      barRt.anchorMax = new Vector2(1f, 1f);
      barRt.pivot = new Vector2(0.5f, 1f);
      barRt.sizeDelta = new Vector2(-24f, 3f); // 12px de marge de chaque côté
      barRt.anchoredPosition = new Vector2(0f, -8f);
      Image barImg = accentBar.GetComponent<Image>();
      barImg.sprite = UIFactory.RoundedSprite;
      barImg.type = Image.Type.Sliced;
      barImg.color = accentColor;
      barImg.raycastTarget = false;

      // ─── Ancrage des cartes (au centre de la zone carte) ──────────────
      GameObject cardAnchor = new("CardAnchor", typeof(RectTransform));
      cardAnchor.transform.SetParent(cardZone.transform, false);
      RectTransform anchorRt = cardAnchor.GetComponent<RectTransform>();
      anchorRt.anchorMin = new Vector2(0.5f, 0.5f);
      anchorRt.anchorMax = new Vector2(0.5f, 0.5f);
      anchorRt.sizeDelta = Vector2.zero;
      anchorRt.anchoredPosition = Vector2.zero;

      // ─── Label sous la pile : "Nom · 0" en typo discrète ─────────────
      GameObject labelObj = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
      labelObj.transform.SetParent(slotObj.transform, false);

      RectTransform labelRt = labelObj.GetComponent<RectTransform>();
      labelRt.sizeDelta = new Vector2(140f, 18f);
      labelRt.anchoredPosition = new Vector2(0f, -90f);

      TextMeshProUGUI labelText = labelObj.GetComponent<TextMeshProUGUI>();
      labelText.fontSize = 11f;
      labelText.color = Constants.TextMuted;
      labelText.alignment = TextAlignmentOptions.Center;
      labelText.fontStyle = FontStyles.Normal;
      labelText.characterSpacing = 2f;
      labelText.richText = true;
      labelText.raycastTarget = false;

      PileSlot slot = new()
      {
        PlayerIndex = playerIndex,
        PlayerName = playerName,
        Root = slotObj,
        SlotRect = slotRt,
        CardZoneRect = zoneRt,
        EmptyStateImage = zoneImg,
        ActiveGlow = glowImg,
        LabelText = labelText,
        CardAnchor = cardAnchor,
        TopCardView = null,
        CardCount = 0
      };

      UpdateCountLabel(slot);
      return slot;
    }

    /// <summary>
    /// Met à jour le label "Nom · N" sous la pile. Le compteur s'estompe à 0
    /// pour ne pas distraire d'une pile vide.
    /// </summary>
    private void UpdateCountLabel(PileSlot slot)
    {
      string nameColor = slot.PlayerIndex == _activePlayerIndex ? "#F9FAFB" : "#9CA3AF";
      if (slot.CardCount > 0)
      {
        slot.LabelText.text =
            $"<color={nameColor}>{slot.PlayerName.ToUpper()}</color>" +
            $"<color=#4B5563>  ·  </color>" +
            $"<color=#9CA3AF>{slot.CardCount}</color>";
      }
      else
      {
        slot.LabelText.text =
            $"<color={nameColor}>{slot.PlayerName.ToUpper()}</color>";
      }
    }

    /// <summary>Affiche / cache le contour pointillé selon que la pile est vide ou non.</summary>
    private static void UpdateEmptyState(PileSlot slot)
    {
      if (slot.EmptyStateImage == null) return;
      // On laisse toujours le hit-test actif (raycastTarget) mais on cache visuellement
      // le contour quand une carte couvre la zone.
      float a = slot.CardCount > 0 ? 0f : 0.10f;
      Color c = slot.EmptyStateImage.color;
      slot.EmptyStateImage.color = new Color(c.r, c.g, c.b, a);
    }

    public void SetActivePlayer(int playerIndex)
    {
      _activePlayerIndex = playerIndex;
      for (int i = 0; i < _pileSlots.Count; i++)
      {
        PileSlot slot = _pileSlots[i];
        bool active = i == playerIndex;

        if (slot.ActiveGlow != null)
        {
          Color g = Constants.AccentLight;
          g.a = active ? 0.18f : 0f; // halo très doux
          slot.ActiveGlow.color = g;
        }
        UpdateCountLabel(slot);
      }
    }

    private void OnCardDiscarded(CardDiscardedEvent evt)
    {
      if (evt.PlayerIndex < 0 || evt.PlayerIndex >= _pileSlots.Count) return;

      PileSlot slot = _pileSlots[evt.PlayerIndex];
      slot.CardCount++;

      if (slot.TopCardView != null)
      {
        Destroy(slot.TopCardView.gameObject);
        slot.TopCardView = null;
      }

      if (_cardPrefab == null) return;

      GameObject cardObj = Instantiate(_cardPrefab, slot.CardAnchor.transform);
      CardView cardView = cardObj.GetComponent<CardView>();
      if (cardView != null)
      {
        cardView.Setup(evt.Card, true);
        cardView.SetInteractable(true);
        cardView.RectTransform.localScale = Vector3.one * 0.65f;
        slot.TopCardView = cardView;

        StartCoroutine(AnimateCardArrival(cardView, slot));
      }

      UpdateCountLabel(slot);
      UpdateEmptyState(slot);
    }

    private IEnumerator AnimateCardArrival(CardView card, PileSlot slot)
    {
      if (card == null) yield break;
      RectTransform rt = card.RectTransform;
      CanvasGroup cg = card.GetComponent<CanvasGroup>();
      if (rt == null) yield break;

      Vector2 targetPos = Vector2.zero;
      rt.anchoredPosition = targetPos + Vector2.up * 100f;
      rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-8f, 8f));

      Vector3 startScale = rt.localScale * 1.3f;
      Vector3 targetScale = rt.localScale;
      rt.localScale = startScale;

      if (cg != null) cg.alpha = 0f;

      float elapsed = 0f;
      float duration = 0.25f;

      while (elapsed < duration)
      {
        if (card == null || rt == null) yield break;
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float eased = t < 0.7f
            ? Mathf.Lerp(0f, 1.08f, t / 0.7f)
            : Mathf.Lerp(1.08f, 1f, (t - 0.7f) / 0.3f);

        rt.anchoredPosition = Vector2.Lerp(targetPos + Vector2.up * 100f, targetPos, eased);
        rt.localScale = Vector3.Lerp(startScale, targetScale, eased);

        if (cg != null) cg.alpha = Mathf.Min(1f, t * 3f);

        yield return null;
      }

      if (card == null || rt == null) yield break;
      rt.anchoredPosition = targetPos;
      rt.localScale = targetScale;
      if (cg != null) cg.alpha = 1f;

      if (_animController != null && rt != null && card != null)
      {
        _animController.AnimatePulse(rt);
      }
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
      foreach (PileSlot slot in _pileSlots)
      {
        if (slot.TopCardView != null)
        {
          Destroy(slot.TopCardView.gameObject);
          slot.TopCardView = null;
        }
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
      return RectTransformUtility.RectangleContainsScreenPoint(
          _container, screenPosition, null);
    }

    public int GetNearestPileIndex(Vector2 screenPosition)
    {
      if (_pileSlots.Count == 0) return -1;
      int best = 0;
      float bestDist = float.MaxValue;
      for (int i = 0; i < _pileSlots.Count; i++)
      {
        Vector3 world = _pileSlots[i].CardZoneRect.position;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
        float d = Vector2.SqrMagnitude(screen - screenPosition);
        if (d < bestDist) { bestDist = d; best = i; }
      }
      return best;
    }

    // ─── Sprite de contour pointillé partagé ──────────────────────────────
    private static Sprite GetDashedBorderSprite()
    {
      if (_dashedBorderSprite != null) return _dashedBorderSprite;

      // Texture 64×64 avec un cadre pointillé : segments de 4px tous les 8px.
      // Sliced 9-slice avec border de 8px pour préserver les coins.
      const int size = 64;
      const int border = 8;
      const int dashLen = 4;
      const int dashGap = 4;

      Texture2D tex = new(size, size, TextureFormat.RGBA32, false)
      {
        wrapMode = TextureWrapMode.Clamp,
        filterMode = FilterMode.Bilinear
      };

      Color[] pixels = new Color[size * size];
      Color transparent = new(1f, 1f, 1f, 0f);
      Color line = Color.white;

      for (int y = 0; y < size; y++)
      {
        for (int x = 0; x < size; x++)
        {
          bool onTop = y >= size - 2;
          bool onBottom = y < 2;
          bool onLeft = x < 2;
          bool onRight = x >= size - 2;

          if (!(onTop || onBottom || onLeft || onRight))
          {
            pixels[y * size + x] = transparent;
            continue;
          }

          // Pour les bords haut/bas → on alterne dashes selon x
          // Pour les bords gauche/droite → selon y
          int t = (onTop || onBottom) ? x : y;
          bool inDash = (t % (dashLen + dashGap)) < dashLen;

          pixels[y * size + x] = inDash ? line : transparent;
        }
      }

      tex.SetPixels(pixels);
      tex.Apply();

      _dashedBorderSprite = Sprite.Create(tex,
          new Rect(0, 0, size, size),
          new Vector2(0.5f, 0.5f),
          100f, 0, SpriteMeshType.FullRect,
          new Vector4(border, border, border, border));

      return _dashedBorderSprite;
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
      public RectTransform SlotRect = null!;
      public RectTransform CardZoneRect = null!;
      public Image? EmptyStateImage;
      public Image? ActiveGlow;
      public TextMeshProUGUI LabelText = null!;
      public GameObject CardAnchor = null!;
      public CardView? TopCardView;
      public int CardCount;
    }
  }
}
