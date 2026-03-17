using TapAway.Core;
using System.Collections;
using UnityEngine;

namespace TapAway
{
    public class GearView : MonoBehaviour
    {
        private const float DEFAULT_SPIN_SPEED = 90f;

        // Transform visual chính của gear.
        [SerializeField] private Transform _mainVisual;
        // Transform bóng/overlay quay đồng bộ với gear.
        [SerializeField] private Transform _shadowVisual;
        // Tốc độ quay gear theo độ/giây.
        [SerializeField] private float _spinSpeed = DEFAULT_SPIN_SPEED;
        // Scale khi nhấn gear để tạo cảm giác bấm.
        [SerializeField, Range(0.7f, 1f)] private float _tapScale = 0.9f;
        // Thời gian thu nhỏ khi nhấn gear.
        [SerializeField, Min(0.01f)] private float _tapDownDuration = 0.04f;
        // Thời gian phục hồi scale sau khi nhấn gear.
        [SerializeField, Min(0.01f)] private float _tapUpDuration = 0.08f;

        private Coroutine _tapPulseCoroutine;
        private Vector3 _initialScale = Vector3.one;

        private void Awake()
        {
            _initialScale = transform.localScale;
        }

        public void Initialise(Block block)
        {
            if (_mainVisual == null)
            {
                _mainVisual = ResolveMainVisualTransform();
            }

            _initialScale = transform.localScale;
        }

        public void PlayTapFeedback()
        {
            if (_tapPulseCoroutine != null)
            {
                StopCoroutine(_tapPulseCoroutine);
            }

            _tapPulseCoroutine = StartCoroutine(PlayTapFeedbackCoroutine());
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

        private IEnumerator PlayTapFeedbackCoroutine()
        {
            yield return ScaleOverTime(_initialScale, _initialScale * _tapScale, _tapDownDuration);
            yield return ScaleOverTime(transform.localScale, _initialScale, _tapUpDuration);
            transform.localScale = _initialScale;
            _tapPulseCoroutine = null;
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

        private Transform ResolveMainVisualTransform()
        {
            SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < sprites.Length; i++)
            {
                Transform candidate = sprites[i].transform;
                if (_shadowVisual != null && candidate == _shadowVisual)
                {
                    continue;
                }

                return candidate;
            }

            return transform;
        }
    }
}

