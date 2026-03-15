using System.Collections.Generic;
using UnityEngine;

namespace TapAway.Core
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private List<LevelData> _levels = new List<LevelData>();
        [SerializeField, Min(0)] private int _currentLevelIndex;

        public int CurrentLevelIndex => _currentLevelIndex;

        public LevelData GetCurrentLevel()
        {
            if (_levels.Count == 0)
            {
                return null;
            }

            _currentLevelIndex = Mathf.Clamp(_currentLevelIndex, 0, _levels.Count - 1);
            return _levels[_currentLevelIndex];
        }

        public LevelData NextLevel()
        {
            if (_levels.Count == 0)
            {
                return null;
            }

            _currentLevelIndex = Mathf.Clamp(_currentLevelIndex + 1, 0, _levels.Count - 1);
            return _levels[_currentLevelIndex];
        }

        public LevelData RestartCurrentLevel()
        {
            return GetCurrentLevel();
        }
    }
}
