using System;


namespace f3
{
    public class RightMouseClickDragBehavior : StandardInputBehavior
    {
        public Func<InputState, bool> WantsCaptureF = (input) => { return true; };
        public Action<InputState> BeginCaptureF = (input) => { };
        public Action<InputState, InputState> UpdateCaptureF = (input, lastInput) => { };
        public Action<InputState> EndCaptureF = (input) => { };


        override public InputDevice SupportedDevices {
            get { return InputDevice.Mouse; }
        }

        InputState lastInput;

        public RightMouseClickDragBehavior()
        {
        }

        override public CaptureRequest WantsCapture(InputState input)
        {
            if (input.bRightMousePressed) {
                if (WantsCaptureF(input))
                    return CaptureRequest.Begin(this);
            }
            return CaptureRequest.Ignore;
        }

        override public Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            BeginCaptureF(input);
            return Capture.Begin(this);
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            if (input.bRightMouseReleased) {
                EndCaptureF(input);
                lastInput = input;
                return Capture.End;
            } else {
                UpdateCaptureF(input, lastInput);
                return Capture.Continue;
            }
        }

        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            EndCaptureF(input);
            return Capture.End;
        }
    }






    public class SpatialClickDragBehaviour : StandardInputBehavior
    {
        public Func<InputState, CaptureSide, bool> WantsCaptureF = (input, eSide) => { return true; };
        public Action<InputState, CaptureSide> BeginCaptureF = (input, eSide) => { };
        public Action<InputState, InputState, CaptureSide> UpdateCaptureF = (input, lastInput, eSide) => { };
        public Action<InputState, CaptureSide> EndCaptureF = (input, eSide) => { };


        override public InputDevice SupportedDevices {
            get { return InputDevice.AnySpatialDevice; }
        }

        protected FContext context;
        protected InputState lastInput;

        public SpatialClickDragBehaviour(FContext context)
        {
            this.context = context;
        }

        override public CaptureRequest WantsCapture(InputState input)
        {
            if ((input.bLeftTriggerPressed && input.LeftCaptureActive == false) ^
                 (input.bRightTriggerPressed && input.RightCaptureActive == false)) {
                CaptureSide eSide = (input.bLeftTriggerPressed) ? CaptureSide.Left : CaptureSide.Right;
                if (WantsCaptureF(input, eSide)) {
                    return CaptureRequest.Begin(this, eSide);
                }
            }
            return CaptureRequest.Ignore;
        }

        override public Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            BeginCaptureF(input, eSide);
            return Capture.Begin(this);
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            bool bReleased = (data.which == CaptureSide.Left) ? input.bLeftTriggerReleased : input.bRightTriggerReleased;
            if (bReleased) {
                EndCaptureF(input, data.which);
                lastInput = input;
                return Capture.End;
            } else {
                UpdateCaptureF(input, lastInput, data.which);
                return Capture.Continue;
            }
        }

        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            EndCaptureF(input, data.which);
            return Capture.End;
        }
    }




}
