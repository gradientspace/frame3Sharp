using System;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{
    public class TouchViewManipBehavior : StandardInputBehavior
    {
        // because we cannot do press-drag vs press-release as separate
        //  behaviors, we have to hack this for now...
        public bool EnableSelectionBehavior = true;


        FContext context;

        float TouchRotateSpeed = 0.1f;

        Vector2f downPos;
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
            if (context.Scene.FindSORayIntersection(input.vTouchWorldRay, out hitSO))
                hitObject = hitSO.hitSO;

            downPos = input.vTouchPosition2D;
            bMaybeInSelect = true;

            return Capture.Begin(this, eSide, null);
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            if ( input.bTouchReleased ) { 
                context.ActiveCamera.SetTargetVisible(false);

                if ( bMaybeInSelect ) {
                    if (context.Scene.IsSelected(hitObject))
                        context.Scene.Deselect(hitObject);
                    else
                        context.Scene.Select(hitObject, true);
                }

                return Capture.End;
            }

            if (bMaybeInSelect) {
                if ((input.vTouchPosition2D - downPos).Length < 5.0f) {
                    return Capture.Continue;
                } else {
                    bMaybeInSelect = false;
                    context.ActiveCamera.SetTargetVisible(true);
                }
            }

            float dx = input.vTouchPosDelta2D.x;
            float dy = input.vTouchPosDelta2D.y;

            context.ActiveCamera.Manipulator().SceneOrbit(context.Scene, context.ActiveCamera, 
                TouchRotateSpeed * dx, TouchRotateSpeed * dy);

            return Capture.Continue;
        }


        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            context.ActiveCamera.SetTargetVisible(false);
            return Capture.End;
        }

    }


}
