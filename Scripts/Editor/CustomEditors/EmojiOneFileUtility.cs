using TMPro;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.InputSpriteMap
{
    public static class EmojiOneFileUtility
    {
        private static bool hasCachedEmojiOneAsset;
        private static TMP_SpriteAsset cachedEmojiOneAsset;
        private static TMP_SpriteAsset EmojiOneAsset
        {
            get
            {
                if (!hasCachedEmojiOneAsset)
                {
                    string[] guids = AssetDatabase.FindAssets($"t:{nameof(TMP_SpriteAsset)} EmojiOne");
                    if (guids.Length > 0)
                    {
                        cachedEmojiOneAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
                        hasCachedEmojiOneAsset = cachedEmojiOneAsset != null;
                    }
                }
                return cachedEmojiOneAsset;
            }
        }
        
        public static bool HasEmojiOneAsset => EmojiOneAsset != null;


        public static void AddSpriteAssetAsFallback(TMP_SpriteAsset spriteAsset)
        {
            if (EmojiOneAsset.fallbackSpriteAssets.Contains(spriteAsset))
                return;
            
            EmojiOneAsset.fallbackSpriteAssets.Add(spriteAsset);
            ObjectUtility.SetDirty(EmojiOneAsset);
        }

        public static bool ContainsSpriteAsset(TMP_SpriteAsset spriteAsset)
        {
            return EmojiOneAsset.fallbackSpriteAssets.Contains(spriteAsset);
        }
    }
}
