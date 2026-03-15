using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace TapAway
{
	public class HUDView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _levelText;
		[SerializeField] private TextMeshProUGUI _movesText;
		[SerializeField] private TextMeshProUGUI _coinText;

		[SerializeField] private Button _settingButton;
		[SerializeField] private Button _retryButton;
		[SerializeField] private Button _bombButton;
		[SerializeField] private Button _addMovesButton;

		[SerializeField] private GameObject _settingPanel;

		public event Action SettingPressed;
		public event Action RetryPressed;
		public event Action BombPressed;
		public event Action AddMovesPressed;

		private void OnEnable()
		{
			if (_settingButton != null) _settingButton.onClick.AddListener(OnSettingClicked);
			if (_retryButton != null) _retryButton.onClick.AddListener(OnRetryClicked);
			if (_bombButton != null) _bombButton.onClick.AddListener(OnBombClicked);
			if (_addMovesButton != null) _addMovesButton.onClick.AddListener(OnAddMovesClicked);

			CoinWallet.OnCoinChanged += SetCoin;
			SetCoin(CoinWallet.Balance);
		}

		private void OnDisable()
		{
			if (_settingButton != null) _settingButton.onClick.RemoveListener(OnSettingClicked);
			if (_retryButton != null) _retryButton.onClick.RemoveListener(OnRetryClicked);
			if (_bombButton != null) _bombButton.onClick.RemoveListener(OnBombClicked);
			if (_addMovesButton != null) _addMovesButton.onClick.RemoveListener(OnAddMovesClicked);

			CoinWallet.OnCoinChanged -= SetCoin;
		}

		public void SetLevel(int level)
		{
			if (_levelText != null)
			{
				_levelText.text = $"Level {level}";
			}
		}

		public void SetMoves(int moves)
		{
			if (_movesText != null)
			{
				_movesText.text = moves < 0 ? "∞ moves" : $"{moves} moves";
			}
		}

		public void SetCoin(int coin)
		{
			if (_coinText != null)
			{
				_coinText.text = coin.ToString();
			}
		}

		private void OnSettingClicked()
		{
			if (_settingPanel != null)
			{
				_settingPanel.SetActive(!_settingPanel.activeSelf);
			}

			SettingPressed?.Invoke();
		}

		private void OnRetryClicked()
		{
			RetryPressed?.Invoke();
		}

		private void OnBombClicked()
		{
			BombPressed?.Invoke();
		}

		private void OnAddMovesClicked()
		{
			AddMovesPressed?.Invoke();
		}
	}
}

