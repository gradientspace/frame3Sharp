using System;
using System.Collections.Generic;
using System.Diagnostics;
using g3;

namespace f3
{

    public interface ICockpitViewTracker
    {
        void UpdateTracking(Cockpit cockpit, fCamera camera);
    }


	public class Cockpit : SceneUIParent
	{
		FContext context;
		fGameObject gameobject;
        fGameObject onCameraGO;

        // additional UI behavior attached to this cockpit
        public InputBehaviorSet InputBehaviors { get; set; }

        // special behaviors that are not part of regular capture-device handling
        public InputBehaviorSet OverrideBehaviors { get; set; }

        public GenericAnimator HUDAnimator { get; set; }

        List<SceneUIElement> vUIElements;
        public int UIElementLayer { get; set; }     // Layer that UIElements will be placed in. default is FPlatform.HUDLayer

        List<IShortcutKeyHandler> vKeyHandlers;


        Dictionary<string, ICockpitLayout> Layouts;
        ICockpitLayout defaultLayout;


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

            Layouts = new Dictionary<string, ICockpitLayout>();
            defaultLayout = null;

            vKeyHandlers = new List<IShortcutKeyHandler>();
            InputBehaviors = new InputBehaviorSet() { DefaultSource = this };
            OverrideBehaviors = new InputBehaviorSet() { DefaultSource = this };
            HUDAnimator = new GenericAnimator();

            TiltAngle = ShiftAngle = 0.0f;
            DefaultCursorDepth = -1;
        }

        public void Destroy()
        {
            while (vUIElements.Count > 0)
                RemoveUIElement(vUIElements[0], true);
            RootGameObject.SetParent(null);
            RootGameObject.Destroy();
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
        public fCamera ActiveCamera {
            get { return Context.ActiveCamera; }
        }
        public fGameObject RootGameObject {
			get { return gameobject; }
		}
        public fGameObject FixedCameraTrackingGO {
			get { return onCameraGO; }
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
            Frame3f viewF = UnityUtil.GetGameObjectFrame(ActiveCamera.GameObject(), eSpace);
            float fAngle = VRUtil.PlaneAngleSigned(Vector3f.AxisZ, viewF.Z, 1);
            return new Frame3f(viewF.Origin, Quaternionf.AxisAngleD(Vector3f.AxisY, fAngle));
        }

        // frame centered at cockpit location and aligned with camera direction
        public virtual Frame3f GetViewFrame2D()
        {
            return new Frame3f(Vector3f.Zero, ActiveCamera.GetRotation());
        }


        /*
         * 2D UI sizing.
         * 
         */

        public virtual void OnWindowResized()
        {
            if (use_constant_scale)
                constant_size_update();
        }

        bool use_constant_scale = false;
        Vector2f start_pixel_scale = Vector2f.One;
        Vector2f cur_pixel_scale = Vector2f.One;
        Vector2f const_scale = Vector2f.One;

        /// <summary>
        /// Call this to turn on constant-size 2D cockpit scaling. This is a bit tricky. 
        /// The orthographic size of the camera stays at height=1, width = aspect*height, regardless
        /// of the screen size. So, we have to keep track of the scaling between ortho and pixel dims.
        /// Then if the window size changes, the relative x/y initial/cur scalings give us a scaling factor that
        /// we can apply to the cockpit itself. This maintains fixed size, but the 2D view bounds will stay
        /// fixed, unless we also scale them up - this is what GetConstantSizeOrthoViewBounds() is for.
        /// 2D layout containers use this function and then the 2D box gets bigger/smaller with the screen size.
        /// </summary>
        public virtual void EnableConstantSize2DCockpit()
        {
            AxisAlignedBox2f uiBounds = GetOrthoViewBounds();
            AxisAlignedBox2f pixelBounds = GetPixelViewBounds_DpiIndependent();
            start_pixel_scale = new Vector2f(
                uiBounds.Width / pixelBounds.Width,
                uiBounds.Height / pixelBounds.Height);
            use_constant_scale = true;
        }

        // updates cockpit scaling to maintain constant size - call in OnWindowResized()
        protected virtual void constant_size_update()
        {
            AxisAlignedBox2f uiBounds = GetOrthoViewBounds();
            AxisAlignedBox2f pixelBounds = GetPixelViewBounds_DpiIndependent();
            cur_pixel_scale = new Vector2f(
                uiBounds.Width / pixelBounds.Width,
                uiBounds.Height / pixelBounds.Height);

            const_scale = cur_pixel_scale / start_pixel_scale;
            this.RootGameObject.SetLocalScale(new Vector3f(const_scale.x, const_scale.y, 1));
        }


        /// <summary>
        ///  get bounds of current orthographic camera view, in world coordinates 
        ///  (ie box.Min is bottom-left corner of screen and box.Max is top-right
        /// </summary>
        public virtual AxisAlignedBox2f GetOrthoViewBounds()
        {
            if (context.OrthoUICamera == null)
                return AxisAlignedBox2f.Empty;
            float verticalSize = context.OrthoUICamera.OrthoHeight;
            float aspect = (float)FPlatform.ScreenWidth / (float)FPlatform.ScreenHeight;
            float horizontalSize = verticalSize * aspect;
            return new AxisAlignedBox2f(-horizontalSize / 2, -verticalSize / 2, horizontalSize / 2, verticalSize / 2);
        }

        /// <summary>
        /// GetOrthoViewBounds() inverse scaled by constant-size scaling factor,
        /// so corners stay at corners of screen as cockpit is scaled
        /// </summary>
        public virtual AxisAlignedBox2f GetConstantSizeOrthoViewBounds()
        {
            AxisAlignedBox2f bounds = GetOrthoViewBounds();
            return new AxisAlignedBox2f(bounds.Center, bounds.Width * 0.5f / const_scale.x, bounds.Height * 0.5f / const_scale.y);
        }

        /// <summary>
        /// Pixel bounding box of 2D ortho viewport
        /// </summary>
        public virtual AxisAlignedBox2f GetPixelViewBounds_Absolute()
        {
            if (context.OrthoUICamera == null)
                return AxisAlignedBox2f.Empty;
            return new AxisAlignedBox2f(0, 0, FPlatform.ScreenWidth, FPlatform.ScreenHeight);
        }

        /// <summary>
        /// DPI-normalized "Pixel" bounding-box of 2D ortho viewport, 
        /// (ie bounds will change depending on dpi)
        /// </summary>
        public virtual AxisAlignedBox2f GetPixelViewBounds_DpiIndependent()
        {
            if (context.OrthoUICamera == null)
                return AxisAlignedBox2f.Empty;
            float dpi_scale = GetDpiIndependentScale();
            return new AxisAlignedBox2f(0, 0, FPlatform.ScreenWidth*dpi_scale, FPlatform.ScreenHeight*dpi_scale);
        }

        /// <summary>
        /// scaling factor that normalizes for screen DPI (default is 96 DPI)
        /// </summary>
        public virtual float GetDpiIndependentScale() {
            return 96.0f / FPlatform.ScreenDPI;
        }

        /// <summary>
        /// Returns dpi-independent pixel scaling factor. Mainly intended to be used
        /// for sizing UI elements. 
        /// This function multiplies by FPlatform.PixelScaleFactor.
        /// </summary>
        public float GetPixelScale(bool bDpiIndependent = true)
        {
            AxisAlignedBox2f uiBounds = GetOrthoViewBounds();
            AxisAlignedBox2f pixelBounds =
                (bDpiIndependent) ? GetPixelViewBounds_DpiIndependent() : GetPixelViewBounds_Absolute();
            float fScale = uiBounds.Height / pixelBounds.Height;

            // use ValidScreenDimensionRange to manipulate scale here?

            if (FPlatform.InUnityEditor())
                fScale *= FPlatform.EditorPixelScaleFactor;
            else
                fScale *= FPlatform.PixelScaleFactor;
            return fScale;
        }


        /// <summary>
        /// If cockpit is scaled (ie to maintain constant-pixel-size elements), 
        /// then we need a way to get 'scaled' dimensions of things 
        /// (what about positions...?)
        /// </summary>
        public Vector2f GetScaledDimensions(Vector2f v)
        {
            return v * const_scale;
        }


        // called by FContext.PushCockpit()
        public void Start( ICockpitInitializer setup )
		{
            // create invisible plane for cockpit
            gameobject = GameObjectFactory.CreateParentGO("cockpit");

            onCameraGO = GameObjectFactory.CreateParentGO("cockpit_camera");
            gameobject.AddChild(onCameraGO, false);

            // add hud animation controller
            gameobject.AddComponent<UnityPerFrameAnimationBehavior>().Animator = HUDAnimator;                

            // create HUD
            try {
                setup.Initialize(this);
            } catch ( Exception e ) {
                // if hud setup fails we still want to keep going
                DebugUtil.Log(2, "[Cockpit.Start] exception in initializer: {0}\nTrace:\n{1}", e.Message, e.StackTrace);
                if (FPlatform.InUnityEditor())
                    throw;
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
            RootGameObject.SetPosition(fromExisting.RootGameObject.GetPosition());
            RootGameObject.SetRotation(fromExisting.RootGameObject.GetRotation());
            //RootGameObject.transform.position = fromExisting.RootGameObject.transform.position;
            //RootGameObject.transform.rotation = fromExisting.RootGameObject.transform.rotation;
        }

        void UpdateTracking(bool bSetInitial)
        {
            if (bSetInitial || PositionMode == MovementMode.Static) {
                // [TODO] should damp out jitter while allowing larger moves
                if (bStaticUpdated == false) {
                    RootGameObject.SetPosition(ActiveCamera.GetPosition());
                    RootGameObject.SetRotation(Quaternionf.Identity);
                    bStaticUpdated = true;
                }

            } else if (PositionMode == MovementMode.TrackOrientation) {
                RootGameObject.SetPosition(ActiveCamera.GetPosition());
                RootGameObject.SetRotation(ActiveCamera.GetRotation());

            } else if (PositionMode == MovementMode.CustomTracking && CustomTracker != null ) {
                CustomTracker.UpdateTracking(this, ActiveCamera);

            } else {
                // MovemementMode.TrackPosition
                RootGameObject.SetPosition(ActiveCamera.GetPosition());
                RootGameObject.SetRotation(Quaternionf.Identity);
            }

            // apply post-rotations
            Frame3f frame = RootGameObject.GetWorldFrame();
            Quaternionf rotateLR = Quaternionf.AxisAngleD(frame.Y, ShiftAngle);
            Quaternionf rotateUp = Quaternionf.AxisAngleD(rotateLR * frame.X, TiltAngle);
            RootGameObject.SetRotation(rotateLR * rotateUp * RootGameObject.GetRotation());


            // camera-tracking GO always tracks camera
            onCameraGO.SetPosition(ActiveCamera.GetPosition());
            onCameraGO.SetRotation(ActiveCamera.GetRotation());
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
				e.RootGameObject.SetParent(RootGameObject, (bIsInLocalFrame == false) );
			}

            e.SetLayer(UIElementLayer);
        }

		public void RemoveUIElement(SceneUIElement e, bool bDestroy) {
            e.Disconnect();
			vUIElements.Remove(e);
            e.Parent = null;
			if ( e.RootGameObject != null && bDestroy) {
                e.RootGameObject.SetParent(null);
                e.RootGameObject.Destroy();
			}
		}


		public bool FindUIRayIntersection(Ray3f ray, out UIRayHit hit) {
            return HUDUtil.FindNearestRayIntersection(vUIElements, ray, out hit);
		}
        public bool FindUIHoverRayIntersection(Ray3f ray, out UIRayHit hit) {
            return HUDUtil.FindNearestHoverRayIntersection(vUIElements, ray, out hit);
        }


        public bool FindAnyRayIntersection(Ray3f ray, out AnyRayHit hit) {
			hit = null;
			UIRayHit bestUIHit = null;
			if (FindUIRayIntersection (ray, out bestUIHit)) {
				hit = new AnyRayHit (bestUIHit);
			}
			return (hit != null);
		}





        /// <summary>
        /// Register a layout
        /// </summary>
        public void AddLayout(ICockpitLayout e, string name, bool bSetAsDefault = false)
        {
            if (Layouts.ContainsKey(name))
                throw new Exception("Cockpit.AddLayout: tried to register duplicate name " + name);

            // always set first layout as default, until another default comes along
            if (Layouts.Count == 0)
                bSetAsDefault = true;

            Layouts[name] = e;
            if (bSetAsDefault)
                defaultLayout = e;
        }


        public void SetDefaultLayout(string name)
        {
            ICockpitLayout setdefault = null;
            if (Layouts.TryGetValue(name, out setdefault) == false)
                throw new Exception("Cockpit.SetDefaultLayout: could not find layout named " + name);
            defaultLayout = setdefault;
        }

        /// <summary>
        /// Remove named layout. 
        /// If bRemoveAllElements, then layout.RemoveAll() is called to clean up contents
        /// (otherwise you must do yourself).
        /// Note: if default layout is removed, we set default to null. You need to call SetDefaultLayout in this case.
        /// </summary>
        public void RemoveLayout(string name, bool bRemoveAllElements = true)
        {
            ICockpitLayout remove = null;
            if (Layouts.TryGetValue(name, out remove) == false)
                throw new Exception("Cockpit.RemoveLayout: could not find layout named " + name);

            Layouts.Remove(name);

            if ( bRemoveAllElements )
                remove.RemoveAll(true);

            if ( defaultLayout == remove )
                defaultLayout = null;

            // [TODO] can we auto-update default layout??
        }


        /// <summary>
        /// get default Layout
        /// </summary>
        public ICockpitLayout DefaultLayout
        {
            get { return defaultLayout; }
        }

        /// <summary>
        /// Find Layout with given name, or default layout if no argument
        /// </summary>
        public ICockpitLayout Layout(string name = "") {
            if (name == "")
                return defaultLayout;
            ICockpitLayout r = null;
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
            //if (FPlatformUI.IsConsumingMouseInput())
            //    return false;
            if (FPlatformUI.TextEntryFieldHasFocus())
                return false;

            foreach ( IShortcutKeyHandler h in vKeyHandlers ) {
                if (h.HandleShortcuts())
                    return true;
            }
            return false;
        }


	}
}

