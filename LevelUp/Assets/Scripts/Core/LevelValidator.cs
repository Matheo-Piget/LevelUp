using System.Collections.Generic;
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
        /// Accepte IReadOnlyList pour compatibilité avec PlayerModel.Hand.
        /// </summary>
        public static bool IsLevelComplete(IReadOnlyList<CardModel> hand, int level, GameConfig? config,
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
        /// Vérifie qu'un ensemble de melds proposés satisfait les exigences d'un niveau.
        /// Utilisé par <see cref="GameCommandExecutor"/> pour valider une commande LayDown
        /// sans faire confiance au caller (UI ou IA) sur la structure exigée.
        /// L'ordre des melds proposés n'a pas d'importance : on cherche un appariement
        /// entre les melds et les exigences (type + nombre de cartes).
        /// </summary>
        public static bool MeldsSatisfyLevel(List<Meld> proposedMelds, int level, GameConfig? config)
        {
            if (proposedMelds == null || proposedMelds.Count == 0) return false;

            List<List<LevelRequirement>> requirementSets = GetRequirements(level, config);
            if (requirementSets.Count == 0) return false;

            foreach (List<LevelRequirement> requirements in requirementSets)
            {
                if (proposedMelds.Count != requirements.Count) continue;
                if (TryMatchRequirements(proposedMelds, requirements, new bool[requirements.Count]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Backtracking simple : pour chaque meld proposé, tente de l'apparier avec
        /// une exigence non encore consommée. Match si Type identique et taille identique.
        /// </summary>
        private static bool TryMatchRequirements(List<Meld> proposedMelds,
            List<LevelRequirement> requirements, bool[] consumed)
        {
            return Match(0);

            bool Match(int meldIdx)
            {
                if (meldIdx >= proposedMelds.Count) return true;
                Meld meld = proposedMelds[meldIdx];

                for (int r = 0; r < requirements.Count; r++)
                {
                    if (consumed[r]) continue;
                    LevelRequirement req = requirements[r];
                    if (req.Type != meld.Type) continue;
                    if (req.CardCount != meld.Cards.Count) continue;

                    consumed[r] = true;
                    if (Match(meldIdx + 1)) return true;
                    consumed[r] = false;
                }
                return false;
            }
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
        /// Pour chaque point de départ, tente de combler les trous avec des wilds.
        /// On accepte de démarrer au-dessus de la valeur minimale du jeu : tous les
        /// runs construits doivent rester dans les bornes définies par GameConfig
        /// (ou Constants.DefaultCardMin/Max si pas de config).
        /// </summary>
        private static void FindRuns(List<CardModel> cards, int length, List<List<CardModel>> results)
        {
            // Buckets par valeur pour accès O(1) — évite le FirstOrDefault dans la boucle.
            Dictionary<int, List<CardModel>> normalsByValue = new();
            List<CardModel> wilds = new();
            int minValue = int.MaxValue;
            int maxValue = int.MinValue;

            foreach (CardModel c in cards)
            {
                if (c.Type == CardType.Normal)
                {
                    if (!normalsByValue.TryGetValue(c.Value, out List<CardModel>? list))
                    {
                        list = new List<CardModel>();
                        normalsByValue[c.Value] = list;
                    }
                    list.Add(c);
                    if (c.Value < minValue) minValue = c.Value;
                    if (c.Value > maxValue) maxValue = c.Value;
                }
                else if (c.IsWild)
                {
                    wilds.Add(c);
                }
            }

            if (normalsByValue.Count == 0 && wilds.Count < length) return;

            // Bornes du run : du min des cartes possibles, jusqu'à un peu au-delà du max
            // pour permettre les runs qui se prolongent uniquement avec des wilds en fin.
            int rangeMin = normalsByValue.Count > 0 ? minValue - wilds.Count : 1;
            int rangeMax = normalsByValue.Count > 0 ? maxValue + wilds.Count : length;
            if (rangeMin < 1) rangeMin = 1;

            // Pour chaque point de départ, construire un run candidat.
            for (int startVal = rangeMin; startVal <= rangeMax - length + 1; startVal++)
            {
                List<CardModel> run = new(length);
                int wildsUsed = 0;
                bool valid = true;

                // Compteurs de cartes normales déjà utilisées par valeur (pour éviter
                // de réutiliser deux fois la même CardModel quand plusieurs cartes
                // partagent une valeur — possible avec un deck "Level 8" custom).
                Dictionary<int, int> usedByValue = new();

                for (int v = startVal; v < startVal + length; v++)
                {
                    bool placed = false;
                    if (normalsByValue.TryGetValue(v, out List<CardModel>? candidates))
                    {
                        usedByValue.TryGetValue(v, out int alreadyUsed);
                        if (alreadyUsed < candidates.Count)
                        {
                            run.Add(candidates[alreadyUsed]);
                            usedByValue[v] = alreadyUsed + 1;
                            placed = true;
                        }
                    }

                    if (!placed)
                    {
                        if (wildsUsed < wilds.Count)
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
                }

                if (valid && run.Count == length && !ContainsSequence(results, run))
                {
                    results.Add(run);
                }
            }
        }

        /// <summary>
        /// Vrai si <paramref name="results"/> contient déjà une liste avec exactement
        /// les mêmes CardModel dans le même ordre. Évite les doublons sans LINQ.
        /// </summary>
        private static bool ContainsSequence(List<List<CardModel>> results, List<CardModel> candidate)
        {
            for (int i = 0; i < results.Count; i++)
            {
                List<CardModel> existing = results[i];
                if (existing.Count != candidate.Count) continue;
                bool same = true;
                for (int j = 0; j < existing.Count; j++)
                {
                    if (existing[j].Id != candidate[j].Id) { same = false; break; }
                }
                if (same) return true;
            }
            return false;
        }

        /// <summary>
        /// Trouve tous les sets (même valeur) possibles de taille donnée.
        /// Réécrit sans LINQ pour réduire les allocations en hot path.
        /// </summary>
        private static void FindSets(List<CardModel> cards, int size, List<List<CardModel>> results)
        {
            // Group manuellement pour éviter LINQ.GroupBy (alloc IGrouping).
            Dictionary<int, List<CardModel>> groups = new();
            int wildCount = 0;
            List<CardModel> wilds = new();

            foreach (CardModel c in cards)
            {
                if (c.Type == CardType.Normal)
                {
                    if (!groups.TryGetValue(c.Value, out List<CardModel>? list))
                    {
                        list = new List<CardModel>();
                        groups[c.Value] = list;
                    }
                    list.Add(c);
                }
                else if (c.IsWild)
                {
                    wilds.Add(c);
                    wildCount++;
                }
            }

            foreach (KeyValuePair<int, List<CardModel>> group in groups)
            {
                int normalsAvailable = group.Value.Count;
                int wildsNeeded = size - normalsAvailable;
                if (wildsNeeded < 0 || wildsNeeded > wildCount) continue;

                List<CardModel> set = new(size);
                int normalsToTake = normalsAvailable < size ? normalsAvailable : size;
                for (int i = 0; i < normalsToTake; i++) set.Add(group.Value[i]);
                for (int w = 0; w < wildsNeeded; w++) set.Add(wilds[w]);

                if (set.Count == size) results.Add(set);
            }
        }

        /// <summary>
        /// Trouve tous les flushes (même couleur) possibles de taille donnée.
        /// </summary>
        private static void FindFlushes(List<CardModel> cards, int size, List<List<CardModel>> results)
        {
            Dictionary<CardColor, List<CardModel>> groups = new();
            int wildCount = 0;
            List<CardModel> wilds = new();

            foreach (CardModel c in cards)
            {
                if (c.Type == CardType.Normal)
                {
                    if (!groups.TryGetValue(c.Color, out List<CardModel>? list))
                    {
                        list = new List<CardModel>();
                        groups[c.Color] = list;
                    }
                    list.Add(c);
                }
                else if (c.IsWild)
                {
                    wilds.Add(c);
                    wildCount++;
                }
            }

            foreach (KeyValuePair<CardColor, List<CardModel>> group in groups)
            {
                int normalsAvailable = group.Value.Count;
                int wildsNeeded = size - normalsAvailable;
                if (wildsNeeded < 0 || wildsNeeded > wildCount) continue;

                List<CardModel> flush = new(size);
                int normalsToTake = normalsAvailable < size ? normalsAvailable : size;
                for (int i = 0; i < normalsToTake; i++) flush.Add(group.Value[i]);
                for (int w = 0; w < wildsNeeded; w++) flush.Add(wilds[w]);

                if (flush.Count == size) results.Add(flush);
            }
        }
    }
}
