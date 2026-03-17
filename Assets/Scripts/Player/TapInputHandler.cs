using TapAway.Core;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine;

namespace TapAway
{
    public class TapInputHandler : MonoBehaviour
    {
        // Camera dùng để convert tọa độ màn hình sang world.
        [SerializeField] private Camera _camera;
        // Tham chiếu GridView để map collider sang block logic.
        [SerializeField] private GridView _gridView;
        // Tham chiếu GameManager để gửi hành động tap.
        [SerializeField] private GameManager _gameManager;
        // Layer mask giới hạn collider được coi là block.
        [SerializeField] private LayerMask _blockLayerMask = ~0;

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void Update()
        {
            if (_camera == null || _gridView == null || _gameManager == null)
            {
                return;
            }

            if (!TryGetTapWorldPosition(out Vector3 worldPos))
            {
                return;
            }

            if (IsTapOverUi())
            {
                return;
            }

            worldPos.z = 0f;

            Collider2D hitCollider = Physics2D.OverlapPoint(worldPos, _blockLayerMask);
            if (hitCollider != null && _gridView.TryGetBlockByCollider(hitCollider, out Block hitBlock))
            {
                _gameManager.TapBlock(hitBlock);
                return;
            }

            if (_gridView.TryGetBlockAtWorldPosition(worldPos, out Block blockByPos))
            {
                _gameManager.TapBlock(blockByPos);
                return;
            }

            Vector2Int cell = _gridView.WorldToCell(worldPos);
            _gameManager.TapGridCell(cell);
        }

        private bool TryGetTapWorldPosition(out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;

            Vector2 screenPosition;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
            }
            else
            {
                return false;
            }

            worldPosition = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
            return true;
        }

        private bool IsTapOverUi()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
                return EventSystem.current.IsPointerOverGameObject(touchId);
            }

            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
