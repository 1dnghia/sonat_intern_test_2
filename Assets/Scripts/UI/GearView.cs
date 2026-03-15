using TapAway.Core;
using UnityEngine;

namespace TapAway
{
    public class GearView : MonoBehaviour
    {
        private const float DEFAULT_SPIN_SPEED = 90f;

        [SerializeField] private Transform _mainVisual;
        [SerializeField] private Transform _shadowVisual;
        [SerializeField] private float _spinSpeed = DEFAULT_SPIN_SPEED;

        public void Initialise(Block block)
        {
            if (_mainVisual == null)
            {
                _mainVisual = transform;
            }
        }

        private void Update()
        {
            float delta = -_spinSpeed * Time.deltaTime;

            if (_mainVisual != null)
            {
                _mainVisual.Rotate(0f, 0f, delta, Space.Self);
            }

            if (_shadowVisual != null)
            {
                _shadowVisual.Rotate(0f, 0f, delta, Space.Self);
            }
        }
    }
}

