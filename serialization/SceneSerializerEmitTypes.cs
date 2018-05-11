using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using g3;

using System.Runtime.InteropServices;


namespace f3
{
    // extension methods to SceneSerializer for emitting built-in types
    public static class SceneSerializerEmitTypesExt
    {


        public static void Emit(this SceneSerializer s, IOutputStream o, CylinderSO so) {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypeCylinder);
            EmitCylinderSO(s, o, so);
        }
        public static void EmitCylinderSO(SceneSerializer s, IOutputStream o, CylinderSO so)
        {
            o.AddAttribute(IOStrings.ASOName, so.Name);
            o.AddAttribute(IOStrings.ASOUuid, so.UUID);
            s.EmitTransform(o, so);
            o.AddAttribute(IOStrings.ARadius, so.Radius);
            o.AddAttribute(IOStrings.AHeight, so.Height);
            Frame3f f = so.GetLocalFrame(CoordSpace.ObjectCoords);
            o.AddAttribute(IOStrings.AStartPoint, (f.Origin - 0.5f * so.ScaledHeight * f.Y));
            o.AddAttribute(IOStrings.AEndPoint, (f.Origin + 0.5f * so.ScaledHeight * f.Y));
            s.EmitMaterial(o, so.GetAssignedSOMaterial());
        }


        public static void Emit(this SceneSerializer s, IOutputStream o, BoxSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypeBox);
            EmitBoxSO(s, o, so);
        }
        public static void EmitBoxSO(SceneSerializer s, IOutputStream o, BoxSO so)
        {
            o.AddAttribute(IOStrings.ASOName, so.Name);
            o.AddAttribute(IOStrings.ASOUuid, so.UUID);
            s.EmitTransform(o, so);
            o.AddAttribute(IOStrings.AWidth, so.Width);
            o.AddAttribute(IOStrings.AHeight, so.Height);
            o.AddAttribute(IOStrings.ADepth, so.Depth);
            s.EmitMaterial(o, so.GetAssignedSOMaterial());
        }



        public static void Emit(this SceneSerializer s, IOutputStream o, SphereSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypeSphere);
            EmitSphereSO(s, o, so);
        }
        public static void EmitSphereSO(SceneSerializer s, IOutputStream o, SphereSO so)
        {
            o.AddAttribute(IOStrings.ASOName, so.Name);
            o.AddAttribute(IOStrings.ASOUuid, so.UUID);
            s.EmitTransform(o, so);
            o.AddAttribute(IOStrings.ARadius, so.Radius);
            s.EmitMaterial(o, so.GetAssignedSOMaterial());
        }



        public static void Emit(this SceneSerializer s, IOutputStream o, PivotSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypePivot);
            EmitPivotSO(s, o, so);
        }
        public static void EmitPivotSO(SceneSerializer s, IOutputStream o, PivotSO so)
        {
            o.AddAttribute(IOStrings.ASOName, so.Name);
            o.AddAttribute(IOStrings.ASOUuid, so.UUID);
            s.EmitTransform(o, so);
            s.EmitMaterial(o, so.GetAssignedSOMaterial());
        }



        public static void Emit(this SceneSerializer s, IOutputStream o, MeshSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypeMesh);
            EmitMeshSO(s, o, so);
        }
        public static void EmitMeshSO(SceneSerializer s, IOutputStream o, MeshSO so)
        {
            o.AddAttribute(IOStrings.ASOName, so.Name);
            o.AddAttribute(IOStrings.ASOUuid, so.UUID);
            s.EmitTransform(o, so);
            SimpleMesh m = so.GetSimpleMesh(true);
            s.EmitMeshBinary(m, o);
        }




        public static void Emit(this SceneSerializer s, IOutputStream o, DMeshSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypeDMesh);
            EmitDMeshSO(s, o, so);
        }
        public static void EmitDMeshSO(SceneSerializer s, IOutputStream o, DMeshSO so)
        {
            SceneSerializer.EmitOptions opt = s.CurrentOptions;

            o.AddAttribute(IOStrings.ASOName, so.Name);
            o.AddAttribute(IOStrings.ASOUuid, so.UUID);
            s.EmitTransform(o, so);
            s.EmitMaterial(o, so.GetAssignedSOMaterial());
            //s.EmitDMeshBinary(so.Mesh, o);
            if (opt.MinimalMeshStorage)
                s.EmitDMeshCompressed_Minimal(so.Mesh, o);
            else
                s.EmitDMeshCompressed(so.Mesh, o);
        }



        public static void Emit(this SceneSerializer s, IOutputStream o, MeshReferenceSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypeMeshReference);
            EmitMeshReferenceSO(s, o, so);
        }
        public static void EmitMeshReferenceSO(SceneSerializer s, IOutputStream o, MeshReferenceSO so)
        {
            o.AddAttribute(IOStrings.ASOName, so.Name);
            o.AddAttribute(IOStrings.ASOUuid, so.UUID);
            s.EmitTransform(o, so);
            // [TODO] be smarter about paths
            o.AddAttribute(IOStrings.AReferencePath, so.MeshReferencePath);

            StringBuilder rel_path = new StringBuilder(260); // MAX_PATH
            if ( PathRelativePathTo(rel_path,
                Path.GetDirectoryName(s.TargetFilePath), FILE_ATTRIBUTE_DIRECTORY, 
                    so.MeshReferencePath, FILE_ATTRIBUTE_NORMAL) == 1) {
                o.AddAttribute(IOStrings.ARelReferencePath, rel_path.ToString());
            }
        }



        // PathRelativePathTo function is only on windows?
        [DllImport("shlwapi.dll", SetLastError = true)]
        private static extern int PathRelativePathTo(StringBuilder pszPath,
            string pszFrom, int dwAttrFrom, string pszTo, int dwAttrTo);
        private const int FILE_ATTRIBUTE_DIRECTORY = 0x10;
        private const int FILE_ATTRIBUTE_NORMAL = 0x80;


        public static void EmitGenericSO(this SceneSerializer s, IOutputStream o, SceneObject so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypeUnknown);
            o.AddAttribute(IOStrings.ASOName, so.Name);
            o.AddAttribute(IOStrings.ASOUuid, so.UUID);
            s.EmitTransform(o, so);
        }



        public static void Emit(this SceneSerializer s, IOutputStream o, PolyCurveSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypePolyCurve);
            EmitPolyCurveSO(s, o, so);
        }
        public static void EmitPolyCurveSO(SceneSerializer s, IOutputStream o, PolyCurveSO so)
        {
            o.AddAttribute(IOStrings.ASOName, so.Name);
            o.AddAttribute(IOStrings.ASOUuid, so.UUID);
            s.EmitTransform(o, so);
            s.EmitMaterial(o, so.GetAssignedSOMaterial());
            o.AddAttribute(IOStrings.APolyCurve3, so.Curve.Vertices);
            o.AddAttribute(IOStrings.APolyCurveClosed, so.Curve.Closed);
        }


        public static void Emit(this SceneSerializer s, IOutputStream o, PolyTubeSO so)
        {
            o.AddAttribute(IOStrings.ASOType, IOStrings.TypePolyTube);
            EmitPolyTubeSO(s, o, so);
        }
        public static void EmitPolyTubeSO(SceneSerializer s, IOutputStream o, PolyTubeSO so)
        {
            o.AddAttribute(IOStrings.ASOName, so.Name);
            o.AddAttribute(IOStrings.ASOUuid, so.UUID);
            s.EmitTransform(o, so);
            s.EmitMaterial(o, so.GetAssignedSOMaterial());
            o.AddAttribute(IOStrings.APolyCurve3, so.Curve.Vertices );
            o.AddAttribute(IOStrings.APolyCurveClosed, so.Curve.Closed );
            o.AddAttribute(IOStrings.APolygon2, so.Polygon.Vertices);
        }




        /// <summary>
        /// Emit a SceneObject transform as a TransformStruct (position, orientation, scale)
        /// </summary>
        public static void EmitTransform(this SceneSerializer s, IOutputStream o, SceneObject so)
        {
            o.BeginStruct(IOStrings.TransformStruct);
            Frame3f f = so.GetLocalFrame(CoordSpace.ObjectCoords);
            o.AddAttribute(IOStrings.APosition, f.Origin );
            o.AddAttribute(IOStrings.AOrientation, f.Rotation );
            o.AddAttribute(IOStrings.AScale, so.RootGameObject.GetLocalScale());
            o.EndStruct();
        }

        /// <summary>
        /// Emit an SOMaterial as a MaterialStruct
        /// </summary>
        public static void EmitMaterial(this SceneSerializer s, IOutputStream o, SOMaterial mat)
        {
            o.BeginStruct(IOStrings.MaterialStruct);
            if (mat.Type == SOMaterial.MaterialType.StandardRGBColor) {
                o.AddAttribute(IOStrings.AMaterialType, IOStrings.AMaterialType_Standard);
                o.AddAttribute(IOStrings.AMaterialName, mat.Name);
                o.AddAttribute(IOStrings.AMaterialRGBColor, mat.RGBColor);
            } else if ( mat.Type == SOMaterial.MaterialType.TransparentRGBColor) {
                o.AddAttribute(IOStrings.AMaterialType, IOStrings.AMaterialType_Transparent);
                o.AddAttribute(IOStrings.AMaterialName, mat.Name);
                o.AddAttribute(IOStrings.AMaterialRGBColor, mat.RGBColor);
            }
            o.EndStruct();
        }


        /// <summary>
        /// Emit frame as a struct
        /// </summary>
        public static void EmitFrame(this SceneSerializer s, IOutputStream o, string structName, ref Frame3f frame)
        {
            o.BeginStruct(structName);
            o.AddAttribute(IOStrings.APosition, frame.Origin);
            o.AddAttribute(IOStrings.AOrientation, frame.Rotation);
            o.EndStruct();
        }
        public static void EmitFrame(this SceneSerializer s, IOutputStream o, string structName, Frame3f frame) {
            EmitFrame(s, o, structName, ref frame);
        }


        /// <summary>
        /// Emit a SimpleMesh as an AsciiMeshStruct
        /// </summary>
        public static void EmitMeshAscii(this SceneSerializer s, SimpleMesh m, IOutputStream o)
        {
            o.BeginStruct(IOStrings.AsciiMeshStruct);
            o.AddAttribute(IOStrings.AMeshVertices3, m.VerticesItr());
            if (m.HasVertexNormals)
                o.AddAttribute(IOStrings.AMeshNormals3, m.NormalsItr());
            if (m.HasVertexColors)
                o.AddAttribute(IOStrings.AMeshColors3, m.ColorsItr());
            if (m.HasVertexUVs)
                o.AddAttribute(IOStrings.AMeshUVs2, m.UVsItr());
            o.AddAttribute(IOStrings.AMeshTriangles, m.TrianglesItr());
            o.EndStruct();
        }


        /// <summary>
        /// Emit a SimpleMesh as a BinaryMeshStruct
        /// </summary>
        public static void EmitMeshBinary(this SceneSerializer s, SimpleMesh m, IOutputStream o)
        {
            // binary version - uuencoded byte buffers
            //    - storing doubles uses roughly same mem as string, but string is only 8 digits precision
            //    - storing floats saves roughly 50%
            //    - storing triangles is worse until vertex count > 9999
            //          - could store as byte or short in those cases...
            o.BeginStruct(IOStrings.BinaryMeshStruct);
            o.AddAttribute(IOStrings.AMeshVertices3Binary, m.Vertices.GetBytes());
            if (m.HasVertexNormals)
                o.AddAttribute(IOStrings.AMeshNormals3Binary, m.Normals.GetBytes());
            if (m.HasVertexColors)
                o.AddAttribute(IOStrings.AMeshColors3Binary, m.Colors.GetBytes());
            if (m.HasVertexUVs)
                o.AddAttribute(IOStrings.AMeshUVs2Binary, m.UVs.GetBytes());
            o.AddAttribute(IOStrings.AMeshTrianglesBinary, m.Triangles.GetBytes());
            o.EndStruct();
        }


        /// <summary>
        /// Emit a DMesh3 as a BinaryDMeshStruct
        /// </summary>
        public static void EmitDMeshBinary(this SceneSerializer s, DMesh3 m, IOutputStream o)
        {
            // binary version - uuencoded byte buffers
            //    - storing doubles uses roughly same mem as string, but string is only 8 digits precision
            //    - storing floats saves roughly 50%
            //    - storing triangles is worse until vertex count > 9999
            //          - could store as byte or short in those cases...
            //    - edges and edge ref counts are stored, then use mesh.RebuildFromEdgeRefcounts() to rebuild 3D mesh (same as gSerialization)
            o.BeginStruct(IOStrings.BinaryDMeshStruct);
            o.AddAttribute(IOStrings.AMeshVertices3Binary, m.VerticesBuffer.GetBytes());
            if (m.HasVertexNormals)
                o.AddAttribute(IOStrings.AMeshNormals3Binary, m.NormalsBuffer.GetBytes());
            if (m.HasVertexColors)
                o.AddAttribute(IOStrings.AMeshColors3Binary, m.ColorsBuffer.GetBytes());
            if (m.HasVertexUVs)
                o.AddAttribute(IOStrings.AMeshUVs2Binary, m.UVBuffer.GetBytes());
            o.AddAttribute(IOStrings.AMeshTrianglesBinary, m.TrianglesBuffer.GetBytes());
            if (m.HasTriangleGroups)
                o.AddAttribute(IOStrings.AMeshTriangleGroupsBinary, m.GroupsBuffer.GetBytes());
            o.AddAttribute(IOStrings.AMeshEdgesBinary, m.EdgesBuffer.GetBytes());
            o.AddAttribute(IOStrings.AMeshEdgeRefCountsBinary, m.EdgesRefCounts.RawRefCounts.GetBytes());
            o.EndStruct();
        }




        /// <summary>
        /// Emit a DMesh3 as a CompressedDMeshStruct
        /// </summary>
        public static void EmitDMeshCompressed(this SceneSerializer s, DMesh3 m, IOutputStream o)
        {
            SceneSerializer.EmitOptions opt = s.CurrentOptions;

            // compressed version - uuencoded byte buffers
            //    - storing doubles uses roughly same mem as string, but string is only 8 digits precision
            //    - storing floats saves roughly 50%
            //    - storing triangles is worse until vertex count > 9999
            //          - could store as byte or short in those cases...
            //    - edges and edge ref counts are stored, then use mesh.RebuildFromEdgeRefcounts() to rebuild 3D mesh (same as gSerialization)
            o.BeginStruct(IOStrings.CompressedDMeshStruct);

            o.AddAttribute(IOStrings.AMeshStorageMode, (int)IOStrings.MeshStorageMode.EdgeRefCounts);

            o.AddAttribute(IOStrings.AMeshVertices3Compressed, BufferUtil.CompressZLib(m.VerticesBuffer.GetBytes(), opt.FastCompression));

            if (opt.StoreMeshVertexNormals && m.HasVertexNormals)
                o.AddAttribute(IOStrings.AMeshNormals3Compressed, BufferUtil.CompressZLib(m.NormalsBuffer.GetBytes(), opt.FastCompression));
            if (opt.StoreMeshVertexColors && m.HasVertexColors)
                o.AddAttribute(IOStrings.AMeshColors3Compressed, BufferUtil.CompressZLib(m.ColorsBuffer.GetBytes(), opt.FastCompression));
            if (opt.StoreMeshVertexUVs && m.HasVertexUVs)
                o.AddAttribute(IOStrings.AMeshUVs2Compressed, BufferUtil.CompressZLib(m.UVBuffer.GetBytes(), opt.FastCompression));

            o.AddAttribute(IOStrings.AMeshTrianglesCompressed, BufferUtil.CompressZLib(m.TrianglesBuffer.GetBytes(), opt.FastCompression));
            if (opt.StoreMeshFaceGroups && m.HasTriangleGroups)
                o.AddAttribute(IOStrings.AMeshTriangleGroupsCompressed, BufferUtil.CompressZLib(m.GroupsBuffer.GetBytes(), opt.FastCompression));

            o.AddAttribute(IOStrings.AMeshEdgesCompressed, BufferUtil.CompressZLib(m.EdgesBuffer.GetBytes(), opt.FastCompression));
            o.AddAttribute(IOStrings.AMeshEdgeRefCountsCompressed, BufferUtil.CompressZLib(m.EdgesRefCounts.RawRefCounts.GetBytes(), opt.FastCompression));
            o.EndStruct();
        }







        /// <summary>
        /// Emit a DMesh3 as a CompressedDMeshStruct
        /// </summary>
        public static void EmitDMeshCompressed_Minimal(this SceneSerializer s, DMesh3 m, IOutputStream o)
        {
            SceneSerializer.EmitOptions opt = s.CurrentOptions;

            // compressed version - uuencoded byte buffers
            //    - storing doubles uses roughly same mem as string, but string is only 8 digits precision
            //    - storing floats saves roughly 50%
            //    - storing triangles is worse until vertex count > 9999
            //          - could store as byte or short in those cases...
            //    - edges and edge ref counts are stored, then use mesh.RebuildFromEdgeRefcounts() to rebuild 3D mesh (same as gSerialization)
            o.BeginStruct(IOStrings.CompressedDMeshStruct);

            o.AddAttribute(IOStrings.AMeshStorageMode, (int)IOStrings.MeshStorageMode.Minimal);

            // need compact mesh to do this
            if (m.IsCompactV == false)
                m = new DMesh3(m, true);

            o.AddAttribute(IOStrings.AMeshVertices3Compressed, BufferUtil.CompressZLib(m.VerticesBuffer.GetBytes(), opt.FastCompression));

            if (opt.StoreMeshVertexNormals && m.HasVertexNormals)
                o.AddAttribute(IOStrings.AMeshNormals3Compressed, BufferUtil.CompressZLib(m.NormalsBuffer.GetBytes(), opt.FastCompression));
            if (opt.StoreMeshVertexColors && m.HasVertexColors)
                o.AddAttribute(IOStrings.AMeshColors3Compressed, BufferUtil.CompressZLib(m.ColorsBuffer.GetBytes(), opt.FastCompression));
            if (opt.StoreMeshVertexUVs && m.HasVertexUVs)
                o.AddAttribute(IOStrings.AMeshUVs2Compressed, BufferUtil.CompressZLib(m.UVBuffer.GetBytes(), opt.FastCompression));

            int[] triangles = new int[3 * m.TriangleCount];
            int k = 0;
            foreach ( int tid in m.TriangleIndices() ) {
                Index3i t = m.GetTriangle(tid);
                triangles[k++] = t.a; triangles[k++] = t.b; triangles[k++] = t.c;
            }
            o.AddAttribute(IOStrings.AMeshTrianglesCompressed, BufferUtil.CompressZLib(BufferUtil.ToBytes(triangles), opt.FastCompression));

            if (opt.StoreMeshFaceGroups && m.HasTriangleGroups) {
                int[] groups = new int[m.TriangleCount];
                k = 0;
                foreach (int tid in m.TriangleIndices())
                    groups[k++] = m.GetTriangleGroup(tid);
                o.AddAttribute(IOStrings.AMeshTriangleGroupsCompressed, BufferUtil.CompressZLib(BufferUtil.ToBytes(groups), opt.FastCompression));
            }

            o.EndStruct();
        }




        /// <summary>
        /// Emit a keyframe sequence as a KeyframeListStruct
        /// </summary>
        public static void EmitKeyframes(this SceneSerializer s, KeyframeSequence seq, IOutputStream o)
        {
            o.BeginStruct(IOStrings.KeyframeListStruct);
            o.AddAttribute(IOStrings.ATimeRange, (Vector2f)seq.ValidRange);

            int i = 0;
            foreach ( Keyframe k  in seq ) {
                o.BeginStruct(IOStrings.KeyframeStruct, i.ToString());
                i++;
                o.AddAttribute(IOStrings.ATime, (float)k.Time, true );
                o.AddAttribute(IOStrings.APosition, k.Frame.Origin, true );
                o.AddAttribute(IOStrings.AOrientation, k.Frame.Rotation, true );
                o.EndStruct();
            }

            o.EndStruct();
        }

    }



}
