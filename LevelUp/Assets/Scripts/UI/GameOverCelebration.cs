using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Écran de fin de partie immersif — overlay avec confettis, titre animé,
    /// boutons Rejouer/Menu principal. Remplace le panel basique de HUDView.
    /// </summary>
    public class GameOverCelebration : MonoBehaviour
    {
        private Canvas? _canvas;
        private RectTransform? _root;
        private CanvasGroup? _rootCg;
        private RectTransform? _panel;
        private bool _isShown;

        /// <summary>Déclenché quand le joueur clique sur Rejouer.</summary>
        public event Action? OnReplayClicked;

        /// <summary>Déclenché quand le joueur clique sur Menu principal.</summary>
        public event Action? OnMainMenuClicked;

        /// <summary>
        /// Prépare l'overlay sous le canvas. Écoute GameOverEvent pour s'afficher.
        /// </summary>
        public void Setup(Canvas canvas)
        {
            if (canvas == null) return;
            _canvas = canvas;

            GameObject rootObj = new("GameOverOverlay", typeof(RectTransform), typeof(CanvasGroup));
            rootObj.transform.SetParent(canvas.transform, false);
            rootObj.transform.SetAsLastSibling();

            _root = rootObj.GetComponent<RectTransform>();
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.sizeDelta = Vector2.zero;

            _rootCg = rootObj.GetComponent<CanvasGroup>();
            _rootCg.alpha = 0f;
            _rootCg.blocksRaycasts = false;

            _root.gameObject.SetActive(false);

            EventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            StopAllCoroutines();
        }

        private void OnGameOver(GameOverEvent evt)
        {
            Show(evt.WinnerIndex);
        }

        /// <summary>
        /// Affiche l'écran de fin de partie pour le gagnant donné.
        /// </summary>
        public void Show(int winnerIndex)
        {
            if (_isShown || _root == null || _rootCg == null) return;
            _isShown = true;

            _root.gameObject.SetActive(true);
            _rootCg.alpha = 0f;
            _rootCg.blocksRaycasts = true;

            // Reconstruire à chaque fois pour permettre replay
            ClearChildren(_root);

            // Veil sombre
            GameObject veil = new("Veil", typeof(RectTransform), typeof(Image));
            veil.transform.SetParent(_root, false);
            RectTransform vrt = veil.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.sizeDelta = Vector2.zero;
            Image vImg = veil.GetComponent<Image>();
            vImg.color = new Color(0f, 0f, 0f, 0.75f);

            // Panel
            GameObject panelObj = UIFactory.CreatePanel(_root, "GOPanel",
                Constants.PanelBackground, new Vector2(640f, 440f),
                withBorder: true, borderColor: Constants.CardYellow);
            _panel = panelObj.GetComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.anchoredPosition = Vector2.zero;
            UIFactory.AddDropShadow(_panel, 12f, 0.55f);

            // Trophée (étoile)
            TextMeshProUGUI trophy = UIFactory.CreateText(_panel, "Trophy", "★", 110f,
                Constants.CardYellow, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform troRt = trophy.rectTransform;
            troRt.anchorMin = new Vector2(0.5f, 1f);
            troRt.anchorMax = new Vector2(0.5f, 1f);
            troRt.pivot = new Vector2(0.5f, 1f);
            troRt.sizeDelta = new Vector2(160f, 140f);
            troRt.anchoredPosition = new Vector2(0f, -20f);
            trophy.outlineWidth = 0.3f;
            trophy.outlineColor = new Color32(120, 80, 0, 255);

            // Titre "VICTOIRE"
            TextMeshProUGUI victory = UIFactory.CreateText(_panel, "Victory", "VICTOIRE !", 54f,
                Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform virt = victory.rectTransform;
            virt.anchorMin = new Vector2(0.5f, 1f);
            virt.anchorMax = new Vector2(0.5f, 1f);
            virt.pivot = new Vector2(0.5f, 1f);
            virt.sizeDelta = new Vector2(580f, 80f);
            virt.anchoredPosition = new Vector2(0f, -160f);
            victory.characterSpacing = 16f;
            victory.enableVertexGradient = true;
            victory.colorGradient = new VertexGradient(
                Color.Lerp(Constants.CardYellow, Color.white, 0.4f),
                Color.Lerp(Constants.CardOrange, Color.white, 0.2f),
                Color.Lerp(Constants.CardPurple, Color.white, 0.15f),
                Color.Lerp(Constants.CardBlue, Color.white, 0.25f));

            // Nom du gagnant
            TextMeshProUGUI winnerLabel = UIFactory.CreateText(_panel, "Winner",
                $"PLAYER {winnerIndex + 1} REMPORTE LA PARTIE", 22f,
                Constants.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform wrt = winnerLabel.rectTransform;
            wrt.anchorMin = new Vector2(0.5f, 1f);
            wrt.anchorMax = new Vector2(0.5f, 1f);
            wrt.pivot = new Vector2(0.5f, 1f);
            wrt.sizeDelta = new Vector2(600f, 30f);
            wrt.anchoredPosition = new Vector2(0f, -240f);
            winnerLabel.characterSpacing = 10f;

            // Boutons
            GameObject row = new("Buttons", typeof(RectTransform));
            row.transform.SetParent(_panel, false);
            RectTransform rrt = row.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.5f, 0f);
            rrt.anchorMax = new Vector2(0.5f, 0f);
            rrt.pivot = new Vector2(0.5f, 0f);
            rrt.sizeDelta = new Vector2(560f, 80f);
            rrt.anchoredPosition = new Vector2(0f, 30f);

            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            UIFactory.CreateButton(row.transform, "BtnReplay", "REJOUER",
                new Vector2(260f, 72f), HandleReplay, Constants.CardGreen);

            UIFactory.CreateButton(row.transform, "BtnMenu", "MENU",
                new Vector2(260f, 72f), HandleMainMenu, Constants.CardRed);

            // Animations d'entrée
            _panel.localScale = Vector3.zero;
            UITween.ScaleTo(_root.gameObject, _panel, Vector3.one, 0.6f);
            UITween.FadeTo(_root.gameObject, _rootCg, 1f, 0.4f);

            // Trophée qui bounce
            StartCoroutine(TrophyBounce(troRt));

            // Confettis
            StartCoroutine(ConfettiBurst());
        }

        private IEnumerator TrophyBounce(RectTransform rt)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            float t = 0f;
            Vector3 baseScale = Vector3.one;
            while (_isShown && rt != null)
            {
                t += Time.unscaledDeltaTime;
                float s = 1f + Mathf.Sin(t * 3f) * 0.08f;
                float rot = Mathf.Sin(t * 2f) * 8f;
                rt.localScale = baseScale * s;
                rt.localRotation = Quaternion.Euler(0, 0, rot);
                yield return null;
            }
        }

        private IEnumerator ConfettiBurst()
        {
            if (_canvas == null) yield break;

            Color[] palette =
            {
                Constants.CardRed, Constants.CardBlue, Constants.CardGreen,
                Constants.CardYellow, Constants.CardPurple, Constants.CardOrange
            };

            // 4 vagues successives de confettis
            for (int wave = 0; wave < 4; wave++)
            {
                SpawnConfettiWave(palette, 30);
                yield return new WaitForSecondsRealtime(0.8f);
            }
        }

        private void SpawnConfettiWave(Color[] palette, int count)
        {
            if (_root == null) return;

            // Dimensions écran
            Rect canvasRect = _root.rect;
            float halfW = canvasRect.width * 0.5f;
            float halfH = canvasRect.height * 0.5f;

            for (int i = 0; i < count; i++)
            {
                GameObject c = new($"Confetti_{i}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                c.transform.SetParent(_root, false);
                c.transform.SetAsLastSibling();

                RectTransform rt = c.GetComponent<RectTransform>();
                float startX = UnityEngine.Random.Range(-halfW, halfW);
                rt.anchoredPosition = new Vector2(startX, halfH + 30f);
                float size = UnityEngine.Random.Range(8f, 18f);
                rt.sizeDelta = new Vector2(size, size * UnityEngine.Random.Range(0.4f, 1.2f));
                rt.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f));

                Image img = c.GetComponent<Image>();
                img.sprite = UIFactory.RoundedSprite;
                img.type = Image.Type.Sliced;
                img.color = palette[UnityEngine.Random.Range(0, palette.Length)];
                img.raycastTarget = false;

                StartCoroutine(AnimateConfetti(c, rt, c.GetComponent<CanvasGroup>(), -halfH - 50f));
            }
        }

        private IEnumerator AnimateConfetti(GameObject obj, RectTransform rt, CanvasGroup cg, float minY)
        {
            float fallSpeed = UnityEngine.Random.Range(200f, 400f);
            float swayAmp = UnityEngine.Random.Range(30f, 80f);
            float swayFreq = UnityEngine.Random.Range(1f, 2.5f);
            float spinSpeed = UnityEngine.Random.Range(-180f, 180f);
            Vector2 startPos = rt.anchoredPosition;
            float t = 0f;
            float lifetime = UnityEngine.Random.Range(2.5f, 4f);

            while (t < lifetime && rt != null && obj != null)
            {
                t += Time.unscaledDeltaTime;
                float y = startPos.y - fallSpeed * t;
                float x = startPos.x + Mathf.Sin(t * swayFreq) * swayAmp;
                rt.anchoredPosition = new Vector2(x, y);
                rt.localRotation = Quaternion.Euler(0, 0, t * spinSpeed);

                if (cg != null && t > lifetime - 0.6f)
                {
                    cg.alpha = Mathf.Lerp(1f, 0f, (t - (lifetime - 0.6f)) / 0.6f);
                }

                if (y < minY) break;
                yield return null;
            }

            if (obj != null) Destroy(obj);
        }

        private void HandleReplay()
        {
            Hide(() => OnReplayClicked?.Invoke());
        }

        private void HandleMainMenu()
        {
            Hide(() => OnMainMenuClicked?.Invoke());
        }

        private void Hide(Action? onComplete = null)
        {
            if (!_isShown || _root == null || _rootCg == null)
            {
                onComplete?.Invoke();
                return;
            }
            _isShown = false;
            _rootCg.blocksRaycasts = false;
            UITween.FadeTo(_root.gameObject, _rootCg, 0f, 0.35f, () =>
            {
                if (_root != null) _root.gameObject.SetActive(false);
                StopAllCoroutines();
                onComplete?.Invoke();
            });
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
