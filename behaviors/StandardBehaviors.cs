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















}
