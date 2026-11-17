using System;
using PuzzleKingdoms.Logic.Army;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.WarCamp
{
    /// <summary>Receives the drags that <see cref="DraggableUnit"/> starts (the War Camp screen).</summary>
    public interface IUnitDragHandler
    {
        /// <summary>Returns false to refuse the drag (for example while an animation runs).</summary>
        bool BeginUnitDrag(ArmyUnit unit, RectTransform source, PointerEventData eventData);
        void UnitDrag(PointerEventData eventData);
        void EndUnitDrag(PointerEventData eventData);
    }

    /// <summary>
    /// Put on a bench tile or a formation cell to make its unit draggable (PB-9).
    /// On a bench tile inside a <see cref="ScrollRect"/>, a mostly sideways drag
    /// scrolls the bench instead, so the bench stays scrollable.
    /// </summary>
    public sealed class DraggableUnit : MonoBehaviour,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Func<ArmyUnit> _unit;
        private IUnitDragHandler _handler;
        private ScrollRect _scroll;
        private Mode _mode;

        private enum Mode { None, Unit, Scroll }

        public static DraggableUnit Add(GameObject target, Func<ArmyUnit> unit, IUnitDragHandler handler,
            ScrollRect scroll = null)
        {
            var draggable = target.AddComponent<DraggableUnit>();
            draggable._unit = unit;
            draggable._handler = handler;
            draggable._scroll = scroll;
            return draggable;
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (_scroll != null)
            {
                _scroll.OnInitializePotentialDrag(eventData);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _mode = Mode.None;
            eventData.eligibleForClick = false; // a drag is never also a tap
            ArmyUnit unit = _unit?.Invoke();
            if (_scroll != null && (unit == null || ScrollsInstead(eventData)))
            {
                _mode = Mode.Scroll;
                _scroll.OnBeginDrag(eventData);
                return;
            }
            if (unit != null && _handler != null && _handler.BeginUnitDrag(unit, (RectTransform)transform, eventData))
            {
                _mode = Mode.Unit;
            }
        }

        /// <summary>
        /// A drag scrolls the bench only when the bench is wider than its panel and the
        /// finger moves clearly sideways; diagonal drags towards the grid move the unit.
        /// </summary>
        private bool ScrollsInstead(PointerEventData eventData)
        {
            bool overflows = _scroll.content.rect.width > _scroll.viewport.rect.width + 0.5f;
            Vector2 moved = eventData.position - eventData.pressPosition;
            return overflows && Mathf.Abs(moved.x) > SidewaysRatio * Mathf.Abs(moved.y);
        }

        /// <summary>How much more sideways than up or down a drag must be to scroll the bench.</summary>
        public const float SidewaysRatio = 2f;

        public void OnDrag(PointerEventData eventData)
        {
            if (_mode == Mode.Scroll)
            {
                _scroll.OnDrag(eventData);
            }
            else if (_mode == Mode.Unit)
            {
                _handler.UnitDrag(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Mode mode = _mode;
            _mode = Mode.None;
            if (mode == Mode.Scroll)
            {
                _scroll.OnEndDrag(eventData);
            }
            else if (mode == Mode.Unit)
            {
                _handler.EndUnitDrag(eventData);
            }
        }
    }
}
