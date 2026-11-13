using System.Collections.Generic;
using System.Text;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Catalogue des contrats (exigences de pose) des 8 niveaux.
    ///
    /// Chaque niveau propose plusieurs variantes de difficulté équivalente ;
    /// la variante 0 est le contrat "classique". Les difficultés ont été
    /// calibrées par simulation (deck 6 couleurs × 1–18, pool de 14 cartes) :
    /// probabilité de pouvoir poser ≈ 60 % au niveau 1, décroissante jusqu'à
    /// ≈ 3 % au niveau 8, sans "mur" ni niveau plus facile que le précédent.
    /// </summary>
    public static class ContractCatalog
    {
        private static LevelValidator.LevelRequirement Run(int n) => new(MeldType.Run, n);
        private static LevelValidator.LevelRequirement Set(int n) => new(MeldType.Set, n);
        private static LevelValidator.LevelRequirement Flush(int n) => new(MeldType.Flush, n);

        // [niveau - 1][variante][exigence]. Probabilité simulée en commentaire.
        private static readonly LevelValidator.LevelRequirement[][][] Variants =
        {
            // Niveau 1 (~60 %)
            new[]
            {
                new[] { Run(3), Run(3) },               // 59 %
                new[] { Run(4), Set(2) },               // 61 %
            },
            // Niveau 2 (~43 %)
            new[]
            {
                new[] { Set(3), Set(2) },               // 43 %
                new[] { Run(4), Run(3) },               // 42 %
            },
            // Niveau 3 (~34 %)
            new[]
            {
                new[] { Run(3), Set(3) },               // 35 %
                new[] { Run(5) },                       // 34 %
                new[] { Flush(5) },                     // 33 %
            },
            // Niveau 4 (~18 %)
            new[]
            {
                new[] { Flush(5), Run(3) },             // 20 %
                new[] { Run(4), Set(3) },               // 17 %
                new[] { Run(3), Run(3), Run(3) },       // 17 %
            },
            // Niveau 5 (~14 %)
            new[]
            {
                new[] { Run(6) },                       // 15 %
                new[] { Run(4), Run(4) },               // 14 %
                new[] { Run(6), Set(2) },               // 13 %
            },
            // Niveau 6 (~7 %)
            new[]
            {
                new[] { Run(7) },                       // 7 %
                new[] { Flush(6) },                     // 7 %
                new[] { Run(5), Run(4) },               // 7,5 %
                new[] { Run(5), Set(3) },               // 7 %
            },
            // Niveau 7 (~5,5 %)
            new[]
            {
                new[] { Set(3), Set(3) },               // 6 %
                new[] { Flush(6), Set(2) },             // 6 %
                new[] { Set(4), Set(2) },               // 4,7 %
            },
            // Niveau 8 (~2 %)
            new[]
            {
                new[] { Run(8) },                       // 3 %
                new[] { Flush(7) },                     // 1,4 %
            },
        };

        /// <summary>Nombre de variantes disponibles pour un niveau (0 si hors bornes).</summary>
        public static int VariantCount(int level)
        {
            return level >= 1 && level <= Variants.Length ? Variants[level - 1].Length : 0;
        }

        /// <summary>
        /// Exigences d'un niveau pour une variante donnée (variante bornée).
        /// Retourne une liste vide si le niveau est hors bornes.
        /// </summary>
        public static List<LevelValidator.LevelRequirement> Get(int level, int variant)
        {
            List<LevelValidator.LevelRequirement> result = new();
            int count = VariantCount(level);
            if (count == 0) return result;

            int v = variant < 0 || variant >= count ? 0 : variant;
            result.AddRange(Variants[level - 1][v]);
            return result;
        }

        /// <summary>Texte court du contrat d'un niveau de la partie en cours.</summary>
        public static string DescribeLevel(int level)
        {
            List<LevelValidator.LevelRequirement> reqs = GameSession.Contracts.GetRequirements(level);
            return reqs.Count == 0 ? "?" : Describe(reqs);
        }

        /// <summary>
        /// Texte court d'un contrat, exigences identiques regroupées :
        /// "2 suites de 3", "1 brelan + 1 paire", "1 couleur de 5 + 1 suite de 3".
        /// </summary>
        public static string Describe(IReadOnlyList<LevelValidator.LevelRequirement> reqs)
        {
            StringBuilder sb = new();
            int i = 0;
            while (i < reqs.Count)
            {
                int same = 1;
                while (i + same < reqs.Count
                       && reqs[i + same].Type == reqs[i].Type
                       && reqs[i + same].CardCount == reqs[i].CardCount)
                {
                    same++;
                }

                if (sb.Length > 0) sb.Append(" + ");
                sb.Append(same).Append(' ').Append(Name(reqs[i], plural: same > 1));
                i += same;
            }
            return sb.ToString();
        }

        /// <summary>Nom d'une exigence seule : "suite de 4", "brelan", "couleur de 5".</summary>
        public static string Name(LevelValidator.LevelRequirement req, bool plural = false)
        {
            string s = plural ? "s" : "";
            return req.Type switch
            {
                MeldType.Run => $"suite{s} de {req.CardCount}",
                MeldType.Flush => $"couleur{s} de {req.CardCount}",
                MeldType.Set => req.CardCount switch
                {
                    2 => $"paire{s}",
                    3 => $"brelan{s}",
                    4 => $"carré{s}",
                    _ => $"série{s} de {req.CardCount}"
                },
                _ => "?"
            };
        }
    }

    /// <summary>
    /// Contrats retenus pour une partie : une variante par niveau.
    /// Le mode aléatoire tire les variantes depuis le seed de la partie, donc
    /// tous les appareils d'une partie en ligne obtiennent le même plan.
    /// </summary>
    public sealed class ContractPlan
    {
        private readonly int[] _variants;

        /// <summary>Plan classique : variante 0 à chaque niveau.</summary>
        public static readonly ContractPlan Classic = new(new int[Constants.MaxLevel], false);

        /// <summary>Vrai si les contrats ont été tirés au sort.</summary>
        public bool IsRandom { get; }

        private ContractPlan(int[] variants, bool isRandom)
        {
            _variants = variants;
            IsRandom = isRandom;
        }

        /// <summary>Tire une variante par niveau, de façon déterministe.</summary>
        public static ContractPlan CreateRandom(int seed)
        {
            // Seed dérivé : ne consomme pas le RNG du DeckManager, les mélanges
            // restent identiques quel que soit le mode de contrats.
            System.Random rng = new(unchecked(seed * 31 + 0x5EED));
            int[] variants = new int[Constants.MaxLevel];
            for (int level = 1; level <= Constants.MaxLevel; level++)
            {
                variants[level - 1] = rng.Next(0, ContractCatalog.VariantCount(level));
            }
            return new ContractPlan(variants, true);
        }

        private List<LevelValidator.LevelRequirement>? _custom;

        /// <summary>Plan à contrat unique imposé pour tous les niveaux (mode Défi).</summary>
        public static ContractPlan Custom(IReadOnlyList<LevelValidator.LevelRequirement> requirements)
        {
            return new ContractPlan(new int[Constants.MaxLevel], false)
            {
                _custom = new List<LevelValidator.LevelRequirement>(requirements)
            };
        }

        /// <summary>Exigences du niveau (1-8) selon ce plan.</summary>
        public List<LevelValidator.LevelRequirement> GetRequirements(int level)
        {
            if (_custom != null) return new List<LevelValidator.LevelRequirement>(_custom);
            int variant = level >= 1 && level <= _variants.Length ? _variants[level - 1] : 0;
            return ContractCatalog.Get(level, variant);
        }
    }

    /// <summary>
    /// Progression d'une main vers un contrat, exigence par exigence — sert
    /// d'aide de lecture ("brelan 2/3"). Estimation gloutonne : chaque exigence
    /// (de la plus longue à la plus courte) prend le meilleur groupe parmi les
    /// cartes restantes. La validation réelle reste <see cref="LevelValidator"/>.
    /// </summary>
    public static class ContractProgress
    {
        public readonly struct Entry
        {
            public readonly LevelValidator.LevelRequirement Requirement;
            public readonly int Have;

            public Entry(LevelValidator.LevelRequirement requirement, int have)
            {
                Requirement = requirement;
                Have = have;
            }

            public bool IsMet => Have >= Requirement.CardCount;
        }

        public static List<Entry> Evaluate(IReadOnlyList<CardModel> hand,
            IReadOnlyList<LevelValidator.LevelRequirement> reqs)
        {
            List<CardModel> remaining = new(hand);

            // Ordre de traitement : exigences les plus longues d'abord.
            int[] order = new int[reqs.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            System.Array.Sort(order, (a, b) => reqs[b].CardCount.CompareTo(reqs[a].CardCount));

            int[] have = new int[reqs.Count];
            List<CardModel> best = new();
            foreach (int idx in order)
            {
                best.Clear();
                BestGroup(remaining, reqs[idx], best);
                have[idx] = best.Count;
                foreach (CardModel c in best) remaining.Remove(c);
            }

            List<Entry> result = new(reqs.Count);
            for (int i = 0; i < reqs.Count; i++)
            {
                result.Add(new Entry(reqs[i], have[i]));
            }
            return result;
        }

        private static void BestGroup(List<CardModel> cards, LevelValidator.LevelRequirement req,
            List<CardModel> output)
        {
            int need = req.CardCount;
            List<CardModel> wilds = new();
            foreach (CardModel c in cards)
            {
                if (c.IsWild) wilds.Add(c);
            }

            if (req.Type == MeldType.Run)
            {
                // Fenêtre de `need` valeurs consécutives contenant le plus de cartes.
                Dictionary<int, CardModel> byValue = new();
                foreach (CardModel c in cards)
                {
                    if (c.Type == CardType.Normal && !byValue.ContainsKey(c.Value))
                        byValue[c.Value] = c;
                }

                List<CardModel> window = new();
                for (int start = Constants.DefaultCardMin;
                     start <= Constants.DefaultCardMax - need + 1; start++)
                {
                    window.Clear();
                    for (int v = start; v < start + need; v++)
                    {
                        if (byValue.TryGetValue(v, out CardModel card)) window.Add(card);
                    }
                    if (window.Count > output.Count)
                    {
                        output.Clear();
                        output.AddRange(window);
                    }
                }
            }
            else
            {
                // Plus grand groupe de même valeur (Set) ou même couleur (Flush).
                Dictionary<int, List<CardModel>> groups = new();
                foreach (CardModel c in cards)
                {
                    if (c.Type != CardType.Normal) continue;
                    int key = req.Type == MeldType.Set ? c.Value : (int)c.Color;
                    if (!groups.TryGetValue(key, out List<CardModel>? g))
                    {
                        g = new List<CardModel>();
                        groups[key] = g;
                    }
                    g.Add(c);
                }
                foreach (List<CardModel> g in groups.Values)
                {
                    if (g.Count > output.Count)
                    {
                        output.Clear();
                        output.AddRange(g);
                    }
                }
                if (output.Count > need) output.RemoveRange(need, output.Count - need);
            }

            // Les wilds comblent le manque.
            for (int w = 0; w < wilds.Count && output.Count < need; w++)
            {
                output.Add(wilds[w]);
            }
        }
    }
}
