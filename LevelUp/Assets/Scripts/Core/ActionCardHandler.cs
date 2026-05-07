using System.Collections.Generic;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Gère les effets des cartes action : Skip, Draw2, Wild, WildDraw2.
    /// </summary>
    public class ActionCardHandler
    {
        private readonly DeckManager _deckManager;
        private readonly List<PlayerModel> _players;

        /// <summary>
        /// Constructeur avec les dépendances nécessaires.
        /// </summary>
        public ActionCardHandler(DeckManager deckManager, List<PlayerModel> players)
        {
            _deckManager = deckManager;
            _players = players;
        }

        /// <summary>
        /// Exécute l'effet d'une carte action jouée comme défausse.
        /// Retourne true si la carte a été traitée comme action.
        /// </summary>
        public bool HandleActionCard(int playerIndex, CardModel card, int targetPlayerIndex)
        {
            if (!card.IsAction) return false;

            switch (card.Type)
            {
                case CardType.Skip:
                    HandleSkip(playerIndex, targetPlayerIndex);
                    break;

                case CardType.Draw2:
                    HandleDraw2(playerIndex, targetPlayerIndex);
                    break;

                case CardType.Wild:
                    // Wild comme défausse : pas d'effet spécial, juste défaussé
                    break;

                case CardType.WildDraw2:
                    HandleWildDraw2(playerIndex, targetPlayerIndex);
                    break;
            }

            EventBus.Publish(new ActionCardPlayedEvent
            {
                PlayerIndex = playerIndex,
                Card = card,
                TargetPlayerIndex = targetPlayerIndex
            });

            return true;
        }

        /// <summary>
        /// Skip : le joueur ciblé saute son prochain tour.
        /// Le compteur s'empile — deux cartes Skip jouées contre le même joueur
        /// le font sauter deux tours d'affilée.
        /// </summary>
        private void HandleSkip(int playerIndex, int targetIndex)
        {
            if (targetIndex >= 0 && targetIndex < _players.Count && targetIndex != playerIndex)
            {
                _players[targetIndex].SkipCount++;
                EventBus.Publish(new PlayerSkippedEvent { PlayerIndex = targetIndex });
            }
        }

        /// <summary>
        /// Draw2 : le joueur ciblé pioche 2 cartes.
        /// </summary>
        private void HandleDraw2(int playerIndex, int targetIndex)
        {
            if (targetIndex >= 0 && targetIndex < _players.Count && targetIndex != playerIndex)
            {
                ForceDrawCards(targetIndex, 2);
            }
        }

        /// <summary>
        /// WildDraw2 : le joueur ciblé pioche 2 cartes (en plus de l'effet Wild).
        /// </summary>
        private void HandleWildDraw2(int playerIndex, int targetIndex)
        {
            if (targetIndex >= 0 && targetIndex < _players.Count && targetIndex != playerIndex)
            {
                ForceDrawCards(targetIndex, 2);
            }
        }

        /// <summary>
        /// Force un joueur à piocher un nombre donné de cartes.
        /// </summary>
        private void ForceDrawCards(int playerIndex, int count)
        {
            PlayerModel player = _players[playerIndex];

            for (int i = 0; i < count; i++)
            {
                CardModel? drawn = _deckManager.DrawFromPile();
                if (drawn.HasValue)
                {
                    player.AddToHand(drawn.Value);
                }
            }

            EventBus.Publish(new ForcedDrawEvent
            {
                PlayerIndex = playerIndex,
                CardCount = count
            });
        }

        /// <summary>
        /// Retourne le prochain joueur valide. Consomme les Skip empilés un par un :
        /// chaque joueur sauté décrémente son compteur de 1, puis on passe au suivant.
        /// Garde-fou : si tous les joueurs sont marqués Skip, on retombe sur celui
        /// d'après pour éviter une boucle infinie.
        /// </summary>
        public int GetNextPlayer(int currentIndex)
        {
            int next = (currentIndex + 1) % _players.Count;
            int safety = _players.Count;

            while (_players[next].SkipCount > 0 && safety-- > 0)
            {
                _players[next].SkipCount--;
                EventBus.Publish(new PlayerSkippedEvent { PlayerIndex = next });
                next = (next + 1) % _players.Count;
            }

            return next;
        }
    }
}
