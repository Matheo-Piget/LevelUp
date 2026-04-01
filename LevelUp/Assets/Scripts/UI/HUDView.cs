using UnityEngine;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Affiche le HUD : joueur actif, phase du tour, cartes restantes dans le deck,
    /// niveau de chaque joueur, et messages de statut.
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
        [SerializeField] private float _statusDisplayDuration = 2f;

        [Header("Game Over Panel")]
        [SerializeField] private GameObject? _gameOverPanel;
        [SerializeField] private TextMeshProUGUI? _winnerText;

        private float _statusTimer;

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
        }

        private void Update()
        {
            // Fade out du message de statut
            if (_statusTimer > 0f)
            {
                _statusTimer -= Time.deltaTime;
                if (_statusTimer <= 0f && _statusCanvasGroup != null)
                {
                    _statusCanvasGroup.alpha = 0f;
                }
            }
        }

        /// <summary>
        /// Affiche un message de statut temporaire.
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
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            if (_currentPlayerText != null)
            {
                _currentPlayerText.text = $"Player {evt.PlayerIndex + 1}";
            }
            UpdatePhaseText(evt.Phase);
        }

        private void OnPhaseChanged(TurnPhaseChangedEvent evt)
        {
            UpdatePhaseText(evt.NewPhase);
        }

        private void UpdatePhaseText(TurnPhase phase)
        {
            if (_currentPhaseText == null) return;

            _currentPhaseText.text = phase switch
            {
                TurnPhase.Draw       => "Cliquez sur la pioche ou une défausse",
                TurnPhase.LayDown    => "Sélectionnez vos cartes puis cliquez sur la table, ou Passer",
                TurnPhase.AddToMelds => "Glissez une carte sur une combinaison, ou Terminer",
                TurnPhase.Discard    => "Cliquez 2x sur une carte ou glissez-la pour défausser",
                _                    => ""
            };
        }

        private void OnDeckChanged(DeckChangedEvent evt)
        {
            if (_deckCountText != null)
            {
                _deckCountText.text = $"Deck: {evt.CardsRemaining}";
            }
        }

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            if (_roundNumberText != null)
            {
                _roundNumberText.text = $"Round {evt.RoundNumber}";
            }
            ShowStatus($"Round {evt.RoundNumber} !");
        }

        private void OnLevelLaidDown(LevelLaidDownEvent evt)
        {
            ShowStatus($"Player {evt.PlayerIndex + 1} pose le niveau {evt.Level} !");
        }

        private void OnPlayerSkipped(PlayerSkippedEvent evt)
        {
            ShowStatus($"Player {evt.PlayerIndex + 1} est sauté !");
        }

        private void OnForcedDraw(ForcedDrawEvent evt)
        {
            ShowStatus($"Player {evt.PlayerIndex + 1} pioche {evt.CardCount} cartes !");
        }

        private void OnActionCardPlayed(ActionCardPlayedEvent evt)
        {
            string actionName = evt.Card.Type switch
            {
                CardType.Skip      => "Skip",
                CardType.Draw2     => "+2",
                CardType.Wild      => "Wild",
                CardType.WildDraw2 => "Wild +2",
                _                  => "Action"
            };
            ShowStatus($"Player {evt.PlayerIndex + 1} joue {actionName} !");
        }

        private void OnRoundEnded(RoundEndedEvent evt)
        {
            ShowStatus($"Player {evt.WinnerIndex + 1} gagne le round !");
        }

        private void OnGameOver(GameOverEvent evt)
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
            }
            if (_winnerText != null)
            {
                _winnerText.text = $"Player {evt.WinnerIndex + 1} gagne la partie !";
            }
        }

        /// <summary>
        /// Met à jour l'affichage du niveau du joueur actif.
        /// </summary>
        public void UpdateCurrentLevel(int level)
        {
            if (_currentLevelText != null)
            {
                _currentLevelText.text = $"Niveau {level}";
            }
        }
    }
}
