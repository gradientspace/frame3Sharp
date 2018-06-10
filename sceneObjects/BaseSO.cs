using System;
using System.Collections.Generic;
using System.Linq;
using g3;

namespace f3
{
    public abstract class BaseSO : GameObjectSet, SceneObject, InputBehaviorSource
    {
        protected FScene parentScene;
        protected SOParent parent;
        protected string uuid;

        SOMaterial sceneMaterial;
        fMaterial displaySceneMaterial;

        fMaterial displayMaterial;
        List<fMaterial> vMaterialStack;

        int _timestamp = 0;
        protected void increment_timestamp() { _timestamp++; }

        protected InputBehaviorSet inputBehaviours;

        public BaseSO()
        {
            uuid = System.Guid.NewGuid().ToString();
            displayMaterial = null;
            vMaterialStack = new List<fMaterial>();
            inputBehaviours = new InputBehaviorSet();
        }

        //
        // SceneObject functions that subclass must implement
        //
        abstract public fGameObject RootGameObject { get; }
        abstract public string Name { get; set; }
        abstract public SOType Type { get; }
        abstract public SceneObject Duplicate();


        //
        // SceneObject impl
        //
        virtual public string UUID {
            get { return uuid; }
        }

        virtual public int Timestamp{
            get { return _timestamp; }
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


        virtual public void Connect(bool bRestore)
        {
        }
        virtual public void Disconnect(bool bDestroying)
        {
        }


        virtual public bool IsTemporary {
            get { return false; }
        }

        virtual public bool IsSelectable { 
            get { return true; }
        }

        virtual public bool IsSurface {
            get { throw new NotImplementedException("BaseSO.IsSurface"); }
        }

        virtual public FScene GetScene()
        {
            return parentScene;
        }
        virtual public void SetScene(FScene s)
        {
            parentScene = s;
        }

        virtual public void OnSelected() {
        }
        virtual public void OnDeselected() {
        }

        virtual public void SetCurrentTime(double time)
        {
            // ignore
        }


        virtual protected void set_material_internal(fMaterial m)
        {
            SetAllGOMaterials(m);
        }


        virtual public void AssignSOMaterial(SOMaterial m) {
            sceneMaterial = m;
            displaySceneMaterial = MaterialUtil.ToMaterialf(m);
            if (vMaterialStack.Count > 0) {
                // material 0 is always our base material, higher levels of stack are
                // temp materials that will be popped eventually
                vMaterialStack[0] = displaySceneMaterial;
            } else { 
                displayMaterial = displaySceneMaterial;
                set_material_internal(displayMaterial);
            }
            increment_timestamp();
        }
        virtual public SOMaterial GetAssignedSOMaterial() {
            return sceneMaterial;
        }

        virtual public void PushOverrideMaterial(fMaterial m) {
            vMaterialStack.Add(displayMaterial);
            displayMaterial = m;
            set_material_internal(displayMaterial);
            increment_timestamp();
        }

        virtual public void PopOverrideMaterial() {
            if (vMaterialStack.Count > 0) {
                fMaterial m = vMaterialStack.Last();
                vMaterialStack.RemoveAt(vMaterialStack.Count - 1);
                displayMaterial = m;
                set_material_internal(displayMaterial);
                increment_timestamp();
            }
        }

        virtual public fMaterial GetActiveMaterial() {
            return CurrentMaterial;
        }

        virtual public fMaterial CurrentMaterial {
            get { return displayMaterial; }
        }


        virtual public bool ShadowsEnabled {
            get { return false; }
        }
        virtual public void DisableShadows() {
            throw new NotImplementedException("BaseSO.DisableShadows: must be implemented by subclasses");
        }

        virtual public int Layer {
            get { return RootGameObject.GetLayer(); }
        }
        virtual public void SetLayer(int nLayer)
        {
            RootGameObject.SetLayer(nLayer);
            this.SetAllGOLayer(nLayer);
        }

        public virtual void PreRender() {
            // nothing
        }


        virtual public Box3f GetBoundingBox(CoordSpace eSpace)
        {
            Box3f box = new Box3f(GetLocalBoundingBox());
            if (eSpace == CoordSpace.ObjectCoords) {
                return box;
            } else if (eSpace == CoordSpace.SceneCoords) {
                return SceneTransforms.ObjectToScene(this, box);
            } else {
                box = SceneTransforms.ObjectToScene(this, box);
                return GetScene().ToWorldBox(box);
            }
        }
        virtual public AxisAlignedBox3f GetLocalBoundingBox()
        {
            // [RMS] is this correct?
            DebugUtil.Log("BaseSO.GetLocalBoundingBox: this bounding box may be incorrect...check this usage");
            return UnityUtil.GetGeometryBoundingBox(RootGameObject, true);
        }


        virtual public bool FindRayIntersection(Ray3f ray, out SORayHit hit)
        {
            hit = null;
            GameObjectRayHit hitg = null;
            if (FindGORayIntersection(ray, out hitg)) {
                if (hitg.hitGO != null) {
                    hit = new SORayHit(hitg, this);
                    return true;
                }
            }
            return false;
        }


        //
        // TransformableSceneObject impl
        //
        public event TransformChangedEventHandler OnTransformModified;

        virtual public Frame3f GetLocalFrame(CoordSpace eSpace) {
            return SceneUtil.GetSOLocalFrame(this,eSpace);
        }

        virtual public void SetLocalFrame(Frame3f newFrame, CoordSpace eSpace) {
            SceneUtil.SetSOLocalFrame(this, eSpace, newFrame);
            increment_timestamp();
            if (OnTransformModified != null)
                OnTransformModified(this);
        }


        virtual public bool SupportsScaling {
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



        /*
         *  InputBehaviour bits
         */
        public virtual InputBehaviorSet InputBehaviors {
            get { return inputBehaviours; }
        }

        protected virtual void initialize_behaviors()
        {
        }



        //
        // transform utility functions
        //   [TODO] use f3 code to do this, rather than unity xform functions!
        //
        virtual public Vector3f ToWorldP(Vector3f ptLocal) {
            Vector3f vW = this.RootGameObject.PointToWorld(ptLocal);
            return vW;
        }
        virtual public Vector3f ToLocalP(Vector3f ptWorld) {
            Vector3f vL = this.RootGameObject.PointToLocal(ptWorld);
            return vL;
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
