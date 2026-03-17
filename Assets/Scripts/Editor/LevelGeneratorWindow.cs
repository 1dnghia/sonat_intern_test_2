using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TapAway.Core;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TapAway.Editor
{
    public class LevelGeneratorWindow : EditorWindow
    {
        private const int MAX_GENERATION_ATTEMPTS_PER_LEVEL = 120;
        private const int SOLVER_NODE_LIMIT = 4000;

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
                if (_config.allowGear && _config.allowRotator)
                {
                    blockCount = Mathf.Clamp(Mathf.Max(2, blockCount), 1, cellCount);
                }

                List<BlockData> generatedBlocks = null;
                bool generatedValid = false;
                for (int attempt = 0; attempt < MAX_GENERATION_ATTEMPTS_PER_LEVEL; attempt++)
                {
                    generatedBlocks = GenerateBlocks(
                        size,
                        blockCount,
                        _config.gearRatio,
                        _config.rotatorRatio,
                        _config.allowGear,
                        _config.allowRotator);

                    SanitizeNormalDirections(generatedBlocks, size);

                    if (!HasRequiredSpecialBlocks(generatedBlocks, _config.allowGear, _config.allowRotator))
                    {
                        continue;
                    }

                    if (!IsValidGeneratedLayout(generatedBlocks, size))
                    {
                        continue;
                    }

                    generatedValid = true;
                    break;
                }

                if (!generatedValid)
                {
                    Debug.LogWarning($"Generator khong tim thay layout hop le sau {MAX_GENERATION_ATTEMPTS_PER_LEVEL} lan thu cho level {_startIndex + i:000}.");
                    generatedBlocks = GenerateBlocks(
                        size,
                        blockCount,
                        _config.gearRatio,
                        _config.rotatorRatio,
                        _config.allowGear,
                        _config.allowRotator);
                    SanitizeNormalDirections(generatedBlocks, size);
                    EnsureRequiredSpecialBlocks(generatedBlocks, size, _config.allowGear, _config.allowRotator);
                }

                LevelData level = ScriptableObject.CreateInstance<LevelData>();
                level.width = size;
                level.height = size;
                level.blocks = generatedBlocks;
                level.moveLimit = EstimateMoveLimit(level.blocks, size, _config.minMovesBuffer, _config.maxMovesBuffer);
                level.NormalizeToMapBounds();
                level.visualTheme = _visualTheme;
                level.trailBindingIndex = ResolveTrailBindingIndex(_visualTheme, _randomTrailBinding, _fixedTrailBindingIndex);

                int generatedRotatorCount = CountCellType(generatedBlocks, CellType.Rotator);
                int generatedGearCount = CountCellType(generatedBlocks, CellType.Gear);
                Debug.Log($"[Generator] Level_{_startIndex + i:000} | size={size} | blocks={generatedBlocks.Count} | gear={generatedGearCount} | rotator={generatedRotatorCount} | config(rotAllow={_config.allowRotator}, ratio={_config.rotatorRatio:0.###})");

                string path = $"{dir}/Level_{_startIndex + i:000}.asset";
                AssetDatabase.CreateAsset(level, path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated levels successfully.");
        }

        private static List<BlockData> GenerateBlocks(
            int size,
            int count,
            float gearRatio,
            float rotatorRatio,
            bool allowGear,
            bool allowRotator)
        {
            List<BlockData> result = new List<BlockData>(count);
            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

            int id = 1;
            int safeLimit = size * size * 2;
            float effectiveGearRatio = Mathf.Clamp01(gearRatio);
            float effectiveRotatorRatio = Mathf.Clamp01(rotatorRatio);

            int minimumGearCount = allowGear ? 1 : 0;
            int minimumRotatorCount = allowRotator ? 1 : 0;
            if (minimumGearCount + minimumRotatorCount > count)
            {
                if (count <= 0)
                {
                    minimumGearCount = 0;
                    minimumRotatorCount = 0;
                }
                else
                {
                    bool preferRotator = effectiveRotatorRatio >= effectiveGearRatio;
                    minimumGearCount = preferRotator ? 0 : 1;
                    minimumRotatorCount = preferRotator ? 1 : 0;
                }
            }

            int gearQuota = minimumGearCount;
            int rotatorQuota = minimumRotatorCount;
            int remainingCount = Mathf.Max(0, count - (gearQuota + rotatorQuota));
            float ratioSum = effectiveGearRatio + effectiveRotatorRatio;
            if (remainingCount > 0 && ratioSum > 0f)
            {
                int extraGear = Mathf.Clamp(Mathf.RoundToInt(remainingCount * (effectiveGearRatio / ratioSum)), 0, remainingCount);
                int extraRotator = Mathf.Clamp(Mathf.RoundToInt(remainingCount * (effectiveRotatorRatio / ratioSum)), 0, remainingCount - extraGear);
                gearQuota += extraGear;
                rotatorQuota += extraRotator;

                int leftover = remainingCount - (extraGear + extraRotator);
                for (int i = 0; i < leftover; i++)
                {
                    if (effectiveRotatorRatio > effectiveGearRatio)
                    {
                        rotatorQuota++;
                        continue;
                    }

                    if (effectiveGearRatio > effectiveRotatorRatio)
                    {
                        gearQuota++;
                        continue;
                    }

                    if (Random.value < 0.5f)
                    {
                        gearQuota++;
                    }
                    else
                    {
                        rotatorQuota++;
                    }
                }
            }

            if (gearQuota + rotatorQuota > count)
            {
                rotatorQuota = Mathf.Max(0, count - gearQuota);
            }

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
                    FillRotatorLinks(result, i, size);
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

                FillRotatorLinks(result, i, size);
            }

            EnforceUniqueRotatorLinks(result);
            EnforceRotatorsCanRotate(result, size);
            EnforceRotatorLinksCanLeadToExit(result, size);
            EnforceGearHasAimingNormal(result, size);
            EnsureMinimumGearPresence(result, size, minimumGearCount);
            EnsureMinimumRotatorPresence(result, size, minimumRotatorCount);

            return result;
        }

        private static bool IsValidGeneratedLayout(List<BlockData> blocks, int size)
        {
            if (blocks == null || blocks.Count == 0)
            {
                return false;
            }

            if (HasMutualHeadOnNormalPair(blocks))
            {
                return false;
            }

            if (HasUnlinkedNormalRayIntoRotator(blocks, size))
            {
                return false;
            }

            if (HasNormalAimingTowardAnyRotatorLine(blocks))
            {
                return false;
            }

            if (!HasAnyInitialAction(blocks, size))
            {
                return false;
            }

            return IsSolvableLayout(blocks, size, SOLVER_NODE_LIMIT);
        }

        private static bool IsSolvableLayout(List<BlockData> blocks, int size, int nodeLimit)
        {
            if (blocks == null || blocks.Count == 0)
            {
                return false;
            }

            int exploredNodeCount = 0;
            HashSet<string> visited = new HashSet<string>();
            List<BlockData> startState = CloneBlocks(blocks);
            return SearchSolvable(startState, size, visited, ref exploredNodeCount, Mathf.Max(100, nodeLimit));
        }

        private static bool SearchSolvable(
            List<BlockData> state,
            int size,
            HashSet<string> visited,
            ref int exploredNodeCount,
            int nodeLimit)
        {
            if (state == null)
            {
                return false;
            }

            if (CountRemainingNormals(state) == 0)
            {
                return true;
            }

            if (exploredNodeCount >= nodeLimit)
            {
                return false;
            }

            string stateKey = EncodeState(state);
            if (!visited.Add(stateKey))
            {
                return false;
            }

            exploredNodeCount++;

            GridSystem grid = BuildGridFromState(state, size);
            if (grid == null)
            {
                return false;
            }

            List<Vector2Int> normalActionPositions = new List<Vector2Int>();
            List<Vector2Int> rotatorActionPositions = new List<Vector2Int>();

            IReadOnlyList<Block> runtimeBlocks = grid.Blocks;
            for (int i = 0; i < runtimeBlocks.Count; i++)
            {
                Block block = runtimeBlocks[i];
                if (block == null || block.IsRemoved)
                {
                    continue;
                }

                if (block.Type == CellType.Normal && grid.CanTapNormalChangeState(block))
                {
                    normalActionPositions.Add(block.Position);
                    continue;
                }

                if (block.Type == CellType.Rotator && grid.CanRotatorMove(block))
                {
                    rotatorActionPositions.Add(block.Position);
                }
            }

            if (normalActionPositions.Count == 0 && rotatorActionPositions.Count == 0)
            {
                return false;
            }

            // Ưu tiên thử action loại bỏ normal trước để hội tụ nhanh hơn.
            for (int i = 0; i < normalActionPositions.Count; i++)
            {
                if (!TryBuildNextStateByNormalTap(state, size, normalActionPositions[i], out List<BlockData> nextState))
                {
                    continue;
                }

                if (SearchSolvable(nextState, size, visited, ref exploredNodeCount, nodeLimit))
                {
                    return true;
                }
            }

            for (int i = 0; i < rotatorActionPositions.Count; i++)
            {
                if (!TryBuildNextStateByRotatorTap(state, size, rotatorActionPositions[i], out List<BlockData> nextState))
                {
                    continue;
                }

                if (SearchSolvable(nextState, size, visited, ref exploredNodeCount, nodeLimit))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryBuildNextStateByNormalTap(
            List<BlockData> state,
            int size,
            Vector2Int normalPosition,
            out List<BlockData> nextState)
        {
            nextState = null;
            GridSystem grid = BuildGridFromState(state, size);
            if (grid == null)
            {
                return false;
            }

            if (!grid.TryGetBlock(normalPosition, out Block normal) || normal == null || normal.Type != CellType.Normal)
            {
                return false;
            }

            if (!grid.TryTapNormal(normal, null))
            {
                return false;
            }

            nextState = ExtractStateFromGrid(grid);
            return nextState != null;
        }

        private static bool TryBuildNextStateByRotatorTap(
            List<BlockData> state,
            int size,
            Vector2Int rotatorPosition,
            out List<BlockData> nextState)
        {
            nextState = null;
            GridSystem grid = BuildGridFromState(state, size);
            if (grid == null)
            {
                return false;
            }

            if (!grid.TryGetBlock(rotatorPosition, out Block rotator)
                || rotator == null
                || rotator.Type != CellType.Rotator)
            {
                return false;
            }

            if (!grid.TryTapRotator(rotator))
            {
                return false;
            }

            nextState = ExtractStateFromGrid(grid);
            return nextState != null;
        }

        private static GridSystem BuildGridFromState(List<BlockData> state, int size)
        {
            if (state == null)
            {
                return null;
            }

            GridSystem grid = new GridSystem();
            grid.Build(size, size, CloneBlocks(state));
            return grid;
        }

        private static List<BlockData> ExtractStateFromGrid(GridSystem grid)
        {
            if (grid == null)
            {
                return null;
            }

            List<BlockData> result = new List<BlockData>();
            IReadOnlyList<Block> runtimeBlocks = grid.Blocks;
            int id = 1;

            for (int i = 0; i < runtimeBlocks.Count; i++)
            {
                Block runtimeBlock = runtimeBlocks[i];
                if (runtimeBlock == null || runtimeBlock.IsRemoved)
                {
                    continue;
                }

                result.Add(new BlockData
                {
                    id = id++,
                    position = runtimeBlock.Position,
                    direction = runtimeBlock.Direction,
                    cellType = runtimeBlock.Type,
                    rotatorLinkedNormals = new List<Vector2Int>(),
                });
            }

            List<Vector2Int> linkedBuffer = new List<Vector2Int>();
            for (int i = 0; i < runtimeBlocks.Count; i++)
            {
                Block runtimeBlock = runtimeBlocks[i];
                if (runtimeBlock == null || runtimeBlock.IsRemoved || runtimeBlock.Type != CellType.Rotator)
                {
                    continue;
                }

                int index = FindBlockIndex(result, runtimeBlock.Position);
                if (index < 0)
                {
                    continue;
                }

                linkedBuffer.Clear();
                grid.GetRotatorLinkedPositions(runtimeBlock, linkedBuffer);

                BlockData rotatorData = result[index];
                rotatorData.rotatorLinkedNormals = new List<Vector2Int>(linkedBuffer);
                result[index] = rotatorData;
            }

            return result;
        }

        private static int CountRemainingNormals(List<BlockData> state)
        {
            if (state == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < state.Count; i++)
            {
                if (state[i].cellType == CellType.Normal)
                {
                    count++;
                }
            }

            return count;
        }

        private static string EncodeState(List<BlockData> state)
        {
            if (state == null || state.Count == 0)
            {
                return string.Empty;
            }

            List<string> encodedBlocks = new List<string>(state.Count);
            for (int i = 0; i < state.Count; i++)
            {
                BlockData block = state[i];
                StringBuilder builder = new StringBuilder();
                builder.Append((int)block.cellType)
                    .Append(':')
                    .Append(block.position.x)
                    .Append(',')
                    .Append(block.position.y)
                    .Append(',')
                    .Append((int)block.direction);

                if (block.cellType == CellType.Rotator && block.rotatorLinkedNormals != null && block.rotatorLinkedNormals.Count > 0)
                {
                    List<Vector2Int> links = new List<Vector2Int>(block.rotatorLinkedNormals);
                    links.Sort((a, b) =>
                    {
                        int xCompare = a.x.CompareTo(b.x);
                        if (xCompare != 0)
                        {
                            return xCompare;
                        }

                        return a.y.CompareTo(b.y);
                    });

                    builder.Append('|');
                    for (int j = 0; j < links.Count; j++)
                    {
                        if (j > 0)
                        {
                            builder.Append(';');
                        }

                        builder.Append(links[j].x)
                            .Append(',')
                            .Append(links[j].y);
                    }
                }

                encodedBlocks.Add(builder.ToString());
            }

            encodedBlocks.Sort(StringComparer.Ordinal);
            return string.Join("#", encodedBlocks);
        }

        private static bool HasMutualHeadOnNormalPair(List<BlockData> blocks)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData source = blocks[i];
                if (source.cellType != CellType.Normal)
                {
                    continue;
                }

                Vector2Int neighborPos = source.position + DirectionToOffset(source.direction);
                int neighborIndex = FindBlockIndex(blocks, neighborPos);
                if (neighborIndex < 0)
                {
                    continue;
                }

                BlockData neighbor = blocks[neighborIndex];
                if (neighbor.cellType != CellType.Normal)
                {
                    continue;
                }

                Vector2Int reverseStep = DirectionToOffset(neighbor.direction);
                if (neighbor.position + reverseStep == source.position)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAnyInitialAction(List<BlockData> blocks, int size)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData block = blocks[i];
                if (block.cellType == CellType.Normal && CanNormalChangeStateInLayout(blocks, size, block))
                {
                    return true;
                }
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].cellType != CellType.Rotator)
                {
                    continue;
                }

                if (TrySimulateSingleRotatorStep(blocks, size, i, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasUnlinkedNormalRayIntoRotator(List<BlockData> blocks, int size)
        {
            if (blocks == null)
            {
                return false;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData normal = blocks[i];
                if (normal.cellType != CellType.Normal)
                {
                    continue;
                }

                if (IsNormalPointingToUnlinkedRotator(blocks, size, normal.position, normal.direction))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanNormalChangeStateInLayout(List<BlockData> blocks, int size, BlockData normal)
        {
            Vector2Int step = DirectionToOffset(normal.direction);
            Vector2Int cursor = normal.position + step;

            while (IsInsideBounds(cursor, size))
            {
                int occupiedIndex = FindBlockIndex(blocks, cursor);
                if (occupiedIndex >= 0)
                {
                    return blocks[occupiedIndex].cellType == CellType.Gear;
                }

                cursor += step;
            }

            // Không bị cản đến biên => có thể thoát grid.
            return true;
        }

        private static int EstimateMoveLimit(List<BlockData> blocks, int gridSize, int minMovesBuffer, int maxMovesBuffer)
        {
            int normalCount = 0;
            int rotatorCount = 0;
            int gearCount = 0;

            for (int i = 0; i < blocks.Count; i++)
            {
                switch (blocks[i].cellType)
                {
                    case CellType.Normal:
                        normalCount++;
                        break;
                    case CellType.Rotator:
                        rotatorCount++;
                        break;
                    case CellType.Gear:
                        gearCount++;
                        break;
                }
            }

            int safeGridSize = Mathf.Max(2, gridSize);
            int cellCount = Mathf.Max(1, safeGridSize * safeGridSize);
            int totalBlocks = Mathf.Max(1, normalCount + rotatorCount + gearCount);
            float density = Mathf.Clamp01((float)totalBlocks / cellCount);

            // Heuristic studio-style: baseline từ "số objective" + chi phí hệ cơ chế + complexity buffer.
            int estimatedOptimalMoves = Mathf.Max(1,
                normalCount
                + Mathf.CeilToInt(rotatorCount * (1.0f + safeGridSize * 0.08f))
                + Mathf.CeilToInt(gearCount * 0.35f));

            int mapComplexityBuffer = Mathf.CeilToInt(safeGridSize * 0.65f);
            int blockComplexityBuffer = Mathf.CeilToInt(totalBlocks * 0.16f);
            int densityBuffer = Mathf.CeilToInt(density * safeGridSize * 1.1f);

            int minBuffer = Mathf.Max(0, minMovesBuffer);
            int maxBuffer = Mathf.Max(minBuffer, maxMovesBuffer);
            int randomBuffer = Random.Range(minBuffer, maxBuffer + 1);

            int moveLimit = estimatedOptimalMoves
                + mapComplexityBuffer
                + blockComplexityBuffer
                + densityBuffer
                + randomBuffer;

            // Không để move nhỏ hơn ngưỡng tối thiểu an toàn theo block count.
            int lowerBound = normalCount + Mathf.CeilToInt(totalBlocks * 0.18f);
            return Mathf.Max(moveLimit, lowerBound);
        }

        private static void SanitizeNormalDirections(List<BlockData> blocks, int size)
        {
            if (blocks == null || blocks.Count == 0)
            {
                return;
            }

            // Chỉ xử lý cặp normal đâm đầu trực diện; không khóa cả hàng/cột để tránh quá đơn điệu.
            const int MAX_FIX_PASSES = 6;
            for (int pass = 0; pass < MAX_FIX_PASSES; pass++)
            {
                bool changed = false;

                for (int i = 0; i < blocks.Count; i++)
                {
                    BlockData source = blocks[i];
                    if (source.cellType != CellType.Normal)
                    {
                        continue;
                    }

                    Vector2Int forwardPos = source.position + DirectionToOffset(source.direction);
                    int neighborIndex = FindBlockIndex(blocks, forwardPos);
                    if (neighborIndex < 0)
                    {
                        continue;
                    }

                    BlockData neighbor = blocks[neighborIndex];
                    if (neighbor.cellType != CellType.Normal)
                    {
                        continue;
                    }

                    Vector2Int neighborForward = neighbor.position + DirectionToOffset(neighbor.direction);
                    if (neighborForward != source.position)
                    {
                        // Không cho normal không link đâm thẳng vào rotator theo tia di chuyển.
                        if (IsNormalPointingToUnlinkedRotator(blocks, size, source.position, source.direction))
                        {
                            BlockData fixedRaySource = source;
                            fixedRaySource.direction = FindSafeDirectionAvoidingRotatorRay(blocks, size, source.position, source.direction);
                            blocks[i] = fixedRaySource;
                            changed = changed || fixedRaySource.direction != source.direction;
                        }

                        continue;
                    }

                    BlockData fixedSource = source;
                    fixedSource.direction = GetRandomDirectionExcept(source.direction, neighbor.direction);
                    blocks[i] = fixedSource;
                    changed = true;
                }

                if (!changed || !HasMutualHeadOnNormalPair(blocks))
                {
                    ResolveUnlinkedNormalRotatorRayConflicts(blocks, size);
                    break;
                }
            }

            ResolveUnlinkedNormalRotatorRayConflicts(blocks, size);
        }

        private static void ResolveUnlinkedNormalRotatorRayConflicts(List<BlockData> blocks, int size)
        {
            if (blocks == null)
            {
                return;
            }

            const int MAX_FIX_PASSES = 4;
            for (int pass = 0; pass < MAX_FIX_PASSES; pass++)
            {
                bool changed = false;

                for (int i = 0; i < blocks.Count; i++)
                {
                    BlockData normal = blocks[i];
                    if (normal.cellType != CellType.Normal)
                    {
                        continue;
                    }

                    if (!IsNormalPointingToUnlinkedRotator(blocks, size, normal.position, normal.direction))
                    {
                        continue;
                    }

                    BlockDirection safeDirection = FindSafeDirectionAvoidingRotatorRay(blocks, size, normal.position, normal.direction);
                    if (safeDirection == normal.direction)
                    {
                        continue;
                    }

                    BlockData fixedNormal = normal;
                    fixedNormal.direction = safeDirection;
                    blocks[i] = fixedNormal;
                    changed = true;
                }

                if (!changed)
                {
                    break;
                }
            }

            ResolveAnyNormalAimingTowardRotatorLine(blocks);
        }

        private static void ResolveAnyNormalAimingTowardRotatorLine(List<BlockData> blocks)
        {
            if (blocks == null)
            {
                return;
            }

            const int MAX_FIX_PASSES = 4;
            for (int pass = 0; pass < MAX_FIX_PASSES; pass++)
            {
                bool changed = false;

                for (int i = 0; i < blocks.Count; i++)
                {
                    BlockData normal = blocks[i];
                    if (normal.cellType != CellType.Normal)
                    {
                        continue;
                    }

                    if (!IsDirectionAimingTowardAnyRotatorLine(blocks, normal.position, normal.direction))
                    {
                        continue;
                    }

                    BlockDirection safeDirection = FindDirectionNotAimingAnyRotatorLine(blocks, normal.position, normal.direction);
                    if (safeDirection == normal.direction)
                    {
                        continue;
                    }

                    BlockData fixedNormal = normal;
                    fixedNormal.direction = safeDirection;
                    blocks[i] = fixedNormal;
                    changed = true;
                }

                if (!changed)
                {
                    break;
                }
            }
        }

        private static bool HasNormalAimingTowardAnyRotatorLine(List<BlockData> blocks)
        {
            if (blocks == null)
            {
                return false;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData normal = blocks[i];
                if (normal.cellType != CellType.Normal)
                {
                    continue;
                }

                if (IsDirectionAimingTowardAnyRotatorLine(blocks, normal.position, normal.direction))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDirectionAimingTowardAnyRotatorLine(
            List<BlockData> blocks,
            Vector2Int normalPosition,
            BlockDirection direction)
        {
            if (blocks == null)
            {
                return false;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData candidate = blocks[i];
                if (candidate.cellType != CellType.Rotator)
                {
                    continue;
                }

                Vector2Int rotatorPosition = candidate.position;

                if (normalPosition.x == rotatorPosition.x)
                {
                    if (rotatorPosition.y > normalPosition.y && direction == BlockDirection.Up)
                    {
                        if (IsNormalLinkedToRotatorAt(blocks, normalPosition, rotatorPosition))
                        {
                            continue;
                        }

                        return true;
                    }

                    if (rotatorPosition.y < normalPosition.y && direction == BlockDirection.Down)
                    {
                        if (IsNormalLinkedToRotatorAt(blocks, normalPosition, rotatorPosition))
                        {
                            continue;
                        }

                        return true;
                    }
                }

                if (normalPosition.y == rotatorPosition.y)
                {
                    if (rotatorPosition.x > normalPosition.x && direction == BlockDirection.Right)
                    {
                        if (IsNormalLinkedToRotatorAt(blocks, normalPosition, rotatorPosition))
                        {
                            continue;
                        }

                        return true;
                    }

                    if (rotatorPosition.x < normalPosition.x && direction == BlockDirection.Left)
                    {
                        if (IsNormalLinkedToRotatorAt(blocks, normalPosition, rotatorPosition))
                        {
                            continue;
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        private static BlockDirection FindDirectionNotAimingAnyRotatorLine(
            List<BlockData> blocks,
            Vector2Int normalPosition,
            BlockDirection currentDirection)
        {
            List<BlockDirection> candidates = new List<BlockDirection>(4)
            {
                BlockDirection.Up,
                BlockDirection.Right,
                BlockDirection.Down,
                BlockDirection.Left,
            };

            for (int i = 0; i < candidates.Count; i++)
            {
                int swapIndex = Random.Range(i, candidates.Count);
                BlockDirection temp = candidates[i];
                candidates[i] = candidates[swapIndex];
                candidates[swapIndex] = temp;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                BlockDirection candidate = candidates[i];
                if (candidate == currentDirection)
                {
                    continue;
                }

                if (!IsDirectionAimingTowardAnyRotatorLine(blocks, normalPosition, candidate))
                {
                    return candidate;
                }
            }

            return currentDirection;
        }

        private static BlockDirection FindSafeDirectionAvoidingRotatorRay(
            List<BlockData> blocks,
            int size,
            Vector2Int origin,
            BlockDirection currentDirection)
        {
            List<BlockDirection> candidates = new List<BlockDirection>(4)
            {
                BlockDirection.Up,
                BlockDirection.Right,
                BlockDirection.Down,
                BlockDirection.Left,
            };

            for (int i = 0; i < candidates.Count; i++)
            {
                int swap = Random.Range(i, candidates.Count);
                BlockDirection temp = candidates[i];
                candidates[i] = candidates[swap];
                candidates[swap] = temp;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                BlockDirection candidate = candidates[i];
                if (candidate == currentDirection)
                {
                    continue;
                }

                if (!IsNormalPointingToUnlinkedRotator(blocks, size, origin, candidate))
                {
                    return candidate;
                }
            }

            return currentDirection;
        }

        private static bool DoesDirectionRayHitRotatorFirst(
            List<BlockData> blocks,
            int size,
            Vector2Int origin,
            BlockDirection direction)
        {
            return TryGetFirstObstacleOnRay(blocks, size, origin, direction, out BlockData obstacle)
                && obstacle.cellType == CellType.Rotator;
        }

        private static bool IsNormalPointingToUnlinkedRotator(
            List<BlockData> blocks,
            int size,
            Vector2Int normalPosition,
            BlockDirection direction)
        {
            if (!TryGetFirstObstacleOnRay(blocks, size, normalPosition, direction, out BlockData obstacle))
            {
                return false;
            }

            if (obstacle.cellType != CellType.Rotator)
            {
                return false;
            }

            return !IsNormalLinkedToRotatorAt(blocks, normalPosition, obstacle.position);
        }

        private static bool TryGetFirstObstacleOnRay(
            List<BlockData> blocks,
            int size,
            Vector2Int origin,
            BlockDirection direction,
            out BlockData obstacle)
        {
            obstacle = null;
            Vector2Int step = DirectionToOffset(direction);
            Vector2Int cursor = origin + step;

            while (IsInsideBounds(cursor, size))
            {
                int index = FindBlockIndex(blocks, cursor);
                if (index >= 0)
                {
                    obstacle = blocks[index];
                    return true;
                }

                cursor += step;
            }

            return false;
        }

        private static bool IsNormalLinkedToRotatorAt(List<BlockData> blocks, Vector2Int normalPosition, Vector2Int rotatorPosition)
        {
            if (blocks == null)
            {
                return false;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData block = blocks[i];
                if (block.cellType != CellType.Rotator || block.position != rotatorPosition || block.rotatorLinkedNormals == null)
                {
                    continue;
                }

                for (int j = 0; j < block.rotatorLinkedNormals.Count; j++)
                {
                    if (block.rotatorLinkedNormals[j] == normalPosition)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static BlockDirection GetRandomDirectionExcept(BlockDirection dirA, BlockDirection dirB)
        {
            List<BlockDirection> candidates = new List<BlockDirection>(4)
            {
                BlockDirection.Up,
                BlockDirection.Right,
                BlockDirection.Down,
                BlockDirection.Left,
            };

            candidates.Remove(dirA);
            candidates.Remove(dirB);
            if (candidates.Count == 0)
            {
                return BlockDirection.Up;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        private static void EnforceGearHasAimingNormal(List<BlockData> blocks, int size)
        {
            if (blocks == null || blocks.Count == 0)
            {
                return;
            }

            List<Vector2Int> simulatedGearSupport = BuildSimulatedGearSupportMap(blocks, size);

            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].cellType != CellType.Gear)
                {
                    continue;
                }

                Vector2Int gearPosition = blocks[i].position;
                bool hasSupportInInitial = HasAimingNormalForGear(blocks, gearPosition);
                bool hasSupportAfterRotator = simulatedGearSupport.Contains(gearPosition);
                if (hasSupportInInitial || hasSupportAfterRotator)
                {
                    continue;
                }

                // Gear không có gameplay value thì đổi thành normal để đảm bảo rule design.
                BlockData converted = blocks[i];
                converted.cellType = CellType.Normal;
                blocks[i] = converted;
            }
        }

        private static List<Vector2Int> BuildSimulatedGearSupportMap(List<BlockData> blocks, int size)
        {
            List<Vector2Int> supportedGears = new List<Vector2Int>();
            if (blocks == null || blocks.Count == 0)
            {
                return supportedGears;
            }

            List<int> rotatorIndexes = new List<int>();
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].cellType == CellType.Rotator)
                {
                    rotatorIndexes.Add(i);
                }
            }

            for (int i = 0; i < rotatorIndexes.Count; i++)
            {
                int rotatorIndex = rotatorIndexes[i];
                if (!TrySimulateSingleRotatorStep(blocks, size, rotatorIndex, out List<BlockData> simulatedState))
                {
                    continue;
                }

                for (int j = 0; j < simulatedState.Count; j++)
                {
                    if (simulatedState[j].cellType != CellType.Gear)
                    {
                        continue;
                    }

                    Vector2Int gearPos = simulatedState[j].position;
                    if (!HasAimingNormalForGear(simulatedState, gearPos))
                    {
                        continue;
                    }

                    if (!supportedGears.Contains(gearPos))
                    {
                        supportedGears.Add(gearPos);
                    }
                }
            }

            return supportedGears;
        }

        private static bool TrySimulateSingleRotatorStep(
            List<BlockData> blocks,
            int size,
            int rotatorIndex,
            out List<BlockData> simulatedState)
        {
            return TrySimulateRotatorStep(blocks, size, rotatorIndex, 1, out simulatedState);
        }

        private static bool TrySimulateRotatorStep(
            List<BlockData> blocks,
            int size,
            int rotatorIndex,
            int stepCount,
            out List<BlockData> simulatedState)
        {
            simulatedState = null;
            if (blocks == null || rotatorIndex < 0 || rotatorIndex >= blocks.Count)
            {
                return false;
            }

            BlockData rotator = blocks[rotatorIndex];
            if (rotator.cellType != CellType.Rotator || rotator.rotatorLinkedNormals == null || rotator.rotatorLinkedNormals.Count == 0)
            {
                return false;
            }

            List<int> linkedNormalIndexes = new List<int>();
            List<Vector2Int> targetPositions = new List<Vector2Int>();

            for (int i = 0; i < rotator.rotatorLinkedNormals.Count; i++)
            {
                Vector2Int linkedPos = rotator.rotatorLinkedNormals[i];
                int linkedIndex = FindBlockIndex(blocks, linkedPos);
                if (linkedIndex < 0 || blocks[linkedIndex].cellType != CellType.Normal)
                {
                    continue;
                }

                linkedNormalIndexes.Add(linkedIndex);
            }

            if (linkedNormalIndexes.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < linkedNormalIndexes.Count; i++)
            {
                int linkedIndex = linkedNormalIndexes[i];
                Vector2Int linkedPos = blocks[linkedIndex].position;

                Vector2Int rotatedTarget = RotateClockwise(linkedPos, rotator.position, stepCount);
                if (!IsInsideBounds(rotatedTarget, size))
                {
                    return false;
                }

                if (targetPositions.Contains(rotatedTarget))
                {
                    return false;
                }

                int occupiedIndex = FindBlockIndex(blocks, rotatedTarget);
                if (occupiedIndex >= 0 && !linkedNormalIndexes.Contains(occupiedIndex))
                {
                    return false;
                }

                targetPositions.Add(rotatedTarget);
            }

            simulatedState = CloneBlocks(blocks);
            for (int i = 0; i < linkedNormalIndexes.Count; i++)
            {
                int normalIndex = linkedNormalIndexes[i];
                BlockData moved = simulatedState[normalIndex];
                moved.position = targetPositions[i];
                simulatedState[normalIndex] = moved;
            }

            return true;
        }

        private static void EnforceRotatorsCanRotate(List<BlockData> blocks, int size)
        {
            if (blocks == null || blocks.Count == 0)
            {
                return;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData rotator = blocks[i];
                if (rotator.cellType != CellType.Rotator)
                {
                    continue;
                }

                bool canRotate = false;
                for (int step = 1; step <= 3; step++)
                {
                    if (TrySimulateRotatorStep(blocks, size, i, step, out _))
                    {
                        canRotate = true;
                        break;
                    }
                }

                if (canRotate)
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

        private static void EnforceRotatorLinksCanLeadToExit(List<BlockData> blocks, int size)
        {
            if (blocks == null || blocks.Count == 0)
            {
                return;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData rotator = blocks[i];
                if (rotator.cellType != CellType.Rotator)
                {
                    continue;
                }

                if (rotator.rotatorLinkedNormals == null || rotator.rotatorLinkedNormals.Count == 0)
                {
                    BlockData convertedNoLink = rotator;
                    convertedNoLink.cellType = CellType.Normal;
                    convertedNoLink.rotatorLinkedNormals?.Clear();
                    blocks[i] = convertedNoLink;
                    continue;
                }

                bool hasExitOutcome = false;
                for (int step = 1; step <= 3; step++)
                {
                    if (!TrySimulateRotatorStep(blocks, size, i, step, out List<BlockData> simulatedState))
                    {
                        continue;
                    }

                    if (HasMutualHeadOnNormalPair(simulatedState))
                    {
                        continue;
                    }

                    int simulatedRotatorIndex = FindBlockIndex(simulatedState, rotator.position);
                    if (simulatedRotatorIndex < 0 || simulatedState[simulatedRotatorIndex].cellType != CellType.Rotator)
                    {
                        continue;
                    }

                    List<Vector2Int> linkedNormals = simulatedState[simulatedRotatorIndex].rotatorLinkedNormals;
                    if (linkedNormals == null || linkedNormals.Count == 0)
                    {
                        continue;
                    }

                    for (int linkIndex = 0; linkIndex < linkedNormals.Count; linkIndex++)
                    {
                        int normalIndex = FindBlockIndex(simulatedState, linkedNormals[linkIndex]);
                        if (normalIndex < 0)
                        {
                            continue;
                        }

                        BlockData linkedNormal = simulatedState[normalIndex];
                        if (linkedNormal.cellType != CellType.Normal)
                        {
                            continue;
                        }

                        if (CanNormalProgressInLayout(simulatedState, size, linkedNormal))
                        {
                            hasExitOutcome = true;
                            break;
                        }
                    }

                    if (hasExitOutcome)
                    {
                        break;
                    }
                }

                if (hasExitOutcome)
                {
                    continue;
                }

                BlockData converted = rotator;
                converted.cellType = CellType.Normal;
                converted.rotatorLinkedNormals?.Clear();
                blocks[i] = converted;
            }
        }

        private static bool CanNormalProgressInLayout(List<BlockData> blocks, int size, BlockData normal)
        {
            Vector2Int step = DirectionToOffset(normal.direction);
            Vector2Int cursor = normal.position + step;
            bool hasFreeCellAhead = false;

            while (IsInsideBounds(cursor, size))
            {
                int obstacleIndex = FindBlockIndex(blocks, cursor);
                if (obstacleIndex >= 0)
                {
                    CellType obstacleType = blocks[obstacleIndex].cellType;
                    if (obstacleType == CellType.Gear)
                    {
                        return true;
                    }

                    // Có ô trống phía trước obstacle thì vẫn được xem là tạo được progress.
                    return hasFreeCellAhead;
                }

                hasFreeCellAhead = true;
                cursor += step;
            }

            return true;
        }

        private static void EnsureMinimumRotatorPresence(List<BlockData> blocks, int size, int minimumRotatorCount)
        {
            if (blocks == null || minimumRotatorCount <= 0)
            {
                return;
            }

            int currentRotatorCount = CountCellType(blocks, CellType.Rotator);
            if (currentRotatorCount >= minimumRotatorCount)
            {
                return;
            }

            List<int> normalIndexes = new List<int>();
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].cellType == CellType.Normal)
                {
                    normalIndexes.Add(i);
                }
            }

            for (int i = 0; i < normalIndexes.Count && currentRotatorCount < minimumRotatorCount; i++)
            {
                int candidateIndex = normalIndexes[i];
                BlockData candidate = blocks[candidateIndex];
                if (!HasAdjacentNormal(blocks, candidate.position))
                {
                    continue;
                }

                List<BlockData> trial = CloneBlocks(blocks);
                BlockData upgraded = trial[candidateIndex];
                upgraded.cellType = CellType.Rotator;
                upgraded.rotatorLinkedNormals = new List<Vector2Int>();
                trial[candidateIndex] = upgraded;

                FillRotatorLinks(trial, candidateIndex, size);
                EnforceUniqueRotatorLinks(trial);
                EnforceRotatorsCanRotate(trial, size);
                EnforceRotatorLinksCanLeadToExit(trial, size);

                int rotatorIndexAfterValidation = FindBlockIndex(trial, candidate.position);
                if (rotatorIndexAfterValidation < 0 || trial[rotatorIndexAfterValidation].cellType != CellType.Rotator)
                {
                    continue;
                }

                blocks.Clear();
                blocks.AddRange(trial);
                currentRotatorCount = CountCellType(blocks, CellType.Rotator);
            }
        }

        private static void EnsureMinimumGearPresence(List<BlockData> blocks, int size, int minimumGearCount)
        {
            if (blocks == null || minimumGearCount <= 0)
            {
                return;
            }

            int currentGearCount = CountCellType(blocks, CellType.Gear);
            if (currentGearCount >= minimumGearCount)
            {
                return;
            }

            List<int> normalIndexes = new List<int>();
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].cellType == CellType.Normal)
                {
                    normalIndexes.Add(i);
                }
            }

            for (int i = 0; i < normalIndexes.Count && currentGearCount < minimumGearCount; i++)
            {
                int candidateIndex = normalIndexes[i];
                BlockData candidate = blocks[candidateIndex];

                List<BlockData> trial = CloneBlocks(blocks);
                BlockData upgraded = trial[candidateIndex];
                upgraded.cellType = CellType.Gear;
                upgraded.rotatorLinkedNormals?.Clear();
                trial[candidateIndex] = upgraded;

                if (!HasAimingNormalForGear(trial, candidate.position))
                {
                    continue;
                }

                int gearIndexAfterValidation = FindBlockIndex(trial, candidate.position);
                if (gearIndexAfterValidation < 0 || trial[gearIndexAfterValidation].cellType != CellType.Gear)
                {
                    continue;
                }

                blocks.Clear();
                blocks.AddRange(trial);
                currentGearCount = CountCellType(blocks, CellType.Gear);
            }
        }

        private static int CountCellType(List<BlockData> blocks, CellType targetType)
        {
            if (blocks == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].cellType == targetType)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasRequiredSpecialBlocks(List<BlockData> blocks, bool requireGear, bool requireRotator)
        {
            if (blocks == null)
            {
                return false;
            }

            if (requireGear && CountCellType(blocks, CellType.Gear) <= 0)
            {
                return false;
            }

            if (requireRotator && CountCellType(blocks, CellType.Rotator) <= 0)
            {
                return false;
            }

            return true;
        }

        private static void EnsureRequiredSpecialBlocks(List<BlockData> blocks, int size, bool requireGear, bool requireRotator)
        {
            if (blocks == null)
            {
                return;
            }

            if (requireGear)
            {
                EnsureMinimumGearPresence(blocks, size, 1);
            }

            if (requireRotator)
            {
                EnsureMinimumRotatorPresence(blocks, size, 1);
            }
        }

        private static List<BlockData> CloneBlocks(List<BlockData> blocks)
        {
            List<BlockData> cloned = new List<BlockData>(blocks.Count);
            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData source = blocks[i];
                BlockData copy = source;
                if (source.rotatorLinkedNormals != null)
                {
                    copy.rotatorLinkedNormals = new List<Vector2Int>(source.rotatorLinkedNormals);
                }
                else
                {
                    copy.rotatorLinkedNormals = new List<Vector2Int>();
                }

                cloned.Add(copy);
            }

            return cloned;
        }

        private static bool HasAimingNormalForGear(List<BlockData> blocks, Vector2Int gearPosition)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData normal = blocks[i];
                if (normal.cellType != CellType.Normal)
                {
                    continue;
                }

                if (DoesNormalRayHitGearFirst(blocks, normal, gearPosition))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool DoesNormalRayHitGearFirst(List<BlockData> blocks, BlockData normal, Vector2Int gearPosition)
        {
            Vector2Int step = DirectionToOffset(normal.direction);
            Vector2Int cursor = normal.position + step;

            while (true)
            {
                int index = FindBlockIndex(blocks, cursor);
                if (index < 0)
                {
                    cursor += step;

                    // Hết hàng/cột tương ứng thì dừng.
                    if (!IsStillOnRayToGear(normal.position, cursor, step, gearPosition))
                    {
                        return false;
                    }

                    continue;
                }

                BlockData encountered = blocks[index];
                if (encountered.position != gearPosition)
                {
                    return false;
                }

                return encountered.cellType == CellType.Gear;
            }
        }

        private static bool IsStillOnRayToGear(Vector2Int origin, Vector2Int cursor, Vector2Int step, Vector2Int gearPosition)
        {
            if (step.x != 0)
            {
                if (origin.y != gearPosition.y)
                {
                    return false;
                }

                if (step.x > 0)
                {
                    return cursor.x <= gearPosition.x;
                }

                return cursor.x >= gearPosition.x;
            }

            if (origin.x != gearPosition.x)
            {
                return false;
            }

            if (step.y > 0)
            {
                return cursor.y <= gearPosition.y;
            }

            return cursor.y >= gearPosition.y;
        }

        private static Vector2Int DirectionToOffset(BlockDirection direction)
        {
            switch (direction)
            {
                case BlockDirection.Right:
                    return Vector2Int.right;
                case BlockDirection.Down:
                    return Vector2Int.down;
                case BlockDirection.Left:
                    return Vector2Int.left;
                default:
                    return Vector2Int.up;
            }
        }

        private static Vector2Int RotateClockwise(Vector2Int source, Vector2Int pivot)
        {
            return RotateClockwise(source, pivot, 1);
        }

        private static Vector2Int RotateClockwise(Vector2Int source, Vector2Int pivot, int stepCount)
        {
            int normalizedStep = Mathf.Abs(stepCount) % 4;
            Vector2Int result = source;

            for (int i = 0; i < normalizedStep; i++)
            {
                int relX = result.x - pivot.x;
                int relY = result.y - pivot.y;
                int newX = pivot.x + relY;
                int newY = pivot.y - relX;
                result = new Vector2Int(newX, newY);
            }

            return result;
        }

        private static bool IsInsideBounds(Vector2Int position, int size)
        {
            return position.x >= 0 && position.x < size && position.y >= 0 && position.y < size;
        }

        private static int FindBlockIndex(List<BlockData> blocks, Vector2Int position)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].position == position)
                {
                    return i;
                }
            }

            return -1;
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

        private static void FillRotatorLinks(List<BlockData> blocks, int rotatorIndex, int size = -1)
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

            List<Vector2Int> adjacentNormals = CollectAdjacentNormalPositions(blocks, rotator.position, size);
            if (adjacentNormals.Count > 0)
            {
                ShufflePositions(adjacentNormals);

                // Random đều từ 1 tới số normal thực tế quanh rotator.
                int linkCount = Random.Range(1, adjacentNormals.Count + 1);
                for (int i = 0; i < linkCount; i++)
                {
                    rotator.rotatorLinkedNormals.Add(adjacentNormals[i]);
                }
            }

            blocks[rotatorIndex] = rotator;
        }

        private static List<Vector2Int> CollectAdjacentNormalPositions(List<BlockData> blocks, Vector2Int center, int size = -1)
        {
            List<Vector2Int> result = new List<Vector2Int>(ROTATOR_LINK_OFFSETS.Length);
            if (blocks == null)
            {
                return result;
            }

            for (int i = 0; i < ROTATOR_LINK_OFFSETS.Length; i++)
            {
                Vector2Int candidate = center + ROTATOR_LINK_OFFSETS[i];
                if (size > 0 && !IsInsideBounds(candidate, size))
                {
                    continue;
                }

                if (ContainsNormalAt(blocks, candidate))
                {
                    result.Add(candidate);
                }
            }

            return result;
        }

        private static void ShufflePositions(List<Vector2Int> positions)
        {
            if (positions == null)
            {
                return;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                int swapIndex = Random.Range(i, positions.Count);
                Vector2Int temp = positions[i];
                positions[i] = positions[swapIndex];
                positions[swapIndex] = temp;
            }
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
