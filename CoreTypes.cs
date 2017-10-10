using System;
using System.Collections.Generic;

namespace f3
{

    public interface FMaterial
    {

    }


    public enum LineWidthType
    {
        World,
        Pixel
    }


    public enum FrameType
    {
        LocalFrame = 0,
        WorldFrame = 1
    };


    public enum UpDirection
    {
        ZUp = 0,
        YUp = 1
    }


    public enum PivotLocation
    {
        Center = 0,
        BaseCenter = 1
    }



    public interface IGameObjectGenerator
    {
        List<fGameObject> Generate();
    }


    // will/may be called per-frame to give a chance to do something with shortcut keys
    // return true to indicate that key was handled, ie "capture" it
    public interface IShortcutKeyHandler
    {
        bool HandleShortcuts();
    }


    public interface ITextEntryTarget
    {
        bool ConsumeAllInput();
        bool OnBeginTextEntry();
        bool OnEndTextEntry();
        bool OnBackspace();
        bool OnDelete();
        bool OnReturn();
        bool OnEscape();
        bool OnLeftArrow();
        bool OnRightArrow();
        bool OnCharacters(string s);
    }


    public enum CameraInteractionState
    {
        BeginCameraAction,
        EndCameraAction,
        Ignore
    }

    public interface ICameraInteraction
    {
        CameraInteractionState CheckCameraControls(InputState input);

        void DoCameraControl(FScene scene, fCamera mainCamera, InputState input);
    }


}
