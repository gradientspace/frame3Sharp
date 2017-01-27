using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{

    public interface FMaterial
    {

    }





    public enum FrameType
    {
        LocalFrame,
        WorldFrame
    };


    public interface IGameObjectGenerator
    {
        List<GameObject> Generate();
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

        void DoCameraControl(FScene scene, Camera mainCamera, InputState input);
    }


}
