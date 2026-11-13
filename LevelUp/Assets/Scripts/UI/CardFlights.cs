using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>Point de départ d'une carte animée : position monde + échelle monde de la carte.</summary>
    public readonly struct CardOrigin
    {
        public readonly Vector3 Position;
        public readonly float Scale;

        public CardOrigin(Vector3 position, float scale)
        {
            Position = position;
            Scale = scale;
        }
    }

    /// <summary>
    /// D'où vient une carte qui arrive sur la table ou une défausse : de la main
    /// du joueur local (position réelle de la carte), sinon de la pastille du
    /// joueur. Branché par GameBootstrapper ; null = pas d'animation de trajet.
    /// </summary>
    public static class CardOrigins
    {
        public static Func<int, CardModel, CardOrigin?>? Provider { get; set; }

        public static CardOrigin? Of(int playerIndex, CardModel card)
        {
            if (Provider == null || GameSettings.ReducedMotion) return null;
            return Provider(playerIndex, card);
        }
    }

    /// <summary>
    /// Cartes « en vol » au-dessus de la table : ce que font les adversaires
    /// (pioche au paquet = dos de carte, ramassage d'une défausse = face) se
    /// voit au lieu de se deviner. Instances recyclées (aucune création en jeu
    /// après les premières), couche sans raycast.
    /// </summary>
    public class CardFlights : MonoBehaviour
    {
        private const float Duration = 0.38f;

        private RectTransform? _layer;
        private GameObject? _prefab;
        private readonly Stack<CardView> _pool = new();

        public void Setup(Canvas canvas, GameObject? cardPrefab)
        {
            _prefab = cardPrefab;
            GameObject layer = new("CardFlights", typeof(RectTransform), typeof(Canvas));
            layer.transform.SetParent(canvas.transform, false);
            _layer = (RectTransform)layer.transform;
            UISkin.Stretch(_layer);
            Canvas c = layer.GetComponent<Canvas>();
            c.overrideSorting = false;
        }

        /// <summary>Garde la couche au-dessus du jeu (les menus créés ensuite passent devant).</summary>
        public void BringToFront(int siblingIndex)
        {
            if (_layer != null) _layer.SetSiblingIndex(siblingIndex);
        }

        /// <summary>Fait voler une carte (face visible si <paramref name="card"/> est fourni, sinon dos).</summary>
        public void Fly(CardModel? card, Vector3 from, Vector3 to, float fromScale, float toScale)
        {
            if (_layer == null || _prefab == null || GameSettings.ReducedMotion) return;
            if (!isActiveAndEnabled) return;

            CardView view = Get();
            if (card.HasValue) view.Setup(card.Value, true);
            else view.Setup(default, false);

            RectTransform rt = view.RectTransform;
            rt.position = from;
            float parentScale = Mathf.Max(0.0001f, _layer.lossyScale.x);
            rt.localScale = Vector3.one * (fromScale / parentScale);
            rt.localRotation = Quaternion.identity;
            view.SetAlpha(1f);
            FrameBudget.Ping(1.2f);
            StartCoroutine(FlyRoutine(view, from, to, fromScale / parentScale, toScale / parentScale));
        }

        private IEnumerator FlyRoutine(CardView view, Vector3 from, Vector3 to, float s0, float s1)
        {
            RectTransform rt = view.RectTransform;
            Vector3 lift = Vector3.up * (Vector3.Distance(from, to) * 0.15f);
            float tilt = from.x < to.x ? -10f : 10f;
            float elapsed = 0f;
            while (elapsed < Duration)
            {
                if (view == null) yield break;
                elapsed += Time.deltaTime;
                float k = UITween.EaseInOutCubic(Mathf.Clamp01(elapsed / Duration));
                Vector3 a = Vector3.Lerp(from, (from + to) * 0.5f + lift, k);
                Vector3 b = Vector3.Lerp((from + to) * 0.5f + lift, to, k);
                rt.position = Vector3.Lerp(a, b, k);
                rt.localScale = Vector3.one * Mathf.Lerp(s0, s1, k);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI) * tilt);
                if (k > 0.8f) view.SetAlpha(Mathf.Lerp(1f, 0f, (k - 0.8f) / 0.2f));
                yield return null;
            }
            Release(view);
        }

        private CardView Get()
        {
            while (_pool.Count > 0)
            {
                CardView pooled = _pool.Pop();
                if (pooled == null) continue;
                pooled.gameObject.SetActive(true);
                pooled.RectTransform.SetAsLastSibling();
                return pooled;
            }

            GameObject obj = Instantiate(_prefab!, _layer);
            CardView view = obj.GetComponent<CardView>();
            view.SetInteractable(false);
            return view;
        }

        private void Release(CardView view)
        {
            if (view == null) return;
            view.gameObject.SetActive(false);
            _pool.Push(view);
        }
    }
}
