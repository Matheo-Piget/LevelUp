using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Affiche en temps réel le statut de la sélection courante : nombre de cartes
    /// sélectionnées, et si elles forment une combinaison valide pour le niveau.
    /// Apparaît au-dessus de la main pendant la phase LayDown. Donne du feedback
    /// instantané au joueur pour réduire la friction.
    /// </summary>
    public class SelectionStatusView : MonoBehaviour
    {
        private GameManager? _gameManager;
        private HandView? _handView;

        private GameObject? _root;
        private CanvasGroup? _canvasGroup;
        private Image? _bg;
        private TextMeshProUGUI? _label;
        private TextMeshProUGUI? _hint;

        private float _targetAlpha;
        private float _currentAlpha;

        private GameInputController? _input;
        private GameObject? _layDownButton;
        private RectTransform? _labelRect;
        private RectTransform? _hintRect;

        /// <summary>
        /// Construit l'overlay et le câble aux dépendances.
        /// </summary>
        public void Setup(Canvas canvas, GameManager gameManager, HandView handView, GameInputController? input = null)
        {
            if (canvas == null) return;

            _gameManager = gameManager;
            _handView = handView;
            _input = input;

            BuildUi(canvas);

            EventBus.Subscribe<TurnPhaseChangedEvent>(OnPhaseChanged);
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<HandChangedEvent>(OnHandChanged);

            handView.OnSelectionChanged += Refresh;
            GameSettings.SettingsChanged += Refresh;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<TurnPhaseChangedEvent>(OnPhaseChanged);
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<HandChangedEvent>(OnHandChanged);

            if (_handView != null)
            {
                _handView.OnSelectionChanged -= Refresh;
            }
            GameSettings.SettingsChanged -= Refresh;
        }

        private void BuildUi(Canvas canvas)
        {
            _root = new GameObject("SelectionStatus",
                typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _root.transform.SetParent(canvas.transform, false);
            _root.transform.SetAsLastSibling();

            RectTransform rt = _root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 322f);
            rt.sizeDelta = new Vector2(640f, 72f);

            _bg = _root.GetComponent<Image>();
            UISkin.SetRounded(_bg, 18f);
            _bg.type = Image.Type.Sliced;
            _bg.color = UISkin.WithAlpha(UISkin.Surface, 0.96f);
            UISkin.Outline(rt, 18f, 1f, UISkin.WithAlpha(UISkin.AccentLight, 0.4f));
            _bg.raycastTarget = false;

            _canvasGroup = _root.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            // Bordure colorée gauche
            GameObject barObj = new("Bar", typeof(RectTransform), typeof(Image));
            barObj.transform.SetParent(_root.transform, false);
            RectTransform barRt = barObj.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0.5f);
            barRt.anchorMax = new Vector2(0f, 0.5f);
            barRt.pivot = new Vector2(0f, 0.5f);
            barRt.sizeDelta = new Vector2(10f, 10f);
            barRt.anchoredPosition = new Vector2(20f, 0f);
            Image barImg = barObj.GetComponent<Image>();
            UISkin.SetRounded(barImg, 5f); // pastille d'état (pas de liseré latéral)
            barImg.color = Constants.TextSecondary;
            barImg.raycastTarget = false;

            // Label principal
            GameObject labelObj = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(_root.transform, false);
            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0.5f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.sizeDelta = Vector2.zero;
            labelRt.offsetMin = new Vector2(42f, 0f);
            labelRt.offsetMax = new Vector2(-12f, -2f);

            _label = labelObj.GetComponent<TextMeshProUGUI>();
            _label.text = "";
            _label.fontSize = 18f;
            _label.color = Constants.TextPrimary;
            _label.alignment = TextAlignmentOptions.MidlineLeft;
            _label.fontStyle = FontStyles.Bold;
            _label.raycastTarget = false;

            // Hint sous le label
            GameObject hintObj = new("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintObj.transform.SetParent(_root.transform, false);
            RectTransform hintRt = hintObj.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0.5f);
            hintRt.sizeDelta = Vector2.zero;
            hintRt.offsetMin = new Vector2(42f, 4f);
            hintRt.offsetMax = new Vector2(-12f, 0f);

            _hint = hintObj.GetComponent<TextMeshProUGUI>();
            _hint.text = "";
            _hint.fontSize = 14f;
            _hint.color = Constants.TextSecondary;
            _hint.alignment = TextAlignmentOptions.MidlineLeft;
            _hint.raycastTarget = false;
            _hint.richText = true;
            _labelRect = labelRt;
            _hintRect = hintRt;

            // Bouton « Poser » : l'action explicite (toucher la table marche aussi).
            _root.AddComponent<UIInputBlocker>();
            Button lay = UIFactory.CreateButton(_root.transform, "BtnLayDown", "POSER",
                new Vector2(150f, 52f), () => _input?.OnLayDownButton(), primary: true);
            RectTransform brt = (RectTransform)lay.transform;
            UISkin.Place(brt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f),
                new Vector2(150f, 52f));
            _layDownButton = lay.gameObject;
            _layDownButton.SetActive(false);
        }

        private void SetLayDownButton(bool visible)
        {
            if (_layDownButton == null || _canvasGroup == null) return;
            if (_layDownButton.activeSelf != visible) _layDownButton.SetActive(visible);
            _canvasGroup.blocksRaycasts = visible;
            _canvasGroup.interactable = visible;
            float right = visible ? -170f : -12f;
            if (_labelRect != null) _labelRect.offsetMax = new Vector2(right, _labelRect.offsetMax.y);
            if (_hintRect != null) _hintRect.offsetMax = new Vector2(right, _hintRect.offsetMax.y);
        }

        private void Update()
        {
            if (_canvasGroup == null || _currentAlpha == _targetAlpha) return;
            _currentAlpha = Mathf.Lerp(_currentAlpha, _targetAlpha, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            if (Mathf.Abs(_currentAlpha - _targetAlpha) < 0.004f) _currentAlpha = _targetAlpha;
            _canvasGroup.alpha = _currentAlpha;
        }

        private void OnPhaseChanged(TurnPhaseChangedEvent evt) => Refresh();
        private void OnTurnStarted(TurnStartedEvent evt) => Refresh();
        private void OnHandChanged(HandChangedEvent evt) => Refresh();

        /// <summary>
        /// Met à jour le statut affiché en fonction de la sélection courante.
        /// </summary>
        public void Refresh()
        {
            if (_gameManager == null || _handView == null || _label == null || _hint == null || _bg == null)
                return;

            TurnManager? tm = _gameManager.TurnManager;
            if (tm == null)
            {
                Hide();
                return;
            }

            PlayerModel current = tm.CurrentPlayer;
            // En ligne, n'aider que le joueur de CET appareil (pas la main d'un autre).
            if (_gameManager.State != GameState.PlayerTurn || !GameSession.IsLocalHuman(current)
                || tm.CurrentPhase != TurnPhase.LayDown || current.HasLaidDownThisRound)
            {
                Hide();
                return;
            }

            int selectedCount = _handView.SelectedCards.Count;
            string requirementText = DescribeRequirement(current.CurrentLevel);

            // Aide « détection du contrat » coupée : rappel de l'objectif seulement.
            // Ni « contrat prêt », ni progression, ni validation de la sélection : le
            // bouton POSER apparaît dès qu'une carte est sélectionnée, le Core tranche.
            if (!GameSettings.HintContract)
            {
                _label.text = $"NIVEAU {current.CurrentLevel}  <size=15><color=#A7A198>{requirementText}</color></size>";
                _label.color = Constants.TextAccent;
                // Consigne neutre (aucune information de jeu) : comment poser, et
                // comment finir son tour sans poser.
                _hint.text = selectedCount == 0
                    ? "Sélectionne ton contrat pour le poser, ou glisse une carte sur ta défausse"
                    : selectedCount == 1
                        ? "1 carte sélectionnée · touche ta défausse pour la jeter"
                        : $"{selectedCount} cartes sélectionnées";
                _hint.color = Constants.TextSecondary;
                SetBarColor(Constants.TextSecondary);
                SetLayDownButton(selectedCount > 0);
                Show();
                return;
            }

            if (selectedCount == 0)
            {
                // Progression de la main vers le contrat : lisible d'un coup d'œil.
                bool ready = _input != null
                    ? _input.LocalHandMeetsContract(current)
                    : LevelValidator.IsLevelComplete(current.Hand, current.CurrentLevel, _gameManager.Config, out _);
                string progress = DescribeProgress(current);

                if (ready)
                {
                    _label.text = $"NIVEAU {current.CurrentLevel} · CONTRAT PRÊT !";
                    _label.color = Constants.CardGreen;
                    _hint.text = $"« Poser » choisit la répartition, ou sélectionne {requirementText}";
                    _hint.color = Constants.CardGreen;
                    SetBarColor(Constants.CardGreen);
                }
                else
                {
                    _label.text = $"NIVEAU {current.CurrentLevel}  <size=15><color=#A7A198>{requirementText}</color></size>";
                    _label.color = Constants.TextAccent;
                    _hint.text = progress;
                    _hint.color = Constants.TextSecondary;
                    SetBarColor(Constants.TextSecondary);
                }
                SetLayDownButton(ready);
                Show();
                return;
            }

            // Évaluer la sélection : on construit une "main" temporaire basée sur la sélection,
            // et on teste si le niveau peut être complété avec uniquement ces cartes.
            List<CardModel> selection = _handView.GetSelectedCardModels();
            bool valid = LevelValidator.IsLevelComplete(selection, current.CurrentLevel,
                _gameManager.Config, out List<Meld> _);

            if (valid)
            {
                _label.text = $"SÉLECTION VALIDE — {selectedCount} cartes";
                _label.color = Constants.CardGreen;
                _hint.text = "Touche « Poser » (ou la table) pour poser ton niveau";
                _hint.color = Constants.CardGreen;
                SetBarColor(Constants.CardGreen);
            }
            else
            {
                _label.text = $"{selectedCount} carte{(selectedCount > 1 ? "s" : "")} sélectionnée{(selectedCount > 1 ? "s" : "")}";
                _label.color = Constants.CardYellow;
                _hint.text = $"Objectif : {requirementText}";
                _hint.color = Constants.TextSecondary;
                SetBarColor(Constants.CardYellow);
            }

            SetLayDownButton(valid);
            Show();
        }

        private void SetBarColor(Color color)
        {
            if (_root == null) return;
            Transform bar = _root.transform.Find("Bar");
            if (bar != null && bar.TryGetComponent(out Image img))
            {
                img.color = color;
            }
        }

        private static string DescribeRequirement(int level)
        {
            return ContractCatalog.DescribeLevel(level);
        }

        /// <summary>
        /// "Suite de 3 3/3 · Brelan 2/3" — chaque exigence avec ce que la main
        /// couvre déjà, en vert quand c'est rempli.
        /// </summary>
        private string DescribeProgress(PlayerModel player)
        {
            List<List<LevelValidator.LevelRequirement>> sets =
                LevelValidator.GetRequirements(player.CurrentLevel, _gameManager?.Config);
            if (sets.Count == 0) return "";

            List<ContractProgress.Entry> entries = ContractProgress.Evaluate(player.Hand, sets[0]);
            System.Text.StringBuilder sb = new();
            foreach (ContractProgress.Entry e in entries)
            {
                if (sb.Length > 0) sb.Append("<color=#6E6A64>  ·  </color>");
                string color = e.IsMet ? "#5FB98A" : (e.Have > 0 ? "#E6CB92" : "#A7A198");
                int have = Mathf.Min(e.Have, e.Requirement.CardCount);
                sb.Append(ContractCatalog.Name(e.Requirement))
                  .Append(' ')
                  .Append($"<color={color}><b>{have}/{e.Requirement.CardCount}</b></color>");
            }
            return sb.ToString();
        }

        private void Show() => _targetAlpha = 1f;

        private void Hide()
        {
            _targetAlpha = 0f;
            SetLayDownButton(false);
        }
    }
}
