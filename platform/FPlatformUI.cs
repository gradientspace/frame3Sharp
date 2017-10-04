using System;



namespace f3
{
    public static class FPlatformUI
    {
        public static bool IsConsumingMouseInput()
        {
            // [RMS] this works?
            bool over_go = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            return over_go;
        }
    }
}
