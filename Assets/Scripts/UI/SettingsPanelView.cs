using TapAway.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapAway
{
    public class SettingsPanelView : MonoBehaviour
    {
        // Toggle bật/tắt âm thanh SFX.
        [SerializeField] private Toggle _soundToggle;
        // Toggle bật/tắt nhạc nền BGM.
        [SerializeField] private Toggle _musicToggle;
        // Toggle bật/tắt rung.
        [SerializeField] private Toggle _vibrationToggle;

        private bool _isSyncingFromData;

        private void OnEnable()
        {
            if (_soundToggle != null)
            {
                _soundToggle.onValueChanged.AddListener(OnSoundChanged);
            }

            if (_musicToggle != null)
            {
                _musicToggle.onValueChanged.AddListener(OnMusicChanged);
            }

            if (_vibrationToggle != null)
            {
                _vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
            }

            SyncFromStore();
        }

        private void OnDisable()
        {
            if (_soundToggle != null)
            {
                _soundToggle.onValueChanged.RemoveListener(OnSoundChanged);
            }

            if (_musicToggle != null)
            {
                _musicToggle.onValueChanged.RemoveListener(OnMusicChanged);
            }

            if (_vibrationToggle != null)
            {
                _vibrationToggle.onValueChanged.RemoveListener(OnVibrationChanged);
            }
        }

        private void SyncFromStore()
        {
            _isSyncingFromData = true;

            if (_soundToggle != null)
            {
                _soundToggle.isOn = GameSettingsStore.IsSoundEnabled;
            }

            if (_musicToggle != null)
            {
                _musicToggle.isOn = GameSettingsStore.IsMusicEnabled;
            }

            if (_vibrationToggle != null)
            {
                _vibrationToggle.isOn = GameSettingsStore.IsVibrationEnabled;
            }

            _isSyncingFromData = false;
        }

        private void OnSoundChanged(bool isOn)
        {
            if (_isSyncingFromData)
            {
                return;
            }

            GameSettingsStore.SetSoundEnabled(isOn);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.RefreshMuteState();
            }
        }

        private void OnMusicChanged(bool isOn)
        {
            if (_isSyncingFromData)
            {
                return;
            }

            GameSettingsStore.SetMusicEnabled(isOn);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.RefreshMuteState();
            }
        }

        private void OnVibrationChanged(bool isOn)
        {
            if (_isSyncingFromData)
            {
                return;
            }

            GameSettingsStore.SetVibrationEnabled(isOn);
        }
    }
}
