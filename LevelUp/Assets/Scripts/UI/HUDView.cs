using System.Collections.Generic;
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

        // Décor pill autour du texte "Tour de X" — flag d'idempotence pour ne pas
        // recréer le décor à chaque restyle.
        private bool _turnPillBuilt;
        private bool _deckPillBuilt;

        // Noms des joueurs (injectés par GameBootstrapper) — sert à afficher "Tour de Bot 2".
        private List<string>? _playerNames;

        // Le tour en cours utilise toujours l'indigo (couleur d'accent unique de l'UI).
        // Les couleurs de cartes restent saturées car elles portent l'info de jeu,
        // mais ici on parle de l'UI -> neutre + indigo.
        private static readonly Color ActiveColor = Constants.AccentPrimary;

        /// <summary>
        /// Injecte les noms des joueurs pour afficher "Tour de Bot 2" plutôt que "PLAYER 2".
        /// À appeler depuis GameBootstrapper après <see cref="GameManager.StartGame"/>.
        /// </summary>
        public void SetPlayerNames(List<string> names)
        {
            _playerNames = names;
        }

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
        /// Style minimal et neutre : la topbar reste fine, lisible, pas de couleurs criardes.
        /// Tailles inspirées de la spec : 12px pill, 13px body, 11px labels.
        /// </summary>
        private void StyleHUD()
        {
            // Joueur actif : "Tour de X" rendu en pill indigo (12px, indigo lighter)
            StyleText(_currentPlayerText, Constants.AccentLighter, 12f, FontStyles.Bold);
            BuildTurnPill(_currentPlayerText);

            // Phase : hint discret (TextSecondary, 13px). Plus de couleur verte criarde.
            StyleText(_currentPhaseText, Constants.TextSecondary, 13f, FontStyles.Normal);

            // Niveau actif : texte primaire neutre, taille topbar (13px)
            StyleText(_currentLevelText, Constants.TextSecondary, 13f, FontStyles.Normal);

            // Deck count : chiffre lisible (12px, blanc) + pill subtile
            StyleText(_deckCountText, Constants.TextPrimary, 12f, FontStyles.Bold);
            BuildDeckPill(_deckCountText);

            // Round : label discret en majuscules avec letter-spacing
            StyleText(_roundNumberText, Constants.TextMuted, 11f, FontStyles.Bold);
            ApplyLetterSpacing(_roundNumberText, 6f);

            // Status message
            StyleText(_statusText, Constants.TextPrimary, 22f, FontStyles.Bold);

            // Winner
            StyleText(_winnerText, Constants.AccentLight, 36f, FontStyles.Bold);

            _initialized = true;
        }

        /// <summary>
        /// Habille _currentPlayerText d'une pill indigo (bg 0.12, border 0.35, dot 6px).
        /// Idempotent : ne crée les éléments qu'une fois.
        /// </summary>
        private void BuildTurnPill(TextMeshProUGUI? text)
        {
            if (text == null || _turnPillBuilt) return;
            _turnPillBuilt = true;

            RectTransform rt = text.rectTransform;
            // Padding interne : le bg et le border débordent du rect texte pour faire la pill.
            const float padH = 14f;
            const float padV = 6f;
            const float dotRoom = 14f; // espace réservé à gauche pour le dot

            // Background (Surface 1 indigo translucide)
            CreatePillLayer(rt, "TurnPillBg", UIFactory.RoundedSprite,
                Constants.PillIndigoBg, padH + dotRoom, padV);
            // Border (Ring)
            CreatePillLayer(rt, "TurnPillBorder", UIFactory.RingSprite,
                Constants.PillIndigoBorder, padH + dotRoom, padV);

            // Dot 6px à gauche du texte
            GameObject dotObj = new("TurnPillDot",
                typeof(RectTransform), typeof(Image));
            dotObj.transform.SetParent(rt, false);
            RectTransform drt = dotObj.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0f, 0.5f);
            drt.anchorMax = new Vector2(0f, 0.5f);
            drt.pivot = new Vector2(0f, 0.5f);
            drt.sizeDelta = new Vector2(6f, 6f);
            drt.anchoredPosition = new Vector2(-22f, 0f);
            Image dotImg = dotObj.GetComponent<Image>();
            dotImg.sprite = UIFactory.RoundedSprite;
            dotImg.type = Image.Type.Sliced;
            dotImg.color = Constants.AccentLight;
            dotImg.raycastTarget = false;
        }

        private static void CreatePillLayer(RectTransform parent, string name,
            Sprite sprite, Color color, float padHLeft, float padV)
        {
            GameObject obj = new(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            obj.transform.SetAsFirstSibling();
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            // Le côté gauche reçoit dotRoom + pad (place pour le dot 6px),
            // le côté droit garde un padding standard de 12px.
            rt.offsetMin = new Vector2(-padHLeft, -padV);
            rt.offsetMax = new Vector2(12f, padV);

            Image img = obj.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
        }

        /// <summary>
        /// Pill discrète autour du compteur de pioche.
        /// </summary>
        private void BuildDeckPill(TextMeshProUGUI? text)
        {
            if (text == null || _deckPillBuilt) return;
            _deckPillBuilt = true;

            RectTransform rt = text.rectTransform;
            GameObject bgObj = new("DeckPillBg",
                typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(rt, false);
            bgObj.transform.SetAsFirstSibling();
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(-50f, -6f);
            bgRt.offsetMax = new Vector2(10f, 6f);
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = UIFactory.RoundedSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Constants.GlassTint;
            bgImg.raycastTarget = false;
        }

        private static void ApplyLetterSpacing(TextMeshProUGUI? text, float spacing)
        {
            if (text == null) return;
            text.characterSpacing = spacing;
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
                string playerName = ResolvePlayerName(evt.PlayerIndex);
                _currentPlayerText.text = $"Tour de {playerName}";
                _currentPlayerText.color = Constants.AccentLighter;

                // Pulse de la pill au changement de tour : pop subtil, pas de flash de couleur.
                if (_initialized)
                {
                    RectTransform rt = _currentPlayerText.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.localScale = Vector3.one * 1.05f;
                        UITween.ScaleTo(_currentPlayerText.gameObject, rt, Vector3.one, 0.25f);
                    }
                }
            }
            UpdateCurrentLevel(evt.PlayerLevel);
            UpdatePhaseText(evt.Phase);
        }

        private string ResolvePlayerName(int playerIndex)
        {
            if (_playerNames != null && playerIndex >= 0 && playerIndex < _playerNames.Count)
            {
                return _playerNames[playerIndex];
            }
            return $"Player {playerIndex + 1}";
        }

        private void OnPhaseChanged(TurnPhaseChangedEvent evt)
        {
            UpdatePhaseText(evt.NewPhase, animated: true);
        }

        /// <summary>
        /// Hint discret de phase : phrase courte, gris secondaire, puce indigo en préfixe.
        /// Plus de couleurs criardes par phase — l'info portée est minimale et neutre.
        /// </summary>
        private void UpdatePhaseText(TurnPhase phase, bool animated = false)
        {
            if (_currentPhaseText == null) return;

            string hint = phase switch
            {
                TurnPhase.Draw       => "Pioche ou defausse pour commencer",
                TurnPhase.LayDown    => "Selectionne tes cartes puis clique la table",
                TurnPhase.AddToMelds => "Glisse une carte sur une combinaison",
                TurnPhase.Discard    => "Clique une carte a defausser",
                _                    => ""
            };

            // Puce indigo (●) + hint en TextSecondary — tout sobre, lisible.
            _currentPhaseText.text = $"<color=#818CF8>●</color>  {hint}";
            _currentPhaseText.color = Constants.TextSecondary;

            if (animated && _initialized)
            {
                RectTransform phaseRt = _currentPhaseText.GetComponent<RectTransform>();
                if (phaseRt != null)
                {
                    UITween.ScaleTo(
                        _currentPhaseText.gameObject, phaseRt, Vector3.one, 0.25f);
                    phaseRt.localScale = Vector3.one * 0.95f;
                }
            }
        }

        private void OnDeckChanged(DeckChangedEvent evt)
        {
            if (_deckCountText != null)
            {
                // "Pioche  42" — label muted + chiffre en blanc pour la hiérarchie.
                _deckCountText.text =
                    $"<color=#9CA3AF><size=11>Pioche</size></color>  {evt.CardsRemaining}";
            }
        }

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            if (_roundNumberText != null)
            {
                _roundNumberText.text = $"ROUND  {evt.RoundNumber}";
                _roundNumberText.color = Constants.TextMuted;
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
            // "Niveau X" en blanc, l'objectif en gris secondaire pour la hiérarchie.
            _currentLevelText.text =
                $"NIVEAU {level}  <color=#9CA3AF>· {req}</color>";

            if (_initialized)
            {
                RectTransform rt = _currentLevelText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.localScale = Vector3.one * 1.1f;
                    UITween.ScaleTo(_currentLevelText.gameObject, rt, Vector3.one, 0.25f);
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
