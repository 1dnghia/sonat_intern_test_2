using TapAway.Core;
using TapAway.Infrastructure;
using UnityEngine;

namespace TapAway.Core
{
    /// <summary>
    /// Manages camera framing for the game grid.
    /// Calculates world-space cell size so the grid always fits the screen width with padding,
    /// and positions the Grid root with a vertical offset to leave space for UI.
    /// Attach to Main Camera.
    /// </summary>
    public class CameraController : SingletonMonoBehaviour<CameraController>
    {
        // ── Serialized Fields ──────────────────────────────────
        [SerializeField, Range(0f, 0.4f)]
        [Tooltip("Fraction of screen width reserved as padding on each side")]
        private float _horizontalPaddingFraction = 0.08f;

        [SerializeField]
        [Tooltip("World-space Y offset applied to Grid root so the grid has room for top UI")]
        private float _verticalOffset = -0.5f;

        [SerializeField, Tooltip("Root transform of the Grid GameObject")]
        private Transform _gridRoot;

        // ── Private Fields ────────────────────────────────────
        private Camera _cam;

        // ── Lifecycle ─────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _cam = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnLevelLoaded += OnLevelLoaded;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnLevelLoaded -= OnLevelLoaded;
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>
        /// Returns the world-space size of a single grid cell so that all
        /// <paramref name="gridSize"/> cells fit within the usable screen width.
        /// </summary>
        public float GetCellSize(int gridSize)
        {
            if (_cam == null || gridSize <= 0) return 1f;

            float screenWidthUnits = _cam.orthographicSize * 2f * _cam.aspect;
            float usableWidth = screenWidthUnits * (1f - _horizontalPaddingFraction * 2f);
            return usableWidth / gridSize;
        }

        // ── Private ───────────────────────────────────────────

        private void OnLevelLoaded()
        {
            if (_gridRoot == null) return;

            // Position grid vertically to leave space for top UI
            _gridRoot.position = new Vector3(0f, _verticalOffset, 0f);
        }
    }
}
