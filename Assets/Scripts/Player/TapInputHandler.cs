using TapAway.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace TapAway.Player
{
    /// <summary>
    /// Converts screen taps to grid coordinates and forwards them to <see cref="GameManager"/>.
    /// Raycast against a flat plane at Z=0 in world space.
    /// </summary>
    public class TapInputHandler : MonoBehaviour
    {
        // ── Serialized Fields ──────────────────────────────────
        [SerializeField, Tooltip("Reference to the GridView to convert world→grid coords")]
        private GridView _gridView;

        // ── Private Fields ────────────────────────────────────
        private Camera _camera;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            // Support both touch and mouse (editor)
            if (Touchscreen.current != null)
            {
                var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
                foreach (var touch in touches)
                {
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        ProcessScreenPosition(touch.screenPosition);
                    }
                }
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                ProcessScreenPosition(Mouse.current.position.ReadValue());
            }
        }

        // ── Private ───────────────────────────────────────────

        private void ProcessScreenPosition(Vector2 screenPos)
        {
            if (_gridView == null) return;

            Ray ray = _camera.ScreenPointToRay(screenPos);
            // Intersect with Z=0 plane
            if (!new Plane(Vector3.forward, Vector3.zero).Raycast(ray, out float dist))
                return;

            Vector3 worldPos = ray.GetPoint(dist);
            if (_gridView.WorldToGrid(worldPos, out int gx, out int gy))
            {
                GameManager.Instance.HandleTap(gx, gy);
            }
        }
    }
}
