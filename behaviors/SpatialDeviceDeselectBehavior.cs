using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    class SpatialDeviceDeselectBehavior : StandardInputBehavior
    {
        FContext scene;

        public SpatialDeviceDeselectBehavior(FContext scene)
        {
            this.scene = scene;
            Priority = 10;
        }

        public override InputDevice SupportedDevices {
            get { return InputDevice.OculusTouch; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            if (input.bLeftTriggerPressed ^ input.bRightTriggerPressed) {
                CaptureSide eSide = (input.bLeftTriggerPressed) ? CaptureSide.Left : CaptureSide.Right;
                Ray3f useRay = (eSide == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
                SORayHit rayHit;
                if (scene.Scene.FindSORayIntersection(useRay, out rayHit) == false) 
                    return CaptureRequest.Begin(this, eSide);
            }
            return CaptureRequest.Ignore;
        }


        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            Ray3f useRay = (eSide == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
            SORayHit rayHit;
            if (scene.Scene.FindSORayIntersection(useRay, out rayHit) == false)
                return Capture.Begin(this, eSide);
            return Capture.Ignore;
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            if ( (data.which == CaptureSide.Left && input.bLeftTriggerReleased)  ||
                 (data.which == CaptureSide.Right && input.bRightTriggerReleased) ) {
                Ray3f useRay = (data.which == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
                SORayHit rayHit;
                if (scene.Scene.FindSORayIntersection(useRay, out rayHit) == false) {
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
