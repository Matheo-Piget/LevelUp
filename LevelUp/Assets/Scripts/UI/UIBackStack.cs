using System;
using System.Collections.Generic;
using UnityEngine;

namespace LevelUp.UI
{
    /// <summary>
    /// Pile des fenêtres ouvertes : le bouton Retour Android (Échap au clavier)
    /// ferme la plus récente au lieu d'ouvrir la pause par-dessus. Chaque
    /// panneau s'y inscrit quand il s'affiche et s'en retire quand il se ferme.
    /// </summary>
    public static class UIBackStack
    {
        private static readonly List<(object Owner, Action Close)> Stack = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Stack.Clear();

        /// <summary>Vrai si au moins une fenêtre est ouverte.</summary>
        public static bool HasOpenPanel => Stack.Count > 0;

        /// <summary>Inscrit une fenêtre (la remet en tête si elle y était déjà).</summary>
        public static void Push(object owner, Action close)
        {
            Remove(owner);
            Stack.Add((owner, close));
        }

        public static void Remove(object owner)
        {
            for (int i = Stack.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(Stack[i].Owner, owner)) Stack.RemoveAt(i);
            }
        }

        /// <summary>Ferme la fenêtre la plus récente ; false s'il n'y en avait aucune.</summary>
        public static bool HandleBack()
        {
            while (Stack.Count > 0)
            {
                (object owner, Action close) = Stack[^1];
                Stack.RemoveAt(Stack.Count - 1);
                if (owner is UnityEngine.Object o && o == null) continue; // détruite entre-temps
                close();
                return true;
            }
            return false;
        }
    }
}
