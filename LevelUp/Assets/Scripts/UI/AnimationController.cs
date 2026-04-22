using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Animations juicy style Balatro : bounce/overshoot, cascade, particules UI,
    /// flash écran. Sans DOTween — coroutines + courbes custom.
    /// </summary>
    public class AnimationController : MonoBehaviour
    {
        [Header("Animation Curves")]
        [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Reference Points")]
        [SerializeField] private RectTransform? _deckPosition;
        [SerializeField] private RectTransform? _discardPosition;
        [SerializeField] private RectTransform? _tableCenter;

        [Header("Effects")]
        [SerializeField] private Canvas? _effectCanvas;

        /// <summary>Indique si une animation est en cours.</summary>
        public bool IsAnimating { get; private set; }

        private static readonly AnimationCurve BounceCurve = CreateBounceCurve();
        private static readonly AnimationCurve OvershootCurve = CreateOvershootCurve();

        /// <summary>Courbe bounce : dépasse la cible puis revient.</summary>
        private static AnimationCurve CreateBounceCurve()
        {
            AnimationCurve curve = new();
            curve.AddKey(new Keyframe(0f, 0f, 0f, 2f));
            curve.AddKey(new Keyframe(0.6f, 1.12f, 0f, 0f));
            curve.AddKey(new Keyframe(0.8f, 0.97f, 0f, 0f));
            curve.AddKey(new Keyframe(1f, 1f, 0f, 0f));
            return curve;
        }

        /// <summary>Courbe overshoot pour les mouvements.</summary>
        private static AnimationCurve CreateOvershootCurve()
        {
            AnimationCurve curve = new();
            curve.AddKey(new Keyframe(0f, 0f, 0f, 0f));
            curve.AddKey(new Keyframe(0.5f, 1.08f, 2f, 0f));
            curve.AddKey(new Keyframe(1f, 1f, 0f, 0f));
            return curve;
        }

        /// <summary>
        /// Anime une carte depuis le deck vers la main avec un arc et un bounce à l'arrivée.
        /// </summary>
        public void AnimateDrawToHand(RectTransform card, Action? onComplete = null)
        {
            if (_deckPosition == null)
            {
                onComplete?.Invoke();
                return;
            }
            StartCoroutine(AnimateDrawCoroutine(card, onComplete));
        }

        /// <summary>
        /// Anime une carte glissant vers la table avec shrink.
        /// </summary>
        public void AnimateCardToTable(RectTransform card, Vector2 targetWorldPos, Action? onComplete = null)
        {
            Vector2 target = _tableCenter != null ? _tableCenter.anchoredPosition : targetWorldPos;
            StartCoroutine(AnimateMoveAndScale(card, card.anchoredPosition, target,
                card.localScale, card.localScale * 0.7f, Constants.AnimPlayDuration, onComplete));
        }

        /// <summary>
        /// Anime une carte défaussée : tourne légèrement + shrink + fade.
        /// </summary>
        public void AnimateDiscard(RectTransform card, CanvasGroup? canvasGroup, Action? onComplete = null)
        {
            Vector2 target = _discardPosition != null
                ? _discardPosition.anchoredPosition
                : card.anchoredPosition + Vector2.down * 200f;

            StartCoroutine(AnimateDiscardCoroutine(card, canvasGroup, target, onComplete));
        }

        /// <summary>
        /// Animation de retournement de carte (scale X).
        /// </summary>
        public void AnimateFlip(RectTransform card, Action? onHalf = null, Action? onComplete = null)
        {
            StartCoroutine(AnimateFlipCoroutine(card, onHalf, onComplete));
        }

        /// <summary>
        /// Shake horizontal punchy (erreur, action invalide).
        /// </summary>
        public void AnimateShake(RectTransform card, Action? onComplete = null)
        {
            StartCoroutine(AnimateShakeCoroutine(card, onComplete));
        }

        /// <summary>
        /// Pulse bounce : scale overshoot puis retour (succès, confirmation).
        /// </summary>
        public void AnimatePulse(RectTransform target, Action? onComplete = null)
        {
            StartCoroutine(AnimatePulseCoroutine(target, onComplete));
        }

        /// <summary>
        /// Glow pulsant via alpha du CanvasGroup.
        /// </summary>
        public void AnimateGlow(CanvasGroup cg, float duration = 0.6f, Action? onComplete = null)
        {
            StartCoroutine(AnimateGlowCoroutine(cg, duration, onComplete));
        }

        /// <summary>
        /// Scale pop-in : de 0 à 1 avec bounce (pour UI elements, status messages).
        /// </summary>
        public void AnimatePopIn(RectTransform target, Action? onComplete = null)
        {
            StartCoroutine(AnimatePopInCoroutine(target, onComplete));
        }

        /// <summary>
        /// Cascade : anime une liste d'éléments avec un délai entre chacun.
        /// </summary>
        public void AnimateCascade(RectTransform[] cards, Vector2[] targets, Action? onComplete = null)
        {
            StartCoroutine(AnimateCascadeCoroutine(cards, targets, onComplete));
        }

        /// <summary>
        /// Flash blanc rapide sur tout l'écran (round win, level complete).
        /// </summary>
        public void AnimateScreenFlash(Color? color = null, float duration = 0.3f)
        {
            StartCoroutine(AnimateScreenFlashCoroutine(color ?? new Color(1f, 1f, 1f, 0.3f), duration));
        }

        /// <summary>
        /// Spawn de particules UI colorées qui explosent depuis un point (level complete, round win).
        /// </summary>
        public void SpawnParticleBurst(Vector2 position, Color color, int count = 12)
        {
            StartCoroutine(ParticleBurstCoroutine(position, color, count));
        }

        // ══════════════════════════════════════════════════
        //  COROUTINES
        // ══════════════════════════════════════════════════

        private IEnumerator AnimateDrawCoroutine(RectTransform card, Action? onComplete)
        {
            IsAnimating = true;
            try
            {
                if (card == null) yield break;

                Vector2 targetPos = card.anchoredPosition;
                Vector3 targetScale = card.localScale;

                // Position du deck en local
                Vector3 deckWorldPos = _deckPosition!.position;
                Transform? cardParent = card.parent;
                Vector3 deckLocalPos = cardParent != null
                    ? cardParent.InverseTransformPoint(deckWorldPos)
                    : deckWorldPos;

                Vector2 startPos = new(deckLocalPos.x, deckLocalPos.y);
                Vector3 startScale = targetScale * 0.4f;
                float startRotation = 15f;

                card.anchoredPosition = startPos;
                card.localScale = startScale;
                card.localRotation = Quaternion.Euler(0, 0, startRotation);

                // Arc plus haut pour un effet spectaculaire
                Vector2 midPoint = Vector2.Lerp(startPos, targetPos, 0.5f) + Vector2.up * 120f;

                float elapsed = 0f;
                float duration = Constants.AnimDrawDuration;

                while (elapsed < duration)
                {
                    if (card == null) yield break;
                    elapsed += Time.deltaTime;
                    float rawT = Mathf.Clamp01(elapsed / duration);
                    float t = OvershootCurve.Evaluate(rawT);

                    // Bézier quadratique
                    Vector2 a = Vector2.Lerp(startPos, midPoint, t);
                    Vector2 b = Vector2.Lerp(midPoint, targetPos, t);
                    card.anchoredPosition = Vector2.Lerp(a, b, t);

                    // Scale avec bounce
                    float scaleT = BounceCurve.Evaluate(rawT);
                    card.localScale = Vector3.Lerp(startScale, targetScale, scaleT);

                    // Rotation revient à 0
                    float rot = Mathf.Lerp(startRotation, 0f, rawT);
                    card.localRotation = Quaternion.Euler(0, 0, rot);

                    yield return null;
                }

                if (card != null)
                {
                    card.anchoredPosition = targetPos;
                    card.localScale = targetScale;
                    card.localRotation = Quaternion.identity;
                }
            }
            finally
            {
                IsAnimating = false;
                // Toujours notifier pour que l'appelant libère ses locks
                // (_animatingDraw, etc.), même si l'objet a été détruit.
                onComplete?.Invoke();
            }
        }

        private IEnumerator AnimateMoveAndScale(RectTransform rt, Vector2 fromPos, Vector2 toPos,
            Vector3 fromScale, Vector3 toScale, float duration, Action? onComplete)
        {
            IsAnimating = true;
            try
            {
                if (rt == null) yield break;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    if (rt == null) yield break;
                    elapsed += Time.deltaTime;
                    float t = OvershootCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                    rt.anchoredPosition = Vector2.LerpUnclamped(fromPos, toPos, t);
                    rt.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);
                    yield return null;
                }

                if (rt != null)
                {
                    rt.anchoredPosition = toPos;
                    rt.localScale = toScale;
                }
            }
            finally
            {
                IsAnimating = false;
                onComplete?.Invoke();
            }
        }

        private IEnumerator AnimateDiscardCoroutine(RectTransform rt, CanvasGroup? cg,
            Vector2 target, Action? onComplete)
        {
            IsAnimating = true;
            try
            {
                if (rt == null) yield break;
                float elapsed = 0f;
                float duration = Constants.AnimDiscardDuration;
                Vector2 start = rt.anchoredPosition;
                Vector3 startScale = rt.localScale;
                float startAlpha = cg != null ? cg.alpha : 1f;
                float startRot = rt.localEulerAngles.z;

                // Rotation aléatoire légère pour un feel organique
                float targetRot = startRot + UnityEngine.Random.Range(-15f, 15f);

                while (elapsed < duration)
                {
                    if (rt == null) yield break;
                    elapsed += Time.deltaTime;
                    float rawT = Mathf.Clamp01(elapsed / duration);
                    float t = _moveCurve.Evaluate(rawT);

                    rt.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
                    rt.localScale = Vector3.Lerp(startScale, startScale * 0.6f, rawT);
                    rt.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(startRot, targetRot, rawT));

                    if (cg != null) cg.alpha = Mathf.Lerp(startAlpha, 0f, rawT * rawT);
                    yield return null;
                }

                if (rt != null)
                {
                    rt.anchoredPosition = target;
                    if (cg != null) cg.alpha = 0f;
                }
            }
            finally
            {
                IsAnimating = false;
                onComplete?.Invoke();
            }
        }

        private IEnumerator AnimateFlipCoroutine(RectTransform rt, Action? onHalf, Action? onComplete)
        {
            float duration = 0.12f;
            float elapsed = 0f;
            Vector3 originalScale = rt.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scaleX = Mathf.Lerp(1f, 0f, t);
                rt.localScale = new Vector3(scaleX * originalScale.x, originalScale.y, originalScale.z);
                yield return null;
            }

            onHalf?.Invoke();
            elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scaleX = Mathf.Lerp(0f, 1f, t);
                rt.localScale = new Vector3(scaleX * originalScale.x, originalScale.y, originalScale.z);
                yield return null;
            }

            rt.localScale = originalScale;
            onComplete?.Invoke();
        }

        private IEnumerator AnimateShakeCoroutine(RectTransform rt, Action? onComplete)
        {
            Vector2 original = rt.anchoredPosition;
            float duration = 0.3f;
            float magnitude = 16f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - (elapsed / duration);
                decay *= decay; // quadratic decay for punchier feel
                float x = original.x + Mathf.Sin(elapsed * 60f) * magnitude * decay;
                rt.anchoredPosition = new Vector2(x, original.y);
                yield return null;
            }

            rt.anchoredPosition = original;
            onComplete?.Invoke();
        }

        private IEnumerator AnimatePulseCoroutine(RectTransform rt, Action? onComplete)
        {
            if (rt == null) yield break;
            Vector3 original = rt.localScale;
            Vector3 big = original * 1.25f;
            float halfDur = 0.1f;

            float elapsed = 0f;
            while (elapsed < halfDur)
            {
                if (rt == null) yield break;
                elapsed += Time.deltaTime;
                float t = BounceCurve.Evaluate(Mathf.Clamp01(elapsed / halfDur));
                rt.localScale = Vector3.LerpUnclamped(original, big, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDur * 1.5f)
            {
                if (rt == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / (halfDur * 1.5f));
                rt.localScale = Vector3.Lerp(big, original, t);
                yield return null;
            }

            if (rt == null) yield break;
            rt.localScale = original;
            onComplete?.Invoke();
        }

        private IEnumerator AnimateGlowCoroutine(CanvasGroup cg, float duration, Action? onComplete)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.PingPong(elapsed * 3f, 1f);
                cg.alpha = Mathf.Lerp(0.5f, 1f, t);
                yield return null;
            }
            cg.alpha = 1f;
            onComplete?.Invoke();
        }

        private IEnumerator AnimatePopInCoroutine(RectTransform rt, Action? onComplete)
        {
            Vector3 targetScale = rt.localScale;
            rt.localScale = Vector3.zero;

            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = BounceCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                rt.localScale = targetScale * t;
                yield return null;
            }

            rt.localScale = targetScale;
            onComplete?.Invoke();
        }

        private IEnumerator AnimateCascadeCoroutine(RectTransform[] cards, Vector2[] targets, Action? onComplete)
        {
            IsAnimating = true;
            try
            {
                int remaining = cards.Length;

                for (int i = 0; i < cards.Length; i++)
                {
                    int idx = i;
                    if (cards[idx] == null) { remaining--; continue; }
                    StartCoroutine(AnimateMoveAndScale(
                        cards[idx],
                        cards[idx].anchoredPosition,
                        targets[idx],
                        cards[idx].localScale,
                        cards[idx].localScale * 0.7f,
                        Constants.AnimPlayDuration,
                        () => remaining--));

                    yield return new WaitForSeconds(Constants.AnimCascadeDelay);
                }

                // Attendre que toutes les animations finissent (avec timeout de sécurité)
                float guard = 0f;
                while (remaining > 0 && guard < 5f)
                {
                    guard += Time.deltaTime;
                    yield return null;
                }
            }
            finally
            {
                IsAnimating = false;
                onComplete?.Invoke();
            }
        }

        private IEnumerator AnimateScreenFlashCoroutine(Color color, float duration)
        {
            Canvas canvas = _effectCanvas != null ? _effectCanvas : GetComponentInParent<Canvas>();
            if (canvas == null) yield break;

            GameObject flashObj = new("ScreenFlash", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            flashObj.transform.SetParent(canvas.transform, false);
            flashObj.transform.SetAsLastSibling();

            RectTransform rt = flashObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image img = flashObj.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            CanvasGroup cg = flashObj.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                cg.alpha = Mathf.Lerp(1f, 0f, t * t);
                yield return null;
            }

            Destroy(flashObj);
        }

        private IEnumerator ParticleBurstCoroutine(Vector2 position, Color color, int count)
        {
            Canvas canvas = _effectCanvas != null ? _effectCanvas : GetComponentInParent<Canvas>();
            if (canvas == null) yield break;

            GameObject[] particles = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                GameObject p = new($"Particle_{i}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                p.transform.SetParent(canvas.transform, false);
                p.transform.SetAsLastSibling();

                RectTransform rt = p.GetComponent<RectTransform>();
                rt.anchoredPosition = position;
                float size = UnityEngine.Random.Range(4f, 10f);
                rt.sizeDelta = new Vector2(size, size);

                Image img = p.GetComponent<Image>();
                // Slight color variation
                float hueShift = UnityEngine.Random.Range(-0.05f, 0.05f);
                Color.RGBToHSV(color, out float h, out float s, out float v);
                img.color = Color.HSVToRGB(Mathf.Repeat(h + hueShift, 1f), s, v);
                img.raycastTarget = false;

                p.GetComponent<CanvasGroup>().blocksRaycasts = false;
                particles[i] = p;
            }

            // Directions aléatoires
            Vector2[] velocities = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i + UnityEngine.Random.Range(-15f, 15f);
                float speed = UnityEngine.Random.Range(200f, 500f);
                velocities[i] = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * speed,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * speed);
            }

            float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                for (int i = 0; i < count; i++)
                {
                    if (particles[i] == null) continue;

                    RectTransform rt = particles[i].GetComponent<RectTransform>();
                    rt.anchoredPosition += velocities[i] * Time.deltaTime;

                    // Gravité
                    velocities[i] += Vector2.down * 400f * Time.deltaTime;

                    // Fade + shrink
                    CanvasGroup cg = particles[i].GetComponent<CanvasGroup>();
                    cg.alpha = Mathf.Lerp(1f, 0f, t * t);

                    float scale = Mathf.Lerp(1f, 0.2f, t);
                    rt.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                if (particles[i] != null) Destroy(particles[i]);
            }
        }
    }
}
