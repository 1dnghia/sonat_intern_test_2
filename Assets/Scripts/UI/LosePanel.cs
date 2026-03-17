using UnityEngine;
using UnityEngine.UI;
using System;

namespace TapAway
{
    public class LosePanel : MonoBehaviour
    {
        // Root của panel thua; fallback dùng chính GameObject hiện tại.
        [SerializeField] private GameObject _root;
        // Nút retry level khi thua.
        [SerializeField] private Button _retryButton;
        // Nút cộng thêm moves từ panel thua.
        [SerializeField] private Button _addMovesButton;

        public event Action RetryPressed;
        public event Action AddMovesPressed;

        private void OnEnable()
        {
            if (_retryButton != null) _retryButton.onClick.AddListener(OnRetry);
            if (_addMovesButton != null) _addMovesButton.onClick.AddListener(OnAddMoves);
        }

        private void OnDisable()
        {
            if (_retryButton != null) _retryButton.onClick.RemoveListener(OnRetry);
            if (_addMovesButton != null) _addMovesButton.onClick.RemoveListener(OnAddMoves);
        }

        public void Show(bool visible)
        {
            if (_root != null)
            {
                _root.SetActive(visible);
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }

        public void SetAddMovesVisible(bool visible)
        {
            if (_addMovesButton == null)
            {
                return;
            }

            _addMovesButton.gameObject.SetActive(visible);
        }

        private void OnRetry()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUiClick();
            }

            RetryPressed?.Invoke();
        }

        private void OnAddMoves()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUiClick();
            }

            AddMovesPressed?.Invoke();
        }
    }
}
