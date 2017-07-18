using System;
using g3;

namespace f3
{
    //
    // This behavior handles the common case of wanting to differentiate between
    // click-in-place and click-and-drag actions. You implement each of those as
    // a separate Behavior, and then use this to handle the click vs drag decision.
    //
    // Note that the sub-Behaviors need to be able to handle getting their Begin/UpdateCapture
    // calls with a different InputState from the one sent to their WantsCapture calls
    //
    // You can change which mouse button this is based on by setting the ButtonPressedF functions.
    // Default is left-mouse.
    //
    public class MouseClickDragSuperBehavior : StandardInputBehavior
    {
        public StandardInputBehavior ClickBehavior;
        public StandardInputBehavior DragBehavior;

        public Func<InputState, bool> ButtonPressedF = MouseBehaviors.LeftButtonPressedF;
        public Func<InputState, bool> ButtonReleasedF = MouseBehaviors.LeftButtonReleasedF;

        // how far cursor has to move in pixel-space units to switch from Click to Drag behavior
        public float DragTolerance = 5.0f;


        Vector2f mouseDownPos;
        StandardInputBehavior inBehavior;


        public MouseClickDragSuperBehavior()
        {
        }

        public override InputDevice SupportedDevices
        {
            get { return InputDevice.Mouse; }
        }


        public override CaptureRequest WantsCapture(InputState input)
        {
            inBehavior = null;

            if (ClickBehavior == null && DragBehavior == null)
                return CaptureRequest.Ignore;

            if ( ButtonPressedF(input) ) {
                if (ClickBehavior == null || DragBehavior == null)
                    return (ClickBehavior == null) ? 
                        DragBehavior.WantsCapture(input) : ClickBehavior.WantsCapture(input);

                CaptureRequest click_req = ClickBehavior.WantsCapture(input);
                CaptureRequest drag_req = DragBehavior.WantsCapture(input);
                if (click_req.type != CaptureRequestType.Ignore && drag_req.type != CaptureRequestType.Ignore)
                    return CaptureRequest.Begin(this, CaptureSide.Any);
                else if (click_req.type != CaptureRequestType.Ignore)
                    return click_req;
                else
                    return drag_req;
            }
            return CaptureRequest.Ignore;
        }


        // we only get to these functions if we are in one of them


        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            mouseDownPos = input.vMousePosition2D;
            return Capture.Begin(this, eSide, null);
        }


        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            // if we already picked a behavior, let it handle things
            if ( inBehavior != null ) {
                Capture result = inBehavior.UpdateCapture(input, data);
                if (result.state == CaptureState.End)
                    inBehavior = null;
                return result;
            }

            // if we released button, and we are here, we must not have picked yet, so
            // do full click behavior
            if ( ButtonReleasedF(input) ) {
                Capture begin = ClickBehavior.BeginCapture(input, data.which);
                if ( begin.state == CaptureState.Begin ) {
                    Capture update = ClickBehavior.UpdateCapture(input, data);
                    if ( update.state != CaptureState.End ) {
                        DebugUtil.Log(2, "MouseClickDragSuperBehavior.UpdateCapture: ClickBehavior did not return End");
                        return Capture.End;
                    }
                    return update;
                }
            }


            float fDelta = mouseDownPos.Distance(input.vMousePosition2D);
            if ( fDelta > DragTolerance ) {
                inBehavior = DragBehavior;
                Capture begin = inBehavior.BeginCapture(input, data.which);
                if (begin.state == CaptureState.Begin) {
                    Capture update = inBehavior.UpdateCapture(input, data);
                    return update;
                } else
                    return Capture.End;
            }

            // haven't decided yet, carry on
            return Capture.Continue;
        }


        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            if (inBehavior != null) {
                Capture result = inBehavior.ForceEndCapture(input, data);
                inBehavior = null;
                return result;
            }
            return Capture.End;
        }


    }
}
