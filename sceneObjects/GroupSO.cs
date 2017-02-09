using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g3;

namespace f3
{
    public class GroupSO : TransformableSO, IParentSO
    {
        GameObject gameObject;
        protected string uuid;
        List<TransformableSO> vChildren;

        FScene parentScene;
        bool defer_origin_update;

        // [TODO] we need to check children timestamps, no??
        int _timestamp = 0;
        protected void increment_timestamp() { _timestamp++; }

        public GroupSO()
        {
            uuid = System.Guid.NewGuid().ToString();
            vChildren = new List<TransformableSO>();
            defer_origin_update = false;
        }
        ~GroupSO()
        {
            //foreach (var so in vChildren)
            //    so.OnTransformModified -= childTransformModified;
        }

        public void Create()
        {
            gameObject = new GameObject(UniqueNames.GetNext("Group"));
            increment_timestamp();
        }


        public void AddChild(TransformableSO so)
        {
            if (!vChildren.Contains(so)) {
                vChildren.Add(so);
                so.RootGameObject.transform.SetParent(gameObject.transform, true);
                update_shared_origin();
                increment_timestamp();
                //so.OnTransformModified += childTransformModified;
            }
        }
        public void AddChildren(IEnumerable<TransformableSO> v)
        {
            defer_origin_update = true;
            foreach (TransformableSO so in v)
                AddChild(so);
            defer_origin_update = false;
            update_shared_origin();
        }

        public void RemoveChild(TransformableSO so)
        {
            if ( vChildren.Contains(so) ) {
                //so.OnTransformModified -= childTransformModified;
                vChildren.Remove(so);
                parentScene.ReparentSceneObject(so);
                update_shared_origin();
                increment_timestamp();
            }
        }

        public void RemoveAllChildren()
        {
            defer_origin_update = true;
            while (vChildren.Count > 0)
                RemoveChild(vChildren[0]);
            defer_origin_update = false;
            update_shared_origin();
        }


        void update_shared_origin()
        {
            if (defer_origin_update)
                return;

            if ( vChildren.Count == 0 ) {
                RootGameObject.transform.position = Vector3.zero;
                return;
            }

            Vector3f origin = Vector3f.Zero;
            foreach (TransformableSO so in vChildren) {
                origin += so.GetLocalFrame(CoordSpace.WorldCoords).Origin;
                so.RootGameObject.transform.SetParent(null);
            }
            origin *= 1.0f / (float)vChildren.Count;
            RootGameObject.transform.position = origin;
            foreach (TransformableSO so in vChildren) {
                so.RootGameObject.transform.SetParent(gameObject.transform, true);
            }
        }


        //
        // SceneObject impl
        //

        public GameObject RootGameObject
        {
            get { return gameObject; }
        }

        public virtual string Name
        {
            get { return gameObject.GetName(); }
            set { gameObject.SetName(value); }
        }

        virtual public SOType Type { get { return SOTypes.Group; } }

        virtual public string UUID
        {
            get { return uuid; }
        }

        virtual public int Timestamp {
            get { return _timestamp; }
        }

        virtual public bool IsTemporary {
            get {
                foreach (var so in vChildren)
                    if (so.IsTemporary == false)
                        return false;
                return true;
            }
        }

        // not really sure what to do here...
        public virtual bool IsSurface {
            get { return true; }
        }

        virtual public SceneObject Duplicate()
        {
            throw new InvalidOperationException("GroupSO::Duplicate not implemented!");
        }

        virtual public void SetCurrentTime(double time)
        {
            foreach (var c in vChildren)
                c.SetCurrentTime(time);
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

        virtual public void AssignSOMaterial(SOMaterial m) {
            foreach (var so in vChildren)
                so.AssignSOMaterial(m);
            increment_timestamp();
        }
        virtual public SOMaterial GetAssignedSOMaterial() {
            throw new InvalidOperationException("GroupSO doesn't have its own SOMaterial...?");
        }

        virtual public void PushOverrideMaterial(Material m)
        {
            foreach (var so in vChildren)
                so.PushOverrideMaterial(m);
            increment_timestamp();
        }
        virtual public void PopOverrideMaterial()
        {
            foreach (var so in vChildren)
                so.PopOverrideMaterial();
            increment_timestamp();
        }
        virtual public Material GetActiveMaterial() {
            throw new InvalidOperationException("GroupSO doesn't have its own active material");
        }

        public virtual void PreRender() {
            foreach (var so in vChildren)
                so.PreRender();
        }


        virtual public AxisAlignedBox3f GetTransformedBoundingBox() {
            return UnityUtil.GetBoundingBox(RootGameObject);
        }
        virtual public AxisAlignedBox3f GetLocalBoundingBox() {
            return SceneUtil.GetLocalBoundingBox(vChildren.Cast<SceneObject>());

        }

        public bool FindRayIntersection(Ray3f ray, out SORayHit hit)
        {
            bool bHit = SceneUtil.FindNearestRayIntersection(vChildren, ray, out hit);
            if (bHit)
                hit.hitSO = this;
            return bHit;
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


        //
        // IParentSO interface
        //
        virtual public IEnumerable<SceneObject> GetChildren()
        {
            return vChildren.Cast<SceneObject>();
        }
    }
}
