using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public class SurfaceBrushToolBuilder : IToolBuilder
    {
        public fDimension InitialRadius = fDimension.World(1.0);

        public virtual bool IsSupported(ToolTargetType type, List<SceneObject> targets)
        {
            return (type == ToolTargetType.SingleObject && targets[0] is DMeshSO);
        }

        public virtual ITool Build(FScene scene, List<SceneObject> targets)
        {
            DMeshSO target = targets[0] as DMeshSO;
            SurfaceBrushTool tool = new_tool(scene, target);
            tool.Radius = InitialRadius.Clone();
            return tool;
        }

        protected virtual SurfaceBrushTool new_tool(FScene scene, DMeshSO target)
        {
            return new SurfaceBrushTool(scene, target);
        }
    }


    /// <summary>
    /// Tool for sculpting 3D PolyCurveSO objects. Supports:
    ///   - adaptive resampling of sculpted curve
    ///   - push/pull and smooth deformation operations
    ///   - arbitrary ProjectionTarget that curve is projected onto (eg for curve-on-surface)
    ///   - arbitrary BrushTarget that brush is stuck to (eg for curve-on-surface, snap-to-axis, etc)
    ///   - Primary & Secondary tools, with toggle
    ///   - change tracking via Scene History (enabled by default)
    /// </summary>
    public class SurfaceBrushTool : ITool
    {
        static readonly public string Identifier = "surface_brush";

        FScene scene;
        public FScene Scene {
            get { return scene; }
        }

        virtual public string Name {
            get { return "SurfaceBrush"; }
        }
        virtual public string TypeIdentifier {
            get { return Identifier; }
        }

        // [RMS] we could probably support this...
        public virtual bool AllowSelectionChanges { get { return false; } }

        fDimension radius = fDimension.World(1.0);
        virtual public fDimension Radius
        {
            get { return radius; }
            set { radius = value; }
        }


        protected SurfaceBrushType primaryBrushOp = new SurfaceBrushNothingType();
        public virtual SurfaceBrushType PrimaryBrush {
            get { return primaryBrushOp; }
            set { primaryBrushOp = value; }
        }

        protected SurfaceBrushType secondaryBrushOp = new SurfaceBrushNothingType();
        public virtual SurfaceBrushType SecondaryBrush {
            get { return secondaryBrushOp; }
            set { secondaryBrushOp = value; }
        }

        public virtual SurfaceBrushType ActiveBrush {
            get { return activeBrushOp; }
            set { activeBrushOp = value; }
        }


        bool invert = false;
        public bool Invert {
            get { return invert; }
            set { invert = value; }
        }


        bool useSecondary = false;
        public bool UseSecondary {
            get { return useSecondary; }
            set { useSecondary = value; }
        }
        




        InputBehaviorSet behaviors;
        virtual public InputBehaviorSet InputBehaviors {
            get { return behaviors; }
            set { behaviors = value; }
        }

        ParameterSet parameters = new ParameterSet();
        public ParameterSet Parameters { get { return parameters; } }

        DMeshSO target;
        virtual public DMeshSO Target {
            get { return target; }
        }

        Frame3f lastBrushPosW;
        ToolIndicatorSet Indicators;
        BrushCursorSphere brushIndicator;
        fMaterial primaryBrushMat;
        fMaterial secondaryBrushMat;

        public SurfaceBrushTool(FScene scene, DMeshSO target)
        {
            this.scene = scene;
            this.target = target;

            behaviors = new InputBehaviorSet();

            // TODO is this where we should be doing this??
            behaviors.Add(
                new SurfaceBrushTool_2DInputBehavior(this, scene.Context) { Priority = 5 });
            if (FPlatform.IsUsingVR()) {
                behaviors.Add(
                    new SurfaceBrushTool_SpatialBehavior(this, scene.Context) { Priority = 5 });
            }

            // shut off transform gizmo
            scene.Context.TransformManager.PushOverrideGizmoType(TransformManager.NoGizmoType);

            Indicators = new ToolIndicatorSet(this, scene);

        }


        virtual public void Setup()
        {
            brushIndicator = new BrushCursorSphere() {
                PositionF = () => { return lastBrushPosW.Origin; },
                Radius = fDimension.World(() => { return radius.WorldValue; })
            };
            primaryBrushMat = MaterialUtil.CreateTransparentMaterialF(Colorf.CornflowerBlue, 0.2f);
            secondaryBrushMat = MaterialUtil.CreateTransparentMaterialF(Colorf.ForestGreen, 0.2f);
            Indicators.AddIndicator(brushIndicator);
            brushIndicator.material = primaryBrushMat;
        }


        virtual public void Shutdown()
        {
            // restore transform gizmo
            scene.Context.TransformManager.PopOverrideGizmoType();

            Indicators.Disconnect(true);
        }


        virtual public void PreRender()
        {
            brushIndicator.sharedMaterial = UseSecondary ? secondaryBrushMat : primaryBrushMat;
            Indicators.PreRender();
        }


        virtual public bool HasApply { get { return false; } }
        virtual public bool CanApply { get { return false; } }
        virtual public void Apply() { }



        protected BrushCursorSphere BrushIndicator {
            get { return brushIndicator; }
        }
        protected fMaterial BrushPrimaryMaterial {
            get { return primaryBrushMat; }
        }
        protected fMaterial BrushSecondaryMaterial {
            get { return secondaryBrushMat; }
        }


        bool in_stroke = false;
        SurfaceBrushType activeBrushOp;

        protected virtual void begin_stroke(Frame3f vStartFrameL, int nHitTID)
        {
            activeBrushOp = (UseSecondary && secondaryBrushOp != null) ? secondaryBrushOp : primaryBrushOp;

            activeBrushOp.Mesh = Target.Mesh;
            activeBrushOp.Radius = this.Radius.SceneValue;
            activeBrushOp.Invert = this.Invert;
            activeBrushOp.BeginStroke(vStartFrameL, nHitTID);
        }


        protected virtual void update_stroke(Frame3f vLocalF, int nHitTID)
        {
            activeBrushOp.UpdateStroke(vLocalF, nHitTID);
        }


        protected virtual void end_stroke()
        {
            activeBrushOp.EndStroke();
        }

        protected virtual void update_preview(Frame3f vLocalF, int nHitTID)
        {
            activeBrushOp = (UseSecondary && secondaryBrushOp != null) ? secondaryBrushOp : primaryBrushOp;

            activeBrushOp.UpdatePreview(vLocalF, nHitTID);
        }


        /// <summary>
        /// Return false from this function to ignore ray-hit for this ray. 
        /// This is useful in cases where brush hit-tests and moves on some proxy
        /// surface, but is modifying another surface/target, and so should only
        /// begin strokes when that object is hit
        /// </summary>
        public virtual bool BeginStrokeHitTestFilter(Ray3f worldRay)
        {
            return true;
        }



        public void BeginBrushStroke(Frame3f vFrameW, int nHitTID)
        {
            if (in_stroke)
                throw new Exception("SurfaceBrushTool.BeginBrushStroke: already in brush stroke!");

            Frame3f vFrameS = scene.ToSceneFrame(vFrameW);
            Frame3f vFrameL = SceneTransforms.SceneToObject(Target, vFrameS);
            begin_stroke(vFrameL, nHitTID);
            
            in_stroke = true;
            lastBrushPosW = vFrameW;
        }

        public void UpdateBrushStroke(Frame3f vFrameW, int nHitTID)
        {
            if (in_stroke == false)
                throw new Exception("SurfaceBrushTool.UpdateBrushStroke: not in brush stroke!");

            Frame3f vFrameS = scene.ToSceneFrame(vFrameW);
            Frame3f vFrameL = SceneTransforms.SceneToObject(Target, vFrameS);
            update_stroke(vFrameL, nHitTID);

            lastBrushPosW = vFrameW;
        }

        public void EndBrushStroke()
        {
            if (in_stroke == false)
                throw new Exception("SurfaceBrushTool.EndBrushStroke: not in brush stroke!");
            in_stroke = false;
            end_stroke();
        }


        public void UpdateBrushPreview(Frame3f vFrameW, int nHitTID)
        {
            Frame3f vFrameS = scene.ToSceneFrame(vFrameW);
            Frame3f vFrameL = SceneTransforms.SceneToObject(Target, vFrameS);
            update_preview(vFrameL, nHitTID);

            lastBrushPosW = vFrameW;
        }

    }








    class SurfaceBrushTool_SpatialBehavior : StandardInputBehavior
    {
        FContext context;
        SurfaceBrushTool tool;

        Vector3f lastHitPosW;
        int lastHitTID;

        void update_last_hit(SurfaceBrushTool tool, Ray3f rayW)
        {
            SORayHit soHit;
            if ( tool.Target.FindRayIntersection(rayW, out soHit) ) { 
                lastHitPosW = soHit.hitPos;
                lastHitTID = soHit.hitIndex;
            } 
        }

        public SurfaceBrushTool_SpatialBehavior(SurfaceBrushTool tool, FContext s)
        {
            context = s;
            this.tool = tool;
        }

        override public InputDevice SupportedDevices
        {
            get { return InputDevice.AnySpatialDevice; }
        }

        override public CaptureRequest WantsCapture(InputState input)
        {
            // [RMS] something doesn't make sense here...if tool was active on both sides, this could
            // capture on either? We should only be capturing if capture-side tool == this.tool...

            if (input.bLeftTriggerPressed ^ input.bRightTriggerPressed) {
                CaptureSide eSide = (input.bLeftTriggerPressed) ? CaptureSide.Left : CaptureSide.Right;
                ITool tool = context.ToolManager.GetActiveTool((int)eSide);
                if (tool != null && tool is SurfaceBrushTool) {
                    return CaptureRequest.Begin(this, eSide);
                }
            }
            return CaptureRequest.Ignore;
        }

        override public Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            //Ray3f sideRay = (eSide == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
            Frame3f sideHandF = (eSide == CaptureSide.Left) ? input.LeftHandFrame : input.RightHandFrame;
            sideHandF.Origin += SceneGraphConfig.VRHandTipOffset * sideHandF.Z;
            SurfaceBrushTool tool = context.ToolManager.GetActiveTool((int)eSide) as SurfaceBrushTool;

            // [RMS] old oculus hack that was not very nice...
            //bool bTouchingStick =
            //    (eSide == CaptureSide.Left) ? input.bLeftStickTouching : input.bRightStickTouching;
            //tool.ActiveBrush = (bTouchingStick) ?
            //    SurfaceBrushTool.BrushTool.Smooth : SurfaceBrushTool.BrushTool.SoftMove;

            tool.BeginBrushStroke(sideHandF, lastHitTID);

            return Capture.Begin(this, eSide);
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            SurfaceBrushTool tool = context.ToolManager.GetActiveTool((int)data.which) as SurfaceBrushTool;

            // [RMS] this is a hack for trigger+shoulder grab gesture...really need some way
            //   to interrupt captures!!
            if ((data.which == CaptureSide.Left && input.bLeftShoulderPressed) ||
                 (data.which == CaptureSide.Right && input.bRightShoulderPressed)) {
                tool.EndBrushStroke();
                return Capture.End;
            }

            Vector2f vStick = (data.which == CaptureSide.Left) ? input.vLeftStickDelta2D : input.vRightStickDelta2D;
            if (vStick[1] != 0) {
                tool.Radius.Add( fDimension.World(vStick[1] * resize_speed(ref input)) );
            }

            //Ray3f sideRay = (data.which == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
            Frame3f sideHandF = (data.which == CaptureSide.Left) ? input.LeftHandFrame : input.RightHandFrame;
            sideHandF.Origin += SceneGraphConfig.VRHandTipOffset * sideHandF.Z;
            //tool.UpdateDraw_Spatial(sideRay, sideHandF);

            bool bReleased = (data.which == CaptureSide.Left) ? input.bLeftTriggerReleased : input.bRightTriggerReleased;
            if (bReleased) {
                tool.EndBrushStroke();
                return Capture.End;
            } else {
                tool.UpdateBrushStroke(sideHandF, lastHitTID);
                return Capture.Continue;
            }
        }

        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            SurfaceBrushTool tool = context.ToolManager.GetActiveTool((int)data.which) as SurfaceBrushTool;
            tool.EndBrushStroke();
            return Capture.End;
        }


        public override bool EnableHover
        {
            get { return true; }
        }
        public override void UpdateHover(InputState input)
        {
            ToolSide eSide = context.ToolManager.FindSide(tool);
            Frame3f sideHandF = (eSide == ToolSide.Left) ? input.LeftHandFrame : input.RightHandFrame;
            sideHandF.Origin += SceneGraphConfig.VRHandTipOffset * sideHandF.Z;
            //update_last_hit(tool, input.vMouseWorldRay);
            tool.UpdateBrushPreview(sideHandF, lastHitTID);

            Vector2f vStick = (eSide == ToolSide.Left) ? input.vLeftStickDelta2D : input.vRightStickDelta2D;
            if ( Math.Abs(vStick[1]) > 0.5f) {
                tool.Radius.Add(fDimension.World(vStick[1] * resize_speed(ref input)));
            }

            // cycle brush on press+left/right
            //bool stick_up = (eSide == ToolSide.Left) ? input.bLeftStickReleased : input.bRightStickReleased;
            //if ( stick_up && Math.Abs(vStick[0]) > 0.9 ) {
            //    int n = (int)tool.ActiveBrush;
            //    n = MathUtil.ModuloClamp(n + (vStick[0] < 0 ? -1 : 1), 2);
            //    tool.ActiveBrush = (SurfaceBrushTool.BrushTool)n;
            //}
        }
        public override void EndHover(InputState input)
        {
        }



        float resize_speed(ref InputState input)
        {
            if (input.IsForDevice(InputDevice.HTCViveWands))
                return 0.0025f;
            else
                return 0.01f;
        }

    }








    class SurfaceBrushTool_2DInputBehavior : Any2DInputBehavior
    {
        FContext context;
        SurfaceBrushTool tool;

        Frame3f lastHitFrameW;
        int lastHitTID;

        bool update_last_hit(SurfaceBrushTool tool, Ray3f rayW)
        {
            SORayHit soHit;
            if (tool.Target.FindRayIntersection(rayW, out soHit)) {
                lastHitFrameW = new Frame3f(soHit.hitPos, soHit.hitNormal);
                lastHitTID = soHit.hitIndex;
                return true;
            }
            return false;
        }

        public SurfaceBrushTool_2DInputBehavior(SurfaceBrushTool tool, FContext s)
        {
            this.tool = tool;
            context = s;
        }

        override public CaptureRequest WantsCapture(InputState input)
        {
            if (context.ToolManager.ActiveRightTool == null || !(context.ToolManager.ActiveRightTool is SurfaceBrushTool))
                return CaptureRequest.Ignore;
            if ( Pressed(ref input) ) {
                if (tool.BeginStrokeHitTestFilter(WorldRay(ref input)) == false)
                    return CaptureRequest.Ignore;
                if ( update_last_hit(tool, WorldRay(ref input)) )
                    return CaptureRequest.Begin(this);
            }
            return CaptureRequest.Ignore;
        }

        override public Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            update_last_hit(tool, WorldRay(ref input));

            if (input.IsForDevice(InputDevice.Mouse)) {
                tool.Invert = input.bCtrlKeyDown;
                tool.UseSecondary = input.bShiftKeyDown;
            }

            tool.BeginBrushStroke(lastHitFrameW, lastHitTID);

            return Capture.Begin(this, CaptureSide.Any);
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            if (tool == null)
                throw new Exception("SurfaceBrushTool_MouseBehavior.UpdateCapture: tool is null, how did we get here?");

            if ( Released(ref input) ) {
                tool.EndBrushStroke();
                return Capture.End;
            } else {
                update_last_hit(tool, WorldRay(ref input));
                tool.UpdateBrushStroke( lastHitFrameW, lastHitTID);
                return Capture.Continue;
            }
        }

        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            tool.EndBrushStroke();
            return Capture.End;
        }


        public override bool EnableHover
        {
            get { return CachedIsMouseInput; }
        }
        public override void UpdateHover(InputState input)
        {
            update_last_hit(tool, WorldRay(ref input));
            tool.UpdateBrushPreview(lastHitFrameW, lastHitTID);
        }
        public override void EndHover(InputState input)
        {
        }
    }












    /// <summary>
    /// Base-class for DMesh3 surface-paint operation. 
    /// Subclasses must implement abstract Apply() method.
    /// </summary>
    public abstract class SurfaceBrushType
    {
        protected DMesh3 _mesh;
        public DMesh3 Mesh {
            get { return _mesh; }
            set { if (_mesh != value) { _mesh = value; mesh_changed();  } }
        }

        protected double radius = 0.1f;
        public double Radius {
            get { return radius; }
            set { radius = value; }
        }

        protected bool invert = false;
        public bool Invert {
            get { return invert; }
            set { invert = value; }
        }

        protected virtual void mesh_changed()
        {
        }

        protected Frame3f previous_posL;
        public Frame3f PreviousPosL {
            get { return previous_posL; }
        }


        public virtual void BeginStroke(Frame3f vStartPosL, int tid)
        {
            previous_posL = vStartPosL;
        }

        public virtual void UpdateStroke(Frame3f vNextPosL, int tid)
        {
            Apply(vNextPosL, tid);
            previous_posL = vNextPosL;
        }

        public virtual void EndStroke()
        {
        }


        public virtual void UpdatePreview(Frame3f vPreviewPosL, int tid)
        {
        }

        public abstract void Apply(Frame3f vNextPosL, int tid);
    }





    public class SurfaceBrushNothingType : SurfaceBrushType
    {
        public override void Apply(Frame3f vNextPos, int tid)
        {
            // do nothing!
        }
    }


}
