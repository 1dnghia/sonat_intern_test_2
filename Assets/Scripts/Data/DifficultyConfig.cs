using UnityEngine;
using UnityEngine.Serialization;

namespace TapAway.Core
{
    [CreateAssetMenu(menuName = "TapAway/Difficulty Config", fileName = "DifficultyConfig")]
    public class DifficultyConfig : ScriptableObject
    {
        // Kích thước map vuông nhỏ nhất khi generator tạo level.
        [Min(2)] public int minGridSize = 4;

        // Kích thước map vuông lớn nhất khi generator tạo level.
        [Min(2)] public int maxGridSize = 8;

        // Mật độ block thấp nhất trên tổng số ô map.
        [Range(0.05f, 1f)] public float minBlockDensity = 0.4f;

        // Mật độ block cao nhất trên tổng số ô map.
        [Range(0.05f, 1f)] public float maxBlockDensity = 0.6f;

        // Tỷ lệ Gear trong tổng block.
        [Range(0f, 1f)] public float gearRatio = 0.15f;

        // Tỷ lệ Rotator trong tổng block.
        [Range(0f, 1f)] public float rotatorRatio = 0.08f;

        [Header("Block Appearance")]
        // Bật để ép generator luôn có ít nhất 1 Gear.
        // Tắt thì số Gear sinh theo gearRatio.
        public bool allowGear = true;

        // Bật để ép generator luôn có ít nhất 1 Rotator.
        // Tắt thì số Rotator sinh theo rotatorRatio.
        public bool allowRotator = true;

        // Số move đệm tối thiểu cộng thêm sau khi ước tính base moves.
        [Min(0)] public int minMovesBuffer = 2;

        // Số move đệm tối đa cộng thêm sau khi ước tính base moves.
        [Min(0)] public int maxMovesBuffer = 5;

        [Header("Legacy (Auto-Migrate)")]
        [SerializeField, HideInInspector, FormerlySerializedAs("minWidth")]
        private int _legacyMinWidth = 4;
        [SerializeField, HideInInspector, FormerlySerializedAs("maxWidth")]
        private int _legacyMaxWidth = 8;
        [SerializeField, HideInInspector, FormerlySerializedAs("minHeight")]
        private int _legacyMinHeight = 4;
        [SerializeField, HideInInspector, FormerlySerializedAs("maxHeight")]
        private int _legacyMaxHeight = 8;
        [SerializeField, HideInInspector, FormerlySerializedAs("minBlocks")]
        private int _legacyMinBlocks = 8;
        [SerializeField, HideInInspector, FormerlySerializedAs("maxBlocks")]
        private int _legacyMaxBlocks = 40;
        [SerializeField, HideInInspector, FormerlySerializedAs("minMoves")]
        private int _legacyMinMoves = 10;
        [SerializeField, HideInInspector, FormerlySerializedAs("maxMoves")]
        private int _legacyMaxMoves = 40;
        [SerializeField, HideInInspector]
        private bool _migrated;

        private void OnValidate()
        {
            if (!_migrated)
            {
                int legacyMinSize = Mathf.Min(_legacyMinWidth, _legacyMinHeight);
                int legacyMaxSize = Mathf.Max(_legacyMaxWidth, _legacyMaxHeight);
                minGridSize = Mathf.Clamp(legacyMinSize, 2, 9);
                maxGridSize = Mathf.Clamp(legacyMaxSize, minGridSize, 9);

                int minCellCount = minGridSize * minGridSize;
                int maxCellCount = Mathf.Max(minCellCount, maxGridSize * maxGridSize);
                minBlockDensity = Mathf.Clamp01((float)_legacyMinBlocks / Mathf.Max(1, minCellCount));
                maxBlockDensity = Mathf.Clamp01((float)_legacyMaxBlocks / Mathf.Max(1, maxCellCount));

                int baseMinMoves = Mathf.Max(1, _legacyMinMoves - _legacyMinBlocks);
                int baseMaxMoves = Mathf.Max(baseMinMoves, _legacyMaxMoves - _legacyMaxBlocks);
                minMovesBuffer = Mathf.Max(0, baseMinMoves);
                maxMovesBuffer = Mathf.Max(minMovesBuffer, baseMaxMoves);
                _migrated = true;
            }

            minGridSize = Mathf.Clamp(minGridSize, 2, 9);
            maxGridSize = Mathf.Clamp(maxGridSize, minGridSize, 9);
            minBlockDensity = Mathf.Clamp(minBlockDensity, 0.05f, 1f);
            maxBlockDensity = Mathf.Clamp(maxBlockDensity, minBlockDensity, 1f);

            gearRatio = Mathf.Clamp01(gearRatio);
            rotatorRatio = Mathf.Clamp01(rotatorRatio);

            float ratioSum = gearRatio + rotatorRatio;
            if (ratioSum > 1f)
            {
                float inv = 1f / ratioSum;
                gearRatio *= inv;
                rotatorRatio *= inv;
            }

            minMovesBuffer = Mathf.Max(0, minMovesBuffer);
            maxMovesBuffer = Mathf.Max(minMovesBuffer, maxMovesBuffer);
        }
    }
}
