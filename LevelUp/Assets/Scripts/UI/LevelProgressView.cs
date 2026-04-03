using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Barre de progression des 8 niveaux style Balatro : indicateurs arrondis,
    /// pulse animé sur le niveau actuel, transitions smooth.
    /// </summary>
    public class LevelProgressView : MonoBehaviour
    {
        [SerializeField] private RectTransform? _container;
        [SerializeField] private float _stepWidth = 40f;
        [SerializeField] private float _stepHeight = 40f;
        [SerializeField] private float _playerRowHeight = 48f;
        [SerializeField] private AnimationController? _animController;

        // Couleurs Balatro
        private readonly Color _completedColor = Constants.CardGreen;
        private readonly Color _currentColor = Constants.TextAccent;
        private readonly Color _pendingColor = new Color32(0x1E, 0x2D, 0x40, 0xFF);
        private readonly Color _pendingTextColor = new Color32(0x3A, 0x4A, 0x60, 0xFF);

        private readonly List<PlayerProgressRow> _rows = new();
        private Coroutine? _pulseCoroutine;

        private void OnEnable()
        {
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Subscribe<RoundEndedEvent>(OnRoundEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Unsubscribe<RoundEndedEvent>(OnRoundEnded);
        }

        /// <summary>
        /// Initialise la barre de progression pour tous les joueurs.
        /// </summary>
        public void Initialize(int playerCount, List<string> playerNames)
        {
            ClearRows();

            if (_container == null) return;

            for (int p = 0; p < playerCount; p++)
            {
                PlayerProgressRow row = CreatePlayerRow(p, playerNames[p]);
                _rows.Add(row);
            }

            StartPulseAnimation();
        }

        /// <summary>
        /// Crée une rangée de progression pour un joueur.
        /// </summary>
        private PlayerProgressRow CreatePlayerRow(int playerIndex, string playerName)
        {
            // Conteneur de la rangée avec fond
            GameObject rowObj = new($"ProgressRow_P{playerIndex}",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(Image));
            rowObj.transform.SetParent(_container, false);

            RectTransform rowRt = rowObj.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(0, _playerRowHeight);

            // Fond subtil pour la rangée
            Image rowBg = rowObj.GetComponent<Image>();
            rowBg.color = playerIndex % 2 == 0
                ? new Color(0.06f, 0.09f, 0.14f, 0.5f)
                : new Color(0.08f, 0.11f, 0.16f, 0.5f);
            rowBg.raycastTarget = false;

            HorizontalLayoutGroup layout = rowObj.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(8, 8, 4, 4);

            // Nom du joueur
            GameObject nameObj = new("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(rowObj.transform, false);

            RectTransform nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.sizeDelta = new Vector2(80, _playerRowHeight);

            TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
            nameText.text = playerName;
            nameText.fontSize = 13;
            nameText.color = Constants.TextSecondary;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.fontStyle = FontStyles.Bold;

            // Étapes de niveau (1-8) — indicateurs arrondis
            List<Image> steps = new();
            List<TextMeshProUGUI> stepTexts = new();
            List<Image> stepBorders = new();

            for (int lvl = 1; lvl <= Constants.MaxLevel; lvl++)
            {
                // Conteneur step
                GameObject stepObj = new($"Step_{lvl}",
                    typeof(RectTransform), typeof(Image));
                stepObj.transform.SetParent(rowObj.transform, false);

                RectTransform stepRt = stepObj.GetComponent<RectTransform>();
                stepRt.sizeDelta = new Vector2(_stepWidth, _stepHeight);

                Image stepImage = stepObj.GetComponent<Image>();
                stepImage.color = lvl == 1 ? _currentColor : _pendingColor;
                steps.Add(stepImage);

                // Bordure (enfant séparé pour le glow)
                GameObject borderObj = new("Border", typeof(RectTransform), typeof(Image));
                borderObj.transform.SetParent(stepObj.transform, false);

                RectTransform borderRt = borderObj.GetComponent<RectTransform>();
                borderRt.anchorMin = Vector2.zero;
                borderRt.anchorMax = Vector2.one;
                borderRt.sizeDelta = new Vector2(4f, 4f);
                borderRt.anchoredPosition = Vector2.zero;

                Image borderImg = borderObj.GetComponent<Image>();
                borderImg.color = lvl == 1 ? _currentColor : Color.clear;
                borderImg.raycastTarget = false;
                stepBorders.Add(borderImg);

                // Numéro du niveau
                GameObject textObj = new("Text",
                    typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(stepObj.transform, false);

                RectTransform textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;

                TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
                text.text = lvl.ToString();
                text.fontSize = 16;
                text.color = lvl == 1 ? Constants.CardFaceColor : _pendingTextColor;
                text.alignment = TextAlignmentOptions.Center;
                text.fontStyle = FontStyles.Bold;
                stepTexts.Add(text);
            }

            return new PlayerProgressRow(playerIndex, nameText, steps, stepTexts, stepBorders);
        }

        /// <summary>
        /// Met à jour la progression d'un joueur avec animation.
        /// </summary>
        public void UpdatePlayerLevel(int playerIndex, int currentLevel)
        {
            if (playerIndex < 0 || playerIndex >= _rows.Count) return;

            PlayerProgressRow row = _rows[playerIndex];

            for (int i = 0; i < row.Steps.Count; i++)
            {
                int lvl = i + 1;
                bool completed = lvl < currentLevel;
                bool current = lvl == currentLevel;

                // Background
                Color targetBg = completed ? _completedColor
                    : current ? _currentColor
                    : _pendingColor;
                row.Steps[i].color = targetBg;

                // Bordure (glow sur current)
                if (i < row.StepBorders.Count)
                {
                    row.StepBorders[i].color = current ? _currentColor : Color.clear;
                }

                // Texte
                if (i < row.StepTexts.Count)
                {
                    row.StepTexts[i].color = (completed || current)
                        ? Constants.CardFaceColor
                        : _pendingTextColor;
                }

                // Animation bounce sur le nouveau niveau current
                if (current && _animController != null)
                {
                    RectTransform stepRt = row.Steps[i].GetComponent<RectTransform>();
                    _animController.AnimatePulse(stepRt);
                }
            }
        }

        /// <summary>
        /// Pulse continu sur tous les niveaux "current".
        /// </summary>
        private void StartPulseAnimation()
        {
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = StartCoroutine(PulseCurrentLevels());
        }

        private IEnumerator PulseCurrentLevels()
        {
            while (true)
            {
                float t = Mathf.PingPong(Time.time * 1.5f, 1f);
                float alpha = Mathf.Lerp(0.7f, 1f, t);

                foreach (PlayerProgressRow row in _rows)
                {
                    for (int i = 0; i < row.StepBorders.Count; i++)
                    {
                        if (row.StepBorders[i].color.a > 0.01f)
                        {
                            Color c = _currentColor;
                            c.a = alpha * 0.5f;
                            row.StepBorders[i].color = c;
                        }
                    }
                }

                yield return null;
            }
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            UpdatePlayerLevel(evt.PlayerIndex, evt.Level + 1);
        }

        private void OnRoundEnded(RoundEndedEvent evt)
        {
            // Mis à jour par GameManager
        }

        /// <summary>
        /// Rafraîchit l'affichage pour tous les joueurs.
        /// </summary>
        public void RefreshAll(IReadOnlyList<Core.PlayerModel> players)
        {
            for (int i = 0; i < players.Count && i < _rows.Count; i++)
            {
                UpdatePlayerLevel(i, players[i].CurrentLevel);
            }
        }

        /// <summary>
        /// Supprime toutes les rangées.
        /// </summary>
        private void ClearRows()
        {
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }

            foreach (PlayerProgressRow row in _rows)
            {
                if (row.NameText != null)
                {
                    Destroy(row.NameText.transform.parent.gameObject);
                }
            }
            _rows.Clear();
        }

        /// <summary>
        /// Données d'une rangée de progression joueur.
        /// </summary>
        private class PlayerProgressRow
        {
            public int PlayerIndex;
            public TextMeshProUGUI? NameText;
            public List<Image> Steps;
            public List<TextMeshProUGUI> StepTexts;
            public List<Image> StepBorders;

            public PlayerProgressRow(int index, TextMeshProUGUI? name,
                List<Image> steps, List<TextMeshProUGUI> stepTexts, List<Image> stepBorders)
            {
                PlayerIndex = index;
                NameText = name;
                Steps = steps;
                StepTexts = stepTexts;
                StepBorders = stepBorders;
            }
        }
    }
}
