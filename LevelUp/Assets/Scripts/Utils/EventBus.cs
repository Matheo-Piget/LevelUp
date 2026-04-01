using System;
using System.Collections.Generic;

namespace LevelUp.Utils
{
    /// <summary>
    /// Système d'événements découplé basé sur des types génériques.
    /// Permet la communication entre systèmes sans dépendances directes.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> Events = new();

        /// <summary>
        /// S'abonner à un événement de type T.
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            Type type = typeof(T);
            if (Events.TryGetValue(type, out Delegate? existing))
            {
                Events[type] = Delegate.Combine(existing, handler);
            }
            else
            {
                Events[type] = handler;
            }
        }

        /// <summary>
        /// Se désabonner d'un événement de type T.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            Type type = typeof(T);
            if (Events.TryGetValue(type, out Delegate? existing))
            {
                Delegate? result = Delegate.Remove(existing, handler);
                if (result == null)
                {
                    Events.Remove(type);
                }
                else
                {
                    Events[type] = result;
                }
            }
        }

        /// <summary>
        /// Publier un événement de type T à tous les abonnés.
        /// </summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            Type type = typeof(T);
            if (Events.TryGetValue(type, out Delegate? existing))
            {
                (existing as Action<T>)?.Invoke(eventData);
            }
        }

        /// <summary>
        /// Supprimer tous les abonnements (utile au changement de scène).
        /// </summary>
        public static void Clear()
        {
            Events.Clear();
        }
    }
}
