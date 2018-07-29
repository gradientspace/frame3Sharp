using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    /// <summary>
    /// DMeshSO needs to convert it's DMesh3 into rendering geometry. 
    /// The ViewMeshManager does this. This may involve multiple render meshes
    /// at the engine level.
    /// </summary>
    public interface IViewMeshManager : IDisposable
    {
        bool AreMeshesValid { get; }

        /// <summary>
        /// Generate new view meshes if they don't exist. Should not
        /// regenerate unless Invalidate() has been called.
        /// </summary>
        void ValidateViewMeshes();

        /// <summary>
        /// discard current view meshes
        /// </summary>
        void InvalidateViewMeshes();

        /// <summary>
        /// only vertex positions/normals/colors have changed, which means we
        /// can do a faster update.
        /// </summary>
        void FastUpdateVertices(bool bNormals, bool bColors);
    }



    /// <summary>
    /// Create a single meshGO for the DMesh3, with no compacting/etc.
    /// Simplest and fastest to do full upates, but cannot partial update,
    /// potentially wastes GPU memory if DMesh3 is sparse.
    /// </summary>
    public class TrivialViewMeshManager : IViewMeshManager
    {
        public DMeshSO SourceSO;

        protected DMesh3 mesh {
            get { return SourceSO.Mesh; }
        }

        fMeshGameObject viewMeshGO;
        bool decomp_valid = false;

        public TrivialViewMeshManager(DMeshSO sourceSO)
        {
            SourceSO = sourceSO;
        }

        public void Dispose()
        {
            InvalidateViewMeshes();
        }

        //
        // AAAHHH should be re-using these GOs !!
        //


        public bool AreMeshesValid { get { return decomp_valid; } }

        public virtual void ValidateViewMeshes()
        {
            if (decomp_valid)
                return;

            fMesh unityMesh = UnityUtil.DMeshToUnityMesh(mesh, false, true);
            viewMeshGO = GameObjectFactory.CreateMeshGO("component", unityMesh, false, true);
            viewMeshGO.SetMaterial(SourceSO.CurrentMaterial, true);
            viewMeshGO.SetLayer(SourceSO.RootGameObject.GetLayer());
            if (SourceSO.ShadowsEnabled == false)
                MaterialUtil.DisableShadows(viewMeshGO, true, true);
            SourceSO.AppendNewGO(viewMeshGO, SourceSO.RootGameObject, false);

            decomp_valid = true;
        }

        public virtual void InvalidateViewMeshes()
        {
            if (viewMeshGO != null) {
                SourceSO.RemoveGO((fGameObject)viewMeshGO);
                viewMeshGO.Destroy();
                viewMeshGO = null;
            }
            decomp_valid = false;
        }

        public virtual void FastUpdateVertices(bool bNormals, bool bColors)
        {
            viewMeshGO.Mesh.FastUpdateVertices(this.mesh, bNormals, bColors);
            if ( bNormals == false )
                viewMeshGO.Mesh.RecalculateNormals();
            viewMeshGO.Mesh.RecalculateBounds();  // otherwise frustum culling bounds are wrong!
        }
    }





    /// <summary>
    /// Simple linear decompposition of DMesh3 into fixed-size mesh blocks. 
    /// No coherence so this is not really helpful for partial updates, was
    /// initially written to work around Unity 16-bit mesh indices limits
    /// </summary>
    public class LinearDecompViewMeshManager : IViewMeshManager, IMeshComponentManager
    {
        public DMeshSO SourceSO;

        public int MaxSubmeshSize = 64000;   // unity uint-mesh limit

        protected DMesh3 mesh {
            get { return SourceSO.Mesh; }
        }

        protected struct DisplayMeshComponent
        {
            public fMeshGameObject go;
            public int[] source_vertices;
        }
        protected List<DisplayMeshComponent> displayComponents;

        MeshDecomposition decomp;
        bool decomp_valid = false;

        public LinearDecompViewMeshManager(DMeshSO sourceSO)
        {
            SourceSO = sourceSO;
            displayComponents = new List<DisplayMeshComponent>();
        }

        public void Dispose()
        {
            InvalidateViewMeshes();
        }

        public bool AreMeshesValid { get { return decomp_valid; } }

        public virtual void ValidateViewMeshes()
        {
            if (decomp_valid)
                return;

            decomp = new MeshDecomposition(mesh, this)
                { MaxComponentSize = this.MaxSubmeshSize };
            decomp.BuildLinear();
            decomp = null;

            decomp_valid = true;
        }

        public virtual void InvalidateViewMeshes()
        {
            ClearAllComponents();
        }

        public virtual void FastUpdateVertices(bool bNormals, bool bColors)
        {
            foreach (var comp in displayComponents) {
                comp.go.Mesh.FastUpdateVertices(this.mesh, comp.source_vertices, bNormals, bColors);
                if (bNormals == false)
                    comp.go.Mesh.RecalculateNormals();
                comp.go.Mesh.RecalculateBounds();  // otherwise frustum culling bounds are wrong!
            }
        }


        #region IMeshComponentManager impl

        public void AddComponent(MeshDecomposition.Component C)
        {
            fMesh submesh = new fMesh(C.triangles, mesh, C.source_vertices, true, true, true);
            fMeshGameObject submesh_go = GameObjectFactory.CreateMeshGO("component", submesh, true);
            submesh_go.SetMaterial(SourceSO.CurrentMaterial, true);
            submesh_go.SetLayer(SourceSO.RootGameObject.GetLayer());
            displayComponents.Add(new DisplayMeshComponent() {
                go = submesh_go, source_vertices = C.source_vertices
            });
            if (SourceSO.ShadowsEnabled == false)
                MaterialUtil.DisableShadows(submesh_go, true, true);
            SourceSO.AppendNewGO(submesh_go, SourceSO.RootGameObject, false);
        }

        public void ClearAllComponents()
        {
            if (displayComponents != null) {
                foreach (DisplayMeshComponent comp in displayComponents) {
                    SourceSO.RemoveGO((fGameObject)comp.go);
                    comp.go.Destroy();
                }
            }
            displayComponents.Clear();

            decomp_valid = false;
        }

        #endregion
    }


}
