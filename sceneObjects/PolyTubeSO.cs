using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;
using UnityEngine;

namespace f3
{
    public class PolyTubeSO : PolyCurveSO
    {
        GameObject meshGO;

        Polygon2d polygon;
        public Polygon2d Polygon
        {
            get { return polygon; }
            set { polygon = value; on_set_curve(); }
        }

        // [RMS] see notes in PrimitiveSO
        bool defer_rebuild;
        virtual public bool DeferRebuild
        {
            get { return defer_rebuild; }
            set {
                bool rebuild = (defer_rebuild == true && value == false);
                defer_rebuild = value;
                if (rebuild)
                    UpdateGeometry();
            }
        }


        public PolyTubeSO()
        {
            base.EnableLineRenderer = false;
        }

        override public SOType Type { get { return SOTypes.PolyTube; } }


        public override bool IsSurface {
            get { return true; }
        }

        public override SceneObject Duplicate()
        {
            PolyTubeSO copy = new PolyTubeSO();
            copy.Curve = new DCurve3(this.curve);
            copy.Polygon = new Polygon2d(this.polygon);
            copy.Create(this.GetAssignedSOMaterial());
            copy.SetLocalFrame(
                this.GetLocalFrame(CoordSpace.ObjectCoords), CoordSpace.ObjectCoords);
            copy.SetLocalScale(this.GetLocalScale());
            return copy;

        }


        override protected void Create_internal(fMaterial useMaterial)
        {
            if (polygon == null) {
                polygon = Polygon2d.MakeCircle(0.3f, 8);
            }

            // generate mesh tube
            TubeGenerator meshGen = new TubeGenerator() {
                Vertices = new List<Vector3d>(curve.Vertices), Capped = true,
                Polygon = polygon,
                Frame = new Frame3f(Vector3f.Zero, Vector3f.AxisY)
            };

            meshGen.Generate();
            Mesh m = meshGen.MakeUnityMesh(false);

            meshGO = UnityUtil.CreateMeshGO("tube_mesh", m, useMaterial, true);
            AppendNewGO(meshGO, RootGameObject, false);
        }


        override protected void UpdateGeometry_internal()
        {
            // generate mesh tube
            TubeGenerator meshGen = new TubeGenerator() {
                Vertices = new List<Vector3d>(curve.Vertices), Capped = true,
                Polygon = polygon,
                Frame = new Frame3f(Vector3f.Zero, Vector3f.AxisY)
            };
            meshGen.Generate();
            Mesh newMesh = meshGen.MakeUnityMesh(false);
            meshGO.SetMesh(newMesh);

            // apparently this is expensive?
            if (DeferRebuild == false) {
                meshGO.GetComponent<MeshCollider>().sharedMesh = meshGO.GetComponent<MeshFilter>().sharedMesh;
            }

            // expand local bounds
            add_to_bounds( newMesh.bounds.min);
            add_to_bounds( newMesh.bounds.max);
        }


        override public bool FindRayIntersection(Ray3f ray, out SORayHit hit)
        {
            hit = null;

            Frame3f frameW = GetLocalFrame(CoordSpace.WorldCoords);
            Ray localRay = frameW.ToFrame(ray);

            Bounds hitBounds = GetLocalBoundingBox();
            if (hitBounds.IntersectRay(localRay) == false)
                return false;

            GameObjectRayHit goHit;
            if ( UnityUtil.FindGORayIntersection(ray, meshGO, out goHit) ) {
                hit = new SORayHit(goHit, this);
                return true;
            }

            return false;
        }

    }
}
