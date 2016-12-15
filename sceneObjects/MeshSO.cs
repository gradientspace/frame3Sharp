using UnityEngine;
using System;

namespace f3
{
	public class MeshSO : BaseSO
	{
		GameObject parentGO;
		GameObject meshGO;

		public MeshSO ()
		{
		}

		public void Create( Mesh mesh, SOMaterial setMaterial) {
            AssignSOMaterial(setMaterial);       // need to do this to setup BaseSO material stack

            parentGO = new GameObject(UniqueNames.GetNext("Mesh"));

            meshGO = new GameObject("mesh");
            meshGO.AddComponent<MeshFilter>();
            meshGO.SetMesh(mesh);
            meshGO.AddComponent<MeshCollider>().enabled = false;
            meshGO.AddComponent<MeshRenderer>().material = CurrentMaterial;

            AppendNewGO(meshGO, parentGO, true);
        }



        //
        // SceneObject impl
        //
        override public GameObject RootGameObject
        {
            get { return parentGO; }
        }

        override public string Name
        {
            get { return parentGO.GetName(); }
            set { parentGO.SetName(value); }
        }

        override public SOType Type { get { return SOTypes.Mesh; } }

        override public SceneObject Duplicate()
        {
            MeshSO copy = new MeshSO();

            copy.Create( meshGO.GetMesh(), this.GetAssignedSOMaterial() );
            copy.SetLocalFrame(
                this.GetLocalFrame(CoordSpace.ObjectCoords), CoordSpace.ObjectCoords);
            copy.SetLocalScale(this.GetLocalScale());

            return copy;
        }

        override public Bounds GetLocalBoundingBox()
        {
            Bounds b = meshGO.GetSharedMesh().bounds;
            Vector3 s = meshGO.transform.localScale;
            b.extents = new Vector3(
                            b.extents[0] * s[0],
                            b.extents[1] * s[1],
                            b.extents[2] * s[2]);

            return b;
        }


    }
}

