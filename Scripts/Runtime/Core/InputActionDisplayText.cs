using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrunoMikoski.InputSpriteMap
{
    [RequireComponent(typeof(TMP_Text))]
    public class InputActionDisplayText : MonoBehaviour
    {
        private bool hasCachedText;
        private TMP_Text cachedText;
        private TMP_Text TMPText
        {
            get
            {
                if (!hasCachedText)
                {
                    cachedText = GetComponent<TMP_Text>();
                    hasCachedText = cachedText != null;
                }

                return cachedText;
            }
        }

        private bool initialized;
        private InputAction displayingInputAction;
        private string displayingFormat;
        private string displayingCompositionSeparator;
        private string[] displayingSpecifiedCompositionParts;
        


        protected void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            InputParser.InputDeviceChangedEvent -= OnInputDeviceChanged;
        }

        private void Initialize()
        {
            if (initialized)
                return;

            InputParser.InputDeviceChangedEvent += OnInputDeviceChanged;
            initialized = true;
        }

        private void OnInputDeviceChanged()
        {
            UpdateDisplayingText();
        }

        public void ShowInputAction(InputAction inputAction, string targetFormat = "", string separator = "", string[] specifiedCompositionParts = null)
        {
            displayingInputAction = inputAction;
            displayingFormat = targetFormat;
            displayingCompositionSeparator = separator;
            displayingSpecifiedCompositionParts = specifiedCompositionParts;

            UpdateDisplayingText();
        }

        private void UpdateDisplayingText()
        {
            Initialize();
            if (displayingInputAction == null)
                return;

            string displayValue = InputParser.ParseInputAction(displayingInputAction, null, displayingFormat, displayingCompositionSeparator, "or",
                displayingSpecifiedCompositionParts);

            if (displayValue.Equals(TMPText.text, StringComparison.Ordinal))
                return;

            TMPText.SetText(displayValue);
        }

        public virtual void Clear()
        {
            TMPText.SetText(string.Empty);
            displayingInputAction = null;
            displayingFormat = string.Empty;
            displayingCompositionSeparator = string.Empty;
            displayingSpecifiedCompositionParts = null;
        }
    }
}
