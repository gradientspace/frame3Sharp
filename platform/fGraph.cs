using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    /// <summary>
    /// fGraph is a node/edge graph.
    /// Unity can represent this using a Mesh in MeshTopology.Lines mode
    /// </summary>
    public class fGraph
    {
        Mesh graph;

        public fGraph(Mesh m)
        {
            if (m.GetTopology(0) != MeshTopology.Lines)
                throw new Exception("fGraph constructor: input mesh does not have lines topology!");
            graph = m;
        }

        public fGraph(DCurve3 curve, bool bInitializeFrames, IList<Colorf> colors = null, IList<float> sizes = null )
        {
            graph = new Mesh();

            int NV = curve.VertexCount;

            Vector3[] verts = new Vector3[NV];
            int[] indices = new int[NV * 2];
            for (int i = 0; i < curve.VertexCount; ++i) {
                indices[2 * i] = i;
                indices[2 * i + 1] = (i + 1) % NV;
                verts[i] = (Vector3)curve[i];
            }
            graph.vertices = verts;

            if (bInitializeFrames) {
                Vector3[] normals = new Vector3[NV];
                Vector4[] tangents = new Vector4[NV];

                Frame3f vf = new Frame3f(curve[0], curve.Tangent(0));
                for (int i = 0; i < curve.VertexCount; ++i) {
                    Vector3d tan = curve.Tangent(i);
                    vf.AlignAxis(2, (Vector3f)tan);
                    normals[i] = vf.X;
                    float s = (sizes == null) ? 1.0f : sizes[i];
                    tangents[i] = new Vector4(vf.Y.x, vf.Y.y, vf.Y.z, s);
                }

                graph.normals = normals;
                graph.tangents = tangents;
            }

            if ( colors != null ) { 
                Color[] ucolors = new Color[NV];
                for (int i = 0; i < curve.VertexCount; ++i)
                    ucolors[i] = colors[i];
                graph.colors = ucolors;
            }

            graph.SetIndices(indices, MeshTopology.Lines, 0);
        }

        public void RecalculateBounds() {
            graph.RecalculateBounds();
        }

        public fMesh Clone()
        {
            return new fMesh(Mesh.Instantiate(graph));
        }

        public static implicit operator Mesh(fGraph graph)
        {
            return graph.graph;
        }
    }





}
