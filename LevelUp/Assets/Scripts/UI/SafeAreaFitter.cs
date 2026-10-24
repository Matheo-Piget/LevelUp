#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace LevelUp.UI
{
    /// <summary>
    /// Cale son RectTransform sur <see cref="Screen.safeArea"/> : évite que
    /// l'UI de jeu passe sous le punch-hole caméra ou la barre de gestes
    /// (Galaxy S25 et autres écrans à encoche). Sur desktop, la safe area
    /// couvre tout l'écran → aucun effet.
    /// </summary>
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform? _rect;
        private Rect _lastSafeArea = new(-1f, -1f, -1f, -1f);
        private Vector2Int _lastScreenSize;

        /// <summary>
        /// Crée un conteneur "SafeAreaRoot" sous le canvas et y déplace tous
        /// ses enfants actuels (l'UI gameplay posée en scène). Les éléments
        /// créés APRÈS (fond, vignette, menus plein écran) restent enfants
        /// directs du canvas et couvrent donc tout l'écran (full bleed).
        /// </summary>
        public static RectTransform Wrap(Canvas canvas)
        {
            GameObject rootObj = new("SafeAreaRoot", typeof(RectTransform));
            RectTransform root = rootObj.GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.sizeDelta = Vector2.zero;
            root.anchoredPosition = Vector2.zero;

            List<Transform> toMove = new();
            foreach (Transform child in canvas.transform)
            {
                if (child != root) toMove.Add(child);
            }
            foreach (Transform child in toMove)
            {
                child.SetParent(root, false);
            }

            rootObj.AddComponent<SafeAreaFitter>();
            return root;
        }

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            Apply();
        }

        private void LateUpdate()
        {
            // La safe area change à la rotation de l'écran ou au passage
            // plein écran : on ne réapplique que sur changement réel.
            if (Screen.safeArea != _lastSafeArea ||
                Screen.width != _lastScreenSize.x ||
                Screen.height != _lastScreenSize.y)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_rect == null) return;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Rect sa = Screen.safeArea;
            _lastSafeArea = sa;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            // Convertit la safe area (pixels écran) en anchors normalisés :
            // le rect reste en full-stretch, seuls les anchors bougent.
            Vector2 min = sa.position;
            Vector2 max = sa.position + sa.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
