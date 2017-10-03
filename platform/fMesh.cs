using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    /// <summary>
    /// Wrapper around unity Mesh, that provides IMesh interface, other useful stuff
    /// </summary>
    public class fMesh
    {
        Mesh mesh;


        public fMesh(UnityEngine.Mesh m)
        {
            mesh = m;
        }


        public fMesh(DMesh3 source)
        {
            mesh = UnityUtil.DMeshToUnityMesh(source, false);
        }
        public fMesh(SimpleMesh source)
        {
            mesh = UnityUtil.SimpleMeshToUnityMesh(source, false);
        }

        public fMesh(int[] triangles, DMesh3 source, int[] source_vertices, bool bCopyNormals = false, bool bCopyColors = false, bool bCopyUVs = false)
        {
            int NV = source_vertices.Length;
            Vector3[] vertices = new Vector3[NV];
            for ( int i = 0; i < NV; ++i ) {
                vertices[i] = (Vector3)source.GetVertex(source_vertices[i]);
            }

            Mesh m = new Mesh();
            m.vertices = vertices;
            m.triangles = triangles;

            if (bCopyNormals && source.HasVertexNormals) {
                Vector3[] normals = new Vector3[NV];
                for (int i = 0; i < NV; ++i)
                    normals[i] =(Vector3)source.GetVertexNormal(source_vertices[i]);
                m.normals = normals;
            } else {
                m.RecalculateNormals();
            }

            if ( bCopyColors && source.HasVertexColors ) {
                Color[] colors = new Color[NV];
                for ( int i = 0; i < NV; ++i )
                    colors[i] = (Color)source.GetVertexColor(source_vertices[i]);
                m.colors = colors;
            }

            if ( bCopyUVs && source.HasVertexUVs ) {
                Vector2[] uvs = new Vector2[NV];
                for ( int i = 0; i < NV; ++i )
                    uvs[i] = source.GetVertexUV(source_vertices[i]);
                m.uv = uvs;
            }

            mesh = m;
        }


        public void FastUpdateVertices(DMesh3 source, int[] source_vertices, bool bCopyNormals = false, bool bCopyColors = false)
        {
            int NV = source_vertices.Length;
            Vector3[] vertices = new Vector3[NV];
            for ( int i = 0; i < NV; ++i ) {
                vertices[i] = (Vector3)source.GetVertex(source_vertices[i]);
            }

            mesh.vertices = vertices;

            if (bCopyNormals && source.HasVertexNormals) {
                Vector3[] normals = new Vector3[NV];
                for (int i = 0; i < NV; ++i)
                    normals[i] =(Vector3)source.GetVertexNormal(source_vertices[i]);
                mesh.normals = normals;
            }

            if ( bCopyColors && source.HasVertexColors ) {
                Color[] colors = new Color[NV];
                for ( int i = 0; i < NV; ++i )
                    colors[i] = (Color)source.GetVertexColor(source_vertices[i]);
                mesh.colors = colors;
            }
        }


        public void RecalculateNormals() {
            mesh.RecalculateNormals();
        }
        public void RecalculateBounds() {
            mesh.RecalculateBounds();
        }


        public fMesh Clone()
        {
            return new fMesh(Mesh.Instantiate(mesh));
        }


        public static implicit operator UnityEngine.Mesh(fMesh mesh)
        {
            return mesh.mesh;
        }





        public interface ExternalIMesh : IMesh, IDisposable
        {
        }

        /// <summary>
        /// Returns an IMesh interface to a copy of internal mesh data
        /// </summary>
        public ExternalIMesh CreateCachedIMesh()
        {
            MeshDataCache c = new MeshDataCache();
            c.vertices = mesh.vertices;
            c.normals = mesh.normals;
            if (c.normals.Length == 0)
                c.normals = null;
            c.colors = mesh.colors;
            if (c.colors.Length == 0)
                c.colors = null;
            c.uv = mesh.uv;
            if (c.uv.Length == 0)
                c.uv = null;
            c.triangles = mesh.triangles;
            return c;
        }


        class MeshDataCache : ExternalIMesh
        {
            public Vector3[] vertices;
            public Vector3[] normals;
            public Color[] colors;
            public Vector2[] uv;
            public int[] triangles;

            public int VertexCount { get { return vertices.Length; } }
		    public int MaxVertexID { get { return vertices.Length; } }

            public bool HasVertexNormals { get { return normals != null && normals.Length == vertices.Length; } }
            public bool HasVertexColors { get { return colors != null && colors.Length == vertices.Length; } }

            public Vector3d GetVertex(int i) { return vertices[i]; }
            public Vector3f GetVertexNormal(int i) { return normals[i]; }
            public Vector3f GetVertexColor(int i) { return colors[i]; }

            public bool IsVertex(int vID) { return vID >= 0 && vID < vertices.Length; }

            // iterators allow us to work with gaps in index space
            public IEnumerable<int> VertexIndices() { return new Interval1i(0, vertices.Length - 1); }


            public int TriangleCount { get { return triangles.Length / 3; } }
		    public int MaxTriangleID { get { return triangles.Length / 3; } }

            public bool HasVertexUVs { get { return uv != null && uv.Length == vertices.Length; } }
            public Vector2f GetVertexUV(int i) { return uv[i]; }

            public NewVertexInfo GetVertexAll(int i) {
                return new NewVertexInfo(GetVertex(i),
                    (HasVertexNormals) ? GetVertexNormal(i) : Vector3f.AxisY,
                    (HasVertexColors) ? GetVertexColor(i) : Vector3f.One,
                    (HasVertexUVs) ? GetVertexUV(i) : Vector2f.Zero);
            }

            public bool HasTriangleGroups { get { return false; } }

            public Index3i GetTriangle(int i) { return new Index3i(triangles[3 * i], triangles[3 * i + 1], triangles[3 * i + 2]); }
            public int GetTriangleGroup(int i) { return 0; }

            public bool IsTriangle(int tID) { return tID >= 0 && 3*tID < triangles.Length; }

            // iterators allow us to work with gaps in index space
            public IEnumerable<int> TriangleIndices(){ return new Interval1i(0, triangles.Length/3 - 1); }


            public void Dispose()
            {
                vertices = null;
                normals = null;
                colors = null;
                uv = null;
                triangles = null;
            }

        }
       


    }
}
