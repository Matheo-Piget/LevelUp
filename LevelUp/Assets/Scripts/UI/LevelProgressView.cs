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
  /// Sidebar joueurs minimaliste et aérée :
  ///   - avatar rond 52px avec anneau de progression circulaire (8 arcs)
  ///   - nom + niveau "x/8" à droite, beaucoup d'air autour
  ///   - joueur actif : point indigo + nom blanc (pas de fond, pas de bordure)
  ///
  /// Philosophie : l'avatar EST l'élément central. La progression vit autour de
  /// lui (anneau), pas en dessous. Tout le reste — nom, niveau, état actif —
  /// est typographique et minimal.
  /// </summary>
  public class LevelProgressView : MonoBehaviour
  {
    [SerializeField] private RectTransform? _container;
    [SerializeField] private float _playerRowHeight = 100f;
    [SerializeField] private AnimationController? _animController;

    // Couleurs joueur saturées : utilisées UNIQUEMENT pour l'avatar (info de jeu),
    // jamais pour le contour de l'UI — le reste reste neutre.
    private static readonly Color[] PlayerAvatarColors =
    {
            Constants.CardBlue,
            Constants.CardRed,
            Constants.CardGreen,
            Constants.CardPurple,
            Constants.CardOrange,
            Constants.CardYellow
        };

    // Sprite d'arc d'anneau, généré une seule fois et partagé entre toutes les rangées.
    // 8 segments × 6 joueurs = 48 sprites sinon, autant en mutualiser un.
    private static Sprite? _ringSegmentSprite;

    private readonly List<PlayerRow> _rows = new();
    private int _activeIndex = -1;

    // Référence lecture seule vers les joueurs de la partie en cours :
    // permet d'afficher le nombre de cartes en main de chacun (info clé
    // pour anticiper qui est sur le point de finir la manche).
    private IReadOnlyList<PlayerModel>? _players;

    private void OnEnable()
    {
      EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
      EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
      EventBus.Subscribe<HandChangedEvent>(OnHandChanged);
      EventBus.Subscribe<ForcedDrawEvent>(OnForcedDraw);
      EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
    }

    private void OnDisable()
    {
      EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
      EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
      EventBus.Unsubscribe<HandChangedEvent>(OnHandChanged);
      EventBus.Unsubscribe<ForcedDrawEvent>(OnForcedDraw);
      EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
    }

    /// <summary>
    /// Lie les modèles joueurs pour lire leur nombre de cartes en main.
    /// À appeler après <see cref="Initialize"/> à chaque (re)démarrage de partie.
    /// </summary>
    public void BindPlayers(IReadOnlyList<PlayerModel> players)
    {
      _players = players;
      RefreshCardCounts();
    }

    public void Initialize(int playerCount, List<string> playerNames)
    {
      ClearRows();
      if (_container == null) return;

      EnsureContainerLayout(_container);
      CreateHeader(_container, playerCount);

      for (int p = 0; p < playerCount; p++)
      {
        _rows.Add(CreatePlayerRow(p, playerNames[p]));
      }

      SetActivePlayer(0);
    }

    private static void EnsureContainerLayout(RectTransform container)
    {
      VerticalLayoutGroup vlg = container.GetComponent<VerticalLayoutGroup>();
      if (vlg == null) vlg = container.gameObject.AddComponent<VerticalLayoutGroup>();
      // Espacement généreux entre rangées : c'est ce qui fait "respirer" la sidebar
      vlg.spacing = 30f;
      vlg.padding = new RectOffset(20, 20, 28, 28);
      vlg.childAlignment = TextAnchor.UpperCenter;
      vlg.childControlWidth = true;
      vlg.childControlHeight = true;
      vlg.childForceExpandWidth = true;
      vlg.childForceExpandHeight = false;

      Image bg = container.GetComponent<Image>();
      if (bg == null) bg = container.gameObject.AddComponent<Image>();
      bg.sprite = UIFactory.RoundedSprite;
      bg.type = Image.Type.Sliced;
      bg.color = Constants.SurfaceB;
      bg.raycastTarget = false;
    }

    /// <summary>"JOUEURS" — label discret en tête, sans le compte (info redondante).</summary>
    private static void CreateHeader(RectTransform container, int playerCount)
    {
      GameObject headerObj = new("Header",
          typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
      headerObj.transform.SetParent(container, false);

      LayoutElement le = headerObj.GetComponent<LayoutElement>();
      le.minHeight = 40f;
      le.preferredHeight = 40f;

      TextMeshProUGUI tmp = headerObj.GetComponent<TextMeshProUGUI>();
      tmp.text = "JOUEURS";
      tmp.fontSize = 10f;
      tmp.characterSpacing = 8f; // tracking large pour un look "label" épuré
      tmp.color = Constants.TextMuted;
      tmp.alignment = TextAlignmentOptions.Left;
      tmp.fontStyle = FontStyles.Bold;
      tmp.margin = new Vector4(4f, 0f, 0f, 16f);
      tmp.raycastTarget = false;
    }

    private PlayerRow CreatePlayerRow(int playerIndex, string playerName)
    {
      // Conteneur de la rangée — entièrement transparent, aucun fond ni bordure.
      // L'état actif sera signalé typographiquement, pas par un cadre.
      GameObject rowObj = new($"Row_P{playerIndex}",
          typeof(RectTransform), typeof(LayoutElement));
      rowObj.transform.SetParent(_container, false);

      LayoutElement rowLe = rowObj.GetComponent<LayoutElement>();
      rowLe.minHeight = _playerRowHeight;
      rowLe.preferredHeight = _playerRowHeight;

      RectTransform rowRt = rowObj.GetComponent<RectTransform>();

      // ─── Bloc avatar + anneau (gauche) ─────────────────────────────────
      // L'avatar fait 52px, l'anneau autour ajoute ~12px de plus → 64px total
      const float avatarSize = 52f;
      const float ringSize = 64f;

      GameObject avatarBlock = new("AvatarBlock", typeof(RectTransform));
      avatarBlock.transform.SetParent(rowObj.transform, false);
      RectTransform abRt = avatarBlock.GetComponent<RectTransform>();
      abRt.anchorMin = new Vector2(0f, 0.5f);
      abRt.anchorMax = new Vector2(0f, 0.5f);
      abRt.pivot = new Vector2(0f, 0.5f);
      abRt.sizeDelta = new Vector2(ringSize, ringSize);
      abRt.anchoredPosition = new Vector2(8f, 0f);

      // Anneau de progression : 8 segments d'arc disposés sur 360°
      List<Image> ringSegments = CreateProgressRing(abRt, ringSize);

      // Avatar rond au centre du bloc
      GameObject avatarObj = new("Avatar",
          typeof(RectTransform), typeof(Image));
      avatarObj.transform.SetParent(avatarBlock.transform, false);

      RectTransform avRt = avatarObj.GetComponent<RectTransform>();
      avRt.anchorMin = new Vector2(0.5f, 0.5f);
      avRt.anchorMax = new Vector2(0.5f, 0.5f);
      avRt.pivot = new Vector2(0.5f, 0.5f);
      avRt.sizeDelta = new Vector2(avatarSize, avatarSize);
      avRt.anchoredPosition = Vector2.zero;

      Color avatarColor = PlayerAvatarColors[playerIndex % PlayerAvatarColors.Length];
      Image avImg = avatarObj.GetComponent<Image>();
      avImg.sprite = UIFactory.RoundedSprite;
      avImg.type = Image.Type.Sliced;
      avImg.color = avatarColor;
      avImg.raycastTarget = false;

      // Initiale dans l'avatar
      GameObject initialObj = new("Initial",
          typeof(RectTransform), typeof(TextMeshProUGUI));
      initialObj.transform.SetParent(avatarObj.transform, false);
      RectTransform irt = initialObj.GetComponent<RectTransform>();
      irt.anchorMin = Vector2.zero;
      irt.anchorMax = Vector2.one;
      irt.sizeDelta = Vector2.zero;

      TextMeshProUGUI initialText = initialObj.GetComponent<TextMeshProUGUI>();
      initialText.text = AvatarInitial(playerIndex, playerName);
      initialText.fontSize = 18f;
      initialText.color = IsLightAvatar(avatarColor)
          ? new Color32(0x1F, 0x29, 0x37, 0xFF) : Color.white;
      initialText.alignment = TextAlignmentOptions.Center;
      initialText.fontStyle = FontStyles.Bold;
      initialText.raycastTarget = false;

      // ─── Bloc texte (droite) ───────────────────────────────────────────
      // Empile : nom (haut) + niveau "x/8" (bas), aligné verticalement au centre
      GameObject textBlock = new("TextBlock",
          typeof(RectTransform), typeof(VerticalLayoutGroup));
      textBlock.transform.SetParent(rowObj.transform, false);

      RectTransform tbRt = textBlock.GetComponent<RectTransform>();
      tbRt.anchorMin = new Vector2(0f, 0.5f);
      tbRt.anchorMax = new Vector2(1f, 0.5f);
      tbRt.pivot = new Vector2(0f, 0.5f);
      // Décalé après le bloc avatar (8 + 64 + 16 = 88px de marge gauche)
      tbRt.offsetMin = new Vector2(88f, -28f);
      tbRt.offsetMax = new Vector2(-12f, 28f);

      VerticalLayoutGroup tbVlg = textBlock.GetComponent<VerticalLayoutGroup>();
      tbVlg.spacing = 4f;
      tbVlg.padding = new RectOffset(0, 0, 0, 0);
      tbVlg.childAlignment = TextAnchor.MiddleLeft;
      tbVlg.childControlWidth = true;
      tbVlg.childControlHeight = true;
      tbVlg.childForceExpandWidth = true;
      tbVlg.childForceExpandHeight = false;

      // Ligne 1 : nom du joueur
      GameObject nameObj = new("Name",
          typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
      nameObj.transform.SetParent(textBlock.transform, false);
      LayoutElement nameLe = nameObj.GetComponent<LayoutElement>();
      nameLe.preferredHeight = 22f;

      TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
      nameText.text = playerName;
      nameText.fontSize = 15f;
      nameText.color = Constants.TextSecondary;
      nameText.alignment = TextAlignmentOptions.Left;
      nameText.fontStyle = FontStyles.Bold;
      nameText.raycastTarget = false;

      // Ligne 2 : piste des niveaux "1 2 3 4 5 6 7 8" — le niveau courant est
      // surligné, les niveaux passés en indigo, les suivants estompés. On voit
      // d'un coup d'œil qui est à quel niveau.
      GameObject levelObj = new("LevelTrack",
          typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
      levelObj.transform.SetParent(textBlock.transform, false);
      LayoutElement levelLe = levelObj.GetComponent<LayoutElement>();
      levelLe.preferredHeight = 20f;

      TextMeshProUGUI levelText = levelObj.GetComponent<TextMeshProUGUI>();
      levelText.text = BuildLevelTrack(1);
      levelText.fontSize = 13f;
      levelText.color = Constants.TextMuted;
      levelText.alignment = TextAlignmentOptions.MidlineLeft;
      levelText.fontStyle = FontStyles.Bold;
      levelText.richText = true;
      levelText.enableWordWrapping = false;
      levelText.raycastTarget = false;

      // ─── Indicateur "actif" ────────────────────────────────────────────
      // Barre d'accent verticale collée au bord gauche de la rangée —
      // invisible par défaut. Immédiatement repérable, sans surcharger.
      GameObject dotObj = new("ActiveBar",
          typeof(RectTransform), typeof(Image));
      dotObj.transform.SetParent(rowObj.transform, false);
      RectTransform dotRt = dotObj.GetComponent<RectTransform>();
      dotRt.anchorMin = new Vector2(0f, 0.5f);
      dotRt.anchorMax = new Vector2(0f, 0.5f);
      dotRt.pivot = new Vector2(1f, 0.5f);
      dotRt.sizeDelta = new Vector2(4f, 56f);
      dotRt.anchoredPosition = new Vector2(-6f, 0f);

      Image dotImg = dotObj.GetComponent<Image>();
      dotImg.sprite = UIFactory.RoundedSprite;
      dotImg.type = Image.Type.Sliced;
      dotImg.color = new Color(0f, 0f, 0f, 0f);
      dotImg.raycastTarget = false;

      // ─── Pastille "cartes en main" ─────────────────────────────────────
      // Pill alignée sur la ligne du nom (haut-droite) : fond + bordure +
      // mini dos de carte incliné + compteur. L'info d'anticipation clé.
      GameObject countObj = new("CardCountPill", typeof(RectTransform));
      countObj.transform.SetParent(rowObj.transform, false);
      RectTransform ccRt = countObj.GetComponent<RectTransform>();
      ccRt.anchorMin = new Vector2(1f, 0.5f);
      ccRt.anchorMax = new Vector2(1f, 0.5f);
      ccRt.pivot = new Vector2(1f, 0.5f);
      ccRt.sizeDelta = new Vector2(54f, 26f);
      // Centrée sur la ligne du nom (~+17 dans une rangée de 100px)
      ccRt.anchoredPosition = new Vector2(-12f, 16f);

      GameObject pillBgObj = new("Bg", typeof(RectTransform), typeof(Image));
      pillBgObj.transform.SetParent(countObj.transform, false);
      RectTransform pbRt = pillBgObj.GetComponent<RectTransform>();
      pbRt.anchorMin = Vector2.zero;
      pbRt.anchorMax = Vector2.one;
      pbRt.sizeDelta = Vector2.zero;
      Image pillBg = pillBgObj.GetComponent<Image>();
      pillBg.sprite = UIFactory.RoundedSprite;
      pillBg.type = Image.Type.Sliced;
      pillBg.color = Constants.PillIndigoBg;
      pillBg.raycastTarget = false;

      GameObject pillBorderObj = new("Border", typeof(RectTransform), typeof(Image));
      pillBorderObj.transform.SetParent(countObj.transform, false);
      RectTransform pbrRt = pillBorderObj.GetComponent<RectTransform>();
      pbrRt.anchorMin = Vector2.zero;
      pbrRt.anchorMax = Vector2.one;
      pbrRt.sizeDelta = Vector2.zero;
      Image pillBorder = pillBorderObj.GetComponent<Image>();
      pillBorder.sprite = UIFactory.RingSprite;
      pillBorder.type = Image.Type.Sliced;
      pillBorder.color = Constants.PillIndigoBorder;
      pillBorder.raycastTarget = false;

      // Mini dos de carte légèrement incliné, façon carte posée sur la table
      GameObject miniObj = new("MiniCard", typeof(RectTransform), typeof(Image));
      miniObj.transform.SetParent(countObj.transform, false);
      RectTransform mcRt = miniObj.GetComponent<RectTransform>();
      mcRt.anchorMin = new Vector2(0f, 0.5f);
      mcRt.anchorMax = new Vector2(0f, 0.5f);
      mcRt.pivot = new Vector2(0.5f, 0.5f);
      mcRt.sizeDelta = new Vector2(13f, 18f);
      mcRt.anchoredPosition = new Vector2(14f, 0f);
      mcRt.localRotation = Quaternion.Euler(0f, 0f, -8f);
      Image miniImg = miniObj.GetComponent<Image>();
      miniImg.sprite = UIFactory.RoundedSprite;
      miniImg.type = Image.Type.Sliced;
      miniImg.color = new Color(1f, 1f, 1f, 0.28f);
      miniImg.raycastTarget = false;

      GameObject countTextObj = new("Count",
          typeof(RectTransform), typeof(TextMeshProUGUI));
      countTextObj.transform.SetParent(countObj.transform, false);
      RectTransform ctRt = countTextObj.GetComponent<RectTransform>();
      ctRt.anchorMin = Vector2.zero;
      ctRt.anchorMax = Vector2.one;
      ctRt.offsetMin = new Vector2(24f, 0f);
      ctRt.offsetMax = new Vector2(-6f, 0f);

      TextMeshProUGUI countText = countTextObj.GetComponent<TextMeshProUGUI>();
      countText.text = "—";
      countText.fontSize = 13f;
      countText.color = Constants.TextPrimary;
      countText.alignment = TextAlignmentOptions.Midline;
      countText.fontStyle = FontStyles.Bold;
      countText.raycastTarget = false;

      return new PlayerRow(playerIndex, nameText, levelText, dotImg,
          playerName, ringSegments, countText, miniImg, pillBg, pillBorder);
    }

    /// <summary>
    /// Crée 8 segments d'arc disposés en cercle pour former l'anneau de progression.
    /// Chaque segment couvre 360/8 = 45° avec un petit gap entre eux.
    /// </summary>
    private static List<Image> CreateProgressRing(RectTransform parent, float ringSize)
    {
      List<Image> segments = new();
      EnsureRingSegmentSprite();

      // On utilise un sprite d'arc qui couvre ~42° (laisse 3° de gap par segment)
      // et on tourne chaque sprite de 45° autour du centre.
      const int segmentCount = 8; // = Constants.MaxLevel mais hardcodé pour l'anneau
      const float anglePerSegment = 360f / segmentCount;

      for (int i = 0; i < segmentCount; i++)
      {
        GameObject segObj = new($"RingSeg_{i + 1}",
            typeof(RectTransform), typeof(Image));
        segObj.transform.SetParent(parent, false);

        RectTransform rt = segObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(ringSize, ringSize);
        rt.anchoredPosition = Vector2.zero;
        // Rotation : segment 0 démarre à -90° (top) et on tourne dans le sens horaire.
        // Le sprite d'arc est dessiné centré sur 0° (top), donc on offset par i*45°.
        rt.localRotation = Quaternion.Euler(0f, 0f, -i * anglePerSegment);

        Image segImg = segObj.GetComponent<Image>();
        segImg.sprite = _ringSegmentSprite;
        segImg.color = Constants.PanelBorderSoft;
        segImg.raycastTarget = false;
        segImg.preserveAspect = true;

        segments.Add(segImg);
      }

      return segments;
    }

    /// <summary>
    /// Génère le sprite d'arc partagé : un anneau fin (3px) qui ne dessine qu'un
    /// secteur de ~42° au sommet, le reste est transparent. Texture 128×128.
    /// </summary>
    private static void EnsureRingSegmentSprite()
    {
      if (_ringSegmentSprite != null) return;

      const int size = 128;
      const float outerRadius = 62f;
      const float innerRadius = 56f; // épaisseur 6px sur 128, soit ~3px à 64px de rendu
      const float halfArcDeg = 21f;  // 42° de couverture par segment (gap = 3°)

      Texture2D tex = new(size, size, TextureFormat.RGBA32, false)
      {
        wrapMode = TextureWrapMode.Clamp,
        filterMode = FilterMode.Bilinear
      };

      Color transparent = new(1f, 1f, 1f, 0f);
      Color[] pixels = new Color[size * size];
      float cx = size * 0.5f;
      float cy = size * 0.5f;

      for (int y = 0; y < size; y++)
      {
        for (int x = 0; x < size; x++)
        {
          float dx = x - cx;
          float dy = y - cy;
          float dist = Mathf.Sqrt(dx * dx + dy * dy);

          // Hors de l'anneau → transparent
          if (dist < innerRadius || dist > outerRadius)
          {
            pixels[y * size + x] = transparent;
            continue;
          }

          // Angle en degrés, 0° = haut (north), sens horaire positif
          float angleDeg = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
          if (angleDeg < 0f) angleDeg += 360f;
          if (angleDeg > 180f) angleDeg -= 360f;

          // On garde uniquement le secteur [-halfArcDeg, +halfArcDeg]
          if (Mathf.Abs(angleDeg) > halfArcDeg)
          {
            pixels[y * size + x] = transparent;
            continue;
          }

          // Anti-aliasing doux sur les bords radiaux
          float edgeFade = 1f;
          float edgeDist = Mathf.Min(dist - innerRadius, outerRadius - dist);
          if (edgeDist < 1f) edgeFade = edgeDist;

          pixels[y * size + x] = new Color(1f, 1f, 1f, edgeFade);
        }
      }

      tex.SetPixels(pixels);
      tex.Apply();

      _ringSegmentSprite = Sprite.Create(tex,
          new Rect(0, 0, size, size),
          new Vector2(0.5f, 0.5f), 100f);
    }

    private static string AvatarInitial(int playerIndex, string playerName)
    {
      if (string.IsNullOrEmpty(playerName)) return $"P{playerIndex + 1}";
      char c = char.ToUpper(playerName[0]);
      for (int i = 0; i < playerName.Length; i++)
      {
        if (char.IsDigit(playerName[i])) return $"{c}{playerName[i]}";
      }
      return $"{c}{playerIndex + 1}";
    }

    private static bool IsLightAvatar(Color c)
    {
      float l = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
      return l > 0.65f;
    }

    // Piste de niveaux pré-construite par niveau courant (évite de re-générer
    // la même chaîne rich text à chaque tour). Index = niveau courant
    // (0 inutilisé, MaxLevel+1 = partie finie, tout complété).
    private static readonly string[] _levelTrackCache = new string[Constants.MaxLevel + 2];

    /// <summary>
    /// "1 2 3 4 5 6 7 8" : niveaux passés en indigo, courant surligné blanc
    /// sur fond indigo, suivants estompés.
    /// </summary>
    private static string BuildLevelTrack(int currentLevel)
    {
      int key = Mathf.Clamp(currentLevel, 0, Constants.MaxLevel + 1);
      if (_levelTrackCache[key] != null) return _levelTrackCache[key];

      System.Text.StringBuilder sb = new(220);
      sb.Append("<mspace=1.15em>");
      for (int lvl = 1; lvl <= Constants.MaxLevel; lvl++)
      {
        if (lvl == key)
        {
          // Surligné : blanc sur pastille indigo (tag <mark> de TMP)
          sb.Append("<mark=#6366F1B4><color=#FFFFFF>").Append(lvl).Append("</color></mark>");
        }
        else if (lvl < key)
        {
          sb.Append("<color=#818CF8>").Append(lvl).Append("</color>");
        }
        else
        {
          sb.Append("<color=#4B5563>").Append(lvl).Append("</color>");
        }
      }
      sb.Append("</mspace>");

      _levelTrackCache[key] = sb.ToString();
      return _levelTrackCache[key];
    }

    public void UpdatePlayerLevel(int playerIndex, int currentLevel)
    {
      if (playerIndex < 0 || playerIndex >= _rows.Count) return;

      PlayerRow row = _rows[playerIndex];

      // Met à jour la piste des niveaux avec le courant surligné
      if (row.LevelText != null)
      {
        row.LevelText.text = BuildLevelTrack(currentLevel);
      }

      // Anneau : segments < currentLevel = pleins, == currentLevel = en cours, > = vides
      Color completedColor = Constants.AccentPrimary;
      Color currentColor = Constants.AccentLight;
      Color pendingColor = new(1f, 1f, 1f, 0.08f);

      for (int i = 0; i < row.RingSegments.Count; i++)
      {
        int lvl = i + 1;
        Image seg = row.RingSegments[i];

        if (lvl < currentLevel)
        {
          seg.color = completedColor;
        }
        else if (lvl == currentLevel)
        {
          seg.color = currentColor;
          if (_animController != null && playerIndex == _activeIndex)
          {
            _animController.AnimatePulse(seg.rectTransform);
          }
        }
        else
        {
          seg.color = pendingColor;
        }
      }
    }

    /// <summary>
    /// Joueur actif : nom en blanc + petit point indigo à droite.
    /// Rien d'autre — pas de fond, pas de bordure. L'épure est l'élégance.
    /// </summary>
    public void SetActivePlayer(int playerIndex)
    {
      _activeIndex = playerIndex;
      for (int i = 0; i < _rows.Count; i++)
      {
        bool active = i == playerIndex;
        PlayerRow row = _rows[i];

        if (row.NameText != null)
        {
          row.NameText.color = active
              ? Constants.TextPrimary
              : Constants.TextSecondary;
        }
        // (La piste de niveaux porte ses couleurs en rich text — rien à teinter.)
        if (row.ActiveDot != null)
        {
          Color c = Constants.AccentLight;
          c.a = active ? 1f : 0f;
          row.ActiveDot.color = c;
        }
      }
    }

    private void OnTurnStarted(TurnStartedEvent evt)
    {
      SetActivePlayer(evt.PlayerIndex);
      UpdatePlayerLevel(evt.PlayerIndex, evt.PlayerLevel);
      // Filet de sécurité : rattrape les mutations de main qui ne publient
      // pas HandChangedEvent (ex. pioche forcée d'un +2 chez la victime).
      RefreshCardCounts();
    }

    private void OnLevelCompleted(LevelCompletedEvent evt)
    {
      UpdatePlayerLevel(evt.PlayerIndex, evt.Level + 1);
    }

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
      // La distribution se termine juste après la publication de l'événement :
      // on lit les mains une frame plus tard.
      StartCoroutine(RefreshCountsNextFrame());
    }

    private IEnumerator RefreshCountsNextFrame()
    {
      yield return null;
      RefreshCardCounts();
    }

    private void RefreshCardCounts()
    {
      if (_players == null) return;
      for (int i = 0; i < _players.Count && i < _rows.Count; i++)
      {
        SetCardCount(i, _players[i].Hand.Count);
      }
    }

    /// <summary>
    /// Met à jour le badge "cartes en main". Passe en orange (et pulse) quand
    /// le joueur est sur le point de finir — signal d'alerte pour les autres.
    /// </summary>
    private void SetCardCount(int playerIndex, int count)
    {
      if (playerIndex < 0 || playerIndex >= _rows.Count) return;

      PlayerRow row = _rows[playerIndex];
      if (row.CountText == null) return;

      bool danger = count <= 2;
      bool changed = row.LastCardCount != count;
      row.LastCardCount = count;

      Color orange = Constants.CardOrange;
      row.CountText.text = count.ToString();
      row.CountText.color = danger
          ? Color.Lerp(orange, Color.white, 0.25f)
          : Constants.TextPrimary;

      if (row.CountIcon != null)
      {
        row.CountIcon.color = danger
            ? new Color(orange.r, orange.g, orange.b, 0.9f)
            : new Color(1f, 1f, 1f, 0.28f);
      }
      // Toute la pastille bascule en orange : fond + bordure, pas juste le chiffre.
      if (row.CountBg != null)
      {
        row.CountBg.color = danger
            ? new Color(orange.r, orange.g, orange.b, 0.16f)
            : Constants.PillIndigoBg;
      }
      if (row.CountBorder != null)
      {
        row.CountBorder.color = danger
            ? new Color(orange.r, orange.g, orange.b, 0.5f)
            : Constants.PillIndigoBorder;
      }

      if (danger && changed && _animController != null)
      {
        _animController.AnimatePulse(row.CountText.rectTransform);
      }
    }

    public void RefreshAll(IReadOnlyList<Core.PlayerModel> players)
    {
      for (int i = 0; i < players.Count && i < _rows.Count; i++)
      {
        UpdatePlayerLevel(i, players[i].CurrentLevel);
      }
    }

    private void ClearRows()
    {
      foreach (PlayerRow row in _rows)
      {
        if (row.NameText != null && row.NameText.transform != null)
        {
          // On remonte au parent de la rangée pour tout détruire
          Transform rowTr = row.NameText.transform.parent;
          while (rowTr != null && rowTr.parent != _container)
          {
            rowTr = rowTr.parent;
          }
          if (rowTr != null) Destroy(rowTr.gameObject);
        }
      }
      _rows.Clear();
    }

    private class PlayerRow
    {
      public int PlayerIndex;
      public TextMeshProUGUI? NameText;
      public TextMeshProUGUI? LevelText;
      public Image? ActiveDot;
      public string PlayerName;
      public List<Image> RingSegments;
      public TextMeshProUGUI? CountText;
      public Image? CountIcon;
      public Image? CountBg;
      public Image? CountBorder;
      public int LastCardCount = -1;

      public PlayerRow(int index, TextMeshProUGUI? name, TextMeshProUGUI? level,
          Image? activeDot, string playerName, List<Image> ringSegments,
          TextMeshProUGUI? countText, Image? countIcon,
          Image? countBg, Image? countBorder)
      {
        PlayerIndex = index;
        NameText = name;
        LevelText = level;
        ActiveDot = activeDot;
        PlayerName = playerName;
        RingSegments = ringSegments;
        CountText = countText;
        CountIcon = countIcon;
        CountBg = countBg;
        CountBorder = countBorder;
      }
    }
  }
}
