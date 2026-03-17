using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace TapAway
{
    public class WinPanel : MonoBehaviour
    {
        // Root của panel thắng; fallback dùng chính GameObject hiện tại.
        [SerializeField] private GameObject _root;
        // Nút nhận thưởng nhiều hơn (ads).
        [SerializeField] private Button _getMoreButton;
        // Nút nhận thưởng thường.
        [SerializeField] private Button _getButton;
        // Text hiển thị số coin hiện tại.
        [SerializeField] private TextMeshProUGUI _coinText;

        public event Action GetMorePressed;
        public event Action GetPressed;

        private void OnEnable()
        {
            if (_getMoreButton != null) _getMoreButton.onClick.AddListener(OnGetMoreClicked);
            if (_getButton != null) _getButton.onClick.AddListener(OnGetClicked);

            CoinWallet.OnCoinChanged += SetCoin;
            SetCoin(CoinWallet.Balance);
        }

        private void OnDisable()
        {
            if (_getMoreButton != null) _getMoreButton.onClick.RemoveListener(OnGetMoreClicked);
            if (_getButton != null) _getButton.onClick.RemoveListener(OnGetClicked);

            CoinWallet.OnCoinChanged -= SetCoin;
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

        public void SetCoin(int coin)
        {
            if (_coinText != null)
            {
                _coinText.text = coin.ToString();
            }
        }

        private void OnGetMoreClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUiClick();
            }

            GetMorePressed?.Invoke();
        }

        private void OnGetClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUiClick();
            }

            GetPressed?.Invoke();
        }
    }
}
