using System;
using g3;

namespace f3
{
    /// <summary>
    /// Mouse interaction behavior for SO selection.
    /// left-click to select/deselect, left-shift-click to multiselect
    /// </summary>
    public class MouseMultiSelectBehavior : StandardInputBehavior
    {
        protected FContext scene;
        protected SceneObject selectSO;

        public bool EnableShiftModifier = true;
        public bool EnableControlModifier = true;

        /// <summary>
        /// If this is true, PivotSO objects are selected first, even if they are "behind" other objects.
        /// </summary>
        public bool SelectPivotsFirst = true;

        /// <summary>
        /// set this to filter out objects
        /// </summary>
        public Func<SceneObject, bool> SelectionFilterF = null;


        public MouseMultiSelectBehavior(FContext scene)
        {
            this.scene = scene;
            Priority = 10;
        }

        public override InputDevice SupportedDevices {
            get { return InputDevice.Mouse; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            selectSO = null;
            if (input.bLeftMousePressed) {
                SORayHit rayHit;
                if (FindSORayIntersection(input.vMouseWorldRay, out rayHit))
                    return CaptureRequest.Begin(this);
            }
            return CaptureRequest.Ignore;
        }

        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            selectSO = null;
            SORayHit rayHit;
            if (FindSORayIntersection(input.vMouseWorldRay, out rayHit)) {
                selectSO = rayHit.hitSO;
                return Capture.Begin(this);
            }
            return Capture.Ignore;
        }


        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            if ( input.bLeftMouseReleased ) { 

                SORayHit rayHit;
                if ( selectSO != null && selectSO.FindRayIntersection(input.vMouseWorldRay, out rayHit)) {
                    if ( (EnableShiftModifier && input.bShiftKeyDown) 
                        || (EnableControlModifier && input.bCtrlKeyDown) ) { 
                        if (scene.Scene.IsSelected(selectSO))
                            scene.Scene.Deselect(selectSO);
                        else
                            scene.Scene.Select(selectSO, false);
                    } else
                        scene.Scene.Select(selectSO, true);
                }
                selectSO = null;
                return Capture.End;

            } else {

                return Capture.Continue;
            }
        }

        public override Capture ForceEndCapture(InputState input, CaptureData data) {
            return Capture.End;
        }


        protected bool FindSORayIntersection(Ray3f ray, out SORayHit hit)
        {
            return (SelectPivotsFirst) ?
                scene.Scene.FindSORayIntersection_PivotPriority(ray, out hit, SelectionFilterF) :
                scene.Scene.FindSORayIntersection(ray, out hit, SelectionFilterF);
        }
    }
}
