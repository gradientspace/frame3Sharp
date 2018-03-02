using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public class TouchUIBehavior : StandardInputBehavior
    {
        FContext scene;
        SceneUIElement pCapturing;

        public TouchUIBehavior(FContext scene)
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
                if (scene.FindUIHit(input.vTouchWorldRay, out uiHit)) {
                    bool bCanCapture = uiHit.hitUI.WantsCapture(InputEvent.Touch(input, new AnyRayHit(uiHit)));
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
                if (scene.FindUIHit(input.vTouchWorldRay, out uiHit)) {
                    bool bCanCapture = uiHit.hitUI.BeginCapture(InputEvent.Touch(input, new AnyRayHit(uiHit)));
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
                pCapturing.UpdateCapture(InputEvent.Touch(input));
                return Capture.Continue;

            } else if (input.bTouchReleased) {
                pCapturing.EndCapture(InputEvent.Touch(input));
                pCapturing = null;
                return Capture.End;

            } else {
                DebugUtil.Log(2, "TouchUIBehavior.UpdateCapture: somehow ended up here without left mouse release!");
                if ( pCapturing != null ) {
                    pCapturing.EndCapture(InputEvent.Touch(input));
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
                temp.EndCapture(InputEvent.Touch(input));
            }
            return Capture.End;
        }


    }
}
