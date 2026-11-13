using System.Collections.Generic;

namespace LevelUp.AI
{
    /// <summary>
    /// Tempérament d'un bot : deux réglages posés par-dessus son niveau de jeu
    /// (Facile / Normal / Expert), sans jamais lui donner d'information cachée.
    /// </summary>
    public readonly struct BotStyle
    {
        /// <summary>Poids du danger d'une défausse (1 = normal ; plus = garde les cartes qui aident les autres).</summary>
        public readonly float DangerFactor;

        /// <summary>Envie de jouer tout de suite une carte « Passe » / « +2 » (0 = normal).</summary>
        public readonly float ActionBonus;

        /// <summary>Mot affiché à côté du nom ("prudente", "fonceur"…), vide pour un bot sans tempérament.</summary>
        public readonly string Label;

        public BotStyle(float dangerFactor, float actionBonus, string label)
        {
            DangerFactor = dangerFactor;
            ActionBonus = actionBonus;
            Label = label;
        }

        public static readonly BotStyle Balanced = new(1f, 0f, "");
    }

    /// <summary>
    /// Adversaires à personnalité : un prénom et un tempérament, au lieu de
    /// « Bot 1 / Bot 2 ». Le tempérament se retrouve par le NOM (sauvegarde,
    /// reprise) : aucun format à faire évoluer, et un « Bot 2 » d'une ancienne
    /// partie ou de la donne du jour reste un bot équilibré.
    /// </summary>
    public static class BotPersonas
    {
        private static readonly (string Name, BotStyle Style)[] All =
        {
            ("Margot", new BotStyle(1.6f, -10f, "prudente")),
            ("Léon", new BotStyle(0.5f, 30f, "fonceur")),
            ("Inès", new BotStyle(1.2f, 45f, "bloqueuse")),
            ("Hugo", new BotStyle(1f, 0f, "posé")),
            ("Zoé", new BotStyle(0.7f, 15f, "joueuse")),
            ("Sacha", new BotStyle(1.4f, 20f, "tenace")),
        };

        /// <summary>Tempérament associé à un nom (équilibré si le nom n'est pas celui d'un personnage).</summary>
        public static BotStyle For(string? name)
        {
            foreach ((string n, BotStyle style) in All)
            {
                if (n == name) return style;
            }
            return BotStyle.Balanced;
        }

        /// <summary>
        /// <paramref name="count"/> prénoms de bots, dans un ordre fixe, en
        /// évitant ceux de <paramref name="taken"/> (pseudo du joueur).
        /// </summary>
        public static List<string> Names(int count, ICollection<string>? taken = null)
        {
            List<string> names = new(count);
            foreach ((string n, BotStyle _) in All)
            {
                if (names.Count >= count) break;
                if (taken != null && taken.Contains(n)) continue;
                names.Add(n);
            }
            for (int i = names.Count; i < count; i++) names.Add($"Bot {i + 1}");
            return names;
        }
    }
}
