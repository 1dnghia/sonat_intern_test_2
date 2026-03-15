using TapAway.Core;
using System.Collections;
using UnityEngine;

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
        // Khoảng đệm ra ngoài mép màn hình trước khi ẩn block.
        [SerializeField, Min(0f)] private float _offscreenPadding = 0.25f;
        // Alpha sát block (đầu vệt), theo mockup là khoảng 0.7.
        [SerializeField, Range(0f, 1f)] private float _slideTrailHeadAlpha = 0.7f;
        // Alpha ở đuôi vệt, theo mockup là 0.
        [SerializeField, Range(0f, 1f)] private float _slideTrailTailAlpha = 0f;
        // Scale nhỏ nhất khi nhấn block trước khi trượt remove.
        [SerializeField, Range(0.6f, 1f)] private float _tapPressScale = 0.88f;
        // Thời gian thu nhỏ khi nhấn.
        [SerializeField, Min(0.01f)] private float _tapPressDownDuration = 0.05f;
        // Thời gian phục hồi scale về trạng thái ban đầu.
        [SerializeField, Min(0.01f)] private float _tapPressUpDuration = 0.08f;
        [SerializeField] private Material _trailMaterialOverride;

        private Block _block;
        private Camera _mainCamera;
        private SpriteRenderer _mainSpriteRenderer;
        private LineRenderer _slideTrailRenderer;
        private Coroutine _prepareRemoveCoroutine;
        private Gradient _trailGradient;
        private bool _isRemoving;
        private bool _isPreparingRemove;
        private bool _hasTrailColorOverride;
        private Vector3 _removeDirection;
        private Vector3 _removeStartWorldPosition;
        private Vector3 _initialLocalScale;
        private Color _trailColorOverride = Color.white;

        public bool IsRemoving => _isRemoving;
        public bool IsPreparingRemove => _isPreparingRemove;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _mainSpriteRenderer = ResolveMainSpriteRenderer();
            _initialLocalScale = transform.localScale;
            SetupSlideTrail();
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

            if (_slideTrailRenderer != null)
            {
                _slideTrailRenderer.enabled = false;
            }

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

            if (_block != null)
            {
                _block.Removed -= OnRemoved;
            }
        }

        private void Update()
        {
            if (!_isRemoving)
            {
                return;
            }

            transform.position += _removeDirection * (_removeSlideSpeed * Time.deltaTime);
            if (!IsOutOfScreen())
            {
                return;
            }

            _isRemoving = false;
            _isPreparingRemove = false;
            transform.localScale = _initialLocalScale;
            if (_slideTrailRenderer != null)
            {
                _slideTrailRenderer.enabled = false;
            }

            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!_isRemoving)
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

            _removeDirection = DirectionToVector(_block.Direction).normalized;
            _removeStartWorldPosition = transform.position;
            _isPreparingRemove = true;

            if (_prepareRemoveCoroutine != null)
            {
                StopCoroutine(_prepareRemoveCoroutine);
            }

            _prepareRemoveCoroutine = StartCoroutine(PlayTapPressAndStartRemove());
        }

        private IEnumerator PlayTapPressAndStartRemove()
        {
            _isRemoving = false;
            transform.localScale = _initialLocalScale;

            yield return ScaleOverTime(_initialLocalScale, _initialLocalScale * _tapPressScale, _tapPressDownDuration);
            yield return ScaleOverTime(transform.localScale, _initialLocalScale, _tapPressUpDuration);

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
            float halfWidth = GetVisualHalfWidth();
            float halfHeight = GetVisualHalfHeight();
            float fullEdgeWidth = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? halfHeight * 2f : halfWidth * 2f;

            Vector3 startPos = transform.position;
            Vector3 endPos = _removeStartWorldPosition;

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
