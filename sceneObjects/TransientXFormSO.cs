using System;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{
    public class TransientXFormSO : SceneObject, SOCollection
    {
        GameObject gameObject;
        SceneObject target;
        protected SOParent parent;
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

        public void ConnectTarget(SceneObject source, SceneObject target)
        {
            //this.source = source;
            this.target = target;
            Frame3f sourceW = source.GetLocalFrame(CoordSpace.WorldCoords);
            this.SetLocalFrame(sourceW, CoordSpace.WorldCoords);

            target.RootGameObject.SetParent(gameObject, true);

            increment_timestamp();
        }

        public void DisconnectTarget()
        {
            if ( target != null )
                parentScene.ReparentSceneObject(target);
            increment_timestamp();
        }

        //
        // SceneObject impl
        //

        virtual public fGameObject RootGameObject
        {
            get { return gameObject; }
        }

        virtual public SOParent Parent
        {
            get {
                return parent;
            }
            set {
                parent = value;
            }
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

        virtual public void Connect(bool bRestore)
        {
        }
        virtual public void Disconnect(bool bDestroying)
        {
        }

        virtual public bool IsTemporary { 
            get { return true; }
        }

        virtual public bool IsSelectable { 
            get { return false; }
        }

        virtual public bool IsSurface {
            get { return (target != null ) ? target.IsSurface : false; }
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
        public void PushOverrideMaterial(fMaterial m)
        {
        }
        public void PopOverrideMaterial()
        {
        }

        virtual public fMaterial GetActiveMaterial() {
            throw new InvalidOperationException("GroupSO doesn't have its own active material");
        }

        public virtual void PreRender() {
            if ( target != null )
                target.PreRender();
        }


        virtual public AxisAlignedBox3f GetTransformedBoundingBox() {
            return UnityUtil.GetBoundingBox(RootGameObject);
        }
        virtual public AxisAlignedBox3f GetLocalBoundingBox() {
            return (target != null) ? target.GetLocalBoundingBox() : AxisAlignedBox3f.Zero;
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
        virtual public Vector3f GetLocalScale()
        {
            return RootGameObject.GetLocalScale();
        }
        virtual public void SetLocalScale(Vector3f scale)
        {
            if (SupportsScaling) {
                RootGameObject.SetLocalScale(scale);
                increment_timestamp();
                if (OnTransformModified != null)
                    OnTransformModified(this);
            }
        }


        // IParentSO interface

        virtual public IEnumerable<SceneObject> GetChildren()
        {
            if ( target != null )
                return new List<SceneObject>() { target };
            return new List<SceneObject>();
        }
    }
}
