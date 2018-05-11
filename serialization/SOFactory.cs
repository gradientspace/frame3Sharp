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

        public virtual SceneObject Build(FScene scene, SceneSerializer serializer, TypedAttribSet attributes)
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
            SceneSerializer serializer, TypedAttribSet attributes )
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
            } else if (typeIdentifier == IOStrings.TypeDMesh) {
                return BuildDMeshSO(scene, attributes);
            } else {
                return null;
            }
        }








        public virtual SceneObject BuildCylinder(FScene scene, TypedAttribSet attributes)
        {
            CylinderSO so = new CylinderSO();
            safe_set_property_f(attributes, IOStrings.ARadius, (f) => { so.Radius = f; });
            safe_set_property_f(attributes, IOStrings.AHeight, (f) => { so.Height = f; });
            so.Create(scene.DefaultSOMaterial);
            RestoreSOInfo(so, attributes);
            RestoreTransform(so, attributes);
            RestoreMaterial(so, attributes);
            return so;
        }


        public virtual SceneObject BuildBox(FScene scene, TypedAttribSet attributes)
        {
            BoxSO so = new BoxSO();
            safe_set_property_f(attributes, IOStrings.AWidth, (f) => { so.Width = f; });
            safe_set_property_f(attributes, IOStrings.AHeight, (f) => { so.Height = f; });
            safe_set_property_f(attributes, IOStrings.ADepth, (f) => { so.Depth = f; });
            so.Create(scene.DefaultSOMaterial);
            RestoreSOInfo(so, attributes);
            RestoreTransform(so, attributes);
            RestoreMaterial(so, attributes);
            return so;
        }


        public virtual SceneObject BuildSphere(FScene scene, TypedAttribSet attributes)
        {
            SphereSO so = new SphereSO();
            safe_set_property_f(attributes, IOStrings.ARadius, (f) => { so.Radius = f; });
            so.Create(scene.DefaultSOMaterial);
            RestoreSOInfo(so, attributes);
            RestoreTransform(so, attributes);
            RestoreMaterial(so, attributes);
            return so;
        }


        public virtual SceneObject BuildPivot(FScene scene, TypedAttribSet attributes)
        {
            PivotSO so = new PivotSO();
            so.Create(scene.PivotSOMaterial, scene.FrameSOMaterial);
            RestorePivotSOType(scene, attributes, so);
            return so;
        }
        public virtual void RestorePivotSOType(FScene scene, TypedAttribSet attributes, PivotSO so)
        {
            RestoreSOInfo(so, attributes);
            RestoreTransform(so, attributes);
            RestoreMaterial(so, attributes);
        }


        public virtual SceneObject BuildPolyCurveSO(FScene scene, TypedAttribSet attributes)
        {
            PolyCurveSO so = new PolyCurveSO();
            so.Create(scene.DefaultSOMaterial);
            RestorePolyCurveSOType(scene, attributes, so);
            return so;
        }
        public virtual void RestorePolyCurveSOType(FScene scene, TypedAttribSet attributes, PolyCurveSO so)
        {
            RestoreSOInfo(so, attributes);
            RestoreTransform(so, attributes);
            RestoreMaterial(so, attributes);

            if (check_key_or_debug_print(attributes, IOStrings.APolyCurve3)) {
                VectorArray3d v = attributes[IOStrings.APolyCurve3] as VectorArray3d;
                so.Curve.SetVertices(v);
            }
            if (check_key_or_debug_print(attributes, IOStrings.APolyCurveClosed))
                so.Curve.Closed = (bool)attributes[IOStrings.APolyCurveClosed];
        }


        public virtual SceneObject BuildPolyTubeSO(FScene scene, TypedAttribSet attributes)
        {
            PolyTubeSO so = new PolyTubeSO();
            so.Create(scene.DefaultSOMaterial);
            RestoreSOInfo(so, attributes);
            RestoreTransform(so, attributes);
            RestoreMaterial(so, attributes);

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


        public virtual SceneObject BuildMeshSO(FScene scene, TypedAttribSet attributes)
        {
            MeshSO so = new MeshSO();

            SimpleMesh m = RestoreSimpleMesh(attributes, true);

            so.Create(m, scene.DefaultSOMaterial);
            RestoreSOInfo(so, attributes);
            RestoreTransform(so, attributes);
            RestoreMaterial(so, attributes);

            return so;
        }


        public virtual SceneObject BuildDMeshSO(FScene scene, TypedAttribSet attributes)
        {
            DMeshSO so = new DMeshSO();
            RestoreDMeshSO(scene, attributes, so);
            return so;
        }
        public virtual void RestoreDMeshSO(FScene scene, TypedAttribSet attributes, DMeshSO so)
        {
            DMesh3 mesh = RestoreDMesh(attributes);
            so.Create(mesh, scene.DefaultSOMaterial);
            RestoreSOInfo(so, attributes);
            RestoreTransform(so, attributes);
            RestoreMaterial(so, attributes);
        }


        public virtual SceneObject BuildMeshReference(FScene scene, string sSceneFilePath, TypedAttribSet attributes)
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

            SceneMeshImporter import = new SceneMeshImporter();
            bool bOK = import.ReadFile(sUsePath);
            if (bOK == false && import.LastReadResult.code != g3.IOCode.Ok) {
                emit_message("import of mesh [" + sUsePath + "] failed: " + import.LastReadResult.message);
                // [TODO] how can we show this message?
                //HUDUtil.ShowCenteredStaticPopupMessage("popups/error_reading_file", activeCockpit);
                return null;
            }
            MeshReferenceSO refSO = import.GetMeshReference(scene.DefaultMeshSOMaterial);
            RestoreSOInfo(refSO, attributes);
            RestoreTransform(refSO, attributes);
            return refSO;
        }






        // restore structs

        public virtual void RestoreSOInfo(BaseSO so, TypedAttribSet attributes)
        {
            safe_set_property_s(attributes, IOStrings.ASOName, (s) => { so.Name = s; });
            safe_set_property_s(attributes, IOStrings.ASOUuid, (s) => { so.__set_uuid(s, "0xDEADBEEF"); });
        }
        public virtual void RestoreSOInfo(GroupSO so, TypedAttribSet attributes)
        {
            safe_set_property_s(attributes, IOStrings.ASOName, (s) => { so.Name = s; });
            safe_set_property_s(attributes, IOStrings.ASOUuid, (s) => { so.__set_uuid(s, "0xDEADBEEF"); });
        }


        public virtual void RestoreTransform(SceneObject so, TypedAttribSet attributes)
        {
            TypedAttribSet transform = find_struct(attributes, IOStrings.TransformStruct);
            if (transform == null)
                throw new Exception("SOFactory.RestoreTransform: Transform struct not found!");

            Frame3f f = Frame3f.Identity;

            if (check_key_or_debug_print(transform, IOStrings.APosition)) {
                Vector3f vPosition = (Vector3f)transform[IOStrings.APosition];
                f.Origin = vPosition;
            }
            if (check_key_or_debug_print(transform, IOStrings.AOrientation)) {
                Quaternionf vRotation = (g3.Quaternionf)transform[IOStrings.AOrientation];
                f.Rotation = vRotation;
            }

            so.SetLocalFrame(f, CoordSpace.ObjectCoords);

            if (check_key_or_debug_print(transform, IOStrings.AOrientation)) {
                Vector3f vScale = (Vector3f)transform[IOStrings.AScale];
                so.RootGameObject.SetLocalScale(vScale);
            }
        }


        public virtual void RestoreMaterial(SceneObject so, TypedAttribSet attributes)
        {
            TypedAttribSet material = find_struct(attributes, IOStrings.MaterialStruct);
            if (material == null)
                throw new Exception("SOFactory.RestoreMaterial: Material struct not found!");


            SOMaterial mat = new SOMaterial();
            bool bKnownType = false;
            if (check_key_or_debug_print(material, IOStrings.AMaterialType)) {
                string sType = material[IOStrings.AMaterialType] as string;
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

            if (check_key_or_debug_print(material, IOStrings.AMaterialName)) {
                mat.Name = material[IOStrings.AMaterialName] as string;
            }

            if (check_key_or_debug_print(material, IOStrings.AMaterialRGBColor)) {
                mat.RGBColor = (Colorf)material[IOStrings.AMaterialRGBColor];
            }

            so.AssignSOMaterial(mat);
        }



        public virtual Frame3f RestoreFrame(TypedAttribSet attributes, string structName)
        {
            TypedAttribSet transform = find_struct(attributes, structName);
            if (transform == null)
                throw new Exception("SOFactory.RestoreTransform: struct " + structName + " not found!");

            Frame3f f = Frame3f.Identity;
            if (check_key_or_debug_print(transform, IOStrings.APosition)) {
                Vector3f vPosition = (Vector3f)transform[IOStrings.APosition];
                f.Origin = vPosition;
            }
            if (check_key_or_debug_print(transform, IOStrings.AOrientation)) {
                Quaternionf vRotation = (g3.Quaternionf)transform[IOStrings.AOrientation];
                f.Rotation = vRotation;
            }
            return f;
        }



        public virtual SimpleMesh RestoreSimpleMesh(TypedAttribSet attributes, bool bSwapRightLeft)
        {
            bool bBinary = true;
            TypedAttribSet meshAttr = find_struct(attributes, IOStrings.BinaryMeshStruct);
            if (meshAttr == null) {
                meshAttr = find_struct(attributes, IOStrings.AsciiMeshStruct);
                bBinary = false;
            }
            if ( meshAttr == null )
                throw new Exception("SOFactory.RestoreSimpleMesh: Mesh ascii/binary struct not found!");


            VectorArray3d v = null;
            VectorArray3i t = null;
            VectorArray3f n = null, c = null;
            VectorArray2f uv = null;

            if (bBinary) {
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshVertices3Binary))
                    v = meshAttr[IOStrings.AMeshVertices3Binary] as VectorArray3d;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshTrianglesBinary))
                    t = meshAttr[IOStrings.AMeshTrianglesBinary] as VectorArray3i;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshNormals3Binary))
                    n = meshAttr[IOStrings.AMeshNormals3Binary] as VectorArray3f;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshColors3Binary))
                    c = meshAttr[IOStrings.AMeshColors3Binary] as VectorArray3f;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshUVs2Binary))
                    uv = meshAttr[IOStrings.AMeshUVs2Binary] as VectorArray2f;

            } else {
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshVertices3))
                    v = meshAttr[IOStrings.AMeshVertices3] as VectorArray3d;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshTriangles))
                    t = meshAttr[IOStrings.AMeshTriangles] as VectorArray3i;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshNormals3))
                    n = meshAttr[IOStrings.AMeshNormals3] as VectorArray3f;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshColors3))
                    c = meshAttr[IOStrings.AMeshColors3] as VectorArray3f;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshUVs2))
                    uv = meshAttr[IOStrings.AMeshUVs2] as VectorArray2f;
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







        public virtual DMesh3 RestoreDMesh(TypedAttribSet attributes)
        {
            bool is_compressed = false;
            TypedAttribSet meshAttr = find_struct(attributes, IOStrings.BinaryDMeshStruct);
            if ( meshAttr == null ) {
                meshAttr = find_struct(attributes, IOStrings.CompressedDMeshStruct);
                is_compressed = true;
            }
            if (meshAttr == null)
                throw new Exception("SOFactory.RestoreDMesh: DMesh binary or compressed struct not found!");

            VectorArray3d v = null;
            VectorArray3f n = null, c = null;
            VectorArray2f uv = null;

            VectorArray3i t = null;
            int[] g = null;

            IndexArray4i e = null;
            short[] e_ref = null;

            var storageMode = IOStrings.MeshStorageMode.EdgeRefCounts;
            if (meshAttr.ContainsKey(IOStrings.AMeshStorageMode))
                storageMode = (IOStrings.MeshStorageMode)(int)meshAttr[IOStrings.AMeshStorageMode];

            if (is_compressed) {
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshVertices3Compressed))
                    v = meshAttr[IOStrings.AMeshVertices3Compressed] as VectorArray3d;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshNormals3Compressed))
                    n = meshAttr[IOStrings.AMeshNormals3Compressed] as VectorArray3f;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshColors3Compressed))
                    c = meshAttr[IOStrings.AMeshColors3Compressed] as VectorArray3f;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshUVs2Compressed))
                    uv = meshAttr[IOStrings.AMeshUVs2Compressed] as VectorArray2f;

                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshTrianglesCompressed))
                    t = meshAttr[IOStrings.AMeshTrianglesCompressed] as VectorArray3i;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshTriangleGroupsCompressed))
                    g = meshAttr[IOStrings.AMeshTriangleGroupsCompressed] as int[];

                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshEdgesCompressed))
                    e = meshAttr[IOStrings.AMeshEdgesCompressed] as IndexArray4i;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshEdgeRefCountsCompressed))
                    e_ref = meshAttr[IOStrings.AMeshEdgeRefCountsCompressed] as short[];

            } else {
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshVertices3Binary))
                    v = meshAttr[IOStrings.AMeshVertices3Binary] as VectorArray3d;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshNormals3Binary))
                    n = meshAttr[IOStrings.AMeshNormals3Binary] as VectorArray3f;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshColors3Binary))
                    c = meshAttr[IOStrings.AMeshColors3Binary] as VectorArray3f;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshUVs2Binary))
                    uv = meshAttr[IOStrings.AMeshUVs2Binary] as VectorArray2f;

                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshTrianglesBinary))
                    t = meshAttr[IOStrings.AMeshTrianglesBinary] as VectorArray3i;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshTriangleGroupsBinary))
                    g = meshAttr[IOStrings.AMeshTriangleGroupsBinary] as int[];

                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshEdgesBinary))
                    e = meshAttr[IOStrings.AMeshEdgesBinary] as IndexArray4i;
                if (check_key_or_debug_print(meshAttr, IOStrings.AMeshEdgeRefCountsBinary))
                    e_ref = meshAttr[IOStrings.AMeshEdgeRefCountsBinary] as short[];
            }

            DMesh3 m = new DMesh3();
            if (n != null)  m.EnableVertexNormals(Vector3f.Zero);
            if (c != null)  m.EnableVertexColors(Vector3f.Zero);
            if (uv != null) m.EnableVertexUVs(Vector2f.Zero);
            if (g != null)  m.EnableTriangleGroups(0);

            if ( storageMode == IOStrings.MeshStorageMode.EdgeRefCounts ) {
                if (v == null || t == null || e == null || e_ref == null)
                    return null;

                m.VerticesBuffer = new DVector<double>(v);
                if (n != null)  m.NormalsBuffer = new DVector<float>(n);
                if (c != null)  m.ColorsBuffer = new DVector<float>(c);
                if (uv != null) m.UVBuffer = new DVector<float>(uv);
                m.TrianglesBuffer = new DVector<int>(t);
                if (g != null)  m.GroupsBuffer = new DVector<int>(g);

                m.EdgesBuffer = new DVector<int>(e);
                m.EdgesRefCounts = new RefCountVector(e_ref);
                m.RebuildFromEdgeRefcounts();

            } else if ( storageMode == IOStrings.MeshStorageMode.Minimal ) {
                if (v == null || t == null)
                    return null;

                int NV = v.Count;
                NewVertexInfo vinfo = new NewVertexInfo();
                for ( int k = 0; k < NV; ++k ) {
                    vinfo.v = v[k];
                    if (n != null)  vinfo.n = n[k];
                    if (c != null)  vinfo.c = c[k];
                    if (uv != null) vinfo.uv = uv[k];
                    m.AppendVertex(ref vinfo);
                }

                int NT = t.Count;
                for ( int k = 0; k < NT; ++k ) {
                    Vector3i tri = t[k];
                    int setg = (g == null) ? -1 : g[k];
                    m.AppendTriangle(tri, setg);
                }

            } else 
                throw new Exception("SOFactory.RestoreDMesh: unsupported mesh storage mode");

            return m;
        }







        public virtual KeyframeSequence RestoreKeyframes(TypedAttribSet attributes)
        {
            TypedAttribSet listAttribs = find_struct(attributes, IOStrings.KeyframeListStruct);
            if (listAttribs == null)
                throw new Exception("SOFactory.RestoreKeyframes: Transform struct not found!");

            KeyframeSequence keys = new KeyframeSequence();

            if (check_key_or_debug_print(listAttribs, IOStrings.ATimeRange)) {
                Vector2f vRange = (Vector2f)listAttribs[IOStrings.ATimeRange];
                keys.SetValidRange(vRange[0], vRange[1]);
            }

            List<TypedAttribSet> frames = find_all_structs(listAttribs, IOStrings.KeyframeStruct);
            foreach ( TypedAttribSet frameAttrib in frames ) {
                double time = double.PositiveInfinity;
                Frame3f frame = Frame3f.Identity;
                if (check_key_or_debug_print(frameAttrib, IOStrings.ATime)) {
                    time = (float)frameAttrib[IOStrings.ATime];
                }
                if (check_key_or_debug_print(frameAttrib, IOStrings.APosition)) {
                    frame.Origin = (Vector3f)frameAttrib[IOStrings.APosition];
                }
                if (check_key_or_debug_print(frameAttrib, IOStrings.AOrientation)) {
                    frame.Rotation = (Quaternionf)frameAttrib[IOStrings.AOrientation];
                }
                if (time != double.PositiveInfinity)
                    keys.AddOrUpdateKey( new Keyframe(time, frame) );
            }

            return keys;
        }





        //
        // utility functions
        //

        public virtual bool safe_set_property_f(TypedAttribSet attributes, string sPropName, Action<float> setter)
        {
            if (check_key_or_debug_print(attributes, sPropName)) {
                setter((float)attributes[sPropName]);
                return true;
            }
            return false;
        }
        public virtual bool safe_set_property_i(TypedAttribSet attributes, string sPropName, Action<int> setter)
        {
            if (check_key_or_debug_print(attributes, sPropName)) {
                setter((int)attributes[sPropName]);
                return true;
            }
            return false;
        }
        public virtual bool safe_set_property_b(TypedAttribSet attributes, string sPropName, Action<bool> setter)
        {
            if (check_key_or_debug_print(attributes, sPropName)) {
                setter((bool)attributes[sPropName]);
                return true;
            }
            return false;
        }
        public virtual bool safe_set_property_s(TypedAttribSet attributes, string sPropName, Action<string> setter)
        {
            if (check_key_or_debug_print(attributes, sPropName)) {
                setter((string)attributes[sPropName]);
                return true;
            }
            return false;
        }

        public virtual bool check_key_or_debug_print(TypedAttribSet attributes, string sKey)
        {
            if (attributes.ContainsKey(sKey) == false) {
                emit_message("attribute " + sKey + " is missing");
                return false;
            } else
                return true;
        }


        public virtual TypedAttribSet find_struct(TypedAttribSet attribs, string sType, string sIdentifier = "")
        {
            string key = sType;
            if (sIdentifier.Length > 0)
                key += ":" + sIdentifier;
            if (attribs.Pairs.ContainsKey(key))
                return attribs.Pairs[key] as TypedAttribSet;
            else
                return null;
        }


        public virtual List<TypedAttribSet> find_all_structs(TypedAttribSet attribs, string sType)
        {
            List<TypedAttribSet> l = new List<TypedAttribSet>();
            foreach ( var pair in attribs.Pairs ) {
                if (pair.Key.StartsWith(sType))
                    l.Add(pair.Value as TypedAttribSet);
            }
            return l;
        }


    }
}
