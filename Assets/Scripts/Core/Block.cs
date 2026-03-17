using System;
using UnityEngine;

namespace TapAway.Core
{
    public enum BlockRemoveReason
    {
        ExitGrid = 0,
        HitGear = 1,
        Bomb = 2,
        Other = 3,
    }

    [Serializable]
    public class Block
    {
        public int Id { get; }
        public Vector2Int Position { get; private set; }
        public CellType Type { get; }
        public bool IsRemoved { get; private set; }
        public BlockDirection Direction { get; private set; }
        public BlockRemoveReason RemoveReason { get; private set; }

        public event Action<Block> Removed;
        public event Action<Block> Rotated;
        public event Action<Block, Vector2Int, Vector2Int> Moved;

        public Block(BlockData data)
        {
            Id = data.id;
            Position = data.position;
            Direction = data.direction;
            Type = data.cellType;
            IsRemoved = false;
            RemoveReason = BlockRemoveReason.Other;
        }

        public bool IsRemovable => Type == CellType.Normal || Type == CellType.Rotator;

        public bool TryRemove(BlockRemoveReason reason = BlockRemoveReason.Other)
        {
            if (IsRemoved || !IsRemovable)
            {
                return false;
            }

            IsRemoved = true;
            RemoveReason = reason;
            Removed?.Invoke(this);
            return true;
        }

        public bool TryRotateClockwise()
        {
            if (IsRemoved || Type != CellType.Rotator)
            {
                return false;
            }

            Direction = (BlockDirection)(((int)Direction + 1) % 4);
            Rotated?.Invoke(this);
            return true;
        }

        public bool NotifyRotated()
        {
            if (IsRemoved || Type != CellType.Rotator)
            {
                return false;
            }

            Rotated?.Invoke(this);
            return true;
        }

        public bool TryMoveTo(Vector2Int newPosition)
        {
            if (IsRemoved || Position == newPosition)
            {
                return false;
            }

            Vector2Int oldPosition = Position;
            Position = newPosition;
            Moved?.Invoke(this, oldPosition, newPosition);
            return true;
        }
    }
}
