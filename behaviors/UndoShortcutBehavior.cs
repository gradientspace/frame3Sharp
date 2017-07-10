using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{
    public class UndoShortcutBehavior : StandardInputBehavior
    {
        FContext context;

        public UndoShortcutBehavior(FContext context)
        {
            this.context = context;
            Priority = 10;
        }

        public override InputDevice SupportedDevices
        {
            get { return InputDevice.AnySpatialDevice; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            if ( (input.bYButtonPressed ^ input.bBButtonPressed) 
                    && input.LeftCaptureActive == false 
                    && input.RightCaptureActive == false ) {
                CaptureSide eSide = (input.bYButtonPressed) ? CaptureSide.Left : CaptureSide.Right;
                return CaptureRequest.Begin(this, eSide);
            }
            return CaptureRequest.Ignore;
        }


        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            return Capture.Begin(this, eSide);
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            if (data.which == CaptureSide.Left && input.bYButtonReleased) {
                context.Scene.History.InteractiveStepBack();
                return Capture.End;
            } else if (data.which == CaptureSide.Right && input.bBButtonReleased) {
                context.Scene.History.InteractiveStepForward();
                return Capture.End;
            } else 
                return Capture.Continue;
        }

        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            return Capture.End;
        }
    }
}
