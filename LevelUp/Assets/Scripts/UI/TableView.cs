using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
  /// <summary>
  /// Combinaisons posées (maquette "Écran de jeu", zone droite du tapis) :
  ///   - en-tête "COMBINAISONS POSÉES" + aide à droite
  ///   - chaque combinaison = panneau verre arrondi "Joueur · Suite" + cartes compactes
  ///   - cible de dépôt pendant un drag : anneau accent 2px + pastille "Relâche pour poser"
  /// Les panneaux s'écoulent en grille ; si la table déborde, le contenu se
  /// réduit pour toujours tenir dans le tapis (pas de scroll en jeu).
  /// </summary>
  public class TableView : MonoBehaviour
  {
    [SerializeField] private RectTransform? _tableContainer;
    [SerializeField] private GameObject? _cardPrefab;
    [SerializeField] private GameObject? _meldGroupPrefab;
    [SerializeField] private float _meldSpacing = 16f;
    [SerializeField] private float _cardInMeldSpacing = 74f;
    [SerializeField] private float _playerZoneSpacing = 16f;
    [SerializeField] private float _leftMargin = 0f;
    [SerializeField] private float _maxContentWidth = 0f;
    [SerializeField] private float _meldHitPadding = 25f;

    private const float HeaderHeight = 40f;

    private readonly Dictionary<int, List<MeldGroupView>> _playerMeldGroups = new();
    private readonly List<int> _ownerOrder = new();
    private List<string>? _playerNames;
    private int _localIndex;
    private RectTransform? _content;

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

    /// <summary>Noms des joueurs (le joueur local s'affiche "Toi").</summary>
    public void SetPlayerNames(List<string> names, int localIndex)
    {
      _playerNames = names;
      _localIndex = localIndex;
      foreach (KeyValuePair<int, List<MeldGroupView>> kvp in _playerMeldGroups)
      {
        foreach (MeldGroupView g in kvp.Value) g.SetOwnerName(OwnerName(kvp.Key));
      }
    }

    private string OwnerName(int index)
    {
      if (index == _localIndex) return "Toi";
      if (_playerNames != null && index >= 0 && index < _playerNames.Count) return _playerNames[index];
      return $"Joueur {index + 1}";
    }

    private void EnsureContent()
    {
      if (_content != null || _tableContainer == null) return;

      TextMeshProUGUI title = UISkin.Text(_tableContainer, "Title", "COMBINAISONS POSÉES", 13f,
          UISkin.TextMuted, TextWeight.SemiBold, TextAlignmentOptions.MidlineLeft);
      title.characterSpacing = 8f;
      RectTransform trt = title.rectTransform;
      trt.anchorMin = new Vector2(0f, 1f);
      trt.anchorMax = new Vector2(0.5f, 1f);
      trt.pivot = new Vector2(0f, 1f);
      trt.offsetMin = new Vector2(0f, -24f);
      trt.offsetMax = new Vector2(0f, 0f);

      TextMeshProUGUI hint = UISkin.Text(_tableContainer, "Hint",
          "Glisse une carte sur une combinaison pour la compléter", 13f, UISkin.TextMuted,
          TextWeight.Regular, TextAlignmentOptions.MidlineRight);
      RectTransform hrt = hint.rectTransform;
      hrt.anchorMin = new Vector2(0.4f, 1f);
      hrt.anchorMax = new Vector2(1f, 1f);
      hrt.pivot = new Vector2(1f, 1f);
      hrt.offsetMin = new Vector2(0f, -24f);
      hrt.offsetMax = new Vector2(0f, 0f);

      GameObject contentObj = new("Melds", typeof(RectTransform));
      contentObj.transform.SetParent(_tableContainer, false);
      _content = (RectTransform)contentObj.transform;
      _content.anchorMin = new Vector2(0f, 1f);
      _content.anchorMax = new Vector2(0f, 1f);
      _content.pivot = new Vector2(0f, 1f);
      _content.anchoredPosition = new Vector2(0f, -HeaderHeight);
      _content.sizeDelta = Vector2.zero;
    }

    private void Awake()
    {
      // Les valeurs sérialisées dans la scène datent de l'ancien layout (cartes
      // chevauchées à 38px, largeur bridée à 900px) : la mise en page actuelle
      // (panneaux de la maquette) impose les siennes.
      _cardInMeldSpacing = 74f;
      _meldSpacing = 16f;
      _playerZoneSpacing = 16f;
      _leftMargin = 0f;
      _maxContentWidth = 0f;
      _meldHitPadding = 25f;
    }

    // L'en-tête "COMBINAISONS POSÉES" est visible dès le début (table vide comprise).
    private void Start() => EnsureContent();

    private void OnRoundStarted(RoundStartedEvent evt) => ClearTable();

    private void OnLevelLaidDown(LevelLaidDownEvent evt)
    {
      if (_tableContainer == null || _cardPrefab == null) return;

      if (!_playerMeldGroups.ContainsKey(evt.PlayerIndex))
      {
        _playerMeldGroups[evt.PlayerIndex] = new List<MeldGroupView>();
        _ownerOrder.Add(evt.PlayerIndex);
      }

      foreach (List<CardModel> meldCards in evt.Melds)
      {
        CreateMeldGroup(evt.PlayerIndex, meldCards);
      }
      LayoutTable();
    }

    private void OnCardAddedToMeld(CardAddedToMeldEvent evt)
    {
      if (!_playerMeldGroups.TryGetValue(evt.MeldOwnerIndex, out List<MeldGroupView>? groups)) return;
      if (evt.MeldIndex < 0 || evt.MeldIndex >= groups.Count) return;
      groups[evt.MeldIndex].TryAddCard(evt.Card, _cardPrefab);
      LayoutTable();
    }

    private void CreateMeldGroup(int playerIndex, List<CardModel> cards)
    {
      EnsureContent();
      if (_content == null || _cardPrefab == null) return;

      GameObject groupObj = new($"Meld_P{playerIndex}", typeof(RectTransform), typeof(Image));
      groupObj.transform.SetParent(_content, false);
      RectTransform grt = (RectTransform)groupObj.transform;
      grt.anchorMin = new Vector2(0f, 1f);
      grt.anchorMax = new Vector2(0f, 1f);
      grt.pivot = new Vector2(0f, 1f);

      MeldGroupView groupView = groupObj.AddComponent<MeldGroupView>();
      groupView.Initialize(playerIndex, OwnerName(playerIndex), ClassifyMeld(cards), _cardInMeldSpacing);

      foreach (CardModel card in cards)
      {
        groupView.TryAddCard(card, _cardPrefab);
      }
      _playerMeldGroups[playerIndex].Add(groupView);
    }

    private static MeldType ClassifyMeld(List<CardModel> cards)
    {
      if (CardExtensions.IsValidRun(cards)) return MeldType.Run;
      if (CardExtensions.IsValidSet(cards)) return MeldType.Set;
      if (CardExtensions.IsValidFlush(cards)) return MeldType.Flush;
      return MeldType.Run;
    }

    /// <summary>Grille fluide (gauche → droite, retour à la ligne), réduite si elle déborde.</summary>
    private void LayoutTable()
    {
      if (_tableContainer == null) return;
      EnsureContent();
      if (_content == null) return;

      float width = _tableContainer.rect.width;
      if (width <= 0f) width = 1000f;
      if (_maxContentWidth > 0f) width = Mathf.Min(width, _maxContentWidth);
      float availH = Mathf.Max(100f, _tableContainer.rect.height - HeaderHeight);

      // Ordre stable : par joueur dans l'ordre de pose, puis par meld.
      List<MeldGroupView> all = new();
      foreach (int owner in _ownerOrder)
      {
        if (_playerMeldGroups.TryGetValue(owner, out List<MeldGroupView>? groups)) all.AddRange(groups);
      }

      // Taille à l'échelle 1, puis échelle de repli si ça ne tient pas.
      float scale = 1f;
      for (int attempt = 0; attempt < 6; attempt++)
      {
        float h = Flow(all, width / scale, apply: false);
        if (h * scale <= availH) break;
        scale *= Mathf.Max(0.6f, availH / (h * scale));
        if (scale < 0.45f) { scale = 0.45f; break; }
      }

      Flow(all, width / scale, apply: true);
      _content.localScale = new Vector3(scale, scale, 1f);
      _content.anchoredPosition = new Vector2(_leftMargin, -HeaderHeight);
    }

    private float Flow(List<MeldGroupView> groups, float width, bool apply)
    {
      float x = 0f, y = 0f, rowH = 0f;
      foreach (MeldGroupView g in groups)
      {
        Vector2 size = g.GetSize();
        if (x > 0f && x + size.x > width)
        {
          x = 0f;
          y += rowH + _playerZoneSpacing;
          rowH = 0f;
        }
        if (apply) ((RectTransform)g.transform).anchoredPosition = new Vector2(x, -y);
        x += size.x + _meldSpacing;
        rowH = Mathf.Max(rowH, size.y);
      }
      return y + rowH;
    }

    public bool GetMeldAtPosition(Vector2 screenPosition, out int ownerIndex, out int meldIndex)
    {
      ownerIndex = -1;
      meldIndex = -1;
      foreach (KeyValuePair<int, List<MeldGroupView>> kvp in _playerMeldGroups)
      {
        for (int i = 0; i < kvp.Value.Count; i++)
        {
          RectTransform rt = (RectTransform)kvp.Value[i].transform;
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

    /// <summary>
    /// Meld le plus proche du point écran dans un rayon donné (distance au bord,
    /// 0 si dedans) — snap magnétique du drag.
    /// </summary>
    public bool TryGetNearestMeld(Vector2 screenPosition, float maxDistance,
        out int ownerIndex, out int meldIndex, out Vector2 meldScreenCenter)
    {
      ownerIndex = -1;
      meldIndex = -1;
      meldScreenCenter = default;
      float best = maxDistance;
      bool found = false;

      foreach (KeyValuePair<int, List<MeldGroupView>> kvp in _playerMeldGroups)
      {
        for (int i = 0; i < kvp.Value.Count; i++)
        {
          RectTransform rt = (RectTransform)kvp.Value[i].transform;
          if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPosition, null, out Vector2 local))
            continue;

          Rect r = rt.rect;
          float dx = Mathf.Max(0f, Mathf.Max(r.xMin - local.x, local.x - r.xMax));
          float dy = Mathf.Max(0f, Mathf.Max(r.yMin - local.y, local.y - r.yMax));
          float dist = Mathf.Sqrt(dx * dx + dy * dy);

          if (dist <= best)
          {
            best = dist;
            found = true;
            ownerIndex = kvp.Key;
            meldIndex = i;
            meldScreenCenter = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(r.center));
          }
        }
      }
      return found;
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

    /// <summary>Surligne la combinaison visée pendant un drag (cible de dépôt).</summary>
    public void SetMeldHover(int ownerIndex, int meldIndex, bool isHovered)
    {
      if (!_playerMeldGroups.TryGetValue(ownerIndex, out List<MeldGroupView>? groups)) return;
      if (meldIndex < 0 || meldIndex >= groups.Count) return;
      groups[meldIndex].SetHover(isHovered);
    }

    public void ClearAllHovers()
    {
      foreach (KeyValuePair<int, List<MeldGroupView>> kvp in _playerMeldGroups)
      {
        foreach (MeldGroupView g in kvp.Value) g.SetHover(false);
      }
    }

    /// <summary>Reconstruit toute la table depuis le modèle (reprise en ligne).</summary>
    public void SyncFromModel(IReadOnlyList<PlayerModel> players)
    {
      ClearTable();
      foreach (PlayerModel player in players)
      {
        if (player.LaidMelds.Count == 0) continue;
        _playerMeldGroups[player.Index] = new List<MeldGroupView>();
        _ownerOrder.Add(player.Index);
        foreach (Meld meld in player.LaidMelds)
        {
          CreateMeldGroup(player.Index, new List<CardModel>(meld.Cards));
        }
      }
      LayoutTable();
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
      _ownerOrder.Clear();
      _ = OnCardDroppedOnMeld;
    }
  }

  /// <summary>
  /// Panneau d'une combinaison : verre arrondi 20px, "Joueur · Type" en tête,
  /// cartes compactes (66×99) espacées de 8px, resserrées si elles sont nombreuses.
  /// </summary>
  public class MeldGroupView : MonoBehaviour
  {
    private const float CardScale = 0.55f;
    private const float CardW = 120f * CardScale;
    private const float CardH = 180f * CardScale;
    private const float PadX = 18f;
    private const float PadTop = 14f;
    private const float PadBottom = 16f;
    private const float HeaderH = 20f;
    private const float HeaderGap = 12f;
    private const float MaxCardsWidth = 420f;

    private int _ownerIndex;
    private float _cardSpacing = 74f;
    private MeldType _meldType;
    private string _ownerName = "";
    private Image? _bg;
    private Image? _ring;
    private TextMeshProUGUI? _header;
    private GameObject? _dropPill;
    private RectTransform? _cardsRoot;
    private readonly List<CardView> _cards = new();

    public int OwnerIndex => _ownerIndex;
    public MeldType MeldType => _meldType;

    public void Initialize(int ownerIndex, string ownerName, MeldType type, float cardSpacing)
    {
      _ownerIndex = ownerIndex;
      _ownerName = ownerName;
      _meldType = type;
      _cardSpacing = cardSpacing;

      _bg = GetComponent<Image>();
      UISkin.SetRounded(_bg, 20f);
      _bg.color = UISkin.Glass;
      _bg.raycastTarget = true;
      _ring = UISkin.Outline((RectTransform)transform, 20f, 1f, UISkin.Line, "Ring");

      _header = UISkin.Text(transform, "Header", "", 14f, UISkin.TextColor, TextWeight.SemiBold);
      _header.richText = true;
      UISkin.Place(_header.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
          new Vector2(PadX, -PadTop), new Vector2(240f, HeaderH));

      // Pastille "Relâche pour poser" (visible quand la carte draguée vise ce meld)
      Image pill = UISkin.Rect(transform, "DropPill", UISkin.Accent, 12f);
      UISkin.Place(pill.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
          new Vector2(-PadX + 2f, -PadTop + 2f), new Vector2(146f, 24f));
      TextMeshProUGUI pillText = UISkin.Text(pill.transform, "Text", "Relâche pour poser", 12f,
          UISkin.OnAccent, TextWeight.Bold, TextAlignmentOptions.Center);
      UISkin.Stretch(pillText.rectTransform);
      _dropPill = pill.gameObject;
      _dropPill.SetActive(false);

      GameObject cardsObj = new("Cards", typeof(RectTransform));
      cardsObj.transform.SetParent(transform, false);
      _cardsRoot = (RectTransform)cardsObj.transform;
      UISkin.Place(_cardsRoot, new Vector2(0f, 1f), new Vector2(0f, 1f),
          new Vector2(PadX, -(PadTop + HeaderH + HeaderGap)), new Vector2(0f, CardH));

      UpdateHeader();
    }

    public void SetOwnerName(string name)
    {
      _ownerName = name;
      UpdateHeader();
    }

    private void UpdateHeader()
    {
      if (_header == null) return;
      string type = _meldType switch
      {
        MeldType.Run => "Suite",
        MeldType.Flush => "Couleur",
        MeldType.Set => _cards.Count switch
        {
          2 => "Paire",
          3 => "Brelan",
          4 => "Carré",
          _ => "Série"
        },
        _ => "Combinaison"
      };
      _header.text = $"{_ownerName} · <color=#A7A198>{type}</color>";
    }

    public bool TryAddCard(CardModel card, GameObject? cardPrefab)
    {
      if (cardPrefab == null || _cardsRoot == null) return false;

      GameObject cardObj = Instantiate(cardPrefab, _cardsRoot);
      CardView cardView = cardObj.GetComponent<CardView>();
      if (cardView == null) return false;

      cardView.Setup(card, true);
      cardView.SetInteractable(false);
      RectTransform rt = cardView.RectTransform;
      rt.anchorMin = new Vector2(0f, 0.5f);
      rt.anchorMax = new Vector2(0f, 0.5f);
      rt.localScale = Vector3.one * CardScale;
      rt.localRotation = Quaternion.identity;

      _cards.Add(cardView);
      _cards.Sort((a, b) => a.CardModel.Value.CompareTo(b.CardModel.Value));
      LayoutCards();
      UpdateHeader();
      return true;
    }

    private float Spacing()
    {
      int n = _cards.Count;
      if (n <= 1) return _cardSpacing;
      return Mathf.Min(_cardSpacing, (MaxCardsWidth - CardW) / (n - 1));
    }

    private float CardsWidth()
    {
      int n = Mathf.Max(1, _cards.Count);
      return (n - 1) * Spacing() + CardW;
    }

    private void LayoutCards()
    {
      float step = Spacing();
      for (int i = 0; i < _cards.Count; i++)
      {
        RectTransform rt = _cards[i].RectTransform;
        rt.anchoredPosition = new Vector2(CardW * 0.5f + i * step, 0f);
        rt.SetSiblingIndex(i);
      }
      ((RectTransform)transform).sizeDelta = GetSize();
    }

    public Vector2 GetSize()
    {
      float w = Mathf.Max(PadX * 2f + CardsWidth(), 236f);
      float h = PadTop + HeaderH + HeaderGap + CardH + PadBottom;
      return new Vector2(w, h);
    }

    /// <summary>Cible de dépôt : fond accent 10 %, anneau 2px, pastille.</summary>
    public void SetHover(bool isHovered)
    {
      if (_bg != null) _bg.color = isHovered ? UISkin.WithAlpha(UISkin.Accent, 0.10f) : UISkin.Glass;
      if (_ring != null)
      {
        UISkin.SetStroke(_ring, 20f, isHovered ? 2f : 1f);
        _ring.color = isHovered ? UISkin.AccentLight : UISkin.Line;
      }
      if (_dropPill != null) _dropPill.SetActive(isHovered);
    }

    public float GetWidth() => GetSize().x;

    public void UpdateBackgroundSize() => ((RectTransform)transform).sizeDelta = GetSize();
  }
}
