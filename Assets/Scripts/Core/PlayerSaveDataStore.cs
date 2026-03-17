using System;
using System.IO;
using UnityEngine;

namespace TapAway.Core
{
    [Serializable]
    public class PlayerSaveData
    {
        public int coinBalance = 0;
        public int currentLevelIndex = 0;
        public int bombFreeCount = 3;
        public int addMovesFreeCount = 3;
        public bool soundEnabled = true;
        public bool musicEnabled = true;
        public bool vibrationEnabled = true;
    }

    public static class PlayerSaveDataStore
    {
        private const string SAVE_FILE_NAME = "player_data.json";
        private static PlayerSaveData _cachedData;

        public static string SavePath => GetSavePath();

        public static PlayerSaveData Data
        {
            get
            {
                if (_cachedData == null)
                {
                    _cachedData = LoadFromDisk();
                }

                return _cachedData;
            }
        }

        public static void Save()
        {
            if (_cachedData == null)
            {
                _cachedData = new PlayerSaveData();
            }

            string path = GetSavePath();
            string json = JsonUtility.ToJson(_cachedData, true);
            File.WriteAllText(path, json);
        }

        public static void ResetToDefaultAndSave()
        {
            _cachedData = new PlayerSaveData();
            Save();
        }

        public static void DeleteSaveFile()
        {
            string path = GetSavePath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            _cachedData = null;
        }

        public static void ReloadFromDisk()
        {
            _cachedData = LoadFromDisk();
        }

        private static PlayerSaveData LoadFromDisk()
        {
            string path = GetSavePath();
            if (!File.Exists(path))
            {
                return new PlayerSaveData();
            }

            try
            {
                string json = File.ReadAllText(path);
                PlayerSaveData loaded = JsonUtility.FromJson<PlayerSaveData>(json);
                if (loaded == null)
                {
                    return new PlayerSaveData();
                }

                loaded.coinBalance = Mathf.Max(0, loaded.coinBalance);
                loaded.currentLevelIndex = Mathf.Max(0, loaded.currentLevelIndex);
                loaded.bombFreeCount = Mathf.Max(0, loaded.bombFreeCount);
                loaded.addMovesFreeCount = Mathf.Max(0, loaded.addMovesFreeCount);
                return loaded;
            }
            catch (Exception)
            {
                return new PlayerSaveData();
            }
        }

        private static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        }
    }
}
