using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using g3;

namespace f3
{
    public class SceneMeshExporter
    {

        public g3.IOCode LastWriteStatus { get; set; }
        public string LastErrorMessage { get; set; }


        public bool Export(FScene s, string filename)
        {
            List<WriteMesh> vMeshes = new List<WriteMesh>();

            foreach ( SceneObject so in s.SceneObjects ) {
                if (so.IsTemporary)
                    continue;

                SimpleMesh m = new SimpleMesh();
                m.Initialize(false, false, true);
                int groupCounter = 1;

                GameObject go = so.RootGameObject;
                
                int[] vertexMap = new int[2048];
                foreach ( GameObject o in go.Children() ) { 
                    MeshFilter filter = o.GetComponent<MeshFilter>();
                    if ( filter != null && filter.mesh != null ) {
                        Mesh curMesh = filter.sharedMesh;
                        if (vertexMap.Length < curMesh.vertexCount)
                            vertexMap = new int[curMesh.vertexCount*2];

                        for ( int i = 0; i < curMesh.vertexCount; ++i ) {
                            Vector3 v = curMesh.vertices[i];
                            // local to world
                            v = filter.gameObject.transform.TransformPoint(v);
                            // world back to scene
                            v = s.RootGameObject.transform.InverseTransformPoint(v);
                            vertexMap[i] = m.AppendVertex(v.x, v.y, v.z);
                        }

                        m.AppendTriangles(curMesh.triangles, vertexMap, groupCounter++);
                    }
                }

                vMeshes.Add( new WriteMesh(m, so.Name) );
            }

            StreamWriter file = File.CreateText(filename);
            OBJWriter writer = new OBJWriter();
            IOWriteResult result = writer.Write(file, vMeshes, new WriteOptions());
            file.Close();

            LastWriteStatus = result.code;
            LastErrorMessage = result.message;

            return (result.code == IOCode.Ok);
        }

    }


}
