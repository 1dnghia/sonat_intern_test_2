using System.Collections.Generic;
using System.Linq;
using TapAway.Data;

namespace TapAway.Core
{
    /// <summary>
    /// Pure-logic grid for the Tap Away game.
    /// Manages block positions and game-rule evaluation without any Unity/presentation dependency.
    /// </summary>
    public class GridSystem
    {
        // ── Public State ──────────────────────────────────────
        public int GridSize { get; }

        /// <summary>All blocks currently on the grid (Normal, Gear, Rotator).</summary>
        public IReadOnlyList<Block> Blocks => _blocks;

        // ── Private Fields ────────────────────────────────────
        private readonly List<Block> _blocks;

        // ── Constructor ───────────────────────────────────────
        public GridSystem(int gridSize, IEnumerable<Block> blocks)
        {
            GridSize = gridSize;
            _blocks = new List<Block>(blocks);
        }

        // ── Query ─────────────────────────────────────────────

        /// <summary>Returns the block at (x,y) or null if empty.</summary>
        public Block GetBlock(int x, int y)
        {
            foreach (var b in _blocks)
            {
                if (b.X == x && b.Y == y) return b;
            }
            return null;
        }

        public bool IsInsideGrid(int x, int y) =>
            x >= 0 && x < GridSize && y >= 0 && y < GridSize;

        public bool IsCellEmpty(int x, int y) => GetBlock(x, y) == null;

        // ── Movement ──────────────────────────────────────────

        public enum TapResult
        {
            Moved,         // Normal block slid out and was removed
            Blocked,       // Normal block was blocked; could not move
            Rotated,       // Rotator and connected blocks rotated OK
            RotateBlocked, // Rotator rotation was blocked
            Destroyed,     // Normal block hit a Gear and was destroyed in-place
            Invalid,       // Tapped Gear, empty cell, etc. — no move counted
        }

        /// <summary>
        /// Attempts to tap the block at (x, y).
        /// Returns TapResult and, for blocked moves, the chain of blocking blocks.
        /// </summary>
        public TapResult TapBlock(int x, int y, out List<Block> blockingChain)
        {
            blockingChain = new List<Block>();
            var target = GetBlock(x, y);

            if (target == null || target.CellType == CellType.Gear)
                return TapResult.Invalid;

            if (target.CellType == CellType.Rotator)
                return TryRotate(target, out blockingChain);

            // Normal block
            return TrySlide(target, out blockingChain);
        }

        // ── Normal Block Slide ────────────────────────────────

        private TapResult TrySlide(Block block, out List<Block> blockingChain)
        {
            blockingChain = new List<Block>();
            DirectionToStep(block.Direction, out int dx, out int dy);

            int nx = block.X + dx;
            int ny = block.Y + dy;

            while (IsInsideGrid(nx, ny))
            {
                var obstacle = GetBlock(nx, ny);
                if (obstacle != null)
                {
                    if (obstacle.CellType == CellType.Gear)
                    {
                        // Block is destroyed by gear
                        _blocks.Remove(block);
                        return TapResult.Destroyed;
                    }

                    // Blocked by Normal or Rotator — build domino chain
                    BuildBlockingChain(nx, ny, dx, dy, blockingChain);
                    return TapResult.Blocked;
                }
                nx += dx;
                ny += dy;
            }

            // Path is clear to edge → fly out
            _blocks.Remove(block);
            return TapResult.Moved;
        }

        /// <summary>
        /// Starting from (startX, startY), follow direction (dx,dy) and collect all
        /// consecutive blocks as the domino blocking chain.
        /// </summary>
        private void BuildBlockingChain(int startX, int startY, int dx, int dy, List<Block> chain)
        {
            int cx = startX;
            int cy = startY;

            while (IsInsideGrid(cx, cy))
            {
                var b = GetBlock(cx, cy);
                if (b == null) break;

                if (b.CellType == CellType.Gear) break; // Gear doesn't shake

                chain.Add(b);
                cx += dx;
                cy += dy;
            }
        }

        // ── Rotator ───────────────────────────────────────────

        private TapResult TryRotate(Block rotator, out List<Block> blockingChain)
        {
            blockingChain = new List<Block>();
            int rx = rotator.X;
            int ry = rotator.Y;

            var connected = GetConnectedToRotator(rx, ry);

            // Compute new positions for all connected blocks
            var newPositions = new List<(Block block, int nx, int ny)>();
            foreach (var b in connected)
            {
                int relX = b.X - rx;
                int relY = b.Y - ry;
                // CW 90° rotation: newX = rx + relY, newY = ry - relX
                int nX = rx + relY;
                int nY = ry - relX;
                newPositions.Add((b, nX, nY));
            }

            // Check all new positions are inside grid and empty (excluding current positions of involved blocks)
            var occupiedByMoving = new HashSet<(int, int)>(connected.Select(b => (b.X, b.Y)));

            foreach (var (b, nX, nY) in newPositions)
            {
                if (!IsInsideGrid(nX, nY))
                    return TapResult.RotateBlocked;

                var existing = GetBlock(nX, nY);
                if (existing != null && !occupiedByMoving.Contains((nX, nY)))
                {
                    blockingChain.Add(existing);
                    return TapResult.RotateBlocked;
                }
            }

            // Apply rotation
            foreach (var (b, nX, nY) in newPositions)
            {
                b.X = nX;
                b.Y = nY;
                // Direction is intentionally preserved per spec
            }

            return TapResult.Rotated;
        }

        /// <summary>Returns all Normal blocks within 8 neighbours of (rx, ry).</summary>
        public List<Block> GetConnectedToRotator(int rx, int ry)
        {
            var result = new List<Block>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var b = GetBlock(rx + dx, ry + dy);
                    if (b != null && b.CellType == CellType.Normal)
                        result.Add(b);
                }
            }
            return result;
        }

        // ── Win Check ─────────────────────────────────────────

        /// <summary>True when all Normal blocks have been removed.</summary>
        public bool IsWon()
        {
            foreach (var b in _blocks)
            {
                if (b.CellType == CellType.Normal) return false;
            }
            return true;
        }

        // ── Clone for Solver ─────────────────────────────────

        public GridSystem Clone()
        {
            return new GridSystem(GridSize, _blocks.Select(b => b.Clone()));
        }

        // ── Helpers ───────────────────────────────────────────

        public static void DirectionToStep(BlockDirection dir, out int dx, out int dy)
        {
            switch (dir)
            {
                case BlockDirection.Up:    dx = 0;  dy = 1;  return;
                case BlockDirection.Down:  dx = 0;  dy = -1; return;
                case BlockDirection.Left:  dx = -1; dy = 0;  return;
                default:                   dx = 1;  dy = 0;  return; // Right
            }
        }
    }
}
