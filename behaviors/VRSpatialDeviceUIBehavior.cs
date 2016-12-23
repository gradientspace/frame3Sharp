using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{
    public class VRSpatialDeviceUIBehavior : StandardInputBehavior
    {
        FContext scene;
        SceneUIElement activeLeftHover, activeRightHover;

        public VRSpatialDeviceUIBehavior(FContext scene)
        {
            this.scene = scene;
            Priority = 0;
        }

        public override InputDevice SupportedDevices {
            get { return InputDevice.OculusTouch; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            if (input.bLeftTriggerPressed ^ input.bRightTriggerPressed) {
                CaptureSide eSide = (input.bLeftTriggerPressed) ? CaptureSide.Left : CaptureSide.Right;
                Ray useRay = (eSide == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
                UIRayHit uiHit;
                if (scene.FindUIHit(useRay, out uiHit)) {
                    bool bCanCapture = uiHit.hitUI.WantsCapture(InputEvent.OculusTouch(eSide, input, new AnyRayHit(uiHit)));
                    if (bCanCapture)
                        return CaptureRequest.Begin(this, eSide);
                }
            }
            return CaptureRequest.Ignore;
        }


        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            Ray useRay = (eSide == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
            UIRayHit uiHit;
            if (scene.FindUIHit(useRay, out uiHit)) {
                bool bCanCapture = uiHit.hitUI.BeginCapture(InputEvent.OculusTouch(eSide, input, new AnyRayHit(uiHit)));
                if (bCanCapture) {
                    return Capture.Begin(this, eSide, uiHit.hitUI );
                }
            }
            return Capture.Ignore;
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            SceneUIElement uiElem = data.custom_data as SceneUIElement;

            if ((data.which == CaptureSide.Left && input.bLeftTriggerReleased) ||
                 (data.which == CaptureSide.Right && input.bRightTriggerReleased)) {

                if (uiElem != null)
                    uiElem.EndCapture(InputEvent.OculusTouch(data.which, input));
                return Capture.End;

            } else if ((data.which == CaptureSide.Left && input.bLeftTriggerDown) ||
                       (data.which == CaptureSide.Right && input.bRightTriggerDown)) {

                if ( uiElem != null )
                    uiElem.UpdateCapture(InputEvent.OculusTouch(data.which, input));
                return Capture.Continue;

            } else {
                // [RMS] can end up here sometimes in Gamepad if we do camera controls
                //   while we are capturing...
                return Capture.End;
            }
        }


        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            SceneUIElement uiElem = data.custom_data as SceneUIElement;
            if (uiElem != null)
                uiElem.EndCapture(InputEvent.OculusTouch(data.which, input));
            return Capture.End;
        }



        // Hover is tricky here because both the cursors might be hovering over
        //   the same thing. Current code keeps track of left and right hover separately.
        //   But perhaps it would be simpler to just have a set of hovered objects, and
        //   we can add/remove as needed??


        public override bool EnableHover
        {
            get { return true; }
        }
        void deactivate_hover(InputState input, bool left )
        {
            if ( left ) {
                if (activeLeftHover != activeRightHover)
                    activeLeftHover.EndHover(input.vLeftSpatialWorldRay);
                activeLeftHover = null;
            } else {
                if ( activeRightHover != activeLeftHover )
                    activeRightHover.EndHover(input.vRightSpatialWorldRay);
                activeRightHover = null;
            }
        }
        public override void UpdateHover(InputState input)
        {
            UIRayHit uiHitL;
            if ( input.bLeftControllerActive && scene.FindUIHoverHit(input.vLeftSpatialWorldRay, out uiHitL)) {
                if (activeLeftHover != null && activeLeftHover != uiHitL.hitUI)
                    deactivate_hover(input, true);

                activeLeftHover = uiHitL.hitUI;
                if (activeLeftHover != activeRightHover)
                    activeLeftHover.UpdateHover(input.vLeftSpatialWorldRay, uiHitL);
            } else if (activeLeftHover != null)
                deactivate_hover(input, true);

            UIRayHit uiHitR;
            if (input.bRightControllerActive && scene.FindUIHoverHit(input.vRightSpatialWorldRay, out uiHitR)) {
                if (activeRightHover != null && activeRightHover != uiHitR.hitUI)
                    deactivate_hover(input, false);

                activeRightHover = uiHitR.hitUI;
                if (activeRightHover != activeLeftHover)
                    activeRightHover.UpdateHover(input.vRightSpatialWorldRay, uiHitR);
            } else if (activeRightHover != null)
                deactivate_hover(input, false);

        }
        public override void EndHover(InputState input)
        {
            if (activeLeftHover != null ) {
                activeLeftHover.EndHover(input.vLeftSpatialWorldRay);
                if (activeLeftHover == activeRightHover)
                    activeRightHover = null;
                activeLeftHover = null;
            }
            if (activeRightHover != null) {
                activeRightHover.EndHover(input.vRightSpatialWorldRay);
            }
        }

    }
}
