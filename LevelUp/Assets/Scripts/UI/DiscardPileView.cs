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
    /// Chaque pile montre la carte du dessus face visible, avec un compteur et un badge joueur.
    /// Anime l'arrivée de chaque carte défaussée.
    /// </summary>
    public class DiscardPileView : MonoBehaviour
    {
        [SerializeField] private RectTransform? _container;
        [SerializeField] private GameObject? _cardPrefab;
        [SerializeField] private AnimationController? _animController;
        [SerializeField] private float _pileSpacing = 130f;

        private readonly List<PileSlot> _pileSlots = new();
        private int _playerCount;
        // Pour peek la nouvelle carte du dessus après une pioche depuis défausse.
        private DeckManager? _deckManager;

        // Couleurs par joueur (mêmes que TableView)
        private static readonly Color[] PlayerBadgeColors =
        {
            Constants.CardBlue,
            Constants.CardRed,
            Constants.CardGreen,
            Constants.CardPurple,
            Constants.CardOrange,
            Constants.CardYellow
        };

        private void OnEnable()
        {
            EventBus.Subscribe<CardDiscardedEvent>(OnCardDiscarded);
            EventBus.Subscribe<CardDrawnEvent>(OnCardDrawn);
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CardDiscardedEvent>(OnCardDiscarded);
            EventBus.Unsubscribe<CardDrawnEvent>(OnCardDrawn);
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        /// <summary>
        /// Un joueur a pioché depuis une pile de défausse → retirer la carte du dessus
        /// ET révéler la carte qui se trouvait juste en dessous, si elle existe.
        /// La pile (List côté modèle) n'est PAS vidée, seule la top est retirée.
        /// </summary>
        private void OnCardDrawn(CardDrawnEvent evt)
        {
            if (!evt.FromDiscard) return;
            if (evt.DiscardPileIndex < 0 || evt.DiscardPileIndex >= _pileSlots.Count) return;

            PileSlot slot = _pileSlots[evt.DiscardPileIndex];

            // 1. Retirer la carte du dessus (celle qui vient d'être piochée)
            if (slot.TopCardView != null)
            {
                Destroy(slot.TopCardView.gameObject);
                slot.TopCardView = null;
            }

            slot.CardCount = Mathf.Max(0, slot.CardCount - 1);
            slot.CountText.text = slot.CardCount.ToString();
            slot.CountText.color = slot.CardCount > 3 ? Constants.CardYellow : Constants.TextSecondary;

            // 2. Révéler la nouvelle carte du dessus si la pile n'est pas vide.
            //    On peek le modèle (DeckManager a déjà pop) pour savoir quelle carte afficher.
            if (_deckManager == null || _cardPrefab == null) return;

            CardModel? newTop = _deckManager.PeekDiscard(evt.DiscardPileIndex);
            if (!newTop.HasValue) return;

            GameObject cardObj = Instantiate(_cardPrefab, slot.Root.transform);
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

        /// <summary>
        /// Initialise les emplacements de défausse pour tous les joueurs.
        /// <paramref name="deckManager"/> permet de révéler la carte précédente
        /// quand on pioche depuis une pile (peek de la nouvelle top).
        /// </summary>
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
        }

        /// <summary>
        /// Crée un slot de pile de défausse pour un joueur.
        /// </summary>
        private PileSlot CreatePileSlot(int playerIndex, string playerName, float xPos)
        {
            // Conteneur du slot
            GameObject slotObj = new($"DiscardSlot_P{playerIndex}",
                typeof(RectTransform));
            slotObj.transform.SetParent(_container, false);

            RectTransform slotRt = slotObj.GetComponent<RectTransform>();
            slotRt.anchoredPosition = new Vector2(xPos, 0);
            slotRt.sizeDelta = new Vector2(130f, 175f);

            // Emplacement vide (fond subtil) — zone de hit-test généreuse pour faciliter le drop
            GameObject emptyBg = new("EmptyBg", typeof(RectTransform), typeof(Image));
            emptyBg.transform.SetParent(slotObj.transform, false);

            RectTransform bgRt = emptyBg.GetComponent<RectTransform>();
            bgRt.sizeDelta = new Vector2(120f, 165f);
            bgRt.anchoredPosition = Vector2.zero;

            Image bgImg = emptyBg.GetComponent<Image>();
            bgImg.sprite = UIFactory.RoundedSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.08f, 0.12f, 0.18f, 0.55f);
            bgImg.raycastTarget = true;

            // Badge joueur
            Color badgeColor = playerIndex < PlayerBadgeColors.Length
                ? PlayerBadgeColors[playerIndex]
                : Constants.TextPrimary;

            GameObject badgeObj = new("Badge", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(slotObj.transform, false);

            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchoredPosition = new Vector2(0, -70f);
            badgeRt.sizeDelta = new Vector2(70f, 20f);

            Image badgeImg = badgeObj.GetComponent<Image>();
            badgeImg.sprite = UIFactory.RoundedSprite;
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = badgeColor;
            badgeImg.raycastTarget = false;

            // Nom joueur dans le badge
            GameObject nameObj = new("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(badgeObj.transform, false);

            RectTransform nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = Vector2.zero;
            nameRt.anchorMax = Vector2.one;
            nameRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
            nameText.text = playerName;
            nameText.fontSize = 11;
            nameText.color = Constants.CardFaceColor;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontStyle = FontStyles.Bold;
            nameText.raycastTarget = false;

            // Compteur de cartes
            GameObject countObj = new("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            countObj.transform.SetParent(slotObj.transform, false);

            RectTransform countRt = countObj.GetComponent<RectTransform>();
            countRt.anchoredPosition = new Vector2(35f, 50f);
            countRt.sizeDelta = new Vector2(30f, 20f);

            TextMeshProUGUI countText = countObj.GetComponent<TextMeshProUGUI>();
            countText.text = "0";
            countText.fontSize = 12;
            countText.color = Constants.TextSecondary;
            countText.alignment = TextAlignmentOptions.Center;
            countText.fontStyle = FontStyles.Bold;
            countText.raycastTarget = false;

            return new PileSlot
            {
                PlayerIndex = playerIndex,
                Root = slotObj,
                SlotRect = slotRt,
                BackgroundRect = bgRt,
                CountText = countText,
                TopCardView = null,
                CardCount = 0
            };
        }

        /// <summary>
        /// Appelé quand une carte est défaussée — met à jour la pile et anime.
        /// </summary>
        private void OnCardDiscarded(CardDiscardedEvent evt)
        {
            if (evt.PlayerIndex < 0 || evt.PlayerIndex >= _pileSlots.Count) return;

            PileSlot slot = _pileSlots[evt.PlayerIndex];
            slot.CardCount++;

            // Supprimer l'ancienne top card
            if (slot.TopCardView != null)
            {
                Destroy(slot.TopCardView.gameObject);
                slot.TopCardView = null;
            }

            if (_cardPrefab == null) return;

            // Créer la nouvelle top card
            GameObject cardObj = Instantiate(_cardPrefab, slot.Root.transform);
            CardView cardView = cardObj.GetComponent<CardView>();
            if (cardView != null)
            {
                cardView.Setup(evt.Card, true);
                cardView.SetInteractable(true);
                cardView.RectTransform.localScale = Vector3.one * 0.65f;
                slot.TopCardView = cardView;

                // Animation d'arrivée
                StartCoroutine(AnimateCardArrival(cardView, slot));
            }

            // Mettre à jour le compteur
            slot.CountText.text = slot.CardCount.ToString();
            slot.CountText.color = slot.CardCount > 3 ? Constants.CardYellow : Constants.TextSecondary;
        }

        /// <summary>
        /// Anime l'arrivée d'une carte sur la pile de défausse. Tous les accès à rt/cg
        /// sont gardés : la carte peut être détruite à tout moment (tour suivant,
        /// nouveau round) sans que la coroutine ne crashe.
        /// </summary>
        private IEnumerator AnimateCardArrival(CardView card, PileSlot slot)
        {
            if (card == null) yield break;
            RectTransform rt = card.RectTransform;
            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            if (rt == null) yield break;

            // Commence au-dessus avec un scale plus grand
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
                // Overshoot ease
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

            // Petit bounce final — uniquement si la carte est toujours là
            if (_animController != null && rt != null && card != null)
            {
                _animController.AnimatePulse(rt);
            }
        }

        /// <summary>
        /// Nettoie les piles au début d'un nouveau round.
        /// </summary>
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
                slot.CountText.text = "0";
                slot.CountText.color = Constants.TextSecondary;
            }
        }

        /// <summary>
        /// Retourne le RectTransform du slot de défausse d'un joueur (pour le hit testing).
        /// </summary>
        public RectTransform? GetPileRect(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _pileSlots.Count) return null;
            return _pileSlots[playerIndex].BackgroundRect;
        }

        /// <summary>
        /// Retourne le nombre de slots de défausse.
        /// </summary>
        public int PileCount => _pileSlots.Count;

        /// <summary>
        /// Vérifie si une position est sur une pile de défausse et retourne l'index.
        /// </summary>
        public bool GetPileAtPosition(Vector2 screenPosition, out int pileIndex)
        {
            pileIndex = -1;
            for (int i = 0; i < _pileSlots.Count; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(
                    _pileSlots[i].BackgroundRect, screenPosition, null))
                {
                    pileIndex = i;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Zone de drop étendue : couvre toute la bande de défausse du joueur courant.
        /// Permet de défausser sans viser précisément une pile (réduit la friction).
        /// </summary>
        public bool IsInsideDiscardArea(Vector2 screenPosition)
        {
            if (_container == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(
                _container, screenPosition, null);
        }

        /// <summary>
        /// Retourne la pile la plus proche (utile quand on drop dans la zone élargie).
        /// </summary>
        public int GetNearestPileIndex(Vector2 screenPosition)
        {
            if (_pileSlots.Count == 0) return -1;
            int best = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _pileSlots.Count; i++)
            {
                Vector3 world = _pileSlots[i].BackgroundRect.position;
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
                float d = Vector2.SqrMagnitude(screen - screenPosition);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
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
            public GameObject Root = null!;
            public RectTransform SlotRect = null!;
            public RectTransform BackgroundRect = null!;
            public TextMeshProUGUI CountText = null!;
            public CardView? TopCardView;
            public int CardCount;
        }
    }
}
