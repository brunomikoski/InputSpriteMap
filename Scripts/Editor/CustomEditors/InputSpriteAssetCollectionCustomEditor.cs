using BrunoMikoski.ScriptableObjectCollections;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.InputSpriteMap
{

    [CustomEditor(typeof(SpriteAssetCollection), true)]
    public class InputSpriteAssetCollectionCustomEditor : CollectionCustomEditor
    {
        private const string SpriteAssetGuidPropertyName = "targetSpriteAssetGuid";
        
        private TMP_SpriteAsset spriteAsset;
        private SerializedProperty spriteAssetGUIDProperty;

        protected override bool CanBeReorderable => false;

        protected override void OnEnable()
        {
            base.OnEnable();

            spriteAssetGUIDProperty = serializedObject.FindProperty(SpriteAssetGuidPropertyName);
        }

        public override void OnInspectorGUI()
        {
            if (!string.IsNullOrEmpty(spriteAssetGUIDProperty.stringValue))
                spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(AssetDatabase.GUIDToAssetPath(spriteAssetGUIDProperty.stringValue));
            
            
            EditorGUI.BeginChangeCheck();
            
            TMP_SpriteAsset newAsset = (TMP_SpriteAsset) EditorGUILayout.ObjectField("Sprite Asset", spriteAsset, typeof(TMP_SpriteAsset), false);

            if (EditorGUI.EndChangeCheck())
            {
                if (newAsset != null)
                {
                    spriteAssetGUIDProperty.stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newAsset));}
                else
                {
                    spriteAssetGUIDProperty.stringValue = string.Empty;
                }
                
                serializedObject.ApplyModifiedProperties();
                spriteAsset = newAsset;
            }

            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                if (Application.isPlaying)
                    InputParser.ClearCache();
            }
        }

        protected override void HideProperties()
        {
            base.HideProperties();
            ExcludeProperty(SpriteAssetGuidPropertyName);
        }
    }
}