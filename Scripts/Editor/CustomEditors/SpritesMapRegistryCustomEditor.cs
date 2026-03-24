using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.InputSpriteMap
{
    [CustomEditor(typeof(SpritesMapRegistry))]
    public sealed class SpritesMapRegistryCustomEditor : UnityEditor.Editor
    {
        private static readonly string[] GenericGamepadIdentifiers =
        {
            "buttonSouth",
            "buttonEast",
            "buttonWest",
            "buttonNorth",
            "start",
            "select",
            "leftShoulder",
            "rightShoulder",
            "leftTrigger",
            "rightTrigger",
            "leftStickPress",
            "rightStickPress",
            "leftStick",
            "rightStick",
            "dpad",
            "dpad/up",
            "dpad/down",
            "dpad/left",
            "dpad/right"
        };
        
        private static readonly string[] MouseAndKeyboardIdentifiers =
        {
            "leftButton",
            "rightButton",
            "middleButton",
            "forwardButton",
            "backButton",
            "scroll/up",
            "scroll/down",
            "w",
            "a",
            "s",
            "d",
            "upArrow",
            "downArrow",
            "leftArrow",
            "rightArrow",
            "space",
            "escape",
            "enter",
            "tab",
            "leftShift",
            "leftCtrl",
            "leftAlt"
        };
        
        private static readonly string[] UiActionIdentifiers =
        {
            "submit",
            "cancel",
            "navigate",
            "point",
            "click",
            "leftClick",
            "rightClick",
            "middleClick",
            "scrollWheel",
            "trackedDevicePosition",
            "trackedDeviceOrientation"
        };

        private const string SpriteDataPropertyName = "spriteData";

        private SerializedProperty _spriteDataProperty;

        private void OnEnable()
        {
            _spriteDataProperty = serializedObject.FindProperty(SpriteDataPropertyName);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();
            EditorGUILayout.Space();
            DrawTools();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTools()
        {
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Creates default Generic entries for common Unity Input System gamepad controls. Existing entries are preserved.",
                MessageType.Info);

            if (GUILayout.Button("Generate Generic Gamepad Defaults"))
                GenerateGenericGamepadDefaults();

            if (GUILayout.Button("Generate Mouse + Keyboard Defaults"))
                GenerateMouseAndKeyboardDefaults();

            if (GUILayout.Button("Generate UI Action Defaults"))
                GenerateUiActionDefaults();
        }

        private void GenerateGenericGamepadDefaults()
        {
            int addedCount = GenerateDefaults(GenericGamepadIdentifiers, PlatformType.MouseAndKeyboard);
            Debug.Log($"SpritesMapRegistry generic gamepad defaults complete. Added {addedCount} new entries.");
        }

        private void GenerateMouseAndKeyboardDefaults()
        {
            int addedCount = GenerateDefaults(MouseAndKeyboardIdentifiers, PlatformType.MouseAndKeyboard);
            Debug.Log($"SpritesMapRegistry mouse + keyboard defaults complete. Added {addedCount} new entries.");
        }

        private void GenerateUiActionDefaults()
        {
            int addedCount = GenerateDefaults(UiActionIdentifiers, PlatformType.MouseAndKeyboard);
            Debug.Log($"SpritesMapRegistry UI action defaults complete. Added {addedCount} new entries.");
        }

        private int GenerateDefaults(string[] namesToAdd, PlatformType platformType)
        {
            serializedObject.Update();

            HashSet<string> existingNames = CollectExistingNames();
            int addedCount = 0;
            for (int i = 0; i < namesToAdd.Length; i++)
            {
                string defaultName = namesToAdd[i];
                if (!existingNames.Add(defaultName))
                    continue;

                int newIndex = _spriteDataProperty.arraySize;
                _spriteDataProperty.InsertArrayElementAtIndex(newIndex);
                SerializedProperty newElement = _spriteDataProperty.GetArrayElementAtIndex(newIndex);

                SerializedProperty nameProperty = newElement.FindPropertyRelative("Name");
                SerializedProperty platformToSpriteProperty = newElement.FindPropertyRelative("PlatformToSprite");

                nameProperty.stringValue = defaultName;
                platformToSpriteProperty.arraySize = 1;

                SerializedProperty platformEntry = platformToSpriteProperty.GetArrayElementAtIndex(0);
                SerializedProperty inputTypeProperty = platformEntry.FindPropertyRelative("InputType");
                SerializedProperty spriteNameProperty = platformEntry.FindPropertyRelative("SpriteName");
                SerializedProperty spriteGuidProperty = platformEntry.FindPropertyRelative("SpriteGuid");

                inputTypeProperty.enumValueIndex = (int)platformType;
                spriteNameProperty.stringValue = string.Empty;
                spriteGuidProperty.stringValue = string.Empty;

                addedCount++;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            return addedCount;
        }

        private HashSet<string> CollectExistingNames()
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _spriteDataProperty.arraySize; i++)
            {
                SerializedProperty element = _spriteDataProperty.GetArrayElementAtIndex(i);
                SerializedProperty nameProperty = element.FindPropertyRelative("Name");
                string value = nameProperty.stringValue;
                if (!string.IsNullOrWhiteSpace(value))
                    names.Add(value);
            }

            return names;
        }
    }
}
