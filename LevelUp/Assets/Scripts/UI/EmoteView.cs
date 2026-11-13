using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;

namespace LevelUp.UI
{
    /// <summary>
    /// Réactions en ligne : un bouton « Réagir » (bas-droite, au-dessus du tri)
    /// ouvre six réactions prédéfinies ; chaque réaction reçue s'affiche en
    /// bulle sous la pastille de son joueur pendant quelques secondes.
    /// Visible uniquement en partie en ligne.
    /// </summary>
    public class EmoteView : MonoBehaviour
    {
        private const float BubbleSeconds = 2.6f;

        private RectTransform? _root;
        private LevelProgressView? _players;
        private GameObject? _button;
        private GameObject? _menu;
        private readonly Dictionary<int, GameObject> _bubbles = new();

        /// <summary>Le joueur local a choisi une réaction (identifiant dans <see cref="SocialProtocol.Emotes"/>).</summary>
        public event Action<int>? EmoteChosen;

        public void Setup(RectTransform root, LevelProgressView? players)
        {
            _root = root;
            _players = players;

            Image button = UISkin.Rect(root, "BtnEmote", UISkin.Surface, 16f, raycast: true);
            button.gameObject.AddComponent<UIInputBlocker>();
            UISkin.Place(button.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 142f),
                new Vector2(132f, 48f));
            UISkin.Outline(button.rectTransform, 16f, 1f, UISkin.Line);
            TextMeshProUGUI label = UISkin.Text(button.transform, "Label", "Réagir", 15f, UISkin.TextColor,
                TextWeight.SemiBold, TextAlignmentOptions.Center);
            UISkin.Stretch(label.rectTransform);
            Button b = button.gameObject.AddComponent<Button>();
            b.targetGraphic = button;
            b.onClick.AddListener(ToggleMenu);
            _button = button.gameObject;

            // Menu : six réactions, deux colonnes, au-dessus du bouton.
            Image menu = UISkin.Rect(root, "EmoteMenu", UISkin.WithAlpha(UISkin.Surface, 0.98f), 18f, raycast: true);
            menu.gameObject.AddComponent<UIInputBlocker>();
            UISkin.Place(menu.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 200f),
                new Vector2(340f, 178f));
            UISkin.Outline(menu.rectTransform, 18f, 1f, UISkin.LineStrong);
            for (int i = 0; i < SocialProtocol.Emotes.Length; i++)
            {
                int id = i;
                Image chip = UISkin.Rect(menu.transform, $"Emote{i}", UISkin.Base, 14f, raycast: true);
                UISkin.Place(chip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(12f + (i % 2) * 162f, -12f - (i / 2) * 54f), new Vector2(154f, 46f));
                TextMeshProUGUI text = UISkin.Text(chip.transform, "Label", SocialProtocol.Emotes[i], 15f,
                    UISkin.TextColor, TextWeight.Medium, TextAlignmentOptions.Center);
                UISkin.Stretch(text.rectTransform);
                Button cb = chip.gameObject.AddComponent<Button>();
                cb.targetGraphic = chip;
                cb.onClick.AddListener(() =>
                {
                    if (_menu != null) _menu.SetActive(false);
                    EmoteChosen?.Invoke(id);
                });
            }
            _menu = menu.gameObject;
            _menu.SetActive(false);
            _button.SetActive(false);
        }

        /// <summary>Affiche ou cache le bouton (partie en ligne en cours seulement).</summary>
        public void SetAvailable(bool available)
        {
            if (_button != null) _button.SetActive(available);
            if (!available && _menu != null) _menu.SetActive(false);
        }

        private void ToggleMenu()
        {
            if (_menu != null) _menu.SetActive(!_menu.activeSelf);
        }

        /// <summary>Affiche la réaction d'un joueur sous sa pastille.</summary>
        public void ShowEmote(int seat, int emoteId)
        {
            if (_root == null || !SocialProtocol.IsValidEmote(emoteId)) return;
            if (_players == null || !_players.TryGetPillOrigin(seat, out CardOrigin origin)) return;

            if (_bubbles.TryGetValue(seat, out GameObject? old) && old != null) Destroy(old);

            Image bubble = UISkin.Rect(_root, "EmoteBubble", UISkin.Accent, 16f);
            bubble.raycastTarget = false;
            UISkin.HRow(bubble.gameObject, 0f, new RectOffset(16, 16, 8, 8), TextAnchor.MiddleCenter);
            ContentSizeFitter fit = bubble.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UISkin.Text(bubble.transform, "Text", SocialProtocol.Emotes[emoteId], 17f, UISkin.OnAccent,
                TextWeight.SemiBold, TextAlignmentOptions.Center);

            RectTransform rt = bubble.rectTransform;
            rt.pivot = new Vector2(0f, 1f);
            rt.position = origin.Position;
            rt.anchoredPosition += new Vector2(-6f, -30f);
            rt.SetAsLastSibling();

            _bubbles[seat] = bubble.gameObject;
            AudioManager.Instance?.PlayClick();
            StartCoroutine(FadeOut(bubble.gameObject));
        }

        private static IEnumerator FadeOut(GameObject bubble)
        {
            CanvasGroup cg = bubble.AddComponent<CanvasGroup>();
            FrameBudget.Ping(BubbleSeconds + 0.5f);
            yield return new WaitForSecondsRealtime(BubbleSeconds);
            float t = 0f;
            while (t < 0.3f && bubble != null)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = 1f - t / 0.3f;
                yield return null;
            }
            if (bubble != null) Destroy(bubble);
        }
    }
}
