using System;
using UnityEngine;

namespace TapAway.Core
{
    [Serializable]
    public class BlockData
    {
        public int id;
        public Vector2Int position;
        public BlockDirection direction = BlockDirection.Up;
        public CellType cellType = CellType.Normal;
    }
}
