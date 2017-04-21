using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    public class SpatialDeviceGrabViewBehavior : StandardInputBehavior
    {
        Cockpit cockpit;

        public float RotationSpeed = 1.0f;
        public float TranslationSpeed = 1.0f;

        public SpatialDeviceGrabViewBehavior(Cockpit cockpit)
        {
            this.cockpit = cockpit;
        }

        public override InputDevice SupportedDevices {
            get { return InputDevice.AnySpatialDevice; }
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


        struct GrabHandInfo
        {
            public Frame3f leftStartF, rightStartF;
            public CameraState camState;
        }


        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            GrabHandInfo hi = new GrabHandInfo();
            hi.leftStartF = input.LeftHandFrame;
            hi.rightStartF = input.RightHandFrame;
            hi.camState = cockpit.ActiveCamera.Manipulator().GetCurrentState(cockpit.Scene);

            return Capture.Begin(this, eSide, hi);
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

            GrabHandInfo hi = (GrabHandInfo)data.custom_data;

            Frame3f StartF = Frame3f.Identity;
            Frame3f CurF = Frame3f.Identity;

            if (data.which == CaptureSide.Left) {
                StartF = hi.leftStartF;
                CurF = input.LeftHandFrame;
            } else if (data.which == CaptureSide.Right) {
                StartF = hi.rightStartF;
                CurF = input.RightHandFrame;
            }

            // translation is just controller world-movement
            Vector3f translation = CurF.Origin - StartF.Origin;

            // We figure out orbit by rotating cur & start frames such that the axis we
            // want to measure around is that world axis. This produces more stable angles
            // as the controller twists

            Frame3f YAlignedStartF = StartF; YAlignedStartF.AlignAxis(1, Vector3f.AxisY);
            Frame3f YAlignedCurF = CurF; YAlignedCurF.AlignAxis(1, Vector3f.AxisY);
            float fAngleX = (float)MathUtil.PlaneAngleSignedD(YAlignedStartF.X, YAlignedCurF.X, Vector3f.AxisY);

            Frame3f XAlignedStartF = StartF; XAlignedStartF.AlignAxis(0, Vector3f.AxisX);
            Frame3f XAlignedCurF = CurF; XAlignedCurF.AlignAxis(0, Vector3f.AxisX);
            float fAngleY = (float)MathUtil.PlaneAngleSignedD(XAlignedStartF.Y, XAlignedCurF.Y, Vector3f.AxisX);

            // apply camera xforms
            cockpit.ActiveCamera.Manipulator().SetCurrentSceneState(cockpit.Scene, hi.camState);
            cockpit.ActiveCamera.Manipulator().SceneOrbitAround(cockpit.Scene,
                CurF.Origin, -RotationSpeed * fAngleX, RotationSpeed * fAngleY);
            cockpit.ActiveCamera.Manipulator().SceneTranslate(cockpit.Scene, TranslationSpeed * translation);

            return Capture.Continue;
        }


        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            cockpit.ActiveCamera.gameObject.GetComponent<CameraTarget>().ShowTarget = false;
            return Capture.End;
        }

    }


}
