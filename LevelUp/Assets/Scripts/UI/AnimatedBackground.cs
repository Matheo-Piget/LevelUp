using UnityEngine;
using UnityEngine.UI;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Fond uni neutre #0F1419 + vignette douce. Pas de dégradé multicolore :
    /// le fond doit faire ressortir les cartes, pas rivaliser avec elles.
    /// Le nom est conservé pour compat (le composant ne fait plus d'animation).
    /// </summary>
    public class AnimatedBackground : MonoBehaviour
    {
        private Sprite? _radialSprite;

        /// <summary>
        /// Construit le fond uni sous le canvas donné.
        /// </summary>
        public void Setup(Canvas canvas)
        {
            if (canvas == null) return;

            GameObject rootObj = new("Background", typeof(RectTransform), typeof(CanvasGroup));
            rootObj.transform.SetParent(canvas.transform, false);
            rootObj.transform.SetAsFirstSibling();

            RectTransform root = rootObj.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.sizeDelta = Vector2.zero;
            root.anchoredPosition = Vector2.zero;

            CanvasGroup rootCg = rootObj.GetComponent<CanvasGroup>();
            rootCg.blocksRaycasts = false;
            rootCg.interactable = false;

            CreateSolidBase(root);
            CreateVignette(root);
        }

        /// <summary>Couche de base : fond uni #0F1419.</summary>
        private static void CreateSolidBase(RectTransform parent)
        {
            GameObject baseObj = new("BaseColor", typeof(RectTransform), typeof(Image));
            baseObj.transform.SetParent(parent, false);

            RectTransform rt = baseObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image img = baseObj.GetComponent<Image>();
            img.color = Constants.BackgroundDark;
            img.raycastTarget = false;
        }

        /// <summary>Vignette sombre très douce sur les bords pour ancrer le regard au centre.</summary>
        private void CreateVignette(RectTransform parent)
        {
            GameObject v = new("Vignette", typeof(RectTransform), typeof(Image));
            v.transform.SetParent(parent, false);
            v.transform.SetAsLastSibling();

            RectTransform rt = v.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image img = v.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.28f);
            img.raycastTarget = false;
            img.sprite = CreateInverseRadial();
        }

        private static Sprite CreateInverseRadial()
        {
            const int size = 128;
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float center = (size - 1) * 0.5f;
            float maxDist = center;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / maxDist;
                    float dy = (y - center) / maxDist;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(d * 1.05f);
                    a = a * a;
                    pixels[y * size + x] = new Color(0f, 0f, 0f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
