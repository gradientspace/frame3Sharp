using System;

using UnityEngine;
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
			if (EventSystem.current.IsPointerOverGameObject())
				return true;
			// https://answers.unity.com/questions/1115464/ispointerovergameobject-not-working-with-touch-inp.html
			if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began) {
				if (EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId))
					return true;
			}	
            return false;
        }


        public static bool TextEntryFieldHasFocus()
        {
            if (EventSystem.current == null)
                return false;
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
