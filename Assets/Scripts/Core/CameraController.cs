using UnityEngine;

namespace TapAway
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform _gridRoot;
        [SerializeField] private Camera _targetCamera;
        [SerializeField, Min(1f)] private float _baseOrthoSize = 6f;

        private void Start()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_gridRoot != null)
            {
                Vector3 p = _gridRoot.position;
                transform.position = new Vector3(p.x, p.y, transform.position.z);
            }

            if (_targetCamera != null)
            {
                _targetCamera.orthographic = true;
                _targetCamera.orthographicSize = _baseOrthoSize;
            }
        }
    }
}
