using System.Collections.Generic;
using UnityEngine;

namespace TapAway.Data
{
    /// <summary>
    /// Stores the layout data for a single level.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_XX", menuName = "TapAway/LevelData")]
    public class LevelData : ScriptableObject
    {
        [Tooltip("Level index (1-based)")]
        public int levelIndex;

        [Tooltip("Size of the square grid (e.g. 3 = 3x3)")]
        [Range(3, 7)]
        public int gridSize = 3;

        [Tooltip("Maximum number of moves allowed. 0 = unlimited (tutorial)")]
        public int movesLimit;

        [Tooltip("All blocks in the level (Normal, Gear, Rotator)")]
        public List<BlockData> blocks = new List<BlockData>();
    }
}
