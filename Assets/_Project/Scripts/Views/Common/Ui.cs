using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// Builds grey-box uGUI from code (D-025): rectangles placed with normalized
    /// anchors, so screens need no prefabs or scene edits while the design moves.
    /// </summary>
    public static class Ui
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

        /// <summary>
        /// The type scale (PB-16), in pixels at the 1080×1920 reference: every label and button caption is snapped to
        /// the nearest of these, so the screens share six sizes instead of seventeen.
        /// </summary>
        public static class Type
        {
            public const int Small = 30;
            public const int Body = 38;
            public const int Action = 44;
            public const int Heading = 52;
            public const int Title = 64;
            public const int Display = 90;

            private static readonly int[] Steps = { Small, Body, Action, Heading, Title, Display };

            /// <summary>The scale step nearest to <paramref name="size"/> (a tie goes to the larger step).</summary>
            public static int Snap(int size)
            {
                int best = Steps[0];
                foreach (int step in Steps)
                {
                    if (Mathf.Abs(step - size) <= Mathf.Abs(best - size))
                    {
                        best = step;
                    }
                }
                return best;
            }
        }

        /// <summary>Spacing scale on an 8 px grid, in pixels at the reference resolution.</summary>
        public static class Space
        {
            public const int Small = 8;
            public const int Medium = 16;
            public const int Large = 32;
        }

        private static Font _font;

        /// <summary>The UI font (PB-16): Lilita One, loaded from <c>Resources/UiFont</c>; the built-in font if it is missing.</summary>
        public static Font Font => _font != null ? _font : (_font = Resources.Load<Font>("UiFont") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        /// <summary>Portrait overlay canvas scaled from 1080×1920.</summary>
        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        /// <summary>
        /// The rect inside <paramref name="canvas"/> that keeps clear of notches and the gesture bar (PB-15): build
        /// screens under it. Backgrounds and full-screen shades still reach the screen edges through <see cref="FullBleed"/>.
        /// </summary>
        public static RectTransform CreateSafeArea(Canvas canvas)
        {
            RectTransform safe = Stretch(canvas.transform, "SafeArea");
            safe.gameObject.AddComponent<SafeArea>();
            return safe;
        }

        /// <summary>A full-screen dimmed layer that swallows taps (pop-ups); it covers the notch strips too.</summary>
        public static Image Shade(Transform parent, string name, float alpha)
        {
            Image shade = Box(parent, name, new Color(0f, 0f, 0f, alpha));
            shade.gameObject.AddComponent<FullBleed>();
            return shade;
        }

        /// <summary>Creates an EventSystem if the scene has none (needed for clicks).</summary>
        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        /// <summary>Empty rect covering the normalized area [xMin..xMax] × [yMin..yMax] of its parent (y up).</summary>
        public static RectTransform Rect(Transform parent, string name, float xMin, float yMin, float xMax, float yMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            SetAnchors(rect, xMin, yMin, xMax, yMax);
            return rect;
        }

        public static RectTransform Stretch(Transform parent, string name) => Rect(parent, name, 0, 0, 1, 1);

        public static void SetAnchors(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Solid colored rectangle.</summary>
        public static Image Box(Transform parent, string name, Color color, float xMin = 0, float yMin = 0, float xMax = 1, float yMax = 1)
        {
            RectTransform rect = Rect(parent, name, xMin, yMin, xMax, yMax);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>A rounded panel with a soft drop shadow: the look of every card, box and bar.</summary>
        public static Image Card(Transform parent, string name, Color color, float xMin = 0, float yMin = 0, float xMax = 1, float yMax = 1)
        {
            Image image = Box(parent, name, color, xMin, yMin, xMax, yMax);
            image.sprite = UiStyle.Rounded;
            image.type = Image.Type.Sliced;
            Shadow shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.38f);
            shadow.effectDistance = new Vector2(0f, -8f);
            return image;
        }

        /// <summary>The screen background: a deep blue gradient with three soft coloured glows, so no screen is a flat slab.</summary>
        public static Image Backdrop(Transform parent)
        {
            Image background = Box(parent, "Background", Color.white);
            background.gameObject.AddComponent<FullBleed>();
            background.sprite = UiStyle.Gradient(Palette.Hex("2E3860"), Palette.Hex("11141E"));
            AddGlow(background.transform, 0.12f, 0.88f, 1100f, new Color(0.24f, 0.48f, 0.85f, 0.30f));
            AddGlow(background.transform, 0.95f, 0.5f, 1000f, new Color(0.55f, 0.32f, 0.85f, 0.22f));
            AddGlow(background.transform, 0.15f, 0.12f, 1000f, new Color(0.2f, 0.7f, 0.65f, 0.18f));
            return background;
        }

        private static void AddGlow(Transform parent, float x, float y, float size, Color color)
        {
            Image glow = Box(parent, "Glow", color, x, y, x, y);
            glow.sprite = UiStyle.Glow;
            glow.raycastTarget = false;
            glow.rectTransform.sizeDelta = new Vector2(size, size);
        }

        /// <summary>Text label; does not block clicks.</summary>
        public static Text Label(Transform parent, string text, int size, TextAnchor alignment, Color color,
            float xMin = 0, float yMin = 0, float xMax = 1, float yMax = 1)
        {
            RectTransform rect = Rect(parent, "Label", xMin, yMin, xMax, yMax);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = Type.Snap(size);
            label.alignment = alignment;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            if (label.fontSize >= 44)
            {
                // Big text gets a soft shadow so it stands off the background.
                Shadow shadow = rect.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
                shadow.effectDistance = new Vector2(0f, -4f);
            }
            return label;
        }

        /// <summary>Button looks (PB-16): the three colours every button uses, all from <see cref="Palette"/>.</summary>
        public enum ButtonStyle
        {
            /// <summary>The one main action of a screen (Play, Fight, Continue): blue.</summary>
            Primary,
            /// <summary>Any other action: grey-blue.</summary>
            Secondary,
            /// <summary>Destructive or committing action (Reset progress, Fight): red.</summary>
            Danger,
        }

        /// <summary>The fill colour of a button style.</summary>
        public static Color StyleColor(ButtonStyle style) =>
            style == ButtonStyle.Primary ? Palette.Primary : style == ButtonStyle.Danger ? Palette.Bad : Palette.Button;

        /// <summary>A button in one of the three styles (with the shared pressed look).</summary>
        public static Button Button(Transform parent, string caption, ButtonStyle style, Action onClick,
            float xMin, float yMin, float xMax, float yMax, int fontSize = 44) =>
            Button(parent, caption, StyleColor(style), onClick, xMin, yMin, xMax, yMax, fontSize);

        /// <summary>
        /// Colored button with a centered caption. The caption is the English text: it is shown in the
        /// current language (<see cref="Loc"/>) and the object keeps the English name ("Button Merge all").
        /// </summary>
        public static Button Button(Transform parent, string caption, Color color, Action onClick,
            float xMin, float yMin, float xMax, float yMax, int fontSize = 44)
        {
            Image image = Box(parent, "Button " + caption, color, xMin, yMin, xMax, yMax);
            image.sprite = UiStyle.Rounded;
            image.type = Image.Type.Sliced;
            Shadow lift = image.gameObject.AddComponent<Shadow>();
            lift.effectColor = new Color(0f, 0f, 0f, 0.42f);
            lift.effectDistance = new Vector2(0f, -7f);
            Image gloss = Box(image.transform, "Gloss", new Color(1f, 1f, 1f, 0.10f), 0.03f, 0.52f, 0.97f, 0.95f);
            gloss.sprite = UiStyle.Rounded;
            gloss.type = Image.Type.Sliced;
            gloss.raycastTarget = false;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            image.gameObject.AddComponent<PressFeel>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f); // the shared pressed look (PB-16)
            colors.colorMultiplier = 1f;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }
            Label(image.transform, Loc.T(caption), fontSize, TextAnchor.MiddleCenter, Palette.Text);
            return button;
        }

        /// <summary>Changes a button's caption; like <see cref="Button(Transform, string, Color, Action, float, float, float, float, int)"/> it takes the English text.</summary>
        public static void SetCaption(Button button, string caption) =>
            button.GetComponentInChildren<Text>().text = Loc.T(caption);

        /// <summary>Destroys all children (at the end of the frame).</summary>
        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
