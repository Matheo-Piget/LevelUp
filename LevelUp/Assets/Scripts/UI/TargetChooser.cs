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
    /// Choix de la cible d'une carte « Passe » ou « +2 » défaussée (cartes
    /// spéciales activées, 3 joueurs ou plus — à deux, la cible est évidente).
    /// Chaque adversaire est présenté avec son niveau et ses cartes en main.
    /// </summary>
    public class TargetChooser : MonoBehaviour
    {
        /// <summary>Vrai tant que le panneau est affiché (l'input de jeu est suspendu).</summary>
        public static bool IsOpen { get; private set; }

        private const float Inner = 560f;

        private RectTransform? _root;
        private CanvasGroup? _cg;
        private TextMeshProUGUI? _title;
        private TextMeshProUGUI? _subtitle;
        private RectTransform? _rows;
        private Action<int>? _onPick;
        private CardType _type;

        public void Setup(Canvas canvas)
        {
            UISkin.EnsureFonts();

            GameObject rootObj = new("TargetChooser", typeof(RectTransform), typeof(CanvasGroup));
            rootObj.transform.SetParent(canvas.transform, false);
            _root = (RectTransform)rootObj.transform;
            UISkin.Stretch(_root);
            _cg = rootObj.GetComponent<CanvasGroup>();

            GameObject veil = new("Veil", typeof(RectTransform), typeof(Image), typeof(Button));
            veil.transform.SetParent(_root, false);
            UISkin.Stretch((RectTransform)veil.transform);
            veil.GetComponent<Image>().color = UISkin.WithAlpha(UISkin.Ground, 0.78f);
            Button veilBtn = veil.GetComponent<Button>();
            veilBtn.transition = Selectable.Transition.None;
            veilBtn.onClick.AddListener(Hide);

            Image panel = UISkin.Rect(_root, "Panel", UISkin.Surface, 28f, raycast: true);
            RectTransform prt = panel.rectTransform;
            UISkin.Place(prt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f),
                new Vector2(Inner + 80f, 0f));
            UISkin.Outline(prt, 28f, 1f, UISkin.Line).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UISkin.VCol(panel.gameObject, 14f, new RectOffset(40, 40, 32, 28), TextAnchor.UpperLeft);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _title = UISkin.Text(panel.transform, "Title", "", 30f, UISkin.TextColor, TextWeight.Display);
            UISkin.Size(_title.gameObject, Inner, 38f);
            _subtitle = UISkin.Text(panel.transform, "Subtitle", "", 15f, UISkin.TextMuted);
            UISkin.Size(_subtitle.gameObject, Inner, 20f);

            GameObject rows = new("Rows", typeof(RectTransform));
            rows.transform.SetParent(panel.transform, false);
            _rows = (RectTransform)rows.transform;
            UISkin.VCol(rows, 10f, new RectOffset(0, 0, 6, 6), TextAnchor.UpperLeft);
            UISkin.Size(rows, Inner);

            Button cancel = UIFactory.CreateButton(panel.transform, "BtnCancelTarget", "ANNULER",
                new Vector2(200f, 52f), Hide, subtle: true);
            UISkin.Size(cancel.gameObject, 200f, 52f);

            rootObj.SetActive(false);
        }

        /// <summary>Affiche les adversaires ; <paramref name="onPick"/> reçoit l'index choisi.</summary>
        public void Show(CardType type, IReadOnlyList<PlayerModel> players, int selfIndex, Action<int> onPick)
        {
            if (_root == null || _rows == null || _cg == null) return;
            _onPick = onPick;
            _type = type;

            bool skip = type == CardType.Skip;
            if (_title != null) _title.text = skip ? "Qui passe son tour ?" : "Qui pioche 2 cartes ?";
            if (_subtitle != null)
            {
                _subtitle.text = skip
                    ? "Ta carte « Passe » fait sauter le prochain tour de ce joueur."
                    : "Ta carte « +2 » lui fait piocher deux cartes tout de suite.";
            }

            for (int i = _rows.childCount - 1; i >= 0; i--) Destroy(_rows.GetChild(i).gameObject);
            foreach (PlayerModel p in players)
            {
                if (p.Index == selfIndex) continue;
                BuildRow(p);
            }

            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            IsOpen = true;
            UIBackStack.Push(this, Hide);
            _cg.alpha = 0f;
            UITween.FadeTo(_root.gameObject, _cg, 1f, 0.14f);
        }

        public void Hide()
        {
            if (_root == null || _cg == null || !IsOpen) return;
            IsOpen = false;
            UIBackStack.Remove(this);
            _onPick = null;
            UITween.FadeTo(_root.gameObject, _cg, 0f, 0.12f, () =>
            {
                if (_root != null && !IsOpen) _root.gameObject.SetActive(false);
            });
        }

        private void OnDisable() => IsOpen = false;

        private void BuildRow(PlayerModel p)
        {
            Image row = UISkin.Rect(_rows!, $"Target{p.Index}", UISkin.Base, 18f, raycast: true);
            UISkin.Size(row.gameObject, Inner, 68f);
            UISkin.HRow(row.gameObject, 14f, new RectOffset(12, 18, 10, 10), TextAnchor.MiddleLeft);
            UISkin.Outline(row.rectTransform, 18f, 1f, UISkin.Line).gameObject.AddComponent<LayoutElement>()
                .ignoreLayout = true;

            Color tint = UISkin.AvatarTints[p.Index % UISkin.AvatarTints.Length];
            Image avatar = UISkin.Rect(row.transform, "Avatar", tint, 13f);
            UISkin.Size(avatar.gameObject, 44f, 44f);
            TextMeshProUGUI initial = UISkin.Text(avatar.transform, "Initial", PlayerProfile.Initial(p.Name), 18f,
                UISkin.Base, TextWeight.Display, TextAlignmentOptions.Center);
            UISkin.Stretch(initial.rectTransform);

            GameObject texts = new("Texts", typeof(RectTransform));
            texts.transform.SetParent(row.transform, false);
            UISkin.VCol(texts, 2f, null, TextAnchor.MiddleLeft);
            UISkin.Size(texts, -1f, -1f, 1f);
            UISkin.Text(texts.transform, "Name", p.Name, 17f, UISkin.TextColor, TextWeight.SemiBold);
            string detail = $"Niveau {p.CurrentLevel} · {p.Hand.Count} carte{(p.Hand.Count > 1 ? "s" : "")}";
            if (p.HasLaidDownThisRound) detail += " · contrat posé";
            if (p.SkipCount > 0) detail += " · passe déjà son tour";
            // Deux cartes de plus peuvent compléter le contrat d'un joueur qui n'a pas posé.
            if (_type == CardType.Draw2 && !p.HasLaidDownThisRound) detail += " · peut l'aider à poser";
            UISkin.Text(texts.transform, "Detail", detail, 13f,
                p.HasLaidDownThisRound && p.Hand.Count <= 3 ? Constants.CardOrange : UISkin.TextMuted);

            Button btn = row.gameObject.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;
            btn.targetGraphic = row;
            int index = p.Index;
            btn.onClick.AddListener(() =>
            {
                Action<int>? pick = _onPick;
                Hide();
                pick?.Invoke(index);
            });
        }
    }
}
