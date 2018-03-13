using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{

    public interface ITransformGizmo : SceneUIElement
    {
        List<SceneObject> Targets { get; }

        bool SupportsFrameMode { get; }
        FrameType CurrentFrameMode { get; set; }

        bool SupportsReferenceObject { get; }
        void SetReferenceObject(SceneObject so);
    }

    public interface ITransformGizmoBuilder
    {
        bool SupportsMultipleObjects { get; }
        ITransformGizmo Build(FScene scene, List<SceneObject> targets);
    }



    /// <summary>
    /// TransformManager handles Gizmos, like 3-axis transform gizmo, resizing handles, etc.
    /// Basically these are 3D widgets that appear on selection, rather than
    /// on starting a tool. For each Gizmo, a Builder is associated with a string, and then
    /// the current default Gizmo can be selected via string.
    /// 
    /// When you would like to modify or disable the default Gizmos, eg inside a Tool, you can 
    /// use  PushOverrideGizmoType/Pop. TransformManager.NoGizmoType is a built-in gizmo type
    /// that does nothing, for this purpose.
    /// 
    /// To customize which gizmo appears for specific objects, pass GizmoTypeFilter 
    /// objects to AddTypeFilter(). Note that the current Override gizmo type still takes 
    /// precedence over the filtered gizmo type.
    /// 
    /// [TODO] currently type filters are only considered if there is a single selected SO
    /// 
    /// To just limit which SO's can be considered for a gizmo, use SetSelectionFilter()
    /// 
    /// </summary>
    public class TransformManager
    {
        public FContext Context { get; set; }

        ITransformGizmoBuilder activeBuilder;
        ITransformGizmo activeGizmo;

        Dictionary<string, ITransformGizmoBuilder> GizmoTypes;
        string sActiveGizmoType;

        string sOverrideGizmoType;
        List<string> OverrideGizmoStack = new List<string>();


        /// <summary>
        /// Used to replace default gizmo tpe - see AddTypeFilter()
        /// </summary>
        public class GizmoTypeFilter
        {
            /// <summary> return Gizmo type name string, or null </summary>
            public Func<SceneObject, string> FilterF;
        }
        List<GizmoTypeFilter> ObjectFilters = new List<GizmoTypeFilter>();


        FrameType defaultFrameType;
        Dictionary<SceneObject, FrameType> lastFrameTypeCache;

        public const string NoGizmoType = "no_gizmo";
        public const string DefaultGizmoType = "default";

        Func<SceneObject, bool> SelectionFilterF = null;


        public TransformManager( ITransformGizmoBuilder defaultBuilder )
        {
            activeBuilder = defaultBuilder;
            activeGizmo = null;

            GizmoTypes = new Dictionary<string, ITransformGizmoBuilder>();
            RegisterGizmoType(NoGizmoType, new NoGizmoBuilder());
            RegisterGizmoType(DefaultGizmoType, activeBuilder);
            sActiveGizmoType = DefaultGizmoType;

            defaultFrameType = FrameType.LocalFrame;
            lastFrameTypeCache = new Dictionary<SceneObject, FrameType>();

            ObjectFilters = new List<GizmoTypeFilter>();

            sOverrideGizmoType = "";
        }

        public void Initialize(FContext manager)
        {
            Context = manager;
            Context.Scene.SelectionChangedEvent += Scene_SelectionChangedEvent;
        }

        
        /// <summary>
        /// Associate a new gizmo builder with an identifier
        /// </summary>
        public void RegisterGizmoType(string sType, ITransformGizmoBuilder builder)
        {
            if (GizmoTypes.ContainsKey(sType))
                throw new ArgumentException("TransformManager.RegisterGizmoType : type " + sType + " already registered!");
            GizmoTypes[sType] = builder;
        }

        /// <summary>
        /// Current active default gizmo type/builder
        /// </summary>
        public string ActiveGizmoType {
            get { return sActiveGizmoType; }
        }

        /// <summary>
        /// Select the current default gizmo type, using identifier passed to RegisterGizmoType()
        /// </summary>
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


        /// <summary>
        /// Temporarily override the current active gizmo type
        /// </summary>
        public void PushOverrideGizmoType(string sType)
        {
            if (GizmoTypes.ContainsKey(sType) == false)
                throw new ArgumentException("TransformManager.SetOverrideGizmoType : type " + sType + " is not registered!");
            if (OverrideGizmoStack.Count > 10)
                throw new Exception("TransformManager.PushOverrideGizmoType: stack is too large, probably a missing pop?");

            OverrideGizmoStack.Add(this.sOverrideGizmoType);
            this.sOverrideGizmoType = sType;

            //update_gizmo();
            Context.RegisterNextFrameAction(update_gizmo);
        }

        /// <summary>
        /// Pop the override gizmo type stack
        /// </summary>
        public void PopOverrideGizmoType()
        {
            if (OverrideGizmoStack.Count == 0)
                throw new Exception("TransformManager.PopOverrideGizmoType: tried to pop empty stack!");

            this.sOverrideGizmoType = OverrideGizmoStack[OverrideGizmoStack.Count - 1];
            OverrideGizmoStack.RemoveAt(OverrideGizmoStack.Count - 1);

            // [RMS] defer this update to next frame, as we often do this inside a Tool
            //  and we should not immediately initialize gizmo...
            //update_gizmo();
            Context.RegisterNextFrameAction(update_gizmo);
        }

        /// <summary>
        /// Pop all pushed override gizmo types
        /// </summary>
        public void PopAllOverrideGizmos()
        {
            while (OverrideGizmoStack.Count > 0) {
                sOverrideGizmoType = OverrideGizmoStack[OverrideGizmoStack.Count - 1];
                OverrideGizmoStack.RemoveAt(OverrideGizmoStack.Count - 1);
            }
            Context.RegisterNextFrameAction(update_gizmo);
        }


        /// <summary>
        /// When the selection filter is set, only objects where filterF(so) == true
        /// will be given the current gizmo.
        /// </summary>
        public void SetSelectionFilter(Func<SceneObject, bool> filterF)
        {
            SelectionFilterF = filterF;
        }

        /// <summary>
        /// Discard current selection filter
        /// </summary>
        public void ClearSelectionFilter()
        {
            SelectionFilterF = null;
        }


        /// <summary>
        /// TypeFilters will checked each time the selection changes. If the filter returns
        /// a gizmo type identifier, we'll use that instead of the current default.
        /// However override gizmos will still take precedence.
        /// </summary>
        public void AddTypeFilter(GizmoTypeFilter filter)
        {
            ObjectFilters.Add(filter);
        }

        /// <summary>
        /// remove a previously-registered gizmo type filter
        /// </summary>
        public void RemoveTypeFilter(GizmoTypeFilter filter)
        {
            ObjectFilters.Remove(filter);
        }

        /// <summary>
        /// remove all registered gizmo type filters
        /// </summary>
        public void ClearAllTypeFilters()
        {
            ObjectFilters.Clear();
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


        public void SetActiveReferenceObject(SceneObject so)
        {
            if (activeGizmo != null && activeGizmo.SupportsReferenceObject)
                activeGizmo.SetReferenceObject(so);
        }


        public void DismissActiveGizmo()
        {
            FScene scene = Context.Scene;
            if ( activeGizmo != null ) {
                scene.RemoveUIElement(activeGizmo, true);
                activeGizmo = null;
                SendOnActiveGizmoModified();
            }
        }


        // 
        FrameType initial_frame_type(SceneObject so)
        {
            return FrameType.LocalFrame;
        }



        protected void AddGizmo( List<SceneObject> targets )
        {
            // current default active gizmo builder
            ITransformGizmoBuilder useBuilder = activeBuilder;

            // apply gizmo type filters
            // [TODO] support multiple here?
            if (targets.Count == 1 && ObjectFilters.Count > 0) {
                foreach (GizmoTypeFilter filter in ObjectFilters) {
                    string typename = filter.FilterF(targets[0]);
                    if (typename != null && GizmoTypes.ContainsKey(typename) == true)
                        useBuilder = GizmoTypes[typename];
                }
            }

            // apply overrides
            if (sOverrideGizmoType != null && sOverrideGizmoType != "")
                useBuilder = GizmoTypes[sOverrideGizmoType];

            // filter target count if builder only supports single object
            List<SceneObject> useTargets = new List<SceneObject>(targets);
            if (useTargets.Count > 0 && useBuilder.SupportsMultipleObjects == false)
                useTargets.RemoveRange(1, useTargets.Count - 1);
            
            // remove existing active gizmo
            // [TODO] support multiple gizmos?
            if (activeGizmo != null) {
                if ( unordered_lists_equal(activeGizmo.Targets, useTargets) )
                    return;     // same targets
                DismissActiveGizmo();
            }

            if (targets != null) {
                activeGizmo = useBuilder.Build(Context.Scene, useTargets);

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

                Context.Scene.AddUIElement(activeGizmo);
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
            FScene scene = Context.Scene;
            List<SceneObject> vSelected = new List<SceneObject>();
            foreach ( SceneObject tso in scene.Selected ) {
                if (tso != null) {
                    if ( SelectionFilterF == null || SelectionFilterF(tso) )
                        vSelected.Add(tso);
                }
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
                Context.RegisterNextFrameAction(add_gizmo_next_frame);
                //AddGizmo(vSelected);
        }



        private void add_gizmo_next_frame()
        {
            FScene scene = Context.Scene;
            List<SceneObject> vSelected = new List<SceneObject>();
            foreach (SceneObject tso in scene.Selected) {
                if (tso != null) {
                    if (SelectionFilterF == null || SelectionFilterF(tso))
                        vSelected.Add(tso);
                }
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
        public ITransformGizmo Build(FScene scene, List<SceneObject> targets)
        {
            return null;
        }
    }

}
