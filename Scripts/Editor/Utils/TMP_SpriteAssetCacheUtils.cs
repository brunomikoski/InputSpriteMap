using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BrunoMikoski.InputSpriteMap
{
    public class InputGlyphAdvancedDropDown : AdvancedDropdown
    {
        private readonly TMP_SpriteAsset spriteAsset;
        private Action<string> callback;

        public InputGlyphAdvancedDropDown(AdvancedDropdownState state, TMP_SpriteAsset spriteAsset) : base(state)
        {
            this.spriteAsset = spriteAsset;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem("Available Glyphs");

            for (int i = 0; i < spriteAsset.spriteCharacterTable.Count; i++)
            {
                TMP_SpriteCharacter spriteCharacter = spriteAsset.spriteCharacterTable[i];
                root.AddChild(new AdvancedDropdownItem(spriteCharacter.name));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            callback?.Invoke(item.name);
        }

        public void Show(Rect rect, Action<string> callback)
        {
            this.callback = callback;
            Show(rect);
        }
    }

    public class TMP_SpriteAssetCacheUtils
    {
        private static readonly Dictionary<string, TMP_SpriteAsset> SpriteAssetCache = new();

        private static readonly Dictionary<TMP_SpriteAsset, InputGlyphAdvancedDropDown> SpriteAssetToDropDownCache = new();
        

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void SupportDomainReload()
        {
            SpriteAssetCache.Clear();
            SpriteAssetToDropDownCache.Clear();
        }

        public static bool TryGetSpriteAsset(string spriteAssetName, out TMP_SpriteAsset spriteAsset)
        {
            if (!SpriteAssetCache.TryGetValue(spriteAssetName, out spriteAsset))
            {
                string[] assets = AssetDatabase.FindAssets($"t:{nameof(TMP_SpriteAsset)} {spriteAssetName}");

                if (assets.Length > 1)
                {
                    Debug.LogError($"Found more than one sprite asset with name {spriteAssetName}");
                    return false;
                }

                if (assets.Length == 0)
                {
                    Debug.LogError($"Could not find any sprite asset with name {spriteAssetName}");
                    return false;
                }


                spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(AssetDatabase.GUIDToAssetPath(assets[0]));
                SpriteAssetCache.Add(spriteAssetName, spriteAsset);
            }

            return spriteAsset != null;
        }

        public static void ShowOptionsForSpriteAsset(string spriteAssetName, Rect targetRect, Action<string> callback)
        {
            if (!TryGetSpriteAsset(spriteAssetName, out TMP_SpriteAsset spriteAsset))
                return;

            if (!SpriteAssetToDropDownCache.TryGetValue(spriteAsset, out InputGlyphAdvancedDropDown dropDown))
            {
                dropDown = new InputGlyphAdvancedDropDown(new AdvancedDropdownState(), spriteAsset);
                SpriteAssetToDropDownCache.Add(spriteAsset, dropDown);
            }

            dropDown.Show(targetRect, callback);
        }

        public static List<TMP_SpriteCharacter> GetAvailableGlyphs(TMP_SpriteAsset spriteAsset)
        {
            List<TMP_SpriteCharacter> availableGlyphs = new List<TMP_SpriteCharacter>();

            for (int i = 0; i < spriteAsset.spriteCharacterTable.Count; i++)
            {
                availableGlyphs.Add(spriteAsset.spriteCharacterTable[i]);
            }

            return availableGlyphs;
        }
    }
}