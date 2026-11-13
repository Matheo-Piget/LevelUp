using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;

namespace LevelUp.UI
{
    /// <summary>Un siège du salon tel que l'UI l'affiche.</summary>
    public readonly struct LobbySeatView
    {
        public readonly string Name;
        public readonly bool IsHost;
        public readonly bool IsYou;

        public LobbySeatView(string name, bool isHost, bool isYou)
        {
            Name = name;
            IsHost = isHost;
            IsYou = isYou;
        }
    }

    /// <summary>État du salon pour l'affichage (rempli par le bootstrapper depuis la session réseau).</summary>
    public sealed class LobbyViewModel
    {
        public readonly List<LobbySeatView> Members = new();
        public int Seats = 4;
        public int BotDifficulty = 1;
        public MatchRules Rules = MatchRules.Classic;
    }

    /// <summary>
    /// Panel multijoueur en ligne : pseudo, créer une partie (code à partager),
    /// rejoindre via code, salon (joueurs présents + règles de l'hôte).
    /// Purement UI — aucune dépendance réseau : le bootstrapper écoute les
    /// événements et pilote la session.
    /// </summary>
    public class OnlinePanel : MonoBehaviour
    {
        private const float PanelWidth = 760f;
        private const float Inner = PanelWidth - 88f;
        private const int MaxSeats = 4;

        private enum PanelState { Home, Join, Busy, HostLobby, ClientLobby }

        private GameObject? _veil;
        private RectTransform? _panel;
        private CanvasGroup? _cg;
        private RectTransform? _content;
        private TextMeshProUGUI? _subtitle;
        private TextMeshProUGUI? _statusText;
        private TMP_InputField? _nameInput;
        private RectTransform? _playersList;
        private TextMeshProUGUI? _playersTitle;
        private TextMeshProUGUI? _rulesText;
        private readonly List<Image> _busyDots = new();
        private PanelState _screen = PanelState.Home;
        private bool _isVisible;
        private LobbyViewModel? _lobby;
        private float _time;

        /// <summary>Dernier code rejoint, pré-rempli à la saisie (revenir après un crash).</summary>
        public string LastCode { get; set; } = "";

        /// <summary>Le joueur veut héberger une partie (pseudo déjà enregistré).</summary>
        public event Action? CreateRequested;

        /// <summary>Le joueur veut rejoindre la partie au code donné (pseudo déjà enregistré).</summary>
        public event Action<string>? JoinRequested;

        /// <summary>L'hôte lance la partie.</summary>
        public event Action? LaunchRequested;

        /// <summary>L'hôte veut modifier les réglages de la partie.</summary>
        public event Action? SettingsRequested;

        /// <summary>Le panel se ferme (retour menu) — quitter la session si besoin.</summary>
        public event Action? Closed;

        public bool IsVisible => _isVisible;

        /// <summary>Construit le panel sous le parent donné. Masqué par défaut.</summary>
        public void Setup(RectTransform parent)
        {
            if (parent == null) return;
            UISkin.EnsureFonts();

            _veil = new GameObject("OnlineVeil", typeof(RectTransform), typeof(Image), typeof(Button));
            _veil.transform.SetParent(parent, false);
            UISkin.Stretch((RectTransform)_veil.transform);
            _veil.GetComponent<Image>().color = new Color(0.04f, 0.055f, 0.07f, 0.86f);
            Button vBtn = _veil.GetComponent<Button>();
            vBtn.transition = Selectable.Transition.None;
            // Toucher à côté ferme l'accueil, mais jamais un salon (on quitterait la partie).
            vBtn.onClick.AddListener(() =>
            {
                if (_screen == PanelState.Home || _screen == PanelState.Join) Hide();
            });

            Image panel = UISkin.Rect(_veil.transform, "OnlinePanel", UISkin.Surface, 28f, raycast: true);
            _panel = panel.rectTransform;
            PortraitFit.Wrap(_panel);
            UISkin.Place(_panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(PanelWidth, 0f));
            UISkin.Outline(_panel, 28f, 1f, new Color(1f, 1f, 1f, 0.08f))
                .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UISkin.VCol(panel.gameObject, 20f, new RectOffset(44, 44, 38, 36), TextAnchor.UpperLeft);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _cg = panel.gameObject.AddComponent<CanvasGroup>();

            // En-tête : EN LIGNE + sous-titre | fermer
            GameObject header = UIControls.Row(panel.transform, "Header", Inner, 64f);
            GameObject titles = UIControls.Column(header.transform, "Titles", Inner - 64f, 4f);
            TextMeshProUGUI title = UISkin.Text(titles.transform, "Title", "EN LIGNE", 34f, UISkin.TextColor,
                TextWeight.Display);
            title.characterSpacing = 2f;
            _subtitle = UISkin.Text(titles.transform, "Subtitle", "", 15f, UISkin.TextMuted);
            GameScreenLayout.IconButton(header.transform, "BtnClose", UIIcon.Close, 20f, "Fermer", Hide);

            GameObject content = new("Content", typeof(RectTransform));
            content.transform.SetParent(panel.transform, false);
            _content = (RectTransform)content.transform;
            UISkin.VCol(content, 14f, null, TextAnchor.UpperLeft);
            UISkin.Size(content, Inner);

            _statusText = UIControls.Paragraph(panel.transform, "Status", "", Inner, 60f, 15f,
                UISkin.Hex(0xE79A86), TextAlignmentOptions.TopLeft);
            _statusText.richText = true;
            _statusText.overflowMode = TextOverflowModes.Overflow;
            _statusText.gameObject.SetActive(false);

            _cg.alpha = 0f;
            _veil.SetActive(false);
        }

        private void Update()
        {
            if (!_isVisible || _busyDots.Count == 0) return;
            _time += Time.unscaledDeltaTime;
            for (int i = 0; i < _busyDots.Count; i++)
            {
                if (_busyDots[i] == null) continue;
                float a = 0.25f + 0.75f * Mathf.Max(0f, Mathf.Sin(_time * 4f - i * 0.9f));
                _busyDots[i].color = UISkin.WithAlpha(UISkin.AccentLight, a);
            }
        }

        // ────────────────────────────────────────────────────
        //  ÉCRANS
        // ────────────────────────────────────────────────────

        /// <summary>Accueil : pseudo puis créer ou rejoindre.</summary>
        public void ShowHome()
        {
            if (_content == null) return;
            BeginScreen(PanelState.Home, "Joue avec tes amis, où qu'ils soient");

            UIControls.SectionLabel(_content, "TON PSEUDO", Inner);
            _nameInput = UIInputs.NameField(_content, "NameInput", Inner, 64f);
            _nameInput.text = PlayerProfile.HasCustomName ? PlayerProfile.Name : "";

            Spacer(_content, 6f);

            Button create = UIFactory.CreateButton(_content, "BtnCreate", "CRÉER UNE PARTIE",
                new Vector2(Inner, 68f), () => { if (CommitName()) CreateRequested?.Invoke(); }, primary: true);
            UISkin.Size(create.gameObject, Inner, 68f);
            Button join = UIFactory.CreateButton(_content, "BtnJoin", "REJOINDRE AVEC UN CODE",
                new Vector2(Inner, 60f), () => { if (CommitName()) ShowJoinEntry(); });
            UISkin.Size(join.gameObject, Inner, 60f);

            UIControls.Paragraph(_content, "Hint",
                "L'un crée la partie et partage son code, les autres le saisissent. " +
                "Fonctionne en Wi-Fi comme en 4G/5G.", Inner, 44f, 14f);
        }

        /// <summary>Saisie du code pour rejoindre.</summary>
        public void ShowJoinEntry()
        {
            if (_content == null) return;
            BeginScreen(PanelState.Join, $"Tu rejoins en tant que {PlayerProfile.Name}");

            UIControls.SectionLabel(_content, "CODE DE LA PARTIE", Inner);
            TMP_InputField input = UIInputs.TextField(_content, "CodeInput", "CODE", Inner, 92f, 44f, 8,
                TextWeight.Display, TextAlignmentOptions.Center, 14f);
            input.onValidateInput = (string _, int _, char c) =>
                char.IsLetterOrDigit(c) ? char.ToUpperInvariant(c) : '\0';
            if (!string.IsNullOrEmpty(LastCode)) input.text = LastCode;

            void Submit()
            {
                string code = input.text.Trim().ToUpperInvariant();
                if (code.Length >= 4)
                {
                    LastCode = code;
                    JoinRequested?.Invoke(code);
                }
                else
                {
                    SetStatus("Code trop court : il fait 6 caractères.");
                }
            }
            input.onSubmit.AddListener(_ => Submit());

            Spacer(_content, 4f);
            Button go = UIFactory.CreateButton(_content, "BtnJoinGo", "REJOINDRE",
                new Vector2(Inner, 68f), Submit, primary: true);
            UISkin.Size(go.gameObject, Inner, 68f);
            Button back = UIFactory.CreateButton(_content, "BtnBack", "RETOUR",
                new Vector2(Inner, 56f), ShowHome, subtle: true);
            UISkin.Size(back.gameObject, Inner, 56f);
        }

        /// <summary>Opération en cours (connexion aux services, création…).</summary>
        public void ShowBusy(string message)
        {
            if (_content == null) return;
            BeginScreen(PanelState.Busy, "Un instant…");

            GameObject box = new("Busy", typeof(RectTransform));
            box.transform.SetParent(_content, false);
            UISkin.VCol(box, 18f, new RectOffset(0, 0, 24, 24), TextAnchor.MiddleCenter);
            UISkin.Size(box, Inner);

            GameObject dots = new("Dots", typeof(RectTransform));
            dots.transform.SetParent(box.transform, false);
            UISkin.HRow(dots, 10f, null, TextAnchor.MiddleCenter);
            UISkin.Size(dots, Inner, 14f);
            for (int i = 0; i < 3; i++)
            {
                Image d = UISkin.Rect(dots.transform, $"Dot{i}", UISkin.AccentLight, 6f);
                UISkin.Size(d.gameObject, 12f, 12f);
                _busyDots.Add(d);
            }

            TextMeshProUGUI text = UISkin.Text(box.transform, "Message", message, 18f, UISkin.TextColor,
                TextWeight.Medium, TextAlignmentOptions.Center);
            UISkin.Size(text.gameObject, Inner, 26f);
        }

        /// <summary>Hôte : code à partager, joueurs présents, règles et lancement.</summary>
        public void ShowHostLobby(string joinCode)
        {
            if (_content == null) return;
            BeginScreen(PanelState.HostLobby, "Ton salon est ouvert");

            BuildCodeBox(joinCode, big: true);
            BuildLobbyBody();

            GameObject buttons = UIControls.Row(_content, "Buttons", Inner, 68f, 14f);
            Button settings = UIFactory.CreateButton(buttons.transform, "BtnSettings", "PARAMÈTRES",
                new Vector2(240f, 60f), () => SettingsRequested?.Invoke());
            UISkin.Size(settings.gameObject, 240f, 60f);
            Button launch = UIFactory.CreateButton(buttons.transform, "BtnHostLaunch", "LANCER LA PARTIE",
                new Vector2(Inner - 254f, 68f), () => LaunchRequested?.Invoke(), primary: true);
            UISkin.Size(launch.gameObject, Inner - 254f, 68f);

            UIControls.Paragraph(_content, "Hint",
                "Garde le jeu ouvert : c'est ton appareil qui héberge la partie.", Inner, 20f, 13f);

            if (_lobby != null) UpdateLobby(_lobby);
        }

        /// <summary>Client : connecté, en attente que l'hôte lance.</summary>
        public void ShowClientWaiting()
        {
            if (_content == null) return;
            BeginScreen(PanelState.ClientLobby, "Connecté au salon");

            if (!string.IsNullOrEmpty(LastCode)) BuildCodeBox(LastCode, big: false);
            BuildLobbyBody();

            GameObject waiting = UIControls.Row(_content, "Waiting", Inner, 30f, 12f);
            waiting.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < 3; i++)
            {
                Image d = UISkin.Rect(waiting.transform, $"Dot{i}", UISkin.AccentLight, 4f);
                UISkin.Size(d.gameObject, 8f, 8f);
                _busyDots.Add(d);
            }
            UISkin.Text(waiting.transform, "Text", "En attente du lancement par l'hôte", 16f, UISkin.TextColor,
                TextWeight.Medium);

            if (_lobby != null) UpdateLobby(_lobby);
        }

        /// <summary>Met à jour la liste des joueurs et les règles affichées (salon hôte ou client).</summary>
        public void UpdateLobby(LobbyViewModel lobby)
        {
            _lobby = lobby;
            if (_playersList == null || _playersTitle == null || _rulesText == null) return;

            for (int i = _playersList.childCount - 1; i >= 0; i--)
            {
                Destroy(_playersList.GetChild(i).gameObject);
            }

            int seats = Mathf.Clamp(Mathf.Max(lobby.Seats, lobby.Members.Count), 2, MaxSeats);
            _playersTitle.text = $"JOUEURS · {lobby.Members.Count}/{seats}";

            for (int i = 0; i < lobby.Members.Count; i++) PlayerRow(i, lobby.Members[i]);
            for (int s = lobby.Members.Count; s < seats; s++) EmptySeatRow(s);

            _rulesText.text = MatchRulesText.Summary(lobby.Rules, lobby.BotDifficulty);
        }

        /// <summary>Affiche un message (erreur / info) sans changer d'écran.</summary>
        public void SetStatus(string message)
        {
            if (_statusText == null) return;
            _statusText.text = message;
            _statusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        // ────────────────────────────────────────────────────
        //  AFFICHAGE
        // ────────────────────────────────────────────────────

        public void Show()
        {
            if (_veil == null || _panel == null || _cg == null) return;
            _veil.SetActive(true);
            _veil.transform.SetAsLastSibling();
            ShowHome();
            UIBackStack.Push(this, OnBack);
            if (_isVisible) return;
            _isVisible = true;
            _panel.localScale = Vector3.one * 0.94f;
            _cg.alpha = 0f;
            UITween.ScaleTo(_panel.gameObject, _panel, Vector3.one, 0.22f);
            UITween.FadeTo(_panel.gameObject, _cg, 1f, 0.18f);
        }

        public void Hide()
        {
            if (_veil == null) return;
            HideSilent();
            Closed?.Invoke();
        }

        /// <summary>Bouton Retour : saisie du code → accueil ; sinon fermer (quitte le salon).</summary>
        private void OnBack()
        {
            if (_screen == PanelState.Join)
            {
                ShowHome();
                UIBackStack.Push(this, OnBack);
                return;
            }
            if (_screen == PanelState.Busy)
            {
                UIBackStack.Push(this, OnBack); // connexion en cours : on attend
                return;
            }
            Hide();
        }

        /// <summary>Masque sans déclencher Closed (lancement de partie).</summary>
        public void HideSilent()
        {
            if (_veil == null) return;
            UIBackStack.Remove(this);
            _veil.SetActive(false);
            _isVisible = false;
            _lobby = null;
            _nameInput?.DeactivateInputField();
        }

        // ────────────────────────────────────────────────────
        //  BRIQUES
        // ────────────────────────────────────────────────────

        private void BeginScreen(PanelState screen, string subtitle)
        {
            _screen = screen;
            ClearContent();
            SetStatus("");
            if (_subtitle != null) _subtitle.text = subtitle;
        }

        private void ClearContent()
        {
            if (_content == null) return;
            _nameInput = null;
            _playersList = null;
            _playersTitle = null;
            _rulesText = null;
            _busyDots.Clear();
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                Destroy(_content.GetChild(i).gameObject);
            }
        }

        /// <summary>Enregistre le pseudo saisi ; false (et message) s'il est vide.</summary>
        private bool CommitName()
        {
            if (_nameInput == null) return true;
            string clean = PlayerProfile.Sanitize(_nameInput.text);
            if (clean.Length == 0)
            {
                SetStatus("Choisis un pseudo : c'est le nom que verront tes amis à la table.");
                _nameInput.ActivateInputField();
                return false;
            }
            PlayerProfile.Name = clean;
            return true;
        }

        private static void Spacer(Transform parent, float height)
        {
            GameObject s = new("Spacer", typeof(RectTransform));
            s.transform.SetParent(parent, false);
            UISkin.Size(s, 10f, height);
        }

        /// <summary>Pastille du code : toucher = copier (pratique pour l'envoyer par message).</summary>
        private void BuildCodeBox(string joinCode, bool big)
        {
            if (_content == null) return;

            Image box = UISkin.Rect(_content, "CodeBox", UISkin.Base, 18f, raycast: true);
            UISkin.Size(box.gameObject, Inner, big ? 104f : 64f);
            UISkin.HRow(box.gameObject, 18f, new RectOffset(26, 18, 10, 10), TextAnchor.MiddleLeft);
            UISkin.Outline(box.rectTransform, 18f, 1f, UISkin.WithAlpha(UISkin.Accent, 0.35f))
                .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            GameObject texts = new("Texts", typeof(RectTransform));
            texts.transform.SetParent(box.transform, false);
            UISkin.VCol(texts, 2f, null, TextAnchor.MiddleLeft);
            UISkin.Size(texts, Inner - 26f - 18f - 18f - 140f);
            TextMeshProUGUI cap = UISkin.Text(texts.transform, "Caption",
                big ? "CODE À PARTAGER" : "CODE DE LA PARTIE", 12f, UISkin.TextMuted, TextWeight.SemiBold);
            cap.characterSpacing = 8f;
            TextMeshProUGUI code = UISkin.Text(texts.transform, "Code", joinCode, big ? 52f : 30f,
                UISkin.AccentLight, TextWeight.Display);
            code.characterSpacing = big ? 16f : 10f;

            GameObject copy = new("Copy", typeof(RectTransform));
            copy.transform.SetParent(box.transform, false);
            UISkin.HRow(copy, 8f, null, TextAnchor.MiddleRight);
            UISkin.Size(copy, 140f, 40f);
            UISkin.Text(copy.transform, "Label", "Copier", 15f, UISkin.TextMuted, TextWeight.SemiBold,
                TextAlignmentOptions.MidlineRight);
            Image icon = UISkin.IconImage(copy.transform, "Icon", UIIcon.Copy, 20f, UISkin.TextMuted);
            UISkin.Size(icon.gameObject, 20f, 20f);

            Button btn = box.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                GUIUtility.systemCopyBuffer = joinCode;
                SetStatus("<color=#5FB98A>Code copié ! Envoie-le à tes amis.</color>");
            });
        }

        /// <summary>Titre "JOUEURS", liste des sièges, résumé des règles.</summary>
        private void BuildLobbyBody()
        {
            if (_content == null) return;

            _playersTitle = UIControls.SectionLabel(_content, "JOUEURS", Inner);

            GameObject list = new("Players", typeof(RectTransform));
            list.transform.SetParent(_content, false);
            UISkin.VCol(list, 8f, null, TextAnchor.UpperLeft);
            UISkin.Size(list, Inner);
            _playersList = (RectTransform)list.transform;

            _rulesText = UIControls.Paragraph(_content, "Rules", "", Inner, 40f, 14f);
        }

        private void PlayerRow(int seat, LobbySeatView member)
        {
            if (_playersList == null) return;

            Image row = UISkin.Rect(_playersList, $"Seat{seat}",
                member.IsYou ? UISkin.WithAlpha(UISkin.Accent, 0.08f) : UISkin.Glass, 16f);
            UISkin.Size(row.gameObject, Inner, 56f);
            UISkin.HRow(row.gameObject, 14f, new RectOffset(10, 16, 8, 8), TextAnchor.MiddleLeft);
            UISkin.Outline(row.rectTransform, 16f, 1f,
                    member.IsYou ? UISkin.WithAlpha(UISkin.AccentLight, 0.45f) : UISkin.Line)
                .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            Color tint = UISkin.AvatarTints[seat % UISkin.AvatarTints.Length];
            Image avatar = UISkin.Rect(row.transform, "Avatar", tint, 12f);
            UISkin.Size(avatar.gameObject, 40f, 40f);
            TextMeshProUGUI initial = UISkin.Text(avatar.transform, "Initial", PlayerProfile.Initial(member.Name),
                17f, UISkin.Base, TextWeight.Display, TextAlignmentOptions.Center);
            UISkin.Stretch(initial.rectTransform);

            TextMeshProUGUI name = UISkin.Text(row.transform, "Name", member.Name, 17f, UISkin.TextColor,
                TextWeight.SemiBold);
            UISkin.Size(name.gameObject, -1f, -1f, 1f);

            if (member.IsHost) Badge(row.transform, "Hôte", filled: true);
            if (member.IsYou) Badge(row.transform, "Toi", filled: false);
        }

        private void EmptySeatRow(int seat)
        {
            if (_playersList == null) return;

            Image row = UISkin.Rect(_playersList, $"Seat{seat}", new Color(1f, 1f, 1f, 0.015f), 16f);
            UISkin.Size(row.gameObject, Inner, 56f);
            UISkin.HRow(row.gameObject, 14f, new RectOffset(10, 16, 8, 8), TextAnchor.MiddleLeft);
            UISkin.Outline(row.rectTransform, 16f, 1f, new Color(1f, 1f, 1f, 0.05f))
                .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            Image avatar = UISkin.Rect(row.transform, "Avatar", UISkin.SurfaceRaised, 12f);
            UISkin.Size(avatar.gameObject, 40f, 40f);
            TextMeshProUGUI plus = UISkin.Text(avatar.transform, "Plus", "+", 22f, UISkin.TextMuted,
                TextWeight.SemiBold, TextAlignmentOptions.Center);
            UISkin.Stretch(plus.rectTransform);

            UISkin.Text(row.transform, "Name", "Place libre · un bot jouera si personne ne vient", 15f,
                UISkin.TextMuted);
        }

        private static void Badge(Transform parent, string text, bool filled)
        {
            Image b = UISkin.Rect(parent, "Badge", filled ? UISkin.AccentWashStrong : new Color(0f, 0f, 0f, 0f), 10f);
            UISkin.HRow(b.gameObject, 0f, new RectOffset(10, 10, 4, 4), TextAnchor.MiddleCenter);
            UISkin.Outline(b.rectTransform, 10f, 1f, UISkin.WithAlpha(UISkin.AccentLight, filled ? 0.35f : 0.5f))
                .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UISkin.Text(b.transform, "Text", text, 13f, UISkin.AccentPale, TextWeight.SemiBold);
        }
    }
}
