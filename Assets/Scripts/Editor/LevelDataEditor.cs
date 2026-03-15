using TapAway.Core;
using UnityEditor;
using UnityEngine;

namespace TapAway.Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        private SerializedProperty _widthProperty;
        private SerializedProperty _heightProperty;
        private SerializedProperty _moveLimitProperty;
        private SerializedProperty _blocksProperty;
        private SerializedProperty _visualThemeProperty;
        private SerializedProperty _trailBindingIndexProperty;

        private void OnEnable()
        {
            _widthProperty = serializedObject.FindProperty("width");
            _heightProperty = serializedObject.FindProperty("height");
            _moveLimitProperty = serializedObject.FindProperty("moveLimit");
            _blocksProperty = serializedObject.FindProperty("blocks");
            _visualThemeProperty = serializedObject.FindProperty("visualTheme");
            _trailBindingIndexProperty = serializedObject.FindProperty("trailBindingIndex");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_widthProperty);
            EditorGUILayout.PropertyField(_heightProperty);
            EditorGUILayout.PropertyField(_moveLimitProperty);
            EditorGUILayout.PropertyField(_visualThemeProperty);

            DrawTrailBindingDropdown();

            EditorGUILayout.PropertyField(_blocksProperty, true);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTrailBindingDropdown()
        {
            LevelVisualTheme visualTheme = _visualThemeProperty.objectReferenceValue as LevelVisualTheme;
            if (visualTheme == null)
            {
                EditorGUILayout.PropertyField(_trailBindingIndexProperty);
                EditorGUILayout.HelpBox("Gan LevelVisualTheme de chon Trail Binding bang dropdown.", MessageType.Info);
                return;
            }

            int bindingCount = visualTheme.TrailBindingCount;
            if (bindingCount <= 0)
            {
                _trailBindingIndexProperty.intValue = 0;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("Trail Binding", 0);
                }

                EditorGUILayout.HelpBox("LevelVisualTheme chua co Trail Binding nao.", MessageType.Warning);
                return;
            }

            string[] options = new string[bindingCount];
            for (int i = 0; i < bindingCount; i++)
            {
                options[i] = BuildBindingLabel(visualTheme, i);
            }

            int currentIndex = Mathf.Clamp(_trailBindingIndexProperty.intValue, 0, bindingCount - 1);
            int selectedIndex = EditorGUILayout.Popup(new GUIContent("Trail Binding"), currentIndex, options);
            _trailBindingIndexProperty.intValue = selectedIndex;
        }

        private static string BuildBindingLabel(LevelVisualTheme visualTheme, int index)
        {
            if (!visualTheme.TryGetBindingByIndex(index, out LevelVisualTheme.TrailBinding binding) || binding == null)
            {
                return "[" + index + "] (Invalid)";
            }

            string spriteName = binding.BlockSprite != null ? binding.BlockSprite.name : "No Sprite";
            string colorHex = ColorUtility.ToHtmlStringRGB(binding.TrailColor);
            return "[" + index + "] " + spriteName + "  #" + colorHex;
        }
    }
}
