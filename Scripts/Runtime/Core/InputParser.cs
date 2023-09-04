using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Pool;

namespace BrunoMikoski.InputSpriteMap
{
    [DefaultExecutionOrder(-1000)]
    public static class InputParser
    {
        private const string MOUSE_AND_KEYBOARD_IDENTIFIER = "MouseAndKeyboard";
        private const string PLAYSTATION_IDENTIFIER = "Playstation";
        private const string GENERIC_IDENTIFIER = "Generic";
        private const string XBOX_IDENTIFIER = "Xbox";
        private const string STEAM_DECK_IDENTIFIER = "SteamDeck";
        private const string SWITCH_IDENTIFIER = "Switch";

        private static List<SpriteAssetCollection> AvailableCollections;
        private static bool Initialized;
        private static bool IsAvailable;
        private static readonly Dictionary<string,string> InputActionGuidToDisplayStringCache = new();
        private static readonly Dictionary<string, SpriteAssetCollection> CollectionIdentifierToAssetCollectionCache = new();

        private static readonly Regex BindingNameRegex = new(@"<(.+)>/(.*)");
        private static string LastKnowDeviceIdentifier = MOUSE_AND_KEYBOARD_IDENTIFIER;
        public static Action InputDeviceChangedEvent;
        private static InputDevice LastKnowDevice;
        
        static ObjectPool<StringBuilder> StringBuilderPool = new(
            () => new StringBuilder(),
            (sb) => sb.Clear());

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SupportDomainReload()
        {
            Clear();
        }

        private static void Clear()
        {
            Initialized = false;
            IsAvailable = false;
            LastKnowDeviceIdentifier = null;
            AvailableCollections?.Clear();
            InputActionGuidToDisplayStringCache.Clear();
            InputSystem.onEvent -= OnInputSystemEvent;
            InputDeviceChangedEvent = null;
        }


        [MenuItem("Tools/Input Sprite Map/Clear Cache")]
        public static void ClearCache()
        {
            Clear();
        }

        public static string ParseInputControls(string format, params InputControl[] controls)
        {
            return ParseInputControls(format, "", "", controls);
        }
        public static string ParseInputControls(params InputControl[] controls)
        {
            return ParseInputControls("", "", "", controls);
        }
        public static string ParseInputControls(IEnumerable<InputControl> controls)
        {
            return ParseInputControls("", "", "", controls.ToArray());
        }

        public static string ParseInputControls(string format = "", string identifier = "", string separator = "", params InputControl[] controls)
        {
            if (controls == null || controls.Length == 0)
            {
                Debug.LogError("No controls provided");
                return format;
            }
            
            Initialize();

            if (!IsAvailable)
            {
                Debug.LogError("There's not any SpriteAssetCollection in the project, please create one or import one");
                return string.Join(separator, controls.Select(control => control.displayName));
            }

            string displayString = string.Empty;
            
            List<string> bindingNames = ListPool<string>.Get();

            for (int i = 0; i < controls.Length; i++)
            {
                InputControl inputControl = controls[i];
                string input = ParseInput(inputControl, identifier);
                if (bindingNames.Contains(input))
                    continue;
                bindingNames.Add(input);
            }

            if (!string.IsNullOrEmpty(format))
               displayString = string.Format(string.Join(separator, bindingNames));
            else
                displayString = string.Join(separator, bindingNames);
            
            ListPool<string>.Release(bindingNames);
            return displayString;
        }

        public static string ParseString(string bindingName, string targetCollection = "", string format = "")
        {
            Initialize();

            if (!IsAvailable)
            {
                Debug.LogError("There's not any SpriteAssetCollection in the project, please create one or import one");
                return bindingName;
            }
            
            if (string.IsNullOrEmpty(targetCollection))
                targetCollection = LastKnowDeviceIdentifier;

            if (!TryGetCollectionByIdentifier(targetCollection, out SpriteAssetCollection collection))
            {
                Debug.LogError($"Failed to find collection for identifier {targetCollection}");
                return bindingName;
            }
            
            string key = $"{targetCollection}.{bindingName}";
            if (InputActionGuidToDisplayStringCache.TryGetValue(key, out string displayString)) 
                return displayString;
            
            if (TryGetDisplayValueFromBindingName(bindingName, collection, out displayString))
            {
                if (!string.IsNullOrEmpty(format))
                    displayString =  string.Format(format, displayString);
                
                InputActionGuidToDisplayStringCache.Add(key, displayString);

                // Debug.Log($"Parsing {targetInputName}:{identifier} to {displayString}");
                return displayString;
            }

            displayString = @$"<sprite name=""?""/><color=""red"">{bindingName}</color>";
            if (!string.IsNullOrEmpty(format))
                displayString =  string.Format(format, displayString);
            
            InputActionGuidToDisplayStringCache.Add(key, displayString);
            return displayString;
        }
        
        public static string ParseInput(InputControl targetInput, string targetCollection = "", string format = "")
        {
            Initialize();

            string inputDisplayName = targetInput.displayName;
            if (!IsAvailable)
            {
                Debug.LogError("There's not any SpriteAssetCollection in the project, please create one or import one");
                return inputDisplayName;
            }

            EnsureCollection(targetInput.device, ref targetCollection);

            string key = $"{targetCollection}.{inputDisplayName}";
            if (InputActionGuidToDisplayStringCache.TryGetValue(key, out string displayString)) 
                return displayString;

            if (TryGetCollectionByIdentifier(targetCollection, out SpriteAssetCollection assetCollection))
            {
                string targetBindingName = assetCollection.ParseInputControlName(targetInput);
                if (TryGetDisplayValueFromBindingName(targetBindingName, assetCollection, out displayString))
                {
                    if (!string.IsNullOrEmpty(format))
                        displayString =  string.Format(format, displayString);
                
                    InputActionGuidToDisplayStringCache.Add(key, displayString);

                    // Debug.Log($"Parsing {targetInputName}:{identifier} to {displayString}");
                    return displayString;
                }
            }
            else
            {
                Debug.LogError($"Failed to find collection for identifier {targetCollection}");
            }

            displayString = @$"<sprite name=""?""/><color=""red"">{inputDisplayName}</color>";
            if (!string.IsNullOrEmpty(format))
                displayString =  string.Format(format, displayString);
            
            InputActionGuidToDisplayStringCache.Add(key, displayString);
            return displayString;
        }

        public static string ParseInputAction(InputAction inputAction, string targetCollection = null, string format = "", string separator = "", string alternativeSeparator = "or", string[] specifyCompositeNames = null, bool onlyFirstResult = true)
        {
            Initialize();

            if (!IsAvailable)
            {
                Debug.LogError("There's not any SpriteAssetCollection in the project, please create one or import one");
                return inputAction.GetBindingDisplayString();
            }

            EnsureCollection(null, ref targetCollection);

            StringBuilder keyStringBuilder = StringBuilderPool.Get();
            keyStringBuilder.Append($"{targetCollection}.");
            keyStringBuilder.Append($"{inputAction.id.ToString()}.");
            
            if (specifyCompositeNames != null)
                keyStringBuilder.Append($".{string.Join("", specifyCompositeNames)}");

            string key = keyStringBuilder.ToString();
            if (InputActionGuidToDisplayStringCache.TryGetValue(key, out string returnResultString))
            {
                StringBuilderPool.Release(keyStringBuilder);
                return returnResultString;
            }

            if (!TryGetCollectionByIdentifier(targetCollection, out SpriteAssetCollection collection))
            {
                Debug.LogError($"Failed to find collection for identifier {targetCollection}");
                return inputAction.GetBindingDisplayString();
            }

            List<string> actionBindings = ListPool<string>.Get();

            List<string> currentCompositionItems = ListPool<string>.Get();
            for (int i = 0; i < inputAction.bindings.Count; i++)
            {
                InputBinding inputBinding = inputAction.bindings[i];

                if (inputBinding.isComposite)
                {
                    //Finished parsing the previous composite binding? 
                    if (currentCompositionItems.Count > 0)
                    {
                        actionBindings.Add(string.Join(separator, currentCompositionItems));
                        currentCompositionItems.Clear();
                    }
                }
                
                if (!collection.CanParse(inputBinding))
                    continue;
                
                Match match = BindingNameRegex.Match(inputBinding.path);
                if (!match.Success)
                    continue;

                if (inputBinding.isPartOfComposite && specifyCompositeNames != null && specifyCompositeNames.Length > 0) 
                {
                    bool ignore = true;
                    for (int j = 0; j < specifyCompositeNames.Length; j++) 
                    {
                        if (string.Equals(specifyCompositeNames[j], inputBinding.name, StringComparison.Ordinal)) 
                        {
                            ignore = false;
                            break;
                        }
                    }

                    if (ignore)
                        continue;
                }


                string bindingIdentifier = match.Groups[2].Value;
                if (TryGetDisplayValueFromBindingName(bindingIdentifier, collection, out string displayValue))
                    currentCompositionItems.Add(displayValue);
            }

            if (currentCompositionItems.Count > 0)
            {
                actionBindings.Add(string.Join(separator, currentCompositionItems));
                currentCompositionItems.Clear();
            }
            ListPool<string>.Release(currentCompositionItems);
            
            if (actionBindings.Count == 0)
            {
                string bindingDisplayString = inputAction.GetBindingDisplayString();
                Debug.LogError($"Failed to find proper replacement text for {bindingDisplayString} [{inputAction}] for identifier {targetCollection}");
                actionBindings.Add(bindingDisplayString);
            }

            if (onlyFirstResult)
                actionBindings.RemoveRange(1, actionBindings.Count - 1);
            
            returnResultString = string.Join(alternativeSeparator, actionBindings);
            if (!string.IsNullOrEmpty(format))
                returnResultString = string.Format(format, returnResultString);

            ListPool<string>.Release(actionBindings);

            InputActionGuidToDisplayStringCache.Add(key, returnResultString);
            return returnResultString;
        }

        private static bool TryGetCollectionByIdentifier(string targetCollection, out SpriteAssetCollection spriteAssetCollection)
        {
            if (!CollectionIdentifierToAssetCollectionCache.TryGetValue(targetCollection, out spriteAssetCollection))
            {
                for (int i = 0; i < AvailableCollections.Count; i++)
                {
                    SpriteAssetCollection availableCollection = AvailableCollections[i];
                    if (availableCollection.MatchIdentifier(targetCollection))
                    {
                        spriteAssetCollection = availableCollection;
                        CollectionIdentifierToAssetCollectionCache.Add(targetCollection, spriteAssetCollection);
                        return true;
                    }
                }

                CollectionIdentifierToAssetCollectionCache.Add(targetCollection, null);
                return false;
            }

            return spriteAssetCollection != null;
        }

        private static void EnsureCollection(InputDevice targetDevice, ref string targetCollection)
        {
            if (!string.IsNullOrEmpty(targetCollection))
                return;

            if (targetDevice == null)
            {
                targetCollection = LastKnowDeviceIdentifier;
                return;
            }
            
            targetCollection = GetCollectionIdentifierFromDevice(targetDevice);
        }

        private static bool TryGetDisplayValueFromBindingName(string inputIdentifier, SpriteAssetCollection spriteAssetCollection, out string resultDisplayValue)
        {
            for (int i = 0; i < spriteAssetCollection.Count; i++)
            {
                SpriteAssetId spriteAssetId = spriteAssetCollection[i];
                if (spriteAssetId.Match(inputIdentifier))
                {
                    resultDisplayValue = spriteAssetId.GetDisplayString();
                    return true;
                }
            }

            resultDisplayValue = string.Empty;
            return false;
        }

        private static void Initialize()
        {
            if (Initialized)
                return;

            CollectionsRegistry.Instance.TryGetCollectionsOfType(out AvailableCollections);

            if (AvailableCollections.Count > 0)
                IsAvailable = true;
            
            LastKnowDeviceIdentifier = MOUSE_AND_KEYBOARD_IDENTIFIER;
            
            InputSystem.onEvent -= OnInputSystemEvent;
            InputSystem.onEvent += OnInputSystemEvent;
            
            Initialized = true;
        }

        private static void OnInputSystemEvent(InputEventPtr eventPtr, InputDevice inputDevice)
        {
            if (inputDevice == LastKnowDevice)
                return;
            
            if (eventPtr.type != StateEvent.Type) 
                return;
 
            bool validPress = false;
            InputControlExtensions.InputEventControlCollection inputEventControlCollection = eventPtr.EnumerateChangedControls(inputDevice, 0.01f);
            foreach (InputControl unused in inputEventControlCollection)
            {
                validPress = true;
                break;
            }
            
            if (!validPress) 
                return;
 
            LastKnowDevice = inputDevice;
            LastKnowDeviceIdentifier = GetCollectionIdentifierFromDevice(inputDevice);
            InputDeviceChangedEvent?.Invoke();
        }

        private static string GetCollectionIdentifierFromDevice(InputDevice inputDevice)
        {
            if (inputDevice is Keyboard or Mouse)
                return MOUSE_AND_KEYBOARD_IDENTIFIER;

            if (inputDevice is Joystick or Gamepad)
            {
                string deviceString = inputDevice.ToString();
                string descriptionManufacturer = inputDevice.description.manufacturer;
                
                if (deviceString.IndexOf("DualShock", StringComparison.Ordinal) > -1
                    || deviceString.IndexOf("DualSense", StringComparison.Ordinal) > -1
                    || descriptionManufacturer.IndexOf("Sony", StringComparison.Ordinal) > -1)
                {
                    return PLAYSTATION_IDENTIFIER;
                }

                if (deviceString.IndexOf("XInput", StringComparison.OrdinalIgnoreCase) > -1 ||
                    descriptionManufacturer.IndexOf("Microsoft", StringComparison.Ordinal) > -1)
                {
                    return XBOX_IDENTIFIER;
                }

                if (deviceString.IndexOf("Steam", StringComparison.OrdinalIgnoreCase) > -1 ||
                    descriptionManufacturer.IndexOf("Valve", StringComparison.Ordinal) > -1)
                {
                    return STEAM_DECK_IDENTIFIER;
                }

                if (deviceString.IndexOf("Switch", StringComparison.OrdinalIgnoreCase) > -1 ||
                    descriptionManufacturer.IndexOf("Nintendo", StringComparison.Ordinal) > -1)
                {
                    return SWITCH_IDENTIFIER;
                }

            }
            return GENERIC_IDENTIFIER;
        }
    }
}
