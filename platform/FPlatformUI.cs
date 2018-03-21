using System;

using UnityEngine.EventSystems;
using UnityEngine.UI;

#if F3_ENABLE_TEXT_MESH_PRO
using TMPro;
#endif


namespace f3
{
    public static class FPlatformUI
    {
        public static bool IsConsumingMouseInput()
        {
            // [RMS] this works?
            if (EventSystem.current == null)
                return false;
            bool over_go = EventSystem.current.IsPointerOverGameObject();
            return over_go;
        }


        public static bool TextEntryFieldHasFocus()
        {
            var focusObj = EventSystem.current.currentSelectedGameObject;
            if (focusObj == null)
                return false;
            if (focusObj.GetComponent<InputField>() != null)
                return true;
#if F3_ENABLE_TEXT_MESH_PRO
            if (focusObj.GetComponent<TMP_InputField>() != null)
                return true;
#endif
            return false;
        }
    }
}
