using TapAway.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TapAway
{
    public class RotatorView : MonoBehaviour
    {
        private sealed class RuntimeLink
        {
            public Block LinkedBlock;
            public Transform LinkTransform;
            public float PrefabLocalZ;
        }

        // Phần đế của rotator dùng để hiển thị hướng.
        [SerializeField] private Transform _baseVisual;
        // Phần nắp pop lên/xuống khi rotator xoay.
        [SerializeField] private Transform _capVisual;
        // Khoảng pop theo trục Y local của nắp.
        [SerializeField, Min(0f)] private float _capPopDistance = 0.08f;
        // Tốc độ di chuyển nắp khi pop.
        [SerializeField, Min(0.01f)] private float _capMoveSpeed = 2f;
        // Prefab dây nối rotator-normal (sprite gốc quay sang phải, đầu trái gắn tại rotator).
        [SerializeField] private AssetReferenceT<GameObject> _linkPrefabRef;
        // Độ dài của sprite link khi scale X = 1 (world unit).
        [SerializeField, Min(0.001f)] private float _linkBaseLength = 1f;
        // Bật nếu pivot sprite link nằm ở giữa, để đặt link vào midpoint của rotator-normal.
        [SerializeField] private bool _linkPivotAtCenter = true;

        private Block _block;
        private Vector3 _capStartLocalPos;
        private bool _isCapAnimating;
        private bool _isCapMovingUp;
        private GridSystem _gridSystem;
        private Func<Block, Vector3> _blockToLocal;
        private Func<Vector2Int, Vector3> _cellToLocal;
        private readonly List<RuntimeLink> _runtimeLinks = new List<RuntimeLink>();
        private readonly List<Block> _linkedBlockBuffer = new List<Block>();
        private AsyncOperationHandle<GameObject> _linkPrefabHandle;
        private GameObject _linkPrefabLoaded;

        private void Awake()
        {
            if (_capVisual != null)
            {
                _capStartLocalPos = _capVisual.localPosition;
            }

            StartCoroutine(LoadLinkPrefabAddressable());
        }

        public void Initialise(Block block)
        {
            _block = block;
            _block.Rotated += OnRotated;
            _block.Removed += OnRemoved;

            if (_capVisual != null)
            {
                _capStartLocalPos = _capVisual.localPosition;
            }
        }

        private void OnDestroy()
        {
            if (_block != null)
            {
                _block.Rotated -= OnRotated;
                _block.Removed -= OnRemoved;
            }

            ClearRuntimeLinks();

            if (_linkPrefabHandle.IsValid())
            {
                Addressables.Release(_linkPrefabHandle);
            }
        }

        public void RefreshLinks(GridSystem gridSystem, Func<Vector2Int, Vector3> cellToLocal)
        {
            RefreshLinks(gridSystem, null, cellToLocal);
        }

        public void RefreshLinks(
            GridSystem gridSystem,
            Func<Block, Vector3> blockToLocal,
            Func<Vector2Int, Vector3> cellToLocal)
        {
            _gridSystem = gridSystem;
            _blockToLocal = blockToLocal;
            _cellToLocal = cellToLocal;

            if (_block == null || _block.IsRemoved || _block.Type != CellType.Rotator)
            {
                ClearRuntimeLinks();
                return;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            GameObject activeLinkPrefab = _linkPrefabLoaded;
            if (activeLinkPrefab == null || gridSystem == null || cellToLocal == null)
            {
                ClearRuntimeLinks();
                return;
            }

            _linkedBlockBuffer.Clear();
            gridSystem.GetRotatorLinkedBlocks(_block, _linkedBlockBuffer);

            for (int i = _runtimeLinks.Count - 1; i >= 0; i--)
            {
                RuntimeLink runtimeLink = _runtimeLinks[i];
                if (runtimeLink == null || runtimeLink.LinkedBlock == null || runtimeLink.LinkTransform == null)
                {
                    DestroyLinkAt(i);
                    continue;
                }

                if (!_linkedBlockBuffer.Contains(runtimeLink.LinkedBlock))
                {
                    DestroyLinkAt(i);
                }
            }

            for (int i = 0; i < _linkedBlockBuffer.Count; i++)
            {
                Block candidate = _linkedBlockBuffer[i];
                if (candidate == null || candidate.IsRemoved || candidate.Type != CellType.Normal)
                {
                    continue;
                }

                if (FindRuntimeLink(candidate) != null)
                {
                    continue;
                }

                Transform parentTransform = transform.parent;
                if (parentTransform == null)
                {
                    continue;
                }

                Vector3 targetLocalFromGrid = blockToLocal != null
                    ? blockToLocal(candidate)
                    : cellToLocal(candidate.Position);
                Vector3 targetWorld = parentTransform.TransformPoint(targetLocalFromGrid);
                Vector3 targetLocal = transform.InverseTransformPoint(targetWorld);
                Vector3 direction = new Vector3(targetLocal.x, targetLocal.y, 0f);
                float length = direction.magnitude;
                if (length <= 0.0001f)
                {
                    continue;
                }

                GameObject linkObject = Instantiate(activeLinkPrefab, transform);
                float prefabLocalZ = linkObject.transform.localPosition.z;
                ApplyLinkTransform(linkObject.transform, direction, length, prefabLocalZ, true);

                _runtimeLinks.Add(new RuntimeLink
                {
                    LinkedBlock = candidate,
                    LinkTransform = linkObject.transform,
                    PrefabLocalZ = prefabLocalZ,
                });
            }

            UpdateRuntimeLinkTransforms(cellToLocal);
        }

        public void CollectWarningRenderers(List<SpriteRenderer> output)
        {
            if (output == null)
            {
                return;
            }

            output.Clear();
            AppendVisualRenderers(_baseVisual, output);
            AppendVisualRenderers(_capVisual, output);
        }

        private static void AppendVisualRenderers(Transform visualRoot, List<SpriteRenderer> output)
        {
            if (visualRoot == null || output == null)
            {
                return;
            }

            SpriteRenderer[] renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer != null)
                {
                    output.Add(renderer);
                }
            }
        }

        private void ApplyLinkTransform(
            Transform linkTransform,
            Vector3 direction,
            float length,
            float prefabLocalZ,
            bool preserveLocalZ)
        {
            if (linkTransform == null || length <= 0.0001f)
            {
                return;
            }

            Vector3 directionNorm = direction / length;
            float angle = Mathf.Atan2(directionNorm.y, directionNorm.x) * Mathf.Rad2Deg;
            linkTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

            float targetLocalZ = preserveLocalZ ? linkTransform.localPosition.z : prefabLocalZ;
            Vector3 localPos = new Vector3(0f, 0f, targetLocalZ);
            if (_linkPivotAtCenter)
            {
                // Pivot ở giữa: đặt object tại midpoint để đường nối đúng từ tâm rotator đến tâm normal.
                localPos += directionNorm * (length * 0.5f);
            }

            linkTransform.localPosition = localPos;

            SpriteRenderer spriteRenderer = linkTransform.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.drawMode != SpriteDrawMode.Simple)
            {
                Vector2 size = spriteRenderer.size;
                float scaleX = Mathf.Max(0.0001f, Mathf.Abs(linkTransform.localScale.x));
                size.x = length / scaleX;
                spriteRenderer.size = size;
                return;
            }

            Vector3 linkScale = linkTransform.localScale;
            linkScale.x = length / _linkBaseLength;
            linkTransform.localScale = linkScale;
        }

        private void OnRotated(Block block)
        {
            if (_capVisual != null)
            {
                _isCapAnimating = true;
                _isCapMovingUp = true;
            }
        }

        private void LateUpdate()
        {
            if (_capVisual == null || !_isCapAnimating)
            {
                return;
            }

            Vector3 upTarget = _capStartLocalPos + Vector3.up * _capPopDistance;
            Vector3 target = _isCapMovingUp ? upTarget : _capStartLocalPos;
            _capVisual.localPosition = Vector3.MoveTowards(
                _capVisual.localPosition,
                target,
                _capMoveSpeed * Time.deltaTime);

            if ((_capVisual.localPosition - target).sqrMagnitude > 0.000001f)
            {
                return;
            }

            _capVisual.localPosition = target;
            if (_isCapMovingUp)
            {
                _isCapMovingUp = false;
                return;
            }

            _isCapAnimating = false;
        }

        private void Update()
        {
            if (_runtimeLinks.Count == 0 || _gridSystem == null)
            {
                return;
            }

            UpdateRuntimeLinkTransforms(null);
        }

        private void UpdateRuntimeLinkTransforms(Func<Vector2Int, Vector3> fallbackCellToLocal)
        {
            Transform parentTransform = transform.parent;
            if (parentTransform == null)
            {
                return;
            }

            for (int i = _runtimeLinks.Count - 1; i >= 0; i--)
            {
                RuntimeLink runtimeLink = _runtimeLinks[i];
                if (runtimeLink == null || runtimeLink.LinkedBlock == null || runtimeLink.LinkTransform == null)
                {
                    DestroyLinkAt(i);
                    continue;
                }

                Block linkedBlock = runtimeLink.LinkedBlock;
                if (linkedBlock.IsRemoved || linkedBlock.Type != CellType.Normal)
                {
                    DestroyLinkAt(i);
                    continue;
                }

                Vector3 linkedLocal;
                if (_blockToLocal != null)
                {
                    linkedLocal = _blockToLocal(linkedBlock);
                }
                else if (fallbackCellToLocal != null)
                {
                    linkedLocal = fallbackCellToLocal(linkedBlock.Position);
                }
                else
                {
                    continue;
                }

                Vector3 targetWorld = parentTransform.TransformPoint(linkedLocal);
                Vector3 targetLocal = transform.InverseTransformPoint(targetWorld);
                Vector3 direction = new Vector3(targetLocal.x, targetLocal.y, 0f);
                float length = direction.magnitude;
                if (length <= 0.0001f)
                {
                    DestroyLinkAt(i);
                    continue;
                }

                ApplyLinkTransform(runtimeLink.LinkTransform, direction, length, runtimeLink.PrefabLocalZ, true);
            }
        }

        private void OnRemoved(Block block)
        {
            ClearRuntimeLinks();
            gameObject.SetActive(false);
        }

        private void ClearRuntimeLinks()
        {
            for (int i = _runtimeLinks.Count - 1; i >= 0; i--)
            {
                DestroyLinkAt(i);
            }

            _runtimeLinks.Clear();
            _linkedBlockBuffer.Clear();
        }

        private RuntimeLink FindRuntimeLink(Block linkedBlock)
        {
            if (linkedBlock == null)
            {
                return null;
            }

            for (int i = 0; i < _runtimeLinks.Count; i++)
            {
                RuntimeLink runtimeLink = _runtimeLinks[i];
                if (runtimeLink != null && runtimeLink.LinkedBlock == linkedBlock)
                {
                    return runtimeLink;
                }
            }

            return null;
        }

        private void DestroyLinkAt(int index)
        {
            if (index < 0 || index >= _runtimeLinks.Count)
            {
                return;
            }

            RuntimeLink runtimeLink = _runtimeLinks[index];
            Transform linkTransform = runtimeLink != null ? runtimeLink.LinkTransform : null;
            if (linkTransform != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(linkTransform.gameObject);
                }
                else
                {
                    DestroyImmediate(linkTransform.gameObject);
                }
            }

            _runtimeLinks.RemoveAt(index);
        }

        private System.Collections.IEnumerator LoadLinkPrefabAddressable()
        {
            if (_linkPrefabRef == null || !_linkPrefabRef.RuntimeKeyIsValid())
            {
                Debug.LogWarning("[RotatorView] Missing Link Prefab Addressable reference.", this);
                yield break;
            }

            _linkPrefabHandle = _linkPrefabRef.LoadAssetAsync<GameObject>();
            yield return _linkPrefabHandle;
            if (_linkPrefabHandle.Status != AsyncOperationStatus.Succeeded)
            {
                yield break;
            }

            _linkPrefabLoaded = _linkPrefabHandle.Result;

            // Link prefab load xong thì refresh ngay để thấy dây nối từ đầu level.
            if (_gridSystem != null && _cellToLocal != null)
            {
                RefreshLinks(_gridSystem, _blockToLocal, _cellToLocal);
            }
        }
    }
}
