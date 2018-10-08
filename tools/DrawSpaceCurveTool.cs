using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    // [TODO] should take optional selection target...
    public class DrawSpaceCurveToolBuilder : IToolBuilder
    {
        public float DefaultSamplingRateS = 0.01f;
        public bool Closed = false;
        public bool IsOverlayCurve = false;
        public bool AttachCurveToTarget = false;
        public Func<SOMaterial> CurveMaterialF = null;
        public Action<CurvePreview> EmitNewCurveF = null;
        public DrawSpaceCurveTool.DrawMode InputMode = DrawSpaceCurveTool.DrawMode.Continuous;

        public bool IsSupported(ToolTargetType type, List<SceneObject> targets)
        {
            return true;
        }

        public virtual ITool Build(FScene scene, List<SceneObject> targets)
        {
            DrawSpaceCurveTool tool = new_tool(scene, targets[0]);
            tool.SamplingRateScene = DefaultSamplingRateS;
            tool.MinSamplingRate = Math.Min(tool.MinSamplingRate, DefaultSamplingRateS * 0.1f);
            tool.Closed = Closed;
            tool.IsOverlayCurve = IsOverlayCurve;
            tool.EmitNewCurveF = EmitNewCurveF;
            tool.CurveMaterialF = CurveMaterialF;
            tool.AttachCurveToTarget = AttachCurveToTarget;
            tool.InputMode = InputMode;
            return tool;
        }

        protected virtual DrawSpaceCurveTool new_tool(FScene scene, SceneObject target)
        {
            return new DrawSpaceCurveTool(scene, target);
        }
    }




    public class DrawSpaceCurveTool : ITool
    {
        static readonly public string Identifier = "draw_space_curve";

        protected FScene Scene;

        virtual public string Name
        {
            get { return "DrawSpaceCurve"; }
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

        ParameterSet parameters = new ParameterSet();
        public ParameterSet Parameters { get { return parameters; } }

        public virtual bool AllowSelectionChanges { get { return true; } }



        /// <summary>
        /// post-processor applied to curve. This does not modify the curve you are drawing,
        /// it modifies the curve you see. See CurvePreview.CurveProcessorF for details.
        /// </summary>
        public Action<List<Vector3d>> CurveProcessorF = null;

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

        float sampling_rate = 0.05f;
        virtual public float SamplingRateScene
        {
            get { return sampling_rate; }
            set { sampling_rate = MathUtil.Clamp(value, MinSamplingRate, MaxSamplingRate); }
        }
        /// <summary>
        /// Closed loop or open curve. When Closed, the closing line will be shown during drawing.
        /// </summary>
        virtual public bool Closed
        {
            get { return closed; }
            set { closed = value; }
        }
        bool closed = false;


        public enum DrawMode {
            Continuous, OnClick
        }
        DrawMode input_mode;
        virtual public DrawMode InputMode {
            get { return input_mode; }
            set { input_mode = value; }
        }


        /// <summary>
        /// Overlay curves are drawn "on top" of scene, ie will be visible even if "inside" target object
        /// </summary>
        virtual public bool IsOverlayCurve
        {
            get { return overlay; }
            set { overlay = value; }
        }
        bool overlay = false;


        /// <summary>
        /// This has two effects. One, it means that if the target surface moves, the curve you are
        /// drawing will move with it (useful in bimanual VR drawing). Two, the generated SO will be
        /// attached to the Target so using an SOLink (see EndDraw())
        /// </summary>
        public bool AttachCurveToTarget = false;


        /// <summary>
        /// Target object you are drawing on.
        /// </summary>
        public SceneObject Target
        {
            get { return target; }
        }
        SceneObject target;


        public DrawSpaceCurveTool(FScene scene, SceneObject target)
        {
            this.Scene = scene;
            this.target = target;

            behaviors = new InputBehaviorSet();

            // TODO is this where we should be doing this??
            if (FPlatform.IsUsingVR()) {
                behaviors.Add(
                    new DrawSpaceCurveTool_SpatialDeviceBehavior(this, scene.Context) { Priority = 5 });
            }

            // shut off transform gizmo
            scene.Context.TransformManager.PushOverrideGizmoType(TransformManager.NoGizmoType);

            scene.SelectionChangedEvent += Scene_SelectionChangedEvent;

            // restore radius
            //if (SavedSettings.Restore("DrawSpaceCurveTool_width") != null)
            //    width = (float)SavedSettings.Restore("DrawSpaceCurveTool_width");
        }

        private void Scene_SelectionChangedEvent(object sender, EventArgs e)
        {
            List<SceneObject> targets = Scene.FindSceneObjectsOfType<SceneObject>();
            if (targets.Count == 1)
                this.target = targets[0];
        }

        virtual public void PreRender()
        {
            if (preview != null)
                preview.PreRender(Scene);
        }


        virtual public bool HasApply { get { return false; } }
        virtual public bool CanApply { get { return false; } }
        virtual public void Apply() { }


        public virtual void Setup()
        {
        }

        virtual public void Shutdown()
        {
            Scene.SelectionChangedEvent -= Scene_SelectionChangedEvent;
            Scene.Context.TransformManager.PopOverrideGizmoType();
        }


        void CreateNewCurve()
        {
            if (AttachCurveToTarget && Target != null) 
                preview = new LocalCurvePreview(Target);
            else
                preview = new CurvePreview();

            preview.Closed = this.Closed;

            SOMaterial useMat = (CurveMaterialF == null) ? Scene.DefaultCurveSOMaterial : CurveMaterialF();
            preview.Create(useMat, Scene.RootGameObject, 
                (overlay) ? FPlatform.WidgetOverlayLayer : -1 );

            if (CurveProcessorF != null)
                preview.CurveProcessorF = CurveProcessorF;
        }


        protected CurvePreview preview;

        public bool InDraw { get { return in_draw; } }
        bool in_draw;


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
            }
        }


        float dist_thresh(float fCurveRadius, float fSceneScale)
        {
            // when you zoom in you can draw smoother curves, but not too much smoother...
            return MathUtil.Lerp(fCurveRadius, fCurveRadius * fSceneScale, 0.35f);
            //return fCurveRadius;
        }


        public virtual void BeginDraw_Ray_Continuous(Frame3f frameW)
        {
            CreateNewCurve();
            in_draw = true;
           
            float fScale = Scene.GetSceneScale();
            smooth_append(preview, Scene.ToSceneP(frameW.Origin), dist_thresh(SamplingRateScene, fScale));
        }


        public virtual void UpdateDraw_Ray_Continuous(Frame3f frameW)
        {
            float fScale = Scene.GetSceneScale();
            smooth_append(preview, Scene.ToSceneP(frameW.Origin), dist_thresh(SamplingRateScene, fScale));
        }




        public virtual void BeginDraw_Ray_MultiClick()
        {
            CreateNewCurve();
            in_draw = true;
        }


        public virtual bool UpdateDraw_Ray_MultiClick(Frame3f frameW)
        {
            bool first = (preview.VertexCount == 0);
            Vector3f vPos = Scene.ToSceneP(frameW.Origin);

            // the last vertex is the one we are repositioning in UpdateDrawPreview. So, on
            // click we actaully want to freeze that vertex to this position and then add a new
            // temporary one. Except for the first vertex.
            if (first) {
                preview.AppendVertex(vPos);
                preview.AppendVertex(vPos);
                OnAddedClickPoint(vPos, true);
            } else {
                // close curve if we are within close threshold
                //if ( preview.VertexCount > 2  &&  vPos.Distance((Vector3f)preview[0]) < CloseThresholdScene )
                //    return false;

                //preview[preview.VertexCount - 1] = vPos;
                preview.AppendVertex(vPos);
                OnAddedClickPoint(vPos, false);
            }

            return true;
        }


        public virtual void UpdateDrawPreview_Ray_MultiClick(Frame3f frameW)
        {
            if (preview == null || preview.VertexCount == 0)
                return;
            Vector3f vPos = Scene.ToSceneP(frameW.Origin);
            preview[preview.VertexCount - 1] = vPos;
        }




        public virtual void EndDraw()
        {
            in_draw = false;
            if (preview == null)
                return;
            if (preview.Curve.VertexCount > 2 && preview.Curve.ArcLength > 2 * SamplingRateScene) {

                // update Closed state because in some cases we change this during drawing
                // (ie when drawing multi-point curve, but closing at end)
                preview.Closed = this.Closed;

                if (EmitNewCurveF == null) {
                    // store undo/redo record for new primitive
                    SOMaterial mat = (CurveMaterialF == null) ? Scene.DefaultCurveSOMaterial : CurveMaterialF();
                    PolyCurveSO CurveSO = preview.BuildSO(mat, 1.0f);
                    Scene.History.PushChange(
                        new AddSOChange() { scene = Scene, so = CurveSO, bKeepWorldPosition = false });

                    // link ?
                    if (AttachCurveToTarget) {
                        Scene.History.PushChange(
                            new SOAddFrameLinkChangeOp(CurveSO, Target));
                    }
                         
                    Scene.History.PushInteractionCheckpoint();


                } else {
                    EmitNewCurveF(preview);
                }
            }

            preview.Destroy();
            preview = null;

            //SavedSettings.Save("DrawSpaceCurveTool_width", width);
        }

        public void CancelDraw()
        {
            if (preview != null) {
                preview.Destroy();
                preview = null;
            }
        }





        /// <summary>
        /// This is for subclasses to use, to respond to clicks in DrawMode==OnClick.
        /// Is called from UpdateDraw_Ray_MultiClick()
        /// </summary>
        protected virtual void OnAddedClickPoint(Vector3d vNew, bool bFirst)
        {
        }

    }










    class DrawSpaceCurveTool_SpatialDeviceBehavior : StandardInputBehavior
    {
        DrawSpaceCurveTool ownerTool;
        FContext context;

        public DrawSpaceCurveTool_SpatialDeviceBehavior(DrawSpaceCurveTool tool, FContext s)
        {
            ownerTool = tool;
            context = s;

            // have to cancel capture if we are in multi-click mode and tool exits
            s.ToolManager.OnToolActivationChanged += ToolManager_OnToolActivationChanged;
        }

        private void ToolManager_OnToolActivationChanged(ITool tool, ToolSide eSide, bool bActivated)
        {
            if (bActivated == false && tool == ownerTool) {
                ownerTool.EndDraw();
            }
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
                if (tool != null && tool is DrawSpaceCurveTool) {
                    DrawSpaceCurveTool drawTool = tool as DrawSpaceCurveTool;
                    return CaptureRequest.Begin(this, eSide);
                }
            }
            return CaptureRequest.Ignore;
        }

        override public Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            Frame3f worldFrame = (eSide == CaptureSide.Left) ? input.LeftHandFrame : input.RightHandFrame;
            DrawSpaceCurveTool tool = context.ToolManager.GetActiveTool((int)eSide) as DrawSpaceCurveTool;

            if (tool.InputMode == DrawSpaceCurveTool.DrawMode.Continuous)
                tool.BeginDraw_Ray_Continuous(worldFrame);
            else
                tool.BeginDraw_Ray_MultiClick();

            return Capture.Begin(this, eSide);
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            Frame3f worldFrame = (data.which == CaptureSide.Left) ? input.LeftHandFrame : input.RightHandFrame;
            DrawSpaceCurveTool tool = context.ToolManager.GetActiveTool((int)data.which) as DrawSpaceCurveTool;

            // [RMS] this is a hack for trigger+shoulder grab gesture...really need some way
            //   to interrupt captures!!
            if ((data.which == CaptureSide.Left && input.bLeftShoulderPressed) ||
                 (data.which == CaptureSide.Right && input.bRightShoulderPressed)) {
                tool.CancelDraw();
                return Capture.End;
            }


            // this happens if we exit tool while in draw (cts or multi-click). We need to fail gracefully in those cases.
            if (tool == null) {
                return Capture.End;
            }
            // this can happen if we called tool.EndDraw() somewhere else
            if (tool.InDraw == false)
                return Capture.End;

            bool bReleased = (data.which == CaptureSide.Left) ? input.bLeftTriggerReleased : input.bRightTriggerReleased;
            if (tool.InputMode == DrawSpaceCurveTool.DrawMode.OnClick) {
                if (bReleased) {
                    if (tool.UpdateDraw_Ray_MultiClick(worldFrame) == false) {
                        tool.EndDraw();
                        return Capture.End;
                    }
                } else
                    tool.UpdateDrawPreview_Ray_MultiClick(worldFrame);

                return Capture.Continue;

            } else {
                tool.UpdateDraw_Ray_Continuous(worldFrame);
                if (bReleased) {
                    tool.EndDraw();
                    return Capture.End;
                } else
                    return Capture.Continue;
            }
        }

        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            DrawSpaceCurveTool tool = context.ToolManager.GetActiveTool((int)data.which) as DrawSpaceCurveTool;
            tool.CancelDraw();
            return Capture.End;
        }
    }









}
