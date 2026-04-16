using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Bannière de transition de tour cinématique :
    /// - Bande full-width avec dégradé de la couleur du joueur
    /// - Nom du joueur + objectif de niveau
    /// - Slide-in avec overshoot, hold, slide-out
    /// - Ligne lumineuse qui balaie la bannière (sweep)
    /// Affiche aussi un indicateur "AI thinking..." animé.
    /// </summary>
    public class TurnBannerView : MonoBehaviour
    {
        [SerializeField] private Canvas? _canvas;
        [SerializeField] private AnimationController? _animController;

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

        // Noms des objectifs par niveau (pour affichage rapide)
        private static readonly string[] LevelObjectives =
        {
            "2 suites de 3",
            "1 suite + 1 brelan",
            "2 brelans",
            "1 suite de 4 + 1 paire",
            "1 flush de 5",
            "1 suite de 5",
            "1 carre + 1 paire",
            "1 flush de 7"
        };

        private GameManager? _gameManager;

        public void Initialize(GameManager gm)
        {
            _gameManager = gm;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<AIThinkingEvent>(OnAIThinking);
            EventBus.Subscribe<LevelLaidDownEvent>(OnLevelLaidDown);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<AIThinkingEvent>(OnAIThinking);
            EventBus.Unsubscribe<LevelLaidDownEvent>(OnLevelLaidDown);
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            ShowTurnBanner(evt.PlayerIndex, evt.PlayerLevel);
        }

        private void OnAIThinking(AIThinkingEvent evt)
        {
            ShowThinking(evt.IsThinking);
        }

        private void OnLevelLaidDown(LevelLaidDownEvent evt)
        {
            ShowLevelCompleteBanner(evt.PlayerIndex, evt.Level);
        }

        /// <summary>
        /// Affiche la bannière de transition de tour cinématique.
        /// </summary>
        public void ShowTurnBanner(int playerIndex, int playerLevel)
        {
            if (_canvas == null) return;

            if (_bannerCoroutine != null) StopCoroutine(_bannerCoroutine);
            if (_bannerObj != null) Destroy(_bannerObj);

            _bannerCoroutine = StartCoroutine(TurnBannerCoroutine(playerIndex, playerLevel));
        }

        /// <summary>
        /// Bannière de célébration quand un joueur pose son niveau.
        /// </summary>
        public void ShowLevelCompleteBanner(int playerIndex, int level)
        {
            if (_canvas == null) return;
            StartCoroutine(LevelCompleteBannerCoroutine(playerIndex, level));
        }

        /// <summary>
        /// Indicateur AI thinking avec dots animés.
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

        // ═══════════════════════════════════════════════
        //  TURN BANNER — Cinématique
        // ═══════════════════════════════════════════════

        private IEnumerator TurnBannerCoroutine(int playerIndex, int playerLevel)
        {
            Color playerColor = playerIndex < PlayerColors.Length
                ? PlayerColors[playerIndex]
                : Constants.TextPrimary;

            string objective = (playerLevel >= 1 && playerLevel <= LevelObjectives.Length)
                ? LevelObjectives[playerLevel - 1]
                : "";

            // ── Container principal ──
            _bannerObj = new GameObject("TurnBanner", typeof(RectTransform), typeof(CanvasGroup));
            _bannerObj.transform.SetParent(_canvas!.transform, false);
            _bannerObj.transform.SetAsLastSibling();

            RectTransform bannerRt = _bannerObj.GetComponent<RectTransform>();
            bannerRt.anchorMin = new Vector2(0f, 0.4f);
            bannerRt.anchorMax = new Vector2(1f, 0.6f);
            bannerRt.offsetMin = Vector2.zero;
            bannerRt.offsetMax = Vector2.zero;

            CanvasGroup bannerCg = _bannerObj.GetComponent<CanvasGroup>();
            bannerCg.blocksRaycasts = false;
            bannerCg.interactable = false;
            bannerCg.alpha = 0f;

            // ── Fond dégradé (couleur joueur → transparent) ──
            GameObject bgObj = new("Bg", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(_bannerObj.transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            Image bgImg = bgObj.GetComponent<Image>();
            Color bgColor = playerColor;
            bgColor.a = 0.2f;
            bgImg.color = bgColor;
            bgImg.raycastTarget = false;

            // ── Barre lumineuse supérieure ──
            GameObject topBar = new("TopBar", typeof(RectTransform), typeof(Image));
            topBar.transform.SetParent(_bannerObj.transform, false);
            RectTransform topBarRt = topBar.GetComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0, 1);
            topBarRt.anchorMax = new Vector2(1, 1);
            topBarRt.sizeDelta = new Vector2(0, 2f);
            topBarRt.anchoredPosition = Vector2.zero;
            Image topBarImg = topBar.GetComponent<Image>();
            topBarImg.color = Color.Lerp(playerColor, Color.white, 0.3f);
            topBarImg.raycastTarget = false;

            // ── Barre lumineuse inférieure ──
            GameObject botBar = new("BotBar", typeof(RectTransform), typeof(Image));
            botBar.transform.SetParent(_bannerObj.transform, false);
            RectTransform botBarRt = botBar.GetComponent<RectTransform>();
            botBarRt.anchorMin = new Vector2(0, 0);
            botBarRt.anchorMax = new Vector2(1, 0);
            botBarRt.sizeDelta = new Vector2(0, 2f);
            botBarRt.anchoredPosition = Vector2.zero;
            Image botBarImg = botBar.GetComponent<Image>();
            botBarImg.color = Color.Lerp(playerColor, Color.white, 0.3f);
            botBarImg.raycastTarget = false;

            // ── Texte joueur (centre) ──
            GameObject titleObj = new("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(_bannerObj.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 0.45f);
            titleRt.anchorMax = new Vector2(1, 1f);
            titleRt.sizeDelta = Vector2.zero;
            titleRt.anchoredPosition = Vector2.zero;

            TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            titleTmp.text = $"PLAYER {playerIndex + 1}";
            titleTmp.fontSize = 42;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.raycastTarget = false;
            titleTmp.outlineWidth = 0.2f;
            titleTmp.outlineColor = new Color32(0, 0, 0, 180);

            // ── Sous-titre : objectif du niveau ──
            GameObject subtitleObj = new("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subtitleObj.transform.SetParent(_bannerObj.transform, false);
            RectTransform subRt = subtitleObj.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0, 0f);
            subRt.anchorMax = new Vector2(1, 0.5f);
            subRt.sizeDelta = Vector2.zero;
            subRt.anchoredPosition = Vector2.zero;

            TextMeshProUGUI subTmp = subtitleObj.GetComponent<TextMeshProUGUI>();
            subTmp.text = $"Niveau {playerLevel}  —  {objective}";
            subTmp.fontSize = 20;
            subTmp.color = Color.Lerp(playerColor, Color.white, 0.5f);
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.fontStyle = FontStyles.Italic;
            subTmp.raycastTarget = false;

            // ── Sweep lumineux (barre qui traverse) ──
            GameObject sweepObj = new("Sweep", typeof(RectTransform), typeof(Image));
            sweepObj.transform.SetParent(_bannerObj.transform, false);
            RectTransform sweepRt = sweepObj.GetComponent<RectTransform>();
            sweepRt.anchorMin = new Vector2(0, 0);
            sweepRt.anchorMax = new Vector2(0, 1);
            sweepRt.sizeDelta = new Vector2(120f, 0);
            sweepRt.anchoredPosition = new Vector2(-60f, 0);

            Image sweepImg = sweepObj.GetComponent<Image>();
            Color sweepColor = Color.white;
            sweepColor.a = 0.1f;
            sweepImg.color = sweepColor;
            sweepImg.raycastTarget = false;

            // ═══ ANIMATION ═══

            // Phase 1 : Fade-in + scale Y (bandeau s'ouvre)
            bannerRt.localScale = new Vector3(1f, 0f, 1f);
            float elapsed = 0f;
            float fadeInDuration = 0.2f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                float eased = UITween.EaseOutCubic(t);

                bannerCg.alpha = eased;
                bannerRt.localScale = new Vector3(1f, eased, 1f);
                yield return null;
            }
            bannerCg.alpha = 1f;
            bannerRt.localScale = Vector3.one;

            // Phase 2 : Titre slide-in avec overshoot
            titleRt.anchoredPosition = new Vector2(-300f, titleRt.anchoredPosition.y);
            subRt.anchoredPosition = new Vector2(300f, subRt.anchoredPosition.y);

            elapsed = 0f;
            float slideDuration = 0.35f;

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                float eased = UITween.EaseOutBack(t);

                titleRt.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-300f, 0f, eased), 0);
                subRt.anchoredPosition = new Vector2(Mathf.LerpUnclamped(300f, 0f, eased), 0);
                yield return null;
            }
            titleRt.anchoredPosition = Vector2.zero;
            subRt.anchoredPosition = Vector2.zero;

            // Phase 3 : Sweep lumineux traverse
            elapsed = 0f;
            float sweepDuration = 0.5f;
            float canvasWidth = _canvas.GetComponent<RectTransform>().rect.width;

            while (elapsed < sweepDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / sweepDuration);
                float eased = UITween.EaseInOutCubic(t);

                float x = Mathf.Lerp(-120f, canvasWidth + 120f, eased);
                sweepRt.anchoredPosition = new Vector2(x, 0);

                float alpha = t < 0.5f ? t * 2f : (1f - t) * 2f;
                sweepColor.a = alpha * 0.15f;
                sweepImg.color = sweepColor;

                yield return null;
            }

            // Hold
            yield return new WaitForSeconds(0.4f);

            // Phase 4 : Fade-out + scale Y (bandeau se ferme)
            elapsed = 0f;
            float fadeOutDuration = 0.25f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                float eased = UITween.EaseInQuart(t);

                bannerCg.alpha = 1f - eased;
                bannerRt.localScale = new Vector3(1f, 1f - eased, 1f);
                yield return null;
            }

            Destroy(_bannerObj);
            _bannerObj = null;
            _bannerCoroutine = null;
        }

        // ═══════════════════════════════════════════════
        //  LEVEL COMPLETE BANNER — Célébration
        // ═══════════════════════════════════════════════

        private IEnumerator LevelCompleteBannerCoroutine(int playerIndex, int level)
        {
            Color playerColor = playerIndex < PlayerColors.Length
                ? PlayerColors[playerIndex]
                : Constants.TextAccent;

            // Flash écran
            if (_animController != null)
            {
                Color flashColor = playerColor;
                flashColor.a = 0.25f;
                _animController.AnimateScreenFlash(flashColor, 0.4f);
            }

            yield return new WaitForSeconds(0.1f);

            // Container
            GameObject celebObj = new("LevelCelebration", typeof(RectTransform), typeof(CanvasGroup));
            celebObj.transform.SetParent(_canvas!.transform, false);
            celebObj.transform.SetAsLastSibling();

            RectTransform celebRt = celebObj.GetComponent<RectTransform>();
            celebRt.anchoredPosition = Vector2.zero;
            celebRt.sizeDelta = new Vector2(500f, 100f);

            CanvasGroup celebCg = celebObj.GetComponent<CanvasGroup>();
            celebCg.blocksRaycasts = false;
            celebCg.interactable = false;
            celebCg.alpha = 0f;

            // Fond glow
            GameObject glowBg = new("GlowBg", typeof(RectTransform), typeof(Image));
            glowBg.transform.SetParent(celebObj.transform, false);
            RectTransform glowRt = glowBg.GetComponent<RectTransform>();
            glowRt.anchorMin = Vector2.zero;
            glowRt.anchorMax = Vector2.one;
            glowRt.sizeDelta = new Vector2(40f, 20f);

            Image glowImg = glowBg.GetComponent<Image>();
            Color glowColor = playerColor;
            glowColor.a = 0.3f;
            glowImg.color = glowColor;
            glowImg.raycastTarget = false;

            // Texte "NIVEAU X !"
            GameObject textObj = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(celebObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
            text.text = $"NIVEAU {level} !";
            text.fontSize = 52;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.raycastTarget = false;
            text.outlineWidth = 0.25f;
            text.outlineColor = new Color32(0, 0, 0, 200);

            // Animation : pop-in élastique
            celebRt.localScale = Vector3.zero;
            float elapsed = 0f;
            float popDuration = 0.5f;

            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);
                float eased = UITween.EaseOutElastic(t);

                celebRt.localScale = Vector3.one * eased;
                celebCg.alpha = Mathf.Clamp01(t * 4f);
                yield return null;
            }
            celebRt.localScale = Vector3.one;
            celebCg.alpha = 1f;

            // Particules
            if (_animController != null)
            {
                _animController.SpawnParticleBurst(Vector2.zero, playerColor, 20);
                yield return new WaitForSeconds(0.15f);
                _animController.SpawnParticleBurst(
                    new Vector2(-100f, 20f),
                    Color.Lerp(playerColor, Color.white, 0.3f), 10);
                _animController.SpawnParticleBurst(
                    new Vector2(100f, -10f),
                    Color.Lerp(playerColor, Color.yellow, 0.2f), 10);
            }

            // Pulse du texte
            elapsed = 0f;
            float pulseDuration = 0.3f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / pulseDuration);
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.08f;
                celebRt.localScale = Vector3.one * scale;
                yield return null;
            }

            // Hold
            yield return new WaitForSeconds(0.8f);

            // Fade-out
            elapsed = 0f;
            float fadeOut = 0.4f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOut);
                celebCg.alpha = 1f - t;
                celebRt.localScale = Vector3.one * (1f + t * 0.3f);
                yield return null;
            }

            Destroy(celebObj);
        }

        // ═══════════════════════════════════════════════
        //  AI THINKING DOTS
        // ═══════════════════════════════════════════════

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

            // Label "AI"
            GameObject labelObj = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(_thinkingObj.transform, false);
            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchoredPosition = new Vector2(-40f, 0);
            labelRt.sizeDelta = new Vector2(60f, 30f);
            TextMeshProUGUI label = labelObj.GetComponent<TextMeshProUGUI>();
            label.text = "AI";
            label.fontSize = 16;
            label.color = Constants.TextSecondary;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;

            // Dots
            Image[] dots = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject dotObj = new($"Dot_{i}", typeof(RectTransform), typeof(Image));
                dotObj.transform.SetParent(_thinkingObj.transform, false);

                RectTransform dotRt = dotObj.GetComponent<RectTransform>();
                dotRt.anchoredPosition = new Vector2(-5f + i * 18f, 0);
                dotRt.sizeDelta = new Vector2(8f, 8f);

                Image dotImg = dotObj.GetComponent<Image>();
                dotImg.color = Constants.TextSecondary;
                dotImg.raycastTarget = false;
                dots[i] = dotImg;
            }

            // Fade-in
            thinkCg.alpha = 0f;
            float fadeIn = 0f;
            while (fadeIn < 0.3f)
            {
                fadeIn += Time.deltaTime;
                thinkCg.alpha = Mathf.Clamp01(fadeIn / 0.3f);
                yield return null;
            }
            thinkCg.alpha = 1f;

            // Animate dots bouncing
            float time = 0f;
            while (_thinkingObj != null)
            {
                time += Time.deltaTime;

                for (int i = 0; i < 3; i++)
                {
                    if (dots[i] == null) yield break;

                    float phase = time * 3.5f - i * 0.6f;
                    float bounce = Mathf.Max(0f, Mathf.Sin(phase)) * 10f;
                    float alpha = 0.3f + Mathf.Max(0f, Mathf.Sin(phase)) * 0.7f;
                    float scale = 1f + Mathf.Max(0f, Mathf.Sin(phase)) * 0.3f;

                    RectTransform dotRt = dots[i].GetComponent<RectTransform>();
                    dotRt.anchoredPosition = new Vector2(-5f + i * 18f, bounce);
                    dotRt.localScale = Vector3.one * scale;
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
