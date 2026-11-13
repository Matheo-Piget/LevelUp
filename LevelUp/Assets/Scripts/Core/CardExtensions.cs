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
        public static bool IsValidRun(this IReadOnlyList<CardModel> cards, CardModel? extra = null)
        {
            int count = cards.Count + (extra.HasValue ? 1 : 0);
            if (count < 2) return false;

            // Sans allocation : masque des valeurs présentes (1..63), appelé pour
            // chaque carte × chaque combinaison par les aides, l'aimant et les bots.
            ulong seen = 0;
            int normals = 0, wilds = 0, min = int.MaxValue, max = int.MinValue;
            for (int i = 0; i < count; i++)
            {
                CardModel c = i < cards.Count ? cards[i] : extra!.Value;
                if (c.IsWild) { wilds++; continue; }
                // Une carte « Passe » / « +2 » n'entre dans aucune combinaison.
                if (c.Type != CardType.Normal || c.Value < 1 || c.Value > 63) return false;
                ulong bit = 1UL << c.Value;
                if ((seen & bit) != 0) return false; // doublon
                seen |= bit;
                normals++;
                if (c.Value < min) min = c.Value;
                if (c.Value > max) max = c.Value;
            }

            if (normals == 0) return wilds >= 2;
            int gaps = (max - min + 1) - normals;
            return gaps <= wilds;
        }

        /// <summary>
        /// Vérifie si un ensemble de cartes forme un set valide (même valeur).
        /// Les Wilds peuvent compléter.
        /// </summary>
        public static bool IsValidSet(this IReadOnlyList<CardModel> cards, CardModel? extra = null)
        {
            int count = cards.Count + (extra.HasValue ? 1 : 0);
            if (count < 2) return false;

            int target = int.MinValue;
            for (int i = 0; i < count; i++)
            {
                CardModel c = i < cards.Count ? cards[i] : extra!.Value;
                if (c.IsWild) continue;
                if (c.Type != CardType.Normal) return false;
                if (target == int.MinValue) target = c.Value;
                else if (c.Value != target) return false;
            }
            return true;
        }

        /// <summary>
        /// Vérifie si un ensemble de cartes forme un flush valide (même couleur).
        /// Les Wilds comptent comme n'importe quelle couleur.
        /// </summary>
        public static bool IsValidFlush(this IReadOnlyList<CardModel> cards, CardModel? extra = null)
        {
            int count = cards.Count + (extra.HasValue ? 1 : 0);
            if (count < 2) return false;

            bool hasColor = false;
            CardColor target = default;
            for (int i = 0; i < count; i++)
            {
                CardModel c = i < cards.Count ? cards[i] : extra!.Value;
                if (c.IsWild) continue;
                if (c.Type != CardType.Normal) return false;
                if (!hasColor) { target = c.Color; hasColor = true; }
                else if (c.Color != target) return false;
            }
            return true;
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
