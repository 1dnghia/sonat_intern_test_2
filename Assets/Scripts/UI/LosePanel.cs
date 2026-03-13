using TapAway.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TapAway.UI
{
    /// <summary>
    /// Shown when the player runs out of moves. Listens to GameManager.OnLose.
    /// </summary>
    public class LosePanel : MonoBehaviour
    {
        // ── Serialized Fields ──────────────────────────────────
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button     _retryButton;
        [SerializeField] private Button     _buyMovesButton;
        [SerializeField] private Button     _homeButton;

        // ── Lifecycle ─────────────────────────────────────────

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnLose += Show;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnLose -= Show;
        }

        private void Start()
        {
            _panel?.SetActive(false);

            if (_retryButton    != null) _retryButton.onClick.AddListener(OnRetry);
            if (_buyMovesButton != null) _buyMovesButton.onClick.AddListener(OnBuyMoves);
            if (_homeButton     != null) _homeButton.onClick.AddListener(OnHome);
        }

        // ── Private ───────────────────────────────────────────

        private void Show()
        {
            _panel?.SetActive(true);
        }

        private void OnRetry()
        {
            _panel?.SetActive(false);
            GameManager.Instance?.LoadLevel(GameManager.Instance.CurrentLevel);
        }

        private void OnBuyMoves()
        {
            _panel?.SetActive(false);
            // TODO: deduct currency then resume
            GameManager.Instance?.AddMoves(5);
        }

        private void OnHome()
        {
            SceneManager.LoadScene(0);
        }
    }
}
