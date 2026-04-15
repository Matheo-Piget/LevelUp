using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Menu principal overlay — procédural, sans asset externe.
    /// Titre animé + boutons Jouer/Options/Quitter + cartes décoratives qui dérivent
    /// en arrière-plan pour donner vie à l'écran. Appelle OnPlayClicked quand
    /// le joueur lance la partie ; le bootstrapper masque alors le menu.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private RectTransform? _root;
        private CanvasGroup? _rootCanvasGroup;
        private TextMeshProUGUI? _titleText;
        private RectTransform? _buttonColumn;
        private OptionsPanel? _optionsPanel;
        private float _time;
        private readonly List<RectTransform> _floatingCards = new();
        private readonly List<Vector2> _cardOrigins = new();
        private readonly List<Vector2> _cardAmplitudes = new();
        private readonly List<float> _cardPhases = new();
        private readonly List<float> _cardSpeeds = new();
        private readonly List<float> _cardSpinSpeeds = new();

        /// <summary>Déclenché quand le joueur clique sur Jouer.</summary>
        public event Action? OnPlayClicked;

        /// <summary>Indique si le menu est actuellement visible.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// Construit le menu sous le canvas parent. Visible par défaut.
        /// </summary>
        public void Setup(Canvas canvas)
        {
            if (canvas == null) return;

            GameObject rootObj = new("MainMenuOverlay", typeof(RectTransform), typeof(CanvasGroup));
            rootObj.transform.SetParent(canvas.transform, false);
            rootObj.transform.SetAsLastSibling();

            _root = rootObj.GetComponent<RectTransform>();
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.sizeDelta = Vector2.zero;
            _root.anchoredPosition = Vector2.zero;

            _rootCanvasGroup = rootObj.GetComponent<CanvasGroup>();
            _rootCanvasGroup.alpha = 1f;
            _rootCanvasGroup.blocksRaycasts = true;

            // Voile sombre pour lisibilité au-dessus du jeu
            CreateBackgroundVeil(_root);

            // Cartes décoratives qui flottent
            CreateFloatingCards(_root);

            // Titre
            CreateTitle(_root);

            // Boutons
            CreateButtonColumn(_root);

            // Footer crédits
            CreateFooter(_root);

            // Pop-in animation
            if (_titleText != null)
            {
                RectTransform titleRt = _titleText.rectTransform;
                titleRt.localScale = Vector3.zero;
                UITween.ScaleTo(rootObj, titleRt, Vector3.one, 0.55f);
            }

            if (_buttonColumn != null)
            {
                UITween.SlideIn(rootObj, _buttonColumn, new Vector2(0f, -80f), 0.5f);
            }

            IsVisible = true;
        }

        private void CreateBackgroundVeil(RectTransform parent)
        {
            GameObject veil = new("Veil", typeof(RectTransform), typeof(Image));
            veil.transform.SetParent(parent, false);
            RectTransform rt = veil.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image img = veil.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.55f);
            img.raycastTarget = true; // bloque les clics sur le jeu dessous
        }

        private void CreateTitle(RectTransform parent)
        {
            // Glow derrière le titre
            GameObject glow = new("TitleGlow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(parent, false);
            RectTransform grt = glow.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.5f, 1f);
            grt.anchorMax = new Vector2(0.5f, 1f);
            grt.pivot = new Vector2(0.5f, 1f);
            grt.sizeDelta = new Vector2(900f, 360f);
            grt.anchoredPosition = new Vector2(0f, -120f);
            Image gimg = glow.GetComponent<Image>();
            gimg.sprite = UIFactory.SoftShadowSprite;
            gimg.color = new Color(Constants.CardPurple.r, Constants.CardPurple.g, Constants.CardPurple.b, 0.35f);
            gimg.raycastTarget = false;

            // Titre principal
            _titleText = UIFactory.CreateText(parent, "Title", "LEVEL UP", 120f,
                Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform trt = _titleText.rectTransform;
            trt.anchorMin = new Vector2(0.5f, 1f);
            trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(900f, 160f);
            trt.anchoredPosition = new Vector2(0f, -160f);
            _titleText.enableVertexGradient = true;
            _titleText.colorGradient = new VertexGradient(
                Color.Lerp(Constants.CardYellow, Color.white, 0.25f),
                Color.Lerp(Constants.CardOrange, Color.white, 0.15f),
                Color.Lerp(Constants.CardPurple, Color.white, 0.15f),
                Color.Lerp(Constants.CardBlue, Color.white, 0.25f));
            _titleText.outlineWidth = 0.25f;
            _titleText.outlineColor = new Color32(0, 0, 0, 180);
            _titleText.characterSpacing = 12f;

            // Sous-titre
            TextMeshProUGUI subtitle = UIFactory.CreateText(parent, "Subtitle", "LE JEU DE CARTES MONTE-NIVEAU",
                20f, Constants.TextSecondary, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform srt = subtitle.rectTransform;
            srt.anchorMin = new Vector2(0.5f, 1f);
            srt.anchorMax = new Vector2(0.5f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.sizeDelta = new Vector2(900f, 30f);
            srt.anchoredPosition = new Vector2(0f, -310f);
            subtitle.characterSpacing = 20f;
        }

        private void CreateButtonColumn(RectTransform parent)
        {
            GameObject col = new("ButtonColumn", typeof(RectTransform));
            col.transform.SetParent(parent, false);
            _buttonColumn = col.GetComponent<RectTransform>();
            _buttonColumn.anchorMin = new Vector2(0.5f, 0.5f);
            _buttonColumn.anchorMax = new Vector2(0.5f, 0.5f);
            _buttonColumn.sizeDelta = new Vector2(320f, 320f);
            _buttonColumn.anchoredPosition = new Vector2(0f, -80f);

            VerticalLayoutGroup layout = col.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            UIFactory.CreateButton(col.transform, "BtnPlay", "JOUER",
                new Vector2(300f, 74f), HandlePlayClicked, Constants.CardGreen);

            UIFactory.CreateButton(col.transform, "BtnOptions", "OPTIONS",
                new Vector2(300f, 74f), HandleOptionsClicked, Constants.CardPurple);

            UIFactory.CreateButton(col.transform, "BtnQuit", "QUITTER",
                new Vector2(300f, 74f), HandleQuitClicked, Constants.CardRed);
        }

        private void CreateFooter(RectTransform parent)
        {
            TextMeshProUGUI footer = UIFactory.CreateText(parent, "Footer", "v0.1 — PROJET PERSO",
                14f, Constants.TextSecondary, TextAlignmentOptions.Center, FontStyles.Normal);
            RectTransform rt = footer.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(400f, 20f);
            rt.anchoredPosition = new Vector2(0f, 20f);
            footer.characterSpacing = 6f;
        }

        /// <summary>
        /// Crée 6 cartes décoratives qui dérivent lentement en arrière-plan.
        /// Donne du mouvement au menu sans distraire.
        /// </summary>
        private void CreateFloatingCards(RectTransform parent)
        {
            Color[] colors =
            {
                Constants.CardBlue, Constants.CardRed, Constants.CardGreen,
                Constants.CardPurple, Constants.CardOrange, Constants.CardYellow
            };
            Vector2[] positions =
            {
                new(-550f, 150f), new(520f, -200f), new(-420f, -280f),
                new(480f, 240f), new(-620f, -80f), new(580f, 60f)
            };

            for (int i = 0; i < 6; i++)
            {
                GameObject card = new($"FloatCard_{i}", typeof(RectTransform), typeof(Image));
                card.transform.SetParent(parent, false);

                RectTransform rt = card.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(90f, 135f);
                rt.anchoredPosition = positions[i];
                rt.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-25f, 25f));

                Image img = card.GetComponent<Image>();
                img.sprite = UIFactory.RoundedSprite;
                img.type = Image.Type.Sliced;
                Color tinted = Color.Lerp(Constants.CardFaceColor, colors[i], 0.35f);
                tinted.a = 0.55f;
                img.color = tinted;
                img.raycastTarget = false;

                // Bordure glow
                GameObject ring = new("Ring", typeof(RectTransform), typeof(Image));
                ring.transform.SetParent(card.transform, false);
                RectTransform rRt = ring.GetComponent<RectTransform>();
                rRt.anchorMin = Vector2.zero;
                rRt.anchorMax = Vector2.one;
                rRt.sizeDelta = new Vector2(2f, 2f);
                Image rImg = ring.GetComponent<Image>();
                rImg.sprite = UIFactory.RingSprite;
                rImg.type = Image.Type.Sliced;
                rImg.color = new Color(colors[i].r, colors[i].g, colors[i].b, 0.8f);
                rImg.raycastTarget = false;

                _floatingCards.Add(rt);
                _cardOrigins.Add(positions[i]);
                _cardAmplitudes.Add(new Vector2(
                    UnityEngine.Random.Range(20f, 60f),
                    UnityEngine.Random.Range(20f, 50f)));
                _cardPhases.Add(UnityEngine.Random.Range(0f, Mathf.PI * 2f));
                _cardSpeeds.Add(UnityEngine.Random.Range(0.2f, 0.45f));
                _cardSpinSpeeds.Add(UnityEngine.Random.Range(-8f, 8f));
            }
        }

        private void Update()
        {
            if (!IsVisible) return;

            _time += Time.unscaledDeltaTime;

            // Cartes qui flottent
            for (int i = 0; i < _floatingCards.Count; i++)
            {
                if (_floatingCards[i] == null) continue;
                float phase = _cardPhases[i] + _time * _cardSpeeds[i];
                float dx = Mathf.Sin(phase) * _cardAmplitudes[i].x;
                float dy = Mathf.Cos(phase * 0.85f) * _cardAmplitudes[i].y;
                _floatingCards[i].anchoredPosition = _cardOrigins[i] + new Vector2(dx, dy);

                float baseAngle = _cardSpinSpeeds[i] * Mathf.Sin(_time * 0.3f + _cardPhases[i]);
                _floatingCards[i].localRotation = Quaternion.Euler(0, 0, baseAngle);
            }

            // Titre qui pulse
            if (_titleText != null)
            {
                float pulse = 1f + Mathf.Sin(_time * 1.8f) * 0.025f;
                _titleText.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }

        private void HandlePlayClicked()
        {
            Hide(() => OnPlayClicked?.Invoke());
        }

        private void HandleOptionsClicked()
        {
            if (_optionsPanel == null && _root != null)
            {
                _optionsPanel = _root.gameObject.AddComponent<OptionsPanel>();
                _optionsPanel.Setup(_root);
            }
            _optionsPanel?.Show();
        }

        private void HandleQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Cache le menu avec une animation de fade-out.
        /// </summary>
        public void Hide(Action? onComplete = null)
        {
            if (!IsVisible || _root == null || _rootCanvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            IsVisible = false;
            _rootCanvasGroup.blocksRaycasts = false;

            UITween.FadeTo(_root.gameObject, _rootCanvasGroup, 0f, 0.4f, () =>
            {
                _root.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Réaffiche le menu (depuis le pause menu par exemple).
        /// </summary>
        public void Show()
        {
            if (IsVisible || _root == null || _rootCanvasGroup == null) return;

            _root.gameObject.SetActive(true);
            IsVisible = true;
            _rootCanvasGroup.blocksRaycasts = true;
            UITween.FadeTo(_root.gameObject, _rootCanvasGroup, 1f, 0.4f);
        }
    }
}
