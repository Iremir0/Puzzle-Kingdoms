using PuzzleKingdoms.Logic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PuzzleKingdoms.Views.Puzzle
{
    /// <summary>Input on one board cell: tap to select, or swipe towards a neighbour.</summary>
    public sealed class GemCell : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        /// <summary>Swipe length in reference pixels (1080-wide canvas) before it counts.</summary>
        private const float SwipeThreshold = 40f;

        private GridPos _cell;
        private PuzzleScreen _screen;
        private Vector2 _pressPosition;
        private bool _swiped;

        public void Init(PuzzleScreen screen, GridPos cell)
        {
            _screen = screen;
            _cell = cell;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressPosition = eventData.position;
            _swiped = false;
            _screen.OnCellPressed(_cell);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_swiped)
            {
                return;
            }
            Canvas canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            Vector2 delta = (eventData.position - _pressPosition) / scale;
            if (delta.magnitude < SwipeThreshold)
            {
                return;
            }
            _swiped = true;
            // Board y grows downwards, screen y upwards.
            GridPos direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? (delta.x > 0 ? GridPos.Right : GridPos.Left)
                : (delta.y > 0 ? GridPos.Up : GridPos.Down);
            _screen.OnCellSwiped(_cell, direction);
        }
    }
}
