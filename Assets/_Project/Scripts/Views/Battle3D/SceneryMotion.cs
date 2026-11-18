using UnityEngine;

namespace PuzzleKingdoms.Views.Battle3D
{
    /// <summary>
    /// Ambient life for the battlefield (D-056): a prop sways in the wind around its base. Trees lean slowly,
    /// bushes tremble a little, banner cloth flutters faster. Purely visual, no state, runs only while playing.
    /// </summary>
    public sealed class SceneryMotion : MonoBehaviour
    {
        public float Degrees = 1.5f;
        public float Speed = 1f;
        public Vector3 Axis = Vector3.forward;
        /// <summary>Extra twist around the up axis (flags).</summary>
        public float YawDegrees;

        private Quaternion _rest;
        private float _phase;

        private void Awake()
        {
            _rest = transform.localRotation;
            _phase = Random.value * 6.28f;
        }

        private void Update()
        {
            float t = Time.time * Speed + _phase;
            float sway = Mathf.Sin(t) * 0.7f + Mathf.Sin(t * 2.3f + 1.1f) * 0.3f;
            transform.localRotation = _rest * Quaternion.AngleAxis(sway * Degrees, Axis)
                * Quaternion.Euler(0f, Mathf.Sin(t * 1.7f) * YawDegrees, 0f);
        }
    }
}
