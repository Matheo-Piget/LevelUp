using System.Collections.Generic;
using System.Text;
using UnityEngine;
using LevelUp.Core;

namespace LevelUp.UI
{
    /// <summary>
    /// Succès : de petits objectifs calculés à partir de compteurs qui existent
    /// déjà (parties, manches, donne du jour, défis, tutoriel). Rien à
    /// enregistrer en plus : un succès est « obtenu » dès que son compteur
    /// atteint son seuil.
    /// </summary>
    public static class Achievements
    {
        public const string KeyTutorialDone = "levelup.tutorial.done";
        public const string KeyChallengesSolved = "levelup.challenge.solved";

        public readonly struct Entry
        {
            public readonly string Title;
            public readonly int Current;
            public readonly int Goal;

            public Entry(string title, int current, int goal)
            {
                Title = title;
                Current = current;
                Goal = goal;
            }

            public bool Done => Current >= Goal;
        }

        public static List<Entry> All()
        {
            int won = PlayerProfile.GamesWon;
            int rounds = PlayerProfile.RoundsWon;
            int solved = PlayerPrefs.GetInt(KeyChallengesSolved, 0);
            int streak = DailyDeal.Streak;
            return new List<Entry>
            {
                new("Premiers pas : finir le tutoriel", PlayerPrefs.GetInt(KeyTutorialDone, 0), 1),
                new("Première victoire", won, 1),
                new("Habitué : 3 victoires (nouveau tapis et dos de carte)", won, 3),
                new("Maître du salon : 10 victoires (tout est débloqué)", won, 10),
                new("Dix manches remportées", rounds, 10),
                new("Assidu : 3 donnes du jour d'affilée", streak, 3),
                new("Fidèle : 7 donnes du jour d'affilée", streak, 7),
                new("Casse-tête : 5 défis réussis", solved, 5),
                new("Stratège : 20 défis réussis", solved, 20),
            };
        }

        /// <summary>Liste prête à afficher : succès obtenus en clair, les autres avec leur progression.</summary>
        public static string Describe()
        {
            StringBuilder sb = new();
            int done = 0;
            List<Entry> all = All();
            foreach (Entry e in all)
            {
                if (e.Done) done++;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(e.Done ? "<color=#5FB98A>Obtenu</color>  " : $"<color=#A7A198>{Mathf.Min(e.Current, e.Goal)}/{e.Goal}</color>  ")
                  .Append(e.Done ? e.Title : $"<color=#A7A198>{e.Title}</color>");
            }
            return $"<b>SUCCÈS · {done}/{all.Count}</b>\n" + sb;
        }
    }
}
