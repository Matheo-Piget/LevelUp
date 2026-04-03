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

            foreach (MeldGroupView group in groups)
            {
                if (group.TryAddCard(evt.Card, _cardPrefab))
                {
                    break;
                }
            }
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

            // Fond semi-transparent arrondi
            Image? bgImage = groupObj.GetComponent<Image>();
            if (bgImage == null) bgImage = groupObj.AddComponent<Image>();
            bgImage.color = Constants.PanelBackground;
            bgImage.raycastTarget = true;

            // Badge joueur au-dessus du meld
            Color badgeColor = playerIndex < PlayerColors.Length
                ? PlayerColors[playerIndex]
                : Constants.TextPrimary;

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
        /// Repositionne tous les groupes sur la table, centré.
        /// </summary>
        private void LayoutTable()
        {
            if (_tableContainer == null) return;

            float currentY = 0f;

            foreach (KeyValuePair<int, List<MeldGroupView>> kvp in _playerMeldGroups)
            {
                // Calculer la largeur totale de la ligne
                float totalWidth = 0f;
                foreach (MeldGroupView group in kvp.Value)
                {
                    totalWidth += group.GetWidth() + _meldSpacing;
                }
                totalWidth -= _meldSpacing; // enlever le dernier espacement

                float startX = -totalWidth / 2f;
                float currentX = startX;

                foreach (MeldGroupView group in kvp.Value)
                {
                    RectTransform rt = group.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(currentX, currentY);
                    currentX += group.GetWidth() + _meldSpacing;
                }

                currentY -= _playerZoneSpacing + 110f;
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

            // Cartes plus petites sur la table
            cardView.RectTransform.localScale = Vector3.one * 0.65f;

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
                    8f + i * _cardSpacing, // padding gauche
                    -4f); // petit offset vers le bas pour laisser place au badge
                _cards[i].RectTransform.SetSiblingIndex(i + 1); // +1 pour le badge/bg
            }
        }

        /// <summary>
        /// Ajuste la taille du fond pour contenir toutes les cartes.
        /// </summary>
        public void UpdateBackgroundSize()
        {
            RectTransform rt = GetComponent<RectTransform>();
            float width = GetWidth() + 16f; // padding
            float height = 105f; // hauteur fixe pour les cartes réduites + badge
            rt.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// Retourne la largeur totale du groupe.
        /// </summary>
        public float GetWidth()
        {
            if (_cards.Count == 0) return 60f;
            return (_cards.Count - 1) * _cardSpacing + 70f;
        }
    }
}
