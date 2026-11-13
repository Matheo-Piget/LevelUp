using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Animations de cartes et petits effets (sans DOTween : coroutines +
    /// courbes) : arrivée d'une carte piochée, secousse d'erreur, pulsation,
    /// apparition, flash et éclats (désactivés en « animations réduites »).
    /// </summary>
    public class AnimationController : MonoBehaviour
    {
        [Header("Reference Points")]
        [SerializeField] private RectTransform? _deckPosition;

        [Header("Effects")]
        [SerializeField] private Canvas? _effectCanvas;

        // Compteur (et non booléen) : plusieurs animations peuvent se chevaucher,
        // la première terminée ne doit pas libérer le verrou des autres.
        private int _running;

        /// <summary>Vrai tant qu'une animation qui verrouille les actions est en cours.</summary>
        public bool IsAnimating => _running > 0;

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
        /// Anime une carte vers sa place dans la main, en arc, depuis le paquet ou
        /// depuis <paramref name="fromWorld"/> (défausse ramassée).
        /// <paramref name="onComplete"/> est TOUJOURS appelé (même si la carte disparaît).
        /// </summary>
        public void AnimateDrawToHand(RectTransform card, Action? onComplete = null, Vector3? fromWorld = null)
        {
            if (_deckPosition == null && !fromWorld.HasValue)
            {
                onComplete?.Invoke();
                return;
            }
            FrameBudget.Ping(0.8f);
            StartCoroutine(AnimateDrawCoroutine(card, onComplete, fromWorld ?? _deckPosition!.position));
        }

        /// <summary>Secousse horizontale (erreur, action invalide).</summary>
        public void AnimateShake(RectTransform card, Action? onComplete = null)
        {
            FrameBudget.Ping(0.6f);
            StartCoroutine(AnimateShakeCoroutine(card, onComplete));
        }

        /// <summary>Pulsation : agrandit puis revient (succès, confirmation).</summary>
        public void AnimatePulse(RectTransform target, Action? onComplete = null)
        {
            FrameBudget.Ping(0.5f);
            StartCoroutine(AnimatePulseCoroutine(target, onComplete));
        }

        /// <summary>Apparition : de 0 à 1 avec rebond.</summary>
        public void AnimatePopIn(RectTransform target, Action? onComplete = null)
        {
            FrameBudget.Ping(0.6f);
            StartCoroutine(AnimatePopInCoroutine(target, onComplete));
        }

        /// <summary>Voile coloré bref sur tout l'écran (désactivé en animations réduites).</summary>
        public void AnimateScreenFlash(Color? color = null, float duration = 0.3f)
        {
            if (GameSettings.ReducedMotion) return;
            FrameBudget.Ping(duration + 0.2f);
            StartCoroutine(AnimateScreenFlashCoroutine(color ?? new Color(1f, 1f, 1f, 0.3f), duration));
        }

        /// <summary>Éclats colorés depuis un point (désactivés en animations réduites).</summary>
        public void SpawnParticleBurst(Vector2 position, Color color, int count = 12)
        {
            if (GameSettings.ReducedMotion) return;
            FrameBudget.Ping(0.9f);
            StartCoroutine(ParticleBurstCoroutine(position, color, count));
        }

        // ══════════════════════════════════════════════════
        //  COROUTINES
        // ══════════════════════════════════════════════════

        private IEnumerator AnimateDrawCoroutine(RectTransform card, Action? onComplete, Vector3 fromWorld)
        {
            _running++;
            try
            {
                if (card == null) yield break;

                Vector2 targetPos = card.anchoredPosition;
                Vector3 targetScale = card.localScale;

                Transform? cardParent = card.parent;
                Vector3 startLocal = cardParent != null ? cardParent.InverseTransformPoint(fromWorld) : fromWorld;

                Vector2 startPos = new(startLocal.x, startLocal.y);
                Vector3 startScale = targetScale * 0.4f;
                float startRotation = 15f;

                card.anchoredPosition = startPos;
                card.localScale = startScale;
                card.localRotation = Quaternion.Euler(0, 0, startRotation);

                Vector2 midPoint = Vector2.Lerp(startPos, targetPos, 0.5f) + Vector2.up * 120f;

                float elapsed = 0f;
                float duration = GameSettings.ReducedMotion
                    ? Constants.AnimDrawDuration * 0.5f
                    : Constants.AnimDrawDuration;

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
                    card.localScale = Vector3.Lerp(startScale, targetScale, BounceCurve.Evaluate(rawT));
                    card.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(startRotation, 0f, rawT));
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
                _running = Mathf.Max(0, _running - 1);
                // Toujours notifier pour que l'appelant libère ses verrous
                // (_animatingDraw, etc.), même si l'objet a été détruit.
                onComplete?.Invoke();
            }
        }

        private IEnumerator AnimateShakeCoroutine(RectTransform rt, Action? onComplete)
        {
            if (rt == null) yield break;
            Vector2 original = rt.anchoredPosition;
            float duration = 0.3f;
            float magnitude = GameSettings.ReducedMotion ? 6f : 16f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (rt == null) yield break;
                elapsed += Time.deltaTime;
                float decay = 1f - (elapsed / duration);
                decay *= decay;
                float x = original.x + Mathf.Sin(elapsed * 60f) * magnitude * decay;
                rt.anchoredPosition = new Vector2(x, original.y);
                yield return null;
            }

            if (rt != null) rt.anchoredPosition = original;
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

        private IEnumerator AnimatePopInCoroutine(RectTransform rt, Action? onComplete)
        {
            if (rt == null) yield break;
            Vector3 targetScale = rt.localScale;
            rt.localScale = Vector3.zero;

            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                if (rt == null) yield break;
                elapsed += Time.deltaTime;
                float t = BounceCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                rt.localScale = targetScale * t;
                yield return null;
            }

            if (rt != null) rt.localScale = targetScale;
            onComplete?.Invoke();
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

            // Composants mis en cache une fois : aucun GetComponent par frame.
            GameObject[] particles = new GameObject[count];
            RectTransform[] rects = new RectTransform[count];
            CanvasGroup[] groups = new CanvasGroup[count];
            Vector2[] velocities = new Vector2[count];

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
                float hueShift = UnityEngine.Random.Range(-0.05f, 0.05f);
                Color.RGBToHSV(color, out float h, out float s, out float v);
                img.color = Color.HSVToRGB(Mathf.Repeat(h + hueShift, 1f), s, v);
                img.raycastTarget = false;

                CanvasGroup cg = p.GetComponent<CanvasGroup>();
                cg.blocksRaycasts = false;

                float angle = (360f / count) * i + UnityEngine.Random.Range(-15f, 15f);
                float speed = UnityEngine.Random.Range(200f, 500f);
                velocities[i] = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad) * speed,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * speed);

                particles[i] = p;
                rects[i] = rt;
                groups[i] = cg;
            }

            float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float alpha = Mathf.Lerp(1f, 0f, t * t);
                float scale = Mathf.Lerp(1f, 0.2f, t);

                for (int i = 0; i < count; i++)
                {
                    if (particles[i] == null) continue;
                    rects[i].anchoredPosition += velocities[i] * Time.deltaTime;
                    velocities[i] += Vector2.down * 400f * Time.deltaTime;
                    groups[i].alpha = alpha;
                    rects[i].localScale = Vector3.one * scale;
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
