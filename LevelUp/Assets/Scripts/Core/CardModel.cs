using System;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Représentation immuable d'une carte du jeu.
    /// Struct pour éviter les allocations heap et garantir l'immutabilité.
    /// </summary>
    public readonly struct CardModel : IEquatable<CardModel>
    {
        /// <summary>Identifiant unique de la carte dans le deck.</summary>
        public readonly int Id;

        /// <summary>Valeur numérique de la carte (1-18 pour normales, valeurs négatives pour actions).</summary>
        public readonly int Value;

        /// <summary>Couleur de la carte.</summary>
        public readonly CardColor Color;

        /// <summary>Type de la carte (Normal, Skip, Draw2, Wild, WildDraw2).</summary>
        public readonly CardType Type;

        /// <summary>
        /// Crée une carte normale avec valeur et couleur.
        /// </summary>
        public CardModel(int id, int value, CardColor color)
        {
            Id = id;
            Value = value;
            Color = color;
            Type = CardType.Normal;
        }

        /// <summary>
        /// Crée une carte action (Skip, Draw2, Wild, WildDraw2).
        /// </summary>
        public CardModel(int id, CardType type)
        {
            Id = id;
            Type = type;
            Color = CardColor.Wild;
            Value = type switch
            {
                CardType.Skip      => Constants.SkipCardValue,
                CardType.Draw2     => Constants.Draw2CardValue,
                CardType.Wild      => Constants.WildCardValue,
                CardType.WildDraw2 => Constants.WildDraw2CardValue,
                _                  => 0
            };
        }

        /// <summary>
        /// Indique si cette carte est un Wild (Wild ou WildDraw2).
        /// </summary>
        public bool IsWild => Type == CardType.Wild || Type == CardType.WildDraw2;

        /// <summary>
        /// Indique si cette carte est une carte action.
        /// </summary>
        public bool IsAction => Type != CardType.Normal;

        public bool Equals(CardModel other) => Id == other.Id;
        public override bool Equals(object? obj) => obj is CardModel other && Equals(other);
        public override int GetHashCode() => Id;
        public static bool operator ==(CardModel a, CardModel b) => a.Id == b.Id;
        public static bool operator !=(CardModel a, CardModel b) => a.Id != b.Id;

        public override string ToString()
        {
            return Type == CardType.Normal
                ? $"[{Color} {Value}]"
                : $"[{Type}]";
        }
    }
}
