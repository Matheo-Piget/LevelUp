using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Met en page l'écran de jeu selon la maquette "Écran de jeu" (1920×1080) :
    ///   top bar (logo, manche, niveau 8 segments, réglages, pause)
    ///   rangée des joueurs · tapis (pioche + défausses | combinaisons posées)
    ///   étapes du tour · main en éventail · contrat (bas-gauche) · tri (bas-droite)
    /// Réutilise les objets posés en scène (aucune logique de jeu ici) : ils sont
    /// seulement replacés/restylés, et l'UI ajoutée observe le Core via EventBus.
    /// </summary>
    public class GameScreenLayout : MonoBehaviour
    {
        // Géométrie de la maquette (unités canvas)
        private const float Margin = 40f;
        private const float TopBarHeight = 80f;
        private const float PlayersTop = 100f;
        private const float FeltTop = 184f;
        private const float FeltBottom = 304f;
        private const float StepperBottom = 252f;
        private const float FeltLeftZone = 824f;

        private GameManager? _gameManager;
        private RectTransform? _root;

        // Top bar
        private TextMeshProUGUI? _roundText;
        private GameObject? _rulesPill;
        private TextMeshProUGUI? _rulesText;
        private readonly List<Image> _levelSegments = new();
        private TextMeshProUGUI? _levelValue;
        private Button? _pauseButton;
        private Button? _settingsButton;
        private PauseMenuController? _pauseMenu;
        private GameInputController? _input;
        private GameObject? _prepareButton;

        // Pioche
        private TextMeshProUGUI? _deckCaption;

        // Étapes
        private readonly List<StepPill> _steps = new();
        private Image? _skipOutline;

        // Contrat
        private TextMeshProUGUI? _contractTitle;
        private RectTransform? _contractRows;
        private readonly List<ContractRow> _contractRowPool = new();

        /// <summary>Racine calée sur la safe area (pour y poser d'autres vues de jeu).</summary>
        public RectTransform? Root => _root;

        // Tapis (teinte au choix), invitation sur la pioche, consigne d'étape, toast.
        private Canvas? _canvas;
        private Image? _felt;
        private Image? _feltLight;
        private Image? _deckRing;
        private TextMeshProUGUI? _phaseHint;
        private Image? _toastEdge;
        private HUDView? _hud;
        private TurnPhase _phase = TurnPhase.Draw;
        private bool _localTurn;
        private float _pulse;

        /// <summary>Construit la mise en page. À appeler après SafeAreaFitter.Wrap.</summary>
        public void Build(Canvas canvas, RectTransform safeRoot, GameManager? gameManager)
        {
            _gameManager = gameManager;
            _root = safeRoot;
            _canvas = canvas;
            UISkin.EnsureFonts();

            ConfigureScaler(canvas);
            _hud = safeRoot.GetComponentInChildren<HUDView>(true);
            if (_hud != null) _hud.StatusStyleChanged += OnStatusStyle;

            Image felt = BuildFelt(safeRoot);
            _felt = felt;
            _feltLight = felt.transform.Find("Light")?.GetComponent<Image>();
            ApplyFelt();
            BuildTopBar(safeRoot);
            PlacePlayersRow(safeRoot);
            PlaceTableAndPiles(safeRoot, felt.rectTransform);
            PlaceHand(safeRoot);
            BuildStepper(safeRoot);
            BuildContractCard(safeRoot);
            BuildSortControl(safeRoot);
            RestyleHud(safeRoot);

            // Ordre de rendu : la main passe devant les étapes / le tapis, et
            // le toast de statut devant tout.
            Find(safeRoot, "PlayerHand")?.SetAsLastSibling();
            Find(safeRoot, "StatusText")?.SetAsLastSibling();

            // Les textes posés en scène passent sur Inter.
            UISkin.ApplyFontsRecursive(safeRoot);

            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<TurnPhaseChangedEvent>(OnPhaseChanged);
            EventBus.Subscribe<DeckChangedEvent>(OnDeckChanged);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Subscribe<LevelLaidDownEvent>(OnLevelLaidDown);
            EventBus.Subscribe<HandChangedEvent>(OnHandChanged);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<RoundSummaryEvent>(OnRoundSummary);
            GameSettings.SettingsChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged()
        {
            if (_canvas != null) ConfigureScaler(_canvas);
            ApplyFelt();
            RefreshContract();
            RefreshPhaseHint();
        }

        private void ApplyFelt()
        {
            int v = GameSettings.Felt;
            if (_felt != null) _felt.color = UISkin.FeltFor(v);
            if (_feltLight != null) _feltLight.color = UISkin.WithAlpha(UISkin.FeltCenterFor(v), 0.9f);
        }

        private void OnStatusStyle(bool error)
        {
            if (_toastEdge != null)
            {
                _toastEdge.color = error ? UISkin.WithAlpha(UISkin.Hex(0xD96C5F), 0.75f)
                    : UISkin.WithAlpha(UISkin.AccentLight, 0.4f);
            }
        }

        private void Update()
        {
            // Invitation sur la pioche pendant l'étape « Piocher » du joueur local.
            if (_deckRing == null) return;
            bool show = _localTurn && _phase == TurnPhase.Draw && GameSettings.HintSteps
                        && _gameManager != null && _gameManager.State == GameState.PlayerTurn;
            if (!show)
            {
                if (_deckRing.enabled) _deckRing.enabled = false;
                return;
            }
            if (!_deckRing.enabled) _deckRing.enabled = true;
            _pulse += Time.unscaledDeltaTime;
            float a = GameSettings.ReducedMotion ? 0.8f : 0.45f + 0.4f * (0.5f + 0.5f * Mathf.Sin(_pulse * 3.2f));
            _deckRing.color = UISkin.WithAlpha(UISkin.AccentLight, a);
        }

        private void OnDestroy()
        {
            GameSettings.SettingsChanged -= OnSettingsChanged;
            if (_hud != null) _hud.StatusStyleChanged -= OnStatusStyle;
            EventBus.Unsubscribe<RoundSummaryEvent>(OnRoundSummary);
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<TurnPhaseChangedEvent>(OnPhaseChanged);
            EventBus.Unsubscribe<DeckChangedEvent>(OnDeckChanged);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Unsubscribe<LevelLaidDownEvent>(OnLevelLaidDown);
            EventBus.Unsubscribe<HandChangedEvent>(OnHandChanged);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        /// <summary>Branche les boutons pause/réglages de la top bar sur le menu pause.</summary>
        public void BindPauseMenu(PauseMenuController pauseMenu)
        {
            _pauseMenu = pauseMenu;
            if (_pauseButton != null) pauseMenu.RegisterPauseButton(_pauseButton.gameObject);
            if (_settingsButton != null) pauseMenu.RegisterPauseButton(_settingsButton.gameObject);
        }

        // ═══════════════════════════════════════════════════════════
        //  ÉCHELLE (Android : écrans 18:9 → 21:9, tablettes 4:3)
        // ═══════════════════════════════════════════════════════════

        private static void ConfigureScaler(Canvas canvas)
        {
            CanvasScaler? scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Interface « grande » (téléphones) : la même maquette, agrandie de
            // 30 % — il y a moins d'unités à l'écran, la mise en page (ancrée sur
            // les bords) les répartit, et chaque texte / bouton est 30 % plus grand.
            float factor = GameSettings.UiScaleFactor;
            scaler.referenceResolution = new Vector2(1920f / factor, 1080f / factor);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // Écran plus large que 16:9 (téléphones) : on cale la hauteur à 1080
            // et la largeur s'étend ; plus étroit (tablettes) : on cale la largeur.
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            scaler.matchWidthOrHeight = aspect >= 16f / 9f ? 1f : 0f;
        }

        // ═══════════════════════════════════════════════════════════
        //  TAPIS
        // ═══════════════════════════════════════════════════════════

        private static Image BuildFelt(RectTransform root)
        {
            Image felt = UISkin.Rect(root, "Felt", UISkin.Felt, 32f);
            felt.transform.SetAsFirstSibling();
            RectTransform rt = felt.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(Margin, FeltBottom);
            rt.offsetMax = new Vector2(-Margin, -FeltTop);

            // Lumière centrale douce (radial-gradient #18222D → #121921)
            GameObject glow = new("Light", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(rt, false);
            RectTransform grt = (RectTransform)glow.transform;
            grt.anchorMin = new Vector2(0.1f, 0.05f);
            grt.anchorMax = new Vector2(0.9f, 0.95f);
            grt.offsetMin = Vector2.zero;
            grt.offsetMax = Vector2.zero;
            Image gimg = glow.GetComponent<Image>();
            gimg.sprite = UISkin.Radial;
            gimg.color = UISkin.WithAlpha(UISkin.FeltCenter, 0.9f);
            gimg.raycastTarget = false;

            UISkin.Outline(rt, 32f, 1f, UISkin.Line, "Edge");
            return felt;
        }

        // ═══════════════════════════════════════════════════════════
        //  TOP BAR
        // ═══════════════════════════════════════════════════════════

        private void BuildTopBar(RectTransform root)
        {
            Image bar = UISkin.Rect(root, "TopBar", UISkin.Surface, 0f);
            RectTransform rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            // Déborde de la safe area pour couvrir toute la largeur de l'écran.
            rt.offsetMin = new Vector2(-400f, -TopBarHeight);
            rt.offsetMax = new Vector2(400f, 400f);
            bar.raycastTarget = true;
            bar.gameObject.AddComponent<UIInputBlocker>();

            Image line = UISkin.Rect(rt, "Line", UISkin.Line, 0f);
            line.rectTransform.anchorMin = new Vector2(0f, 0f);
            line.rectTransform.anchorMax = new Vector2(1f, 0f);
            line.rectTransform.pivot = new Vector2(0.5f, 0f);
            line.rectTransform.sizeDelta = new Vector2(0f, 1f);

            // Contenu calé sur la safe area (pas sur le fond débordant)
            GameObject content = new("TopBarContent", typeof(RectTransform));
            content.transform.SetParent(root, false);
            RectTransform crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.offsetMin = new Vector2(Margin, -TopBarHeight);
            crt.offsetMax = new Vector2(-Margin, 0f);

            // Gauche : logo + manche
            GameObject left = new("Left", typeof(RectTransform));
            left.transform.SetParent(crt, false);
            RectTransform lrt = (RectTransform)left.transform;
            UISkin.Place(lrt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(640f, 48f));
            UISkin.HRow(left, 16f, null, TextAnchor.MiddleLeft);

            TextMeshProUGUI logo = UISkin.Text(left.transform, "Logo", "Level<color=#D8B66E>Up</color>", 26f,
                UISkin.TextColor, TextWeight.Display);
            logo.characterSpacing = -2f;

            Image pill = UISkin.Rect(left.transform, "RoundPill", UISkin.AccentWash, 15f);
            UISkin.HRow(pill.gameObject, 0f, new RectOffset(12, 12, 6, 6), TextAnchor.MiddleCenter);
            UISkin.Outline(pill.rectTransform, 15f, 1f, UISkin.WithAlpha(UISkin.Accent, 0.35f))
                .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            _roundText = UISkin.Text(pill.transform, "Text", "Manche 1", 13f, UISkin.AccentPale, TextWeight.SemiBold);

            // Règles qui diffèrent du jeu classique (jokers, minuteur…) : rappel discret.
            Image rules = UISkin.Rect(left.transform, "RulesPill", UISkin.Glass, 15f);
            UISkin.HRow(rules.gameObject, 0f, new RectOffset(12, 12, 6, 6), TextAnchor.MiddleCenter);
            UISkin.Outline(rules.rectTransform, 15f, 1f, UISkin.Line)
                .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            _rulesText = UISkin.Text(rules.transform, "Text", "", 13f, UISkin.TextMuted, TextWeight.SemiBold);
            _rulesPill = rules.gameObject;
            _rulesPill.SetActive(false);

            // Centre : niveau du joueur local
            GameObject center = new("Level", typeof(RectTransform));
            center.transform.SetParent(crt, false);
            UISkin.Place((RectTransform)center.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(520f, 48f));
            UISkin.HRow(center, 18f, null, TextAnchor.MiddleCenter);
            UISkin.Text(center.transform, "Label", "Niveau", 14f, UISkin.TextMuted);
            GameObject segs = new("Segments", typeof(RectTransform));
            segs.transform.SetParent(center.transform, false);
            UISkin.HRow(segs, 6f, null, TextAnchor.MiddleCenter);
            for (int i = 0; i < Constants.MaxLevel; i++)
            {
                Image seg = UISkin.Rect(segs.transform, $"Seg{i + 1}", UISkin.SurfaceRaised, 4f);
                UISkin.Size(seg.gameObject, 34f, 8f);
                _levelSegments.Add(seg);
            }
            _levelValue = UISkin.Text(center.transform, "Value", "1<color=#A7A198><size=18> / 8</size></color>", 18f,
                UISkin.TextColor, TextWeight.Display);

            // Droite : réglages + pause (48×48, cibles tactiles)
            GameObject right = new("Right", typeof(RectTransform));
            right.transform.SetParent(crt, false);
            UISkin.Place((RectTransform)right.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                Vector2.zero, new Vector2(120f, 48f));
            UISkin.HRow(right, 10f, null, TextAnchor.MiddleRight);
            _settingsButton = IconButton(right.transform, "BtnSettings", UIIcon.Settings, 22f, "Paramètres",
                () => { _pauseMenu?.Show(); _pauseMenu?.ShowOptions(); });
            _pauseButton = IconButton(right.transform, "BtnPause", UIIcon.Pause, 20f, "Pause",
                () => _pauseMenu?.Toggle());
            // Zones tactiles agrandies sans changer le visuel (48 unités = moins
            // de 4 mm sur téléphone) ; côté voisin : la moitié de l'écart seulement.
            ((Image)_settingsButton.targetGraphic).raycastPadding = new Vector4(-20f, -20f, -5f, -16f);
            ((Image)_pauseButton.targetGraphic).raycastPadding = new Vector4(-5f, -20f, -20f, -16f);

            SetLevel(1);
        }

        /// <summary>Bouton carré 48×48 (radius 14, #1F2630) avec icône.</summary>
        public static Button IconButton(Transform parent, string name, UIIcon icon, float iconSize, string label,
            System.Action onClick)
        {
            Image bg = UISkin.Rect(parent, name, UISkin.SurfaceRaised, 14f, raycast: true);
            UISkin.Size(bg.gameObject, 48f, 48f);
            Image ic = UISkin.IconImage(bg.transform, "Icon", icon, iconSize, UISkin.TextColor);
            UISkin.Place(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(iconSize, iconSize));
            Button btn = bg.gameObject.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.35f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => onClick());
            _ = label;
            return btn;
        }

        /// <summary>Barre de niveau : autant de segments que de niveaux dans la partie.</summary>
        private void SetLevel(int level)
        {
            int total = GameSession.Rules.LevelsToWin;
            int shown = Mathf.Min(level, total);
            for (int i = 0; i < _levelSegments.Count; i++)
            {
                int l = i + 1;
                bool inGame = l <= total;
                if (_levelSegments[i].gameObject.activeSelf != inGame) _levelSegments[i].gameObject.SetActive(inGame);
                _levelSegments[i].color = l < shown ? UISkin.AccentLight : l == shown ? UISkin.Accent : UISkin.SurfaceRaised;
            }
            if (_levelValue != null)
            {
                _levelValue.text = $"{shown}<color=#A7A198><size=18> / {total}</size></color>";
            }
        }

        private void RefreshRulesPill()
        {
            if (_rulesPill == null || _rulesText == null) return;
            string tags = MatchRulesText.ShortTags(GameSession.Rules);
            _rulesText.text = tags;
            _rulesPill.SetActive(tags.Length > 0);
            if (tags.Length == 0) return;

            // Largeur bornée : la pastille ne doit jamais toucher la barre de niveau.
            _rulesText.overflowMode = TextOverflowModes.Ellipsis;
            float width = Mathf.Min(_rulesText.GetPreferredValues(tags).x + 2f, 300f);
            UISkin.Size(_rulesText.gameObject, width);
        }

        // ═══════════════════════════════════════════════════════════
        //  JOUEURS · TABLE · PILES · MAIN
        // ═══════════════════════════════════════════════════════════

        private static void PlacePlayersRow(RectTransform root)
        {
            RectTransform? players = Find(root, "LevelProgress");
            if (players == null) return;
            players.anchorMin = new Vector2(0f, 1f);
            players.anchorMax = new Vector2(1f, 1f);
            players.pivot = new Vector2(0.5f, 1f);
            players.offsetMin = new Vector2(Margin, -(PlayersTop + 64f));
            players.offsetMax = new Vector2(-Margin, -PlayersTop);
        }

        private void PlaceTableAndPiles(RectTransform root, RectTransform felt)
        {
            RectTransform? table = Find(root, "Table");
            RectTransform? deck = table != null ? Find(table, "DeckArea") : null;
            RectTransform? piles = Find(root, "DiscarPileView");

            // Pioche : à gauche du tapis
            if (deck != null)
            {
                deck.SetParent(felt, false);
                UISkin.Place(deck, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(64f + 60f, 16f), new Vector2(120f, 180f));
                deck.localRotation = Quaternion.identity;

                // Anneau d'invitation (pulse pendant l'étape « Piocher » du joueur local).
                _deckRing = UISkin.Outline(deck, 17f, 2.5f, UISkin.AccentLight, "DeckRing");
                UISkin.Stretch(_deckRing.rectTransform, -6f);
                _deckRing.enabled = false;
                // Son propre canvas : la pulsation ne fait plus reconstruire tout l'écran de jeu.
                _deckRing.gameObject.AddComponent<Canvas>();

                _deckCaption = UISkin.Text(felt, "DeckCaption", "Pioche", 13f, UISkin.TextMuted,
                    TextWeight.SemiBold, TextAlignmentOptions.Center);
                UISkin.Place(_deckCaption.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(64f + 60f, -96f), new Vector2(160f, 18f));
            }

            // Défausses : à droite de la pioche, une pile par joueur
            if (piles != null)
            {
                piles.SetParent(felt, false);
                UISkin.Place(piles, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(64f + 120f + 40f, 0f), new Vector2(560f, 210f));
                Image? img = piles.GetComponent<Image>();
                if (img != null) img.color = new Color(0f, 0f, 0f, 0f);
                DiscardPileView? view = piles.GetComponent<DiscardPileView>();
                if (view != null) view.PileSpacing = 140f;
            }

            // Combinaisons : zone droite du tapis
            if (table != null)
            {
                table.anchorMin = Vector2.zero;
                table.anchorMax = Vector2.one;
                table.pivot = new Vector2(0.5f, 0.5f);
                table.offsetMin = new Vector2(Margin + FeltLeftZone, FeltBottom + 36f);
                table.offsetMax = new Vector2(-(Margin + 48f), -(FeltTop + 36f));
                Image? img = table.GetComponent<Image>();
                if (img != null) img.color = new Color(0f, 0f, 0f, 0f);
                // Au-dessus du tapis
                table.SetSiblingIndex(felt.GetSiblingIndex() + 1);
            }
        }

        private static void PlaceHand(RectTransform root)
        {
            RectTransform? hand = Find(root, "PlayerHand");
            if (hand == null) return;
            hand.anchorMin = new Vector2(0f, 0f);
            hand.anchorMax = new Vector2(1f, 0f);
            hand.pivot = new Vector2(0.5f, 0f);
            hand.offsetMin = new Vector2(0f, 0f);
            hand.offsetMax = new Vector2(0f, 260f);
            hand.SetAsLastSibling();

            RectTransform? container = Find(hand, "HandContainer");
            if (container != null)
            {
                container.anchorMin = Vector2.zero;
                container.anchorMax = Vector2.one;
                container.offsetMin = new Vector2(0f, 20f);
                container.offsetMax = new Vector2(0f, 20f);
            }

            HandView? view = hand.GetComponent<HandView>();
            view?.SetLayout(cardSpacing: 84f, maxWidth: 980f);
        }

        // ═══════════════════════════════════════════════════════════
        //  ÉTAPES DU TOUR
        // ═══════════════════════════════════════════════════════════

        private void BuildStepper(RectTransform root)
        {
            Image bar = UISkin.Rect(root, "PhaseStepper", UISkin.Surface, 26f, raycast: true);
            bar.gameObject.AddComponent<UIInputBlocker>();
            RectTransform rt = bar.rectTransform;
            UISkin.Place(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, StepperBottom),
                Vector2.zero);
            UISkin.HRow(bar.gameObject, 8f, new RectOffset(6, 6, 6, 6), TextAnchor.MiddleCenter);
            ContentSizeFitter fit = bar.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UISkin.Outline(rt, 26f, 1f, UISkin.Line).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            string[] labels = { "Piocher", "Poser", "Compléter", "Défausser" };
            for (int i = 0; i < labels.Length; i++)
            {
                Image pill = UISkin.Rect(bar.transform, $"Step{i + 1}", new Color(0f, 0f, 0f, 0f), 20f);
                UISkin.HRow(pill.gameObject, 8f, new RectOffset(8, 16, 8, 8), TextAnchor.MiddleLeft);
                Image dot = UISkin.Rect(pill.transform, "Dot", UISkin.SurfaceRaised, 12f);
                UISkin.Size(dot.gameObject, 24f, 24f);
                TextMeshProUGUI num = UISkin.Text(dot.transform, "Num", (i + 1).ToString(), 12f, Color.white,
                    TextWeight.Bold, TextAlignmentOptions.Center);
                UISkin.Stretch(num.rectTransform);
                Image check = UISkin.IconImage(dot.transform, "Check", UIIcon.Check, 14f, UISkin.Hex(0x04170B));
                UISkin.Place(check.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(14f, 14f));
                TextMeshProUGUI label = UISkin.Text(pill.transform, "Label", labels[i], 14f, UISkin.TextMuted,
                    TextWeight.SemiBold);
                _steps.Add(new StepPill { Bg = pill, Dot = dot, Num = num, Check = check, Label = label });
            }

            // « Défausser » se touche : passe directement à la défausse (sans poser
            // ni compléter), puis on touche la carte à jeter.
            Image discardStep = _steps[3].Bg;
            discardStep.raycastTarget = true;
            Button skip = discardStep.gameObject.AddComponent<Button>();
            skip.targetGraphic = discardStep;
            skip.onClick.AddListener(() => _input?.SkipToDiscard());
            // Zone tactile agrandie (la pastille fait ~40 unités de haut).
            discardStep.raycastPadding = new Vector4(-4f, -22f, -16f, -22f);
            // Liseré visible quand l'étape est touchable : c'est un vrai bouton.
            _skipOutline = UISkin.Outline(discardStep.rectTransform, 20f, 1.5f,
                UISkin.WithAlpha(UISkin.AccentLight, 0.55f), "SkipOutline");
            _skipOutline.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            _skipOutline.enabled = false;

            // Consigne de l'étape en cours, juste au-dessus des étapes.
            _phaseHint = UISkin.Text(root, "PhaseHint", "", 15f, UISkin.TextMuted, TextWeight.Medium,
                TextAlignmentOptions.Center);
            UISkin.Place(_phaseHint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, StepperBottom + 60f), new Vector2(1100f, 24f));
            _phaseHint.richText = true;

            SetStep(TurnPhase.Draw, active: false);
        }

        /// <summary>Consigne courte de l'étape en cours (aides de jeu activées).</summary>
        private void RefreshPhaseHint()
        {
            if (_phaseHint == null) return;
            TurnManager? tm = _gameManager?.TurnManager;
            if (tm == null || _gameManager!.State != GameState.PlayerTurn || !GameSettings.HintSteps)
            {
                _phaseHint.text = "";
                return;
            }

            if (!_localTurn)
            {
                _phaseHint.text = $"Tour de {tm.CurrentPlayer.Name}…  <color=#8F8A82>tu peux ranger ta main</color>";
                return;
            }

            PlayerModel me = tm.CurrentPlayer;
            _phaseHint.text = _phase switch
            {
                TurnPhase.Draw => "Pioche : touche la pioche, ou une défausse pour prendre sa carte",
                // Étape Poser : le panneau de sélection (juste au-dessus) donne déjà
                // la consigne — pas de second texte dessous (ils se chevauchaient).
                TurnPhase.LayDown => "",
                TurnPhase.AddToMelds => me.HasLaidDownThisRound
                    ? GameSettings.HintPlayable
                        ? "Complète la table : glisse une carte, ou touche une carte marquée « + »"
                        : "Complète la table en y glissant tes cartes, ou touche « Défausser »"
                    : "",
                TurnPhase.Discard => GameSettings.QuickDiscard
                    ? "Touche une carte pour la défausser"
                    : "Touche une carte, puis touche-la encore pour la défausser",
                _ => ""
            };
        }

        /// <summary>« Défausser » n'est touchable que lorsqu'il permet de sauter des étapes.</summary>
        private void RefreshSkipStep()
        {
            if (_steps.Count < 4) return;
            bool canSkip = _localTurn && (_phase == TurnPhase.LayDown || _phase == TurnPhase.AddToMelds);
            Button? b = _steps[3].Bg.GetComponent<Button>();
            if (b != null) b.interactable = canSkip;
            if (_skipOutline != null) _skipOutline.enabled = canSkip;
            _steps[3].Label.text = canSkip ? "Défausser  ›" : "Défausser";
            if (canSkip)
            {
                _steps[3].Label.color = UISkin.TextColor;
                _steps[3].Bg.color = UISkin.Glass;
            }
        }

        private void UpdateTurnContext(int playerIndex, TurnPhase phase)
        {
            _phase = phase;
            _localTurn = _gameManager != null && playerIndex >= 0 && playerIndex < _gameManager.Players.Count
                         && GameSession.IsLocalHuman(_gameManager.Players[playerIndex]);
            RefreshPhaseHint();
            RefreshSkipStep();
        }

        private void SetStep(TurnPhase phase, bool active)
        {
            int current = phase switch
            {
                TurnPhase.Draw => 0,
                TurnPhase.LayDown => 1,
                TurnPhase.AddToMelds => 2,
                TurnPhase.Discard => 3,
                _ => 0
            };
            for (int i = 0; i < _steps.Count; i++)
            {
                StepPill s = _steps[i];
                bool done = active && i < current;
                bool cur = active && i == current;
                s.Bg.color = cur ? UISkin.AccentWashStrong : new Color(0f, 0f, 0f, 0f);
                s.Label.color = cur ? UISkin.AccentPale : done ? UISkin.TextColor : UISkin.TextMuted;
                s.Num.color = cur ? UISkin.OnAccent : UISkin.TextColor;
                s.Dot.color = cur ? UISkin.Accent : done ? UISkin.Success : UISkin.SurfaceRaised;
                s.Num.gameObject.SetActive(!done);
                s.Check.gameObject.SetActive(done);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  CONTRAT (bas-gauche)
        // ═══════════════════════════════════════════════════════════

        private void BuildContractCard(RectTransform root)
        {
            Image card = UISkin.Rect(root, "ContractCard", UISkin.Surface, 20f, raycast: true);
            card.gameObject.AddComponent<UIInputBlocker>();
            RectTransform rt = card.rectTransform;
            UISkin.Place(rt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(Margin, Margin),
                new Vector2(300f, 0f));
            UISkin.VCol(card.gameObject, 12f, new RectOffset(20, 20, 18, 18), TextAnchor.UpperLeft);
            ContentSizeFitter fit = card.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UISkin.Outline(rt, 20f, 1f, UISkin.Line).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            _contractTitle = UISkin.Text(card.transform, "Title", "CONTRAT · NIVEAU 1", 12f, UISkin.TextMuted,
                TextWeight.SemiBold);
            _contractTitle.characterSpacing = 8f;

            GameObject rows = new("Rows", typeof(RectTransform));
            rows.transform.SetParent(card.transform, false);
            _contractRows = (RectTransform)rows.transform;
            UISkin.VCol(rows, 12f, null, TextAnchor.UpperLeft);
            UISkin.Size(rows, 260f);

            // "Préparer la pose" : visible quand le contrat est faisable avec la
            // main ; sélectionne une répartition valide, le joueur garde la main.
            Image prep = UISkin.Rect(card.transform, "BtnPrepare", UISkin.AccentWash, 12f, raycast: true);
            UISkin.Size(prep.gameObject, 260f, 44f);
            UISkin.Outline(prep.rectTransform, 12f, 1f, UISkin.WithAlpha(UISkin.Accent, 0.45f));
            TextMeshProUGUI prepLabel = UISkin.Text(prep.transform, "Label", "Préparer la pose", 15f,
                UISkin.AccentPale, TextWeight.SemiBold, TextAlignmentOptions.Center);
            UISkin.Stretch(prepLabel.rectTransform);
            Button prepBtn = prep.gameObject.AddComponent<Button>();
            ColorBlock colors = prepBtn.colors;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            prepBtn.colors = colors;
            prepBtn.targetGraphic = prep;
            prepBtn.onClick.AddListener(() =>
            {
                if (_input != null && _input.SelectSuggestedLayDown())
                {
                    ShowHint("Touche « Poser » pour poser ton niveau");
                }
            });
            _prepareButton = prep.gameObject;
            _prepareButton.SetActive(false);
        }

        /// <summary>Branche le contrôleur d'input (bouton "Préparer la pose").</summary>
        public void BindInput(GameInputController? input)
        {
            _input = input;
        }

        private void ShowHint(string message)
        {
            _hud?.ShowStatus(message);
        }

        private ContractRow GetContractRow(int index)
        {
            while (_contractRowPool.Count <= index && _contractRows != null)
            {
                GameObject row = new($"Req{_contractRowPool.Count}", typeof(RectTransform));
                row.transform.SetParent(_contractRows, false);
                UISkin.HRow(row, 12f, null, TextAnchor.MiddleLeft);

                Image box = UISkin.Rect(row.transform, "Box", UISkin.SurfaceRaised, 7f);
                UISkin.Size(box.gameObject, 24f, 24f);
                Image ring = UISkin.Outline(box.rectTransform, 7f, 1f, UISkin.LineStrong);
                Image check = UISkin.IconImage(box.transform, "Check", UIIcon.Check, 14f, UISkin.OnAccent);
                UISkin.Place(check.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(14f, 14f));

                TextMeshProUGUI label = UISkin.Text(row.transform, "Label", "", 15f, UISkin.TextColor, TextWeight.Medium);
                UISkin.Size(label.gameObject, 170f);
                TextMeshProUGUI progress = UISkin.Text(row.transform, "Progress", "", 14f, UISkin.TextMuted,
                    TextWeight.SemiBold, TextAlignmentOptions.MidlineRight);
                UISkin.Size(progress.gameObject, 40f);

                _contractRowPool.Add(new ContractRow
                {
                    Root = row, Box = box, Ring = ring, Check = check, Label = label, Progress = progress
                });
            }
            return _contractRowPool[index];
        }

        private void RefreshContract()
        {
            PlayerModel? local = GameSession.GetLocalPlayer(_gameManager);
            if (local == null || _contractTitle == null) return;

            int level = local.CurrentLevel;
            _contractTitle.text = $"CONTRAT · NIVEAU {level}";

            List<List<LevelValidator.LevelRequirement>> sets =
                LevelValidator.GetRequirements(level, _gameManager?.Config);
            List<LevelValidator.LevelRequirement> reqs = sets.Count > 0 ? sets[0] : new List<LevelValidator.LevelRequirement>();
            List<ContractProgress.Entry> entries = ContractProgress.Evaluate(local.Hand, reqs);
            bool laid = local.HasLaidDownThisRound;
            // Sans l'aide « détection du contrat », la carte rappelle seulement les
            // exigences : pas de "2/3" ni de coche tant que le contrat n'est pas posé.
            bool showProgress = GameSettings.HintContract;

            for (int i = 0; i < _contractRowPool.Count; i++) _contractRowPool[i].Root.SetActive(false);
            for (int i = 0; i < entries.Count; i++)
            {
                ContractRow row = GetContractRow(i);
                ContractProgress.Entry e = entries[i];
                bool done = laid || (showProgress && e.IsMet);
                string name = ContractCatalog.Name(e.Requirement);
                row.Root.SetActive(true);
                row.Label.text = char.ToUpperInvariant(name[0]) + name.Substring(1);
                int have = laid ? e.Requirement.CardCount : Mathf.Min(e.Have, e.Requirement.CardCount);
                row.Progress.text = laid ? "posé" : showProgress ? $"{have}/{e.Requirement.CardCount}" : "";
                row.Box.color = done ? UISkin.Success : UISkin.SurfaceRaised;
                row.Ring.enabled = !done;
                row.Check.gameObject.SetActive(done);
            }

            if (_prepareButton != null)
            {
                _prepareButton.SetActive(_input != null && _input.CanLocalPlayerLayDown());
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TRI (bas-droite)
        // ═══════════════════════════════════════════════════════════

        private static void BuildSortControl(RectTransform root)
        {
            RectTransform? hand = Find(root, "PlayerHand");
            HandView? view = hand != null ? hand.GetComponent<HandView>() : null;
            if (view == null) return;

            GameObject group = new("SortControl", typeof(RectTransform));
            group.transform.SetParent(root, false);
            RectTransform grt = (RectTransform)group.transform;
            UISkin.Place(grt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-Margin, Margin),
                new Vector2(280f, 90f));
            UISkin.VCol(group, 10f, null, TextAnchor.LowerRight);

            TextMeshProUGUI caption = UISkin.Text(group.transform, "Caption", "TRIER LA MAIN", 12f, UISkin.TextMuted,
                TextWeight.SemiBold, TextAlignmentOptions.MidlineRight);
            caption.characterSpacing = 8f;

            Image seg = UISkin.Rect(group.transform, "Segmented", UISkin.Surface, 16f, raycast: true);
            seg.gameObject.AddComponent<UIInputBlocker>();
            UISkin.HRow(seg.gameObject, 4f, new RectOffset(4, 4, 4, 4), TextAnchor.MiddleCenter);
            UISkin.Outline(seg.rectTransform, 16f, 1f, UISkin.Line).gameObject.AddComponent<LayoutElement>()
                .ignoreLayout = true;

            view.MoveSortButtonsInto(seg.rectTransform);
            group.transform.SetAsLastSibling();
        }

        // ═══════════════════════════════════════════════════════════
        //  HUD : seuls les messages de statut restent (toast central)
        // ═══════════════════════════════════════════════════════════

        private void RestyleHud(RectTransform root)
        {
            RectTransform? hud = Find(root, "HUD");
            if (hud == null) return;

            Image? hudBg = hud.GetComponent<Image>();
            if (hudBg != null) hudBg.enabled = false;

            string[] hidden = { "CurrentPhaseText", "CurrentLevelText", "CurrentPlayerText", "RounNumberText", "DeckLabel" };
            foreach (string n in hidden)
            {
                RectTransform? t = Find(hud, n);
                if (t != null) t.gameObject.SetActive(false);
            }

            RectTransform? status = Find(hud, "StatusText");
            if (status == null) return;
            status.SetParent(root, false);
            // En haut au centre, dans la hauteur de la top bar : au milieu de
            // l'écran, le toast masquait les combinaisons qu'il annonçait.
            UISkin.Place(status, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f),
                new Vector2(520f, 48f));
            status.SetAsLastSibling();

            TextMeshProUGUI? text = status.GetComponent<TextMeshProUGUI>();
            if (text == null) return;

            // Toast : carte verre sombre + liseré accent. Les enfants d'un TMP se
            // dessinent PAR-DESSUS lui : le texte de HUDView (piloté par son
            // script, fade/pop compris) est donc recopié dans un TMP enfant
            // posé après le fond, et l'original ne se dessine plus.
            GameObject panel = new("ToastBg", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(status, false);
            RectTransform prt = (RectTransform)panel.transform;
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(-28f, -10f);
            prt.offsetMax = new Vector2(28f, 10f);
            Image pimg = panel.GetComponent<Image>();
            UISkin.SetRounded(pimg, 24f);
            pimg.color = UISkin.WithAlpha(UISkin.Surface, 0.94f);
            pimg.raycastTarget = false;
            _toastEdge = UISkin.Outline(prt, 24f, 1.5f, UISkin.WithAlpha(UISkin.AccentLight, 0.4f));

            TextMeshProUGUI front = UISkin.Text(status, "Front", "", 30f, Color.white, TextWeight.Display,
                TextAlignmentOptions.Center);
            UISkin.Stretch(front.rectTransform);
            front.enableAutoSizing = true;
            front.fontSizeMin = 16f;
            front.fontSizeMax = 24f;
            status.gameObject.AddComponent<TextMirror>().Bind(text, front);
        }

        // ═══════════════════════════════════════════════════════════
        //  ÉVÉNEMENTS
        // ═══════════════════════════════════════════════════════════

        private void OnGameStarted(GameStartedEvent evt)
        {
            RefreshLocalLevel();
            RefreshContract();
            RefreshRulesPill();
        }

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            if (_roundText != null) _roundText.text = $"Manche {evt.RoundNumber}";
            RefreshLocalLevel();
            RefreshContract();
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            SetStep(evt.Phase, active: true);
            RefreshLocalLevel();
            RefreshContract();
            UpdateTurnContext(evt.PlayerIndex, evt.Phase);
        }

        private void OnPhaseChanged(TurnPhaseChangedEvent evt)
        {
            SetStep(evt.NewPhase, active: true);
            RefreshContract();
            UpdateTurnContext(evt.PlayerIndex, evt.NewPhase);
        }

        private void OnRoundSummary(RoundSummaryEvent evt)
        {
            _localTurn = false;
            SetStep(TurnPhase.Draw, active: false);
            if (_phaseHint != null) _phaseHint.text = "";
            RefreshSkipStep();
        }

        private void OnDeckChanged(DeckChangedEvent evt)
        {
            if (_deckCaption != null) _deckCaption.text = $"Pioche · {evt.CardsRemaining}";
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            RefreshLocalLevel();
            RefreshContract();
        }

        private void OnLevelLaidDown(LevelLaidDownEvent evt) => RefreshContract();

        private void OnHandChanged(HandChangedEvent evt)
        {
            if (evt.PlayerIndex != GameSession.LocalPlayerIndex) return;
            RefreshContract();
            if (_localTurn && _phase == TurnPhase.LayDown) RefreshPhaseHint();
        }

        private void RefreshLocalLevel()
        {
            PlayerModel? local = GameSession.GetLocalPlayer(_gameManager);
            if (local != null) SetLevel(local.CurrentLevel);
        }

        // ═══════════════════════════════════════════════════════════
        //  OUTILS
        // ═══════════════════════════════════════════════════════════

        /// <summary>Recherche récursive d'un enfant par nom.</summary>
        public static RectTransform? Find(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name) return child as RectTransform;
                RectTransform? found = Find(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private class StepPill
        {
            public Image Bg = null!;
            public Image Dot = null!;
            public TextMeshProUGUI Num = null!;
            public Image Check = null!;
            public TextMeshProUGUI Label = null!;
        }

        private class ContractRow
        {
            public GameObject Root = null!;
            public Image Box = null!;
            public Image Ring = null!;
            public Image Check = null!;
            public TextMeshProUGUI Label = null!;
            public TextMeshProUGUI Progress = null!;
        }
    }

    /// <summary>
    /// Recopie le texte d'un TMP source (masqué) vers un TMP de rendu — permet
    /// de dessiner un fond SOUS un texte piloté par un script existant.
    /// </summary>
    public class TextMirror : MonoBehaviour
    {
        private TextMeshProUGUI? _source;
        private TextMeshProUGUI? _target;
        private string _last = "";

        public void Bind(TextMeshProUGUI source, TextMeshProUGUI target)
        {
            _source = source;
            _target = target;
            // La source garde son rôle (texte, rect animé, CanvasGroup) mais ne
            // se dessine plus : c'est le TMP enfant qui affiche.
            source.enabled = false;
            Sync();
        }

        private void LateUpdate() => Sync();

        private void Sync()
        {
            if (_source == null || _target == null) return;
            string t = _source.text;
            if (!ReferenceEquals(t, _last) && t != _last)
            {
                _last = t;
                _target.text = t;
            }
        }
    }
}
