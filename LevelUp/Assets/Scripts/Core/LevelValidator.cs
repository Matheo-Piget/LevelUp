using System.Collections.Generic;
using System.Linq;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Définit les exigences de chaque niveau et valide si une main les satisfait.
    /// </summary>
    public static class LevelValidator
    {
        /// <summary>
        /// Représente une exigence de combinaison pour un niveau.
        /// </summary>
        public readonly struct LevelRequirement
        {
            public readonly MeldType Type;
            public readonly int CardCount;

            public LevelRequirement(MeldType type, int cardCount)
            {
                Type = type;
                CardCount = cardCount;
            }
        }

        /// <summary>
        /// Retourne les exigences pour un niveau donné (1-8).
        /// Configurable via GameConfig, sinon utilise les valeurs par défaut.
        /// </summary>
        public static List<List<LevelRequirement>> GetRequirements(int level, GameConfig? config = null)
        {
            if (config != null && config.LevelDefinitions.Count >= level)
            {
                return config.LevelDefinitions[level - 1].ToRequirements();
            }

            return GetDefaultRequirements(level);
        }

        /// <summary>
        /// Exigences par défaut pour les 8 niveaux.
        /// </summary>
        private static List<List<LevelRequirement>> GetDefaultRequirements(int level)
        {
            return level switch
            {
                // 1 → 2 suites de 3
                1 => new List<List<LevelRequirement>>
                {
                    new() { new LevelRequirement(MeldType.Run, 3), new LevelRequirement(MeldType.Run, 3) }
                },
                // 2 → 1 suite de 3 + 1 brelan
                2 => new List<List<LevelRequirement>>
                {
                    new() { new LevelRequirement(MeldType.Run, 3), new LevelRequirement(MeldType.Set, 3) }
                },
                // 3 → 2 brelans
                3 => new List<List<LevelRequirement>>
                {
                    new() { new LevelRequirement(MeldType.Set, 3), new LevelRequirement(MeldType.Set, 3) }
                },
                // 4 → 1 suite de 4 + 1 paire
                4 => new List<List<LevelRequirement>>
                {
                    new() { new LevelRequirement(MeldType.Run, 4), new LevelRequirement(MeldType.Set, 2) }
                },
                // 5 → 1 flush de 5
                5 => new List<List<LevelRequirement>>
                {
                    new() { new LevelRequirement(MeldType.Flush, 5) }
                },
                // 6 → 1 suite de 5
                6 => new List<List<LevelRequirement>>
                {
                    new() { new LevelRequirement(MeldType.Run, 5) }
                },
                // 7 → 1 carré + 1 paire
                7 => new List<List<LevelRequirement>>
                {
                    new() { new LevelRequirement(MeldType.Set, 4), new LevelRequirement(MeldType.Set, 2) }
                },
                // 8 → 1 flush de 7
                8 => new List<List<LevelRequirement>>
                {
                    new() { new LevelRequirement(MeldType.Flush, 7) }
                },
                _ => new List<List<LevelRequirement>>()
            };
        }

        /// <summary>
        /// Vérifie si une main contient les cartes nécessaires pour compléter un niveau.
        /// Retourne true et la liste des combinaisons trouvées si le niveau est validé.
        /// </summary>
        public static bool IsLevelComplete(List<CardModel> hand, int level, GameConfig? config,
            out List<Meld> foundMelds)
        {
            foundMelds = new List<Meld>();
            List<List<LevelRequirement>> requirementSets = GetRequirements(level, config);

            if (requirementSets.Count == 0) return false;

            // Essayer chaque set d'exigences (normalement un seul)
            foreach (List<LevelRequirement> requirements in requirementSets)
            {
                List<Meld> melds = new();
                if (TryFindMelds(new List<CardModel>(hand), requirements, 0, melds))
                {
                    foundMelds = melds;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Essaie récursivement de trouver des combinaisons satisfaisant les exigences.
        /// Utilise le backtracking pour explorer toutes les possibilités.
        /// </summary>
        private static bool TryFindMelds(List<CardModel> availableCards,
            List<LevelRequirement> requirements, int reqIndex, List<Meld> foundMelds)
        {
            if (reqIndex >= requirements.Count) return true;

            LevelRequirement req = requirements[reqIndex];

            List<List<CardModel>> candidates = FindCandidates(availableCards, req);

            foreach (List<CardModel> candidate in candidates)
            {
                // Retirer les cartes utilisées
                List<CardModel> remaining = new(availableCards);
                foreach (CardModel card in candidate)
                {
                    remaining.Remove(card);
                }

                Meld meld = new(req.Type, candidate, -1);
                foundMelds.Add(meld);

                if (TryFindMelds(remaining, requirements, reqIndex + 1, foundMelds))
                {
                    return true;
                }

                foundMelds.RemoveAt(foundMelds.Count - 1);
            }

            return false;
        }

        /// <summary>
        /// Trouve toutes les combinaisons candidates pour une exigence donnée.
        /// </summary>
        private static List<List<CardModel>> FindCandidates(List<CardModel> cards, LevelRequirement req)
        {
            List<List<CardModel>> results = new();

            switch (req.Type)
            {
                case MeldType.Run:
                    FindRuns(cards, req.CardCount, results);
                    break;
                case MeldType.Set:
                    FindSets(cards, req.CardCount, results);
                    break;
                case MeldType.Flush:
                    FindFlushes(cards, req.CardCount, results);
                    break;
            }

            return results;
        }

        /// <summary>
        /// Trouve toutes les suites possibles de longueur donnée.
        /// </summary>
        private static void FindRuns(List<CardModel> cards, int length, List<List<CardModel>> results)
        {
            List<CardModel> normals = cards.Where(c => c.Type == CardType.Normal)
                                           .OrderBy(c => c.Value).ToList();
            List<CardModel> wilds = cards.GetWilds();

            // Essayer chaque point de départ possible
            HashSet<int> startValues = normals.Select(c => c.Value).Distinct().ToHashSet();

            foreach (int startVal in startValues)
            {
                for (int endVal = startVal + length - 1; endVal >= startVal; endVal--)
                {
                    List<CardModel> run = new();
                    int wildsUsed = 0;
                    bool valid = true;

                    for (int v = startVal; v < startVal + length; v++)
                    {
                        CardModel? card = normals.FirstOrDefault(c =>
                            c.Value == v && !run.Contains(c));

                        if (card.HasValue && card.Value.Value != 0)
                        {
                            run.Add(card.Value);
                        }
                        else if (wildsUsed < wilds.Count)
                        {
                            run.Add(wilds[wildsUsed]);
                            wildsUsed++;
                        }
                        else
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (valid && run.Count == length)
                    {
                        // Vérifier pas de doublon dans les résultats
                        if (!results.Any(r => r.SequenceEqual(run)))
                        {
                            results.Add(run);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Trouve tous les sets (même valeur) possibles de taille donnée.
        /// </summary>
        private static void FindSets(List<CardModel> cards, int size, List<List<CardModel>> results)
        {
            List<CardModel> wilds = cards.GetWilds();
            Dictionary<int, List<CardModel>> groups = cards.GroupByValue();

            foreach (KeyValuePair<int, List<CardModel>> group in groups)
            {
                int normalsAvailable = group.Value.Count;
                int wildsNeeded = size - normalsAvailable;

                if (wildsNeeded <= wilds.Count && wildsNeeded >= 0)
                {
                    List<CardModel> set = group.Value.Take(
                        System.Math.Min(normalsAvailable, size)).ToList();

                    for (int w = 0; w < wildsNeeded; w++)
                    {
                        set.Add(wilds[w]);
                    }

                    if (set.Count == size)
                    {
                        results.Add(set);
                    }
                }
            }
        }

        /// <summary>
        /// Trouve tous les flushes (même couleur) possibles de taille donnée.
        /// </summary>
        private static void FindFlushes(List<CardModel> cards, int size, List<List<CardModel>> results)
        {
            List<CardModel> wilds = cards.GetWilds();
            Dictionary<CardColor, List<CardModel>> groups = cards.GroupByColor();

            foreach (KeyValuePair<CardColor, List<CardModel>> group in groups)
            {
                int normalsAvailable = group.Value.Count;
                int wildsNeeded = size - normalsAvailable;

                if (wildsNeeded <= wilds.Count && wildsNeeded >= 0)
                {
                    List<CardModel> flush = group.Value.Take(
                        System.Math.Min(normalsAvailable, size)).ToList();

                    for (int w = 0; w < wildsNeeded; w++)
                    {
                        flush.Add(wilds[w]);
                    }

                    if (flush.Count == size)
                    {
                        results.Add(flush);
                    }
                }
            }
        }
    }
}
