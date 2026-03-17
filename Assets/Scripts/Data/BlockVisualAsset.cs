using UnityEngine;

namespace TapAway.Core
{
    [CreateAssetMenu(menuName = "TapAway/Block Visual Asset", fileName = "BlockVisualAsset")]
    public class BlockVisualAsset : ScriptableObject
    {
        [Tooltip("Mau block dung cho map/theme.")]
        [SerializeField] private Color _blockColor = Color.white;

        public Color BlockColor => _blockColor;
    }
}
