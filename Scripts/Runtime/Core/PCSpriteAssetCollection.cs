using System;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrunoMikoski.InputSpriteMap
{
    [CreateAssetMenu(menuName = "ScriptableObject Collection/Collections/Input/Create PCSpriteAssetCollection", fileName = "PCSpriteAssetCollection", order = 0)]
    public class PCSpriteAssetCollection : ScriptableObjectCollection<SpriteAssetId>
    {
        public virtual string ParseInputControlName(InputControl targetInputControl)
        {
            string targetBindingName = targetInputControl.name;
            if (targetInputControl.path.IndexOf("mouse/position/", StringComparison.OrdinalIgnoreCase) > -1
                || targetInputControl.path.IndexOf("mouse/delta/", StringComparison.OrdinalIgnoreCase) > -1)
                return "mouseSimple";

            return targetBindingName;
        }
    }
}
