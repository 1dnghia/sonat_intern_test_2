using System.IO;
using TapAway.Core;
using UnityEditor;
using UnityEngine;

namespace TapAway.Editor
{
    public static class HandCraftLevelCreator
    {
        [MenuItem("TapAway/Create Hand-Craft Levels")]
        public static void CreateDefaultLevels()
        {
            const string dir = "Assets/Data/Levels";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            for (int i = 1; i <= 8; i++)
            {
                string path = $"{dir}/Level_{i:000}.asset";
                LevelData existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (existing != null)
                {
                    continue;
                }

                LevelData level = ScriptableObject.CreateInstance<LevelData>();
                level.width = 6;
                level.height = 6;
                level.moveLimit = 20;
                level.CreateGridTemplate(false);
                level.visualTheme = LevelVisualThemeAutoAssign.ResolveDefaultTheme();
                level.trailBindingIndex = 0;

                AssetDatabase.CreateAsset(level, path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created hand-craft levels Level_001..Level_008");
        }
    }
}
