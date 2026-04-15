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
        private const int BlobCount = 4;

        private RectTransform[] _blobs = new RectTransform[BlobCount];
        private Vector2[] _blobOrigins = new Vector2[BlobCount];
        private Vector2[] _blobAmplitudes = new Vector2[BlobCount];
        private float[] _phaseOffsets = new float[BlobCount];
        private float[] _speeds = new float[BlobCount];
        private CanvasGroup[] _blobGroups = new CanvasGroup[BlobCount];

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
            CreateBlobs(_root);
            CreateGrid(_root);
            CreateVignette(_root);
        }

        /// <summary>Couche de base : couleur sombre uniforme.</summary>
        private static void CreateBaseGradient(RectTransform parent)
        {
            GameObject baseObj = new("BaseColor", typeof(RectTransform), typeof(Image));
            baseObj.transform.SetParent(parent, false);

            RectTransform rt = baseObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image img = baseObj.GetComponent<Image>();
            img.color = new Color32(0x07, 0x0C, 0x16, 0xFF);
            img.raycastTarget = false;
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

        /// <summary>4 blobs colorés qui dérivent.</summary>
        private void CreateBlobs(RectTransform parent)
        {
            Color[] colors =
            {
                new(Constants.CardBlue.r, Constants.CardBlue.g, Constants.CardBlue.b, 0.18f),
                new(Constants.CardPurple.r, Constants.CardPurple.g, Constants.CardPurple.b, 0.16f),
                new(Constants.CardGreen.r, Constants.CardGreen.g, Constants.CardGreen.b, 0.12f),
                new(Constants.CardOrange.r, Constants.CardOrange.g, Constants.CardOrange.b, 0.10f),
            };

            Vector2[] origins =
            {
                new(-380f, 200f),
                new(420f, -150f),
                new(-300f, -250f),
                new(350f, 280f),
            };

            for (int i = 0; i < BlobCount; i++)
            {
                GameObject blobObj = new($"Blob_{i}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                blobObj.transform.SetParent(parent, false);

                RectTransform rt = blobObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(900f, 900f);
                rt.anchoredPosition = origins[i];

                Image img = blobObj.GetComponent<Image>();
                img.sprite = _radialSprite;
                img.color = colors[i];
                img.raycastTarget = false;
                img.type = Image.Type.Simple;

                CanvasGroup cg = blobObj.GetComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                cg.interactable = false;
                cg.alpha = 1f;

                _blobs[i] = rt;
                _blobOrigins[i] = origins[i];
                _blobAmplitudes[i] = new Vector2(Random.Range(60f, 130f), Random.Range(50f, 100f));
                _phaseOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
                _speeds[i] = Random.Range(0.12f, 0.28f);
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
                    float pulse = 0.85f + Mathf.Sin(phase * 1.3f) * 0.15f;
                    _blobGroups[i].alpha = pulse;
                }
            }
        }
    }
}
