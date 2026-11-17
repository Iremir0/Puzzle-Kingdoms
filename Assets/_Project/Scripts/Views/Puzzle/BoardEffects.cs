using System.Collections;
using System.Collections.Generic;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Puzzle
{
    /// <summary>
    /// Short visual effects drawn over the board (PB-8): the Blast ring, Prism
    /// beams, stone chips, the flash of a new special gem and the board shake.
    /// Every effect lasts at most 0.3 s, removes itself, and runs on this layer's
    /// own coroutines so a new swap never cuts one off halfway.
    /// </summary>
    public sealed class BoardEffects : MonoBehaviour
    {
        private const float RingSeconds = 0.28f;
        private const float BeamSeconds = 0.3f;
        private const float ChipSeconds = 0.3f;
        private const int ChipCount = 5;
        private const float FlashSeconds = 0.25f;
        private static readonly Color PrismBeam = new Color(1f, 0.95f, 1f, 0.95f);
        private static readonly Color StoneChip = new Color(0.62f, 0.62f, 0.66f, 1f);

        private RectTransform _layer;
        private RectTransform _shakeTarget;
        private int _columns;
        private int _rows;
        private Vector2 _restPosition;
        private Coroutine _shake;
        private int _running;

        /// <summary>Effects still on screen (tests wait for 0).</summary>
        public int Running => _running;

        /// <summary>Creates the layer over <paramref name="board"/>; it must be the board's last child to draw on top.</summary>
        public static BoardEffects Create(RectTransform board, RectTransform shakeTarget, int columns, int rows)
        {
            RectTransform layer = Ui.Stretch(board, "Effects");
            var effects = layer.gameObject.AddComponent<BoardEffects>();
            effects._layer = layer;
            effects._shakeTarget = shakeTarget;
            effects._restPosition = shakeTarget.anchoredPosition;
            effects._columns = columns;
            effects._rows = rows;
            return effects;
        }

        private Vector2 CellSize
        {
            get
            {
                Rect rect = _layer.rect;
                return new Vector2(rect.width / _columns, rect.height / _rows);
            }
        }

        /// <summary>Centre of a board cell in this layer's units (origin in the middle, y up).</summary>
        private Vector2 Centre(GridPos cell)
        {
            Rect rect = _layer.rect;
            Vector2 size = CellSize;
            return new Vector2((cell.X + 0.5f) * size.x - rect.width / 2f, rect.height / 2f - (cell.Y + 0.5f) * size.y);
        }

        /// <summary>An image centred on a point of the layer, not catching touches.</summary>
        private Image Piece(string name, Sprite sprite, Color color, Vector2 position, Vector2 size)
        {
            RectTransform rect = Ui.Rect(_layer, name, 0.5f, 0.5f, 0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = sprite != null;
            return image;
        }

        /// <summary>Blast: a ring grows from one cell to cover the 3×3 area and fades.</summary>
        public void Ring(GridPos origin, Color color)
        {
            Vector2 cell = CellSize;
            Image ring = Piece("Blast Ring", GemShapes.Get(GemShape.Ring), color, Centre(origin), cell);
            Run(ring.gameObject, RingSeconds, t =>
            {
                float grow = Tween.EaseOut(t);
                ring.rectTransform.sizeDelta = cell * Mathf.Lerp(1f, 3.4f, grow);
                ring.color = new Color(color.r, color.g, color.b, 1f - t * t);
            });
        }

        /// <summary>Prism: a beam shoots from the Prism to every gem it clears, then fades.</summary>
        public void Beams(GridPos origin, IEnumerable<GridPos> targets)
        {
            Vector2 from = Centre(origin);
            float thickness = CellSize.x * 0.12f;
            foreach (GridPos target in targets)
            {
                Vector2 delta = Centre(target) - from;
                Image beam = Piece("Prism Beam", null, PrismBeam, from, new Vector2(0f, thickness));
                RectTransform rect = beam.rectTransform;
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = from;
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                float length = delta.magnitude;
                Run(beam.gameObject, BeamSeconds, t =>
                {
                    rect.sizeDelta = new Vector2(length * Tween.EaseOut(Mathf.Clamp01(t / 0.4f)), thickness * (1f - 0.5f * t));
                    beam.color = new Color(PrismBeam.r, PrismBeam.g, PrismBeam.b, PrismBeam.a * (t < 0.4f ? 1f : 1f - (t - 0.4f) / 0.6f));
                });
            }
        }

        /// <summary>Stone cracks: a few grey chips fly off the cell, spin and fade.</summary>
        public void Chips(GridPos cell)
        {
            Vector2 size = CellSize;
            Vector2 centre = Centre(cell);
            for (int i = 0; i < ChipCount; i++)
            {
                // Spread evenly with a per-cell twist, so neighbours do not look identical.
                float angle = (i * 360f / ChipCount + cell.X * 37f + cell.Y * 53f) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float chip = size.x * (0.12f + 0.04f * (i % 2));
                Image piece = Piece("Stone Chip", null, StoneChip, centre, new Vector2(chip, chip));
                RectTransform rect = piece.rectTransform;
                float spin = (i % 2 == 0 ? 1f : -1f) * 220f;
                Run(piece.gameObject, ChipSeconds, t =>
                {
                    // Out fast, then a little gravity.
                    rect.anchoredPosition = centre + direction * size.x * 0.55f * Tween.EaseOut(t)
                                            + Vector2.down * size.y * 0.25f * t * t;
                    rect.localRotation = Quaternion.Euler(0f, 0f, spin * t);
                    piece.color = new Color(StoneChip.r, StoneChip.g, StoneChip.b, 1f - t);
                });
            }
        }

        /// <summary>A new special gem flashes white once.</summary>
        public void Flash(GridPos cell, Sprite gemSprite)
        {
            Vector2 size = CellSize * 0.98f;
            Image flash = Piece("Special Flash", gemSprite, Color.white, Centre(cell), size);
            Run(flash.gameObject, FlashSeconds, t =>
            {
                flash.rectTransform.sizeDelta = size * (1f + 0.25f * t);
                flash.color = new Color(1f, 1f, 1f, 0.9f * (1f - t));
            });
        }

        /// <summary>Shakes the board by up to <paramref name="pixels"/>, fading out; a new shake replaces the old one.</summary>
        public void Shake(float pixels, float seconds)
        {
            if (_shake != null)
            {
                StopCoroutine(_shake);
                _running--;
            }
            _running++;
            _shake = StartCoroutine(ShakeRoutine(pixels, seconds));
        }

        private IEnumerator ShakeRoutine(float pixels, float seconds)
        {
            yield return Tween.Run(seconds, elapsed =>
            {
                float t = Tween.Progress(elapsed, seconds);
                float strength = pixels * (1f - t);
                _shakeTarget.anchoredPosition = _restPosition + new Vector2(
                    Mathf.Sin(t * 47f) * strength, Mathf.Cos(t * 61f) * strength * 0.6f);
            });
            _shakeTarget.anchoredPosition = _restPosition;
            _shake = null;
            _running--;
        }

        private void Run(GameObject piece, float seconds, System.Action<float> step)
        {
            _running++;
            StartCoroutine(RunRoutine(piece, seconds, step));
        }

        private IEnumerator RunRoutine(GameObject piece, float seconds, System.Action<float> step)
        {
            step(0f);
            yield return Tween.Run(seconds, elapsed => step(Tween.Progress(elapsed, seconds)));
            Destroy(piece);
            _running--;
        }
    }
}
