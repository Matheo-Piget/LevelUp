using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LevelUp.UI
{
    /// <summary>
    /// Contrôles de formulaire à la DA, partagés par Options, Nouvelle partie et
    /// le salon en ligne : ligne "libellé | contrôle", contrôle segmenté (actif
    /// champagne plein), interrupteur, titre de section, paragraphe.
    /// </summary>
    public static class UIControls
    {
        /// <summary>Ligne horizontale de hauteur fixe (libellé + contrôle).</summary>
        public static GameObject Row(Transform parent, string name, float width, float height, float spacing = 16f)
        {
            GameObject row = new(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            UISkin.HRow(row, spacing, null, TextAnchor.MiddleLeft);
            UISkin.Size(row, width, height);
            return row;
        }

        /// <summary>Colonne verticale de largeur fixe.</summary>
        public static GameObject Column(Transform parent, string name, float width, float spacing)
        {
            GameObject col = new(name, typeof(RectTransform));
            col.transform.SetParent(parent, false);
            UISkin.VCol(col, spacing, null, TextAnchor.UpperLeft);
            // Hauteur = contenu : à côté d'une colonne plus haute, elle ne s'étire
            // pas (sinon ses contrôles segmentés, extensibles, grandissaient).
            UISkin.Size(col, width).flexibleHeight = 0f;
            return col;
        }

        public static TextMeshProUGUI RowLabel(Transform row, string text, float width)
        {
            TextMeshProUGUI lbl = UISkin.Text(row, "Label", text, 16f, UISkin.TextColor, TextWeight.SemiBold);
            UISkin.Size(lbl.gameObject, width);
            return lbl;
        }

        /// <summary>Titre de section discret : "LA TABLE", "TON PSEUDO"…</summary>
        public static TextMeshProUGUI SectionLabel(Transform parent, string text, float width)
        {
            TextMeshProUGUI t = UISkin.Text(parent, "Section", text, 12f, UISkin.TextMuted, TextWeight.SemiBold);
            t.characterSpacing = 8f;
            UISkin.Size(t.gameObject, width, 18f);
            return t;
        }

        /// <summary>Paragraphe multi-lignes (retour à la ligne automatique).</summary>
        public static TextMeshProUGUI Paragraph(Transform parent, string name, string text, float width,
            float height, float size = 14f, Color? color = null,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            TextMeshProUGUI t = UISkin.Text(parent, name, text, size, color ?? UISkin.TextMuted,
                TextWeight.Regular, align);
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Truncate;
            t.lineSpacing = 4f;
            UISkin.Size(t.gameObject, width, height);
            return t;
        }

        /// <summary>
        /// Contrôle segmenté (fond Base, liseré, option active champagne plein).
        /// Retourne une action qui resynchronise l'affichage sur <paramref name="getIndex"/>.
        /// </summary>
        public static Action Segmented(Transform parent, string name, string[] options, float width,
            Func<int> getIndex, Action<int> setIndex, float height = 52f)
        {
            Image seg = UISkin.Rect(parent, name, UISkin.Base, 14f);
            UISkin.Size(seg.gameObject, width, height);
            UISkin.HRow(seg.gameObject, 4f, new RectOffset(4, 4, 4, 4), TextAnchor.MiddleCenter);
            HorizontalLayoutGroup h = seg.GetComponent<HorizontalLayoutGroup>();
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            UISkin.Outline(seg.rectTransform, 14f, 1f, UISkin.Line).gameObject.AddComponent<LayoutElement>()
                .ignoreLayout = true;

            Image[] bgs = new Image[options.Length];
            TextMeshProUGUI[] labels = new TextMeshProUGUI[options.Length];

            void Refresh()
            {
                int active = getIndex();
                for (int i = 0; i < bgs.Length; i++)
                {
                    bool on = i == active;
                    bgs[i].color = on ? UISkin.Accent : new Color(0f, 0f, 0f, 0f);
                    labels[i].color = on ? UISkin.OnAccent : UISkin.TextMuted;
                }
            }

            for (int i = 0; i < options.Length; i++)
            {
                int idx = i;
                Image b = UISkin.Rect(seg.transform, $"Opt{i}", new Color(0f, 0f, 0f, 0f), 10f, raycast: true);
                UISkin.Size(b.gameObject, -1f, height - 8f, 1f);
                TextMeshProUGUI t = UISkin.Text(b.transform, "Label", options[i], 14f, UISkin.TextMuted,
                    TextWeight.Bold, TextAlignmentOptions.Center);
                t.characterSpacing = 6f;
                UISkin.Stretch(t.rectTransform);
                Button btn = b.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() =>
                {
                    setIndex(idx);
                    Refresh();
                });
                bgs[i] = b;
                labels[i] = t;
            }
            Refresh();
            return Refresh;
        }

        /// <summary>Interrupteur 60×34 (fond accent quand actif, bouton blanc qui glisse).</summary>
        public static Action Switch(Transform parent, string name, Func<bool> get, Action<bool> set)
        {
            Image track = UISkin.Rect(parent, name, UISkin.SurfaceRaised, 17f, raycast: true);
            UISkin.Size(track.gameObject, 60f, 34f);
            Image knob = UISkin.Rect(track.transform, "Knob", Color.white, 13f);
            UISkin.Place(knob.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f),
                new Vector2(26f, 26f));

            void Apply(bool on, bool animate)
            {
                track.color = on ? UISkin.Accent : UISkin.SurfaceRaised;
                Vector2 target = new(on ? 30f : 4f, 0f);
                if (animate) UITween.MoveTo(track.gameObject, knob.rectTransform, target, 0.15f);
                else knob.rectTransform.anchoredPosition = target;
            }

            Button btn = track.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                set(!get());
                Apply(get(), true);
            });
            Apply(get(), false);
            return () => Apply(get(), false);
        }

        /// <summary>Carte "titre + description | interrupteur" (daltonisme, aides…).</summary>
        public static Action SwitchCard(Transform parent, string name, string title, string description,
            float width, Func<bool> get, Action<bool> set, Action<Transform>? extra = null, float height = 76f)
        {
            Image card = UISkin.Rect(parent, name, UISkin.Base, 16f);
            UISkin.Size(card.gameObject, width, height);
            float padV = height >= 70f ? 14f : 8f;
            UISkin.HRow(card.gameObject, 16f, new RectOffset(18, 18, (int)padV, (int)padV), TextAnchor.MiddleLeft);
            UISkin.Outline(card.rectTransform, 16f, 1f, UISkin.Line).gameObject.AddComponent<LayoutElement>()
                .ignoreLayout = true;

            GameObject texts = new("Texts", typeof(RectTransform));
            texts.transform.SetParent(card.transform, false);
            UISkin.VCol(texts, 4f, null, TextAnchor.MiddleLeft);
            UISkin.Size(texts, width - 36f - 60f - 16f);
            UISkin.Text(texts.transform, "Title", title, 16f, UISkin.TextColor, TextWeight.SemiBold);
            if (extra != null) extra(texts.transform);
            else UISkin.Text(texts.transform, "Desc", description, 13f, UISkin.TextMuted);

            return Switch(card.transform, "Switch", get, set);
        }
    }
}
