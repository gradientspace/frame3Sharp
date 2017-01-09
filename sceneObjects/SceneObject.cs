using System;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{


	public enum CoordSpace {
		WorldCoords = 0,
		SceneCoords = 1,
		ObjectCoords = 2
	};

	public interface ITransformable
	{
		Frame3f GetLocalFrame(CoordSpace eSpace);
		void SetLocalFrame(Frame3f newFrame, CoordSpace eSpace);

        bool SupportsScaling { get; }
        Vector3 GetLocalScale();
        void SetLocalScale(Vector3 scale);
	}

    public interface IParentSO
    {
        IEnumerable<SceneObject> GetChildren();
    }


	public interface SceneObject
	{

		GameObject RootGameObject { get; }

        string UUID { get; }
        string Name { get; set; }
        int Timestamp { get; }
        SOType Type { get; }

        bool IsTemporary { get; }       // If return true, means object should not be serialized, etc
                                        // Useful for temp parent objects, that kind of thing.

        bool IsSurface { get; }         // does this object have a surface we can use (ie a mesh/etc)

		void SetScene(FScene s);
		FScene GetScene();

        SceneObject Duplicate();

        void SetCurrentTime(double time);   // for keyframing

		void AssignSOMaterial(SOMaterial m);
        SOMaterial GetAssignedSOMaterial();

        void PushOverrideMaterial(Material m);
        void PopOverrideMaterial();
        Material GetActiveMaterial();

        // called on per-frame Update()
        void PreRender();

		bool FindRayIntersection(Ray ray, out SORayHit hit);

        Bounds GetTransformedBoundingBox();
        Bounds GetLocalBoundingBox();
	}


    // should we just make scene object transformable??
    public delegate void TransformChangedEventHandler(TransformableSceneObject so);
	public interface TransformableSceneObject : SceneObject, ITransformable
	{
        event TransformChangedEventHandler OnTransformModified;
	}

}

