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
        private RectTransform? _leftColumn;
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

        /// <summary>Déclenché quand le joueur clique sur En ligne.</summary>
        public event Action? OnOnlineClicked;

        /// <summary>Indique si le menu est actuellement visible.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>Racine du menu — pour y parenter des panels (ex: OnlinePanel).</summary>
        public RectTransform? Root => _root;

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

            // Apparition douce (fondu) — les éléments sont dans des layout groups,
            // on n'anime donc pas leur position.
            _rootCanvasGroup.alpha = 0f;
            UITween.FadeTo(rootObj, _rootCanvasGroup, 1f, 0.35f);

            IsVisible = true;
        }

        private void CreateBackgroundVeil(RectTransform parent)
        {
            // Fond opaque (la partie derrière reste cachée) + lumière douce à
            // droite : radial-gradient(ellipse at 70% 50%, #151C26 → #0F1419 → #0A0E12).
            GameObject veil = new("Veil", typeof(RectTransform), typeof(Image));
            veil.transform.SetParent(parent, false);
            UISkin.Stretch((RectTransform)veil.transform);
            Image img = veil.GetComponent<Image>();
            img.color = UISkin.Ground;
            img.raycastTarget = true; // bloque les clics sur le jeu dessous

            GameObject glow = new("Light", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(veil.transform, false);
            RectTransform lrt = (RectTransform)glow.transform;
            lrt.anchorMin = new Vector2(0.25f, -0.25f);
            lrt.anchorMax = new Vector2(1.15f, 1.25f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            Image limg = glow.GetComponent<Image>();
            limg.sprite = UISkin.Radial;
            limg.color = UISkin.Hex(0x151C26);
            limg.raycastTarget = false;
        }

        private void CreateTitle(RectTransform parent)
        {
            // Colonne gauche (x = 160) : titre, sous-titre, navigation.
            GameObject col = new("LeftColumn", typeof(RectTransform));
            col.transform.SetParent(parent, false);
            RectTransform crt = (RectTransform)col.transform;
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(0f, 1f);
            crt.pivot = new Vector2(0f, 0.5f);
            crt.offsetMin = new Vector2(160f, 0f);
            crt.offsetMax = new Vector2(160f + 560f, 0f);
            UISkin.VCol(col, 14f, null, TextAnchor.MiddleLeft);
            _leftColumn = crt;

            _titleText = UISkin.Text(col.transform, "Title", "LEVEL <color=#D8B66E>UP</color>", 112f,
                UISkin.TextColor, TextWeight.Display);
            _titleText.characterSpacing = -4f;
            UISkin.Size(_titleText.gameObject, 560f, 112f);

            TextMeshProUGUI subtitle = UISkin.Text(col.transform, "Subtitle", "LE JEU DE CARTES MONTE-NIVEAU",
                16f, UISkin.TextMuted, TextWeight.SemiBold);
            subtitle.characterSpacing = 24f;
            UISkin.Size(subtitle.gameObject, 560f, 24f);

            GameObject spacer = new("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(col.transform, false);
            UISkin.Size(spacer, 10f, 42f);
        }

        private void CreateButtonColumn(RectTransform parent)
        {
            Transform host = _leftColumn != null ? _leftColumn : parent;
            GameObject nav = new("ButtonColumn", typeof(RectTransform));
            nav.transform.SetParent(host, false);
            _buttonColumn = (RectTransform)nav.transform;
            UISkin.VCol(nav, 14f, null, TextAnchor.UpperLeft);
            VerticalLayoutGroup vlg = nav.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            UISkin.Size(nav, 400f, 72f + 60f * 3f + 14f * 3f);

            Button play = UIFactory.CreateButton(nav.transform, "BtnPlay", "JOUER",
                new Vector2(400f, 72f), HandlePlayClicked, primary: true);
            // Libellé à gauche + icône lecture à droite (maquette)
            LeftAlign(play);
            Image playIcon = UISkin.IconImage(play.transform, "PlayIcon", UIIcon.Play, 24f, UISkin.OnAccent);
            UISkin.Place(playIcon.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-28f, 0f), new Vector2(24f, 24f));

            LeftAlign(UIFactory.CreateButton(nav.transform, "BtnOnline", "EN LIGNE",
                new Vector2(400f, 60f), () => OnOnlineClicked?.Invoke()));
            LeftAlign(UIFactory.CreateButton(nav.transform, "BtnOptions", "OPTIONS",
                new Vector2(400f, 60f), HandleOptionsClicked));
            LeftAlign(UIFactory.CreateButton(nav.transform, "BtnQuit", "QUITTER",
                new Vector2(400f, 60f), HandleQuitClicked, subtle: true));
        }

        private static void LeftAlign(Button b)
        {
            TextMeshProUGUI? l = b.GetComponentInChildren<TextMeshProUGUI>();
            if (l == null) return;
            l.alignment = TextAlignmentOptions.MidlineLeft;
            l.rectTransform.offsetMin = new Vector2(28f, 0f);
        }

        private void CreateFooter(RectTransform parent)
        {
            // Pas de footer dans la maquette.
            _ = parent;
        }

        /// <summary>
        /// Cartes de la maquette (vraies faces : couleur, valeur, symbole) qui
        /// dérivent lentement à droite — fond vivant sans distraire.
        /// </summary>
        private void CreateFloatingCards(RectTransform parent)
        {
            // x, y (coin haut-gauche, repère 1920×1080), rotation, valeur, couleur, opacité
            (float x, float y, float r, int v, CardColor c, float a)[] cards =
            {
                (1000f, 180f, -14f, 4, CardColor.Blue, 0.9f),
                (1160f, 140f, -5f, 5, CardColor.Red, 1f),
                (1320f, 150f, 6f, 6, CardColor.Green, 1f),
                (1480f, 200f, 15f, 7, CardColor.Yellow, 0.9f),
                (1080f, 560f, 9f, 12, CardColor.Purple, 0.55f),
                (1400f, 620f, -11f, 12, CardColor.Orange, 0.55f),
                (1660f, 480f, 22f, 18, CardColor.Blue, 0.35f)
            };

            foreach ((float x, float y, float r, int v, CardColor c, float a) in cards)
            {
                // Conteneur (déplacé/tourné par Update) : ombre puis face.
                GameObject holder = new($"FloatCard_{v}_{c}", typeof(RectTransform), typeof(CanvasGroup));
                holder.transform.SetParent(parent, false);
                RectTransform rt = (RectTransform)holder.transform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(150f, 214f);
                Vector2 pos = new(x + 75f - 960f, 540f - (y + 107f));
                rt.anchoredPosition = pos;
                rt.localRotation = Quaternion.Euler(0f, 0f, -r);
                CanvasGroup cg = holder.GetComponent<CanvasGroup>();
                cg.alpha = a;
                cg.blocksRaycasts = false;

                Image shadow = UISkin.Rect(rt, "Shadow", new Color(0f, 0f, 0f, 0.35f), 22f);
                UISkin.Stretch(shadow.rectTransform, -6f);
                shadow.rectTransform.anchoredPosition = new Vector2(0f, -22f);

                Image face = UISkin.Rect(rt, "Face", Constants.GetColor(c), 18f);
                UISkin.Stretch(face.rectTransform);

                Color ink = UISkin.InkFor(c);
                Image wm = UISkin.SuitImage(face.transform, "Watermark", c, 120f, UISkin.WithAlpha(ink, 0.16f));
                UISkin.Place(wm.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(120f, 120f));
                TextMeshProUGUI corner = UISkin.Text(face.transform, "Corner", v.ToString(), 24f, ink,
                    TextWeight.Display, TextAlignmentOptions.TopLeft);
                UISkin.Place(corner.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -10f),
                    new Vector2(60f, 30f));
                TextMeshProUGUI value = UISkin.Text(face.transform, "Value", v.ToString(), 76f, ink,
                    TextWeight.Display, TextAlignmentOptions.Center);
                value.characterSpacing = -3f;
                UISkin.Stretch(value.rectTransform);
                UISkin.Outline(face.rectTransform, 18f, 2f, new Color(1f, 1f, 1f, 0.2f));

                _floatingCards.Add(rt);
                _cardOrigins.Add(pos);
                _cardAmplitudes.Add(new Vector2(UnityEngine.Random.Range(8f, 18f), UnityEngine.Random.Range(10f, 22f)));
                _cardPhases.Add(UnityEngine.Random.Range(0f, Mathf.PI * 2f));
                _cardSpeeds.Add(UnityEngine.Random.Range(0.25f, 0.45f));
                _cardSpinSpeeds.Add(-r);
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

                // Inclinaison de la maquette + léger balancement (±2.5°)
                float angle = _cardSpinSpeeds[i] + 2.5f * Mathf.Sin(_time * 0.3f + _cardPhases[i]);
                _floatingCards[i].localRotation = Quaternion.Euler(0, 0, angle);
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
