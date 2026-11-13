// Outil de vérification visuelle : compilé seulement pour l'éditeur et le PC
// (jamais dans l'APK).
#if UNITY_EDITOR || UNITY_STANDALONE
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace LevelUp.Core
{
    /// <summary>
    /// Outil de vérification visuelle (aucun effet en jeu normal) : lancé avec
    /// <c>-autoshot &lt;dossier&gt;</c>, le jeu enchaîne menu → partie → pause →
    /// options en prenant une capture d'écran à chaque étape, puis se ferme.
    /// Sert à contrôler le rendu sur différents ratios d'écran sans appareil.
    /// </summary>
    public class DevAutoScreenshot : MonoBehaviour
    {
        private string _dir = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string? dir = GetArg("-autoshot");
            if (string.IsNullOrEmpty(dir)) return;

            // Sans focus, le lecteur PC se met en pause (Run In Background désactivé
            // dans le projet) : une capture lancée en arrière-plan se figeait.
            Application.runInBackground = true;

            GameObject go = new("DevAutoScreenshot");
            DontDestroyOnLoad(go);
            DevAutoScreenshot shot = go.AddComponent<DevAutoScreenshot>();
            shot._dir = dir!;

            // -lang 0|1 : force la langue (français / anglais), rétablie à la fin.
            string? lang = GetArg("-lang");
            if (lang != null && int.TryParse(lang, out int l))
            {
                shot._savedLang = Utils.Loc.Language;
                Utils.Loc.Language = l;
            }

            // -uiscale 0|1 : force la taille d'interface (rétablie à la fin).
            string? scale = GetArg("-uiscale");
            if (scale != null && int.TryParse(scale, out int s))
            {
                shot._savedUiScale = UI.GameSettings.UiScale;
                UI.GameSettings.UiScale = s;
            }
        }

        private int _savedUiScale = -1;
        private int _savedLang = -1;

        // OnlineSession.Leave() remet runInBackground à faux (comportement voulu
        // en jeu) : le harnais le réimpose, sinon une perte de focus le fige.
        private void Update() => Application.runInBackground = true;

        private void OnApplicationFocus(bool focus) =>
            Debug.Log($"[DevAutoScreenshot] focus={focus} t={Time.realtimeSinceStartup:0.0}");

        private IEnumerator Start()
        {
            Directory.CreateDirectory(_dir);
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Shot("01_menu");

            Click("BtnRulesMenu");
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("01c_rules");
            Click("BtnCloseRules");
            yield return new WaitForSecondsRealtime(0.6f);

            Click("BtnDaily");
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("01d_daily");
            Click("BtnDailyClose");
            yield return new WaitForSecondsRealtime(0.6f);

            // Fenêtre de pseudo (rien n'est enregistré : on annule)
            Click("ProfileChip");
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("01b_profile");
            Click("BtnCancel");
            yield return new WaitForSecondsRealtime(0.6f);

            Click("BtnOptions");
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Shot("02_menu_options");
            Click("BtnClose");
            yield return new WaitForSecondsRealtime(0.8f);

            // En ligne : accueil (pseudo) puis aperçu d'un salon (données factices, sans réseau)
            Click("BtnOnline");
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("02b_online_home");
            yield return ShowcaseLobby();

            // Nouvelle partie : jokers + actions et minuteur pour la capture, réglages
            // du joueur rétablis juste après le lancement.
            int savedSpecial = UI.GameSettings.SpecialCards;
            int savedTimer = UI.GameSettings.TurnTimer;
            UI.GameSettings.SpecialCards = 2;
            UI.GameSettings.TurnTimer = 30;
            Click("BtnPlay");
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Shot("02d_setup");
            Click("BtnLaunch");
            yield return new WaitForSecondsRealtime(0.7f);
            // Partie déjà sauvegardée : capture de la confirmation, puis on remplace.
            if (FindButton("BtnConfirmOk") != null)
            {
                yield return Shot("02e_confirm_replace");
                Click("BtnConfirmOk");
            }
            yield return new WaitForSecondsRealtime(0.3f);
            UI.GameSettings.SpecialCards = savedSpecial;
            UI.GameSettings.TurnTimer = savedTimer;

            yield return new WaitForSecondsRealtime(5.5f);
            yield return Shot("03_game");

            // Étape Poser (pioche jouée en commande directe) avec deux cartes
            // sélectionnées : panneau de sélection, bouton POSER, consigne.
            GameManager? gmPose = FindObjectOfType<GameManager>();
            UI.HandView? handPose = FindObjectOfType<UI.HandView>();
            if (gmPose?.TurnManager != null && handPose != null
                && gmPose.TurnManager.CurrentPhase == Utils.TurnPhase.Draw)
            {
                gmPose.ExecuteCommand(new DrawFromDeckCommand(gmPose.TurnManager.CurrentPlayerIndex));
                yield return new WaitForSecondsRealtime(0.8f);
                handPose.ToggleCardSelection(0);
                handPose.ToggleCardSelection(1);
                yield return new WaitForSecondsRealtime(0.6f);
                yield return Shot("03a_laydown");
                handPose.DeselectAll();
            }

            // Aperçu visuel (données factices, mode capture uniquement) : table
            // avec combinaisons, indices "jouable", panneau "Choisis ta pose".
            ShowcaseTable();
            yield return new WaitForSecondsRealtime(1.5f);
            // Les indices réels ne s'affichent qu'en phase "Compléter" : on les
            // force après le recalcul automatique pour les voir à l'écran.
            FindObjectOfType<UI.HandView>()?.SetPlayableHints(c => c.Value % 3 == 0);
            yield return null;
            yield return Shot("03b_table");
            yield return ShowcaseChooser();
            yield return ShowcaseTargets();

            // Récap de manche (aperçu sur l'état courant, aucun événement publié)
            UI.RoundSummaryView? summary = FindObjectOfType<UI.RoundSummaryView>();
            GameManager? gm = FindObjectOfType<GameManager>();
            if (summary != null && gm != null && gm.Players.Count > 1)
            {
                summary.ShowSnapshot(1, 2);
                yield return new WaitForSecondsRealtime(0.9f);
                yield return Shot("03e_round_summary");
            }

            Click("BtnPause");
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Shot("04_pause");

            Click("BtnOptions");
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Shot("05_options");

            // Fin de partie perdue (un bot gagne) : titre, classement
            UI.GameOverCelebration? over = FindObjectOfType<UI.GameOverCelebration>();
            if (over != null && gm != null && gm.Players.Count > 1)
            {
                foreach (UI.OptionsPanel options in FindObjectsOfType<UI.OptionsPanel>()) options.Hide();
                FindObjectOfType<UI.PauseMenuController>()?.Hide();
                yield return new WaitForSecondsRealtime(0.5f);
                over.Show(1);
                yield return new WaitForSecondsRealtime(1.4f);
                yield return Shot("06_game_over");
            }

            if (_savedUiScale >= 0) UI.GameSettings.UiScale = _savedUiScale;
            if (_savedLang >= 0) Utils.Loc.Language = _savedLang;
            UI.GameSettings.Flush();
            Application.Quit();
        }

        /// <summary>Salon en ligne factice : code, trois joueurs, une place libre, règles.</summary>
        private IEnumerator ShowcaseLobby()
        {
            UI.OnlinePanel? panel = FindObjectOfType<UI.OnlinePanel>();
            if (panel == null) yield break;

            panel.ShowHostLobby("K7Q2PX");
            UI.LobbyViewModel lobby = new()
            {
                Seats = 4,
                BotDifficulty = 2,
                Rules = new MatchRules(true, SpecialCardsMode.Jokers, 10, 6, 60)
            };
            lobby.Members.Add(new UI.LobbySeatView("Alex", true, true));
            lobby.Members.Add(new UI.LobbySeatView("Camille", false, false));
            lobby.Members.Add(new UI.LobbySeatView("Lucas", false, false));
            panel.UpdateLobby(lobby);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("02c_online_lobby");

            Click("BtnClose");
            yield return new WaitForSecondsRealtime(0.6f);
        }

        /// <summary>Choix de cible d'une carte « Passe » (aperçu sur les joueurs réels).</summary>
        private IEnumerator ShowcaseTargets()
        {
            Canvas? canvas = FindObjectOfType<Canvas>();
            GameManager? gm = FindObjectOfType<GameManager>();
            if (canvas == null || gm == null || gm.Players.Count < 3) yield break;

            UI.TargetChooser chooser = gameObject.AddComponent<UI.TargetChooser>();
            chooser.Setup(canvas.rootCanvas);
            chooser.Show(Utils.CardType.Skip, gm.Players, 0, _ => { });
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("03d_targets");
            chooser.Hide();
            yield return new WaitForSecondsRealtime(0.4f);
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            // -supersize N : capture N fois plus grande que la fenêtre (visuels de fiche Play Store).
            int super = int.TryParse(GetArg("-supersize"), out int s) && s > 1 ? Mathf.Min(s, 4) : 1;
            string path = Path.Combine(_dir, name + ".png");
            ScreenCapture.CaptureScreenshot(path, super);
            Debug.Log($"[DevAutoScreenshot] {path}");
            yield return new WaitForSecondsRealtime(0.3f);
        }

        private static int _fakeId = 100000;

        private static Utils.CardColor C(int i) => (Utils.CardColor)i;

        private static CardModel Card(int value, Utils.CardColor color) => new(_fakeId++, value, color);

        /// <summary>Affiche des combinaisons factices (vue seulement) et des indices sur la main.</summary>
        private static void ShowcaseTable()
        {
            Utils.EventBus.Publish(new LevelLaidDownEvent
            {
                PlayerIndex = 1,
                Level = 1,
                Melds = new System.Collections.Generic.List<System.Collections.Generic.List<CardModel>>
                {
                    new() { Card(4, C(1)), Card(5, C(0)), Card(6, C(3)) },
                    new() { Card(9, C(0)), Card(9, C(3)), Card(9, C(1)) }
                }
            });
            Utils.EventBus.Publish(new LevelLaidDownEvent
            {
                PlayerIndex = 2,
                Level = 2,
                Melds = new System.Collections.Generic.List<System.Collections.Generic.List<CardModel>>
                {
                    new() { Card(2, C(0)), Card(5, C(0)), Card(9, C(0)), Card(11, C(0)), Card(13, C(0)) },
                    new() { Card(10, C(5)), Card(11, C(4)), Card(12, C(1)), new CardModel(_fakeId++, Utils.CardType.Wild) }
                }
            });

            UI.HandView? hand = FindObjectOfType<UI.HandView>();
            hand?.SetPlayableHints(c => c.Value % 3 == 0);
        }

        /// <summary>Ouvre le panneau "Choisis ta pose" avec deux répartitions d'exemple.</summary>
        private IEnumerator ShowcaseChooser()
        {
            Canvas? canvas = FindObjectOfType<Canvas>();
            if (canvas == null) yield break;
            UI.LayDownChooser chooser = gameObject.AddComponent<UI.LayDownChooser>();
            chooser.Setup(canvas.rootCanvas);

            CardModel r7 = Card(7, C(0));
            CardModel b7 = Card(7, C(1)), g7 = Card(7, C(2)), y7 = Card(7, C(3));
            System.Collections.Generic.List<CardModel> reds = new()
            {
                Card(2, C(0)), Card(5, C(0)), Card(9, C(0)), Card(11, C(0))
            };
            System.Collections.Generic.List<Meld> a = new()
            {
                new Meld(Utils.MeldType.Set, new System.Collections.Generic.List<CardModel> { b7, g7, y7 }, 0),
                new Meld(Utils.MeldType.Flush, new System.Collections.Generic.List<CardModel>(reds) { r7 }, 0)
            };
            System.Collections.Generic.List<Meld> b = new()
            {
                new Meld(Utils.MeldType.Set, new System.Collections.Generic.List<CardModel> { r7, b7, g7 }, 0),
                new Meld(Utils.MeldType.Flush, new System.Collections.Generic.List<CardModel>(reds) { Card(13, C(0)) }, 0)
            };
            chooser.Show(new System.Collections.Generic.List<System.Collections.Generic.List<Meld>> { a, b },
                false, _ => { });
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("03c_chooser");
            chooser.Hide();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        /// <summary>Journalise chaque Graphic actif sous l'objet nommé (diagnostic de rendu).</summary>
        private static void DumpGraphics(string rootName)
        {
            GameObject? root = GameObject.Find(rootName);
            if (root == null)
            {
                Debug.Log($"[DevAutoScreenshot] dump: {rootName} introuvable");
                return;
            }
            foreach (Graphic g in root.GetComponentsInChildren<Graphic>(false))
            {
                string path = g.name;
                for (Transform t = g.transform.parent; t != null && t != root.transform.parent; t = t.parent)
                {
                    path = t.name + "/" + path;
                }
                string sprite = g is Image img && img.sprite != null ? img.sprite.name + " " + img.type : "-";
                Debug.Log($"[DevAutoScreenshot] dump: {path} enabled={g.enabled} color={g.color} " +
                          $"sprite={sprite} canvasAlpha={g.canvasRenderer.GetAlpha()}");
            }
        }

        /// <summary>Clique le dernier bouton actif portant ce nom (le plus au premier plan).</summary>
        private static void Click(string name)
        {
            Button? target = FindButton(name);
            if (target != null) target.onClick.Invoke();
            else Debug.LogWarning($"[DevAutoScreenshot] bouton introuvable : {name}");
        }

        private static Button? FindButton(string name)
        {
            Button? target = null;
            foreach (Button b in FindObjectsOfType<Button>())
            {
                if (b.name == name && b.isActiveAndEnabled) target = b;
            }
            return target;
        }

        private static string? GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }
    }
}
#endif
