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

        /// <summary>
        /// Indique si ce joueur est contrôlé par l'IA. Setter internal : en ligne,
        /// un humain déconnecté est reconverti en bot par <see cref="GameManager.ConvertPlayerToAI"/>.
        /// </summary>
        public bool IsAI { get; internal set; }

        /// <summary>Niveau actuel du joueur (1-8).</summary>
        public int CurrentLevel { get; internal set; }

        /// <summary>Indique si le joueur a posé son niveau ce round.</summary>
        public bool HasLaidDownThisRound { get; internal set; }

        /// <summary>
        /// Nombre de tours que ce joueur doit sauter. Empilable : si plusieurs cartes
        /// Skip ciblent le même joueur, le compteur s'accumule et chaque tour à venir
        /// décrémente jusqu'à zéro. Remplace l'ancien <c>IsSkipped</c> qui écrasait.
        /// </summary>
        public int SkipCount { get; internal set; }

        /// <summary>Compat : indique si le joueur sera sauté au moins une fois.</summary>
        public bool IsSkipped => SkipCount > 0;

        /// <summary>
        /// Points de pénalité cumulés sur la partie : valeur des cartes restées en
        /// main à chaque fin de manche (cf. <see cref="PenaltyFor"/>). Ne décide pas
        /// du vainqueur (ce sont les niveaux) : départage le classement à niveau
        /// égal et donne un enjeu aux joueurs distancés — moins c'est mieux.
        /// </summary>
        public int PenaltyPoints { get; internal set; }

        /// <summary>Pénalité d'une carte restée en main : 5 (1–9), 10 (10–18), 15 (Passe / +2), 25 (joker).</summary>
        public static int PenaltyFor(CardModel card)
        {
            if (card.IsWild) return 25;
            if (card.IsAction) return 15;
            return card.Value >= 10 ? 10 : 5;
        }

        /// <summary>Pénalité totale de la main actuelle.</summary>
        public int HandPenalty()
        {
            int total = 0;
            foreach (CardModel card in _hand) total += PenaltyFor(card);
            return total;
        }

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
            SkipCount = 0;
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
            SkipCount = 0;
        }

        // ────────────────────────────────────────────────────
        //  QUERY (public)
        // ────────────────────────────────────────────────────

        /// <summary>Vérifie si la main contient une carte donnée.</summary>
        public bool HandContains(CardModel card) => _hand.Contains(card);

        /// <summary>
        /// Retrouve dans la main la carte portant cet identifiant — la vraie,
        /// avec sa valeur et sa couleur d'origine. L'exécuteur s'en sert pour ne
        /// jamais se fier aux champs d'une carte reçue du réseau.
        /// </summary>
        public bool TryGetHandCard(int cardId, out CardModel card)
        {
            for (int i = 0; i < _hand.Count; i++)
            {
                if (_hand[i].Id != cardId) continue;
                card = _hand[i];
                return true;
            }
            card = default;
            return false;
        }

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

        /// <summary>
        /// Trie la main selon un comparateur. Purement cosmétique.
        /// </summary>
        public void SortHand(System.Comparison<CardModel> comparator)
        {
            _hand.Sort(comparator);
        }

        /// <summary>Comparateur par valeur croissante (actions en fin, tiebreak par couleur).</summary>
        public static readonly System.Comparison<CardModel> CompareByValue = (a, b) =>
        {
            if (a.IsAction != b.IsAction) return a.IsAction ? 1 : -1;
            int v = a.Value.CompareTo(b.Value);
            return v != 0 ? v : ((int)a.Color).CompareTo((int)b.Color);
        };

        /// <summary>Comparateur par couleur (tiebreak par valeur croissante). Les Wild finissent en dernier.</summary>
        public static readonly System.Comparison<CardModel> CompareByColor = (a, b) =>
        {
            int c = ((int)a.Color).CompareTo((int)b.Color);
            return c != 0 ? c : a.Value.CompareTo(b.Value);
        };
    }

    /// <summary>
    /// Représente une combinaison posée sur la table (suite, brelan, flush, etc.).
    /// </summary>
    public class Meld
    {
        /// <summary>Type de la combinaison.</summary>
        public MeldType Type { get; }

        /// <summary>Cartes composant la combinaison.</summary>
        public IReadOnlyList<CardModel> Cards => _cards;

        private readonly List<CardModel> _cards;

        /// <summary>Index du joueur propriétaire.</summary>
        public int OwnerIndex { get; }

        public Meld(MeldType type, IReadOnlyList<CardModel> cards, int ownerIndex)
        {
            Type = type;
            _cards = new List<CardModel>(cards);
            OwnerIndex = ownerIndex;
        }

        /// <summary>
        /// Vérifie si une carte PEUT être ajoutée, sans modifier l'état.
        /// Utiliser pour les previews, le feedback visuel, et les vérifications IA.
        /// </summary>
        public bool CanAddCard(CardModel card)
        {
            return Type switch
            {
                MeldType.Run   => CardExtensions.IsValidRun(Cards, card),
                MeldType.Set   => CardExtensions.IsValidSet(Cards, card),
                MeldType.Flush => CardExtensions.IsValidFlush(Cards, card),
                _              => false
            };
        }

        /// <summary>
        /// Tente d'ajouter une carte à la combinaison.
        /// Retourne true si la carte est compatible et a été ajoutée.
        /// Le tri post-ajout dépend du type :
        /// <list type="bullet">
        /// <item><description><c>Run</c> : ordonne par valeur croissante en plaçant les
        /// wilds dans les trous de la séquence (ou en extension à la fin).</description></item>
        /// <item><description><c>Set</c> / <c>Flush</c> : pas de tri — on évite de
        /// remettre les wilds (Value négative) en début de liste, et on garde
        /// l'ordre logique de la pose.</description></item>
        /// </list>
        /// </summary>
        public bool TryAddCard(CardModel card)
        {
            if (!CanAddCard(card)) return false;

            _cards.Add(card);
            if (Type == MeldType.Run)
            {
                SortRunCards(_cards);
            }
            return true;
        }

        /// <summary>
        /// Ordre d'affichage d'une combinaison : suite rangée avec les jokers dans
        /// les trous (comme sur la table du modèle), série / couleur par valeur
        /// croissante avec les jokers à la fin.
        /// </summary>
        public static void ArrangeForDisplay(List<CardModel> cards, MeldType type)
        {
            if (type == MeldType.Run && CardExtensions.IsValidRun(cards))
            {
                SortRunCards(cards);
                return;
            }
            cards.Sort((a, b) =>
            {
                if (a.IsWild != b.IsWild) return a.IsWild ? 1 : -1;
                int v = a.Value.CompareTo(b.Value);
                return v != 0 ? v : a.Id.CompareTo(b.Id);
            });
        }

        /// <summary>
        /// Réordonne les cartes d'un Run pour que :
        /// 1. les normales soient triées par Value croissante,
        /// 2. les wilds soient placés dans les trous (positions manquantes
        ///    dans la séquence), puis en extension à la fin si surplus.
        ///
        /// Pré-condition : les cartes forment un Run valide (vérifié en amont
        /// par <see cref="CanAddCard"/>). On ne tente pas de gérer un input
        /// dégénéré ici.
        /// </summary>
        private static void SortRunCards(List<CardModel> cards)
        {
            // Sépare normales et wilds.
            List<CardModel> normals = new(cards.Count);
            List<CardModel> wilds = new(cards.Count);
            foreach (CardModel c in cards)
            {
                if (c.IsWild) wilds.Add(c);
                else normals.Add(c);
            }

            // Cas dégénéré : que des wilds, l'ordre relatif n'a pas d'importance.
            if (normals.Count == 0) return;

            normals.Sort((a, b) => a.Value.CompareTo(b.Value));

            int totalLength = cards.Count;
            int minVal = normals[0].Value;

            // Sortie ordonnée : on parcourt [minVal .. minVal+totalLength-1]
            // et on place une normale si elle existe à cette valeur, sinon un wild.
            cards.Clear();
            int normalIdx = 0;
            int wildIdx = 0;
            for (int v = minVal; v < minVal + totalLength; v++)
            {
                if (normalIdx < normals.Count && normals[normalIdx].Value == v)
                {
                    cards.Add(normals[normalIdx]);
                    normalIdx++;
                }
                else if (wildIdx < wilds.Count)
                {
                    cards.Add(wilds[wildIdx]);
                    wildIdx++;
                }
                else
                {
                    // Sécurité : ne devrait jamais arriver si le run est valide.
                    // On rajoute la prochaine normale s'il en reste pour ne pas
                    // perdre une carte ; la liste en ressort cohérente même si
                    // la séquence affichée saute une valeur.
                    if (normalIdx < normals.Count)
                    {
                        cards.Add(normals[normalIdx]);
                        normalIdx++;
                    }
                }
            }

            // Si des cartes n'ont pas pu être placées dans la fenêtre (cas pathologique),
            // on les append pour ne rien perdre.
            while (normalIdx < normals.Count) cards.Add(normals[normalIdx++]);
            while (wildIdx < wilds.Count) cards.Add(wilds[wildIdx++]);
        }
    }
}
