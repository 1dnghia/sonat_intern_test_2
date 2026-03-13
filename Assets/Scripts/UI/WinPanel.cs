using TapAway.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TapAway.UI
{
    /// <summary>
    /// Shown when the player clears all blocks. Listens to GameManager.OnWin.
    /// </summary>
    public class WinPanel : MonoBehaviour
    {
        // ── Serialized Fields ──────────────────────────────────
        [SerializeField] private GameObject    _panel;
        [SerializeField] private TextMeshProUGUI _winText;
        [SerializeField] private Button        _nextLevelButton;
        [SerializeField] private Button        _replayButton;
        [SerializeField] private Button        _homeButton;

        // ── Lifecycle ─────────────────────────────────────────

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnWin += Show;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnWin -= Show;
        }

        private void Start()
        {
            _panel?.SetActive(false);

            if (_nextLevelButton != null) _nextLevelButton.onClick.AddListener(OnNextLevel);
            if (_replayButton    != null) _replayButton.onClick.AddListener(OnReplay);
            if (_homeButton      != null) _homeButton.onClick.AddListener(OnHome);
        }

        // ── Private ───────────────────────────────────────────

        private void Show()
        {
            _panel?.SetActive(true);
        }

        private void OnNextLevel()
        {
            // TODO: advance to next level via LevelManager
            OnReplay();
        }

        private void OnReplay()
        {
            _panel?.SetActive(false);
            GameManager.Instance?.LoadLevel(GameManager.Instance.CurrentLevel);
        }

        private void OnHome()
        {
            SceneManager.LoadScene(0);
        }
    }
}
