using System.Collections.Generic;
using PuzzleKingdoms.Data;
using UnityEngine;
using UnityEngine.UI;

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

        /// <summary>
        /// Margin of a unit icon inside its cell: tier 2 and 3 are drawn about 10% and
        /// 20% larger than tier 1, like in the battle (PB-3).
        /// </summary>
        public static float Inset(int tier) => tier >= 3 ? 0.04f : tier == 2 ? 0.08f : 0.12f;

        /// <summary>Places <paramref name="icon"/> inside its parent with the margin for <paramref name="tier"/>.</summary>
        public static void FitIcon(RectTransform icon, int tier)
        {
            float inset = Inset(tier);
            Ui.SetAnchors(icon, inset, inset, 1f - inset, 1f - inset);
        }

        /// <summary>A white silhouette of <paramref name="look"/> filling <paramref name="parent"/>; it never blocks clicks.</summary>
        public static Image AddIcon(Transform parent, UnitLook look, int tier)
        {
            Image icon = Ui.Box(parent, "Silhouette", Color.white);
            icon.sprite = Get(look);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            FitIcon(icon.rectTransform, tier);
            return icon;
        }

        /// <summary>"T1" in the lower-right corner of a unit cell or bench button.</summary>
        public static Text AddTierLabel(Transform parent, int tier, int fontSize)
        {
            Text label = Ui.Label(parent, "T" + tier, fontSize, TextAnchor.LowerRight, Color.white, 0.4f, 0.02f, 0.96f, 0.4f);
            label.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            // Small grids (the enemy preview) shrink the text instead of wrapping "T1".
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = fontSize;
            return label;
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
