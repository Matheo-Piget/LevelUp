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

        /// <summary>
        /// Construit l'overlay et le câble aux dépendances.
        /// </summary>
        public void Setup(Canvas canvas, GameManager gameManager, HandView handView)
        {
            if (canvas == null) return;

            _gameManager = gameManager;
            _handView = handView;

            BuildUi(canvas);

            EventBus.Subscribe<TurnPhaseChangedEvent>(OnPhaseChanged);
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<HandChangedEvent>(OnHandChanged);

            handView.OnSelectionChanged += Refresh;
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
            rt.anchoredPosition = new Vector2(0f, 270f);
            rt.sizeDelta = new Vector2(500f, 76f);

            _bg = _root.GetComponent<Image>();
            _bg.sprite = UIFactory.RoundedSprite;
            _bg.type = Image.Type.Sliced;
            _bg.color = new Color(0.06f, 0.10f, 0.16f, 0.88f);
            _bg.raycastTarget = false;

            _canvasGroup = _root.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            // Bordure colorée gauche
            GameObject barObj = new("Bar", typeof(RectTransform), typeof(Image));
            barObj.transform.SetParent(_root.transform, false);
            RectTransform barRt = barObj.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(0f, 1f);
            barRt.pivot = new Vector2(0f, 0.5f);
            barRt.sizeDelta = new Vector2(5f, 0f);
            barRt.anchoredPosition = Vector2.zero;
            Image barImg = barObj.GetComponent<Image>();
            barImg.color = Constants.TextSecondary;
            barImg.raycastTarget = false;

            // Label principal
            GameObject labelObj = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(_root.transform, false);
            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0.5f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.sizeDelta = Vector2.zero;
            labelRt.offsetMin = new Vector2(16f, 0f);
            labelRt.offsetMax = new Vector2(-12f, -2f);

            _label = labelObj.GetComponent<TextMeshProUGUI>();
            _label.text = "";
            _label.fontSize = 22f;
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
            hintRt.offsetMin = new Vector2(16f, 4f);
            hintRt.offsetMax = new Vector2(-12f, 0f);

            _hint = hintObj.GetComponent<TextMeshProUGUI>();
            _hint.text = "";
            _hint.fontSize = 15f;
            _hint.color = Constants.TextSecondary;
            _hint.alignment = TextAlignmentOptions.MidlineLeft;
            _hint.raycastTarget = false;
        }

        private void Update()
        {
            if (_canvasGroup == null) return;
            _currentAlpha = Mathf.Lerp(_currentAlpha, _targetAlpha, Time.deltaTime * 12f);
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
            if (current.IsAI || tm.CurrentPhase != TurnPhase.LayDown || current.HasLaidDownThisRound)
            {
                Hide();
                return;
            }

            int selectedCount = _handView.SelectedCards.Count;
            string requirementText = DescribeRequirement(current.CurrentLevel);

            if (selectedCount == 0)
            {
                _label.text = $"NIVEAU {current.CurrentLevel}";
                _label.color = Constants.TextAccent;
                _hint.text = $"Objectif : {requirementText}  •  ou glissez ↑ pour défausser";
                _hint.color = Constants.TextSecondary;
                SetBarColor(Constants.TextSecondary);
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
                _label.text = $"COMBO VALIDE — {selectedCount} cartes";
                _label.color = Constants.CardGreen;
                _hint.text = "Cliquez la table pour poser le niveau";
                _hint.color = Constants.CardGreen;
                SetBarColor(Constants.CardGreen);
            }
            else
            {
                _label.text = $"{selectedCount} cartes sélectionnées";
                _label.color = Constants.CardYellow;
                _hint.text = $"Objectif : {requirementText}";
                _hint.color = Constants.TextSecondary;
                SetBarColor(Constants.CardYellow);
            }

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

        private string DescribeRequirement(int level)
        {
            return level switch
            {
                1 => "2 suites de 3",
                2 => "1 suite de 3 + 1 brelan",
                3 => "2 brelans",
                4 => "1 suite de 4 + 1 paire",
                5 => "1 flush de 5",
                6 => "1 suite de 5",
                7 => "1 carré + 1 paire",
                8 => "1 flush de 7",
                _ => "?"
            };
        }

        private void Show() => _targetAlpha = 1f;
        private void Hide() => _targetAlpha = 0f;
    }
}
