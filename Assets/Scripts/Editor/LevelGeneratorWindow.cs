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
        private static readonly string[] GRID_SIZE_OPTIONS =
        {
            "2x2",
            "3x3",
            "4x4",
            "5x5",
            "6x6",
            "7x7",
            "8x8",
            "9x9",
        };

        private static readonly Vector2Int[] ROTATOR_LINK_OFFSETS =
        {
            new Vector2Int(-1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 1),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
        };

        private DifficultyConfig _config;
        private LevelVisualTheme _visualTheme;
        private bool _randomTrailBinding = true;
        private int _fixedTrailBindingIndex;
        private int _count = 1;
        private int _startIndex = 100;
        private bool _useFixedGridSize;
        private int _fixedGridSizeIndex = 4;

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
            _useFixedGridSize = EditorGUILayout.Toggle("Use Fixed Grid Size", _useFixedGridSize);
            if (_useFixedGridSize)
            {
                _fixedGridSizeIndex = EditorGUILayout.Popup("Fixed Grid Size", _fixedGridSizeIndex, GRID_SIZE_OPTIONS);
                _fixedGridSizeIndex = Mathf.Clamp(_fixedGridSizeIndex, 0, GRID_SIZE_OPTIONS.Length - 1);
            }

            GUI.enabled = _config != null;
            if (GUILayout.Button("Generate"))
            {
                Generate();
            }
            GUI.enabled = true;
        }

        private void Generate()
        {
            if (_visualTheme == null)
            {
                _visualTheme = LevelVisualThemeAutoAssign.ResolveDefaultTheme();
            }

            const string dir = "Assets/Data/Levels";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            for (int i = 0; i < _count; i++)
            {
                int size;
                if (_useFixedGridSize)
                {
                    size = _fixedGridSizeIndex + 2;
                }
                else
                {
                    size = Random.Range(_config.minGridSize, _config.maxGridSize + 1);
                }

                int cellCount = size * size;
                float density = Random.Range(_config.minBlockDensity, _config.maxBlockDensity);
                int blockCount = Mathf.Clamp(Mathf.RoundToInt(cellCount * density), 1, cellCount);
                int baseMoves = Mathf.Max(1, Mathf.RoundToInt(blockCount * 0.8f));
                int move = baseMoves + Random.Range(_config.minMovesBuffer, _config.maxMovesBuffer + 1);

                LevelData level = ScriptableObject.CreateInstance<LevelData>();
                level.width = size;
                level.height = size;
                level.moveLimit = move;
                level.blocks = GenerateBlocks(size, blockCount, _config.gearRatio, _config.rotatorRatio);
                level.NormalizeToMapBounds();
                level.visualTheme = _visualTheme;
                level.trailBindingIndex = ResolveTrailBindingIndex(_visualTheme, _randomTrailBinding, _fixedTrailBindingIndex);

                string path = $"{dir}/Level_{_startIndex + i:000}.asset";
                AssetDatabase.CreateAsset(level, path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated levels successfully.");
        }

        private static List<BlockData> GenerateBlocks(int size, int count, float gearRatio, float rotatorRatio)
        {
            List<BlockData> result = new List<BlockData>(count);
            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

            int id = 1;
            int safeLimit = size * size * 2;
            int gearQuota = Mathf.Clamp(Mathf.RoundToInt(count * Mathf.Clamp01(gearRatio)), 0, count);
            int rotatorQuota = Mathf.Clamp(Mathf.RoundToInt(count * Mathf.Clamp01(rotatorRatio)), 0, count - gearQuota);
            int normalQuota = Mathf.Max(0, count - gearQuota - rotatorQuota);
            List<CellType> typePool = BuildTypePool(normalQuota, gearQuota, rotatorQuota);

            for (int i = 0; i < count && safeLimit > 0; i++)
            {
                safeLimit--;
                Vector2Int pos = new Vector2Int(Random.Range(0, size), Random.Range(0, size));
                if (!occupied.Add(pos))
                {
                    i--;
                    continue;
                }

                CellType type = typePool[i];

                result.Add(new BlockData
                {
                    id = id++,
                    position = pos,
                    direction = (BlockDirection)Random.Range(0, 4),
                    cellType = type
                });
            }

            for (int i = 0; i < result.Count; i++)
            {
                if (result[i].cellType != CellType.Rotator)
                {
                    if (result[i].rotatorLinkedNormals != null)
                    {
                        result[i].rotatorLinkedNormals.Clear();
                    }

                    continue;
                }

                if (HasAdjacentNormal(result, result[i].position))
                {
                    FillRotatorLinks(result, i);
                    continue;
                }

                if (!TryPromoteNeighborToNormal(result, result[i].position))
                {
                    BlockData data = result[i];
                    data.cellType = CellType.Normal;
                    if (data.rotatorLinkedNormals != null)
                    {
                        data.rotatorLinkedNormals.Clear();
                    }
                    result[i] = data;
                    continue;
                }

                FillRotatorLinks(result, i);
            }

            EnforceUniqueRotatorLinks(result);

            return result;
        }

        private static void EnforceUniqueRotatorLinks(List<BlockData> blocks)
        {
            if (blocks == null || blocks.Count == 0)
            {
                return;
            }

            HashSet<Vector2Int> claimedNormals = new HashSet<Vector2Int>();
            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData rotator = blocks[i];
                if (rotator.cellType != CellType.Rotator)
                {
                    continue;
                }

                if (rotator.rotatorLinkedNormals == null)
                {
                    rotator.rotatorLinkedNormals = new List<Vector2Int>();
                    blocks[i] = rotator;
                    continue;
                }

                List<Vector2Int> uniqueLinks = new List<Vector2Int>();
                for (int j = 0; j < rotator.rotatorLinkedNormals.Count; j++)
                {
                    Vector2Int linkedPos = rotator.rotatorLinkedNormals[j];
                    if (!ContainsNormalAt(blocks, linkedPos))
                    {
                        continue;
                    }

                    if (!claimedNormals.Add(linkedPos))
                    {
                        continue;
                    }

                    uniqueLinks.Add(linkedPos);
                }

                rotator.rotatorLinkedNormals = uniqueLinks;
                blocks[i] = rotator;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData rotator = blocks[i];
                if (rotator.cellType != CellType.Rotator)
                {
                    continue;
                }

                if (rotator.rotatorLinkedNormals != null && rotator.rotatorLinkedNormals.Count > 0)
                {
                    continue;
                }

                BlockData converted = rotator;
                converted.cellType = CellType.Normal;
                if (converted.rotatorLinkedNormals != null)
                {
                    converted.rotatorLinkedNormals.Clear();
                }

                blocks[i] = converted;
            }
        }

        private static List<CellType> BuildTypePool(int normalCount, int gearCount, int rotatorCount)
        {
            int total = Mathf.Max(1, normalCount + gearCount + rotatorCount);
            List<CellType> pool = new List<CellType>(total);

            for (int i = 0; i < normalCount; i++)
            {
                pool.Add(CellType.Normal);
            }

            for (int i = 0; i < gearCount; i++)
            {
                pool.Add(CellType.Gear);
            }

            for (int i = 0; i < rotatorCount; i++)
            {
                pool.Add(CellType.Rotator);
            }

            if (pool.Count == 0)
            {
                pool.Add(CellType.Normal);
            }

            for (int i = 0; i < pool.Count; i++)
            {
                int swapIndex = Random.Range(i, pool.Count);
                CellType temp = pool[i];
                pool[i] = pool[swapIndex];
                pool[swapIndex] = temp;
            }

            return pool;
        }

        private static bool HasAdjacentNormal(List<BlockData> blocks, Vector2Int center)
        {
            if (blocks == null)
            {
                return false;
            }

            for (int i = 0; i < ROTATOR_LINK_OFFSETS.Length; i++)
            {
                Vector2Int target = center + ROTATOR_LINK_OFFSETS[i];
                for (int j = 0; j < blocks.Count; j++)
                {
                    if (blocks[j].position == target && blocks[j].cellType == CellType.Normal)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ContainsNormalAt(List<BlockData> blocks, Vector2Int position)
        {
            if (blocks == null)
            {
                return false;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].position == position && blocks[i].cellType == CellType.Normal)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryPromoteNeighborToNormal(List<BlockData> blocks, Vector2Int center)
        {
            if (blocks == null)
            {
                return false;
            }

            for (int i = 0; i < ROTATOR_LINK_OFFSETS.Length; i++)
            {
                Vector2Int target = center + ROTATOR_LINK_OFFSETS[i];
                for (int j = 0; j < blocks.Count; j++)
                {
                    if (blocks[j].position != target)
                    {
                        continue;
                    }

                    if (blocks[j].cellType == CellType.Gear || blocks[j].cellType == CellType.Rotator)
                    {
                        BlockData data = blocks[j];
                        data.cellType = CellType.Normal;
                        blocks[j] = data;
                        return true;
                    }
                }
            }

            return false;
        }

        private static void FillRotatorLinks(List<BlockData> blocks, int rotatorIndex)
        {
            if (blocks == null || rotatorIndex < 0 || rotatorIndex >= blocks.Count)
            {
                return;
            }

            BlockData rotator = blocks[rotatorIndex];
            if (rotator.rotatorLinkedNormals == null)
            {
                rotator.rotatorLinkedNormals = new List<Vector2Int>();
            }
            else
            {
                rotator.rotatorLinkedNormals.Clear();
            }

            for (int i = 0; i < ROTATOR_LINK_OFFSETS.Length; i++)
            {
                Vector2Int candidate = rotator.position + ROTATOR_LINK_OFFSETS[i];
                if (ContainsNormalAt(blocks, candidate))
                {
                    rotator.rotatorLinkedNormals.Add(candidate);
                }
            }

            blocks[rotatorIndex] = rotator;
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
