using System;
using g3;

namespace f3
{
    // This behavior implements zoom in/out towards the camera target point based
    // on mouse-wheel movement. 
    //
    // Since there is no start/end signal for wheel changes (in particular no end),
    // this is not a behavior that can capture/etc. Currently can only be used
    // as an Override behavior, ie outside the normal capture pipeline.
    //
    // Also, it is very basic, if you are going to use the wheel
    // for anything else you need to subclass and prevent this behavior from also happening.
    public class MouseWheelZoomBehavior : StandardInputBehavior
    {
        // distance that one wheel-tick zooms
        public float ZoomScale = 1.0f;
        public bool Adaptive = false;

        Cockpit cockpit;

        public override InputDevice SupportedDevices {
            get { return InputDevice.Mouse; }
        }

        public MouseWheelZoomBehavior(Cockpit cockpit)
        {
            this.cockpit = cockpit;
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
            if ( input.fMouseWheel != 0 ) {
                float fZoom = ZoomScale * input.fMouseWheel;
                if ( Adaptive )
                    cockpit.ActiveCamera.Manipulator().SceneAdaptiveZoom(cockpit.Scene, cockpit.ActiveCamera, fZoom);
                else
                    cockpit.ActiveCamera.Manipulator().SceneZoom(cockpit.Scene, cockpit.ActiveCamera, fZoom);
            }
            return Capture.Continue;
        }


        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            throw new NotImplementedException();
        }

    }
}
