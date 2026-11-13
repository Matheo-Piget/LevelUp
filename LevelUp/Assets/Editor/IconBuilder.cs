using System.IO;
using UnityEditor;
using UnityEngine;

namespace LevelUp.EditorTools
{
    /// <summary>
    /// Fabrique l'icône de l'application dans l'identité « Salon » (fond graphite,
    /// deux cartes en éventail, accent champagne) et l'assigne au projet.
    /// Dessinée en code, comme le reste de l'interface : aucun fichier source.
    ///   Menu : LevelUp > Générer l'icône
    ///   Batch : -executeMethod LevelUp.EditorTools.IconBuilder.Generate
    /// </summary>
    public static class IconBuilder
    {
        private const string Path = "Assets/Icon/AppIcon.png";
        private const int Size = 512;

        [MenuItem("LevelUp/Générer l'icône")]
        public static void Generate()
        {
            Color ground = Hex(0x111516), groundEdge = Hex(0x1B2D27);
            Color back = Hex(0x3F78C6), front = Hex(0xD8B66E), ink = Hex(0x1B160E), edge = new(1f, 1f, 1f, 0.9f);

            Color[] px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Vector2 p = new(x + 0.5f - Size / 2f, y + 0.5f - Size / 2f);

                    // Fond : carré arrondi graphite, lueur verte du tapis au centre.
                    float bg = Coverage(RoundedBox(p, new Vector2(248f, 248f), 108f));
                    Color c = Color.Lerp(groundEdge, ground, Mathf.Clamp01(p.magnitude / 300f));
                    c.a = bg;

                    // Carte du dessous (bleue), inclinée à gauche.
                    c = Over(c, back, Coverage(Card(p, new Vector2(-52f, 6f), -14f)) * bg);
                    c = Over(c, edge, Ring(Card(p, new Vector2(-52f, 6f), -14f), 5f) * bg);

                    // Carte du dessus (champagne), inclinée à droite, losange sombre au centre.
                    float top = Card(p, new Vector2(44f, -6f), 9f);
                    c = Over(c, new Color(0f, 0f, 0f, 0.35f), Coverage(Card(p, new Vector2(48f, -16f), 9f)) * bg * (1f - Coverage(top)));
                    c = Over(c, front, Coverage(top) * bg);
                    c = Over(c, edge, Ring(top, 5f) * bg);
                    Vector2 q = Rotate(p - new Vector2(44f, -6f), -9f);
                    float diamond = (Mathf.Abs(q.x) / 46f + Mathf.Abs(q.y) / 64f - 1f) * 40f;
                    c = Over(c, ink, Coverage(diamond) * bg);

                    px[y * Size + x] = c;
                }
            }

            Texture2D tex = new(Size, Size, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.WriteAllBytes(Path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(Path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Path);
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
            AssetDatabase.SaveAssets();
            Debug.Log($"[IconBuilder] Icône générée : {Path}");
        }

        private static Color Hex(int rgb) =>
            new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        private static Vector2 Rotate(Vector2 p, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad, s = Mathf.Sin(r), c = Mathf.Cos(r);
            return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
        }

        /// <summary>Distance signée à un rectangle arrondi centré (négative à l'intérieur).</summary>
        private static float RoundedBox(Vector2 p, Vector2 half, float radius)
        {
            Vector2 d = new(Mathf.Abs(p.x) - half.x + radius, Mathf.Abs(p.y) - half.y + radius);
            return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude
                   + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - radius;
        }

        private static float Card(Vector2 p, Vector2 center, float degrees) =>
            RoundedBox(Rotate(p - center, -degrees), new Vector2(92f, 134f), 22f);

        private static float Coverage(float distance) => Mathf.Clamp01(0.5f - distance);

        private static float Ring(float distance, float width) =>
            Mathf.Clamp01(0.5f - (Mathf.Abs(distance + width * 0.5f) - width * 0.5f));

        private static Color Over(Color under, Color over, float alpha)
        {
            float a = Mathf.Clamp01(alpha * over.a);
            return new Color(Mathf.Lerp(under.r, over.r, a), Mathf.Lerp(under.g, over.g, a),
                Mathf.Lerp(under.b, over.b, a), Mathf.Max(under.a, a));
        }
    }
}
