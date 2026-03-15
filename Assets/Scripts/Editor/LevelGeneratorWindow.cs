using System.Collections.Generic;
using System.IO;
using TapAway.Core;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TapAway.Editor
{
    public class LevelGeneratorWindow : EditorWindow
    {
        private DifficultyConfig _config;
        private LevelVisualTheme _visualTheme;
        private bool _randomTrailBinding = true;
        private int _fixedTrailBindingIndex;
        private int _count = 1;
        private int _startIndex = 100;

        [MenuItem("TapAway/Level Generator")]
        public static void Open()
        {
            GetWindow<LevelGeneratorWindow>("TapAway Generator");
        }

        private void OnGUI()
        {
            _config = (DifficultyConfig)EditorGUILayout.ObjectField("Difficulty Config", _config, typeof(DifficultyConfig), false);
            _visualTheme = (LevelVisualTheme)EditorGUILayout.ObjectField("Level Visual Theme", _visualTheme, typeof(LevelVisualTheme), false);
            _randomTrailBinding = EditorGUILayout.Toggle("Random Trail Binding", _randomTrailBinding);
            if (!_randomTrailBinding)
            {
                _fixedTrailBindingIndex = EditorGUILayout.IntField("Fixed Trail Binding Index", _fixedTrailBindingIndex);
            }
            _count = EditorGUILayout.IntSlider("Generate Count", _count, 1, 20);
            _startIndex = EditorGUILayout.IntField("Start Index", _startIndex);

            GUI.enabled = _config != null;
            if (GUILayout.Button("Generate"))
            {
                Generate();
            }
            GUI.enabled = true;
        }

        private void Generate()
        {
            const string dir = "Assets/Data/Levels";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            for (int i = 0; i < _count; i++)
            {
                int w = Random.Range(_config.minWidth, _config.maxWidth + 1);
                int h = Random.Range(_config.minHeight, _config.maxHeight + 1);
                int move = Random.Range(_config.minMoves, _config.maxMoves + 1);
                int blockCount = Random.Range(_config.minBlocks, _config.maxBlocks + 1);

                LevelData level = ScriptableObject.CreateInstance<LevelData>();
                level.width = w;
                level.height = h;
                level.moveLimit = move;
                level.blocks = GenerateBlocks(w, h, blockCount);
                level.visualTheme = _visualTheme;
                level.trailBindingIndex = ResolveTrailBindingIndex(_visualTheme, _randomTrailBinding, _fixedTrailBindingIndex);

                string path = $"{dir}/Level_{_startIndex + i:000}.asset";
                AssetDatabase.CreateAsset(level, path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated levels successfully.");
        }

        private static List<BlockData> GenerateBlocks(int width, int height, int count)
        {
            List<BlockData> result = new List<BlockData>(count);
            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

            int id = 1;
            int safeLimit = width * height * 2;

            for (int i = 0; i < count && safeLimit > 0; i++)
            {
                safeLimit--;
                Vector2Int pos = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
                if (!occupied.Add(pos))
                {
                    i--;
                    continue;
                }

                CellType type = CellType.Normal;
                float roll = Random.value;
                if (roll > 0.85f)
                {
                    type = CellType.Gear;
                }
                else if (roll > 0.65f)
                {
                    type = CellType.Rotator;
                }

                result.Add(new BlockData
                {
                    id = id++,
                    position = pos,
                    direction = (BlockDirection)Random.Range(0, 4),
                    cellType = type
                });
            }

            return result;
        }

        private static int ResolveTrailBindingIndex(LevelVisualTheme visualTheme, bool randomTrailBinding, int fixedTrailBindingIndex)
        {
            if (visualTheme == null || visualTheme.TrailBindingCount <= 0)
            {
                return 0;
            }

            if (randomTrailBinding)
            {
                return Random.Range(0, visualTheme.TrailBindingCount);
            }

            return Mathf.Clamp(fixedTrailBindingIndex, 0, visualTheme.TrailBindingCount - 1);
        }
    }
}
