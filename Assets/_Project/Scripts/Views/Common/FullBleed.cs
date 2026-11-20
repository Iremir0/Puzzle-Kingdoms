using UnityEngine;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// Stretches its RectTransform over the whole canvas even when it sits inside the <see cref="SafeArea"/> (PB-15):
    /// used for screen backgrounds and full-screen shades, so the strips behind the notch and the gesture bar are not
    /// left bare. Assumes no scaling or rotation between the canvas and this rect, as in the code-built UI.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class FullBleed : MonoBehaviour
    {
        private readonly Vector3[] _corners = new Vector3[4];
        private RectTransform _canvas;
        private Rect _parentRect;
        private Rect _canvasRect;
        private Vector3 _parentPosition;

        private void OnEnable() => Fit(true);

        private void LateUpdate() => Fit(false);

        private void Fit(bool force)
        {
            var self = (RectTransform)transform;
            var parent = self.parent as RectTransform;
            if (parent == null)
            {
                return;
            }
            if (_canvas == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    return;
                }
                _canvas = (RectTransform)canvas.rootCanvas.transform;
            }
            // Only redo the maths when the parent or the canvas changed size or place (rotation, resize, a shake).
            if (!force && parent.rect == _parentRect && _canvas.rect == _canvasRect && parent.position == _parentPosition)
            {
                return;
            }
            _parentRect = parent.rect;
            _canvasRect = _canvas.rect;
            _parentPosition = parent.position;

            _canvas.GetWorldCorners(_corners);
            Vector2 min = parent.InverseTransformPoint(_corners[0]);
            Vector2 max = parent.InverseTransformPoint(_corners[2]);
            self.anchorMin = Vector2.zero;
            self.anchorMax = Vector2.one;
            self.offsetMin = min - _parentRect.min;
            self.offsetMax = max - _parentRect.max;
        }
    }
}
