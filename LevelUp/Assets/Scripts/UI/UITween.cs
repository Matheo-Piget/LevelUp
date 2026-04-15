using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LevelUp.UI
{
    /// <summary>
    /// Utilitaire de tween pour UI (scale, fade, move, color) sans DOTween.
    /// Crée un runner MonoBehaviour global à la demande pour héberger les coroutines
    /// déclenchées depuis du code statique (boutons, évènements). Chaque tween ciblant
    /// le même GameObject + même propriété annule le précédent pour éviter les conflits.
    /// </summary>
    public static class UITween
    {
        private class TweenRunner : MonoBehaviour { }

        private static TweenRunner? _runner;
        private static readonly Dictionary<(int, string), Coroutine> _active = new();

        private static TweenRunner Runner
        {
            get
            {
                if (_runner == null)
                {
                    GameObject go = new("UITweenRunner");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    go.hideFlags = HideFlags.HideAndDontSave;
                    _runner = go.AddComponent<TweenRunner>();
                }
                return _runner;
            }
        }

        /// <summary>
        /// Ease out cubic (doux arrivée).
        /// </summary>
        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        /// <summary>
        /// Ease out back (overshoot léger puis stabilisation).
        /// </summary>
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>
        /// Ease in out cubic.
        /// </summary>
        public static float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        /// <summary>
        /// Anime le scale d'un RectTransform.
        /// </summary>
        public static void ScaleTo(GameObject owner, RectTransform rt, Vector3 target,
            float duration, Action? onComplete = null)
        {
            if (owner == null || rt == null) return;
            var key = (owner.GetInstanceID(), "scale");
            StopIfRunning(key);
            _active[key] = Runner.StartCoroutine(ScaleCo(owner, rt, target, duration, key, onComplete));
        }

        /// <summary>
        /// Anime l'alpha d'un CanvasGroup.
        /// </summary>
        public static void FadeTo(GameObject owner, CanvasGroup cg, float target,
            float duration, Action? onComplete = null)
        {
            if (owner == null || cg == null) return;
            var key = (owner.GetInstanceID(), "fade_" + cg.GetInstanceID());
            StopIfRunning(key);
            _active[key] = Runner.StartCoroutine(FadeCo(owner, cg, target, duration, key, onComplete));
        }

        /// <summary>
        /// Anime la position ancrée d'un RectTransform.
        /// </summary>
        public static void MoveTo(GameObject owner, RectTransform rt, Vector2 target,
            float duration, Action? onComplete = null)
        {
            if (owner == null || rt == null) return;
            var key = (owner.GetInstanceID(), "move");
            StopIfRunning(key);
            _active[key] = Runner.StartCoroutine(MoveCo(owner, rt, target, duration, key, onComplete));
        }

        /// <summary>
        /// Anime la couleur d'un Graphic (Image, TMP).
        /// </summary>
        public static void ColorTo(GameObject owner, Graphic graphic, Color target,
            float duration, Action? onComplete = null)
        {
            if (owner == null || graphic == null) return;
            var key = (owner.GetInstanceID(), "color_" + graphic.GetInstanceID());
            StopIfRunning(key);
            _active[key] = Runner.StartCoroutine(ColorCo(owner, graphic, target, duration, key, onComplete));
        }

        /// <summary>
        /// Fait apparaître un GameObject avec scale + fade from zero.
        /// </summary>
        public static void PopIn(GameObject owner, RectTransform rt, CanvasGroup? cg,
            float duration = 0.35f, Action? onComplete = null)
        {
            if (owner == null || rt == null) return;
            rt.localScale = Vector3.zero;
            if (cg != null) cg.alpha = 0f;
            ScaleTo(owner, rt, Vector3.one, duration);
            if (cg != null) FadeTo(owner, cg, 1f, duration * 0.7f, onComplete);
            else if (onComplete != null)
            {
                Runner.StartCoroutine(InvokeAfter(duration, onComplete));
            }
        }

        /// <summary>
        /// Fait disparaître un GameObject avec scale + fade + destroy optionnel.
        /// </summary>
        public static void PopOut(GameObject owner, RectTransform rt, CanvasGroup? cg,
            float duration = 0.25f, Action? onComplete = null)
        {
            if (owner == null || rt == null) return;
            ScaleTo(owner, rt, Vector3.zero, duration);
            if (cg != null) FadeTo(owner, cg, 0f, duration, onComplete);
            else if (onComplete != null)
            {
                Runner.StartCoroutine(InvokeAfter(duration, onComplete));
            }
        }

        /// <summary>
        /// Slide horizontal depuis l'extérieur.
        /// </summary>
        public static void SlideIn(GameObject owner, RectTransform rt, Vector2 fromOffset,
            float duration = 0.4f, Action? onComplete = null)
        {
            if (owner == null || rt == null) return;
            Vector2 target = rt.anchoredPosition;
            rt.anchoredPosition = target + fromOffset;
            MoveTo(owner, rt, target, duration, onComplete);
        }

        // ═══════════════════════════════════════════════════════════

        private static void StopIfRunning((int, string) key)
        {
            if (_active.TryGetValue(key, out Coroutine co) && co != null)
            {
                Runner.StopCoroutine(co);
                _active.Remove(key);
            }
        }

        private static IEnumerator ScaleCo(GameObject owner, RectTransform rt, Vector3 target,
            float duration, (int, string) key, Action? onComplete)
        {
            Vector3 start = rt.localScale;
            float t = 0f;
            while (t < duration)
            {
                if (owner == null || rt == null) { _active.Remove(key); yield break; }
                t += Time.unscaledDeltaTime;
                float k = EaseOutBack(Mathf.Clamp01(t / duration));
                rt.localScale = Vector3.LerpUnclamped(start, target, k);
                yield return null;
            }
            if (rt != null) rt.localScale = target;
            _active.Remove(key);
            onComplete?.Invoke();
        }

        private static IEnumerator FadeCo(GameObject owner, CanvasGroup cg, float target,
            float duration, (int, string) key, Action? onComplete)
        {
            float start = cg.alpha;
            float t = 0f;
            while (t < duration)
            {
                if (owner == null || cg == null) { _active.Remove(key); yield break; }
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                cg.alpha = Mathf.Lerp(start, target, EaseOutCubic(k));
                yield return null;
            }
            if (cg != null) cg.alpha = target;
            _active.Remove(key);
            onComplete?.Invoke();
        }

        private static IEnumerator MoveCo(GameObject owner, RectTransform rt, Vector2 target,
            float duration, (int, string) key, Action? onComplete)
        {
            Vector2 start = rt.anchoredPosition;
            float t = 0f;
            while (t < duration)
            {
                if (owner == null || rt == null) { _active.Remove(key); yield break; }
                t += Time.unscaledDeltaTime;
                float k = EaseOutCubic(Mathf.Clamp01(t / duration));
                rt.anchoredPosition = Vector2.LerpUnclamped(start, target, k);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = target;
            _active.Remove(key);
            onComplete?.Invoke();
        }

        private static IEnumerator ColorCo(GameObject owner, Graphic graphic, Color target,
            float duration, (int, string) key, Action? onComplete)
        {
            Color start = graphic.color;
            float t = 0f;
            while (t < duration)
            {
                if (owner == null || graphic == null) { _active.Remove(key); yield break; }
                t += Time.unscaledDeltaTime;
                float k = EaseOutCubic(Mathf.Clamp01(t / duration));
                graphic.color = Color.Lerp(start, target, k);
                yield return null;
            }
            if (graphic != null) graphic.color = target;
            _active.Remove(key);
            onComplete?.Invoke();
        }

        private static IEnumerator InvokeAfter(float delay, Action callback)
        {
            yield return new WaitForSecondsRealtime(delay);
            callback?.Invoke();
        }
    }
}
