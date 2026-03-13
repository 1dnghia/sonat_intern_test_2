using TapAway.Core;
using UnityEngine;

namespace TapAway
{
    /// <summary>
    /// Gear block visual: a single SpriteRenderer that continuously rotates clockwise.
    /// Gear is fixed, never tapped, carries no game logic beyond spinning.
    /// Plain MonoBehaviour — does NOT inherit BlockView.
    /// </summary>
    public class GearView : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────
        private const float DEFAULT_SPIN_SPEED = 90f;

        // ── Serialized Fields ──────────────────────────────────
        [SerializeField, Tooltip("Main gear visual transform (pivot phải đúng tâm)")]
        private Transform _mainVisual;

        [SerializeField, Tooltip("Shadow ring visual transform (pivot phải đúng tâm)")]
        private Transform _shadowVisual;

        [SerializeField, Tooltip("Spin speed (deg/sec, cả main và shadow)")]
        private float _spinSpeed = DEFAULT_SPIN_SPEED;

        // ── Public API ────────────────────────────────────────

        /// <summary>Called by GridView after instantiation to bind the data block.</summary>
        public void Initialise(Block block)
        {
            // Gear has no visual state that depends on block data currently,
            // but this method exists for a consistent spawn contract with GridView.
            if (_mainVisual == null)
            {
                _mainVisual = transform;
            }
            // Kiểm tra pivot prefab: pivot của _mainVisual và _shadowVisual phải ở center
            // Nếu không đúng, sprite sẽ xoay lệch tâm
        }

        // ── Unity Lifecycle ────────────────────────────────────

        private void Update()
        {
            // Negative Z = clockwise when viewed from front camera
            if (_mainVisual != null)
            {
                _mainVisual.Rotate(0f, 0f, -_spinSpeed * Time.deltaTime, Space.Self);
            }

            if (_shadowVisual != null)
            {
                _shadowVisual.Rotate(0f, 0f, -_spinSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}
