using System;
using System.Collections.Generic;
using PuzzleKingdoms.Logic.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// Tutorial prompts (PB-13): dims the screen except a hole round the target
    /// element, and shows the text in a bubble next to it. A tap anywhere shows the
    /// next prompt or closes; "Skip tutorial" closes and turns the tutorial off.
    /// The screen below gets no touches while a prompt is up.
    /// </summary>
    public sealed class TutorialOverlay : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color Shade = new Color(0f, 0f, 0f, 0.65f);
        private const float HolePadding = 14f;
        private const float FrameWidth = 6f;

        private readonly Queue<TutorialStep> _queue = new Queue<TutorialStep>();
        private Func<string, RectTransform> _findTarget;
        private Action<TutorialStep> _onShown;
        private Action _onSkip;
        private Action _onClosed;

        private RectTransform _root;
        private RectTransform _top, _bottom, _left, _right, _frame;
        private RectTransform _bubble;
        private Text _text;
        private RectTransform _target;
        private bool _closed;

        /// <summary>The prompt on screen now.</summary>
        public TutorialStep Current { get; private set; }

        public string CurrentText => _text.text;

        public static TutorialOverlay Show(Transform canvasRoot, IEnumerable<TutorialStep> steps,
            Func<string, RectTransform> findTarget, Action<TutorialStep> onShown, Action onSkip, Action onClosed)
        {
            RectTransform root = Ui.Stretch(canvasRoot, "TutorialOverlay");
            root.SetAsLastSibling();
            var overlay = root.gameObject.AddComponent<TutorialOverlay>();
            overlay.Build(findTarget, onShown, onSkip, onClosed);
            overlay.Enqueue(steps);
            overlay.Next();
            return overlay;
        }

        /// <summary>Adds prompts after the ones already waiting.</summary>
        public void Enqueue(IEnumerable<TutorialStep> steps)
        {
            foreach (TutorialStep step in steps)
            {
                _queue.Enqueue(step);
            }
        }

        private void Build(Func<string, RectTransform> findTarget, Action<TutorialStep> onShown, Action onSkip,
            Action onClosed)
        {
            _findTarget = findTarget;
            _onShown = onShown;
            _onSkip = onSkip;
            _onClosed = onClosed;
            _root = (RectTransform)transform;

            // Invisible layer that takes every tap (the shade has a hole, the catcher does not).
            Image catcher = Ui.Box(_root, "Tap Catcher", Color.clear);
            catcher.raycastTarget = true;
            _top = Dim("Shade Top");
            _bottom = Dim("Shade Bottom");
            _left = Dim("Shade Left");
            _right = Dim("Shade Right");
            // A gold border round the target: four thin bars (an Outline effect would fill the hole).
            _frame = Ui.Stretch(_root, "Target Frame");
            Edge(_frame, 0f, 0f, 1f, 0f, new Vector2(0f, FrameWidth), new Vector2(0.5f, 0f));
            Edge(_frame, 0f, 1f, 1f, 1f, new Vector2(0f, FrameWidth), new Vector2(0.5f, 1f));
            Edge(_frame, 0f, 0f, 0f, 1f, new Vector2(FrameWidth, 0f), new Vector2(0f, 0.5f));
            Edge(_frame, 1f, 0f, 1f, 1f, new Vector2(FrameWidth, 0f), new Vector2(1f, 0.5f));

            Image bubble = Ui.Box(_root, "Bubble", Palette.Panel, 0.05f, 0.4f, 0.95f, 0.6f);
            bubble.raycastTarget = false;
            Outline edge = bubble.gameObject.AddComponent<Outline>();
            edge.effectColor = Palette.Gold;
            edge.effectDistance = new Vector2(4, 4);
            _bubble = bubble.rectTransform;
            _text = Ui.Label(_bubble, "", 42, TextAnchor.MiddleLeft, Palette.Text, 0.05f, 0.3f, 0.95f, 0.95f);
            Ui.Label(_bubble, "Tap to continue", 30, TextAnchor.LowerLeft, Palette.Muted, 0.05f, 0.05f, 0.6f, 0.28f);
            Button skip = Ui.Button(_bubble, "Skip tutorial", Palette.Button, Skip, 0.6f, 0.06f, 0.96f, 0.28f, 30);
            skip.gameObject.name = "Skip Tutorial";
        }

        private static void Edge(RectTransform frame, float xMin, float yMin, float xMax, float yMax, Vector2 size, Vector2 pivot)
        {
            Image bar = Ui.Box(frame, "Edge", Palette.Gold, xMin, yMin, xMax, yMax);
            bar.raycastTarget = false;
            bar.rectTransform.pivot = pivot;
            bar.rectTransform.sizeDelta = size;
            bar.rectTransform.anchoredPosition = Vector2.zero;
        }

        private RectTransform Dim(string name)
        {
            Image shade = Ui.Box(_root, name, Shade);
            shade.raycastTarget = false;
            return shade.rectTransform;
        }

        public void OnPointerClick(PointerEventData eventData) => Next();

        /// <summary>Shows the next prompt, or closes when there is none.</summary>
        public void Next()
        {
            if (_closed)
            {
                return;
            }
            if (_queue.Count == 0)
            {
                Close();
                return;
            }
            Current = _queue.Dequeue();
            _text.text = Current.Text;
            _target = string.IsNullOrEmpty(Current.Target) ? null : _findTarget?.Invoke(Current.Target);
            _onShown?.Invoke(Current);
            Layout();
        }

        public void Skip()
        {
            if (_closed)
            {
                return;
            }
            _queue.Clear();
            _onSkip?.Invoke();
            Close();
        }

        private void Close()
        {
            _closed = true;
            Destroy(gameObject);
            _onClosed?.Invoke();
        }

        // The target may move (layout, fitters); keep the hole on it.
        private void LateUpdate() => Layout();

        private void Layout()
        {
            if (_target == null)
            {
                SetArea(_top, 0f, 0f, 1f, 1f);
                SetArea(_bottom, 0f, 0f, 0f, 0f);
                SetArea(_left, 0f, 0f, 0f, 0f);
                SetArea(_right, 0f, 0f, 0f, 0f);
                _frame.gameObject.SetActive(false);
                Ui.SetAnchors(_bubble, 0.05f, 0.4f, 0.95f, 0.6f);
                return;
            }

            Rect area = _root.rect;
            var corners = new Vector3[4];
            _target.GetWorldCorners(corners);
            Vector2 min = _root.InverseTransformPoint(corners[0]);
            Vector2 max = _root.InverseTransformPoint(corners[2]);
            float xMin = Mathf.Clamp01((min.x - HolePadding - area.xMin) / area.width);
            float yMin = Mathf.Clamp01((min.y - HolePadding - area.yMin) / area.height);
            float xMax = Mathf.Clamp01((max.x + HolePadding - area.xMin) / area.width);
            float yMax = Mathf.Clamp01((max.y + HolePadding - area.yMin) / area.height);

            SetArea(_bottom, 0f, 0f, 1f, yMin);
            SetArea(_top, 0f, yMax, 1f, 1f);
            SetArea(_left, 0f, yMin, xMin, yMax);
            SetArea(_right, xMax, yMin, 1f, yMax);
            _frame.gameObject.SetActive(true);
            SetArea(_frame, xMin, yMin, xMax, yMax);

            // Bubble on the side of the screen with more room.
            const float height = 0.2f;
            bool below = (yMin + yMax) * 0.5f > 0.5f;
            float top = below ? Mathf.Max(height + 0.02f, yMin - 0.02f) : Mathf.Min(0.98f, yMax + 0.02f + height);
            Ui.SetAnchors(_bubble, 0.05f, top - height, 0.95f, top);
        }

        private static void SetArea(RectTransform rect, float xMin, float yMin, float xMax, float yMax) =>
            Ui.SetAnchors(rect, xMin, yMin, xMax, yMax);
    }
}
