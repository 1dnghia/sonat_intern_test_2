using System.Collections.Generic;
using System.Linq;
using System.Text;
using TapAway.Data;

namespace TapAway.Core
{
    /// <summary>
    /// Solver metrics returned after a BFS run.
    /// </summary>
    public struct SolverResult
    {
        public bool IsSolvable;
        public int OptimalMoves;
        public int MaxChainDepth;
        public float BranchingFactor;
        public int FreeBlocksAtStart;
        public int LockedBlocks;
    }

    /// <summary>
    /// BFS solver for the Tap Away grid.
    /// Used by the Level Generator to validate solvability and collect difficulty metrics.
    /// </summary>
    public class LevelSolver
    {
        private readonly GridSystem _initialGrid;

        public LevelSolver(GridSystem grid)
        {
            _initialGrid = grid;
        }

        /// <summary>
        /// Runs BFS and returns solver metrics.
        /// </summary>
        public SolverResult Solve()
        {
            int normalCount = _initialGrid.Blocks.Count(b => b.CellType == CellType.Normal);
            if (normalCount == 0)
            {
                return new SolverResult
                {
                    IsSolvable = true,
                    OptimalMoves = 0,
                    MaxChainDepth = 0,
                    BranchingFactor = 0f,
                    FreeBlocksAtStart = 0,
                    LockedBlocks = 0,
                };
            }

            int freeAtStart = CountFreeBlocks(_initialGrid);

            var visited = new HashSet<string>();
            var queue = new Queue<(GridSystem grid, int depth)>();
            string initState = EncodeState(_initialGrid);
            visited.Add(initState);
            queue.Enqueue((_initialGrid.Clone(), 0));

            int optimalMoves = -1;
            long totalBranches = 0;
            long totalNodes = 0;
            int maxChainDepth = 0;

            while (queue.Count > 0)
            {
                var (current, depth) = queue.Dequeue();
                totalNodes++;

                var moves = GetPlayableMoves(current);
                totalBranches += moves.Count;

                if (optimalMoves >= 0 && depth >= optimalMoves)
                    continue;

                foreach (var (x, y) in moves)
                {
                    var next = current.Clone();
                    var result = next.TapBlock(x, y, out var chain);

                    if (result == GridSystem.TapResult.Invalid || result == GridSystem.TapResult.Blocked || result == GridSystem.TapResult.RotateBlocked)
                        continue;

                    int newDepth = depth + 1;

                    if (next.IsWon())
                    {
                        if (optimalMoves < 0 || newDepth < optimalMoves)
                            optimalMoves = newDepth;
                        continue;
                    }

                    string state = EncodeState(next);
                    if (!visited.Contains(state))
                    {
                        visited.Add(state);
                        queue.Enqueue((next, newDepth));
                    }
                }
            }

            if (optimalMoves < 0)
            {
                return new SolverResult { IsSolvable = false };
            }

            // Chain depth: max contiguous blocks blocked in one direction
            maxChainDepth = ComputeMaxChainDepth(_initialGrid);
            int lockedBlocks = normalCount - freeAtStart;

            float branchingFactor = totalNodes > 0
                ? (float)totalBranches / totalNodes
                : 1f;

            return new SolverResult
            {
                IsSolvable = true,
                OptimalMoves = optimalMoves,
                MaxChainDepth = maxChainDepth,
                BranchingFactor = branchingFactor,
                FreeBlocksAtStart = freeAtStart,
                LockedBlocks = lockedBlocks,
            };
        }

        // ── Helpers ───────────────────────────────────────────

        /// <summary>Returns (x, y) of every tappable block (Normal + Rotator).</summary>
        private static List<(int x, int y)> GetPlayableMoves(GridSystem grid)
        {
            var moves = new List<(int, int)>();
            foreach (var b in grid.Blocks)
            {
                if (b.CellType == CellType.Normal || b.CellType == CellType.Rotator)
                    moves.Add((b.X, b.Y));
            }
            return moves;
        }

        private static int CountFreeBlocks(GridSystem grid)
        {
            int count = 0;
            foreach (var b in grid.Blocks)
            {
                if (b.CellType != CellType.Normal) continue;
                if (IsBlockFree(grid, b)) count++;
            }
            return count;
        }

        private static bool IsBlockFree(GridSystem grid, Block block)
        {
            GridSystem.DirectionToStep(block.Direction, out int dx, out int dy);
            int nx = block.X + dx;
            int ny = block.Y + dy;

            while (grid.IsInsideGrid(nx, ny))
            {
                if (grid.GetBlock(nx, ny) != null)
                    return false;
                nx += dx;
                ny += dy;
            }
            return true;
        }

        private static int ComputeMaxChainDepth(GridSystem grid)
        {
            int max = 0;
            foreach (var a in grid.Blocks)
            {
                if (a.CellType != CellType.Normal) continue;
                int depth = ChainDepthFrom(grid, a, new HashSet<int>());
                if (depth > max) max = depth;
            }
            return max;
        }

        private static int ChainDepthFrom(GridSystem grid, Block block, HashSet<int> visited)
        {
            if (!visited.Add(block.Id)) return 0;

            GridSystem.DirectionToStep(block.Direction, out int dx, out int dy);
            int nx = block.X + dx;
            int ny = block.Y + dy;

            while (grid.IsInsideGrid(nx, ny))
            {
                var obstacle = grid.GetBlock(nx, ny);
                if (obstacle != null)
                {
                    if (obstacle.CellType == CellType.Normal)
                        return 1 + ChainDepthFrom(grid, obstacle, visited);
                    return 1; // blocked by Rotator or Gear
                }
                nx += dx;
                ny += dy;
            }
            return 0;
        }

        private static string EncodeState(GridSystem grid)
        {
            var sb = new StringBuilder();
            sb.Append(grid.GridSize);
            var sorted = grid.Blocks
                .Where(b => b.CellType == CellType.Normal || b.CellType == CellType.Rotator)
                .OrderBy(b => b.X * 100 + b.Y);
            foreach (var b in sorted)
            {
                sb.Append('|');
                sb.Append(b.X);
                sb.Append(',');
                sb.Append(b.Y);
                sb.Append(',');
                sb.Append((int)b.Direction);
                sb.Append(',');
                sb.Append((int)b.CellType);
            }
            return sb.ToString();
        }
    }
}
