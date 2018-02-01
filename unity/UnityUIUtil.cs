using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
#if F3_ENABLE_TEXT_MESH_PRO
using TMPro;
#endif

using g3;

namespace f3
{
    public static class UnityUIUtil
    {
        public static Text FindTextAndSet(string textName, string newText)
        {
            var text = UnityUtil.FindGameObjectByName(textName).GetComponent<Text>();
            text.text = newText;
            return text;
        }
        public static Text FindTextAndSet(GameObject parentGO, string textName, string newText)
        {
            var text = UnityUtil.FindChildByName(parentGO, textName).GetComponent<Text>();
            text.text = newText;
            return text;
        }


        public static Button FindButtonAndAddClickHandler(string buttonName, UnityAction handler)
        {
            var button = UnityUtil.FindGameObjectByName(buttonName).GetComponent<Button>();
            button.onClick.AddListener(handler);
            return button;
        }
        public static Button FindButtonAndAddClickHandler(GameObject parentGO, string buttonName, UnityAction handler)
        {
            var button = UnityUtil.FindChildByName(parentGO, buttonName).GetComponent<Button>();
            button.onClick.AddListener(handler);
            return button;
        }




        public static Button FindButtonAndAddToggleBehavior(string buttonName, Func<bool> getValue, Action<bool> setValue, Action<bool, Button> updateF)
        {
            var button = UnityUtil.FindGameObjectByName(buttonName).GetComponent<Button>();
            AddToggleBehavior(button, getValue, setValue, updateF);
            return button;
        }
        public static Button FindButtonAndAddToggleBehavior(GameObject parentGO, string buttonName, Func<bool> getValue, Action<bool> setValue, Action<bool, Button> updateF)
        {
            var button = UnityUtil.FindChildByName(parentGO, buttonName).GetComponent<Button>();
            AddToggleBehavior(button, getValue, setValue, updateF);
            return button;
        }
        public static void AddToggleBehavior(Button button, Func<bool> getValue, Action<bool> setValue, Action<bool, Button> updateF)
        {
            button.onClick.AddListener(() => {
                bool curState = getValue();
                setValue(!curState);
                bool newState = getValue();
                if (newState != curState)
                    updateF(newState, button);

            });
            updateF(getValue(), button);
        }



        public static void SetDisabledColor(Button button, Color color)
        {
            var newColorBlock = button.colors;
            newColorBlock.disabledColor = color;
            button.colors = newColorBlock;
        }


        public static void SetColors(Button button, Color normalColor, Color disabledColor)
        {
            var newColorBlock = button.colors;
            newColorBlock.normalColor = normalColor;
            newColorBlock.highlightedColor = ColorMixer.Darken(normalColor, 0.9f);
            newColorBlock.disabledColor = disabledColor;
            button.colors = newColorBlock;
        }


        public static Toggle FindToggleAndAddHandler(string toggleName, UnityAction<bool> handler)
        {
            var toggle = UnityUtil.FindGameObjectByName(toggleName).GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(handler);
            return toggle;
        }



        public static Toggle FindToggleAndConnectToSource(string toggleName, Func<bool> getValue, Action<bool> setValue) {
            var toggle = UnityUtil.FindGameObjectByName(toggleName).GetComponent<Toggle>();
            ToggleConnectToSource(toggle, getValue, setValue);
            return toggle;
        }
        public static Toggle FindToggleAndConnectToSource(GameObject parentGO, string toggleName, Func<bool> getValue, Action<bool> setValue) {
            var toggle = UnityUtil.FindChildByName(parentGO, toggleName).GetComponent<Toggle>();
            ToggleConnectToSource(toggle, getValue, setValue);
            return toggle;
        }
        public static void ToggleConnectToSource(Toggle toggle, Func<bool> getValue, Action<bool> setValue)
        {
            toggle.onValueChanged.AddListener((b) => {
                setValue(b);
                toggle.isOn = getValue();
            });
            toggle.isOn = getValue();
        }


        public static void SetBackgroundColor(Toggle toggle, Color color)
        {
            var newColorBlock = toggle.colors;
            newColorBlock.normalColor = color;
            newColorBlock.highlightedColor = color;
            toggle.colors = newColorBlock;
        }




        public static InputField FindInput(string inputName)
        {
            var field = UnityUtil.FindGameObjectByName(inputName).GetComponent<InputField>();
            return field;
        }
        public static InputField FindInput(GameObject parent, string inputName)
        {
            var field = UnityUtil.FindChildByName(parent, inputName).GetComponent<InputField>();
            return field;
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
                try {
                    int value = int.Parse(fieldString);
                    if (value < minValue) value = minValue;
                    if (value > maxValue) value = maxValue;
                    setValue(value);
                } catch { return; }
            });
            field.text = getValue().ToString();
            return field;
        }


        public static InputField FindInputAndAddFloatHandlers(string inputName, Func<float> getValue, Action<float> setValue, float minValue, float maxValue)
        {
            var field = UnityUtil.FindGameObjectByName(inputName).GetComponent<InputField>();
            field.onValueChanged.AddListener((fieldString) => {
                try {
                    float value = float.Parse(fieldString);
                    if (value < minValue) value = minValue;
                    if (value > maxValue) value = maxValue;
                    setValue(value);
                } catch { return; }
            });
            field.text = getValue().ToString();
            return field;
        }



        public static void SetBackgroundColor(InputField field, Color color)
        {
            var newColorBlock = field.colors;
            newColorBlock.normalColor = color;
            newColorBlock.highlightedColor = color;
            field.colors = newColorBlock;
        }




        public static Dropdown FindDropDownAndAddHandlers(string inputName, Func<int> getValue, Action<int> setValue) {
            var dropdown = UnityUtil.FindGameObjectByName(inputName).GetComponent<Dropdown>();
            DropDownAddHandlers(dropdown, getValue, setValue, int.MinValue, int.MaxValue);
            return dropdown;
        }
        public static Dropdown FindDropDownAndAddHandlers(GameObject parentGO, string inputName, Func<int> getValue, Action<int> setValue) {
            var dropdown = UnityUtil.FindChildByName(parentGO, inputName).GetComponent<Dropdown>();
            DropDownAddHandlers(dropdown, getValue, setValue, int.MinValue, int.MaxValue);
            return dropdown;
        }


        public static Dropdown FindDropDownAndAddHandlers(string inputName, Func<int> getValue, Action<int> setValue, int minValue, int maxValue) {
            var dropdown = UnityUtil.FindGameObjectByName(inputName).GetComponent<Dropdown>();
            DropDownAddHandlers(dropdown, getValue, setValue, minValue, maxValue);
            return dropdown;
        }
        public static Dropdown FindDropDownAndAddHandlers(GameObject parentGO, string inputName, Func<int> getValue, Action<int> setValue, int minValue, int maxValue) {
            var dropdown = UnityUtil.FindChildByName(parentGO, inputName).GetComponent<Dropdown>();
            DropDownAddHandlers(dropdown, getValue, setValue, minValue, maxValue);
            return dropdown;
        }
        public static void DropDownAddHandlers(Dropdown dropdown, Func<int> getValue, Action<int> setValue, int minValue, int maxValue)
        {
            dropdown.onValueChanged.AddListener((value) => {
                if (value < minValue) value = minValue;
                if (value > maxValue) value = maxValue;
                setValue(value);
                dropdown.value = getValue();
            });
            dropdown.value = getValue();
        }




#if F3_ENABLE_TEXT_MESH_PRO


        public static TMP_InputField FindTMPInputAndAddHandlers(GameObject parent, string inputName, UnityAction<string> editHandler)
        {
            var field = parent.FindChildByName(inputName, true).GetComponent<TMP_InputField>();
            if (editHandler != null)
                field.onEndEdit.AddListener(editHandler);
            return field;
        }

        public static TMP_Dropdown FindTMPDropDownAndAddHandlers(GameObject parent, string inputName, UnityAction<int> valueChangeHandler)
        {
            var dropdown = parent.FindChildByName(inputName, true).GetComponent<TMP_Dropdown>();
            if (valueChangeHandler != null)
                dropdown.onValueChanged.AddListener(valueChangeHandler);
            return dropdown;
        }



        public static void SetBackgroundColor(TMP_InputField field, Color color) {
            var newColorBlock = field.colors;
            newColorBlock.normalColor = color;
            newColorBlock.highlightedColor = color;
            field.colors = newColorBlock;
        }
#endif







        /// <summary>
        /// Load Sprite from path on-demand 
        /// </summary>
        public class AutoSprite
        {
            string path;
            Sprite sprite;
            public AutoSprite(string resource_path)
            {
                path = resource_path;
            }
            public Sprite Sprite {
                get {
                    if (sprite == null)
                        sprite = Resources.Load<Sprite>(path);
                    return sprite;
                }
            }
        }




        /// <summary>
        /// Utility class for tabbing between elements in a dialog. Add them in-order, then
        /// use following code in Update()
        /// 
        /// if (Input.GetKeyDown(KeyCode.Tab)) {
        ///     if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        ///         tabber.Previous();
        ///     else
        ///         tabber.Next();
        /// }
        /// </summary>
        public class DialogTabber
        {
            List<GameObject> tab_order = new List<GameObject>();

            public void Add(GameObject go) {
                tab_order.Add(go);
            }

            public void Next()
            {
                GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
                if (selectedObject != null) {
                    for (int k = 0; k < tab_order.Count; k++) {
                        if (tab_order[k] == selectedObject) {
                            int next = (k + 1) % tab_order.Count;
                            EventSystem.current.SetSelectedGameObject(tab_order[next]);
                            return;
                        }
                    }
                } else {
                    EventSystem.current.SetSelectedGameObject(tab_order[0]);
                    return;
                }
            }


            public void Previous()
            {
                GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
                if (selectedObject != null) {
                    for (int k = 0; k < tab_order.Count; k++) {
                        if (tab_order[k] == selectedObject) {
                            int prev = (k == 0) ? tab_order.Count - 1 : k - 1;
                            EventSystem.current.SetSelectedGameObject(tab_order[prev]);
                            return;
                        }
                    }
                } else {
                    EventSystem.current.SetSelectedGameObject(tab_order[tab_order.Count-1]);
                    return;
                }
            }

        }




    }
}
