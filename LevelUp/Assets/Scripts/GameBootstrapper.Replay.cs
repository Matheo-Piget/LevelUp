using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LevelUp.UI;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// « Revoir la manche » (partie solo) : la partie étant déterministe et
    /// enregistrée commande par commande, on la reconstruit en silence jusqu'au
    /// début de la manche qui vient de finir, puis on rejoue ses commandes une à
    /// une, animations comprises. À la fin, l'état est exactement celui d'avant
    /// (récap de la même manche) : rien n'est sauvegardé ni compté deux fois.
    /// </summary>
    public partial class GameBootstrapper
    {
        // Journal de la partie solo en cours (de quoi la rejouer).
        private readonly List<IGameCommand> _log = new();
        private List<PlayerSetup>? _logSetups;
        private int _logSeed;
        private MatchRules _logRules = MatchRules.Classic;

        private bool _replaying;
        private bool _replayStep;
        private bool _replaySkip;
        private GameObject? _replaySkipButton;

        private void BeginLog(IReadOnlyList<PlayerSetup> setups, int seed, MatchRules rules,
            IReadOnlyList<IGameCommand>? history = null)
        {
            _log.Clear();
            if (history != null) _log.AddRange(history);
            _logSetups = new List<PlayerSetup>(setups);
            _logSeed = seed;
            _logRules = rules;
        }

        /// <summary>Vrai si la manche qui vient de finir peut être revue (solo, hors tutoriel et pass-and-play).</summary>
        private bool CanReplayRound()
        {
            return !_replaying && _saveActive && !GameSession.IsOnline && _logSetups != null
                   && _gameManager != null && _gameManager.State == GameState.EndRound
                   && (_passAndPlay == null || !_passAndPlay.Active)
                   && (_tutorial == null || !_tutorial.IsRunning);
        }

        private void HandleReplayRound()
        {
            if (!CanReplayRound()) return;
            StartCoroutine(ReplayRoundRoutine());
        }

        private IEnumerator ReplayRoundRoutine()
        {
            GameManager gm = _gameManager!;
            int round = gm.RoundNumber;

            // Début de la manche dans le journal : juste après la commande qui l'a lancée.
            int start = 0, seen = 1;
            for (int i = 0; i < _log.Count && seen < round; i++)
            {
                if (_log[i] is StartNextRoundCommand)
                {
                    seen++;
                    start = i + 1;
                }
            }
            List<IGameCommand> before = _log.GetRange(0, start);
            List<IGameCommand> during = _log.GetRange(start, _log.Count - start);
            if (during.Count == 0) yield break;

            _replaying = true;
            _replaySkip = false;
            CleanupAIPlayers();
            if (_turnTimer != null) _turnTimer.enabled = false;
            _inputController?.SetInputEnabled(false);
            // Pendant la relecture, seules les commandes rejouées ici passent.
            gm.CommandFilter = _ => _replayStep;
            ShowReplaySkip(true);

            _replayStep = true;
            bool ok = gm.RebuildFromHistory(_logSetups!, _logSeed, _logRules, before);
            _replayStep = false;
            if (ok)
            {
                SyncViewsFromModel();
                _hudView?.ShowStatus($"Relecture de la manche {round}");
                yield return new WaitForSeconds(1.2f);

                for (int i = 0; i < during.Count && ok; i++)
                {
                    // « Passer » : tout sauf la dernière commande en silence, puis
                    // la dernière normalement (c'est elle qui rouvre le récap).
                    if (_replaySkip && i < during.Count - 1)
                    {
                        bool muted = EventBus.Muted;
                        EventBus.Muted = true;
                        _replayStep = true;
                        for (; i < during.Count - 1 && ok; i++) ok = gm.ExecuteCommand(during[i]).Success;
                        _replayStep = false;
                        EventBus.Muted = muted;
                        if (!ok) break;
                        SyncViewsFromModel();
                    }

                    _replayStep = true;
                    ok = gm.ExecuteCommand(during[i]).Success;
                    _replayStep = false;
                    if (!_replaySkip) yield return new WaitForSeconds(during[i] is SkipPhaseCommand ? 0.12f : 0.5f);
                }
            }

            if (!ok)
            {
                // Ne devrait jamais arriver (partie déterministe) : on retombe sur l'état sauvegardé.
                Debug.LogWarning("[Replay] Relecture incohérente : état restauré depuis le journal");
                _replayStep = true;
                gm.RebuildFromHistory(_logSetups!, _logSeed, _logRules, _log);
                _replayStep = false;
                SyncViewsFromModel();
            }

            gm.CommandFilter = null;
            ShowReplaySkip(false);
            _inputController?.SetInputEnabled(true);
            if (_turnTimer != null) _turnTimer.enabled = true;
            CreateAIPlayers(_soloDifficulty, _soloMode == 1 ? _logSeed : (int?)null);
            _replaying = false;
        }

        private void ShowReplaySkip(bool visible)
        {
            if (_replaySkipButton == null && visible && _screenLayout?.Root != null)
            {
                Button skip = UIFactory.CreateButton(_screenLayout.Root, "BtnReplaySkip", "PASSER LA RELECTURE",
                    new Vector2(300f, 52f), () => _replaySkip = true);
                UISkin.Place((RectTransform)skip.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                    new Vector2(-40f, GameScreenLayout.IsPortrait ? 384f : 142f), new Vector2(300f, 52f));
                skip.gameObject.AddComponent<UIInputBlocker>();
                _replaySkipButton = skip.gameObject;
            }
            if (_replaySkipButton != null)
            {
                _replaySkipButton.SetActive(visible);
                if (visible) _replaySkipButton.transform.SetAsLastSibling();
            }
        }
    }
}
