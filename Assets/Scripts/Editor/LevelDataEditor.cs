using TapAway.Core;
using UnityEditor;
using UnityEngine;

namespace TapAway.Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        private static readonly string[] MAP_SIZE_OPTIONS =
        {
            "2x2",
            "3x3",
            "4x4",
            "5x5",
            "6x6",
            "7x7",
            "8x8",
            "9x9",
        };

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

            DrawMapSizeDropdown();
            EditorGUILayout.PropertyField(_widthProperty);
            EditorGUILayout.PropertyField(_heightProperty);
            EditorGUILayout.PropertyField(_moveLimitProperty);
            EditorGUILayout.PropertyField(_visualThemeProperty);

            DrawTrailBindingDropdown();

            EditorGUILayout.PropertyField(_blocksProperty, true);

            DrawTemplateTools();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMapSizeDropdown()
        {
            LevelData levelData = (LevelData)target;
            int currentWidth = Mathf.Max(1, _widthProperty.intValue);
            int currentHeight = Mathf.Max(1, _heightProperty.intValue);

            int selectedIndex = Mathf.Clamp(currentWidth - 2, 0, MAP_SIZE_OPTIONS.Length - 1);
            if (currentWidth == currentHeight && currentWidth >= 2 && currentWidth <= 9)
            {
                selectedIndex = currentWidth - 2;
            }

            int newIndex = EditorGUILayout.Popup(new GUIContent("Map Size"), selectedIndex, MAP_SIZE_OPTIONS);
            if (newIndex == selectedIndex)
            {
                return;
            }

            int newSize = newIndex + 2;
            Undo.RecordObject(levelData, "Change Map Size");
            _widthProperty.intValue = newSize;
            _heightProperty.intValue = newSize;
            serializedObject.ApplyModifiedProperties();

            levelData.CreateGridTemplate(true);
            EditorUtility.SetDirty(levelData);
            serializedObject.Update();
        }

        private void DrawTemplateTools()
        {
            LevelData levelData = (LevelData)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Map Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Tao san danh sach cell theo width/height de ban chi can sua cellType/direction.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Template (Keep Existing)"))
                {
                    Undo.RecordObject(levelData, "Refresh Level Template");
                    levelData.CreateGridTemplate(true);
                    EditorUtility.SetDirty(levelData);
                    serializedObject.Update();
                }

                if (GUILayout.Button("Rebuild Empty Template"))
                {
                    Undo.RecordObject(levelData, "Rebuild Empty Template");
                    levelData.CreateGridTemplate(false);
                    EditorUtility.SetDirty(levelData);
                    serializedObject.Update();
                }
            }

            if (GUILayout.Button("Normalize Blocks To Map Size"))
            {
                Undo.RecordObject(levelData, "Normalize Blocks To Map Size");
                levelData.NormalizeToMapBounds();
                EditorUtility.SetDirty(levelData);
                serializedObject.Update();
            }
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
