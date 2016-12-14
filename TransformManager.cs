using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{

    public interface ITransformGizmo : SceneUIElement
    {
        List<TransformableSceneObject> Targets { get; }

        bool SupportsFrameMode { get; }
        FrameType CurrentFrameMode { get; set; }

        bool SupportsReferenceObject { get; }
        void SetReferenceObject(TransformableSceneObject so);
    }

    public interface ITransformGizmoBuilder
    {
        bool SupportsMultipleObjects { get; }
        ITransformGizmo Build(FScene scene, List<TransformableSceneObject> targets);
    }




    public class TransformManager
    {
        public FContext SceneManager { get; set; }

        ITransformGizmoBuilder activeBuilder;
        ITransformGizmo activeGizmo;

        Dictionary<string, ITransformGizmoBuilder> GizmoTypes;
        string sActiveGizmoType;
        string sOverrideGizmoType;

        FrameType defaultFrameType;
        Dictionary<SceneObject, FrameType> lastFrameTypeCache;

        public const string NoGizmoType = "no_gizmo";

        
        public TransformManager()
        {
            activeBuilder = new AxisTransformGizmoBuilder();
            activeGizmo = null;

            GizmoTypes = new Dictionary<string, ITransformGizmoBuilder>();
            RegisterGizmoType(NoGizmoType, new NoGizmoBuilder());
            RegisterGizmoType("default", activeBuilder);
            sActiveGizmoType = "default";

            defaultFrameType = FrameType.LocalFrame;
            lastFrameTypeCache = new Dictionary<SceneObject, FrameType>();

            sOverrideGizmoType = "";
        }

        public void Initialize(FContext manager)
        {
            SceneManager = manager;
            SceneManager.Scene.SelectionChangedEvent += Scene_SelectionChangedEvent;
        }

        

        public void RegisterGizmoType(string sType, ITransformGizmoBuilder builder)
        {
            if (GizmoTypes.ContainsKey(sType))
                throw new ArgumentException("TransformManager.RegisterGizmoType : type " + sType + " already registered!");
            GizmoTypes[sType] = builder;
        }

        public string ActiveGizmoType {
            get { return sActiveGizmoType; }
        }

        public void SetActiveGizmoType(string sType)
        {
            if (sActiveGizmoType == sType)
                return;
            if (GizmoTypes.ContainsKey(sType) == false)
                throw new ArgumentException("TransformManager.SetActiveGizmoType : type " + sType + " is not registered!");

            activeBuilder = GizmoTypes[sType];
            sActiveGizmoType = sType;

            update_gizmo();
        }

        public void SetOverrideGizmoType(string sType)
        {
            if (GizmoTypes.ContainsKey(sType) == false)
                throw new ArgumentException("TransformManager.SetOverrideGizmoType : type " + sType + " is not registered!");
            this.sOverrideGizmoType = sType;
            update_gizmo();
        }
        public void ClearOverrideGizmoType()
        {
            sOverrideGizmoType = "";
            update_gizmo();
        }


        void update_gizmo()
        {
            DismissActiveGizmo();
            Scene_SelectionChangedEvent(null, null);
        }



        public bool HaveActiveGizmo
        {
            get { return activeGizmo != null; }
        }
        public ITransformGizmo ActiveGizmo
        {
            get { return activeGizmo; }
        }

        public event EventHandler OnActiveGizmoModified;
        protected virtual void SendOnActiveGizmoModified() {
            var tmp = OnActiveGizmoModified;
            if (tmp != null)
                tmp(this, new EventArgs());
        }



        public FrameType ActiveFrameType
        {
            get {
                return (activeGizmo != null && activeGizmo.SupportsFrameMode) ? 
                    activeGizmo.CurrentFrameMode : defaultFrameType;
            }
            set {
                if (activeGizmo != null && activeGizmo.SupportsFrameMode) {
                    activeGizmo.CurrentFrameMode = value;
                    if ( activeGizmo.Targets.Count == 1 )
                    lastFrameTypeCache[activeGizmo.Targets[0]] = value;
                    //defaultFrameType = value;       // always change default when we explicitly change type
                    SendOnActiveGizmoModified();
                } else {
                    defaultFrameType = value;
                    SendOnActiveGizmoModified();        // not really right...but UI using it now
                }
            }
        }


        public void SetActiveReferenceObject(TransformableSceneObject so)
        {
            if (activeGizmo != null && activeGizmo.SupportsReferenceObject)
                activeGizmo.SetReferenceObject(so);
        }


        public void DismissActiveGizmo()
        {
            FScene scene = SceneManager.Scene;
            if ( activeGizmo != null ) {
                scene.RemoveUIElement(activeGizmo, true);
                activeGizmo = null;
                SendOnActiveGizmoModified();
            }
        }


        // 
        FrameType initial_frame_type(TransformableSceneObject so)
        {
            return FrameType.LocalFrame;
        }


        public void AddGizmo( List<TransformableSceneObject> targets )
        {
            ITransformGizmoBuilder useBuilder = activeBuilder;
            if (sOverrideGizmoType != null && sOverrideGizmoType != "")
                useBuilder = GizmoTypes[sOverrideGizmoType]; 

            List<TransformableSceneObject> useTargets = new List<TransformableSceneObject>(targets);
            if (useTargets.Count > 0 && useBuilder.SupportsMultipleObjects == false)
                useTargets.RemoveRange(1, useTargets.Count - 1);

            FScene scene = SceneManager.Scene;
             
            // remove existing active gizmo
            // [TODO] support multiple gizmos?
            if (activeGizmo != null) {
                if ( unordered_lists_equal(activeGizmo.Targets, useTargets) )
                    return;     // same targets
                DismissActiveGizmo();
            }

            if (targets != null) {
                activeGizmo = useBuilder.Build(scene, useTargets);

                if (activeGizmo == null)
                    return;

                // set frame type. behavior here is a bit tricky...we have a default frame type
                // and then a cached type for each object. However if we only cache type on explicit
                // user changes, then if user changes default, all other gizmos inherit this default.
                // This is currently a problem because we are also using default frame type to
                // control things like snapping behavior (local=translate+rotate, world=translate-only).
                // So then if we change that, we can change default, which then changes object gizmo 
                // behavior in unexpected ways. So right now we are initializing cache with a per-type
                // default (always Local right now), which user can then change. This "feels" right-est...
                if (activeGizmo.SupportsFrameMode) {
                    if (targets.Count == 1) {
                        if (lastFrameTypeCache.ContainsKey(useTargets[0]) == false)
                            lastFrameTypeCache[useTargets[0]] = initial_frame_type(useTargets[0]);
                        activeGizmo.CurrentFrameMode = lastFrameTypeCache[useTargets[0]];
                    } else
                        activeGizmo.CurrentFrameMode = defaultFrameType;
                }

                scene.AddUIElement(activeGizmo);
                SendOnActiveGizmoModified();
            }
        }



        public static bool unordered_lists_equal<T>(List<T> l1, List<T> l2)
        {
            if (l1.Count != l2.Count)
                return false;
            foreach(T o in l1) {
                if (!l2.Contains(o))
                    return false;
            }
            return true;
        }


        private void Scene_SelectionChangedEvent(object sender, EventArgs e)
        {
            FScene scene = SceneManager.Scene;
            List<TransformableSceneObject> vSelected = new List<TransformableSceneObject>();
            foreach ( SceneObject so in scene.Selected ) {
                TransformableSceneObject tso = so as TransformableSceneObject;
                if (tso != null)
                    vSelected.Add(tso);
            }

            if (vSelected.Count == 0 && activeGizmo != null) {
                // object de-selected, dismiss gizmo
                DismissActiveGizmo();
                return;
            }
            if (activeGizmo != null && unordered_lists_equal(vSelected, activeGizmo.Targets) == false) {
                DismissActiveGizmo();
            }

            if (vSelected.Count > 0)
                AddGizmo(vSelected);
        }
    }




    public class NoGizmoBuilder : ITransformGizmoBuilder
    {
        public bool SupportsMultipleObjects {
            get { return true; }
        }
        public ITransformGizmo Build(FScene scene, List<TransformableSceneObject> targets)
        {
            return null;
        }
    }

}
