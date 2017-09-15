using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{
    // This class enables mouse input in the optional 2D Cockpit
    // The 2D UI must be enabled by passing SceneOptions.Use2DCockpit=true to FContext.Start()
    public class Touch2DCockpitUIBehavior : StandardInputBehavior
    {
        FContext scene;
        SceneUIElement pCapturing;
        SceneUIElement activeHover;

        public Touch2DCockpitUIBehavior(FContext scene)
        {
            this.scene = scene;
            pCapturing = null;
            Priority = 0;
        }

        public override InputDevice SupportedDevices {
            get { return InputDevice.TabletFingers; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            UIRayHit uiHit;

            if (input.bTouchPressed && input.nTouchCount == 1) {
                if (scene.Find2DCockpitUIHit(input.vTouchOrthoWorldRay, out uiHit)) {
                    bool bCanCapture = uiHit.hitUI.WantsCapture(InputEvent.Touch(input.ToOrthoLayerInput(), new AnyRayHit(uiHit)));
                    if (bCanCapture)
                        return CaptureRequest.Begin(this);
                }
            }
            return CaptureRequest.Ignore;
        }

        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            // [TODO] we just did above...

            pCapturing = null;

            UIRayHit uiHit;
            if (input.bTouchPressed) {
                if (scene.Find2DCockpitUIHit(input.vTouchOrthoWorldRay, out uiHit)) {
                    bool bCanCapture = uiHit.hitUI.BeginCapture(InputEvent.Touch(input.ToOrthoLayerInput(), new AnyRayHit(uiHit)));
                    if (bCanCapture) {
                        pCapturing = uiHit.hitUI;
                        return Capture.Begin(this);
                    }
                }
            }
            return Capture.Ignore;
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            if (input.bTouchDown) {
                pCapturing.UpdateCapture(InputEvent.Touch(input.ToOrthoLayerInput()));
                return Capture.Continue;

            } else if (input.bTouchReleased) {
                pCapturing.EndCapture(InputEvent.Touch(input.ToOrthoLayerInput()));
                pCapturing = null;
                return Capture.End;

            } else {
                DebugUtil.Log(2, "Touch2DCockpitUIBehavior.UpdateCapture: somehow ended up here without left mouse release!");
                if ( pCapturing != null ) {
                    pCapturing.EndCapture(InputEvent.Touch(input.ToOrthoLayerInput()));
                    pCapturing = null;
                }
                return Capture.End;
            }
        }


        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            if (pCapturing != null) {
                pCapturing.EndCapture(InputEvent.Touch(input.ToOrthoLayerInput()));
                pCapturing = null;
            }
            return Capture.End;
        }


        public override bool EnableHover
        {
            get { return false; }
        }
        public override void UpdateHover(InputState input)
        {
            UIRayHit uiHit;
            if (scene.Find2DCockpitUIHoverHit(input.vTouchOrthoWorldRay, out uiHit)) {
                if (activeHover != null && activeHover != uiHit.hitUI)
                    EndHover(input);

                activeHover = uiHit.hitUI;
                activeHover.UpdateHover(input.vTouchOrthoWorldRay, uiHit);
            } else if ( activeHover != null )
                EndHover(input);
        }
        public override void EndHover(InputState input)
        {
            if (activeHover != null) {
                activeHover.EndHover(input.vTouchOrthoWorldRay);
                activeHover = null;
            }
        }

    }
}
