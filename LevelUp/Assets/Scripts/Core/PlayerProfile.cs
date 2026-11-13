using System;
using System.Text;
using UnityEngine;

namespace LevelUp.Core
{
    /// <summary>
    /// Profil du joueur de cet appareil : pseudo (affiché à la table et envoyé à
    /// l'hôte en ligne) et statistiques de parties. Persistant via PlayerPrefs.
    /// Dans le Core (et non l'UI) : le réseau et le bootstrapper le lisent aussi.
    /// </summary>
    public static class PlayerProfile
    {
        /// <summary>Longueur maximum d'un pseudo (tient dans les pastilles joueurs).</summary>
        public const int MaxNameLength = 14;

        /// <summary>Pseudo par défaut tant que le joueur n'en a pas choisi.</summary>
        public const string DefaultName = "Joueur";

        private const string KeyName = "levelup.profile.name";
        private const string KeyPlayed = "levelup.stats.played";
        private const string KeyWon = "levelup.stats.won";
        private const string KeyRounds = "levelup.stats.rounds";

        private static string? _name;

        /// <summary>Déclenché quand le pseudo ou les statistiques changent.</summary>
        public static event Action? Changed;

        /// <summary>Pseudo du joueur (toujours non vide, déjà nettoyé).</summary>
        public static string Name
        {
            get
            {
                if (_name == null)
                {
                    string stored = Sanitize(PlayerPrefs.GetString(KeyName, ""));
                    _name = stored.Length > 0 ? stored : DefaultName;
                }
                return _name;
            }
            set
            {
                string clean = Sanitize(value);
                if (clean.Length == 0) clean = DefaultName;
                if (clean == _name) return;
                _name = clean;
                PlayerPrefs.SetString(KeyName, clean);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        /// <summary>Vrai si le joueur a déjà choisi un pseudo (sinon : pseudo par défaut).</summary>
        public static bool HasCustomName => PlayerPrefs.HasKey(KeyName) && Name != DefaultName;

        /// <summary>Parties terminées sur cet appareil.</summary>
        public static int GamesPlayed => PlayerPrefs.GetInt(KeyPlayed, 0);

        /// <summary>Parties gagnées sur cet appareil.</summary>
        public static int GamesWon => PlayerPrefs.GetInt(KeyWon, 0);

        /// <summary>Manches remportées sur cet appareil.</summary>
        public static int RoundsWon => PlayerPrefs.GetInt(KeyRounds, 0);

        /// <summary>Enregistre une partie terminée (gagnée ou non).</summary>
        public static void RecordGame(bool won)
        {
            PlayerPrefs.SetInt(KeyPlayed, GamesPlayed + 1);
            if (won) PlayerPrefs.SetInt(KeyWon, GamesWon + 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Enregistre une manche remportée.</summary>
        public static void RecordRoundWon()
        {
            PlayerPrefs.SetInt(KeyRounds, RoundsWon + 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>
        /// Nettoie un pseudo saisi : retire les caractères de contrôle et ceux qui
        /// casseraient le protocole réseau ('|') ou le rich text TMP ('&lt;', '&gt;'),
        /// fusionne les espaces, coupe à <see cref="MaxNameLength"/> caractères.
        /// Peut retourner une chaîne vide (l'appelant choisit le repli).
        /// </summary>
        public static string Sanitize(string? raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";

            StringBuilder sb = new(raw!.Length);
            bool lastWasSpace = true; // supprime les espaces de tête
            foreach (char c in raw)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!lastWasSpace) sb.Append(' ');
                    lastWasSpace = true;
                    continue;
                }
                if (char.IsControl(c) || char.IsSurrogate(c)) continue;
                if (c == '|' || c == '<' || c == '>' || c == '\\') continue;
                sb.Append(c);
                lastWasSpace = false;
                if (sb.Length >= MaxNameLength) break;
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Initiale d'avatar : chiffre final pour "Bot 2" (→ "2"), sinon la première
        /// lettre en majuscule.
        /// </summary>
        public static string Initial(string? name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            char last = name![name.Length - 1];
            if (char.IsDigit(last) && name.StartsWith("Bot")) return last.ToString();
            return char.ToUpperInvariant(name[0]).ToString();
        }

        /// <summary>Oublie le cache (tests / changement de PlayerPrefs externe).</summary>
        internal static void ResetCache() => _name = null;
    }
}
