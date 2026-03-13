using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapAway.Data
{
    [Serializable]
    public class DifficultyRange
    {
        public int levelFrom;
        public int levelTo;
        [Range(3, 7)] public int gridSize;
        [Range(0f, 1f)] public float blockDensityMin;
        [Range(0f, 1f)] public float blockDensityMax;
        [Range(0f, 1f)] public float gearRatio;
        [Range(0f, 1f)] public float rotatorRatio;

        [Tooltip("Extra moves on top of optimalMoves. 0 = unlimited.")]
        public int movesBuffer;

        public float targetScoreMin;
        public float targetScoreMax;
        public int minChainDepth;
        public int maxChainDepth;
        public int maxFreeBlocksAtStart;
    }

    /// <summary>
    /// Configuration asset that drives the procedural level generator.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyConfig", menuName = "TapAway/DifficultyConfig")]
    public class DifficultyConfig : ScriptableObject
    {
        [Tooltip("Ordered list of difficulty ranges (by level index)")]
        public List<DifficultyRange> ranges = new List<DifficultyRange>();

        /// <summary>Returns the DifficultyRange for the given level index.</summary>
        public DifficultyRange GetRange(int levelIndex)
        {
            foreach (var r in ranges)
            {
                if (levelIndex >= r.levelFrom && levelIndex <= r.levelTo)
                    return r;
            }
            return ranges.Count > 0 ? ranges[ranges.Count - 1] : null;
        }
    }
}
