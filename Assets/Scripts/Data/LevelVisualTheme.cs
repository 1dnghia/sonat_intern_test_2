using System.Collections.Generic;
using UnityEngine;

namespace TapAway.Core
{
    [CreateAssetMenu(menuName = "TapAway/Level Visual Theme", fileName = "LevelVisualTheme")]
    public class LevelVisualTheme : ScriptableObject
    {
        [System.Serializable]
        public class TrailBinding
        {
            [Tooltip("Sprite của block (hiển thị trên SpriteRenderer prefab block).")]
            // Sprite block dùng cho binding visual.
            [SerializeField] private Sprite _blockSprite;

            [Tooltip("Màu trail tương ứng với block sprite ở trên.")]
            // Màu trail tương ứng sprite ở trên.
            [SerializeField] private Color _trailColor = Color.white;

            public Sprite BlockSprite => _blockSprite;
            public Color TrailColor => _trailColor;
        }

        [Header("Block Sprite <-> Trail Color")]
        // Danh sách map giữa sprite block và màu trail.
        [SerializeField] private List<TrailBinding> _trailBindings = new List<TrailBinding>();

        public int TrailBindingCount => _trailBindings != null ? _trailBindings.Count : 0;

        public bool TryGetBindingByIndex(int bindingIndex, out TrailBinding binding)
        {
            binding = null;
            if (_trailBindings == null || _trailBindings.Count == 0)
            {
                return false;
            }

            if (bindingIndex < 0 || bindingIndex >= _trailBindings.Count)
            {
                return false;
            }

            binding = _trailBindings[bindingIndex];
            return binding != null;
        }
    }
}
