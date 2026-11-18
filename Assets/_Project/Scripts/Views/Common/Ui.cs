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

        /// <summary>Unity's built-in font; grey-box only (TextMeshPro comes with real UI).</summary>
        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

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
            return label;
        }

        /// <summary>Colored button with a centered caption.</summary>
        public static Button Button(Transform parent, string caption, Color color, Action onClick,
            float xMin, float yMin, float xMax, float yMax, int fontSize = 44)
        {
            Image image = Box(parent, "Button " + caption, color, xMin, yMin, xMax, yMax);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }
            Label(image.transform, caption, fontSize, TextAnchor.MiddleCenter, Palette.Text);
            return button;
        }

        public static void SetCaption(Button button, string caption) =>
            button.GetComponentInChildren<Text>().text = caption;

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
