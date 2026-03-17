using System.IO;
using TapAway.Core;
using UnityEditor;
using UnityEngine;

namespace TapAway.Editor
{
    public static class SaveDataDebugMenu
    {
        [MenuItem("TapAway/Debug/Save Data/Open Save Folder")]
        public static void OpenSaveFolder()
        {
            string folder = Application.persistentDataPath;
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            EditorUtility.RevealInFinder(folder);
        }

        [MenuItem("TapAway/Debug/Save Data/Reset To Default")]
        public static void ResetToDefault()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Reset Save Data",
                "Reset toàn bộ dữ liệu về mặc định và lưu lại file JSON mới?",
                "Reset",
                "Cancel");

            if (!confirm)
            {
                return;
            }

            PlayerSaveDataStore.ResetToDefaultAndSave();
            CoinWallet.ReloadFromSave();
            Debug.Log("[SaveDataDebugMenu] Save data reset to default: " + PlayerSaveDataStore.SavePath);
        }

        [MenuItem("TapAway/Debug/Save Data/Delete Save File")]
        public static void DeleteSaveFile()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Delete Save File",
                "Xóa hẳn file player_data.json? Dữ liệu sẽ tạo lại mặc định khi chạy game.",
                "Delete",
                "Cancel");

            if (!confirm)
            {
                return;
            }

            PlayerSaveDataStore.DeleteSaveFile();
            PlayerSaveDataStore.ReloadFromDisk();
            CoinWallet.ReloadFromSave();
            Debug.Log("[SaveDataDebugMenu] Save file deleted. Current path: " + PlayerSaveDataStore.SavePath);
        }
    }
}
