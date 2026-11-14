using System.Collections.Generic;
using PuzzleKingdoms.Data;
using UnityEngine;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// Sprites for the grey-box unit silhouettes (D-029), generated once from
    /// <see cref="SilhouetteMath"/>. The sprite is grey-scale; tint it with the unit
    /// color through <c>Image.color</c> and details come out darker.
    /// </summary>
    public static class UnitSilhouettes
    {
        private const int Size = 128;
        private const int Samples = 3;

        private static readonly Dictionary<UnitLook, Sprite> Cache = new Dictionary<UnitLook, Sprite>();

        public static Sprite Get(UnitLook look)
        {
            if (!Cache.TryGetValue(look, out Sprite sprite) || sprite == null)
            {
                sprite = Build(look);
                Cache[look] = sprite;
            }
            return sprite;
        }

        private static Sprite Build(UnitLook look)
        {
            var pixels = new Color32[Size * Size];
            for (int py = 0; py < Size; py++)
            {
                for (int px = 0; px < Size; px++)
                {
                    int inside = 0;
                    float shadeSum = 0f;
                    for (int sy = 0; sy < Samples; sy++)
                    {
                        for (int sx = 0; sx < Samples; sx++)
                        {
                            float x = ((px + (sx + 0.5f) / Samples) / Size) * 2f - 1f;
                            float y = ((py + (sy + 0.5f) / Samples) / Size) * 2f - 1f;
                            float shade = SilhouetteMath.Shade(look, x, y);
                            if (shade >= 0f)
                            {
                                inside++;
                                shadeSum += shade;
                            }
                        }
                    }
                    if (inside == 0)
                    {
                        pixels[py * Size + px] = new Color32(255, 255, 255, 0);
                        continue;
                    }
                    byte value = (byte)Mathf.RoundToInt(255f * shadeSum / inside);
                    byte alpha = (byte)Mathf.RoundToInt(255f * inside / (Samples * Samples));
                    pixels[py * Size + px] = new Color32(value, value, value, alpha);
                }
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "UnitSilhouette_" + look,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }
    }
}
