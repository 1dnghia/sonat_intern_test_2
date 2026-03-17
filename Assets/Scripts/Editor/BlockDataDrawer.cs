using TapAway.Core;
using UnityEditor;
using UnityEngine;

namespace TapAway.Editor
{
    [CustomPropertyDrawer(typeof(BlockData))]
    public class BlockDataDrawer : PropertyDrawer
    {
        private const float LINE_HEIGHT = 18f;
        private const float LINE_SPACING = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty idProperty = property.FindPropertyRelative("id");
            SerializedProperty positionProperty = property.FindPropertyRelative("position");
            SerializedProperty directionProperty = property.FindPropertyRelative("direction");
            SerializedProperty cellTypeProperty = property.FindPropertyRelative("cellType");
            SerializedProperty linksProperty = property.FindPropertyRelative("rotatorLinkedNormals");

            Rect line = new Rect(position.x, position.y, position.width, LINE_HEIGHT);
            EditorGUI.PropertyField(line, idProperty);

            line.y += LINE_HEIGHT + LINE_SPACING;
            EditorGUI.PropertyField(line, positionProperty);

            line.y += LINE_HEIGHT + LINE_SPACING;
            EditorGUI.PropertyField(line, directionProperty);

            line.y += LINE_HEIGHT + LINE_SPACING;
            EditorGUI.PropertyField(line, cellTypeProperty);

            CellType cellType = (CellType)cellTypeProperty.enumValueIndex;
            if (cellType == CellType.Rotator)
            {
                line.y += LINE_HEIGHT + LINE_SPACING;
                EditorGUI.PropertyField(line, linksProperty, new GUIContent("Rotator Linked Normals"), true);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty cellTypeProperty = property.FindPropertyRelative("cellType");
            SerializedProperty linksProperty = property.FindPropertyRelative("rotatorLinkedNormals");

            float height = (LINE_HEIGHT + LINE_SPACING) * 4f;
            CellType cellType = (CellType)cellTypeProperty.enumValueIndex;
            if (cellType == CellType.Rotator)
            {
                height += LINE_HEIGHT + LINE_SPACING;
                height += EditorGUI.GetPropertyHeight(linksProperty, true);
            }

            return height;
        }
    }
}
