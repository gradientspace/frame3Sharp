using System;


namespace f3
{
    // currently only supports rotation
    public class MouseViewRotateBehavior : StandardInputBehavior
    {
        // how does user start/end this action
        public Func<InputState, bool> ActivateF = MouseBehaviors.LeftButtonPressedF;
        public Func<InputState, bool> ContinueF = MouseBehaviors.LeftButtonDownF;

        // tumbling speed
        public float RotateSpeed = 2.0f;

        public enum RotateModes
        {
            Turntable = 0,
            CrazyCam = 1
        }
        public RotateModes RotationMode = RotateModes.Turntable;

        FContext Context;

        public MouseViewRotateBehavior(FContext context)
        {
            this.Context = context;
        }

        public override InputDevice SupportedDevices
        {
            get { return InputDevice.Mouse; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            if (ActivateF(input)) {
                return CaptureRequest.Begin(this, CaptureSide.Any);
            }
            return CaptureRequest.Ignore;
        }

        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            Context.ActiveCamera.SetTargetVisible(true);
            return Capture.Begin(this, eSide, null);
        }


        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            if (ContinueF(input)) {
                // do view manipuluation
                float dx = input.vMouseDelta2D.x;
                float dy = input.vMouseDelta2D.y;

                if (RotationMode == RotateModes.Turntable) {
                    Context.ActiveCamera.Manipulator().SceneOrbit(Context.Scene, Context.ActiveCamera,
                        RotateSpeed * dx, RotateSpeed * dy);
                } else {
                    Context.ActiveCamera.Manipulator().SceneTumble(Context.Scene, Context.ActiveCamera,
                    -RotateSpeed * dx, RotateSpeed * dy);
                }

                return Capture.Continue;
            } else {
                Context.ActiveCamera.SetTargetVisible(false);
                return Capture.End;
            }
        }


        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            Context.ActiveCamera.SetTargetVisible(false);
            return Capture.End;
        }
    }







    // currently only supports rotation
    public class MouseViewPanBehavior : StandardInputBehavior
    {
        // how does user start/end this action
        public Func<InputState, bool> ActivateF = MouseBehaviors.LeftButtonPressedF;
        public Func<InputState, bool> ContinueF = MouseBehaviors.LeftButtonDownF;

        // movement speed
        public float PanSpeed = 0.5f;
        public bool Adaptive = false;

        FContext Context;

        public MouseViewPanBehavior(FContext context)
        {
            this.Context = context;
        }

        public override InputDevice SupportedDevices
        {
            get { return InputDevice.Mouse; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            if ( ActivateF(input) ) {
                return CaptureRequest.Begin(this, CaptureSide.Any);
            }
            return CaptureRequest.Ignore;
        }

        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            Context.ActiveCamera.SetTargetVisible(true);
            return Capture.Begin(this, eSide, null);
        }


        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            if ( ContinueF(input) ) {
                // do view manipuluation
                float dx = input.vMouseDelta2D.x;
                float dy = input.vMouseDelta2D.y;
                if (Adaptive) {
                    Context.ActiveCamera.Manipulator().SceneAdaptivePan(Context.Scene,
                        Context.ActiveCamera, PanSpeed * dx, PanSpeed * dy);
                } else {
                    Context.ActiveCamera.Manipulator().ScenePan(Context.Scene,
                        Context.ActiveCamera, PanSpeed * dx, PanSpeed * dy);
                }

                return Capture.Continue;
            } else {
                Context.ActiveCamera.SetTargetVisible(false);
                return Capture.End;
            }
        }


        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            Context.ActiveCamera.SetTargetVisible(false);
            return Capture.End;
        }
    }



}
