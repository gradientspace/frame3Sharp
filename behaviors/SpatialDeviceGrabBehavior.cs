using System;
using g3;

namespace f3
{
    public class SpatialDeviceGrabBehavior : StandardInputBehavior
    {
        Cockpit cockpit;


        public SpatialDeviceGrabBehavior(Cockpit cockpit)
        {
            this.cockpit = cockpit;
        }

        public override InputDevice SupportedDevices
        {
            get { return InputDevice.AnySpatialDevice; }
        }

        class GrabInfo
        {
            public TransformableSO so;
            public Cockpit cockpit;
            public Frame3f startObjFW;
            public Frame3f startObjRelF;
            public Frame3f startHandF;
            public Vector2f stickDelta;

            public TransformGizmoChange change;        // [TODO] shouldn't be using gizmo change for this?
            public GrabInfo(Cockpit cockpit, TransformableSO so, Frame3f handF)
            {
                this.cockpit = cockpit;
                this.so = so;
                this.startHandF = handF;
                this.startObjFW = so.GetLocalFrame(CoordSpace.WorldCoords);
                this.startObjRelF = this.startHandF.ToFrame(this.startObjFW);
                this.stickDelta = Vector2f.Zero;
                change = new TransformGizmoChange() {
                    parentSO = new WeakReference(so),
                    parentBefore = so.GetLocalFrame(CoordSpace.SceneCoords),
                    parentScaleBefore = so.GetLocalScale()
                };
            }

            public Frame3f curHandF;
            public Frame3f curHandTargetF;
            public Frame3f curUseTargetF;

            public void update(Frame3f handF)
            {
                curHandF = handF;

                // [RMS] this function updates the position of object based on hand frame
                //  Not clear how this should work, there are lots of options...

                // [1] scaled relative motion of hand inherited by object  (lags ray though)
                //Vector3 dt = startHandF.ToFrameP(handF.Origin);
                //dt *= 10.0f;
                //Frame3 fNew = new Frame3(startObjFW);
                //fNew.Origin += dt;

                // [2] object stays on ray, inherits a bit of xform
                //   - resulting orientation is weird. works well for rotate in-place around ray,
                //     but up/down/left/right tilts are impossible w/o moving object
                Frame3f fNew = handF.FromFrame(this.startObjRelF);

                // [3] object stays on ray but no rotation
                //   - weird if you rotate left/right, because distance stays same but it
                //     keeps pointing in same direction
                //   - we have gizmo for this kind of translation.
                //Frame3 fNew = handF.FromFrame(this.startObjRelF);
                //fNew.Rotation = startObjFW.Rotation;

                // [4] object stays in place, rotate by hand rotation
                //   - pretty hard to control, but would be good for approx orienting...
                //   - definitely needs damping!!
                //Frame3 fNew = startObjFW;
                //Quaternion relative = handF.Rotation * Quaternion.Inverse(startHandF.Rotation);
                //fNew.Rotation = relative * fNew.Rotation;

                // apply stick rotation  DOESN"T WORK
                //Quaternion stickY = Quaternion.AngleAxis(stickDelta[1], startHandF.X);
                //fNew.Rotation = fNew.Rotation * stickY;

                // shift in/out along hand-ray by Z
                fNew.Origin += 0.1f * stickDelta[1] * handF.Z * cockpit.Scene.GetSceneScale();

                curHandTargetF = fNew;
                curUseTargetF = new Frame3f(curHandTargetF);

                // update so
                so.SetLocalFrame(curUseTargetF, CoordSpace.WorldCoords);
            }


            public void complete()
            {
            }

        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            if ((input.bLeftTriggerDown && input.bLeftShoulderPressed)
                  || (input.bLeftTriggerPressed && input.bLeftShoulderDown)) {
                return CaptureRequest.Begin(this, CaptureSide.Left);
            } else if ((input.bRightTriggerDown && input.bRightShoulderPressed)
                  || (input.bRightTriggerPressed && input.bRightShoulderDown)) {
                return CaptureRequest.Begin(this, CaptureSide.Right);
            } else
                return CaptureRequest.Ignore;
        }

        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            Ray3f useRay = (eSide == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
            SORayHit rayHit;
            if (cockpit.Scene.FindSORayIntersection(useRay, out rayHit)) {
                var tso = rayHit.hitSO as TransformableSO;
                if (tso != null) {
                    Frame3f handF = (eSide == CaptureSide.Left) ? input.LeftHandFrame : input.RightHandFrame;
                    return Capture.Begin(this, eSide, new GrabInfo(cockpit, tso, handF));
                }
            }
            return Capture.Ignore;
        }


        Capture end_transform(CaptureData data)
        {
            GrabInfo gi = data.custom_data as GrabInfo;
            gi.change.parentAfter = gi.so.GetLocalFrame(CoordSpace.SceneCoords);
            gi.change.parentScaleAfter = gi.so.GetLocalScale();

            cockpit.Scene.History.PushChange(gi.change, true);
            cockpit.Scene.History.PushInteractionCheckpoint();

            return Capture.End;
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            GrabInfo gi = data.custom_data as GrabInfo;

            if (data.which == CaptureSide.Left && (input.bLeftShoulderReleased || input.bLeftTriggerReleased)) {
                gi.complete();
                return end_transform(data);
            } else if (data.which == CaptureSide.Right && (input.bRightShoulderReleased || input.bRightTriggerReleased)) {
                gi.complete();
                return end_transform(data);
            }

            Frame3f handF = (data.which == CaptureSide.Left) ? input.LeftHandFrame : input.RightHandFrame;
            gi.stickDelta += (data.which == CaptureSide.Left) ? input.vLeftStickDelta2D : input.vRightStickDelta2D;
            gi.update(handF);


            // drop-a-copy on X/A button release
            if ( (data.which == CaptureSide.Left && input.bXButtonReleased) ||
                 (data.which == CaptureSide.Right && input.bAButtonReleased ) ) {

                SceneObject copy = gi.so.Duplicate();

                // save an undo-point for the current xform, and start a new one. That way we can
                //  step between drop-a-copy stages
                end_transform(data);
                gi.change = new TransformGizmoChange() {
                    parentSO = new WeakReference(gi.so),
                    parentBefore = gi.so.GetLocalFrame(CoordSpace.SceneCoords),
                    parentScaleBefore = gi.so.GetLocalScale()
                };

                // if we do this afterwards, and don't push an interaction state, then when 
                //   we undo/redo we don't end up sitting on top of a duplicate.
                cockpit.Scene.History.PushChange(
                    new AddSOChange() { scene = cockpit.Scene, so = copy, bKeepWorldPosition = false });

            }


            return Capture.Continue;
        }

        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            GrabInfo gi = data.custom_data as GrabInfo;
            gi.complete();

            return end_transform(data);
        }

    }

}
