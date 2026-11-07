using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Panel multijoueur en ligne : créer une partie (affiche le code à partager),
    /// rejoindre via code, salle d'attente. Purement UI — aucune dépendance
    /// réseau : le bootstrapper écoute les événements et pilote la session.
    /// </summary>
    public class OnlinePanel : MonoBehaviour
    {
        private RectTransform? _panel;
        private RectTransform? _content;
        private GameObject? _veil;
        private TextMeshProUGUI? _statusText;
        private TextMeshProUGUI? _lobbyCountText;
        private bool _isVisible;

        /// <summary>Dernier code rejoint, pré-rempli à la saisie (revenir après un crash).</summary>
        public string LastCode { get; set; } = "";

        /// <summary>Le joueur veut héberger une partie.</summary>
        public event Action? CreateRequested;

        /// <summary>Le joueur veut rejoindre la partie au code donné.</summary>
        public event Action<string>? JoinRequested;

        /// <summary>L'hôte lance la partie.</summary>
        public event Action? LaunchRequested;

        /// <summary>Le panel se ferme (retour menu) — quitter la session si besoin.</summary>
        public event Action? Closed;

        /// <summary>Construit le panel sous le parent donné. Masqué par défaut.</summary>
        public void Setup(RectTransform parent)
        {
            if (parent == null) return;

            // Veil sombre plein écran
            _veil = new GameObject("OnlineVeil", typeof(RectTransform), typeof(Image), typeof(Button));
            _veil.transform.SetParent(parent, false);
            RectTransform vrt = _veil.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.sizeDelta = Vector2.zero;
            Image vImg = _veil.GetComponent<Image>();
            vImg.color = new Color(0f, 0f, 0f, 0.65f);
            Button vBtn = _veil.GetComponent<Button>();
            vBtn.transition = Selectable.Transition.None;
            vBtn.onClick.AddListener(Hide);

            // Panel central
            GameObject panelObj = UIFactory.CreatePanel(_veil.transform, "OnlinePanel",
                Constants.PanelBackground, new Vector2(580f, 660f),
                withBorder: true, borderColor: Constants.CardBlue);
            _panel = panelObj.GetComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.anchoredPosition = Vector2.zero;
            Image blocker = panelObj.GetComponent<Image>();
            if (blocker != null) blocker.raycastTarget = true;
            UIFactory.AddDropShadow(_panel, 10f, 0.5f);

            // Titre
            TextMeshProUGUI title = UIFactory.CreateText(_panel, "Title", "EN LIGNE", 38f,
                Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform trt = title.rectTransform;
            trt.anchorMin = new Vector2(0.5f, 1f);
            trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(500f, 60f);
            trt.anchoredPosition = new Vector2(0f, -30f);
            title.characterSpacing = 14f;
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(
                Color.Lerp(Constants.CardBlue, Color.white, 0.3f),
                Color.Lerp(Constants.CardBlue, Color.white, 0.3f),
                Color.Lerp(Constants.CardGreen, Color.white, 0.2f),
                Color.Lerp(Constants.CardGreen, Color.white, 0.2f));

            // Séparateur
            GameObject sep = new("Separator", typeof(RectTransform), typeof(Image));
            sep.transform.SetParent(_panel, false);
            RectTransform srt = sep.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 1f);
            srt.anchorMax = new Vector2(0.5f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.sizeDelta = new Vector2(460f, 2f);
            srt.anchoredPosition = new Vector2(0f, -95f);
            Image sImg = sep.GetComponent<Image>();
            sImg.color = new Color(Constants.CardBlue.r, Constants.CardBlue.g, Constants.CardBlue.b, 0.5f);
            sImg.raycastTarget = false;

            // Zone de contenu reconstruite selon l'état
            GameObject content = new("Content", typeof(RectTransform));
            content.transform.SetParent(_panel, false);
            _content = content.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0.5f, 1f);
            _content.anchorMax = new Vector2(0.5f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.sizeDelta = new Vector2(500f, 420f);
            _content.anchoredPosition = new Vector2(0f, -120f);

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            // Ligne de statut (erreurs / info)
            _statusText = UIFactory.CreateText(_panel, "Status", "", 17f,
                new Color(1f, 0.65f, 0.5f, 0.95f), TextAlignmentOptions.Center, FontStyles.Normal);
            RectTransform strt = _statusText.rectTransform;
            strt.anchorMin = new Vector2(0.5f, 0f);
            strt.anchorMax = new Vector2(0.5f, 0f);
            strt.pivot = new Vector2(0.5f, 0f);
            // Assez haut pour un message d'erreur détaillé (3-4 lignes)
            strt.sizeDelta = new Vector2(540f, 96f);
            strt.anchoredPosition = new Vector2(0f, 95f);

            // Bouton fermer
            Button closeBtn = UIFactory.CreateButton(_panel, "BtnClose", "RETOUR",
                new Vector2(240f, 60f), Hide, Constants.CardRed);
            RectTransform closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 30f);
            closeBtn.transform.SetAsLastSibling();

            _veil.SetActive(false);
        }

        // ────────────────────────────────────────────────────
        //  ÉTATS
        // ────────────────────────────────────────────────────

        /// <summary>Écran d'accueil : créer ou rejoindre.</summary>
        public void ShowHome()
        {
            if (_content == null) return;
            ClearContent();
            SetStatus("");

            AddHint("Jouez avec vos amis, où qu'ils soient.\nL'un crée la partie, les autres entrent son code.");

            UIFactory.CreateButton(_content, "BtnCreate", "CRÉER UNE PARTIE",
                new Vector2(420f, 74f), () => CreateRequested?.Invoke(), Constants.CardGreen);

            UIFactory.CreateButton(_content, "BtnJoin", "REJOINDRE AVEC UN CODE",
                new Vector2(420f, 74f), ShowJoinEntry, Constants.CardBlue);
        }

        /// <summary>Opération en cours (connexion aux services, création…).</summary>
        public void ShowBusy(string message)
        {
            if (_content == null) return;
            ClearContent();
            SetStatus("");
            AddHint(message);
        }

        /// <summary>Hôte : code à partager + nombre de joueurs + bouton lancer.</summary>
        public void ShowHostLobby(string joinCode, int humanCount)
        {
            if (_content == null) return;
            ClearContent();
            SetStatus("");

            AddHint("Partagez ce code (touchez-le pour le copier) :");

            // Code de partie en très gros, dans une pastille
            GameObject codeBox = new("CodeBox", typeof(RectTransform), typeof(Image));
            codeBox.transform.SetParent(_content, false);
            RectTransform cbrt = codeBox.GetComponent<RectTransform>();
            cbrt.sizeDelta = new Vector2(420f, 110f);
            Image cbImg = codeBox.GetComponent<Image>();
            cbImg.sprite = UIFactory.RoundedSprite;
            cbImg.type = Image.Type.Sliced;
            cbImg.color = new Color(0f, 0f, 0f, 0.35f);
            // Toucher le code le copie : pratique pour l'envoyer par message.
            cbImg.raycastTarget = true;
            Button copyBtn = codeBox.AddComponent<Button>();
            copyBtn.transition = Selectable.Transition.None;
            copyBtn.onClick.AddListener(() =>
            {
                GUIUtility.systemCopyBuffer = joinCode;
                SetStatus("<color=#5FB98A>Code copié !</color>");
            });

            TextMeshProUGUI codeText = UIFactory.CreateText(codeBox.transform, "Code",
                joinCode, 56f, Constants.CardYellow, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform ctrt = codeText.rectTransform;
            ctrt.anchorMin = Vector2.zero;
            ctrt.anchorMax = Vector2.one;
            ctrt.sizeDelta = Vector2.zero;
            codeText.characterSpacing = 18f;

            _lobbyCountText = UIFactory.CreateText(_content, "LobbyCount", "", 22f,
                Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            _lobbyCountText.rectTransform.sizeDelta = new Vector2(460f, 34f);
            SetLobbyCount(humanCount);

            UIFactory.CreateButton(_content, "BtnLaunch", "LANCER LA PARTIE",
                new Vector2(420f, 74f), () => LaunchRequested?.Invoke(), Constants.CardGreen);

            AddHint("Places libres = bots. Gardez le jeu ouvert :\nc'est votre appareil qui héberge la partie.");
        }

        /// <summary>Met à jour le compteur de joueurs du lobby hôte.</summary>
        public void SetLobbyCount(int humanCount)
        {
            if (_lobbyCountText != null)
            {
                _lobbyCountText.text = humanCount <= 1
                    ? "En attente des joueurs…"
                    : $"{humanCount} joueurs connectés";
            }
        }

        /// <summary>Saisie du code pour rejoindre.</summary>
        public void ShowJoinEntry()
        {
            if (_content == null) return;
            ClearContent();
            SetStatus("");

            AddHint("Entrez le code de partie de votre ami :");

            TMP_InputField input = CreateCodeInput(_content);
            if (!string.IsNullOrEmpty(LastCode)) input.text = LastCode;

            UIFactory.CreateButton(_content, "BtnJoinGo", "REJOINDRE",
                new Vector2(420f, 74f), () =>
                {
                    string code = input.text.Trim();
                    if (code.Length >= 4)
                    {
                        JoinRequested?.Invoke(code);
                    }
                    else
                    {
                        SetStatus("Code trop court");
                    }
                }, Constants.CardBlue);
        }

        /// <summary>Client : connecté, en attente que l'hôte lance.</summary>
        public void ShowClientWaiting()
        {
            if (_content == null) return;
            ClearContent();
            SetStatus("");
            AddHint("Connecté !\n\nEn attente du lancement par l'hôte…");
        }

        /// <summary>Affiche un message d'erreur sans changer d'écran.</summary>
        public void SetStatus(string message)
        {
            if (_statusText != null) _statusText.text = message;
        }

        // ────────────────────────────────────────────────────
        //  AFFICHAGE
        // ────────────────────────────────────────────────────

        public bool IsVisible => _isVisible;

        public void Show()
        {
            if (_veil == null) return;
            _veil.SetActive(true);
            _veil.transform.SetAsLastSibling();
            _isVisible = true;
            ShowHome();
        }

        public void Hide()
        {
            if (_veil == null) return;
            _veil.SetActive(false);
            _isVisible = false;
            Closed?.Invoke();
        }

        /// <summary>Masque sans déclencher Closed (lancement de partie).</summary>
        public void HideSilent()
        {
            if (_veil == null) return;
            _veil.SetActive(false);
            _isVisible = false;
        }

        // ────────────────────────────────────────────────────
        //  HELPERS
        // ────────────────────────────────────────────────────

        private void ClearContent()
        {
            if (_content == null) return;
            _lobbyCountText = null;
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                Destroy(_content.GetChild(i).gameObject);
            }
        }

        private void AddHint(string text)
        {
            if (_content == null) return;
            TextMeshProUGUI hint = UIFactory.CreateText(_content, "Hint", text, 19f,
                new Color(1f, 1f, 1f, 0.75f), TextAlignmentOptions.Center, FontStyles.Normal);
            hint.rectTransform.sizeDelta = new Vector2(470f, 80f);
        }

        /// <summary>
        /// Champ de saisie du code construit à la main (le projet n'utilise aucun
        /// prefab). Majuscules forcées, 8 caractères max, clavier natif sur mobile.
        /// </summary>
        private static TMP_InputField CreateCodeInput(Transform parent)
        {
            GameObject fieldObj = new("CodeInput", typeof(RectTransform), typeof(Image));
            fieldObj.transform.SetParent(parent, false);
            RectTransform frt = fieldObj.GetComponent<RectTransform>();
            frt.sizeDelta = new Vector2(420f, 90f);
            Image fImg = fieldObj.GetComponent<Image>();
            fImg.sprite = UIFactory.RoundedSprite;
            fImg.type = Image.Type.Sliced;
            fImg.color = new Color(0f, 0f, 0f, 0.4f);

            // Zone de texte masquée (viewport)
            GameObject areaObj = new("TextArea", typeof(RectTransform), typeof(RectMask2D));
            areaObj.transform.SetParent(fieldObj.transform, false);
            RectTransform art = areaObj.GetComponent<RectTransform>();
            art.anchorMin = Vector2.zero;
            art.anchorMax = Vector2.one;
            art.offsetMin = new Vector2(20f, 10f);
            art.offsetMax = new Vector2(-20f, -10f);

            TextMeshProUGUI placeholder = UIFactory.CreateText(areaObj.transform, "Placeholder",
                "CODE", 40f, new Color(1f, 1f, 1f, 0.25f), TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform prt = placeholder.rectTransform;
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = UIFactory.CreateText(areaObj.transform, "Text",
                "", 40f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform txrt = text.rectTransform;
            txrt.anchorMin = Vector2.zero;
            txrt.anchorMax = Vector2.one;
            txrt.sizeDelta = Vector2.zero;
            text.characterSpacing = 12f;

            TMP_InputField input = fieldObj.AddComponent<TMP_InputField>();
            input.textViewport = art;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 8;
            input.onValidateInput = (string _, int _, char c) =>
                char.IsLetterOrDigit(c) ? char.ToUpperInvariant(c) : '\0';

            return input;
        }
    }
}
