using System;



namespace f3
{
    public static class FPlatformUI
    {
        public static bool IsConsumingMouseInput()
        {
            // [RMS] this works?
            if (UnityEngine.EventSystems.EventSystem.current == null)
                return false;
            bool over_go = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            return over_go;
        }


        public static bool TextEntryFieldHasFocus()
        {
            var focusObj = UnityEngine.EventSystems.EventSystem.current;
            if (focusObj == null)
                return false;
            if (focusObj.GetComponent<UnityEngine.UI.InputField>() != null)
                return true;
            return false;
        }
    }
}
