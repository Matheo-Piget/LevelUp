using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// "Choisis ta pose" : quand les cartes permettent plusieurs façons valides
    /// de poser le contrat (ex. un 7 rouge qui peut aller dans le brelan OU
    /// dans la couleur), le joueur choisit la répartition au lieu de subir
    /// celle du jeu. Une seule possibilité = pose directe (zéro friction).
    /// </summary>
    public class LayDownChooser : MonoBehaviour
    {
        /// <summary>Vrai tant que le panneau est affiché (l'input de jeu est suspendu).</summary>
        public static bool IsOpen { get; private set; }

        private const int MaxRows = 4;

        private RectTransform? _root;
        private CanvasGroup? _cg;
        private RectTransform? _rows;
        private TextMeshProUGUI? _note;
        private Action<List<Meld>>? _onPick;

        public void Setup(Canvas canvas)
        {
            UISkin.EnsureFonts();

            GameObject rootObj = new("LayDownChooser", typeof(RectTransform), typeof(CanvasGroup));
            rootObj.transform.SetParent(canvas.transform, false);
            _root = (RectTransform)rootObj.transform;
            UISkin.Stretch(_root);
            _cg = rootObj.GetComponent<CanvasGroup>();

            // Voile : toucher à côté = annuler
            GameObject veil = new("Veil", typeof(RectTransform), typeof(Image), typeof(Button));
            veil.transform.SetParent(_root, false);
            UISkin.Stretch((RectTransform)veil.transform);
            veil.GetComponent<Image>().color = UISkin.WithAlpha(UISkin.Ground, 0.78f);
            Button veilBtn = veil.GetComponent<Button>();
            veilBtn.transition = Selectable.Transition.None;
            veilBtn.onClick.AddListener(Hide);

            Image panel = UISkin.Rect(_root, "Panel", UISkin.Surface, 28f, raycast: true);
            RectTransform prt = panel.rectTransform;
            UISkin.Place(prt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(1040f, 0f));
            UISkin.Outline(prt, 28f, 1f, UISkin.Line).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UISkin.VCol(panel.gameObject, 18f, new RectOffset(40, 40, 34, 30), TextAnchor.UpperLeft);
            ContentSizeFitter fit = panel.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI title = UISkin.Text(panel.transform, "Title", "Choisis ta pose", 32f,
                UISkin.TextColor, TextWeight.Display);
            UISkin.Size(title.gameObject, 960f, 40f);
            TextMeshProUGUI sub = UISkin.Text(panel.transform, "Subtitle",
                "Tes cartes peuvent se répartir de plusieurs façons. Touche celle que tu veux poser.",
                15f, UISkin.TextMuted);
            UISkin.Size(sub.gameObject, 960f, 20f);

            GameObject rows = new("Rows", typeof(RectTransform));
            rows.transform.SetParent(panel.transform, false);
            _rows = (RectTransform)rows.transform;
            UISkin.VCol(rows, 12f, null, TextAnchor.UpperLeft);
            UISkin.Size(rows, 960f);

            _note = UISkin.Text(panel.transform, "Note",
                "D'autres répartitions existent : sélectionne précisément tes cartes pour les obtenir.",
                13f, UISkin.TextMuted);
            UISkin.Size(_note.gameObject, 960f, 18f);

            Button cancel = UIFactory.CreateButton(panel.transform, "BtnCancel", "ANNULER",
                new Vector2(220f, 52f), Hide, subtle: true);
            UISkin.Size(cancel.gameObject, 220f, 52f);

            rootObj.SetActive(false);
        }

        /// <summary>Affiche les options ; <paramref name="onPick"/> reçoit la pose choisie.</summary>
        public void Show(List<List<Meld>> options, bool hasMore, Action<List<Meld>> onPick)
        {
            if (_root == null || _rows == null || _cg == null) return;
            _onPick = onPick;

            for (int i = _rows.childCount - 1; i >= 0; i--) Destroy(_rows.GetChild(i).gameObject);
            int count = Mathf.Min(options.Count, MaxRows);
            for (int i = 0; i < count; i++) BuildRow(i, options[i]);
            if (_note != null) _note.gameObject.SetActive(hasMore || options.Count > MaxRows);

            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            IsOpen = true;
            _cg.alpha = 0f;
            UITween.FadeTo(_root.gameObject, _cg, 1f, 0.16f);
        }

        public void Hide()
        {
            if (_root == null || _cg == null || !IsOpen) return;
            IsOpen = false;
            _onPick = null;
            UITween.FadeTo(_root.gameObject, _cg, 0f, 0.12f, () =>
            {
                if (_root != null && !IsOpen) _root.gameObject.SetActive(false);
            });
        }

        private void OnDisable() => IsOpen = false;

        private void BuildRow(int index, List<Meld> option)
        {
            Image row = UISkin.Rect(_rows!, $"Option{index}", UISkin.Base, 18f, raycast: true);
            UISkin.HRow(row.gameObject, 34f, new RectOffset(22, 22, 16, 16), TextAnchor.MiddleLeft);
            UISkin.Size(row.gameObject, 960f);
            Image ring = UISkin.Outline(row.rectTransform, 18f, 1f, UISkin.Line);
            ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            Button btn = row.gameObject.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;
            btn.targetGraphic = row;
            List<Meld> captured = option;
            btn.onClick.AddListener(() =>
            {
                Action<List<Meld>>? pick = _onPick;
                Hide();
                pick?.Invoke(captured);
            });

            foreach (Meld meld in option)
            {
                GameObject block = new("Meld", typeof(RectTransform));
                block.transform.SetParent(row.transform, false);
                UISkin.VCol(block, 8f, null, TextAnchor.UpperLeft);

                UISkin.Text(block.transform, "Label", MeldLabel(meld), 13f, UISkin.TextMuted, TextWeight.SemiBold);

                GameObject cards = new("Cards", typeof(RectTransform));
                cards.transform.SetParent(block.transform, false);
                UISkin.HRow(cards, 6f, null, TextAnchor.MiddleLeft);

                List<CardModel> sorted = new(meld.Cards);
                sorted.Sort((a, b) => a.Value.CompareTo(b.Value));
                foreach (CardModel c in sorted) MiniCard(cards.transform, c);
            }
        }

        private static string MeldLabel(Meld meld) => meld.Type switch
        {
            MeldType.Run => $"Suite de {meld.Cards.Count}",
            MeldType.Flush => $"Couleur de {meld.Cards.Count}",
            MeldType.Set => meld.Cards.Count switch
            {
                2 => "Paire",
                3 => "Brelan",
                4 => "Carré",
                _ => $"Série de {meld.Cards.Count}"
            },
            _ => "Combinaison"
        };

        /// <summary>Petite carte 40×58 : couleur, valeur, symbole (ou joker).</summary>
        private static void MiniCard(Transform parent, CardModel card)
        {
            bool normal = card.Type == CardType.Normal;
            Color face = normal ? Constants.GetColor(card.Color) : UISkin.CardFace;
            Color ink = normal ? UISkin.InkFor(card.Color) : UISkin.AccentPale;

            Image bg = UISkin.Rect(parent, "Mini", face, 7f);
            UISkin.Size(bg.gameObject, 40f, 58f);
            UISkin.Outline(bg.rectTransform, 7f, 1f, normal ? new Color(1f, 1f, 1f, 0.2f)
                : UISkin.WithAlpha(UISkin.AccentLight, 0.5f));

            string label = card.Type switch
            {
                CardType.Normal => card.Value.ToString(),
                CardType.Wild => "W",
                CardType.WildDraw2 => "W+2",
                _ => "?"
            };
            TextMeshProUGUI value = UISkin.Text(bg.transform, "Value", label, normal ? 22f : 15f, ink,
                TextWeight.Display, TextAlignmentOptions.Center);
            UISkin.Place(value.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, normal ? 5f : 0f), new Vector2(40f, 30f));

            if (normal)
            {
                Image suit = UISkin.SuitImage(bg.transform, "Suit", card.Color, 11f, UISkin.WithAlpha(ink, 0.85f));
                UISkin.Place(suit.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 7f),
                    new Vector2(11f, 11f));
            }
        }
    }
}
