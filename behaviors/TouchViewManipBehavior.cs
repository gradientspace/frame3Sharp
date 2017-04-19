using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    public class TouchViewManipBehavior : StandardInputBehavior
    {
        // because we cannot do press-drag vs press-release as separate
        //  behaviors, we have to hack this for now...
        public bool EnableSelectionBehavior = true;


        FContext context;

        public float TouchRotateSpeed = 0.1f;
        public float TouchZoomSpeed = 0.00025f;
        public float TouchPanSpeed = 0.00025f;

        Vector2f downPos;
        Vector2f downPos2;
        bool bInitializedPair = false;

        CameraState startCamState;
        bool bHaveCameraState = false;

        bool bMaybeInSelect = false;
        SceneObject hitObject;


        public TouchViewManipBehavior(FContext context)
        {
            this.context = context;
        }

        public override InputDevice SupportedDevices {
            get { return InputDevice.TabletFingers; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            if (input.bHaveTouch == false)
                return CaptureRequest.Ignore;

            if ( input.bTouchPressed ) {
                return CaptureRequest.Begin(this, CaptureSide.Any);
            }
            return CaptureRequest.Ignore;
        }


        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            SORayHit hitSO;
            if (context.Scene.FindSORayIntersection_PivotPriority(input.vTouchWorldRay, out hitSO))
                hitObject = hitSO.hitSO;

            bInitializedPair = false;
            downPos = input.vTouchPosition2D;
            if ( input.nTouchCount == 2 ) {
                downPos2 = input.vSecondTouchPosition2D;
                bInitializedPair = true;
                bHaveCameraState = false;
                bMaybeInSelect = false;
            } else
                bMaybeInSelect = true;

            return Capture.Begin(this, eSide, null);
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            // when we release last touch, if we are in select state, try select
            if ( input.bTouchReleased ) { 
                context.ActiveCamera.SetTargetVisible(false);

                if ( bMaybeInSelect ) {
                    if (hitObject == null)
                        context.Scene.ClearSelection();
                    else if ( hitObject is PivotSO && context.TransformManager.HaveActiveGizmo)
                        context.TransformManager.SetActiveReferenceObject(hitObject as PivotSO);
                    else if (context.Scene.IsSelected(hitObject))
                        context.Scene.Deselect(hitObject);
                    else
                        context.Scene.Select(hitObject, true);
                }

                bMaybeInSelect = false;
                hitObject = null;

                return Capture.End;
            }


            // check if we should exit select state (2+ touches, or first touch moved far enough)
            if (bMaybeInSelect) {
                bool bExitSelect =
                    (input.nTouchCount > 1) ||
                    ( (input.vTouchPosition2D - downPos).Length > 5.0f );

                if (bExitSelect) {
                    bMaybeInSelect = false;
                    hitObject = null;
                    context.ActiveCamera.SetTargetVisible(true);
                    bHaveCameraState = false;
                } else {
                    return Capture.Continue;
                }
            }


            // do view manipuluation
            if (input.nTouchCount == 1) {
                float dx = input.vTouchPosDelta2D.x;
                float dy = input.vTouchPosDelta2D.y;
                context.ActiveCamera.Manipulator().SceneOrbit(context.Scene, context.ActiveCamera,
                    TouchRotateSpeed * dx, TouchRotateSpeed * dy);
                bHaveCameraState = false;

            } else {
                if ( bInitializedPair == false ) {
                    downPos2 = input.vSecondTouchPosition2D;
                    bInitializedPair = true;
                }
                if ( bHaveCameraState == false ) {
                    startCamState = context.ActiveCamera.Manipulator().GetCurrentState(context.Scene);
                    bHaveCameraState = true;
                }

                float dist_initial = (downPos - downPos2).Length;
                float dist_cur = (input.vTouchPosition2D - input.vSecondTouchPosition2D).Length;
                float dz = TouchZoomSpeed * (dist_cur - dist_initial);

                Vector2f center_initial = 0.5f * (downPos + downPos2);
                Vector2f center_cur = 0.5f * (input.vTouchPosition2D + input.vSecondTouchPosition2D);
                Vector2f dt = TouchPanSpeed * (center_cur - center_initial);

                context.ActiveCamera.GetManipulator().SetCurrentSceneState(context.Scene, startCamState);
                context.ActiveCamera.GetManipulator().SceneZoom(context.Scene, context.ActiveCamera, dz);
                context.ActiveCamera.GetManipulator().ScenePan(context.Scene, context.ActiveCamera, dt.x, dt.y);

            }

            return Capture.Continue;
        }


        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            context.ActiveCamera.SetTargetVisible(false);
            return Capture.End;
        }

    }


}
