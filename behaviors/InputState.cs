using System;
using UnityEngine;
using g3;

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
        HTCVive = 8,

        TabletFingers = 1024
    }


    // [RMS] [TODO] this object has gotten pretty huge...passing around as struct 
    //   means a ton of copies!! maybe should be a class??
    public struct InputState
    {
        public InputDevice eDevice;
        public bool IsForDevice(InputDevice d) { return (eDevice & d) != 0; }

        // these are not set internally, placed here for convenience
        public bool MouseGamepadCaptureActive;      // [RMS] cannot differentiate these right now??
        public bool TouchCaptureActive;
        public bool LeftCaptureActive;
        public bool RightCaptureActive;


        // keys
        public bool bShiftKeyDown;

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
        public Ray vMouseWorldRay;



        // gamepad-specific
        public Ray vGamepadWorldRay;


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

        public bool bMenuButtonPressed;
        public bool bMenuButtonDown;
        public bool bMenuButtonReleased;


        public Vector2 vLeftStickDelta2D;
        public Vector2 vRightStickDelta2D;
        public Vector2 StickDelta2D(int nSide) { return (nSide == 0) ? vLeftStickDelta2D : vRightStickDelta2D; }


        // spatial controllers
        public bool bLeftControllerActive;
        public bool bRightControllerActive;

        public Ray vLeftSpatialWorldRay;
        public Ray vRightSpatialWorldRay;
        public Ray SpatialWorldRay(int nSide) { return (nSide == 0) ? vLeftSpatialWorldRay : vRightSpatialWorldRay; }

        public Frame3f LeftHandFrame;
        public Frame3f RightHandFrame;
        public Frame3f HandFrame(int nSide) { return (nSide == 0) ? LeftHandFrame: RightHandFrame; }


        // 
        public bool bHaveTouch;     // if this is false, none of the other values are initialized!

        public bool bTouchPressed;
        public bool bTouchDown;
        public bool bTouchReleased;

        public Vector2f vTouchPosDelta2D;
        public Vector2f vTouchPosition2D;
        public Ray vTouchWorldRay;


        public void Initialize_MouseGamepad(FContext s)
        {
            // [RMS] cannot differentiate these right now...
            eDevice = InputDevice.Mouse | InputDevice.Gamepad;

            bShiftKeyDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

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



        public void Initialize_Oculus(FContext s)
        {
            eDevice = InputDevice.OculusTouch;

            bLeftControllerActive = OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch);
            bRightControllerActive = OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch);

            bLeftTriggerPressed = InputExtension.Get.OculusLeftTrigger.Pressed;
            bLeftTriggerDown = InputExtension.Get.OculusLeftTrigger.Down;
            bLeftTriggerReleased = InputExtension.Get.OculusLeftTrigger.Released;

            bRightTriggerPressed = InputExtension.Get.OculusRightTrigger.Pressed;
            bRightTriggerDown = InputExtension.Get.OculusRightTrigger.Down;
            bRightTriggerReleased = InputExtension.Get.OculusRightTrigger.Released;

            bLeftShoulderPressed = InputExtension.Get.OculusLeftShoulder.Pressed;
            bLeftShoulderDown = InputExtension.Get.OculusLeftShoulder.Down;
            bLeftShoulderReleased = InputExtension.Get.OculusLeftShoulder.Released;

            bRightShoulderPressed = InputExtension.Get.OculusRightShoulder.Pressed;
            bRightShoulderDown = InputExtension.Get.OculusRightShoulder.Down;
            bRightShoulderReleased = InputExtension.Get.OculusRightShoulder.Released;

            bLeftStickPressed = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
            bLeftStickDown = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
            bLeftStickReleased = OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
            bLeftStickTouching = OVRInput.Get(OVRInput.Touch.PrimaryThumbstick, OVRInput.Controller.LTouch);

            bRightStickPressed = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);
            bRightStickDown = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);
            bRightStickReleased = OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);
            bRightStickTouching = OVRInput.Get(OVRInput.Touch.PrimaryThumbstick, OVRInput.Controller.RTouch);

            bAButtonPressed = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);
            bAButtonDown = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);
            bAButtonReleased = OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch);

            bBButtonPressed = OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch);
            bBButtonDown = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch);
            bBButtonReleased = OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch);

            bXButtonPressed = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch);
            bXButtonDown = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch);
            bXButtonReleased = OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch);

            bYButtonPressed = OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch);
            bYButtonDown = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch);
            bYButtonReleased = OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTouch);

            bMenuButtonPressed = OVRInput.GetDown(OVRInput.Button.Start);
            bMenuButtonDown = OVRInput.Get(OVRInput.Button.Start);
            bMenuButtonReleased = OVRInput.Get(OVRInput.Button.Start);

            vLeftStickDelta2D = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
            vRightStickDelta2D = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);

            // [RMS] bit of a hack here, if controller is not active then ray is 0/0, and
            //   that causes lots of exceptions elsewhere! So we return a default ray pointing
            //   straight up, but hopefully clients are checking active flag and will ignore it...
            vLeftSpatialWorldRay = (bLeftControllerActive) ?
                s.SpatialController.Left.CursorRay : new Ray(Vector3.zero, Vector3.up);
            vRightSpatialWorldRay = (bRightControllerActive) ?
                s.SpatialController.Right.CursorRay : new Ray(Vector3.zero, Vector3.up);

            LeftHandFrame = s.SpatialController.Left.SmoothedHandFrame;
            RightHandFrame = s.SpatialController.Right.SmoothedHandFrame;
        }




        public void Initialize_TouchInput(FContext s)
        {
            eDevice = InputDevice.TabletFingers;

            bTouchDown = bTouchPressed = bTouchReleased = false;

            if ( Input.touchCount == 0 ) {
                bHaveTouch = false;
                return;
            }

            Touch t = Input.touches[0];

            if ( t.phase == TouchPhase.Began ) {
                bTouchPressed = true;
                bTouchDown = true;
            } else if ( t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled ) {
                bTouchReleased = true;
            } else {
                bTouchDown = true;
            }

            vTouchPosition2D = t.position;
            vTouchPosDelta2D = t.deltaPosition;
            // not really a mouse, but that is where we put it...
            vTouchWorldRay = s.MouseController.CurrentCursorWorldRay();
        }



    }
}
