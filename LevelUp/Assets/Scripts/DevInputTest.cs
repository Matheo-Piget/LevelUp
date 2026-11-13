// Outil de vérification des gestes : compilé seulement pour l'éditeur et le PC
// (jamais dans l'APK).
#if UNITY_EDITOR || UNITY_STANDALONE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using LevelUp.UI;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Test des gestes sans appareil (aucun effet en jeu normal) : lancé avec
    /// <c>-inputtest &lt;dossier&gt;</c>, le jeu démarre une partie à 2 et rejoue de
    /// vrais gestes (toucher, glisser) à travers <see cref="PointerInput.Override"/>,
    /// puis écrit <c>input-report.txt</c> (une ligne OK / ECHEC / IGNORE par
    /// scénario) et se ferme. Les réglages modifiés sont rétablis à la fin.
    /// </summary>
    public class DevInputTest : MonoBehaviour
    {
        private string _dir = "";
        private readonly StringBuilder _report = new();
        private PointerState? _pointer;
        private int _failures;

        private GameManager? _gm;
        private HandView? _hand;
        private GameInputController? _input;
        private DiscardPileView? _piles;
        private TableView? _table;
        private RectTransform? _deck;
        private RectTransform? _tableRect;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-inputtest");
            bool tutorial = false;
            if (i < 0)
            {
                i = Array.IndexOf(args, "-tutorialtest");
                tutorial = true;
            }
            if (i < 0)
            {
                i = Array.IndexOf(args, "-passtest");
                tutorial = false;
                _passMode = i >= 0;
            }
            if (i < 0)
            {
                i = Array.IndexOf(args, "-replaytest");
                _passMode = false;
                _replayMode = i >= 0;
            }
            if (i < 0 || i + 1 >= args.Length) return;
            _tutorialMode = tutorial;

            Application.runInBackground = true;
            GameObject go = new("DevInputTest");
            DontDestroyOnLoad(go);
            go.AddComponent<DevInputTest>()._dir = args[i + 1];
        }

        private void Update() => Application.runInBackground = true;

        private IEnumerator Start()
        {
            Directory.CreateDirectory(_dir);
            int savedPlayers = GameSettings.PlayerCount;
            int savedHumans = GameSettings.HumanCount;
            GameSettings.HumanCount = _passMode ? 2 : 1;
            int savedSpeed = GameSettings.BotSpeed;
            int savedSpecial = GameSettings.SpecialCards;
            int savedTimer = GameSettings.TurnTimer;
            bool savedPlayable = GameSettings.HintPlayable;
            bool savedQuick = GameSettings.QuickDiscard;
            int savedSort = GameSettings.HandSort;
            GameSettings.PlayerCount = 2;
            GameSettings.BotSpeed = 2;
            GameSettings.SpecialCards = 0;
            GameSettings.TurnTimer = 0;
            GameSettings.HintPlayable = false;
            GameSettings.QuickDiscard = false;
            GameSettings.HandSort = 0;

            yield return new WaitForSecondsRealtime(2.5f);
            if (_tutorialMode)
            {
                RulesPanel.TutorialRequested?.Invoke();
            }
            else
            {
                Click("BtnPlay");
                yield return new WaitForSecondsRealtime(1.0f);
                Click("BtnLaunch");
                yield return new WaitForSecondsRealtime(0.7f);
                if (FindButton("BtnConfirmOk") != null) Click("BtnConfirmOk");
            }
            yield return new WaitForSecondsRealtime(4.5f);

            _gm = FindObjectOfType<GameManager>();
            _hand = FindObjectOfType<HandView>();
            _input = FindObjectOfType<GameInputController>();
            _piles = FindObjectOfType<DiscardPileView>();
            _table = FindObjectOfType<TableView>();
            Canvas? canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                _deck = GameScreenLayout.Find(canvas.rootCanvas.transform, "DeckArea");
                _tableRect = GameScreenLayout.Find(canvas.rootCanvas.transform, "Table");
            }

            PointerInput.Override = () => _pointer;
            IEnumerator scenarios = _tutorialMode ? RunTutorial() : _passMode ? RunPassAndPlay() : _replayMode ? RunReplay() : RunScenarios();
            while (true)
            {
                bool more;
                try { more = scenarios.MoveNext(); }
                catch (Exception e)
                {
                    Line("ECHEC", "exception", e.ToString());
                    break;
                }
                if (!more) break;
                yield return scenarios.Current;
            }
            PointerInput.Override = null;

            GameSettings.PlayerCount = savedPlayers;
            GameSettings.HumanCount = savedHumans;
            GameSettings.BotSpeed = savedSpeed;
            GameSettings.SpecialCards = savedSpecial;
            GameSettings.TurnTimer = savedTimer;
            GameSettings.HintPlayable = savedPlayable;
            GameSettings.QuickDiscard = savedQuick;
            GameSettings.HandSort = savedSort;
            GameSettings.Flush();

            _report.Insert(0, $"RESULTAT : {(_failures == 0 ? "OK" : _failures + " ECHEC(S)")}\n");
            File.WriteAllText(Path.Combine(_dir, "input-report.txt"), _report.ToString());
            Application.Quit();
        }

        // ────────────────────────────────────────────────────
        //  SCÉNARIOS
        // ────────────────────────────────────────────────────

        private IEnumerator RunScenarios()
        {
            if (_gm?.TurnManager == null || _hand == null || _input == null || _piles == null || _deck == null)
            {
                Line("ECHEC", "initialisation", "objets de jeu introuvables");
                yield break;
            }
            TurnManager tm = _gm.TurnManager;
            PlayerModel me = _gm.Players[0];

            // 1. Toucher la pioche pioche une carte.
            yield return WaitMyDraw();
            int before = me.Hand.Count;
            yield return Tap(Center(_deck));
            Check("pioche au toucher", me.Hand.Count == before + 1 && tm.CurrentPhase != TurnPhase.Draw,
                $"main {before} -> {me.Hand.Count}, phase {tm.CurrentPhase}");

            // 2. Action demandée pendant l'animation de pioche : mise en attente, pas perdue.
            _input.SkipToDiscard();
            bool lockedAtCall = tm.CurrentPhase != TurnPhase.Discard;
            yield return new WaitForSecondsRealtime(1.0f);
            Check("action mise en attente pendant l'animation", tm.CurrentPhase == TurnPhase.Discard,
                $"verrou actif à l'appel : {lockedAtCall}, phase {tm.CurrentPhase}");

            // 3. Défausse : toucher la carte (soulevée), la retoucher (jetée).
            before = me.Hand.Count;
            yield return Tap(CardPos(0));
            bool liftedOnly = me.Hand.Count == before;
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Tap(CardPos(0));
            yield return new WaitForSecondsRealtime(0.3f);
            Check("défausse en deux touchers", liftedOnly && me.Hand.Count == before - 1,
                $"1er toucher sans effet : {liftedOnly}, main {before} -> {me.Hand.Count}");

            // 4. Depuis l'étape Poser : glisser une carte sur sa défausse finit le tour.
            yield return WaitMyDraw();
            yield return Tap(Center(_deck));
            yield return new WaitForSecondsRealtime(0.7f);
            before = me.Hand.Count;
            yield return Drag(CardPos(0), PilePos(0));
            yield return new WaitForSecondsRealtime(0.4f);
            Check("glisser sur la défausse depuis Poser", me.Hand.Count == before - 1,
                $"main {before} -> {me.Hand.Count}");

            // 5. Une carte sélectionnée puis la défausse touchée : carte jetée.
            yield return WaitMyDraw();
            yield return Tap(Center(_deck));
            yield return new WaitForSecondsRealtime(0.7f);
            before = me.Hand.Count;
            yield return Tap(CardPos(1));
            yield return new WaitForSecondsRealtime(0.25f);
            bool selected = _hand.SelectedCards.Count == 1 && me.Hand.Count == before;
            yield return Tap(PilePos(0));
            yield return new WaitForSecondsRealtime(0.4f);
            Check("carte sélectionnée puis défausse touchée", selected && me.Hand.Count == before - 1,
                $"sélection : {selected}, main {before} -> {me.Hand.Count}");

            // 6. Ranger sa main en glissant une carte (étape Piocher).
            yield return WaitMyDraw();
            int firstId = _hand.GetCardViewAt(0)!.CardModel.Id;
            Vector2 from = CardPos(0);
            Vector2 to = CardPos(3) + new Vector2(Screen.width * 0.02f, 0f);
            yield return Drag(from, new Vector2(to.x, from.y));
            yield return new WaitForSecondsRealtime(0.5f);
            Check("rangement de la main", _hand.GetCardViewAt(0)!.CardModel.Id != firstId
                                          && me.Hand[0].Id != firstId,
                $"1re carte {firstId} -> vue {_hand.GetCardViewAt(0)!.CardModel.Id}, modèle {me.Hand[0].Id}");

            // 7. Survol stable sur le bord bas d'une carte (pas d'oscillation).
            yield return new WaitForSecondsRealtime(0.5f);
            CardView hovered = _hand.GetCardViewAt(2)!;
            RectTransform hrt = hovered.RectTransform;
            Vector2 bottom = RectTransformUtility.WorldToScreenPoint(null,
                hrt.TransformPoint(new Vector3(hrt.rect.center.x - hrt.rect.width * 0.3f, hrt.rect.yMin + 5f, 0f)));
            int flips = 0;
            bool last = false;
            for (int f = 0; f < 90; f++)
            {
                _pointer = new PointerState(bottom, false, false, false);
                yield return null;
                if (hovered.IsHovered != last) { flips++; last = hovered.IsHovered; }
            }
            _pointer = null;
            Check("survol stable au bord bas d'une carte", flips <= 1 && last, $"{flips} bascule(s), survolée à la fin : {last}");
            yield return new WaitForSecondsRealtime(0.3f);

            // 8. Pose au relâchement + ajout à une combinaison (dès que la main le permet).
            bool laid = false;
            for (int turn = 0; turn < 30 && !laid; turn++)
            {
                yield return WaitMyDraw();
                if (_gm.State == GameState.GameOver) break;
                if (tm.CurrentPhase == TurnPhase.Draw) yield return Tap(Center(_deck));
                yield return new WaitForSecondsRealtime(0.7f);
                if (tm.CurrentPlayerIndex != 0 || _gm.State != GameState.PlayerTurn) continue;

                if (tm.CurrentPhase == TurnPhase.LayDown && !me.HasLaidDownThisRound
                    && LevelValidator.IsLevelComplete(me.Hand, me.CurrentLevel, _gm.Config, out _)
                    && _input.SelectSuggestedLayDown() && _tableRect != null)
                {
                    Vector2 spot = TableSpot();
                    _pointer = new PointerState(spot, true, true, false);
                    yield return null;
                    _pointer = new PointerState(spot, false, true, false);
                    yield return null;
                    yield return null;
                    bool notOnPress = !me.HasLaidDownThisRound;
                    _pointer = new PointerState(spot, false, false, true);
                    yield return null;
                    _pointer = null;
                    yield return new WaitForSecondsRealtime(0.6f);
                    // Plusieurs répartitions possibles : le jeu demande laquelle.
                    bool asked = LayDownChooser.IsOpen;
                    if (asked)
                    {
                        Click("Option0");
                        yield return new WaitForSecondsRealtime(0.6f);
                    }
                    laid = me.HasLaidDownThisRound;
                    Check("pose au relâchement sur la table", notOnPress && laid,
                        $"rien à l'appui : {notOnPress}, posé au relâchement : {laid}");
                    if (!laid) break;

                    yield return TryAddToMeld(me, tm);
                }

                // Fin de tour : glisser la carte la moins utile sur sa défausse.
                if (_gm.State == GameState.PlayerTurn && tm.CurrentPlayerIndex == 0 && me.Hand.Count > 0
                    && tm.CurrentPhase != TurnPhase.Draw)
                {
                    CardModel worst = TurnAutoPlay.ChooseDiscard(me.Hand);
                    int idx = IndexInView(worst.Id);
                    yield return Drag(CardPos(Mathf.Max(0, idx)), PilePos(0));
                    yield return new WaitForSecondsRealtime(0.4f);
                }
            }
            if (!laid) Line("IGNORE", "pose au relâchement sur la table", "contrat jamais réalisable en 30 tours");
        }

        private static bool _replayMode;

        /// <summary>Finit une manche, la revoit, et vérifie que l'état et la sauvegarde n'ont pas bougé.</summary>
        private IEnumerator RunReplay()
        {
            if (_gm?.TurnManager == null)
            {
                Line("ECHEC", "relecture", "partie introuvable");
                yield break;
            }

            // Le joueur joue « au plus simple » (commandes directes) jusqu'à la fin de la manche.
            float waited = 0f;
            while (_gm.State == GameState.PlayerTurn && waited < 150f)
            {
                if (_gm.TurnManager.CurrentPlayerIndex == 0)
                {
                    IGameCommand? next = TurnAutoPlay.NextCommand(_gm);
                    if (next != null) _gm.ExecuteCommand(next);
                }
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Check("relecture : manche terminée", _gm.State == GameState.EndRound, $"état {_gm.State} après {waited:0} s");
            if (_gm.State != GameState.EndRound) yield break;
            yield return new WaitForSecondsRealtime(1.0f);

            int hash = _gm.ComputeStateHash();
            int round = _gm.RoundNumber;
            string savePath = Path.Combine(Application.persistentDataPath, "partie-solo.sav");
            long saveLength = new FileInfo(savePath).Length;
            Check("relecture : bouton « Revoir » proposé", FindButton("BtnReplayRound") != null, "");
            yield return Capture("r1_recap");

            Click("BtnReplayRound");
            yield return new WaitForSecondsRealtime(3.0f);
            Check("relecture : la manche se rejoue", _gm.State == GameState.PlayerTurn && _gm.RoundNumber == round,
                $"état {_gm.State}, manche {_gm.RoundNumber}");
            // Pendant la relecture, une commande du joueur est refusée.
            CommandResult intruder = _gm.ExecuteCommand(new DrawFromDeckCommand(_gm.TurnManager.CurrentPlayerIndex));
            Check("relecture : commandes du joueur bloquées", intruder.Error == CommandError.Filtered, $"{intruder.Error}");
            yield return Capture("r2_relecture");

            Click("BtnReplaySkip");
            yield return new WaitForSecondsRealtime(2.5f);
            Check("relecture : retour au même état", _gm.State == GameState.EndRound && _gm.ComputeStateHash() == hash,
                $"état {_gm.State}, empreinte identique : {_gm.ComputeStateHash() == hash}");
            Check("relecture : sauvegarde inchangée", new FileInfo(savePath).Length == saveLength,
                $"{saveLength} -> {new FileInfo(savePath).Length} octets");
            Check("relecture : récap rouvert", FindButton("BtnContinue") != null, "");
        }

        private static bool _passMode;

        /// <summary>Deux humains sur l'appareil : la main reste cachée jusqu'au passage.</summary>
        private IEnumerator RunPassAndPlay()
        {
            if (_gm?.TurnManager == null || _hand == null || _piles == null || _deck == null)
            {
                Line("ECHEC", "pass-and-play", "objets de jeu introuvables");
                yield break;
            }
            TurnManager tm = _gm.TurnManager;
            bool twoHumans = _gm.Players.Count == 2 && !_gm.Players[0].IsAI && !_gm.Players[1].IsAI;
            Check("pass-and-play : deux humains, aucun bot", twoHumans, $"{_gm.Players.Count} joueurs");

            Check("pass-and-play : donne cachée derrière l'écran de passage",
                FindButton("BtnPassReady") != null && _hand.CardCount == 0, $"cartes visibles : {_hand.CardCount}");
            yield return Capture("p1_passage");

            Click("BtnPassReady");
            yield return new WaitForSecondsRealtime(2.2f);
            Check("pass-and-play : main du joueur 1 affichée",
                FindButton("BtnPassReady") == null && _hand.CardCount == _gm.Players[0].Hand.Count
                && GameSession.LocalPlayerIndex == 0, $"cartes : {_hand.CardCount}");

            yield return Tap(Center(_deck));
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Drag(CardPos(0), PilePos(0));
            yield return new WaitForSecondsRealtime(0.6f);
            Check("pass-and-play : tour passé, main cachée, écran de passage",
                tm.CurrentPlayerIndex == 1 && FindButton("BtnPassReady") != null && _hand.CardCount == 0,
                $"joueur courant {tm.CurrentPlayerIndex}, cartes visibles {_hand.CardCount}");

            Click("BtnPassReady");
            yield return new WaitForSecondsRealtime(2.2f);
            bool sameCards = _hand.CardCount == _gm.Players[1].Hand.Count;
            for (int i = 0; sameCards && i < _hand.CardCount; i++)
            {
                if (!_gm.Players[1].TryGetHandCard(_hand.GetCardViewAt(i)!.CardModel.Id, out _)) sameCards = false;
            }
            Check("pass-and-play : main du joueur 2 affichée", sameCards && GameSession.LocalPlayerIndex == 1,
                $"siège affiché {GameSession.LocalPlayerIndex}, cartes {_hand.CardCount}");
            yield return Capture("p2_joueur2");

            int before = _gm.Players[1].Hand.Count;
            yield return Tap(Center(_deck));
            yield return new WaitForSecondsRealtime(0.8f);
            Check("pass-and-play : le joueur 2 joue son tour", _gm.Players[1].Hand.Count == before + 1,
                $"main {before} -> {_gm.Players[1].Hand.Count}");
        }

        private static bool _tutorialMode;

        /// <summary>Joue le tutoriel de bout en bout avec de vrais gestes.</summary>
        private IEnumerator RunTutorial()
        {
            if (_gm?.TurnManager == null || _hand == null || _input == null || _piles == null || _deck == null
                || _table == null)
            {
                Line("ECHEC", "tutoriel", "objets de jeu introuvables");
                yield break;
            }
            TurnManager tm = _gm.TurnManager;
            PlayerModel me = _gm.Players[0];
            yield return Capture("t1_debut");

            // Geste non attendu : refusé par le scénario, rien ne bouge.
            CommandResult wrong = _gm.ExecuteCommand(new DrawFromDiscardCommand(0, 1));
            Check("tutoriel : geste hors scénario refusé", wrong.Error == CommandError.Filtered, $"{wrong.Error}");

            yield return Tap(Center(_deck));
            yield return new WaitForSecondsRealtime(0.9f);
            Check("tutoriel : pioche", me.Hand.Count == TutorialDirector.HandSize + 1, $"main {me.Hand.Count}");

            foreach (int value in new[] { 3, 4, 5, 9, 10, 11 })
            {
                yield return Tap(CardPos(IndexOfValue(value)));
                yield return new WaitForSecondsRealtime(0.15f);
            }
            yield return Capture("t2_selection");
            _input.OnLayDownButton();
            yield return new WaitForSecondsRealtime(0.8f);
            Check("tutoriel : pose du contrat", me.HasLaidDownThisRound && me.LaidMelds.Count == 2,
                $"posé : {me.HasLaidDownThisRound}");

            yield return DragOnMeldContaining(me, 6, 5);
            Check("tutoriel : 6 ajouté à la suite", me.Hand.Count == 2, $"main {me.Hand.Count}");

            _input.SkipToDiscard();
            yield return new WaitForSecondsRealtime(0.3f);
            int seventeen = IndexOfValue(17);
            yield return Tap(CardPos(seventeen));
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Tap(CardPos(IndexOfValue(17)));
            yield return new WaitForSecondsRealtime(0.4f);
            Check("tutoriel : défausse", me.Hand.Count == 1, $"main {me.Hand.Count}");

            // Tour scripté de l'adversaire, puis son 12 à ramasser.
            float waited = 0f;
            while (waited < 8f && !(tm.CurrentPlayerIndex == 0 && tm.CurrentPhase == TurnPhase.Draw))
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1.0f);
            CardModel? top = _gm.DeckManager!.PeekDiscard(1);
            Check("tutoriel : l'adversaire a jeté le 12", top.HasValue && top.Value.Value == 12, $"{top}");
            yield return Capture("t3_ramasser");

            yield return Tap(PilePos(1));
            yield return new WaitForSecondsRealtime(0.9f);
            Check("tutoriel : 12 ramassé dans la défausse", me.Hand.Count == 2, $"main {me.Hand.Count}");

            yield return DragOnMeldContaining(me, 12, 11);
            Check("tutoriel : 12 ajouté à la suite", me.Hand.Count == 1, $"main {me.Hand.Count}");

            _input.SkipToDiscard();
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Tap(CardPos(0));
            yield return new WaitForSecondsRealtime(0.3f);
            if (_hand.CardCount > 0) yield return Tap(CardPos(0));
            yield return new WaitForSecondsRealtime(1.2f);
            Check("tutoriel : sortie, manche gagnée, +2 niveaux",
                _gm.State == GameState.EndRound && me.Hand.Count == 0 && me.CurrentLevel == 3,
                $"état {_gm.State}, main {me.Hand.Count}, niveau {me.CurrentLevel}");
            yield return Capture("t4_fin");

            Click("BtnTutorialQuit");
            yield return new WaitForSecondsRealtime(1.2f);
            Check("tutoriel : retour au menu", _gm.State == GameState.Setup, $"état {_gm.State}");
        }

        private IEnumerator DragOnMeldContaining(PlayerModel me, int cardValue, int meldValue)
        {
            for (int m = 0; m < me.LaidMelds.Count; m++)
            {
                bool match = false;
                foreach (CardModel c in me.LaidMelds[m].Cards)
                {
                    if (c.Value == meldValue) match = true;
                }
                RectTransform? rect = match ? _table!.GetMeldRect(0, m) : null;
                if (rect == null) continue;
                yield return Drag(CardPos(IndexOfValue(cardValue)), Center(rect));
                yield return new WaitForSecondsRealtime(0.6f);
                yield break;
            }
        }

        private int IndexOfValue(int value)
        {
            for (int i = 0; i < _hand!.CardCount; i++)
            {
                CardView? cv = _hand.GetCardViewAt(i);
                if (cv != null && cv.CardModel.Value == value) return i;
            }
            return 0;
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(_dir, name + ".png"));
            yield return null;
            yield return null;
        }

        private IEnumerator TryAddToMeld(PlayerModel me, TurnManager tm)
        {
            if (_gm == null || _table == null || _hand == null) yield break;
            if (_gm.State != GameState.PlayerTurn || tm.CurrentPhase != TurnPhase.AddToMelds) yield break;

            foreach (CardModel card in new List<CardModel>(me.Hand))
            {
                foreach (PlayerModel owner in _gm.Players)
                {
                    for (int m = 0; m < owner.LaidMelds.Count; m++)
                    {
                        if (!owner.LaidMelds[m].CanAddCard(card)) continue;
                        RectTransform? rect = _table.GetMeldRect(owner.Index, m);
                        int idx = IndexInView(card.Id);
                        if (rect == null || idx < 0) continue;

                        int before = me.Hand.Count;
                        int meldBefore = owner.LaidMelds[m].Cards.Count;
                        yield return Drag(CardPos(idx), Center(rect));
                        yield return new WaitForSecondsRealtime(0.5f);
                        Check("glisser une carte sur une combinaison",
                            me.Hand.Count == before - 1 && owner.LaidMelds[m].Cards.Count == meldBefore + 1,
                            $"main {before} -> {me.Hand.Count}, combinaison {meldBefore} -> {owner.LaidMelds[m].Cards.Count}");
                        yield break;
                    }
                }
            }
            Line("IGNORE", "glisser une carte sur une combinaison", "aucune carte jouable après la pose");
        }

        // ────────────────────────────────────────────────────
        //  OUTILS
        // ────────────────────────────────────────────────────

        /// <summary>Attend le début du tour du joueur 0 (relance la manche si elle s'est terminée).</summary>
        private IEnumerator WaitMyDraw()
        {
            float waited = 0f;
            while (waited < 40f)
            {
                if (_gm == null || _gm.State == GameState.GameOver) yield break;
                if (_gm.State == GameState.EndRound)
                {
                    _gm.ExecuteCommand(new StartNextRoundCommand(0, _gm.RoundNumber));
                    yield return new WaitForSecondsRealtime(2.5f);
                }
                TurnManager? tm = _gm.TurnManager;
                if (tm != null && _gm.State == GameState.PlayerTurn && tm.CurrentPlayerIndex == 0
                    && _hand != null && !_hand.IsAnimatingDeal && !_hand.IsAnimatingDraw)
                {
                    yield return new WaitForSecondsRealtime(0.5f);
                    yield break;
                }
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Line("ECHEC", "attente du tour", "le tour du joueur n'est jamais revenu");
        }

        private IEnumerator Tap(Vector2 pos)
        {
            _pointer = new PointerState(pos, true, true, false);
            yield return null;
            _pointer = new PointerState(pos, false, true, false);
            yield return null;
            _pointer = new PointerState(pos, false, false, true);
            yield return null;
            _pointer = null;
            yield return null;
        }

        private IEnumerator Drag(Vector2 from, Vector2 to)
        {
            const int steps = 14;
            _pointer = new PointerState(from, true, true, false);
            yield return null;
            for (int i = 1; i <= steps; i++)
            {
                _pointer = new PointerState(Vector2.Lerp(from, to, i / (float)steps), false, true, false);
                yield return null;
            }
            _pointer = new PointerState(to, false, true, false);
            yield return null;
            _pointer = new PointerState(to, false, false, true);
            yield return null;
            _pointer = null;
            yield return null;
        }

        private static Vector2 Center(RectTransform rt) =>
            RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));

        /// <summary>Point de la carte visible dans l'éventail (bande gauche, non recouverte).</summary>
        private Vector2 CardPos(int index)
        {
            RectTransform rt = _hand!.GetCardViewAt(index)!.RectTransform;
            Rect r = rt.rect;
            return RectTransformUtility.WorldToScreenPoint(null,
                rt.TransformPoint(new Vector3(r.xMin + r.width * 0.22f, r.center.y, 0f)));
        }

        private Vector2 PilePos(int player) => Center(_piles!.GetPileRect(player)!);

        /// <summary>Un point de la table qui ne touche aucune combinaison (bas de la zone).</summary>
        private Vector2 TableSpot()
        {
            Rect r = _tableRect!.rect;
            return RectTransformUtility.WorldToScreenPoint(null,
                _tableRect.TransformPoint(new Vector3(r.xMin + r.width * 0.88f, r.yMin + r.height * 0.2f, 0f)));
        }

        private int IndexInView(int cardId)
        {
            for (int i = 0; i < _hand!.CardCount; i++)
            {
                CardView? cv = _hand.GetCardViewAt(i);
                if (cv != null && cv.CardModel.Id == cardId) return i;
            }
            return -1;
        }

        private void Check(string name, bool ok, string detail)
        {
            if (!ok) _failures++;
            Line(ok ? "OK" : "ECHEC", name, detail);
        }

        private void Line(string status, string name, string detail)
        {
            string line = $"{status,-6} {name} — {detail}";
            _report.AppendLine(line);
            Debug.Log("[DevInputTest] " + line);
        }

        private static Button? FindButton(string name)
        {
            foreach (Button b in FindObjectsOfType<Button>())
            {
                if (b.name == name && b.gameObject.activeInHierarchy) return b;
            }
            return null;
        }

        private static void Click(string name) => FindButton(name)?.onClick.Invoke();
    }
}
#endif
