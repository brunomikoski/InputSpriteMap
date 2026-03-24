using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.InputSpriteMap
{
    [CustomPropertyDrawer(typeof(SpriteData))]
    public sealed class SpriteDataPropertyDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            SerializedProperty nameProperty = property.FindPropertyRelative("Name");
            SerializedProperty platformToSpriteProperty = property.FindPropertyRelative("PlatformToSprite");
            float nameHeight = EditorGUI.GetPropertyHeight(nameProperty, true);
            float platformsHeight = EditorGUI.GetPropertyHeight(platformToSpriteProperty, true);

            return lineHeight + VerticalSpacing + nameHeight + VerticalSpacing + platformsHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect currentRect = new Rect(position.x, position.y, position.width, lineHeight);
            property.isExpanded = EditorGUI.Foldout(currentRect, property.isExpanded, label, true);
            if (!property.isExpanded)
                return;

            SerializedProperty nameProperty = property.FindPropertyRelative("Name");
            SerializedProperty platformToSpriteProperty = property.FindPropertyRelative("PlatformToSprite");

            EditorGUI.indentLevel++;
            currentRect.y += lineHeight + VerticalSpacing;
            float nameHeight = EditorGUI.GetPropertyHeight(nameProperty, true);
            Rect nameRect = new Rect(currentRect.x, currentRect.y, currentRect.width, nameHeight);
            EditorGUI.PropertyField(nameRect, nameProperty, true);

            currentRect.y += nameHeight + VerticalSpacing;
            float platformsHeight = EditorGUI.GetPropertyHeight(platformToSpriteProperty, true);
            Rect platformsRect = new Rect(currentRect.x, currentRect.y, currentRect.width, platformsHeight);
            EditorGUI.PropertyField(platformsRect, platformToSpriteProperty, true);
            EditorGUI.indentLevel--;
        }
    }

    [CustomPropertyDrawer(typeof(PlatformToSprite))]
    public sealed class PlatformToSpritePropertyDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;
        private const float SquareSize = 64f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            return lineHeight + VerticalSpacing + SquareSize + VerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            SerializedProperty inputTypeProperty = property.FindPropertyRelative("PlatformType");
            SerializedProperty spriteNameProperty = property.FindPropertyRelative("SpriteName");
            SerializedProperty spriteGuidProperty = property.FindPropertyRelative("SpriteGuid");

            Rect firstLine = new Rect(position.x, position.y, position.width, lineHeight);
            EditorGUI.PropertyField(firstLine, inputTypeProperty, label);

            Rect secondLine = new Rect(position.x, position.y + lineHeight + VerticalSpacing, position.width, SquareSize);
            DrawSpritePickerLine(secondLine, spriteGuidProperty, spriteNameProperty);
        }

        private static void DrawSpritePickerLine(
            Rect rect,
            SerializedProperty spriteGuidProperty,
            SerializedProperty spriteNameProperty)
        {
            Rect labelRect = EditorGUI.PrefixLabel(rect, new GUIContent("Sprite"));
            Rect objectFieldRect = new Rect(labelRect.x, labelRect.y, SquareSize, SquareSize);
            Rect nameRect = new Rect(
                labelRect.x + SquareSize + 6f,
                labelRect.y,
                Mathf.Max(0f, labelRect.width - SquareSize - 6f),
                EditorGUIUtility.singleLineHeight);

            Sprite currentSprite = ResolveSprite(spriteGuidProperty.stringValue, spriteNameProperty.stringValue);
            EditorGUI.BeginChangeCheck();
            Sprite newSprite = (Sprite)EditorGUI.ObjectField(objectFieldRect, GUIContent.none, currentSprite, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck())
                ApplySpriteSelection(newSprite, spriteGuidProperty, spriteNameProperty);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.TextField(nameRect, spriteNameProperty.stringValue);
            }
        }

        private static void ApplySpriteSelection(
            Sprite sprite,
            SerializedProperty spriteGuidProperty,
            SerializedProperty spriteNameProperty)
        {
            if (sprite == null)
            {
                spriteGuidProperty.stringValue = string.Empty;
                spriteNameProperty.stringValue = string.Empty;
                return;
            }

            string path = AssetDatabase.GetAssetPath(sprite);
            spriteGuidProperty.stringValue = AssetDatabase.AssetPathToGUID(path);
            spriteNameProperty.stringValue = sprite.name;
        }

        private static Sprite ResolveSprite(string spriteGuid, string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteGuid) || string.IsNullOrWhiteSpace(spriteName))
                return null;

            string assetPath = AssetDatabase.GUIDToAssetPath(spriteGuid);
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                Sprite sprite = assets[i] as Sprite;
                if (sprite == null)
                    continue;

                if (sprite.name == spriteName)
                    return sprite;
            }

            return null;
        }
    }
}
