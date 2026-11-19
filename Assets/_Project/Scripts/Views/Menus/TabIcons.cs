using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>
    /// Pictograms for the bottom tabs, drawn once from simple shapes (no art files): a flag for the Map, a castle
    /// for the Kingdom, a crown for the Commanders, a coin bag for the Shop. White on transparent, tinted by the image.
    /// </summary>
    public static class TabIcons
    {
        private const int Size = 128;
        private const int Samples = 3;
        private static readonly Dictionary<MainTab, Sprite> Cache = new Dictionary<MainTab, Sprite>();

        public static Sprite Get(MainTab tab)
        {
            if (Cache.TryGetValue(tab, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }
            Func<float, float, bool> shape = tab == MainTab.Map ? Flag : tab == MainTab.Kingdom ? Castle
                : tab == MainTab.Commanders ? Crown : tab == MainTab.Quests ? Scroll : (Func<float, float, bool>)Bag;
            sprite = Render(shape, tab.ToString());
            Cache[tab] = sprite;
            return sprite;
        }

        private static Sprite Render(Func<float, float, bool> inside, string name)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "TabIcon " + name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < Samples; sy++)
                    {
                        for (int sx = 0; sx < Samples; sx++)
                        {
                            float u = (x + (sx + 0.5f) / Samples) / Size;
                            float v = (y + (sy + 0.5f) / Samples) / Size;
                            hits += inside(u, v) ? 1 : 0;
                        }
                    }
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(255 * hits / (Samples * Samples)));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static bool Rect(float x, float y, float x0, float y0, float x1, float y1) => x >= x0 && x <= x1 && y >= y0 && y <= y1;

        private static bool Circle(float x, float y, float cx, float cy, float r) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;

        private static bool Triangle(float x, float y, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float d1 = (x - bx) * (ay - by) - (ax - bx) * (y - by);
            float d2 = (x - cx) * (by - cy) - (bx - cx) * (y - cy);
            float d3 = (x - ax) * (cy - ay) - (cx - ax) * (y - ay);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0;
            bool pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }

        /// <summary>A pole with a swallow-tail pennant.</summary>
        private static bool Flag(float x, float y)
        {
            if (Rect(x, y, 0.2f, 0.06f, 0.29f, 0.94f) || Circle(x, y, 0.245f, 0.94f, 0.07f))
            {
                return true;
            }
            bool cloth = Rect(x, y, 0.29f, 0.48f, 0.88f, 0.9f);
            bool notch = Triangle(x, y, 0.88f, 0.9f, 0.88f, 0.48f, 0.68f, 0.69f);
            return cloth && !notch;
        }

        /// <summary>A castle: a wide keep, two taller towers with battlements and an arched gate. Kept chunky to read small.</summary>
        private static bool Castle(float x, float y)
        {
            bool body = Rect(x, y, 0.1f, 0.08f, 0.9f, 0.55f) || Rect(x, y, 0.1f, 0.08f, 0.36f, 0.82f) || Rect(x, y, 0.64f, 0.08f, 0.9f, 0.82f);
            bool teeth = Rect(x, y, 0.1f, 0.82f, 0.18f, 0.94f) || Rect(x, y, 0.22f, 0.82f, 0.36f, 0.94f)
                || Rect(x, y, 0.64f, 0.82f, 0.78f, 0.94f) || Rect(x, y, 0.82f, 0.82f, 0.9f, 0.94f)
                || Rect(x, y, 0.4f, 0.55f, 0.6f, 0.67f);
            bool gate = Rect(x, y, 0.4f, 0.08f, 0.6f, 0.3f) || Circle(x, y, 0.5f, 0.3f, 0.1f);
            return (body || teeth) && !gate;
        }

        /// <summary>Emblem of a Rally variant on a commander's crest.</summary>
        public static Sprite Emblem(Logic.Combat.RallyKind kind)
        {
            MainTab key = kind == Logic.Combat.RallyKind.ShieldWall ? (MainTab)10 : kind == Logic.Combat.RallyKind.ArrowRain ? (MainTab)11
                : kind == Logic.Combat.RallyKind.Fireball ? (MainTab)12 : (MainTab)13;
            if (!Cache.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                Func<float, float, bool> shape = kind == Logic.Combat.RallyKind.ShieldWall ? Shield
                    : kind == Logic.Combat.RallyKind.ArrowRain ? Arrows : kind == Logic.Combat.RallyKind.Fireball ? Flame : (Func<float, float, bool>)Crown;
                sprite = Render(shape, "Emblem " + kind);
                Cache[key] = sprite;
            }
            return sprite;
        }

        private static bool Shield(float x, float y)
        {
            bool outline = Rect(x, y, 0.18f, 0.5f, 0.82f, 0.9f) || Triangle(x, y, 0.18f, 0.52f, 0.82f, 0.52f, 0.5f, 0.06f);
            bool cross = Rect(x, y, 0.45f, 0.24f, 0.55f, 0.78f) || Rect(x, y, 0.3f, 0.6f, 0.7f, 0.7f);
            return outline && !cross;
        }

        private static bool Segment(float x, float y, float ax, float ay, float bx, float by, float half)
        {
            float dx = bx - ax, dy = by - ay;
            float t = Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy));
            float px = ax + dx * t - x, py = ay + dy * t - y;
            return px * px + py * py <= half * half;
        }

        /// <summary>Three arrows falling.</summary>
        private static bool Arrows(float x, float y)
        {
            for (int i = 0; i < 3; i++)
            {
                float cx = 0.24f + i * 0.26f;
                float topY = i == 1 ? 0.94f : 0.86f;
                float tipY = i == 1 ? 0.1f : 0.18f;
                if (Segment(x, y, cx + 0.07f, topY, cx, tipY + 0.1f, 0.035f)
                    || Triangle(x, y, cx - 0.09f, tipY + 0.2f, cx + 0.09f, tipY + 0.2f, cx, tipY))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>A flame: a round body, a pointed tip and a hollow heart.</summary>
        private static bool Flame(float x, float y)
        {
            bool outer = Circle(x, y, 0.5f, 0.36f, 0.3f) || Triangle(x, y, 0.22f, 0.45f, 0.78f, 0.45f, 0.5f, 0.95f)
                || Triangle(x, y, 0.2f, 0.5f, 0.42f, 0.4f, 0.28f, 0.72f);
            bool heart = Circle(x, y, 0.5f, 0.3f, 0.12f) || Triangle(x, y, 0.4f, 0.32f, 0.6f, 0.32f, 0.5f, 0.6f);
            return outer && !heart;
        }

        /// <summary>A crown with three points and jewels on the tips.</summary>
        private static bool Crown(float x, float y)
        {
            bool band = Rect(x, y, 0.12f, 0.16f, 0.88f, 0.34f);
            bool spikes = Triangle(x, y, 0.12f, 0.3f, 0.12f, 0.74f, 0.38f, 0.3f)
                || Triangle(x, y, 0.3f, 0.3f, 0.5f, 0.84f, 0.7f, 0.3f)
                || Triangle(x, y, 0.62f, 0.3f, 0.88f, 0.74f, 0.88f, 0.3f);
            bool jewels = Circle(x, y, 0.12f, 0.78f, 0.065f) || Circle(x, y, 0.5f, 0.89f, 0.07f) || Circle(x, y, 0.88f, 0.78f, 0.065f);
            bool studs = Circle(x, y, 0.3f, 0.25f, 0.04f) || Circle(x, y, 0.5f, 0.25f, 0.04f) || Circle(x, y, 0.7f, 0.25f, 0.04f);
            return (band || spikes || jewels) && !studs;
        }

        /// <summary>A quest board: a plank with a clip and three lines of text cut out.</summary>
        private static bool Scroll(float x, float y)
        {
            bool plank = Rect(x, y, 0.18f, 0.06f, 0.82f, 0.86f) || Rect(x, y, 0.36f, 0.82f, 0.64f, 0.96f);
            bool lines = Rect(x, y, 0.3f, 0.64f, 0.7f, 0.72f) || Rect(x, y, 0.3f, 0.46f, 0.7f, 0.54f) || Rect(x, y, 0.3f, 0.28f, 0.58f, 0.36f);
            return plank && !lines;
        }

        /// <summary>A tied money bag with a coin on its belly.</summary>
        private static bool Bag(float x, float y)
        {
            bool belly = Circle(x, y, 0.5f, 0.36f, 0.34f);
            bool neck = Triangle(x, y, 0.3f, 0.5f, 0.7f, 0.5f, 0.5f, 0.8f) || Rect(x, y, 0.38f, 0.62f, 0.62f, 0.82f);
            bool frill = Triangle(x, y, 0.26f, 0.95f, 0.74f, 0.95f, 0.5f, 0.78f);
            bool tie = Rect(x, y, 0.36f, 0.68f, 0.64f, 0.74f);
            bool coin = Circle(x, y, 0.5f, 0.34f, 0.17f) && !Circle(x, y, 0.5f, 0.34f, 0.11f);
            return (belly || neck || frill) && !tie && !coin;
        }
    }
}
