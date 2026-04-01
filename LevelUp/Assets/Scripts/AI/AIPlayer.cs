using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.AI
{
    /// <summary>
    /// Intelligence artificielle heuristique pour un joueur bot.
    /// Évalue la main, décide de poser le niveau, choisit la meilleure défausse.
    /// </summary>
    public class AIPlayer : MonoBehaviour
    {
        [SerializeField] private float _thinkDelay = 0.5f;
        [SerializeField] private float _actionDelay = 0.3f;

        private GameManager? _gameManager;
        private int _playerIndex = -1;

        /// <summary>
        /// Initialise l'IA avec les références nécessaires.
        /// </summary>
        public void Initialize(GameManager gameManager, int playerIndex)
        {
            _gameManager = gameManager;
            _playerIndex = playerIndex;
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
        /// Joue un tour complet avec des délais pour simuler la réflexion.
        /// </summary>
        private IEnumerator PlayTurnCoroutine()
        {
            if (_gameManager?.TurnManager == null) yield break;

            TurnManager tm = _gameManager.TurnManager;
            PlayerModel player = tm.CurrentPlayer;

            // Phase 1 : Piocher
            yield return new WaitForSeconds(_thinkDelay);
            DecideDraw(tm, player);

            // Phase 2 : Tenter de poser le niveau
            yield return new WaitForSeconds(_actionDelay);
            DecideLayDown(tm, player);

            // Phase 3 : Ajouter aux combinaisons si possible
            if (player.HasLaidDownThisRound)
            {
                yield return new WaitForSeconds(_actionDelay);
                DecideAddToMelds(tm, player);
            }
            else
            {
                tm.SkipLayDown();
            }

            // Phase 4 : Défausser
            yield return new WaitForSeconds(_actionDelay);
            DecideDiscard(tm, player);
        }

        /// <summary>
        /// Décide d'où piocher : deck ou une défausse.
        /// Privilégie la défausse si la carte visible aide à compléter le niveau.
        /// </summary>
        private void DecideDraw(TurnManager tm, PlayerModel player)
        {
            if (_gameManager?.DeckManager == null) return;

            DeckManager deck = _gameManager.DeckManager;
            bool drewFromDiscard = false;

            // Vérifier chaque pile de défausse
            for (int i = 0; i < deck.DiscardPileCount; i++)
            {
                CardModel? topCard = deck.PeekDiscard(i);
                if (!topCard.HasValue) continue;

                // Évaluer si la carte aide
                if (IsCardUseful(topCard.Value, player))
                {
                    if (tm.DrawFromDiscard(i))
                    {
                        drewFromDiscard = true;
                        break;
                    }
                }
            }

            if (!drewFromDiscard)
            {
                tm.DrawFromDeck();
            }
        }

        /// <summary>
        /// Décide si le bot pose son niveau.
        /// </summary>
        private void DecideLayDown(TurnManager tm, PlayerModel player)
        {
            if (player.HasLaidDownThisRound)
            {
                tm.SkipLayDown();
                return;
            }

            if (LevelValidator.IsLevelComplete(player.Hand, player.CurrentLevel,
                    _gameManager?.Config, out List<Meld> melds))
            {
                // Assigner les bons index aux melds
                List<Meld> playerMelds = new();
                foreach (Meld m in melds)
                {
                    playerMelds.Add(new Meld(m.Type, m.Cards, player.Index));
                }

                tm.TryLayDownLevel(playerMelds);
            }
            else
            {
                tm.SkipLayDown();
            }
        }

        /// <summary>
        /// Tente d'ajouter des cartes aux combinaisons existantes.
        /// </summary>
        private void DecideAddToMelds(TurnManager tm, PlayerModel player)
        {
            if (_gameManager == null)
            {
                tm.SkipAddToMelds();
                return;
            }

            bool addedAny = true;

            // Continuer tant qu'on peut ajouter des cartes
            while (addedAny && player.Hand.Count > 1) // garder au moins 1 carte pour défausser
            {
                addedAny = false;

                foreach (PlayerModel otherPlayer in _gameManager.Players)
                {
                    for (int m = 0; m < otherPlayer.LaidMelds.Count; m++)
                    {
                        // Essayer chaque carte de la main
                        for (int c = player.Hand.Count - 1; c >= 0; c--)
                        {
                            if (player.Hand.Count <= 1) break;

                            CardModel card = player.Hand[c];
                            if (tm.AddToMeld(card, otherPlayer.Index, m))
                            {
                                addedAny = true;
                                break;
                            }
                        }
                    }
                }
            }

            tm.SkipAddToMelds();
        }

        /// <summary>
        /// Choisit la meilleure carte à défausser.
        /// Stratégie : garder les cartes utiles pour le niveau, défausser les moins utiles.
        /// </summary>
        private void DecideDiscard(TurnManager tm, PlayerModel player)
        {
            if (player.Hand.Count == 0) return;

            CardModel bestDiscard = player.Hand[0];
            float bestScore = float.MaxValue;

            foreach (CardModel card in player.Hand)
            {
                float score = EvaluateCardUsefulness(card, player);

                // Pénaliser les action cards pour qu'on les garde pas inutilement
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

            // Si on défausse une action card, cibler le joueur le plus avancé
            int targetIndex = -1;
            if (bestDiscard.IsAction)
            {
                targetIndex = FindBestTarget(player);
            }

            bool roundEnded = tm.Discard(bestDiscard, targetIndex);
            if (roundEnded)
            {
                _gameManager?.OnRoundEnd(player.Index);
            }
        }

        /// <summary>
        /// Évalue l'utilité d'une carte pour le niveau actuel du joueur.
        /// Score élevé = carte utile à garder.
        /// </summary>
        private float EvaluateCardUsefulness(CardModel card, PlayerModel player)
        {
            if (card.IsWild) return 10f; // Les Wilds sont toujours utiles

            float score = 0f;
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
                            // Bonus si la carte a des voisins consécutifs dans la main
                            int neighbors = player.Hand.Count(c =>
                                c.Type == CardType.Normal &&
                                System.Math.Abs(c.Value - card.Value) <= 1 &&
                                c.Id != card.Id);
                            score += neighbors * 2f;
                            break;

                        case MeldType.Set:
                            // Bonus si d'autres cartes ont la même valeur
                            int sameValue = player.Hand.Count(c =>
                                c.Type == CardType.Normal &&
                                c.Value == card.Value &&
                                c.Id != card.Id);
                            score += sameValue * 3f;
                            break;

                        case MeldType.Flush:
                            // Bonus si d'autres cartes ont la même couleur
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

        /// <summary>
        /// Vérifie si une carte de la défausse est utile pour le bot.
        /// </summary>
        private bool IsCardUseful(CardModel card, PlayerModel player)
        {
            return EvaluateCardUsefulness(card, player) >= 4f;
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
