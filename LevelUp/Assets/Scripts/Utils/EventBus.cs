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
    ///
    /// Performance : Publish n'alloue rien tant que les abonnements ne changent
    /// pas — le snapshot d'invocation est mis en cache et invalidé uniquement
    /// par Subscribe/Unsubscribe. Itérer un snapshot immuable protège aussi
    /// contre les (dés)abonnements effectués par un handler pendant le dispatch.
    /// </summary>
    public static class EventBus
    {
        private sealed class Subscription
        {
            public Delegate? Combined;
            public Delegate[]? InvocationCache;
        }

        private static readonly Dictionary<Type, Subscription> Events = new();

        // Avec "Enter Play Mode Options" (sans domain reload), les statics
        // survivent entre deux Play : on repart proprement à chaque lancement.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Events.Clear();
            Muted = false;
        }

        /// <summary>
        /// Coupe toute publication tant que vrai. Sert à rejouer silencieusement
        /// l'historique d'une partie (resynchronisation en ligne) sans déclencher
        /// des centaines d'animations ; les vues sont ensuite resynchronisées d'un bloc.
        /// </summary>
        public static bool Muted { get; set; }

        /// <summary>
        /// S'abonner à un événement de type T. Idempotent : un même handler
        /// abonné deux fois (ex: OnEnable rappelé après SetActive) ne sera
        /// pas invoqué deux fois.
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            Type type = typeof(T);
            if (!Events.TryGetValue(type, out Subscription? sub))
            {
                sub = new Subscription();
                Events[type] = sub;
            }

            // Dédoublonnage : retire le handler s'il est déjà présent.
            Delegate? without = Delegate.Remove(sub.Combined, handler);
            sub.Combined = Delegate.Combine(without, handler);
            sub.InvocationCache = null;
        }

        /// <summary>
        /// Se désabonner d'un événement de type T.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            Type type = typeof(T);
            if (!Events.TryGetValue(type, out Subscription? sub)) return;

            sub.Combined = Delegate.Remove(sub.Combined, handler);
            sub.InvocationCache = null;
            if (sub.Combined == null)
            {
                Events.Remove(type);
            }
        }

        /// <summary>
        /// Publier un événement de type T à tous les abonnés.
        /// Chaque handler est isolé : une exception dans l'un n'empêche
        /// pas les suivants de s'exécuter.
        /// </summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            if (Muted) return;
            if (!Events.TryGetValue(typeof(T), out Subscription? sub)) return;
            if (sub.Combined is not Action<T>) return;

            Delegate[] handlers = sub.InvocationCache ??= sub.Combined.GetInvocationList();

            for (int i = 0; i < handlers.Length; i++)
            {
                try
                {
                    ((Action<T>)handlers[i]).Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Exception in {typeof(T).Name} handler: {ex}");
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
