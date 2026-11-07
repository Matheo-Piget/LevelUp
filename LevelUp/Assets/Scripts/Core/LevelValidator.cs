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
        /// Priorité : définitions explicites du GameConfig, sinon le plan de
        /// contrats de la partie (<see cref="GameSession.Contracts"/>, cf. <see cref="ContractCatalog"/>).
        /// </summary>
        public static List<List<LevelRequirement>> GetRequirements(int level, GameConfig? config = null)
        {
            if (config != null && config.LevelDefinitions.Count >= level)
            {
                return config.LevelDefinitions[level - 1].ToRequirements();
            }

            List<LevelRequirement> reqs = GameSession.Contracts.GetRequirements(level);
            return reqs.Count == 0
                ? new List<List<LevelRequirement>>()
                : new List<List<LevelRequirement>> { reqs };
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

        // ─────────────────────────────────────────────────────────────────
        //  Recherche des combinaisons candidates
        //
        //  Les règles ne changent pas (le GameCommandExecutor revalide chaque
        //  combinaison posée) : cette recherche doit seulement être COMPLÈTE.
        //  Avant, un seul candidat était tenté par valeur/couleur (« les N
        //  premières cartes »), et un groupe plus grand que nécessaire (quatre 7
        //  pour un brelan, six rouges pour une couleur de 5) était ignoré : la
        //  pose échouait alors qu'une répartition valide existait, et le joueur
        //  ne pouvait pas choisir quelle carte allait dans quelle combinaison.
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Garde-fou : nombre max de candidats générés par exigence.</summary>
        private const int MaxCandidatesPerRequirement = 512;

        /// <summary>Garde-fou : nombre max de nœuds explorés par recherche de poses.</summary>
        private const int MaxSearchNodes = 20000;

        /// <summary>
        /// Toutes les façons DISTINCTES de poser le contrat du niveau avec ces
        /// cartes (au plus <paramref name="maxOptions"/>). Deux poses qui ne
        /// diffèrent que par des cartes identiques (même valeur, même couleur)
        /// comptent pour une. Ordre : celles qui utilisent le moins de jokers
        /// d'abord (les jokers restent en main).
        /// </summary>
        public static List<List<Meld>> FindLayDownOptions(IReadOnlyList<CardModel> cards, int level,
            GameConfig? config, int maxOptions = 6)
        {
            List<List<Meld>> options = new();
            HashSet<string> seen = new();
            List<List<LevelRequirement>> requirementSets = GetRequirements(level, config);

            foreach (List<LevelRequirement> requirements in requirementSets)
            {
                int budget = MaxSearchNodes;
                CollectLayDowns(new List<CardModel>(cards), requirements, 0, new List<Meld>(),
                    options, seen, maxOptions, ref budget);
                if (options.Count >= maxOptions) break;
            }

            options.Sort((a, b) => CountWilds(a).CompareTo(CountWilds(b)));
            return options;
        }

        private static void CollectLayDowns(List<CardModel> available, List<LevelRequirement> requirements,
            int reqIndex, List<Meld> current, List<List<Meld>> options, HashSet<string> seen,
            int maxOptions, ref int budget)
        {
            if (options.Count >= maxOptions || budget-- <= 0) return;

            if (reqIndex >= requirements.Count)
            {
                string signature = Signature(current);
                if (seen.Add(signature)) options.Add(new List<Meld>(current));
                return;
            }

            LevelRequirement req = requirements[reqIndex];
            foreach (List<CardModel> candidate in FindCandidates(available, req))
            {
                List<CardModel> remaining = new(available);
                foreach (CardModel card in candidate) remaining.Remove(card);

                current.Add(new Meld(req.Type, candidate, -1));
                CollectLayDowns(remaining, requirements, reqIndex + 1, current, options, seen,
                    maxOptions, ref budget);
                current.RemoveAt(current.Count - 1);

                if (options.Count >= maxOptions || budget <= 0) return;
            }
        }

        /// <summary>
        /// Empreinte d'une pose, indépendante de l'ordre des combinaisons et des
        /// cartes, et de l'identité des cartes identiques (valeur + couleur).
        /// </summary>
        private static string Signature(List<Meld> melds)
        {
            List<string> parts = new(melds.Count);
            foreach (Meld meld in melds)
            {
                List<string> tokens = new(meld.Cards.Count);
                foreach (CardModel c in meld.Cards)
                {
                    tokens.Add(c.Type == CardType.Normal
                        ? $"{c.Value}.{(int)c.Color}"
                        : $"T{(int)c.Type}");
                }
                tokens.Sort(string.CompareOrdinal);
                parts.Add($"{(int)meld.Type}:{string.Join(",", tokens)}");
            }
            parts.Sort(string.CompareOrdinal);
            return string.Join("|", parts);
        }

        private static int CountWilds(List<Meld> melds)
        {
            int n = 0;
            foreach (Meld meld in melds)
            {
                foreach (CardModel c in meld.Cards)
                {
                    if (c.IsWild) n++;
                }
            }
            return n;
        }

        /// <summary>
        /// Trouve toutes les suites possibles de longueur donnée. Pour chaque
        /// valeur de la suite, chaque carte disponible de cette valeur (une par
        /// couleur) est essayée, puis un joker : la couleur des cartes de la
        /// suite peut ainsi être laissée à une autre combinaison (une couleur).
        /// Bornes inchangées : du min des cartes (moins les jokers) au max (plus
        /// les jokers), la validité finale restant celle de CardExtensions.
        /// </summary>
        private static void FindRuns(List<CardModel> cards, int length, List<List<CardModel>> results)
        {
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
                    // Une carte par couleur : deux cartes identiques sont interchangeables.
                    bool sameColor = false;
                    foreach (CardModel other in list)
                    {
                        if (other.Color == c.Color) { sameColor = true; break; }
                    }
                    if (!sameColor) list.Add(c);
                    if (c.Value < minValue) minValue = c.Value;
                    if (c.Value > maxValue) maxValue = c.Value;
                }
                else if (c.IsWild)
                {
                    wilds.Add(c);
                }
            }

            if (normalsByValue.Count == 0 && wilds.Count < length) return;

            int rangeMin = normalsByValue.Count > 0 ? minValue - wilds.Count : 1;
            int rangeMax = normalsByValue.Count > 0 ? maxValue + wilds.Count : length;
            if (rangeMin < 1) rangeMin = 1;

            List<CardModel> run = new(length);
            for (int startVal = rangeMin; startVal <= rangeMax - length + 1; startVal++)
            {
                BuildRun(startVal, 0, 0);
                if (results.Count >= MaxCandidatesPerRequirement) return;
            }

            void BuildRun(int startVal, int pos, int wildsUsed)
            {
                if (results.Count >= MaxCandidatesPerRequirement) return;
                if (pos == length)
                {
                    if (!ContainsSequence(results, run)) results.Add(new List<CardModel>(run));
                    return;
                }

                int value = startVal + pos;
                if (normalsByValue.TryGetValue(value, out List<CardModel>? options))
                {
                    foreach (CardModel card in options)
                    {
                        run.Add(card);
                        BuildRun(startVal, pos + 1, wildsUsed);
                        run.RemoveAt(run.Count - 1);
                    }
                }

                if (wildsUsed < wilds.Count)
                {
                    run.Add(wilds[wildsUsed]);
                    BuildRun(startVal, pos + 1, wildsUsed + 1);
                    run.RemoveAt(run.Count - 1);
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
        /// Trouve tous les sets (même valeur) possibles de taille donnée : toutes
        /// les façons de prendre les cartes d'une valeur (+ jokers si besoin).
        /// </summary>
        private static void FindSets(List<CardModel> cards, int size, List<List<CardModel>> results)
        {
            SortedDictionary<int, List<CardModel>> groups = new();
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
                }
            }

            foreach (KeyValuePair<int, List<CardModel>> group in groups)
            {
                AddGroupCombinations(group.Value, wilds, size, results);
                if (results.Count >= MaxCandidatesPerRequirement) return;
            }
        }

        /// <summary>
        /// Trouve tous les flushes (même couleur) possibles de taille donnée :
        /// toutes les façons de prendre les cartes d'une couleur (+ jokers si besoin).
        /// </summary>
        private static void FindFlushes(List<CardModel> cards, int size, List<List<CardModel>> results)
        {
            SortedDictionary<CardColor, List<CardModel>> groups = new();
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
                }
            }

            foreach (KeyValuePair<CardColor, List<CardModel>> group in groups)
            {
                AddGroupCombinations(group.Value, wilds, size, results);
                if (results.Count >= MaxCandidatesPerRequirement) return;
            }
        }

        /// <summary>
        /// Combinaisons « j cartes du groupe + (taille − j) jokers », du plus de
        /// cartes normales au moins (les jokers restent en main de préférence).
        /// Au moins une carte normale, comme avant : un groupe n'existe que
        /// s'il contient une carte de la valeur / couleur.
        /// </summary>
        private static void AddGroupCombinations(List<CardModel> group, List<CardModel> wilds, int size,
            List<List<CardModel>> results)
        {
            int maxNormals = group.Count < size ? group.Count : size;
            int minNormals = size - wilds.Count;
            if (minNormals < 1) minNormals = 1;

            for (int normals = maxNormals; normals >= minNormals; normals--)
            {
                int wildCount = size - normals;
                foreach (List<CardModel> picked in Combinations(group, normals))
                {
                    foreach (List<CardModel> pickedWilds in Combinations(wilds, wildCount))
                    {
                        if (results.Count >= MaxCandidatesPerRequirement) return;
                        List<CardModel> meld = new(size);
                        meld.AddRange(picked);
                        meld.AddRange(pickedWilds);
                        results.Add(meld);
                    }
                }
            }
        }

        /// <summary>Toutes les combinaisons de k éléments (ordre lexicographique des index).</summary>
        private static IEnumerable<List<CardModel>> Combinations(List<CardModel> source, int k)
        {
            if (k < 0 || k > source.Count) yield break;
            if (k == 0)
            {
                yield return new List<CardModel>();
                yield break;
            }

            int[] idx = new int[k];
            for (int i = 0; i < k; i++) idx[i] = i;

            while (true)
            {
                List<CardModel> combo = new(k);
                for (int i = 0; i < k; i++) combo.Add(source[idx[i]]);
                yield return combo;

                int pos = k - 1;
                while (pos >= 0 && idx[pos] == source.Count - k + pos) pos--;
                if (pos < 0) yield break;
                idx[pos]++;
                for (int i = pos + 1; i < k; i++) idx[i] = idx[i - 1] + 1;
            }
        }
    }
}
