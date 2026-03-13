using System;
using UnityEngine;

namespace TapAway.Data
{
    [Serializable]
    public class BlockData
    {
        [Tooltip("Grid X position (column, 0-based)")]
        public int x;

        [Tooltip("Grid Y position (row, 0-based)")]
        public int y;

        [Tooltip("Type of this cell")]
        public CellType cellType;

        [Tooltip("Movement direction (only relevant for Normal blocks)")]
        public BlockDirection direction;
    }
}
