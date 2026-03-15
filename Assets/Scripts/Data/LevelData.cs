using System.Collections.Generic;
using UnityEngine;

namespace TapAway.Core
{
    [CreateAssetMenu(menuName = "TapAway/Level Data", fileName = "Level_001")]
    public class LevelData : ScriptableObject
    {
        // Chiều rộng grid (số cột).
        [Min(1)] public int width = 6;

        // Chiều cao grid (số hàng).
        [Min(1)] public int height = 6;

        // Giới hạn số lượt chơi của level (0 hoặc nhỏ hơn nghĩa là không giới hạn nếu logic game hỗ trợ).
        [Min(1)] public int moveLimit = 20;

        // Danh sách block xuất hiện trong level (vị trí, loại block, hướng).
        public List<BlockData> blocks = new List<BlockData>();

        [Tooltip("Theme visual theo map: sprite block và màu trail.")]
        public LevelVisualTheme visualTheme;

        [Tooltip("Level này đang dùng Trail Binding nào trong LevelVisualTheme (0-based).")]
        [Min(0)] public int trailBindingIndex = 0;
    }
}
