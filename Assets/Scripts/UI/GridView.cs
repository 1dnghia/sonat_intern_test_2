using System.Collections.Generic;
using System.Collections;
using TapAway.Core;
using UnityEngine;

namespace TapAway
{
    public class GridView : MonoBehaviour
    {
        [SerializeField] private GameObject _normalBlockPrefab;
        [SerializeField] private GameObject _gearBlockPrefab;
        [SerializeField] private GameObject _rotatorBlockPrefab;
        [SerializeField, Min(0.1f)] private float _cellSize = 1f;
        [Header("Blocked Feedback")]
        [SerializeField, Min(0f)] private float _blockedShakeStepDelay = 0.07f;
        [SerializeField, Min(0.01f)] private float _blockedShakeDuration = 0.16f;
        [SerializeField, Min(0f)] private float _blockedShakeDistance = 0.06f;
        [SerializeField, Min(0.01f)] private float _warnFlashDuration = 0.2f;
        [SerializeField] private Color _warnFlashColor = new Color(1f, 0.5f, 0.35f, 1f);

        private readonly Dictionary<Block, MonoBehaviour> _viewByBlock = new Dictionary<Block, MonoBehaviour>();
        private readonly Dictionary<MonoBehaviour, Block> _blockByView = new Dictionary<MonoBehaviour, Block>();
        private readonly Dictionary<Vector2Int, Block> _blockByPosition = new Dictionary<Vector2Int, Block>();

        public void Build(GridSystem gridSystem, LevelVisualTheme visualTheme, int trailBindingIndex)
        {
            UnsubscribeBlockEvents();
            ClearChildren();
            _viewByBlock.Clear();
            _blockByView.Clear();
            _blockByPosition.Clear();

            if (gridSystem == null)
            {
                return;
            }

            for (int i = 0; i < gridSystem.Blocks.Count; i++)
            {
                Block block = gridSystem.Blocks[i];
                if (block.Type == CellType.Empty)
                {
                    continue;
                }

                GameObject prefab = ResolvePrefab(block.Type);
                if (prefab == null)
                {
                    Debug.LogWarning($"Missing prefab for type {block.Type}", this);
                    continue;
                }

                GameObject go = Instantiate(prefab, transform);
                go.transform.localPosition = ToLocalPosition(block.Position);

                MonoBehaviour view = BindView(go, block, visualTheme, trailBindingIndex);
                if (view == null)
                {
                    continue;
                }

                _viewByBlock[block] = view;
                _blockByView[view] = block;
                _blockByPosition[block.Position] = block;
                block.Moved += OnBlockMoved;
                block.Removed += OnBlockRemoved;
            }
        }

        public bool TryGetBlockByCollider(Collider2D collider2D, out Block block)
        {
            block = null;
            if (collider2D == null)
            {
                return false;
            }

            MonoBehaviour view = collider2D.GetComponentInParent<BlockView>();
            if (view == null) view = collider2D.GetComponentInParent<GearView>();
            if (view == null) view = collider2D.GetComponentInParent<RotatorView>();

            if (view == null)
            {
                return false;
            }

            return _blockByView.TryGetValue(view, out block);
        }

        public bool TryGetBlockAtWorldPosition(Vector3 worldPosition, out Block block)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            Vector2Int cell = new Vector2Int(
                Mathf.RoundToInt(local.x / _cellSize),
                Mathf.RoundToInt(local.y / _cellSize));

            return _blockByPosition.TryGetValue(cell, out block);
        }

        public void PlayBlockedChainFeedback(IReadOnlyList<Block> blockerChain)
        {
            if (blockerChain == null || blockerChain.Count == 0)
            {
                return;
            }

            for (int i = 0; i < blockerChain.Count; i++)
            {
                Block block = blockerChain[i];
                if (block == null)
                {
                    continue;
                }

                if (!_viewByBlock.TryGetValue(block, out MonoBehaviour view) || view == null)
                {
                    continue;
                }

                float delay = _blockedShakeStepDelay * i;
                bool isPrimaryBlocker = i == 0;
                StartCoroutine(PlayBlockedFeedbackCoroutine(view, delay, isPrimaryBlocker));
            }
        }

        public bool HasRemovingBlocks()
        {
            foreach (MonoBehaviour view in _viewByBlock.Values)
            {
                if (view is BlockView blockView && (blockView.IsRemoving || blockView.IsPreparingRemove))
                {
                    return true;
                }
            }

            return false;
        }

        private MonoBehaviour BindView(GameObject go, Block block, LevelVisualTheme visualTheme, int trailBindingIndex)
        {
            switch (block.Type)
            {
                case CellType.Gear:
                {
                    GearView view = go.GetComponent<GearView>();
                    if (view != null)
                    {
                        view.Initialise(block);
                    }
                    return view;
                }
                case CellType.Rotator:
                {
                    RotatorView view = go.GetComponent<RotatorView>();
                    if (view != null)
                    {
                        view.Initialise(block);
                    }
                    return view;
                }
                default:
                {
                    BlockView view = go.GetComponent<BlockView>();
                    if (view != null)
                    {
                        view.Initialise(block);

                        bool hasBlockSprite = false;
                        Sprite blockSprite = null;
                        bool hasTrailColor = false;
                        Color trailColor = Color.white;

                        if (visualTheme != null && visualTheme.TryGetBindingByIndex(trailBindingIndex, out LevelVisualTheme.TrailBinding binding))
                        {
                            if (binding.BlockSprite != null)
                            {
                                hasBlockSprite = true;
                                blockSprite = binding.BlockSprite;
                            }

                            hasTrailColor = true;
                            trailColor = binding.TrailColor;
                        }

                        view.ApplyVisualTheme(hasBlockSprite, blockSprite, hasTrailColor, trailColor);
                    }
                    return view;
                }
            }
        }

        private GameObject ResolvePrefab(CellType cellType)
        {
            switch (cellType)
            {
                case CellType.Gear:
                    return _gearBlockPrefab;
                case CellType.Rotator:
                    return _rotatorBlockPrefab;
                case CellType.Normal:
                    return _normalBlockPrefab;
                default:
                    return null;
            }
        }

        private Vector3 ToLocalPosition(Vector2Int cell)
        {
            return new Vector3(cell.x * _cellSize, cell.y * _cellSize, 0f);
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void UnsubscribeBlockEvents()
        {
            foreach (Block block in _viewByBlock.Keys)
            {
                if (block == null)
                {
                    continue;
                }

                block.Moved -= OnBlockMoved;
                block.Removed -= OnBlockRemoved;
            }
        }

        private void OnBlockMoved(Block block, Vector2Int oldPos, Vector2Int newPos)
        {
            if (block == null)
            {
                return;
            }

            _blockByPosition.Remove(oldPos);
            _blockByPosition[newPos] = block;

            if (_viewByBlock.TryGetValue(block, out MonoBehaviour view) && view != null)
            {
                view.transform.localPosition = ToLocalPosition(newPos);
            }
        }

        private void OnBlockRemoved(Block block)
        {
            if (block == null)
            {
                return;
            }

            _blockByPosition.Remove(block.Position);
        }

        private IEnumerator PlayBlockedFeedbackCoroutine(MonoBehaviour view, float delay, bool isPrimaryBlocker)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (view == null)
            {
                yield break;
            }

            SpriteRenderer[] renderers = view.GetComponentsInChildren<SpriteRenderer>();
            Color[] originalColors = null;
            if (isPrimaryBlocker && renderers != null && renderers.Length > 0)
            {
                originalColors = new Color[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                {
                    originalColors[i] = renderers[i].color;
                    renderers[i].color = _warnFlashColor;
                }
            }

            Transform target = view.transform;
            Vector3 originalPosition = target.localPosition;
            float elapsed = 0f;

            while (elapsed < _blockedShakeDuration)
            {
                elapsed += Time.deltaTime;
                float progress = _blockedShakeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / _blockedShakeDuration);
                float strength = 1f - progress;
                float offsetX = Mathf.Sin(elapsed * 70f) * _blockedShakeDistance * strength;
                target.localPosition = originalPosition + new Vector3(offsetX, 0f, 0f);
                yield return null;
            }

            if (target != null)
            {
                target.localPosition = originalPosition;
            }

            if (!isPrimaryBlocker || originalColors == null)
            {
                yield break;
            }

            if (_warnFlashDuration > 0f)
            {
                yield return new WaitForSeconds(_warnFlashDuration);
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].color = originalColors[i];
                }
            }
        }
    }
}
