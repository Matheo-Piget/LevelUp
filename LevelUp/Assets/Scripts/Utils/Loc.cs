using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LevelUp.Utils
{
    /// <summary>
    /// Traduction de l'interface. Le jeu est écrit en français ; les autres
    /// langues passent par une table « texte français → traduction ».
    ///
    /// Un texte affiché est traduit d'abord en entier (<see cref="Exact"/>),
    /// sinon morceau par morceau (<see cref="Fragments"/>) : on parcourt la
    /// chaîne de gauche à droite en prenant à chaque position le plus long
    /// fragment connu. Les textes composés à l'exécution (« Margot pose le
    /// niveau 3 ! ») se traduisent ainsi sans toucher au code qui les fabrique.
    /// Un fragment qui commence ou finit par une lettre ne s'applique qu'à un
    /// mot entier (« posé » ne touche pas « posée »).
    ///
    /// Ajouter un texte au jeu : l'écrire en français dans le code, puis ajouter
    /// sa traduction dans <see cref="LocTable"/>. Un texte absent reste en français.
    /// </summary>
    public static class Loc
    {
        public const int French = 0;
        public const int English = 1;

        private const string Key = "levelup.lang";
        private const int MaxCache = 3000;

        private static int _language = -1;
        private static readonly Dictionary<string, string> Cache = new();
        private static List<KeyValuePair<string, string>>? _sorted;

        /// <summary>La langue a changé : les textes déjà affichés doivent être retraduits.</summary>
        public static event Action? LanguageChanged;

        /// <summary>Langue courante (français par défaut si l'appareil est en français, sinon anglais).</summary>
        public static int Language
        {
            get
            {
                if (_language < 0)
                {
                    int fallback = Application.systemLanguage == SystemLanguage.French ? French : English;
                    _language = Mathf.Clamp(PlayerPrefs.GetInt(Key, fallback), French, English);
                }
                return _language;
            }
            set
            {
                int v = Mathf.Clamp(value, French, English);
                if (v == Language) return;
                _language = v;
                PlayerPrefs.SetInt(Key, v);
                Cache.Clear();
                LanguageChanged?.Invoke();
            }
        }

        public static bool IsFrench => Language == French;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _language = -1;
            Cache.Clear();
            LanguageChanged = null;
        }

        /// <summary>Texte dans la langue courante (le français est rendu tel quel).</summary>
        public static string T(string? french)
        {
            if (string.IsNullOrEmpty(french)) return french ?? "";
            if (Language == French) return french!;
            if (Cache.TryGetValue(french!, out string? cached)) return cached;

            string result = LocTable.Exact.TryGetValue(french!, out string? exact) ? exact : TranslateFragments(french!);
            if (Cache.Count >= MaxCache) Cache.Clear();
            Cache[french!] = result;
            return result;
        }

        private static string TranslateFragments(string s)
        {
            _sorted ??= BuildSorted();
            StringBuilder sb = new(s.Length + 16);
            int i = 0;
            while (i < s.Length)
            {
                bool matched = false;
                char c = s[i];
                foreach (KeyValuePair<string, string> f in _sorted)
                {
                    string fr = f.Key;
                    if (fr[0] != c || fr.Length > s.Length - i) continue;
                    if (string.CompareOrdinal(s, i, fr, 0, fr.Length) != 0) continue;
                    // Mot entier seulement quand le fragment commence / finit par une lettre.
                    if (char.IsLetter(fr[0]) && i > 0 && char.IsLetter(s[i - 1])) continue;
                    int end = i + fr.Length;
                    if (char.IsLetter(fr[fr.Length - 1]) && end < s.Length && char.IsLetter(s[end])) continue;

                    sb.Append(f.Value);
                    i = end;
                    matched = true;
                    break;
                }
                if (!matched)
                {
                    sb.Append(c);
                    i++;
                }
            }
            return sb.ToString();
        }

        // Les plus longs d'abord : « Suite automatique dans » avant « Suite ».
        private static List<KeyValuePair<string, string>> BuildSorted()
        {
            List<KeyValuePair<string, string>> list = new(LocTable.Fragments.Count + LocTable.Exact.Count);
            foreach (KeyValuePair<string, string> f in LocTable.Fragments) list.Add(f);
            // Les textes entiers servent aussi de fragments (ils apparaissent parfois dans un texte composé).
            foreach (KeyValuePair<string, string> e in LocTable.Exact)
            {
                if (e.Key.Length >= 12 && !LocTable.Fragments.ContainsKey(e.Key)) list.Add(e);
            }
            list.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));
            return list;
        }
    }
}
