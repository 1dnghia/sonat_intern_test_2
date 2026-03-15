using System.Collections.Generic;
using UnityEngine;

namespace TapAway.Core
{
    public class GridSystem
    {
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

        private readonly Dictionary<Vector2Int, Block> _blocksByPosition = new Dictionary<Vector2Int, Block>();
        private readonly List<Block> _allBlocks = new List<Block>();

        public IReadOnlyList<Block> Blocks => _allBlocks;

        public void Build(LevelData levelData)
        {
            _blocksByPosition.Clear();
            _allBlocks.Clear();

            if (levelData == null)
            {
                Width = 0;
                Height = 0;
                return;
            }

            Width = levelData.width;
            Height = levelData.height;

            foreach (BlockData blockData in levelData.blocks)
            {
                if (_blocksByPosition.ContainsKey(blockData.position))
                {
                    continue;
                }

                Block block = new Block(blockData);
                block.Removed += OnBlockRemoved;
                block.Moved += OnBlockMoved;
                _blocksByPosition.Add(blockData.position, block);
                _allBlocks.Add(block);
            }
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

            while (IsInsideGrid(cursor))
            {
                if (TryGetBlock(cursor, out Block obstacle))
                {
                    if (obstacle.Type == CellType.Gear)
                    {
                        // Block bị cắt khi hướng đi đâm thẳng vào Gear.
                        block.TryRemove();
                        return true;
                    }

                    CollectBlockingChain(cursor, step, blockerChain);
                    return false;
                }

                cursor += step;
            }

            block.TryRemove();
            return true;
        }

        public bool TryTapRotator(Block rotator)
        {
            if (rotator == null || rotator.IsRemoved || rotator.Type != CellType.Rotator)
            {
                return false;
            }

            List<Block> rotateBlocks = new List<Block>();
            List<Vector2Int> targetPositions = new List<Vector2Int>();

            for (int i = 0; i < ADJACENT_OFFSETS.Length; i++)
            {
                Vector2Int sourcePos = rotator.Position + ADJACENT_OFFSETS[i];
                if (!TryGetBlock(sourcePos, out Block candidate))
                {
                    continue;
                }

                if (candidate.Type != CellType.Normal)
                {
                    continue;
                }

                Vector2Int targetPos = RotateClockwise(sourcePos, rotator.Position);
                if (!IsInsideGrid(targetPos))
                {
                    return false;
                }

                if (TryGetBlock(targetPos, out _))
                {
                    return false;
                }

                rotateBlocks.Add(candidate);
                targetPositions.Add(targetPos);
            }

            if (rotateBlocks.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < rotateBlocks.Count; i++)
            {
                rotateBlocks[i].TryMoveTo(targetPositions[i]);
            }

            rotator.TryRotateClockwise();
            return true;
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
        }

        private bool IsInsideGrid(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
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
            int relX = source.x - pivot.x;
            int relY = source.y - pivot.y;
            int newX = pivot.x + relY;
            int newY = pivot.y - relX;
            return new Vector2Int(newX, newY);
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
