using System.Collections;
using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Minuteur de tour (règle de partie, désactivé par défaut). Compte le temps
    /// du joueur humain actif, l'affiche sur sa pastille, prévient à 10 s, puis
    /// termine le tour à sa place à 0 s (pioche, passe, défausse — commandes
    /// normales, donc relayées en ligne).
    ///
    /// En ligne, chaque appareil ne termine que SON tour ; l'hôte sert de filet
    /// pour un joueur distant resté muet (appli en arrière-plan) après un délai
    /// de grâce. Les bots n'ont pas de minuteur (ils jouent vite).
    /// </summary>
    public class TurnTimer : MonoBehaviour
    {
        private const float WarningSeconds = 10f;
        private const float HostGraceSeconds = 8f;

        private GameManager? _gameManager;
        private GameInputController? _input;
        private LevelProgressView? _players;
        private HUDView? _hud;

        private bool _active;
        private int _turnPlayer = -1;
        private int _round;
        private float _total;
        private float _remaining;
        private float _hostGrace;
        private bool _warned;
        private bool _expiredHandled;

        public void Initialize(GameManager gameManager, GameInputController? input,
            LevelProgressView? players, HUDView? hud)
        {
            _gameManager = gameManager;
            _input = input;
            _players = players;
            _hud = hud;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<RoundEndedEvent>(OnRoundEnded);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<RoundEndedEvent>(OnRoundEnded);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            if (_gameManager == null) return;
            MatchRules rules = GameSession.Rules;
            if (!rules.HasTurnTimer || evt.PlayerIndex < 0 || evt.PlayerIndex >= _gameManager.Players.Count)
            {
                Stop();
                return;
            }

            PlayerModel player = _gameManager.Players[evt.PlayerIndex];
            if (player.IsAI)
            {
                Stop();
                return;
            }

            _active = true;
            _turnPlayer = evt.PlayerIndex;
            _round = _gameManager.RoundNumber;
            _total = rules.TurnTimerSeconds;
            _remaining = _total;
            _hostGrace = HostGraceSeconds;
            _warned = false;
            _expiredHandled = false;
            _players?.SetTurnTimer(_turnPlayer, _remaining, _total);
        }

        private void OnRoundEnded(RoundEndedEvent evt) => Stop();

        private void OnGameOver(GameOverEvent evt) => Stop();

        private void Stop()
        {
            if (!_active) return;
            _active = false;
            _turnPlayer = -1;
            _players?.ClearTurnTimer();
        }

        private void Update()
        {
            if (!_active || _gameManager?.TurnManager == null) return;

            // Tour terminé sans nouvel événement (joueur converti en bot, fin de partie…).
            PlayerModel current = _gameManager.TurnManager.CurrentPlayer;
            if (_gameManager.State != GameState.PlayerTurn || current.Index != _turnPlayer || current.IsAI
                || _gameManager.RoundNumber != _round)
            {
                Stop();
                return;
            }

            // Time.deltaTime : la pause solo (timeScale 0) fige aussi le minuteur.
            _remaining -= Time.deltaTime;
            _players?.SetTurnTimer(_turnPlayer, _remaining, _total);

            bool local = GameSession.IsLocalHuman(current);
            if (local && !_warned && _remaining <= WarningSeconds && _total > WarningSeconds * 1.5f)
            {
                _warned = true;
                _hud?.ShowStatus("Plus que 10 secondes !");
            }

            if (_remaining > 0f || _expiredHandled) return;

            if (local)
            {
                _expiredHandled = true;
                _hud?.ShowStatus("Temps écoulé : fin du tour");
                _input?.AutoFinishTurn();
            }
            else if (GameSession.IsOnline && GameSession.IsHost)
            {
                _hostGrace -= Time.deltaTime;
                if (_hostGrace <= 0f)
                {
                    _expiredHandled = true;
                    StartCoroutine(HostFinishTurn(current.Index));
                }
            }
        }

        /// <summary>
        /// Hôte : termine le tour d'un joueur distant qui ne répond plus. Les
        /// commandes partent à tous les appareils comme n'importe quelle action.
        /// </summary>
        private IEnumerator HostFinishTurn(int seat)
        {
            if (_gameManager?.TurnManager == null) yield break;
            int round = _gameManager.RoundNumber;

            for (int step = 0; step < 8; step++)
            {
                if (_gameManager?.TurnManager == null || _gameManager.State != GameState.PlayerTurn) yield break;
                PlayerModel current = _gameManager.TurnManager.CurrentPlayer;
                if (current.Index != seat || current.IsAI || _gameManager.RoundNumber != round) yield break;

                IGameCommand? command = TurnAutoPlay.NextCommand(_gameManager);
                if (command == null || !_gameManager.ExecuteCommand(command).Success) yield break;
                yield return new WaitForSeconds(0.25f);
            }
        }
    }
}
