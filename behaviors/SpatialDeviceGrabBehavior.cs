using System;
using g3;

namespace f3
{
    public class SpatialDeviceGrabBehavior : StandardInputBehavior
    {
        FContext context;
        Func<SceneObject, bool> ObjectFilterF;

        public float RotationSpeed = 1.0f;
        public float TranslationSpeed = 1.0f;
        public float StickMoveSpeed = 0.1f;

        public delegate void GrabEventHandler(object sender, SceneObject target);
        public event GrabEventHandler OnBeginGrab;
        public event GrabEventHandler OnEndGrab;

        public SpatialDeviceGrabBehavior(Cockpit cockpit) {
            this.context = cockpit.Context;
        }
        public SpatialDeviceGrabBehavior(FContext context, Func<SceneObject, bool> filterF) {
            this.context = context;
            ObjectFilterF = filterF;
        }

        public override InputDevice SupportedDevices
        {
            get { return InputDevice.AnySpatialDevice; }
        }

        class GrabInfo
        {
            public SceneObject so;
            public Frame3f startObjFW;
            public Frame3f startObjRelF;
            public Frame3f startHandF;
            public Vector2f stickDelta;

            public float RotationSpeed = 1.0f;
            public float TranslationSpeed = 1.0f;
            public float StickSpeed = 0.1f;

            public TransformGizmoChange change;        // [TODO] shouldn't be using gizmo change for this?
            public GrabInfo(SceneObject so, Frame3f handF)
            {
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
                //Vector3f dt = startHandF.ToFrameP(handF.Origin);
                //dt *= 10.0f;
                //Frame3f fNew = new Frame3f(startObjFW);
                //fNew.Origin += dt;

                // [2] object stays on ray, inherits a bit of xform
                //   - resulting orientation is weird. works well for rotate in-place around ray,
                //     but up/down/left/right tilts are impossible w/o moving object
                Frame3f fNew = handF.FromFrame(this.startObjRelF);
                if (RotationSpeed != 1.0f) {
                    fNew.Rotation = Quaternionf.Slerp(startObjFW.Rotation, fNew.Rotation, RotationSpeed);
                }
                if (TranslationSpeed != 1.0f) {
                    fNew.Origin = Vector3f.Lerp(startObjFW.Origin, fNew.Origin, TranslationSpeed);
                }

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
                fNew.Origin += StickSpeed * stickDelta[1] * handF.Z * so.GetScene().GetSceneScale();

                curHandTargetF = fNew;
                curUseTargetF = new Frame3f(curHandTargetF);

                // update so
                so.SetLocalFrame(curUseTargetF, CoordSpace.WorldCoords);
            }


            public void complete()
            {
            }
        }


        bool check_object_ray_hit(InputState input, CaptureSide eSide)
        {
            Ray3f useRay = (eSide == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
            SORayHit rayHit;
            if (context.Scene.FindSORayIntersection(useRay, out rayHit, ObjectFilterF)) {
                var tso = rayHit.hitSO;
                if (tso != null)
                    return true;
            }
            return false;
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            if ((input.bLeftTriggerDown && input.bLeftShoulderPressed)
                  || (input.bLeftTriggerPressed && input.bLeftShoulderDown)) {
                if ( check_object_ray_hit(input, CaptureSide.Left) )
                    return CaptureRequest.Begin(this, CaptureSide.Left);
            } else if ((input.bRightTriggerDown && input.bRightShoulderPressed)
                  || (input.bRightTriggerPressed && input.bRightShoulderDown)) {
                if (check_object_ray_hit(input, CaptureSide.Right))
                    return CaptureRequest.Begin(this, CaptureSide.Right);
            } 
            return CaptureRequest.Ignore;
        }

        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            Ray3f useRay = (eSide == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
            SORayHit rayHit;
            if (context.Scene.FindSORayIntersection(useRay, out rayHit, ObjectFilterF)) {
                var tso = rayHit.hitSO;
                if (tso != null) {
                    Frame3f handF = (eSide == CaptureSide.Left) ? input.LeftHandFrame : input.RightHandFrame;
                    if ( OnBeginGrab != null )
                        OnBeginGrab(this, tso);
                    return Capture.Begin(this, eSide,
                        new GrabInfo(tso, handF) {
                            RotationSpeed = this.RotationSpeed, TranslationSpeed = this.TranslationSpeed, StickSpeed = this.StickMoveSpeed } );
                }
            }
            return Capture.Ignore;
        }


        Capture end_transform(CaptureData data)
        {
            GrabInfo gi = data.custom_data as GrabInfo;
            gi.change.parentAfter = gi.so.GetLocalFrame(CoordSpace.SceneCoords);
            gi.change.parentScaleAfter = gi.so.GetLocalScale();

            context.Scene.History.PushChange(gi.change, true);
            context.Scene.History.PushInteractionCheckpoint();

            return Capture.End;
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            GrabInfo gi = data.custom_data as GrabInfo;


            bool bFinished = false;
            if (data.which == CaptureSide.Left && (input.bLeftShoulderReleased || input.bLeftTriggerReleased)) {
                bFinished = true;
            } else if (data.which == CaptureSide.Right && (input.bRightShoulderReleased || input.bRightTriggerReleased)) {
                bFinished = true;
            }
            if ( bFinished ) {
                gi.complete();
                Capture result = end_transform(data);
                if ( OnEndGrab != null )
                    OnEndGrab(this, gi.so);
                return result;
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
                context.Scene.History.PushChange(
                    new AddSOChange() { scene = context.Scene, so = copy, bKeepWorldPosition = false });

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
