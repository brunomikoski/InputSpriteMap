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
        public PlatformType PlatformType;
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
#if UNITY_EDITOR
        private static SpritesMapRegistry _cachedEditorSpriteMapRegistry;
        private static bool _hasCachedEditorSpriteMapRegistry;

        public static SpritesMapRegistry FromAssetDatabase()
        {
            if (!_hasCachedEditorSpriteMapRegistry)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(SpritesMapRegistry)}");
                if (guids.Length == 0)
                {
                    throw new Exception($"Could not find {nameof(SpritesMapRegistry)} asset, create one by the Create/Input Sprite Map/Sprites Map Registry");
                }
                _cachedEditorSpriteMapRegistry = UnityEditor.AssetDatabase.LoadAssetAtPath<SpritesMapRegistry>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));;
                _hasCachedEditorSpriteMapRegistry = _cachedEditorSpriteMapRegistry != null;
            }

            return _cachedEditorSpriteMapRegistry;
            
        }
        
#endif
        
        [field: SerializeField]
        private bool _showLogs;
        [SerializeField]
        private  bool _cacheEnabled = true;


        [SerializeField]
        public SpriteData[] spriteData;

        private readonly Dictionary<string, string> _displayStringCache = new Dictionary<string, string>();

        private void Log(string message)
        {
            if (!_showLogs)
                return;

            Debug.Log($"[SpritesMapRegistry] {message}", this);
        }

        public bool TryGetSpriteName(string inputName, PlatformType platformType, out string spriteName)
        {
            string candidateName = inputName;
            while (!string.IsNullOrEmpty(candidateName))
            {
                if (TryGetSpriteNameForControl(candidateName, platformType, out spriteName))
                    return true;

                int lastSeparatorIndex = candidateName.LastIndexOf('/');
                if (lastSeparatorIndex < 0)
                    break;

                candidateName = candidateName.Substring(0, lastSeparatorIndex);
            }

            Log($"TryGetSpriteName: no match for input '{inputName}', platform={platformType}");
            spriteName = string.Empty;
            return false;
        }

        private bool TryGetSpriteNameForControl(string inputName, PlatformType platformType, out string spriteName)
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

                Log($"TryGetSpriteName: matched data name '{data.Name}' for input '{inputName}', platform={platformType}");
                if (TryGetSpriteNameFromPlatformEntries(data.PlatformToSprite, inputName, platformType, out spriteName))
                    return true;
            }

            spriteName = string.Empty;
            return false;
        }

        public bool TryGetSpriteTag(string inputName, PlatformType platformType, out string spriteTag)
        {
            if (TryGetSpriteName(inputName, platformType, out string spriteName))
            {
                spriteTag = BuildSpriteTag(spriteName);
                return true;
            }

            spriteTag = string.Empty;
            return false;
        }

        public string GetDisplayStringForInput(InputAction inputAction, PlatformType platformType, int specificBindingIndex = -1, string compositionSeparator = "", string[] specifyCompositeNames = null, bool onlyFirstResult = true,
            bool useMissingTagWhenNotFound = false)
        {
            if (inputAction == null)
                return string.Empty;

            string cacheKey = BuildCacheKey(
                inputAction,
                platformType,
                specificBindingIndex,
                compositionSeparator,
                specifyCompositeNames,
                onlyFirstResult,
                useMissingTagWhenNotFound);
            if (_cacheEnabled && _displayStringCache.TryGetValue(cacheKey, out string cached))
            {
                Log($"GetDisplayStringForInput: cache hit action='{inputAction.name}' platform={platformType} -> '{cached}'");
                return cached;
            }

            Log(
                $"GetDisplayStringForInput: action='{inputAction.name}' id={inputAction.id} platform={platformType} bindingCount={inputAction.bindings.Count} onlyFirst={onlyFirstResult} specificIndex={specificBindingIndex}");

            List<string> actionBindings = new List<string>();
            List<string> currentCompositeItems = new List<string>();
            List<string> missingInputNames = new List<string>();

            int startIndex = specificBindingIndex >= 0 ? specificBindingIndex : 0;
            ReadOnlyArray<InputBinding> bindings = inputAction.bindings;
            for (int i = startIndex; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];

                if (binding.isComposite)
                {
                    FlushComposite(actionBindings, currentCompositeItems, compositionSeparator);
                    if (specificBindingIndex >= 0)
                        break;
                    continue;
                }

                string bindingPath = binding.effectivePath;
                if (string.IsNullOrEmpty(bindingPath))
                    bindingPath = binding.path;

                if (string.IsNullOrEmpty(bindingPath))
                    continue;

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

                Log(
                    $"  binding[{i}] path='{bindingPath}' parsed='{inputName}' isComposite={binding.isComposite} partOfComposite={binding.isPartOfComposite} partName='{binding.name}'");

                if (TryGetSpriteTag(inputName, platformType, out string spriteTag))
                {
                    Log($"  -> sprite tag: {spriteTag}");
                    currentCompositeItems.Add(spriteTag);
                }
                else
                {
                    if (useMissingTagWhenNotFound)
                    {
                        Log($"  -> no mapping for '{inputName}' on {platformType}, tracking missing label fallback");
                        missingInputNames.Add(inputName);
                    }
                    else
                    {
                        Log($"  -> no mapping for '{inputName}' on {platformType}");
                    }
                }
            }

            FlushComposite(actionBindings, currentCompositeItems, compositionSeparator);

            if (onlyFirstResult && actionBindings.Count > 1)
                actionBindings.RemoveRange(1, actionBindings.Count - 1);

            if (useMissingTagWhenNotFound && actionBindings.Count == 0 && missingInputNames.Count > 0)
            {
                int missingCount = onlyFirstResult ? 1 : missingInputNames.Count;
                for (int i = 0; i < missingCount; i++)
                {
                    actionBindings.Add(BuildMissingLabel(missingInputNames[i]));
                }
            }

            string result = string.Join(compositionSeparator, actionBindings);
            _displayStringCache[cacheKey] = result;
            Log($"GetDisplayStringForInput: result='{result}'");
            return result;
        }

        [ContextMenu("Clear Display String Cache")]
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

        private bool TryGetSpriteNameFromPlatformEntries(PlatformToSprite[] platformEntries, string inputNameForLog, PlatformType platformType, out string spriteName)
        {
            if (platformEntries == null)
            {
                Log($"  PlatformEntries: null for input '{inputNameForLog}'");
                spriteName = string.Empty;
                return false;
            }

            Log($"  PlatformEntries: resolving '{inputNameForLog}' platform={platformType} entryCount={platformEntries.Length}");

            for (int i = 0; i < platformEntries.Length; i++)
            {
                PlatformToSprite platformToSprite = platformEntries[i];
                if (platformToSprite.PlatformType != platformType)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(platformToSprite.SpriteName))
                {
                    continue;
                }

                Log($"    [{i}] CHOSEN (exact): flags={platformToSprite.PlatformType} sprite='{platformToSprite.SpriteName}'");
                spriteName = platformToSprite.SpriteName;
                return true;
            }

            for (int i = 0; i < platformEntries.Length; i++)
            {
                PlatformToSprite platformToSprite = platformEntries[i];
                if (platformToSprite.PlatformType == PlatformType.None)
                {
                    continue;
                }

                if (!platformToSprite.PlatformType.HasAnyFlagFast(platformType))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(platformToSprite.SpriteName))
                {
                    continue;
                }

                Log($"    [{i}] CHOSEN (overlap): entryFlags={platformToSprite.PlatformType} query={platformType} sprite='{platformToSprite.SpriteName}'");
                spriteName = platformToSprite.SpriteName;
                return true;
            }

            Log($"  PlatformEntries: no match for '{inputNameForLog}' on {platformType}");
            spriteName = string.Empty;
            return false;
        }

        private static string ParseInputNameFromBindingPath(string bindingPath)
        {
            int deviceEndIndex = bindingPath.IndexOf('>');
            int controlStartIndex = bindingPath.IndexOf('/', deviceEndIndex + 1);
            if (controlStartIndex < 0 || controlStartIndex >= bindingPath.Length - 1)
                return string.Empty;

            return bindingPath.Substring(controlStartIndex + 1).Trim();
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

        private static string BuildMissingLabel(string parsedInputName)
        {
            return $"[Missing] {parsedInputName}";
        }

        private static string BuildCacheKey(
            InputAction inputAction,
            PlatformType platformType,
            int specificBindingIndex,
            string compositionSeparator,
            string[] specifyCompositeNames,
            bool onlyFirstResult,
            bool useMissingTagWhenNotFound)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(inputAction.id);
            builder.Append('|');
            builder.Append(platformType);
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
