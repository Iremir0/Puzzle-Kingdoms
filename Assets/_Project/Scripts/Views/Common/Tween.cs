using System;
using System.Collections;
using UnityEngine;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// Minimal coroutine tweens for the grey-box views (PB-7): no package, scaled
    /// time (so <c>Time.timeScale</c> speeds tests up), and easing curves that
    /// take and return 0..1.
    /// </summary>
    public static class Tween
    {
        /// <summary>
        /// Calls <paramref name="step"/> every frame with the elapsed seconds,
        /// and once more with exactly <paramref name="seconds"/> at the end, so the
        /// last call always lands on the final value.
        /// </summary>
        public static IEnumerator Run(float seconds, Action<float> step)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                step(elapsed);
                yield return null;
                elapsed += Time.deltaTime;
            }
            step(seconds);
        }

        /// <summary>Moves <paramref name="rect"/> from where it is to <paramref name="to"/> with an ease-out.</summary>
        public static IEnumerator Move(RectTransform rect, Vector2 to, float seconds)
        {
            Vector2 from = rect.anchoredPosition;
            return Run(seconds, elapsed =>
            {
                if (rect != null)
                {
                    rect.anchoredPosition = Vector2.LerpUnclamped(from, to, EaseOut(Progress(elapsed, seconds)));
                }
            });
        }

        /// <summary>0..1 share of <paramref name="duration"/> that has passed.</summary>
        public static float Progress(float elapsed, float duration) =>
            duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);

        /// <summary>Fast start, soft stop.</summary>
        public static float EaseOut(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        /// <summary>Slow start, fast end: things that fall.</summary>
        public static float EaseIn(float t) => t * t;

        /// <summary>Soft start and stop.</summary>
        public static float EaseInOut(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

        /// <summary>0 → 1 → 0 hump, for moves that go somewhere and come back.</summary>
        public static float Hump(float t) => Mathf.Sin(t * Mathf.PI);
    }
}
