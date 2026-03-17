using UnityEngine;

namespace TapAway.Core
{
    public static class GameSettingsStore
    {
        public static bool IsSoundEnabled
        {
            get
            {
                return PlayerSaveDataStore.Data.soundEnabled;
            }
        }

        public static bool IsMusicEnabled
        {
            get
            {
                return PlayerSaveDataStore.Data.musicEnabled;
            }
        }

        public static bool IsVibrationEnabled
        {
            get
            {
                return PlayerSaveDataStore.Data.vibrationEnabled;
            }
        }

        public static void SetSoundEnabled(bool enabled)
        {
            PlayerSaveDataStore.Data.soundEnabled = enabled;
            PlayerSaveDataStore.Save();
        }

        public static void SetMusicEnabled(bool enabled)
        {
            PlayerSaveDataStore.Data.musicEnabled = enabled;
            PlayerSaveDataStore.Save();
        }

        public static void SetVibrationEnabled(bool enabled)
        {
            PlayerSaveDataStore.Data.vibrationEnabled = enabled;
            PlayerSaveDataStore.Save();
        }
    }
}
