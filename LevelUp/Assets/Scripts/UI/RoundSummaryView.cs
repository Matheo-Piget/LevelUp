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
    /// Récap de fin de manche, au centre du tapis : qui a gagné, et pour chaque
    /// joueur son niveau avant → après (+2 vainqueur posé, +1 contrat posé),
    /// les cartes restées en main et leurs points de pénalité.
    ///
    /// La partie attend en <see cref="GameState.EndRound"/> : la table et le
    /// dernier coup restent visibles derrière le récap. CONTINUER (ou la suite
    /// automatique après quelques secondes) lance la manche suivante — en
    /// ligne, c'est l'hôte qui la lance pour tout le monde.
    /// </summary>
    public class RoundSummaryView : MonoBehaviour
    {
        private const float Width = 880f;
        private const float AutoContinueSeconds = 12f;

        private RectTransform? _root;
        private CanvasGroup? _cg;
        private TextMeshProUGUI? _title;
        private TextMeshProUGUI? _winner;
        private TextMeshProUGUI? _caption;
        private RectTransform? _rows;
        private GameObject? _continueButton;
        private TextMeshProUGUI? _continueLabel;
        private TextMeshProUGUI? _waitingLabel;
        private IReadOnlyList<PlayerModel>? _players;

        private bool _visible;
        private bool _canContinue;
        private float _remaining;
        private int _shownSecond = -1;
        private int _round;

        /// <summary>Le joueur (ou la suite automatique) demande la manche suivante.</summary>
        public event Action<int>? ContinueRequested;

        /// <summary>« Revoir » : rejouer la manche qui vient de finir (solo).</summary>
        public event Action? ReplayRequested;

        /// <summary>Vrai si la relecture est possible (branché par le bootstrapper).</summary>
        public Func<bool>? ReplayAvailable { get; set; }

        private GameObject? _replayButton;

        public bool IsVisible => _visible;

        public void Setup(RectTransform parent)
        {
            UISkin.EnsureFonts();

            Image card = UISkin.Rect(parent, "RoundSummary", UISkin.WithAlpha(UISkin.Surface, 0.98f), 26f, raycast: true);
            _root = card.rectTransform;
            UISkin.Place(_root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f),
                new Vector2(Width, 0f));
            UISkin.Outline(_root, 26f, 1f, UISkin.WithAlpha(UISkin.AccentLight, 0.35f))
                .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UISkin.VCol(card.gameObject, 10f, new RectOffset(36, 36, 26, 24), TextAnchor.UpperLeft);
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            card.gameObject.AddComponent<UIInputBlocker>();
            _cg = card.gameObject.AddComponent<CanvasGroup>();

            float inner = Width - 72f;
            _title = UISkin.Text(card.transform, "Title", "", 12f, UISkin.TextMuted, TextWeight.SemiBold);
            _title.characterSpacing = 8f;
            UISkin.Size(_title.gameObject, inner, 18f);
            _winner = UISkin.Text(card.transform, "Winner", "", 30f, UISkin.TextColor, TextWeight.Display);
            UISkin.Size(_winner.gameObject, inner, 40f);
            _caption = UISkin.Text(card.transform, "Caption", "", 14f, UISkin.TextMuted);
            UISkin.Size(_caption.gameObject, inner, 18f);

            GameObject rows = new("Rows", typeof(RectTransform));
            rows.transform.SetParent(card.transform, false);
            _rows = (RectTransform)rows.transform;
            UISkin.VCol(rows, 6f, new RectOffset(0, 0, 6, 4), TextAnchor.UpperLeft);
            UISkin.Size(rows, inner);

            GameObject footer = UIControls.Row(card.transform, "Footer", inner, 60f, 14f);
            footer.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            _waitingLabel = UISkin.Text(footer.transform, "Waiting", "", 14f, UISkin.TextMuted,
                TextWeight.Medium, TextAlignmentOptions.MidlineLeft);
            UISkin.Size(_waitingLabel.gameObject, -1f, 40f, 1f);
            Button replay = UIFactory.CreateButton(footer.transform, "BtnReplayRound", "REVOIR",
                new Vector2(150f, 52f), () => ReplayRequested?.Invoke(), subtle: true);
            UISkin.Size(replay.gameObject, 150f, 52f);
            _replayButton = replay.gameObject;
            Button cont = UIFactory.CreateButton(footer.transform, "BtnContinue", "CONTINUER",
                new Vector2(260f, 60f), RequestContinue, primary: true);
            UISkin.Size(cont.gameObject, 260f, 60f);
            _continueButton = cont.gameObject;
            _continueLabel = cont.GetComponentInChildren<TextMeshProUGUI>();

            card.gameObject.SetActive(false);
        }

        /// <summary>Joueurs de la partie (même liste que le GameManager).</summary>
        public void Bind(IReadOnlyList<PlayerModel> players) => _players = players;

        private void OnEnable()
        {
            EventBus.Subscribe<RoundSummaryEvent>(OnRoundSummary);
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RoundSummaryEvent>(OnRoundSummary);
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnRoundSummary(RoundSummaryEvent evt)
        {
            if (evt.GameOver || !evt.AwaitingNextRound) return; // l'écran de fin prend le relais
            Show(evt);
        }

        private void OnRoundStarted(RoundStartedEvent evt) => HideNow();
        private void OnGameOver(GameOverEvent evt) => HideNow();
        private void OnGameStarted(GameStartedEvent evt) => HideNow();

        private void Update()
        {
            // Suite automatique en ligne seulement (l'hôte ne doit pas bloquer la
            // table) ; en solo, le joueur lit le récap aussi longtemps qu'il veut.
            if (!_visible || !_canContinue || !GameSession.IsOnline) return;
            _remaining -= Time.deltaTime;
            int second = Mathf.CeilToInt(Mathf.Max(0f, _remaining));
            if (second != _shownSecond && _waitingLabel != null)
            {
                _shownSecond = second;
                _waitingLabel.text = $"Suite automatique dans {second} s";
            }
            if (_remaining <= 0f) RequestContinue();
        }

        private void RequestContinue()
        {
            if (!_visible || !_canContinue) return;
            _canContinue = false;
            ContinueRequested?.Invoke(_round);
        }

        /// <summary>
        /// Aperçu sur l'état courant (outil de capture DevAutoScreenshot) : le
        /// joueur <paramref name="winnerIndex"/> gagne +2, un joueur sur deux a posé.
        /// </summary>
        public void ShowSnapshot(int winnerIndex, int round)
        {
            if (_players == null) return;
            int n = _players.Count;
            RoundSummaryEvent evt = new()
            {
                RoundNumber = round,
                WinnerIndex = winnerIndex,
                AwaitingNextRound = true,
                LevelsBefore = new int[n],
                LevelsAfter = new int[n],
                LaidDown = new bool[n],
                CardsLeft = new int[n],
                RoundPenalty = new int[n]
            };
            for (int i = 0; i < n; i++)
            {
                PlayerModel p = _players[i];
                bool winner = i == winnerIndex;
                bool laid = winner || i % 2 == 0;
                evt.LevelsBefore[i] = p.CurrentLevel;
                evt.LevelsAfter[i] = p.CurrentLevel + (winner ? 2 : laid ? 1 : 0);
                evt.LaidDown[i] = laid;
                evt.CardsLeft[i] = winner ? 0 : p.Hand.Count;
                evt.RoundPenalty[i] = winner ? 0 : p.HandPenalty();
            }
            Show(evt);
        }

        private void Show(RoundSummaryEvent evt)
        {
            if (_root == null || _cg == null || _rows == null || _players == null) return;
            _round = evt.RoundNumber;

            int local = LocalIndex();
            string winnerName = Name(evt.WinnerIndex);
            if (_title != null) _title.text = $"MANCHE {evt.RoundNumber} TERMINÉE";
            if (_winner != null)
            {
                _winner.text = evt.WinnerIndex == local
                    ? "Tu remportes la manche !"
                    : $"{winnerName} remporte la manche";
            }
            if (_caption != null)
            {
                _caption.text = evt.TurnLimitReached
                    ? "Limite de tours atteinte : seuls ceux qui ont posé montent d'un niveau."
                    : evt.DeckExhausted
                    ? "Pioche épuisée : la main la plus courte l'emporte."
                    : "Cartes restées en main : 5 pts (1-9), 10 pts (10-18), 25 pts (joker).";
            }

            for (int i = _rows.childCount - 1; i >= 0; i--) Destroy(_rows.GetChild(i).gameObject);
            int count = Mathf.Min(_players.Count, evt.LevelsBefore?.Length ?? 0);
            for (int i = 0; i < count; i++) BuildRow(i, local, evt);

            // Solo et hôte : bouton + suite automatique. Client en ligne : l'hôte décide.
            _canContinue = !GameSession.IsOnline || GameSession.IsHost;
            _remaining = AutoContinueSeconds;
            _shownSecond = -1;
            if (_continueButton != null) _continueButton.SetActive(_canContinue);
            if (_replayButton != null) _replayButton.SetActive(ReplayAvailable != null && ReplayAvailable());
            if (_waitingLabel != null)
            {
                _waitingLabel.text = _canContinue ? "" : "L'hôte lance la manche suivante…";
            }
            if (_continueLabel != null) _continueLabel.text = $"MANCHE {evt.RoundNumber + 1}";

            _visible = true;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _cg.blocksRaycasts = true;
            _root.localScale = Vector3.one * 0.94f;
            _cg.alpha = 0f;
            UITween.ScaleTo(_root.gameObject, _root, Vector3.one, 0.25f);
            UITween.FadeTo(_root.gameObject, _cg, 1f, 0.2f);
        }

        private void BuildRow(int index, int local, RoundSummaryEvent evt)
        {
            PlayerModel p = _players![index];
            int before = evt.LevelsBefore[index];
            int after = evt.LevelsAfter[index];
            int delta = after - before;
            bool isWinner = index == evt.WinnerIndex;
            float inner = Width - 72f;

            Image row = UISkin.Rect(_rows!, $"Row{index}",
                isWinner ? UISkin.WithAlpha(UISkin.Accent, 0.09f) : UISkin.Glass, 14f);
            UISkin.Size(row.gameObject, inner, 50f);
            UISkin.HRow(row.gameObject, 12f, new RectOffset(8, 14, 6, 6), TextAnchor.MiddleLeft);

            Color tint = UISkin.AvatarTints[index % UISkin.AvatarTints.Length];
            Image avatar = UISkin.Rect(row.transform, "Avatar", tint, 10f);
            UISkin.Size(avatar.gameObject, 36f, 36f);
            TextMeshProUGUI initial = UISkin.Text(avatar.transform, "Initial", PlayerProfile.Initial(p.Name), 15f,
                UISkin.Base, TextWeight.Display, TextAlignmentOptions.Center);
            UISkin.Stretch(initial.rectTransform);

            string name = index == local ? $"{p.Name} <color=#A7A198>(toi)</color>" : p.Name;
            TextMeshProUGUI nameText = UISkin.Text(row.transform, "Name", name, 16f, UISkin.TextColor,
                TextWeight.SemiBold);
            nameText.richText = true;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            UISkin.Size(nameText.gameObject, 170f);

            int cap = GameSession.Rules.LevelsToWin;
            string levels = delta > 0
                ? $"Niv. {Mathf.Min(before, cap)} → {Mathf.Min(after, cap)}"
                : $"Niv. {Mathf.Min(before, cap)}";
            TextMeshProUGUI levelText = UISkin.Text(row.transform, "Levels", levels, 15f, UISkin.TextMuted,
                TextWeight.Medium);
            UISkin.Size(levelText.gameObject, 112f);

            int left = evt.CardsLeft != null && index < evt.CardsLeft.Length ? evt.CardsLeft[index] : 0;
            int penalty = evt.RoundPenalty != null && index < evt.RoundPenalty.Length ? evt.RoundPenalty[index] : 0;

            // Mains révélées : les cartes restées (la partie attend en EndRound,
            // les mains du modèle sont encore intactes).
            GameObject chips = new("Hand", typeof(RectTransform));
            chips.transform.SetParent(row.transform, false);
            UISkin.HRow(chips, 2f, null, TextAnchor.MiddleLeft);
            UISkin.Size(chips, -1f, 30f, 1f);
            if (left == 0)
            {
                UISkin.Text(chips.transform, "Empty", "main vide", 14f, UISkin.Success, TextWeight.Medium);
            }
            else
            {
                IReadOnlyList<CardModel> hand = p.Hand;
                int shown = Mathf.Min(hand.Count, MaxChips);
                for (int c = 0; c < shown; c++) BuildChip(chips.transform, hand[c]);
                if (hand.Count > shown)
                {
                    UISkin.Text(chips.transform, "More", $"+{hand.Count - shown}", 12f, UISkin.TextMuted,
                        TextWeight.SemiBold);
                }
            }

            TextMeshProUGUI pts = UISkin.Text(row.transform, "Penalty", left == 0 ? "" : $"+{penalty} pts", 14f,
                UISkin.AccentLight, TextWeight.SemiBold, TextAlignmentOptions.MidlineRight);
            UISkin.Size(pts.gameObject, 66f);

            bool laid = evt.LaidDown != null && index < evt.LaidDown.Length && evt.LaidDown[index];
            string badge = delta > 0 ? $"+{delta}" : laid ? "posé" : "—";
            Color bg = delta >= 2 ? UISkin.Accent : delta == 1 ? UISkin.WithAlpha(UISkin.Success, 0.22f)
                : new Color(0f, 0f, 0f, 0f);
            Color ink = delta >= 2 ? UISkin.OnAccent : delta == 1 ? UISkin.Hex(0xBFE6CF) : UISkin.TextMuted;
            Image pill = UISkin.Rect(row.transform, "Delta", bg, 10f);
            UISkin.Size(pill.gameObject, 58f, 28f);
            TextMeshProUGUI t = UISkin.Text(pill.transform, "Text", badge, 14f, ink, TextWeight.Bold,
                TextAlignmentOptions.Center);
            UISkin.Stretch(t.rectTransform);
        }

        private const int MaxChips = 12;

        /// <summary>Mini-carte : fond de la couleur, valeur à l'encre lisible ; joker / action en neutre.</summary>
        private static void BuildChip(Transform parent, CardModel card)
        {
            bool action = card.IsAction;
            Color face = action ? UISkin.CardFace : Constants.GetColor(card.Color);
            Color ink = action ? UISkin.AccentPale : UISkin.InkFor(card.Color);
            Image chip = UISkin.Rect(parent, "Chip", face, 4f);
            UISkin.Size(chip.gameObject, 19f, 27f);
            string label = card.Type switch
            {
                CardType.Normal => card.Value.ToString(),
                CardType.Skip => "P",
                CardType.Draw2 => "+2",
                _ => "J"
            };
            TextMeshProUGUI t = UISkin.Text(chip.transform, "V", label, card.Value >= 10 || card.Type == CardType.Draw2 ? 10f : 12f,
                ink, TextWeight.Bold, TextAlignmentOptions.Center);
            UISkin.Stretch(t.rectTransform);
        }

        private void HideNow()
        {
            _visible = false;
            _canContinue = false;
            if (_root == null || _cg == null || !_root.gameObject.activeSelf) return;
            _cg.blocksRaycasts = false;
            UITween.FadeTo(_root.gameObject, _cg, 0f, 0.2f, () =>
            {
                if (_root != null && !_visible) _root.gameObject.SetActive(false);
            });
        }

        private int LocalIndex()
        {
            if (_players == null) return -1;
            foreach (PlayerModel p in _players)
            {
                if (GameSession.IsLocalHuman(p)) return p.Index;
            }
            return -1;
        }

        private string Name(int index) =>
            _players != null && index >= 0 && index < _players.Count ? _players[index].Name : "Un joueur";
    }
}
