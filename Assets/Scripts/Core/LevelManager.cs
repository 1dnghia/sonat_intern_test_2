using System.Collections.Generic;
using TapAway.Core;
using TapAway.Data;
using TapAway.Infrastructure;
using UnityEngine;

namespace TapAway.Core
{
    /// <summary>
    /// Manages the ordered list of levels; saves / loads current progress.
    /// </summary>
    public class LevelManager : SingletonMonoBehaviour<LevelManager>
    {
        // ── Constants ──────────────────────────────────────────
        private const string PREFS_KEY_CURRENT_LEVEL = "CurrentLevelIndex";

        // ── Serialized Fields ──────────────────────────────────
        [SerializeField, Tooltip("Ordered list of all LevelData assets")]
        private List<LevelData> _levels = new List<LevelData>();

        // ── Properties ────────────────────────────────────────
        public int CurrentLevelIndex { get; private set; }
        public int TotalLevels       => _levels.Count;

        // ── Lifecycle ─────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            CurrentLevelIndex = PlayerPrefs.GetInt(PREFS_KEY_CURRENT_LEVEL, 0);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnWin += OnLevelWon;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnWin -= OnLevelWon;
        }

        // ── Public API ────────────────────────────────────────

        public void LoadCurrentLevel()
        {
            var level = GetLevel(CurrentLevelIndex);
            if (level != null)
                GameManager.Instance?.LoadLevel(level);
            else
                Debug.LogWarning("[LevelManager] No level found at index " + CurrentLevelIndex);
        }

        public void LoadLevel(int index)
        {
            CurrentLevelIndex = Mathf.Clamp(index, 0, _levels.Count - 1);
            PlayerPrefs.SetInt(PREFS_KEY_CURRENT_LEVEL, CurrentLevelIndex);
            var level = GetLevel(CurrentLevelIndex);
            GameManager.Instance?.LoadLevel(level);
        }

        public LevelData GetLevel(int index) =>
            index >= 0 && index < _levels.Count ? _levels[index] : null;

        // ── Private ───────────────────────────────────────────

        private void OnLevelWon()
        {
            int next = CurrentLevelIndex + 1;
            if (next < _levels.Count)
            {
                CurrentLevelIndex = next;
                PlayerPrefs.SetInt(PREFS_KEY_CURRENT_LEVEL, CurrentLevelIndex);
            }
        }
    }
}
