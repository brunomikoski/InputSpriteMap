using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrunoMikoski.InputSpriteMap
{
    [CreateAssetMenu(menuName = "ScriptableObject Collection/Collections/Input/Create GamepadSpriteAssetCollection", fileName = "GamepadSpriteAssetCollection", order = 0)]
    public class GamepadSpriteAssetCollection : SpriteAssetCollection
    {
        public override string ParseInputControlName(InputControl targetInputControl)
        {
            string targetBindingName = targetInputControl.name;
            if (targetInputControl.path.IndexOf("dpad", StringComparison.OrdinalIgnoreCase) > -1)
                return $"dpad/{targetBindingName}";
            //If stick press, we return directly
            if (targetInputControl.path.IndexOf("leftStickPress", StringComparison.OrdinalIgnoreCase) > -1
                || targetInputControl.path.IndexOf("rightStickPress", StringComparison.OrdinalIgnoreCase) > -1)
                return targetBindingName;

            if (targetInputControl.path.IndexOf("leftStick", StringComparison.OrdinalIgnoreCase) > -1)
                return $"leftStick/{targetBindingName}";
            if (targetInputControl.path.IndexOf("rightStick", StringComparison.OrdinalIgnoreCase) > -1)
                return $"rightStick/{targetBindingName}";
            return targetBindingName;
        }
    }
}
