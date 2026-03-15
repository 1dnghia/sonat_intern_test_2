using System;
using UnityEngine;

namespace TapAway
{
    // Ví tiền dùng chung cho HUD và popup; lưu qua PlayerPrefs để giữ coin giữa các lần load scene.
    public static class CoinWallet
    {
        private const string COIN_KEY = "tapaway_coin_balance";
        private static int _cachedBalance = int.MinValue;

        public static event Action<int> OnCoinChanged;

        public static int Balance
        {
            get
            {
                if (_cachedBalance == int.MinValue)
                {
                    _cachedBalance = Mathf.Max(0, PlayerPrefs.GetInt(COIN_KEY, 0));
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
            PlayerPrefs.SetInt(COIN_KEY, _cachedBalance);
            PlayerPrefs.Save();
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
    }
}
