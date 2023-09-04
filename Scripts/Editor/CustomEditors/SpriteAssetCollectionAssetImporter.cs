using System;
using TMPro;
using UnityEditor;

namespace BrunoMikoski.InputSpriteMap
{
    public class SpriteAssetCollectionAssetImporter : AssetPostprocessor
    {
        public static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!EmojiOneFileUtility.HasEmojiOneAsset)
                return;
            
            for (int i = 0; i < importedAssets.Length; i++)
            {
                string importedAssetPath = importedAssets[i];
                
                Type type = AssetDatabase.GetMainAssetTypeAtPath(importedAssetPath);

                if (type != typeof(SpriteAssetCollection) || type.IsSubclassOf(typeof(SpriteAssetCollection)))
                    continue;
                
                SpriteAssetCollection spriteAssetCollection = AssetDatabase.LoadAssetAtPath<SpriteAssetCollection>(importedAssetPath);
        
                if (spriteAssetCollection == null)
                    continue;

                if (string.IsNullOrEmpty(spriteAssetCollection.TargetSpriteAssetGuid))
                    continue;
                
                TMP_SpriteAsset spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(AssetDatabase.GUIDToAssetPath(spriteAssetCollection.TargetSpriteAssetGuid));
                if (spriteAsset == null)
                    continue;
                
                if (EmojiOneFileUtility.ContainsSpriteAsset(spriteAsset))
                    continue;
        
                if (!EditorUtility.DisplayDialog("Input Sprite Map",
                        $"Would you like to add {spriteAsset.name} as a fallback at EmojiOne? (You can do that later, or make your own system to dynamically add it based on the platform",
                        "Yes", "No"))
                {
                    continue;
                }
        
                EmojiOneFileUtility.AddSpriteAssetAsFallback(spriteAsset);
            }
        }
    }
}
