using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
  /// <summary>
  /// Affiche les combinaisons posées sur la table.
  ///
  /// Direction visuelle : minimaliste, beaucoup d'air. Chaque meld c'est :
  ///   - un label discret au-dessus ("SUITE", "BRELAN", "FLUSH")
  ///   - les cartes alignées, légèrement chevauchées
  ///   - un fin liseré coloré (2px) en dessous → identité du joueur poseur
  ///
  /// Pas de fond plein, pas de bordure, pas de footer : le groupement est
  /// purement spatial. L'œil voit "trois cartes ensemble + un trait coloré
  /// en dessous" et comprend immédiatement.
  /// </summary>
  public class TableView : MonoBehaviour
  {
    [SerializeField] private RectTransform? _tableContainer;
    [SerializeField] private GameObject? _cardPrefab;
    [SerializeField] private GameObject? _meldGroupPrefab;
    // Espacement BETWEEN melds : large pour bien séparer les groupes.
    [SerializeField] private float _meldSpacing = 48f;
    // Espacement DANS un meld : plus serré pour que les cartes "tiennent ensemble".
    [SerializeField] private float _cardInMeldSpacing = 38f;
    [SerializeField] private float _playerZoneSpacing = 40f;
    [SerializeField] private float _leftMargin = 40f;
    [SerializeField] private float _maxContentWidth = 900f;
    [SerializeField] private float _meldHitPadding = 25f;

    private readonly Dictionary<int, List<MeldGroupView>> _playerMeldGroups = new();

    // Couleurs joueur — utilisées UNIQUEMENT pour le liseré 2px sous chaque meld.
    // Cohérent avec la sidebar (avatar) et la défausse (liseré du dessus).
    private static readonly Color[] PlayerColors =
    {
            Constants.CardBlue,
            Constants.CardRed,
            Constants.CardGreen,
            Constants.CardPurple,
            Constants.CardOrange,
            Constants.CardYellow
        };

    public event System.Action<CardModel, int, int>? OnCardDroppedOnMeld;

    private void OnEnable()
    {
      EventBus.Subscribe<LevelLaidDownEvent>(OnLevelLaidDown);
      EventBus.Subscribe<CardAddedToMeldEvent>(OnCardAddedToMeld);
      EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
    }

    private void OnDisable()
    {
      EventBus.Unsubscribe<LevelLaidDownEvent>(OnLevelLaidDown);
      EventBus.Unsubscribe<CardAddedToMeldEvent>(OnCardAddedToMeld);
      EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
      ClearTable();
    }

    private void OnLevelLaidDown(LevelLaidDownEvent evt)
    {
      if (_tableContainer == null || _cardPrefab == null) return;

      if (!_playerMeldGroups.ContainsKey(evt.PlayerIndex))
      {
        _playerMeldGroups[evt.PlayerIndex] = new List<MeldGroupView>();
      }

      foreach (List<CardModel> meldCards in evt.Melds)
      {
        CreateMeldGroup(evt.PlayerIndex, meldCards);
      }

      LayoutTable();
    }

    private void OnCardAddedToMeld(CardAddedToMeldEvent evt)
    {
      if (!_playerMeldGroups.TryGetValue(evt.MeldOwnerIndex, out List<MeldGroupView>? groups))
        return;

      if (evt.MeldIndex < 0 || evt.MeldIndex >= groups.Count) return;

      groups[evt.MeldIndex].TryAddCard(evt.Card, _cardPrefab);
      LayoutTable();
    }

    /// <summary>
    /// Crée un groupe de combinaison entièrement transparent : pas de fond,
    /// pas de bordure. Le groupement est purement spatial.
    /// </summary>
    private void CreateMeldGroup(int playerIndex, List<CardModel> cards)
    {
      if (_tableContainer == null || _cardPrefab == null) return;

      GameObject groupObj;
      if (_meldGroupPrefab != null)
      {
        groupObj = Instantiate(_meldGroupPrefab, _tableContainer);
      }
      else
      {
        groupObj = new GameObject($"Meld_P{playerIndex}", typeof(RectTransform));
        groupObj.transform.SetParent(_tableContainer, false);
      }

      // Fond très léger pour le hover state. Invisible par défaut, visible
      // seulement quand on drag une carte au-dessus du meld (signal de drop).
      // On le garde séparé des cartes pour pouvoir l'animer indépendamment.
      Image? hoverBg = groupObj.GetComponent<Image>();
      if (hoverBg == null) hoverBg = groupObj.AddComponent<Image>();
      hoverBg.sprite = UIFactory.RoundedSprite;
      hoverBg.type = Image.Type.Sliced;
      hoverBg.color = new Color(0f, 0f, 0f, 0f); // transparent par défaut
      hoverBg.raycastTarget = true;

      Color playerColor = playerIndex < PlayerColors.Length
          ? PlayerColors[playerIndex]
          : Constants.TextPrimary;

      MeldType meldType = ClassifyMeld(cards);

      // Header au-dessus : juste "SUITE" / "BRELAN" / "FLUSH" en typo discrète.
      // Le compte est évident visuellement, pas besoin de "· 3 CARTES".
      CreateMeldHeader(groupObj.transform, meldType);

      // Liseré 2px en dessous : identité joueur.
      Image accentBar = CreateAccentBar(groupObj.transform, playerColor);

      MeldGroupView groupView = groupObj.GetComponent<MeldGroupView>();
      if (groupView == null) groupView = groupObj.AddComponent<MeldGroupView>();
      groupView.Initialize(playerIndex, _cardInMeldSpacing, meldType, playerColor,
          hoverBg, accentBar);

      foreach (CardModel card in cards)
      {
        groupView.TryAddCard(card, _cardPrefab);
      }

      groupView.UpdateBackgroundSize();
      _playerMeldGroups[playerIndex].Add(groupView);
    }

    private static MeldType ClassifyMeld(List<CardModel> cards)
    {
      if (CardExtensions.IsValidRun(cards)) return MeldType.Run;
      if (CardExtensions.IsValidSet(cards)) return MeldType.Set;
      if (CardExtensions.IsValidFlush(cards)) return MeldType.Flush;
      return MeldType.Run;
    }

    private static string MeldTypeLabel(MeldType type) => type switch
    {
      MeldType.Run => "SUITE",
      MeldType.Set => "BRELAN",
      MeldType.Flush => "FLUSH",
      _ => "MELD"
    };

    /// <summary>
    /// Header au-dessus du meld : juste le type, en label minuscule
    /// (10px, tracking 6px, gris muet). On laisse l'œil compter les cartes.
    /// </summary>
    private static void CreateMeldHeader(Transform parent, MeldType type)
    {
      GameObject lblObj = new("Header",
          typeof(RectTransform), typeof(TextMeshProUGUI));
      lblObj.transform.SetParent(parent, false);

      RectTransform lrt = lblObj.GetComponent<RectTransform>();
      lrt.anchorMin = new Vector2(0f, 1f);
      lrt.anchorMax = new Vector2(1f, 1f);
      lrt.pivot = new Vector2(0.5f, 1f);
      lrt.sizeDelta = new Vector2(0f, 14f);
      lrt.anchoredPosition = new Vector2(0f, -2f);

      TextMeshProUGUI lbl = lblObj.GetComponent<TextMeshProUGUI>();
      lbl.text = MeldTypeLabel(type);
      lbl.fontSize = 10f;
      lbl.characterSpacing = 6f;
      lbl.color = Constants.TextMuted;
      lbl.alignment = TextAlignmentOptions.Center;
      lbl.fontStyle = FontStyles.Bold;
      lbl.raycastTarget = false;
    }

    /// <summary>
    /// Liseré 2px sous le meld, dans la couleur du poseur. Centré horizontalement,
    /// largeur = ~70% du meld pour laisser de l'air sur les côtés.
    /// </summary>
    private static Image CreateAccentBar(Transform parent, Color playerColor)
    {
      GameObject barObj = new("AccentBar",
          typeof(RectTransform), typeof(Image));
      barObj.transform.SetParent(parent, false);

      RectTransform brt = barObj.GetComponent<RectTransform>();
      brt.anchorMin = new Vector2(0.5f, 0f);
      brt.anchorMax = new Vector2(0.5f, 0f);
      brt.pivot = new Vector2(0.5f, 0f);
      // sizeDelta.x sera ajusté dynamiquement par MeldGroupView selon le nombre de cartes
      brt.sizeDelta = new Vector2(60f, 2f);
      brt.anchoredPosition = new Vector2(0f, 4f);

      Image img = barObj.GetComponent<Image>();
      img.sprite = UIFactory.RoundedSprite;
      img.type = Image.Type.Sliced;
      img.color = playerColor;
      img.raycastTarget = false;

      return img;
    }

    private void LayoutTable()
    {
      if (_tableContainer == null) return;

      float containerWidth = _tableContainer.rect.width;
      if (containerWidth <= 0f) containerWidth = 1400f;

      float availableWidth = containerWidth - _leftMargin;
      if (_maxContentWidth > 0f) availableWidth = Mathf.Min(availableWidth, _maxContentWidth);

      const float meldHeight = 180f;

      List<List<MeldGroupView>> rows = new();
      List<MeldGroupView> currentRow = new();
      float currentRowWidth = 0f;

      foreach (KeyValuePair<int, List<MeldGroupView>> kvp in _playerMeldGroups)
      {
        foreach (MeldGroupView group in kvp.Value)
        {
          float w = group.GetWidth();
          float widthIfAdded = currentRow.Count == 0
              ? w
              : currentRowWidth + _meldSpacing + w;

          if (currentRow.Count > 0 && widthIfAdded > availableWidth)
          {
            rows.Add(currentRow);
            currentRow = new List<MeldGroupView>();
            currentRowWidth = 0f;
            widthIfAdded = w;
          }

          currentRow.Add(group);
          currentRowWidth = widthIfAdded;
        }
      }
      if (currentRow.Count > 0) rows.Add(currentRow);

      float totalHeight = rows.Count * meldHeight + Mathf.Max(0, rows.Count - 1) * _playerZoneSpacing;
      float startY = totalHeight / 2f - meldHeight / 2f;

      float leftEdgeLocal = -containerWidth / 2f + _leftMargin;

      for (int r = 0; r < rows.Count; r++)
      {
        List<MeldGroupView> row = rows[r];
        float currentX = leftEdgeLocal;
        float y = startY - r * (meldHeight + _playerZoneSpacing);

        foreach (MeldGroupView g in row)
        {
          RectTransform rt = g.GetComponent<RectTransform>();
          float halfW = g.GetWidth() / 2f;
          rt.anchoredPosition = new Vector2(currentX + halfW, y);
          currentX += g.GetWidth() + _meldSpacing;
        }
      }
    }

    public bool GetMeldAtPosition(Vector2 screenPosition, out int ownerIndex, out int meldIndex)
    {
      ownerIndex = -1;
      meldIndex = -1;

      foreach (KeyValuePair<int, List<MeldGroupView>> kvp in _playerMeldGroups)
      {
        for (int i = 0; i < kvp.Value.Count; i++)
        {
          RectTransform rt = kvp.Value[i].GetComponent<RectTransform>();
          if (IsInsidePaddedRect(rt, screenPosition, _meldHitPadding))
          {
            ownerIndex = kvp.Key;
            meldIndex = i;
            return true;
          }
        }
      }

      return false;
    }

    private static bool IsInsidePaddedRect(RectTransform rt, Vector2 screenPos, float padding)
    {
      if (rt == null) return false;
      if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, null, out Vector2 local))
        return false;
      Rect r = rt.rect;
      return local.x >= r.xMin - padding && local.x <= r.xMax + padding
          && local.y >= r.yMin - padding && local.y <= r.yMax + padding;
    }

    /// <summary>
    /// Active/désactive le hover state d'un meld donné (appelé par le drag system
    /// quand une carte survole le meld). Affiche un fond indigo très doux.
    /// </summary>
    public void SetMeldHover(int ownerIndex, int meldIndex, bool isHovered)
    {
      if (!_playerMeldGroups.TryGetValue(ownerIndex, out List<MeldGroupView>? groups)) return;
      if (meldIndex < 0 || meldIndex >= groups.Count) return;
      groups[meldIndex].SetHover(isHovered);
    }

    /// <summary>Coupe tous les hover states (utile quand le drag se termine).</summary>
    public void ClearAllHovers()
    {
      foreach (var kvp in _playerMeldGroups)
      {
        foreach (var g in kvp.Value) g.SetHover(false);
      }
    }

    public void ClearTable()
    {
      foreach (KeyValuePair<int, List<MeldGroupView>> kvp in _playerMeldGroups)
      {
        foreach (MeldGroupView group in kvp.Value)
        {
          if (group != null) Destroy(group.gameObject);
        }
      }
      _playerMeldGroups.Clear();
    }
  }

  /// <summary>
  /// Vue d'un groupe de combinaison. Pas de fond visible (transparent par défaut),
  /// juste un header au-dessus + un liseré coloré en dessous + les cartes.
  /// </summary>
  public class MeldGroupView : MonoBehaviour
  {
    private int _ownerIndex;
    private float _cardSpacing;
    private MeldType _meldType;
    private Color _ownerColor;
    private Image? _hoverBg;
    private Image? _accentBar;
    private readonly List<CardView> _cards = new();

    public int OwnerIndex => _ownerIndex;
    public MeldType MeldType => _meldType;

    public void Initialize(int ownerIndex, float cardSpacing,
        MeldType type, Color ownerColor, Image? hoverBg, Image? accentBar)
    {
      _ownerIndex = ownerIndex;
      _cardSpacing = cardSpacing;
      _meldType = type;
      _ownerColor = ownerColor == default ? Constants.TextSecondary : ownerColor;
      _hoverBg = hoverBg;
      _accentBar = accentBar;
    }

    public bool TryAddCard(CardModel card, GameObject? cardPrefab)
    {
      if (cardPrefab == null) return false;

      GameObject cardObj = Instantiate(cardPrefab, transform);
      CardView cardView = cardObj.GetComponent<CardView>();

      if (cardView == null) return false;

      cardView.Setup(card, true);
      cardView.SetInteractable(false);
      cardView.RectTransform.localScale = Vector3.one * 0.8f;

      _cards.Add(cardView);
      _cards.Sort((a, b) => a.CardModel.Value.CompareTo(b.CardModel.Value));
      LayoutCards();
      UpdateBackgroundSize();
      return true;
    }

    // Largeur d'une carte à l'échelle 0.8 (prefab 120×180 → 96×144).
    private const float CardVisualWidth = 96f;
    private const float CardVisualHeight = 144f;
    private const float PaddingH = 16f;
    // Hauteur réservée au header au-dessus + au liseré en dessous.
    private const float HeaderSpace = 20f;
    private const float AccentSpace = 12f;

    /// <summary>
    /// Positionne les cartes en ligne, centrées dans la zone "carte" du meld
    /// (entre le header en haut et le liseré en bas).
    /// </summary>
    private void LayoutCards()
    {
      float startX = PaddingH + CardVisualWidth / 2f;
      // Décalage vertical : on centre les cartes entre header (haut) et accent (bas)
      // Le header prend HeaderSpace en haut, l'accent AccentSpace en bas → on décale
      // les cartes vers le bas de (HeaderSpace - AccentSpace) / 2.
      float yOffset = -(HeaderSpace - AccentSpace) / 2f;

      for (int i = 0; i < _cards.Count; i++)
      {
        _cards[i].RectTransform.anchoredPosition = new Vector2(
            startX + i * _cardSpacing - GetWidth() / 2f,
            yOffset);
        // SetSiblingIndex décalé de +2 pour passer après hoverBg + header.
        // L'accentBar est positionné après dans la hiérarchie volontairement
        // pour qu'il s'affiche AU-DESSUS des cartes (sinon caché par les cartes).
        _cards[i].RectTransform.SetSiblingIndex(i + 2);
      }

      // L'accent bar doit toujours être au-dessus des cartes et adapté à leur largeur
      if (_accentBar != null)
      {
        _accentBar.transform.SetAsLastSibling();
        // Liseré = ~70% de la largeur des cartes (pas du meld entier, pour aérer)
        float cardsWidth = (_cards.Count - 1) * _cardSpacing + CardVisualWidth;
        _accentBar.rectTransform.sizeDelta = new Vector2(cardsWidth * 0.7f, 2f);
      }
    }

    /// <summary>
    /// Active/désactive le fond hover indigo. Très doux (alpha 8%) — juste assez
    /// pour signaler "tu peux drop ici" sans casser l'épure.
    /// </summary>
    public void SetHover(bool isHovered)
    {
      if (_hoverBg == null) return;
      if (isHovered)
      {
        Color c = Constants.AccentLight;
        c.a = 0.08f;
        _hoverBg.color = c;
      }
      else
      {
        _hoverBg.color = new Color(0f, 0f, 0f, 0f);
      }
    }

    public void UpdateBackgroundSize()
    {
      RectTransform rt = GetComponent<RectTransform>();
      // Hauteur totale = header + carte + accent + un peu d'air
      float h = HeaderSpace + CardVisualHeight + AccentSpace + 16f;
      rt.sizeDelta = new Vector2(GetWidth(), h);
    }

    public float GetWidth()
    {
      int n = Mathf.Max(1, _cards.Count);
      return (n - 1) * _cardSpacing + CardVisualWidth + PaddingH * 2f;
    }
  }
}
