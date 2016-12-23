using System;
using System.Collections.Generic;
using UnityEngine;

namespace f3
{
    public class TouchViewManipBehavior : StandardInputBehavior
    {
        Cockpit cockpit;

        float TouchRotateSpeed = 0.1f;

        public TouchViewManipBehavior(Cockpit cockpit)
        {
            this.cockpit = cockpit;
        }

        public override InputDevice SupportedDevices {
            get { return InputDevice.TabletFingers; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            if (input.bHaveTouch == false)
                return CaptureRequest.Ignore;

            if ( input.bTouchPressed ) {
                SORayHit hitSO;
                if ( cockpit.Scene.FindSORayIntersection( input.vTouchWorldRay, out hitSO ) == false ) {
                    return CaptureRequest.Begin(this, CaptureSide.Any);
                }
            }
            return CaptureRequest.Ignore;
        }


        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            return Capture.Begin(this, eSide, null);
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            if ( input.bTouchReleased ) { 
                cockpit.ActiveCamera.gameObject.GetComponent<CameraTarget>().ShowTarget = false;
                return Capture.End;
            }

            float dx = input.vTouchPosDelta2D.x;
            float dy = input.vTouchPosDelta2D.y;

            cockpit.ActiveCamera.Manipulator().SceneOrbit(cockpit.Scene, cockpit.ActiveCamera, 
                TouchRotateSpeed * dx, TouchRotateSpeed * dy);

            return Capture.Continue;
        }


        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            cockpit.ActiveCamera.gameObject.GetComponent<CameraTarget>().ShowTarget = false;
            return Capture.End;
        }

    }


}
