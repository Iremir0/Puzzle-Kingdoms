using System.IO;
using UnityEditor;
using UnityEngine;

namespace PuzzleKingdoms.EditorTools
{
    /// <summary>
    /// PB-17: draws the app icon in code (a gold crown over a red gem on navy, our palette) so it is reproducible and
    /// needs no art file: a 512 master, an adaptive icon (432 foreground and background, the picture inside the centre
    /// 66 %), a round and a legacy icon, and the splash logo. Assigns them in the Android player settings and the
    /// splash screen. A placeholder until a real icon exists (replace the PNGs in <c>Assets/_Project/Art/Icon/</c>).
    /// Menu: Puzzle Kingdoms → Build App Icon. Batch mode:
    /// -executeMethod PuzzleKingdoms.EditorTools.IconBuilder.Build
    /// </summary>
    public static class IconBuilder
    {
        public const string Folder = "Assets/_Project/Art/Icon";
        private static readonly Color Navy = new Color32(0x1E, 0x22, 0x30, 0xFF);
        private static readonly Color Gold = new Color32(0xF4, 0xBE, 0x4F, 0xFF);
        private static readonly Color Red = new Color32(0xE8, 0x5B, 0x55, 0xFF);
        private static readonly Color GoldDark = new Color32(0xC9, 0x93, 0x2E, 0xFF);

        [MenuItem("Puzzle Kingdoms/Build App Icon")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            Texture2D master = Draw(512, withBackground: true, scale: 0.78f);
            Texture2D foreground = Draw(432, withBackground: false, scale: 0.62f); // inside the centre 66 %
            Texture2D background = Solid(432, Navy);
            Texture2D round = Draw(512, withBackground: true, scale: 0.7f);
            Texture2D logo = Draw(512, withBackground: false, scale: 0.9f);

            Save(master, "icon-master");
            Save(foreground, "icon-adaptive-foreground");
            Save(background, "icon-adaptive-background");
            Save(round, "icon-round");
            Save(logo, "splash-logo", sprite: true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Texture2D Load(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/{name}.png");
            Assign(BuildTargetGroup.Android, IconSet.Adaptive, Load("icon-adaptive-background"), Load("icon-adaptive-foreground"));
            Assign(BuildTargetGroup.Android, IconSet.Round, Load("icon-round"), null);
            Assign(BuildTargetGroup.Android, IconSet.Legacy, Load("icon-master"), null);

            // Splash: navy background (already set), the Unity logo first (kept on a free plan), then ours.
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/splash-logo.png");
            PlayerSettings.SplashScreen.backgroundColor = Navy;
            PlayerSettings.SplashScreen.logos = new[]
            {
                PlayerSettings.SplashScreenLogo.CreateWithUnityLogo(),
                PlayerSettings.SplashScreenLogo.Create(2f, sprite),
            };
            AssetDatabase.SaveAssets();
            Debug.Log($"[IconBuilder] icons and splash logo written to {Folder} and set in the player settings.");
        }

        private enum IconSet { Adaptive, Round, Legacy }

        private static void Assign(BuildTargetGroup target, IconSet kind, Texture2D first, Texture2D second)
        {
            PlatformIconKind androidKind = kind == IconSet.Adaptive
                ? UnityEditor.Android.AndroidPlatformIconKind.Adaptive
                : kind == IconSet.Round ? UnityEditor.Android.AndroidPlatformIconKind.Round : UnityEditor.Android.AndroidPlatformIconKind.Legacy;
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(target, androidKind);
            foreach (PlatformIcon icon in icons)
            {
                icon.SetTexture(first, 0);
                if (second != null)
                {
                    icon.SetTexture(second, 1);
                }
            }
            PlayerSettings.SetPlatformIcons(target, androidKind, icons);
        }

        private static Texture2D Solid(int size, Color color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            return texture;
        }

        /// <summary>Crown over a gem; <paramref name="scale"/> is the picture's share of the width.</summary>
        private static Texture2D Draw(int size, bool withBackground, float scale)
        {
            Texture2D texture = Solid(size, withBackground ? Navy : new Color(0, 0, 0, 0));
            float s = size * scale;
            float x0 = (size - s) / 2f;
            float y0 = (size - s) / 2f;
            Vector2 P(float u, float v) => new Vector2(x0 + u * s, y0 + v * s); // u, v in 0..1, v up

            // Gem: a diamond in the lower half.
            Fill(texture, new[] { P(0.5f, 0.05f), P(0.78f, 0.34f), P(0.5f, 0.63f), P(0.22f, 0.34f) }, Red);
            Fill(texture, new[] { P(0.5f, 0.05f), P(0.78f, 0.34f), P(0.5f, 0.34f) }, new Color(0.62f, 0.2f, 0.2f, 1f));
            // Crown: a band and three points.
            Fill(texture, new[]
            {
                P(0.12f, 0.58f), P(0.88f, 0.58f), P(0.88f, 0.9f), P(0.7f, 0.74f), P(0.5f, 0.98f), P(0.3f, 0.74f), P(0.12f, 0.9f),
            }, Gold);
            Fill(texture, new[] { P(0.12f, 0.58f), P(0.88f, 0.58f), P(0.88f, 0.66f), P(0.12f, 0.66f) }, GoldDark);
            texture.Apply();
            return texture;
        }

        /// <summary>Fills a (possibly concave) polygon with 2×2 supersampling for smooth edges.</summary>
        private static void Fill(Texture2D texture, Vector2[] polygon, Color color)
        {
            int size = texture.width;
            float minY = float.MaxValue, maxY = float.MinValue, minX = float.MaxValue, maxX = float.MinValue;
            foreach (Vector2 p in polygon)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            for (int y = Mathf.Max(0, (int)minY); y <= Mathf.Min(size - 1, (int)maxY + 1); y++)
            {
                for (int x = Mathf.Max(0, (int)minX); x <= Mathf.Min(size - 1, (int)maxX + 1); x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            if (Contains(polygon, new Vector2(x + 0.25f + 0.5f * sx, y + 0.25f + 0.5f * sy)))
                            {
                                inside++;
                            }
                        }
                    }
                    if (inside > 0)
                    {
                        Color under = texture.GetPixel(x, y);
                        float a = inside / 4f;
                        texture.SetPixel(x, y, new Color(
                            Mathf.Lerp(under.r, color.r, a), Mathf.Lerp(under.g, color.g, a), Mathf.Lerp(under.b, color.b, a),
                            Mathf.Max(under.a, a)));
                    }
                }
            }
        }

        private static bool Contains(Vector2[] polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].y > point.y) != (polygon[j].y > point.y)
                    && point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static void Save(Texture2D texture, string name, bool sprite = false)
        {
            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (sprite && AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
        }
    }
}
