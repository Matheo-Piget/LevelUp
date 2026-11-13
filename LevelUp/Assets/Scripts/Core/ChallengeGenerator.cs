using System.Collections.Generic;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>Un défi : une main, quelques pioches connues d'avance, un contrat à poser.</summary>
    public sealed class Challenge
    {
        public int Seed;
        public int Difficulty;
        public int HandSize;
        public int Draws;
        public List<LevelValidator.LevelRequirement> Contract = new();

        /// <summary>Ordre du paquet (dernier élément = dessus), à donner à <see cref="GameManager.NextDeckOrder"/>.</summary>
        public List<CardModel> DeckOrder = new();

        /// <summary>Les pioches à venir, dans l'ordre (montrées au joueur).</summary>
        public List<CardModel> Upcoming = new();
    }

    /// <summary>
    /// Fabrique des défis déterministes (même graine = même défi) et toujours
    /// faisables : on part d'une pose valide du contrat, on en retire 1 à 3
    /// cartes pour les glisser dans les pioches, et on complète la main et la
    /// pioche avec des cartes sans rapport. Le joueur doit donc garder les
    /// bonnes cartes et défausser les autres. Pur C#, sans Unity.
    /// </summary>
    public static class ChallengeGenerator
    {
        public const int HandSize = 8;
        public const int Draws = 6;

        public static readonly string[] DifficultyNames = { "Facile", "Moyen", "Difficile" };

        private static List<LevelValidator.LevelRequirement> ContractFor(int difficulty) => difficulty switch
        {
            0 => new() { new(MeldType.Run, 3), new(MeldType.Run, 3) },
            1 => new() { new(MeldType.Run, 4), new(MeldType.Set, 3) },
            _ => new() { new(MeldType.Run, 5), new(MeldType.Set, 3) },
        };

        public static Challenge Create(GameConfig config, int seed, int difficulty)
        {
            // Les cartes « sans rapport » peuvent, par hasard, former le contrat à
            // elles seules : on retire alors (même graine, tentative suivante)
            // jusqu'à une main de départ qui ne le pose pas déjà.
            ContractPlan previous = GameSession.Contracts;
            try
            {
                Challenge challenge = Build(config, seed, difficulty, 0);
                for (int attempt = 1; attempt <= 40; attempt++)
                {
                    GameSession.Contracts = ContractPlan.Custom(challenge.Contract);
                    List<CardModel> hand = new();
                    for (int i = 0; i < challenge.HandSize; i++)
                        hand.Add(challenge.DeckOrder[challenge.DeckOrder.Count - 1 - i]);
                    if (!LevelValidator.IsLevelComplete(hand, 1, config, out _)) break;
                    challenge = Build(config, seed, difficulty, attempt);
                }
                return challenge;
            }
            finally
            {
                GameSession.Contracts = previous;
            }
        }

        private static Challenge Build(GameConfig config, int seed, int difficulty, int attempt)
        {
            difficulty = difficulty < 0 ? 0 : difficulty > 2 ? 2 : difficulty;
            System.Random rng = new(unchecked(seed * 7919 + difficulty * 104729 + attempt * 15485863));
            List<CardModel> pool = DeckManager.BuildComposition(config);
            Challenge challenge = new()
            {
                Seed = seed,
                Difficulty = difficulty,
                HandSize = HandSize,
                Draws = Draws,
                Contract = ContractFor(difficulty)
            };

            CardModel Take(System.Predicate<CardModel> match)
            {
                List<int> candidates = new();
                for (int i = 0; i < pool.Count; i++)
                {
                    if (match(pool[i])) candidates.Add(i);
                }
                int pick = candidates[rng.Next(candidates.Count)];
                CardModel card = pool[pick];
                pool.RemoveAt(pick);
                return card;
            }

            // 1. Une pose valide du contrat (valeurs distinctes entre combinaisons : pas d'ambiguïté).
            List<CardModel> solution = new();
            HashSet<int> usedValues = new();
            foreach (LevelValidator.LevelRequirement req in challenge.Contract)
            {
                if (req.Type == MeldType.Run)
                {
                    int start;
                    int guard = 0;
                    do
                    {
                        start = config.CardMinValue + rng.Next(config.CardMaxValue - config.CardMinValue - req.CardCount + 2);
                    } while (Overlaps(usedValues, start, req.CardCount) && guard++ < 50);
                    for (int v = start; v < start + req.CardCount; v++)
                    {
                        int value = v;
                        solution.Add(Take(c => c.Value == value));
                        usedValues.Add(v);
                    }
                }
                else
                {
                    int value;
                    int guard = 0;
                    do
                    {
                        value = config.CardMinValue + rng.Next(config.CardMaxValue - config.CardMinValue + 1);
                    } while (usedValues.Contains(value) && guard++ < 50);
                    usedValues.Add(value);
                    for (int n = 0; n < req.CardCount; n++) solution.Add(Take(c => c.Value == value));
                }
            }

            // 2. 1 à 3 cartes de la pose partent dans les pioches à venir.
            int missing = difficulty + 1;
            List<CardModel> toDraw = new();
            for (int i = 0; i < missing; i++)
            {
                int pick = rng.Next(solution.Count);
                toDraw.Add(solution[pick]);
                solution.RemoveAt(pick);
            }

            // 3. Cartes sans rapport : ni une valeur de la pose, ni une voisine
            //    (elles ne doivent pas offrir une autre solution par hasard).
            CardModel Noise() => Take(c => !NearUsed(usedValues, c.Value));

            List<CardModel> hand = new(solution);
            while (hand.Count < HandSize) hand.Add(Noise());
            Shuffle(hand, rng);

            CardModel?[] draws = new CardModel?[Draws];
            foreach (CardModel card in toDraw)
            {
                int slot;
                do { slot = rng.Next(Draws); } while (draws[slot].HasValue);
                draws[slot] = card;
            }
            for (int i = 0; i < Draws; i++)
            {
                CardModel card = draws[i] ?? Noise();
                challenge.Upcoming.Add(card);
            }

            // 4. Ordre du paquet : la main (distribuée en premier), puis les pioches.
            List<CardModel> top = new(hand);
            top.AddRange(challenge.Upcoming);
            top.Reverse();
            challenge.DeckOrder = pool; // le reste du paquet, jamais atteint
            challenge.DeckOrder.AddRange(top);
            return challenge;
        }

        private static bool Overlaps(HashSet<int> used, int start, int count)
        {
            for (int v = start - 1; v <= start + count; v++)
            {
                if (used.Contains(v)) return true;
            }
            return false;
        }

        private static bool NearUsed(HashSet<int> used, int value) =>
            used.Contains(value) || used.Contains(value - 1) || used.Contains(value + 1);

        private static void Shuffle(List<CardModel> cards, System.Random rng)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }
    }
}
