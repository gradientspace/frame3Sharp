using System;
using System.Collections.Generic;
using g3;

namespace f3
{

    /// <summary>
    /// WorldCoords are the absolute values of coordinates after all transforms, after any viewing transforms.
    /// SceneCoords are coordinates "in the scene", ie they are not affected by viewing transforms
    /// ObjectCoords are the local coordinates of an SO. If the SO does not have a parent SO, these
    ///   will be the same as SceneCoords (right?)
    /// </summary>
	public enum CoordSpace {
		WorldCoords = 0,
		SceneCoords = 1,
		ObjectCoords = 2
	};


    /// <summary>
    /// An object in our universe that has a transformation and scale
    /// </summary>
    public interface ITransformed
    {
        fGameObject RootGameObject { get; }
        Frame3f GetLocalFrame(CoordSpace eSpace);
        Vector3f GetLocalScale();
    }

    /// <summary>
    /// An object in our universe that has an editable transformation and scale
    /// </summary>
	public interface ITransformable : ITransformed
	{
		void SetLocalFrame(Frame3f newFrame, CoordSpace eSpace);
        bool SupportsScaling { get; }
        void SetLocalScale(Vector3f scale);
	}

    /// <summary>
    /// An object that contains other SceneObjects
    /// </summary>
    public interface SOCollection
    {
        IEnumerable<SceneObject> GetChildren();
    }

    /// <summary>
    /// SOParent is used to define our own hierarchy of objects. We 
    /// need a type where the Parent can either be another SO, or the Scene.
    /// Then we can traverse "up" the parent hierarchy until we hit the Scene.
    /// </summary>
    public interface SOParent : ITransformed
    {
    }

    public delegate void TransformChangedEventHandler(SceneObject so);

    public interface SceneObject : ITransformable
    {
        SOParent Parent { get; set; }

        string UUID { get; }
        string Name { get; set; }
        int Timestamp { get; }
        SOType Type { get; }

        bool IsTemporary { get; }       // If return true, means object should not be serialized, etc
                                        // Useful for temp parent objects, that kind of thing.

        bool IsSurface { get; }         // does this object have a surface we can use (ie a mesh/etc)

        bool IsSelectable { get; }      // can this SO be selected. Some cannot (eg TransientGroupSO)

		void SetScene(FScene s);
		FScene GetScene();

        void Connect(bool bRestore);          // called when SO is created or restored
        void Disconnect(bool bDestroying);    // called when SO is marked as deleted, or destroyed

        SceneObject Duplicate();

        void SetCurrentTime(double time);   // for keyframing

		void AssignSOMaterial(SOMaterial m);
        SOMaterial GetAssignedSOMaterial();

        void PushOverrideMaterial(fMaterial m);
        void PopOverrideMaterial();
        fMaterial GetActiveMaterial();

        // called on per-frame Update()
        void PreRender();

		bool FindRayIntersection(Ray3f ray, out SORayHit hit);


        /// <summary> Return local bounding box transformed into requested space </summary>
        Box3f GetBoundingBox(CoordSpace eSpace);

        /// <summary> Return local geometry bounding box, before any transforms </summary>
        AxisAlignedBox3f GetLocalBoundingBox();

        event TransformChangedEventHandler OnTransformModified;
    }



    public interface SpatialQueryableSO
    {
        bool SupportsNearestQuery { get; }
        bool FindNearest(Vector3d point, double maxDist, out SORayHit nearest, CoordSpace eInCoords = CoordSpace.WorldCoords);
    }


}

