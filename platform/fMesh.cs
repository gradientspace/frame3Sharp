using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    public class fMesh
    {
        Mesh unitymesh;


        public fMesh(UnityEngine.Mesh m)
        {
            unitymesh = m;
        }


        public fMesh(DMesh3 source)
        {
            unitymesh = UnityUtil.DMeshToUnityMesh(source, false);
        }


        public fMesh(int[] triangles, DMesh3 source, int[] source_vertices, bool bCopyNormals = false, bool bCopyColors = false)
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

            unitymesh = m;
        }


        public void FastUpdateVertices(DMesh3 source, int[] source_vertices, bool bCopyNormals = false, bool bCopyColors = false)
        {
            int NV = source_vertices.Length;
            Vector3[] vertices = new Vector3[NV];
            for ( int i = 0; i < NV; ++i ) {
                vertices[i] = (Vector3)source.GetVertex(source_vertices[i]);
            }

            unitymesh.vertices = vertices;

            if (bCopyNormals && source.HasVertexNormals) {
                Vector3[] normals = new Vector3[NV];
                for (int i = 0; i < NV; ++i)
                    normals[i] =(Vector3)source.GetVertexNormal(source_vertices[i]);
                unitymesh.normals = normals;
            }

            if ( bCopyColors && source.HasVertexColors ) {
                Color[] colors = new Color[NV];
                for ( int i = 0; i < NV; ++i )
                    colors[i] = (Color)source.GetVertexColor(source_vertices[i]);
                unitymesh.colors = colors;
            }
        }


        public void RecalculateNormals() {
            unitymesh.RecalculateNormals();
        }
        public void RecalculateBounds() {
            unitymesh.RecalculateBounds();
        }


        public fMesh Clone()
        {
            return new fMesh(Mesh.Instantiate(unitymesh));
        }


        public static implicit operator UnityEngine.Mesh(fMesh mesh)
        {
            return mesh.unitymesh;
        }
    }
}
