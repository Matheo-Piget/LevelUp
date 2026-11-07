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

            GameObject go = new("DevAutoScreenshot");
            DontDestroyOnLoad(go);
            DevAutoScreenshot shot = go.AddComponent<DevAutoScreenshot>();
            shot._dir = dir!;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(_dir);
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Shot("01_menu");

            Click("BtnOptions");
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Shot("02_menu_options");
            Click("BtnClose");
            yield return new WaitForSecondsRealtime(0.8f);

            Click("BtnPlay");
            yield return new WaitForSecondsRealtime(6f);
            yield return Shot("03_game");

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

            Click("BtnPause");
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Shot("04_pause");

            Click("BtnOptions");
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Shot("05_options");

            Application.Quit();
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(_dir, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
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
                new Meld(Utils.MeldType.Set, new() { b7, g7, y7 }, 0),
                new Meld(Utils.MeldType.Flush, new(reds) { r7 }, 0)
            };
            System.Collections.Generic.List<Meld> b = new()
            {
                new Meld(Utils.MeldType.Set, new() { r7, b7, g7 }, 0),
                new Meld(Utils.MeldType.Flush, new(reds) { Card(13, C(0)) }, 0)
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
            Button? target = null;
            foreach (Button b in FindObjectsOfType<Button>())
            {
                if (b.name == name && b.isActiveAndEnabled) target = b;
            }
            if (target != null) target.onClick.Invoke();
            else Debug.LogWarning($"[DevAutoScreenshot] bouton introuvable : {name}");
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
