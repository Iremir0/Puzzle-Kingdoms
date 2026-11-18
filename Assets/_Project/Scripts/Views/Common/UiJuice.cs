using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>Small touches that make the code-built UI feel alive (D-058): press squash, pop-ins, confetti, pulses.</summary>
    public sealed class PressFeel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float PressedScale = 0.94f;
        private float _target = 1f;
        private float _scale = 1f;
        private float _velocity;
        private bool _touched;

        public void OnPointerDown(PointerEventData eventData) => Set(PressedScale);

        public void OnPointerUp(PointerEventData eventData) => Set(1f);

        public void OnPointerExit(PointerEventData eventData) => Set(1f);

        private void Set(float target)
        {
            _target = target;
            _touched = true;
        }

        private void Update()
        {
            if (!_touched)
            {
                return;
            }
            // A spring: pressing squashes fast, letting go overshoots a little and settles.
            float force = (_target - _scale) * 520f - _velocity * 22f;
            _velocity += force * Time.unscaledDeltaTime;
            _scale += _velocity * Time.unscaledDeltaTime;
            transform.localScale = new Vector3(_scale, _scale, 1f);
            if (Mathf.Abs(_target - _scale) < 0.001f && Mathf.Abs(_velocity) < 0.01f)
            {
                _scale = _target;
                transform.localScale = new Vector3(_scale, _scale, 1f);
                _touched = false;
            }
        }
    }

    /// <summary>Confetti piece: falls with a sway and a spin, then removes itself.</summary>
    public sealed class ConfettiPiece : MonoBehaviour
    {
        public Vector2 Velocity;
        public float Spin;
        public float Life = 2.8f;
        private float _age;
        private Image _image;

        private void Awake() => _image = GetComponent<Image>();

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _age += dt;
            Velocity.y -= 900f * dt;
            Velocity.x *= 1f - 1.2f * dt;
            var rect = (RectTransform)transform;
            rect.anchoredPosition += Velocity * dt;
            rect.Rotate(0f, 0f, Spin * dt);
            if (_image != null)
            {
                Color c = _image.color;
                c.a = Mathf.Clamp01((Life - _age) / 0.6f);
                _image.color = c;
            }
            if (_age >= Life)
            {
                Destroy(gameObject);
            }
        }
    }

    public static class UiJuice
    {
        private static readonly Color[] ConfettiColors =
        {
            new Color(0.96f, 0.75f, 0.31f), new Color(0.35f, 0.62f, 0.95f), new Color(0.9f, 0.4f, 0.45f),
            new Color(0.4f, 0.85f, 0.55f), new Color(0.75f, 0.55f, 0.95f),
        };

        /// <summary>A shower of confetti shot up from the bottom corners and the middle top, then falling.</summary>
        public static void Confetti(Transform parent, int count)
        {
            RectTransform host = Ui.Rect(parent, "Confetti", 0f, 0f, 1f, 1f);
            host.SetAsLastSibling();
            for (int i = 0; i < count; i++)
            {
                Image piece = Ui.Box(host, "Piece", ConfettiColors[Random.Range(0, ConfettiColors.Length)]);
                piece.raycastTarget = false;
                var rect = piece.rectTransform;
                bool fromSide = i % 3 != 0;
                float x = fromSide ? (i % 2 == 0 ? 0.05f : 0.95f) : Random.Range(0.3f, 0.7f);
                float y = fromSide ? 0.25f : 0.95f;
                rect.anchorMin = rect.anchorMax = new Vector2(x, y);
                rect.sizeDelta = new Vector2(Random.Range(14f, 26f), Random.Range(24f, 40f));
                rect.anchoredPosition = Vector2.zero;
                rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                var motion = piece.gameObject.AddComponent<ConfettiPiece>();
                float dir = fromSide ? (x < 0.5f ? 1f : -1f) : Random.Range(-1f, 1f);
                motion.Velocity = fromSide ? new Vector2(dir * Random.Range(250f, 700f), Random.Range(900f, 1700f))
                    : new Vector2(dir * 260f, Random.Range(-100f, 250f));
                motion.Spin = Random.Range(-420f, 420f);
                motion.Life = Random.Range(2.2f, 3.4f);
            }
        }

        /// <summary>Scales a rectangle in from nothing with a small overshoot, after an optional delay.</summary>
        public static IEnumerator PopIn(RectTransform rect, float delay = 0f, float seconds = 0.45f)
        {
            if (rect == null)
            {
                yield break;
            }
            rect.localScale = Vector3.zero;
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }
            float elapsed = 0f;
            while (elapsed < seconds && rect != null)
            {
                float t = elapsed / seconds - 1f;
                float s = 1f + 2.70158f * t * t * t + 1.70158f * t * t; // ease-out-back
                rect.localScale = new Vector3(s, s, 1f);
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            if (rect != null)
            {
                rect.localScale = Vector3.one;
            }
        }

        /// <summary>Makes a rectangle breathe in size and brightness (the current stage on the map).</summary>
        public static void Pulse(RectTransform rect, float amount = 0.08f, float speed = 3f)
        {
            rect.gameObject.AddComponent<Pulser>().Setup(amount, speed);
        }

        private sealed class Pulser : MonoBehaviour
        {
            private float _amount;
            private float _speed;
            private Vector3 _rest;

            public void Setup(float amount, float speed)
            {
                _amount = amount;
                _speed = speed;
                _rest = transform.localScale;
            }

            private void Update()
            {
                float s = 1f + _amount * Mathf.Sin(Time.unscaledTime * _speed);
                transform.localScale = _rest * s;
            }
        }
    }
}
