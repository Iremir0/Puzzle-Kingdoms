using UnityEngine;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// Fits its RectTransform to <see cref="Screen.safeArea"/> (PB-15), so buttons and labels stay clear of notches,
    /// punch-hole cameras and the gesture bar. Every screen is built under this rect; backgrounds and dimming shades that
    /// should still cover the whole screen add <see cref="FullBleed"/>.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        private Rect _applied;
        private Vector2Int _screen;

        private void Awake() => Apply();

        private void Update() => Apply();

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (safe == _applied && screen == _screen)
            {
                return;
            }
            _applied = safe;
            _screen = screen;
            if (screen.x <= 0 || screen.y <= 0)
            {
                return;
            }
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(safe.xMin / screen.x, safe.yMin / screen.y);
            rect.anchorMax = new Vector2(safe.xMax / screen.x, safe.yMax / screen.y);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
