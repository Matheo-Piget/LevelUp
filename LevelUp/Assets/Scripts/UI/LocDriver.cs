using System.Collections.Generic;
using UnityEngine;
using TMPro;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Applique la traduction aux textes de l'interface, juste avant le rendu.
    /// Le code du jeu écrit toujours en français ; ce composant remplace, dans
    /// chaque texte enregistré, le français par sa traduction (<see cref="Loc.T"/>)
    /// dès qu'il change. En français, il ne fait rien.
    ///
    /// Chaque texte garde sa source française : un changement de langue retraduit
    /// tout sur place, sans reconstruire les écrans.
    /// </summary>
    public sealed class LocDriver : MonoBehaviour
    {
        private sealed class Entry
        {
            public TMP_Text Text = null!;
            public string Source = "";   // dernier texte français écrit par le jeu
            public string Applied = "";  // ce que nous avons affiché à la place
        }

        private static readonly List<Entry> Entries = new();
        private static readonly HashSet<int> Known = new();
        private static LocDriver? _instance;
        private float _nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Entries.Clear();
            Known.Clear();
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (_instance != null) return;
            GameObject go = new("LocDriver");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<LocDriver>();
        }

        /// <summary>Enregistre un texte créé par l'interface (appelé par les fabriques de texte).</summary>
        public static void Register(TMP_Text? text)
        {
            if (text == null || !Known.Add(text.GetInstanceID())) return;
            Entries.Add(new Entry { Text = text });
        }

        private void OnEnable()
        {
            Canvas.willRenderCanvases += Apply;
            Loc.LanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= Apply;
            Loc.LanguageChanged -= OnLanguageChanged;
        }

        // Changement de langue : on repart des sources françaises.
        private static void OnLanguageChanged()
        {
            foreach (Entry e in Entries)
            {
                if (e.Text == null) continue;
                if (ReferenceEquals(e.Text.text, e.Applied) || e.Text.text == e.Applied) e.Text.text = e.Source;
                e.Applied = "";
            }
        }

        private void Update()
        {
            // Filet : textes posés en scène ou créés hors des fabriques.
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + 1f;
            foreach (TMP_Text t in FindObjectsOfType<TMP_Text>(true)) Register(t);
        }

        private static void Apply()
        {
            if (Loc.IsFrench) return;

            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                Entry e = Entries[i];
                if (e.Text == null)
                {
                    Entries.RemoveAt(i);
                    continue;
                }

                string current = e.Text.text;
                // Inchangé depuis notre dernière traduction : rien à faire.
                if (ReferenceEquals(current, e.Applied) || current == e.Applied) continue;

                e.Source = current;
                string translated = Loc.T(current);
                e.Applied = translated;
                if (!ReferenceEquals(translated, current)) e.Text.text = translated;
            }
        }
    }
}
