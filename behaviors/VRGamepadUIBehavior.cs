using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{
    public class VRGamepadUIBehavior : StandardInputBehavior
    {
        FContext scene;
        SceneUIElement pCapturing;
        SceneUIElement activeHover;

        enum WhichInput
        {
            None, LeftTrigger, AButton
        }
        WhichInput ActiveInput;

        public VRGamepadUIBehavior(FContext scene)
        {
            this.scene = scene;
            pCapturing = null;
            Priority = 0;
            ActiveInput = WhichInput.LeftTrigger;
        }

        public override InputDevice SupportedDevices {
            get { return InputDevice.Gamepad; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            UIRayHit uiHit;
            if (input.bLeftTriggerPressed || input.bAButtonPressed) {
                ActiveInput = (input.bLeftTriggerPressed) ? WhichInput.LeftTrigger : WhichInput.AButton;
                if (scene.FindUIHit(input.vGamepadWorldRay, out uiHit)) {
                    bool bCanCapture = uiHit.hitUI.WantsCapture( InputEvent.Gamepad(input, new AnyRayHit(uiHit)) );
                    if (bCanCapture)
                        return CaptureRequest.Begin(this);
                }
            }
            return CaptureRequest.Ignore;
        }

        public override Capture BeginCapture(InputState input, CaptureSide eWhich)
        {
            pCapturing = null;

            UIRayHit uiHit;
            if (input.bLeftTriggerPressed || input.bAButtonPressed) {
                ActiveInput = (input.bLeftTriggerPressed) ? WhichInput.LeftTrigger : WhichInput.AButton;
                if (scene.FindUIHit(input.vGamepadWorldRay, out uiHit)) {
                    bool bCanCapture = uiHit.hitUI.BeginCapture( InputEvent.Gamepad(input, new AnyRayHit(uiHit)) );
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
            if ( (ActiveInput == WhichInput.LeftTrigger && input.bLeftTriggerDown) 
                || (ActiveInput == WhichInput.AButton && input.bAButtonDown) ) { 
                pCapturing.UpdateCapture( InputEvent.Gamepad(input) );
                return Capture.Continue;

            } else if ((ActiveInput == WhichInput.LeftTrigger && input.bLeftTriggerReleased) 
                || (ActiveInput == WhichInput.AButton && input.bAButtonReleased) ) { 
                pCapturing.EndCapture( InputEvent.Gamepad(input) );
                pCapturing = null;
                ActiveInput = WhichInput.None;
                return Capture.End;

            } else {
                // [RMS] can end up here sometimes in Gamepad if we do camera controls
                //   while we are capturing...
                ActiveInput = WhichInput.None;
                return Capture.End;
            }
        }

        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            if ( pCapturing != null ) {
                pCapturing.EndCapture( InputEvent.Gamepad(input) );
                pCapturing = null;
                ActiveInput = WhichInput.None;
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
            if (scene.FindUIHoverHit(input.vGamepadWorldRay, out uiHit)) {
                if (activeHover != null && activeHover != uiHit.hitUI)
                    EndHover(input);

                activeHover = uiHit.hitUI;
                activeHover.UpdateHover(input.vGamepadWorldRay, uiHit);
            } else if ( activeHover != null )
                EndHover(input);
        }
        public override void EndHover(InputState input)
        {
            if (activeHover != null) {
                activeHover.EndHover(input.vGamepadWorldRay);
                activeHover = null;
            }
        }

    }
}
