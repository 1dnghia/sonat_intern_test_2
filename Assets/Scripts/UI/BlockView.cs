using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TapAway.Core;
using TapAway.Data;
using UnityEngine;

namespace TapAway
{
    /// <summary>
    /// Visual representation of a single block.
    /// Handles move animation, destroy animation, shake (blocked feedback) and colour warning.
    /// </summary>
    public class BlockView : MonoBehaviour
    {
        // ── Constants ──────────────────────────────────────────
        private const float FLY_OUT_DURATION   = 0.35f;
        private const float SHAKE_DURATION     = 0.25f;
        private const float SHAKE_MAGNITUDE    = 0.12f;
        private const float SHAKE_DELAY_STEP   = 0.07f;   // domino delay between blocks
        private const float WARNING_DURATION   = 0.4f;

        // ── Serialized Fields ──────────────────────────────────
        [SerializeField, Tooltip("Renderer to tint for warning flash")]
        private SpriteRenderer _spriteRenderer;

        [SerializeField, Tooltip("Arrow indicator showing block direction")]
        private Transform _arrowTransform;

        [SerializeField] private Color _defaultColor  = Color.white;
        [SerializeField] private Color _warningColor  = new Color(1f, 0.3f, 0.3f);

        // ── Private Fields ────────────────────────────────────
        private Block _block;
        private GridView _gridView;
        // ── Protected Properties ──────────────────────────────
        protected Block Block => _block;
        // ── Lifecycle ─────────────────────────────────────────

        protected virtual void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnBlockTapped += HandleBlockTapped;
        }

        protected virtual void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnBlockTapped -= HandleBlockTapped;
            DOTween.Kill(transform);
        }

        // ── Public API ────────────────────────────────────────

        public void Initialise(Block block)
        {
            _block = block;
            _gridView = GetComponentInParent<GridView>();
            UpdateArrowRotation();

            if (_spriteRenderer != null)
                _spriteRenderer.color = _defaultColor;
        }

        // ── Internal Event Handling ───────────────────────────

        private void HandleBlockTapped(GridSystem.TapResult result, int tappedId, List<Block> chain)
        {
            if (_block == null) return;

            switch (result)
            {
                case GridSystem.TapResult.Moved:
                    if (tappedId == _block.Id)
                        PlayFlyOut();
                    break;

                case GridSystem.TapResult.Destroyed:
                    if (tappedId == _block.Id)
                        PlayDestroy();
                    break;

                case GridSystem.TapResult.Blocked:
                    HandleBlockedChain(chain);
                    break;

                case GridSystem.TapResult.Rotated:
                    // Reposition all connected blocks (any might have moved)
                    UpdateWorldPosition();
                    UpdateArrowRotation();
                    OnRotated();
                    break;

                case GridSystem.TapResult.RotateBlocked:
                    // Shake all blocks that would have been moved (chain contains the blocker)
                    if (tappedId == _block.Id)
                        PlayShake(0f);
                    else
                        HandleBlockedChain(chain);
                    break;
            }
        }

        /// <summary>Called when this block's rotation completes. Override in subclasses for extra visuals.</summary>
        protected virtual void OnRotated() { }

        // ── Animations ────────────────────────────────────────

        /// <summary>Slide out past the grid edge and destroy.</summary>
        private void PlayFlyOut()
        {
            if (_gridView == null) { Destroy(gameObject); return; }

            GridSystem.DirectionToStep(_block.Direction, out int dx, out int dy);
            // Fly far enough off-screen (gridSize + 2 cells)
            float cellSize = CameraController.Instance != null
                ? CameraController.Instance.GetCellSize(GameManager.Instance.Grid.GridSize)
                : 1f;
            int cells = GameManager.Instance.Grid.GridSize + 2;
            Vector3 target = transform.position + new Vector3(dx, dy, 0f) * (cells * cellSize);

            DOTween.Kill(transform);
            transform.DOMove(target, FLY_OUT_DURATION)
                .SetEase(Ease.InCubic)
                .SetLink(gameObject)
                .OnComplete(() => Destroy(gameObject));
        }

        /// <summary>Block hit a Gear — scale-down burst.</summary>
        private void PlayDestroy()
        {
            DOTween.Kill(transform);
            transform.DOScale(Vector3.zero, 0.25f)
                .SetEase(Ease.InBack)
                .SetLink(gameObject)
                .OnComplete(() => Destroy(gameObject));
        }

        /// <summary>Handle domino shake chain for blocked moves.</summary>
        private void HandleBlockedChain(List<Block> chain)
        {
            if (chain == null || chain.Count == 0) return;

            for (int i = 0; i < chain.Count; i++)
            {
                if (chain[i].Id != _block.Id) continue;

                float delay = i * SHAKE_DELAY_STEP;
                bool isFirstBlocker = i == 0;

                if (isFirstBlocker)
                    PlayWarningFlash(delay);

                PlayShake(delay);
                break;
            }
        }

        private void PlayWarningFlash(float delay)
        {
            if (_spriteRenderer == null) return;

            DOTween.Kill(_spriteRenderer);
            _spriteRenderer.DOColor(_warningColor, WARNING_DURATION * 0.4f)
                .SetDelay(delay)
                .SetEase(Ease.OutFlash)
                .SetLink(gameObject)
                .OnComplete(() =>
                    _spriteRenderer.DOColor(_defaultColor, WARNING_DURATION * 0.6f)
                        .SetLink(gameObject));
        }

        private void PlayShake(float delay)
        {
            DOTween.Kill(transform);
            transform.DOShakePosition(SHAKE_DURATION, SHAKE_MAGNITUDE, 20, 90f, false, true)
                .SetDelay(delay)
                .SetLink(gameObject);
        }

        // ── Helpers ───────────────────────────────────────────

        private void UpdateWorldPosition()
        {
            if (_gridView == null || _block == null) return;
            transform.position = _gridView.GridToWorld(_block.X, _block.Y);
        }

        private void UpdateArrowRotation()
        {
            if (_arrowTransform == null || _block == null) return;

            float angle = _block.Direction switch
            {
                BlockDirection.Up    => 90f,
                BlockDirection.Down  => 270f,
                BlockDirection.Left  => 180f,
                _                    => 0f,   // Right
            };
            _arrowTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
