using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;
using UnityEngine;

namespace f3
{
    public delegate void PolyCurveSOModifiedEvent(PolyCurveSO so);


    public class PolyCurveSO : BaseSO
    {
        protected GameObject root;

        protected DCurve3 curve;
        public DCurve3 Curve
        {
            get { return curve; }
            set { curve = value; invalidate_geometry(); }
        }
        int curve_timestamp = -1;
        protected void invalidate_geometry() {
            curve_timestamp = -1;
        }

        float visibleWidth;
        AxisAlignedBox3d localBounds;

        protected bool EnableLineRenderer { get; set; }

        public PolyCurveSO()
        {
            EnableLineRenderer = true;
        }

        public override string Name {
            get { return root.GetName(); }
            set { root.SetName(value); }
        }

        override public SOType Type { get { return SOTypes.PolyCurve; } }

        public override GameObject RootGameObject {
            get { return root; }
        }

        public override bool IsSurface {
            get { return false; }
        }

        public override SceneObject Duplicate()
        {
            PolyCurveSO copy = new PolyCurveSO();
            copy.Curve = new DCurve3(this.curve);
            copy.Create(this.GetAssignedSOMaterial());
            copy.SetLocalFrame(
                this.GetLocalFrame(CoordSpace.ObjectCoords), CoordSpace.ObjectCoords);
            copy.SetLocalScale(this.GetLocalScale());
            return copy;
        }

        virtual protected void Create_internal(Material useMaterial)
        {
            // this is for children to subclass
        }

        public PolyCurveSO Create(SOMaterial defaultMaterial)
        {
            if (curve == null) {
                LineGenerator gen = new LineGenerator() {
                    Start = Vector3.zero, End = 10.0f * Vector3.up, StepSize = 0.1f
                };
                gen.Generate();
                curve = new DCurve3();
                gen.Make(curve);
            }

            // assumes type identifier is something like BlahBlahSO
            root = new GameObject(UniqueNames.GetNext( Type.identifier.Remove(Type.identifier.Length-2) ));

            if (EnableLineRenderer) {
                LineRenderer ren = root.AddComponent<LineRenderer>();
                ren.startWidth = ren.endWidth = 0.05f;
                ren.useWorldSpace = false;
            }

            AssignSOMaterial(defaultMaterial);       // need to do this to setup BaseSO material stack
            Material useMaterial = CurrentMaterial;
            Create_internal(useMaterial);

            UpdateGeometry();

            increment_timestamp();

            return this;
        }


        override protected void set_material_internal(Material m)
        {
            base.set_material_internal(m);

            if (EnableLineRenderer) {
                LineRenderer ren = root.GetComponent<LineRenderer>();
                ren.material = m;
            }
        }



        virtual protected void UpdateGeometry_internal()
        {
            // this is for children to subclass
        }
        protected void add_to_bounds(Vector3f v) {
            localBounds.Contain(v);
        }

        virtual public void UpdateGeometry()
        {
            if (curve_timestamp != curve.Timestamp) {

                if (EnableLineRenderer) {
                    LineRenderer ren = root.GetComponent<LineRenderer>();
                    Vector3[] vec = new Vector3[curve.VertexCount];
                    int i = 0;
                    foreach (var v in curve.Vertices)
                        vec[i++] = (Vector3)v;
                    ren.numPositions = curve.VertexCount;
                    ren.SetPositions(vec);
                }

                localBounds = curve.GetBoundingBox();

                UpdateGeometry_internal();

                curve_timestamp = curve.Timestamp;
                increment_timestamp();

                on_curve_modified();
            }
        }




        public override void PreRender()
        {
            UpdateGeometry();

            // figuring out a decent line width is tricky. Want to be responsive to camera
            //  pos, so line doesn't get super-thick when zoomed in. So we want to measure
            //  screen-space radius. But off-screen vertices are a problem. So, only consider
            //  vertices within a level, pointing-forward view cone (can't be actual view cone
            //  because then line thickness changes as you turn head!). 
            //
            //  Also sub-sample verts for efficiency. Probably we don't need to do this
            //  every frame...but how to distribute?

            if (EnableLineRenderer) {

                float fNewWidth = VRUtil.EstimateStableCurveWidth( GetScene(),
                    GetLocalFrame(CoordSpace.SceneCoords), curve, SceneGraphConfig.DefaultSceneCurveVisualDegrees);

                if (fNewWidth > 0) {
                    visibleWidth = fNewWidth;
                    LineRenderer r = root.GetComponent<LineRenderer>();
                    r.startWidth = r.endWidth = visibleWidth;
                }
            }
        }



        public PolyCurveSOModifiedEvent OnCurveModified;
        void on_curve_modified()
        {
            var tmp = OnCurveModified;
            if (tmp != null) tmp(this);
        }



        override public AxisAlignedBox3f GetTransformedBoundingBox()
        {
            throw new NotImplementedException("PolyCurveSO.GetTransformedBoundingBox");
        }
        override public AxisAlignedBox3f GetLocalBoundingBox()
        {
            return new AxisAlignedBox3f((Vector3f)localBounds.Min, (Vector3f)localBounds.Max);
        }


        override public bool FindRayIntersection(Ray3f ray, out SORayHit hit)
        {
            hit = null;

            Ray sceneRay = GetScene().ToSceneRay(ray);
            Frame3f frameL = GetLocalFrame(CoordSpace.ObjectCoords);
            Ray localRay = frameL.ToFrame(sceneRay);

            float sceneWidth = GetScene().ToSceneDimension(visibleWidth);

            AxisAlignedBox3d hitBox = localBounds;
            hitBox.Expand(sceneWidth * 0.5f);
            Bounds hitBounds = new Bounds((Vector3)hitBox.Center, (Vector3)hitBox.Diagonal);
            if ( hitBounds.IntersectRay(localRay) == false)
                return false;

            double rayHitT;
            if (CurveUtils.FindClosestRayIntersection(curve, sceneWidth * 0.5f, localRay, out rayHitT)) {
                hit = new SORayHit();
                hit.fHitDist = (float)rayHitT;
                hit.hitPos = localRay.GetPoint(hit.fHitDist);
                hit.hitPos = GetScene().ToWorldP(frameL.FromFrameP(hit.hitPos));
                hit.hitNormal = Vector3.zero;
                hit.hitGO = root;
                hit.hitSO = this;
                return true;
            }
            return false;
        }




    }
}
