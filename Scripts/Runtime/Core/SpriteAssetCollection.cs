using System;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrunoMikoski.InputSpriteMap
{
    [CreateAssetMenu(menuName = "ScriptableObject Collection/Collections/Input/Create InputSpriteAssetCollection", fileName = "InputSpriteAssetCollection", order = 0)]
    public class SpriteAssetCollection : ScriptableObjectCollection<SpriteAssetId>
    {
        [SerializeField]
        private string targetSpriteAssetGuid;
        public string TargetSpriteAssetGuid => targetSpriteAssetGuid;

        [SerializeField]
        public string[] identifiers;

        [SerializeField] 
        private string[] bindingPrefixes = Array.Empty<string>();
        
        public bool MatchIdentifier(string targetCollectionIdentifier)
        {
            if (string.IsNullOrEmpty(targetCollectionIdentifier))
                return true;

            if (string.Equals(this.name, targetCollectionIdentifier, StringComparison.OrdinalIgnoreCase))
                return true;
            
            for (int i = 0; i < identifiers.Length; i++)
            {
                if (string.Equals(targetCollectionIdentifier, identifiers[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public bool CanParse(InputBinding inputBinding)
        {
            if (bindingPrefixes.Length == 0)
                return true;
            
            for (int i = 0; i < bindingPrefixes.Length; i++)
            {
                if (inputBinding.path.IndexOf(bindingPrefixes[i], StringComparison.OrdinalIgnoreCase) > -1)
                    return true;
            }

            return false;
        }

        public virtual string ParseInputControlName(InputControl targetInputControl)
        {
            return targetInputControl.name;
        }
        
    }
}
