using UnityEngine;
using UnityEngine.UI;
using System;

namespace TapAway
{
    public class LosePanel : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _retryButton;
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

        private void OnRetry()
        {
            RetryPressed?.Invoke();
        }

        private void OnAddMoves()
        {
            AddMovesPressed?.Invoke();
        }
    }
}
