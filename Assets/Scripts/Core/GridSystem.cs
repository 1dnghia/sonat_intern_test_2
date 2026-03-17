using System.Collections.Generic;
using UnityEngine;

namespace TapAway.Core
{
    public class GridSystem
    {
        private sealed class RotatorLinkedNormal
        {
            public RotatorLinkedNormal(Block block, Vector2Int anchorPosition)
            {
                Block = block;
                AnchorPosition = anchorPosition;
            }

            public Block Block { get; }
            public Vector2Int AnchorPosition { get; set; }
        }

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

        private static readonly Vector2Int[] ADJACENT_OFFSETS =
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

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int MinX => _minX;
        public int MaxX => _maxX;
        public int MinY => _minY;
        public int MaxY => _maxY;

        private int _minX;
        private int _maxX;
        private int _minY;
        private int _maxY;

        private readonly Dictionary<Vector2Int, Block> _blocksByPosition = new Dictionary<Vector2Int, Block>();
        private readonly List<Block> _allBlocks = new List<Block>();
        private readonly Dictionary<Vector2Int, List<RotatorLinkedNormal>> _rotatorLinkedNormalsByPosition =
            new Dictionary<Vector2Int, List<RotatorLinkedNormal>>();
        private bool _isApplyingRotatorMove;

        public IReadOnlyList<Block> Blocks => _allBlocks;

        public void Build(LevelData levelData)
        {
            if (levelData == null)
            {
                Build(0, 0, null);
                return;
            }

            Build(levelData.width, levelData.height, levelData.blocks);
        }

        public void Build(int width, int height, IReadOnlyList<BlockData> blocks)
        {
            _blocksByPosition.Clear();
            _allBlocks.Clear();
            _rotatorLinkedNormalsByPosition.Clear();

            Width = Mathf.Max(0, width);
            Height = Mathf.Max(0, height);
            ConfigureGridBounds(width, height, blocks);

            if (blocks == null)
            {
                return;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData blockData = blocks[i];
                if (blockData.cellType == CellType.Empty)
                {
                    continue;
                }

                if (_blocksByPosition.ContainsKey(blockData.position))
                {
                    continue;
                }

                if (blockData.cellType == CellType.Rotator && !HasLinkedNormalInBlocks(blocks, blockData))
                {
                    // Rotator chỉ hợp lệ khi có ít nhất 1 Normal block liên kết xung quanh.
                    continue;
                }

                Block block = new Block(blockData);
                block.Removed += OnBlockRemoved;
                block.Moved += OnBlockMoved;
                _blocksByPosition.Add(blockData.position, block);
                _allBlocks.Add(block);

            }

            BuildRotatorLinks(blocks);
        }

        private void BuildRotatorLinks(IReadOnlyList<BlockData> blocks)
        {
            if (blocks == null)
            {
                return;
            }

            HashSet<Block> linkedNormals = new HashSet<Block>();

            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData blockData = blocks[i];
                if (blockData.cellType != CellType.Rotator)
                {
                    continue;
                }

                if (!TryGetBlock(blockData.position, out Block rotator) || rotator.Type != CellType.Rotator)
                {
                    continue;
                }

                List<Vector2Int> linkedPositions = CollectRotatorLinks(blocks, blockData);
                if (linkedPositions.Count == 0)
                {
                    continue;
                }

                List<RotatorLinkedNormal> linkedBlocks = new List<RotatorLinkedNormal>();
                for (int j = 0; j < linkedPositions.Count; j++)
                {
                    Vector2Int linkedPosition = linkedPositions[j];
                    if (!TryGetBlock(linkedPosition, out Block linkedBlock))
                    {
                        continue;
                    }

                    if (linkedBlock.Type != CellType.Normal || linkedBlock.IsRemoved || linkedNormals.Contains(linkedBlock))
                    {
                        continue;
                    }

                    linkedBlocks.Add(new RotatorLinkedNormal(linkedBlock, linkedPosition));
                    linkedNormals.Add(linkedBlock);
                }

                if (linkedBlocks.Count > 0)
                {
                    _rotatorLinkedNormalsByPosition[rotator.Position] = linkedBlocks;
                }
            }
        }

        private static bool HasLinkedNormalInBlocks(IReadOnlyList<BlockData> blocks, BlockData rotator)
        {
            if (blocks == null)
            {
                return false;
            }

            if (rotator != null && rotator.rotatorLinkedNormals != null)
            {
                for (int i = 0; i < rotator.rotatorLinkedNormals.Count; i++)
                {
                    Vector2Int linkedPos = rotator.rotatorLinkedNormals[i];
                    for (int j = 0; j < blocks.Count; j++)
                    {
                        BlockData blockData = blocks[j];
                        if (blockData.position == linkedPos && blockData.cellType == CellType.Normal)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static List<Vector2Int> CollectRotatorLinks(IReadOnlyList<BlockData> blocks, BlockData rotator)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            if (rotator == null)
            {
                return result;
            }

            if (rotator.rotatorLinkedNormals != null)
            {
                for (int i = 0; i < rotator.rotatorLinkedNormals.Count; i++)
                {
                    Vector2Int linkedPos = rotator.rotatorLinkedNormals[i];
                    if (result.Contains(linkedPos))
                    {
                        continue;
                    }

                    if (!ContainsNormalAt(blocks, linkedPos))
                    {
                        continue;
                    }

                    result.Add(linkedPos);
                }
            }

            if (result.Count > 0)
            {
                return result;
            }

            return result;
        }

        private static bool ContainsNormalAt(IReadOnlyList<BlockData> blocks, Vector2Int position)
        {
            if (blocks == null)
            {
                return false;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData blockData = blocks[i];
                if (blockData.position == position && blockData.cellType == CellType.Normal)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetBlock(Vector2Int position, out Block block)
        {
            if (_blocksByPosition.TryGetValue(position, out block) && !block.IsRemoved)
            {
                return true;
            }

            block = null;
            return false;
        }

        public bool TryTapNormal(Block block, List<Block> blockerChain)
        {
            if (block == null || block.IsRemoved || block.Type != CellType.Normal)
            {
                return false;
            }

            if (blockerChain != null)
            {
                blockerChain.Clear();
            }

            Vector2Int step = DirectionToOffset(block.Direction);
            Vector2Int cursor = block.Position + step;
            Vector2Int lastFreePosition = block.Position;
            Block blockingObstacle = null;

            while (IsInsideGrid(cursor))
            {
                if (TryGetBlock(cursor, out Block obstacle))
                {
                    blockingObstacle = obstacle;
                    break;
                }

                lastFreePosition = cursor;
                cursor += step;
            }

            if (blockingObstacle != null)
            {
                if (blockingObstacle.Type == CellType.Gear)
                {
                    // Block chạy tới ô cuối cùng trước Gear, rồi bị cắt.
                    if (lastFreePosition != block.Position)
                    {
                        block.TryMoveTo(lastFreePosition);
                    }

                    block.TryRemove(BlockRemoveReason.HitGear);
                    return true;
                }

                CollectBlockingChain(blockingObstacle.Position, step, blockerChain);

                if (lastFreePosition != block.Position)
                {
                    block.TryMoveTo(lastFreePosition);
                    return true;
                }

                return false;
            }

            if (lastFreePosition != block.Position)
            {
                block.TryMoveTo(lastFreePosition);
            }

            block.TryRemove(BlockRemoveReason.ExitGrid);
            return true;
        }

        public bool CanTapNormalChangeState(Block block)
        {
            if (block == null || block.IsRemoved || block.Type != CellType.Normal)
            {
                return false;
            }

            Vector2Int step = DirectionToOffset(block.Direction);
            Vector2Int cursor = block.Position + step;
            bool hasFreeCellAhead = false;

            while (IsInsideGrid(cursor))
            {
                if (TryGetBlock(cursor, out Block obstacle))
                {
                    if (obstacle.Type == CellType.Gear)
                    {
                        return true;
                    }

                    return hasFreeCellAhead;
                }

                hasFreeCellAhead = true;
                cursor += step;
            }

            // Không gặp obstacle đến biên nghĩa là có thể thoát khỏi grid.
            return true;
        }

        public bool IsNormalBlocked(Block block, List<Block> blockerChain)
        {
            if (block == null || block.IsRemoved || block.Type != CellType.Normal)
            {
                return false;
            }

            if (blockerChain != null)
            {
                blockerChain.Clear();
            }

            Vector2Int step = DirectionToOffset(block.Direction);
            Vector2Int cursor = block.Position + step;

            while (IsInsideGrid(cursor))
            {
                if (TryGetBlock(cursor, out Block obstacle))
                {
                    if (obstacle.Type == CellType.Gear)
                    {
                        return false;
                    }

                    CollectBlockingChain(cursor, step, blockerChain);
                    return true;
                }

                cursor += step;
            }

            return false;
        }

        public bool TryTapRotator(Block rotator)
        {
            if (rotator == null || rotator.IsRemoved || rotator.Type != CellType.Rotator)
            {
                return false;
            }

            List<Block> rotateBlocks = new List<Block>();
            CollectRotatorLinkedBlocks(rotator, rotateBlocks);
            if (rotateBlocks.Count == 0)
            {
                return false;
            }

            List<Vector2Int> targetPositions = new List<Vector2Int>();

            for (int stepCount = 1; stepCount <= 3; stepCount++)
            {
                targetPositions.Clear();
                if (!TryBuildRotatorTargets(rotator, rotateBlocks, stepCount, targetPositions))
                {
                    continue;
                }

                _isApplyingRotatorMove = true;
                for (int i = 0; i < rotateBlocks.Count; i++)
                {
                    rotateBlocks[i].TryMoveTo(targetPositions[i]);
                }

                rotator.NotifyRotated();

                _isApplyingRotatorMove = false;
                return true;
            }

            return false;
        }

        public bool CanAnyRotatorMove()
        {
            for (int i = 0; i < _allBlocks.Count; i++)
            {
                Block block = _allBlocks[i];
                if (block == null || block.IsRemoved || block.Type != CellType.Rotator)
                {
                    continue;
                }

                if (CanRotatorMove(block))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanRotatorMove(Block rotator)
        {
            if (rotator == null || rotator.IsRemoved || rotator.Type != CellType.Rotator)
            {
                return false;
            }

            List<Block> rotateBlocks = new List<Block>();
            CollectRotatorLinkedBlocks(rotator, rotateBlocks);
            if (rotateBlocks.Count == 0)
            {
                return false;
            }

            List<Vector2Int> targets = new List<Vector2Int>();
            return TryBuildRotatorTargets(rotator, rotateBlocks, 1, targets);
        }

        public void GetRotatorLinkedPositions(Block rotator, List<Vector2Int> output)
        {
            if (output == null)
            {
                return;
            }

            output.Clear();
            if (rotator == null || rotator.Type != CellType.Rotator)
            {
                return;
            }

            if (_rotatorLinkedNormalsByPosition.TryGetValue(rotator.Position, out List<RotatorLinkedNormal> linkedBlocks)
                && linkedBlocks != null
                && linkedBlocks.Count > 0)
            {
                for (int i = 0; i < linkedBlocks.Count; i++)
                {
                    RotatorLinkedNormal linkedNormal = linkedBlocks[i];
                    Block linkedBlock = linkedNormal.Block;
                    if (linkedBlock == null || linkedBlock.IsRemoved || linkedBlock.Type != CellType.Normal)
                    {
                        continue;
                    }

                    // Normal rời anchor thì link bị tắt.
                    if (linkedBlock.Position != linkedNormal.AnchorPosition)
                    {
                        continue;
                    }

                    output.Add(linkedBlock.Position);
                }
            }
        }

        public void GetRotatorLinkedBlocks(Block rotator, List<Block> output)
        {
            if (output == null)
            {
                return;
            }

            CollectRotatorLinkedBlocks(rotator, output);
        }

        private void CollectRotatorLinkedBlocks(Block rotator, List<Block> output)
        {
            if (output == null)
            {
                return;
            }

            output.Clear();
            if (rotator == null || rotator.IsRemoved || rotator.Type != CellType.Rotator)
            {
                return;
            }

            if (_rotatorLinkedNormalsByPosition.TryGetValue(rotator.Position, out List<RotatorLinkedNormal> linkedBlocks)
                && linkedBlocks != null
                && linkedBlocks.Count > 0)
            {
                for (int i = 0; i < linkedBlocks.Count; i++)
                {
                    RotatorLinkedNormal linkedNormal = linkedBlocks[i];
                    Block linkedBlock = linkedNormal.Block;
                    if (linkedBlock == null || linkedBlock.IsRemoved || linkedBlock.Type != CellType.Normal)
                    {
                        continue;
                    }

                    if (linkedBlock.Position != linkedNormal.AnchorPosition)
                    {
                        continue;
                    }

                    output.Add(linkedBlock);
                }
            }
        }

        private bool TryBuildRotatorTargets(
            Block rotator,
            List<Block> rotateBlocks,
            int stepCount,
            List<Vector2Int> targetPositions)
        {
            if (rotator == null || rotateBlocks == null || targetPositions == null)
            {
                return false;
            }

            targetPositions.Clear();
            for (int i = 0; i < rotateBlocks.Count; i++)
            {
                Block candidate = rotateBlocks[i];
                if (candidate == null || candidate.IsRemoved || candidate.Type != CellType.Normal)
                {
                    return false;
                }

                Vector2Int targetPos = RotateClockwise(candidate.Position, rotator.Position, stepCount);
                if (!IsInsideGrid(targetPos))
                {
                    return false;
                }

                if (targetPositions.Contains(targetPos))
                {
                    return false;
                }

                if (TryGetBlock(targetPos, out Block occupied) && !rotateBlocks.Contains(occupied))
                {
                    return false;
                }

                targetPositions.Add(targetPos);
            }

            return targetPositions.Count > 0;
        }

        public int RemainingRemovableCount()
        {
            int count = 0;
            for (int i = 0; i < _allBlocks.Count; i++)
            {
                Block block = _allBlocks[i];
                if (!block.IsRemoved && block.IsRemovable)
                {
                    count++;
                }
            }

            return count;
        }

        private void OnBlockRemoved(Block block)
        {
            if (block == null)
            {
                return;
            }

            if (_blocksByPosition.TryGetValue(block.Position, out Block current) && current == block)
            {
                _blocksByPosition.Remove(block.Position);
            }
        }

        private void OnBlockMoved(Block block, Vector2Int oldPos, Vector2Int newPos)
        {
            if (block == null)
            {
                return;
            }

            if (_blocksByPosition.TryGetValue(oldPos, out Block oldBlock) && oldBlock == block)
            {
                _blocksByPosition.Remove(oldPos);
            }

            _blocksByPosition[newPos] = block;

            if (block.Type != CellType.Normal)
            {
                return;
            }

            if (_isApplyingRotatorMove)
            {
                UpdateLinkedAnchorForNormal(block, newPos);
                return;
            }

            // Normal di chuyển do player tap thì cắt link vĩnh viễn.
            BreakLinksForNormal(block);
        }

        private void UpdateLinkedAnchorForNormal(Block normalBlock, Vector2Int newAnchor)
        {
            foreach (KeyValuePair<Vector2Int, List<RotatorLinkedNormal>> pair in _rotatorLinkedNormalsByPosition)
            {
                List<RotatorLinkedNormal> linked = pair.Value;
                if (linked == null)
                {
                    continue;
                }

                for (int i = 0; i < linked.Count; i++)
                {
                    if (linked[i].Block == normalBlock)
                    {
                        linked[i].AnchorPosition = newAnchor;
                        return;
                    }
                }
            }
        }

        private void BreakLinksForNormal(Block normalBlock)
        {
            if (normalBlock == null)
            {
                return;
            }

            List<Vector2Int> emptyRotators = null;

            foreach (KeyValuePair<Vector2Int, List<RotatorLinkedNormal>> pair in _rotatorLinkedNormalsByPosition)
            {
                List<RotatorLinkedNormal> linked = pair.Value;
                if (linked == null || linked.Count == 0)
                {
                    continue;
                }

                linked.RemoveAll(item => item.Block == normalBlock);
                if (linked.Count == 0)
                {
                    if (emptyRotators == null)
                    {
                        emptyRotators = new List<Vector2Int>();
                    }

                    emptyRotators.Add(pair.Key);
                }
            }

            if (emptyRotators == null)
            {
                return;
            }

            for (int i = 0; i < emptyRotators.Count; i++)
            {
                _rotatorLinkedNormalsByPosition.Remove(emptyRotators[i]);
            }
        }

        private bool IsInsideGrid(Vector2Int cell)
        {
            return cell.x >= _minX && cell.x <= _maxX && cell.y >= _minY && cell.y <= _maxY;
        }

        private void ConfigureGridBounds(int width, int height, IReadOnlyList<BlockData> blocks)
        {
            if (width <= 0 || height <= 0)
            {
                _minX = 0;
                _maxX = -1;
                _minY = 0;
                _maxY = -1;
                return;
            }

            bool isZeroBased = AreAllBlocksWithinZeroBasedBounds(width, height, blocks);
            if (isZeroBased)
            {
                _minX = 0;
                _maxX = Mathf.Max(0, Width - 1);
                _minY = 0;
                _maxY = Mathf.Max(0, Height - 1);
                return;
            }

            // Hỗ trợ level handcraft kiểu centered coordinates (ví dụ -1,0,1).
            _minX = -Mathf.FloorToInt(Width * 0.5f);
            _maxX = _minX + Width - 1;
            _minY = -Mathf.FloorToInt(Height * 0.5f);
            _maxY = _minY + Height - 1;
        }

        private static bool AreAllBlocksWithinZeroBasedBounds(int width, int height, IReadOnlyList<BlockData> blocks)
        {
            if (blocks == null)
            {
                return true;
            }

            int safeWidth = Mathf.Max(0, width);
            int safeHeight = Mathf.Max(0, height);
            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData blockData = blocks[i];
                if (blockData.position.x < 0 || blockData.position.x >= safeWidth)
                {
                    return false;
                }

                if (blockData.position.y < 0 || blockData.position.y >= safeHeight)
                {
                    return false;
                }
            }

            return true;
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

        private static Vector2Int RotateClockwise(Vector2Int source, Vector2Int pivot, int stepCount)
        {
            Vector2Int result = source;
            int steps = Mathf.Clamp(stepCount, 1, 3);
            for (int i = 0; i < steps; i++)
            {
                int relX = result.x - pivot.x;
                int relY = result.y - pivot.y;
                int newX = pivot.x + relY;
                int newY = pivot.y - relX;
                result = new Vector2Int(newX, newY);
            }

            return result;
        }

        private void CollectBlockingChain(Vector2Int start, Vector2Int step, List<Block> blockerChain)
        {
            if (blockerChain == null)
            {
                return;
            }

            Vector2Int cursor = start;
            while (IsInsideGrid(cursor))
            {
                if (!TryGetBlock(cursor, out Block block))
                {
                    break;
                }

                blockerChain.Add(block);
                cursor += step;
            }
        }
    }
}
