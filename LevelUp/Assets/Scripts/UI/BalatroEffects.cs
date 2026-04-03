using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Effets visuels style Balatro declenchees automatiquement sur les events du jeu :
    /// particules, flash ecran, texte flottant, particules ambiantes.
    /// </summary>
    public class BalatroEffects : MonoBehaviour
    {
        [SerializeField] private AnimationController? _animController;
        [SerializeField] private Canvas? _canvas;
        [SerializeField] private RectTransform? _tableCenter;

        private void OnEnable()
        {
            EventBus.Subscribe<LevelLaidDownEvent>(HandleLevelLaidDown);
            EventBus.Subscribe<RoundEndedEvent>(HandleRoundEnded);
            EventBus.Subscribe<GameOverEvent>(HandleGameOver);
            EventBus.Subscribe<CardDrawnEvent>(HandleCardDrawn);
            EventBus.Subscribe<LevelCompletedEvent>(HandleLevelCompleted);
            EventBus.Subscribe<CardDiscardedEvent>(HandleCardDiscarded);
            EventBus.Subscribe<ActionCardPlayedEvent>(HandleActionCardPlayed);
            EventBus.Subscribe<PlayerSkippedEvent>(HandlePlayerSkipped);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LevelLaidDownEvent>(HandleLevelLaidDown);
            EventBus.Unsubscribe<RoundEndedEvent>(HandleRoundEnded);
            EventBus.Unsubscribe<GameOverEvent>(HandleGameOver);
            EventBus.Unsubscribe<CardDrawnEvent>(HandleCardDrawn);
            EventBus.Unsubscribe<LevelCompletedEvent>(HandleLevelCompleted);
            EventBus.Unsubscribe<CardDiscardedEvent>(HandleCardDiscarded);
            EventBus.Unsubscribe<ActionCardPlayedEvent>(HandleActionCardPlayed);
            EventBus.Unsubscribe<PlayerSkippedEvent>(HandlePlayerSkipped);
        }

        private void Start()
        {
            StartCoroutine(RunAmbientParticles());
        }

        private void HandleLevelLaidDown(LevelLaidDownEvent evt)
        {
            if (_animController == null) return;

            Vector2 center = _tableCenter != null ? _tableCenter.anchoredPosition : Vector2.zero;
            Color color = Constants.CardGreen;

            _animController.SpawnParticleBurst(center, color, 16);
            _animController.AnimateScreenFlash(new Color(0.3f, 1f, 0.5f, 0.15f), 0.3f);

            ShowFloatingText($"LEVEL {evt.Level}!", center + Vector2.up * 50f, color);
        }

        private void HandleRoundEnded(RoundEndedEvent evt)
        {
            if (_animController == null) return;

            Vector2 center = Vector2.zero;
            _animController.SpawnParticleBurst(center, Constants.CardYellow, 20);
            _animController.AnimateScreenFlash(new Color(1f, 0.85f, 0.3f, 0.2f), 0.4f);

            ShowFloatingText("ROUND WIN!", center, Constants.CardYellow);
        }

        private void HandleGameOver(GameOverEvent evt)
        {
            if (_animController == null) return;
            StartCoroutine(RunGameOverSequence());
        }

        private void HandleCardDrawn(CardDrawnEvent evt)
        {
            if (_animController != null)
            {
                _animController.AnimateScreenFlash(new Color(1f, 1f, 1f, 0.05f), 0.15f);
            }
        }

        private void HandleLevelCompleted(LevelCompletedEvent evt)
        {
            if (_animController == null) return;

            Vector2 center = Vector2.zero;
            _animController.SpawnParticleBurst(center, Constants.CardGreen, 24);
            _animController.AnimateScreenFlash(new Color(0.3f, 1f, 0.5f, 0.2f), 0.4f);

            ShowFloatingText("+1 LEVEL!", center + Vector2.up * 30f, Constants.CardGreen, 48f);
        }

        private void HandleCardDiscarded(CardDiscardedEvent evt)
        {
            if (_animController == null) return;

            CardModel card = evt.Card;
            if (card.IsAction)
            {
                Color flashColor = card.Type switch
                {
                    CardType.Skip => new Color(1f, 0.3f, 0.4f, 0.12f),
                    CardType.Draw2 => new Color(1f, 0.55f, 0.25f, 0.12f),
                    CardType.Wild => new Color(0.3f, 1f, 0.57f, 0.12f),
                    CardType.WildDraw2 => new Color(0.73f, 0.42f, 1f, 0.12f),
                    _ => new Color(1f, 1f, 1f, 0.05f)
                };
                _animController.AnimateScreenFlash(flashColor, 0.2f);
            }
        }

        private void HandleActionCardPlayed(ActionCardPlayedEvent evt)
        {
            if (_animController == null) return;

            Vector2 center = Vector2.zero;
            Color color = evt.Card.Type switch
            {
                CardType.Skip => Constants.CardRed,
                CardType.Draw2 => Constants.CardOrange,
                CardType.Wild => Constants.CardGreen,
                CardType.WildDraw2 => Constants.CardPurple,
                _ => Constants.TextPrimary
            };

            _animController.SpawnParticleBurst(center, color, 10);

            string text = evt.Card.Type switch
            {
                CardType.Skip => "SKIP!",
                CardType.Draw2 => "+2!",
                CardType.Wild => "WILD!",
                CardType.WildDraw2 => "WILD +2!",
                _ => "!"
            };

            ShowFloatingText(text, center + Vector2.up * 60f, color, 42f);
        }

        private void HandlePlayerSkipped(PlayerSkippedEvent evt)
        {
            if (_animController == null) return;
            _animController.AnimateScreenFlash(new Color(1f, 0.3f, 0.3f, 0.15f), 0.25f);
        }

        private IEnumerator RunGameOverSequence()
        {
            Vector2 center = Vector2.zero;

            for (int i = 0; i < 3; i++)
            {
                Color burstColor = i switch
                {
                    0 => Constants.CardYellow,
                    1 => Constants.CardPurple,
                    _ => Constants.CardRed
                };

                Vector2 offset = new(Random.Range(-100f, 100f), Random.Range(-50f, 50f));
                _animController!.SpawnParticleBurst(center + offset, burstColor, 15);
                yield return new WaitForSeconds(0.15f);
            }

            _animController!.AnimateScreenFlash(new Color(1f, 1f, 1f, 0.25f), 0.5f);
            ShowFloatingText("VICTORY!", center, Constants.TextAccent, 72f);
        }

        private IEnumerator RunAmbientParticles()
        {
            if (_canvas == null) yield break;

            while (true)
            {
                yield return new WaitForSeconds(Random.Range(2f, 4f));
                StartCoroutine(RunSingleAmbientParticle());
            }
        }

        private IEnumerator RunSingleAmbientParticle()
        {
            if (_canvas == null) yield break;

            GameObject p = new("AmbientP", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            p.transform.SetParent(_canvas.transform, false);
            p.transform.SetAsFirstSibling();

            RectTransform rt = p.GetComponent<RectTransform>();
            float startX = Random.Range(-500f, 500f);
            float startY = -400f;
            rt.anchoredPosition = new Vector2(startX, startY);
            float size = Random.Range(3f, 6f);
            rt.sizeDelta = new Vector2(size, size);

            Image img = p.GetComponent<Image>();
            Color[] ambientColors = { Constants.CardBlue, Constants.CardPurple, Constants.CardGreen };
            Color c = ambientColors[Random.Range(0, ambientColors.Length)];
            c.a = 0.08f;
            img.color = c;
            img.raycastTarget = false;

            CanvasGroup cg = p.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
            cg.alpha = 0f;

            float duration = Random.Range(6f, 10f);
            float driftX = Random.Range(-30f, 30f);
            float speed = Random.Range(40f, 80f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                float x = startX + Mathf.Sin(elapsed * 0.5f) * driftX;
                float y = startY + elapsed * speed;
                rt.anchoredPosition = new Vector2(x, y);

                float alpha = t < 0.2f ? t / 0.2f : t > 0.8f ? (1f - t) / 0.2f : 1f;
                cg.alpha = alpha * 0.08f;

                yield return null;
            }

            Destroy(p);
        }

        private void ShowFloatingText(string text, Vector2 position, Color color, float fontSize = 36f)
        {
            if (_canvas == null) return;
            StartCoroutine(RunFloatingText(text, position, color, fontSize));
        }

        private IEnumerator RunFloatingText(string text, Vector2 position, Color color, float fontSize)
        {
            GameObject textObj = new("FloatingText", typeof(RectTransform),
                typeof(TextMeshProUGUI), typeof(CanvasGroup));
            textObj.transform.SetParent(_canvas!.transform, false);
            textObj.transform.SetAsLastSibling();

            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(400f, 80f);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            tmp.outlineWidth = 0.3f;
            tmp.outlineColor = new Color32(0, 0, 0, 180);

            CanvasGroup cg = textObj.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // Pop in with overshoot
            rt.localScale = Vector3.one * 0.3f;
            float elapsed = 0f;
            float popDuration = 0.2f;

            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / popDuration;
                float scale = t < 0.7f
                    ? Mathf.Lerp(0.3f, 1.2f, t / 0.7f)
                    : Mathf.Lerp(1.2f, 1f, (t - 0.7f) / 0.3f);
                rt.localScale = Vector3.one * scale;
                yield return null;
            }

            rt.localScale = Vector3.one;

            // Float up + fade
            float floatDuration = 1.2f;
            elapsed = 0f;
            Vector2 startPos = position;

            while (elapsed < floatDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / floatDuration;

                rt.anchoredPosition = startPos + Vector2.up * (80f * t);
                cg.alpha = t < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);

                yield return null;
            }

            Destroy(textObj);
        }
    }
}
