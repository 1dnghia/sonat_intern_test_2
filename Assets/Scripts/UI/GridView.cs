using System.Collections.Generic;
using System.Collections;
using TapAway.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TapAway
{
    public class GridView : MonoBehaviour
    {
        [Header("Prefab Source")]
        [SerializeField] private AssetReferenceT<GameObject> _normalBlockPrefabRef;
        [SerializeField] private AssetReferenceT<GameObject> _gearBlockPrefabRef;
        [SerializeField] private AssetReferenceT<GameObject> _rotatorBlockPrefabRef;
        // Kích thước mỗi ô grid theo world unit.
        [SerializeField, Min(0.1f)] private float _cellSize = 2f;
        // Khoảng cách thêm giữa các block theo world unit.
        [SerializeField, Min(0f)] private float _blockSpacing = 0f;
        [Header("Blocked Feedback")]
        // Độ trễ giữa mỗi block trong chuỗi rung domino.
        [SerializeField, Min(0f)] private float _blockedShakeStepDelay = 0.07f;
        // Thời lượng rung của từng block.
        [SerializeField, Min(0.01f)] private float _blockedShakeDuration = 0.16f;
        // Biên độ rung ngang của block bị chặn.
        [SerializeField, Min(0f)] private float _blockedShakeDistance = 0.06f;
        // Thời gian giữ màu cảnh báo cho block chặn đầu tiên.
        [SerializeField, Min(0.01f)] private float _warnFlashDuration = 0.2f;
        // Màu cảnh báo cho block chặn đầu tiên.
        [SerializeField] private Color _warnFlashColor = new Color(1f, 0.5f, 0.35f, 1f);

        private readonly Dictionary<Block, MonoBehaviour> _viewByBlock = new Dictionary<Block, MonoBehaviour>();
        private readonly Dictionary<MonoBehaviour, Block> _blockByView = new Dictionary<MonoBehaviour, Block>();
        private readonly Dictionary<Vector2Int, Block> _blockByPosition = new Dictionary<Vector2Int, Block>();
        private readonly List<SpriteRenderer> _warnRendererBuffer = new List<SpriteRenderer>();
        private Coroutine _softlockFeedbackCoroutine;
        private Coroutine _refreshLinksCoroutine;
        private GridSystem _activeGridSystem;
        private float _gridCenterCellX;
        private float _gridCenterCellY;
        private bool _isReady;
        private GameObject _normalBlockPrefab;
        private GameObject _gearBlockPrefab;
        private GameObject _rotatorBlockPrefab;
        private AsyncOperationHandle<GameObject> _normalPrefabHandle;
        private AsyncOperationHandle<GameObject> _gearPrefabHandle;
        private AsyncOperationHandle<GameObject> _rotatorPrefabHandle;

        public float CellPitch => _cellSize + _blockSpacing;
        public bool IsReady => _isReady;

        private void Awake()
        {
            StartCoroutine(PreloadAddressablePrefabs());
        }

        private void OnDestroy()
        {
            ReleaseHandleIfValid(_normalPrefabHandle);
            ReleaseHandleIfValid(_gearPrefabHandle);
            ReleaseHandleIfValid(_rotatorPrefabHandle);
        }

        public void Build(GridSystem gridSystem, LevelVisualTheme visualTheme, int trailBindingIndex)
        {
            UnsubscribeBlockEvents();
            ClearChildren();
            _viewByBlock.Clear();
            _blockByView.Clear();
            _blockByPosition.Clear();

            // Luon reset root grid ve (0,0) de tam grid trung voi world origin.
            if (transform.parent != null)
            {
                transform.localPosition = new Vector3(0f, 0f, transform.localPosition.z);
            }
            else
            {
                transform.position = new Vector3(0f, 0f, transform.position.z);
            }

            if (gridSystem == null)
            {
                return;
            }

            _gridCenterCellX = (gridSystem.MinX + gridSystem.MaxX) * 0.5f;
            _gridCenterCellY = (gridSystem.MinY + gridSystem.MaxY) * 0.5f;
            _activeGridSystem = gridSystem;

            if (_refreshLinksCoroutine != null)
            {
                StopCoroutine(_refreshLinksCoroutine);
                _refreshLinksCoroutine = null;
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

            RefreshAllRotatorLinks();
        }

        private IEnumerator PreloadAddressablePrefabs()
        {
            // Preload 3 prefab chính của grid trước khi Build để tránh missing prefab frame đầu.
            yield return LoadPrefabFromAddressable(_normalBlockPrefabRef, prefab =>
            {
                _normalBlockPrefab = prefab;
            }, handle => _normalPrefabHandle = handle);

            yield return LoadPrefabFromAddressable(_gearBlockPrefabRef, prefab =>
            {
                _gearBlockPrefab = prefab;
            }, handle => _gearPrefabHandle = handle);

            yield return LoadPrefabFromAddressable(_rotatorBlockPrefabRef, prefab =>
            {
                _rotatorBlockPrefab = prefab;
            }, handle => _rotatorPrefabHandle = handle);

            _isReady = true;
        }

        private static IEnumerator LoadPrefabFromAddressable(
            AssetReferenceT<GameObject> prefabReference,
            System.Action<GameObject> onLoaded,
            System.Action<AsyncOperationHandle<GameObject>> onHandle)
        {
            if (prefabReference == null || !prefabReference.RuntimeKeyIsValid())
            {
                Debug.LogWarning("[GridView] Missing Addressable prefab reference.");
                yield break;
            }

            AsyncOperationHandle<GameObject> handle = prefabReference.LoadAssetAsync<GameObject>();
            yield return handle;
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                yield break;
            }

            onHandle?.Invoke(handle);
            onLoaded?.Invoke(handle.Result);
        }

        private static void ReleaseHandleIfValid(AsyncOperationHandle<GameObject> handle)
        {
            if (!handle.IsValid())
            {
                return;
            }

            Addressables.Release(handle);
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
            float pitch = _cellSize + _blockSpacing;
            Vector2Int cell = new Vector2Int(
                Mathf.RoundToInt(local.x / pitch + _gridCenterCellX),
                Mathf.RoundToInt(local.y / pitch + _gridCenterCellY));

            return _blockByPosition.TryGetValue(cell, out block);
        }

        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            float pitch = _cellSize + _blockSpacing;
            return new Vector2Int(
                Mathf.RoundToInt(local.x / pitch + _gridCenterCellX),
                Mathf.RoundToInt(local.y / pitch + _gridCenterCellY));
        }

        public void PlayGearTapFeedback(Block gearBlock)
        {
            if (gearBlock == null)
            {
                return;
            }

            if (!_viewByBlock.TryGetValue(gearBlock, out MonoBehaviour view) || view == null)
            {
                return;
            }

            GearView gearView = view as GearView;
            if (gearView == null)
            {
                return;
            }

            gearView.PlayTapFeedback();
        }

        public void PlayBlockedChainFeedback(
            IReadOnlyList<Block> blockerChain,
            bool colorPrimaryBlocker,
            float impactDelay)
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

                float delay = Mathf.Max(0f, impactDelay) + _blockedShakeStepDelay * i;
                bool isPrimaryBlocker = colorPrimaryBlocker && i == 0;
                StartCoroutine(PlayBlockedFeedbackCoroutine(view, delay, isPrimaryBlocker));
            }
        }

        public bool TryEstimateBlockTravelDuration(Block block, Vector2Int fromCell, Vector2Int toCell, out float duration)
        {
            duration = 0f;
            if (block == null)
            {
                return false;
            }

            if (!_viewByBlock.TryGetValue(block, out MonoBehaviour view) || !(view is BlockView blockView))
            {
                return false;
            }

            float speed = Mathf.Max(0.01f, blockView.GridMoveSpeed);
            float distance = Vector2Int.Distance(fromCell, toCell) * CellPitch;
            duration = distance / speed;
            return true;
        }

        public bool HasRemovingBlocks()
        {
            foreach (MonoBehaviour view in _viewByBlock.Values)
            {
                if (view is BlockView blockView
                    && (blockView.IsRemoving || blockView.IsPreparingRemove || blockView.IsGridMoving))
                {
                    return true;
                }
            }

            return false;
        }

        public float PlaySoftlockFeedback(IReadOnlyList<Block> blockedNormals)
        {
            if (blockedNormals == null || blockedNormals.Count == 0)
            {
                return 0f;
            }

            List<MonoBehaviour> views = new List<MonoBehaviour>();
            for (int i = 0; i < blockedNormals.Count; i++)
            {
                Block block = blockedNormals[i];
                if (block == null)
                {
                    continue;
                }

                if (!_viewByBlock.TryGetValue(block, out MonoBehaviour view) || view == null)
                {
                    continue;
                }

                views.Add(view);
            }

            if (views.Count == 0)
            {
                return 0f;
            }

            if (_softlockFeedbackCoroutine != null)
            {
                StopCoroutine(_softlockFeedbackCoroutine);
            }

            _softlockFeedbackCoroutine = StartCoroutine(PlaySoftlockFeedbackSequential(views));

            float oneBlockDuration = _blockedShakeDuration;
            return views.Count * oneBlockDuration + Mathf.Max(0, views.Count - 1) * _blockedShakeStepDelay;
        }

        private IEnumerator PlaySoftlockFeedbackSequential(IReadOnlyList<MonoBehaviour> views)
        {
            for (int i = 0; i < views.Count; i++)
            {
                MonoBehaviour view = views[i];
                if (view == null)
                {
                    continue;
                }

                yield return PlayBlockedFeedbackCoroutine(view, 0f, true, true);

                if (i < views.Count - 1 && _blockedShakeStepDelay > 0f)
                {
                    yield return new WaitForSeconds(_blockedShakeStepDelay);
                }
            }

            _softlockFeedbackCoroutine = null;
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
            float pitch = _cellSize + _blockSpacing;
            return new Vector3(
                (cell.x - _gridCenterCellX) * pitch,
                (cell.y - _gridCenterCellY) * pitch,
                0f);
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
                Vector3 targetPos = ToLocalPosition(newPos);
                if (view is BlockView blockView)
                {
                    blockView.AnimateGridMoveTo(targetPos);
                }
                else
                {
                    view.transform.localPosition = targetPos;
                }
            }

            QueueRefreshAllRotatorLinks();
        }

        private void OnBlockRemoved(Block block)
        {
            if (block == null)
            {
                return;
            }

            _blockByPosition.Remove(block.Position);
            QueueRefreshAllRotatorLinks();
        }

        private void QueueRefreshAllRotatorLinks()
        {
            if (_refreshLinksCoroutine != null)
            {
                return;
            }

            _refreshLinksCoroutine = StartCoroutine(RefreshAllRotatorLinksNextFrame());
        }

        private IEnumerator RefreshAllRotatorLinksNextFrame()
        {
            yield return null;
            RefreshAllRotatorLinks();
            _refreshLinksCoroutine = null;
        }

        private void RefreshAllRotatorLinks()
        {
            if (_activeGridSystem == null)
            {
                return;
            }

            foreach (KeyValuePair<Block, MonoBehaviour> pair in _viewByBlock)
            {
                Block block = pair.Key;
                MonoBehaviour view = pair.Value;

                if (block == null || view == null || block.Type != CellType.Rotator)
                {
                    continue;
                }

                RotatorView rotatorView = view as RotatorView;
                if (rotatorView == null)
                {
                    continue;
                }

                rotatorView.RefreshLinks(_activeGridSystem, GetBlockLocalPositionForLink, ToLocalPosition);
            }
        }

        private Vector3 GetBlockLocalPositionForLink(Block block)
        {
            if (block == null)
            {
                return Vector3.zero;
            }

            if (_viewByBlock.TryGetValue(block, out MonoBehaviour view) && view != null)
            {
                return view.transform.localPosition;
            }

            return ToLocalPosition(block.Position);
        }

        private IEnumerator PlayBlockedFeedbackCoroutine(
            MonoBehaviour view,
            float delay,
            bool isPrimaryBlocker,
            bool keepWarnColor = false)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (view == null)
            {
                yield break;
            }

            SpriteRenderer[] renderers = ResolveWarningRenderers(view);
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
            bool interruptedByGridMove = false;

            while (elapsed < _blockedShakeDuration)
            {
                if (view is BlockView blockView && blockView.IsGridMoving)
                {
                    interruptedByGridMove = true;
                    break;
                }

                elapsed += Time.deltaTime;
                float progress = _blockedShakeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / _blockedShakeDuration);
                float strength = 1f - progress;
                float offsetX = Mathf.Sin(elapsed * 70f) * _blockedShakeDistance * strength;
                target.localPosition = originalPosition + new Vector3(offsetX, 0f, 0f);
                yield return null;
            }

            if (!interruptedByGridMove && target != null)
            {
                target.localPosition = originalPosition;
            }

            if (!isPrimaryBlocker || originalColors == null || keepWarnColor)
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

        private SpriteRenderer[] ResolveWarningRenderers(MonoBehaviour view)
        {
            if (view is RotatorView rotatorView)
            {
                _warnRendererBuffer.Clear();
                rotatorView.CollectWarningRenderers(_warnRendererBuffer);

                if (_warnRendererBuffer.Count == 0)
                {
                    return view.GetComponentsInChildren<SpriteRenderer>();
                }

                return _warnRendererBuffer.ToArray();
            }

            return view.GetComponentsInChildren<SpriteRenderer>();
        }
    }
}
