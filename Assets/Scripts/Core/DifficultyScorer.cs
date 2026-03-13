using System.Linq;
using TapAway.Data;
using UnityEngine;

namespace TapAway.Core
{
    /// <summary>
    /// Computes the Difficulty Score for a level based on solver metrics and grid parameters.
    /// Formula from level-generator.instructions.md.
    /// </summary>
    public static class DifficultyScorer
    {
        /// <summary>
        /// Calculates the difficulty score for a level.
        /// </summary>
        public static float Calculate(
            GridSystem grid,
            SolverResult solverResult)
        {
            if (!solverResult.IsSolvable) return 0f;

            int gridSize = grid.GridSize;
            int blockCount = grid.Blocks.Count(b => b.CellType == CellType.Normal);
            int gearCount = grid.Blocks.Count(b => b.CellType == CellType.Gear);
            int rotatorCount = grid.Blocks.Count(b => b.CellType == CellType.Rotator);
            int optimalMoves = solverResult.OptimalMoves;
            int maxChainDepth = solverResult.MaxChainDepth;
            float branchingFactor = solverResult.BranchingFactor;
            int freeAtStart = solverResult.FreeBlocksAtStart;
            int lockedBlocks = solverResult.LockedBlocks;

            // normalizedMoves: clamp(optimalMoves / (blockCount * 2), 0, 1)
            float normalizedMoves = blockCount > 0
                ? Mathf.Clamp01((float)optimalMoves / (blockCount * 2))
                : 0f;

            // normalizedGridSize: sqrt((gridSize - 3) / (7 - 3))
            float normalizedGridSize = Mathf.Sqrt((gridSize - 3f) / 4f);

            // blockDensity: blockCount / gridSize²
            float blockDensity = (float)blockCount / (gridSize * gridSize);

            // lockedBlockRatio: lockedBlocks / blockCount
            float lockedBlockRatio = blockCount > 0
                ? Mathf.Clamp01((float)lockedBlocks / blockCount)
                : 0f;

            // normalizedChainDepth: clamp(maxChainDepth / blockCount, 0, 1)
            float normalizedChainDepth = blockCount > 0
                ? Mathf.Clamp01((float)maxChainDepth / blockCount)
                : 0f;

            // normalizedBranching: clamp(log(branchingFactor + 1) / log(7), 0, 1)
            float normalizedBranching = Mathf.Clamp01(
                Mathf.Log(branchingFactor + 1f) / Mathf.Log(7f));

            // log(gearCount + 1)
            float gearLog = Mathf.Log(gearCount + 1f);

            // log(rotatorCount + 1)
            float rotatorLog = Mathf.Log(rotatorCount + 1f);

            // normalizedFreeBlock: clamp(freeAtStart / blockCount, 0, 1)
            float normalizedFreeBlock = blockCount > 0
                ? Mathf.Clamp01((float)freeAtStart / blockCount)
                : 0f;

            float score =
                normalizedMoves      * 4.0f +
                normalizedGridSize   * 3.0f +
                blockDensity         * 3.0f +
                lockedBlockRatio     * 2.0f +
                normalizedChainDepth * 3.0f +
                normalizedBranching  * 3.0f +
                gearLog              * 2.0f +
                rotatorLog           * 3.0f -
                normalizedFreeBlock  * 3.0f;

            return score;
        }
    }
}
