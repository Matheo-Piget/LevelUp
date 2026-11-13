using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Écran de fin de partie — overlay avec titre (victoire du joueur local ou
    /// partie terminée), classement final par niveau, confettis si l'on gagne,
    /// boutons Rejouer/Menu principal. Remplace le panel basique de HUDView.
    /// </summary>
    public class GameOverCelebration : MonoBehaviour
    {
        private Canvas? _canvas;
        private RectTransform? _root;
        private CanvasGroup? _rootCg;
        private RectTransform? _panel;
        private TextMeshProUGUI? _replayLabel;

        /// <summary>Vrai si « Rejouer » est un vote (partie en ligne) : l'écran ne se ferme pas.</summary>
        public Func<bool>? ReplayIsVote { get; set; }

        /// <summary>Texte du bouton Rejouer (« REVANCHE 2/3 » pendant un vote en ligne).</summary>
        public void SetReplayLabel(string text)
        {
            if (_replayLabel != null) _replayLabel.text = text;
        }

        /// <summary>Ferme l'écran sans rien déclencher (la revanche démarre).</summary>
        public void Dismiss() => Hide();
        private bool _isShown;

        /// <summary>Déclenché quand le joueur clique sur Rejouer.</summary>
        public event Action? OnReplayClicked;

        /// <summary>Déclenché quand le joueur clique sur Menu principal.</summary>
        public event Action? OnMainMenuClicked;

        /// <summary>Noms des joueurs (pour afficher "Bot 2 remporte la partie").</summary>
        public System.Collections.Generic.List<string>? PlayerNames { get; set; }

        /// <summary>Joueurs de la partie (classement final). Même liste que le GameManager.</summary>
        public System.Collections.Generic.IReadOnlyList<PlayerModel>? Players { get; set; }

        /// <summary>Manches jouées (affichées sous le classement).</summary>
        public System.Func<int>? RoundsPlayed { get; set; }

        /// <summary>
        /// Prépare l'overlay sous le canvas. Écoute GameOverEvent pour s'afficher.
        /// </summary>
        public void Setup(Canvas canvas)
        {
            if (canvas == null) return;
            _canvas = canvas;

            GameObject rootObj = new("GameOverOverlay", typeof(RectTransform), typeof(CanvasGroup));
            rootObj.transform.SetParent(canvas.transform, false);
            rootObj.transform.SetAsLastSibling();

            _root = rootObj.GetComponent<RectTransform>();
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.sizeDelta = Vector2.zero;

            _rootCg = rootObj.GetComponent<CanvasGroup>();
            _rootCg.alpha = 0f;
            _rootCg.blocksRaycasts = false;

            _root.gameObject.SetActive(false);

            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            StopAllCoroutines();
        }

        /// <summary>
        /// Une nouvelle partie démarre (ex: replay lancé par l'hôte en ligne) :
        /// fermer l'overlay s'il est encore affiché sur cet appareil.
        /// </summary>
        private void OnGameStarted(GameStartedEvent evt)
        {
            if (_isShown) Hide();
        }

        private void OnGameOver(GameOverEvent evt)
        {
            Show(evt.WinnerIndex);
        }

        /// <summary>
        /// Affiche l'écran de fin de partie pour le gagnant donné.
        /// </summary>
        public void Show(int winnerIndex)
        {
            if (_isShown || _root == null || _rootCg == null) return;
            _isShown = true;

            _root.gameObject.SetActive(true);
            _rootCg.alpha = 0f;
            _rootCg.blocksRaycasts = true;

            // Reconstruire à chaque fois pour permettre replay
            ClearChildren(_root);

            // Voile sombre
            GameObject veil = new("Veil", typeof(RectTransform), typeof(Image));
            veil.transform.SetParent(_root, false);
            UISkin.Stretch((RectTransform)veil.transform);
            veil.GetComponent<Image>().color = new Color(0.04f, 0.055f, 0.07f, 0.86f);

            // Classement : du plus avancé au moins avancé, le vainqueur en tête.
            System.Collections.Generic.List<PlayerModel> ranking = new();
            int localIndex = -1;
            if (Players != null)
            {
                ranking.AddRange(Players);
                ranking.Sort((a, b) =>
                {
                    if (a.Index == winnerIndex) return -1;
                    if (b.Index == winnerIndex) return 1;
                    int byLevel = b.CurrentLevel.CompareTo(a.CurrentLevel);
                    if (byLevel != 0) return byLevel;
                    // À niveau égal : le moins de points de pénalité devant.
                    int byPenalty = a.PenaltyPoints.CompareTo(b.PenaltyPoints);
                    return byPenalty != 0 ? byPenalty : a.Index.CompareTo(b.Index);
                });
                foreach (PlayerModel p in Players)
                {
                    if (GameSession.IsLocalHuman(p)) { localIndex = p.Index; break; }
                }
            }
            bool localWon = localIndex >= 0 && localIndex == winnerIndex;
            bool celebrate = localWon || localIndex < 0;
            float rowsHeight = ranking.Count * 50f;

            // Panneau (maquette : #161B22 radius 28 + liseré)
            Image panel = UISkin.Rect(_root, "GOPanel", UISkin.Surface, 28f, raycast: true);
            _panel = panel.rectTransform;
            PortraitFit.Wrap(_panel);
            UISkin.Place(_panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(640f, 400f + rowsHeight + (ranking.Count > 0 ? 40f : 0f)));
            UISkin.Outline(_panel, 28f, 1f, new Color(1f, 1f, 1f, 0.08f));

            // Étoile (symbole jaune de la DA) dans une pastille — atténuée en cas de défaite
            Color badgeTint = celebrate ? Constants.CardYellow : UISkin.TextMuted;
            Image badge = UISkin.Rect(_panel, "TrophyBadge", UISkin.WithAlpha(badgeTint, 0.12f), 28f);
            RectTransform troRt = badge.rectTransform;
            UISkin.Place(troRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -88f),
                new Vector2(104f, 104f));
            UISkin.Outline(troRt, 28f, 1.5f, UISkin.WithAlpha(badgeTint, 0.45f));
            Image star = UISkin.SuitImage(badge.transform, "Star", CardColor.Yellow, 60f, badgeTint);
            UISkin.Place(star.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(60f, 60f));

            // Titre
            TextMeshProUGUI victory = UISkin.Text(_panel, "Victory", celebrate ? "VICTOIRE !" : "PARTIE TERMINÉE",
                celebrate ? 54f : 46f, UISkin.TextColor, TextWeight.Display, TextAlignmentOptions.Center);
            UISkin.Place(victory.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -160f), new Vector2(580f, 70f));
            victory.characterSpacing = 2f;

            // Gagnant
            string winnerName = PlayerNames != null && winnerIndex >= 0 && winnerIndex < PlayerNames.Count
                ? PlayerNames[winnerIndex]
                : $"Joueur {winnerIndex + 1}";
            string winnerLine = localWon
                ? $"Bravo {winnerName}, tu remportes la partie"
                : $"{winnerName} remporte la partie";
            TextMeshProUGUI winnerLabel = UISkin.Text(_panel, "Winner", winnerLine, 18f, UISkin.TextMuted,
                TextWeight.SemiBold, TextAlignmentOptions.Center);
            UISkin.Place(winnerLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -236f), new Vector2(600f, 26f));

            BuildRanking(ranking, winnerIndex, localIndex);

            // Boutons : une action principale (REJOUER) + MENU
            GameObject row = new("Buttons", typeof(RectTransform));
            row.transform.SetParent(_panel, false);
            UISkin.Place((RectTransform)row.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 44f), new Vector2(552f, 64f));
            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            _replayLabel = UIFactory.CreateButton(row.transform, "BtnReplay", "REJOUER",
                new Vector2(268f, 64f), HandleReplay, primary: true).GetComponentInChildren<TextMeshProUGUI>();
            UIFactory.CreateButton(row.transform, "BtnMenu", "MENU",
                new Vector2(268f, 64f), HandleMainMenu);

            // Animations d'entrée
            _panel.localScale = Vector3.zero;
            UITween.ScaleTo(_root.gameObject, _panel, Vector3.one, 0.6f);
            UITween.FadeTo(_root.gameObject, _rootCg, 1f, 0.4f);

            // Trophée qui bounce
            StartCoroutine(TrophyBounce(troRt));

            // Confettis : seulement pour le gagnant (une défaite n'en mérite pas)
            if (celebrate) StartCoroutine(ConfettiBurst());
        }

        /// <summary>Lignes "1  [A] Alice   Terminé" sous le titre, puis le nombre de manches.</summary>
        private void BuildRanking(System.Collections.Generic.List<PlayerModel> ranking, int winnerIndex, int localIndex)
        {
            if (_panel == null || ranking.Count == 0) return;

            GameObject list = new("Ranking", typeof(RectTransform));
            list.transform.SetParent(_panel, false);
            RectTransform lrt = (RectTransform)list.transform;
            UISkin.Place(lrt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -280f),
                new Vector2(520f, ranking.Count * 50f));
            UISkin.VCol(list, 6f, null, TextAnchor.UpperLeft);

            int cap = GameSession.Rules.LevelsToWin;
            for (int rank = 0; rank < ranking.Count; rank++)
            {
                PlayerModel p = ranking[rank];
                bool winner = p.Index == winnerIndex;

                Image row = UISkin.Rect(list.transform, $"Rank{rank + 1}",
                    winner ? UISkin.WithAlpha(UISkin.Accent, 0.10f) : UISkin.Glass, 14f);
                UISkin.Size(row.gameObject, 520f, 44f);
                UISkin.HRow(row.gameObject, 12f, new RectOffset(14, 16, 5, 5), TextAnchor.MiddleLeft);

                TextMeshProUGUI pos = UISkin.Text(row.transform, "Pos", (rank + 1).ToString(), 18f,
                    winner ? UISkin.AccentLight : UISkin.TextMuted, TextWeight.Display, TextAlignmentOptions.Center);
                UISkin.Size(pos.gameObject, 22f);

                Color tint = UISkin.AvatarTints[p.Index % UISkin.AvatarTints.Length];
                Image avatar = UISkin.Rect(row.transform, "Avatar", tint, 10f);
                UISkin.Size(avatar.gameObject, 32f, 32f);
                TextMeshProUGUI initial = UISkin.Text(avatar.transform, "Initial", PlayerProfile.Initial(p.Name), 14f,
                    UISkin.Base, TextWeight.Display, TextAlignmentOptions.Center);
                UISkin.Stretch(initial.rectTransform);

                string name = p.Index == localIndex ? $"{p.Name} <color=#A7A198>(toi)</color>" : p.Name;
                TextMeshProUGUI nameText = UISkin.Text(row.transform, "Name", name, 16f, UISkin.TextColor,
                    TextWeight.SemiBold);
                nameText.richText = true;
                UISkin.Size(nameText.gameObject, -1f, -1f, 1f);

                string level = p.CurrentLevel > cap ? "Terminé" : $"Niveau {p.CurrentLevel}";
                TextMeshProUGUI penalty = UISkin.Text(row.transform, "Penalty", $"{p.PenaltyPoints} pts", 14f,
                    UISkin.TextMuted, TextWeight.Medium, TextAlignmentOptions.MidlineRight);
                UISkin.Size(penalty.gameObject, 70f);
                TextMeshProUGUI levelText = UISkin.Text(row.transform, "Level", level, 15f,
                    winner ? UISkin.AccentPale : UISkin.TextMuted, TextWeight.SemiBold, TextAlignmentOptions.MidlineRight);
                UISkin.Size(levelText.gameObject, 90f);
            }

            int rounds = RoundsPlayed?.Invoke() ?? 0;
            if (rounds > 0)
            {
                TextMeshProUGUI meta = UISkin.Text(_panel, "Meta",
                    $"{rounds} manche{(rounds > 1 ? "s" : "")} jouée{(rounds > 1 ? "s" : "")}", 13f,
                    UISkin.TextMuted, TextWeight.Medium, TextAlignmentOptions.Center);
                UISkin.Place(meta.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -280f - ranking.Count * 50f - 6f), new Vector2(520f, 20f));
            }
        }

        private IEnumerator TrophyBounce(RectTransform rt)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            float t = 0f;
            Vector3 baseScale = Vector3.one;
            while (_isShown && rt != null)
            {
                t += Time.unscaledDeltaTime;
                float s = 1f + Mathf.Sin(t * 3f) * 0.08f;
                float rot = Mathf.Sin(t * 2f) * 8f;
                rt.localScale = baseScale * s;
                rt.localRotation = Quaternion.Euler(0, 0, rot);
                yield return null;
            }
        }

        private IEnumerator ConfettiBurst()
        {
            if (_canvas == null) yield break;

            Color[] palette =
            {
                Constants.CardRed, Constants.CardBlue, Constants.CardGreen,
                Constants.CardYellow, Constants.CardPurple, Constants.CardOrange
            };

            // 4 vagues successives de confettis
            for (int wave = 0; wave < 4; wave++)
            {
                SpawnConfettiWave(palette, 30);
                yield return new WaitForSecondsRealtime(0.8f);
            }
        }

        private void SpawnConfettiWave(Color[] palette, int count)
        {
            if (_root == null) return;

            // Dimensions écran
            Rect canvasRect = _root.rect;
            float halfW = canvasRect.width * 0.5f;
            float halfH = canvasRect.height * 0.5f;

            for (int i = 0; i < count; i++)
            {
                GameObject c = new($"Confetti_{i}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                c.transform.SetParent(_root, false);
                c.transform.SetAsLastSibling();

                RectTransform rt = c.GetComponent<RectTransform>();
                float startX = UnityEngine.Random.Range(-halfW, halfW);
                rt.anchoredPosition = new Vector2(startX, halfH + 30f);
                float size = UnityEngine.Random.Range(8f, 18f);
                rt.sizeDelta = new Vector2(size, size * UnityEngine.Random.Range(0.4f, 1.2f));
                rt.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f));

                Image img = c.GetComponent<Image>();
                img.sprite = UIFactory.RoundedSprite;
                img.type = Image.Type.Sliced;
                img.color = palette[UnityEngine.Random.Range(0, palette.Length)];
                img.raycastTarget = false;

                StartCoroutine(AnimateConfetti(c, rt, c.GetComponent<CanvasGroup>(), -halfH - 50f));
            }
        }

        private IEnumerator AnimateConfetti(GameObject obj, RectTransform rt, CanvasGroup cg, float minY)
        {
            float fallSpeed = UnityEngine.Random.Range(200f, 400f);
            float swayAmp = UnityEngine.Random.Range(30f, 80f);
            float swayFreq = UnityEngine.Random.Range(1f, 2.5f);
            float spinSpeed = UnityEngine.Random.Range(-180f, 180f);
            Vector2 startPos = rt.anchoredPosition;
            float t = 0f;
            float lifetime = UnityEngine.Random.Range(2.5f, 4f);

            while (t < lifetime && rt != null && obj != null)
            {
                t += Time.unscaledDeltaTime;
                float y = startPos.y - fallSpeed * t;
                float x = startPos.x + Mathf.Sin(t * swayFreq) * swayAmp;
                rt.anchoredPosition = new Vector2(x, y);
                rt.localRotation = Quaternion.Euler(0, 0, t * spinSpeed);

                if (cg != null && t > lifetime - 0.6f)
                {
                    cg.alpha = Mathf.Lerp(1f, 0f, (t - (lifetime - 0.6f)) / 0.6f);
                }

                if (y < minY) break;
                yield return null;
            }

            if (obj != null) Destroy(obj);
        }

        private void HandleReplay()
        {
            // En ligne : on vote pour la revanche, l'écran reste ouvert en attendant les autres.
            if (ReplayIsVote != null && ReplayIsVote())
            {
                OnReplayClicked?.Invoke();
                return;
            }
            Hide(() => OnReplayClicked?.Invoke());
        }

        private void HandleMainMenu()
        {
            Hide(() => OnMainMenuClicked?.Invoke());
        }

        private void Hide(Action? onComplete = null)
        {
            if (!_isShown || _root == null || _rootCg == null)
            {
                onComplete?.Invoke();
                return;
            }
            _isShown = false;
            _rootCg.blocksRaycasts = false;
            UITween.FadeTo(_root.gameObject, _rootCg, 0f, 0.35f, () =>
            {
                if (_root != null) _root.gameObject.SetActive(false);
                StopAllCoroutines();
                onComplete?.Invoke();
            });
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
