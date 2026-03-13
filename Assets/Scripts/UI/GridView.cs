using System.Collections.Generic;
using TapAway.Core;
using TapAway.Data;
using UnityEngine;

namespace TapAway
{
    /// <summary>
    /// Manages the visual grid: instantiates BlockView / GearView for each block in the level,
    /// handles world↔grid coordinate mapping.
    /// GearView is stored separately because it is not a BlockView.
    /// </summary>
    public class GridView : MonoBehaviour
    {
        // ── Constants ──────────────────────────────────────────
        private const float DEFAULT_CELL_SIZE = 1.0f;

        // ── Serialized Fields ──────────────────────────────────
        [SerializeField, Tooltip("Prefab used to render a Normal block")]
        private BlockView _normalBlockPrefab;

        [SerializeField, Tooltip("Prefab used to render a Gear block (GearView, NOT BlockView)")]
        private GearView _gearBlockPrefab;

        [SerializeField, Tooltip("Prefab used to render a Rotator block (RotatorView, NOT BlockView)")]
        private RotatorView _rotatorBlockPrefab;

        // ── Private Fields ────────────────────────────────────
        private int _gridSize;
        private float _cellSize = DEFAULT_CELL_SIZE;
        private Vector3 _gridOrigin; // world position of cell (0,0)

        // blockId → BlockView (Normal only)
        private readonly Dictionary<int, BlockView>   _blockViews   = new Dictionary<int, BlockView>();
        // blockId → GearView (Gear only)
        private readonly Dictionary<int, GearView>    _gearViews    = new Dictionary<int, GearView>();
        // blockId → RotatorView (Rotator only)
        private readonly Dictionary<int, RotatorView> _rotatorViews = new Dictionary<int, RotatorView>();

        // ── Lifecycle ─────────────────────────────────────────

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

        // ── Public ────────────────────────────────────────────

        /// <summary>
        /// Converts a world position to grid coordinates.
        /// Returns false if out of grid bounds.
        /// </summary>
        public bool WorldToGrid(Vector3 worldPos, out int gx, out int gy)
        {
            Vector3 local = worldPos - _gridOrigin;
            gx = Mathf.RoundToInt(local.x / _cellSize);
            gy = Mathf.RoundToInt(local.y / _cellSize);
            return gx >= 0 && gx < _gridSize && gy >= 0 && gy < _gridSize;
        }

        /// <summary>Returns the world-space centre of a grid cell.</summary>
        public Vector3 GridToWorld(int gx, int gy)
        {
            return _gridOrigin + new Vector3(gx * _cellSize, gy * _cellSize, 0f);
        }

        public BlockView GetBlockView(int blockId) =>
            _blockViews.TryGetValue(blockId, out var v) ? v : null;

        // ── Private ───────────────────────────────────────────

        private void OnLevelLoaded()
        {
            ClearAll();
            Build(GameManager.Instance.Grid);
        }

        private void Build(GridSystem grid)
        {
            _gridSize = grid.GridSize;
            _cellSize = CameraController.Instance != null
                ? CameraController.Instance.GetCellSize(_gridSize)
                : DEFAULT_CELL_SIZE;

            float offset = (_gridSize - 1) * _cellSize * 0.5f;
            _gridOrigin = transform.position - new Vector3(offset, offset, 0f);

            foreach (var block in grid.Blocks)
            {
                Vector3 pos = GridToWorld(block.X, block.Y);

                if (block.CellType == CellType.Gear)
                {
                    if (_gearBlockPrefab == null)
                    {
                        Debug.LogWarning("[GridView] Missing GearView prefab assignment.", this);
                        continue;
                    }
                    var gv = Instantiate(_gearBlockPrefab, pos, Quaternion.identity, transform);
                    gv.transform.localScale = Vector3.one * _cellSize;
                    gv.Initialise(block);
                    _gearViews[block.Id] = gv;
                }
                else if (block.CellType == CellType.Rotator)
                {
                    if (_rotatorBlockPrefab == null)
                    {
                        Debug.LogWarning("[GridView] Missing RotatorView prefab assignment.", this);
                        continue;
                    }
                    var rv = Instantiate(_rotatorBlockPrefab, pos, Quaternion.identity, transform);
                    rv.transform.localScale = Vector3.one * _cellSize;
                    rv.Initialise(block);
                    _rotatorViews[block.Id] = rv;
                }
                else if (block.CellType == CellType.Normal)
                {
                    if (_normalBlockPrefab == null)
                    {
                        Debug.LogWarning("[GridView] Missing Normal BlockView prefab assignment.", this);
                        continue;
                    }
                    var bv = Instantiate(_normalBlockPrefab, pos, Quaternion.identity, transform);
                    bv.transform.localScale = Vector3.one * _cellSize;
                    bv.Initialise(block);
                    _blockViews[block.Id] = bv;
                }
            }
        }

        private void ClearAll()
        {
            foreach (var v in _blockViews.Values)
                if (v != null) Destroy(v.gameObject);
            _blockViews.Clear();

            foreach (var v in _gearViews.Values)
                if (v != null) Destroy(v.gameObject);
            _gearViews.Clear();

            foreach (var v in _rotatorViews.Values)
                if (v != null) Destroy(v.gameObject);
            _rotatorViews.Clear();
        }
    }
}
