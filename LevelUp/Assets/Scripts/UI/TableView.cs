using System.Collections.Generic;
using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Affiche les combinaisons posées sur la table par tous les joueurs.
    /// Gère le positionnement dynamique des zones par joueur.
    /// </summary>
    public class TableView : MonoBehaviour
    {
        [SerializeField] private RectTransform? _tableContainer;
        [SerializeField] private GameObject? _cardPrefab;
        [SerializeField] private GameObject? _meldGroupPrefab;
        [SerializeField] private float _meldSpacing = 20f;
        [SerializeField] private float _cardInMeldSpacing = 35f;
        [SerializeField] private float _playerZoneSpacing = 40f;

        private readonly Dictionary<int, List<MeldGroupView>> _playerMeldGroups = new();

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

        /// <summary>
        /// Nettoie la table au début d'un nouveau round.
        /// </summary>
        private void OnRoundStarted(RoundStartedEvent evt)
        {
            ClearTable();
        }

        /// <summary>
        /// Affiche les combinaisons quand un joueur pose son niveau.
        /// </summary>
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

        /// <summary>
        /// Met à jour une combinaison quand une carte y est ajoutée.
        /// </summary>
        private void OnCardAddedToMeld(CardAddedToMeldEvent evt)
        {
            if (!_playerMeldGroups.TryGetValue(evt.MeldOwnerIndex, out List<MeldGroupView>? groups))
                return;

            // Retrouver le bon groupe et ajouter la carte
            foreach (MeldGroupView group in groups)
            {
                if (group.TryAddCard(evt.Card, _cardPrefab))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Crée un groupe de combinaison sur la table.
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
                groupObj = new GameObject($"Meld_P{playerIndex}",
                    typeof(RectTransform));
                groupObj.transform.SetParent(_tableContainer, false);
            }

            MeldGroupView groupView = groupObj.AddComponent<MeldGroupView>();
            groupView.Initialize(playerIndex, _cardInMeldSpacing);

            foreach (CardModel card in cards)
            {
                groupView.TryAddCard(card, _cardPrefab);
            }

            _playerMeldGroups[playerIndex].Add(groupView);
        }

        /// <summary>
        /// Repositionne tous les groupes de combinaisons sur la table.
        /// </summary>
        private void LayoutTable()
        {
            if (_tableContainer == null) return;

            float currentY = 0f;

            foreach (KeyValuePair<int, List<MeldGroupView>> kvp in _playerMeldGroups)
            {
                float currentX = 0f;

                foreach (MeldGroupView group in kvp.Value)
                {
                    RectTransform rt = group.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(currentX, currentY);
                    currentX += group.GetWidth() + _meldSpacing;
                }

                currentY -= _playerZoneSpacing + 100f;
            }
        }

        /// <summary>
        /// Vérifie si une position écran correspond à une combinaison.
        /// Retourne l'index du propriétaire et l'index de la combinaison.
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
    /// Vue d'un groupe de combinaison (une suite, un brelan, etc.) sur la table.
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

            // Réduire la taille des cartes sur la table
            cardView.RectTransform.localScale = Vector3.one * 0.7f;

            _cards.Add(cardView);
            LayoutCards();
            return true;
        }

        /// <summary>
        /// Positionne les cartes du groupe horizontalement.
        /// </summary>
        private void LayoutCards()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].RectTransform.anchoredPosition = new Vector2(i * _cardSpacing, 0);
            }
        }

        /// <summary>
        /// Retourne la largeur totale du groupe.
        /// </summary>
        public float GetWidth()
        {
            if (_cards.Count == 0) return 0f;
            return (_cards.Count - 1) * _cardSpacing + 80f; // 80 = largeur d'une carte réduite
        }
    }
}
