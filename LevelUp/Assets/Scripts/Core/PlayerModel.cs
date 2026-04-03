using System.Collections.Generic;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Modèle de données d'un joueur : main, niveau actuel, combinaisons posées.
    /// </summary>
    public class PlayerModel
    {
        /// <summary>Index du joueur dans la partie (0-based).</summary>
        public int Index { get; }

        /// <summary>Nom affiché du joueur.</summary>
        public string Name { get; }

        /// <summary>Indique si ce joueur est contrôlé par l'IA.</summary>
        public bool IsAI { get; }

        /// <summary>Niveau actuel du joueur (1-8).</summary>
        public int CurrentLevel { get; set; }

        /// <summary>Indique si le joueur a posé son niveau ce round.</summary>
        public bool HasLaidDownThisRound { get; set; }

        /// <summary>Indique si le joueur est sauté ce tour.</summary>
        public bool IsSkipped { get; set; }

        /// <summary>La main du joueur.</summary>
        public List<CardModel> Hand { get; }

        /// <summary>Les combinaisons posées par le joueur sur la table.</summary>
        public List<Meld> LaidMelds { get; }

        /// <summary>
        /// Crée un nouveau joueur.
        /// </summary>
        public PlayerModel(int index, string name, bool isAI)
        {
            Index = index;
            Name = name;
            IsAI = isAI;
            CurrentLevel = 1;
            HasLaidDownThisRound = false;
            IsSkipped = false;
            Hand = new List<CardModel>();
            LaidMelds = new List<Meld>();
        }

        /// <summary>
        /// Ajoute une carte à la main du joueur.
        /// </summary>
        public void AddToHand(CardModel card)
        {
            Hand.Add(card);
            EventBus.Publish(new HandChangedEvent { PlayerIndex = Index, NewHand = Hand });
        }

        /// <summary>
        /// Retire une carte de la main du joueur.
        /// </summary>
        public bool RemoveFromHand(CardModel card)
        {
            bool removed = Hand.Remove(card);
            if (removed)
            {
                EventBus.Publish(new HandChangedEvent { PlayerIndex = Index, NewHand = Hand });
            }
            return removed;
        }

        /// <summary>
        /// Retire plusieurs cartes de la main.
        /// </summary>
        public void RemoveFromHand(List<CardModel> cards)
        {
            foreach (CardModel card in cards)
            {
                Hand.Remove(card);
            }
            EventBus.Publish(new HandChangedEvent { PlayerIndex = Index, NewHand = Hand });
        }

        /// <summary>
        /// Réinitialise l'état du joueur pour un nouveau round (garde le niveau).
        /// </summary>
        public void ResetForNewRound()
        {
            Hand.Clear();
            LaidMelds.Clear();
            HasLaidDownThisRound = false;
            IsSkipped = false;
        }

        /// <summary>
        /// Réordonne une carte dans la main (drag-to-reorder).
        /// </summary>
        public void ReorderHand(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= Hand.Count) return;
            if (toIndex < 0 || toIndex >= Hand.Count) return;
            if (fromIndex == toIndex) return;

            CardModel card = Hand[fromIndex];
            Hand.RemoveAt(fromIndex);
            Hand.Insert(toIndex, card);
            // Pas de HandChangedEvent ici — la vue gère le réordonnement visuellement
        }

        /// <summary>
        /// Indique si la main est vide.
        /// </summary>
        public bool IsHandEmpty => Hand.Count == 0;
    }

    /// <summary>
    /// Représente une combinaison posée sur la table (suite, brelan, flush, etc.).
    /// </summary>
    public class Meld
    {
        /// <summary>Type de la combinaison.</summary>
        public MeldType Type { get; }

        /// <summary>Cartes composant la combinaison.</summary>
        public List<CardModel> Cards { get; }

        /// <summary>Index du joueur propriétaire.</summary>
        public int OwnerIndex { get; }

        /// <summary>
        /// Crée une nouvelle combinaison.
        /// </summary>
        public Meld(MeldType type, List<CardModel> cards, int ownerIndex)
        {
            Type = type;
            Cards = new List<CardModel>(cards);
            OwnerIndex = ownerIndex;
        }

        /// <summary>
        /// Tente d'ajouter une carte à la combinaison.
        /// Retourne true si la carte est compatible.
        /// </summary>
        public bool TryAddCard(CardModel card)
        {
            List<CardModel> test = new(Cards) { card };

            bool valid = Type switch
            {
                MeldType.Run   => CardExtensions.IsValidRun(test),
                MeldType.Set   => CardExtensions.IsValidSet(test),
                MeldType.Flush => CardExtensions.IsValidFlush(test),
                _              => false
            };

            if (valid)
            {
                Cards.Add(card);
                if (Type == MeldType.Run)
                {
                    Cards.Sort((a, b) => a.Value.CompareTo(b.Value));
                }
            }

            return valid;
        }
    }
}
