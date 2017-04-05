using System;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{

    public interface ICockpitViewTracker
    {
        void UpdateTracking(Cockpit cockpit, Camera camera);
    }


	public class Cockpit : SceneUIParent
	{
		FContext context;
		GameObject gameobject;

        // additional UI behavior attached to this cockpit
        public InputBehaviorSet InputBehaviors { get; set; }

        // special behaviors that are not part of regular capture-device handling
        public InputBehaviorSet OverrideBehaviors { get; set; }

        public GenericAnimator HUDAnimator { get; set; }

        List<SceneUIElement> vUIElements;
        public int UIElementLayer { get; set; }     // Layer that UIElements will be placed in. default is FPlatform.HUDLayer

        List<IShortcutKeyHandler> vKeyHandlers;


        Dictionary<string, ILayoutEngine> Layouts;
        ILayoutEngine defaultLayout;


        public enum MovementMode
        {
            Static = 0,
            TrackPosition = 1,
            TrackOrientation = 2,
            CustomTracking = 3
        }
        public MovementMode PositionMode { get; set; }
        public ICockpitViewTracker CustomTracker { get; set; }

        bool bStaticUpdated;

        // [TODO] should be using this to restrict input but nobody respects it...
        public bool GrabFocus { get; set; }

        // this is up/down angle from straight-forward, that we apply after any tracking rotation
        public float TiltAngle { get; set; }

        // this is l/r angle from straight-forward, that we apply after any tracking rotation
        public float ShiftAngle { get; set; }


        // use to control depth of no-hit cursor (ie control focal plane)
        //  default is -1, ie ignore this value
        public float DefaultCursorDepth { get; set; }


        public Cockpit(FContext context)
		{
            PositionMode = MovementMode.TrackPosition;
            GrabFocus = false;
            bStaticUpdated = false;

            this.context = context;
			vUIElements = new List<SceneUIElement> ();
            UIElementLayer = FPlatform.HUDLayer;

            Layouts = new Dictionary<string, ILayoutEngine>();
            defaultLayout = null;

            vKeyHandlers = new List<IShortcutKeyHandler>();
            InputBehaviors = new InputBehaviorSet();
            OverrideBehaviors = new InputBehaviorSet();
            HUDAnimator = new GenericAnimator();

            TiltAngle = ShiftAngle = 0.0f;
            DefaultCursorDepth = -1;
        }

        public void Destroy()
        {
            while (vUIElements.Count > 0)
                RemoveUIElement(vUIElements[0], true);
            RootGameObject.transform.parent = null;
            UnityEngine.Object.Destroy(RootGameObject);
        }


        public string Name
        {
            get { return RootGameObject.GetName(); }
            set { RootGameObject.SetName(value); }
        }

		public FContext Context {
			get { return context; }
		}
        public FScene Scene {
            get { return Context.Scene; }
        }
        public Camera ActiveCamera {
            get { return Context.ActiveCamera; }
        }
        public GameObject RootGameObject {
			get { return gameobject; }
		}
        public bool IsActive {
            get { return Context.ActiveCockpit == this; }
        }

		// cockpit frame is oriented such that
		//    +X is right
		//    +Y is up
		//    +Z is into scene
		// note that for most unity mesh objects are created on the XZ plane, and so you 
		// need to rotate world_y to point to -cockpit_z, ie Quaternion.FromToRotation (Vector3.up, -cockpitF.Z)
		public virtual Frame3f GetLocalFrame(CoordSpace eSpace) 
		{
			return UnityUtil.GetGameObjectFrame (gameobject, eSpace);
		}
		public virtual void SetLocalFrame(Frame3f newFrame, CoordSpace eSpace)
		{
            UnityUtil.SetGameObjectFrame (gameobject, newFrame, eSpace);
		}

        // same as LocalFrame, but LocalFrame may be tilted up/down, which we want to undo
        public virtual Frame3f GetLevelViewFrame(CoordSpace eSpace)
        {
            // otherwise we can't just set y to 0...
            Debug.Assert(eSpace == CoordSpace.WorldCoords);
            Frame3f viewF = UnityUtil.GetGameObjectFrame(ActiveCamera.gameObject, eSpace);
            float fAngle = VRUtil.PlaneAngleSigned(Vector3.forward, viewF.Z, 1);
            return new Frame3f(viewF.Origin, Quaternion.AngleAxis(fAngle, Vector3.up));
        }

        // frame centered at cockpit location and aligned with camera direction
        public virtual Frame3f GetViewFrame2D()
        {
            return new Frame3f(Vector3f.Zero, ActiveCamera.transform.rotation);
        }


        // get bounds of current orthographic camera view, in world coordinates (ie
        // box.Min is bottom-left corner of screen and box.Max is top-right
        // [TODO] move this elsewhere?? 
        public virtual AxisAlignedBox2f GetOrthoViewBounds()
        {
            if (context.OrthoUICamera == null)
                return AxisAlignedBox2f.Empty;
            float verticalSize = ((Camera)context.OrthoUICamera).orthographicSize * 2.0f;
            float horizontalSize = verticalSize * (float)Screen.width / (float)Screen.height;
            return new AxisAlignedBox2f(-horizontalSize / 2, -verticalSize / 2, horizontalSize / 2, verticalSize / 2);
        }

        public virtual AxisAlignedBox2f GetPixelViewBounds()
        {
            if (context.OrthoUICamera == null)
                return AxisAlignedBox2f.Empty;
            return new AxisAlignedBox2f(0, 0, Screen.width, Screen.height);
        }

        public float GetPixelScale()
        {
            AxisAlignedBox2f uiBounds = GetOrthoViewBounds();
            AxisAlignedBox2f pixelBounds = GetPixelViewBounds();
            float fScale = uiBounds.MaxDim / pixelBounds.MaxDim;
            if (FPlatform.InUnityEditor())
                fScale *= FPlatform.EditorUIScaleFactor;
            else
                fScale *= FPlatform.UIScaleFactor;
            return fScale;
        }


        // these should be called by parent Unity functions
        public void Start( ICockpitInitializer setup )
		{
			// create invisible plane for cockpit
			gameobject = GameObject.CreatePrimitive (PrimitiveType.Plane);
			gameobject.SetName("cockpit");
			MeshRenderer ren = gameobject.GetComponent<MeshRenderer> ();
			ren.enabled = false;
			gameobject.GetComponent<MeshCollider> ().enabled = false;

            // add hud animation controller
            gameobject.AddComponent<UnityPerFrameAnimationBehavior>().Animator = HUDAnimator;                

            // create HUD
            try {
                setup.Initialize(this);
            } catch ( Exception e ) {
                DebugUtil.Log(2, "[Cockpit.Start] exception in initializer: {0}\nTrace:\n{1}", e.Message,
                    e.StackTrace);
                // if hud setup fails we still want to keep going
            }

            // position in front of camera
            UpdateTracking(true);
        }



        public void Update()
        {
            UpdateTracking(false);
        }

        public void PreRender()
        {
            foreach (SceneUIElement ui in vUIElements)
                ui.PreRender();
        }

        public void InitializeTracking(Cockpit fromExisting)
        {
            RootGameObject.transform.position = fromExisting.RootGameObject.transform.position;
            RootGameObject.transform.rotation = fromExisting.RootGameObject.transform.rotation;
        }

        void UpdateTracking(bool bSetInitial)
        {
            if (bSetInitial || PositionMode == MovementMode.Static) {
                // [TODO] should damp out jitter while allowing larger moves
                if (bStaticUpdated == false) {
                    RootGameObject.transform.position = ActiveCamera.transform.position;
                    bStaticUpdated = true;
                }

            } else if (PositionMode == MovementMode.TrackOrientation) {
                RootGameObject.transform.position = ActiveCamera.transform.position;
                RootGameObject.transform.rotation = ActiveCamera.transform.rotation;

            } else if (PositionMode == MovementMode.CustomTracking && CustomTracker != null ) {
                CustomTracker.UpdateTracking(this, ActiveCamera);

            } else {
                // MovemementMode.TrackPosition
                RootGameObject.transform.position = ActiveCamera.transform.position;
            }

            // apply post-rotations
            Quaternion rotateLR = Quaternion.AngleAxis(ShiftAngle, RootGameObject.transform.up);
            Quaternion rotateUp = Quaternion.AngleAxis(TiltAngle, rotateLR * RootGameObject.transform.right);
            RootGameObject.transform.rotation = rotateLR * rotateUp * RootGameObject.transform.rotation;

        }

        public void InvalidateStaticPosition()
        {
            bStaticUpdated = false;
        }



		public void AddUIElement(SceneUIElement e, bool bIsInLocalFrame = true) {
			vUIElements.Add (e);
            e.Parent = this;
			if (e.RootGameObject != null) {
				// assume element transform is set to a local transform, so we want to apply current scene transform?
				e.RootGameObject.transform.SetParent (RootGameObject.transform, (bIsInLocalFrame == false) );
			}

            e.SetLayer(UIElementLayer);
        }

		public void RemoveUIElement(SceneUIElement e, bool bDestroy) {
            e.Disconnect();
			vUIElements.Remove(e);
            e.Parent = null;
			if ( e.RootGameObject != null && bDestroy) {
				e.RootGameObject.transform.parent = null;
				UnityEngine.Object.Destroy (e.RootGameObject);
			}
		}


		public bool FindUIRayIntersection(Ray ray, out UIRayHit hit) {
            return HUDUtil.FindNearestRayIntersection(vUIElements, ray, out hit);
		}
        public bool FindUIHoverRayIntersection(Ray ray, out UIRayHit hit) {
            return HUDUtil.FindNearestHoverRayIntersection(vUIElements, ray, out hit);
        }


        public bool FindAnyRayIntersection(Ray ray, out AnyRayHit hit) {
			hit = null;
			UIRayHit bestUIHit = null;
			if (FindUIRayIntersection (ray, out bestUIHit)) {
				hit = new AnyRayHit (bestUIHit);
			}
			return (hit != null);
		}





        /// <summary>
        /// Register a layout engine
        /// </summary>
        public void AddLayoutEngine(ILayoutEngine e, string name, bool bSetAsDefault = false)
        {
            if (Layouts.ContainsKey(name))
                throw new Exception("Cockpit.AddLayoutEngine: tried to register duplicate name " + name);

            // always set first layout as default, until another default comes along
            if (Layouts.Count == 0)
                bSetAsDefault = true;

            Layouts[name] = e;
            if (bSetAsDefault)
                defaultLayout = e;
        }


        public void SetDefaultLayoutEngine(string name)
        {
            ILayoutEngine setdefault = null;
            if (Layouts.TryGetValue(name, out setdefault) == false)
                throw new Exception("Cockpit.SetDefaultLayoutEngine: could not find layout named " + name);
            defaultLayout = setdefault;
        }

        /// <summary>
        /// Remove named layout engine. 
        /// If bRemoveAllElements, then layout.RemoveAll() is called to clean up contents
        /// (otherwise you must do yourself).
        /// Note: if default layout is removed, we set default to null. You need to call SetDefaultLayoutEngine in this case.
        /// </summary>
        public void RemoveLayoutEngine(string name, bool bRemoveAllElements = true)
        {
            ILayoutEngine remove = null;
            if (Layouts.TryGetValue(name, out remove) == false)
                throw new Exception("Cockpit.RemoveLayoutEngine: could not find layout named " + name);

            Layouts.Remove(name);

            if ( bRemoveAllElements )
                remove.RemoveAll(true);

            if ( defaultLayout == remove )
                defaultLayout = null;

            // [TODO] can we auto-update default layout??
        }


        /// <summary>
        /// get default LayoutEngine
        /// </summary>
        public ILayoutEngine DefaultLayout
        {
            get { return defaultLayout; }
        }

        /// <summary>
        /// Find LayoutEngine with given name, or default layout if no argument
        /// </summary>
        public ILayoutEngine Layout(string name = "") {
            if (name == "")
                return defaultLayout;
            ILayoutEngine r = null;
            if (Layouts.TryGetValue(name, out r) == false)
                throw new Exception("Cockpit.Layout: could not find layout named " + name);
            return r;
        }


   


        public void AddKeyHandler(IShortcutKeyHandler h)
        {
            vKeyHandlers.Add(h);
        }
        public void RemoveKeyHandler(IShortcutKeyHandler h)
        {
            vKeyHandlers.Remove(h);
        }

        public bool HandleShortcutKeys()
        {
            foreach ( IShortcutKeyHandler h in vKeyHandlers ) {
                if (h.HandleShortcuts())
                    return true;
            }
            return false;
        }


	}
}

