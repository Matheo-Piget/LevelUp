using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LevelUp.UI
{
    /// <summary>
    /// Champs de saisie à la DA (le projet n'utilise aucun prefab) : fond Base
    /// arrondi, liseré qui passe au champagne quand le champ a le focus, curseur
    /// et sélection accent. Clavier natif automatique sur mobile (TMP_InputField).
    /// </summary>
    public static class UIInputs
    {
        /// <summary>Crée un champ texte une ligne. Le GameObject racine porte le TMP_InputField.</summary>
        public static TMP_InputField TextField(Transform parent, string name, string placeholder,
            float width, float height, float fontSize = 20f, int characterLimit = 16,
            TextWeight weight = TextWeight.SemiBold,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, float characterSpacing = 0f)
        {
            Image bg = UISkin.Rect(parent, name, UISkin.Base, 14f, raycast: true);
            RectTransform rt = bg.rectTransform;
            rt.sizeDelta = new Vector2(width, height);
            UISkin.Size(bg.gameObject, width, height);
            Image ring = UISkin.Outline(rt, 14f, 1f, UISkin.LineStrong);
            ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            // Viewport masqué : le texte défile si besoin, sans déborder du champ.
            GameObject area = new("TextArea", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(rt, false);
            RectTransform art = (RectTransform)area.transform;
            art.anchorMin = Vector2.zero;
            art.anchorMax = Vector2.one;
            art.offsetMin = new Vector2(18f, 6f);
            art.offsetMax = new Vector2(-18f, -6f);
            area.AddComponent<LayoutElement>().ignoreLayout = true;

            TextMeshProUGUI ph = UISkin.Text(area.transform, "Placeholder", placeholder, fontSize,
                UISkin.WithAlpha(UISkin.TextMuted, 0.6f), weight, align);
            UISkin.Stretch(ph.rectTransform);
            ph.characterSpacing = characterSpacing;

            TextMeshProUGUI text = UISkin.Text(area.transform, "Text", "", fontSize, UISkin.TextColor, weight, align);
            UISkin.Stretch(text.rectTransform);
            text.characterSpacing = characterSpacing;

            TMP_InputField input = bg.gameObject.AddComponent<TMP_InputField>();
            input.transition = Selectable.Transition.None;
            input.textViewport = art;
            input.textComponent = text;
            input.placeholder = ph;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = characterLimit;
            input.richText = false;
            input.customCaretColor = true;
            input.caretColor = UISkin.AccentLight;
            input.caretWidth = 2;
            input.selectionColor = UISkin.WithAlpha(UISkin.Accent, 0.35f);
            input.onSelect.AddListener(_ => ring.color = UISkin.WithAlpha(UISkin.AccentLight, 0.85f));
            input.onDeselect.AddListener(_ => ring.color = UISkin.LineStrong);
            return input;
        }

        /// <summary>Champ de pseudo : bloque les caractères réservés au protocole et au rich text.</summary>
        public static TMP_InputField NameField(Transform parent, string name, float width, float height = 60f)
        {
            TMP_InputField input = TextField(parent, name, "Ton pseudo", width, height, 22f,
                LevelUp.Core.PlayerProfile.MaxNameLength);
            input.onValidateInput = (string _, int _, char c) =>
                c == '|' || c == '<' || c == '>' || c == '\\' || char.IsControl(c) ? '\0' : c;
            return input;
        }
    }
}
