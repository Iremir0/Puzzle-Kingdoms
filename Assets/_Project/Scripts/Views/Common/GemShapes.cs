using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleKingdoms.Views.Common
{
    public enum GemShape
    {
        Square,
        Triangle,
        Diamond,
        Circle,
        Star,
        /// <summary>Hollow ring: marks a Blast gem.</summary>
        Ring,
        /// <summary>Four-pointed sparkle: marks a Prism gem.</summary>
        Sparkle,
    }

    /// <summary>
    /// Procedural white shape sprites for gems, so every gem type is readable by
    /// shape as well as by color (also for color-blind players). Tint them with
    /// <c>Image.color</c>. Generated once and cached; no art files needed.
    /// </summary>
    public static class GemShapes
    {
        private const int Size = 128;
        private const int Samples = 3;

        private static readonly Dictionary<GemShape, Sprite> Cache = new Dictionary<GemShape, Sprite>();

        public static Sprite Get(GemShape shape)
        {
            if (!Cache.TryGetValue(shape, out Sprite sprite) || sprite == null)
            {
                sprite = Build(shape);
                Cache[shape] = sprite;
            }
            return sprite;
        }

        private static readonly Dictionary<GemShape, Sprite> StoneCache = new Dictionary<GemShape, Sprite>();

        /// <summary>
        /// The right half of a gem turned to stone (D-032): the same outline as <see cref="Get"/>,
        /// a speckled grey rock texture with cracks, and a jagged edge towards the left half.
        /// Draw it over the gem so the plain half still shows the gem's color.
        /// </summary>
        public static Sprite GetStoneHalf(GemShape shape)
        {
            if (!StoneCache.TryGetValue(shape, out Sprite sprite) || sprite == null)
            {
                sprite = BuildSprite("GemStone_" + shape, (x, y) => StoneShade(shape, x, y));
                StoneCache[shape] = sprite;
            }
            return sprite;
        }

        private static Sprite Build(GemShape shape) =>
            BuildSprite("GemShape_" + shape, (x, y) => Contains(shape, x, y) ? 1f : -1f);

        /// <summary>Renders a grey-scale sprite: <paramref name="shade"/> is -1 for transparent, else a brightness in [0, 1].</summary>
        private static Sprite BuildSprite(string name, Func<float, float, float> shade)
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
                            // Shape space: x right, y up, both in [-1, 1].
                            float x = ((px + (sx + 0.5f) / Samples) / Size) * 2f - 1f;
                            float y = ((py + (sy + 0.5f) / Samples) / Size) * 2f - 1f;
                            float value = shade(x, y);
                            if (value >= 0f)
                            {
                                inside++;
                                shadeSum += value;
                            }
                        }
                    }
                    if (inside == 0)
                    {
                        pixels[py * Size + px] = new Color32(255, 255, 255, 0);
                        continue;
                    }
                    byte grey = (byte)Mathf.RoundToInt(255f * shadeSum / inside);
                    byte alpha = (byte)Mathf.RoundToInt(255f * inside / (Samples * Samples));
                    pixels[py * Size + px] = new Color32(grey, grey, grey, alpha);
                }
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }

        /// <summary>Brightness of the stone half at a point, or -1 outside the stone.</summary>
        private static float StoneShade(GemShape shape, float x, float y)
        {
            if (!Contains(shape, x, y))
            {
                return -1f;
            }
            if (x < 0.06f * Mathf.Sin(y * 9f) + 0.02f)
            {
                return -1f; // the left half stays a plain gem
            }
            if (NearSegment(x, y, 0.15f, 0.7f, 0.4f, 0.2f, 0.03f) || NearSegment(x, y, 0.4f, 0.2f, 0.25f, -0.25f, 0.03f)
                || NearSegment(x, y, 0.4f, 0.2f, 0.7f, 0.05f, 0.03f) || NearSegment(x, y, 0.25f, -0.25f, 0.4f, -0.6f, 0.03f))
            {
                return 0.3f; // cracks
            }
            float noise = Mathf.Repeat(Mathf.Sin(Mathf.Floor(x * 14f) * 12.9898f + Mathf.Floor(y * 14f) * 78.233f) * 43758.5453f, 1f);
            return 0.6f + 0.32f * noise; // speckled rock
        }

        private static bool NearSegment(float x, float y, float x0, float y0, float x1, float y1, float r)
        {
            float vx = x1 - x0;
            float vy = y1 - y0;
            float t = Mathf.Clamp01(((x - x0) * vx + (y - y0) * vy) / (vx * vx + vy * vy));
            float dx = x - (x0 + vx * t);
            float dy = y - (y0 + vy * t);
            return dx * dx + dy * dy <= r * r;
        }

        private static bool Contains(GemShape shape, float x, float y)
        {
            switch (shape)
            {
                case GemShape.Square:
                    return Mathf.Abs(x) <= 0.78f && Mathf.Abs(y) <= 0.78f;
                case GemShape.Circle:
                    return x * x + y * y <= 0.88f * 0.88f;
                case GemShape.Diamond:
                    return Mathf.Abs(x) + Mathf.Abs(y) <= 0.96f;
                case GemShape.Triangle:
                    return y >= -0.78f && y <= 0.92f && Mathf.Abs(x) <= (0.92f - y) * 0.56f;
                case GemShape.Star:
                    return InsidePolygon(StarPoints, x, y);
                case GemShape.Ring:
                {
                    float radius = Mathf.Sqrt(x * x + y * y);
                    return radius <= 0.92f && radius >= 0.62f;
                }
                case GemShape.Sparkle:
                    return InsidePolygon(SparklePoints, x, y);
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }

        private static readonly Vector2[] StarPoints = MakeStar(5, 0.98f, 0.42f);
        private static readonly Vector2[] SparklePoints = MakeSparkle();

        /// <summary>Four-pointed star with a pinched waist, rotated so a point faces up.</summary>
        private static Vector2[] MakeSparkle()
        {
            var result = new Vector2[8];
            for (int i = 0; i < result.Length; i++)
            {
                float radius = i % 2 == 0 ? 0.95f : 0.2f;
                float angle = Mathf.PI / 2f + i * Mathf.PI / 4f;
                result[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            }
            return result;
        }

        private static Vector2[] MakeStar(int points, float outer, float inner)
        {
            var result = new Vector2[points * 2];
            for (int i = 0; i < result.Length; i++)
            {
                float radius = i % 2 == 0 ? outer : inner;
                float angle = Mathf.PI / 2f + i * Mathf.PI / points;
                result[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            }
            return result;
        }

        /// <summary>Even-odd point-in-polygon test.</summary>
        private static bool InsidePolygon(Vector2[] polygon, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}
