using System;
using System.Collections.Generic;
using g3;

using UnityEngine;

namespace f3
{


    public class DMeshSO : BaseSO, IMeshComponentManager
    {
        protected fGameObject parentGO;
        protected List<fMeshGameObject> displayGOs;

        DMesh3 mesh;
        DMeshAABBTree3 spatial;
        MeshDecomposition decomp;


		public DMeshSO()
		{
		}

        public virtual DMeshSO Create(DMesh3 mesh, SOMaterial setMaterial)
        {
            AssignSOMaterial(setMaterial);       // need to do this to setup BaseSO material stack
            parentGO = GameObjectFactory.CreateParentGO(UniqueNames.GetNext("DMesh"));

            this.mesh = mesh;

            // always create spatial decomp ?
            spatial = new DMeshAABBTree3(mesh);
            spatial.Build();

            displayGOs = new List<fMeshGameObject>();
            update_decomposition();

            return this;
        }




        void update_decomposition()
        {
            if ( decomp == null ) {
                decomp = new MeshDecomposition(mesh, this);
                decomp.BuildLinear();
            }
        }


        public void AddComponent(MeshDecomposition.Component C)
        {
            fMesh submesh = new fMesh(C.triangles, mesh, C.source_vertices, true, true);
            fMeshGameObject go = GameObjectFactory.CreateMeshGO("component", submesh, false);
            go.SetMaterial( new fMaterial(CurrentMaterial) );
            displayGOs.Add(go);

            AppendNewGO(go, parentGO, false);
        }

        public void ClearAllComponents()
        {
            throw new Exception("argh");
        }



        public void UpdateVertexPositions(Vector3f[] vPositions)
        {
            //UnityUtil.UpdateMeshVertices(meshGO.GetSharedMesh(), vPositions, true);
            // update collider...
        }



        //
        // SceneObject impl
        //
        override public GameObject RootGameObject
        {
            get { return parentGO; }
        }

        override public string Name
        {
            get { return parentGO.GetName(); }
            set { parentGO.SetName(value); }
        }

        override public SOType Type { get { return SOTypes.DMesh; } }

        public override bool IsSurface {
            get { return true; }
        }

        override public SceneObject Duplicate()
        {
            DMeshSO copy = new DMeshSO();
            DMesh3 copyMesh = new DMesh3(mesh);
            copy.Create( copyMesh, this.GetAssignedSOMaterial() );
            copy.SetLocalFrame(
                this.GetLocalFrame(CoordSpace.ObjectCoords), CoordSpace.ObjectCoords);
            copy.SetLocalScale(this.GetLocalScale());
            return copy;
        }

        override public Bounds GetLocalBoundingBox()
        {
            Bounds b = (AxisAlignedBox3f)mesh.CachedBounds;
            Vector3f s = parentGO.GetLocalScale();
            b.extents = new Vector3f(
                            b.extents[0] * s[0],
                            b.extents[1] * s[1],
                            b.extents[2] * s[2]);
            return b;
        }


        override public void DisableShadows() {
            //MaterialUtil.DisableShadows(meshGO, true, false);
        }



        // [RMS] this is not working right now...
        override public bool FindRayIntersection(Ray3f ray, out SORayHit hit)
        {
            if (spatial == null) {
                spatial = new DMeshAABBTree3(mesh);
                spatial.Build();
            }

            Transform xform = ((GameObject)RootGameObject).transform;

            // convert ray to local
            Ray3d local_ray = new Ray3d();
            local_ray.Origin = xform.InverseTransformPoint(ray.Origin);
            local_ray.Direction = xform.InverseTransformDirection(ray.Direction);
            local_ray.Direction.Normalize();

            hit = null;
            int hit_tid = spatial.FindNearestHitTriangle(local_ray);
            if (hit_tid != DMesh3.InvalidID) {
                IntrRay3Triangle3 intr = MeshQueries.TriangleIntersection(mesh, hit_tid, local_ray);

                hit = new SORayHit();
                hit.fHitDist = (float)intr.RayParameter;
                hit.hitPos = xform.TransformPoint((Vector3f)local_ray.PointAt(intr.RayParameter));
                hit.hitNormal = xform.TransformDirection((Vector3f)mesh.GetTriNormal(hit_tid));
                hit.hitGO = RootGameObject;
                hit.hitSO = this;
                return true;
            }
            return false;
        }


    }
}
