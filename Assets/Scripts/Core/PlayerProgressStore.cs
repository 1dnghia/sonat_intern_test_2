using UnityEngine;

namespace TapAway.Core
{
    public static class PlayerProgressStore
    {
        private const int DEFAULT_FREE_USE_COUNT = 3;

        public static int CurrentLevelIndex
        {
            get
            {
                return Mathf.Max(0, PlayerSaveDataStore.Data.currentLevelIndex);
            }
        }

        public static int BombFreeCount
        {
            get
            {
                return Mathf.Max(0, PlayerSaveDataStore.Data.bombFreeCount);
            }
        }

        public static int AddMovesFreeCount
        {
            get
            {
                return Mathf.Max(0, PlayerSaveDataStore.Data.addMovesFreeCount);
            }
        }

        public static void SetCurrentLevelIndex(int levelIndex)
        {
            PlayerSaveDataStore.Data.currentLevelIndex = Mathf.Max(0, levelIndex);
            PlayerSaveDataStore.Save();
        }

        public static void ConsumeBombFreeCount()
        {
            int remaining = Mathf.Max(0, BombFreeCount - 1);
            PlayerSaveDataStore.Data.bombFreeCount = remaining;
            PlayerSaveDataStore.Save();
        }

        public static void ConsumeAddMovesFreeCount()
        {
            int remaining = Mathf.Max(0, AddMovesFreeCount - 1);
            PlayerSaveDataStore.Data.addMovesFreeCount = remaining;
            PlayerSaveDataStore.Save();
        }
    }
}
