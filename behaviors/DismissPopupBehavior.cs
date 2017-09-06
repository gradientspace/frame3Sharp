using System;
using g3;

namespace f3
{
    /// <summary>
    /// Fires DimissF if PopupHitTestF fails on input pressed. Works for any input type.
    /// Primary use case is to dismiss a popup like a context menu.
    /// </summary>
    public class DismissPopupBehavior : StandardInputBehavior
    {
        public Action DismissF = null;
        public Func<Ray3f, bool> PopupHitTestF = null;

        public DismissPopupBehavior()
        {
            Priority = 0;
        }

        public override InputDevice SupportedDevices { get {
                return InputDevice.AnySpatialDevice | InputDevice.Mouse | InputDevice.Gamepad | InputDevice.TabletFingers;
            }
        }


        public override CaptureRequest WantsCapture(InputState input)
        {
            throw new NotImplementedException();
        }

        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            throw new NotImplementedException();
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            if ( input.IsForDevice(InputDevice.Mouse) ) {
                if (input.bLeftMousePressed || input.bMiddleMousePressed || input.bRightMousePressed) {
                    if (PopupHitTestF(input.vMouseOrthoWorldRay) == false)
                        DismissF();
                }
            } else if ( input.IsForDevice(InputDevice.TabletFingers) ) { 
                if ( input.bTouchPressed ) {
                    if (PopupHitTestF(input.vTouchOrthoWorldRay) == false)
                        DismissF();
                }
            } else if ( input.IsForDevice(InputDevice.AnySpatialDevice) ) {
                if (input.bLeftTriggerPressed && PopupHitTestF(input.vLeftSpatialWorldRay) == false) {
                    DismissF();
                } else if (input.bRightTriggerPressed && PopupHitTestF(input.vRightSpatialWorldRay) == false) {
                    DismissF();
                }
            }
           
            return Capture.Continue;
        }

        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            throw new NotImplementedException();
        }
    }
}
