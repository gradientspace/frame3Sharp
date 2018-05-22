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

        public static Text FindText(GameObject parentGO, string textName) {
            return UnityUtil.FindChildByName(parentGO, textName).GetComponent<Text>();
        }


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
            if ( handler != null )
                button.onClick.AddListener(handler);
            return button;
        }
        public static Button FindButtonAndAddClickHandler(GameObject parentGO, string buttonName, UnityAction handler)
        {
            var button = UnityUtil.FindChildByName(parentGO, buttonName).GetComponent<Button>();
            if ( handler != null )
                button.onClick.AddListener(handler);
            return button;
        }
        public static Button GetButtonAndAddClickHandler(GameObject parentGO, UnityAction handler)
        {
            var button = parentGO.GetComponent<Button>();
            if ( handler != null )
                button.onClick.AddListener(handler);
            return button;
        }



        public static Button FindButtonAndAddToggleBehavior(string buttonName, Func<bool> getValue, Action<bool> setValue, Action<bool, Button> updateF)
        {
            var button = UnityUtil.FindGameObjectByName(buttonName).GetComponent<Button>();
            AddToggleBehavior(button, getValue, setValue, updateF);
            return button;
        }
        public static Button FindButtonAndAddToggleBehavior(GameObject parentGO, string buttonName, 
            Func<bool> getValue, Action<bool> setValue, Action<bool, Button> updateF,
            bool bWatchForUpdates = false)
        {
            var button = UnityUtil.FindChildByName(parentGO, buttonName).GetComponent<Button>();
            AddToggleBehavior(button, getValue, setValue, updateF, bWatchForUpdates);
            return button;
        }
        public static void AddToggleBehavior(Button button, 
            Func<bool> getValue, Action<bool> setValue, Action<bool, Button> updateF,
            bool bWatchForUpdates = false)
        {
            button.onClick.AddListener(() => {
                bool curState = getValue();
                setValue(!curState);
                bool newState = getValue();
                if (newState != curState)
                    updateF(newState, button);

            });
            updateF(getValue(), button);
            if (bWatchForUpdates) {
                var watcher = button.gameObject.AddComponent<ToggleButtonWatcher>();
                watcher.button = button;
                watcher.getValueF = getValue;
                watcher.updateF = updateF;
                watcher.cur_value = getValue();
            }
        }

        class ToggleButtonWatcher : MonoBehaviour
        {
            public Button button;
            public Func<bool> getValueF;
            public Action<bool, Button> updateF;

            public bool cur_value = false;

            public void Update()
            {
                if ( getValueF() != cur_value ) {
                    cur_value = getValueF();
                    updateF(cur_value, button);
                }
            }
        }





        public static void SetButtonText(Button button, string newLabel)
        {
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
                text.text = newLabel;
            else
                DebugUtil.Log("SetButtonText: button " + button.name + " has no Text component!");
        }




        public static void SetDisabledColor(Button button, Colorf color)
        {
            var newColorBlock = button.colors;
            newColorBlock.disabledColor = color;
            button.colors = newColorBlock;
        }


        public static void SetColors(Button button, Colorf normalColor, Colorf disabledColor)
        {
            var newColorBlock = button.colors;
            newColorBlock.normalColor = normalColor;
            newColorBlock.highlightedColor = ColorMixer.Darken(normalColor, 0.9f);
            newColorBlock.disabledColor = disabledColor;
            button.colors = newColorBlock;
        }



        public static Toggle FindToggleAndSet(GameObject toggleGO, bool value)
        {
            var toggle = toggleGO.GetComponent<Toggle>();
            if (toggle != null)
                toggle.isOn = value;
            return toggle;
        }
        public static Toggle FindToggleAndSet(GameObject parentGO, string toggleName, bool value)
        {
            var toggle = UnityUtil.FindChildByName(parentGO, toggleName).GetComponent<Toggle>();
            if (toggle != null)
                toggle.isOn = value;
            return toggle;
        }

        public static Toggle FindToggleAndAddHandler(GameObject parentGO, string toggleName, UnityAction<bool> handler)
        {
            var toggle = UnityUtil.FindChildByName(parentGO, toggleName).GetComponent<Toggle>();
            if (handler != null)
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
            AddIntHandlers(field, getValue, setValue, minValue, maxValue);
            return field;
        }
        public static InputField FindInputAndAddIntHandlers(GameObject parentGO, string inputName, Func<int> getValue, Action<int> setValue, int minValue, int maxValue)
        {
            var field = UnityUtil.FindChildByName(parentGO, inputName).GetComponent<InputField>();
            AddIntHandlers(field, getValue, setValue, minValue, maxValue);
            return field;
        }
        public static void AddIntHandlers(InputField field, Func<int> getValue, Action<int> setValue, int minValue, int maxValue)
        {
            field.onEndEdit.AddListener((fieldString) => {
                try {
                    int value = int.Parse(fieldString);
                    if (value < minValue) value = minValue;
                    if (value > maxValue) value = maxValue;
                    setValue(value);
                    field.text = getValue().ToString();
                } catch { return; }
            });
            field.text = getValue().ToString();
        }



        public static InputField FindInputAndAddFloatHandlers(string inputName, Func<float> getValue, Action<float> setValue, float minValue, float maxValue)
        {
            var field = UnityUtil.FindGameObjectByName(inputName).GetComponent<InputField>();
            AddFloatHandlers(field, getValue, setValue, minValue, maxValue);
            return field;
        }
        public static InputField FindInputAndAddFloatHandlers(GameObject parentGO, string inputName, Func<float> getValue, Action<float> setValue, float minValue, float maxValue)
        {
            var field = UnityUtil.FindChildByName(parentGO, inputName).GetComponent<InputField>();
            AddFloatHandlers(field, getValue, setValue, minValue, maxValue);
            return field;
        }
        public static void AddFloatHandlers(InputField field, Func<float> getValue, Action<float> setValue, float minValue, float maxValue)
        {
            field.onEndEdit.AddListener((fieldString) => {
                try {
                    float value = float.Parse(fieldString);
                    if (value < minValue) value = minValue;
                    if (value > maxValue) value = maxValue;
                    setValue(value);
                    field.text = getValue().ToString();
                } catch { return; }
            });
            field.text = getValue().ToString();
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
        public static Dropdown FindDropDownAndAddHandlers(GameObject parentGO, string inputName) {
            return UnityUtil.FindChildByName(parentGO, inputName).GetComponent<Dropdown>();
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

        public static TMP_Text FindTMPText(GameObject parent, string textName)
        {
            var field = parent.FindChildByName(textName, true).GetComponent<TMP_Text>();
            return field;
        }

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


        public static void SetFloatNumberValidator(TMP_InputField field)
        {
            field.onValidateInput = (text, pos, ch) => {
                return ValidateDecimal_TMP(text, pos, ch, field);
            };
        }


        static char LocalDecimalSeparator {
            get { return System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]; }
        }

        /// <summary>
        /// validator for decimal-number TMP_InputField that works with . and , decimal separators. Allows negative numbers. 
        /// TMP_InputField.onValidateInput is passed placeholder text, even if character will replace that text.
        /// Similarly if text is fully selected, ie if character will replace text, we still get current text.
        /// So, we have to pass the inputfield, and special-case the situation where the number would be replaced.
        /// </summary>
        public static char ValidateDecimal_TMP(string text, int pos, char ch, TMP_InputField field)
        {
            if (Char.IsDigit(ch) == false && ch != '-' && ch != ',' && ch != '.') return '\0';
            bool all_selected = Math.Abs(field.selectionFocusPosition - field.selectionAnchorPosition) == field.text.Length;
            bool is_sep = (ch == '.' || ch == ',');
            if (is_sep) {
                char sep = LocalDecimalSeparator;
                if (sep == '.' && ch == ',')
                    return '\0';
                if (sep == ',' && ch == '.')
                    ch = ',';
            }
            if (all_selected)
                return ch;

            if (ch == '-' && pos != 0)        return '\0';
            if (is_sep && text.Contains(ch))  return '\0';
            if (text.Length == 1 && text[0] == '-' && is_sep)
                return ch;

            float f;
            if (float.TryParse(text.Insert(pos, ch.ToString()), out f))
                return ch;
            return '\0';
        }

#endif






        /*
         * RectTransform manipulation
         */



        public static AxisAlignedBox2f GetBounds2D(GameObject go)
        {
            RectTransform rectT = go.GetComponent<RectTransform>();
            AxisAlignedBox2f box = rectT.rect;
            box.Translate(rectT.anchoredPosition);
            return box;
        }
        public static AxisAlignedBox2f GetBounds2D(RectTransform rectT)
        {
            AxisAlignedBox2f box = rectT.rect;
            box.Translate(rectT.anchoredPosition);
            return box;
        }

        public static void Translate(RectTransform rectT, Vector2f translate)
        {
            rectT.anchoredPosition = (Vector2f)rectT.anchoredPosition + translate;
        }


        public static void PositionRelative2D(GameObject setGO, BoxPosition setBoxPos, GameObject relativeToGO, BoxPosition relBoxPos, Vector2f offset)
        {
            RectTransform setRectT = setGO.GetComponent<RectTransform>();
            AxisAlignedBox2f setBox = GetBounds2D(setRectT);
            Vector2f fromPos = BoxModel.GetBoxPosition(ref setBox, setBoxPos);

            AxisAlignedBox2f relBox = GetBounds2D(relativeToGO);
            Vector2f toPos = BoxModel.GetBoxPosition(ref relBox, relBoxPos);

            Vector2f dv = toPos - fromPos + offset;

            Translate(setRectT, dv);
        }







        /// <summary>
        /// fallback sprite for when things go wrong (red image)
        /// </summary>
        public static Sprite DefaultSprite {
            get { return Resources.Load<Sprite>("icons/f3_default_sprite"); }
        }


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
                    if (sprite == null)
                        sprite = DefaultSprite;
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









        /// <summary>
        /// Utility wrapper around a UnityUI DropDown that handles mapping between sequential integer
        /// indices and arbitrary ID integers that we want to associate with label strings
        /// </summary>
        public class MappedDropDown
        {
            public Dropdown drop;
            Func<int> getId;
            Action<int> setId;

            public MappedDropDown(Dropdown d, Func<int> getId, Action<int> setId)
            {
                drop = d;
                this.getId = getId;
                this.setId = setId;
                UnityUIUtil.DropDownAddHandlers(drop, getIndexFromId, setIdFromIndex, 0, int.MaxValue );
            }

            List<string> options = new List<string>();
            List<int> identifiers = new List<int>();

            public void SetOptions(List<string> options, List<int> ids)
            {
                this.options = new List<string>(options);
                this.identifiers = new List<int>(ids);

                drop.ClearOptions();
                drop.AddOptions(options);
            }

            public void SetFromId(int id)
            {
                drop.value = getIndexFromId();
            }


            int getIndexFromId()
            {
                int id = getId();
                int idx = identifiers.FindIndex((j) => { return j == id; });
                return (idx < 0 || idx >= options.Count) ? 0 : idx;
            }
            void setIdFromIndex(int dropdown_index)
            {
                int id = identifiers[dropdown_index];
                setId(id);
            }

        }


    }






    // create text mesh
    // [TODO] only used by HUDRadialMenu, can get rid of when
    //   we replace that with fText...
    public class TextLabelGenerator : IGameObjectGenerator
    {
        public string Text { get; set; }
        public float Scale { get; set; }
        public Vector3 Translate { get; set; }
        public Colorf Color { get; set; }
        public float ZOffset { get; set; }

        public TextAlignment TextAlign { get; set; }

        public enum Alignment
        {
            Default, HCenter, VCenter, HVCenter
        }
        public Alignment Align { get; set; }

        public TextLabelGenerator()
        {
            Text = "(label)";
            TextAlign = TextAlignment.Center;
            Scale = 0.1f;
            Translate = new Vector3f(0, 0, 0);
            Color = ColorUtil.make(10, 10, 10);
            ZOffset = -1.0f;
        }

        public List<fGameObject> Generate()
        {
            var gameObj = new GameObject("label");

            TextMesh tm = gameObj.AddComponent<TextMesh>();
            tm.text = Text;
            tm.color = Color;
            tm.fontSize = 50;
            tm.offsetZ = ZOffset;
            tm.alignment = TextAlign;

            // [RMS] this isn't quite right, on the vertical centering...
            Vector2f size = UnityUtil.EstimateTextMeshDimensions(tm);
            Vector3 vCenterShift = Vector3f.Zero;
            if (Align == Alignment.HCenter || Align == Alignment.HVCenter)
                vCenterShift.x -= size.x * 0.5f;
            if (Align == Alignment.VCenter || Align == Alignment.HVCenter)
                vCenterShift.y += size.y * 0.5f;

            // apply orientation
            float useScale = Scale;
            tm.transform.localScale = new Vector3(useScale, useScale, useScale);
            tm.transform.localPosition += useScale * vCenterShift;
            tm.transform.localPosition += Translate;

            // ignore material changes when we add to GameObjectSet
            gameObj.AddComponent<IgnoreMaterialChanges>();

            // use our textmesh material instead
            // [TODO] can we share between texts?
            MaterialUtil.SetTextMeshDefaultMaterial(tm);

            return new List<fGameObject>() { gameObj };
        }
    }

}
