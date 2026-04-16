using System.Collections.Generic;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Modèle de données d'un joueur : main, niveau actuel, combinaisons posées.
    ///
    /// Encapsulation stricte :
    /// - Les propriétés mutables ont des setters <c>internal</c> (Core uniquement).
    /// - La main et les melds sont exposés en lecture seule (<see cref="IReadOnlyList{T}"/>).
    /// - Les méthodes de mutation sont <c>internal</c> : seul le <see cref="GameCommandExecutor"/> y accède.
    /// - Aucun événement publié ici — c'est la responsabilité de l'exécuteur.
    /// </summary>
    public class PlayerModel
    {
        private readonly List<CardModel> _hand;
        private readonly List<Meld> _laidMelds;

        /// <summary>Index du joueur dans la partie (0-based).</summary>
        public int Index { get; }

        /// <summary>Nom affiché du joueur.</summary>
        public string Name { get; }

        /// <summary>Indique si ce joueur est contrôlé par l'IA.</summary>
        public bool IsAI { get; }

        /// <summary>Niveau actuel du joueur (1-8).</summary>
        public int CurrentLevel { get; internal set; }

        /// <summary>Indique si le joueur a posé son niveau ce round.</summary>
        public bool HasLaidDownThisRound { get; internal set; }

        /// <summary>Indique si le joueur est sauté ce tour.</summary>
        public bool IsSkipped { get; internal set; }

        /// <summary>La main du joueur (lecture seule).</summary>
        public IReadOnlyList<CardModel> Hand => _hand;

        /// <summary>Les combinaisons posées sur la table (lecture seule).</summary>
        public IReadOnlyList<Meld> LaidMelds => _laidMelds;

        /// <summary>Indique si la main est vide.</summary>
        public bool IsHandEmpty => _hand.Count == 0;

        public PlayerModel(int index, string name, bool isAI)
        {
            Index = index;
            Name = name;
            IsAI = isAI;
            CurrentLevel = 1;
            HasLaidDownThisRound = false;
            IsSkipped = false;
            _hand = new List<CardModel>();
            _laidMelds = new List<Meld>();
        }

        // ────────────────────────────────────────────────────
        //  MUTATION (internal — Core assembly uniquement)
        // ────────────────────────────────────────────────────

        /// <summary>Ajoute une carte à la main.</summary>
        internal void AddToHand(CardModel card)
        {
            _hand.Add(card);
        }

        /// <summary>Retire une carte de la main.</summary>
        internal bool RemoveFromHand(CardModel card)
        {
            return _hand.Remove(card);
        }

        /// <summary>Retire plusieurs cartes de la main.</summary>
        internal void RemoveFromHand(List<CardModel> cards)
        {
            foreach (CardModel card in cards)
            {
                _hand.Remove(card);
            }
        }

        /// <summary>Ajoute une combinaison posée.</summary>
        internal void AddMeld(Meld meld)
        {
            _laidMelds.Add(meld);
        }

        /// <summary>Réinitialise l'état pour un nouveau round (conserve le niveau).</summary>
        internal void ResetForNewRound()
        {
            _hand.Clear();
            _laidMelds.Clear();
            HasLaidDownThisRound = false;
            IsSkipped = false;
        }

        // ────────────────────────────────────────────────────
        //  QUERY (public)
        // ────────────────────────────────────────────────────

        /// <summary>Vérifie si la main contient une carte donnée.</summary>
        public bool HandContains(CardModel card) => _hand.Contains(card);

        /// <summary>Retourne une combinaison posée par index.</summary>
        public Meld GetMeld(int index) => _laidMelds[index];

        /// <summary>
        /// Réordonne une carte dans la main (drag-to-reorder).
        /// Public car c'est cosmétique — n'affecte pas les règles.
        /// </summary>
        public void ReorderHand(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _hand.Count) return;
            if (toIndex < 0 || toIndex >= _hand.Count) return;
            if (fromIndex == toIndex) return;

            CardModel card = _hand[fromIndex];
            _hand.RemoveAt(fromIndex);
            _hand.Insert(toIndex, card);
        }
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

        public Meld(MeldType type, List<CardModel> cards, int ownerIndex)
        {
            Type = type;
            Cards = new List<CardModel>(cards);
            OwnerIndex = ownerIndex;
        }

        /// <summary>
        /// Vérifie si une carte PEUT être ajoutée, sans modifier l'état.
        /// Utiliser pour les previews, le feedback visuel, et les vérifications IA.
        /// </summary>
        public bool CanAddCard(CardModel card)
        {
            List<CardModel> test = new(Cards) { card };
            return Type switch
            {
                MeldType.Run   => CardExtensions.IsValidRun(test),
                MeldType.Set   => CardExtensions.IsValidSet(test),
                MeldType.Flush => CardExtensions.IsValidFlush(test),
                _              => false
            };
        }

        /// <summary>
        /// Tente d'ajouter une carte à la combinaison.
        /// Retourne true si la carte est compatible et a été ajoutée.
        /// </summary>
        public bool TryAddCard(CardModel card)
        {
            if (!CanAddCard(card)) return false;

            Cards.Add(card);
            if (Type == MeldType.Run)
            {
                Cards.Sort((a, b) => a.Value.CompareTo(b.Value));
            }
            return true;
        }
    }
}
