using System;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Battle
{
    /// <summary>
    /// The bar under a replay (PB-28): how far the battle has come, the ticks where Rally was pressed as gold marks,
    /// and a tap anywhere on it to jump there.
    /// </summary>
    public sealed class ReplayTimeline : MonoBehaviour, IPointerClickHandler
    {
        private RectTransform _fill;
        private int _totalTicks = 1;
        private Action<int> _onSeek;

        /// <summary>How much of the bar is filled, 0..1.</summary>
        public float FillFraction => _fill != null ? _fill.anchorMax.x : 0f;

        public static ReplayTimeline Create(Transform parent, BattleReplay replay, BattleRules rules, Action<int> onSeek)
        {
            Image track = Ui.Box(parent, "Replay Timeline", new Color(1f, 1f, 1f, 0.16f), 0.03f, 0.122f, 0.97f, 0.14f);
            track.sprite = UiStyle.Rounded;
            track.type = Image.Type.Sliced;
            var timeline = track.gameObject.AddComponent<ReplayTimeline>();
            timeline._onSeek = onSeek;
            timeline._totalTicks = Mathf.Max(1, replay.Ticks);
            Image fill = Ui.Box(track.transform, "Fill", Palette.Primary, 0f, 0f, 0f, 1f);
            fill.raycastTarget = false;
            timeline._fill = fill.rectTransform;
            foreach (int tick in replay.RallyTicks)
            {
                float x = Mathf.Clamp01(tick / (float)timeline._totalTicks);
                Image mark = Ui.Box(track.transform, "Rally Mark", Palette.Gold, x - 0.006f, -0.25f, x + 0.006f, 1.25f);
                mark.raycastTarget = false;
            }
            return timeline;
        }

        public void SetTick(int tick)
        {
            if (_fill != null)
            {
                _fill.anchorMax = new Vector2(Mathf.Clamp01(tick / (float)_totalTicks), 1f);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var rect = (RectTransform)transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                SeekTo(Mathf.Clamp01((local.x - rect.rect.xMin) / rect.rect.width));
            }
        }

        /// <summary>Jumps to a share of the battle (the tap handler and tests use it).</summary>
        public void SeekTo(float fraction) => _onSeek?.Invoke(Mathf.RoundToInt(Mathf.Clamp01(fraction) * _totalTicks));
    }
}
