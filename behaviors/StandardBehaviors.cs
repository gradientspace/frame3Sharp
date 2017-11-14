using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{

    // abstract basic Mouse / Touch input
    public abstract class Any2DInputBehavior : StandardInputBehavior
    {

        override public InputDevice SupportedDevices {
            get { return InputDevice.Mouse | InputDevice.TabletFingers; }
        }


        public bool Pressed(InputState input)
        {
            if ( input.IsForDevice(InputDevice.Mouse) )
                return input.bLeftMousePressed;
            else if ( input.IsForDevice(InputDevice.TabletFingers) )
                return input.bTouchPressed;
            return false;
        }

        public bool Down(InputState input)
        {
            if (input.IsForDevice(InputDevice.Mouse))
                return input.bLeftMouseDown;
            else if ( input.IsForDevice(InputDevice.TabletFingers) )
                return input.bTouchDown;
            return false;
        }

        public bool Released(InputState input)
        {
            if (input.IsForDevice(InputDevice.Mouse))
                return input.bLeftMouseReleased;
            else if (input.IsForDevice(InputDevice.TabletFingers))
                return input.bTouchReleased;
            return false;
        }

        public Vector2f ClickPoint(InputState input)
        {
            if (input.IsForDevice(InputDevice.Mouse))
                return input.vMousePosition2D;
            else if (input.IsForDevice(InputDevice.TabletFingers))
                return input.vTouchPosition2D;
            return Vector2f.Zero;
        }

        public Ray3f WorldRay(InputState input)
        {
            if (input.IsForDevice(InputDevice.Mouse))
                return input.vMouseWorldRay;
            else if (input.IsForDevice(InputDevice.TabletFingers))
                return input.vTouchWorldRay;
            return new Ray3f(Vector3f.Zero, Vector3f.AxisY); ;
        }
    }












    public class RightMouseClickDragBehavior : StandardInputBehavior
    {
        public Func<InputState, bool> WantsCaptureF = (input) => { return true; };
        public Action<InputState> BeginCaptureF = (input) => { };
        public Action<InputState, InputState> UpdateCaptureF = (input, lastInput) => { };
        public Action<InputState> EndCaptureF = (input) => { };


        override public InputDevice SupportedDevices {
            get { return InputDevice.Mouse; }
        }

        InputState lastInput;

        public RightMouseClickDragBehavior()
        {
        }

        override public CaptureRequest WantsCapture(InputState input)
        {
            if (input.bRightMousePressed) {
                if (WantsCaptureF(input))
                    return CaptureRequest.Begin(this);
            }
            return CaptureRequest.Ignore;
        }

        override public Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            BeginCaptureF(input);
            return Capture.Begin(this);
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            if (input.bRightMouseReleased) {
                EndCaptureF(input);
                lastInput = input;
                return Capture.End;
            } else {
                UpdateCaptureF(input, lastInput);
                return Capture.Continue;
            }
        }

        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            EndCaptureF(input);
            return Capture.End;
        }
    }



}
