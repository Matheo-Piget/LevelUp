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
        [SerializeField] private float _meldSpacing = 25f;
        [SerializeField] private float _cardInMeldSpacing = 30f;
        [SerializeField] private float _playerZoneSpacing = 30f;

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

            // Fond semi-transparent avec bordure colorée
            Image? bgImage = groupObj.GetComponent<Image>();
            if (bgImage == null) bgImage = groupObj.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.12f, 0.20f, 0.90f);
            bgImage.raycastTarget = true;

            // Badge joueur au-dessus du meld
            Color badgeColor = playerIndex < PlayerColors.Length
                ? PlayerColors[playerIndex]
                : Constants.TextPrimary;

            CreatePlayerBadge(groupObj.transform, playerIndex, badgeColor);

            // Bordure colorée en bas du meld pour identifier le joueur
            GameObject borderObj = new("MeldBorder", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(groupObj.transform, false);
            RectTransform borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = new Vector2(0f, 0f);
            borderRt.anchorMax = new Vector2(1f, 0f);
            borderRt.pivot = new Vector2(0.5f, 0f);
            borderRt.sizeDelta = new Vector2(0f, 3f);
            borderRt.anchoredPosition = Vector2.zero;
            Image borderImg = borderObj.GetComponent<Image>();
            borderImg.color = badgeColor;
            borderImg.raycastTarget = false;

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
            GameObject badgeObj = new("PlayerBadge",
                typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(parent, false);

            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0, 1);
            badgeRt.anchorMax = new Vector2(0, 1);
            badgeRt.pivot = new Vector2(0, 1);
            badgeRt.anchoredPosition = new Vector2(4f, 18f);
            badgeRt.sizeDelta = new Vector2(50f, 18f);

            Image badgeImg = badgeObj.GetComponent<Image>();
            badgeImg.color = color;
            badgeImg.raycastTarget = false;

            // Texte du badge
            GameObject textObj = new("BadgeText",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(badgeObj.transform, false);

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
            text.text = $"P{playerIndex + 1}";
            text.fontSize = 11;
            text.color = Constants.CardFaceColor;
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

            float maxWidth = _tableContainer.rect.width;
            if (maxWidth <= 0f) maxWidth = 680f; // fallback si le layout n'est pas encore résolu

            const float meldHeight = 135f;

            // Collecte les melds en ligne dans l'ordre d'apparition (joueur par joueur)
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

                    if (currentRow.Count > 0 && widthIfAdded > maxWidth)
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

            // Positionne chaque ligne centrée, empilée verticalement
            float totalHeight = rows.Count * meldHeight + Mathf.Max(0, rows.Count - 1) * _playerZoneSpacing;
            float startY = totalHeight / 2f - meldHeight / 2f;

            for (int r = 0; r < rows.Count; r++)
            {
                List<MeldGroupView> row = rows[r];
                float rowWidth = 0f;
                foreach (MeldGroupView g in row) rowWidth += g.GetWidth();
                rowWidth += Mathf.Max(0, row.Count - 1) * _meldSpacing;

                float currentX = -rowWidth / 2f;
                float y = startY - r * (meldHeight + _playerZoneSpacing);

                foreach (MeldGroupView g in row)
                {
                    RectTransform rt = g.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(currentX, y);
                    currentX += g.GetWidth() + _meldSpacing;
                }
            }
        }

        /// <summary>
        /// Vérifie si une position écran correspond à une combinaison.
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
                    if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPosition))
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
        /// </summary>
        public bool TryAddCard(CardModel card, GameObject? cardPrefab)
        {
            if (cardPrefab == null) return false;

            GameObject cardObj = Instantiate(cardPrefab, transform);
            CardView cardView = cardObj.GetComponent<CardView>();

            if (cardView == null) return false;

            cardView.Setup(card, true);
            cardView.SetInteractable(false);

            // Cartes sur la table — assez grosses pour être lisibles
            cardView.RectTransform.localScale = Vector3.one * 0.75f;

            _cards.Add(cardView);
            LayoutCards();
            UpdateBackgroundSize();
            return true;
        }

        /// <summary>
        /// Positionne les cartes avec overlap compact.
        /// </summary>
        private void LayoutCards()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].RectTransform.anchoredPosition = new Vector2(
                    10f + i * _cardSpacing, // padding gauche
                    -8f); // offset pour laisser place au badge
                _cards[i].RectTransform.SetSiblingIndex(i + 1); // +1 pour le badge/bg
            }
        }

        /// <summary>
        /// Ajuste la taille du fond pour contenir toutes les cartes.
        /// </summary>
        public void UpdateBackgroundSize()
        {
            RectTransform rt = GetComponent<RectTransform>();
            float width = GetWidth() + 20f; // padding
            float height = 125f; // hauteur pour cartes 0.75x + badge
            rt.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// Retourne la largeur totale du groupe.
        /// </summary>
        public float GetWidth()
        {
            if (_cards.Count == 0) return 70f;
            return (_cards.Count - 1) * _cardSpacing + 80f;
        }
    }
}
