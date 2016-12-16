using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    public static class UnityExtensions
    {
        public static void SetName(this GameObject go, string name) {
            go.name = name;
        }
        public static string GetName(this GameObject go) {
            return go.name;
        }

        public static void SetLayer(this GameObject go, int layer) {
            go.layer = layer;
        }
        public static int GetLayer(this GameObject go) {
            return go.layer;
        }

        public static bool HasChildren(this GameObject go) {
            return go.transform.childCount > 0;
        }
        public static System.Collections.IEnumerable Children(this GameObject go) {
            for (int k = 0; k < go.transform.childCount; ++k)
                yield return go.transform.GetChild(k).gameObject;
        }


        public static Mesh GetMesh(this GameObject go)
        {
            return go.GetComponent<MeshFilter>().mesh;
        }
        public static Mesh GetSharedMesh(this GameObject go)
        {
            return go.GetComponent<MeshFilter>().sharedMesh;
        }
        public static void SetMesh(this GameObject go, Mesh m) {
            go.GetComponent<MeshFilter>().mesh = m;
        }
        public static void SetSharedMesh(this GameObject go, Mesh m) {
            go.GetComponent<MeshFilter>().sharedMesh = m;
        }


        public static void SetName(this Camera cam, string name) {
            cam.name = name;
        }
        public static string GetName(this Camera cam) {
            return cam.name;
        }

        public static void SetName(this Material mat, string name) {
            mat.name = name;
        }
        public static string GetName(this Material mat) {
            return mat.name;
        }

    }


    // various utility code
    public class UnityUtil
    {

        public static void SafeSendEvent(EventHandler handler, object sender, EventArgs e) {
            var tmp = handler;
            if (tmp != null)
                tmp(sender, e);
        }
        public static void SafeSendEvent(InputEventHandler handler, object sender, InputEvent e) {
            var tmp = handler;
            if (tmp != null)
                tmp(sender, e);
        }


        public static GameObject CreatePrimitiveGO(string name, PrimitiveType eType, Material setMaterial = null, bool bCollider = true)
        {
            var gameObj = GameObject.CreatePrimitive(eType);
            if (bCollider) {
                gameObj.AddComponent(typeof(MeshCollider));
                gameObj.GetComponent<MeshCollider>().enabled = false;
            }
            if ( setMaterial )
                gameObj.GetComponent<MeshRenderer>().material = setMaterial;
            gameObj.SetName(name);
            return gameObj;
        }



        public static GameObject CreateMeshGO(string name, Mesh mesh,
            Material setMaterial = null, bool bCollider = true)
        {
            var gameObj = new GameObject(name);
            gameObj.AddComponent<MeshFilter>();
            gameObj.SetMesh(mesh);
            if (bCollider) {
                gameObj.AddComponent(typeof(MeshCollider));
                gameObj.GetComponent<MeshCollider>().enabled = false;
            }
            if (setMaterial) {
                (gameObj.AddComponent(typeof(MeshRenderer)) as MeshRenderer).material = setMaterial;
            } else {
                (gameObj.AddComponent(typeof(MeshRenderer)) as MeshRenderer).material =
                    MaterialUtil.CreateStandardMaterial(Color.red);
            }
            return gameObj;
        }




        public enum MeshAlignOption
        {
            NoAlignment,
            AllAxesCentered,
            XZCenteredOnY
        };
        public static GameObject CreateMeshGO(string name, string meshpath, 
            float fTargetSize = -1.0f, 
            MeshAlignOption alignment = MeshAlignOption.NoAlignment, 
            Material setMaterial = null, bool bCollider = true)
        {
            Mesh mesh = Resources.Load<Mesh>(meshpath);
            if (mesh == null) {
                Debug.Log("[UnityUtil.CreateMeshGO] not found at path " + meshpath);
            }
            var gameObj = new GameObject(name);
            gameObj.AddComponent<MeshFilter>();
            gameObj.SetMesh(mesh);
            if (bCollider) {
                gameObj.AddComponent(typeof(MeshCollider));
                gameObj.GetComponent<MeshCollider>().enabled = false;
            }
            if (setMaterial) {
                (gameObj.AddComponent(typeof(MeshRenderer)) as MeshRenderer).material = setMaterial;
            }

            // doesn't fucking work aaaaagghhh
            if (fTargetSize > 0) {
                Bounds b = mesh.bounds;
                Vector3 diag = b.max - b.min;
                float fScale = fTargetSize / Math.Max(diag[0], Mathf.Max(diag[1], diag[2]));
                gameObj.transform.localScale = new Vector3(fScale, fScale, fScale);
            }
            if (alignment != MeshAlignOption.NoAlignment) {
                Bounds b = gameObj.GetComponent<MeshRenderer>().bounds;
                Vector3 c = (b.max + b.min) * 0.5f;
                if (alignment == MeshAlignOption.AllAxesCentered)
                    gameObj.transform.Translate(-c[0] * 0.5f, -c[1] * 0.5f, -c[2] * 0.5f);
                else
                    gameObj.transform.Translate(-c[0] * 0.5f, -b.min[0], -c[2] * 0.5f);
            }
            return gameObj;
        }


        public static Frame3f GetGameObjectFrame(GameObject go, CoordSpace eSpace)
        {
            if (eSpace == CoordSpace.WorldCoords)
                return new Frame3f(go.transform.position, go.transform.rotation);
            else if (eSpace == CoordSpace.ObjectCoords)
                return new Frame3f(go.transform.localPosition, go.transform.localRotation);
            else
                throw new ArgumentException("not possible without refernce to scene!");
        }
        public static void SetGameObjectFrame(GameObject go, Frame3f newFrame, CoordSpace eSpace)
        {
            if (eSpace == CoordSpace.WorldCoords) {
                go.transform.position = newFrame.Origin;
                go.transform.rotation = newFrame.Rotation;
            } else if (eSpace == CoordSpace.ObjectCoords) {
                go.transform.localPosition = newFrame.Origin;
                go.transform.localRotation = newFrame.Rotation;
            } else {
                // [RMS] cannot do this w/o handle to scene...
                Debug.Log("[MathUtil.SetGameObjectFrame] unsupported!\n");
                throw new ArgumentException("not possible without refernce to scene!");
            }
        }


        // translate origin of GO by dx/dy/dz in x/y/z frame axes
        public static void TranslateInFrame(GameObject go, float dx, float dy, float dz, CoordSpace eSpace)
        {
            Frame3f f = GetGameObjectFrame(go, eSpace);
            f.Origin += dx * f.X + dy * f.Y + dz * f.Z;
            SetGameObjectFrame(go, f, eSpace);
        }





        public static Vector3 TransformPointToLocalSpace(Vector3 point, GameObject obj)
        {
            return obj.transform.InverseTransformPoint(point);
        }
        public static Vector3 TransformVectorToLocalSpace(Vector3 vector, GameObject obj)
        {
            return obj.transform.InverseTransformDirection(vector);
        }
        public static Ray TransformRayToLocalSpace(Ray r, GameObject obj)
        {
            return new Ray(
                obj.transform.InverseTransformPoint(r.origin),
                obj.transform.InverseTransformDirection(r.direction));
        }


        // try to collapse the localScale on an object, to get the actual scaling
        public static Vector3 GetFreeLocalScale(GameObject go)
        {
            Transform p = go.transform.parent;
            go.transform.parent = null;
            Vector3 v = go.transform.localScale;
            go.transform.SetParent(p, true);
            return v;
        }




        // rayhit-test a GameObject, handling collider enable/disable
        public static bool FindGORayIntersection(Ray ray, GameObject go, out GameObjectRayHit hit)
        {
            hit = null;
            MeshCollider collider = go.GetComponent<MeshCollider>();
            if (collider == null)
                return false;

            bool bIsEnabled = collider.enabled;
            collider.enabled = true;
            RaycastHit hitInfo;
            if (collider.Raycast(ray, out hitInfo, Mathf.Infinity)) {
                hit = new GameObjectRayHit();
                hit.fHitDist = hitInfo.distance;
                hit.hitPos = hitInfo.point;
                hit.hitNormal = hitInfo.normal;
                hit.hitGO = go;
            }
            collider.enabled = bIsEnabled;

            return (hit != null);
        }



        public static readonly Bounds InvalidBounds = new Bounds(Vector3.zero, new Vector3(-1.31337f, -1.31337f, -1.31337f));

        public static Bounds GetBoundingBox(GameObject go)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) {
                return r.bounds;
            } else if ( go.HasChildren() ) {
                Bounds b = InvalidBounds; int i = 0;
                foreach (GameObject child_go in go.Children()) {
                    if (i++ == 0)
                        b = GetBoundingBox(child_go);
                    else
                        b.Encapsulate(GetBoundingBox(child_go));
                }
                return b;
            } else {
                return new Bounds(go.transform.position, new Vector3(0.001f, 0.001f, 0.001f));
            }
        }


        public static Bounds GetBoundingBox(List<GameObject> objects) {
            if (objects.Count == 0)
                return InvalidBounds;
            Bounds b = GetBoundingBox(objects[0]);
            for (int i = 1; i < objects.Count; ++i)
                b.Encapsulate(GetBoundingBox(objects[i]));
            return b;
        }


        public static bool HasMesh(GameObject go) {
            return go.GetComponent<MeshFilter>() != null;
        }


        // will return InvalidBounds if GO doesn't have a mesh
        public static Bounds GetGeometryBoundingBox(GameObject obj) {
            MeshFilter f = obj.GetComponent<MeshFilter>();
            return (f != null) ? f.mesh.bounds : InvalidBounds;
        }

        // will return InvalidBounds if any element in list doesn't have a mesh
        public static Bounds GetGeometryBoundingBox(List<GameObject> objects)
        {
            if (objects.Count == 0)
                return InvalidBounds;
            Bounds b = GetGeometryBoundingBox(objects[0]);
            foreach ( GameObject go in objects ) 
                b.Encapsulate(GetGeometryBoundingBox(go));
            return b;
        }



        // knows about our magic invalid value
        public static Bounds Combine(Bounds b1, Bounds b2)
        {
            if (b1 == InvalidBounds)
                return b2;
            else if (b2 == InvalidBounds)
                return b1;
            Bounds r = b1;
            r.Encapsulate(b2);
            return r;
        }







        public static void AddChild(GameObject parent, GameObject child, bool bKeepPosition)
        {
            child.transform.SetParent(parent.transform, bKeepPosition);
        }
        public static void AddChildren(GameObject parent, List<GameObject> vChildren, bool bKeepPosition)
        {
            foreach (GameObject child in vChildren)
                child.transform.SetParent(parent.transform, bKeepPosition);
        }

        // recursively extract all children of root GO
        public static void CollectAllChildren(GameObject root, List<GameObject> vChildren)
        {
            foreach ( GameObject go in root.Children() ) { 
                vChildren.Add(go);
                CollectAllChildren(go, vChildren);
            }
        }


        public static void SetLayerRecursive(GameObject root, int nLayer)
        {
            root.SetLayer(nLayer);
            foreach (GameObject go in root.Children()) {
                SetLayerRecursive(go, nLayer);
            }
        }


        public static void Hide(GameObject go)
        {
            if (go.activeSelf == true)
                go.SetActive(false);
        }
        public static void Show(GameObject go)
        {
            if (go.activeSelf == false)
                go.SetActive(true);
        }
        public static void SetVisible(GameObject go, bool bVisible)
        {
            if (bVisible) Show(go); else Hide(go);
        }
        public static bool IsVisible(GameObject go)
        {
            return (go.activeSelf == true);
        }




        public static void ToggleChildMeshColliders(GameObject root, bool bEnable)
        {
            foreach (GameObject go in root.Children()) {
                MeshCollider collider = go.GetComponent<MeshCollider>();
                if (collider)
                    collider.enabled = bEnable;
                ToggleChildMeshColliders(go, bEnable);
            }
        }


        public static void DestroyAllGameObjects(GameObject root, float fDelay = 0.0f)
        {
            foreach (GameObject go in root.Children()) { 
                DestroyAllGameObjects(go);
                go.transform.SetParent(null);
                UnityEngine.Object.Destroy(go, fDelay);
            }
        }



        public static Vector2 EstimateTextMeshDimensions(TextMesh mesh)
        {
            float maxLineWidth = 0;
            float width = 0;
            int nLines = 0;
            foreach (char symbol in mesh.text) {
                CharacterInfo info;
                if ( symbol == '\n') {
                    maxLineWidth = Math.Max(maxLineWidth, width);
                    width = 0;
                    nLines++;
                } else if (mesh.font.GetCharacterInfo(symbol, out info, mesh.fontSize, mesh.fontStyle)) {
                    width += info.advance;
                }
            }
            if (width > 0) {
                maxLineWidth = Math.Max(maxLineWidth, width);
                nLines++;
            }
            if (width == 0) {
                CharacterInfo info;
                mesh.font.GetCharacterInfo(' ', out info, mesh.fontSize, mesh.fontStyle);
                maxLineWidth = info.advance;
                nLines = 1;
            }
            // [RMS] glyph height doesn't account for ascend/descend... 
            //   fontSize seems to be an absolute max ascend-tip to descend-base number
            float height = nLines * mesh.fontSize;
            return new Vector2(maxLineWidth, height) * mesh.characterSize * 0.1f;
        }




        public static void TranslateMesh(Mesh m, float tx, float ty, float tz)
        {
            Vector3[] verts = m.vertices;
            for ( int k = 0; k < verts.Length; ++k ) {
                verts[k][0] += tx;
                verts[k][1] += ty;
                verts[k][2] += tz;
            }
            m.vertices = verts;
        }

        public static void ScaleMesh(Mesh m)
        {
            // TODO
        }


        public static AxisAlignedBox3f BoundsInFrame(Mesh m, Frame3f f)
        {
            AxisAlignedBox3f box = AxisAlignedBox3f.Empty;
            int N = m.vertexCount;
            for (int k = 0; k < N; ++k)
                box.Contain(f.ToFrameP(m.vertices[k]));
            return box;
        }


        public static void ReverseMeshOrientation(Mesh m)
        {
            int[] tris = m.triangles;
            for ( int k = 0; k < tris.Length; k += 3) {
                int tmp = tris[k];
                tris[k] = tris[k + 1];
                tris[k + 1] = tmp;
            }
            m.triangles = tris;
        }




        // [RMS] cached construction of unity primitive meshes
        private static Dictionary<PrimitiveType, Mesh> primitiveMeshes = new Dictionary<PrimitiveType, Mesh>();
        public static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            if (!UnityUtil.primitiveMeshes.ContainsKey(type)) {
                GameObject gameObject = GameObject.CreatePrimitive(type);
                Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                GameObject.Destroy(gameObject);
                UnityUtil.primitiveMeshes[type] = mesh;
            }
            return UnityUtil.primitiveMeshes[type];
        }


        public static UnityEngine.Mesh SimpleMeshToUnityMesh(SimpleMesh m, bool bSwapLeftRight)
        {
            if (m.VertexCount > 65000 || m.TriangleCount > 65000) {
                Debug.Log("[SimpleMeshReader] attempted to import object larger than 65000 verts/tris, not supported by Unity!");
                return null;
            }

            UnityEngine.Mesh unityMesh = new UnityEngine.Mesh();

            Vector3[] vertices = dvector_to_vector3(m.Vertices);
            Vector3[] normals = (m.HasVertexNormals) ? dvector_to_vector3(m.Normals) : null;
            if (bSwapLeftRight) {
                int nV = vertices.Length;
                for (int i = 0; i < nV; ++i) {
                    vertices[i].x = -vertices[i].x;
                    vertices[i].z = -vertices[i].z;
                    if (normals != null) {
                        normals[i].x = -normals[i].x;
                        normals[i].z = -normals[i].z;
                    }
                }
            }

            unityMesh.vertices = vertices;
            if (m.HasVertexNormals)
                unityMesh.normals = normals;
            if (m.HasVertexColors)
                unityMesh.colors = dvector_to_color(m.Colors);
            if (m.HasVertexUVs)
                unityMesh.uv = dvector_to_vector2(m.UVs);
            unityMesh.triangles = m.GetTriangleArray();

            if (m.HasVertexNormals == false)
                unityMesh.RecalculateNormals();

            return unityMesh;
        }


        public static SimpleMesh UnityMeshToSimpleMesh(UnityEngine.Mesh mesh, bool bSwapLeftright)
        {
            SimpleMesh smesh = new SimpleMesh();

            bool bNormals = (mesh.normals.Length == mesh.vertexCount);
            bool bColors = (mesh.colors.Length == mesh.vertexCount || mesh.colors32.Length == mesh.vertexCount);
            bool bByteColors = (mesh.colors32.Length == mesh.vertexCount);
            bool bUVs = (mesh.uv.Length == mesh.vertexCount);

            smesh.Initialize(bNormals, bColors, bUVs, false);

            for ( int i = 0; i < mesh.vertexCount; ++i ) {
                Vector3d v = mesh.vertices[i];
                if ( bSwapLeftright ) {
                    v.x = -v.x;
                    v.z = -v.z;
                }
                NewVertexInfo vInfo = new NewVertexInfo(v);
                if ( bNormals ) {
                    vInfo.bHaveN = true;
                    vInfo.n = mesh.normals[i];
                    if ( bSwapLeftright ) {
                        vInfo.n.x = -vInfo.n.x;
                        vInfo.n.z = -vInfo.n.z;
                    }
                }
                if (bColors) {
                    vInfo.bHaveC = true;
                    if (bByteColors)
                        vInfo.c = new Colorf(mesh.colors32[i].r, mesh.colors32[i].g, mesh.colors32[i].b, 255);
                    else
                        vInfo.c = mesh.colors[i];
                }
                if ( bUVs ) {
                    vInfo.bHaveUV = true;
                    vInfo.uv = mesh.uv[i];
                }

                int vid = smesh.AppendVertex(vInfo);
                if (vid != i)
                    throw new InvalidOperationException("UnityUtil.UnityMeshToSimpleMesh: indices weirdness...");
            }

            for (int i = 0; i < mesh.triangles.Length / 3; ++i)
                smesh.AppendTriangle(mesh.triangles[3 * i], mesh.triangles[3 * i + 1], mesh.triangles[3 * i + 2]);

            return smesh;
        }



        // stupid per-type conversion functions because fucking C# 
        // can't do typecasts in generic functions
        public static Vector3[] dvector_to_vector3(DVector<double> vec)
        {
            int nLen = vec.Length / 3;
            Vector3[] result = new Vector3[nLen];
            for (int i = 0; i < nLen; ++i) {
                result[i].x = (float)vec[3 * i];
                result[i].y = (float)vec[3 * i + 1];
                result[i].z = (float)vec[3 * i + 2];
            }
            return result;
        }
        public static Vector3[] dvector_to_vector3(DVector<float> vec)
        {
            int nLen = vec.Length / 3;
            Vector3[] result = new Vector3[nLen];
            for (int i = 0; i < nLen; ++i) {
                result[i].x = vec[3 * i];
                result[i].y = vec[3 * i + 1];
                result[i].z = vec[3 * i + 2];
            }
            return result;
        }
        public static Vector2[] dvector_to_vector2(DVector<float> vec)
        {
            int nLen = vec.Length / 2;
            Vector2[] result = new Vector2[nLen];
            for (int i = 0; i < nLen; ++i) {
                result[i].x = vec[2 * i];
                result[i].y = vec[2 * i + 1];
            }
            return result;
        }
        public static Color[] dvector_to_color(DVector<float> vec)
        {
            int nLen = vec.Length / 3;
            Color[] result = new Color[nLen];
            for (int i = 0; i < nLen; ++i) {
                result[i].r = vec[3 * i];
                result[i].g = vec[3 * i + 1];
                result[i].b = vec[3 * i + 2];
            }
            return result;
        }








        public static bool InputAxisExists(string sName) {
            try {
                Input.GetAxis(sName);
                return true;
            } catch {
                return false;
            }
        }

        public static bool InputButtonExists(string sName) {
            try {
                Input.GetButton(sName);
                return true;
            } catch {
                return false;
            }
        }


    }
}
