using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{
    public class VRMouseUIBehavior : StandardInputBehavior
    {
        FContext scene;
        SceneUIElement pCapturing;
        SceneUIElement activeHover;

        public VRMouseUIBehavior(FContext scene)
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
                if (scene.FindUIHit(input.vMouseWorldRay, out uiHit)) {
                    bool bCanCapture = uiHit.hitUI.WantsCapture(InputEvent.Mouse(input, new AnyRayHit(uiHit)));
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
                if (scene.FindUIHit(input.vMouseWorldRay, out uiHit)) {
                    bool bCanCapture = uiHit.hitUI.BeginCapture(InputEvent.Mouse(input, new AnyRayHit(uiHit)));
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
                pCapturing.UpdateCapture(InputEvent.Mouse(input));
                return Capture.Continue;

            } else if (input.bLeftMouseReleased) {
                pCapturing.EndCapture(InputEvent.Mouse(input));
                pCapturing = null;
                return Capture.End;

            } else {
                DebugUtil.Log(2, "VRMouseUIBehavior.UpdateCapture: somehow ended up here without left mouse release!");
                if ( pCapturing != null ) {
                    pCapturing.EndCapture(InputEvent.Mouse(input));
                    pCapturing = null;
                }
                return Capture.End;
            }
        }


        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            if (pCapturing != null) {
                // if we don't do this, and EndCapture throws, we end up in a loop of exceptions!
                var temp = pCapturing;
                pCapturing = null;
                temp.EndCapture(InputEvent.Mouse(input));
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
            if (scene.FindUIHoverHit(input.vMouseWorldRay, out uiHit)) {
                if (activeHover != null && activeHover != uiHit.hitUI)
                    EndHover(input);

                activeHover = uiHit.hitUI;
                activeHover.UpdateHover(input.vMouseWorldRay, uiHit);
            } else if ( activeHover != null )
                EndHover(input);
        }
        public override void EndHover(InputState input)
        {
            if (activeHover != null) {
                activeHover.EndHover(input.vMouseWorldRay);
                activeHover = null;
            }
        }

    }
}
