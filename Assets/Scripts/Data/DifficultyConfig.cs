using UnityEngine;

namespace TapAway.Core
{
    [CreateAssetMenu(menuName = "TapAway/Difficulty Config", fileName = "DifficultyConfig")]
    public class DifficultyConfig : ScriptableObject
    {
        // Chiều rộng grid nhỏ nhất khi generator tạo level.
        [Min(2)] public int minWidth = 4;

        // Chiều rộng grid lớn nhất khi generator tạo level.
        [Min(2)] public int maxWidth = 8;

        // Chiều cao grid nhỏ nhất khi generator tạo level.
        [Min(2)] public int minHeight = 4;

        // Chiều cao grid lớn nhất khi generator tạo level.
        [Min(2)] public int maxHeight = 8;

        // Số block nhỏ nhất trong một level được sinh.
        [Min(1)] public int minBlocks = 8;

        // Số block lớn nhất trong một level được sinh.
        [Min(1)] public int maxBlocks = 40;

        // Số lượt đi tối thiểu được sinh cho level.
        [Min(1)] public int minMoves = 10;

        // Số lượt đi tối đa được sinh cho level.
        [Min(1)] public int maxMoves = 40;
    }
}
