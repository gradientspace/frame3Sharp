using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public abstract class BaseSingleClickTool : ITool
    {
        protected FScene Scene;

        abstract public string Name { get; }
        abstract public string TypeIdentifier { get; }


        InputBehaviorSet behaviors;
        virtual public InputBehaviorSet InputBehaviors
        {
            get { return behaviors; }
            set { behaviors = value; }
        }


        public BaseSingleClickTool(FScene scene)
        {
            this.Scene = scene;

            // do this here ??
            behaviors = new InputBehaviorSet();
            behaviors.Add(
                new BaseSingleClickTool_2DBehavior(scene.Context, ObjectFilter) { Priority = 5 });
        }


        // override this to limit SOs that can be clicked
        virtual public bool ObjectFilter(SceneObject so)
        {
            return true;
        }



        virtual public bool HasApply { get { return false; } }
        virtual public bool CanApply { get { return false; } }
        virtual public void Apply() { }


        virtual public void PreRender()
        {
        }

        virtual public void Shutdown()
        {
        }


        virtual public void OnClicked(SceneObject so, Vector2d clickPos, Ray3f clickRay)
        {

        }


    }





    class BaseSingleClickTool_2DBehavior : Any2DInputBehavior
    {
        public static float MaxClickMoveDelta = 3.0f;       // in pixels

        FContext context;
        Func<SceneObject, bool> ObjectFilterF;

        Vector2d vClickDown;
        SceneObject clickSO;

        public BaseSingleClickTool_2DBehavior(FContext s, Func<SceneObject, bool> filterF )
        {
            context = s;
            ObjectFilterF = filterF;
        }

        override public CaptureRequest WantsCapture(InputState input)
        {
            if (context.ToolManager.ActiveRightTool == null || !(context.ToolManager.ActiveRightTool is BaseSingleClickTool))
                return CaptureRequest.Ignore;
            if ( Pressed(input) ) {
                SORayHit rayHit;
                if ( context.Scene.FindSORayIntersection(WorldRay(input), out rayHit, ObjectFilterF) ) {
                    return CaptureRequest.Begin(this);
                }
            }
            return CaptureRequest.Ignore;
        }

        override public Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            SORayHit rayHit;
            bool bHit = context.Scene.FindSORayIntersection(WorldRay(input), out rayHit, ObjectFilterF);
            if (bHit) {
                clickSO = rayHit.hitSO;
                vClickDown = ClickPoint(input);
                return Capture.Begin(this);
            }
            return Capture.Ignore;
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            Vector2d vCurPos = ClickPoint(input);

            if ( Released(input) ) {

                if (vCurPos.Distance(vClickDown) > MaxClickMoveDelta)
                    return Capture.End;

                SORayHit rayHit;
                bool bHit = context.Scene.FindSORayIntersection(WorldRay(input), out rayHit, ObjectFilterF);
                if (bHit == false || rayHit.hitSO != clickSO)
                    return Capture.End;

                BaseSingleClickTool tool =
                    (context.ToolManager.ActiveRightTool as BaseSingleClickTool);
                tool.OnClicked(clickSO, vCurPos, WorldRay(input));
                return Capture.End;
            } else
                return Capture.Continue;
        }


        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            return Capture.End;
        }
    }

}
