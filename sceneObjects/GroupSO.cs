using System;
using System.Collections.Generic;
using System.Linq;
using g3;

namespace f3
{
    /// <summary>
    /// GroupSO enables hierarchies of Scene Objects. It has no geometry
    /// itself, but SOs can be added as children, and then transforms to
    /// the Group apply hierarchically to the children. 
    /// 
    /// Note that this adds lots of complications. Generally using Groups
    /// is not as well-tested as a flat scene of objects. So if you have weird
    /// bugs, try taking the objects out of the Group and see if it still happens!
    /// </summary>
    public class GroupSO : SceneObject, SOCollection, SOParent
    {
        fGameObject parentGO;
        SOParent parent;
        protected string uuid;
        List<SceneObject> vChildren;

        FScene parentScene;
        bool defer_origin_update;

        // [TODO] we need to check children timestamps, no??
        int _timestamp = 0;
        protected void increment_timestamp() { _timestamp++; }


        // enable/disable selection of group, or alternately allow selection
        // of child objects. *however* child object selection is not really
        // supported properly in other places, this is mainly meant for
        // special situations, like selection forwarding, etc
        public enum SelectionModes
        {
            NoSelection, 
            SelectGroup,
            SelectChildren
        }
        public SelectionModes SelectionMode = SelectionModes.SelectGroup;


        /// <summary>
        /// The GroupSO has its own frame and scaling. By default, when we add a child
        /// to the Group, we recompute a shared origin point and shift all children
        /// so that they keep their current positions.
        /// </summary>
        public bool EnableSharedOrigin {
            get { return enable_shared_origin; }
            set {
                if (enable_shared_origin != value) {
                    enable_shared_origin = value;
                    if (enable_shared_origin == true)
                        update_shared_origin();
                }
            }
        }
        bool enable_shared_origin = true;




        public GroupSO()
        {
            uuid = System.Guid.NewGuid().ToString();
            vChildren = new List<SceneObject>();
            defer_origin_update = false;
        }
        ~GroupSO()
        {
            //foreach (var so in vChildren)
            //    so.OnTransformModified -= childTransformModified;
        }

        public void Create()
        {
            parentGO = GameObjectFactory.CreateParentGO(UniqueNames.GetNext("Group"));
            increment_timestamp();
        }


        public void AddChild(SceneObject so, bool bMaintainOrigin = false)
        {
            Util.gDevAssert(so != this);
            if (!vChildren.Contains(so)) {
                vChildren.Add(so);
                if (parentScene != null) {
                    if (so.Parent == null)
                        throw new Exception("GroupSO.AddChild: tried to re-parent SO to group that has no parent!");
                    parentScene.AddSceneObjectToParentSO(so, this);
                }
                if ( bMaintainOrigin == false )
                    update_shared_origin();
                increment_timestamp();
                //so.OnTransformModified += childTransformModified;
            }
        }
        public void AddChildren(IEnumerable<SceneObject> v, bool bMaintainOrigin = false)
        {
            defer_origin_update = true;
            foreach (SceneObject so in v)
                AddChild(so);
            defer_origin_update = false;
            if ( ! bMaintainOrigin )
                update_shared_origin();
        }

        public void RemoveChild(SceneObject so, bool bMaintainOrigin = false)
        {
            if ( vChildren.Contains(so) ) {
                //so.OnTransformModified -= childTransformModified;
                vChildren.Remove(so);
                parentScene.RemoveSceneObjectFromParentSO(so);
                if ( bMaintainOrigin == false )
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


        /// <summary>
        /// TODO: this is a terrible way to implement this. Each time we reparent
        /// an SO it accumulates a little bit of numerical drift, because of the
        /// rotation transformations applied to it. The most immediate way this 
        /// shows up as a problem is that the localScale can drift away from One!!
        /// </summary>
        void update_shared_origin()
        {
            if (enable_shared_origin == false)
                return;
            if (defer_origin_update)
                return;

            if ( vChildren.Count == 0 ) {
                SetLocalFrame(Frame3f.Identity, CoordSpace.SceneCoords);
                return;
            }

            Vector3f origin = Vector3f.Zero;
            foreach (SceneObject so in vChildren) {
                origin += so.GetLocalFrame(CoordSpace.SceneCoords).Origin;
                // remove from any existing parent
                so.GetScene().ReparentSceneObject(so);
            }
            origin *= 1.0f / (float)vChildren.Count;
            SetLocalFrame(new Frame3f(origin), CoordSpace.SceneCoords);
            foreach (SceneObject so in vChildren) {
                so.GetScene().AddSceneObjectToParentSO(so, this);
            }
        }


        //
        // SceneObject impl
        //

        public fGameObject RootGameObject
        {
            get { return parentGO; }
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

        public virtual string Name
        {
            get { return parentGO.GetName(); }
            set { parentGO.SetName(value); }
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


        virtual public void Connect(bool bRestore)
        {
            foreach (var so in vChildren)
                so.Connect(bRestore);
        }
        virtual public void Disconnect(bool bDestroying)
        {
            foreach (var so in vChildren)
                so.Disconnect(bDestroying);
        }

        // not really sure what to do here...
        public virtual bool IsSurface {
            get { return true; }
        }

        virtual public bool IsSelectable { 
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

        virtual public void PushOverrideMaterial(fMaterial m)
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
        virtual public fMaterial GetActiveMaterial() {
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
            return SceneUtil.GetLocalBoundingBox(vChildren);

        }

        public bool FindRayIntersection(Ray3f ray, out SORayHit hit)
        {
            hit = null;
            if (SelectionMode == SelectionModes.NoSelection)
                return false;

            bool bHit = SceneUtil.FindNearestRayIntersection(vChildren, ray, out hit);
            if (bHit && SelectionMode == SelectionModes.SelectGroup )
                hit.hitSO = this;
            return bHit;
        }


        //
        // SceneObject impl
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
                RootGameObject.SetLocalScale( scale );
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
            return vChildren;
        }



        // Need to be able to set UUID during restore. But don't use this. really.
        public void __set_uuid(string new_uuid, string magic_key)
        {
            if (magic_key != "0xDEADBEEF")
                throw new Exception("BaseSO.__set_uuid: are you sure you should be calling this function?");
            uuid = new_uuid;
        }


    }
}
