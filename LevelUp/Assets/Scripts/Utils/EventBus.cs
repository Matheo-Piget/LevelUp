using System;
using System.Collections.Generic;
using UnityEngine;

namespace LevelUp.Utils
{
    /// <summary>
    /// Système d'événements découplé basé sur des types génériques.
    /// Permet la communication entre systèmes sans dépendances directes.
    ///
    /// Robustesse : chaque subscriber est invoqué dans un try/catch isolé.
    /// Un handler qui lève une exception ne bloque pas les suivants.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> Events = new();

        /// <summary>
        /// S'abonner à un événement de type T. Idempotent : un même handler
        /// abonné deux fois (ex: OnEnable rappelé après SetActive) ne sera
        /// pas invoqué deux fois.
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            Type type = typeof(T);
            if (Events.TryGetValue(type, out Delegate? existing))
            {
                // Dédoublonnage : retire le handler s'il est déjà présent.
                Delegate? without = Delegate.Remove(existing, handler);
                Events[type] = Delegate.Combine(without, handler);
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
        /// Chaque handler est isolé : une exception dans l'un n'empêche
        /// pas les suivants de s'exécuter.
        /// </summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            Type type = typeof(T);
            if (!Events.TryGetValue(type, out Delegate? existing)) return;
            if (existing is not Action<T> action) return;

            foreach (Delegate d in action.GetInvocationList())
            {
                try
                {
                    ((Action<T>)d).Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Exception in {type.Name} handler: {ex}");
                }
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
