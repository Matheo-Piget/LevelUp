using System.Collections.Generic;
using UnityEngine;

namespace LevelUp.UI
{
    /// <summary>
    /// Réserve partagée de <see cref="CardView"/> (main, table). Une carte est
    /// coûteuse à créer : le prefab plus la vingtaine d'objets construits par
    /// <c>CardView.Build()</c>. Avant, chaque pioche instanciait une carte, chaque
    /// pose en instanciait 6 à 9 dans la même image, et tout était détruit à la
    /// manche suivante. Ici les cartes rendues sont gardées inactives et resservent.
    /// </summary>
    public static class CardViewPool
    {
        private static readonly Stack<CardView> Free = new();
        private static Transform? _root;

        // Géométrie du prefab : une carte rendue par la table (ancrée à gauche,
        // réduite) doit repartir comme neuve dans la main.
        private static bool _hasDefaults;
        private static Vector2 _anchorMin, _anchorMax, _pivot, _sizeDelta;

        /// <summary>Cartes actuellement en réserve (diagnostic, tests).</summary>
        public static int FreeCount => Free.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Free.Clear();
            _root = null;
            _hasDefaults = false;
        }

        /// <summary>Une carte prête à l'emploi sous <paramref name="parent"/> (réserve, sinon création).</summary>
        public static CardView? Get(GameObject prefab, Transform parent)
        {
            CaptureDefaults(prefab);

            while (Free.Count > 0)
            {
                CardView pooled = Free.Pop();
                if (pooled == null) continue; // détruite avec son ancien parent
                RectTransform rt = pooled.RectTransform;
                rt.SetParent(parent, false);
                rt.anchorMin = _anchorMin;
                rt.anchorMax = _anchorMax;
                rt.pivot = _pivot;
                rt.sizeDelta = _sizeDelta;
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
                pooled.SetAlpha(1f);
                pooled.SetInteractable(true);
                if (!pooled.gameObject.activeSelf) pooled.gameObject.SetActive(true);
                return pooled;
            }

            GameObject obj = Object.Instantiate(prefab, parent);
            return obj.GetComponent<CardView>();
        }

        /// <summary>Rend une carte à la réserve (elle disparaît de l'écran).</summary>
        public static void Release(CardView? view)
        {
            if (view == null) return;
            view.RectTransform.SetParent(Root(), false);
            Free.Push(view);
        }

        /// <summary>Crée à l'avance <paramref name="count"/> cartes (à étaler hors des moments de jeu).</summary>
        public static void Prewarm(GameObject prefab, int count)
        {
            CaptureDefaults(prefab);
            Transform root = Root();
            for (int i = Free.Count; i < count; i++)
            {
                GameObject obj = Object.Instantiate(prefab, root);
                CardView? view = obj.GetComponent<CardView>();
                if (view != null) Free.Push(view);
            }
        }

        private static void CaptureDefaults(GameObject prefab)
        {
            if (_hasDefaults) return;
            RectTransform? rt = prefab.transform as RectTransform;
            if (rt == null) return;
            _anchorMin = rt.anchorMin;
            _anchorMax = rt.anchorMax;
            _pivot = rt.pivot;
            _sizeDelta = rt.sizeDelta;
            _hasDefaults = true;
        }

        // Racine inactive : les cartes en réserve ne coûtent ni rendu ni Update.
        private static Transform Root()
        {
            if (_root != null) return _root;
            GameObject go = new("CardPool", typeof(RectTransform));
            go.SetActive(false);
            Object.DontDestroyOnLoad(go);
            _root = go.transform;
            return _root;
        }
    }
}
