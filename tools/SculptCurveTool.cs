using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public class SculptCurveToolBuilder : IToolBuilder
    {
        public fDimension InitialRadius = fDimension.World(1.0);
        public float SmoothAlpha = 0.15f;
        public int SmoothIterations = 5;

        public bool IsSupported(ToolTargetType type, List<SceneObject> targets)
        {
            return (type == ToolTargetType.Scene) ||
                (type == ToolTargetType.SingleObject && targets[0] is PolyCurveSO);
        }

        public ITool Build(FScene scene, List<SceneObject> targets)
        {
            return new SculptCurveTool(scene, targets) {
                Radius = InitialRadius.Clone(),
                SmoothAlpha = this.SmoothAlpha,
                SmoothIterations = this.SmoothIterations,

            };
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
    public class SculptCurveTool : ITool
    {
        static readonly public string Identifier = "sculpt_curve";

        FScene scene;

        virtual public string Name {
            get { return "SculptCurve"; }
        }
        virtual public string TypeIdentifier {
            get { return Identifier; }
        }

        public virtual bool AllowSelectionChanges { get { return true; } }

        public bool EnableStrokeChangeTracking = true;


        // curve will be projected onto this target as it is edited
        IProjectionTarget target = null;
        virtual public IProjectionTarget ProjectionTarget
        {
            get { return target; }
            set { target = value; }
        }

        // brush rays will 'hit' this target instead of hitting curve
        IIntersectionTarget brushTarget = null;
        virtual public IIntersectionTarget BrushTarget
        {
            get { return brushTarget; }
            set { brushTarget = value; }
        }


        fDimension radius = fDimension.World(1.0);
        virtual public fDimension Radius
        {
            get { return radius; }
            set { radius = value; }
        }


        virtual public float SmoothAlpha { get; set; }
        virtual public int SmoothIterations { get; set; }



        // Various sculpting brushes are supported. Setting ActiveBrush changes the current brush.
        // In a sculpting UI, usually there is a toggle between a primary and secondary brush (eg like hold shift to smooth).
        // To implement this, set PrimaryBrushTool and SecondaryBrushTool to the types you want to use, and
        // then the (default) behaviors will auto-switch between them
        // Currenty, default SculptCurveTool_MouseBehavior is hardcoded to use secondary brush when shift key is down


        public enum BrushTool
        {
            SoftMove,
            Smooth
        }

        public BrushTool PrimaryBrushTool {
            get { return primary_brush; }
            set { primary_brush = value; }
        }
        BrushTool primary_brush = BrushTool.SoftMove;

        public BrushTool SecondaryBrushTool {
            get { return secondary_brush; }
            set { secondary_brush = value; }
        }
        BrushTool secondary_brush = BrushTool.Smooth;

        public BrushTool ActiveBrush {
            get { return active_brush; }
            set { active_brush = value; }
        }
        BrushTool active_brush = BrushTool.SoftMove;




        InputBehaviorSet behaviors;
        virtual public InputBehaviorSet InputBehaviors {
            get { return behaviors; }
            set { behaviors = value; }
        }

        ParameterSet parameters = new ParameterSet();
        public ParameterSet Parameters { get { return parameters; } }

        List<PolyCurveSO> targets = new List<PolyCurveSO>();
        virtual public IEnumerable<SceneObject> Targets
        {
            get { return targets.Cast<SceneObject>(); }
        }

        Frame3f lastBrushPos;
        ToolIndicatorSet indicators;
        BrushCursorSphere brushIndicator;
        fMaterial moveSphereMat;
        fMaterial smoothSphereMat;

        public SculptCurveTool(FScene scene, List<SceneObject> targets)
        {
            this.scene = scene;

            behaviors = new InputBehaviorSet();

            // TODO is this where we should be doing this??
            behaviors.Add(
                new SculptCurveTool_2DInputBehavior(this, scene.Context) { Priority = 5 });
            behaviors.Add(
                new SculptCurveTool_SpatialBehavior(this, scene.Context) { Priority = 5 });

            // shut off transform gizmo
            scene.Context.TransformManager.PushOverrideGizmoType(TransformManager.NoGizmoType);

            scene.SelectionChangedEvent += Scene_SelectionChangedEvent;
            // initialize active set with input selection
            Scene_SelectionChangedEvent(null, null);


            indicators = new ToolIndicatorSet(this, scene);
            brushIndicator = new BrushCursorSphere() {
                PositionF = () => { return lastBrushPos.Origin; },
                Radius = fDimension.World( () => { return radius.WorldValue; } )
            };
            moveSphereMat = MaterialUtil.CreateTransparentMaterialF(Colorf.CornflowerBlue, 0.2f);
            smoothSphereMat = MaterialUtil.CreateTransparentMaterialF(Colorf.ForestGreen, 0.2f);
            indicators.AddIndicator(brushIndicator);
            brushIndicator.material = moveSphereMat;

            SmoothAlpha = 0.15f;
            SmoothIterations = 5;
        }


        // update snaps
        private void Scene_SelectionChangedEvent(object sender, EventArgs e)
        {
            if (scene.Selected.Count > 0)
                update_curves(scene.Selected);
            else
                update_curves(scene.SceneObjects);
        }

        void update_curves(IEnumerable<SceneObject> vTargets)
        {
            List<PolyCurveSO> vCurves = new List<PolyCurveSO>();
            foreach (var so in vTargets) {
                if (so is PolyCurveSO)
                    vCurves.Add(so as PolyCurveSO);
            }
            if (targets.SequenceEqual(vCurves) == false)
                targets = vCurves;
        }




        virtual public void PreRender()
        {
            brushIndicator.material = (ActiveBrush == BrushTool.Smooth) ? smoothSphereMat : moveSphereMat;
            indicators.PreRender();
        }


        virtual public bool HasApply { get { return false; } }
        virtual public bool CanApply { get { return false; } }
        virtual public void Apply() { }


        public virtual void Setup()
        {
        }

        virtual public void Shutdown()
        {
            scene.SelectionChangedEvent -= Scene_SelectionChangedEvent;
            
            // restore transform gizmo
            scene.Context.TransformManager.PopOverrideGizmoType();

            indicators.Disconnect(true);
        }




        SculptCurveDeformation sculptOp;
        CurveResampler resampler = new CurveResampler();
        bool in_stroke = false;

        DCurve3VerticesEditedOp activeChange = null;


        void begin_stroke(Frame3f vStartFrameL)
        {
            if ( active_brush == BrushTool.SoftMove ) {
                SculptCurveMove move = new SculptCurveMove();
                move.SmoothAlpha = 0.1;
                move.SmoothIterations = 1;
                sculptOp = move;
            } else if ( active_brush == BrushTool.Smooth ) {
                SculptCurveSmooth smooth = new SculptCurveSmooth();
                smooth.SmoothAlpha = this.SmoothAlpha;
                smooth.SmoothIterations = this.SmoothIterations;
                sculptOp = smooth;
            }

            sculptOp.Curve = targets[0].Curve;
            sculptOp.Radius = this.Radius.SceneValue;
            sculptOp.BeginDeformation( vStartFrameL );

            if (EnableStrokeChangeTracking)
                activeChange = new DCurve3VerticesEditedOp(targets[0], true);
        }


        bool update_stroke(Frame3f vLocalF)
        {
            SculptCurveMove.DeformInfo dinfo = sculptOp.UpdateDeformation(vLocalF);
            if (dinfo.bNoChange)
                return false;

            double dMaxEdgeLen = this.Radius.SceneValue * 0.1;
            double dMinEdgeLen = dMaxEdgeLen * 0.3;

            // resample curve
            if ( dinfo.maxEdgeLenSqr > dMaxEdgeLen * dMaxEdgeLen ||
                    dinfo.minEdgeLenSqr < dMinEdgeLen*dMinEdgeLen ) {
                //List<Vector3d> newV = resampler.SplitResample(targets[0].Curve, dMaxEdgeLen);
                List<Vector3d> newV = resampler.SplitCollapseResample(targets[0].Curve, dMaxEdgeLen, dMinEdgeLen);
                if (newV != null && newV.Count > 0) {
                    targets[0].Curve.SetVertices(newV, true);
                }
            }

            return true;
        }


        void end_stroke()
        {
            sculptOp = null;

            if (activeChange != null) {
                activeChange.StoreAfter();
                scene.History.PushChange(activeChange, true);
                scene.History.PushInteractionCheckpoint();
                activeChange = null;
            }
        }



        public void BeginBrushStroke(Frame3f vFrame)
        {
            if (in_stroke)
                throw new Exception("SculptCurveTool.BeginBrushStroke: already in brush stroke!");

            if (targets.Count > 0) {
                Frame3f localFrame = targets[0].GetLocalFrame(CoordSpace.ObjectCoords);
                Frame3f vFrameL = localFrame.ToFrame(scene.ToSceneFrame(vFrame));
                begin_stroke(vFrameL);
            }
            in_stroke = true;
            lastBrushPos = vFrame;
        }

        public void UpdateBrushStroke(Frame3f vFrame)
        {
            if (in_stroke == false)
                throw new Exception("SculptCurveTool.UpdateBrushStroke: not in brush stroke!");

            // ignore large jumps in brush position, which make a mess of curve
            if (lastBrushPos.Origin.Distance(vFrame.Origin) > Radius.WorldValue)
                return;

            Frame3f localFrame = targets[0].GetLocalFrame(CoordSpace.ObjectCoords);
            Frame3f vFrameL = localFrame.ToFrame(scene.ToSceneFrame(vFrame));
            bool bModified = update_stroke(vFrameL);

            if (bModified && ProjectionTarget != null)
                project_to_target();

            lastBrushPos = vFrame;
        }

        public void EndBrushStroke()
        {
            if (in_stroke == false)
                throw new Exception("SculptCurveTool.EndBrushStroke: not in brush stroke!");
            in_stroke = false;
            end_stroke();
        }


        public void UpdateBrushPreview(Frame3f vPos)
        {
            lastBrushPos = vPos;
        }



        void project_to_target()
        {
            PolyCurveSO sourceSO = targets[0];
            DCurve3 curve = sourceSO.Curve;
            int N = curve.VertexCount;
            for ( int i = 0; i < N; ++i ) {
                Vector3f v = (Vector3f)curve[i];
                Vector3f vW = SceneTransforms.TransformTo(v, sourceSO, CoordSpace.ObjectCoords, CoordSpace.WorldCoords);
                vW = (Vector3f)ProjectionTarget.Project(vW);
                curve[i] = SceneTransforms.TransformTo(vW, sourceSO, CoordSpace.WorldCoords, CoordSpace.ObjectCoords);
            }
        }

    }












    class SculptCurveTool_SpatialBehavior : StandardInputBehavior
    {
        FContext context;
        SculptCurveTool tool;

        Vector3f lastHitPosW;
        Frame3f curDrawFrameW;
        bool in_draw = false;

        void update_last_hit(SculptCurveTool tool, Ray3f ray)
        {
            SORayHit soHit;
            // stick brush to target if we have one
            if (tool.BrushTarget != null) {
                Vector3d hitPos, hitNormal;
                bool bHit = tool.BrushTarget.RayIntersect(ray, out hitPos, out hitNormal);
                if (bHit)
                    lastHitPosW = (Vector3f)hitPos;

            } else if (in_draw) {
                lastHitPosW = curDrawFrameW.RayPlaneIntersection(ray.Origin, ray.Direction, 2);
            } else if (SceneUtil.FindNearestRayIntersection(tool.Targets, ray, out soHit)) {
                lastHitPosW = soHit.hitPos;
            } else {
                Frame3f f = new Frame3f(lastHitPosW, context.ActiveCamera.Forward());
                lastHitPosW = f.RayPlaneIntersection(ray.Origin, ray.Direction, 2);
            }
        }

        public SculptCurveTool_SpatialBehavior(SculptCurveTool tool, FContext s)
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
                if (tool != null && tool is SculptCurveTool) {
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
            SculptCurveTool tool = context.ToolManager.GetActiveTool((int)eSide) as SculptCurveTool;

            // [RMS] old oculus hack that was not very nice...
            //bool bTouchingStick =
            //    (eSide == CaptureSide.Left) ? input.bLeftStickTouching : input.bRightStickTouching;
            //tool.ActiveBrush = (bTouchingStick) ?
            //    SculptCurveTool.BrushTool.Smooth : SculptCurveTool.BrushTool.SoftMove;

            tool.BeginBrushStroke(sideHandF);
            in_draw = true;

            return Capture.Begin(this, eSide);
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            SculptCurveTool tool = context.ToolManager.GetActiveTool((int)data.which) as SculptCurveTool;

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
                in_draw = false;
                tool.EndBrushStroke();
                return Capture.End;
            } else {
                tool.UpdateBrushStroke(sideHandF);

                // update draw frame if we are drawing on a target (?)
                if ( tool.BrushTarget != null )
                    curDrawFrameW = new Frame3f(lastHitPosW, context.ActiveCamera.Forward());

                return Capture.Continue;
            }
        }

        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            SculptCurveTool tool = context.ToolManager.GetActiveTool((int)data.which) as SculptCurveTool;
            in_draw = false;
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
            tool.UpdateBrushPreview(sideHandF);

            Vector2f vStick = (eSide == ToolSide.Left) ? input.vLeftStickDelta2D : input.vRightStickDelta2D;
            if ( Math.Abs(vStick[1]) > 0.5f) {
                tool.Radius.Add(fDimension.World(vStick[1] * resize_speed(ref input)));
            }

            // cycle brush on press+left/right
            bool stick_up = (eSide == ToolSide.Left) ? input.bLeftStickReleased : input.bRightStickReleased;
            if ( stick_up && Math.Abs(vStick[0]) > 0.9 ) {
                int n = (int)tool.ActiveBrush;
                n = MathUtil.ModuloClamp(n + (vStick[0] < 0 ? -1 : 1), 2);
                tool.ActiveBrush = (SculptCurveTool.BrushTool)n;
            }
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








    class SculptCurveTool_2DInputBehavior : Any2DInputBehavior
    {
        FContext context;
        SculptCurveTool tool;

        Vector3f lastHitPosW;
        Frame3f curDrawFrameW;
        bool in_draw = false;

        void update_last_hit(SculptCurveTool tool, Ray3f ray)
        {
            SORayHit soHit;
            // stick brush to target if we have one
            if (tool.BrushTarget != null) {
                Vector3d hitPos, hitNormal;
                bool bHit = tool.BrushTarget.RayIntersect(ray, out hitPos, out hitNormal);
                if (bHit)
                    lastHitPosW = (Vector3f)hitPos;

            } else if (in_draw) {
                lastHitPosW = curDrawFrameW.RayPlaneIntersection(ray.Origin, ray.Direction, 2);
            } else if (SceneUtil.FindNearestRayIntersection(tool.Targets, ray, out soHit)) {
                lastHitPosW = soHit.hitPos;
            } else {
                Frame3f f = new Frame3f(lastHitPosW, context.ActiveCamera.Forward());
                lastHitPosW = f.RayPlaneIntersection(ray.Origin, ray.Direction, 2);
            }
        }

        public SculptCurveTool_2DInputBehavior(SculptCurveTool tool, FContext s)
        {
            this.tool = tool;
            context = s;
        }

        override public CaptureRequest WantsCapture(InputState input)
        {
            if (context.ToolManager.ActiveRightTool == null || !(context.ToolManager.ActiveRightTool is SculptCurveTool))
                return CaptureRequest.Ignore;
            if ( Pressed(ref input) ) {

                // if we have a brush target, we only capture when we click on it
                if (tool.BrushTarget != null) {
                    Vector3d vHit, vNormal;
                    bool bHit = tool.BrushTarget.RayIntersect( WorldRay(ref input), out vHit, out vNormal);
                    if ( bHit == false )
                        return CaptureRequest.Ignore;
                }

                return CaptureRequest.Begin(this);
            }
            return CaptureRequest.Ignore;
        }

        override public Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            update_last_hit(tool, WorldRay(ref input));
            curDrawFrameW = new Frame3f(lastHitPosW, context.ActiveCamera.Forward());

            tool.ActiveBrush = (input.bShiftKeyDown) ?
                tool.SecondaryBrushTool : tool.PrimaryBrushTool;

            tool.BeginBrushStroke(new Frame3f(lastHitPosW));
            in_draw = true;

            return Capture.Begin(this, CaptureSide.Any);
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            if (tool == null)
                throw new Exception("SculptCurveTool_MouseBehavior.UpdateCapture: tool is null, how did we get here?");

            if ( Released(ref input) ) {
                in_draw = false;
                tool.EndBrushStroke();
                return Capture.End;
            } else {
                update_last_hit(tool, WorldRay(ref input));
                tool.UpdateBrushStroke( new Frame3f(lastHitPosW) );

                // update draw frame if we are drawing on a target (?)
                if ( tool.BrushTarget != null )
                    curDrawFrameW = new Frame3f(lastHitPosW, context.ActiveCamera.Forward());

                return Capture.Continue;
            }
        }

        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            in_draw = false;
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
            tool.UpdateBrushPreview(new Frame3f(lastHitPosW));
        }
        public override void EndHover(InputState input)
        {
        }
    }

}
