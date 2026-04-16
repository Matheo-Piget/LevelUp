using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Overlay cinématique pour les transitions de round :
    /// - Round start : voile noir → texte "ROUND N" scale-in → voile s'ouvre
    /// - Round end : flash → "Player X WIN" → voile noir → nouveau round
    /// Écoute RoundStartedEvent et RoundEndedEvent via EventBus.
    /// </summary>
    public class RoundTransitionView : MonoBehaviour
    {
        [SerializeField] private Canvas? _canvas;
        [SerializeField] private AnimationController? _animController;

        private Coroutine? _activeTransition;

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
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Subscribe<RoundEndedEvent>(OnRoundEnded);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Unsubscribe<RoundEndedEvent>(OnRoundEnded);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        }

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            if (_activeTransition != null) StopCoroutine(_activeTransition);
            _activeTransition = StartCoroutine(RoundStartCoroutine(evt.RoundNumber));
        }

        private void OnRoundEnded(RoundEndedEvent evt)
        {
            if (_activeTransition != null) StopCoroutine(_activeTransition);
            _activeTransition = StartCoroutine(RoundEndCoroutine(evt.WinnerIndex));
        }

        private void OnGameOver(GameOverEvent evt)
        {
            if (_activeTransition != null) StopCoroutine(_activeTransition);
            _activeTransition = StartCoroutine(GameOverCoroutine(evt.WinnerIndex));
        }

        // ═══════════════════════════════════════════════
        //  ROUND START
        // ═══════════════════════════════════════════════

        private IEnumerator RoundStartCoroutine(int roundNumber)
        {
            if (_canvas == null) yield break;

            // Container overlay plein écran
            GameObject overlay = CreateFullScreenOverlay("RoundStartOverlay");
            CanvasGroup overlayCg = overlay.GetComponent<CanvasGroup>();
            Image overlayBg = overlay.transform.Find("Bg").GetComponent<Image>();

            // Texte "ROUND N"
            GameObject textObj = new("RoundText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(overlay.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchoredPosition = Vector2.zero;
            textRt.sizeDelta = new Vector2(600f, 100f);

            TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
            text.text = $"ROUND {roundNumber}";
            text.fontSize = 56;
            text.color = Constants.TextAccent;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.raycastTarget = false;
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color32(0, 0, 0, 200);

            // Ligne décorative horizontale
            GameObject lineObj = new("Line", typeof(RectTransform), typeof(Image));
            lineObj.transform.SetParent(overlay.transform, false);
            RectTransform lineRt = lineObj.GetComponent<RectTransform>();
            lineRt.anchoredPosition = new Vector2(0, -40f);
            lineRt.sizeDelta = new Vector2(0f, 2f);
            Image lineImg = lineObj.GetComponent<Image>();
            lineImg.color = Constants.TextAccent;
            lineImg.raycastTarget = false;

            // ── Animation ──

            // Phase 1 : Overlay fade-in
            overlayCg.alpha = 0f;
            textRt.localScale = Vector3.one * 0.3f;
            float elapsed = 0f;

            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.3f);
                overlayCg.alpha = UITween.EaseOutCubic(t) * 0.85f;
                yield return null;
            }

            // Phase 2 : Texte scale-in avec bounce + ligne s'étend
            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.5f);
                float eased = UITween.EaseOutBack(t);

                textRt.localScale = Vector3.one * eased;
                text.color = new Color(
                    Constants.TextAccent.r,
                    Constants.TextAccent.g,
                    Constants.TextAccent.b,
                    Mathf.Clamp01(t * 3f));

                // Ligne s'étend depuis le centre
                float lineWidth = Mathf.Lerp(0f, 300f, UITween.EaseOutCubic(t));
                lineRt.sizeDelta = new Vector2(lineWidth, 2f);

                yield return null;
            }

            // Hold
            yield return new WaitForSeconds(0.6f);

            // Phase 3 : Tout disparaît
            elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.35f);
                overlayCg.alpha = Mathf.Lerp(0.85f, 0f, UITween.EaseInQuart(t));
                textRt.localScale = Vector3.one * (1f + t * 0.2f);
                yield return null;
            }

            Destroy(overlay);
            _activeTransition = null;
        }

        // ═══════════════════════════════════════════════
        //  ROUND END
        // ═══════════════════════════════════════════════

        private IEnumerator RoundEndCoroutine(int winnerIndex)
        {
            if (_canvas == null) yield break;

            Color winnerColor = winnerIndex < PlayerColors.Length
                ? PlayerColors[winnerIndex]
                : Constants.TextAccent;

            // Flash
            if (_animController != null)
            {
                Color flash = winnerColor;
                flash.a = 0.3f;
                _animController.AnimateScreenFlash(flash, 0.3f);
            }

            yield return new WaitForSeconds(0.15f);

            // Container
            GameObject overlay = CreateFullScreenOverlay("RoundEndOverlay");
            CanvasGroup overlayCg = overlay.GetComponent<CanvasGroup>();

            // Texte
            GameObject textObj = new("WinText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(overlay.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchoredPosition = new Vector2(0, 15f);
            textRt.sizeDelta = new Vector2(600f, 80f);

            TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
            text.text = $"PLAYER {winnerIndex + 1} WINS!";
            text.fontSize = 48;
            text.color = winnerColor;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.raycastTarget = false;
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color32(0, 0, 0, 200);

            // Sous-texte "Round terminé"
            GameObject subObj = new("SubText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(overlay.transform, false);
            RectTransform subRt = subObj.GetComponent<RectTransform>();
            subRt.anchoredPosition = new Vector2(0, -25f);
            subRt.sizeDelta = new Vector2(400f, 40f);

            TextMeshProUGUI subText = subObj.GetComponent<TextMeshProUGUI>();
            subText.text = "ROUND TERMINE";
            subText.fontSize = 20;
            subText.color = Constants.TextSecondary;
            subText.alignment = TextAlignmentOptions.Center;
            subText.fontStyle = FontStyles.Normal;
            subText.raycastTarget = false;

            // Animation
            overlayCg.alpha = 0f;
            textRt.localScale = Vector3.one * 2f;
            subRt.localScale = Vector3.zero;

            // Fade-in + texte shrink-in
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.4f);

                overlayCg.alpha = UITween.EaseOutCubic(t) * 0.9f;
                textRt.localScale = Vector3.one * Mathf.LerpUnclamped(2f, 1f, UITween.EaseOutBack(t));
                yield return null;
            }

            // Sous-texte pop-in
            elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.3f);
                subRt.localScale = Vector3.one * UITween.EaseOutBack(t);
                yield return null;
            }

            // Particules
            if (_animController != null)
            {
                _animController.SpawnParticleBurst(Vector2.zero, winnerColor, 16);
            }

            // Hold
            yield return new WaitForSeconds(1.2f);

            // Fade-out
            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.5f);
                overlayCg.alpha = Mathf.Lerp(0.9f, 0f, t);
                yield return null;
            }

            Destroy(overlay);
            _activeTransition = null;
        }

        // ═══════════════════════════════════════════════
        //  GAME OVER
        // ═══════════════════════════════════════════════

        private IEnumerator GameOverCoroutine(int winnerIndex)
        {
            if (_canvas == null) yield break;

            Color winnerColor = winnerIndex < PlayerColors.Length
                ? PlayerColors[winnerIndex]
                : Constants.TextAccent;

            // Double flash
            if (_animController != null)
            {
                _animController.AnimateScreenFlash(Color.white, 0.4f);
                yield return new WaitForSeconds(0.2f);
                Color flash2 = winnerColor;
                flash2.a = 0.4f;
                _animController.AnimateScreenFlash(flash2, 0.5f);
            }

            yield return new WaitForSeconds(0.3f);

            // Container
            GameObject overlay = CreateFullScreenOverlay("GameOverOverlay");
            CanvasGroup overlayCg = overlay.GetComponent<CanvasGroup>();

            // Texte GAME OVER
            GameObject goText = new("GameOverText", typeof(RectTransform), typeof(TextMeshProUGUI));
            goText.transform.SetParent(overlay.transform, false);
            RectTransform goRt = goText.GetComponent<RectTransform>();
            goRt.anchoredPosition = new Vector2(0, 30f);
            goRt.sizeDelta = new Vector2(600f, 80f);

            TextMeshProUGUI goTmp = goText.GetComponent<TextMeshProUGUI>();
            goTmp.text = "GAME OVER";
            goTmp.fontSize = 60;
            goTmp.color = Constants.TextAccent;
            goTmp.alignment = TextAlignmentOptions.Center;
            goTmp.fontStyle = FontStyles.Bold;
            goTmp.raycastTarget = false;

            // Winner text
            GameObject winText = new("WinnerText", typeof(RectTransform), typeof(TextMeshProUGUI));
            winText.transform.SetParent(overlay.transform, false);
            RectTransform winRt = winText.GetComponent<RectTransform>();
            winRt.anchoredPosition = new Vector2(0, -25f);
            winRt.sizeDelta = new Vector2(600f, 60f);

            TextMeshProUGUI winTmp = winText.GetComponent<TextMeshProUGUI>();
            winTmp.text = $"PLAYER {winnerIndex + 1} CHAMPION !";
            winTmp.fontSize = 36;
            winTmp.color = winnerColor;
            winTmp.alignment = TextAlignmentOptions.Center;
            winTmp.fontStyle = FontStyles.Bold;
            winTmp.raycastTarget = false;

            // Animation
            overlayCg.alpha = 0f;
            goRt.localScale = Vector3.zero;
            winRt.localScale = Vector3.zero;

            // Fade-in overlay
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.5f);
                overlayCg.alpha = UITween.EaseOutCubic(t) * 0.95f;
                yield return null;
            }

            // "GAME OVER" elastic pop
            elapsed = 0f;
            while (elapsed < 0.6f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.6f);
                goRt.localScale = Vector3.one * UITween.EaseOutElastic(t);
                yield return null;
            }

            // Burst de particules
            if (_animController != null)
            {
                _animController.SpawnParticleBurst(new Vector2(0, 30f), Constants.TextAccent, 24);
            }

            yield return new WaitForSeconds(0.2f);

            // Winner pop-in
            elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.4f);
                winRt.localScale = Vector3.one * UITween.EaseOutBack(t);
                yield return null;
            }

            // Confetti continu
            if (_animController != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    yield return new WaitForSeconds(0.3f);
                    float x = Random.Range(-200f, 200f);
                    float y = Random.Range(-40f, 60f);
                    Color confettiColor = PlayerColors[Random.Range(0, PlayerColors.Length)];
                    _animController.SpawnParticleBurst(new Vector2(x, y), confettiColor, 8);
                }
            }

            // L'overlay reste visible — le GameOverPanel du HUD prend le relais
            _activeTransition = null;
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════

        private GameObject CreateFullScreenOverlay(string name)
        {
            GameObject overlay = new(name, typeof(RectTransform), typeof(CanvasGroup));
            overlay.transform.SetParent(_canvas!.transform, false);
            overlay.transform.SetAsLastSibling();

            RectTransform rt = overlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            CanvasGroup cg = overlay.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // Fond sombre
            GameObject bg = new("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(overlay.transform, false);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            Image bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0.02f, 0.05f, 0.08f, 0.85f);
            bgImg.raycastTarget = false;

            return overlay;
        }
    }
}
