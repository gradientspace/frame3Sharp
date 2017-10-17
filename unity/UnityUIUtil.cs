using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace f3
{
    public static class UnityUIUtil
    {

        public static Button FindButtonAndAddClickHandler(string buttonName, UnityAction handler)
        {
            var button = UnityUtil.FindGameObjectByName(buttonName).GetComponent<Button>();
            button.onClick.AddListener(handler);
            return button;
        }


        public static Button FindButtonAndAddToggleBehavior(string buttonName, Func<bool> getValue, Action<bool> setValue, Action<bool,Button> updateF )
        {
            var button = UnityUtil.FindGameObjectByName(buttonName).GetComponent<Button>();
            button.onClick.AddListener( () => {
                bool curState = getValue();
                setValue(! curState);
                bool newState = getValue();
                if (newState != curState)
                    updateF(newState, button);

            });
            updateF(getValue(), button);
            return button;
        }





        public static InputField FindInputAndAddValueChangedHandler(string inputName, UnityAction<string> handler)
        {
            var field = UnityUtil.FindGameObjectByName(inputName).GetComponent<InputField>();
            field.onValueChanged.AddListener(handler);
            return field;
        }

        public static InputField FindInputAndAddIntHandlers(string inputName, Func<int> getValue, Action<int> setValue, int minValue, int maxValue)
        {
            var field = UnityUtil.FindGameObjectByName(inputName).GetComponent<InputField>();
            field.onValueChanged.AddListener((fieldString) => {
                int value = int.Parse(fieldString);
                if (value < minValue) value = minValue;
                if (value > maxValue) value = maxValue;
                setValue(value);
            });
            field.text = getValue().ToString();
            return field;
        }


        public static InputField FindInputAndAddFloatHandlers(string inputName, Func<float> getValue, Action<float> setValue, float minValue, float maxValue)
        {
            var field = UnityUtil.FindGameObjectByName(inputName).GetComponent<InputField>();
            field.onValueChanged.AddListener((fieldString) => {
                float value = float.Parse(fieldString);
                if (value < minValue) value = minValue;
                if (value > maxValue) value = maxValue;
                setValue(value);
            });
            field.text = getValue().ToString();
            return field;
        }

    }
}
