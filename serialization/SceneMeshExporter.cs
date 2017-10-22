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

        // If IsComputing == true, then exporter is still working in background
        // threads. It will eventually go false and flags will be set.
        // The computing function /might/ set the progress fields, but no guarantees
        public bool IsComputing;

        public bool Ok;
        public bool Error { get { return Ok == false; } }
        public string LastErrorMessage;

        public int Progress = 0;
        public int MaxProgress = 0;
    }


    public class SceneMeshExporter
    {

        public g3.IOCode LastWriteStatus { get; set; }
        public string LastErrorMessage { get; set; }


        public bool WriteInBackgroundThreads = false;
        public Action<SceneMeshExporter, ExportStatus> BackgroundWriteCompleteF = null;

        public bool WriteNormals = false;
        public bool WriteUVs = false;
        public bool WriteVertexColors = false;
        public bool WriteFaceGroups = false;

        public WriteOptions Options = WriteOptions.Defaults;

        // will be called for each SO. Return false to not include mesh in Export.
        public Func<SceneObject, bool> SOFilterF = null;

        // Will be called for each GO child of an SO that contains a MeshFilter.
        // Return false to not include that mesh in Export
        public Func<SceneObject, fGameObject, bool> GOFilterF = null;


        /// <summary>
        /// This is called for each SO we are going to export, and only fGameObject
        /// elements in the returned list will be included in mesh export. By default
        /// this is anything with a MeshFilter.
        /// *However*, DMeshSO is handled separately because it has an internal DMesh
        /// that should be written, instead of the fGOs it uses for rendering!
        /// </summary>
        protected virtual List<fGameObject> CollectGOChildren(SceneObject so)
        {
            List<fGameObject> vExports = new List<fGameObject>();
            if (so is DMeshSO) {
                // handled separately
            } else { 
                GameObject rootgo = so.RootGameObject;
                foreach (GameObject childgo in rootgo.Children()) {
                    MeshFilter filter = childgo.GetComponent<MeshFilter>();
                    if (filter == null || filter.mesh == null)
                        continue;
                    vExports.Add(childgo);
                }
            }
            return vExports;
        }




        protected virtual DMesh3 GetMeshForDMeshSO(DMeshSO meshSO)
        {
            return new DMesh3(meshSO.Mesh, true);
        }



        public virtual ExportStatus Export(FScene scene, string filename)
        {
            int[] vertexMap = new int[2048];        // temp
            List<WriteMesh> vMeshes = new List<WriteMesh>();

            if (WriteFaceGroups)
                throw new Exception("SceneMeshExporter.Export: writing face groups has not yet been implemented!");

            // extract all the mesh data we want to export
            foreach (SceneObject so in scene.SceneObjects) {
                if (so.IsTemporary || so.IsSurface == false || SceneUtil.IsVisible(so) == false)
                    continue;
                if (SOFilterF != null && SOFilterF(so) == false)
                    continue;

                // if this SO has an internal mesh we can just copy, use it
                if (so is DMeshSO) {
                    DMeshSO meshSO = so as DMeshSO;

                    // todo: flags

                    // make a copy of mesh
                    DMesh3 m = GetMeshForDMeshSO(meshSO);

                    // transform to scene coords and swap left/right
                    foreach (int vid in m.VertexIndices()) {
                        Vector3f v = (Vector3f)m.GetVertex(vid);
                        v = SceneTransforms.ObjectToSceneP(meshSO, v);
                        v = UnityUtil.SwapLeftRight(v);
                        m.SetVertex(vid, v);
                    }
                    m.ReverseOrientation();

                    vMeshes.Add(new WriteMesh(m, so.Name));
                }


                // Look for lower-level fGameObject items to export. By default
                // this is anything with a MeshFilter, override CollectGOChildren
                // or use GOFilterF to add restrictions
                List<fGameObject> vExports = CollectGOChildren(so);
                if (vExports.Count > 0) {
                    SimpleMesh m = new SimpleMesh();
                    m.Initialize(WriteNormals, WriteVertexColors, WriteUVs, WriteFaceGroups);
                    int groupCounter = 1;

                    foreach (fGameObject childgo in vExports) {
                        if (GOFilterF != null && GOFilterF(so, childgo) == false)
                            continue;

                        if (AppendGOMesh(childgo, m, vertexMap, scene, groupCounter))
                            groupCounter++;
                    }

                    vMeshes.Add(new WriteMesh(m, so.Name));
                }


            }


            // ok, we are independent of Scene now and can write in bg thread
            if (WriteInBackgroundThreads) {

                ExportStatus status = new ExportStatus() {
                    Exporter = this, IsComputing = true
                };
                WriteOptions useOptions = Options;
                useOptions.ProgressFunc = (cur, max) => {
                    status.Progress = cur;
                    status.MaxProgress = max;
                };
                BackgroundWriteThread t = new BackgroundWriteThread() {
                    Meshes = vMeshes, options = useOptions, Filename = filename,
                    CompletionF = (result) => {
                        LastWriteStatus = result.code;
                        LastErrorMessage = result.message;
                        status.LastErrorMessage = result.message;
                        status.Ok = (result.code == IOCode.Ok);
                        status.IsComputing = false;
                        if (BackgroundWriteCompleteF != null)
                            BackgroundWriteCompleteF(this, status);
                    }
                };
                t.Start();
                return status;

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




        /// <summary>
        /// If go has a MeshFilter, extract it and append to SimpleMesh. 
        /// Returns false if no filter.
        /// </summary>
        bool AppendGOMesh(GameObject go, SimpleMesh m, int[] vertexMap, FScene scene, int gid)
        {
            MeshFilter filter = go.GetComponent<MeshFilter>();
            if (filter == null || filter.mesh == null)
                return false;

            Mesh curMesh = filter.sharedMesh;
            Vector3[] vertices = curMesh.vertices;
            Vector3[] normals = (WriteNormals) ? curMesh.normals : null;
            Color[] colors = (WriteVertexColors) ? curMesh.colors : null;
            Vector2[] uvs = (WriteUVs) ? curMesh.uv : null;

            if (vertexMap.Length < curMesh.vertexCount)
                vertexMap = new int[curMesh.vertexCount * 2];

            for (int i = 0; i < curMesh.vertexCount; ++i) {
                NewVertexInfo vi = new NewVertexInfo();
                vi.bHaveN = WriteNormals; vi.bHaveC = WriteVertexColors; vi.bHaveUV = WriteUVs;

                Vector3f v = vertices[i];
                // local to world
                v = filter.gameObject.transform.TransformPoint(v);
                // world back to scene
                v = scene.ToSceneP(v);
                vi.v = UnityUtil.SwapLeftRight(v);

                if (WriteNormals) {
                    Vector3 n = normals[i];
                    n = filter.gameObject.transform.TransformDirection(n);  // to world
                    n = scene.ToSceneN(n);  // to scene
                    vi.n = UnityUtil.SwapLeftRight(n);
                }
                if (WriteVertexColors)
                    vi.c = colors[i];
                if (WriteUVs)
                    vi.uv = uvs[i];

                vertexMap[i] = m.AppendVertex(vi);
            }

            int[] triangles = curMesh.triangles;
            int nTriangles = triangles.Length / 3;
            for (int i = 0; i < nTriangles; ++i) {
                int a = vertexMap[triangles[3 * i]];
                int b = vertexMap[triangles[3 * i + 1]];
                int c = vertexMap[triangles[3 * i + 2]];
                m.AppendTriangle(a, c, b, gid);  // TRI ORIENTATION IS REVERSED HERE!!
            }

            return true;
        }



    }



    class BackgroundWriteThread
    {
        public List<WriteMesh> Meshes;
        public string Filename;
        public WriteOptions options;
        public Action<IOWriteResult> CompletionF;

        public IOWriteResult Status { get; set; }

        public void Start()
        {
            Thread t = new Thread(ThreadFunc);
            t.Start();
        }

        void ThreadFunc()
        {
            Status = StandardMeshWriter.WriteFile(Filename, Meshes, options);
            if (CompletionF != null)
                CompletionF(Status);
        }
    }


}
