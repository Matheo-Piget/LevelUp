using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;

namespace LevelUp.UI
{
    /// <summary>
    /// "Nouvelle partie" : réglages de la table (joueurs, bots, minuteur) et des
    /// règles (contrats, cartes spéciales, main de départ, durée), mémorisés
    /// dans <see cref="GameSettings"/>. Deux usages :
    ///   - solo : bouton LANCER LA PARTIE ;
    ///   - hôte en ligne : bouton VALIDER (les réglages partent à tout le salon).
    /// Cartes spéciales désactivées par défaut : c'est au joueur de les mettre en jeu.
    /// </summary>
    public class MatchSetupPanel : MonoBehaviour
    {
        private const float PanelWidth = 1080f;
        private const float ColumnWidth = 476f;
        private const float LabelWidth = 150f;
        private const float ControlWidth = ColumnWidth - LabelWidth - 16f;

        private GameObject? _veil;
        private RectTransform? _panel;
        private CanvasGroup? _cg;
        private TextMeshProUGUI? _title;
        private TextMeshProUGUI? _subtitle;
        private TextMeshProUGUI? _playersCaption;
        private TextMeshProUGUI? _specialCaption;
        private TextMeshProUGUI? _primaryLabel;
        private GameObject? _cancelButton;
        private readonly List<Action> _refreshers = new();
        private Action? _onConfirm;
        private bool _applyOnClose;
        private bool _isVisible;

        public bool IsVisible => _isVisible;

        public void Setup(RectTransform parent)
        {
            GameSettings.Initialize();
            UISkin.EnsureFonts();

            _veil = new GameObject("MatchSetupVeil", typeof(RectTransform), typeof(Image), typeof(Button));
            _veil.transform.SetParent(parent, false);
            UISkin.Stretch((RectTransform)_veil.transform);
            _veil.GetComponent<Image>().color = new Color(0.04f, 0.055f, 0.07f, 0.86f);
            Button vBtn = _veil.GetComponent<Button>();
            vBtn.transition = Selectable.Transition.None;
            vBtn.onClick.AddListener(Hide);

            Image panel = UISkin.Rect(_veil.transform, "MatchSetupPanel", UISkin.Surface, 28f, raycast: true);
            _panel = panel.rectTransform;
            UISkin.Place(_panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(PanelWidth, 0f));
            UISkin.Outline(_panel, 28f, 1f, new Color(1f, 1f, 1f, 0.08f))
                .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UISkin.VCol(panel.gameObject, 24f, new RectOffset(44, 44, 38, 36), TextAnchor.UpperLeft);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _cg = panel.gameObject.AddComponent<CanvasGroup>();

            float inner = PanelWidth - 88f;

            // En-tête : titre + sous-titre | fermer
            GameObject header = UIControls.Row(panel.transform, "Header", inner, 64f);
            GameObject titles = UIControls.Column(header.transform, "Titles", inner - 64f, 4f);
            _title = UISkin.Text(titles.transform, "Title", "NOUVELLE PARTIE", 34f, UISkin.TextColor,
                TextWeight.Display);
            _title.characterSpacing = 2f;
            _subtitle = UISkin.Text(titles.transform, "Subtitle", "", 15f, UISkin.TextMuted);
            GameScreenLayout.IconButton(header.transform, "BtnX", UIIcon.Close, 20f, "Fermer", Hide);

            // Deux colonnes : la table | les règles
            GameObject columns = UIControls.Row(panel.transform, "Columns", inner, -1f, 40f);
            HorizontalLayoutGroup ch = columns.GetComponent<HorizontalLayoutGroup>();
            ch.childAlignment = TextAnchor.UpperLeft;
            LayoutElement cle = columns.GetComponent<LayoutElement>();
            cle.minHeight = -1f;
            cle.preferredHeight = -1f;

            GameObject left = UIControls.Column(columns.transform, "Table", ColumnWidth, 14f);
            UIControls.SectionLabel(left.transform, "LA TABLE", ColumnWidth);
            Choice(left.transform, "Joueurs", new[] { "2", "3", "4" },
                () => GameSettings.PlayerCount - 2, v => GameSettings.PlayerCount = v + 2);
            Choice(left.transform, "Bots", new[] { "FACILE", "NORMAL", "EXPERT" },
                () => GameSettings.Difficulty, v => GameSettings.Difficulty = v);
            Choice(left.transform, "Minuteur", new[] { "AUCUN", "30 S", "60 S" },
                () => IndexOf(GameSettings.TurnTimerOptions, GameSettings.TurnTimer),
                v => GameSettings.TurnTimer = GameSettings.TurnTimerOptions[v]);
            _playersCaption = UIControls.Paragraph(left.transform, "PlayersCaption", "", ColumnWidth, 40f, 13f);

            GameObject right = UIControls.Column(columns.transform, "Rules", ColumnWidth, 14f);
            UIControls.SectionLabel(right.transform, "LES RÈGLES", ColumnWidth);
            Choice(right.transform, "Contrats", new[] { "FIXES", "VARIÉS" },
                () => GameSettings.RandomContracts ? 1 : 0, v => GameSettings.RandomContracts = v == 1);
            Choice(right.transform, "Cartes spéciales", new[] { "AUCUNE", "JOKERS", "TOUTES" },
                () => GameSettings.SpecialCards, v =>
                {
                    GameSettings.SpecialCards = v;
                    RefreshCaptions();
                });
            Choice(right.transform, "Main de départ", new[] { "8", "10", "12" },
                () => IndexOf(GameSettings.HandSizeOptions, GameSettings.HandSize),
                v =>
                {
                    GameSettings.HandSize = GameSettings.HandSizeOptions[v];
                    RefreshCaptions();
                });
            Choice(right.transform, "Niveaux", new[] { "4", "6", "8" },
                () => IndexOf(GameSettings.LevelsToWinOptions, GameSettings.LevelsToWin),
                v =>
                {
                    GameSettings.LevelsToWin = GameSettings.LevelsToWinOptions[v];
                    RefreshCaptions();
                });

            // Variantes (coupées par défaut : jeu classique), une pastille chacune.
            GameObject variants = UIControls.Row(panel.transform, "Variants", inner, 44f, 10f);
            UIControls.RowLabel(variants.transform, "Variantes", 110f);
            VariantChip(variants.transform, "VarBonus", "Sortie : +1 niveau",
                () => GameSettings.VariantSingleBonus, v => GameSettings.VariantSingleBonus = v);
            VariantChip(variants.transform, "VarFinal", "Dernier niveau obligatoire",
                () => GameSettings.VariantFinalLevel, v => GameSettings.VariantFinalLevel = v);
            VariantChip(variants.transform, "VarLimit", $"{GameSettings.TurnLimitValue} tours max",
                () => GameSettings.VariantTurnLimit, v => GameSettings.VariantTurnLimit = v);
            VariantChip(variants.transform, "VarCatchUp", "Coup de pouce",
                () => GameSettings.VariantCatchUp, v => GameSettings.VariantCatchUp = v);

            // Explication des cartes spéciales, sur toute la largeur
            Image note = UISkin.Rect(panel.transform, "SpecialNote", UISkin.Base, 16f);
            UISkin.Size(note.gameObject, inner, 76f);
            UISkin.HRow(note.gameObject, 14f, new RectOffset(18, 18, 10, 10), TextAnchor.MiddleLeft);
            UISkin.Outline(note.rectTransform, 16f, 1f, UISkin.Line).gameObject.AddComponent<LayoutElement>()
                .ignoreLayout = true;
            Image badge = UISkin.Rect(note.transform, "Badge", UISkin.WithAlpha(UISkin.Accent, 0.16f), 10f);
            UISkin.Size(badge.gameObject, 36f, 36f);
            TextMeshProUGUI j = UISkin.Text(badge.transform, "J", "J", 20f, UISkin.AccentPale, TextWeight.Display,
                TextAlignmentOptions.Center);
            UISkin.Stretch(j.rectTransform);
            _specialCaption = UIControls.Paragraph(note.transform, "Caption", "", inner - 36f - 14f - 36f, 58f, 13f,
                UISkin.TextMuted, TextAlignmentOptions.MidlineLeft);

            // Pied : annuler | action principale
            GameObject footer = UIControls.Row(panel.transform, "Footer", inner, 64f);
            footer.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            Button cancel = UIFactory.CreateButton(footer.transform, "BtnCancel", "ANNULER",
                new Vector2(220f, 56f), Hide, subtle: true);
            UISkin.Size(cancel.gameObject, 220f, 56f);
            _cancelButton = cancel.gameObject;
            Button primary = UIFactory.CreateButton(footer.transform, "BtnLaunch", "LANCER LA PARTIE",
                new Vector2(380f, 64f), Confirm, primary: true);
            UISkin.Size(primary.gameObject, 380f, 64f);
            _primaryLabel = primary.GetComponentInChildren<TextMeshProUGUI>();

            _cg.alpha = 0f;
            _veil.SetActive(false);
        }

        private void Choice(Transform parent, string label, string[] options, Func<int> get, Action<int> set)
        {
            GameObject row = UIControls.Row(parent, label + "Row", ColumnWidth, 52f);
            UIControls.RowLabel(row.transform, label, LabelWidth);
            _refreshers.Add(UIControls.Segmented(row.transform, "Segmented", options, ControlWidth, get, set));
        }

        /// <summary>Pastille à bascule : pleine (accent) quand la variante est active.</summary>
        private void VariantChip(Transform parent, string name, string label, Func<bool> get, Action<bool> set)
        {
            Image chip = UISkin.Rect(parent, name, UISkin.Base, 20f, raycast: true);
            UISkin.HRow(chip.gameObject, 0f, new RectOffset(16, 16, 0, 0), TextAnchor.MiddleCenter);
            UISkin.Size(chip.gameObject, -1f, 40f);
            Image ring = UISkin.Outline(chip.rectTransform, 20f, 1f, UISkin.LineStrong);
            ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            TextMeshProUGUI text = UISkin.Text(chip.transform, "Label", label, 14f, UISkin.TextMuted,
                TextWeight.SemiBold, TextAlignmentOptions.Center);

            void Refresh()
            {
                bool on = get();
                chip.color = on ? UISkin.Accent : UISkin.Base;
                text.color = on ? UISkin.OnAccent : UISkin.TextMuted;
                ring.enabled = !on;
            }

            Button button = chip.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                set(!get());
                Refresh();
                RefreshCaptions();
                AudioManager.Instance?.PlayClick();
            });
            _refreshers.Add(Refresh);
            Refresh();
        }

        private static int IndexOf(int[] options, int value)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] == value) return i;
            }
            // Valeur hors liste (réglage d'une ancienne version) : la plus proche.
            int best = 0;
            for (int i = 1; i < options.Length; i++)
            {
                if (Mathf.Abs(options[i] - value) < Mathf.Abs(options[best] - value)) best = i;
            }
            return best;
        }

        private string _playersCaptionBase = "";

        /// <summary>
        /// Durée indicative d'une partie. Base mesurée (BotSimulationTests.GameLength_report,
        /// 4 bots Normal, règles classiques) : 45 / 73 / 116 tours pour 4 / 6 / 8 niveaux.
        /// Hypothèse : ~5 s par tour en moyenne (humain + bots), plus le récap de
        /// chaque manche — un ordre de grandeur, pas une promesse.
        /// </summary>
        private static int EstimatedMinutes(int levels)
        {
            float turns = levels <= 4 ? 45f : levels <= 6 ? 73f : 116f;
            float rounds = levels <= 4 ? 3f : levels <= 6 ? 4.5f : 6f;
            float seconds = turns * 5f + rounds * 15f;
            return Mathf.Max(5, Mathf.RoundToInt(seconds / 60f / 5f) * 5);
        }

        private void RefreshCaptions()
        {
            if (_playersCaption != null)
            {
                _playersCaption.text = _playersCaptionBase
                    + $"\nPartie en {GameSettings.LevelsToWin} niveaux : environ {EstimatedMinutes(GameSettings.LevelsToWin)} min.";
            }
            if (_specialCaption != null)
            {
                string text = MatchRulesText.SpecialCardsCaption((SpecialCardsMode)GameSettings.SpecialCards);
                // Petite main + longue partie : les derniers contrats (suite de 8,
                // couleur de 7) deviennent presque impossibles.
                if (GameSettings.HandSize <= 8 && GameSettings.LevelsToWin >= 8)
                {
                    text += "\n<color=#E6CB92>Main de 8 : les contrats des niveaux 7 et 8 seront très durs.</color>";
                }
                string variants = MatchRulesText.VariantsCaption(GameSettings.BuildMatchRules());
                if (variants.Length > 0) text += "\n<color=#E6CB92>" + variants + "</color>";
                _specialCaption.richText = true;
                _specialCaption.text = text;
            }
        }

        // ─── Affichage ──────────────────────────────────────────────────

        /// <summary>Solo : réglages puis lancement (fermer = ne pas lancer).</summary>
        public void ShowSolo(Action onLaunch)
        {
            Open("NOUVELLE PARTIE", "Contre des bots · tes réglages sont gardés pour la prochaine fois",
                "LANCER LA PARTIE", "Les places sont complétées par des bots.", onLaunch, applyOnClose: false);
        }

        /// <summary>
        /// Hôte en ligne : réglages appliqués à tout le salon. Chaque choix est
        /// enregistré aussitôt : fermer le panneau les diffuse aussi (sinon le
        /// salon afficherait d'autres règles que celles du lancement).
        /// </summary>
        public void ShowOnlineHost(Action onApply)
        {
            Open("PARAMÈTRES DE PARTIE", "Appliqués à tous les joueurs du salon",
                "VALIDER", "Places totales : les places libres sont jouées par des bots.", onApply,
                applyOnClose: true);
        }

        private void Open(string title, string subtitle, string primary, string playersCaption, Action onConfirm,
            bool applyOnClose)
        {
            if (_veil == null || _panel == null || _cg == null) return;
            _onConfirm = onConfirm;
            _applyOnClose = applyOnClose;
            if (_cancelButton != null) _cancelButton.SetActive(!applyOnClose);
            if (_title != null) _title.text = title;
            if (_subtitle != null) _subtitle.text = subtitle;
            if (_primaryLabel != null) _primaryLabel.text = primary;
            _playersCaptionBase = playersCaption;
            foreach (Action refresh in _refreshers) refresh();
            RefreshCaptions();

            if (_isVisible) return;
            _isVisible = true;
            UIBackStack.Push(this, Hide);
            _veil.SetActive(true);
            _veil.transform.SetAsLastSibling();
            _panel.localScale = Vector3.one * 0.94f;
            _cg.alpha = 0f;
            UITween.ScaleTo(_panel.gameObject, _panel, Vector3.one, 0.22f);
            UITween.FadeTo(_panel.gameObject, _cg, 1f, 0.18f);
        }

        private void Confirm()
        {
            Action? confirm = _onConfirm;
            _onConfirm = null;
            Hide();
            confirm?.Invoke();
        }

        public void Hide()
        {
            if (_veil == null || _panel == null || _cg == null || !_isVisible) return;
            _isVisible = false;
            UIBackStack.Remove(this);
            GameSettings.Flush();

            if (_applyOnClose && _onConfirm != null)
            {
                Action apply = _onConfirm;
                _onConfirm = null;
                apply();
            }
            UITween.ScaleTo(_panel.gameObject, _panel, Vector3.one * 0.96f, 0.16f);
            UITween.FadeTo(_panel.gameObject, _cg, 0f, 0.16f, () =>
            {
                if (_veil != null && !_isVisible) _veil.SetActive(false);
            });
        }
    }
}
