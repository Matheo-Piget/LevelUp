using System.Text;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Textes des règles de partie (écran "Nouvelle partie", salon en ligne,
    /// pastille de la top bar) — un seul endroit pour la formulation.
    /// </summary>
    public static class MatchRulesText
    {
        public static readonly string[] BotLevels = { "Facile", "Normal", "Expert" };

        /// <summary>"Sans cartes spéciales" / "Jokers" / "Jokers, Passe et +2".</summary>
        public static string SpecialCards(SpecialCardsMode mode) => mode switch
        {
            SpecialCardsMode.Jokers => "Jokers",
            SpecialCardsMode.All => "Jokers, Passe et +2",
            _ => "Sans cartes spéciales"
        };

        /// <summary>Explication d'une ligne sous le choix des cartes spéciales.</summary>
        public static string SpecialCardsCaption(SpecialCardsMode mode) => mode switch
        {
            SpecialCardsMode.Jokers =>
                $"+ {MatchRules.JokersInDeck} jokers : chacun remplace n'importe quelle carte dans une combinaison.",
            SpecialCardsMode.All =>
                $"+ {MatchRules.JokersInDeck} jokers, {MatchRules.ActionsPerTypeInDeck} « Passe » (un adversaire saute son tour) " +
                $"et {MatchRules.ActionsPerTypeInDeck} « +2 » (il pioche 2 cartes), joués en défausse.",
            _ => "Paquet classique : uniquement les cartes numérotées de 1 à 18."
        };

        public static string TurnTimer(int seconds) => seconds <= 0 ? "Sans minuteur" : $"{seconds} s par tour";

        /// <summary>Résumé complet sur une ligne (salon) : "Contrats fixes · Sans cartes spéciales · …".</summary>
        public static string Summary(MatchRules rules, int botDifficulty)
        {
            StringBuilder sb = new();
            sb.Append(rules.RandomContracts ? "Contrats variés" : "Contrats fixes");
            sb.Append("  ·  ").Append(SpecialCards(rules.SpecialCards));
            sb.Append("  ·  ").Append(rules.HandSize).Append(" cartes");
            sb.Append("  ·  ").Append(rules.LevelsToWin).Append(" niveaux");
            sb.Append("  ·  ").Append(TurnTimer(rules.TurnTimerSeconds));
            if (botDifficulty >= 0 && botDifficulty < BotLevels.Length)
            {
                sb.Append("  ·  Bots ").Append(BotLevels[botDifficulty].ToLowerInvariant());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Étiquette courte des écarts au jeu classique (top bar) : "Jokers · 30 s ·
        /// 6 niveaux". Vide si la partie suit les règles d'origine.
        /// </summary>
        public static string ShortTags(MatchRules rules)
        {
            StringBuilder sb = new();
            void Add(string s)
            {
                if (sb.Length > 0) sb.Append("  ·  ");
                sb.Append(s);
            }

            if (rules.SpecialCards == SpecialCardsMode.Jokers) Add("Jokers");
            else if (rules.SpecialCards == SpecialCardsMode.All) Add("Jokers · actions");
            if (rules.HasTurnTimer) Add($"{rules.TurnTimerSeconds} s/tour");
            if (rules.LevelsToWin != Constants.MaxLevel) Add($"{rules.LevelsToWin} niv.");
            if (rules.HandSize != MatchRules.DefaultHandSize) Add($"{rules.HandSize} cartes");
            if (rules.RandomContracts) Add("Contrats variés");
            if (rules.GoOutBonus != 2) Add("Sortie +1");
            if (rules.FinalLevelRequired) Add("Dernier niv. obligatoire");
            if (rules.RoundTurnLimit > 0) Add($"{rules.RoundTurnLimit} tours max");
            if (rules.CatchUp) Add("Coup de pouce");
            return sb.ToString();
        }

        /// <summary>Explication des variantes actives, en une phrase chacune (vide si aucune).</summary>
        public static string VariantsCaption(MatchRules rules)
        {
            StringBuilder sb = new();
            void Add(string s)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(s);
            }

            if (rules.GoOutBonus != 2) Add("Sortir ne fait monter que d'un niveau.");
            if (rules.FinalLevelRequired) Add("Le dernier contrat doit être posé pour gagner.");
            if (rules.RoundTurnLimit > 0)
                Add($"Après {rules.RoundTurnLimit} tours chacun, la manche s'arrête : seuls ceux qui ont posé montent.");
            if (rules.CatchUp) Add("Deux manches au même niveau : une carte de plus à la donne.");
            return sb.ToString();
        }
    }
}
