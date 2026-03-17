using System;
using UnityEngine;
using TapAway.Core;

namespace TapAway
{
    // Ví tiền dùng chung cho HUD và popup; lưu qua file JSON player_data.json.
    public static class CoinWallet
    {
        private static int _cachedBalance = -1;

        public static event Action<int> OnCoinChanged;

        public static int Balance
        {
            get
            {
                if (_cachedBalance < 0)
                {
                    _cachedBalance = Mathf.Max(0, PlayerSaveDataStore.Data.coinBalance);
                }

                return _cachedBalance;
            }
        }

        public static void Set(int value)
        {
            int safeValue = Mathf.Max(0, value);
            if (safeValue == Balance)
            {
                return;
            }

            _cachedBalance = safeValue;
            PlayerSaveDataStore.Data.coinBalance = _cachedBalance;
            PlayerSaveDataStore.Save();
            OnCoinChanged?.Invoke(_cachedBalance);
        }

        public static void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Set(Balance + amount);
        }

        public static bool TrySpend(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (Balance < amount)
            {
                return false;
            }

            Set(Balance - amount);
            return true;
        }

        public static void ReloadFromSave()
        {
            _cachedBalance = Mathf.Max(0, PlayerSaveDataStore.Data.coinBalance);
            OnCoinChanged?.Invoke(_cachedBalance);
        }
    }
}
