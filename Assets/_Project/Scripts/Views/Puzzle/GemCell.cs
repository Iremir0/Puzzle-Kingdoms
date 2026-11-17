using PuzzleKingdoms.Logic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PuzzleKingdoms.Views.Puzzle
{
    /// <summary>
    /// Input on one board cell: tap to select, or swipe towards a neighbour.
    /// While the finger drags, the gem follows it along one axis; the swipe counts
    /// once it moved <see cref="SwipeShare"/> of a cell, letting go earlier snaps
    /// the gem back.
    /// </summary>
    public sealed class GemCell : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        /// <summary>Share of a cell the finger must move before a swipe counts (tune on a phone).</summary>
        private const float SwipeShare = 0.3f;
        /// <summary>The gem follows the finger at most this share of a cell.</summary>
        private const float FollowShare = 0.5f;

        private GridPos _cell;
        private PuzzleScreen _screen;
        private RectTransform _gem;
        private Vector2 _pressLocal;
        private bool _swiped;

        public void Init(PuzzleScreen screen, GridPos cell, RectTransform gem)
        {
            _screen = screen;
            _cell = cell;
            _gem = gem;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressLocal = ToLocal(eventData);
            _swiped = false;
            _screen.OnCellPressed(_cell);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_swiped || !_screen.AcceptsInput)
            {
                return;
            }
            Vector2 delta = ToLocal(eventData) - _pressLocal;
            float size = ((RectTransform)transform).rect.width;
            bool horizontal = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);
            float along = horizontal ? delta.x : delta.y;
            if (Mathf.Abs(along) >= size * SwipeShare)
            {
                _swiped = true;
                // Board y grows downwards, screen y upwards.
                GridPos direction = horizontal
                    ? (delta.x > 0 ? GridPos.Right : GridPos.Left)
                    : (delta.y > 0 ? GridPos.Up : GridPos.Down);
                _screen.OnCellSwiped(_cell, direction);
                return;
            }
            float follow = Mathf.Clamp(along, -size * FollowShare, size * FollowShare);
            _screen.DragGem(_cell, horizontal ? new Vector2(follow, 0f) : new Vector2(0f, follow));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_swiped && _gem != null)
            {
                _screen.ReleaseGem(_cell);
            }
        }

        /// <summary>Pointer position in this cell's own units (the cell never moves, only its gem).</summary>
        private Vector2 ToLocal(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position,
                eventData.pressEventCamera, out Vector2 local);
            return local;
        }
    }
}
