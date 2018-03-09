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


        bool using_mouse = true;
        public bool CachedIsMouseInput {
            get { return using_mouse; }
        }


        public bool Pressed(InputState input) {
            return Pressed(ref input);
        }
        public bool Pressed(ref InputState input)
        {
            if (input.IsForDevice(InputDevice.Mouse)) {
                using_mouse = true;
                return input.bLeftMousePressed;
            } else if (input.IsForDevice(InputDevice.TabletFingers)) {
                using_mouse = false;
                return input.bTouchPressed;
            }
            return false;
        }


        public bool Down(InputState input) {
            return Down(ref input);
        }
        public bool Down(ref InputState input)
        {
            if (input.IsForDevice(InputDevice.Mouse)) {
                using_mouse = true;
                return input.bLeftMouseDown;
            } else if (input.IsForDevice(InputDevice.TabletFingers)) {
                using_mouse = false;
                return input.bTouchDown;
            }
            return false;
        }


        public bool Released(InputState input) {
            return Released(ref input);
        }
        public bool Released(ref InputState input)
        {
            if (input.IsForDevice(InputDevice.Mouse)) {
                using_mouse = true;
                return input.bLeftMouseReleased;
            } else if (input.IsForDevice(InputDevice.TabletFingers)) {
                using_mouse = false;
                return input.bTouchReleased;
            }
            return false;
        }


        public Vector2f ClickPoint(InputState input) {
            return ClickPoint(ref input);
        }
        public Vector2f ClickPoint(ref InputState input)
        {
            if (input.IsForDevice(InputDevice.Mouse)) {
                using_mouse = true;
                return input.vMousePosition2D;
            } else if (input.IsForDevice(InputDevice.TabletFingers)) {
                using_mouse = false;
                return input.vTouchPosition2D;
            }
            return Vector2f.Zero;
        }


        public Ray3f WorldRay(InputState input) {
            return WorldRay(ref input);
        }
        public Ray3f WorldRay(ref InputState input)
        {
            if (input.IsForDevice(InputDevice.Mouse)) {
                using_mouse = true;
                return input.vMouseWorldRay;
            } else if (input.IsForDevice(InputDevice.TabletFingers)) {
                using_mouse = false;
                return input.vTouchWorldRay;
            }
            return new Ray3f(Vector3f.Zero, Vector3f.AxisY); ;
        }


        public Ray3f SceneRay(InputState input, FScene scene) {
            return SceneRay(ref input, scene);
        }
        public Ray3f SceneRay(ref InputState input, FScene scene)
        {
            if (input.IsForDevice(InputDevice.Mouse)) {
                using_mouse = true;
                return scene.ToSceneRay(input.vMouseWorldRay);
            } else if (input.IsForDevice(InputDevice.TabletFingers)) {
                using_mouse = false;
                return scene.ToSceneRay(input.vTouchWorldRay);
            }
            return new Ray3f(Vector3f.Zero, Vector3f.AxisY); ;
        }

    }















}
