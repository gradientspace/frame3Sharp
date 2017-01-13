using System;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{
    public class TransientXFormSO : TransformableSceneObject, IParentSO
    {
        GameObject gameObject;
        TransformableSceneObject target;
        //TransformableSceneObject source;

        FScene parentScene;

        int _timestamp = 0;
        protected void increment_timestamp() { _timestamp++; }

        public TransientXFormSO()
        {
        }

        public void Create()
        {
            gameObject = new GameObject(UniqueNames.GetNext("TransientXForm"));
            increment_timestamp();
        }

        public void Connect(TransformableSceneObject source, TransformableSceneObject target)
        {
            //this.source = source;
            this.target = target;
            Frame3f sourceW = source.GetLocalFrame(CoordSpace.WorldCoords);
            this.SetLocalFrame(sourceW, CoordSpace.WorldCoords);

            target.RootGameObject.transform.SetParent(gameObject.transform, true);

            increment_timestamp();
        }

        public void Disconnect()
        {
            parentScene.ReparentSceneObject(target);
            increment_timestamp();
        }

        //
        // SceneObject impl
        //

        public GameObject RootGameObject
        {
            get { return gameObject; }
        }

        virtual public string Name
        {
            get { return gameObject.GetName(); }
            set { gameObject.SetName(value); increment_timestamp(); }
        }

        // [RMS] not sure this is the right thing to do...
        virtual public SOType Type { get { return SOTypes.Unknown; } }

        virtual public string UUID
        {
            get { return SceneUtil.InvalidUUID; }
        }

        virtual public int Timestamp {
            get { return _timestamp; }
        }

        virtual public bool IsTemporary { 
            get { return true; }
        }

        virtual public bool IsSurface {
            get { return target.IsSurface; }
        }

        public SceneObject Duplicate()
        {
            throw new InvalidOperationException("TransientXFormSO::Duplicate not implemented!");
        }

        public void SetCurrentTime(double time)
        {
            // nothing
        }

        public void SetScene(FScene s)
        {
            parentScene = s;
            increment_timestamp();
        }
        public FScene GetScene()
        {
            return parentScene;
        }

        public void AssignSOMaterial(SOMaterial m)
        {
            throw new InvalidOperationException("TransientXFormSO should not be assigning material");
        }
        public SOMaterial GetAssignedSOMaterial()
        {
            throw new InvalidOperationException("TransientXFormSO doesn't have its own assigned material");
        }
        public void PushOverrideMaterial(Material m)
        {
        }
        public void PopOverrideMaterial()
        {
        }

        virtual public Material GetActiveMaterial() {
            throw new InvalidOperationException("GroupSO doesn't have its own active material");
        }

        public virtual void PreRender() {
            target.PreRender();
        }


        virtual public Bounds GetTransformedBoundingBox() {
            return UnityUtil.GetBoundingBox(RootGameObject);
        }
        virtual public Bounds GetLocalBoundingBox() {
            return target.GetLocalBoundingBox();
        }

        public bool FindRayIntersection(Ray3f ray, out SORayHit hit)
        {
            hit = null;
            return false;
        }


        //
        // TransformableSceneObject impl
        //
        public event TransformChangedEventHandler OnTransformModified;

        virtual public Frame3f GetLocalFrame(CoordSpace eSpace) {
            return SceneUtil.GetSOLocalFrame(this, eSpace);
        }

        virtual public void SetLocalFrame(Frame3f newFrame, CoordSpace eSpace) {
            SceneUtil.SetSOLocalFrame(this, eSpace, newFrame);
            increment_timestamp();
            if (OnTransformModified != null)
                OnTransformModified(this);
        }

        virtual public bool SupportsScaling
        {
            get { return true; }
        }
        virtual public Vector3 GetLocalScale()
        {
            return RootGameObject.transform.localScale;
        }
        virtual public void SetLocalScale(Vector3 scale)
        {
            if (SupportsScaling) {
                RootGameObject.transform.localScale = scale;
                increment_timestamp();
                if (OnTransformModified != null)
                    OnTransformModified(this);
            }
        }


        // IParentSO interface

        virtual public IEnumerable<SceneObject> GetChildren()
        {
            return new List<SceneObject>() { target };
        }
    }
}
