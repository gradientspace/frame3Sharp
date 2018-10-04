using System;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{
    //
    // This is a boilerplate Gizmo class that assumes the Gizmo is composed
    // of a set of Widget objects that support hover, and the point of the Gizmo
    // is to transform one or more Target SOs
    //
    abstract public class BaseTransformGizmo : GameObjectSet, ITransformGizmo
    {
        protected fGameObject gizmo;

        protected SceneUIParent parent;
        protected FScene parentScene;
        protected List<SceneObject> targets;
        protected ITransformWrapper targetWrapper;

        protected Dictionary<fGameObject, Standard3DWidget> Widgets;
        protected Standard3DWidget activeWidget;
        protected Standard3DWidget hoverWidget;

        bool is_interactive = true;

        // if false, transform changes will not be emitted
        public bool EmitChanges = true;

        virtual public fGameObject RootGameObject {
            get { return gizmo; }
        }
        virtual public List<SceneObject> Targets {
            get { return targets; }
            set { Debug.Assert(false, "not implemented!"); }
        }
        virtual public FScene Scene {
            get { return parentScene;  }
        }

		public virtual SceneUIParent Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public virtual string Name
        {
            get { return RootGameObject.GetName(); }
            set { RootGameObject.SetName(value); }
        }

        public BaseTransformGizmo()
        {
            Widgets = new Dictionary<fGameObject, Standard3DWidget>();
        }


        public virtual void OnTransformInteractionStart()
        {
            // this is called when the user starts an interactive transform change
            // via the gizmo (eg mouse-down). Override to customize behavior.
        }
        public virtual void OnTransformInteractionEnd()
        {
            // this is called when the user ends an interactive transform change
            // via the gizmo (eg mouse-up). Override to customize behavior.
        }



        virtual public void Disconnect()
        {
            // we could get this while we are in an active capture, if selection
            // changes. in that case we need to terminate gracefully.
            if (activeWidget != null)
                EndCapture(null);

            if (targetWrapper != null)
                targetWrapper.Target.OnTransformModified -= onTransformModified;

            FUtil.SafeSendEvent(OnDisconnected, this, EventArgs.Empty);
        }
        public event EventHandler OnDisconnected;


        public virtual bool IsVisible {
            get { return RootGameObject.IsVisible(); }
            set { RootGameObject.SetVisible(value); }
        }

        public virtual bool IsInteractive {
            get { return is_interactive; }
            set { is_interactive = value; }
        }

        // [TODO] why isn't this in GameObjectSet?
        virtual public void SetLayer(int nLayer)
        {
            foreach (var go in GameObjects)
                go.SetLayer(nLayer);
        }


        virtual public void PreRender()
        {
            // do nothing!
        }



        virtual public ITransformGizmo Create(FScene parentScene, List<SceneObject> targets)
        {
            this.parentScene = parentScene;
            this.targets = targets;
            gizmo = new GameObject("BaseGizmo");

            BuildGizmo();

            // disable shadows on widget components
            foreach (var go in GameObjects)
                MaterialUtil.DisableShadows(go);

            // set layer
            SetLayer(FPlatform.WidgetOverlayLayer);

            InitializeTargetWrapper();
            targetWrapper.Target.OnTransformModified += onTransformModified;
            onTransformModified(null);

            return this;
        }


        // this is the only function you should really need to implement
        abstract protected void BuildGizmo();

        // you can override this with your own targetWrapper setup code
        virtual protected void InitializeTargetWrapper()
        {
            Debug.Assert(this.targets.Count == 1);
            targetWrapper = new PassThroughWrapper(this.targets[0]);
        }



        //
        // Boilerplate stuff
        //


        virtual protected void onTransformModified(SceneObject so) {
            // keep widget synced with object frame of target
            Frame3f widgetFrame = targetWrapper.GetLocalFrame(CoordSpace.ObjectCoords);
            gizmo.SetLocalPosition(widgetFrame.Origin);
            gizmo.SetLocalRotation(widgetFrame.Rotation);
        }


        virtual public bool FindRayIntersection(Ray3f ray, out UIRayHit hit)
        {
            hit = null;
            GameObjectRayHit hitg = null;
            if (is_interactive && FindGORayIntersection(ray, out hitg)) {
                if (hitg.hitGO != null) {
                    hit = new UIRayHit(hitg, this);
                    return true;
                }
            }
            return false;
        }
        public bool FindHoverRayIntersection(Ray3f ray, out UIRayHit hit)
        {
            return FindRayIntersection(ray, out hit);
        }



        // these are called in functions below - easier to override to customize behavior
        // Note that Begin will be called after wrapper.BeginTransformation, and
        // End will be called before wrapper.EndTransformation
        virtual protected void OnBeginCapture(Ray3f worldRay, Standard3DWidget w)
        {
        }
        virtual protected void OnUpdateCapture(Ray3f worldRay, Standard3DWidget w)
        {
        }
        virtual protected void OnEndCapture(Ray3f worldRay, Standard3DWidget w)
        {
        }

        virtual public bool WantsCapture(InputEvent e)
        {
            return Widgets.ContainsKey(e.hit.hitGO);
        }

        virtual public bool BeginCapture(InputEvent e)
        {
            activeWidget = null;

            // if the hit gameobject has a widget attached to it, begin capture & transformation
            // TODO maybe wrapper class should have Begin/Update/End capture functions, then we do not need BeginTransformation/EndTransformation ?
            if (Widgets.ContainsKey(e.hit.hitGO)) {
                Standard3DWidget w = Widgets[e.hit.hitGO];
                if (w.BeginCapture(Targets[0], e.ray, e.hit.toUIHit())) {
                    MaterialUtil.SetMaterial(w.RootGameObject, w.HoverMaterial);
                    targetWrapper.BeginTransformation();
                    activeWidget = w;
                    OnBeginCapture(e.ray, activeWidget);
                    OnTransformInteractionStart();
                    return true;
                }
            }
            return false;
        }

        virtual public bool UpdateCapture(InputEvent e)
        {
            // update capture if we have an active widget
            if (activeWidget != null) {

                // [TODO] can remove this once we fix test/begin capture
                MaterialUtil.SetMaterial(activeWidget.RootGameObject, activeWidget.HoverMaterial);
                activeWidget.UpdateCapture(targetWrapper, e.ray);
                OnUpdateCapture(e.ray, activeWidget);
                return true;
            }
            return false;
        }

        virtual public bool EndCapture(InputEvent e)
        {
            if (activeWidget != null) {
                MaterialUtil.SetMaterial(activeWidget.RootGameObject, activeWidget.StandardMaterial);

                // update widget frame in case we want to do something like stay scene-aligned...
                activeWidget.EndCapture(targetWrapper);

                // note: e will be null if we call this from Disconnect(), because we were in an
                //   active capture when we were Disconnected. Not sure how to handle this gracefully
                //   in subclasses...could pass e directly to OnEndCapture? pass a flag?
                if ( e != null)
                    OnEndCapture(e.ray, activeWidget);

                // tell wrapper we are done with capture, so it should bake transform/etc
                bool bModified = targetWrapper.DoneTransformation(EmitChanges);
                if (bModified) {
                    // update gizmo
                    onTransformModified(null);
                    // allow client/subclass to add any other change records
                    OnTransformInteractionEnd();
                    // gizmos drop change events by default
                    if (EmitChanges)
                        Scene.History.PushInteractionCheckpoint();
                }

            }

            activeWidget = null;
            return true;
        }



        public virtual bool EnableHover
        {
            get { return is_interactive; }
        }
        public virtual void UpdateHover(Ray3f ray, UIRayHit hit)
        {
            if (hoverWidget != null)
                EndHover(ray);
            if (Widgets.ContainsKey(hit.hitGO)) {
                hoverWidget = Widgets[hit.hitGO];
                MaterialUtil.SetMaterial(hoverWidget.RootGameObject, hoverWidget.HoverMaterial);
            }
        }
        public virtual void EndHover(Ray3f ray)
        {
            if (hoverWidget != null) {
                MaterialUtil.SetMaterial(hoverWidget.RootGameObject, hoverWidget.StandardMaterial);
                hoverWidget = null;
            }
        }



        //
        // ITransformGizmo impl
        //  (base does not implement these)
        //

        virtual public FrameType CurrentFrameMode { get; set; }
        virtual public bool SupportsFrameMode { get { return false; } }


        virtual public bool SupportsReferenceObject { get { return false; } }
        virtual public void SetReferenceObject(SceneObject sourceSO) { }

    }

}
