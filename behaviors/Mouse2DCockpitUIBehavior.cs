using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{
    // This class enables mouse input in the optional 2D Cockpit
    // The 2D UI must be enabled by passing SceneOptions.Use2DCockpit=true to FContext.Start()
    public class Mouse2DCockpitUIBehavior : StandardInputBehavior
    {
        FContext scene;
        SceneUIElement pCapturing;
        SceneUIElement activeHover;

        public Mouse2DCockpitUIBehavior(FContext scene)
        {
            this.scene = scene;
            pCapturing = null;
            Priority = 0;
        }

        public override InputDevice SupportedDevices {
            get { return InputDevice.Mouse; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            UIRayHit uiHit;
            if (input.bLeftMousePressed) {
                if (scene.Find2DCockpitUIHit(input.vMouseOrthoWorldRay, out uiHit)) {
                    bool bCanCapture = uiHit.hitUI.WantsCapture(InputEvent.Mouse(input.ToOrthoLayerInput(), new AnyRayHit(uiHit)));
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
            if (input.bLeftMousePressed) {
                if (scene.Find2DCockpitUIHit(input.vMouseOrthoWorldRay, out uiHit)) {
                    bool bCanCapture = uiHit.hitUI.BeginCapture(InputEvent.Mouse(input.ToOrthoLayerInput(), new AnyRayHit(uiHit)));
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
            if (input.bLeftMouseDown) {
                pCapturing.UpdateCapture(InputEvent.Mouse(input.ToOrthoLayerInput()));
                return Capture.Continue;

            } else if (input.bLeftMouseReleased) {
                pCapturing.EndCapture(InputEvent.Mouse(input.ToOrthoLayerInput()));
                pCapturing = null;
                return Capture.End;

            } else {
                DebugUtil.Log(2, "VRMouseUIBehavior.UpdateCapture: somehow ended up here without left mouse release!");
                if ( pCapturing != null ) {
                    pCapturing.EndCapture(InputEvent.Mouse(input.ToOrthoLayerInput()));
                    pCapturing = null;
                }
                return Capture.End;
            }
        }


        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            if (pCapturing != null) {
                pCapturing.EndCapture(InputEvent.Mouse(input.ToOrthoLayerInput()));
                pCapturing = null;
            }
            return Capture.End;
        }


        public override bool EnableHover
        {
            get { return true; }
        }
        public override void UpdateHover(InputState input)
        {
            UIRayHit uiHit;
            if (scene.Find2DCockpitUIHoverHit(input.vMouseOrthoWorldRay, out uiHit)) {
                if (activeHover != null && activeHover != uiHit.hitUI)
                    EndHover(input);

                activeHover = uiHit.hitUI;
                activeHover.UpdateHover(input.vMouseOrthoWorldRay, uiHit);
            } else if ( activeHover != null )
                EndHover(input);
        }
        public override void EndHover(InputState input)
        {
            if (activeHover != null) {
                activeHover.EndHover(input.vMouseOrthoWorldRay);
                activeHover = null;
            }
        }

    }
}
