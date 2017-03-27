using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    public class SpatialDeviceMultiSelectBehavior : StandardInputBehavior
    {
        FContext scene;

        public SpatialDeviceMultiSelectBehavior(FContext scene)
        {
            this.scene = scene;
            Priority = 10;
        }

        public override InputDevice SupportedDevices {
            get { return InputDevice.AnySpatialDevice; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            if (input.bLeftTriggerPressed ^ input.bRightTriggerPressed) {
                CaptureSide eSide = (input.bLeftTriggerPressed) ? CaptureSide.Left : CaptureSide.Right;
                Ray3f useRay = (eSide == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
                SORayHit rayHit;
                if (scene.Scene.FindSORayIntersection(useRay, out rayHit)) 
                    return CaptureRequest.Begin(this, eSide);
            }
            return CaptureRequest.Ignore;
        }


        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            Ray3f useRay = (eSide == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
            SORayHit rayHit;
            if (scene.Scene.FindSORayIntersection(useRay, out rayHit)) 
                return Capture.Begin(this, eSide, rayHit.hitSO);
            return Capture.Ignore;
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            // [RMS] this is a hack to release input for shoulder+trigger gestures
            if ((data.which == CaptureSide.Left && input.bLeftShoulderPressed) ||
                 (data.which == CaptureSide.Right && input.bRightShoulderPressed)) {
                return Capture.End;
            }

            if ((data.which == CaptureSide.Left && input.bLeftTriggerReleased) ||
                 (data.which == CaptureSide.Right && input.bRightTriggerReleased)) {
                Ray3f useRay = (data.which == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
                SceneObject so = data.custom_data as SceneObject;
                SORayHit rayHit;
                if (so != null && so.FindRayIntersection(useRay, out rayHit)) {

                    // if other trigger is down we do multi-select add/remove toggling
                    bool bOtherDown =
                        (data.which == CaptureSide.Left && input.bRightTriggerDown) ||
                        (data.which == CaptureSide.Right && input.bLeftTriggerDown);
                    if (bOtherDown) {
                        if (scene.Scene.IsSelected(so))
                            scene.Scene.Deselect(so);
                        else
                            scene.Scene.Select(so, false);
                    } else
                        scene.Scene.Select(so, true);
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
