using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.AI
{
    /// <summary>
    /// Niveau de jeu des bots.
    /// Facile : se trompe souvent, ramasse peu, oublie de compléter la table.
    /// Normal : joue son contrat proprement et évite de nourrir la table.
    /// Difficile (« Expert » dans l'UI) : compte les cartes, lit les ramassages
    /// adverses, choisit sa pose et garde ses cartes action pour le bon moment.
    /// </summary>
    public enum AIDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    /// <summary>
    /// Pilote d'un bot : joue son tour phase par phase avec des délais de
    /// "réflexion", en déléguant chaque décision à <see cref="BotBrain"/>.
    ///
    /// Principe fondamental : toute action passe par <see cref="GameManager.ExecuteCommand"/>.
    /// L'IA emprunte exactement le même chemin que le joueur humain —
    /// aucun accès direct en écriture au modèle ou au TurnManager.
    /// </summary>
    public class AIPlayer : MonoBehaviour
    {
        [SerializeField] private float _thinkDelay = 0.55f;
        [SerializeField] private float _actionDelay = 0.35f;

        private GameManager? _gameManager;
        private int _playerIndex = -1;
        private AIDifficulty _difficulty = AIDifficulty.Normal;
        private float _delayFactor = 1f;
        private BotBrain? _brain;
        private Coroutine? _turnRoutine;
        private int _lastDeckCount = -1;

        /// <summary>Siège joué par ce bot.</summary>
        public int PlayerIndex => _playerIndex;

        /// <summary>
        /// Initialise l'IA. <paramref name="delayFactor"/> règle la vitesse de jeu
        /// (1 = normale, &lt; 1 = plus rapide, &gt; 1 = plus posée).
        /// </summary>
        public void Initialize(GameManager gameManager, int playerIndex,
            AIDifficulty difficulty = AIDifficulty.Normal, float delayFactor = 1f, int? seed = null)
        {
            _gameManager = gameManager;
            _playerIndex = playerIndex;
            _difficulty = difficulty;
            _delayFactor = Mathf.Clamp(delayFactor, 0.2f, 3f);
            // Graine imposée (donne du jour) : les bots jouent pareil pour tout le monde.
            int baseSeed = seed ?? System.Environment.TickCount;
            string botName = playerIndex >= 0 && playerIndex < gameManager.Players.Count ? gameManager.Players[playerIndex].Name : "";
            _brain = new BotBrain(gameManager, playerIndex, difficulty,
                unchecked(baseSeed * 31 + playerIndex * 7919), BotPersonas.For(botName));
        }

        private void OnEnable()
        {
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Subscribe<CardDiscardedEvent>(OnCardDiscarded);
            EventBus.Subscribe<CardDrawnEvent>(OnCardDrawn);
            EventBus.Subscribe<DeckChangedEvent>(OnDeckChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Unsubscribe<CardDiscardedEvent>(OnCardDiscarded);
            EventBus.Unsubscribe<CardDrawnEvent>(OnCardDrawn);
            EventBus.Unsubscribe<DeckChangedEvent>(OnDeckChanged);
        }

        // ────────────────────────────────────────────────────
        //  MÉMOIRE PUBLIQUE (ce que tout joueur voit à la table)
        // ────────────────────────────────────────────────────

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            _brain?.OnRoundStarted();
            _lastDeckCount = -1;
        }

        private void OnCardDiscarded(CardDiscardedEvent evt) => _brain?.OnCardDiscarded(evt.PlayerIndex, evt.Card);

        private void OnCardDrawn(CardDrawnEvent evt) =>
            _brain?.OnCardDrawn(evt.PlayerIndex, evt.Card, evt.FromDiscard);

        private void OnDeckChanged(DeckChangedEvent evt)
        {
            // Pioche qui remonte = défausses remélangées : la mémoire est périmée.
            if (_lastDeckCount >= 0 && evt.CardsRemaining > _lastDeckCount) _brain?.OnDeckRecycled();
            _lastDeckCount = evt.CardsRemaining;
        }

        // ────────────────────────────────────────────────────
        //  TOUR
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Réagit au début d'un tour : si c'est le tour de ce bot, joue automatiquement.
        /// </summary>
        private void OnTurnStarted(TurnStartedEvent evt)
        {
            if (evt.PlayerIndex != _playerIndex) return;
            if (_gameManager == null) return;

            PlayerModel? player = _gameManager.GetCurrentPlayer();
            if (player == null || !player.IsAI) return;

            StartTurn();
        }

        /// <summary>
        /// Force le bot à jouer le tour EN COURS s'il lui appartient. Utilisé
        /// quand un humain déconnecté (en ligne) est converti en bot au milieu
        /// de son propre tour : sans ça, la partie resterait bloquée jusqu'au
        /// prochain TurnStartedEvent.
        /// </summary>
        public void PlayTurnIfCurrent()
        {
            if (_gameManager?.TurnManager == null) return;
            if (_gameManager.TurnManager.CurrentPlayerIndex != _playerIndex) return;

            StartTurn();
        }

        private void StartTurn()
        {
            if (_turnRoutine != null) StopCoroutine(_turnRoutine);
            _turnRoutine = StartCoroutine(PlayTurnCoroutine());
        }

        /// <summary>Vrai tant que c'est toujours le même tour de ce bot (même manche).</summary>
        private bool StillMyTurn(int round)
        {
            if (_gameManager?.TurnManager == null) return false;
            if (_gameManager.State != GameState.PlayerTurn) return false;
            if (_gameManager.RoundNumber != round) return false;
            if (_gameManager.TurnManager.CurrentPlayerIndex != _playerIndex) return false;
            PlayerModel? me = _gameManager.GetCurrentPlayer();
            return me != null && me.IsAI;
        }

        private WaitForSeconds Wait(float seconds) => new(seconds * _delayFactor);

        /// <summary>
        /// Joue un tour complet avec des délais pour simuler la réflexion.
        /// Reprend depuis la phase courante : si la pioche est déjà faite
        /// (reprise après déconnexion), on enchaîne sur les phases suivantes.
        /// </summary>
        private IEnumerator PlayTurnCoroutine()
        {
            if (_gameManager?.TurnManager == null || _brain == null) yield break;
            int round = _gameManager.RoundNumber;
            if (!StillMyTurn(round)) yield break;


            // Phase 1 : Piocher (sautée si le tour a déjà dépassé cette phase)
            yield return Wait(_thinkDelay);
            if (StillMyTurn(round) && _gameManager.TurnManager.CurrentPhase == TurnPhase.Draw)
            {
                DecideDraw();
            }

            // Phase 2 : Poser le contrat si possible
            yield return Wait(_actionDelay);
            if (StillMyTurn(round) && _gameManager.TurnManager.CurrentPhase == TurnPhase.LayDown)
            {
                DecideLayDown();
            }

            // Phase 3 : Compléter les combinaisons (une carte à la fois, lisible)
            while (StillMyTurn(round) && _gameManager.TurnManager.CurrentPhase == TurnPhase.AddToMelds)
            {
                yield return Wait(_actionDelay);
                if (!StillMyTurn(round)) break;
                if (!TryAddOneCard())
                {
                    if (StillMyTurn(round) && _gameManager.TurnManager.CurrentPhase == TurnPhase.AddToMelds)
                    {
                        _gameManager.ExecuteCommand(new SkipPhaseCommand(_playerIndex, TurnPhase.AddToMelds));
                    }
                    break;
                }
            }

            // Phase 4 : Défausser
            yield return Wait(_actionDelay);
            if (StillMyTurn(round) && _gameManager.TurnManager.CurrentPhase == TurnPhase.Discard)
            {
                DecideDiscard();
            }

            _turnRoutine = null;
        }

        // ────────────────────────────────────────────────────
        //  DÉCISIONS → COMMANDES
        // ────────────────────────────────────────────────────

        private void DecideDraw()
        {
            if (_gameManager == null || _brain == null) return;

            int pile = _brain.ChooseDrawPile();
            if (pile >= 0)
            {
                CommandResult result = _gameManager.ExecuteCommand(new DrawFromDiscardCommand(_playerIndex, pile));
                if (result.Success) return;
            }
            _gameManager.ExecuteCommand(new DrawFromDeckCommand(_playerIndex));
        }

        private void DecideLayDown()
        {
            if (_gameManager?.TurnManager == null || _brain == null) return;

            List<Meld>? melds = _brain.ChooseLayDown();
            if (melds != null)
            {
                List<Meld> playerMelds = new();
                foreach (Meld m in melds)
                {
                    playerMelds.Add(new Meld(m.Type, new List<CardModel>(m.Cards), _playerIndex));
                }
                if (_gameManager.ExecuteCommand(new LayDownLevelCommand(_playerIndex, playerMelds)).Success)
                {
                    return;
                }
            }

            if (_gameManager.TurnManager.CurrentPhase == TurnPhase.LayDown)
            {
                _gameManager.ExecuteCommand(new SkipPhaseCommand(_playerIndex, TurnPhase.LayDown));
            }
        }

        /// <summary>Pose une carte sur la table si une est jouable ; false sinon.</summary>
        private bool TryAddOneCard()
        {
            if (_gameManager == null || _brain == null) return false;
            if (!_brain.TryChooseAddToMeld(out CardModel card, out int owner, out int meld)) return false;
            return _gameManager.ExecuteCommand(new AddToMeldCommand(_playerIndex, card, owner, meld)).Success;
        }

        private void DecideDiscard()
        {
            if (_gameManager == null || _brain == null) return;

            PlayerModel? me = _gameManager.GetCurrentPlayer();
            if (me == null || me.Hand.Count == 0) return;

            // ExecuteCommand gère automatiquement OnRoundEnd si la main est vide
            DiscardCommand discard = _brain.ChooseDiscard();
            if (_gameManager.ExecuteCommand(discard).Success) return;

            // Filet de sécurité : la décision a échoué (cas imprévu) → fin de tour simple.
            IGameCommand? fallback = TurnAutoPlay.NextCommand(_gameManager);
            if (fallback != null) _gameManager.ExecuteCommand(fallback);
        }
    }
}
