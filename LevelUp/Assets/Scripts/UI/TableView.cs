using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
  /// <summary>
  /// Affiche les combinaisons posées sur la table style Balatro :
  /// fond arrondi semi-transparent, badge joueur coloré, overlap compact.
  /// </summary>
  public class TableView : MonoBehaviour
  {
    [SerializeField] private RectTransform? _tableContainer;
    [SerializeField] private GameObject? _cardPrefab;
    [SerializeField] private GameObject? _meldGroupPrefab;
    [SerializeField] private float _meldSpacing = 28f;
    [SerializeField] private float _cardInMeldSpacing = 52f;
    [SerializeField] private float _playerZoneSpacing = 32f;
    // Marge gauche depuis le bord du TableContainer.
    [SerializeField] private float _leftMargin = 40f;
    // Largeur max occupée par les melds (0 = toute la largeur dispo).
    // Utilisé pour laisser de la place à la pioche/défausse à droite.
    [SerializeField] private float _maxContentWidth = 900f;
    // Padding autour du rect d'un meld pour le hit-test (drop généreux).
    [SerializeField] private float _meldHitPadding = 25f;

    private readonly Dictionary<int, List<MeldGroupView>> _playerMeldGroups = new();

    // Couleurs de badge par joueur
    private static readonly Color[] PlayerColors =
    {
            Constants.CardBlue,
            Constants.CardRed,
            Constants.CardGreen,
            Constants.CardPurple,
            Constants.CardOrange,
            Constants.CardYellow
        };

    /// <summary>Événement quand on drop une carte sur une combinaison.</summary>
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
    /// Crée un groupe de combinaison avec fond arrondi et badge joueur.
    /// </summary>
    private void CreateMeldGroup(int playerIndex, List<CardModel> cards)
    {
      if (_tableContainer == null || _cardPrefab == null) return;

      // Conteneur principal du meld avec fond
      GameObject groupObj;
      if (_meldGroupPrefab != null)
      {
        groupObj = Instantiate(_meldGroupPrefab, _tableContainer);
      }
      else
      {
        groupObj = new GameObject($"Meld_P{playerIndex}",
            typeof(RectTransform), typeof(Image));
        groupObj.transform.SetParent(_tableContainer, false);
      }

      // Fond doux arrondi, teinté subtilement par la couleur du joueur
      Image? bgImage = groupObj.GetComponent<Image>();
      if (bgImage == null) bgImage = groupObj.AddComponent<Image>();
      bgImage.sprite = UIFactory.RoundedSprite;
      bgImage.type = Image.Type.Sliced;

      Color badgeColor = playerIndex < PlayerColors.Length
          ? PlayerColors[playerIndex]
          : Constants.TextPrimary;

      // Teinte très légère de la couleur joueur mélangée au surface sombre
      Color surface = Constants.SurfaceA;
      bgImage.color = new Color(
          Mathf.Lerp(surface.r, badgeColor.r, 0.12f),
          Mathf.Lerp(surface.g, badgeColor.g, 0.12f),
          Mathf.Lerp(surface.b, badgeColor.b, 0.12f),
          0.72f);
      bgImage.raycastTarget = true;

      // Badge joueur discret en haut-gauche
      CreatePlayerBadge(groupObj.transform, playerIndex, badgeColor);

      MeldGroupView groupView = groupObj.GetComponent<MeldGroupView>();
      if (groupView == null) groupView = groupObj.AddComponent<MeldGroupView>();
      groupView.Initialize(playerIndex, _cardInMeldSpacing);

      foreach (CardModel card in cards)
      {
        groupView.TryAddCard(card, _cardPrefab);
      }

      // Ajuster la taille du fond
      groupView.UpdateBackgroundSize();

      _playerMeldGroups[playerIndex].Add(groupView);
    }

    /// <summary>
    /// Crée un petit badge coloré avec le nom du joueur.
    /// </summary>
    private static void CreatePlayerBadge(Transform parent, int playerIndex, Color color)
    {
      // Pastille ronde discrète flottant au-dessus du meld
      GameObject badgeObj = new("PlayerBadge",
          typeof(RectTransform), typeof(Image));
      badgeObj.transform.SetParent(parent, false);

      RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
      badgeRt.anchorMin = new Vector2(0, 1);
      badgeRt.anchorMax = new Vector2(0, 1);
      badgeRt.pivot = new Vector2(0, 1);
      badgeRt.anchoredPosition = new Vector2(8f, 12f);
      badgeRt.sizeDelta = new Vector2(22f, 22f);

      Image badgeImg = badgeObj.GetComponent<Image>();
      badgeImg.sprite = UIFactory.RoundedSprite;
      badgeImg.type = Image.Type.Sliced;
      badgeImg.color = color;
      badgeImg.raycastTarget = false;

      GameObject textObj = new("BadgeText",
          typeof(RectTransform), typeof(TextMeshProUGUI));
      textObj.transform.SetParent(badgeObj.transform, false);

      RectTransform textRt = textObj.GetComponent<RectTransform>();
      textRt.anchorMin = Vector2.zero;
      textRt.anchorMax = Vector2.one;
      textRt.sizeDelta = Vector2.zero;

      TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
      text.text = $"{playerIndex + 1}";
      text.fontSize = 13;
      text.color = Constants.BackgroundDark;
      text.alignment = TextAlignmentOptions.Center;
      text.fontStyle = FontStyles.Bold;
      text.raycastTarget = false;
    }

    /// <summary>
    /// Repositionne tous les groupes sur la table en flow horizontal avec wrap.
    /// Utilise toute la largeur disponible avant de descendre à une nouvelle ligne.
    /// </summary>
    private void LayoutTable()
    {
      if (_tableContainer == null) return;

      float containerWidth = _tableContainer.rect.width;
      if (containerWidth <= 0f) containerWidth = 1400f;

      // Largeur effectivement utilisable pour les melds : container - marge gauche,
      // capée par _maxContentWidth si > 0 pour laisser la pioche à droite.
      float availableWidth = containerWidth - _leftMargin;
      if (_maxContentWidth > 0f) availableWidth = Mathf.Min(availableWidth, _maxContentWidth);

      const float meldHeight = 150f;

      // Collecte les melds en lignes (wrap quand la ligne dépasse availableWidth).
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

      // Empile les lignes verticalement, centrées verticalement.
      float totalHeight = rows.Count * meldHeight + Mathf.Max(0, rows.Count - 1) * _playerZoneSpacing;
      float startY = totalHeight / 2f - meldHeight / 2f;

      // Left-align : le bord gauche de la zone de layout est à -containerWidth/2 + _leftMargin
      // dans l'espace local du TableContainer (pivot 0.5, 0.5).
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
          // Pivot (0.5,0.5) par défaut : on place le centre à leftEdge + halfW
          rt.anchoredPosition = new Vector2(currentX + halfW, y);
          currentX += g.GetWidth() + _meldSpacing;
        }
      }
    }

    /// <summary>
    /// Vérifie si une position écran correspond à une combinaison.
    /// Hit-test avec padding (_meldHitPadding) pour un drop plus généreux.
    /// </summary>
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

    /// <summary>
    /// Hit-test d'un rect étendu par <paramref name="padding"/> dans son espace local.
    /// </summary>
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
    /// Supprime tous les éléments de la table.
    /// </summary>
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
  /// Vue d'un groupe de combinaison avec fond arrondi et overlap compact.
  /// </summary>
  public class MeldGroupView : MonoBehaviour
  {
    private int _ownerIndex;
    private float _cardSpacing;
    private readonly List<CardView> _cards = new();

    /// <summary>Index du joueur propriétaire.</summary>
    public int OwnerIndex => _ownerIndex;

    /// <summary>
    /// Initialise le groupe.
    /// </summary>
    public void Initialize(int ownerIndex, float cardSpacing)
    {
      _ownerIndex = ownerIndex;
      _cardSpacing = cardSpacing;
    }

    /// <summary>
    /// Ajoute une carte au groupe.
    /// Les cartes sont maintenues triées par valeur pour rester lisibles,
    /// y compris quand on complète un meld existant.
    /// </summary>
    public bool TryAddCard(CardModel card, GameObject? cardPrefab)
    {
      if (cardPrefab == null) return false;

      GameObject cardObj = Instantiate(cardPrefab, transform);
      CardView cardView = cardObj.GetComponent<CardView>();

      if (cardView == null) return false;

      cardView.Setup(card, true);
      cardView.SetInteractable(false);

      // Cartes sur la table — échelle 0.8 pour bonne lisibilité des valeurs
      cardView.RectTransform.localScale = Vector3.one * 0.8f;

      _cards.Add(cardView);
      // Tri par valeur (même ordre que le modèle Meld.TryAddCard).
      _cards.Sort((a, b) => a.CardModel.Value.CompareTo(b.CardModel.Value));
      LayoutCards();
      UpdateBackgroundSize();
      return true;
    }

    // Largeur d'une carte à l'échelle 0.8 (prefab 120×180 → 96×144).
    private const float CardVisualWidth = 96f;
    private const float PaddingH = 20f;

    /// <summary>
    /// Positionne les cartes avec un espacement lisible (chaque valeur visible).
    /// </summary>
    private void LayoutCards()
    {
      float startX = PaddingH + CardVisualWidth / 2f; // pivot centré
      for (int i = 0; i < _cards.Count; i++)
      {
        _cards[i].RectTransform.anchoredPosition = new Vector2(
            startX + i * _cardSpacing - GetWidth() / 2f,
            -4f);
        _cards[i].RectTransform.SetSiblingIndex(i + 1);
      }
    }

    /// <summary>
    /// Ajuste la taille du fond pour contenir toutes les cartes.
    /// </summary>
    public void UpdateBackgroundSize()
    {
      RectTransform rt = GetComponent<RectTransform>();
      rt.sizeDelta = new Vector2(GetWidth(), 140f);
    }

    /// <summary>
    /// Retourne la largeur totale du groupe (cartes + paddings).
    /// </summary>
    public float GetWidth()
    {
      int n = Mathf.Max(1, _cards.Count);
      return (n - 1) * _cardSpacing + CardVisualWidth + PaddingH * 2f;
    }
  }
}
