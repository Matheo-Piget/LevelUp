using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Barre de progression des 8 niveaux pour tous les joueurs.
    /// Affiche visuellement où chaque joueur en est.
    /// </summary>
    public class LevelProgressView : MonoBehaviour
    {
        [SerializeField] private RectTransform? _container;
        [SerializeField] private float _stepWidth = 60f;
        [SerializeField] private float _playerRowHeight = 40f;
        [SerializeField] private Color _completedColor = new Color32(0x45, 0xC8, 0x78, 0xFF);
        [SerializeField] private Color _currentColor = new Color32(0xF5, 0xC8, 0x42, 0xFF);
        [SerializeField] private Color _pendingColor = new Color32(0x3A, 0x3F, 0x5C, 0xFF);

        private readonly List<PlayerProgressRow> _rows = new();

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
        }

        /// <summary>
        /// Crée une rangée de progression pour un joueur.
        /// </summary>
        private PlayerProgressRow CreatePlayerRow(int playerIndex, string playerName)
        {
            // Conteneur de la rangée
            GameObject rowObj = new($"ProgressRow_P{playerIndex}",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowObj.transform.SetParent(_container, false);

            RectTransform rowRt = rowObj.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(0, _playerRowHeight);

            HorizontalLayoutGroup layout = rowObj.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Nom du joueur
            GameObject nameObj = new("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(rowObj.transform, false);

            RectTransform nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.sizeDelta = new Vector2(100, _playerRowHeight);

            TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
            nameText.text = playerName;
            nameText.fontSize = 14;
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;

            // Étapes de niveau (1-8)
            List<Image> steps = new();
            List<TextMeshProUGUI> stepTexts = new();

            for (int lvl = 1; lvl <= Constants.MaxLevel; lvl++)
            {
                GameObject stepObj = new($"Step_{lvl}",
                    typeof(RectTransform), typeof(Image));
                stepObj.transform.SetParent(rowObj.transform, false);

                RectTransform stepRt = stepObj.GetComponent<RectTransform>();
                stepRt.sizeDelta = new Vector2(_stepWidth, _playerRowHeight - 8);

                Image stepImage = stepObj.GetComponent<Image>();
                stepImage.color = lvl == 1 ? _currentColor : _pendingColor;
                steps.Add(stepImage);

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
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
                stepTexts.Add(text);
            }

            return new PlayerProgressRow(playerIndex, nameText, steps, stepTexts);
        }

        /// <summary>
        /// Met à jour la progression d'un joueur.
        /// </summary>
        public void UpdatePlayerLevel(int playerIndex, int currentLevel)
        {
            if (playerIndex < 0 || playerIndex >= _rows.Count) return;

            PlayerProgressRow row = _rows[playerIndex];

            for (int i = 0; i < row.Steps.Count; i++)
            {
                int lvl = i + 1;
                if (lvl < currentLevel)
                {
                    row.Steps[i].color = _completedColor;
                }
                else if (lvl == currentLevel)
                {
                    row.Steps[i].color = _currentColor;
                }
                else
                {
                    row.Steps[i].color = _pendingColor;
                }
            }
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            UpdatePlayerLevel(evt.PlayerIndex, evt.Level + 1);
        }

        private void OnRoundEnded(RoundEndedEvent evt)
        {
            // La mise à jour sera faite par le GameManager quand il avance les niveaux
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

            public PlayerProgressRow(int index, TextMeshProUGUI? name,
                List<Image> steps, List<TextMeshProUGUI> stepTexts)
            {
                PlayerIndex = index;
                NameText = name;
                Steps = steps;
                StepTexts = stepTexts;
            }
        }
    }
}
