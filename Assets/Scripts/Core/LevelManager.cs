using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TapAway.Core
{
    public class LevelManager : MonoBehaviour
    {
        // Danh sách level reference theo thứ tự chơi (Addressables-only).
        [SerializeField] private List<AssetReferenceT<LevelData>> _levelReferences = new List<AssetReferenceT<LevelData>>();
        // Index level hiện tại trong danh sách.
        [SerializeField, Min(0)] private int _currentLevelIndex;

        private readonly List<LevelData> _loadedLevels = new List<LevelData>();
        private readonly List<AsyncOperationHandle<LevelData>> _levelLoadHandles = new List<AsyncOperationHandle<LevelData>>();
        private bool _isReady;

        public int CurrentLevelIndex => _currentLevelIndex;
        public bool IsReady => _isReady;

        private void Awake()
        {
            _currentLevelIndex = Mathf.Max(_currentLevelIndex, PlayerProgressStore.CurrentLevelIndex);
            StartCoroutine(PreloadAddressableLevels());
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _levelLoadHandles.Count; i++)
            {
                AsyncOperationHandle<LevelData> handle = _levelLoadHandles[i];
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            _levelLoadHandles.Clear();
            _loadedLevels.Clear();
        }

        public LevelData GetCurrentLevel()
        {
            if (_loadedLevels.Count == 0)
            {
                return null;
            }

            _currentLevelIndex = Mathf.Clamp(_currentLevelIndex, 0, _loadedLevels.Count - 1);
            PlayerProgressStore.SetCurrentLevelIndex(_currentLevelIndex);
            return _loadedLevels[_currentLevelIndex];
        }

        public LevelData NextLevel()
        {
            if (_loadedLevels.Count == 0)
            {
                return null;
            }

            _currentLevelIndex = Mathf.Clamp(_currentLevelIndex + 1, 0, _loadedLevels.Count - 1);
            PlayerProgressStore.SetCurrentLevelIndex(_currentLevelIndex);
            return _loadedLevels[_currentLevelIndex];
        }

        public LevelData RestartCurrentLevel()
        {
            return GetCurrentLevel();
        }

        private System.Collections.IEnumerator PreloadAddressableLevels()
        {
            _loadedLevels.Clear();

            // Giữ đúng thứ tự list reference để thứ tự level runtime không đổi.
            for (int i = 0; i < _levelReferences.Count; i++)
            {
                AssetReferenceT<LevelData> levelReference = _levelReferences[i];
                if (levelReference == null || !levelReference.RuntimeKeyIsValid())
                {
                    Debug.LogWarning("[LevelManager] Missing Addressable LevelData reference.", this);
                    continue;
                }

                AsyncOperationHandle<LevelData> handle = levelReference.LoadAssetAsync<LevelData>();
                yield return handle;
                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    continue;
                }

                _levelLoadHandles.Add(handle);
                _loadedLevels.Add(handle.Result);
            }

            _isReady = true;
        }
    }
}
