using TapAway.Core;
using UnityEditor;

namespace TapAway.Editor
{
    public class LevelDataThemePostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            LevelVisualTheme defaultTheme = LevelVisualThemeAutoAssign.ResolveDefaultTheme();
            if (defaultTheme == null)
            {
                return;
            }

            bool hasChanges = false;
            for (int i = 0; i < importedAssets.Length; i++)
            {
                string path = importedAssets[i];
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level == null || level.visualTheme != null)
                {
                    continue;
                }

                level.visualTheme = defaultTheme;
                level.trailBindingIndex = defaultTheme.TrailBindingCount > 0
                    ? level.trailBindingIndex % defaultTheme.TrailBindingCount
                    : 0;
                EditorUtility.SetDirty(level);
                hasChanges = true;
            }

            if (hasChanges)
            {
                AssetDatabase.SaveAssets();
            }
        }
    }
}
