using UnityEngine;
using UnityEngine.UI;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Fond animé : 4 blobs radiaux colorés qui dérivent en boucle, plus une grille
    /// subtile et un overlay vignette. Donne de la profondeur et de la vie au plateau
    /// sans avoir besoin de shader ou de texture custom.
    /// </summary>
    public class AnimatedBackground : MonoBehaviour
    {
        private const int BlobCount = 6;

        private RectTransform[] _blobs = new RectTransform[BlobCount];
        private Vector2[] _blobOrigins = new Vector2[BlobCount];
        private Vector2[] _blobAmplitudes = new Vector2[BlobCount];
        private float[] _phaseOffsets = new float[BlobCount];
        private float[] _speeds = new float[BlobCount];
        private CanvasGroup[] _blobGroups = new CanvasGroup[BlobCount];
        private Image[] _blobImages = new Image[BlobCount];
        private Color[] _blobBaseColors = new Color[BlobCount];
        private Color[] _blobAltColors = new Color[BlobCount];

        private RectTransform? _root;
        private Sprite? _radialSprite;
        private float _time;

        /// <summary>
        /// Construit le fond animé sous le canvas donné.
        /// </summary>
        public void Setup(Canvas canvas)
        {
            if (canvas == null) return;

            // Conteneur racine du fond — toujours derrière tout
            GameObject rootObj = new("AnimatedBackground", typeof(RectTransform), typeof(CanvasGroup));
            rootObj.transform.SetParent(canvas.transform, false);
            rootObj.transform.SetAsFirstSibling();

            _root = rootObj.GetComponent<RectTransform>();
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.sizeDelta = Vector2.zero;
            _root.anchoredPosition = Vector2.zero;

            CanvasGroup rootCg = rootObj.GetComponent<CanvasGroup>();
            rootCg.blocksRaycasts = false;
            rootCg.interactable = false;

            CreateBaseGradient(_root);
            CreateRadialSprite();
            CreateTopAurora(_root);
            CreateBlobs(_root);
            CreateGrid(_root);
            CreateVignette(_root);
        }

        /// <summary>Couche de base : dégradé profond nuit -> indigo.</summary>
        private void CreateBaseGradient(RectTransform parent)
        {
            GameObject baseObj = new("BaseColor", typeof(RectTransform), typeof(Image));
            baseObj.transform.SetParent(parent, false);

            RectTransform rt = baseObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image img = baseObj.GetComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = false;
            img.sprite = CreateVerticalGradient(
                new Color32(0x1A, 0x0F, 0x2E, 0xFF),  // haut : violet profond
                new Color32(0x05, 0x08, 0x14, 0xFF)); // bas : quasi noir bleuté
        }

        /// <summary>Bande lumineuse "aurora" en haut pour un look vibrant.</summary>
        private void CreateTopAurora(RectTransform parent)
        {
            GameObject a = new("Aurora", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            a.transform.SetParent(parent, false);

            RectTransform rt = a.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 520f);
            rt.anchoredPosition = new Vector2(0f, 120f);

            Image img = a.GetComponent<Image>();
            img.sprite = _radialSprite;
            img.color = new Color(0.55f, 0.35f, 0.85f, 0.22f);
            img.raycastTarget = false;

            CanvasGroup cg = a.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        private static Sprite CreateVerticalGradient(Color top, Color bottom)
        {
            const int w = 4;
            const int h = 128;
            Texture2D tex = new(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                Color c = Color.Lerp(bottom, top, t);
                for (int x = 0; x < w; x++) pixels[y * w + x] = c;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Crée un sprite radial procédural (alpha qui décroît du centre vers les bords).
        /// Utilisé pour les blobs et la vignette pour éviter les bords nets.
        /// </summary>
        private void CreateRadialSprite()
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
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a; // courbe douce
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            _radialSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>6 blobs colorés qui dérivent, alternant 2 couleurs chacun.</summary>
        private void CreateBlobs(RectTransform parent)
        {
            // Palette vibrante façon Balatro : magenta, cyan, rose, ambre, violet, turquoise
            Color[] baseColors =
            {
                new(0.55f, 0.25f, 0.95f, 0.42f),  // violet vif
                new(0.20f, 0.80f, 0.95f, 0.38f),  // cyan
                new(0.95f, 0.35f, 0.70f, 0.36f),  // rose
                new(1.00f, 0.65f, 0.25f, 0.32f),  // ambre chaud
                new(0.30f, 0.95f, 0.70f, 0.28f),  // turquoise
                new(0.85f, 0.30f, 0.45f, 0.34f),  // framboise
            };
            Color[] altColors =
            {
                new(0.30f, 0.20f, 0.95f, 0.42f),  // bleu électrique
                new(0.50f, 0.90f, 0.95f, 0.38f),  // azur
                new(0.95f, 0.55f, 0.85f, 0.36f),  // lilas
                new(0.95f, 0.85f, 0.35f, 0.32f),  // or
                new(0.50f, 0.95f, 0.85f, 0.28f),  // menthe
                new(1.00f, 0.45f, 0.35f, 0.34f),  // corail
            };

            Vector2[] origins =
            {
                new(-500f, 220f),
                new(500f, -180f),
                new(-420f, -260f),
                new(420f, 300f),
                new(0f, 120f),
                new(-80f, -360f),
            };

            float[] sizes = { 1100f, 1050f, 900f, 950f, 800f, 900f };

            for (int i = 0; i < BlobCount; i++)
            {
                GameObject blobObj = new($"Blob_{i}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                blobObj.transform.SetParent(parent, false);

                RectTransform rt = blobObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(sizes[i], sizes[i]);
                rt.anchoredPosition = origins[i];

                Image img = blobObj.GetComponent<Image>();
                img.sprite = _radialSprite;
                img.color = baseColors[i];
                img.raycastTarget = false;
                img.type = Image.Type.Simple;

                CanvasGroup cg = blobObj.GetComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                cg.interactable = false;
                cg.alpha = 1f;

                _blobs[i] = rt;
                _blobImages[i] = img;
                _blobBaseColors[i] = baseColors[i];
                _blobAltColors[i] = altColors[i];
                _blobOrigins[i] = origins[i];
                _blobAmplitudes[i] = new Vector2(Random.Range(80f, 170f), Random.Range(60f, 130f));
                _phaseOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
                _speeds[i] = Random.Range(0.10f, 0.24f);
                _blobGroups[i] = cg;
            }
        }

        /// <summary>Grille subtile en lignes horizontales pour la profondeur.</summary>
        private static void CreateGrid(RectTransform parent)
        {
            const int lineCount = 14;
            for (int i = 0; i < lineCount; i++)
            {
                GameObject lineObj = new($"GridLine_{i}", typeof(RectTransform), typeof(Image));
                lineObj.transform.SetParent(parent, false);

                RectTransform rt = lineObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.sizeDelta = new Vector2(0f, 1f);
                float yOffset = (i - lineCount * 0.5f) * 60f;
                rt.anchoredPosition = new Vector2(0f, yOffset);

                Image img = lineObj.GetComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.018f);
                img.raycastTarget = false;
            }
        }

        /// <summary>Vignette sombre sur les bords.</summary>
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
            img.color = new Color(0f, 0f, 0f, 0.35f);
            img.raycastTarget = false;
            // Réutilise le sprite radial mais inversé via une nouvelle texture
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
                    float a = Mathf.Clamp01(d * 1.1f);
                    a = a * a;
                    pixels[y * size + x] = new Color(0f, 0f, 0f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void Update()
        {
            _time += Time.deltaTime;

            for (int i = 0; i < BlobCount; i++)
            {
                if (_blobs[i] == null) continue;

                float phase = _phaseOffsets[i] + _time * _speeds[i];
                float dx = Mathf.Sin(phase) * _blobAmplitudes[i].x;
                float dy = Mathf.Cos(phase * 0.83f) * _blobAmplitudes[i].y;
                _blobs[i].anchoredPosition = _blobOrigins[i] + new Vector2(dx, dy);

                if (_blobGroups[i] != null)
                {
                    float pulse = 0.75f + Mathf.Sin(phase * 1.3f) * 0.25f;
                    _blobGroups[i].alpha = pulse;
                }

                // Cycle lent entre les 2 teintes — rend le fond vivant sans effet de flash.
                if (_blobImages[i] != null)
                {
                    float t = 0.5f + 0.5f * Mathf.Sin(phase * 0.35f);
                    _blobImages[i].color = Color.Lerp(_blobBaseColors[i], _blobAltColors[i], t);
                }

                // Léger "breathing" d'échelle pour plus de vie
                float scale = 1f + Mathf.Sin(phase * 0.9f) * 0.06f;
                _blobs[i].localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
