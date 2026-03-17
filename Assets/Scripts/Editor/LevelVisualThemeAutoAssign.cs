using TapAway.Core;
using UnityEditor;
using UnityEngine;

namespace TapAway.Editor
{
    public static class LevelVisualThemeAutoAssign
    {
        private const string PREFERRED_THEME_PATH = "Assets/Data/Levels/LevelVisualTheme.asset";

        [MenuItem("TapAway/Assign Default Visual Theme To All Levels")]
        public static void AssignToAllLevels()
        {
            LevelVisualTheme theme = ResolveDefaultTheme();
            if (theme == null)
            {
                Debug.LogWarning("Khong tim thay LevelVisualTheme de gan.");
                return;
            }

            string[] levelGuids = AssetDatabase.FindAssets("t:LevelData");
            int updated = 0;
            for (int i = 0; i < levelGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(levelGuids[i]);
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level == null)
                {
                    continue;
                }

                level.visualTheme = theme;
                if (theme.TrailBindingCount <= 0)
                {
                    level.trailBindingIndex = 0;
                }
                else
                {
                    level.trailBindingIndex = Mathf.Clamp(level.trailBindingIndex, 0, theme.TrailBindingCount - 1);
                }

                EditorUtility.SetDirty(level);
                updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Da gan LevelVisualTheme cho {updated} level.");
        }

        public static LevelVisualTheme ResolveDefaultTheme()
        {
            LevelVisualTheme preferred = AssetDatabase.LoadAssetAtPath<LevelVisualTheme>(PREFERRED_THEME_PATH);
            if (preferred != null)
            {
                return preferred;
            }

            string[] guids = AssetDatabase.FindAssets("t:LevelVisualTheme");
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<LevelVisualTheme>(path);
        }
    }
}
