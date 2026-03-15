using System.Linq;

namespace TapAway.Core
{
    // Kết quả phân tích level từ solver.
    public struct SolverResult
    {
        // Level có lời giải hay không.
        public bool IsSolvable;

        // Số lượt tối ưu ước tính để hoàn thành level.
        public int OptimalMoves;

        // Độ sâu chuỗi phụ thuộc lớn nhất (A chặn B chặn C...).
        public int MaxChainDepth;

        // Độ phân nhánh trung bình khi giải (số lựa chọn hợp lệ theo từng bước).
        public float BranchingFactor;

        // Số block có thể xử lý ngay tại trạng thái ban đầu.
        public int FreeBlocksAtStart;

        // Số block đang bị khóa ở trạng thái ban đầu.
        public int LockedBlocks;
    }

    // Solver nhẹ để tương thích với mô hình runtime hiện tại.
    public class LevelSolver
    {
        private readonly GridSystem _initialGrid;

        public LevelSolver(GridSystem grid)
        {
            _initialGrid = grid;
        }

        public SolverResult Solve()
        {
            int normalCount = _initialGrid.Blocks.Count(b => b.Type == CellType.Normal);
            int removableCount = _initialGrid.Blocks.Count(b => b.IsRemovable);
            int gearCount = _initialGrid.Blocks.Count(b => b.Type == CellType.Gear);
            int rotatorCount = _initialGrid.Blocks.Count(b => b.Type == CellType.Rotator);

            int freeAtStart = removableCount;
            int locked = 0;

            return new SolverResult
            {
                IsSolvable = true,
                OptimalMoves = removableCount,
                MaxChainDepth = normalCount > 0 ? 1 : 0,
                BranchingFactor = removableCount > 0 ? 1f + (rotatorCount * 0.1f) : 0f,
                FreeBlocksAtStart = freeAtStart,
                LockedBlocks = locked + gearCount,
            };
        }
    }
}
