using TapAway.Core;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TapAway
{
    public class BlockView : MonoBehaviour
    {
        private const float MIN_HALF_EXTENT = 0.25f;
        private const float MIN_TRAIL_WIDTH = 0.08f;
        private static Material _cachedTrailMaterial;

        // Mũi tên thể hiện hướng di chuyển của block.
        [SerializeField] private Transform _arrow;
        // Tốc độ trượt khi block bị remove.
        [SerializeField, Min(0.1f)] private float _removeSlideSpeed = 20f;
        // Gia tốc khi block đã ra khỏi grid để tạo cảm giác phi ra ngoài.
        [SerializeField, Min(0f)] private float _removeAcceleration = 50f;
        // Khoảng đệm ra ngoài mép màn hình trước khi ẩn block.
        [SerializeField, Min(0f)] private float _offscreenPadding = 0.25f;
        // Alpha sát block (đầu vệt), theo mockup là khoảng 0.7.
        [SerializeField, Range(0f, 1f)] private float _slideTrailHeadAlpha = 0.7f;
        // Alpha ở đuôi vệt, theo mockup là 0.
        [SerializeField, Range(0f, 1f)] private float _slideTrailTailAlpha = 0f;
        // Độ dài tối đa của trail phía sau block.
        [SerializeField, Min(0.05f)] private float _slideTrailMaxLength = 1.75f;
        // Scale nhỏ nhất khi nhấn block trước khi trượt remove.
        [SerializeField, Range(0.6f, 1f)] private float _tapPressScale = 0.88f;
        // Thời gian thu nhỏ khi nhấn.
        [SerializeField, Min(0.01f)] private float _tapPressDownDuration = 0.05f;
        // Thời gian phục hồi scale về trạng thái ban đầu.
        [SerializeField, Min(0.01f)] private float _tapPressUpDuration = 0.08f;
        // Material override cho trail; nếu null sẽ dùng material mặc định được tạo runtime.
        [SerializeField] private Material _trailMaterialOverride;
        // VFX khi normal block bị Gear cắt.
        [SerializeField] private AssetReferenceT<GameObject> _gearHitVfxPrefabRef;

        private Block _block;
        private Camera _mainCamera;
        private SpriteRenderer _mainSpriteRenderer;
        private LineRenderer _slideTrailRenderer;
        private Coroutine _prepareRemoveCoroutine;
        private Coroutine _gridMoveCoroutine;
        private Gradient _trailGradient;
        private bool _isRemoving;
        private bool _isPreparingRemove;
        private bool _isGridMoving;
        private bool _pendingRemoveAfterGridMove;
        private bool _hasPrecomputedRemoveStart;
        private bool _hasTrailColorOverride;
        private Vector3 _removeDirection;
        private Vector3 _removeStartWorldPosition;
        private Vector3 _initialLocalScale;
        private float _currentRemoveSpeed;
        private Color _trailColorOverride = Color.white;
        private AsyncOperationHandle<GameObject> _gearHitVfxHandle;
        private GameObject _gearHitVfxLoaded;

        public bool IsRemoving => _isRemoving;
        public bool IsPreparingRemove => _isPreparingRemove;
        public bool IsGridMoving => _isGridMoving;
        public float GridMoveSpeed => _removeSlideSpeed;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _mainSpriteRenderer = ResolveMainSpriteRenderer();
            _initialLocalScale = transform.localScale;
            SetupSlideTrail();

            StartCoroutine(LoadGearHitVfxAddressable());
        }

        public void Initialise(Block block)
        {
            _block = block;
            _block.Removed += OnRemoved;

            _isRemoving = false;
            _isPreparingRemove = false;
            transform.localScale = _initialLocalScale;

            if (_prepareRemoveCoroutine != null)
            {
                StopCoroutine(_prepareRemoveCoroutine);
                _prepareRemoveCoroutine = null;
            }

            if (_gridMoveCoroutine != null)
            {
                StopCoroutine(_gridMoveCoroutine);
                _gridMoveCoroutine = null;
            }

            if (_slideTrailRenderer != null)
            {
                _slideTrailRenderer.enabled = false;
            }

            _isGridMoving = false;
            _pendingRemoveAfterGridMove = false;
            _hasPrecomputedRemoveStart = false;
            _currentRemoveSpeed = _removeSlideSpeed;

            _mainSpriteRenderer = ResolveMainSpriteRenderer();
            SyncSlideTrailSorting();
            RefreshDirection();
        }

        public void ApplyVisualTheme(bool hasBlockSprite, Sprite blockSprite, bool hasTrailColor, Color trailColor)
        {
            _hasTrailColorOverride = hasTrailColor;
            _trailColorOverride = trailColor;

            // Nhận Sprite từ SO, gán vào SpriteRenderer
            if (hasBlockSprite && blockSprite != null)
            {
                if (_mainSpriteRenderer == null)
                {
                    _mainSpriteRenderer = ResolveMainSpriteRenderer();
                }
                if (_mainSpriteRenderer != null)
                {
                    _mainSpriteRenderer.sprite = blockSprite;
                }
            }
        }

        private void OnDestroy()
        {
            if (_prepareRemoveCoroutine != null)
            {
                StopCoroutine(_prepareRemoveCoroutine);
                _prepareRemoveCoroutine = null;
            }

            if (_gridMoveCoroutine != null)
            {
                StopCoroutine(_gridMoveCoroutine);
                _gridMoveCoroutine = null;
            }

            if (_block != null)
            {
                _block.Removed -= OnRemoved;
            }

            if (_gearHitVfxHandle.IsValid())
            {
                Addressables.Release(_gearHitVfxHandle);
            }
        }

        private void Update()
        {
            if (!_isRemoving)
            {
                return;
            }

            _currentRemoveSpeed += _removeAcceleration * Time.deltaTime;
            transform.position += _removeDirection * (_currentRemoveSpeed * Time.deltaTime);
            if (!IsOutOfScreen())
            {
                return;
            }

            _isRemoving = false;
            _isPreparingRemove = false;
            _isGridMoving = false;
            _pendingRemoveAfterGridMove = false;
            _hasPrecomputedRemoveStart = false;
            transform.localScale = _initialLocalScale;
            if (_slideTrailRenderer != null)
            {
                _slideTrailRenderer.enabled = false;
            }

            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            bool shouldPreviewExitTrail = _isGridMoving
                && _pendingRemoveAfterGridMove
                && _block != null
                && _block.RemoveReason == BlockRemoveReason.ExitGrid;

            if (!_isRemoving && !shouldPreviewExitTrail)
            {
                return;
            }

            UpdateSlideTrail();
        }

        private void OnRemoved(Block block)
        {
            if (_block == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (_isGridMoving || _gridMoveCoroutine != null)
            {
                _pendingRemoveAfterGridMove = true;

                if (_block.RemoveReason == BlockRemoveReason.ExitGrid)
                {
                    _removeDirection = DirectionToVector(_block.Direction).normalized;
                    _removeStartWorldPosition = transform.position;
                    _hasPrecomputedRemoveStart = true;

                    if (_slideTrailRenderer != null)
                    {
                        _slideTrailRenderer.enabled = true;
                        UpdateSlideTrail();
                    }
                }

                return;
            }

            if (_gridMoveCoroutine != null)
            {
                StopCoroutine(_gridMoveCoroutine);
                _gridMoveCoroutine = null;
            }

            _isGridMoving = false;

            StartRemoveSequence();
        }

        private IEnumerator PlayTapPressAndStartRemove()
        {
            if (_block != null && _block.RemoveReason == BlockRemoveReason.HitGear)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayNormalHitGear();
                }

                SpawnGearHitVfx();
                gameObject.SetActive(false);
                _isPreparingRemove = false;
                _isRemoving = false;
                _prepareRemoveCoroutine = null;
                yield break;
            }

            if (_block != null && _block.RemoveReason == BlockRemoveReason.Bomb)
            {
                // Bomb xóa block ngay tại chỗ, không chạy animation trượt ra khỏi màn hình.
                gameObject.SetActive(false);
                _isPreparingRemove = false;
                _isRemoving = false;
                _prepareRemoveCoroutine = null;
                yield break;
            }

            _isRemoving = false;
            transform.localScale = _initialLocalScale;

            transform.localScale = _initialLocalScale;

            if (_slideTrailRenderer != null)
            {
                _slideTrailRenderer.enabled = true;
                UpdateSlideTrail();
            }

            _isPreparingRemove = false;
            _isRemoving = true;
            _prepareRemoveCoroutine = null;
        }

        public void AnimateGridMoveTo(Vector3 targetLocalPosition)
        {
            if (!gameObject.activeInHierarchy)
            {
                transform.localPosition = targetLocalPosition;
                _isGridMoving = false;
                return;
            }

            _isGridMoving = true;

            if (_gridMoveCoroutine != null)
            {
                StopCoroutine(_gridMoveCoroutine);
            }

            _gridMoveCoroutine = StartCoroutine(AnimateGridMoveCoroutine(targetLocalPosition));
        }

        private IEnumerator AnimateGridMoveCoroutine(Vector3 targetLocalPosition)
        {
            _isGridMoving = true;
            if (_slideTrailRenderer != null)
            {
                _slideTrailRenderer.enabled = false;
            }

            while ((transform.localPosition - targetLocalPosition).sqrMagnitude > 0.000001f)
            {
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    targetLocalPosition,
                    _removeSlideSpeed * Time.deltaTime);
                yield return null;
            }

            transform.localPosition = targetLocalPosition;
            _isGridMoving = false;
            _gridMoveCoroutine = null;

            if (_pendingRemoveAfterGridMove)
            {
                _pendingRemoveAfterGridMove = false;
                StartRemoveSequence();
            }
        }

        private void StartRemoveSequence()
        {
            _removeDirection = DirectionToVector(_block.Direction).normalized;
            if (!_hasPrecomputedRemoveStart)
            {
                _removeStartWorldPosition = transform.position;
            }

            _currentRemoveSpeed = _removeSlideSpeed;
            _isPreparingRemove = true;
            _hasPrecomputedRemoveStart = false;

            if (_prepareRemoveCoroutine != null)
            {
                StopCoroutine(_prepareRemoveCoroutine);
            }

            _prepareRemoveCoroutine = StartCoroutine(PlayTapPressAndStartRemove());
        }

        private void SpawnGearHitVfx()
        {
            GameObject vfxPrefab = _gearHitVfxLoaded;
            if (vfxPrefab == null)
            {
                return;
            }

            GameObject vfx = Instantiate(vfxPrefab, transform.position, Quaternion.identity);
            ParticleSystem particle = vfx.GetComponentInChildren<ParticleSystem>();
            if (particle == null)
            {
                Destroy(vfx, 1.5f);
                return;
            }

            ParticleSystem.MainModule main = particle.main;
            float lifetime = main.duration + main.startLifetime.constantMax;
            Destroy(vfx, Mathf.Max(0.5f, lifetime));
        }

        private IEnumerator LoadGearHitVfxAddressable()
        {
            if (_gearHitVfxPrefabRef == null || !_gearHitVfxPrefabRef.RuntimeKeyIsValid())
            {
                Debug.LogWarning("[BlockView] Missing Gear Hit VFX Addressable reference.", this);
                yield break;
            }

            _gearHitVfxHandle = _gearHitVfxPrefabRef.LoadAssetAsync<GameObject>();
            yield return _gearHitVfxHandle;
            if (_gearHitVfxHandle.Status != AsyncOperationStatus.Succeeded)
            {
                yield break;
            }

            _gearHitVfxLoaded = _gearHitVfxHandle.Result;
        }

        private IEnumerator ScaleOverTime(Vector3 from, Vector3 to, float duration)
        {
            if (duration <= 0f)
            {
                transform.localScale = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }

            transform.localScale = to;
        }

        private void SetupSlideTrail()
        {
            Transform trailTransform = transform.Find("SlideTrail");
            if (trailTransform == null)
            {
                GameObject trailObject = new GameObject("SlideTrail");
                trailObject.transform.SetParent(transform, false);
                trailTransform = trailObject.transform;
            }

            _slideTrailRenderer = trailTransform.GetComponent<LineRenderer>();
            if (_slideTrailRenderer == null)
            {
                _slideTrailRenderer = trailTransform.gameObject.AddComponent<LineRenderer>();
            }

            _slideTrailRenderer.enabled = false;
            _slideTrailRenderer.positionCount = 2;
            _slideTrailRenderer.useWorldSpace = true;
            _slideTrailRenderer.alignment = LineAlignment.View;
            _slideTrailRenderer.textureMode = LineTextureMode.Stretch;
            _slideTrailRenderer.numCapVertices = 0;
            _slideTrailRenderer.numCornerVertices = 0;
            _slideTrailRenderer.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);

            Material trailMaterial = _trailMaterialOverride != null ? _trailMaterialOverride : GetOrCreateTrailMaterial();
            if (trailMaterial != null)
            {
                _slideTrailRenderer.sharedMaterial = trailMaterial;
            }

            SyncSlideTrailSorting();
        }

        private void UpdateSlideTrail()
        {
            if (_slideTrailRenderer == null)
            {
                return;
            }

            Vector3 direction = _removeDirection.normalized;
            if (direction.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            float halfWidth = GetVisualHalfWidth();
            float halfHeight = GetVisualHalfHeight();
            float fullEdgeWidth = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? halfHeight * 2f : halfWidth * 2f;

            Vector3 startPos = transform.position;
            float travelled = Vector3.Dot(startPos - _removeStartWorldPosition, direction);
            float trailLength = Mathf.Clamp(travelled, 0f, _slideTrailMaxLength);
            Vector3 endPos = startPos - direction * trailLength;

            // Point 0 gần block, point 1 là đuôi trail.
            _slideTrailRenderer.SetPosition(0, startPos);
            _slideTrailRenderer.SetPosition(1, endPos);

            float width = Mathf.Max(MIN_TRAIL_WIDTH, fullEdgeWidth);
            _slideTrailRenderer.widthMultiplier = width;

            Color baseColor = _hasTrailColorOverride ? _trailColorOverride : GetBlockBaseColor();
            ApplyTrailGradient(baseColor);
        }

        private void ApplyTrailGradient(Color baseColor)
        {
            if (_slideTrailRenderer == null)
            {
                return;
            }

            if (_trailGradient == null)
            {
                _trailGradient = new Gradient();
            }

            Color normalizedBaseColor = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
            GradientColorKey[] colorKeys =
            {
                new GradientColorKey(normalizedBaseColor, 0f),
                new GradientColorKey(normalizedBaseColor, 1f),
            };

            GradientAlphaKey[] alphaKeys =
            {
                new GradientAlphaKey(_slideTrailHeadAlpha, 0f),
                new GradientAlphaKey(_slideTrailTailAlpha, 1f),
            };

            _trailGradient.SetKeys(colorKeys, alphaKeys);
            _slideTrailRenderer.colorGradient = _trailGradient;
        }

        private Color GetBlockBaseColor()
        {
            if (_mainSpriteRenderer == null)
            {
                _mainSpriteRenderer = ResolveMainSpriteRenderer();
            }

            if (_mainSpriteRenderer != null)
            {
                return _mainSpriteRenderer.color;
            }

            return Color.white;
        }

        private void SyncSlideTrailSorting()
        {
            if (_slideTrailRenderer == null)
            {
                return;
            }

            if (_mainSpriteRenderer == null)
            {
                _mainSpriteRenderer = ResolveMainSpriteRenderer();
            }

            if (_mainSpriteRenderer == null)
            {
                _slideTrailRenderer.sortingLayerName = "Default";
                _slideTrailRenderer.sortingOrder = -1;
                return;
            }

            // Trail dùng cùng sorting layer với block nhưng order thấp hơn 1 để luôn nằm dưới.
            _slideTrailRenderer.sortingLayerID = _mainSpriteRenderer.sortingLayerID;
            _slideTrailRenderer.sortingOrder = _mainSpriteRenderer.sortingOrder - 1;
        }

        private float GetVisualHalfWidth()
        {
            if (_mainSpriteRenderer != null)
            {
                return Mathf.Max(MIN_HALF_EXTENT, _mainSpriteRenderer.bounds.extents.x);
            }

            return MIN_HALF_EXTENT;
        }

        private float GetVisualHalfHeight()
        {
            if (_mainSpriteRenderer != null)
            {
                return Mathf.Max(MIN_HALF_EXTENT, _mainSpriteRenderer.bounds.extents.y);
            }

            return MIN_HALF_EXTENT;
        }

        private SpriteRenderer ResolveMainSpriteRenderer()
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return null;
            }

            SpriteRenderer best = null;
            float bestArea = -1f;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer candidate = renderers[i];
                if (candidate == null || !candidate.enabled)
                {
                    continue;
                }

                float area = Mathf.Abs(candidate.bounds.size.x * candidate.bounds.size.y);
                if (area > bestArea)
                {
                    bestArea = area;
                    best = candidate;
                }
            }

            return best;
        }

        private bool IsOutOfScreen()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null)
                {
                    return true;
                }
            }

            Vector3 viewportPoint = _mainCamera.WorldToViewportPoint(transform.position);
            return viewportPoint.x < -_offscreenPadding
                || viewportPoint.x > 1f + _offscreenPadding
                || viewportPoint.y < -_offscreenPadding
                || viewportPoint.y > 1f + _offscreenPadding;
        }

        private void RefreshDirection()
        {
            if (_arrow == null || _block == null)
            {
                return;
            }

            float z = 0f;
            switch (_block.Direction)
            {
                case BlockDirection.Up:
                    z = 90f;
                    break;
                case BlockDirection.Right:
                    z = 0f;
                    break;
                case BlockDirection.Down:
                    z = -90f;
                    break;
                case BlockDirection.Left:
                    z = 180f;
                    break;
            }

            _arrow.localRotation = Quaternion.Euler(0f, 0f, z);
        }

        private static Material GetOrCreateTrailMaterial()
        {
            if (_cachedTrailMaterial != null)
            {
                return _cachedTrailMaterial;
            }

            Shader trailShader = Shader.Find("Sprites/Default");
            if (trailShader == null)
            {
                return null;
            }

            _cachedTrailMaterial = new Material(trailShader)
            {
                name = "BlockSlideTrail_Mat",
            };
            return _cachedTrailMaterial;
        }

        private static Vector3 DirectionToVector(BlockDirection direction)
        {
            switch (direction)
            {
                case BlockDirection.Right:
                    return Vector3.right;
                case BlockDirection.Down:
                    return Vector3.down;
                case BlockDirection.Left:
                    return Vector3.left;
                default:
                    return Vector3.up;
            }
        }
    }
}
