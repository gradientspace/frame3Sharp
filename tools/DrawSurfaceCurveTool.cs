using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    // [TODO] should take optional selection target...
    public class DrawSurfaceCurveToolBuilder : IToolBuilder
    {
        public float DefaultSamplingRate = 0.25f;
        public float DefaultSurfaceOffset = 0.05f;
        public bool Closed = false;
        public bool IsOverlayCurve = false;
        public bool AttachCurveToSurface = false;
        public Func<SOMaterial> CurveMaterialF = null;
        public Action<CurvePreview> EmitNewCurveF = null;

        public bool IsSupported(ToolTargetType type, List<SceneObject> targets)
        {
            return (type == ToolTargetType.SingleObject && targets[0].IsSurface);
        }

        public ITool Build(FScene scene, List<SceneObject> targets)
        {
            DrawSurfaceCurveTool tool = new DrawSurfaceCurveTool(scene, targets[0]);
            tool.SamplingRate = DefaultSamplingRate;
            tool.MinSamplingRate = Math.Min(tool.MinSamplingRate, DefaultSamplingRate * 0.1f);
            tool.SurfaceOffset = DefaultSurfaceOffset;
            tool.Closed = Closed;
            tool.IsOverlayCurve = IsOverlayCurve;
            tool.EmitNewCurveF = EmitNewCurveF;
            tool.CurveMaterialF = CurveMaterialF;
            tool.AttachCurveToSurface = AttachCurveToSurface;
            return tool;
        }
    }




    public class DrawSurfaceCurveTool : ITool
    {
        static readonly public string Identifier = "draw_surface_curve";

        FScene scene;

        virtual public string Name
        {
            get { return "DrawSurfaceCurve"; }
        }
        virtual public string TypeIdentifier
        {
            get { return Identifier; }
        }

        InputBehaviorSet behaviors;
        virtual public InputBehaviorSet InputBehaviors
        {
            get { return behaviors; }
            set { behaviors = value; }
        }

        public virtual bool AllowSelectionChanges { get { return true; } }


        /// <summary>
        /// Default behavior is to emit a PolyCurveSO in EndDraw(). To override
        /// this behavior, set this function and it will be called instead.
        /// </summary>
        public Action<CurvePreview> EmitNewCurveF = null;


        /// <summary>
        /// Material used for curve. Default is scene curve material
        /// </summary>
        public Func<SOMaterial> CurveMaterialF = null;


        public float MinSamplingRate = 0.001f;
        public float MaxSamplingRate = 9999.0f;

        float sampling_rate = 0.25f;
        virtual public float SamplingRate
        {
            get { return sampling_rate; }
            set { sampling_rate = MathUtil.Clamp(value, MinSamplingRate, MaxSamplingRate); }
        }

        float surface_offset = 0.05f;
        virtual public float SurfaceOffset
        {
            get { return surface_offset; }
            set { surface_offset = value; }
        }

        /// <summary>
        /// Closed loop or open curve
        /// </summary>
        virtual public bool Closed
        {
            get { return closed; }
            set { closed = value; }
        }
        bool closed = false;


        /// <summary>
        /// Overlay curves are drawn on top of scene
        /// </summary>
        virtual public bool IsOverlayCurve
        {
            get { return overlay; }
            set { overlay = value; }
        }
        bool overlay = false;



        public bool AttachCurveToSurface = false;



        SceneObject target;
        public SceneObject Target
        {
            get { return target; }
        }


        public DrawSurfaceCurveTool(FScene scene, SceneObject target)
        {
            this.scene = scene;
            this.target = target;

            behaviors = new InputBehaviorSet();

            // TODO is this where we should be doing this??
            behaviors.Add(
                new DrawSurfaceCurveTool_2DBehavior(scene.Context) { Priority = 5 });
            behaviors.Add(
                new DrawSurfaceCurveTool_SpatialDeviceBehavior(scene.Context) { Priority = 5 });


            // shut off transform gizmo
            scene.Context.TransformManager.PushOverrideGizmoType(TransformManager.NoGizmoType);

            scene.SelectionChangedEvent += Scene_SelectionChangedEvent;

            // restore radius
            //if (SavedSettings.Restore("DrawSurfaceCurveTool_width") != null)
            //    width = (float)SavedSettings.Restore("DrawSurfaceCurveTool_width");
        }

        private void Scene_SelectionChangedEvent(object sender, EventArgs e)
        {
            List<SceneObject> targets = new List<SceneObject>(scene.Selected);
            if (targets.Count == 1 && targets[0].IsSurface)
                this.target = targets[0];
        }

        virtual public void PreRender()
        {
            if (preview != null)
                preview.PreRender(scene);
        }

        virtual public bool HasApply { get { return false; } }
        virtual public bool CanApply { get { return false; } }
        virtual public void Apply() { }



        public void Shutdown()
        {
            scene.SelectionChangedEvent -= Scene_SelectionChangedEvent;
            scene.Context.TransformManager.PopOverrideGizmoType();
        }


        void CreateNewCurve()
        {
            if (AttachCurveToSurface) 
                preview = new LocalCurvePreview(Target as TransformableSO);
            else
                preview = new CurvePreview();

            preview.Closed = this.Closed;

            SOMaterial useMat = (CurveMaterialF == null) ? scene.DefaultCurveSOMaterial : CurveMaterialF();
            preview.Create(useMat, scene.RootGameObject, 
                (overlay) ? FPlatform.WidgetOverlayLayer : -1 );
        }


        CurvePreview preview;
        //InPlaceIterativeCurveSmooth smoother;



        // wow this is tricky. Want to do smoothed appending but as-you-draw, not at the end.
        // The main problem is the last vertex. We can wait until next vertex is far enough
        // (fDistThresh) but then drawing is very "poppy". So, we append the last point as
        // a "temp" vertex, which we then re-use when we actually append. 
        //
        // However, just allowing any temp vertex is not idea, as with spatial control your
        // hand will wiggle, and then the Curve end will do ugly things. So, if the temp
        // vertex isn't lying close to the tangent at the actual curve-end, we project
        // it onto the tangent, and use that. Unless it has negative T, then we ignore it. whew!
        //
        int last_append_idx;
        bool appended_last_update;
        bool have_temp_append;
        public void smooth_append(CurvePreview preview, Vector3f newPos, float fDistThresh)
        {
            // empty curve, always append
            if (preview.VertexCount == 0) {
                preview.AppendVertex(newPos);
                last_append_idx = preview.VertexCount-1;
                appended_last_update = true;
                have_temp_append = false;
                return;
            }

            double d = (newPos - preview[last_append_idx]).Length;
            if (d < fDistThresh) {
                // have not gone far enough for a real new vertex!

                Vector3f usePos = new Vector3f(newPos);
                bool bValid = false;

                // do we have enough vertices to do a good job?
                if (preview.VertexCount > 3) {
                    int nLast = (have_temp_append) ? preview.VertexCount - 2 : preview.VertexCount - 1;
                    Vector3d tan = preview.Tangent(nLast);
                    double fDot = tan.Dot((usePos - preview[nLast]).Normalized);
                    if (fDot > 0.9f) {      // cos(25) ~= 0.9f
                        // new vtx is aligned with tangent of last "real" vertex
                        bValid = true;
                    } else {
                        // not aligned, try projection onto tangent
                        Line3d l = new Line3d(preview[nLast], tan);
                        double t = l.Project(newPos);
                        if (t > 0) {
                            // projection of new vtx is 'ahead' so we can use it
                            usePos = (Vector3f)l.PointAt(t);
                            bValid = true;
                        }
                    }
                }

                if (bValid) {
                    if (appended_last_update) {
                        preview.AppendVertex(usePos);
                        have_temp_append = true;
                    } else if (have_temp_append) {
                        preview[preview.VertexCount - 1] = usePos;
                    }
                }

                appended_last_update = false;

            } else {
                // ok we drew far enough, add this position

                if (have_temp_append) {
                    // re-use temp vertex
                    preview[preview.VertexCount - 1] = newPos;
                    have_temp_append = false;
                } else {
                    preview.AppendVertex(newPos);
                }
                last_append_idx = preview.VertexCount - 1;
                appended_last_update = true;

                // do smoothing pass
                // [TODO] cannot do this until we can reproject onto surface!!
                //smoother.End = curve.VertexCount - 1;
                //smoother.Start = MathUtil.Clamp(smoother.End - 5, 0, smoother.End);
                //smoother.UpdateDeformation(2);
            }
        }


        float dist_thresh(float fCurveRadius, float fSceneScale)
        {
            // when you zoom in you can draw smoother curves, but not too much smoother...
            return MathUtil.Lerp(fCurveRadius, fCurveRadius * fSceneScale, 0.35f);
            //return fCurveRadius;
        }


        public void BeginDraw_Ray(Ray3f ray)
        {
            CreateNewCurve();

            SORayHit hit;
            bool bHit = target.FindRayIntersection(ray, out hit);
            if (!bHit)
                throw new Exception("DrawSurfaceCurveTool.BeginDraw_Ray: how did we get here if no target hit???");

            float offset = SurfaceOffset * scene.GetSceneScale();
            Vector3f vHit = hit.hitPos + offset * hit.hitNormal;
            float fScale = scene.GetSceneScale();
            smooth_append(preview,
                scene.SceneFrame.ToFrameP(vHit) / fScale, dist_thresh(SamplingRate, fScale));
        }


        public void UpdateDraw_Ray(Ray3f ray)
        {
            SORayHit hit;
            bool bHit = target.FindRayIntersection(ray, out hit);
            if (bHit) {
                float offset = SurfaceOffset * scene.GetSceneScale();
                Vector3f vHit = hit.hitPos + offset * hit.hitNormal;
                float fScale = scene.GetSceneScale();
                smooth_append(preview, scene.SceneFrame.ToFrameP(vHit) / fScale, dist_thresh(SamplingRate, fScale));
            }
        }


        public void EndDraw()
        {
            if (preview.Curve.ArcLength > 2 * SamplingRate) {

                if (EmitNewCurveF == null) {
                    // store undo/redo record for new primitive
                    SOMaterial mat = (CurveMaterialF == null) ? scene.DefaultCurveSOMaterial : CurveMaterialF();
                    PolyCurveSO CurveSO = preview.BuildSO(mat, 1.0f);
                    scene.History.PushChange(
                        new AddSOChange() { scene = scene, so = CurveSO, bKeepWorldPosition = false });

                    // link ?
                    if (AttachCurveToSurface) {
                        scene.History.PushChange(
                            new SOAddFrameLinkChangeOp(CurveSO, Target as TransformableSO));
                    }
                         
                    scene.History.PushInteractionCheckpoint();


                } else {
                    EmitNewCurveF(preview);
                }
            }

            preview.Destroy();
            preview = null;

            //SavedSettings.Save("DrawSurfaceCurveTool_width", width);
        }

        public void CancelDraw()
        {
            if (preview != null) {
                preview.Destroy();
                preview = null;
            }
        }


    }










    class DrawSurfaceCurveTool_SpatialDeviceBehavior : StandardInputBehavior
    {
        FContext context;

        public DrawSurfaceCurveTool_SpatialDeviceBehavior(FContext s)
        {
            context = s;
        }

        override public InputDevice SupportedDevices
        {
            get { return InputDevice.AnySpatialDevice; }
        }

        override public CaptureRequest WantsCapture(InputState input)
        {
            if (input.bLeftTriggerPressed ^ input.bRightTriggerPressed) {
                CaptureSide eSide = (input.bLeftTriggerPressed) ? CaptureSide.Left : CaptureSide.Right;
                ITool tool = context.ToolManager.GetActiveTool((int)eSide);
                if (tool != null && tool is DrawSurfaceCurveTool) {
                    DrawSurfaceCurveTool drawTool = tool as DrawSurfaceCurveTool;
                    SORayHit rayHit;
                    Ray3f ray = (input.bLeftTriggerPressed) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
                    if ( drawTool.Target.FindRayIntersection( ray, out rayHit ) )
                        return CaptureRequest.Begin(this, eSide);
                }
            }
            return CaptureRequest.Ignore;
        }

        override public Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            Ray3f sideRay = (eSide == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
            DrawSurfaceCurveTool tool = context.ToolManager.GetActiveTool((int)eSide) as DrawSurfaceCurveTool;
            tool.BeginDraw_Ray(sideRay);
            return Capture.Begin(this, eSide);
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            DrawSurfaceCurveTool tool = context.ToolManager.GetActiveTool((int)data.which) as DrawSurfaceCurveTool;
            Ray3f sideRay = (data.which == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;

            // [RMS] this is a hack for trigger+shoulder grab gesture...really need some way
            //   to interrupt captures!!
            if ((data.which == CaptureSide.Left && input.bLeftShoulderPressed) ||
                 (data.which == CaptureSide.Right && input.bRightShoulderPressed)) {
                tool.CancelDraw();
                return Capture.End;
            }

            tool.UpdateDraw_Ray( sideRay );

            bool bReleased = (data.which == CaptureSide.Left) ? input.bLeftTriggerReleased : input.bRightTriggerReleased;
            if (bReleased) {
                tool.EndDraw();
                return Capture.End;
            } else
                return Capture.Continue;
        }

        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            DrawSurfaceCurveTool tool = context.ToolManager.GetActiveTool((int)data.which) as DrawSurfaceCurveTool;
            tool.CancelDraw();
            return Capture.End;
        }
    }










    class DrawSurfaceCurveTool_2DBehavior : Any2DInputBehavior
    {
        FContext context;

        public DrawSurfaceCurveTool_2DBehavior(FContext s)
        {
            context = s;
        }

        override public CaptureRequest WantsCapture(InputState input)
        {
            if (context.ToolManager.ActiveRightTool == null || !(context.ToolManager.ActiveRightTool is DrawSurfaceCurveTool))
                return CaptureRequest.Ignore;
            if ( Pressed(input) ) {
                DrawSurfaceCurveTool tool =
                    (context.ToolManager.ActiveRightTool as DrawSurfaceCurveTool);
                SORayHit rayHit;
                if ( tool.Target.FindRayIntersection( WorldRay(input), out rayHit ) )
                    return CaptureRequest.Begin(this);
            }
            return CaptureRequest.Ignore;
        }

        override public Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            DrawSurfaceCurveTool tool =
                (context.ToolManager.ActiveRightTool as DrawSurfaceCurveTool);
            tool.BeginDraw_Ray(WorldRay(input));
            return Capture.Begin(this);
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            DrawSurfaceCurveTool tool =
                (context.ToolManager.ActiveRightTool as DrawSurfaceCurveTool);

            tool.UpdateDraw_Ray( WorldRay(input) );

            if ( Released(input) ) {
                tool.EndDraw();
                return Capture.End;
            } else
                return Capture.Continue;
        }

        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            DrawSurfaceCurveTool tool =
                (context.ToolManager.ActiveRightTool as DrawSurfaceCurveTool);
            tool.CancelDraw();
            return Capture.End;
        }
    }
}
