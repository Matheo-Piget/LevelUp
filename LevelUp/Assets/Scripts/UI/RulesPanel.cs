using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// « Comment jouer » : but du jeu, déroulé d'un tour, combinaisons, contrats
    /// des 8 niveaux, progression et cartes spéciales. Ouvert depuis le menu
    /// principal et la pause ; défilement vertical (tactile ou molette).
    /// </summary>
    public class RulesPanel : MonoBehaviour
    {
        private const float PanelWidth = 900f;
        private const float Inner = PanelWidth - 88f;

        private GameObject? _veil;
        private RectTransform? _panel;
        private CanvasGroup? _cg;
        private ScrollRect? _scroll;
        private bool _visible;

        public bool IsVisible => _visible;

        public void Setup(RectTransform parent)
        {
            UISkin.EnsureFonts();

            _veil = new GameObject("RulesVeil", typeof(RectTransform), typeof(Image), typeof(Button));
            _veil.transform.SetParent(parent, false);
            UISkin.Stretch((RectTransform)_veil.transform);
            _veil.GetComponent<Image>().color = new Color(0.04f, 0.055f, 0.07f, 0.88f);
            Button vBtn = _veil.GetComponent<Button>();
            vBtn.transition = Selectable.Transition.None;
            vBtn.onClick.AddListener(Hide);

            Image panel = UISkin.Rect(_veil.transform, "RulesPanel", UISkin.Surface, 28f, raycast: true);
            _panel = panel.rectTransform;
            _panel.anchorMin = new Vector2(0.5f, 0f);
            _panel.anchorMax = new Vector2(0.5f, 1f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(PanelWidth, -80f);
            _panel.anchoredPosition = Vector2.zero;
            UISkin.Outline(_panel, 28f, 1f, new Color(1f, 1f, 1f, 0.08f));
            _cg = panel.gameObject.AddComponent<CanvasGroup>();

            // En-tête fixe
            GameObject header = new("Header", typeof(RectTransform));
            header.transform.SetParent(_panel, false);
            RectTransform hrt = (RectTransform)header.transform;
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.offsetMin = new Vector2(44f, -100f);
            hrt.offsetMax = new Vector2(-44f, -32f);
            UISkin.HRow(header, 16f, null, TextAnchor.MiddleLeft);
            TextMeshProUGUI title = UISkin.Text(header.transform, "Title", "COMMENT JOUER", 34f, UISkin.TextColor,
                TextWeight.Display);
            title.characterSpacing = 2f;
            UISkin.Size(title.gameObject, -1f, -1f, 1f);
            GameScreenLayout.IconButton(header.transform, "BtnCloseRules", UIIcon.Close, 20f, "Fermer", Hide);

            // Zone défilante
            GameObject viewport = new("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(_panel, false);
            RectTransform vrt = (RectTransform)viewport.transform;
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = new Vector2(44f, 28f);
            vrt.offsetMax = new Vector2(-44f, -112f);
            Image vimg = viewport.GetComponent<Image>();
            vimg.color = new Color(0f, 0f, 0f, 0f);

            GameObject content = new("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            UISkin.VCol(content, 14f, new RectOffset(0, 0, 0, 24), TextAnchor.UpperLeft);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scroll = panel.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = vrt;
            _scroll.content = crt;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Elastic;
            _scroll.scrollSensitivity = 40f;
            _scroll.inertia = true;

            _content = content.transform;
            RebuildContent();

            _cg.alpha = 0f;
            _veil.SetActive(false);
        }

        private Transform? _content;
        private int _builtFor = -1;

        /// <summary>Le texte dépend des aides actives : reconstruit quand elles changent.</summary>
        private void RebuildContent()
        {
            if (_content == null) return;
            int key = (GameSettings.HintContract ? 1 : 0) | (GameSettings.HintPlayable ? 2 : 0)
                      | (GameSettings.QuickDiscard ? 4 : 0);
            if (key == _builtFor) return;
            _builtFor = key;
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                // Détaché tout de suite : Destroy n'agit qu'en fin d'image et la
                // mise en page compterait encore les anciens blocs.
                GameObject old = _content.GetChild(i).gameObject;
                old.transform.SetParent(null, false);
                Destroy(old);
            }
            BuildContent(_content);
        }

        private static void BuildContent(Transform c)
        {
            Section(c, "LE BUT");
            Paragraph(c, "Franchis les 8 niveaux avant les autres. À chaque niveau correspond un <b>contrat</b> : " +
                         "des combinaisons à poser sur la table. La première personne qui dépasse le dernier niveau gagne la partie.");

            Section(c, "TON TOUR, EN 4 ÉTAPES");
            Step(c, "1", "Piocher", "Touche la pioche, ou une défausse pour prendre sa carte visible.");
            // Les aides coupées (Options) ne sont pas décrites comme si elles étaient là.
            Step(c, "2", "Poser", "Si ta main remplit le contrat, sélectionne les cartes puis touche « Poser » (ou la table). " +
                                  (GameSettings.HintContract
                                      ? "« Préparer la pose » sélectionne une répartition valide pour toi. "
                                      : "") +
                                  "Sinon, passe à la suite : touche « Défausser », ou glisse une carte sur ta défausse.");
            Step(c, "3", "Compléter", "Une fois ton contrat posé, ajoute des cartes aux combinaisons de la table " +
                                      "(les tiennes comme celles des autres) en y glissant la carte" +
                                      (GameSettings.HintPlayable ? ", ou en touchant une carte marquée « + »." : "."));
            Step(c, "4", "Défausser", "Termine en défaussant une carte : " +
                                      (GameSettings.QuickDiscard ? "touche-la" : "touche-la deux fois") +
                                      ", glisse-la sur ta défausse, ou touche la carte puis ta défausse.");
            Paragraph(c, "Les aides (détection du contrat, cartes jouables, défausse en un toucher) s'activent dans les Options.",
                muted: true);

            Section(c, "LES COMBINAISONS");
            Paragraph(c, "<b>Suite</b> : valeurs qui se suivent, couleurs mélangées (4-5-6-7).\n" +
                         "<b>Série</b> : même valeur (paire, brelan, carré).\n" +
                         "<b>Couleur</b> : même couleur, valeurs quelconques.");

            Section(c, "LES CONTRATS");
            StringBuilder sb = new();
            for (int level = 1; level <= Constants.MaxLevel; level++)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append("<color=#E6CB92><b>Niveau ").Append(level).Append("</b></color>   ")
                  .Append(ContractCatalog.Describe(ContractCatalog.Get(level, 0)));
            }
            Paragraph(c, sb.ToString());
            Paragraph(c, "Option « Contrats variés » : chaque niveau tire un contrat de difficulté équivalente.", muted: true);

            Section(c, "FIN DE MANCHE");
            Paragraph(c, "La manche s'arrête dès qu'un joueur n'a plus de carte. Il monte de <b>2 niveaux</b> s'il avait posé " +
                         "son contrat (sinon 1). Les autres montent d'un niveau s'ils ont posé leur contrat.\n" +
                         "Les cartes restées en main comptent des <b>points de pénalité</b> (5 pour 1 à 9, 10 pour 10 à 18, " +
                         "15 pour une carte action, 25 pour un joker) : ils départagent le classement à niveau égal.");

            Section(c, "CARTES SPÉCIALES (OPTION)");
            Paragraph(c, "<b>Joker</b> : remplace n'importe quelle carte dans une combinaison.\n" +
                         "<b>Passe</b> : défaussée, elle fait sauter le prochain tour d'un adversaire.\n" +
                         "<b>+2</b> : défaussée, l'adversaire choisi pioche 2 cartes — attention, ça peut l'aider à poser !");
        }

        private static void Section(Transform parent, string text)
        {
            TextMeshProUGUI t = UISkin.Text(parent, "Section", text, 13f, UISkin.Accent, TextWeight.SemiBold);
            t.characterSpacing = 8f;
            UISkin.Size(t.gameObject, Inner, 34f);
            t.alignment = TextAlignmentOptions.BottomLeft;
        }

        private static void Paragraph(Transform parent, string text, bool muted = false)
        {
            TextMeshProUGUI t = UISkin.Text(parent, "Text", text, 17f, muted ? UISkin.TextMuted : UISkin.TextColor);
            t.richText = true;
            t.enableWordWrapping = true;
            t.lineSpacing = 8f;
            t.alignment = TextAlignmentOptions.TopLeft;
            LayoutElement le = UISkin.Size(t.gameObject, Inner);
            le.preferredHeight = t.GetPreferredValues(text, Inner, 0f).y + 4f;
        }

        private static void Step(Transform parent, string num, string title, string text)
        {
            GameObject row = new("Step", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            UISkin.HRow(row, 14f, null, TextAnchor.UpperLeft);
            UISkin.Size(row, Inner);

            Image dot = UISkin.Rect(row.transform, "Dot", UISkin.Accent, 14f);
            UISkin.Size(dot.gameObject, 28f, 28f);
            TextMeshProUGUI n = UISkin.Text(dot.transform, "Num", num, 14f, UISkin.OnAccent, TextWeight.Bold,
                TextAlignmentOptions.Center);
            UISkin.Stretch(n.rectTransform);

            string body = $"<b>{title}</b> — {text}";
            TextMeshProUGUI t = UISkin.Text(row.transform, "Text", body, 17f, UISkin.TextColor);
            t.richText = true;
            t.enableWordWrapping = true;
            t.lineSpacing = 6f;
            t.alignment = TextAlignmentOptions.TopLeft;
            float w = Inner - 28f - 14f;
            LayoutElement le = UISkin.Size(t.gameObject, w);
            le.preferredHeight = t.GetPreferredValues(body, w, 0f).y + 4f;
        }

        public void Show()
        {
            if (_veil == null || _panel == null || _cg == null || _visible) return;
            _visible = true;
            _veil.SetActive(true);
            RebuildContent();
            _veil.transform.SetAsLastSibling();
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;
            _panel.localScale = Vector3.one * 0.95f;
            UITween.ScaleTo(_panel.gameObject, _panel, Vector3.one, 0.2f);
            UITween.FadeTo(_panel.gameObject, _cg, 1f, 0.16f);
            UIBackStack.Push(this, Hide);
        }

        public void Hide()
        {
            if (_veil == null || _panel == null || _cg == null || !_visible) return;
            _visible = false;
            UIBackStack.Remove(this);
            UITween.FadeTo(_panel.gameObject, _cg, 0f, 0.14f, () =>
            {
                if (_veil != null && !_visible) _veil.SetActive(false);
            });
        }
    }
}
