using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Bannière de transition de tour style Balatro : le nom du joueur
    /// slide depuis le côté avec un flash, puis disparaît.
    /// Affiche aussi un indicateur "AI thinking..." pour les bots.
    /// </summary>
    public class TurnBannerView : MonoBehaviour
    {
        [SerializeField] private Canvas? _canvas;

        private GameObject? _bannerObj;
        private GameObject? _thinkingObj;
        private Coroutine? _bannerCoroutine;
        private Coroutine? _thinkingCoroutine;

        // Couleurs par joueur
        private static readonly Color[] PlayerColors =
        {
            Constants.CardBlue,
            Constants.CardRed,
            Constants.CardGreen,
            Constants.CardPurple,
            Constants.CardOrange,
            Constants.CardYellow
        };

        private void OnEnable()
        {
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<AIThinkingEvent>(OnAIThinking);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<AIThinkingEvent>(OnAIThinking);
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            ShowTurnBanner(evt.PlayerIndex);
        }

        private void OnAIThinking(AIThinkingEvent evt)
        {
            ShowThinking(evt.IsThinking);
        }

        /// <summary>
        /// Affiche la bannière de transition avec le nom du joueur.
        /// </summary>
        public void ShowTurnBanner(int playerIndex)
        {
            if (_canvas == null) return;

            if (_bannerCoroutine != null) StopCoroutine(_bannerCoroutine);
            if (_bannerObj != null) Destroy(_bannerObj);

            _bannerCoroutine = StartCoroutine(BannerCoroutine(playerIndex));
        }

        /// <summary>
        /// Affiche l'indicateur "AI thinking..." avec des dots animés.
        /// </summary>
        public void ShowThinking(bool show)
        {
            if (_thinkingCoroutine != null)
            {
                StopCoroutine(_thinkingCoroutine);
                _thinkingCoroutine = null;
            }

            if (_thinkingObj != null)
            {
                Destroy(_thinkingObj);
                _thinkingObj = null;
            }

            if (show && _canvas != null)
            {
                _thinkingCoroutine = StartCoroutine(ThinkingDotsCoroutine());
            }
        }

        private IEnumerator BannerCoroutine(int playerIndex)
        {
            Color playerColor = playerIndex < PlayerColors.Length
                ? PlayerColors[playerIndex]
                : Constants.TextPrimary;

            // Créer le banner
            _bannerObj = new GameObject("TurnBanner", typeof(RectTransform), typeof(CanvasGroup));
            _bannerObj.transform.SetParent(_canvas!.transform, false);
            _bannerObj.transform.SetAsLastSibling();

            RectTransform bannerRt = _bannerObj.GetComponent<RectTransform>();
            bannerRt.anchoredPosition = Vector2.zero;
            bannerRt.sizeDelta = new Vector2(600f, 70f);

            CanvasGroup bannerCg = _bannerObj.GetComponent<CanvasGroup>();
            bannerCg.blocksRaycasts = false;
            bannerCg.interactable = false;

            // Fond du banner
            GameObject bgObj = new("Bg", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(_bannerObj.transform, false);

            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            Image bgImg = bgObj.GetComponent<Image>();
            Color bgColor = playerColor;
            bgColor.a = 0.15f;
            bgImg.color = bgColor;
            bgImg.raycastTarget = false;

            // Barre de couleur à gauche
            GameObject barObj = new("Bar", typeof(RectTransform), typeof(Image));
            barObj.transform.SetParent(_bannerObj.transform, false);

            RectTransform barRt = barObj.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 0);
            barRt.anchorMax = new Vector2(0, 1);
            barRt.sizeDelta = new Vector2(4f, 0);
            barRt.anchoredPosition = new Vector2(2f, 0);

            Image barImg = barObj.GetComponent<Image>();
            barImg.color = playerColor;
            barImg.raycastTarget = false;

            // Texte
            GameObject textObj = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(_bannerObj.transform, false);

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            textRt.anchoredPosition = new Vector2(20f, 0);

            TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
            text.text = $"PLAYER {playerIndex + 1}";
            text.fontSize = 32;
            text.color = playerColor;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.raycastTarget = false;

            // Animation : slide in from left
            float slideDistance = 800f;
            bannerRt.anchoredPosition = new Vector2(-slideDistance, 0);
            bannerCg.alpha = 0f;

            // Slide in
            float elapsed = 0f;
            float slideInDuration = 0.3f;

            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideInDuration);
                // Overshoot ease
                float eased = t < 0.7f
                    ? Mathf.Lerp(0f, 1.05f, t / 0.7f)
                    : Mathf.Lerp(1.05f, 1f, (t - 0.7f) / 0.3f);

                bannerRt.anchoredPosition = new Vector2(Mathf.Lerp(-slideDistance, 0f, eased), 0);
                bannerCg.alpha = Mathf.Min(1f, t * 3f);
                yield return null;
            }

            bannerRt.anchoredPosition = Vector2.zero;
            bannerCg.alpha = 1f;

            // Hold
            yield return new WaitForSeconds(0.6f);

            // Slide out to right
            elapsed = 0f;
            float slideOutDuration = 0.25f;

            while (elapsed < slideOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideOutDuration);

                bannerRt.anchoredPosition = new Vector2(Mathf.Lerp(0f, slideDistance, t * t), 0);
                bannerCg.alpha = 1f - t;
                yield return null;
            }

            Destroy(_bannerObj);
            _bannerObj = null;
            _bannerCoroutine = null;
        }

        private IEnumerator ThinkingDotsCoroutine()
        {
            _thinkingObj = new GameObject("ThinkingDots", typeof(RectTransform), typeof(CanvasGroup));
            _thinkingObj.transform.SetParent(_canvas!.transform, false);
            _thinkingObj.transform.SetAsLastSibling();

            RectTransform thinkRt = _thinkingObj.GetComponent<RectTransform>();
            thinkRt.anchoredPosition = new Vector2(0, -180f);
            thinkRt.sizeDelta = new Vector2(200f, 40f);

            CanvasGroup thinkCg = _thinkingObj.GetComponent<CanvasGroup>();
            thinkCg.blocksRaycasts = false;
            thinkCg.interactable = false;

            // Dots
            Image[] dots = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject dotObj = new($"Dot_{i}", typeof(RectTransform), typeof(Image));
                dotObj.transform.SetParent(_thinkingObj.transform, false);

                RectTransform dotRt = dotObj.GetComponent<RectTransform>();
                dotRt.anchoredPosition = new Vector2(-20f + i * 20f, 0);
                dotRt.sizeDelta = new Vector2(10f, 10f);

                Image dotImg = dotObj.GetComponent<Image>();
                dotImg.color = Constants.TextSecondary;
                dotImg.raycastTarget = false;
                dots[i] = dotImg;
            }

            // Animate dots bouncing
            float time = 0f;
            while (_thinkingObj != null)
            {
                time += Time.deltaTime;

                for (int i = 0; i < 3; i++)
                {
                    if (dots[i] == null) yield break;

                    float phase = time * 4f - i * 0.5f;
                    float bounce = Mathf.Max(0f, Mathf.Sin(phase)) * 8f;
                    float alpha = 0.4f + Mathf.Max(0f, Mathf.Sin(phase)) * 0.6f;

                    RectTransform dotRt = dots[i].GetComponent<RectTransform>();
                    dotRt.anchoredPosition = new Vector2(-20f + i * 20f, bounce);
                    dots[i].color = new Color(
                        Constants.TextSecondary.r,
                        Constants.TextSecondary.g,
                        Constants.TextSecondary.b,
                        alpha);
                }

                yield return null;
            }
        }

        private void OnDestroy()
        {
            if (_bannerObj != null) Destroy(_bannerObj);
            if (_thinkingObj != null) Destroy(_thinkingObj);
        }
    }
}
