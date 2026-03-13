using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TapAway.Core;
using TapAway.Data;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TapAway.Editor
{
    /// <summary>
    /// Unity Editor tool for procedural level generation.
    /// Menu: TapAway → Level Generator
    /// </summary>
    public class LevelGeneratorWindow : EditorWindow
    {
        // ── Constants ──────────────────────────────────────────
        private const int MAX_ATTEMPTS = 1000;
        private const string LEVEL_OUTPUT_PATH = "Assets/Data/Levels";

        // ── Serialized (window state) ──────────────────────────
        private DifficultyConfig _difficultyConfig;
        private int _levelIndexFrom = 1;
        private int _levelIndexTo = 10;
        private bool _useLevelRange = true;
        private int _singleLevelIndex = 1;

        private static readonly HashSet<string> _generatedHashes = new HashSet<string>();

        // ── Menu Item ─────────────────────────────────────────

        [MenuItem("TapAway/Level Generator")]
        public static void ShowWindow()
        {
            GetWindow<LevelGeneratorWindow>("Level Generator");
        }

        // ── GUI ───────────────────────────────────────────────

        private void OnGUI()
        {
            GUILayout.Label("Tap Away — Level Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _difficultyConfig = (DifficultyConfig)EditorGUILayout.ObjectField(
                "Difficulty Config", _difficultyConfig, typeof(DifficultyConfig), false);

            EditorGUILayout.Space();
            _useLevelRange = EditorGUILayout.Toggle("Generate Range", _useLevelRange);

            if (_useLevelRange)
            {
                _levelIndexFrom = EditorGUILayout.IntField("Level From", _levelIndexFrom);
                _levelIndexTo   = EditorGUILayout.IntField("Level To",   _levelIndexTo);
            }
            else
            {
                _singleLevelIndex = EditorGUILayout.IntField("Level Index", _singleLevelIndex);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Clear Duplicate Cache"))
                _generatedHashes.Clear();

            EditorGUILayout.HelpBox(
                $"Duplicate cache: {_generatedHashes.Count} hashes stored.",
                MessageType.Info);

            EditorGUILayout.Space();

            GUI.enabled = _difficultyConfig != null;

            if (GUILayout.Button("Generate Level(s)"))
            {
                if (_useLevelRange)
                {
                    for (int i = _levelIndexFrom; i <= _levelIndexTo; i++)
                        GenerateLevel(i);
                }
                else
                {
                    GenerateLevel(_singleLevelIndex);
                }
            }

            GUI.enabled = true;
        }

        // ── Generator Pipeline ────────────────────────────────

        private void GenerateLevel(int levelIndex)
        {
            var range = _difficultyConfig.GetRange(levelIndex);
            if (range == null)
            {
                Debug.LogError($"[LevelGenerator] No DifficultyRange found for level {levelIndex}");
                return;
            }

            LevelData result = null;
            int attempt = 0;

            while (attempt < MAX_ATTEMPTS)
            {
                attempt++;
                var candidate = TryGenerateCandidate(levelIndex, range);
                if (candidate == null) continue;

                string hash = HashLevel(candidate);
                if (_generatedHashes.Contains(hash))
                    continue; // Duplicate — does NOT count toward limit

                _generatedHashes.Add(hash);
                result = candidate;
                break;
            }

            if (result == null)
            {
                Debug.LogWarning($"[LevelGenerator] Failed after {MAX_ATTEMPTS} attempts for level {levelIndex}. Using fallback.");
                result = CreateFallbackLevel(levelIndex, range);
            }

            SaveLevelData(result);
        }

        private LevelData TryGenerateCandidate(int levelIndex, DifficultyRange range)
        {
            int gridSize = range.gridSize;
            int totalCells = gridSize * gridSize;

            float density = Random.Range(range.blockDensityMin, range.blockDensityMax);
            int blockCount   = Mathf.RoundToInt(density * totalCells);
            int gearCount    = Mathf.RoundToInt(range.gearRatio    * blockCount);
            int rotatorCount = Mathf.RoundToInt(range.rotatorRatio * blockCount);
            int normalCount  = blockCount - gearCount - rotatorCount;

            if (normalCount <= 0) return null;

            var blocks = new List<Block>();
            var occupied = new HashSet<(int, int)>();
            int id = 0;

            // Place Gears
            PlaceGears(gridSize, gearCount, blocks, occupied, ref id);

            // Place Rotators
            PlaceRotators(gridSize, rotatorCount, blocks, occupied, ref id);

            // Place Normal blocks
            PlaceNormalBlocks(gridSize, normalCount, blocks, occupied, ref id);

            // Validate: at least 50% blocked
            var grid = new GridSystem(gridSize, blocks);
            int freeCount = CountFreeBlocks(grid);
            if (freeCount > range.maxFreeBlocksAtStart) return null;

            int lockedCount = normalCount - freeCount;
            if (normalCount > 0 && (float)lockedCount / normalCount < 0.5f) return null;

            // Solve
            var solver = new LevelSolver(grid);
            var solverResult = solver.Solve();

            if (!solverResult.IsSolvable) return null;

            // Chain depth check
            if (solverResult.MaxChainDepth < range.minChainDepth) return null;
            if (range.maxChainDepth > 0 && solverResult.MaxChainDepth > range.maxChainDepth) return null;

            // Score check
            float score = DifficultyScorer.Calculate(grid, solverResult);
            if (score < range.targetScoreMin || score > range.targetScoreMax) return null;

            // Build LevelData
            var levelData = ScriptableObject.CreateInstance<LevelData>();
            levelData.levelIndex = levelIndex;
            levelData.gridSize   = gridSize;
            levelData.movesLimit = range.movesBuffer == 0
                ? 0
                : solverResult.OptimalMoves + range.movesBuffer;

            foreach (var b in blocks)
            {
                levelData.blocks.Add(new BlockData
                {
                    x = b.X, y = b.Y,
                    cellType  = b.CellType,
                    direction = b.Direction,
                });
            }

            return levelData;
        }

        // ── Placement Helpers ─────────────────────────────────

        private void PlaceGears(int gridSize, int count, List<Block> blocks,
            HashSet<(int, int)> occupied, ref int id)
        {
            int center = gridSize / 2;
            int placed = 0;
            int tries = 0;
            int maxTries = count * 20;

            while (placed < count && tries++ < maxTries)
            {
                // Bias toward center, avoid edges
                int margin = Mathf.Max(1, gridSize / 4);
                int x = Random.Range(margin, gridSize - margin);
                int y = Random.Range(margin, gridSize - margin);

                if (occupied.Contains((x, y))) continue;

                occupied.Add((x, y));
                blocks.Add(new Block(id++, x, y, CellType.Gear, BlockDirection.Up));
                placed++;
            }
        }

        private void PlaceRotators(int gridSize, int count, List<Block> blocks,
            HashSet<(int, int)> occupied, ref int id)
        {
            int placed = 0;
            int tries = 0;
            int maxTries = count * 20;

            while (placed < count && tries++ < maxTries)
            {
                // Avoid corners (margin of 1)
                int x = Random.Range(1, gridSize - 1);
                int y = Random.Range(1, gridSize - 1);

                if (occupied.Contains((x, y))) continue;

                occupied.Add((x, y));
                blocks.Add(new Block(id++, x, y, CellType.Rotator, BlockDirection.Up));
                placed++;
            }
        }

        private void PlaceNormalBlocks(int gridSize, int count, List<Block> blocks,
            HashSet<(int, int)> occupied, ref int id)
        {
            var freeCells = new List<(int x, int y)>();
            for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
            {
                if (!occupied.Contains((x, y)))
                    freeCells.Add((x, y));
            }

            Shuffle(freeCells);
            int placed = 0;

            foreach (var (x, y) in freeCells)
            {
                if (placed >= count) break;
                var dir = ChooseDirectionByPosition(x, y, gridSize);
                blocks.Add(new Block(id++, x, y, CellType.Normal, dir));
                occupied.Add((x, y));
                placed++;
            }
        }

        /// <summary>Weight direction based on proximity to border.</summary>
        private BlockDirection ChooseDirectionByPosition(int x, int y, int gridSize)
        {
            float left  = x;
            float right = gridSize - 1 - x;
            float down  = y;
            float up    = gridSize - 1 - y;

            // Bias: closer to edge → higher weight for that direction
            float wLeft  = 1f / (left  + 1f);
            float wRight = 1f / (right + 1f);
            float wDown  = 1f / (down  + 1f);
            float wUp    = 1f / (up    + 1f);

            float total = wLeft + wRight + wDown + wUp;
            float r = Random.value * total;

            if (r < wLeft)  return BlockDirection.Left;
            r -= wLeft;
            if (r < wRight) return BlockDirection.Right;
            r -= wRight;
            if (r < wDown)  return BlockDirection.Down;
            return BlockDirection.Up;
        }

        private int CountFreeBlocks(GridSystem grid)
        {
            int count = 0;
            foreach (var b in grid.Blocks)
            {
                if (b.CellType != CellType.Normal) continue;
                if (IsBlockFree(grid, b)) count++;
            }
            return count;
        }

        private bool IsBlockFree(GridSystem grid, Block block)
        {
            GridSystem.DirectionToStep(block.Direction, out int dx, out int dy);
            int nx = block.X + dx;
            int ny = block.Y + dy;
            while (grid.IsInsideGrid(nx, ny))
            {
                if (grid.GetBlock(nx, ny) != null) return false;
                nx += dx;
                ny += dy;
            }
            return true;
        }

        // ── Fallback Level ────────────────────────────────────

        private LevelData CreateFallbackLevel(int levelIndex, DifficultyRange range)
        {
            int gs = range.gridSize;
            var levelData = ScriptableObject.CreateInstance<LevelData>();
            levelData.levelIndex = levelIndex;
            levelData.gridSize   = gs;
            levelData.movesLimit = 0;
            int half = gs / 2;

            // Simple cross pattern: blocks pointing outward from center
            var positions = new (int x, int y, BlockDirection dir)[]
            {
                (half - 1, half, BlockDirection.Left),
                (half + 1, half, BlockDirection.Right),
                (half, half - 1, BlockDirection.Down),
                (half, half + 1, BlockDirection.Up),
            };

            foreach (var (x, y, dir) in positions)
            {
                if (x >= 0 && x < gs && y >= 0 && y < gs)
                {
                    levelData.blocks.Add(new BlockData { x = x, y = y, cellType = CellType.Normal, direction = dir });
                }
            }
            return levelData;
        }

        // ── Save ──────────────────────────────────────────────

        private void SaveLevelData(LevelData levelData)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(LEVEL_OUTPUT_PATH))
                AssetDatabase.CreateFolder("Assets/Data", "Levels");

            string path = $"{LEVEL_OUTPUT_PATH}/Level_{levelData.levelIndex:D3}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            AssetDatabase.CreateAsset(levelData, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LevelGenerator] Saved: {path} (gridSize={levelData.gridSize}, moves={levelData.movesLimit})");
        }

        // ── Hash ──────────────────────────────────────────────

        private string HashLevel(LevelData level)
        {
            var sb = new StringBuilder();
            sb.Append(level.gridSize);

            var normals = level.blocks.Where(b => b.cellType == CellType.Normal)
                .OrderBy(b => b.x * 100 + b.y);
            foreach (var b in normals)
                sb.Append($"|{b.x},{b.y},{b.direction}");

            var gears = level.blocks.Where(b => b.cellType == CellType.Gear)
                .OrderBy(b => b.x * 100 + b.y);
            foreach (var b in gears)
                sb.Append($"|G{b.x},{b.y}");

            var rotators = level.blocks.Where(b => b.cellType == CellType.Rotator)
                .OrderBy(b => b.x * 100 + b.y);
            foreach (var b in rotators)
                sb.Append($"|R{b.x},{b.y}");

            return sb.ToString();
        }

        // ── Utility ───────────────────────────────────────────

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
