using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    /// <summary>
    /// This is a generic Tool that provides a press/drag/release type interaction
    /// for a point on a 3D surface. This allows you to implement behaviors like draw-on-surface,
    /// position-at-surface-point, etc
    /// 
    /// Override Begin/Update/End to implement behavior, and ObjectFilter/WantCapture to limit target objects
    /// </summary>
    public abstract class BaseSurfacePointTool : ITool
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

        public virtual bool AllowSelectionChanges { get { return false; } }


        public BaseSurfacePointTool(FScene scene)
        {
            this.Scene = scene;

            // do this here ??
            behaviors = new InputBehaviorSet();
            behaviors.Add(
                new BaseSurfacePointTool_2DBehavior(scene.Context, ObjectFilter) { Priority = 5 });
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



        virtual public bool WantCapture(SceneObject so, Vector2d downPos, Ray3f downRay)
        {
            return true;
        }


        /// <summary>
        /// called on click-down
        /// </summary>
        virtual public void Begin(SceneObject so, Vector2d downPos, Ray3f downRay)
        {
        }

        /// <summary>
        /// called each frame as cursor moves
        /// </summary>
        virtual public void Update(Vector2d downPos, Ray3f downRay)
        {
        }

        /// <summary>
        /// called after click is released
        /// </summary>
        virtual public void End()
        {
        }


    }





    class BaseSurfacePointTool_2DBehavior : Any2DInputBehavior
    {
        FContext context;
        Func<SceneObject, bool> ObjectFilterF;

        Vector2d vStartDown;
        SceneObject targetSO;

        public BaseSurfacePointTool_2DBehavior(FContext s, Func<SceneObject, bool> filterF )
        {
            context = s;
            ObjectFilterF = filterF;
        }

        override public CaptureRequest WantsCapture(InputState input)
        {
            if (context.ToolManager.ActiveRightTool == null || !(context.ToolManager.ActiveRightTool is BaseSurfacePointTool))
                return CaptureRequest.Ignore;
            if ( Pressed(input) ) {
                SORayHit rayHit;
                if ( context.Scene.FindSORayIntersection(WorldRay(input), out rayHit, ObjectFilterF) ) {
                    BaseSurfacePointTool tool =
                        (context.ToolManager.ActiveRightTool as BaseSurfacePointTool);
                    if (tool.WantCapture(rayHit.hitSO, ClickPoint(input), WorldRay(input))) {
                        return CaptureRequest.Begin(this);
                    }
                }
            }
            return CaptureRequest.Ignore;
        }

        override public Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            SORayHit rayHit;
            bool bHit = context.Scene.FindSORayIntersection(WorldRay(input), out rayHit, ObjectFilterF);
            if (bHit) {
                targetSO = rayHit.hitSO;
                vStartDown = ClickPoint(input);

                BaseSurfacePointTool tool =
                    (context.ToolManager.ActiveRightTool as BaseSurfacePointTool);
                tool.Begin(targetSO, vStartDown, WorldRay(input));

                return Capture.Begin(this);
            }
            return Capture.Ignore;
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            Vector2d vCurPos = ClickPoint(input);

            BaseSurfacePointTool tool =
                (context.ToolManager.ActiveRightTool as BaseSurfacePointTool);
            if ( Released(input) ) {
                tool.End();
                return Capture.End;
            } else {
                SORayHit rayHit;
                bool bHit = targetSO.FindRayIntersection(WorldRay(input), out rayHit);
                if ( bHit )
                    tool.Update(vCurPos, WorldRay(input));
                return Capture.Continue;
            }
        }


        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            return Capture.End;
        }
    }

}
