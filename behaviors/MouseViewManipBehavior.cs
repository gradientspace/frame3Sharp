using System;


namespace f3
{
    // currently only supports rotation
    public class MouseViewManipBehavior : StandardInputBehavior
    {
        FContext Context;

        float RotateSpeed = 2.0f;

        public MouseViewManipBehavior(FContext context)
        {
            this.Context = context;
        }

        public override InputDevice SupportedDevices
        {
            get { return InputDevice.Mouse; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            if ( input.bLeftMousePressed ) {
                SORayHit rayHit;
                if (Context.Scene.FindSORayIntersection(input.vMouseWorldRay, out rayHit) == false)
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
            if (input.bLeftMouseDown) {
                // do view manipuluation
                float dx = input.vMouseDelta2D.x;
                float dy = input.vMouseDelta2D.y;
                Context.ActiveCamera.Manipulator().SceneOrbit(Context.Scene, Context.ActiveCamera,
                    RotateSpeed * dx, RotateSpeed * dy);

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
