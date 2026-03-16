using System.Collections.Generic;
using System.Linq;
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
        [Min(0)] public int moveLimit = 20;

        // Danh sách block xuất hiện trong level (vị trí, loại block, hướng).
        public List<BlockData> blocks = new List<BlockData>();

        [Tooltip("Theme visual theo map: sprite block và màu trail.")]
        public LevelVisualTheme visualTheme;

        [Tooltip("Level này đang dùng Trail Binding nào trong LevelVisualTheme (0-based).")]
        [Min(0)] public int trailBindingIndex = 0;

        public IEnumerable<Vector2Int> EnumerateMapPositions()
        {
            int safeWidth = Mathf.Max(1, width);
            int safeHeight = Mathf.Max(1, height);

            for (int y = 0; y < safeHeight; y++)
            {
                for (int x = 0; x < safeWidth; x++)
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }

        public void CreateGridTemplate(bool keepExistingCells)
        {
            if (blocks == null)
            {
                blocks = new List<BlockData>();
            }

            Dictionary<Vector2Int, BlockData> existingByPosition = keepExistingCells
                ? blocks
                    .Where(IsInsideMap)
                    .GroupBy(b => b.position)
                    .ToDictionary(g => g.Key, g => g.First())
                : new Dictionary<Vector2Int, BlockData>();

            List<BlockData> rebuilt = new List<BlockData>();
            int nextId = 1;

            foreach (Vector2Int cell in EnumerateMapPositions())
            {
                if (existingByPosition.TryGetValue(cell, out BlockData existing))
                {
                    existing.id = nextId++;
                    if (existing.cellType != CellType.Rotator && existing.rotatorLinkedNormals != null)
                    {
                        existing.rotatorLinkedNormals.Clear();
                    }
                    rebuilt.Add(existing);
                    continue;
                }

                rebuilt.Add(new BlockData
                {
                    id = nextId++,
                    position = cell,
                    direction = BlockDirection.Up,
                    cellType = CellType.Empty,
                    rotatorLinkedNormals = new List<Vector2Int>(),
                });
            }

            blocks = rebuilt;
        }

        public void NormalizeToMapBounds()
        {
            if (blocks == null)
            {
                blocks = new List<BlockData>();
                return;
            }

            blocks = blocks
                .Where(IsInsideMap)
                .GroupBy(b => b.position)
                .Select(g => g.First())
                .ToList();

            for (int i = 0; i < blocks.Count; i++)
            {
                blocks[i].id = i + 1;

                if (blocks[i].cellType != CellType.Rotator)
                {
                    if (blocks[i].rotatorLinkedNormals != null)
                    {
                        blocks[i].rotatorLinkedNormals.Clear();
                    }

                    continue;
                }

                if (blocks[i].rotatorLinkedNormals == null)
                {
                    blocks[i].rotatorLinkedNormals = new List<Vector2Int>();
                    continue;
                }

                Vector2Int rotatorPosition = blocks[i].position;

                blocks[i].rotatorLinkedNormals = blocks[i].rotatorLinkedNormals
                    .Where(pos => pos.x >= 0
                        && pos.x < Mathf.Max(1, width)
                        && pos.y >= 0
                        && pos.y < Mathf.Max(1, height)
                        && Mathf.Abs(pos.x - rotatorPosition.x) <= 1
                        && Mathf.Abs(pos.y - rotatorPosition.y) <= 1
                        && !(pos.x == rotatorPosition.x && pos.y == rotatorPosition.y)
                        && HasNormalAt(pos))
                    .Distinct()
                    .ToList();
            }
        }

        private bool HasNormalAt(Vector2Int position)
        {
            if (blocks == null)
            {
                return false;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                BlockData block = blocks[i];
                if (block == null)
                {
                    continue;
                }

                if (block.position == position && block.cellType == CellType.Normal)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsInsideMap(BlockData block)
        {
            if (block == null)
            {
                return false;
            }

            return block.position.x >= 0
                && block.position.x < Mathf.Max(1, width)
                && block.position.y >= 0
                && block.position.y < Mathf.Max(1, height);
        }
    }
}
