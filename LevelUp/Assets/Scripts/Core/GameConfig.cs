using System.Collections.Generic;
using UnityEngine;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// ScriptableObject contenant toute la configuration du jeu.
    /// Permet de modifier les paramètres sans toucher au code.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "LevelUp/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Deck Configuration")]
        [Tooltip("Nombre total de cartes dans le deck")]
        [SerializeField] private int _deckSize = 108;

        [Tooltip("Valeur minimale des cartes")]
        [SerializeField] private int _cardMinValue = 1;

        [Tooltip("Valeur maximale des cartes")]
        [SerializeField] private int _cardMaxValue = 18;

        [Tooltip("Nombre de cartes distribuées par joueur")]
        [SerializeField] private int _cardsPerPlayer = 10;

        [Header("Game Rules")]
        [Tooltip("Nombre minimum de joueurs")]
        [SerializeField] private int _minPlayers = 2;

        [Tooltip("Nombre maximum de joueurs")]
        [SerializeField] private int _maxPlayers = 6;

        /// <summary>Nombre total de cartes dans le deck.</summary>
        public int DeckSize => _deckSize;

        /// <summary>Valeur minimale des cartes.</summary>
        public int CardMinValue => _cardMinValue;

        /// <summary>Valeur maximale des cartes.</summary>
        public int CardMaxValue => _cardMaxValue;

        /// <summary>Nombre de cartes distribuées par joueur au début d'un round.</summary>
        public int CardsPerPlayer => _cardsPerPlayer;

        /// <summary>Nombre minimum de joueurs.</summary>
        public int MinPlayers => _minPlayers;

        /// <summary>Nombre maximum de joueurs.</summary>
        public int MaxPlayers => _maxPlayers;

    }
}
