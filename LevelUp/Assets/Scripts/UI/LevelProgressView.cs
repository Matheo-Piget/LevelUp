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
  /// Barre des joueurs (maquette "Écran de jeu") : une pastille par joueur,
  /// centrées en haut de l'écran sous la top bar.
  ///   [avatar initiale] Nom / statut   [▭ cartes en main]  [Niv. N]
  /// Joueur actif : anneau accent 2px + statut "Joue…".
  /// Cartes en main ≤ 2 : la puce passe en orange (danger de fin de manche).
  /// API publique inchangée (Initialize / BindPlayers / UpdatePlayerLevel…).
  /// </summary>
  public class LevelProgressView : MonoBehaviour
  {
    [SerializeField] private RectTransform? _container;
    [SerializeField] private float _playerRowHeight = 100f;
    [SerializeField] private AnimationController? _animController;

    private readonly List<PlayerPill> _pills = new();
    private int _activeIndex = -1;
    private IReadOnlyList<PlayerModel>? _players;
    private RectTransform? _row;

    private void OnEnable()
    {
      EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
      EventBus.Subscribe<LevelLaidDownEvent>(OnLevelLaidDown);
      EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
      EventBus.Subscribe<HandChangedEvent>(OnHandChanged);
      EventBus.Subscribe<ForcedDrawEvent>(OnForcedDraw);
      EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
    }

    private void OnDisable()
    {
      EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
      EventBus.Unsubscribe<LevelLaidDownEvent>(OnLevelLaidDown);
      EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
      EventBus.Unsubscribe<HandChangedEvent>(OnHandChanged);
      EventBus.Unsubscribe<ForcedDrawEvent>(OnForcedDraw);
      EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
    }

    /// <summary>Lie les modèles joueurs (cartes en main, niveau posé).</summary>
    public void BindPlayers(IReadOnlyList<PlayerModel> players)
    {
      _players = players;
      RefreshAll(players);
      RefreshCardCounts();
      RefreshStatuses();
    }

    public void Initialize(int playerCount, List<string> playerNames)
    {
      ClearPills();
      RectTransform host = _container != null ? _container : (RectTransform)transform;
      _ = _playerRowHeight;

      // Le conteneur de scène ne sert plus de fond : la rangée est autonome.
      Image? oldBg = host.GetComponent<Image>();
      if (oldBg != null) oldBg.enabled = false;
      VerticalLayoutGroup? oldVlg = host.GetComponent<VerticalLayoutGroup>();
      if (oldVlg != null) oldVlg.enabled = false;
      for (int i = host.childCount - 1; i >= 0; i--)
      {
        Transform child = host.GetChild(i);
        if (child.name != "PlayersRow") child.gameObject.SetActive(false);
      }

      if (_row == null)
      {
        GameObject rowObj = new("PlayersRow", typeof(RectTransform));
        rowObj.transform.SetParent(host, false);
        _row = (RectTransform)rowObj.transform;
        UISkin.Stretch(_row);
        UISkin.HRow(rowObj, 20f, null, TextAnchor.MiddleCenter);
      }

      for (int p = 0; p < playerCount; p++)
      {
        string name = p < playerNames.Count ? playerNames[p] : $"Joueur {p + 1}";
        _pills.Add(CreatePill(_row, p, name));
      }

      SetActivePlayer(0);
    }

    private PlayerPill CreatePill(RectTransform parent, int index, string name)
    {
      Image bg = UISkin.Rect(parent, $"Player_{index}", UISkin.Glass, 18f);
      GameObject obj = bg.gameObject;
      UISkin.HRow(obj, 14f, new RectOffset(10, 18, 10, 10), TextAnchor.MiddleLeft);
      UISkin.Size(obj, -1f, 64f);
      Image ring = UISkin.Outline(bg.rectTransform, 18f, 1f, UISkin.Line, "Ring");
      ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

      // Avatar
      Color tint = UISkin.AvatarTints[index % UISkin.AvatarTints.Length];
      Image avatar = UISkin.Rect(obj.transform, "Avatar", tint, 13f);
      UISkin.Size(avatar.gameObject, 44f, 44f);
      TextMeshProUGUI initial = UISkin.Text(avatar.transform, "Initial", Initial(name), 18f,
          UISkin.Base, TextWeight.Display, TextAlignmentOptions.Center);
      UISkin.Stretch(initial.rectTransform);

      // Nom + statut
      GameObject texts = new("Texts", typeof(RectTransform));
      texts.transform.SetParent(obj.transform, false);
      UISkin.VCol(texts, 2f, null, TextAnchor.MiddleLeft);
      TextMeshProUGUI nameText = UISkin.Text(texts.transform, "Name", name, 15f, UISkin.TextColor,
          TextWeight.SemiBold);
      TextMeshProUGUI statusText = UISkin.Text(texts.transform, "Status", "En attente", 13f,
          UISkin.TextMuted);
      UISkin.Size(texts, 112f);

      // Cartes en main
      Image countChip = UISkin.Rect(obj.transform, "Cards", UISkin.SurfaceRaised, 10f);
      UISkin.HRow(countChip.gameObject, 6f, new RectOffset(10, 10, 6, 6), TextAnchor.MiddleCenter);
      Image countIcon = UISkin.IconImage(countChip.transform, "Icon", UIIcon.Cards, 14f, UISkin.TextColor);
      UISkin.Size(countIcon.gameObject, 14f, 14f);
      TextMeshProUGUI countText = UISkin.Text(countChip.transform, "Count", "0", 13f, UISkin.TextColor,
          TextWeight.SemiBold);

      // Niveau
      Image levelChip = UISkin.Rect(obj.transform, "Level", UISkin.WithAlpha(UISkin.Accent, 0.14f), 10f);
      UISkin.HRow(levelChip.gameObject, 0f, new RectOffset(10, 10, 6, 6), TextAnchor.MiddleCenter);
      TextMeshProUGUI levelText = UISkin.Text(levelChip.transform, "Text", "Niv. 1", 13f,
          UISkin.AccentPale, TextWeight.SemiBold);

      return new PlayerPill
      {
        Index = index,
        Root = bg,
        Ring = ring,
        Name = nameText,
        Status = statusText,
        CountChip = countChip,
        CountIcon = countIcon,
        CountText = countText,
        LevelText = levelText
      };
    }

    private static string Initial(string name)
    {
      if (string.IsNullOrEmpty(name)) return "?";
      char last = name[name.Length - 1];
      // "Bot 2" → "2" ; sinon première lettre.
      if (char.IsDigit(last)) return last.ToString();
      return char.ToUpperInvariant(name[0]).ToString();
    }

    public void UpdatePlayerLevel(int playerIndex, int currentLevel)
    {
      if (playerIndex < 0 || playerIndex >= _pills.Count) return;
      PlayerPill pill = _pills[playerIndex];
      if (pill.Level == currentLevel) return;
      bool changed = pill.Level > 0;
      pill.Level = currentLevel;
      pill.LevelText.text = $"Niv. {currentLevel}";
      if (changed && _animController != null) _animController.AnimatePulse(pill.LevelText.rectTransform);
    }

    /// <summary>Joueur actif : anneau accent 2px, statut "Joue…".</summary>
    public void SetActivePlayer(int playerIndex)
    {
      _activeIndex = playerIndex;
      for (int i = 0; i < _pills.Count; i++)
      {
        PlayerPill pill = _pills[i];
        bool active = i == playerIndex;
        UISkin.SetStroke(pill.Ring, 18f, active ? 2f : 1f);
        pill.Ring.color = active ? UISkin.AccentLight : UISkin.Line;
        pill.Root.color = active ? UISkin.WithAlpha(UISkin.Accent, 0.08f) : UISkin.Glass;
      }
      RefreshStatuses();
    }

    private void RefreshStatuses()
    {
      for (int i = 0; i < _pills.Count; i++)
      {
        PlayerPill pill = _pills[i];
        string status;
        if (i == _activeIndex) status = "Joue…";
        else if (_players != null && i < _players.Count && _players[i].HasLaidDownThisRound) status = "Niveau posé";
        else status = "En attente";
        if (pill.Status.text != status) pill.Status.text = status;
        pill.Status.color = i == _activeIndex ? UISkin.AccentPale : UISkin.TextMuted;
      }
    }

    private void OnTurnStarted(TurnStartedEvent evt)
    {
      SetActivePlayer(evt.PlayerIndex);
      UpdatePlayerLevel(evt.PlayerIndex, evt.PlayerLevel);
      // Filet de sécurité : mutations de main sans HandChangedEvent (+2 forcé).
      RefreshCardCounts();
    }

    private void OnLevelCompleted(LevelCompletedEvent evt)
    {
      UpdatePlayerLevel(evt.PlayerIndex, evt.Level + 1);
    }

    private void OnLevelLaidDown(LevelLaidDownEvent evt) => RefreshStatuses();

    private void OnHandChanged(HandChangedEvent evt)
    {
      SetCardCount(evt.PlayerIndex, evt.NewHand.Count);
    }

    private void OnForcedDraw(ForcedDrawEvent evt)
    {
      if (_players != null && evt.PlayerIndex >= 0 && evt.PlayerIndex < _players.Count)
      {
        SetCardCount(evt.PlayerIndex, _players[evt.PlayerIndex].Hand.Count);
      }
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
      // La distribution se termine juste après l'événement : lecture une frame plus tard.
      if (isActiveAndEnabled) StartCoroutine(RefreshNextFrame());
    }

    private IEnumerator RefreshNextFrame()
    {
      yield return null;
      RefreshCardCounts();
      RefreshStatuses();
    }

    private void RefreshCardCounts()
    {
      if (_players == null) return;
      for (int i = 0; i < _players.Count && i < _pills.Count; i++)
      {
        SetCardCount(i, _players[i].Hand.Count);
      }
    }

    /// <summary>Puce "cartes en main" — orange quand le joueur est près de finir.</summary>
    private void SetCardCount(int playerIndex, int count)
    {
      if (playerIndex < 0 || playerIndex >= _pills.Count) return;
      PlayerPill pill = _pills[playerIndex];
      if (pill.LastCount == count) return;
      pill.LastCount = count;

      bool danger = count <= 2;
      Color orange = Constants.CardOrange;
      pill.CountText.text = count.ToString();
      pill.CountText.color = danger ? Color.Lerp(orange, Color.white, 0.25f) : UISkin.TextColor;
      pill.CountIcon.color = danger ? orange : UISkin.TextColor;
      pill.CountChip.color = danger ? UISkin.WithAlpha(orange, 0.16f) : UISkin.SurfaceRaised;

      if (danger && _animController != null) _animController.AnimatePulse(pill.CountText.rectTransform);
    }

    public void RefreshAll(IReadOnlyList<PlayerModel> players)
    {
      for (int i = 0; i < players.Count && i < _pills.Count; i++)
      {
        UpdatePlayerLevel(i, players[i].CurrentLevel);
      }
    }

    private void ClearPills()
    {
      foreach (PlayerPill pill in _pills)
      {
        if (pill.Root != null) Destroy(pill.Root.gameObject);
      }
      _pills.Clear();
    }

    private class PlayerPill
    {
      public int Index;
      public Image Root = null!;
      public Image Ring = null!;
      public TextMeshProUGUI Name = null!;
      public TextMeshProUGUI Status = null!;
      public Image CountChip = null!;
      public Image CountIcon = null!;
      public TextMeshProUGUI CountText = null!;
      public TextMeshProUGUI LevelText = null!;
      public int Level;
      public int LastCount = -1;
    }
  }
}
