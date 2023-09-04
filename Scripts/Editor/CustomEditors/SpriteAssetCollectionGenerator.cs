using System;
using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections;
using JetBrains.Annotations;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.InputSpriteMap
{
    // public class SpriteAssetIDTemplate : ItemTemplate
    // {
    //     public string spriteName;
    // }
    //
    // [UsedImplicitly]
    // public sealed class SpriteAssetCollectionGenerator : IScriptableObjectCollectionGenerator<SpriteAssetCollection, SpriteAssetIDTemplate>
    // {
    //     private const string SPRITE_ASSET_GUID_PROPERTY_NAME = "targetSpriteAssetGuid";
    //     public bool ShouldRemoveNonGeneratedItems => false;
    //
    //     public void GetItemTemplates(List<SpriteAssetIDTemplate> templates, SpriteAssetCollection collection)
    //     {
    //         SerializedObject collectionSO = new SerializedObject(collection);
    //         SerializedProperty spriteAssetGUIDProperty = collectionSO.FindProperty(SPRITE_ASSET_GUID_PROPERTY_NAME);
    //         if (string.IsNullOrEmpty(spriteAssetGUIDProperty.stringValue))
    //         {
    //             Debug.LogError("Please set the target sprite asset first");
    //             return;
    //         }
    //
    //         TMP_SpriteAsset spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(AssetDatabase.GUIDToAssetPath(spriteAssetGUIDProperty.stringValue));
    //         
    //
    //         List<TMP_SpriteCharacter> spriteCharacters = TMP_SpriteAssetCacheUtils.GetAvailableGlyphs(spriteAsset);
    //         Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(spriteAsset.spriteSheet));
    //
    //         if (sprites.Length - 1 != spriteCharacters.Count)
    //         {
    //             throw new Exception(
    //                 $"The number of sprites on the atlas {spriteAsset.spriteSheet} doesn't match the number of glyphs on the sprite asset {spriteAsset}");
    //         }
    //
    //         for (int i = 0; i < spriteCharacters.Count; i++)
    //         {
    //             TMP_SpriteCharacter spriteCharacter = spriteCharacters[i];
    //             templates.Add(new SpriteAssetIDTemplate() { spriteName = spriteCharacter.name, name = spriteCharacter.name });
    //         }
    //     }
    //
    //     public void OnItemsGenerationComplete(SpriteAssetCollection collection)
    //     {
    //         collection.Items.Sort(new SortByLongestName());
    //         EditorUtility.SetDirty(collection);
    //         AssetDatabase.SaveAssets();
    //     }
    // }
    //
    // internal class SortByLongestName : IComparer<ScriptableObject>
    // {
    //     public int Compare(ScriptableObject x, ScriptableObject y)
    //     {
    //         if (ReferenceEquals(x, y))
    //             return 0;
    //         if (ReferenceEquals(null, y))
    //             return 1;
    //         if (ReferenceEquals(null, x))
    //             return -1;
    //
    //         int xLongestName = GetLongestName(x);
    //         int yLongestName = GetLongestName(y);
    //
    //         if (xLongestName == yLongestName)
    //             return x.name.Length.CompareTo(y.name.Length);
    //
    //         return yLongestName.CompareTo(xLongestName);
    //     }
    //
    //     private int GetLongestName(ScriptableObject scriptableObject)
    //     {
    //         SpriteAssetId spriteAssetId = (SpriteAssetId)scriptableObject;
    //         int longestName = int.MinValue;
    //         for (int i = 0; i < spriteAssetId.additionalNames.Length; i++)
    //         {
    //             string additionalName = spriteAssetId.additionalNames[i];
    //             if (additionalName.Length > longestName)
    //                 longestName = additionalName.Length;
    //         }
    //
    //         return longestName;
    //     }
    // }
}
