using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// HUD style Balatro : badges arrondis, textes punchy, status pop-in animé.
    /// </summary>
    public class HUDView : MonoBehaviour
    {
        [Header("Current Player Info")]
        [SerializeField] private TextMeshProUGUI? _currentPlayerText;
        [SerializeField] private TextMeshProUGUI? _currentPhaseText;
        [SerializeField] private TextMeshProUGUI? _currentLevelText;

        [Header("Deck Info")]
        [SerializeField] private TextMeshProUGUI? _deckCountText;
        [SerializeField] private TextMeshProUGUI? _roundNumberText;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI? _statusText;
        [SerializeField] private CanvasGroup? _statusCanvasGroup;
        [SerializeField] private float _statusDisplayDuration = 2.5f;

        [Header("Game Over Panel")]
        [SerializeField] private GameObject? _gameOverPanel;
        [SerializeField] private TextMeshProUGUI? _winnerText;

        [Header("Animation")]
        [SerializeField] private AnimationController? _animController;

        private float _statusTimer;
        private RectTransform? _statusRect;
        private bool _initialized;

        private static readonly Color[] PlayerColors =
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
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<TurnPhaseChangedEvent>(OnPhaseChanged);
            EventBus.Subscribe<DeckChangedEvent>(OnDeckChanged);
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Subscribe<LevelLaidDownEvent>(OnLevelLaidDown);
            EventBus.Subscribe<PlayerSkippedEvent>(OnPlayerSkipped);
            EventBus.Subscribe<ForcedDrawEvent>(OnForcedDraw);
            EventBus.Subscribe<RoundEndedEvent>(OnRoundEnded);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<ActionCardPlayedEvent>(OnActionCardPlayed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<TurnPhaseChangedEvent>(OnPhaseChanged);
            EventBus.Unsubscribe<DeckChangedEvent>(OnDeckChanged);
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Unsubscribe<LevelLaidDownEvent>(OnLevelLaidDown);
            EventBus.Unsubscribe<PlayerSkippedEvent>(OnPlayerSkipped);
            EventBus.Unsubscribe<ForcedDrawEvent>(OnForcedDraw);
            EventBus.Unsubscribe<RoundEndedEvent>(OnRoundEnded);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<ActionCardPlayedEvent>(OnActionCardPlayed);
        }

        private void Start()
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
            if (_statusCanvasGroup != null) _statusCanvasGroup.alpha = 0f;
            if (_statusText != null) _statusRect = _statusText.GetComponent<RectTransform>();

            StyleHUD();
        }

        /// <summary>
        /// Applique le style Balatro aux éléments du HUD.
        /// </summary>
        private void StyleHUD()
        {
            // Style du texte joueur
            StyleText(_currentPlayerText, Constants.TextAccent, 22f, FontStyles.Bold);

            // Phase : plus lisible
            StyleText(_currentPhaseText, Constants.TextSecondary, 16f, FontStyles.Normal);

            // Level : gros et doré, bien visible
            StyleText(_currentLevelText, Constants.TextAccent, 22f, FontStyles.Bold);

            // Deck count
            StyleText(_deckCountText, Constants.TextPrimary, 18f, FontStyles.Bold);

            // Round
            StyleText(_roundNumberText, Constants.TextSecondary, 16f, FontStyles.Normal);

            // Status message
            StyleText(_statusText, Constants.TextPrimary, 24f, FontStyles.Bold);

            // Winner
            StyleText(_winnerText, Constants.TextAccent, 36f, FontStyles.Bold);

            // Ajouter des fonds arrondis derrière les groupes d'info
            AddPanelBackground(_currentPlayerText, 10f, 6f);
            AddPanelBackground(_currentLevelText, 10f, 6f);
            AddPanelBackground(_deckCountText, 8f, 4f);
            AddPanelBackground(_roundNumberText, 8f, 4f);

            _initialized = true;
        }

        /// <summary>
        /// Style un TextMeshProUGUI avec les couleurs Balatro.
        /// </summary>
        private static void StyleText(TextMeshProUGUI? text, Color color, float size, FontStyles style)
        {
            if (text == null) return;
            text.color = color;
            text.fontSize = size;
            text.fontStyle = style;
        }

        /// <summary>
        /// Ajoute un panneau de fond semi-transparent arrondi derrière un texte.
        /// </summary>
        private static void AddPanelBackground(TextMeshProUGUI? text, float paddingH, float paddingV)
        {
            if (text == null) return;

            Transform parent = text.transform.parent;
            if (parent == null) return;

            // Vérifier si un background existe déjà
            Transform? existingBg = parent.Find("PanelBg_" + text.name);
            if (existingBg != null) return;

            GameObject bgObj = new("PanelBg_" + text.name, typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(text.transform, false);
            bgObj.transform.SetAsFirstSibling();

            RectTransform rt = bgObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(paddingH * 2f, paddingV * 2f);
            rt.anchoredPosition = Vector2.zero;

            Image img = bgObj.GetComponent<Image>();
            img.sprite = UIFactory.RoundedSprite;
            img.type = Image.Type.Sliced;
            img.color = Constants.PanelBackground;
            img.raycastTarget = false;
        }

        private void Update()
        {
            if (_statusTimer > 0f)
            {
                _statusTimer -= Time.deltaTime;

                // Fade out progressif dans la dernière seconde
                if (_statusTimer <= 0.5f && _statusCanvasGroup != null)
                {
                    _statusCanvasGroup.alpha = Mathf.Max(0f, _statusTimer / 0.5f);
                }

                if (_statusTimer <= 0f && _statusCanvasGroup != null)
                {
                    _statusCanvasGroup.alpha = 0f;
                }
            }
        }

        /// <summary>
        /// Affiche un message de statut avec pop-in animé et slide.
        /// </summary>
        public void ShowStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
            if (_statusCanvasGroup != null)
            {
                _statusCanvasGroup.alpha = 1f;
            }
            _statusTimer = _statusDisplayDuration;

            if (_statusRect != null)
            {
                // Scale pop via UITween
                _statusRect.localScale = Vector3.one * 0.5f;
                UITween.ScaleTo(_statusText!.gameObject, _statusRect, Vector3.one, 0.35f);

                // Slide-in léger depuis le bas
                Vector2 target = _statusRect.anchoredPosition;
                _statusRect.anchoredPosition = target + Vector2.down * 20f;
                UITween.MoveTo(_statusText.gameObject, _statusRect, target, 0.3f);
            }
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            if (_currentPlayerText != null)
            {
                _currentPlayerText.text = $"PLAYER {evt.PlayerIndex + 1}";
                Color playerColor = PlayerColors[evt.PlayerIndex % PlayerColors.Length];

                if (_initialized)
                {
                    // Flash blanc → couleur joueur
                    _currentPlayerText.color = Color.white;
                    UITween.ColorTo(
                        _currentPlayerText.gameObject, _currentPlayerText, playerColor, 0.4f);

                    // Scale pop
                    RectTransform rt = _currentPlayerText.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.localScale = Vector3.one * 1.3f;
                        UITween.ScaleTo(_currentPlayerText.gameObject, rt, Vector3.one, 0.35f);
                    }
                }
                else
                {
                    _currentPlayerText.color = playerColor;
                }
            }
            UpdateCurrentLevel(evt.PlayerLevel);
            UpdatePhaseText(evt.Phase);
        }

        private void OnPhaseChanged(TurnPhaseChangedEvent evt)
        {
            UpdatePhaseText(evt.NewPhase, animated: true);
        }

        /// <summary>
        /// Texte de phase court et punchy style Balatro.
        /// Si animated=true, le texte fait un pop-in et le badge pulse.
        /// </summary>
        private void UpdatePhaseText(TurnPhase phase, bool animated = false)
        {
            if (_currentPhaseText == null) return;

            _currentPhaseText.text = phase switch
            {
                TurnPhase.Draw       => "PIOCHE - Cliquez pioche ou defausse",
                TurnPhase.LayDown    => "POSE - Selectionnez puis cliquez table",
                TurnPhase.AddToMelds => "AJOUTE - Glissez sur combinaison",
                TurnPhase.Discard    => "DEFAUSSE - Cliquez une carte",
                _                    => ""
            };

            Color phaseColor = Constants.GetPhaseColor(phase);
            _currentPhaseText.color = phaseColor;

            if (animated && _initialized)
            {
                RectTransform phaseRt = _currentPhaseText.GetComponent<RectTransform>();
                if (phaseRt != null)
                {
                    // Pop-in via UITween
                    UITween.ScaleTo(
                        _currentPhaseText.gameObject, phaseRt, Vector3.one, 0.3f);
                    phaseRt.localScale = Vector3.one * 0.7f;

                    // Flash de couleur blanche puis retour à la phase color
                    _currentPhaseText.color = Color.white;
                    UITween.ColorTo(
                        _currentPhaseText.gameObject, _currentPhaseText, phaseColor, 0.4f);
                }
            }
        }

        private void OnDeckChanged(DeckChangedEvent evt)
        {
            if (_deckCountText != null)
            {
                _deckCountText.text = $"{evt.CardsRemaining}";
            }
        }

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            if (_roundNumberText != null)
            {
                _roundNumberText.text = $"ROUND {evt.RoundNumber}";
            }
            ShowStatus($"ROUND {evt.RoundNumber}");
        }

        private void OnLevelLaidDown(LevelLaidDownEvent evt)
        {
            ShowStatus($"Player {evt.PlayerIndex + 1} pose le NIVEAU {evt.Level} !");
        }

        private void OnPlayerSkipped(PlayerSkippedEvent evt)
        {
            ShowStatus($"Player {evt.PlayerIndex + 1} SKIP !");
        }

        private void OnForcedDraw(ForcedDrawEvent evt)
        {
            ShowStatus($"Player {evt.PlayerIndex + 1} +{evt.CardCount} CARTES !");
        }

        private void OnActionCardPlayed(ActionCardPlayedEvent evt)
        {
            string actionName = evt.Card.Type switch
            {
                CardType.Skip      => "SKIP",
                CardType.Draw2     => "+2",
                CardType.Wild      => "WILD",
                CardType.WildDraw2 => "WILD +2",
                _                  => "ACTION"
            };
            ShowStatus($"{actionName} !");
        }

        private void OnRoundEnded(RoundEndedEvent evt)
        {
            ShowStatus($"Player {evt.WinnerIndex + 1} WIN !");
        }

        private void OnGameOver(GameOverEvent evt)
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);

                // Style le panel game over
                Image? panelImg = _gameOverPanel.GetComponent<Image>();
                if (panelImg != null)
                {
                    panelImg.color = new Color(0f, 0f, 0f, 0.85f);
                }

                // Pop-in animation
                RectTransform? panelRt = _gameOverPanel.GetComponent<RectTransform>();
                if (_animController != null && panelRt != null)
                {
                    _animController.AnimatePopIn(panelRt);
                }
            }
            if (_winnerText != null)
            {
                _winnerText.text = $"PLAYER {evt.WinnerIndex + 1} WINS!";
            }
        }

        /// <summary>
        /// Met à jour l'affichage du niveau du joueur actif avec l'objectif.
        /// </summary>
        public void UpdateCurrentLevel(int level)
        {
            if (_currentLevelText == null) return;

            string req = DescribeLevelRequirement(level);
            _currentLevelText.text = $"NIVEAU {level}  —  {req}";

            if (_initialized)
            {
                RectTransform rt = _currentLevelText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.localScale = Vector3.one * 1.2f;
                    UITween.ScaleTo(_currentLevelText.gameObject, rt, Vector3.one, 0.3f);
                }
            }
        }

        /// <summary>
        /// Décrit l'objectif d'un niveau en texte court.
        /// </summary>
        private static string DescribeLevelRequirement(int level)
        {
            return level switch
            {
                1 => "2 suites de 3",
                2 => "1 suite de 3 + 1 brelan",
                3 => "2 brelans",
                4 => "1 suite de 4 + 1 paire",
                5 => "1 flush de 5",
                6 => "1 suite de 5",
                7 => "1 carre + 1 paire",
                8 => "1 flush de 7",
                _ => "?"
            };
        }
    }
}
