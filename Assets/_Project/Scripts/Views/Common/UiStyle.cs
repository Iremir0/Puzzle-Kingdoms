using System.Collections.Generic;
using UnityEngine;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// Procedural sprites that give the code-built UI its look (PB-24 polish): rounded 9-slice panels, the soft
    /// gradient backdrop and radial glows. Built once, cached, no art files.
    /// </summary>
    public static class UiStyle
    {
        private const int RoundSize = 64;
        private const int Radius = 22;

        private static Sprite _rounded;
        private static Sprite _glow;
        private static readonly Dictionary<long, Sprite> Gradients = new Dictionary<long, Sprite>();

        /// <summary>A white rounded rectangle to slice: corners stay round at any size (radius about 22 reference pixels).</summary>
        public static Sprite Rounded
        {
            get
            {
                if (_rounded == null)
                {
                    var texture = new Texture2D(RoundSize, RoundSize, TextureFormat.RGBA32, false)
                    {
                        name = "UiRounded",
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear,
                    };
                    var pixels = new Color32[RoundSize * RoundSize];
                    for (int y = 0; y < RoundSize; y++)
                    {
                        for (int x = 0; x < RoundSize; x++)
                        {
                            // Distance outside the rounded corner, anti-aliased over one pixel.
                            float cx = Mathf.Clamp(x + 0.5f, Radius, RoundSize - Radius);
                            float cy = Mathf.Clamp(y + 0.5f, Radius, RoundSize - Radius);
                            float distance = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                            float alpha = Mathf.Clamp01(Radius - distance + 0.5f);
                            pixels[y * RoundSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                        }
                    }
                    texture.SetPixels32(pixels);
                    texture.Apply();
                    _rounded = Sprite.Create(texture, new Rect(0, 0, RoundSize, RoundSize), new Vector2(0.5f, 0.5f), 100f, 0,
                        SpriteMeshType.FullRect, new Vector4(Radius, Radius, Radius, Radius));
                }
                return _rounded;
            }
        }

        /// <summary>A soft round glow: white in the middle, fading to nothing at the edge (tint it with the image colour).</summary>
        public static Sprite Glow
        {
            get
            {
                if (_glow == null)
                {
                    const int size = 128;
                    var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                    {
                        name = "UiGlow",
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear,
                    };
                    var pixels = new Color32[size * size];
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dx = (x + 0.5f) / size * 2f - 1f;
                            float dy = (y + 0.5f) / size * 2f - 1f;
                            float falloff = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                            float alpha = falloff * falloff * (3f - 2f * falloff); // smoothstep
                            pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                        }
                    }
                    texture.SetPixels32(pixels);
                    texture.Apply();
                    _glow = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                }
                return _glow;
            }
        }

        /// <summary>A vertical gradient from <paramref name="top"/> to <paramref name="bottom"/>, stretched over any rectangle.</summary>
        public static Sprite Gradient(Color top, Color bottom)
        {
            long key = ((long)ColorKey(top) << 32) | ColorKey(bottom);
            if (!Gradients.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                const int height = 128;
                var texture = new Texture2D(2, height, TextureFormat.RGBA32, false)
                {
                    name = "UiGradient",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
                for (int y = 0; y < height; y++)
                {
                    Color color = Color.Lerp(bottom, top, y / (height - 1f));
                    texture.SetPixel(0, y, color);
                    texture.SetPixel(1, y, color);
                }
                texture.Apply();
                sprite = Sprite.Create(texture, new Rect(0, 0, 2, height), new Vector2(0.5f, 0.5f), 100f);
                Gradients[key] = sprite;
            }
            return sprite;
        }

        private static uint ColorKey(Color color)
        {
            Color32 c = color;
            return (uint)(c.r << 24 | c.g << 16 | c.b << 8 | c.a);
        }
    }
}
