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
        [SerializeField] private float _stepWidth = 30f;
        [SerializeField] private float _stepHeight = 30f;
        [SerializeField] private float _playerRowHeight = 64f;
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

            EnsureContainerLayout(_container);

            for (int p = 0; p < playerCount; p++)
            {
                PlayerProgressRow row = CreatePlayerRow(p, playerNames[p]);
                _rows.Add(row);
            }

            StartPulseAnimation();
        }

        private static void EnsureContainerLayout(RectTransform container)
        {
            VerticalLayoutGroup vlg = container.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = container.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }

        /// <summary>
        /// Crée une rangée de progression pour un joueur.
        /// </summary>
        private PlayerProgressRow CreatePlayerRow(int playerIndex, string playerName)
        {
            // Conteneur de la rangée (layout vertical : nom au-dessus, steps en-dessous)
            GameObject rowObj = new($"ProgressRow_P{playerIndex}",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(Image), typeof(LayoutElement));
            rowObj.transform.SetParent(_container, false);

            // Fond subtil pour la rangée
            Image rowBg = rowObj.GetComponent<Image>();
            rowBg.color = playerIndex % 2 == 0
                ? new Color(0.06f, 0.09f, 0.14f, 0.5f)
                : new Color(0.08f, 0.11f, 0.16f, 0.5f);
            rowBg.raycastTarget = false;

            VerticalLayoutGroup rowLayout = rowObj.GetComponent<VerticalLayoutGroup>();
            rowLayout.spacing = 4f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;
            rowLayout.padding = new RectOffset(8, 8, 6, 6);

            LayoutElement rowLe = rowObj.GetComponent<LayoutElement>();
            rowLe.minHeight = _playerRowHeight;
            rowLe.preferredHeight = _playerRowHeight;

            // Nom du joueur (ligne du haut)
            GameObject nameObj = new("Name", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            nameObj.transform.SetParent(rowObj.transform, false);

            TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
            nameText.text = playerName;
            nameText.fontSize = 13;
            nameText.color = Constants.TextSecondary;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontStyle = FontStyles.Bold;

            LayoutElement nameLe = nameObj.GetComponent<LayoutElement>();
            nameLe.preferredHeight = 18f;

            // Conteneur des steps (ligne du bas, horizontal)
            GameObject stepsObj = new("Steps",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            stepsObj.transform.SetParent(rowObj.transform, false);

            HorizontalLayoutGroup stepsLayout = stepsObj.GetComponent<HorizontalLayoutGroup>();
            stepsLayout.spacing = 4f;
            stepsLayout.childAlignment = TextAnchor.MiddleCenter;
            stepsLayout.childControlWidth = false;
            stepsLayout.childControlHeight = false;
            stepsLayout.childForceExpandWidth = false;
            stepsLayout.childForceExpandHeight = false;

            LayoutElement stepsLe = stepsObj.GetComponent<LayoutElement>();
            stepsLe.preferredHeight = _stepHeight + 4f;

            // Étapes de niveau (1-8) — indicateurs arrondis
            List<Image> steps = new();
            List<TextMeshProUGUI> stepTexts = new();
            List<Image> stepBorders = new();

            for (int lvl = 1; lvl <= Constants.MaxLevel; lvl++)
            {
                // Conteneur step
                GameObject stepObj = new($"Step_{lvl}",
                    typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                stepObj.transform.SetParent(stepsObj.transform, false);

                RectTransform stepRt = stepObj.GetComponent<RectTransform>();
                stepRt.sizeDelta = new Vector2(_stepWidth, _stepHeight);

                LayoutElement stepLe = stepObj.GetComponent<LayoutElement>();
                stepLe.preferredWidth = _stepWidth;
                stepLe.preferredHeight = _stepHeight;

                Image stepImage = stepObj.GetComponent<Image>();
                stepImage.sprite = UIFactory.RoundedSprite;
                stepImage.type = Image.Type.Sliced;
                stepImage.color = lvl == 1 ? _currentColor : _pendingColor;
                steps.Add(stepImage);

                // Halo doux (SoftShadow) pour le niveau courant, au lieu d'un contour dur
                GameObject glowObj = new("Glow", typeof(RectTransform), typeof(Image));
                glowObj.transform.SetParent(stepObj.transform, false);
                glowObj.transform.SetAsFirstSibling();

                RectTransform glowRt = glowObj.GetComponent<RectTransform>();
                glowRt.anchorMin = Vector2.zero;
                glowRt.anchorMax = Vector2.one;
                glowRt.sizeDelta = new Vector2(22f, 22f);
                glowRt.anchoredPosition = Vector2.zero;

                Image borderImg = glowObj.GetComponent<Image>();
                borderImg.sprite = UIFactory.SoftShadowSprite;
                borderImg.color = lvl == 1
                    ? new Color(_currentColor.r, _currentColor.g, _currentColor.b, 0.55f)
                    : Color.clear;
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

            if (row.IsCurrent == null || row.IsCurrent.Length != row.Steps.Count)
            {
                row.IsCurrent = new bool[row.Steps.Count];
            }

            for (int i = 0; i < row.Steps.Count; i++)
            {
                int lvl = i + 1;
                bool completed = lvl < currentLevel;
                bool current = lvl == currentLevel;

                row.IsCurrent[i] = current;

                // Background
                Color targetBg = completed ? _completedColor
                    : current ? _currentColor
                    : _pendingColor;
                row.Steps[i].color = targetBg;

                // Reset scale for non-current
                if (!current)
                {
                    row.Steps[i].GetComponent<RectTransform>().localScale = Vector3.one;
                }

                // Glow soft sur le niveau courant
                if (i < row.StepBorders.Count)
                {
                    row.StepBorders[i].color = current
                        ? new Color(_currentColor.r, _currentColor.g, _currentColor.b, 0.55f)
                        : Color.clear;
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
                float alpha = Mathf.Lerp(0.45f, 1f, t);

                foreach (PlayerProgressRow row in _rows)
                {
                    for (int i = 0; i < row.StepBorders.Count; i++)
                    {
                        // L'index "current" est marqué via le tag (color.r très haut + jaune)
                        // mais on identifie plus simplement via le step background.
                        if (row.IsCurrent != null && i < row.IsCurrent.Length && row.IsCurrent[i])
                        {
                            Color c = _currentColor;
                            c.a = alpha;
                            row.StepBorders[i].color = c;

                            // Petit scale pulse sur la step elle-même
                            RectTransform stepRt = row.Steps[i].GetComponent<RectTransform>();
                            float scale = 1f + (alpha - 0.45f) * 0.12f;
                            stepRt.localScale = Vector3.one * scale;
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
            public bool[]? IsCurrent;

            public PlayerProgressRow(int index, TextMeshProUGUI? name,
                List<Image> steps, List<TextMeshProUGUI> stepTexts, List<Image> stepBorders)
            {
                PlayerIndex = index;
                NameText = name;
                Steps = steps;
                StepTexts = stepTexts;
                StepBorders = stepBorders;
                IsCurrent = new bool[steps.Count];
                if (steps.Count > 0) IsCurrent[0] = true;
            }
        }
    }
}
