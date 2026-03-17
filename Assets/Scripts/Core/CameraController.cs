using UnityEngine;

namespace TapAway
{
    public class CameraController : MonoBehaviour
    {
        // Camera mục tiêu cần set thông số.
        [SerializeField] private Camera _targetCamera;
        // Orthographic size tối thiểu cho camera gameplay.
        [SerializeField, Min(0.1f)] private float _minOrthoSize = 1f;
        // Khoảng đệm thêm quanh grid để UI/viền không bị sát mép.
        [SerializeField, Min(0f)] private float _gridPadding = 1f;

        private bool _hasAppliedGridFit;

        private void Awake()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_targetCamera != null)
            {
                _targetCamera.orthographic = true;
                _targetCamera.orthographicSize = _minOrthoSize;
            }

            CenterOnWorldOrigin();
        }

        private void Start()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_targetCamera != null)
            {
                _targetCamera.orthographic = true;
                if (!_hasAppliedGridFit)
                {
                    _targetCamera.orthographicSize = _minOrthoSize;
                }
            }

            CenterOnWorldOrigin();
        }

        public void SetupForGrid(int width, int height, float cellPitch)
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_targetCamera == null)
            {
                return;
            }

            int safeWidth = Mathf.Max(1, width);
            int safeHeight = Mathf.Max(1, height);
            float safeCellPitch = Mathf.Max(0.1f, cellPitch);

            // Span theo center-to-center cộng thêm nửa ô 2 đầu.
            float boardWidth = (safeWidth - 1) * safeCellPitch + safeCellPitch + _gridPadding * 2f;
            float boardHeight = (safeHeight - 1) * safeCellPitch + safeCellPitch + _gridPadding * 2f;
            float aspect = Mathf.Max(0.01f, _targetCamera.aspect);

            float fitByHeight = boardHeight * 0.5f;
            float fitByWidth = boardWidth * 0.5f / aspect;
            float targetOrtho = Mathf.Max(_minOrthoSize, fitByHeight, fitByWidth);

            _targetCamera.orthographic = true;
            _targetCamera.orthographicSize = targetOrtho;
            _hasAppliedGridFit = true;

            CenterOnWorldOrigin();
        }

        private void CenterOnWorldOrigin()
        {
            if (_targetCamera != null)
            {
                Transform cameraTransform = _targetCamera.transform;
                cameraTransform.position = new Vector3(0f, 0f, cameraTransform.position.z);
            }

            transform.position = new Vector3(0f, 0f, transform.position.z);
        }
    }
}
