using System;
using System.Collections.Generic;
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
        public List<Vector2Int> rotatorLinkedNormals = new List<Vector2Int>();
    }
}
