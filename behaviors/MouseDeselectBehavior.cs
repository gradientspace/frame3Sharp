using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{
    public  class MouseDeselectBehavior : StandardInputBehavior
    {
        FContext scene;

        public MouseDeselectBehavior(FContext scene)
        {
            this.scene = scene;
            Priority = 1000;
        }

        public override InputDevice SupportedDevices {
            get { return InputDevice.Mouse; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            if (scene.Scene.Selected.Count > 0 && input.bLeftMousePressed) {
                SORayHit rayHit;
                if (scene.Scene.FindSORayIntersection(input.vMouseWorldRay, out rayHit) == false)
                    return CaptureRequest.Begin(this);
            }
            return CaptureRequest.Ignore;
        }

        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            return Capture.Begin(this);
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            if (input.bLeftMouseReleased) {
                SORayHit rayHit;
                if ( scene.Scene.FindSORayIntersection(input.vMouseWorldRay, out rayHit) == false ) {
                    scene.Scene.ClearSelection();
                }
                return Capture.End;
            } else {
                return Capture.Continue;
            }
        }

        public override Capture ForceEndCapture(InputState input, CaptureData data) {
            return Capture.End;
        }

    }
}
