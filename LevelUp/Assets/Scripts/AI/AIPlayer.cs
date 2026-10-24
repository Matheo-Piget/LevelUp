using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.AI
{
    /// <summary>
    /// Niveau de jeu des bots. Facile fait des erreurs d'évaluation et ignore
    /// les défausses ; Difficile ramasse plus agressivement les cartes utiles.
    /// </summary>
    public enum AIDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    /// <summary>
    /// Intelligence artificielle heuristique pour un joueur bot.
    /// Évalue la main, décide de poser le niveau, choisit la meilleure défausse.
    ///
    /// Principe fondamental : toute action passe par <see cref="GameManager.ExecuteCommand"/>.
    /// L'IA emprunte exactement le même chemin que le joueur humain —
    /// aucun accès direct en écriture au modèle ou au TurnManager.
    /// </summary>
    public class AIPlayer : MonoBehaviour
    {
        [SerializeField] private float _thinkDelay = 0.5f;
        [SerializeField] private float _actionDelay = 0.3f;

        private GameManager? _gameManager;
        private int _playerIndex = -1;
        private AIDifficulty _difficulty = AIDifficulty.Normal;

        /// <summary>Siège joué par ce bot.</summary>
        public int PlayerIndex => _playerIndex;

        /// <summary>
        /// Initialise l'IA avec les références nécessaires.
        /// </summary>
        public void Initialize(GameManager gameManager, int playerIndex,
            AIDifficulty difficulty = AIDifficulty.Normal)
        {
            _gameManager = gameManager;
            _playerIndex = playerIndex;
            _difficulty = difficulty;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
        }

        /// <summary>
        /// Réagit au début d'un tour : si c'est le tour de ce bot, joue automatiquement.
        /// </summary>
        private void OnTurnStarted(TurnStartedEvent evt)
        {
            if (evt.PlayerIndex != _playerIndex) return;
            if (_gameManager == null) return;

            PlayerModel? player = _gameManager.GetCurrentPlayer();
            if (player == null || !player.IsAI) return;

            StartCoroutine(PlayTurnCoroutine());
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

            StartCoroutine(PlayTurnCoroutine());
        }

        /// <summary>
        /// Joue un tour complet avec des délais pour simuler la réflexion.
        /// Reprend depuis la phase courante : si la pioche est déjà faite
        /// (reprise après déconnexion), on enchaîne sur les phases suivantes.
        /// </summary>
        private IEnumerator PlayTurnCoroutine()
        {
            if (_gameManager?.TurnManager == null) yield break;
            if (_gameManager.TurnManager.CurrentPlayerIndex != _playerIndex) yield break;

            EventBus.Publish(new AIThinkingEvent { PlayerIndex = _playerIndex, IsThinking = true });

            // Phase 1 : Piocher (sautée si le tour a déjà dépassé cette phase)
            yield return new WaitForSeconds(_thinkDelay);
            if (_gameManager.TurnManager.CurrentPhase == TurnPhase.Draw)
            {
                DecideDraw();
            }

            // Phase 2 : Tenter de poser le niveau
            yield return new WaitForSeconds(_actionDelay);
            if (_gameManager.TurnManager.CurrentPhase == TurnPhase.LayDown)
            {
                DecideLayDown();
            }

            // Phase 3 : Ajouter aux combinaisons si possible
            if (_gameManager.TurnManager.CurrentPhase == TurnPhase.AddToMelds)
            {
                yield return new WaitForSeconds(_actionDelay);
                DecideAddToMelds();
            }

            // Phase 4 : Défausser
            yield return new WaitForSeconds(_actionDelay);
            if (_gameManager.TurnManager.CurrentPhase == TurnPhase.Discard)
            {
                DecideDiscard();
            }

            EventBus.Publish(new AIThinkingEvent { PlayerIndex = _playerIndex, IsThinking = false });
        }

        // ────────────────────────────────────────────────────
        //  DÉCISIONS (lecture du modèle + envoi de commandes)
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Décide d'où piocher : deck ou une défausse.
        /// Privilégie la défausse si la carte visible aide à compléter le niveau.
        /// </summary>
        private void DecideDraw()
        {
            if (_gameManager?.DeckManager == null) return;

            PlayerModel player = _gameManager.TurnManager!.CurrentPlayer;
            DeckManager deck = _gameManager.DeckManager;
            bool drewFromDiscard = false;

            // Facile : ne regarde jamais les défausses, pioche toujours au deck.
            for (int i = 0; _difficulty != AIDifficulty.Easy && i < deck.DiscardPileCount; i++)
            {
                CardModel? topCard = deck.PeekDiscard(i);
                if (!topCard.HasValue) continue;

                if (IsCardUseful(topCard.Value, player))
                {
                    CommandResult result = _gameManager.ExecuteCommand(
                        new DrawFromDiscardCommand(_playerIndex, i));
                    if (result.Success)
                    {
                        drewFromDiscard = true;
                        break;
                    }
                }
            }

            if (!drewFromDiscard)
            {
                _gameManager.ExecuteCommand(new DrawFromDeckCommand(_playerIndex));
            }
        }

        /// <summary>
        /// Décide si le bot pose son niveau.
        /// </summary>
        private void DecideLayDown()
        {
            if (_gameManager == null) return;

            PlayerModel player = _gameManager.TurnManager!.CurrentPlayer;

            if (player.HasLaidDownThisRound)
            {
                _gameManager.ExecuteCommand(
                    new SkipPhaseCommand(_playerIndex, TurnPhase.LayDown));
                return;
            }

            if (LevelValidator.IsLevelComplete(player.Hand, player.CurrentLevel,
                    _gameManager.Config, out List<Meld> melds))
            {
                List<Meld> playerMelds = new();
                foreach (Meld m in melds)
                {
                    playerMelds.Add(new Meld(m.Type, m.Cards, player.Index));
                }

                _gameManager.ExecuteCommand(
                    new LayDownLevelCommand(_playerIndex, playerMelds));
            }
            else
            {
                _gameManager.ExecuteCommand(
                    new SkipPhaseCommand(_playerIndex, TurnPhase.LayDown));
            }
        }

        /// <summary>
        /// Tente d'ajouter des cartes aux combinaisons existantes.
        /// </summary>
        private void DecideAddToMelds()
        {
            if (_gameManager == null)
            {
                return;
            }

            PlayerModel player = _gameManager.TurnManager!.CurrentPlayer;

            // Facile : oublie parfois de compléter les combinaisons posées.
            if (_difficulty == AIDifficulty.Easy && Random.value < 0.4f)
            {
                _gameManager.ExecuteCommand(
                    new SkipPhaseCommand(_playerIndex, TurnPhase.AddToMelds));
                return;
            }

            bool addedAny = true;

            while (addedAny && player.Hand.Count > 1)
            {
                addedAny = false;

                foreach (PlayerModel otherPlayer in _gameManager.Players)
                {
                    for (int m = 0; m < otherPlayer.LaidMelds.Count; m++)
                    {
                        for (int c = player.Hand.Count - 1; c >= 0; c--)
                        {
                            if (player.Hand.Count <= 1) break;

                            CardModel card = player.Hand[c];
                            CommandResult result = _gameManager.ExecuteCommand(
                                new AddToMeldCommand(_playerIndex, card, otherPlayer.Index, m));

                            if (result.Success)
                            {
                                addedAny = true;
                                break;
                            }
                        }
                    }
                }
            }

            _gameManager.ExecuteCommand(
                new SkipPhaseCommand(_playerIndex, TurnPhase.AddToMelds));
        }

        /// <summary>
        /// Choisit la meilleure carte à défausser.
        /// Stratégie : garder les cartes utiles pour le niveau, défausser les moins utiles.
        /// </summary>
        private void DecideDiscard()
        {
            if (_gameManager == null) return;

            PlayerModel player = _gameManager.TurnManager!.CurrentPlayer;
            if (player.Hand.Count == 0) return;

            CardModel bestDiscard = player.Hand[0];
            float bestScore = float.MaxValue;

            foreach (CardModel card in player.Hand)
            {
                float score = EvaluateCardUsefulness(card, player);

                if (card.IsAction && !card.IsWild)
                {
                    score -= 5f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestDiscard = card;
                }
            }

            int targetIndex = -1;
            if (bestDiscard.IsAction)
            {
                targetIndex = FindBestTarget(player);
            }

            // ExecuteCommand gère automatiquement OnRoundEnd si la main est vide
            _gameManager.ExecuteCommand(
                new DiscardCommand(_playerIndex, bestDiscard, targetIndex));
        }

        // ────────────────────────────────────────────────────
        //  HEURISTIQUES (lecture seule)
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Évalue l'utilité d'une carte pour le niveau actuel du joueur.
        /// Score élevé = carte utile à garder.
        /// </summary>
        private float EvaluateCardUsefulness(CardModel card, PlayerModel player)
        {
            if (card.IsWild) return 10f;

            // Facile : bruit d'évaluation — le bot se trompe régulièrement sur
            // la valeur d'une carte et défausse parfois une carte utile.
            float score = _difficulty == AIDifficulty.Easy
                ? Random.Range(-4f, 4f)
                : 0f;
            int level = player.CurrentLevel;

            List<List<LevelValidator.LevelRequirement>> reqs =
                LevelValidator.GetRequirements(level, _gameManager?.Config);

            if (reqs.Count == 0) return score;

            foreach (List<LevelValidator.LevelRequirement> reqSet in reqs)
            {
                foreach (LevelValidator.LevelRequirement req in reqSet)
                {
                    switch (req.Type)
                    {
                        case MeldType.Run:
                            int neighbors = player.Hand.Count(c =>
                                c.Type == CardType.Normal &&
                                System.Math.Abs(c.Value - card.Value) <= 1 &&
                                c.Id != card.Id);
                            score += neighbors * 2f;
                            break;

                        case MeldType.Set:
                            int sameValue = player.Hand.Count(c =>
                                c.Type == CardType.Normal &&
                                c.Value == card.Value &&
                                c.Id != card.Id);
                            score += sameValue * 3f;
                            break;

                        case MeldType.Flush:
                            int sameColor = player.Hand.Count(c =>
                                c.Type == CardType.Normal &&
                                c.Color == card.Color &&
                                c.Id != card.Id);
                            score += sameColor * 2f;
                            break;
                    }
                }
            }

            return score;
        }

        private bool IsCardUseful(CardModel card, PlayerModel player)
        {
            // Difficile : ramasse plus agressivement dans les défausses.
            float threshold = _difficulty == AIDifficulty.Hard ? 3f : 4f;
            return EvaluateCardUsefulness(card, player) >= threshold;
        }

        /// <summary>
        /// Trouve le meilleur joueur à cibler avec une carte action.
        /// Cible le joueur le plus avancé en niveau.
        /// </summary>
        private int FindBestTarget(PlayerModel self)
        {
            if (_gameManager == null) return -1;

            int bestTarget = -1;
            int highestLevel = -1;

            foreach (PlayerModel player in _gameManager.Players)
            {
                if (player.Index == self.Index) continue;

                if (player.CurrentLevel > highestLevel)
                {
                    highestLevel = player.CurrentLevel;
                    bestTarget = player.Index;
                }
            }

            return bestTarget;
        }
    }
}
