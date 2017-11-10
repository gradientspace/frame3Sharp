using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

// [RMS] still need this for LineRenderer
using UnityEngine;

namespace f3
{
    public delegate void PolyCurveSOModifiedEvent(PolyCurveSO so);


    public class PolyCurveSO : BaseSO, DCurve3Source
    {
        /// <summary>
        /// To make it easier to click on a PolyCurveSO, set this to a larger
        /// multiplier. Can override per-SO with HitWidthMultiplier param
        /// </summary>
        public static float DefaultHitWidthMultiplier = 1.0f;


        protected fGameObject root;

        protected DCurve3 curve;
        public DCurve3 Curve
        {
            get { return curve; }
            set { curve = value; on_set_curve(); }
        }
        int curve_timestamp = -1;
        protected virtual void on_set_curve() {
            curve_timestamp = -1;
        }


        /// <summary>
        /// scale width of curve for hit-testing. uses DefaultHitWidthMultiplier until you set a custom value.
        /// </summary>
        public float HitWidthMultiplier {
            get { return (hit_width_multiplier == -1) ? DefaultHitWidthMultiplier : hit_width_multiplier; }
            set { hit_width_multiplier = MathUtil.Clamp(value, 0.001f, float.MaxValue); }
        }
        float hit_width_multiplier = -1;


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

        public override fGameObject RootGameObject {
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

        virtual protected void Create_internal(fMaterial useMaterial)
        {
            // this is for children to subclass
        }

        public virtual PolyCurveSO Create(SOMaterial defaultMaterial)
        {
            if (curve == null) {
                LineGenerator gen = new LineGenerator() {
                    Start = Vector3f.Zero, End = 10.0f * Vector3f.AxisY, StepSize = 0.1f
                };
                gen.Generate();
                curve = new DCurve3();
                gen.Make(curve);
            }

            // assumes type identifier is something like BlahBlahSO
            root = GameObjectFactory.CreateParentGO(UniqueNames.GetNext( Type.identifier.Remove(Type.identifier.Length-2) ));

            if (EnableLineRenderer) {
                LineRenderer ren = root.AddComponent<LineRenderer>();
                ren.startWidth = ren.endWidth = 0.05f;
                ren.useWorldSpace = false;
            }

            AssignSOMaterial(defaultMaterial);       // need to do this to setup BaseSO material stack
            fMaterial useMaterial = CurrentMaterial;
            Create_internal(useMaterial);

            UpdateGeometry();

            increment_timestamp();

            return this;
        }


        override protected void set_material_internal(fMaterial m)
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
                    int Nc = curve.VertexCount;
                    int N = Nc;
                    Vector3[] vec = new Vector3[N];
                    for ( int i = 0; i < N; ++i )
                        vec[i] = (Vector3)curve[ i % Nc ];
                    ren.positionCount = N;
                    ren.SetPositions(vec);
                    ren.loop = (curve.Closed);
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


        override public bool FindRayIntersection(Ray3f worldRay, out SORayHit hit)
        {
            hit = null;

            // project world ray into local coords
            FScene scene = GetScene();
            Ray3f sceneRay = scene.ToSceneRay(worldRay);
            Ray3f localRay = SceneTransforms.SceneToObject(this, sceneRay);

            // also need width in local coords
            float sceneWidth = scene.ToSceneDimension(visibleWidth);
            float localWidth = SceneTransforms.SceneToObject(this, sceneWidth) * HitWidthMultiplier;

            // bounding-box hit test (would be nice to do w/o object allocation...)
            AxisAlignedBox3d hitBox = localBounds;
            hitBox.Expand(localWidth);
            IntrRay3AxisAlignedBox3 box_test = new IntrRay3AxisAlignedBox3(localRay, hitBox);
            if (box_test.Find() == false)
                return false;

            // raycast against curve (todo: spatial data structure for this? like 2D polycurve bbox tree?)
            double rayHitT;
            if (CurveUtils.FindClosestRayIntersection(curve, localWidth, localRay, out rayHitT)) {
                hit = new SORayHit();
                // transform local hit point back into world coords
                Vector3f rayPos = localRay.PointAt((float)rayHitT);
                Vector3f scenePos = SceneTransforms.ObjectToSceneP(this, rayPos);
                hit.hitPos = SceneTransforms.SceneToWorldP(scene, scenePos);
                hit.fHitDist = worldRay.Project(hit.hitPos);
                hit.hitNormal = Vector3f.Zero;
                hit.hitGO = root;
                hit.hitSO = this;
                return true;
            }
            return false;
        }




    }
}
