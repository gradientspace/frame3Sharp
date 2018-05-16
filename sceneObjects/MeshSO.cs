using System;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{
	public class MeshSO : BaseSO
	{
		protected fGameObject parentGO;
		protected GameObject meshGO;

		public MeshSO ()
		{
		}

        public virtual MeshSO Create(SimpleMesh mesh, SOMaterial setMaterial)
        {
            Mesh umesh = UnityUtil.SimpleMeshToUnityMesh(mesh, false);
            Create(umesh, setMaterial);
            return this;
        }

        public virtual MeshSO Create( Mesh mesh, SOMaterial setMaterial) {
            AssignSOMaterial(setMaterial);       // need to do this to setup BaseSO material stack

            parentGO = GameObjectFactory.CreateParentGO(UniqueNames.GetNext("Mesh"));

            meshGO = new GameObject("mesh");
            meshGO.AddComponent<MeshFilter>();
            meshGO.SetMesh(mesh);
            meshGO.AddComponent<MeshCollider>();
            meshGO.DisableCollider();
            meshGO.AddComponent<MeshRenderer>().material = CurrentMaterial;

            AppendNewGO(meshGO, (GameObject)parentGO, true);
            return this;
        }

        public SimpleMesh GetSimpleMesh(bool bSwapLeftToRight)
        {
            return UnityUtil.UnityMeshToSimpleMesh(meshGO.GetSharedMesh(), bSwapLeftToRight);
        }
        public DMesh3 GetDMesh(bool bSwapLeftToRight)
        {
            return UnityUtil.UnityMeshToDMesh(meshGO.GetSharedMesh(), bSwapLeftToRight);
        }

        public void UpdateVertexPositions(Vector3f[] vPositions)
        {
            UnityUtil.UpdateMeshVertices(meshGO.GetSharedMesh(), vPositions, true);
            // update collider...
        }



        //
        // SceneObject impl
        //
        override public fGameObject RootGameObject
        {
            get { return parentGO; }
        }

        override public string Name
        {
            get { return parentGO.GetName(); }
            set { parentGO.SetName(value); }
        }

        override public SOType Type { get { return SOTypes.Mesh; } }

        public override bool IsSurface {
            get { return true; }
        }

        override public SceneObject Duplicate()
        {
            MeshSO copy = new MeshSO();

            copy.Create( meshGO.GetMesh(), this.GetAssignedSOMaterial() );
            copy.SetLocalFrame(
                this.GetLocalFrame(CoordSpace.ObjectCoords), CoordSpace.ObjectCoords);
            copy.SetLocalScale(this.GetLocalScale());

            return copy;
        }

        override public AxisAlignedBox3f GetLocalBoundingBox()
        {
            AxisAlignedBox3f b = (AxisAlignedBox3f)meshGO.GetSharedMesh().bounds;
            return b;
        }


        override public void DisableShadows() {
            MaterialUtil.DisableShadows(meshGO, true, false);
        }

       



        // this should probably not be here! also not ideal code...
        public struct SectionInfo
        {
            public float maxDiameter;
            public Vector3f maxDiamPos1, maxDiamPos2;
            public List<Vector3f> vCrossings;
        }
        public SectionInfo MeasureSection(Frame3f f)
        {
            Mesh m = meshGO.GetSharedMesh();
            Vector3[] vertices = m.vertices;        // these return copies!!
            int[] triangles = m.triangles;

            Vector3f up = f.Z;

            SectionInfo si = new SectionInfo();
            si.vCrossings = new List<Vector3f>();

            float[] signs = new float[m.vertexCount];
            for ( int i = 0; i < m.vertexCount; ++i ) {
                Vector3f v = vertices[i];
                signs[i] = (v - f.Origin).Dot(up); 
            }

            int[] t = new int[3];
            float[] ts = new float[3];
            Vector3f[] tv = new Vector3f[3];

            int triCount = triangles.Length / 3;
            for (int i = 0; i < triCount; ++i) {
                int ti = 3 * i;
                t[0] = triangles[ti];
                t[1] = triangles[ti + 1];
                t[2] = triangles[ti + 2];

                int c = 0;
                for ( int j = 0; j < 3; ++j ) {
                    ts[j] = signs[t[j]];
                    c += (int)Math.Sign(ts[j]);
                    tv[j] = vertices[t[j]];
                }
                if (c == -3 || c == 3)
                    continue;

                for (int j = 0; j < 3; ++j) {
                    float a = ts[j];
                    float b = ts[(j + 1) % 3];
                    if ( a == 0 ) {
                        si.vCrossings.Add(tv[j]);
                    }
                    if ( b == 0 ) {
                        si.vCrossings.Add(tv[(j + 1) % 3]);
                    }
                    if ( (a < 0 && b > 0) || (b < 0 && a > 0) ) {
                        float alpha = (-b) / (a - b);
                        si.vCrossings.Add(
                            alpha * tv[j] + (1 - alpha) * tv[(j + 1) % 3]);
                    }
                }
            }


            si.maxDiameter = 0;
            for ( int i = 0; i < si.vCrossings.Count; ++i ) {
                for (int j = i+1; j < si.vCrossings.Count; ++j ) {
                    float d = (si.vCrossings[i] - si.vCrossings[j]).LengthSquared;
                    if (d > si.maxDiameter) {
                        si.maxDiameter = d;
                        si.maxDiamPos1 = si.vCrossings[i];
                        si.maxDiamPos2 = si.vCrossings[j];
                    }
                }
            }
            si.maxDiameter = (float)Math.Sqrt(si.maxDiameter);

            return si;
        }



    }
}

