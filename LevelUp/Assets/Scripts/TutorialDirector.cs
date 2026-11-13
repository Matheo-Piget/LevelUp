using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.UI;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Tutoriel jouable : une manche scriptée à deux (donne imposée, adversaire
    /// qui joue des coups écrits d'avance) où chaque étape n'autorise qu'un
    /// geste — piocher, poser, compléter, défausser, ramasser une défausse,
    /// sortir. Tout passe par les commandes normales du jeu : le tutoriel ne
    /// fait que filtrer celles qui ne sont pas attendues et afficher la consigne.
    /// Rien n'est sauvegardé ni compté dans les statistiques.
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        public const string OpponentName = "Margot";
        public const int HandSize = 8;

        private enum Expect { DrawDeck, LayDown, AddToMeld, Discard, DrawDiscard, Done }

        private sealed class Step
        {
            public Expect Expect;
            public string Text = "";
            public string Wrong = "";
        }

        private static readonly Step[] Steps =
        {
            new() { Expect = Expect.DrawDeck,
                Text = "<b>1 · Piocher.</b> Chaque tour commence par une carte : touche la pioche.",
                Wrong = "D'abord la pioche, à gauche du tapis." },
            new() { Expect = Expect.LayDown,
                Text = "<b>2 · Poser.</b> Ton contrat : 2 suites de 3. Touche 3-4-5 puis 9-10-11, et « Poser ».",
                Wrong = "Sélectionne 3-4-5 et 9-10-11 (six cartes), puis touche « Poser »." },
            new() { Expect = Expect.AddToMeld,
                Text = "<b>3 · Compléter.</b> Contrat posé ! Glisse ton 6 sur ta suite 3-4-5 pour t'en débarrasser.",
                Wrong = "Glisse le 6 sur la suite 3-4-5 posée sur la table." },
            new() { Expect = Expect.Discard,
                Text = "<b>4 · Défausser.</b> Finis ton tour : touche « Défausser », puis ta carte deux fois (ou glisse-la sur ta défausse).",
                Wrong = "Il ne reste qu'à défausser une carte pour finir ton tour." },
            new() { Expect = Expect.DrawDiscard,
                Text = "Margot a jeté un 12 : il prolonge ta suite 9-10-11. Touche <b>sa défausse</b> pour le prendre.",
                Wrong = "Prends le 12 : touche la défausse de Margot, pas la pioche." },
            new() { Expect = Expect.AddToMeld,
                Text = "Glisse le 12 sur ta suite 9-10-11.",
                Wrong = "Glisse le 12 sur la suite 9-10-11 posée sur la table." },
            new() { Expect = Expect.Discard,
                Text = "Dernière carte : défausse-la. Main vide = manche gagnée, et tu montes de <b>2 niveaux</b> !",
                Wrong = "Défausse ta dernière carte pour sortir." },
        };

        private GameManager? _gm;
        private HUDView? _hud;
        private int _step;
        private bool _running;
        private int _opponentDiscardId = -1;

        private GameObject? _banner;
        private TextMeshProUGUI? _text;
        private Action? _onQuit;

        /// <summary>Vrai pendant le tutoriel (le bootstrapper ne sauvegarde ni ne compte rien).</summary>
        public bool IsRunning => _running;

        public void Setup(GameManager gameManager, HUDView? hud, RectTransform root, Action onQuit)
        {
            _gm = gameManager;
            _hud = hud;
            _onQuit = onQuit;
            BuildBanner(root);
        }

        /// <summary>Sièges du tutoriel : le joueur et une adversaire scriptée (aucun AIPlayer ne la pilote : ses coups sont écrits ici).</summary>
        public static List<PlayerSetup> Seats() => new()
        {
            new PlayerSetup(PlayerProfile.Name, false),
            new PlayerSetup(OpponentName, true)
        };

        public static MatchRules Rules => new(handSize: HandSize, levelsToWin: 4);

        /// <summary>
        /// Ordre du paquet (dernier élément = dessus) : la main du joueur contient
        /// déjà son contrat, sa première pioche est un 6, et l'adversaire tient le 12.
        /// </summary>
        public List<CardModel> BuildDeck(GameConfig config)
        {
            List<CardModel> all = DeckManager.BuildComposition(config, Rules);
            CardModel Take(int value, CardColor color)
            {
                int i = all.FindIndex(c => c.Type == CardType.Normal && c.Value == value && c.Color == color);
                CardModel card = all[i];
                all.RemoveAt(i);
                return card;
            }

            List<CardModel> top = new()
            {
                // Main du joueur (8) : deux suites de 3 toutes prêtes + deux cartes isolées.
                Take(3, CardColor.Red), Take(4, CardColor.Blue), Take(5, CardColor.Green),
                Take(9, CardColor.Yellow), Take(10, CardColor.Purple), Take(11, CardColor.Orange),
                Take(15, CardColor.Red), Take(17, CardColor.Blue),
            };
            // Main de l'adversaire (8) : sans intérêt, sauf le 12 qu'elle jettera.
            CardModel twelve = Take(12, CardColor.Yellow);
            _opponentDiscardId = twelve.Id;
            top.Add(twelve);
            top.Add(Take(1, CardColor.Red)); top.Add(Take(18, CardColor.Green)); top.Add(Take(2, CardColor.Purple));
            top.Add(Take(14, CardColor.Orange)); top.Add(Take(7, CardColor.Blue)); top.Add(Take(16, CardColor.Yellow));
            top.Add(Take(13, CardColor.Green));
            // Pioches : le 6 du joueur, puis une carte quelconque pour l'adversaire.
            top.Add(Take(6, CardColor.Purple));
            top.Add(Take(8, CardColor.Red));

            top.Reverse();
            all.AddRange(top);
            return all;
        }

        public void Begin()
        {
            if (_gm == null) return;
            _step = 0;
            _running = true;
            _gm.CommandFilter = Allow;
            _gm.CommandFiltered += OnFiltered;
            _gm.CommandExecuted += OnExecuted;
            EventBus.Subscribe<RoundSummaryEvent>(OnRoundSummary);
            if (_banner != null) _banner.SetActive(true);
            ShowStep();
        }

        public void End()
        {
            if (!_running) return;
            _running = false;
            StopAllCoroutines();
            if (_gm != null)
            {
                _gm.CommandFilter = null;
                _gm.CommandFiltered -= OnFiltered;
                _gm.CommandExecuted -= OnExecuted;
            }
            EventBus.Unsubscribe<RoundSummaryEvent>(OnRoundSummary);
            if (_banner != null) _banner.SetActive(false);
        }

        private void OnDestroy() => End();

        // ────────────────────────────────────────────────────
        //  SCÉNARIO
        // ────────────────────────────────────────────────────

        private bool Allow(IGameCommand command)
        {
            // L'adversaire scriptée (siège 1) et la suite du jeu passent toujours.
            // Une seule manche : pas de manche suivante.
            if (command is StartNextRoundCommand) return false;
            if (command.PlayerIndex != 0) return true;
            if (_step >= Steps.Length) return false;

            return Steps[_step].Expect switch
            {
                Expect.DrawDeck => command is DrawFromDeckCommand,
                Expect.LayDown => command is LayDownLevelCommand,
                Expect.AddToMeld => command is AddToMeldCommand,
                // « Défausser » passe d'abord les étapes Poser / Compléter : autorisé aussi.
                Expect.Discard => command is DiscardCommand || command is SkipPhaseCommand,
                Expect.DrawDiscard => command is DrawFromDiscardCommand,
                _ => false
            };
        }

        private void OnFiltered(IGameCommand command)
        {
            if (_step < Steps.Length) _hud?.ShowStatus(Steps[_step].Wrong);
        }

        private void OnExecuted(IGameCommand command, CommandResult result)
        {
            if (!_running || command.PlayerIndex != 0 || _step >= Steps.Length) return;
            // Un passage d'étape ne termine pas la consigne « Défausser ».
            if (command is SkipPhaseCommand) return;

            Expect done = Steps[_step].Expect;
            _step++;
            ShowStep();

            // Fin du premier tour du joueur : l'adversaire joue ses coups écrits.
            if (done == Expect.Discard && _step < Steps.Length) StartCoroutine(OpponentTurn());
        }

        private IEnumerator OpponentTurn()
        {
            if (_text != null) _text.text = "Au tour de Margot…";
            yield return new WaitForSeconds(1.1f);
            if (_gm == null || !_running) yield break;
            _gm.ExecuteCommand(new DrawFromDeckCommand(1));
            yield return new WaitForSeconds(0.8f);
            _gm.ExecuteCommand(new SkipPhaseCommand(1, TurnPhase.LayDown));
            yield return new WaitForSeconds(0.5f);
            if (_gm.Players[1].TryGetHandCard(_opponentDiscardId, out CardModel twelve))
            {
                _gm.ExecuteCommand(new DiscardCommand(1, twelve));
            }
            yield return new WaitForSeconds(0.6f);
            ShowStep();
        }

        private void OnRoundSummary(RoundSummaryEvent evt)
        {
            if (!_running) return;
            _step = Steps.Length;
            PlayerPrefs.SetInt(Achievements.KeyTutorialDone, 1);
            PlayerPrefs.Save();
            if (_text != null)
            {
                _text.text = "<b>Bravo, tu connais l'essentiel !</b> Les contrats se corsent à chaque niveau. " +
                             "Touche « Terminer » pour revenir au menu.";
            }
        }

        private void ShowStep()
        {
            if (_text == null || _step >= Steps.Length) return;
            _text.text = Steps[_step].Text;
        }

        // ────────────────────────────────────────────────────
        //  BANDEAU
        // ────────────────────────────────────────────────────

        private void BuildBanner(RectTransform root)
        {
            Image banner = UISkin.Rect(root, "TutorialBanner", UISkin.WithAlpha(UISkin.Surface, 0.97f), 20f, raycast: true);
            banner.gameObject.AddComponent<UIInputBlocker>();
            RectTransform rt = banner.rectTransform;
            // Bas-gauche, au-dessus de la carte contrat : ne masque ni la main ni la table.
            UISkin.Place(rt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 196f), new Vector2(470f, 0f));
            UISkin.VCol(banner.gameObject, 10f, new RectOffset(20, 20, 16, 14), TextAnchor.UpperLeft);
            banner.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UISkin.Outline(rt, 20f, 1.5f, UISkin.WithAlpha(UISkin.AccentLight, 0.6f))
                .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            TextMeshProUGUI caption = UISkin.Text(banner.transform, "Caption", "TUTORIEL", 12f, UISkin.Accent,
                TextWeight.SemiBold);
            caption.characterSpacing = 8f;

            _text = UIControls.Paragraph(banner.transform, "Text", "", 430f, 72f, 17f, UISkin.TextColor);
            _text.richText = true;

            Button quit = UIFactory.CreateButton(banner.transform, "BtnTutorialQuit", "TERMINER",
                new Vector2(160f, 44f), () => _onQuit?.Invoke(), subtle: true);
            UISkin.Size(quit.gameObject, 160f, 44f);

            _banner = banner.gameObject;
            _banner.SetActive(false);
        }
    }
}
