using System;
using System.Collections;
using UnityEngine;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Gère toutes les animations de cartes : pioche, pose, défausse.
    /// Sans DOTween — utilise des coroutines et des courbes d'animation Unity.
    /// </summary>
    public class AnimationController : MonoBehaviour
    {
        [Header("Animation Curves")]
        [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Reference Points")]
        [SerializeField] private RectTransform? _deckPosition;
        [SerializeField] private RectTransform? _discardPosition;
        [SerializeField] private RectTransform? _tableCenter;

        /// <summary>Indique si une animation est en cours.</summary>
        public bool IsAnimating { get; private set; }

        /// <summary>
        /// Anime une carte depuis le deck vers une position cible dans la main.
        /// Crée un visuel temporaire qui vole du deck à la destination.
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
        /// Anime une carte glissant de la main vers la table (pose de niveau).
        /// </summary>
        public void AnimateCardToTable(RectTransform card, Vector2 targetWorldPos, Action? onComplete = null)
        {
            Vector2 target = _tableCenter != null ? _tableCenter.anchoredPosition : targetWorldPos;
            StartCoroutine(AnimateMoveAndScale(card, card.anchoredPosition, target,
                card.localScale, card.localScale * 0.7f, Constants.AnimPlayDuration, onComplete));
        }

        /// <summary>
        /// Anime une carte qui est défaussée (glisse vers la défausse avec fondu).
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
        /// Anime un shake horizontal (erreur, action invalide).
        /// </summary>
        public void AnimateShake(RectTransform card, Action? onComplete = null)
        {
            StartCoroutine(AnimateShakeCoroutine(card, onComplete));
        }

        /// <summary>
        /// Anime un scale pulse (confirmation, succès).
        /// </summary>
        public void AnimatePulse(RectTransform target, Action? onComplete = null)
        {
            StartCoroutine(AnimatePulseCoroutine(target, onComplete));
        }

        /// <summary>
        /// Anime un highlight glow pulsant sur un RectTransform.
        /// </summary>
        public void AnimateGlow(CanvasGroup cg, float duration = 0.6f, Action? onComplete = null)
        {
            StartCoroutine(AnimateGlowCoroutine(cg, duration, onComplete));
        }

        /// <summary>
        /// Animation de pioche : la carte part du deck, grossit légèrement, et arrive dans la main.
        /// </summary>
        private IEnumerator AnimateDrawCoroutine(RectTransform card, Action? onComplete)
        {
            IsAnimating = true;

            // Sauvegarder la destination finale
            Vector2 targetPos = card.anchoredPosition;
            Vector3 targetScale = card.localScale;

            // Calculer la position du deck en coordonnées locales du parent de la carte
            Vector3 deckWorldPos = _deckPosition!.position;
            Transform? cardParent = card.parent;
            Vector3 deckLocalPos = cardParent != null
                ? cardParent.InverseTransformPoint(deckWorldPos)
                : deckWorldPos;

            Vector2 startPos = new Vector2(deckLocalPos.x, deckLocalPos.y);
            Vector3 startScale = targetScale * 0.5f;

            // Positionner au deck
            card.anchoredPosition = startPos;
            card.localScale = startScale;

            // Arc de mouvement : passer par un point plus haut pour un effet visuel
            Vector2 midPoint = Vector2.Lerp(startPos, targetPos, 0.5f) + Vector2.up * 80f;

            float elapsed = 0f;
            float duration = Constants.AnimDrawDuration;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = _moveCurve.Evaluate(Mathf.Clamp01(elapsed / duration));

                // Bézier quadratique pour un arc fluide
                Vector2 a = Vector2.Lerp(startPos, midPoint, t);
                Vector2 b = Vector2.Lerp(midPoint, targetPos, t);
                card.anchoredPosition = Vector2.Lerp(a, b, t);

                card.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            card.anchoredPosition = targetPos;
            card.localScale = targetScale;
            IsAnimating = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Mouvement + changement de scale simultané.
        /// </summary>
        private IEnumerator AnimateMoveAndScale(RectTransform rt, Vector2 fromPos, Vector2 toPos,
            Vector3 fromScale, Vector3 toScale, float duration, Action? onComplete)
        {
            IsAnimating = true;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = _moveCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                rt.anchoredPosition = Vector2.LerpUnclamped(fromPos, toPos, t);
                rt.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);
                yield return null;
            }

            rt.anchoredPosition = toPos;
            rt.localScale = toScale;
            IsAnimating = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Animation de défausse : glisse + fondu.
        /// </summary>
        private IEnumerator AnimateDiscardCoroutine(RectTransform rt, CanvasGroup? cg,
            Vector2 target, Action? onComplete)
        {
            IsAnimating = true;
            float elapsed = 0f;
            float duration = Constants.AnimDiscardDuration;
            Vector2 start = rt.anchoredPosition;
            float startAlpha = cg != null ? cg.alpha : 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = _moveCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                rt.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
                if (cg != null) cg.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            rt.anchoredPosition = target;
            if (cg != null) cg.alpha = 0f;
            IsAnimating = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Animation de retournement de carte (scale X).
        /// </summary>
        private IEnumerator AnimateFlipCoroutine(RectTransform rt, Action? onHalf, Action? onComplete)
        {
            float duration = 0.15f;
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

        /// <summary>
        /// Animation de shake horizontal.
        /// </summary>
        private IEnumerator AnimateShakeCoroutine(RectTransform rt, Action? onComplete)
        {
            Vector2 original = rt.anchoredPosition;
            float duration = 0.35f;
            float magnitude = 12f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - elapsed / duration;
                float x = original.x + Mathf.Sin(elapsed * 50f) * magnitude * decay;
                rt.anchoredPosition = new Vector2(x, original.y);
                yield return null;
            }

            rt.anchoredPosition = original;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Animation de pulse (scale up puis retour).
        /// </summary>
        private IEnumerator AnimatePulseCoroutine(RectTransform rt, Action? onComplete)
        {
            Vector3 original = rt.localScale;
            Vector3 target = original * 1.2f;
            float duration = 0.12f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rt.localScale = Vector3.Lerp(original, target, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rt.localScale = Vector3.Lerp(target, original, t);
                yield return null;
            }

            rt.localScale = original;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Animation de glow pulsant via alpha du CanvasGroup.
        /// </summary>
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
    }
}
