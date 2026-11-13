using System;
using System.Collections.Generic;
using UnityEngine;
using LevelUp.Core;

namespace LevelUp.UI
{
    /// <summary>
    /// Donne du jour : la même partie pour tout le monde ce jour-là (même
    /// mélange, mêmes bots), courte (4 niveaux, règles classiques). Le premier
    /// résultat du jour est retenu, avec la série de jours joués d'affilée.
    /// </summary>
    public static class DailyDeal
    {
        public const int Bots = 3;
        public const int Difficulty = 1;
        private const string KeyPrefix = "levelup.daily.";
        private const string KeyStreak = "levelup.daily.streak";
        private const string KeyLastDay = "levelup.daily.last";

        /// <summary>Jour courant au format aaaammjj (heure locale).</summary>
        public static int TodayKey
        {
            get
            {
                DateTime d = DateTime.Now;
                return d.Year * 10000 + d.Month * 100 + d.Day;
            }
        }

        /// <summary>Règles de la donne : classiques, 4 niveaux (une quinzaine de minutes).</summary>
        public static MatchRules Rules => new(false, SpecialCardsMode.None, MatchRules.DefaultHandSize, 4, 0);

        public static int SeedFor(int dayKey) => unchecked(dayKey * 7919 + 1013);

        public static List<PlayerSetup> Seats()
        {
            List<PlayerSetup> seats = new() { new PlayerSetup(PlayerProfile.Name, false) };
            for (int i = 1; i <= Bots; i++) seats.Add(new PlayerSetup($"Bot {i}", true));
            return seats;
        }

        /// <summary>Résultat retenu pour ce jour : 0 = pas joué, &gt;0 gagnée en N manches, &lt;0 perdue en N manches.</summary>
        public static int ResultFor(int dayKey) => PlayerPrefs.GetInt(KeyPrefix + dayKey, 0);

        /// <summary>Enregistre le premier résultat du jour (les parties suivantes ne comptent pas).</summary>
        public static void Record(int dayKey, bool won, int rounds)
        {
            if (ResultFor(dayKey) != 0) return;
            PlayerPrefs.SetInt(KeyPrefix + dayKey, won ? Math.Max(1, rounds) : -Math.Max(1, rounds));

            int last = PlayerPrefs.GetInt(KeyLastDay, 0);
            int streak = PlayerPrefs.GetInt(KeyStreak, 0);
            streak = last == PreviousDay(dayKey) ? streak + 1 : 1;
            PlayerPrefs.SetInt(KeyStreak, streak);
            PlayerPrefs.SetInt(KeyLastDay, dayKey);
            PlayerPrefs.Save();
        }

        /// <summary>Texte d'état pour le bouton du menu.</summary>
        public static string Status()
        {
            int today = TodayKey;
            int result = ResultFor(today);
            int streak = PlayerPrefs.GetInt(KeyStreak, 0);
            bool streakAlive = PlayerPrefs.GetInt(KeyLastDay, 0) is int last
                               && (last == today || last == PreviousDay(today));
            string streakText = streakAlive && streak > 1 ? $" · série {streak} j" : "";
            if (result == 0) return "à jouer" + streakText;
            int rounds = Math.Abs(result);
            return (result > 0 ? $"gagnée en {rounds} manche{(rounds > 1 ? "s" : "")}" : "perdue") + streakText;
        }

        /// <summary>Série de jours joués d'affilée encore en cours (0 si elle est rompue).</summary>
        public static int Streak
        {
            get
            {
                int today = TodayKey;
                int last = PlayerPrefs.GetInt(KeyLastDay, 0);
                return last == today || last == PreviousDay(today) ? PlayerPrefs.GetInt(KeyStreak, 0) : 0;
            }
        }

        /// <summary>Le jour situé <paramref name="daysBack"/> jours avant <paramref name="dayKey"/>.</summary>
        public static int DayBefore(int dayKey, int daysBack)
        {
            DateTime d = new DateTime(dayKey / 10000, dayKey / 100 % 100, dayKey % 100).AddDays(-daysBack);
            return d.Year * 10000 + d.Month * 100 + d.Day;
        }

        /// <summary>"13/11" pour 20261113.</summary>
        public static string ShortDate(int dayKey) => $"{dayKey % 100:00}/{dayKey / 100 % 100:00}";

        /// <summary>
        /// Résultat à partager (presse-papiers) : la donne est la même pour tous,
        /// on compare donc directement entre amis.
        /// </summary>
        public static string ShareText(int dayKey)
        {
            int result = ResultFor(dayKey);
            if (result == 0) return "";
            int rounds = Math.Abs(result);
            string outcome = result > 0
                ? $"gagnée en {rounds} manche{(rounds > 1 ? "s" : "")}"
                : $"perdue en {rounds} manche{(rounds > 1 ? "s" : "")}";
            int streak = Streak;
            return $"LevelUp · Donne du {ShortDate(dayKey)} : {outcome}"
                   + (streak > 1 ? $" · série de {streak} jours" : "")
                   + "\nMême donne pour tout le monde aujourd'hui : fais mieux !";
        }

        private static int PreviousDay(int dayKey)
        {
            DateTime d = new(dayKey / 10000, dayKey / 100 % 100, dayKey % 100);
            DateTime p = d.AddDays(-1);
            return p.Year * 10000 + p.Month * 100 + p.Day;
        }
    }
}
