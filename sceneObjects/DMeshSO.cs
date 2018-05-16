using System;
using System.Collections.Generic;
using g3;


namespace f3
{

    public delegate void DMeshChangedEventHandler(DMeshSO so);


    public enum GeometryEditTypes
    {
        ArbitraryEdit,
        VertexDeformation
    }


    /// <summary>
    /// SO wrapper around a DMesh3.
    /// 
    /// 
    /// </summary>
    public class DMeshSO : BaseSO, SpatialQueryableSO
    {
        protected fGameObject parentGO;


        DMesh3 mesh;
        object mesh_write_lock = new object();     // in some cases we would like to directly access internal Mesh from
                                                   // a background thread, to avoid making mesh copies. Functions that
                                                   // internally modify .mesh will lock this first.

        IViewMeshManager viewMeshes;

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

            //viewMeshes = new LinearDecompViewMeshManager(this);
            viewMeshes = new TrivialViewMeshManager(this);

            on_mesh_changed();
            viewMeshes.ValidateViewMeshes();

            return this;
        }



        // To reduce memory usage, when we disconnect a DMeshSO we can
        // discard temporary data structures. Also, when we destroy it,
        // it doesn't necessarily get GC'd right away (and if we make mistakes,
        // maybe never!). But we can manually free the mesh.
        override public void Connect(bool bRestore)
        {
            if ( bRestore ) {
                viewMeshes.ValidateViewMeshes();
            }
        }
        override public void Disconnect(bool bDestroying)
        {
            this.spatial = null;
            viewMeshes.InvalidateViewMeshes();
            if ( bDestroying ) {
                this.mesh = null;
            }
        }




        /// <summary>
        /// Event will be called whenever our internal mesh changes
        /// </summary>
        public event DMeshChangedEventHandler OnMeshModified;



        /// <summary>
        /// Direct access to internal mesh. Safe for reading. **DO NOT** modify this mesh
        /// unless you are very careful. Better alternatives: ReplaceMesh(), EditAndUpdateMesh().
        /// If those are not sufficient, please use AcquireDangerousMeshLockForEditing()
        /// </summary>
        public DMesh3 Mesh {
            get { return mesh; }
        }


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




        /// <summary>
        /// This function returns an object that holds a lock on the .Mesh for writing.
        /// You should only call like this:
        /// 
        /// using (var danger = meshSO.AcquireDangerousMeshLockForEditing(editType)) {
        ///      ...do your thing to meshSO.Mesh
        /// }
        /// 
        /// This will safely lock the mesh so that background mesh-read threads are blocked.
        /// ***DO NOT*** hold onto this lock, or you will never be able to update the mesh again!
        /// </summary>
        DangerousExternalLock AcquireDangerousMeshLockForEditing(GeometryEditTypes editType)
        {
            return DangerousExternalLock.Lock(mesh_write_lock, 
                () => { notify_mesh_edited(editType); } );
        }


        /// <summary>
        /// tell DMeshSO that you have modified .Mesh
        /// </summary>
        [System.Obsolete("This should no longer be used. Use EditAndUpdateMesh() or AcquireDangerousMeshLockForEditing()")]
        public void NotifyMeshEdited(bool bVertexDeformation = false)
        {
            notify_mesh_edited(bVertexDeformation ?
                GeometryEditTypes.VertexDeformation : GeometryEditTypes.ArbitraryEdit);
        }


        /// <summary>
        /// Change mesh under lock. This safely allows mesh edits to happen in concert
        /// with background threads reading from the mesh. 
        /// 
        /// Currently it is only safe to call this from the main thread!
        /// </summary>
        public void EditAndUpdateMesh(Action<DMesh3> EditF, GeometryEditTypes editType)
        {
            lock (mesh_write_lock) {
                EditF(mesh);
            }
            notify_mesh_edited(editType);
        }



        /// <summary>
        /// replace .Mesh with newMesh. By default, DMeshSO now "owns" this mesh.
        /// Will make copy instead if you pass bTakeOwnership=false 
        /// </summary>
        public void ReplaceMesh(DMesh3 newMesh, bool bTakeOwnership = true)
        {
            lock (mesh_write_lock) {
                if (bTakeOwnership)
                    this.mesh = newMesh;
                else
                    this.mesh = new DMesh3(newMesh);
            }

            on_mesh_changed();
            viewMeshes.ValidateViewMeshes();
            post_mesh_modified();
        }

        /// <summary>
        /// replace vertex positions of internal .Mesh
        /// [TODO] This function is probably unnecessary...use NotifyMeshEdited(true) instead
        /// </summary>
        public void UpdateVertexPositions(Vector3f[] vPositions) {
            if (vPositions.Length < mesh.MaxVertexID)
                throw new Exception("DMeshSO.UpdateVertexPositions: not enough positions provided!");

            lock (mesh_write_lock) {
                foreach (int vid in mesh.VertexIndices())
                    mesh.SetVertex(vid, vPositions[vid]);
            }

            fast_mesh_update(false, false);
            post_mesh_modified();
        }

        /// <summary>
        /// Double version of UpdateVertexPositions
        /// </summary>
        public void UpdateVertexPositions(Vector3d[] vPositions) {
            if (vPositions.Length < mesh.MaxVertexID)
                throw new Exception("DMeshSO.UpdateVertexPositions: not enough positions provided!");

            lock (mesh_write_lock) {
                foreach (int vid in mesh.VertexIndices())
                    mesh.SetVertex(vid, vPositions[vid]);
            }

            fast_mesh_update(false, false);
            post_mesh_modified();
        }


        /// <summary>
        /// copy vertex positions from sourceMesh.
        /// [TODO] perhaps can refactor into a call to EditAndUpdateMesh() ?
        /// </summary>
        public void UpdateVertices(DMesh3 sourceMesh, bool bNormals = true, bool bColors = true)
        {
            if (sourceMesh.MaxVertexID != mesh.MaxVertexID)
                throw new Exception("DMeshSO.UpdateVertexPositions: not enough positions provided!");

            bNormals &= sourceMesh.HasVertexNormals;
            if (bNormals && mesh.HasVertexNormals == false)
                mesh.EnableVertexNormals(Vector3f.AxisY);
            bColors &= sourceMesh.HasVertexColors;
            if (bColors && mesh.HasVertexColors == false)
                mesh.EnableVertexColors(Colorf.White);

            lock (mesh_write_lock) {
                foreach (int vid in mesh.VertexIndices()) {
                    Vector3d sourceV = sourceMesh.GetVertex(vid);
                    mesh.SetVertex(vid, sourceV);
                    if (bNormals) {
                        Vector3f sourceN = sourceMesh.GetVertexNormal(vid);
                        mesh.SetVertexNormal(vid, sourceN);
                    }
                    if ( bColors ) {
                        Vector3f sourceC = sourceMesh.GetVertexColor(vid);
                        mesh.SetVertexColor(vid, sourceC);
                    }
                }
            }

            fast_mesh_update(bNormals, bColors);
            post_mesh_modified();
        }



        // fast update of view meshes for vertex deformations/changes
        void fast_mesh_update(bool bNormals, bool bColors) {
            viewMeshes.FastUpdateVertices(bNormals, bColors);
            on_mesh_changed(true, false);
            viewMeshes.ValidateViewMeshes();
        }


        /// <summary>
        /// Run ReadMeshF() and return result. This function write-locks the mesh
        /// around the call to ReadMeshF, so you can call this from a background thread.
        /// If ReadMeshF throws an Exception, the Exception is returned.
        /// </summary>
        public object SafeMeshRead(Func<DMesh3, object> ReadMeshF)
        {
            object result = null;
            lock (mesh_write_lock) {
                try {
                    result = ReadMeshF(mesh);
                } catch (Exception e) {
                    result = e;
                }
            }
            return result;
        }





        //
        // internals
        // 

        public void notify_mesh_edited(GeometryEditTypes editType)
        {
            if (editType == GeometryEditTypes.VertexDeformation) {
                fast_mesh_update(true, true);
            } else {
                on_mesh_changed();
                viewMeshes.ValidateViewMeshes();
            }
            post_mesh_modified();
        }

        void on_mesh_changed(bool bInvalidateSpatial = true, bool bInvalidateDecomp = true)
        {
            if (bInvalidateSpatial) 
                spatial = null;

            // discard existing mesh GOs
            if (bInvalidateDecomp) {
                viewMeshes.InvalidateViewMeshes();
            }
        }

        void validate_spatial()
        {
            if ( enable_spatial && spatial == null ) {
                spatial = new DMeshAABBTree3(mesh);
                spatial.Build();
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
            return (AxisAlignedBox3f)mesh.CachedBounds;
        }

        public override bool ShadowsEnabled {
            get { return enable_shadows; }
        }
        override public void DisableShadows() {
            enable_shadows = false;
            MaterialUtil.DisableShadows(parentGO, true, true, true);
        }
        public virtual void SetShadowsEnabled(bool enabled)
        {
            if ( enabled != enable_shadows ) {
                if ( enabled ) {
                    enable_shadows = true;
                    MaterialUtil.EnableShadows(parentGO, true, true, true);
                } else {
                    enable_shadows = false;
                    MaterialUtil.DisableShadows(parentGO, true, true, true);
                }
            }
        }

        override public void SetLayer(int nLayer) {
            parentGO.SetLayer(nLayer);
            base.SetLayer(nLayer);
        }


        /// <summary>
        /// Set the position of the object frame for this SO without moving the mesh in the scene.
        /// The input frame is the new object frame. So, this is a frame "in scene coordinates" unless
        /// this object has a parent SceneObject (ie is not a child of Scene directly). In that case
        /// the frame needs to be specified relative to that SO. An easy way to do this is to via
        ///   obj_pivot = GetLocalFrame(Object).FromFrame( SceneTransforms.SceneToObject(pivot_in_scene) )
        /// TODO: specify frame coordpace as input argument?
        /// </summary>
        public void RepositionPivot(Frame3f objFrame)
        {
            //if (Parent is FScene == false)
            //    throw new NotSupportedException("DMeshSO.RepositionMeshFrame: have not tested this!");

            Frame3f curFrame = this.GetLocalFrame(CoordSpace.ObjectCoords);
            bool bNormals = mesh.HasVertexNormals;

            // map vertices to new frame
            foreach (int vid in mesh.VertexIndices()) {
                Vector3f v = (Vector3f)mesh.GetVertex(vid);
                v = curFrame.FromFrameP(ref v); 
                v = objFrame.ToFrameP(ref v);
                mesh.SetVertex(vid, v);

                if ( bNormals ) {
                    Vector3f n = mesh.GetVertexNormal(vid);
                    n = curFrame.FromFrameV(ref n);
                    n = objFrame.ToFrameV(ref n);
                    mesh.SetVertexNormal(vid, n);
                }
            }

            // set new object frame
            SetLocalFrame(objFrame, CoordSpace.ObjectCoords);

            fast_mesh_update(bNormals, false);
            post_mesh_modified();
        }




        /// <summary>
        /// Find intersection of *WORLD* ray with Mesh
        /// </summary>
        override public bool FindRayIntersection(Ray3f rayW, out SORayHit hit)
        {
            hit = null;
            if (enable_spatial == false)
                return false;

            if (spatial == null) {
                spatial = new DMeshAABBTree3(mesh);
                spatial.Build();
            }

            // convert ray to local
            Frame3f f = new Frame3f(rayW.Origin, rayW.Direction);
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
                hit.hitIndex = hit_tid;
                hit.fHitDist = hit.hitPos.Distance(rayW.Origin);    // simpler than transforming!
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
