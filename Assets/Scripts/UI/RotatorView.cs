using TapAway.Core;
using UnityEngine;

namespace TapAway
{
    public class RotatorView : MonoBehaviour
    {
        [SerializeField] private Transform _baseVisual;
        [SerializeField] private Transform _capVisual;
        [SerializeField, Min(0f)] private float _capPopDistance = 0.08f;
        [SerializeField, Min(0.01f)] private float _capMoveSpeed = 2f;

        private Block _block;
        private Vector3 _capStartLocalPos;
        private bool _isCapAnimating;
        private bool _isCapMovingUp;

        private void Awake()
        {
            if (_capVisual != null)
            {
                _capStartLocalPos = _capVisual.localPosition;
            }
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

            RefreshDirection();
        }

        private void OnDestroy()
        {
            if (_block != null)
            {
                _block.Rotated -= OnRotated;
                _block.Removed -= OnRemoved;
            }
        }

        private void RefreshDirection()
        {
            if (_baseVisual == null || _block == null)
            {
                return;
            }

            float z = -90f * (int)_block.Direction;
            _baseVisual.localRotation = Quaternion.Euler(0f, 0f, z);
        }

        private void OnRotated(Block block)
        {
            RefreshDirection();

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

        private void OnRemoved(Block block)
        {
            gameObject.SetActive(false);
        }
    }
}
