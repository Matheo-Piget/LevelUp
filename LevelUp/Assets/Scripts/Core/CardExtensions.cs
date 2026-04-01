using System.Collections.Generic;
using System.Linq;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Méthodes d'extension utilitaires pour les collections de cartes.
    /// </summary>
    public static class CardExtensions
    {
        /// <summary>
        /// Trie les cartes par valeur puis par couleur.
        /// </summary>
        public static List<CardModel> SortByValue(this List<CardModel> cards)
        {
            return cards.OrderBy(c => c.Value).ThenBy(c => c.Color).ToList();
        }

        /// <summary>
        /// Trie les cartes par couleur puis par valeur.
        /// </summary>
        public static List<CardModel> SortByColor(this List<CardModel> cards)
        {
            return cards.OrderBy(c => c.Color).ThenBy(c => c.Value).ToList();
        }

        /// <summary>
        /// Retourne toutes les cartes d'une couleur donnée (hors Wilds).
        /// </summary>
        public static List<CardModel> OfColor(this List<CardModel> cards, CardColor color)
        {
            return cards.Where(c => c.Color == color && c.Type == CardType.Normal).ToList();
        }

        /// <summary>
        /// Retourne toutes les cartes d'une valeur donnée (hors Wilds).
        /// </summary>
        public static List<CardModel> OfValue(this List<CardModel> cards, int value)
        {
            return cards.Where(c => c.Value == value && c.Type == CardType.Normal).ToList();
        }

        /// <summary>
        /// Retourne toutes les cartes Wild (Wild et WildDraw2).
        /// </summary>
        public static List<CardModel> GetWilds(this List<CardModel> cards)
        {
            return cards.Where(c => c.Type == CardType.Wild || c.Type == CardType.WildDraw2).ToList();
        }

        /// <summary>
        /// Retourne toutes les cartes normales (pas d'action).
        /// </summary>
        public static List<CardModel> GetNormalCards(this List<CardModel> cards)
        {
            return cards.Where(c => c.Type == CardType.Normal).ToList();
        }

        /// <summary>
        /// Retourne toutes les cartes action (Skip, Draw2, Wild, WildDraw2).
        /// </summary>
        public static List<CardModel> GetActionCards(this List<CardModel> cards)
        {
            return cards.Where(c => c.Type != CardType.Normal).ToList();
        }

        /// <summary>
        /// Compte le nombre de cartes Wild disponibles.
        /// </summary>
        public static int WildCount(this List<CardModel> cards)
        {
            return cards.Count(c => c.Type == CardType.Wild || c.Type == CardType.WildDraw2);
        }

        /// <summary>
        /// Groupe les cartes normales par valeur.
        /// </summary>
        public static Dictionary<int, List<CardModel>> GroupByValue(this List<CardModel> cards)
        {
            return cards.Where(c => c.Type == CardType.Normal)
                        .GroupBy(c => c.Value)
                        .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Groupe les cartes normales par couleur.
        /// </summary>
        public static Dictionary<CardColor, List<CardModel>> GroupByColor(this List<CardModel> cards)
        {
            return cards.Where(c => c.Type == CardType.Normal)
                        .GroupBy(c => c.Color)
                        .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Vérifie si un ensemble de cartes forme une suite valide (consécutive).
        /// Les Wilds peuvent combler des trous.
        /// </summary>
        public static bool IsValidRun(this List<CardModel> cards)
        {
            if (cards.Count < 2) return false;

            List<CardModel> normals = cards.GetNormalCards().SortByValue();
            int wilds = cards.WildCount();

            if (normals.Count == 0) return wilds >= 2;

            int gaps = 0;
            for (int i = 1; i < normals.Count; i++)
            {
                int diff = normals[i].Value - normals[i - 1].Value;
                if (diff == 0) return false; // doublon
                if (diff > 1) gaps += diff - 1;
            }

            return gaps <= wilds;
        }

        /// <summary>
        /// Vérifie si un ensemble de cartes forme un set valide (même valeur).
        /// Les Wilds peuvent compléter.
        /// </summary>
        public static bool IsValidSet(this List<CardModel> cards)
        {
            if (cards.Count < 2) return false;

            List<CardModel> normals = cards.GetNormalCards();
            if (normals.Count == 0) return true; // que des wilds

            int targetValue = normals[0].Value;
            return normals.All(c => c.Value == targetValue);
        }

        /// <summary>
        /// Vérifie si un ensemble de cartes forme un flush valide (même couleur).
        /// Les Wilds comptent comme n'importe quelle couleur.
        /// </summary>
        public static bool IsValidFlush(this List<CardModel> cards)
        {
            if (cards.Count < 2) return false;

            List<CardModel> normals = cards.GetNormalCards();
            if (normals.Count == 0) return true; // que des wilds

            CardColor targetColor = normals[0].Color;
            return normals.All(c => c.Color == targetColor);
        }

        /// <summary>
        /// Trouve la plus longue suite possible dans une liste de cartes.
        /// </summary>
        public static List<CardModel> FindLongestRun(this List<CardModel> cards)
        {
            List<CardModel> normals = cards.GetNormalCards().SortByValue();
            int wilds = cards.WildCount();

            if (normals.Count == 0) return new List<CardModel>();

            List<CardModel> bestRun = new();
            List<CardModel> currentRun = new() { normals[0] };
            int wildsUsed = 0;

            for (int i = 1; i < normals.Count; i++)
            {
                int diff = normals[i].Value - normals[i - 1].Value;

                if (diff == 1)
                {
                    currentRun.Add(normals[i]);
                }
                else if (diff > 1 && diff - 1 <= wilds - wildsUsed)
                {
                    wildsUsed += diff - 1;
                    currentRun.Add(normals[i]);
                }
                else
                {
                    if (currentRun.Count > bestRun.Count) bestRun = new List<CardModel>(currentRun);
                    currentRun = new List<CardModel> { normals[i] };
                    wildsUsed = 0;
                }
            }

            if (currentRun.Count > bestRun.Count) bestRun = currentRun;
            return bestRun;
        }
    }
}
