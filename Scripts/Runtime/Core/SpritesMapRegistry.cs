using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace BrunoMikoski.InputSpriteMap
{
    [Serializable]
    public class PlatformToSprite
    {
        public InputType InputType;
        public string SpriteName;
        public string SpriteGuid;
    }
    [Serializable]
    public class SpriteData
    {
        public string Name;
        public PlatformToSprite[] PlatformToSprite;
        
    }

    [CreateAssetMenu(menuName = "Input Sprite Map/Sprites Map Registry", fileName = "SpritesMapRegistry", order = 0)]
    public class SpritesMapRegistry : ScriptableObject
    {
        private const string MissingSpriteTag = @"<sprite name=""????""/>";

        [SerializeField]
        public SpriteData[] spriteData;

        private readonly Dictionary<string, string> _displayStringCache = new Dictionary<string, string>();

        public bool TryGetSpriteName(string inputName, InputType inputType, out string spriteName)
        {
            if (spriteData == null)
            {
                spriteName = string.Empty;
                return false;
            }

            for (int i = 0; i < spriteData.Length; i++)
            {
                SpriteData data = spriteData[i];
                if (data == null)
                    continue;

                if (!string.Equals(data.Name, inputName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (TryGetSpriteNameFromPlatformEntries(data.PlatformToSprite, inputType, out spriteName))
                    return true;
            }

            spriteName = string.Empty;
            return false;
        }

        public bool TryGetSpriteTag(string inputName, InputType inputType, out string spriteTag)
        {
            if (TryGetSpriteName(inputName, inputType, out string spriteName))
            {
                spriteTag = BuildSpriteTag(spriteName);
                return true;
            }

            spriteTag = string.Empty;
            return false;
        }

        public string GetDisplayStringForInput(
            InputAction inputAction,
            InputType inputType,
            int specificBindingIndex = -1,
            string compositionSeparator = "",
            string[] specifyCompositeNames = null,
            bool onlyFirstResult = true,
            bool useMissingTagWhenNotFound = false)
        {
            if (inputAction == null)
                return string.Empty;

            string cacheKey = BuildCacheKey(inputAction, inputType, specificBindingIndex, compositionSeparator, specifyCompositeNames, onlyFirstResult, useMissingTagWhenNotFound);
            if (_displayStringCache.TryGetValue(cacheKey, out string cached))
                return cached;

            List<string> actionBindings = new List<string>();
            List<string> currentCompositeItems = new List<string>();

            int startIndex = specificBindingIndex >= 0 ? specificBindingIndex : 0;
            ReadOnlyArray<InputBinding> bindings = inputAction.bindings;
            for (int i = startIndex; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];
                string bindingPath = binding.effectivePath;
                if (string.IsNullOrEmpty(bindingPath))
                    continue;

                if (binding.isComposite)
                {
                    FlushComposite(actionBindings, currentCompositeItems, compositionSeparator);
                    if (specificBindingIndex >= 0)
                        break;
                    continue;
                }

                if (!binding.isPartOfComposite && (onlyFirstResult || specificBindingIndex >= 0))
                {
                    if (actionBindings.Count > 0 || currentCompositeItems.Count > 0)
                        break;
                }

                if (binding.isPartOfComposite && !ShouldUseCompositePart(binding.name, specifyCompositeNames))
                    continue;

                string inputName = ParseInputNameFromBindingPath(bindingPath);
                if (string.IsNullOrEmpty(inputName))
                    continue;

                if (TryGetSpriteTag(inputName, inputType, out string spriteTag))
                {
                    currentCompositeItems.Add(spriteTag);
                }
                else if (useMissingTagWhenNotFound)
                {
                    currentCompositeItems.Add(MissingSpriteTag);
                }
            }

            FlushComposite(actionBindings, currentCompositeItems, compositionSeparator);

            if (onlyFirstResult && actionBindings.Count > 1)
                actionBindings.RemoveRange(1, actionBindings.Count - 1);

            string result = string.Join(compositionSeparator, actionBindings);
            _displayStringCache[cacheKey] = result;
            return result;
        }

        public void ClearCache()
        {
            _displayStringCache.Clear();
        }

        public void ClearCacheForAction(InputAction inputAction)
        {
            if (inputAction == null)
                return;

            string actionId = inputAction.id.ToString();
            List<string> keysToRemove = new List<string>();
            foreach (KeyValuePair<string, string> pair in _displayStringCache)
            {
                if (pair.Key.Contains(actionId, StringComparison.Ordinal))
                    keysToRemove.Add(pair.Key);
            }

            for (int i = 0; i < keysToRemove.Count; i++)
                _displayStringCache.Remove(keysToRemove[i]);
        }

        private static bool TryGetSpriteNameFromPlatformEntries(PlatformToSprite[] platformEntries, InputType inputType, out string spriteName)
        {
            if (platformEntries == null)
            {
                spriteName = string.Empty;
                return false;
            }

            for (int i = 0; i < platformEntries.Length; i++)
            {
                PlatformToSprite platformToSprite = platformEntries[i];
                if (platformToSprite.InputType != inputType)
                    continue;

                if (string.IsNullOrWhiteSpace(platformToSprite.SpriteName))
                    continue;

                spriteName = platformToSprite.SpriteName;
                return true;
            }

            spriteName = string.Empty;
            return false;
        }

        private static string ParseInputNameFromBindingPath(string bindingPath)
        {
            int slashIndex = bindingPath.LastIndexOf('/');
            if (slashIndex < 0 || slashIndex >= bindingPath.Length - 1)
                return string.Empty;

            string parsed = bindingPath.Substring(slashIndex + 1);
            int closeBracketIndex = parsed.IndexOf('}');
            if (closeBracketIndex >= 0 && closeBracketIndex < parsed.Length - 1)
                parsed = parsed.Substring(closeBracketIndex + 1);

            return parsed.Trim();
        }

        private static bool ShouldUseCompositePart(string partName, string[] specifyCompositeNames)
        {
            if (specifyCompositeNames == null || specifyCompositeNames.Length == 0)
                return true;

            for (int i = 0; i < specifyCompositeNames.Length; i++)
            {
                if (string.Equals(specifyCompositeNames[i], partName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void FlushComposite(List<string> actionBindings, List<string> currentCompositeItems, string compositionSeparator)
        {
            if (currentCompositeItems.Count == 0)
                return;

            actionBindings.Add(string.Join(compositionSeparator, currentCompositeItems));
            currentCompositeItems.Clear();
        }

        private static string BuildSpriteTag(string spriteName)
        {
            return $@"<sprite name=""{spriteName}""/>";
        }

        private static string BuildCacheKey(
            InputAction inputAction,
            InputType inputType,
            int specificBindingIndex,
            string compositionSeparator,
            string[] specifyCompositeNames,
            bool onlyFirstResult,
            bool useMissingTagWhenNotFound)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(inputAction.id);
            builder.Append('|');
            builder.Append(inputType);
            builder.Append('|');
            builder.Append(specificBindingIndex);
            builder.Append('|');
            builder.Append(compositionSeparator);
            builder.Append('|');
            builder.Append(onlyFirstResult);
            builder.Append('|');
            builder.Append(useMissingTagWhenNotFound);

            if (specifyCompositeNames != null && specifyCompositeNames.Length > 0)
            {
                builder.Append('|');
                for (int i = 0; i < specifyCompositeNames.Length; i++)
                {
                    if (i > 0)
                        builder.Append(',');
                    builder.Append(specifyCompositeNames[i]);
                }
            }

            return builder.ToString();
        }
    }
}
