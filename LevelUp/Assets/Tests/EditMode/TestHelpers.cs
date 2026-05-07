using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.Tests
{
    /// <summary>
    /// Fabrique de cartes pour les tests. Les IDs sont uniques par test (réinitialisés
    /// dans <see cref="Reset"/>) pour garder l'égalité de CardModel déterministe.
    /// </summary>
    internal static class CardFactory
    {
        private static int _nextId;

        public static void Reset() => _nextId = 0;

        public static CardModel Normal(int value, CardColor color)
            => new(_nextId++, value, color);

        public static CardModel Wild() => new(_nextId++, CardType.Wild);
        public static CardModel WildDraw2() => new(_nextId++, CardType.WildDraw2);
        public static CardModel Skip() => new(_nextId++, CardType.Skip);
        public static CardModel Draw2() => new(_nextId++, CardType.Draw2);

        // Raccourcis couleurs
        public static CardModel R(int v) => Normal(v, CardColor.Red);
        public static CardModel B(int v) => Normal(v, CardColor.Blue);
        public static CardModel G(int v) => Normal(v, CardColor.Green);
        public static CardModel Y(int v) => Normal(v, CardColor.Yellow);
        public static CardModel P(int v) => Normal(v, CardColor.Purple);
        public static CardModel O(int v) => Normal(v, CardColor.Orange);
    }
}
