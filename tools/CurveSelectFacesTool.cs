using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    public class CurveSelectFacesToolBuilder : IToolBuilder
    {
        public Action<DMeshSO, MeshFaceSelection> OnStrokeCompletedF = null;
        public Action<DMeshSO, MeshFaceSelection> OnApplyF = null;
        public double DefaultLineWidth = 0.1f;
        public Colorf DefaultLineColor = Colorf.VideoRed;

        public bool IsSupported(ToolTargetType type, List<SceneObject> targets)
        {
            return (type == ToolTargetType.SingleObject && targets[0] is DMeshSO);
        }

        public ITool Build(FScene scene, List<SceneObject> targets)
        {
            CurveSelectFacesTool tool = build_tool(scene, targets[0] as DMeshSO);
            tool.OnStrokeCompletedF = this.OnStrokeCompletedF;
            tool.OnApplyF = this.OnApplyF;
            tool.LineWidth = DefaultLineWidth;
            tool.LineColor = DefaultLineColor;
            return tool;
        }

        public CurveSelectFacesTool build_tool(FScene scene, DMeshSO meshSO)
        {
            return new CurveSelectFacesTool(scene, meshSO);
        }

    }




    public class CurveSelectFacesTool : ITool
    {
        static readonly public string BaseIdentifier = "curve_select_faces";

        public FScene Scene;
        public DMeshSO Target;

        virtual public string Name {
            get { return "CurveSelectFaces"; }
        }
        virtual public string TypeIdentifier {
            get { return BaseIdentifier; }
        }

        InputBehaviorSet behaviors;
        virtual public InputBehaviorSet InputBehaviors
        {
            get { return behaviors; }
            set { behaviors = value; }
        }

        ParameterSet parameters = new ParameterSet();
        public ParameterSet Parameters { get { return parameters; } }

        public ToolIndicatorSet Indicators { get; set; }


        public double LineWidth = 0.1f;
        public Colorf LineColor = Colorf.VideoRed;

        public Action<DMeshSO, MeshFaceSelection> OnStrokeCompletedF = null;
        public Action<DMeshSO, MeshFaceSelection> OnApplyF = null;


        public virtual bool AllowSelectionChanges { get { return false; } }


        public CurveSelectFacesTool(FScene scene, DMeshSO target)
        {
            Scene = scene;
            Target = target;

            behaviors = new InputBehaviorSet();

            // TODO is this where we should be doing this??
            behaviors.Add(
                new CurveSelectFacesTool_2DBehavior(this, scene.Context) { Priority = 5 });

            Indicators = new ToolIndicatorSet(this, scene);
        }

        virtual public void PreRender()
        {
            Indicators.PreRender();
        }

        virtual public bool HasApply { get { return (OnApplyF != null); } }
        virtual public bool CanApply { get { return HasApply && (lastSelection != null); } }
        virtual public void Apply() {
            if (OnApplyF != null && lastSelection != null)
                OnApplyF(Target, lastSelection);
        }


        public virtual void Setup()
        {
            Scene.Context.TransformManager.PushOverrideGizmoType(TransformManager.NoGizmoType);
        }
        public virtual void Shutdown()
        {
            Indicators.Disconnect(true);
            Scene.Context.TransformManager.PopOverrideGizmoType();
        }


        List<Ray3f> CurrentStroke = new List<Ray3f>();
        Vector3f CurrentStart = Vector3f.Zero;
        Vector3f CurrentEnd = Vector3f.Zero;
        Frame3f PlaneFrameS;

        MeshFaceSelection lastSelection;

        LineIndicator currentLine;

        public virtual void BeginStroke(Ray3f rayS)
        {
            Vector3f camDirW = Scene.ActiveCamera.Forward();
            Vector3f camDirS = Scene.ToSceneP(camDirW);
            PlaneFrameS = new Frame3f(Target.GetLocalFrame(CoordSpace.SceneCoords).Origin, camDirS);

            CurrentStroke.Clear();
            CurrentStroke.Add(rayS);

            CurrentStart = PlaneFrameS.RayPlaneIntersection(rayS.Origin, rayS.Direction, 2);
            CurrentEnd = CurrentStart;

            if (currentLine == null) {
                currentLine = new LineIndicator() {
                    SceneStartF = () => { return CurrentStart; },
                    SceneEndF = () => { return CurrentEnd; },
                    VisibleF = () => { return CurrentStroke.Count > 1; },
                    ColorF = () => { return this.LineColor; },
                    LineWidth = fDimension.Scene(LineWidth)
                };
                Indicators.AddIndicator(currentLine);
            }
        }


        public void UpdateStroke(Ray3f rayS)
        {
            CurrentStroke.Add(rayS);
            CurrentEnd = PlaneFrameS.RayPlaneIntersection(rayS.Origin, rayS.Direction, 2);
        }


        public void EndStroke()
        {
            if (CurrentStroke.Count >= 2) {
                DMesh3 mesh = Target.Mesh;

                TransformSequence toScene = SceneTransforms.ObjectToSceneXForm(Target);

                List<int> tris1 = new List<int>(), tris2 = new List<int>();
                Ray3f first = CurrentStroke[0], last = CurrentStroke[CurrentStroke.Count - 1];

                Vector3f v0 = PlaneFrameS.RayPlaneIntersection(first.Origin, first.Direction, 2);
                Vector3f v1 = PlaneFrameS.RayPlaneIntersection(last.Origin, last.Direction, 2);

                Vector3f planeN = Vector3f.Cross(first.Direction, last.Direction);
                Frame3f planeF = new Frame3f((v0 + v1)/2, planeN);

                foreach ( int tid in mesh.TriangleIndices()) {
                    Vector3f c = (Vector3f)mesh.GetTriCentroid(tid);
                    c = toScene.TransformP(c);
                    if (planeF.DistanceToPlaneSigned(c, 2) < 0)
                        tris1.Add(tid);
                    else
                        tris2.Add(tid);
                }

                double area1 = MeshMeasurements.AreaT(mesh, tris1);
                double area2 = MeshMeasurements.AreaT(mesh, tris2);

                lastSelection = new MeshFaceSelection(mesh);
                lastSelection.Select((area1 > area2) ? tris2 : tris1);
                lastSelection.LocalOptimize();

                if (OnStrokeCompletedF != null)
                    OnStrokeCompletedF(Target, lastSelection);
            }

            CurrentStroke.Clear();
        }



        public void CancelStroke()
        {
        }


    }




    





    class CurveSelectFacesTool_2DBehavior : Any2DInputBehavior
    {
        CurveSelectFacesTool Tool;
        FContext Context;

        public CurveSelectFacesTool_2DBehavior(CurveSelectFacesTool tool, FContext context)
        {
            Tool = tool;
            Context = context;
        }

        override public CaptureRequest WantsCapture(InputState input)
        {
            if ( Context.ToolManager.ActiveRightTool == Tool  && Pressed(input) )
                return CaptureRequest.Begin(this);
            return CaptureRequest.Ignore;
        }

        override public Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            Tool.BeginStroke(SceneRay(ref input, Context.Scene));
            return Capture.Begin(this);
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            Tool.UpdateStroke(SceneRay(ref input, Context.Scene));

            if ( Released(input) ) {
                Tool.EndStroke();
                return Capture.End;
            } else
                return Capture.Continue;
        }

        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            Tool.CancelStroke();
            return Capture.End;
        }
    }
}
