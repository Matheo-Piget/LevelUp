using UnityEngine;
using UnityEngine.UI;

namespace LevelUp.UI
{
    /// <summary>
    /// Fait tenir une fenêtre dans la largeur de l'écran. Les fenêtres sont
    /// dessinées pour le paysage ; en portrait (ou sur un écran étroit), celles
    /// qui dépassent sont réduites juste assez pour tenir, marges comprises.
    ///
    /// La fenêtre est glissée dans un conteneur intermédiaire, et c'est LUI qui
    /// est mis à l'échelle : les animations d'ouverture de la fenêtre (qui
    /// pilotent sa propre échelle) ne sont pas perturbées.
    /// </summary>
    public sealed class PortraitFit : MonoBehaviour
    {
        private const float SideMargin = 12f;

        private RectTransform? _self;
        private RectTransform? _panel;
        private RectTransform? _host;
        private float _lastHostWidth = -1f, _lastHostHeight = -1f, _lastPanelWidth = -1f;

        /// <summary>Enveloppe la fenêtre ; à appeler une fois, après sa création.</summary>
        public static void Wrap(RectTransform? panel)
        {
            if (panel == null || panel.parent == null) return;
            if (panel.parent.GetComponent<PortraitFit>() != null) return;

            RectTransform host = (RectTransform)panel.parent;
            GameObject go = new(panel.name + "Fit", typeof(RectTransform));
            RectTransform self = (RectTransform)go.transform;
            self.SetParent(host, false);
            self.SetSiblingIndex(panel.GetSiblingIndex());
            self.anchorMin = new Vector2(0.5f, 0.5f);
            self.anchorMax = new Vector2(0.5f, 0.5f);
            self.pivot = new Vector2(0.5f, 0.5f);
            self.anchoredPosition = Vector2.zero;
            // Hors mise en page si l'hôte en a une (le conteneur couvre l'hôte).
            go.AddComponent<LayoutElement>().ignoreLayout = true;

            panel.SetParent(self, false);

            PortraitFit fit = go.AddComponent<PortraitFit>();
            fit._self = self;
            fit._panel = panel;
            fit._host = host;
            fit.Refresh();
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (_self == null || _panel == null || _host == null) return;
            float hostW = _host.rect.width, hostH = _host.rect.height, panelW = _panel.rect.width;
            if (hostW <= 0f || panelW <= 0f) return;
            if (Mathf.Approximately(hostW, _lastHostWidth) && Mathf.Approximately(hostH, _lastHostHeight)
                && Mathf.Approximately(panelW, _lastPanelWidth)) return;
            _lastHostWidth = hostW;
            _lastHostHeight = hostH;
            _lastPanelWidth = panelW;

            float scale = Mathf.Min(1f, (hostW - 2f * SideMargin) / panelW);
            // Le conteneur garde, une fois réduit, la taille de l'hôte : une fenêtre
            // ancrée sur toute la hauteur continue de la remplir.
            _self.sizeDelta = new Vector2(hostW / scale, hostH / scale);
            _self.localScale = new Vector3(scale, scale, 1f);
        }
    }
}

namespace LevelUp.UI
{
    /// <summary>
    /// Fenêtres à deux colonnes (Options, Nouvelle partie) : en portrait, les
    /// colonnes passent l'une sous l'autre et la fenêtre prend une largeur
    /// étroite, au lieu d'être réduite jusqu'à devenir illisible.
    /// </summary>
    public static class PortraitStack
    {
        /// <param name="panel">La fenêtre (ses enfants directs larges comme l'intérieur sont ajustés).</param>
        /// <param name="columns">La rangée qui contient les deux colonnes.</param>
        public static void Apply(RectTransform panel, GameObject columns, float landscapeWidth, float portraitWidth,
            float padding = 88f)
        {
            bool portrait = GameScreenLayout.IsPortrait;
            float width = portrait ? portraitWidth : landscapeWidth;
            float inner = width - padding;

            // Enfants directs calés sur la largeur intérieure (titres, rangées, pied).
            foreach (Transform child in panel)
            {
                LayoutElement? le = child.GetComponent<LayoutElement>();
                if (le == null || le.ignoreLayout) continue;
                bool wasInner = Mathf.Abs(le.preferredWidth - (landscapeWidth - padding)) < 1f
                                || Mathf.Abs(le.preferredWidth - (portraitWidth - padding)) < 1f;
                if (!wasInner) continue;
                le.preferredWidth = inner;
                le.minWidth = inner;
            }
            panel.sizeDelta = new Vector2(width, panel.sizeDelta.y);

            HorizontalLayoutGroup? h = columns.GetComponent<HorizontalLayoutGroup>();
            VerticalLayoutGroup? v = columns.GetComponent<VerticalLayoutGroup>();
            if (portrait && v == null)
            {
                if (h != null) Object.DestroyImmediate(h);
                UISkin.VCol(columns, 22f, null, TextAnchor.UpperCenter);
            }
            else if (!portrait && h == null)
            {
                if (v != null) Object.DestroyImmediate(v);
                HorizontalLayoutGroup row = UISkin.HRow(columns, 40f, null, TextAnchor.UpperLeft);
                _ = row;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        }
    }
}
