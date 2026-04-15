using UnityEngine;
using UnityEngine.UI;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Affiche un glow coloré sur les bords de l'écran selon le joueur actif.
    /// 4 bandes (haut, bas, gauche, droite) avec un fade radial vers l'intérieur.
    /// Pulse subtilement pour donner le rythme du tour. Force visuelle clé pour
    /// que le joueur sache instantanément à qui c'est le tour.
    /// </summary>
    public class PlayerTurnGlow : MonoBehaviour
    {
        private static readonly Color[] PlayerColors =
        {
            Constants.CardBlue,
            Constants.CardRed,
            Constants.CardGreen,
            Constants.CardPurple,
            Constants.CardOrange,
            Constants.CardYellow
        };

        private Image[] _edges = new Image[4];
        private Color _currentColor = Constants.CardBlue;
        private float _intensity;
        private float _targetIntensity;
        private float _pulseTime;

        /// <summary>
        /// Construit les bordures sous le canvas donné.
        /// </summary>
        public void Setup(Canvas canvas)
        {
            if (canvas == null) return;

            GameObject root = new("PlayerTurnGlow", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            root.transform.SetAsFirstSibling(); // au-dessus du fond mais sous tout le reste
            root.transform.SetSiblingIndex(1); // juste après le fond

            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            CanvasGroup cg = root.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            Sprite gradient = CreateGradientSprite();

            // 4 bordures : top, bottom, left, right
            _edges[0] = CreateEdge(root.transform, "EdgeTop",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f),
                new Vector2(0, 180f), 180f, gradient);
            _edges[1] = CreateEdge(root.transform, "EdgeBottom",
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f),
                new Vector2(0, 180f), 0f, gradient);
            _edges[2] = CreateEdge(root.transform, "EdgeLeft",
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0f, 0.5f),
                new Vector2(180f, 0f), -90f, gradient);
            _edges[3] = CreateEdge(root.transform, "EdgeRight",
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1f, 0.5f),
                new Vector2(180f, 0f), 90f, gradient);

            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<RoundEndedEvent>(OnRoundEnded);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<RoundEndedEvent>(OnRoundEnded);
        }

        private static Image CreateEdge(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 sizeDelta, float rotationZ, Sprite sprite)
        {
            GameObject obj = new(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.localRotation = Quaternion.Euler(0, 0, rotationZ);

            Image img = obj.GetComponent<Image>();
            img.sprite = sprite;
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = false;
            img.type = Image.Type.Simple;

            return img;
        }

        /// <summary>
        /// Crée un sprite gradient (alpha 1 en bas, alpha 0 en haut), réutilisable
        /// avec rotation pour les 4 bords.
        /// </summary>
        private static Sprite CreateGradientSprite()
        {
            const int width = 4;
            const int height = 64;
            Texture2D tex = new(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                float a = Mathf.Pow(t, 1.6f); // courbe douce
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            int colorIdx = evt.PlayerIndex % PlayerColors.Length;
            _currentColor = PlayerColors[colorIdx];
            _targetIntensity = 1f;
        }

        private void OnRoundEnded(RoundEndedEvent evt)
        {
            _targetIntensity = 0f;
        }

        private void Update()
        {
            _pulseTime += Time.deltaTime;
            _intensity = Mathf.Lerp(_intensity, _targetIntensity, Time.deltaTime * 4f);

            float pulse = 0.7f + Mathf.Sin(_pulseTime * 2.2f) * 0.3f;
            float alpha = _intensity * pulse * 0.55f;

            Color c = _currentColor;
            c.a = alpha;

            for (int i = 0; i < _edges.Length; i++)
            {
                if (_edges[i] != null) _edges[i].color = c;
            }
        }
    }
}
