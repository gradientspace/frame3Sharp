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

        public bool WriteNormals = false;
        public bool WriteUVs = false;
        public bool WriteVertexColors = false;
        public bool WriteFaceGroups = false;

        public WriteOptions Options = WriteOptions.Defaults;




        public ExportStatus Export(FScene s, string filename)
        {
            List<WriteMesh> vMeshes = new List<WriteMesh>();

            if (WriteFaceGroups)
                throw new Exception("SceneMeshExporter.Export: writing face groups has not yet been implemented!");

            foreach ( SceneObject so in s.SceneObjects ) {
                if (so.IsTemporary)
                    continue;

                SimpleMesh m = new SimpleMesh();
                m.Initialize(WriteNormals, WriteVertexColors, WriteUVs, WriteFaceGroups);
                int groupCounter = 1;

                GameObject go = so.RootGameObject;
                
                int[] vertexMap = new int[2048];
                foreach ( GameObject o in go.Children() ) { 
                    MeshFilter filter = o.GetComponent<MeshFilter>();
                    if ( filter != null && filter.mesh != null ) {
                        Mesh curMesh = filter.sharedMesh;
                        Vector3[] vertices = curMesh.vertices;
                        Vector3[] normals = (WriteNormals) ? curMesh.normals : null;
                        Color[] colors = (WriteVertexColors) ? curMesh.colors : null;
                        Vector2[] uvs = (WriteUVs) ? curMesh.uv : null;

                        if (vertexMap.Length < curMesh.vertexCount)
                            vertexMap = new int[curMesh.vertexCount*2];

                        for ( int i = 0; i < curMesh.vertexCount; ++i ) {
                            NewVertexInfo vi = new NewVertexInfo();
                            vi.bHaveN = WriteNormals; vi.bHaveC = WriteVertexColors; vi.bHaveUV = WriteUVs;

                            Vector3 v = vertices[i];
                            // local to world
                            v = filter.gameObject.transform.TransformPoint(v);
                            // world back to scene
                            vi.v = UnityUtil.SwapLeftRight(s.RootGameObject.transform.InverseTransformPoint(v));

                            if (WriteNormals) {
                                Vector3 n = normals[i];
                                n = filter.gameObject.transform.TransformDirection(n);
                                vi.n = UnityUtil.SwapLeftRight(s.RootGameObject.transform.InverseTransformDirection(n));
                            }
                            if ( WriteVertexColors ) 
                                vi.c = colors[i];
                            if (WriteUVs)
                                vi.uv = uvs[i];

                            vertexMap[i] = m.AppendVertex(vi);
                        }

                        int[] triangles = curMesh.triangles;
                        int nTriangles = triangles.Length / 3;
                        for ( int i = 0; i < nTriangles; ++i ) {
                            int a = vertexMap[triangles[3 * i]];
                            int b = vertexMap[triangles[3 * i + 1]];
                            int c = vertexMap[triangles[3 * i + 2]];
                            m.AppendTriangle(a, c, b, groupCounter);  // TRI ORIENTATION IS REVERSED HERE!!
                        }
                        groupCounter++;
                    }
                }

                vMeshes.Add( new WriteMesh(m, so.Name) );
            }


            if (WriteInBackgroundThreads) {
                BackgroundWriteThread t = new BackgroundWriteThread() {
                    Meshes = vMeshes, options = Options, Filename = filename
                };
                t.Start();
                return new ExportStatus() {
                    Exporter = this, IsComputing = true
                };

            } else {
                IOWriteResult result = StandardMeshWriter.WriteFile(filename, vMeshes, Options);
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
