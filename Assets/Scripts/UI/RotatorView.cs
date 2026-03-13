using System.Collections.Generic;
using DG.Tweening;
using TapAway.Core;
using TapAway.Data;
using UnityEngine;

namespace TapAway
{
    /// <summary>
    /// Rotator block visual — standalone MonoBehaviour, two-part "button" design.
    ///   _base : static bottom layer (body / shadow, never moves)
    ///   _cap  : top layer that pops upward on successful rotation,
    ///           giving a satisfying physical "key press" feel.
    /// Does NOT inherit BlockView — Rotator has its own distinct visual contract.
    /// </summary>
    public class RotatorView : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────
        private const float CAP_POP_OFFSET    = 0.12f;
        private const float CAP_POP_DURATION  = 0.08f;
        private const float CAP_DOWN_DURATION = 0.14f;
        private const float SHAKE_DURATION    = 0.25f;
        private const float SHAKE_MAGNITUDE   = 0.12f;
        private const float SHAKE_DELAY_STEP  = 0.07f;
        private const float WARNING_DURATION  = 0.4f;

        // ── Serialized Fields ──────────────────────────────────
        [SerializeField, Tooltip("Top part of the button — pops upward on successful rotation")]
        private Transform _cap;

        [SerializeField, Tooltip("Static base / body of the button")]
        private Transform _base;

        [SerializeField, Tooltip("Renderer used for warning flash when blocked")]
        private SpriteRenderer _capRenderer;

        [SerializeField] private Color _defaultColor = Color.white;
        [SerializeField] private Color _warningColor = new Color(1f, 0.3f, 0.3f);

        // ── Private Fields ────────────────────────────────────
        private Block    _block;
        private GridView _gridView;
        private Vector3  _capRestLocalPos;
        private bool     _isSubscribed;

        // ── Lifecycle ─────────────────────────────────────────

        private void OnEnable()
        {
            if (_cap != null)
            {
                _capRestLocalPos = _cap.localPosition;
            }

            if (_base == null)
            {
                _base = transform;
            }

            TrySubscribe();
        }

        private void Start()
        {
            // Safety: if GameManager was not ready in OnEnable, subscribe now.
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (_isSubscribed && GameManager.Instance != null)
            {
                GameManager.Instance.OnBlockTapped -= HandleBlockTapped;
                _isSubscribed = false;
            }

            DOTween.Kill(transform);
            if (_cap != null) DOTween.Kill(_cap);
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>Called by GridView after instantiation.</summary>
        public void Initialise(Block block)
        {
            _block    = block;
            _gridView = GetComponentInParent<GridView>();

            if (_cap != null)
            {
                _capRestLocalPos = _cap.localPosition;
            }

            if (_capRenderer == null && _cap != null)
            {
                _capRenderer = _cap.GetComponent<SpriteRenderer>();
            }

            if (_capRenderer != null)
                _capRenderer.color = _defaultColor;
        }

        // ── Event Handling ────────────────────────────────────

        private void HandleBlockTapped(GridSystem.TapResult result, int tappedId, List<Block> chain)
        {
            if (_block == null) return;

            switch (result)
            {
                case GridSystem.TapResult.Rotated:
                    // All connected blocks may have moved — reposition then pop cap.
                    UpdateWorldPosition();
                    PlayCapPop();
                    break;

                case GridSystem.TapResult.RotateBlocked:
                    // This rotator was tapped but blocked.
                    if (tappedId == _block.Id)
                        PlayShake(0f);
                    break;

                case GridSystem.TapResult.Blocked:
                    // This rotator may appear as a blocker in a normal-block chain.
                    PlayDominoShake(chain);
                    break;
            }
        }

        // ── Animations ────────────────────────────────────────

        private void PlayCapPop()
        {
            if (_cap == null) return;

            DOTween.Kill(_cap);
            float peakY = _capRestLocalPos.y + CAP_POP_OFFSET;

            _cap.DOLocalMoveY(peakY, CAP_POP_DURATION)
                .SetEase(Ease.OutCubic)
                .SetLink(gameObject)
                .OnComplete(() =>
                    _cap.DOLocalMoveY(_capRestLocalPos.y, CAP_DOWN_DURATION)
                        .SetEase(Ease.InCubic)
                        .SetLink(gameObject));
        }

        private void PlayDominoShake(List<Block> chain)
        {
            if (chain == null) return;
            for (int i = 0; i < chain.Count; i++)
            {
                if (chain[i].Id != _block.Id) continue;
                if (i == 0) PlayWarningFlash(0f);
                PlayShake(i * SHAKE_DELAY_STEP);
                break;
            }
        }

        private void PlayShake(float delay)
        {
            DOTween.Kill(transform);
            transform.DOShakePosition(SHAKE_DURATION, SHAKE_MAGNITUDE, 20, 90f, false, true)
                .SetDelay(delay)
                .SetLink(gameObject);
        }

        private void PlayWarningFlash(float delay)
        {
            if (_capRenderer == null) return;
            DOTween.Kill(_capRenderer);
            _capRenderer.DOColor(_warningColor, WARNING_DURATION * 0.4f)
                .SetDelay(delay)
                .SetEase(Ease.OutFlash)
                .SetLink(gameObject)
                .OnComplete(() =>
                    _capRenderer.DOColor(_defaultColor, WARNING_DURATION * 0.6f)
                        .SetLink(gameObject));
        }

        // ── Helpers ───────────────────────────────────────────

        private void UpdateWorldPosition()
        {
            if (_gridView == null || _block == null) return;
            transform.position = _gridView.GridToWorld(_block.X, _block.Y);
        }

        private void TrySubscribe()
        {
            if (_isSubscribed)
            {
                return;
            }

            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.OnBlockTapped += HandleBlockTapped;
            _isSubscribed = true;
        }
    }
}
