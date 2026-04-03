using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Affichage du deck (pioche) style Balatro : pile de cartes empilées avec
    /// compteur, idle breathing, et réaction au clic (pulse).
    /// </summary>
    public class DeckView : MonoBehaviour
    {
        [SerializeField] private RectTransform? _deckContainer;
        [SerializeField] private Image? _topCardImage;
        [SerializeField] private TextMeshProUGUI? _countText;
        [SerializeField] private AnimationController? _animController;

        private int _cardCount;
        private float _breathTime;
        private readonly Image[] _stackCards = new Image[3];

        private void OnEnable()
        {
            EventBus.Subscribe<DeckChangedEvent>(OnDeckChanged);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DeckChangedEvent>(OnDeckChanged);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void Start()
        {
            StyleDeck();
            CreateStackEffect();
        }

        private void Update()
        {
            // Idle breathing subtil sur le deck
            if (_deckContainer != null && _cardCount > 0)
            {
                _breathTime += Time.deltaTime;
                float breathScale = 1f + Mathf.Sin(_breathTime * 2f) * 0.008f;
                float breathY = Mathf.Sin(_breathTime * 1.5f) * 1.5f;

                _deckContainer.localScale = Vector3.one * breathScale;
                // Petit mouvement Y subtil pas appliqué pour éviter les conflits de layout
            }
        }

        /// <summary>
        /// Style le deck avec les couleurs Balatro.
        /// </summary>
        private void StyleDeck()
        {
            if (_topCardImage != null)
            {
                _topCardImage.color = Constants.CardBack;
            }

            if (_countText != null)
            {
                _countText.color = Constants.TextPrimary;
                _countText.fontSize = 20;
                _countText.fontStyle = FontStyles.Bold;
            }
        }

        /// <summary>
        /// Crée des cartes empilées derrière le deck pour simuler l'épaisseur.
        /// </summary>
        private void CreateStackEffect()
        {
            if (_deckContainer == null) return;

            for (int i = 0; i < _stackCards.Length; i++)
            {
                GameObject stackObj = new($"StackCard_{i}", typeof(RectTransform), typeof(Image));
                stackObj.transform.SetParent(_deckContainer, false);
                stackObj.transform.SetAsFirstSibling();

                RectTransform rt = stackObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = new Vector2(-(i + 1) * 2f, (i + 1) * 2f);

                Image img = stackObj.GetComponent<Image>();
                float darken = 1f - (i + 1) * 0.15f;
                img.color = new Color(
                    Constants.CardBack.r * darken,
                    Constants.CardBack.g * darken,
                    Constants.CardBack.b * darken,
                    0.8f);
                img.raycastTarget = false;

                _stackCards[i] = img;
            }
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            UpdateVisuals();
        }

        private void OnDeckChanged(DeckChangedEvent evt)
        {
            _cardCount = evt.CardsRemaining;
            UpdateVisuals();

            // Pulse quand le deck change
            if (_animController != null && _deckContainer != null)
            {
                _animController.AnimatePulse(_deckContainer);
            }
        }

        private void UpdateVisuals()
        {
            if (_countText != null)
            {
                _countText.text = _cardCount.ToString();

                // Rouge si peu de cartes
                _countText.color = _cardCount switch
                {
                    <= 5 => Constants.CardRed,
                    <= 15 => Constants.CardYellow,
                    _ => Constants.TextPrimary
                };
            }

            // Cacher les cartes stack si le deck est presque vide
            for (int i = 0; i < _stackCards.Length; i++)
            {
                if (_stackCards[i] != null)
                {
                    _stackCards[i].gameObject.SetActive(_cardCount > (i + 1) * 10);
                }
            }
        }
    }
}
