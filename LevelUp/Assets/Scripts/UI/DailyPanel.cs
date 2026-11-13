using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LevelUp.UI
{
    /// <summary>
    /// Fenêtre « Donne du jour » : résultat du jour, série en cours, les 14
    /// derniers jours d'un coup d'œil, et le partage du résultat (la donne est
    /// la même pour tout le monde : on compare entre amis).
    /// </summary>
    public class DailyPanel : MonoBehaviour
    {
        private const float Width = 760f;
        private const float Inner = Width - 88f;
        private const int Days = 14;

        private GameObject? _veil;
        private RectTransform? _panel;
        private CanvasGroup? _cg;
        private TextMeshProUGUI? _status;
        private TextMeshProUGUI? _streak;
        private TextMeshProUGUI? _playLabel;
        private GameObject? _shareButton;
        private TextMeshProUGUI? _shareLabel;
        private readonly List<(Image Dot, Image Ring, TextMeshProUGUI Label)> _dots = new();
        private Action? _onPlay;
        private bool _visible;

        public bool IsVisible => _visible;

        public void Setup(Transform parent)
        {
            UISkin.EnsureFonts();

            _veil = new GameObject("DailyVeil", typeof(RectTransform), typeof(Image), typeof(Button));
            _veil.transform.SetParent(parent, false);
            UISkin.Stretch((RectTransform)_veil.transform);
            _veil.GetComponent<Image>().color = new Color(0.04f, 0.055f, 0.07f, 0.86f);
            Button vBtn = _veil.GetComponent<Button>();
            vBtn.transition = Selectable.Transition.None;
            vBtn.onClick.AddListener(Hide);

            Image panel = UISkin.Rect(_veil.transform, "DailyPanel", UISkin.Surface, 28f, raycast: true);
            _panel = panel.rectTransform;
            UISkin.Place(_panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width, 0f));
            UISkin.Outline(_panel, 28f, 1f, new Color(1f, 1f, 1f, 0.08f))
                .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UISkin.VCol(panel.gameObject, 18f, new RectOffset(44, 44, 36, 32), TextAnchor.UpperLeft);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _cg = panel.gameObject.AddComponent<CanvasGroup>();

            TextMeshProUGUI title = UISkin.Text(panel.transform, "Title", "DONNE DU JOUR", 34f, UISkin.TextColor,
                TextWeight.Display);
            title.characterSpacing = 2f;
            UISkin.Size(title.gameObject, Inner, 44f);

            UIControls.Paragraph(panel.transform, "Intro",
                "La même partie pour tout le monde aujourd'hui : mêmes cartes, mêmes adversaires, 4 niveaux. " +
                "Seul ton premier résultat du jour compte.", Inner, 48f, 15f, UISkin.TextMuted);

            _status = UISkin.Text(panel.transform, "Status", "", 22f, UISkin.AccentPale, TextWeight.SemiBold);
            UISkin.Size(_status.gameObject, Inner, 30f);
            _streak = UISkin.Text(panel.transform, "Streak", "", 15f, UISkin.TextMuted);
            UISkin.Size(_streak.gameObject, Inner, 22f);

            // 14 derniers jours : pastille pleine = gagnée, creuse claire = perdue, vide = pas jouée.
            GameObject row = UIControls.Row(panel.transform, "Calendar", Inner, 62f, 8f);
            for (int i = 0; i < Days; i++)
            {
                GameObject cell = new($"Day{i}", typeof(RectTransform));
                cell.transform.SetParent(row.transform, false);
                UISkin.Size(cell, 40f, 62f);

                Image dot = UISkin.Rect(cell.transform, "Dot", UISkin.SurfaceRaised, 14f);
                UISkin.Place(dot.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f),
                    new Vector2(28f, 28f));
                Image ring = UISkin.Outline(dot.rectTransform, 14f, 2f, UISkin.AccentLight, "Today");
                TextMeshProUGUI label = UISkin.Text(cell.transform, "Label", "", 12f, UISkin.TextMuted,
                    TextWeight.Regular, TextAlignmentOptions.Center);
                UISkin.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f),
                    new Vector2(48f, 18f));
                _dots.Add((dot, ring, label));
            }

            GameObject footer = UIControls.Row(panel.transform, "Footer", Inner, 64f, 14f);
            footer.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            Button close = UIFactory.CreateButton(footer.transform, "BtnDailyClose", "FERMER",
                new Vector2(160f, 56f), Hide, subtle: true);
            UISkin.Size(close.gameObject, 160f, 56f);
            Button share = UIFactory.CreateButton(footer.transform, "BtnDailyShare", "PARTAGER",
                new Vector2(200f, 56f), Share);
            UISkin.Size(share.gameObject, 200f, 56f);
            _shareButton = share.gameObject;
            _shareLabel = share.GetComponentInChildren<TextMeshProUGUI>();
            Button play = UIFactory.CreateButton(footer.transform, "BtnDailyPlay", "JOUER",
                new Vector2(240f, 60f), Play, primary: true);
            UISkin.Size(play.gameObject, 240f, 60f);
            _playLabel = play.GetComponentInChildren<TextMeshProUGUI>();

            _cg.alpha = 0f;
            _veil.SetActive(false);
        }

        public void Show(Action onPlay)
        {
            if (_veil == null || _panel == null || _cg == null || _visible) return;
            _onPlay = onPlay;
            Refresh();

            _visible = true;
            _veil.SetActive(true);
            _veil.transform.SetAsLastSibling();
            _panel.localScale = Vector3.one * 0.95f;
            UITween.ScaleTo(_panel.gameObject, _panel, Vector3.one, 0.2f);
            UITween.FadeTo(_panel.gameObject, _cg, 1f, 0.16f);
            UIBackStack.Push(this, Hide);
        }

        private void Refresh()
        {
            int today = DailyDeal.TodayKey;
            int result = DailyDeal.ResultFor(today);
            int rounds = Mathf.Abs(result);

            if (_status != null)
            {
                _status.text = result == 0
                    ? $"Donne du {DailyDeal.ShortDate(today)} : à jouer"
                    : result > 0
                        ? $"Gagnée en {rounds} manche{(rounds > 1 ? "s" : "")}"
                        : $"Perdue en {rounds} manche{(rounds > 1 ? "s" : "")}";
            }
            if (_streak != null)
            {
                int streak = DailyDeal.Streak;
                _streak.text = streak > 1 ? $"Série en cours : {streak} jours d'affilée"
                    : streak == 1 ? "Série en cours : 1 jour — reviens demain pour la prolonger"
                    : "Pas de série en cours";
            }
            if (_playLabel != null) _playLabel.text = result == 0 ? "JOUER" : "REJOUER";
            if (_shareButton != null) _shareButton.SetActive(result != 0);
            if (_shareLabel != null) _shareLabel.text = "PARTAGER";

            for (int i = 0; i < _dots.Count; i++)
            {
                int day = DailyDeal.DayBefore(today, _dots.Count - 1 - i);
                int r = DailyDeal.ResultFor(day);
                (Image dot, Image ring, TextMeshProUGUI label) = _dots[i];
                dot.color = r > 0 ? UISkin.Success : r < 0 ? UISkin.WithAlpha(UISkin.TextMuted, 0.55f) : UISkin.SurfaceRaised;
                ring.enabled = day == today;
                label.text = (day % 100).ToString();
                label.color = day == today ? UISkin.TextColor : UISkin.TextMuted;
            }
        }

        private void Share()
        {
            string text = DailyDeal.ShareText(DailyDeal.TodayKey);
            if (text.Length == 0) return;
            GUIUtility.systemCopyBuffer = LevelUp.Utils.Loc.T(text);
            if (_shareLabel != null) _shareLabel.text = "COPIÉ !";
        }

        private void Play()
        {
            Action? action = _onPlay;
            Hide();
            action?.Invoke();
        }

        public void Hide()
        {
            if (!_visible || _veil == null || _panel == null || _cg == null) return;
            _visible = false;
            UIBackStack.Remove(this);
            UITween.FadeTo(_panel.gameObject, _cg, 0f, 0.14f, () =>
            {
                if (_veil != null && !_visible) _veil.SetActive(false);
            });
        }
    }
}
