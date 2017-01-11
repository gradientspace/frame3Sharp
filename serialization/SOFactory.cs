using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using g3;

namespace f3
{
    // To implement restore of your own SO types:
    //   1) you can subclass SOFactory and implement/extend BuildSOFromType
    //   2) You can register your so Type and associated SOBuildFunc with SORegistry. The
    //      default BuildSOFromType will look this up and use it.
    public class SOFactory : ISceneObjectFactory
    {
        public event SerializeMessageHandler OnMessage;

        string message_prefix;
        virtual protected void emit_message(string s)
        {
            if (OnMessage != null)
                OnMessage(message_prefix + " " + s);
        }

        public virtual SceneObject Build(FScene scene, SceneSerializer serializer, Dictionary<string, object> attributes)
        {
            message_prefix = "[UnitySOFactory.Build]";

            if (attributes.ContainsKey(IOStrings.ASOType) == false) {
                emit_message("SO Type is missing");
                return null;
            }

            string typeIdentifier = attributes[IOStrings.ASOType] as string;
            message_prefix += " " + typeIdentifier;

            SceneObject so = BuildSOFromType(typeIdentifier, scene, serializer, attributes);
            if ( so == null )
                emit_message("builder not implemented!");
            return so;
        }


        protected virtual SceneObject BuildSOFromType(string typeIdentifier, FScene scene, 
            SceneSerializer serializer, Dictionary<string, object> attributes )
        {
            // restore custom type if client has registered a builder for it
            if ( scene.TypeRegistry.ContainsType(typeIdentifier) ) {
                SOBuildFunc buildFunc = scene.TypeRegistry.FindBuilder(typeIdentifier);
                if ( buildFunc != null ) {
                    SceneObject so = buildFunc(this, scene, attributes);
                    if ( so != null )
                        return so;
                }
            }

            if (typeIdentifier == IOStrings.TypeCylinder) {
                return BuildCylinder(scene, attributes);
            } else if (typeIdentifier == IOStrings.TypeBox) {
                return BuildBox(scene, attributes);
            } else if (typeIdentifier == IOStrings.TypeSphere) {
                return BuildSphere(scene, attributes);
            } else if (typeIdentifier == IOStrings.TypePivot) {
                return BuildPivot(scene, attributes);
            } else if (typeIdentifier == IOStrings.TypeMeshReference) {
                return BuildMeshReference(scene, serializer.TargetFilePath, attributes);
            } else if (typeIdentifier == IOStrings.TypePolyCurve) {
                return BuildPolyCurveSO(scene, attributes);
            } else if (typeIdentifier == IOStrings.TypePolyTube) {
                return BuildPolyTubeSO(scene, attributes);
            } else if (typeIdentifier == IOStrings.TypeMesh) {
                return BuildMeshSO(scene, attributes);
            } else {
                return null;
            }
        }




        public virtual bool safe_set_property_f(Dictionary<string, object> attributes, string sPropName, Action<float> setter)
        {
            if (check_key_or_debug_print(attributes, sPropName)) {
                setter((float)attributes[sPropName]);
                return true;
            }
            return false;
        }
        public virtual bool safe_set_property_i(Dictionary<string, object> attributes, string sPropName, Action<int> setter)
        {
            if (check_key_or_debug_print(attributes, sPropName)) {
                setter((int)attributes[sPropName]);
                return true;
            }
            return false;
        }
        public virtual bool safe_set_property_b(Dictionary<string, object> attributes, string sPropName, Action<bool> setter)
        {
            if (check_key_or_debug_print(attributes, sPropName)) {
                setter((bool)attributes[sPropName]);
                return true;
            }
            return false;
        }
        public virtual bool safe_set_property_s(Dictionary<string, object> attributes, string sPropName, Action<string> setter)
        {
            if (check_key_or_debug_print(attributes, sPropName)) {
                setter((string)attributes[sPropName]);
                return true;
            }
            return false;
        }



        public virtual SceneObject BuildCylinder(FScene scene, Dictionary<string, object> attributes)
        {
            CylinderSO so = new CylinderSO();
            safe_set_property_f(attributes, IOStrings.ARadius, (f) => { so.Radius = f; });
            safe_set_property_f(attributes, IOStrings.AHeight, (f) => { so.Height = f; });
            so.Create(scene.DefaultSOMaterial);
            safe_set_property_s(attributes, IOStrings.ASOName, (s) => { so.Name = s; });
            set_transform(so, attributes);
            set_material(so, attributes);
            return so;
        }


        public virtual SceneObject BuildBox(FScene scene, Dictionary<string, object> attributes)
        {
            BoxSO so = new BoxSO();
            safe_set_property_f(attributes, IOStrings.AWidth, (f) => { so.Width = f; });
            safe_set_property_f(attributes, IOStrings.AHeight, (f) => { so.Height = f; });
            safe_set_property_f(attributes, IOStrings.ADepth, (f) => { so.Depth = f; });
            so.Create(scene.DefaultSOMaterial);
            safe_set_property_s(attributes, IOStrings.ASOName, (s) => { so.Name = s; });
            set_transform(so, attributes);
            set_material(so, attributes);
            return so;
        }


        public virtual SceneObject BuildSphere(FScene scene, Dictionary<string, object> attributes)
        {
            SphereSO so = new SphereSO();
            safe_set_property_f(attributes, IOStrings.ARadius, (f) => { so.Radius = f; });
            so.Create(scene.DefaultSOMaterial);
            safe_set_property_s(attributes, IOStrings.ASOName, (s) => { so.Name = s; });
            set_transform(so, attributes);
            set_material(so, attributes);
            return so;
        }


        public virtual SceneObject BuildPivot(FScene scene, Dictionary<string, object> attributes)
        {
            PivotSO so = new PivotSO();
            so.Create(scene.PivotSOMaterial, scene.FrameMaterial);
            safe_set_property_s(attributes, IOStrings.ASOName, (s) => { so.Name = s; });
            set_transform(so, attributes);
            set_material(so, attributes);
            return so;
        }


        public virtual SceneObject BuildPolyCurveSO(FScene scene, Dictionary<string, object> attributes)
        {
            PolyCurveSO so = new PolyCurveSO();
            so.Create(scene.DefaultSOMaterial);
            safe_set_property_s(attributes, IOStrings.ASOName, (s) => { so.Name = s; });
            set_transform(so, attributes);
            set_material(so, attributes);

            if (check_key_or_debug_print(attributes, IOStrings.APolyCurve3)) {
                VectorArray3d v = attributes[IOStrings.APolyCurve3] as VectorArray3d;
                so.Curve.SetVertices(v);
            }
            if (check_key_or_debug_print(attributes, IOStrings.APolyCurveClosed))
                so.Curve.Closed = (bool)attributes[IOStrings.APolyCurveClosed];

            return so;
        }


        public virtual SceneObject BuildPolyTubeSO(FScene scene, Dictionary<string, object> attributes)
        {
            PolyTubeSO so = new PolyTubeSO();
            so.Create(scene.DefaultSOMaterial);
            safe_set_property_s(attributes, IOStrings.ASOName, (s) => { so.Name = s; });
            set_transform(so, attributes);
            set_material(so, attributes);

            if (check_key_or_debug_print(attributes, IOStrings.APolyCurve3)) {
                VectorArray3d v = attributes[IOStrings.APolyCurve3] as VectorArray3d;
                so.Curve.SetVertices(v);
            }
            if (check_key_or_debug_print(attributes, IOStrings.APolyCurveClosed))
                so.Curve.Closed = (bool)attributes[IOStrings.APolyCurveClosed];
            if ( check_key_or_debug_print(attributes, IOStrings.APolygon2)) {
                VectorArray2d v = attributes[IOStrings.APolygon2] as VectorArray2d;
                so.Polygon = new Polygon2d(v);
            }

            return so;
        }


        public virtual SceneObject BuildMeshSO(FScene scene, Dictionary<string, object> attributes)
        {
            MeshSO so = new MeshSO();

            SimpleMesh m = BuildSimpleMesh(attributes, true);

            so.Create(m, scene.DefaultSOMaterial);
            safe_set_property_s(attributes, IOStrings.ASOName, (s) => { so.Name = s; });
            set_transform(so, attributes);
            set_material(so, attributes);

            return so;
        }



        public virtual SceneObject BuildMeshReference(FScene scene, string sSceneFilePath, Dictionary<string, object> attributes)
        {
            string sAbsPath = "", sRelPath = "";
            bool bAbsPathOK = safe_set_property_s(attributes, IOStrings.AReferencePath, (str) => { sAbsPath = str; });
            bool bRelPathOK = safe_set_property_s(attributes, IOStrings.ARelReferencePath, (str) => { sRelPath = str; });

            string sScenePathDir = Path.GetDirectoryName(sSceneFilePath);

            // ok we are going to try really hard to find references...
            string sUsePath = "";
            string sBaseFilename = "";
            if (bRelPathOK) {
                sBaseFilename = Path.GetFileName(sRelPath);

                // first we check if relative path exists
                string sAbsRelPath = Path.Combine(sScenePathDir, sRelPath);
                if (System.IO.File.Exists(sAbsRelPath)) {
                    DebugUtil.Log(2, "[UnitySerialization.BuildMeshReference] using relative path " + sRelPath);
                    sUsePath = sAbsRelPath;
                }

                // if not, we try appending filename to scene path
                if (sUsePath == "") {
                    string sLocalPath = Path.Combine(sScenePathDir, sBaseFilename);
                    if (System.IO.File.Exists(sLocalPath)) {
                        DebugUtil.Log(2, "[UnitySerialization.BuildMeshReference] using local path " + sLocalPath);
                        sUsePath = sLocalPath;
                    }
                }

                // if that fails we accumulate relative path segments (from last folder backwards)
                // and see if we can find the file in any of those
                List<string> subdirs = new List<string>(sRelPath.Split(Path.DirectorySeparatorChar).Reverse());
                int N = subdirs.Count;
                string sAccumPath = "";
                for (int i = 1; i < N && sUsePath == ""; ++i) {
                    sAccumPath = Path.Combine(subdirs[i], sAccumPath);
                    string sLocalPath = Path.Combine(Path.Combine(sScenePathDir, sAccumPath), sBaseFilename);
                    DebugUtil.Log(2, "trying " + sLocalPath);
                    if (System.IO.File.Exists(sLocalPath)) {
                        DebugUtil.Log(2, "[UnitySerialization.BuildMeshReference] using local path " + sLocalPath);
                        sUsePath = sLocalPath;
                    }
                }
            }

            // ok if all that failed, try absolute path 
            if (sUsePath == "" && bAbsPathOK) {
                sBaseFilename = Path.GetFileName(sAbsPath);
                if (System.IO.File.Exists(sAbsPath)) {
                    DebugUtil.Log(2, "[UnitySerialization.BuildMeshReference] using absolute path " + sAbsPath);
                    sUsePath = sAbsPath;
                }

                // try appending filename to scene path in case we didn't have a relative path at all (bRelPathOK = false)
                if (sUsePath == "") {
                    string sLocalPath = Path.Combine(sScenePathDir, sBaseFilename);
                    if (System.IO.File.Exists(sLocalPath)) {
                        DebugUtil.Log(2, "[UnitySerialization.BuildMeshReference] using local path " + sLocalPath);
                        sUsePath = sLocalPath;
                    }
                }
            }

            if (sUsePath == "") {
                emit_message("referenced mesh does not exist at path [" + sAbsPath + "] or [" + sRelPath + "] ");
                return null;
            }

            SceneImporter import = new SceneImporter();
            bool bOK = import.ReadFile(sUsePath);
            if (bOK == false && import.LastReadResult.result != g3.ReadResult.Ok) {
                emit_message("import of mesh [" + sUsePath + "] failed: " + import.LastReadResult.info);
                // [TODO] how can we show this message?
                //HUDUtil.ShowCenteredStaticPopupMessage("popups/error_reading_file", activeCockpit);
                return null;
            }
            MeshReferenceSO refSO = import.GetMeshReference(scene.DefaultMeshSOMaterial);
            safe_set_property_s(attributes, IOStrings.ASOName, (s) => { refSO.Name = s; });
            set_transform(refSO, attributes);
            return refSO;
        }



        //
        // utility functions
        //


        public virtual SimpleMesh BuildSimpleMesh(Dictionary<string, object> attributes, bool bSwapRightLeft)
        {
            string sFormat = IOStrings.AMeshFormat_Ascii;
            if (check_key_or_debug_print(attributes, IOStrings.AMeshFormat)) {
                sFormat = attributes[IOStrings.AMeshFormat] as string;
            }
            // only have ascii and uuencoded binary right now..
            bool bBinary = (sFormat == IOStrings.AMeshFormat_UUBinary);

            VectorArray3d v = null;
            VectorArray3i t = null;
            VectorArray3f n = null, c = null;
            VectorArray2f uv = null;

            if (bBinary) {
                if (check_key_or_debug_print(attributes, IOStrings.AMeshVertices3Binary))
                    v = attributes[IOStrings.AMeshVertices3Binary] as VectorArray3d;
                if (check_key_or_debug_print(attributes, IOStrings.AMeshTrianglesBinary))
                    t = attributes[IOStrings.AMeshTrianglesBinary] as VectorArray3i;
                if (check_key_or_debug_print(attributes, IOStrings.AMeshNormals3Binary))
                    n = attributes[IOStrings.AMeshNormals3Binary] as VectorArray3f;
                if (check_key_or_debug_print(attributes, IOStrings.AMeshColors3Binary))
                    c = attributes[IOStrings.AMeshColors3Binary] as VectorArray3f;
                if (check_key_or_debug_print(attributes, IOStrings.AMeshUVs2Binary))
                    uv = attributes[IOStrings.AMeshUVs2Binary] as VectorArray2f;

            } else {
                if (check_key_or_debug_print(attributes, IOStrings.AMeshVertices3))
                    v = attributes[IOStrings.AMeshVertices3] as VectorArray3d;
                if (check_key_or_debug_print(attributes, IOStrings.AMeshTriangles))
                    t = attributes[IOStrings.AMeshTriangles] as VectorArray3i;
                if (check_key_or_debug_print(attributes, IOStrings.AMeshNormals3))
                    n = attributes[IOStrings.AMeshNormals3] as VectorArray3f;
                if (check_key_or_debug_print(attributes, IOStrings.AMeshColors3))
                    c = attributes[IOStrings.AMeshColors3] as VectorArray3f;
                if (check_key_or_debug_print(attributes, IOStrings.AMeshUVs2))
                    uv = attributes[IOStrings.AMeshUVs2] as VectorArray2f;
            }

            if (v == null || t == null)
                return null;

            if ( bSwapRightLeft ) {
                int N = v.Count;
                for (int i = 0; i < N; ++i) {
                    Vector3d vv = v[i];
                    v.Set(i, -vv.x, vv.y, -vv.z);
                }
                if ( n != null && n.Count == N ) {
                    for (int i = 0; i < N; ++i) {
                        Vector3f nn = n[i];
                        n.Set(i, -nn.x, nn.y, -nn.z);
                    }
                }

            }

            SimpleMesh m = new SimpleMesh();
            m.Initialize(v, t, n, c, uv);
            return m;
        }



        public virtual bool check_key_or_debug_print(Dictionary<string, object> attributes, string sKey)
        {
            if (attributes.ContainsKey(sKey) == false) {
                emit_message("attribute " + sKey + " is missing");
                return false;
            } else
                return true;
        }



        public virtual void set_transform(TransformableSceneObject so, Dictionary<string, object> attributes)
        {
            Frame3f f = Frame3f.Identity;

            if (check_key_or_debug_print(attributes, IOStrings.APosition)) {
                Vector3f vPosition = (Vector3f)attributes[IOStrings.APosition];
                f.Origin = vPosition;
            }
            if (check_key_or_debug_print(attributes, IOStrings.AOrientation)) {
                Quaternionf vRotation = (g3.Quaternionf)attributes[IOStrings.AOrientation];
                f.Rotation = vRotation;
            }

            so.SetLocalFrame(f, CoordSpace.ObjectCoords);

            if (check_key_or_debug_print(attributes, IOStrings.AOrientation)) {
                Vector3f vScale = (Vector3f)attributes[IOStrings.AScale];
                so.RootGameObject.transform.localScale = vScale;
            }
        }


        public virtual void set_material(SceneObject so, Dictionary<string, object> attributes)
        {
            SOMaterial mat = new SOMaterial();
            bool bKnownType = false;
            if (check_key_or_debug_print(attributes, IOStrings.AMaterialType)) {
                string sType = attributes[IOStrings.AMaterialType] as string;
                if (sType == IOStrings.AMaterialType_Standard) {
                    mat.Type = SOMaterial.MaterialType.StandardRGBColor;
                    bKnownType = true;
                } else if (sType == IOStrings.AMaterialType_Transparent) {
                    mat.Type = SOMaterial.MaterialType.TransparentRGBColor;
                    bKnownType = true;
                }
            }
            if (bKnownType == false)
                return;

            if (check_key_or_debug_print(attributes, IOStrings.AMaterialName)) {
                mat.Name = attributes[IOStrings.AMaterialName] as string;
            }

            if (check_key_or_debug_print(attributes, IOStrings.AMaterialRGBColor)) {
                mat.RGBColor = (Colorf)attributes[IOStrings.AMaterialRGBColor];
            }

            so.AssignSOMaterial(mat);
        }


    }
}
