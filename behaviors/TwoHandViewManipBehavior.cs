using System;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{
    public class TwoHandViewManipBehavior : StandardInputBehavior
    {
        Cockpit cockpit;

        public TwoHandViewManipBehavior(Cockpit cockpit)
        {
            this.cockpit = cockpit;
        }

        public override InputDevice SupportedDevices {
            get { return InputDevice.AnySpatialDevice; }
        }


        enum ActionMode
        {
            TransformCamera,
            SetWorldScale, 
        }
        enum TransformType
        {
            NoTransform, 
            Zoom,
            Pan, 
            Rotate
        }

        class HandInfo
        {
            public ActionMode eMode;

            public Frame3f leftStartF, rightStartF;
            public CameraState camState;
            public Vector3f camRight, camForward;

            // current zoom/pan/azimuth/altitude values
            public float runningZ;
            public Vector3f runningT;
            public float runningAz;
            public float runningAlt;
            public TransformType activeXForm;

            //   These values are for addressing deadzone problem: once we leave deadzone, we would like to ignore it, 
            //   otherwise the camera gets "stuck" when you move hands back together. However, we need to continue
            //   to shift delta by deadzone or we get a 'jump' when we first leave it. 
            public bool bInZoomDeadzone;
            public float fZoomShift;
            public bool bInPanDeadzone;
            public Vector3f vPanShift;
            public bool bInAzimuthDeadzone;
            public float fAzimuthShift;
            public bool bInAltitudeDeadzone;
            public float fAltitudeShift;


            public HandInfo() {
                runningZ = runningAlt = runningAz = 0.0f;
                runningT = Vector3f.Zero;
                activeXForm = TransformType.NoTransform;

                bInZoomDeadzone = true;
                fZoomShift = 0;
                bInPanDeadzone = true;
                vPanShift = Vector3f.Zero;
                bInAzimuthDeadzone = true;
                fAzimuthShift = 0;
                bInAltitudeDeadzone = true;
                fAltitudeShift = 0;
            }




            GameObject go;
            Frame3f hitFrame;
            float deadzone_r;
            Material mYes, mNo;
            public void BeginSetWorldScale(Frame3f center, Frame3f plane, float minr)
            {
                deadzone_r = minr;
                hitFrame = plane;
                mYes = MaterialUtil.CreateTransparentMaterial(ColorUtil.PivotYellow, 0.5f);
                mNo = MaterialUtil.CreateTransparentMaterial(ColorUtil.MiddleGrey, 0.3f);
                go = UnityUtil.CreatePrimitiveGO("worldscale_ball", PrimitiveType.Sphere, mNo);
                UnityUtil.SetGameObjectFrame(go, center, CoordSpace.WorldCoords);
            }
            public void UpdateSetWorldScale(Ray3f left, Ray3f right) {
                Vector3f hit1 = hitFrame.RayPlaneIntersection(left.Origin, left.Direction, 2);
                Vector3f hit2 = hitFrame.RayPlaneIntersection(right.Origin, right.Direction, 2);
                Vector3f avg = (hit1 + hit2) * 0.5f;
                float r0 = (hit1 - (Vector3f)go.transform.position).Length;
                float r1 = (hit2 - (Vector3f)go.transform.position).Length;
                float r = (r0 + r1) * 0.5f;
                float min_r = VRUtil.GetVRRadiusForVisualAngle(avg, camState.camPosition, 2.0f);
                r = (float)Math.Max(r, min_r);
                go.transform.localScale = r * Vector3f.One;
                go.GetComponent<Renderer>().material =
                    (r > deadzone_r) ? mYes : mNo;
            }
            public void CompleteSetWorldScale(Cockpit cockpit)
            {
                Vector3f c = go.transform.position;
                float r = go.transform.localScale[0];
                go.Destroy();

                if (r > deadzone_r) {
                    cockpit.ActiveCamera.Animator().DoActionDuringDipToBlack( () => {
                            cockpit.Context.ScaleView(c, r);
                        }, 0.5f);
                }
            }
        }


        public override CaptureRequest WantsCapture(InputState input)
        {
            // [RMS] ugh hack to prevent this from capturing when triggers are down
            if (input.bLeftTriggerDown || input.bRightTriggerDown)
                return CaptureRequest.Ignore;

            if ((input.bLeftShoulderPressed && input.bRightShoulderPressed) ||
                 (input.bLeftShoulderPressed && input.bRightShoulderDown) ||
                 (input.bLeftShoulderDown && input.bRightShoulderPressed)) {
                return CaptureRequest.Begin(this, CaptureSide.Both);
            }
            return CaptureRequest.Ignore;
        }

        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            HandInfo hi = new HandInfo();
            hi.eMode = ActionMode.TransformCamera;

            Frame3f camFrame = cockpit.ActiveCamera.GetWorldFrame();

            // if both rays hit scene and are within a few visual degrees, then we 
            // we pull out a sphere and use it to re-scale the world.
            // Otherwise do normal hand-transform actions
            AnyRayHit hit1, hit2;
            bool bHit1 = cockpit.Scene.FindSceneRayIntersection(input.vLeftSpatialWorldRay, out hit1);
            bool bHit2 = cockpit.Scene.FindSceneRayIntersection(input.vRightSpatialWorldRay, out hit2);
            if ( bHit1 && bHit2 ) {
                Vector3 avg = (hit1.hitPos + hit2.hitPos) * 0.5f;
                float d = VRUtil.GetVRRadiusForVisualAngle(avg, camFrame.Origin, 2.0f);
                if ( (hit1.hitPos - hit2.hitPos).Length < d ) {
                    hi.eMode = ActionMode.SetWorldScale;
                    Frame3f centerF = cockpit.Scene.SceneFrame;
                    centerF.Origin = avg;
                    Frame3f planeF = new Frame3f(centerF.Origin, camFrame.Z);
                    hi.BeginSetWorldScale(centerF, planeF, 2*d);
                    hi.UpdateSetWorldScale(input.vLeftSpatialWorldRay, input.vRightSpatialWorldRay);
                }
            }

            hi.leftStartF = input.LeftHandFrame;
            hi.rightStartF = input.RightHandFrame;
            hi.camState = cockpit.ActiveCamera.Manipulator().GetCurrentState(cockpit.Scene);
            hi.camRight = camFrame.X;
            hi.camForward = camFrame.Z;

            cockpit.ActiveCamera.SetTargetVisible(true);

            return Capture.Begin(this, CaptureSide.Both, hi);
        }



        float ApplyDeadzone(float fValue, float fDeadzoneWidth)
        {
            float s = Mathf.Sign(fValue);
            fValue = Mathf.Abs(fValue);
            if (fValue <= fDeadzoneWidth)
                return 0;
            fValue -= fDeadzoneWidth;
            return s * fValue;
        }


        Vector3f ApplyDeadzone(Vector3f vValue, float fDeadzoneWidthRad)
        {
            float mag = vValue.Length;
            Vector3f n = vValue.Normalized;

            if (mag < fDeadzoneWidthRad)
                return Vector3.zero;
            mag -= fDeadzoneWidthRad;
            return mag * n;
        }

        public Capture Update_TransformCamera(InputState input, CaptureData data)
        {
            // need both controllers to have valid positions
            if (input.bLeftControllerActive == false || input.bRightControllerActive == false)
                return Capture.Continue;

            float fSceneScale = cockpit.Scene.GetSceneScale();
            HandInfo hi = (HandInfo)data.custom_data;

            // deadzones and scaling factors
            const float fZoomDeadzoneInM = 0.1f;
            const float fZoomScale = 25.0f;
            const float fPanDeadzoneInM = 0.05f;
            const float fPanScale = 25.0f;
            const float fAzimuthDeadzoneInDeg = 10.0f;
            const float fAltitudeDeadzoneInDeg = 15.0f;
            const float fTrackAlpha = 0.1f;     // larger == faster tracking

            // zoom is indicated by moving hands together/apart. But we want to get rid of
            // influence from other gestures (like rotate below) so we project to camera-right axis first.
            Frame3f camFrame = cockpit.ActiveCamera.GetWorldFrame();
            Vector3 right = camFrame.X;
            float fOrig = Vector3.Dot((hi.rightStartF.Origin - hi.leftStartF.Origin), hi.camRight);
            float fCur = Vector3.Dot((input.RightHandFrame.Origin - input.LeftHandFrame.Origin), right);
            float deltaZAbs = fCur - fOrig;
            float deltaZ = (hi.bInZoomDeadzone) ? ApplyDeadzone(deltaZAbs, fZoomDeadzoneInM) : deltaZAbs-hi.fZoomShift;
            if (Math.Abs(deltaZ) > 0 && hi.bInZoomDeadzone) {
                hi.bInZoomDeadzone = false;
                hi.fZoomShift = deltaZAbs - deltaZ;
            }
            hi.runningZ = Mathf.Lerp(hi.runningZ, deltaZ, fTrackAlpha);
            float tz = hi.runningZ * fZoomScale;
            if (tz != 0.0f && hi.activeXForm == TransformType.NoTransform)
                hi.activeXForm = TransformType.Zoom;


            // translation is done by moving both hands in unison. We find the midpoint
            // of the start and current pairs, delta is translate
            Vector3f cOrig = 0.5f * (hi.leftStartF.Origin + hi.rightStartF.Origin);
            Vector3f cCur = 0.5f * (input.LeftHandFrame.Origin + input.RightHandFrame.Origin);
            Vector3f translateO = cCur - cOrig;
            Vector3f translate = (hi.bInPanDeadzone) ? ApplyDeadzone(translateO, fPanDeadzoneInM) : translateO - hi.vPanShift;
            if (translate.LengthSquared > 0 && hi.bInPanDeadzone) {
                hi.bInPanDeadzone = false;
                hi.vPanShift = translateO-translate;
            }
            hi.runningT = Vector3.Lerp(hi.runningT, translate, fTrackAlpha);
            Vector3f tx = hi.runningT * fPanScale * fSceneScale;
            if (tx.Length != 0.0f && hi.activeXForm == TransformType.NoTransform)
                hi.activeXForm = TransformType.Pan;

            // azimuth (left/right rotate) is specified by making a spin-the-wheel gesture, 
            //   where one hand slides forward and the other back. We guess a center-of-rotation
            //   as the midpoint of the start and current frames, and then average the two rotation
            //   angles in the XZ plane (by the vectors on the left and right sides). 
            //   *But* if there is also a translation, this goes wonky, so we have to subtract
            //   of the translation first!
            //     (above only applies if we permit simultaneous rotate & translate, which is currently disabled...)
            //Vector3 rotTranslate = translate;
            Vector3f rotTranslate = Vector3.zero;
            Vector3f origCenterXY = new Vector3(cOrig[0], 0, cOrig[2]);
            Vector3f shiftLeftO = input.LeftHandFrame.Origin - rotTranslate;
            Vector3f shiftRightO = input.RightHandFrame.Origin - rotTranslate;
            Vector3f shiftCenter = 0.5f * (shiftLeftO + shiftRightO);
            Vector3f curCenterXY = new Vector3(shiftCenter[0], 0, shiftCenter[2]);
            Vector3f sharedC = 0.5f * (origCenterXY + curCenterXY);
            float aLeft = VRUtil.PlaneAngleSigned(hi.leftStartF.Origin - sharedC, shiftLeftO - sharedC, 1);
            float aRight = VRUtil.PlaneAngleSigned(hi.rightStartF.Origin - sharedC, shiftRightO - sharedC, 1);
            float azO = -(aLeft + aRight) * 0.5f;
            float az = (hi.bInAzimuthDeadzone) ? ApplyDeadzone(azO, fAzimuthDeadzoneInDeg) : azO - hi.fAzimuthShift;
            if (Math.Abs(az) > 0 && hi.bInAzimuthDeadzone ) {
                hi.bInAzimuthDeadzone = false;
                hi.fAzimuthShift = azO - az;
            }
            hi.runningAz = Mathf.Lerp(hi.runningAz, az, fTrackAlpha);
            float fAzimuth = hi.runningAz;
            if (fAzimuth != 0.0f && hi.activeXForm == TransformType.NoTransform)
                hi.activeXForm = TransformType.Rotate;


            // altitude (up/down rotate) is specified by tilting controllers up or down. 
            // This is the trickiest one as hands tend to tilt up/down during the other actions.
            // We compute an average tilt up/down angle at the start, and then the per-hand delta
            // each frame, as well as the average delta, and use the smallest of these. 
            // mean hand-tilt at start frame
            float o1 = VRUtil.PlaneAngleSigned(hi.leftStartF.Rotation * Vector3.forward, hi.camForward, hi.camRight);
            float o2 = VRUtil.PlaneAngleSigned(hi.rightStartF.Rotation * Vector3.forward, hi.camForward, hi.camRight);
            float oa = (o1 + o2) * 0.5f;
            // per-frame hand tilt
            Vector3 camfw = camFrame.Z, camright = camFrame.X;
            float c1 = VRUtil.PlaneAngleSigned(input.LeftHandFrame.Rotation * Vector3.forward, camfw, camright);
            float c2 = VRUtil.PlaneAngleSigned(input.RightHandFrame.Rotation * Vector3.forward, camfw, camright);
            // use the smallest per-hand tilt delta, to prevent one-hand tilting from having an effect
            float d1 = oa - c1, d2 = oa - c2;

            float altO = (Mathf.Abs(d1) < Mathf.Abs(d2)) ? d1 : d2;
            // also consider the average, to reduce crazy popping from each hand tilting in opposite direction
            float dm = 0.5f * (d1 + d2);
            altO = (Mathf.Abs(altO) < Mathf.Abs(dm)) ? altO : dm;

            // deadzone and smoothing
            float alt = (hi.bInAltitudeDeadzone) ? ApplyDeadzone(altO, fAltitudeDeadzoneInDeg) : altO - hi.fAltitudeShift;
            if ( Math.Abs(alt) > 0 && hi.bInAltitudeDeadzone ) {
                hi.bInAltitudeDeadzone = false;
                hi.fAltitudeShift = altO - alt;
            }
            hi.runningAlt = Mathf.Lerp(hi.runningAlt, alt, fTrackAlpha);
            float fAltitude = hi.runningAlt;
            if (fAltitude != 0.0f && hi.activeXForm == TransformType.NoTransform)
                hi.activeXForm = TransformType.Rotate;


            // reset view to state when we started, then apply the accumulated rotate/zoom/translate
            cockpit.ActiveCamera.Manipulator().SetCurrentSceneState(cockpit.Scene, hi.camState);
            if ( hi.activeXForm == TransformType.Rotate )
                cockpit.ActiveCamera.Manipulator().SceneOrbit(cockpit.Scene, cockpit.ActiveCamera, fAzimuth, fAltitude);
            else if ( hi.activeXForm == TransformType.Pan )
                cockpit.ActiveCamera.Manipulator().SceneTranslate(cockpit.Scene, tx, false);
            else if ( hi.activeXForm == TransformType.Zoom )
                cockpit.ActiveCamera.Manipulator().SceneZoom(cockpit.Scene, cockpit.ActiveCamera, tz);

            return Capture.Continue;
        }


        public Capture Update_SetWorldScale(InputState input, CaptureData data)
        {
            // need both controllers to have valid positions
            if (input.bLeftControllerActive == false || input.bRightControllerActive == false)
                return Capture.Continue;

            //float fSceneScale = cockpit.ActiveScene.GetSceneScale();
            HandInfo hi = (HandInfo)data.custom_data;

            hi.UpdateSetWorldScale(input.vLeftSpatialWorldRay, input.vRightSpatialWorldRay);

            return Capture.Continue;
        }


        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            if ( input.bLeftShoulderReleased || input.bRightShoulderReleased ) {
                cockpit.ActiveCamera.SetTargetVisible(false);

                HandInfo hi = (HandInfo)data.custom_data;
                if (hi.eMode == ActionMode.SetWorldScale)
                    hi.CompleteSetWorldScale(cockpit);

                return Capture.End;
            } else {
                HandInfo hi = (HandInfo)data.custom_data;
                if (hi.eMode == ActionMode.SetWorldScale)
                    return Update_SetWorldScale(input, data);
                else
                    return Update_TransformCamera(input, data);
            }
        }


        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            cockpit.ActiveCamera.SetTargetVisible(false);
            return Capture.End;
        }

    }
}
