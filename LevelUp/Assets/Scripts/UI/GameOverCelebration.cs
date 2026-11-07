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

        /// <summary>Noms des joueurs (pour afficher "Bot 2 remporte la partie").</summary>
        public System.Collections.Generic.List<string>? PlayerNames { get; set; }

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
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            StopAllCoroutines();
        }

        /// <summary>
        /// Une nouvelle partie démarre (ex: replay lancé par l'hôte en ligne) :
        /// fermer l'overlay s'il est encore affiché sur cet appareil.
        /// </summary>
        private void OnGameStarted(GameStartedEvent evt)
        {
            if (_isShown) Hide();
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

            // Voile sombre
            GameObject veil = new("Veil", typeof(RectTransform), typeof(Image));
            veil.transform.SetParent(_root, false);
            UISkin.Stretch((RectTransform)veil.transform);
            veil.GetComponent<Image>().color = new Color(0.04f, 0.055f, 0.07f, 0.86f);

            // Panneau (maquette : #161B22 radius 28 + liseré)
            Image panel = UISkin.Rect(_root, "GOPanel", UISkin.Surface, 28f, raycast: true);
            _panel = panel.rectTransform;
            UISkin.Place(_panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(640f, 460f));
            UISkin.Outline(_panel, 28f, 1f, new Color(1f, 1f, 1f, 0.08f));

            // Étoile (symbole jaune de la DA) dans une pastille
            Image badge = UISkin.Rect(_panel, "TrophyBadge", UISkin.WithAlpha(Constants.CardYellow, 0.12f), 28f);
            RectTransform troRt = badge.rectTransform;
            UISkin.Place(troRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -88f),
                new Vector2(104f, 104f));
            UISkin.Outline(troRt, 28f, 1.5f, UISkin.WithAlpha(Constants.CardYellow, 0.45f));
            Image star = UISkin.SuitImage(badge.transform, "Star", CardColor.Yellow, 60f, Constants.CardYellow);
            UISkin.Place(star.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(60f, 60f));

            // Titre
            TextMeshProUGUI victory = UISkin.Text(_panel, "Victory", "VICTOIRE !", 54f, UISkin.TextColor,
                TextWeight.Display, TextAlignmentOptions.Center);
            UISkin.Place(victory.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -160f), new Vector2(580f, 70f));
            victory.characterSpacing = 2f;

            // Gagnant
            string winnerName = PlayerNames != null && winnerIndex >= 0 && winnerIndex < PlayerNames.Count
                ? PlayerNames[winnerIndex]
                : $"Joueur {winnerIndex + 1}";
            TextMeshProUGUI winnerLabel = UISkin.Text(_panel, "Winner",
                $"{winnerName} remporte la partie", 18f, UISkin.TextMuted, TextWeight.SemiBold,
                TextAlignmentOptions.Center);
            UISkin.Place(winnerLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -236f), new Vector2(600f, 26f));

            // Boutons : une action principale (REJOUER) + MENU
            GameObject row = new("Buttons", typeof(RectTransform));
            row.transform.SetParent(_panel, false);
            UISkin.Place((RectTransform)row.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 44f), new Vector2(552f, 64f));
            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            UIFactory.CreateButton(row.transform, "BtnReplay", "REJOUER",
                new Vector2(268f, 64f), HandleReplay, primary: true);
            UIFactory.CreateButton(row.transform, "BtnMenu", "MENU",
                new Vector2(268f, 64f), HandleMainMenu);

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
