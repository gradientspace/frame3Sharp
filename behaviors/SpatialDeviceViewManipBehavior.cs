using System;
using System.Collections.Generic;
using UnityEngine;

namespace f3
{
    public class SpatialDeviceViewManipBehavior : StandardInputBehavior
    {
        Cockpit cockpit;

        public SpatialDeviceViewManipBehavior(Cockpit cockpit)
        {
            this.cockpit = cockpit;
        }

        public override InputDevice SupportedDevices {
            get { return InputDevice.OculusTouch; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            if ((input.bLeftShoulderPressed == true && input.bRightShoulderPressed == false && input.bRightShoulderDown == false) ||
                 (input.bLeftShoulderDown == true && input.bRightShoulderReleased == true)) {

                // [RMS] ugh hack to prevent this from capturing when triggers are down
                if (input.bLeftTriggerDown == false)
                    return CaptureRequest.Begin(this, CaptureSide.Left);
            }
            if ((input.bRightShoulderPressed == true && input.bLeftShoulderPressed == false && input.bLeftShoulderDown == false) ||
                 (input.bRightShoulderDown == true && input.bLeftShoulderReleased == true)) {

                // [RMS] ugh hack to prevent this from capturing when triggers are down
                if (input.bRightTriggerDown == false)
                    return CaptureRequest.Begin(this, CaptureSide.Right);
            }

            return CaptureRequest.Ignore;
        }


        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            return Capture.Begin(this, eSide, null);
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            if ( (data.which == CaptureSide.Left && input.bLeftShoulderReleased) ||
                 (data.which == CaptureSide.Right&& input.bRightShoulderReleased) ||
                 (data.which == CaptureSide.Left && input.bRightShoulderPressed) ||
                 (data.which == CaptureSide.Right && input.bLeftShoulderPressed)  ) 
            {
                cockpit.ActiveCamera.gameObject.GetComponent<CameraTarget>().ShowTarget = false;
                return Capture.End;
            }

            // [RMS] this is a hack to release input for shoulder+trigger gestures
            if ( (data.which == CaptureSide.Left && input.bLeftTriggerPressed) ||
                 (data.which == CaptureSide.Right && input.bRightTriggerPressed) ) {
                cockpit.ActiveCamera.gameObject.GetComponent<CameraTarget>().ShowTarget = false;
                return Capture.End;
            }

            cockpit.ActiveCamera.gameObject.GetComponent<CameraTarget>().ShowTarget = false;

            float dx = input.vLeftStickDelta2D[0], dy = input.vLeftStickDelta2D[1];
            float /*dx2 = input.vRightStickDelta2D[0], */dy2 = input.vRightStickDelta2D[1];

            float fDead = 0.3f;
            dx = Mathf.Clamp(Mathf.Abs(dx) - fDead, 0, 1.0f) * Mathf.Sign(dx);
            dy = Mathf.Clamp(Mathf.Abs(dy) - fDead, 0, 1.0f) * Mathf.Sign(dy);

            if (data.which == CaptureSide.Left) {
                cockpit.ActiveCamera.Manipulator().SceneOrbit(cockpit.Scene, cockpit.ActiveCamera, -0.75f * dx, -0.3f * dy);
                cockpit.ActiveCamera.Manipulator().SceneZoom(cockpit.Scene, cockpit.ActiveCamera, 0.1f * dy2);
                //mainCamera.Manipulator().ScenePan(cockpit.ActiveScene, cockpit.ActiveCamera, -0.05f * dx2, -0.05f * dy2);

            } else if (data.which == CaptureSide.Right) {
                cockpit.ActiveCamera.Manipulator().ScenePan(cockpit.Scene, cockpit.ActiveCamera, -0.05f * dx, -0.05f * dy);
                cockpit.ActiveCamera.Manipulator().SceneZoom(cockpit.Scene, cockpit.ActiveCamera, 0.1f * dy2);
                //mainCamera.Manipulator().SceneZoom(cockpit.ActiveScene, cockpit.ActiveCamera, 0.1f * dy);
                //mainCamera.Manipulator().ScenePan(cockpit.ActiveScene, cockpit.ActiveCamera, -0.05f * dx2, -0.05f * dy2);
            }

            return Capture.Continue;
        }


        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            cockpit.ActiveCamera.gameObject.GetComponent<CameraTarget>().ShowTarget = false;
            return Capture.End;
        }

    }


}
