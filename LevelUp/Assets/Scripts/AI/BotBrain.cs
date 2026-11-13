using System.Collections.Generic;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.AI
{
    /// <summary>
    /// Décisions d'un bot, sans MonoBehaviour (testable) : où piocher, quoi poser,
    /// quoi ajouter, quoi défausser et sur qui jouer une carte « Passe » / « +2 ».
    ///
    /// Le bot ne voit QUE l'information publique : sa main, la table, le dessus
    /// des défausses, le nombre de cartes et le niveau de chacun, et (en Expert)
    /// ce que chaque adversaire a défaussé ou ramassé pendant la manche.
    ///
    /// Évaluation d'une main avant la pose : cartes manquantes pour le contrat
    /// (glouton sur toutes les permutations d'exigences) puis "outs" — nombre de
    /// cartes encore invisibles qui réduiraient ce manque. Défausse = la carte
    /// dont le retrait coûte le moins, moins le danger de la donner :
    ///   - carte ajoutable à une combinaison posée alors qu'un adversaire a déjà
    ///     posé (il la ramasse et la pose : danger croissant quand il lui reste
    ///     peu de cartes) ;
    ///   - (Expert) carte proche de ce qu'un adversaire ramasse pour son contrat.
    ///
    /// Difficulté : Facile se trompe et ignore les défausses ; Normal joue
    /// proprement et évite de nourrir la table ; Expert compte les cartes,
    /// lit les ramassages adverses, optimise sa pose et garde ses cartes action.
    /// </summary>
    public sealed class BotBrain
    {
        private const int ColorCount = 6;
        private const float MissingWeight = 100f;
        private const float OutWeight = 3f;

        private readonly GameManager _gm;
        private readonly int _seat;
        private readonly AIDifficulty _difficulty;
        private readonly System.Random _rng;

        private readonly int _minValue;
        private readonly int _maxValue;

        // Composition du paquet [valeur, couleur] et nombre de jokers.
        private readonly int[,] _deckCounts;
        private readonly int _deckWilds;

        // Mémoire publique de la manche (utilisée en Expert).
        private readonly List<CardModel> _seenDiscards = new();
        private readonly Dictionary<int, List<CardModel>> _picksByPlayer = new();

        // Tampons réutilisés : aucune allocation dans les boucles d'évaluation.
        private readonly int[,] _counts;
        private readonly int[,] _work;
        private readonly int[,] _unseen;
        private readonly HashSet<int> _removedIds = new();
        private int _wilds;
        private int _unseenWilds;
        private int[] _perm = new int[4];

        public BotBrain(GameManager gameManager, int seat, AIDifficulty difficulty, int seed)
        {
            _gm = gameManager;
            _seat = seat;
            _difficulty = difficulty;
            _rng = new System.Random(seed);

            GameConfig? config = gameManager.Config;
            _minValue = config != null ? config.CardMinValue : Constants.DefaultCardMin;
            _maxValue = config != null ? config.CardMaxValue : Constants.DefaultCardMax;

            _deckCounts = new int[_maxValue + 2, ColorCount];
            _counts = new int[_maxValue + 2, ColorCount];
            _work = new int[_maxValue + 2, ColorCount];
            _unseen = new int[_maxValue + 2, ColorCount];

            if (config != null)
            {
                foreach (CardModel c in DeckManager.BuildComposition(config, gameManager.Rules))
                {
                    if (c.Type == CardType.Normal && InRange(c.Value)) _deckCounts[c.Value, (int)c.Color]++;
                    else if (c.IsWild) _deckWilds++;
                }
            }
        }

        public AIDifficulty Difficulty => _difficulty;

        // ═══════════════════════════════════════════════════════════
        //  MÉMOIRE (événements publics)
        // ═══════════════════════════════════════════════════════════

        public void OnRoundStarted()
        {
            _seenDiscards.Clear();
            _picksByPlayer.Clear();
        }

        public void OnCardDiscarded(int playerIndex, CardModel card)
        {
            _ = playerIndex;
            _seenDiscards.Add(card);
        }

        public void OnCardDrawn(int playerIndex, CardModel card, bool fromDiscard)
        {
            if (!fromDiscard || playerIndex == _seat) return;
            if (!_picksByPlayer.TryGetValue(playerIndex, out List<CardModel>? picks))
            {
                picks = new List<CardModel>();
                _picksByPlayer[playerIndex] = picks;
            }
            picks.Add(card);
        }

        /// <summary>Les défausses ont été remélangées dans la pioche : ces cartes redeviennent inconnues.</summary>
        public void OnDeckRecycled()
        {
            _seenDiscards.Clear();
        }

        /// <summary>Cartes manquantes d'une main pour le contrat d'un niveau (outil de test / debug).</summary>
        public int MissingFor(IReadOnlyList<CardModel> hand, int level)
        {
            List<LevelValidator.LevelRequirement> reqs = Requirements(level);
            LoadCounts(hand, -1, null);
            return Missing(reqs);
        }

        // ═══════════════════════════════════════════════════════════
        //  PIOCHE
        // ═══════════════════════════════════════════════════════════

        /// <summary>Index de la défausse à ramasser, ou -1 pour piocher au deck.</summary>
        public int ChooseDrawPile()
        {
            DeckManager? deck = _gm.DeckManager;
            if (deck == null) return -1;
            if (_difficulty == AIDifficulty.Easy && _rng.NextDouble() < 0.5) return -1;

            PlayerModel me = Me;
            IReadOnlyList<CardModel> hand = me.Hand;
            List<LevelValidator.LevelRequirement> reqs = Requirements(me.CurrentLevel);

            int bestPile = -1;
            float bestGain = 0f;

            float deckGain = 0f;
            int baseMissing = 0;
            if (!me.HasLaidDownThisRound && reqs.Count > 0)
            {
                LoadCounts(hand, -1, null);
                baseMissing = Missing(reqs);
                if (_difficulty == AIDifficulty.Hard && baseMissing > 0)
                {
                    BuildUnseen(hand);
                    int total = UnseenTotal();
                    int outs = CountOuts(reqs, baseMissing);
                    deckGain = total > 0 ? MissingWeight * outs / total : 0f;
                }
            }

            for (int i = 0; i < deck.DiscardPileCount; i++)
            {
                CardModel? top = deck.PeekDiscard(i);
                if (!top.HasValue) continue;
                CardModel card = top.Value;
                if (card.IsAction && !card.IsWild) continue;

                float gain;
                if (me.HasLaidDownThisRound)
                {
                    // Après la pose : une carte immédiatement jouable = une carte de moins.
                    gain = card.IsWild || CanAddToTable(card) ? MissingWeight : 0f;
                }
                else if (card.IsWild)
                {
                    gain = baseMissing > 0 ? MissingWeight : 0f;
                }
                else
                {
                    if (reqs.Count == 0) continue;
                    int after = BestMissingAfterTaking(hand, card, reqs);
                    gain = (baseMissing - after) * MissingWeight;

                    // Facile ne ramasse que ce qui termine son contrat.
                    if (_difficulty == AIDifficulty.Easy && after > 0) gain = 0f;
                }

                float threshold = _difficulty == AIDifficulty.Hard ? deckGain + 5f : MissingWeight - 1f;
                if (gain > threshold && gain > bestGain)
                {
                    bestGain = gain;
                    bestPile = i;
                }
            }
            return bestPile;
        }

        /// <summary>Manque minimal après avoir pris <paramref name="taken"/> et défaussé la pire carte.</summary>
        private int BestMissingAfterTaking(IReadOnlyList<CardModel> hand, CardModel taken,
            List<LevelValidator.LevelRequirement> reqs)
        {
            int best = int.MaxValue;
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].IsWild) continue;
                LoadCounts(hand, hand[i].Id, taken);
                int m = Missing(reqs);
                if (m < best) best = m;
            }
            if (best == int.MaxValue)
            {
                LoadCounts(hand, -1, taken);
                best = Missing(reqs);
            }
            return best;
        }

        // ═══════════════════════════════════════════════════════════
        //  POSE
        // ═══════════════════════════════════════════════════════════

        /// <summary>Combinaisons à poser (null : contrat impossible ou déjà posé).</summary>
        public List<Meld>? ChooseLayDown()
        {
            PlayerModel me = Me;
            if (me.HasLaidDownThisRound) return null;

            int maxOptions = _difficulty == AIDifficulty.Hard ? 6 : 1;
            List<List<Meld>> options = LevelValidator.FindLayDownOptions(me.Hand, me.CurrentLevel,
                _gm.Config, maxOptions);
            if (options.Count == 0) return null;
            if (options.Count == 1 || _difficulty != AIDifficulty.Hard) return options[0];

            // Expert : la pose qui laisse en main le plus de cartes jouables
            // ensuite (sur la table ou sur ses propres combinaisons), jokers gardés.
            List<Meld> best = options[0];
            float bestScore = float.MinValue;
            List<CardModel> remaining = new();
            foreach (List<Meld> option in options)
            {
                remaining.Clear();
                remaining.AddRange(me.Hand);
                foreach (Meld m in option)
                {
                    foreach (CardModel c in m.Cards) remaining.Remove(c);
                }

                float score = 0f;
                foreach (CardModel c in remaining)
                {
                    if (c.IsWild) score += 20f;
                    else if (CanAddTo(option, c) || CanAddToTable(c)) score += 30f;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = option;
                }
            }
            return best;
        }

        private static bool CanAddTo(List<Meld> melds, CardModel card)
        {
            foreach (Meld m in melds)
            {
                if (m.CanAddCard(card)) return true;
            }
            return false;
        }

        // ═══════════════════════════════════════════════════════════
        //  AJOUTS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Une carte de la main à poser sur une combinaison, s'il y en a une.
        /// Les cartes numérotées passent avant les jokers (un joker se pose
        /// partout : il reste une sortie garantie pour plus tard).
        /// </summary>
        public bool TryChooseAddToMeld(out CardModel card, out int ownerIndex, out int meldIndex)
        {
            card = default;
            ownerIndex = -1;
            meldIndex = -1;

            PlayerModel me = Me;
            if (!me.HasLaidDownThisRound) return false;
            if (_difficulty == AIDifficulty.Easy && _rng.NextDouble() < 0.3) return false;

            for (int pass = 0; pass < 2; pass++)
            {
                bool wildsPass = pass == 1;
                foreach (CardModel c in me.Hand)
                {
                    if (c.IsWild != wildsPass) continue;
                    if (c.IsAction && !c.IsWild) continue;
                    foreach (PlayerModel owner in _gm.Players)
                    {
                        for (int m = 0; m < owner.LaidMelds.Count; m++)
                        {
                            if (!owner.LaidMelds[m].CanAddCard(c)) continue;
                            card = c;
                            ownerIndex = owner.Index;
                            meldIndex = m;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // ═══════════════════════════════════════════════════════════
        //  DÉFAUSSE
        // ═══════════════════════════════════════════════════════════

        /// <summary>Carte à défausser et cible éventuelle (cartes « Passe » / « +2 »).</summary>
        public DiscardCommand ChooseDiscard()
        {
            PlayerModel me = Me;
            IReadOnlyList<CardModel> hand = me.Hand;
            List<LevelValidator.LevelRequirement> reqs = Requirements(me.CurrentLevel);
            bool laid = me.HasLaidDownThisRound;

            bool onlyWilds = true;
            foreach (CardModel c in hand)
            {
                if (!c.IsWild) { onlyWilds = false; break; }
            }

            int urgentTarget = FindUrgentThreat();

            // Évaluation de chaque défausse possible : (score, carte)
            int count = hand.Count;
            float[] scores = new float[count];
            int[] missings = new int[count];
            int bestMissing = int.MaxValue;

            for (int i = 0; i < count; i++)
            {
                CardModel c = hand[i];
                if (c.IsWild && !onlyWilds)
                {
                    scores[i] = float.MinValue;
                    missings[i] = int.MaxValue;
                    continue;
                }

                if (laid || reqs.Count == 0)
                {
                    missings[i] = 0;
                    scores[i] = -KeepValueAfterLayDown(c);
                }
                else
                {
                    LoadCounts(hand, c.Id, null);
                    missings[i] = Missing(reqs);
                    scores[i] = -missings[i] * MissingWeight;
                }
                if (missings[i] < bestMissing) bestMissing = missings[i];
            }

            // Outs (Normal/Expert) : départage les défausses de même manque.
            if (!laid && reqs.Count > 0 && _difficulty != AIDifficulty.Easy && bestMissing > 0)
            {
                BuildUnseen(hand);
                for (int i = 0; i < count; i++)
                {
                    if (missings[i] != bestMissing || scores[i] == float.MinValue) continue;
                    LoadCounts(hand, hand[i].Id, null);
                    scores[i] += OutWeight * CountOuts(reqs, bestMissing);
                }
            }

            int bestIndex = 0;
            float bestScore = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                if (scores[i] == float.MinValue) continue;
                CardModel c = hand[i];
                float s = scores[i];

                if (c.IsAction && !c.IsWild)
                {
                    s += ActionDiscardBonus(urgentTarget >= 0);
                }
                else
                {
                    s -= Danger(c);
                }

                if (_difficulty == AIDifficulty.Easy) s += (float)(_rng.NextDouble() * 120.0 - 60.0);
                else if (_difficulty == AIDifficulty.Normal) s += (float)(_rng.NextDouble() * 6.0 - 3.0);

                // À score égal : défausser la plus haute (moins de "valeur" à garder).
                if (s > bestScore || (s == bestScore && c.Value > hand[bestIndex].Value))
                {
                    bestScore = s;
                    bestIndex = i;
                }
            }

            CardModel discard = hand[bestIndex];
            int target = -1;
            if (discard.IsAction && !discard.IsWild)
            {
                target = urgentTarget >= 0 ? urgentTarget : ChooseTarget(discard.Type);
            }
            return new DiscardCommand(_seat, discard, target);
        }

        /// <summary>
        /// Valeur de garder une carte après sa pose : les jokers sont toujours
        /// jouables, une carte à deux crans d'une suite posée peut le devenir.
        /// </summary>
        private float KeepValueAfterLayDown(CardModel c)
        {
            if (c.IsWild) return 1000f;
            if (c.IsAction) return -5f;
            if (CanAddToTable(c)) return 200f;
            return IsNearTableRun(c) ? 15f : 0f;
        }

        /// <summary>
        /// Bonus de défausse d'une carte « Passe » / « +2 ». Menace urgente : on
        /// la joue. Sinon Normal s'en débarrasse, Expert la garde pour le bon
        /// moment s'il a une autre carte inutile à défausser.
        /// </summary>
        private float ActionDiscardBonus(bool urgent)
        {
            if (urgent && _difficulty != AIDifficulty.Easy) return 400f;
            return _difficulty == AIDifficulty.Hard ? -8f : 20f;
        }

        /// <summary>
        /// Danger de donner cette carte : un adversaire pourra la ramasser dans
        /// la défausse (toutes les piles sont accessibles) avant notre prochain tour.
        /// </summary>
        public float Danger(CardModel card)
        {
            if (_difficulty == AIDifficulty.Easy || card.IsAction) return 0f;

            float danger = 0f;
            IReadOnlyList<PlayerModel> players = _gm.Players;
            int next = NextOpponent();

            // 1. Carte jouable sur la table alors qu'un adversaire a posé.
            if (CanAddToTable(card))
            {
                foreach (PlayerModel o in players)
                {
                    if (o.Index == _seat || !o.HasLaidDownThisRound) continue;
                    int n = o.Hand.Count;
                    float d = n <= 1 ? 450f : n <= 2 ? 190f : n <= 4 ? 95f : 55f;
                    if (o.Index == next) d *= 1.15f;
                    if (_difficulty == AIDifficulty.Hard) d *= 1.2f;
                    if (d > danger) danger = d;
                }
            }

            // 2. (Expert) Carte proche de ce qu'un adversaire ramasse pour son contrat.
            if (_difficulty == AIDifficulty.Hard && card.Type == CardType.Normal)
            {
                foreach (KeyValuePair<int, List<CardModel>> entry in _picksByPlayer)
                {
                    if (entry.Key < 0 || entry.Key >= players.Count) continue;
                    PlayerModel o = players[entry.Key];
                    if (o.HasLaidDownThisRound) continue;

                    List<LevelValidator.LevelRequirement> theirs = Requirements(o.CurrentLevel);
                    float d = 0f;
                    foreach (CardModel pick in entry.Value)
                    {
                        if (pick.Type != CardType.Normal) continue;
                        foreach (LevelValidator.LevelRequirement r in theirs)
                        {
                            if (r.Type == MeldType.Set && pick.Value == card.Value) d += 45f;
                            else if (r.Type == MeldType.Flush && pick.Color == card.Color) d += 14f;
                            else if (r.Type == MeldType.Run && pick.Value != card.Value
                                     && System.Math.Abs(pick.Value - card.Value) <= 2) d += 12f;
                        }
                    }
                    if (o.Index == next) d *= 1.3f;
                    danger += System.Math.Min(d, 120f);
                }
            }

            return danger;
        }

        // ═══════════════════════════════════════════════════════════
        //  CIBLES DES CARTES ACTION
        // ═══════════════════════════════════════════════════════════

        /// <summary>Adversaire sur le point de sortir (posé, 1–2 cartes ; 3 pour Expert), sinon -1.</summary>
        private int FindUrgentThreat()
        {
            int limit = _difficulty == AIDifficulty.Hard ? 3 : 2;
            int best = -1;
            int bestCount = int.MaxValue;
            foreach (PlayerModel o in _gm.Players)
            {
                if (o.Index == _seat || !o.HasLaidDownThisRound) continue;
                if (o.Hand.Count <= limit && o.Hand.Count < bestCount)
                {
                    best = o.Index;
                    bestCount = o.Hand.Count;
                }
            }
            return best;
        }

        /// <summary>Adversaire le plus menaçant : posé, peu de cartes, niveau élevé.</summary>
        public int ChooseTarget(CardType type)
        {
            IReadOnlyList<PlayerModel> players = _gm.Players;
            if (_difficulty == AIDifficulty.Easy)
            {
                int pick = _rng.Next(players.Count - 1);
                return pick >= _seat ? pick + 1 : pick;
            }

            int next = NextOpponent();
            int best = -1;
            float bestScore = float.MinValue;
            foreach (PlayerModel o in players)
            {
                if (o.Index == _seat) continue;
                float s = (o.HasLaidDownThisRound ? 60f : 0f)
                          + (12 - o.Hand.Count) * 8f
                          + o.CurrentLevel * 6f
                          + (o.Index == next ? 4f : 0f);
                if (type == CardType.Skip && o.SkipCount > 0) s -= 40f;
                if (s > bestScore)
                {
                    bestScore = s;
                    best = o.Index;
                }
            }
            return best;
        }

        // ═══════════════════════════════════════════════════════════
        //  TABLE
        // ═══════════════════════════════════════════════════════════

        private PlayerModel Me => _gm.Players[_seat];

        private int NextOpponent() => (_seat + 1) % _gm.Players.Count;

        /// <summary>Vrai si la carte peut compléter une combinaison posée (n'importe quel joueur).</summary>
        public bool CanAddToTable(CardModel card)
        {
            foreach (PlayerModel p in _gm.Players)
            {
                foreach (Meld m in p.LaidMelds)
                {
                    if (m.CanAddCard(card)) return true;
                }
            }
            return false;
        }

        /// <summary>Carte à deux crans d'une suite posée (jouable dès qu'on la prolonge).</summary>
        private bool IsNearTableRun(CardModel card)
        {
            if (card.Type != CardType.Normal) return false;
            foreach (PlayerModel p in _gm.Players)
            {
                foreach (Meld m in p.LaidMelds)
                {
                    if (m.Type != MeldType.Run) continue;
                    int min = int.MaxValue;
                    foreach (CardModel c in m.Cards)
                    {
                        if (c.Type == CardType.Normal && c.Value < min) min = c.Value;
                    }
                    if (min == int.MaxValue) continue;
                    int max = min + m.Cards.Count - 1;
                    if (card.Value == min - 2 || card.Value == max + 2) return true;
                }
            }
            return false;
        }

        private List<LevelValidator.LevelRequirement> Requirements(int level)
        {
            List<List<LevelValidator.LevelRequirement>> sets = LevelValidator.GetRequirements(level, _gm.Config);
            return sets.Count > 0 ? sets[0] : new List<LevelValidator.LevelRequirement>();
        }

        // ═══════════════════════════════════════════════════════════
        //  ÉVALUATION (sans allocation)
        // ═══════════════════════════════════════════════════════════

        private bool InRange(int v) => v >= _minValue && v <= _maxValue;

        /// <summary>Charge la main dans le tableau de comptes (sans <paramref name="excludeId"/>, avec <paramref name="extra"/>).</summary>
        private void LoadCounts(IReadOnlyList<CardModel> hand, int excludeId, CardModel? extra)
        {
            System.Array.Clear(_counts, 0, _counts.Length);
            _wilds = 0;
            foreach (CardModel c in hand)
            {
                if (c.Id == excludeId) continue;
                AddToCounts(c);
            }
            if (extra.HasValue) AddToCounts(extra.Value);
        }

        private void AddToCounts(CardModel c)
        {
            if (c.IsWild) _wilds++;
            else if (c.Type == CardType.Normal && InRange(c.Value)) _counts[c.Value, (int)c.Color]++;
        }

        /// <summary>
        /// Cartes manquantes pour le contrat (estimation gloutonne, meilleure
        /// sur toutes les permutations d'exigences, jokers en complément).
        /// </summary>
        private int Missing(List<LevelValidator.LevelRequirement> reqs)
        {
            int n = reqs.Count;
            if (n == 0) return 0;
            if (_perm.Length < n) _perm = new int[n];
            for (int i = 0; i < n; i++) _perm[i] = i;
            // Au-delà de 4 exigences (contrats personnalisés), l'ordre seul suffit.
            bool permute = n <= 4;

            int best = int.MaxValue;
            do
            {
                System.Array.Copy(_counts, _work, _counts.Length);
                int deficit = 0;
                for (int k = 0; k < n; k++)
                {
                    LevelValidator.LevelRequirement r = reqs[_perm[k]];
                    deficit += r.CardCount - TakeBest(r);
                }
                int missing = deficit - _wilds;
                if (missing < 0) missing = 0;
                if (missing < best) best = missing;
            }
            while (permute && best > 0 && NextPermutation(_perm, n));

            return best;
        }

        /// <summary>Prend dans _work le meilleur groupe pour l'exigence et retourne le nombre de cartes trouvées.</summary>
        private int TakeBest(LevelValidator.LevelRequirement r)
        {
            int need = r.CardCount;
            switch (r.Type)
            {
                case MeldType.Run:
                {
                    int bestStart = -1, bestHave = 0;
                    for (int s = _minValue; s + need - 1 <= _maxValue; s++)
                    {
                        int have = 0;
                        for (int v = s; v < s + need; v++)
                        {
                            if (ValueCount(v) > 0) have++;
                        }
                        if (have > bestHave)
                        {
                            bestHave = have;
                            bestStart = s;
                        }
                    }
                    if (bestStart < 0) return 0;
                    for (int v = bestStart; v < bestStart + need; v++) RemoveOneOfValue(v);
                    return bestHave;
                }
                case MeldType.Set:
                {
                    int bestValue = -1, bestHave = 0;
                    for (int v = _minValue; v <= _maxValue; v++)
                    {
                        int have = ValueCount(v);
                        if (have > bestHave)
                        {
                            bestHave = have;
                            bestValue = v;
                        }
                    }
                    if (bestValue < 0) return 0;
                    int take = bestHave < need ? bestHave : need;
                    for (int i = 0; i < take; i++) RemoveOneOfValue(bestValue);
                    return take;
                }
                case MeldType.Flush:
                {
                    int bestColor = -1, bestHave = 0;
                    for (int c = 0; c < ColorCount; c++)
                    {
                        int have = 0;
                        for (int v = _minValue; v <= _maxValue; v++) have += _work[v, c];
                        if (have > bestHave)
                        {
                            bestHave = have;
                            bestColor = c;
                        }
                    }
                    if (bestColor < 0) return 0;
                    int take = bestHave < need ? bestHave : need;
                    // Garde les valeurs centrales pour une éventuelle suite : retire
                    // d'abord les extrêmes (haut, puis bas, en se rapprochant du centre).
                    int lo = _minValue, hi = _maxValue, taken = 0;
                    while (taken < take && lo <= hi)
                    {
                        if (_work[hi, bestColor] > 0) { _work[hi, bestColor]--; taken++; continue; }
                        if (_work[lo, bestColor] > 0) { _work[lo, bestColor]--; taken++; continue; }
                        hi--;
                        lo++;
                    }
                    return taken;
                }
            }
            return 0;
        }

        private int ValueCount(int v)
        {
            int n = 0;
            for (int c = 0; c < ColorCount; c++) n += _work[v, c];
            return n;
        }

        private void RemoveOneOfValue(int v)
        {
            // La couleur la plus fournie de cette valeur (préserve les couleurs rares).
            int bestC = -1, bestN = 0;
            for (int c = 0; c < ColorCount; c++)
            {
                if (_work[v, c] > bestN)
                {
                    bestN = _work[v, c];
                    bestC = c;
                }
            }
            if (bestC >= 0) _work[v, bestC]--;
        }

        private static bool NextPermutation(int[] a, int n)
        {
            int i = n - 2;
            while (i >= 0 && a[i] >= a[i + 1]) i--;
            if (i < 0) return false;
            int j = n - 1;
            while (a[j] <= a[i]) j--;
            (a[i], a[j]) = (a[j], a[i]);
            for (int l = i + 1, r = n - 1; l < r; l++, r--) (a[l], a[r]) = (a[r], a[l]);
            return true;
        }

        /// <summary>
        /// Cartes encore invisibles pour ce bot : le paquet moins sa main, la
        /// table, le dessus des défausses — et en Expert tout ce qui a été
        /// défaussé pendant la manche (déjà sorti du jeu ou dans une main adverse).
        /// </summary>
        private void BuildUnseen(IReadOnlyList<CardModel> hand)
        {
            System.Array.Copy(_deckCounts, _unseen, _deckCounts.Length);
            _unseenWilds = _deckWilds;
            _removedIds.Clear();

            foreach (CardModel c in hand) RemoveUnseen(c);
            foreach (PlayerModel p in _gm.Players)
            {
                foreach (Meld m in p.LaidMelds)
                {
                    foreach (CardModel c in m.Cards) RemoveUnseen(c);
                }
            }

            DeckManager? deck = _gm.DeckManager;
            if (deck != null)
            {
                for (int i = 0; i < deck.DiscardPileCount; i++)
                {
                    CardModel? top = deck.PeekDiscard(i);
                    if (top.HasValue) RemoveUnseen(top.Value);
                }
            }

            // Expert : se souvient de tout ce qui est passé par les défausses.
            if (_difficulty == AIDifficulty.Hard)
            {
                foreach (CardModel c in _seenDiscards) RemoveUnseen(c);
            }
        }

        private void RemoveUnseen(CardModel c)
        {
            if (!_removedIds.Add(c.Id)) return;
            if (c.IsWild)
            {
                if (_unseenWilds > 0) _unseenWilds--;
            }
            else if (c.Type == CardType.Normal && InRange(c.Value) && _unseen[c.Value, (int)c.Color] > 0)
            {
                _unseen[c.Value, (int)c.Color]--;
            }
        }

        private int UnseenTotal()
        {
            int n = _unseenWilds;
            for (int v = _minValue; v <= _maxValue; v++)
            {
                for (int c = 0; c < ColorCount; c++) n += _unseen[v, c];
            }
            return n;
        }

        /// <summary>
        /// Cartes invisibles qui réduiraient le manque de la main chargée dans
        /// _counts (chaque joker compte toujours tant qu'il manque des cartes).
        /// </summary>
        private int CountOuts(List<LevelValidator.LevelRequirement> reqs, int baseMissing)
        {
            if (baseMissing <= 0) return 0;
            int outs = _unseenWilds;
            for (int v = _minValue; v <= _maxValue; v++)
            {
                for (int c = 0; c < ColorCount; c++)
                {
                    int copies = _unseen[v, c];
                    if (copies == 0) continue;
                    _counts[v, c]++;
                    if (Missing(reqs) < baseMissing) outs += copies;
                    _counts[v, c]--;
                }
            }
            return outs;
        }
    }
}
