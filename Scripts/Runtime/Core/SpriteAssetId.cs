#undef UNITY
using System;
using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;
using UnityEngine.Pool;

namespace BrunoMikoski.InputSpriteMap
{
    public class SpriteAssetId : ScriptableObjectCollectionItem
    {
        [SerializeField]
        public string[] additionalNames = Array.Empty<string>();

        [SerializeField] 
        private string displayFormat = @"<sprite name=""{0}""/>";

        [SerializeField, HideInInspector]
        private string spriteName;

        public string GetDisplayString(string format = null)
        {
            if (format == null)
                format = displayFormat;

            return string.Format(format, name);
        }

        public bool Matches(string targetName)
        {
            if (name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                return true;
            
            for (int i = 0; i < additionalNames.Length; i++)
            {
                string additionalName = additionalNames[i];
                if (targetName.IndexOf(additionalName, StringComparison.Ordinal) > -1)
                    return true;
            }

            return false;
        }

        public bool Match(string targetName)
        {
            if (name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                return true;
            
            for (int i = 0; i < additionalNames.Length; i++)
            {
                string additionalName = additionalNames[i];
                if (targetName.Equals(additionalName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public void SetSpriteData(string targetSpriteName)
        {
            spriteName = targetSpriteName;
        }

    }
}