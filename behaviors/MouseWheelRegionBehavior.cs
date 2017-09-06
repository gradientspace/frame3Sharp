using System;
using g3;

namespace f3
{
    /// <summary>
    /// Applies MouseWheelF if RegionHitTest passes and you spun the mouse wheel.
    /// Region could be for example the ray-hit test of a context menu popup.
    /// This is only meant to be used as an Override behavior, as wheel does not
    /// provide pressed/release so we can't do normal capture process.
    /// </summary>
    public class MouseWheelRegionBehavior : StandardInputBehavior
    {
        public Action<float> MouseWheelF = null;
        public Func<Ray3f, bool> RegionHitTest = null;

        public MouseWheelRegionBehavior()
        {
            Priority = 0;
        }

        public override InputDevice SupportedDevices { get {
                return InputDevice.Mouse;
            }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            throw new NotImplementedException();
        }

        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            throw new NotImplementedException();
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            if (input.fMouseWheel == 0)
                return Capture.Continue;

            if ( input.IsForDevice(InputDevice.Mouse) ) {
                if (RegionHitTest(input.vMouseOrthoWorldRay)) {
                    MouseWheelF(input.fMouseWheel);
                    return Capture.End;
                }
            }
           
            return Capture.Continue;
        }

        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            throw new NotImplementedException();
        }
    }
}
