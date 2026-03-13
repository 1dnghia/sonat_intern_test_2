using TapAway.Data;

namespace TapAway.Core
{
    /// <summary>
    /// Runtime representation of a single block on the grid.
    /// </summary>
    public class Block
    {
        public int X { get; set; }
        public int Y { get; set; }
        public CellType CellType { get; }
        public BlockDirection Direction { get; set; }

        /// <summary>Unique id within a level state (matches BlockData index).</summary>
        public int Id { get; }

        public Block(int id, int x, int y, CellType cellType, BlockDirection direction)
        {
            Id = id;
            X = x;
            Y = y;
            CellType = cellType;
            Direction = direction;
        }

        public Block Clone()
        {
            return new Block(Id, X, Y, CellType, Direction);
        }
    }
}
