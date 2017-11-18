using System;
using System.Collections.Generic;
using g3;


namespace f3
{

    public delegate void DMeshChangedEventHandler(DMeshSO so);


    public class DMeshSO : BaseSO, IMeshComponentManager, SpatialQueryableSO
    {
        protected fGameObject parentGO;

        protected struct DisplayMeshComponent
        {
            public fMeshGameObject go;
            public int[] source_vertices;
        }
        protected List<DisplayMeshComponent> displayComponents;

        DMesh3 mesh;
        MeshDecomposition decomp;


        bool enable_spatial = true;
        DMeshAABBTree3 spatial;

        bool enable_shadows = true;

        public DMeshSO()
        {
        }

        public virtual DMeshSO Create(DMesh3 mesh, SOMaterial setMaterial)
        {
            AssignSOMaterial(setMaterial);       // need to do this to setup BaseSO material stack
            parentGO = GameObjectFactory.CreateParentGO(UniqueNames.GetNext("DMesh"));

            this.mesh = mesh;
            on_mesh_changed();

            displayComponents = new List<DisplayMeshComponent>();
            validate_decomp();

            return this;
        }


        // Currently do not support changing mesh after creation!!
        public DMesh3 Mesh
        {
            get { return mesh; }
        }


        /// <summary>
        /// Event will be called whenever our internal mesh changes
        /// </summary>
        public event DMeshChangedEventHandler OnMeshModified;


        public DMeshAABBTree3 Spatial
        {
            get { validate_spatial(); return spatial; }
        }
        public bool EnableSpatial
        {
            get { return enable_spatial; }
            set { enable_spatial = value; }
        }


        override public void AssignSOMaterial(SOMaterial m)
        {
            base.AssignSOMaterial(m);
        }
        override public void PushOverrideMaterial(fMaterial m)
        {
            base.PushOverrideMaterial(m);
        }
        override public void PopOverrideMaterial()
        {
            base.PopOverrideMaterial();
        }


        public void NotifyMeshEdited(bool bVertexDeformation = false)
        {
            if (bVertexDeformation) {
                fast_mesh_update();
            } else {
                on_mesh_changed();
                validate_decomp();
            }
            post_mesh_modified();
        }


        public void ReplaceMesh(DMesh3 newMesh, bool bTakeOwnership = true)
        {
            if (bTakeOwnership)
                this.mesh = newMesh;
            else
                this.mesh = new DMesh3(newMesh);

            on_mesh_changed();
            validate_decomp();
            post_mesh_modified();
        }

        public void UpdateVertexPositions(Vector3f[] vPositions) {
            if (vPositions.Length < mesh.MaxVertexID)
                throw new Exception("DMeshSO.UpdateVertexPositions: not enough positions provided!");
            foreach (int vid in mesh.VertexIndices())
                mesh.SetVertex(vid, vPositions[vid]);
            fast_mesh_update();
            post_mesh_modified();
        }
        public void UpdateVertexPositions(Vector3d[] vPositions) {
            if (vPositions.Length < mesh.MaxVertexID)
                throw new Exception("DMeshSO.UpdateVertexPositions: not enough positions provided!");
            foreach (int vid in mesh.VertexIndices())
                mesh.SetVertex(vid, vPositions[vid]);
            fast_mesh_update();
            post_mesh_modified();
        }

        // fast update of existing spatial decomp
        void fast_mesh_update() {
            foreach (var comp in displayComponents) {
                comp.go.Mesh.FastUpdateVertices(this.mesh, comp.source_vertices, false, false);
                comp.go.Mesh.RecalculateNormals();
            }
            on_mesh_changed(true, false);
            validate_decomp();
        }


        #region IMeshComponentManager impl

        public void AddComponent(MeshDecomposition.Component C)
        {
            fMesh submesh = new fMesh(C.triangles, mesh, C.source_vertices, true, true, true);
            fMeshGameObject submesh_go = GameObjectFactory.CreateMeshGO("component", submesh, false);
            submesh_go.SetMaterial(this.CurrentMaterial, true);
            submesh_go.SetLayer(parentGO.GetLayer());
            displayComponents.Add(new DisplayMeshComponent() {
                go = submesh_go, source_vertices = C.source_vertices
            });
            if (enable_shadows == false)
                MaterialUtil.DisableShadows(submesh_go, true, true);
            AppendNewGO(submesh_go, parentGO, false);
        }

        public void ClearAllComponents()
        {
            if (displayComponents != null) {
                foreach (DisplayMeshComponent comp in displayComponents) {
                    RemoveGO((fGameObject)comp.go);
                    comp.go.Destroy();
                }
            }
            displayComponents = new List<DisplayMeshComponent>();
        }

        #endregion





        //
        // internals
        // 
        void on_mesh_changed(bool bInvalidateSpatial = true, bool bInvalidateDecomp = true)
        {
            if (bInvalidateSpatial) 
                spatial = null;

            // discard existing mesh GOs
            if (bInvalidateDecomp) {
                ClearAllComponents();
                decomp = null;
            }
        }

        void validate_spatial()
        {
            if ( enable_spatial && spatial == null ) {
                spatial = new DMeshAABBTree3(mesh);
                spatial.Build();
            }
        }

        void validate_decomp()
        {
            if ( decomp == null ) {
                decomp = new MeshDecomposition(mesh, this);
                decomp.BuildLinear();
            }
        }

        void post_mesh_modified()
        {
            var tmp = OnMeshModified;
            if (tmp != null)
                tmp(this);
        }



        //
        // SceneObject impl
        //
        override public fGameObject RootGameObject
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

        /// <summary>
        /// Duplicate this DMeshSO. This will properly instantiate subtypes of DMeshSO,
        /// but will not copy an data members you add...
        /// </summary>
        override public SceneObject Duplicate()
        {
            DMeshSO copy = (DMeshSO)Activator.CreateInstance(this.GetType());
            duplicate_to(copy);
            return copy;
        }

        /// <summary>
        /// explicitly duplicate to any subtype of DMeshSO
        /// </summary>
        virtual public T DuplicateSubtype<T>() where T: DMeshSO, new()
        {
            T copy = new T();
            duplicate_to(copy);
            return copy;
        }

        /// <summary>
        /// called internally by Duplicate() and DuplicateSubtype(), 
        /// override to add things you want to duplicate
        /// </summary>
        protected virtual void duplicate_to(DMeshSO copy) {
            DMesh3 copyMesh = new DMesh3(mesh);
            copy.Create(copyMesh, this.GetAssignedSOMaterial());
            copy.SetLocalFrame(
                this.GetLocalFrame(CoordSpace.ObjectCoords), CoordSpace.ObjectCoords);
            copy.SetLocalScale(this.GetLocalScale());
            copy.enable_shadows = this.enable_shadows;
            copy.enable_spatial = this.enable_spatial;
        }



        // [RMS] this is not a good name...
        override public AxisAlignedBox3f GetLocalBoundingBox()
        {
            AxisAlignedBox3f b = (AxisAlignedBox3f)mesh.CachedBounds;
            Vector3f scale = parentGO.GetLocalScale();
            b.Scale(scale.x, scale.y, scale.z);
            return b;
        }


        override public void DisableShadows() {
            enable_shadows = false;
            MaterialUtil.DisableShadows(parentGO, true, true);
        }

        override public void SetLayer(int nLayer)
        {
            parentGO.SetLayer(nLayer);
            base.SetLayer(nLayer);
        }



        public void RepositionPivot(Frame3f objFrame)
        {
            if (Parent is FScene == false)
                throw new NotSupportedException("DMeshSO.RepositionMeshFrame: have not tested this!");

            Frame3f curFrame = this.GetLocalFrame(CoordSpace.ObjectCoords);
            bool bNormals = mesh.HasVertexNormals;

            // map vertices to new frame
            foreach (int vid in mesh.VertexIndices()) {
                Vector3f v = (Vector3f)mesh.GetVertex(vid);
                v = curFrame.FromFrameP(v);   // 
                v = objFrame.ToFrameP(v);
                mesh.SetVertex(vid, v);

                if ( bNormals ) {
                    Vector3f n = mesh.GetVertexNormal(vid);
                    n = curFrame.FromFrameV(n);
                    n = objFrame.ToFrameV(n);
                    mesh.SetVertexNormal(vid, n);
                }
            }

            // set new object frame
            SetLocalFrame(objFrame, CoordSpace.ObjectCoords);

            fast_mesh_update();
            post_mesh_modified();
        }




        // [RMS] this is not working right now...
        override public bool FindRayIntersection(Ray3f ray, out SORayHit hit)
        {
            hit = null;
            if (enable_spatial == false)
                return false;

            if (spatial == null) {
                spatial = new DMeshAABBTree3(mesh);
                spatial.Build();
            }

            // convert ray to local
            Frame3f f = new Frame3f(ray.Origin, ray.Direction);
            f = SceneTransforms.TransformTo(f, this, CoordSpace.WorldCoords, CoordSpace.ObjectCoords);
            Ray3d local_ray = new Ray3d(f.Origin, f.Z);

            int hit_tid = spatial.FindNearestHitTriangle(local_ray);
            if (hit_tid != DMesh3.InvalidID) {
                IntrRay3Triangle3 intr = MeshQueries.TriangleIntersection(mesh, hit_tid, local_ray);

                Frame3f hitF = new Frame3f(local_ray.PointAt(intr.RayParameter), mesh.GetTriNormal(hit_tid));
                hitF = SceneTransforms.TransformTo(hitF, this, CoordSpace.ObjectCoords, CoordSpace.WorldCoords);

                hit = new SORayHit();
                hit.hitPos = hitF.Origin;
                hit.hitNormal = hitF.Z;
                hit.fHitDist = hit.hitPos.Distance(ray.Origin);    // simpler than transforming!
                hit.hitGO = RootGameObject;
                hit.hitSO = this;
                return true;
            }
            return false;
        }



        // SpatialQueryableSO impl

        public virtual bool SupportsNearestQuery { get { return enable_spatial; } }
        public virtual bool FindNearest(Vector3d point, double maxDist, out SORayHit nearest, CoordSpace eInCoords)
        {
            nearest = null;
            if (enable_spatial == false)
                return false;

            if (spatial == null) {
                spatial = new DMeshAABBTree3(mesh);
                spatial.Build();
            }

            // convert to local
            Vector3f local_pt = SceneTransforms.TransformTo((Vector3f)point, this, eInCoords, CoordSpace.ObjectCoords);

            if (mesh.CachedBounds.Distance(local_pt) > maxDist)
                return false;

            int tid = spatial.FindNearestTriangle(local_pt);
            if (tid != DMesh3.InvalidID) {
                DistPoint3Triangle3 dist = MeshQueries.TriangleDistance(mesh, tid, local_pt);

                nearest = new SORayHit();
                nearest.fHitDist = (float)Math.Sqrt(dist.DistanceSquared);

                Frame3f f_local = new Frame3f(dist.TriangleClosest, mesh.GetTriNormal(tid));
                Frame3f f = SceneTransforms.TransformTo(f_local, this, CoordSpace.ObjectCoords, eInCoords);

                nearest.hitPos = f.Origin;
                nearest.hitNormal = f.Z;
                nearest.hitGO = RootGameObject;
                nearest.hitSO = this;
                return true;
            }
            return false;
        }



    }
}
