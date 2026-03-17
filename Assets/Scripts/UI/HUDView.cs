using UnityEngine;
using UnityEngine.UI;
using System;
using System.Diagnostics;
using TMPro;

namespace TapAway
{
	public class HUDView : MonoBehaviour
	{
		// Text hiển thị số level hiện tại.
		[SerializeField] private TextMeshProUGUI _levelText;
		// Text hiển thị số moves còn lại.
		[SerializeField] private TextMeshProUGUI _movesText;
		// Text hiển thị số coin hiện có.
		[SerializeField] private TextMeshProUGUI _coinText;

		// Nút mở/đóng panel setting.
		[SerializeField] private Button _settingButton;
		// Nút chơi lại level hiện tại.
		[SerializeField] private Button _retryButton;
		// Nút bật chế độ dùng bomb.
		[SerializeField] private Button _bombButton;
		// Nút mua thêm moves.
		[SerializeField] private Button _addMovesButton;
		// Text phụ của nút bomb (lượt miễn phí hoặc giá coin).
		[SerializeField] private TextMeshProUGUI _bombMetaText;
		// Root UI chứa icon coin + text giá của bomb.
		[SerializeField] private GameObject _bombCostRoot;
		// Text phụ của nút add moves (lượt miễn phí hoặc giá coin).
		[SerializeField] private TextMeshProUGUI _addMovesMetaText;
		// Root UI chứa icon coin + text giá của add moves.
		[SerializeField] private GameObject _addMovesCostRoot;
		// Badge tròn hiển thị lượt free còn lại của bomb.
		[SerializeField] private Image _bombFreeBadgeImage;
		// Text số lượt free còn lại trong badge bomb.
		[SerializeField] private TextMeshProUGUI _bombFreeBadgeText;
		// Badge tròn hiển thị lượt free còn lại của add moves.
		[SerializeField] private Image _addMovesFreeBadgeImage;
		// Text số lượt free còn lại trong badge add moves.
		[SerializeField] private TextMeshProUGUI _addMovesFreeBadgeText;
		// Màu hiển thị nút bomb khi ở trạng thái bình thường.
		[SerializeField] private Color _bombNormalColor = Color.white;
		// Màu hiển thị nút bomb khi đang armed.
		[SerializeField] private Color _bombArmedColor = Color.red;

		// Panel cài đặt trên HUD.
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

			AutoBindBoosterCostTexts();
			WarnIfBoosterCostBindingsConflict();

			SetCostRootVisible(_bombCostRoot, false);
			SetCostRootVisible(_addMovesCostRoot, false);

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

		public void SetBombState(bool isArmed, int freeUsesRemaining, int cost, bool canUseByCoin)
		{
			if (_bombButton != null)
			{
				bool canUse = freeUsesRemaining > 0 || canUseByCoin;
				_bombButton.interactable = isArmed || canUse;

				Graphic targetGraphic = _bombButton.targetGraphic;
				if (targetGraphic != null)
				{
					targetGraphic.color = isArmed ? _bombArmedColor : _bombNormalColor;
				}
			}


			UpdateBoosterCostUi(_bombMetaText, _bombCostRoot, freeUsesRemaining, cost);

			UpdateFreeBadge(_bombFreeBadgeImage, _bombFreeBadgeText, freeUsesRemaining);
		}

		public void SetAddMovesState(int freeUsesRemaining, int cost, bool canUseByCoin)
		{
			if (_addMovesButton != null)
			{
				bool canUse = freeUsesRemaining > 0 || canUseByCoin;
				_addMovesButton.interactable = canUse;
			}

			UpdateBoosterCostUi(_addMovesMetaText, _addMovesCostRoot, freeUsesRemaining, cost);

			UpdateFreeBadge(_addMovesFreeBadgeImage, _addMovesFreeBadgeText, freeUsesRemaining);
		}

		private void OnSettingClicked()
		{
			if (AudioManager.Instance != null)
			{
				AudioManager.Instance.PlayUiClick();
			}

			if (_settingPanel != null)
			{
				_settingPanel.SetActive(!_settingPanel.activeSelf);
			}

			SettingPressed?.Invoke();
		}

		private void OnRetryClicked()
		{
			if (AudioManager.Instance != null)
			{
				AudioManager.Instance.PlayUiClick();
			}

			RetryPressed?.Invoke();
		}

		private void OnBombClicked()
		{
			if (AudioManager.Instance != null)
			{
				AudioManager.Instance.PlayUiClick();
			}

			BombPressed?.Invoke();
		}

		private void OnAddMovesClicked()
		{
			if (AudioManager.Instance != null)
			{
				AudioManager.Instance.PlayUiClick();
			}

			AddMovesPressed?.Invoke();
		}

		private static string FormatBoosterMetaText(int freeUsesRemaining, int cost)
		{
			if (freeUsesRemaining > 0)
			{
				return string.Empty;
			}

			// Icon coin nằm trong cost root nên text chỉ giữ giá trị số.
			return Mathf.Max(0, cost).ToString();
		}

		private static void UpdateBoosterCostUi(
			TextMeshProUGUI costText,
			GameObject costRoot,
			int freeUsesRemaining,
			int cost)
		{
			bool showCost = freeUsesRemaining <= 0;
			SetCostRootVisible(costRoot, showCost);

			if (costText == null)
			{
				return;
			}

			costText.text = showCost ? FormatBoosterMetaText(freeUsesRemaining, cost) : string.Empty;
		}

		private static void SetCostRootVisible(GameObject costRoot, bool visible)
		{
			if (costRoot == null)
			{
				return;
			}

			costRoot.SetActive(visible);
		}

		private void AutoBindBoosterCostTexts()
		{
			if (_bombCostRoot != null)
			{
				TextMeshProUGUI bombTextFromRoot = _bombCostRoot.GetComponentInChildren<TextMeshProUGUI>(true);
				if (bombTextFromRoot != null)
				{
					_bombMetaText = bombTextFromRoot;
				}
			}

			if (_addMovesCostRoot != null)
			{
				TextMeshProUGUI addMovesTextFromRoot = _addMovesCostRoot.GetComponentInChildren<TextMeshProUGUI>(true);
				if (addMovesTextFromRoot != null)
				{
					_addMovesMetaText = addMovesTextFromRoot;
				}
			}
		}

		[Conditional("UNITY_EDITOR")]
		private void WarnIfBoosterCostBindingsConflict()
		{
			if (_bombCostRoot != null && _addMovesCostRoot != null && _bombCostRoot == _addMovesCostRoot)
			{
				UnityEngine.Debug.LogWarning("[HUDView] BombCostRoot va AddMovesCostRoot dang trung nhau.", this);
			}

			if (_bombMetaText != null && _addMovesMetaText != null && _bombMetaText == _addMovesMetaText)
			{
				UnityEngine.Debug.LogWarning("[HUDView] BombMetaText va AddMovesMetaText dang trung nhau.", this);
			}
		}

		private static void UpdateFreeBadge(Image badgeImage, TextMeshProUGUI badgeText, int freeUsesRemaining)
		{
			bool isVisible = freeUsesRemaining > 0;

			if (badgeImage != null)
			{
				badgeImage.gameObject.SetActive(isVisible);
			}

			if (badgeText != null)
			{
				badgeText.gameObject.SetActive(isVisible);
				if (isVisible)
				{
					badgeText.text = freeUsesRemaining.ToString();
				}
			}
		}
	}
}

