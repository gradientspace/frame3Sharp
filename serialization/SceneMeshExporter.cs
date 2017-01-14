using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using UnityEngine;
using g3;

namespace f3
{

    // [TODO] we want to support writing exports in background thread, w/o blocking UI.
    // To do this we need to hold on to the writer objects, so we return ExportStatus object
    // from Export. It should have query functions/etc, and maybe events, to indicate
    // that writes are complete, have failed, etc, etc.

    public class ExportStatus
    {
        public SceneMeshExporter Exporter;
        public bool IsComputing;

        public bool Ok;
        public bool Error { get { return Ok == false; } }
        public string LastErrorMessage;
    }


    public class SceneMeshExporter
    {

        public g3.IOCode LastWriteStatus { get; set; }
        public string LastErrorMessage { get; set; }


        public bool WriteInBackgroundThreads = true;


        public ExportStatus Export(FScene s, string filename)
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
                        Vector3[] vertices = curMesh.vertices;
                        if (vertexMap.Length < curMesh.vertexCount)
                            vertexMap = new int[curMesh.vertexCount*2];

                        for ( int i = 0; i < curMesh.vertexCount; ++i ) {
                            Vector3 v = vertices[i];
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

            WriteOptions options = new WriteOptions();
            options.bCombineMeshes = false;

            if (WriteInBackgroundThreads) {
                BackgroundWriteThread t = new BackgroundWriteThread() {
                    Meshes = vMeshes, options = options, Filename = filename
                };
                t.Start();
                return new ExportStatus() {
                    Exporter = this, IsComputing = true
                };

            } else {
                IOWriteResult result = StandardMeshWriter.WriteFile(filename, vMeshes, options);
                LastWriteStatus = result.code;
                LastErrorMessage = result.message;
                return new ExportStatus() {
                    Exporter = this, IsComputing = false,
                    Ok = (result.code == IOCode.Ok),
                    LastErrorMessage = result.message
                };
            }
        }


    }



    class BackgroundWriteThread
    {
        public List<WriteMesh> Meshes;
        public string Filename;
        public WriteOptions options;


        public IOWriteResult Status { get; set; }


        public void Start()
        {
            Thread t = new Thread(ThreadFunc);
            t.Start();
        }

        void ThreadFunc()
        {
            Status = StandardMeshWriter.WriteFile(Filename, Meshes, options);
        }
    }


}
