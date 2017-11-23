using System;
using UnityEngine;
using g3;
using gs;  // for VRPlatform

namespace f3
{
    // useful websites:
    //   https://ritchielozada.com/2016/01/16/part-11-using-an-xbox-one-controller-with-unity-on-windows-10/
    //   https://blogs.msdn.microsoft.com/nathalievangelist/2014/12/16/joystick-input-in-unity-using-xbox360-controller/

    [Flags]
    public enum InputDevice
    {
        Mouse = 1,
        Gamepad = 2,

        OculusTouch = 4,
        HTCViveWands = 8,
        AnySpatialDevice = OculusTouch | HTCViveWands,

        TabletFingers = 1024
    }


    // [RMS] [TODO] this object has gotten pretty huge...passing around as struct 
    //   means a ton of copies!! maybe should be a class??
    public struct InputState
    {
        public static bool IsHandedDevice(InputDevice d) { return (d & InputDevice.AnySpatialDevice) != 0; }
        public static bool IsDevice(InputDevice set, InputDevice test) { return (set & test) != 0; }

        public InputDevice eDevice;
        public bool IsForDevice(InputDevice d) { return (eDevice & d) != 0; }

        // these are not set internally, placed here for convenience
        public bool MouseGamepadCaptureActive;      // [RMS] cannot differentiate these right now??
        public bool TouchCaptureActive;
        public bool LeftCaptureActive;
        public bool RightCaptureActive;


        // keys
        public bool bShiftKeyDown;
        public bool bCtrlKeyDown;
        public bool bAltKeyDown;

        // mouse
        public bool bLeftMousePressed;
        public bool bLeftMouseDown;
        public bool bLeftMouseReleased;

        public bool bMiddleMousePressed;
        public bool bMiddleMouseDown;
        public bool bMiddleMouseReleased;

        public bool bRightMousePressed;
        public bool bRightMouseDown;
        public bool bRightMouseReleased;

        public float fMouseWheel;

        public Vector2f vMouseDelta2D;
        public Vector2f vMousePosition2D;
        public Ray3f vMouseWorldRay;
        public Ray3f vMouseOrthoWorldRay;



        // gamepad-specific
        public Ray3f vGamepadWorldRay;


        // gamepad, oculus touch controllers

        public bool bLeftTriggerPressed;
        public bool bLeftTriggerDown;
        public bool bLeftTriggerReleased;

        public bool bRightTriggerPressed;
        public bool bRightTriggerDown;
        public bool bRightTriggerReleased;


        public bool bLeftShoulderPressed;
        public bool bLeftShoulderDown;
        public bool bLeftShoulderReleased;

        public bool bRightShoulderPressed;
        public bool bRightShoulderDown;
        public bool bRightShoulderReleased;


        public bool bLeftStickPressed;
        public bool bLeftStickDown;
        public bool bLeftStickReleased;
        public bool bLeftStickTouching;

        public bool bRightStickPressed;
        public bool bRightStickDown;
        public bool bRightStickReleased;
        public bool bRightStickTouching;


        public bool bAButtonPressed;
        public bool bAButtonDown;
        public bool bAButtonReleased;

        public bool bBButtonPressed;
        public bool bBButtonDown;
        public bool bBButtonReleased;

        public bool bXButtonPressed;
        public bool bXButtonDown;
        public bool bXButtonReleased;

        public bool bYButtonPressed;
        public bool bYButtonDown;
        public bool bYButtonReleased;

        public bool bLeftMenuButtonPressed;
        public bool bLeftMenuButtonDown;
        public bool bLeftMenuButtonReleased;

        // Oculus Touch does not have 'right' menu button, but Vive does
        public bool bRightMenuButtonPressed;
        public bool bRightMenuButtonDown;
        public bool bRightMenuButtonReleased;

        public Vector2f vLeftStickDelta2D;
        public Vector2f vRightStickDelta2D;
        public Vector2f StickDelta2D(int nSide) { return (nSide == 0) ? vLeftStickDelta2D : vRightStickDelta2D; }


        // spatial controllers
        public bool bLeftControllerActive;
        public bool bRightControllerActive;

        public Ray3f vLeftSpatialWorldRay;
        public Ray3f vRightSpatialWorldRay;
        public Ray3f SpatialWorldRay(int nSide) { return (nSide == 0) ? vLeftSpatialWorldRay : vRightSpatialWorldRay; }

        public Frame3f LeftHandFrame;
        public Frame3f RightHandFrame;
        public Frame3f HandFrame(int nSide) { return (nSide == 0) ? LeftHandFrame: RightHandFrame; }


        // 
        public bool bHaveTouch;     // if this is false, none of the other values are initialized!
        public int nTouchCount;

        public bool bTouchPressed;
        public bool bTouchDown;
        public bool bTouchReleased;

        public Vector2f vTouchPosDelta2D;
        public Vector2f vTouchPosition2D;
        public Ray3f vTouchWorldRay;
        public Ray3f vTouchOrthoWorldRay;

        public bool bSecondTouchPressed;
        public bool bSecondTouchDown;
        public bool bSecondTouchReleased;

        public Vector2f vSecondTouchPosDelta2D;
        public Vector2f vSecondTouchPosition2D;
        public Ray3f vSecondTouchWorldRay;


        public void Initialize_MouseGamepad(FContext s)
        {
            // [RMS] cannot differentiate these right now...
            eDevice = InputDevice.Mouse | InputDevice.Gamepad;

            bShiftKeyDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bCtrlKeyDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bAltKeyDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.LeftAlt);

            bLeftMousePressed = Input.GetMouseButtonDown(0);
            bLeftMouseDown = Input.GetMouseButton(0);
            bLeftMouseReleased = Input.GetMouseButtonUp(0);

            bMiddleMousePressed = Input.GetMouseButtonDown(2);
            bMiddleMouseDown = Input.GetMouseButton(2);
            bMiddleMouseReleased = Input.GetMouseButtonUp(2);

            bRightMousePressed = Input.GetMouseButtonDown(1);
            bRightMouseDown = Input.GetMouseButton(1);
            bRightMouseReleased = Input.GetMouseButtonUp(1);

            fMouseWheel = InputExtension.Get.Mouse.WheelDelta;

            vMouseDelta2D = InputExtension.Get.Mouse.PositionDelta;
            vMousePosition2D = InputExtension.Get.Mouse.Position;
            vMouseWorldRay = s.MouseController.CurrentCursorWorldRay();
            if (s.Use2DCockpit)
                vMouseOrthoWorldRay = s.MouseController.CurrentCursorOrthoRay();



            bLeftTriggerPressed = InputExtension.Get.GamepadLeft.Pressed;
            bLeftTriggerDown = InputExtension.Get.GamepadLeft.Down;
            bLeftTriggerReleased = InputExtension.Get.GamepadLeft.Released;

            bRightTriggerPressed = InputExtension.Get.GamepadRight.Pressed;
            bRightTriggerDown = InputExtension.Get.GamepadRight.Down;
            bRightTriggerReleased = InputExtension.Get.GamepadRight.Released;

            bAButtonPressed =  InputExtension.Get.GamepadA.Pressed;
            bAButtonDown = InputExtension.Get.GamepadA.Down;
            bAButtonReleased = InputExtension.Get.GamepadA.Released;

            bBButtonPressed = InputExtension.Get.GamepadB.Pressed;
            bBButtonDown = InputExtension.Get.GamepadB.Down;
            bBButtonReleased = InputExtension.Get.GamepadB.Released;

            bXButtonPressed = InputExtension.Get.GamepadX.Pressed;
            bXButtonDown = InputExtension.Get.GamepadX.Down;
            bXButtonReleased = InputExtension.Get.GamepadX.Released;

            bYButtonPressed = InputExtension.Get.GamepadY.Pressed;
            bYButtonDown = InputExtension.Get.GamepadY.Down;
            bYButtonReleased = InputExtension.Get.GamepadY.Released;

            vLeftStickDelta2D = InputExtension.Get.GamepadLeftStick.Position;
            vRightStickDelta2D = InputExtension.Get.GamepadRightStick.Position;

            // [RMS] assuming that left joystick/mouse are the same cursor pos...
            vGamepadWorldRay = vMouseWorldRay;
        }



        public void Initialize_SpatialController(FContext s)
        {
            eDevice = (VRPlatform.CurrentVRDevice == VRPlatform.Device.HTCVive) ?
                InputDevice.HTCViveWands : InputDevice.OculusTouch;

            // would we ever get this far if controller was not tracked?
            bLeftControllerActive = VRPlatform.IsLeftControllerTracked;
            bRightControllerActive = VRPlatform.IsRightControllerTracked;

            bLeftTriggerPressed = InputExtension.Get.SpatialLeftTrigger.Pressed;
            bLeftTriggerDown = InputExtension.Get.SpatialLeftTrigger.Down;
            bLeftTriggerReleased = InputExtension.Get.SpatialLeftTrigger.Released;

            bRightTriggerPressed = InputExtension.Get.SpatialRightTrigger.Pressed;
            bRightTriggerDown = InputExtension.Get.SpatialRightTrigger.Down;
            bRightTriggerReleased = InputExtension.Get.SpatialRightTrigger.Released;

            bLeftShoulderPressed = InputExtension.Get.SpatialLeftShoulder.Pressed;
            bLeftShoulderDown = InputExtension.Get.SpatialLeftShoulder.Down;
            bLeftShoulderReleased = InputExtension.Get.SpatialLeftShoulder.Released;

            bRightShoulderPressed = InputExtension.Get.SpatialRightShoulder.Pressed;
            bRightShoulderDown = InputExtension.Get.SpatialRightShoulder.Down;
            bRightShoulderReleased = InputExtension.Get.SpatialRightShoulder.Released;

            bLeftStickPressed = VRPlatform.LeftStickPressed;
            bLeftStickDown = VRPlatform.LeftStickDown;
            bLeftStickReleased = VRPlatform.LeftStickReleased;
            bLeftStickTouching = VRPlatform.LeftStickTouching;

            bRightStickPressed = VRPlatform.RightStickPressed;
            bRightStickDown = VRPlatform.RightStickDown;
            bRightStickReleased = VRPlatform.RightStickReleased;
            bRightStickTouching = VRPlatform.RightStickTouching;

            // [TODO] emulate these buttons w/ vive touchpad locations?

            bAButtonPressed = VRPlatform.AButtonPressed;
            bAButtonDown = VRPlatform.AButtonDown;
            bAButtonReleased = VRPlatform.AButtonReleased;

            bBButtonPressed = VRPlatform.BButtonPressed;
            bBButtonDown = VRPlatform.BButtonDown;
            bBButtonReleased = VRPlatform.BButtonReleased;

            bXButtonPressed = VRPlatform.XButtonPressed;
            bXButtonDown = VRPlatform.XButtonDown;
            bXButtonReleased = VRPlatform.XButtonReleased;

            bYButtonPressed = VRPlatform.YButtonPressed;
            bYButtonDown = VRPlatform.YButtonDown;
            bYButtonReleased = VRPlatform.YButtonReleased;

            bLeftMenuButtonPressed = VRPlatform.LeftMenuButtonPressed;
            bLeftMenuButtonDown = VRPlatform.LeftMenuButtonDown;
            bLeftMenuButtonReleased = VRPlatform.LeftMenuButtonReleased;

            bRightMenuButtonPressed = VRPlatform.RightMenuButtonPressed;
            bRightMenuButtonDown = VRPlatform.RightMenuButtonDown;
            bRightMenuButtonReleased = VRPlatform.RightMenuButtonReleased;

            vLeftStickDelta2D = VRPlatform.LeftStickPosition;
            vRightStickDelta2D = VRPlatform.RightStickPosition;

            // [RMS] bit of a hack here, if controller is not active then ray is 0/0, and
            //   that causes lots of exceptions elsewhere! So we return a default ray pointing
            //   straight up, but hopefully clients are checking active flag and will ignore it...
            vLeftSpatialWorldRay = (bLeftControllerActive) ?
                s.SpatialController.Left.CursorRay : new Ray(Vector3f.Zero, Vector3f.AxisY);
            vRightSpatialWorldRay = (bRightControllerActive) ?
                s.SpatialController.Right.CursorRay : new Ray(Vector3f.Zero, Vector3f.AxisY);

            LeftHandFrame = s.SpatialController.Left.SmoothedHandFrame;
            RightHandFrame = s.SpatialController.Right.SmoothedHandFrame;
        }




        public void Initialize_TouchInput(FContext s)
        {
            eDevice = InputDevice.TabletFingers;

            bTouchDown = bTouchPressed = bTouchReleased = false;

            nTouchCount = Input.touchCount;
            if ( nTouchCount == 0 ) {
                bHaveTouch = false;
                return;
            }
            bHaveTouch = true;

            Touch t = Input.touches[0];
            get_touch(t, ref bTouchPressed, ref bTouchDown, ref bTouchReleased,
                ref vTouchPosition2D, ref vTouchPosDelta2D);
            vTouchWorldRay = s.MouseController.CurrentCursorWorldRay();
            if (s.Use2DCockpit)
                vTouchOrthoWorldRay = s.MouseController.CurrentCursorOrthoRay();            

            if (nTouchCount > 1) {
                Touch t2 = Input.touches[1];
                get_touch(t2, ref bSecondTouchPressed, ref bSecondTouchDown, ref bSecondTouchReleased,
                    ref vSecondTouchPosition2D, ref vSecondTouchPosDelta2D);
                vSecondTouchWorldRay = s.MouseController.SecondWorldRay();
            }

        }

        static void get_touch(Touch t, ref bool bPressed, ref bool bDown, ref bool bReleased, 
            ref Vector2f pos, ref Vector2f delta)
        {
            if ( t.phase == TouchPhase.Began ) {
                bPressed = true;
                bDown = true;
            } else if ( t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled ) {
                bReleased = true;
            } else {
                bDown = true;
            }

            pos = t.position;
            delta = t.deltaPosition;
        }


        // this is kind of hacky. To avoid having to write perspective/ortho branching
        // code in all the SceneUIElements, we assume vMouseWorldRay is the ray to use,
        // and rewrite the InputState at the Behavior level. This helper function does the rewrite.
        public InputState ToOrthoLayerInput()
        {
            InputState s2 = this;
            s2.vMouseWorldRay = s2.vMouseOrthoWorldRay;
            s2.vTouchWorldRay = s2.vTouchOrthoWorldRay;
            return s2;
        }


    }
}
