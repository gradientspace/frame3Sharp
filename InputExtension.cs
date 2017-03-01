using System;
using UnityEngine;
using g3;

namespace f3
{
    // InputTrigger converts an Axis input (eg like a trigger on a gamepad) to 
    // a Pressed/Down/Released-style button, based on "down" and "up" thresholds
    public class InputTrigger
    {
        float fPrevValue = 0, fCurValue = 0;
        Func<float> valueF;
        float fValueUpThresh, fValueDownThresh;

        bool in_pressed;
        bool pressed_this_frame;
        bool released_this_frame;

        public InputTrigger(Func<float> valueFunction, float fDownThresh = 0.98f, float fUpThresh = 0.1f) {
            valueF = valueFunction;
            fValueDownThresh = fDownThresh;
            fValueUpThresh = fUpThresh;
            fPrevValue = fCurValue = 0;
            in_pressed = false;
        }

        public void Update() {
            fPrevValue = fCurValue;
            fCurValue = valueF();

            pressed_this_frame = released_this_frame = false;
            if ( in_pressed == false && fPrevValue < fValueDownThresh && fCurValue >= fValueDownThresh ) {
                in_pressed = true;
                pressed_this_frame = true;
            } else if ( in_pressed == true && fPrevValue > fValueUpThresh && fCurValue <= fValueUpThresh ) {
                in_pressed = false;
                released_this_frame = true;
            } 

        }

        public bool Pressed
            { get { return pressed_this_frame; } }
        public bool Down
            { get { return in_pressed; } }
        public bool Released
            { get { return released_this_frame; } }


        public static readonly InputTrigger NoTrigger = new InputTrigger(() => { return 0; });
    }



    public class InputJoystick
    {
        Func<Vector2f> positionF;

        public InputJoystick(Func<Vector2f> positionFunc)
        {
            positionF = positionFunc;
        }

        public Vector2f Position
        {
            get { return positionF(); }
        }

        public static readonly InputJoystick NoStick = new InputJoystick( () => { return Vector2f.Zero; } );
    }



    public class InputMouse
    {
        Func<Vector2f> deltaF;
        Func<Vector2f> positionF;
        Func<float> wheelF;

        public InputMouse(Func<Vector2f> deltaFunc, Func<Vector2f> positionFunc, Func<float> wheelFunc)
        {
            deltaF = deltaFunc;
            positionF = positionFunc;
            wheelF = wheelFunc;
        }

        public Vector2f Position
        {
            get { return positionF(); }
        }

        public Vector2f PositionDelta
        {
            get { return deltaF(); }
        }

        public float WheelDelta
        {
            get { return wheelF(); }
        }

        public static readonly InputMouse NoMouse = new InputMouse(
            () => { return Vector2f.Zero; }, () => { return Vector2f.Zero; }, () => { return 0; });
    }



    public class InputButton
    {
        Func<bool> pressedF, downF, releasedF;
        public InputButton(Func<bool> pressedFunc, Func<bool> downFunc, Func<bool> releasedFunc)
        {
            pressedF = pressedFunc;
            downF = downFunc;
            releasedF = releasedFunc;
        }

        public bool Pressed {
            get { return pressedF(); }
        }
        public bool Down {
            get { return downF(); }
        }
        public bool Released {
            get { return releasedF(); }
        }

        public static readonly InputButton NoButton = new InputButton(
            () => { return false; },
            () => { return false; },
            () => { return false; });
    }



    public class InputExtension
    {
        static InputExtension Singleton = null;
        public static InputExtension Get {
            get { if (Singleton == null) Singleton = new InputExtension(); return Singleton; }
        }

        public InputMouse Mouse;

        public InputTrigger GamepadLeft;
        public InputTrigger GamepadRight;
        public InputJoystick GamepadLeftStick;
        public InputJoystick GamepadRightStick;
        public InputButton GamepadA, GamepadB, GamepadX, GamepadY;
        public InputButton GamepadLeftShoulder, GamepadRightShoulder;

        public InputTrigger SpatialLeftTrigger;
        public InputTrigger SpatialRightTrigger;

        public InputTrigger SpatialLeftShoulder;
        public InputTrigger SpatialRightShoulder;


        public void Start()
        {

            // configure mouse

            if (UnityUtil.InputAxisExists("Mouse X") && UnityUtil.InputAxisExists("Mouse Y")) {
                Func<float> wheelFunc = () => { return 0; };
                if (UnityUtil.InputAxisExists("Mouse ScrollWheel"))
                    wheelFunc = () => { return Input.GetAxis("Mouse ScrollWheel"); };
                Mouse = new InputMouse(
                    () => { return new Vector2f(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); }, 
                    () => { return new Vector2f(Input.mousePosition.x, Input.mousePosition.y); },
                    wheelFunc);
            } else {
                Mouse = InputMouse.NoMouse;
            }



            // configure gamepad


            if (UnityUtil.InputAxisExists("LeftTrigger")) {
                GamepadLeft = new InputTrigger(() => {
                    return Input.GetAxis("LeftTrigger");
                });
            } else {
                GamepadLeft = InputTrigger.NoTrigger;
            }

            if (UnityUtil.InputAxisExists("RightTrigger")) {
                GamepadRight = new InputTrigger(() => {
                    return Input.GetAxis("RightTrigger");
                });
            } else {
                GamepadRight = InputTrigger.NoTrigger;
            }

            if ( UnityUtil.InputAxisExists("Joystick X") && UnityUtil.InputAxisExists("Joystick Y") ) {
                GamepadLeftStick = new InputJoystick(() => {
                    return new Vector2f(Input.GetAxis("Joystick X"), Input.GetAxis("Joystick Y"));
                });
            } else {
                GamepadLeftStick = InputJoystick.NoStick;
            }

            if (UnityUtil.InputAxisExists("Joystick2 X") && UnityUtil.InputAxisExists("Joystick2 Y")) {
                GamepadRightStick = new InputJoystick(() => {
                    return new Vector2f(Input.GetAxis("Joystick2 X"), Input.GetAxis("Joystick2 Y"));
                });
            } else {
                GamepadRightStick = InputJoystick.NoStick;
            }

            
            if ( UnityUtil.InputButtonExists("ButtonA") ) {
                GamepadA = new InputButton(
                    () => { return Input.GetButtonDown("ButtonA"); },
                    () => { return Input.GetButton("ButtonA"); },
                    () => { return Input.GetButtonUp("ButtonA"); });
            } else {
                GamepadA = InputButton.NoButton;
            }

            if (UnityUtil.InputButtonExists("ButtonB")) {
                GamepadB = new InputButton(
                    () => { return Input.GetButtonDown("ButtonB"); },
                    () => { return Input.GetButton("ButtonB"); },
                    () => { return Input.GetButtonUp("ButtonB"); });
            } else {
                GamepadB = InputButton.NoButton;
            }

            if (UnityUtil.InputButtonExists("ButtonX")) {
                GamepadX = new InputButton(
                    () => { return Input.GetButtonDown("ButtonX"); },
                    () => { return Input.GetButton("ButtonX"); },
                    () => { return Input.GetButtonUp("ButtonX"); });
            } else {
                GamepadX = InputButton.NoButton;
            }

            if (UnityUtil.InputButtonExists("ButtonY")) {
                GamepadY = new InputButton(
                    () => { return Input.GetButtonDown("ButtonY"); },
                    () => { return Input.GetButton("ButtonY"); },
                    () => { return Input.GetButtonUp("ButtonY"); });
            } else {
                GamepadY = InputButton.NoButton;
            }


            if (UnityUtil.InputButtonExists("LeftShoulder")) {
                GamepadLeftShoulder = new InputButton(
                    () => { return Input.GetButtonDown("LeftShoulder"); },
                    () => { return Input.GetButton("LeftShoulder"); },
                    () => { return Input.GetButtonUp("LeftShoulder"); });
            } else {
                GamepadLeftShoulder = InputButton.NoButton;
            }
            if (UnityUtil.InputButtonExists("RightShoulder")) {
                GamepadRightShoulder = new InputButton(
                    () => { return Input.GetButtonDown("RightShoulder"); },
                    () => { return Input.GetButton("RightShoulder"); },
                    () => { return Input.GetButtonUp("RightShoulder"); });
            } else {
                GamepadRightShoulder = InputButton.NoButton;
            }


            // configure spatial controller triggers

            float fDownThresh = (gs.VRPlatform.CurrentVRDevice == gs.VRPlatform.Device.HTCVive) ? 0.8f : 0.9f;
            SpatialLeftTrigger = 
                new InputTrigger(() => { return gs.VRPlatform.LeftTrigger; }, fDownThresh, 0.1f);
            SpatialRightTrigger = 
                new InputTrigger(() => { return gs.VRPlatform.RightTrigger; }, fDownThresh, 0.1f);
            SpatialLeftShoulder = 
                new InputTrigger(() => { return gs.VRPlatform.LeftSecondaryTrigger; }, fDownThresh, 0.1f);
            SpatialRightShoulder = 
                new InputTrigger(() => { return gs.VRPlatform.RightSecondaryTrigger; }, fDownThresh, 0.1f);
        }

        // call this from a MonoBehavior Update before you use the Input extension methods
        public void Update()
        {
            GamepadLeft.Update();
            GamepadRight.Update();
            SpatialLeftTrigger.Update();
            SpatialRightTrigger.Update();
            SpatialLeftShoulder.Update();
            SpatialRightShoulder.Update();
        }


    }



}
