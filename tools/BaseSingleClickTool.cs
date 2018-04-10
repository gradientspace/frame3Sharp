using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public abstract class BaseSingleClickTool : ITool
    {

        /// <summary>
        /// "Normal" click behavior is to apply action on the release of a click, that way you can 
        /// cancel by mousing off object before releasing. Set this to false to apply action on click-down.
        /// </summary>
        public bool ActionOnRelease = true;


        // Tool identification that you need to provide
        abstract public string Name { get; }
        abstract public string TypeIdentifier { get; }


        protected FScene Scene;


        InputBehaviorSet behaviors;
        virtual public InputBehaviorSet InputBehaviors
        {
            get { return behaviors; }
            set { behaviors = value; }
        }

        ParameterSet parameters = new ParameterSet();
        public ParameterSet Parameters { get { return parameters; } }

        public BaseSingleClickTool(FScene scene)
        {
            this.Scene = scene;

            // do this here ??
            behaviors = new InputBehaviorSet();
            behaviors.Add(
                new BaseSingleClickTool_2DBehavior(scene.Context, ObjectFilter) { Priority = 5 });
        }

        public virtual bool AllowSelectionChanges { get { return false; } }


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

        public virtual void Setup()
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
        FContext context;
        Func<SceneObject, bool> ObjectFilterF;

        //Vector2d vClickDown;
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
                //vClickDown = ClickPoint(input);

                // if tool wants to apply action on press instead of release, we send it next frame
                BaseSingleClickTool tool = (context.ToolManager.ActiveRightTool as BaseSingleClickTool);
                if (tool.ActionOnRelease == false) {
                    clicked(clickSO, ClickPoint(input), WorldRay(input));
                }

                return Capture.Begin(this);
            }
            return Capture.Ignore;
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            BaseSingleClickTool tool = (context.ToolManager.ActiveRightTool as BaseSingleClickTool);
            Vector2d vCurPos = ClickPoint(input);

            if ( Released(input) ) {

                if (tool.ActionOnRelease) {
                    SORayHit rayHit;
                    bool bHit = context.Scene.FindSORayIntersection(WorldRay(input), out rayHit, ObjectFilterF);
                    if (bHit && rayHit.hitSO == clickSO) {
                        clicked(clickSO, vCurPos, WorldRay(input));
                    }
                }

                return Capture.End;
            } else
                return Capture.Continue;
        }


        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            return Capture.End;
        }


        protected virtual void clicked(SceneObject clickedSO, Vector2d position, Ray3f worldRay)
        {
            BaseSingleClickTool tool = (context.ToolManager.ActiveRightTool as BaseSingleClickTool);
            context.RegisterNextFrameAction(() => {
                tool.OnClicked(clickedSO, position, worldRay);
            });
        }
    }

}
