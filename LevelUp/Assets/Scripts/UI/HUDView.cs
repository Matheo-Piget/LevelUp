using System.Collections.Generic;
using UnityEngine;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Messages de statut du jeu (« toast ») : annonces des coups marquants et
    /// erreurs d'action, avec une courte file d'attente. Le reste de l'ancien
    /// HUD (tour, phase, niveau, pioche) est porté par <see cref="GameScreenLayout"/>
    /// et les pastilles joueurs ; l'habillage du toast aussi.
    /// </summary>
    public class HUDView : MonoBehaviour
    {
        [Header("Status")]
        [SerializeField] private TextMeshProUGUI? _statusText;
        [SerializeField] private CanvasGroup? _statusCanvasGroup;
        [SerializeField] private float _statusDisplayDuration = 2.5f;

        private float _statusTimer;
        private RectTransform? _statusRect;
        private Vector2? _statusRest;

        // Noms des joueurs (injectés par GameBootstrapper) : « Bot 2 pose le niveau 3 ».
        private List<string>? _playerNames;

        // File des messages : un message arrivé pendant qu'un autre vient de
        // s'afficher attend son tour (au lieu de l'écraser dans la même image).
        private const float MinVisibleSeconds = 1.1f;
        private const int MaxQueued = 3;
        private readonly Queue<(string Text, bool Error)> _queue = new();
        private float _shownFor;

        /// <summary>Le toast s'affiche (vrai = erreur) : le layout l'habille en conséquence.</summary>
        public event System.Action<bool>? StatusStyleChanged;

        /// <summary>Noms des joueurs, à injecter après <see cref="GameManager.StartGame(int, int, bool)"/>.</summary>
        public void SetPlayerNames(List<string> names) => _playerNames = names;

        private void OnEnable()
        {
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Subscribe<LevelLaidDownEvent>(OnLevelLaidDown);
            EventBus.Subscribe<PlayerSkippedEvent>(OnPlayerSkipped);
            EventBus.Subscribe<ForcedDrawEvent>(OnForcedDraw);
            EventBus.Subscribe<RoundEndedEvent>(OnRoundEnded);
            EventBus.Subscribe<ActionCardPlayedEvent>(OnActionCardPlayed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Unsubscribe<LevelLaidDownEvent>(OnLevelLaidDown);
            EventBus.Unsubscribe<PlayerSkippedEvent>(OnPlayerSkipped);
            EventBus.Unsubscribe<ForcedDrawEvent>(OnForcedDraw);
            EventBus.Unsubscribe<RoundEndedEvent>(OnRoundEnded);
            EventBus.Unsubscribe<ActionCardPlayedEvent>(OnActionCardPlayed);
        }

        private void Start()
        {
            // Sans CanvasGroup, le fondu n'agirait sur rien et le message resterait affiché.
            if (_statusCanvasGroup == null && _statusText != null)
            {
                _statusCanvasGroup = _statusText.GetComponent<CanvasGroup>();
                if (_statusCanvasGroup == null)
                {
                    _statusCanvasGroup = _statusText.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (_statusCanvasGroup != null) _statusCanvasGroup.alpha = 0f;
            if (_statusText != null) _statusRect = _statusText.GetComponent<RectTransform>();
        }

        private void Update()
        {
            _shownFor += Time.unscaledDeltaTime;

            // Message en attente : affiché dès que le précédent a été lu.
            if (_queue.Count > 0 && _shownFor >= MinVisibleSeconds)
            {
                (string text, bool error) = _queue.Dequeue();
                Display(text, error);
            }

            if (_statusTimer <= 0f) return;
            _statusTimer -= Time.unscaledDeltaTime;
            FrameBudget.Ping();

            // Fondu dans la dernière demi-seconde.
            if (_statusTimer <= 0.5f && _statusCanvasGroup != null)
            {
                _statusCanvasGroup.alpha = Mathf.Max(0f, _statusTimer / 0.5f);
            }
        }

        /// <summary>Message d'erreur (action refusée) : toast teinté, au même endroit.</summary>
        public void ShowError(string message) => ShowStatus(message, error: true);

        /// <summary>
        /// Affiche un message. Si un autre vient d'apparaître, celui-ci attend
        /// son tour (file courte, sans doublon).
        /// </summary>
        public void ShowStatus(string message) => ShowStatus(message, error: false);

        public void ShowStatus(string message, bool error)
        {
            if (string.IsNullOrEmpty(message)) return;
            bool busy = _statusTimer > 0f && _shownFor < MinVisibleSeconds;
            if (busy)
            {
                if (_statusText != null && _statusText.text == message) return;
                foreach ((string text, bool _) in _queue)
                {
                    if (text == message) return;
                }
                if (_queue.Count >= MaxQueued) _queue.Dequeue();
                _queue.Enqueue((message, error));
                return;
            }
            Display(message, error);
        }

        private void Display(string message, bool error)
        {
            _shownFor = 0f;
            StatusStyleChanged?.Invoke(error);
            if (_statusText != null) _statusText.text = message;
            if (_statusCanvasGroup != null) _statusCanvasGroup.alpha = 1f;
            _statusTimer = _statusDisplayDuration;

            if (_statusRect == null || _statusText == null) return;

            _statusRect.localScale = Vector3.one * 0.9f;
            UITween.ScaleTo(_statusText.gameObject, _statusRect, Vector3.one, 0.25f);

            // Glisse depuis le haut (le toast vit sous le bord supérieur). Cible
            // mémorisée : un message qui en chasse un autre en plein glissement
            // ne doit pas faire dériver la position de repos.
            _statusRest ??= _statusRect.anchoredPosition;
            Vector2 target = _statusRest.Value;
            _statusRect.anchoredPosition = target + Vector2.up * 20f;
            UITween.MoveTo(_statusText.gameObject, _statusRect, target, 0.3f);
        }

        private string ResolvePlayerName(int playerIndex)
        {
            if (_playerNames != null && playerIndex >= 0 && playerIndex < _playerNames.Count)
            {
                return _playerNames[playerIndex];
            }
            return $"Joueur {playerIndex + 1}";
        }

        private void OnRoundStarted(RoundStartedEvent evt) => ShowStatus($"Manche {evt.RoundNumber}");

        private void OnLevelLaidDown(LevelLaidDownEvent evt) =>
            ShowStatus($"{ResolvePlayerName(evt.PlayerIndex)} pose le niveau {evt.Level} !");

        private void OnPlayerSkipped(PlayerSkippedEvent evt) =>
            ShowStatus($"{ResolvePlayerName(evt.PlayerIndex)} passe son tour");

        private void OnForcedDraw(ForcedDrawEvent evt) =>
            ShowStatus($"+{evt.CardCount} cartes pour {ResolvePlayerName(evt.PlayerIndex)}");

        /// <summary>Carte action jouée : un seul message clair, qui nomme la cible.</summary>
        private void OnActionCardPlayed(ActionCardPlayedEvent evt)
        {
            string target = ResolvePlayerName(evt.TargetPlayerIndex);
            string message = evt.Card.Type switch
            {
                CardType.Skip      => $"{ResolvePlayerName(evt.PlayerIndex)} fait passer son tour à {target}",
                CardType.Draw2     => $"{target} pioche 2 cartes",
                CardType.WildDraw2 => $"{target} pioche 2 cartes",
                _                  => ""
            };
            if (message.Length > 0) ShowStatus(message);
        }

        // Le récap de fin de manche annonce le vainqueur : pas de toast en double.
        private void OnRoundEnded(RoundEndedEvent evt) => _queue.Clear();
    }
}
