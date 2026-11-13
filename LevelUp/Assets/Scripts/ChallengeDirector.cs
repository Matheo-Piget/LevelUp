using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.AI;
using LevelUp.UI;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Mode Défi : seul à la table, une main de 8 cartes, 6 pioches connues
    /// d'avance, un contrat à poser avant la fin. Tout passe par le jeu normal
    /// (mêmes gestes, mêmes commandes) : ce directeur impose la donne, montre
    /// les pioches à venir, interdit la défausse comme source de pioche et
    /// annonce le résultat. Ni sauvegarde ni statistiques de partie.
    /// </summary>
    public class ChallengeDirector : MonoBehaviour
    {
        private const string KeySolved = "levelup.challenge.solved";

        private GameManager? _gm;
        private Challenge? _challenge;
        private bool _running;
        private bool _finished;
        private int _drawn;

        private GameObject? _banner;
        private TextMeshProUGUI? _text;
        private ConfirmDialog? _dialog;
        private Action? _onQuit;
        private Action<int>? _onNext;

        public bool IsRunning => _running;

        /// <summary>Défis réussis sur cet appareil.</summary>
        public static int Solved => PlayerPrefs.GetInt(KeySolved, 0);

        public static List<PlayerSetup> Seats() => new() { new PlayerSetup(PlayerProfile.Name, false) };

        public static MatchRules Rules => new(handSize: ChallengeGenerator.HandSize, levelsToWin: 2,
            roundTurnLimit: ChallengeGenerator.Draws);

        /// <param name="onNext">Lance un nouveau défi de la difficulté donnée.</param>
        public void Setup(GameManager gameManager, RectTransform root, Transform dialogParent, Action onQuit,
            Action<int> onNext)
        {
            _gm = gameManager;
            _onQuit = onQuit;
            _onNext = onNext;
            BuildBanner(root);
            _dialog = gameObject.AddComponent<ConfirmDialog>();
            _dialog.Setup(dialogParent);
        }

        public void Begin(Challenge challenge)
        {
            if (_gm == null) return;
            _challenge = challenge;
            _drawn = 0;
            _finished = false;
            _running = true;
            RoundSummaryView.Suppressed = true;
            _gm.CommandFilter = Allow;
            _gm.CommandExecuted += OnExecuted;
            EventBus.Subscribe<LevelLaidDownEvent>(OnLaidDown);
            EventBus.Subscribe<RoundSummaryEvent>(OnRoundSummary);
            if (_banner != null) _banner.SetActive(true);
            RefreshBanner();
        }

        public void End()
        {
            if (!_running) return;
            _running = false;
            RoundSummaryView.Suppressed = false;
            if (_gm != null)
            {
                _gm.CommandFilter = null;
                _gm.CommandExecuted -= OnExecuted;
            }
            EventBus.Unsubscribe<LevelLaidDownEvent>(OnLaidDown);
            EventBus.Unsubscribe<RoundSummaryEvent>(OnRoundSummary);
            if (_banner != null) _banner.SetActive(false);
        }

        private void OnDestroy() => End();

        // Les pioches sont celles du paquet : on ne ramasse pas sa propre défausse,
        // et il n'y a pas de manche suivante.
        private bool Allow(IGameCommand command)
        {
            if (_finished) return false;
            return !(command is DrawFromDiscardCommand) && !(command is StartNextRoundCommand);
        }

        private void OnExecuted(IGameCommand command, CommandResult result)
        {
            if (command is DrawFromDeckCommand) _drawn++;
            RefreshBanner();
        }

        private void OnLaidDown(LevelLaidDownEvent evt) => Finish(won: true);

        // Fin par limite de tours (ou main vide) : perdu si le contrat n'a pas été posé.
        private void OnRoundSummary(RoundSummaryEvent evt)
        {
            if (!_finished) Finish(won: evt.LaidDown != null && evt.LaidDown.Length > 0 && evt.LaidDown[0]);
        }

        private void Finish(bool won)
        {
            if (_finished || _challenge == null) return;
            _finished = true;
            if (won)
            {
                PlayerPrefs.SetInt(KeySolved, Solved + 1);
                PlayerPrefs.Save();
            }

            int difficulty = _challenge.Difficulty;
            string level = ChallengeGenerator.DifficultyNames[difficulty];
            if (won)
            {
                int next = Mathf.Min(2, difficulty + 1);
                _dialog?.Show("Défi réussi !",
                    $"Contrat posé en {_drawn} pioche{(_drawn > 1 ? "s" : "")} sur {_challenge.Draws}. " +
                    $"Défis réussis : {Solved}.",
                    next > difficulty ? "PLUS DIFFICILE" : "NOUVEAU DÉFI",
                    () => _onNext?.Invoke(next), () => _onQuit?.Invoke());
            }
            else
            {
                _dialog?.Show("Défi manqué",
                    $"Le contrat ({ContractCatalog.Describe(_challenge.Contract)}) n'a pas été posé à temps. " +
                    "Une solution existait : regarde bien les pioches annoncées avant de défausser.",
                    "RÉESSAYER", () => _onNext?.Invoke(-1), () => _onQuit?.Invoke());
            }
        }

        // ────────────────────────────────────────────────────
        //  BANDEAU : contrat, tours restants, pioches à venir
        // ────────────────────────────────────────────────────

        private void RefreshBanner()
        {
            if (_text == null || _challenge == null) return;
            int left = Mathf.Max(0, _challenge.Draws - _drawn);
            StringBuilder sb = new();
            sb.Append("<b>Pose : ").Append(ContractCatalog.Describe(_challenge.Contract)).Append("</b>\n");
            sb.Append(left > 0 ? $"Pioches restantes ({left}) : " : "Dernière chance : pose maintenant.");
            for (int i = _drawn; i < _challenge.Upcoming.Count; i++)
            {
                if (i > _drawn) sb.Append("  ·  ");
                sb.Append(i == _drawn ? "<color=#E6CB92>" : "<color=#A7A198>")
                  .Append(CoachAdvisor.Describe(_challenge.Upcoming[i])).Append("</color>");
            }
            _text.text = sb.ToString();
        }

        private void BuildBanner(RectTransform root)
        {
            Image banner = UISkin.Rect(root, "ChallengeBanner", UISkin.WithAlpha(UISkin.Surface, 0.97f), 20f,
                raycast: true);
            banner.gameObject.AddComponent<UIInputBlocker>();
            RectTransform rt = banner.rectTransform;
            UISkin.Place(rt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 196f), new Vector2(520f, 0f));
            UISkin.VCol(banner.gameObject, 10f, new RectOffset(20, 20, 16, 14), TextAnchor.UpperLeft);
            banner.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UISkin.Outline(rt, 20f, 1.5f, UISkin.WithAlpha(UISkin.AccentLight, 0.6f))
                .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            TextMeshProUGUI caption = UISkin.Text(banner.transform, "Caption", "DÉFI", 12f, UISkin.Accent,
                TextWeight.SemiBold);
            caption.characterSpacing = 8f;

            _text = UIControls.Paragraph(banner.transform, "Text", "", 480f, 92f, 16f, UISkin.TextColor);
            _text.richText = true;

            Button quit = UIFactory.CreateButton(banner.transform, "BtnChallengeQuit", "QUITTER",
                new Vector2(150f, 44f), () => _onQuit?.Invoke(), subtle: true);
            UISkin.Size(quit.gameObject, 150f, 44f);

            _banner = banner.gameObject;
            _banner.SetActive(false);
        }
    }
}
