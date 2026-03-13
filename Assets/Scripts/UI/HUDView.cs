using TapAway.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapAway.UI
{
    /// <summary>
    /// In-game HUD: shows moves counter, level info, and help buttons.
    /// </summary>
    public class HUDView : MonoBehaviour
    {
        // ── Serialized Fields ──────────────────────────────────
        [SerializeField] private TextMeshProUGUI _movesText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Button          _addMovesButton;
        [SerializeField] private Button          _removeBlockButton;

        // ── Lifecycle ─────────────────────────────────────────

        private void OnEnable()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.OnMovesChanged += RefreshMoves;
            gm.OnLevelLoaded  += RefreshAll;
        }

        private void OnDisable()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.OnMovesChanged -= RefreshMoves;
            gm.OnLevelLoaded  -= RefreshAll;
        }

        private void Start()
        {
            if (_addMovesButton != null)
                _addMovesButton.onClick.AddListener(OnAddMovesClicked);

            if (_removeBlockButton != null)
                _removeBlockButton.onClick.AddListener(OnRemoveBlockClicked);

            RefreshAll();
        }

        // ── Private ───────────────────────────────────────────

        private void RefreshAll()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (_levelText != null)
                _levelText.text = $"Level {gm.CurrentLevel?.levelIndex}";

            RefreshMoves(gm.MovesRemaining);
        }

        private void RefreshMoves(int remaining)
        {
            if (_movesText == null) return;

            _movesText.text = remaining < 0
                ? "∞"
                : remaining.ToString();
        }

        private void OnAddMovesClicked()
        {
            // TODO: deduct currency; for now just add 5 for free
            GameManager.Instance?.AddMoves(5);
        }

        private void OnRemoveBlockClicked()
        {
            // TODO: enter "select block to remove" mode
            Debug.Log("[HUD] Remove block not yet wired to block-selection mode.");
        }
    }
}
