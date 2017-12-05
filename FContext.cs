using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g3;

namespace f3 {

    public delegate void ContextWindowResizeEvent();


    /// <summary>
    /// The universe
    /// </summary>
    public class FContext {

        // [TODO] would like to get rid of this...but in a few places it was clunky to keep a reference
        public static FContext ActiveContext_HACK;


        // global events
        public event ContextWindowResizeEvent OnWindowResized;


        SceneOptions options;
        FScene scene;                            // set of objects in our universe
        ICursorController mouseCursor;		    // handles mouse cursor interaction
        SpatialInputController spatialCursor;	// handles spatial interaction in VR
        CameraTracking camTracker;              // tracks some camera stuff that we probably could just put here now...
        TransformManager transformManager;      // manages transform gizmos
        ToolManager toolManager;                // manages active tools

        ICameraInteraction mouseCamControls;    // camera hotkeys and interactions
        bool bInCameraControl;

        Capture captureMouse;                   // current object that is capturing mouse/gamepad input

        Capture captureTouch;                   // current object that is capturing touch input

        Capture captureLeft;                    // current object capturing left spatial controller input
        Capture captureRight;                   // ditto right

        Cockpit activeCockpit;                  // (optional) HUD that kind of sticks to view
        Stack<Cockpit> cockpitStack;

        InputBehaviorSet inputBehaviors;        // behaviors that can capture left/right input
        InputBehaviorSet overrideBehaviors;     // behaviors that do not capture, just respond
                                                // to specific events (like menu button)


        ITextEntryTarget activeTextTarget;      // current object that receives text input
                                                // (there can be only one!)


        ActionSet nextFrameActions;             // actions that will be run in the next frame, before
                                                // UI event handling, prerender(), etc

        ActionSet everyFrameActions;            // actions that will be run every frame until removed


        public TransformManager TransformManager {
            get { return this.transformManager; }
        }

        public ToolManager ToolManager {
            get { return this.toolManager; }
        }

        // [RMS] why do it this way? can't we just create in Start?
        public FScene Scene {
			get { return GetScene (); }
		}
		public FScene GetScene() {
			if (scene == null)
				scene = new FScene (this);
			return scene;
		}

        public fCamera ActiveCamera
        {
            get { return camTracker.MainCamera; }
        }

        // UI camera is orthographic projection, does not exist in VR contexts
        public fCamera OrthoUICamera
        {
            get { return camTracker.OrthoUICamera; }
        }

		public Cockpit ActiveCockpit { 
			get { return activeCockpit; }
		}

		public ICursorController MouseController {
            get {
                if (mouseCursor == null) {
                    if (FPlatform.IsUsingVR())
                        mouseCursor = new VRMouseCursorController(ActiveCamera, this);
                    else if ( FPlatform.IsTouchDevice() )
                        mouseCursor = new TouchMouseCursorController(this);
                    else
                        mouseCursor = new SystemMouseCursorController(this);
                }
                return mouseCursor;
            }
		}
        public SpatialInputController SpatialController
        {
            get {
                if (spatialCursor == null) {
                    spatialCursor = new SpatialInputController(options.SpatialCameraRig, ActiveCamera, this);
                }
                return spatialCursor;
            }
        }
        public InputDevice ActiveInputDevice = InputDevice.Mouse;

        public ICameraInteraction MouseCameraController
        {
            get { return this.mouseCamControls; }
            set { this.mouseCamControls = value; }
        }

        public bool Use2DCockpit
        {
            get { return options.Use2DCockpit; }
        }


        // Use this for initialization
        public void Start(SceneOptions options)
        {
            this.options = options;

            DebugUtil.LogLevel = options.LogLevel;
            FPlatform.InitializeMainThreadID();

            // initialize VR platform if VR is active
            if (gs.VRPlatform.VREnabled) {
                if (options.Use2DCockpit)
                    throw new Exception("FContext.Start: cannot use 2D Orthographic Cockpit with VR!");
                if (options.SpatialCameraRig != null)
                    gs.VRPlatform.Initialize(options.SpatialCameraRig);
            }

            InputExtension.Get.Start();

            nextFrameActions = new ActionSet();
            everyFrameActions = new ActionSet();

            // intialize camera stuff
            camTracker = new CameraTracking();
            camTracker.Initialize(this);

            GetScene();
            if (options.SceneInitializer != null)
                options.SceneInitializer.Initialize(GetScene());
            Scene.SelectionChangedEvent += OnSceneSelectionChanged;


            if (options.DefaultGizmoBuilder != null)
                transformManager = new TransformManager(options.DefaultGizmoBuilder);
            else
                transformManager = new TransformManager( new AxisTransformGizmoBuilder() );
            if (options.EnableTransforms) {
                transformManager.Initialize(this);
            }

            toolManager = new ToolManager();
            toolManager.Initialize(this);
            toolManager.OnToolActivationChanged += OnToolActivationChanged;

            MouseController.Start();
            SpatialController.Start();

            // [RMS] hardcode starting cam target point to origin
            ActiveCamera.SetTarget(Vector3f.Zero);

            if (options.MouseCameraControls != null)
                MouseCameraController = options.MouseCameraControls;

            // apply initial transformation to scene
            ActiveCamera.Manipulator().SceneTranslate(Scene, SceneGraphConfig.InitialSceneTranslate, true);

            // create behavior sets
            inputBehaviors = new InputBehaviorSet();
            overrideBehaviors = new InputBehaviorSet();

            // cockpit needs to go last because UI setup may depend on above
            cockpitStack = new Stack<Cockpit>();
            if (options.EnableCockpit)
                PushCockpit(options.CockpitInitializer);


            captureMouse = null;
            captureTouch = null;
            captureLeft = captureRight = null;
            bInCameraControl = false;

			// [RMS] this locks cursor to game unless user presses escape or exits
            if ( FPlatform.IsUsingVR() || options.UseSystemMouseCursor == false )
			    Cursor.lockState = CursorLockMode.Locked;

            // set hacky hackenstein global
            ActiveContext_HACK = this;

            startup_checks();
        }


        // sometimes we need the last valid InputState
        //  (should this be exposed somehow?)
        InputState lastInputState;

        // we use this as a guard to prevent calling things that would cancel current capture
        bool inCapturingObjectCall = false;

        // Update is called once per frame
        public void Update() {

            ThreadMailbox.ProcessMainThreadMail();   // is this the right spot to do this?

            FPlatform.IncrementFrameCounter();

            if (FPlatform.IsWindowResized()) {
                ActiveCockpit.OnWindowResized();
                // need to tell other cockpits about this...
                foreach (Cockpit c in cockpitStack)
                    c.OnWindowResized();
                FUtil.SafeSendAnyEvent(OnWindowResized);
            }

            // update our wrappers around various different Input modes
            InputExtension.Get.Update();

            // update cockpit tracking and let UI do per-frame rendering computations
            if (options.EnableCockpit) 
                ActiveCockpit.Update();

            // run per-frame actions
            Action runAllOnceActions = null;
            lock (nextFrameActions) {
                runAllOnceActions = nextFrameActions.GetRunnable();
                nextFrameActions.Clear();
            }
            if ( runAllOnceActions != null )
                runAllOnceActions();
            Action runAllEveryFrameActions = null;
            lock (everyFrameActions) {
                runAllEveryFrameActions = everyFrameActions.GetRunnable();
            }
            if (runAllEveryFrameActions != null)
                runAllEveryFrameActions();


            // can either use spacecontrols or mouse, but not both at same time
            // [TODO] ask spatial input controller instead, it knows better (?)
            if ( FPlatform.IsUsingVR() && SpatialController.CheckForSpatialInputActive() ) {
                Configure_SpaceControllers();
                HandleInput_SpaceControllers();
            } else if ( FPlatform.IsTouchDevice() ) {
                Configure_TouchInput();
                HandleInput_Touch();
            } else {
                Configure_MouseOrGamepad();
                HandleInput_MouseOrGamepad();
            }

            // after we have handled input, do per-frame rendering computations
            if (options.EnableCockpit)
                ActiveCockpit.PreRender();
            ToolManager.PreRender();
            Scene.PreRender();
        }


        void Configure_SpaceControllers()
        {
            SceneGraphConfig.ActiveDoubleClickDelay = SceneGraphConfig.TriggerDoubleClickDelay;
            ActiveInputDevice = (gs.VRPlatform.CurrentVRDevice == gs.VRPlatform.Device.HTCVive) ?
                InputDevice.HTCViveWands : InputDevice.OculusTouch;
        }

        void HandleInput_SpaceControllers()
        {
            // update cursors
            SpatialController.Update();
            MouseController.HideCursor();

            // have to do this after cursor update in case hotkey uses mouse position
            HandleKeyboardInput();

            // create our super-input object  (wraps all supported input types)
            InputState input = new InputState();
            input.Initialize_SpatialController(this);
            lastInputState = input;

            // run override behaviors
            overrideBehaviors.SendOverrideInputs(input);

 
            input.LeftCaptureActive = (captureLeft != null);
            input.RightCaptureActive = (captureRight != null);

            // update left-capture
            if (captureLeft != null) {
                inCapturingObjectCall = true;
                Capture cap = Capture.End;
                try {
                    cap = captureLeft.element.UpdateCapture(input, captureLeft.data);
                }catch ( Exception e ) {
                    DebugUtil.Log(2, "FContext.HandleInput_SpaceControllers: exception in left UpdateCapture! " + e.Message);
                }
                inCapturingObjectCall = false;
                if (cap.state == CaptureState.Continue) {
                    // (carry on)
                } else if (cap.state == CaptureState.End) {
                    DebugUtil.Log(10, "[SceneController] released left capture " + captureLeft.element.CaptureIdentifier);
                    if (captureRight == captureLeft)
                        captureRight = null;        // if we are doing a dual-capture, we only want to end once!!
                    captureLeft = null;
                }
            }

            // update right-capture
            // if we are doing a both-capture, we only want to send update once
            if ( captureRight != null && captureRight != captureLeft ) {
                inCapturingObjectCall = true;
                Capture cap = Capture.End;
                try {
                    cap = captureRight.element.UpdateCapture(input, captureRight.data);
                } catch (Exception e) {
                    DebugUtil.Log(2, "FContext.HandleInput_SpaceControllers: exception in right UpdateCapture! " + e.Message);
                }
                inCapturingObjectCall = false;
                if (cap.state == CaptureState.Continue) {
                    // (carry on)
                } else if (cap.state == CaptureState.End) {
                    DebugUtil.Log(10, "[SceneController] released right capture " + captureRight.element.CaptureIdentifier);
                    captureRight = null;
                }
            }

            // if we have a free device, check for capture. 
            bool bCanCapture = (bInCameraControl == false);
            if (bCanCapture && (captureLeft == null || captureRight == null) ) {

                // collect up capture requests 
                List<CaptureRequest> vRequests = new List<CaptureRequest>();
                inputBehaviors.CollectWantsCapture(input, vRequests);
                if ( vRequests.Count > 0 ) {

                    // end outstanding hovers
                    TerminateHovers(input);

                    // select one of the capture requests. technically we could end
                    //  up with none successfully Begin'ing, but behaviors should be
                    //  doing those checks in WantCapture, not BeginCapture !!
                    vRequests.OrderBy(x => x.element.Priority);
                    Capture capReq = null;
                    for ( int i = 0; i < vRequests.Count && capReq == null; ++i ) {

                        // filter out invalid requests
                        CaptureSide eUseSide = vRequests[i].side;
                        if (eUseSide == CaptureSide.Any)        // replace Any with Both. Does that make sense??
                            eUseSide = CaptureSide.Both;
                        if ( (eUseSide == CaptureSide.Left || eUseSide == CaptureSide.Both) 
                               && captureLeft != null)
                            continue;
                        if ( (eUseSide == CaptureSide.Right || eUseSide == CaptureSide.Both)
                               && captureRight != null)
                            continue;

                        Capture c = vRequests[i].element.BeginCapture(input, eUseSide);
                        if (c.state == CaptureState.Begin)
                            capReq = c;
                    }

                    if (capReq != null) {
                        // technically we only should terminate hover on capture controller,
                        // but that seems really hard. This will clear hovers but they will
                        // come back next frame. Perhaps revisit if this is causing flicker...
                        TerminateHovers(input);

                        // [RMS] most of this checking is redundant now, but leaving because of debug logging
                        if (capReq.data.which == CaptureSide.Left) {
                            if (captureLeft != null) {
                                DebugUtil.Warning("[SceneController.HandleInput_SpaceControllers] received Capture request for Left side from {0}, but already capturing! Ignoring.", capReq.element.CaptureIdentifier);
                            } else {
                                captureLeft = capReq;
                                DebugUtil.Log(10, "[SceneController] began left-capture" + captureLeft.element.CaptureIdentifier);
                            }
                        } else if (capReq.data.which == CaptureSide.Right) {
                            if (captureRight != null) {
                                DebugUtil.Warning("[SceneController.HandleInput_SpaceControllers] received Capture request for Right side from {0}, but already capturing! Ignoring.", capReq.element.CaptureIdentifier);
                            } else {
                                captureRight = capReq;
                                DebugUtil.Log(10, "[SceneController] began right-capture" + captureRight.element.CaptureIdentifier);
                            }
                        } else if (capReq.data.which == CaptureSide.Both || capReq.data.which == CaptureSide.Any) {
                            if (captureLeft != null || captureRight != null) {
                                DebugUtil.Warning("[SceneController.HandleInput_SpaceControllers] received Capture request for both sides from {0}, but already capturing! Ignoring.", capReq.element.CaptureIdentifier);
                            } else {
                                captureLeft = captureRight = capReq;
                                DebugUtil.Log(10, "[SceneController] began both-capture " + captureLeft.element.CaptureIdentifier);
                            }
                        }
                    }
                }
            }

            // update hover if we have a free device
            if ( captureLeft == null || captureRight == null )
                inputBehaviors.UpdateHover(input);
            
        }



        void Configure_TouchInput()
        {
            SceneGraphConfig.ActiveDoubleClickDelay = SceneGraphConfig.MouseDoubleClickDelay;
            ActiveInputDevice = InputDevice.TabletFingers;

        }

        void HandleInput_Touch()
        {
            // update mouse/gamepad cursor
            MouseController.Update();

            // create our super-input object  (wraps all supported input types)
            InputState input = new InputState();
            input.Initialize_TouchInput(this);
            lastInputState = input;

            // [RMS] not sure if this is 100% correct thing to do. We need to allow Platform UI
            // layer (eg like Unity ui) to consume mouse events before we see them. However this
            // only applies if they are "on top". It is a bit tricky...
            if (FPlatformUI.IsConsumingMouseInput()) {
                return;
            }

            // run override behaviors
            overrideBehaviors.SendOverrideInputs(input);

            input.TouchCaptureActive = (captureTouch != null);

            // update left-capture
            if (captureTouch != null) {
                inCapturingObjectCall = true;
                Capture cap = Capture.End;
                try {
                    cap = captureTouch.element.UpdateCapture(input, captureTouch.data);
                } catch (Exception e) {
                    DebugUtil.Log(2, "FContext.HandleInput_Touch: exception in UpdateCapture! " + e.Message);
                }
                inCapturingObjectCall = false;
                if (cap.state == CaptureState.Continue) {
                    // (carry on)
                } else if (cap.state == CaptureState.End) {
                    DebugUtil.Log(10, "[SceneController] released touch capture " + captureTouch.element.CaptureIdentifier);
                    captureTouch = null;
                }
            }

            // if we have a free device, check for capture. 
            bool bCanCapture = (bInCameraControl == false);
            if (bCanCapture && captureTouch == null ) {

                // collect up capture requests 
                List<CaptureRequest> vRequests = new List<CaptureRequest>();
                inputBehaviors.CollectWantsCapture(input, vRequests);
                if ( vRequests.Count > 0 ) {

                    // select one of the capture requests. technically we could end
                    //  up with none successfully Begin'ing, but behaviors should be
                    //  doing those checks in WantCapture, not BeginCapture !!
                    vRequests.OrderBy(x => x.element.Priority);
                    Capture capReq = null;
                    for ( int i = 0; i < vRequests.Count && capReq == null; ++i ) {

                        // filter out invalid requests
                        //  (??)

                        // before we actually begin capture we will complete any text editing
                        // [RMS] perhaps this should be configurable for behavior? Some behaviors
                        // do not require this (eg view controls...)
                        completeTextEntryOnFocusChange();

                        Capture c = vRequests[i].element.BeginCapture(input, CaptureSide.Any);
                        if (c.state == CaptureState.Begin)
                            capReq = c;
                    }
                    if (capReq != null) 
                        captureTouch = capReq;

                }
            }

        }




        void Configure_MouseOrGamepad()
        {
            SceneGraphConfig.ActiveDoubleClickDelay = SceneGraphConfig.MouseDoubleClickDelay;
            ActiveInputDevice = InputDevice.Mouse | InputDevice.Gamepad;
        }

        void HandleInput_MouseOrGamepad()
        {
            // update mouse/gamepad cursor
            MouseController.Update();

            // have to do this after cursor update in case hotkey uses mouse position
            HandleKeyboardInput();

            // create our super-input object  (wraps all supported input types)
            InputState input = new InputState();
            input.Initialize_MouseGamepad(this);
            lastInputState = input;


            // [RMS] not sure if this is 100% correct thing to do. We need to allow Platform UI
            // layer (eg like Unity ui) to consume mouse events before we see them. However this
            // only applies if they are "on top". It is a bit tricky...
            if ( FPlatformUI.IsConsumingMouseInput()) { 
                return;
            }


            CameraInteractionState eCamState = (MouseCameraController != null) 
                ? MouseCameraController.CheckCameraControls(input) : CameraInteractionState.Ignore;
            if (eCamState == CameraInteractionState.BeginCameraAction) {
                TerminateHovers(input);

                bInCameraControl = true;
                ActiveCamera.SetTargetVisible(true);
            } else if (eCamState == CameraInteractionState.EndCameraAction) {
                bInCameraControl = false;
                ActiveCamera.SetTargetVisible(false);
            } else if (bInCameraControl) {
                ActiveCamera.SetTargetVisible(true);
                MouseCameraController.DoCameraControl(Scene, ActiveCamera, input);

            } else {

                // run override behaviors
                overrideBehaviors.SendOverrideInputs(input);

                input.MouseGamepadCaptureActive = (captureMouse != null);

                if (InCaptureMouse) {
                    inCapturingObjectCall = true;
                    Capture cap = Capture.End;
                    try {
                        cap = captureMouse.element.UpdateCapture(input, captureMouse.data);
                    } catch (Exception e) {
                        DebugUtil.Log(2, "FContext.HandleInput_MouseOrGamepad: exception in UpdateCapture! " + e.Message);
                        if (FPlatform.InUnityEditor())
                            throw;
                    }
                    inCapturingObjectCall = false;
                    if (cap.state == CaptureState.Continue) {
                        // (carry on)
                    } else if (cap.state == CaptureState.End) {
                        captureMouse = null;
                    }

                } else {

                    // this is very simplistic...needs to be rewritten like space controllers

                    List<CaptureRequest> vRequests = new List<CaptureRequest>();
                    inputBehaviors.CollectWantsCapture(input, vRequests);
                    if (vRequests.Count > 0) {

                        // end outstanding hovers
                        TerminateHovers(input);

                        // select one of the capture requests. technically we could end
                        //  up with none successfully Begin'ing, but behaviors should be
                        //  doing those checks in WantCapture, not BeginCapture !!
                        vRequests.OrderBy(x => x.element.Priority);
                        Capture capReq = null;
                        for (int i = 0; i < vRequests.Count && capReq == null; ++i) {
                            if (vRequests[i].side != CaptureSide.Any)
                                continue;       // not possible in mouse paths...

                            // before we actually begin capture we will complete any text editing
                            // [RMS] perhaps this should be configurable for behavior? Some behaviors
                            // do not require this (eg view controls...)
                            completeTextEntryOnFocusChange();

                            Capture c = vRequests[i].element.BeginCapture(input, vRequests[i].side);
                            if (c.state == CaptureState.Begin) {
                                capReq = c;
                            }
                        }

                        captureMouse = capReq;
                    }
                }

                // if we don't have a capture, do hover
                if (captureMouse == null)
                    inputBehaviors.UpdateHover(input);

            }
        }







        public bool InCaptureMouse {
            get { return (captureMouse != null); }
        }
        public bool InCameraManipulation
        {
            get { return bInCameraControl;  }
        }

        void TerminateHovers(InputState input)
        {
            inputBehaviors.EndHover(input);
        }

        void TerminateCaptures(InputState input)
        {
            if ( captureMouse != null ) {
                captureMouse.element.ForceEndCapture(input, captureMouse.data);
                captureMouse = null;
            }
            if ( captureTouch != null ) {
                captureTouch.element.ForceEndCapture(input, captureTouch.data);
                captureTouch = null;
            }
            if ( captureLeft != null ) {
                captureLeft.element.ForceEndCapture(input, captureLeft.data);
                captureLeft = null;
            }
            if ( captureRight != null ) {
                captureRight.element.ForceEndCapture(input, captureRight.data);
                captureRight = null;
            }
        }

        void TerminateIfCapturing(IEnumerable<InputBehavior> behaviors, InputState input)
        {
            foreach ( InputBehavior b in behaviors ) {
                if ( captureMouse != null && captureMouse.element == b) {
                    captureMouse.element.ForceEndCapture(lastInputState, captureMouse.data);
                    captureMouse = null;
                }
                if (captureTouch != null && captureTouch.element == b) {
                    captureTouch.element.ForceEndCapture(lastInputState, captureTouch.data);
                    captureTouch = null;
                }
                if (captureLeft != null && captureLeft.element == b) {
                    captureLeft.element.ForceEndCapture(lastInputState, captureLeft.data);
                    captureLeft = null;
                }
                if (captureRight != null && captureRight.element == b) {
                    captureRight.element.ForceEndCapture(lastInputState, captureRight.data);
                    captureRight = null;
                }
            }
        }



        // called when we lose window focus
        public virtual void OnFocusChange(bool bFocused)
        {
            if (bFocused == false) {
                TerminateHovers(lastInputState);

                if ( captureMouse != null || captureTouch != null || captureLeft != null || captureRight != null ) {
                    if ( FPlatform.ShowingExternalPopup ) {
                        throw new Exception("TerminateCapture: Need to complete capture bhefore showing external popup dialog. Use Context.RegisterNextFrameAction()");
                    }
                }
                TerminateCaptures(lastInputState);

                if (bInCameraControl) {
                    bInCameraControl = false;
                    ActiveCamera.SetTargetVisible(false);
                }
            }
        }





        public void PushCockpit(ICockpitInitializer initializer)
        {
            if (inCapturingObjectCall) {
                DebugUtil.Log(2, "FContext.PushCockpit: called from inside Behaviour.UpdateCapture(). This is not permitted. Use FContext.RegisterNextFrameAction().");
                throw new Exception("FContext.PushCockpit: called from inside Behaviour.UpdateCapture(). This is not permitted. Use FContext.RegisterNextFrameAction().");
            }

            Cockpit trackingInitializer = null;
            if (activeCockpit != null) {
                trackingInitializer = activeCockpit;
                inputBehaviors.Remove(activeCockpit.InputBehaviors);
                overrideBehaviors.Remove(activeCockpit.OverrideBehaviors);
                activeCockpit.InputBehaviors.OnSetChanged -= on_cockpit_behaviors_changed;
                activeCockpit.OverrideBehaviors.OnSetChanged -= on_cockpit_behaviors_changed;
                cockpitStack.Push(activeCockpit);
                activeCockpit.RootGameObject.SetActive(false);
            }

            Cockpit c = new Cockpit(this);
            activeCockpit = c;
            if (Use2DCockpit) {
                c.UIElementLayer = FPlatform.UILayer;
                if (options.ConstantSize2DCockpit)
                    c.EnableConstantSize2DCockpit();
            }
            c.Start(initializer);
            if (trackingInitializer != null)
                c.InitializeTracking(trackingInitializer);
            inputBehaviors.Add(c.InputBehaviors, "active_cockpit");
            overrideBehaviors.Add(c.OverrideBehaviors, "active_cockpit_override");
            activeCockpit.InputBehaviors.OnSetChanged += on_cockpit_behaviors_changed;
            activeCockpit.OverrideBehaviors.OnSetChanged += on_cockpit_behaviors_changed;

            mouseCursor.ResetCursorToCenter();
        }
        public void PopCockpit(bool bDestroy)
        {
            if (inCapturingObjectCall) {
                DebugUtil.Log(2, "FContext.PopCockpit: called from inside Behaviour.UpdateCapture(). This is not permitted. Use FContext.RegisterNextFrameAction().");
                throw new Exception("FContext.PopCockpit: called from inside Behaviour.UpdateCapture(). This is not permitted. Use FContext.RegisterNextFrameAction().");
            }

            if (activeCockpit != null) {
                inputBehaviors.Remove(activeCockpit.InputBehaviors);
                overrideBehaviors.Remove(activeCockpit.OverrideBehaviors);
                activeCockpit.InputBehaviors.OnSetChanged -= on_cockpit_behaviors_changed;
                activeCockpit.OverrideBehaviors.OnSetChanged -= on_cockpit_behaviors_changed;
                activeCockpit.RootGameObject.SetActive(false);
                if (bDestroy)
                    activeCockpit.Destroy();
                activeCockpit = null;
            }

            activeCockpit = cockpitStack.Pop();
            if (activeCockpit != null) {
                activeCockpit.RootGameObject.SetActive(true);
                inputBehaviors.Add(activeCockpit.InputBehaviors, "active_cockpit");
                overrideBehaviors.Add(activeCockpit.OverrideBehaviors, "active_cockpit_override");
                activeCockpit.InputBehaviors.OnSetChanged += on_cockpit_behaviors_changed;
                activeCockpit.OverrideBehaviors.OnSetChanged += on_cockpit_behaviors_changed;
            }

            mouseCursor.ResetCursorToCenter();
        }




        protected virtual void OnToolActivationChanged(ITool tool, ToolSide eSide, bool bActivated)
        {
            if (bActivated) {
                inputBehaviors.Add(tool.InputBehaviors, "active_tool");
                tool.InputBehaviors.OnSetChanged += on_tool_behaviors_changed;
            } else {
                tool.InputBehaviors.OnSetChanged -= on_tool_behaviors_changed;
                TerminateIfCapturing(tool.InputBehaviors, lastInputState);
                inputBehaviors.Remove(tool.InputBehaviors);
            }
        }
        void on_tool_behaviors_changed(InputBehaviorSet behaviors)
        {
            List<InputBehavior> removed = inputBehaviors.RemoveByGroup("active_tool");
            TerminateIfCapturing(removed, lastInputState);
        }



        InputBehaviorSource activeSOBehaviourSource;

        protected virtual void OnSceneSelectionChanged(object sender, EventArgs e)
        {
            InputBehaviorSource newSource = (Scene.Selected.Count == 1) ? Scene.Selected[0] as InputBehaviorSource : null;
            if ( (newSource != null && newSource == activeSOBehaviourSource) || (newSource == null && activeSOBehaviourSource == null) )
                return;   // did not actually change

            // remove existing source that is no longer selected
            if (newSource != activeSOBehaviourSource && activeSOBehaviourSource != null) {
                activeSOBehaviourSource.InputBehaviors.OnSetChanged -= on_selected_so_behaviors_changed;
                TerminateIfCapturing(activeSOBehaviourSource.InputBehaviors, lastInputState);
                inputBehaviors.Remove(activeSOBehaviourSource.InputBehaviors);
                activeSOBehaviourSource = null;
            }

            // if new selection has behaviors, register them
            if (newSource != null) {
                InputBehaviorSet newBehaviors = newSource.InputBehaviors;
                if (newBehaviors.Count > 0) {
                    inputBehaviors.Add(newBehaviors, "active_so");
                    newBehaviors.OnSetChanged += on_selected_so_behaviors_changed;
                    activeSOBehaviourSource = newSource;
                }
            }
        }
        void on_selected_so_behaviors_changed(InputBehaviorSet behaviors)
        {
            List<InputBehavior> removed = inputBehaviors.RemoveByGroup("active_so");
            TerminateIfCapturing(removed, lastInputState);
        }

        void on_cockpit_behaviors_changed(InputBehaviorSet behaviors)
        {
            activeCockpit.InputBehaviors.OnSetChanged -= on_cockpit_behaviors_changed;
            activeCockpit.OverrideBehaviors.OnSetChanged -= on_cockpit_behaviors_changed;

            // remove from global behavior set
            List<InputBehavior> actives = inputBehaviors.RemoveByGroup("active_cockpit");
            List<InputBehavior> overrides = inputBehaviors.RemoveByGroup("active_cockpit_override");

            // if a Behavior that is currently capturing is no longer in active or override sets,
            // we need to terminate that behavior.
            actives.RemoveAll((x) => { return activeCockpit.InputBehaviors.Contains(x); });
            TerminateIfCapturing(actives, lastInputState);
            overrides.RemoveAll((x) => { return activeCockpit.OverrideBehaviors.Contains(x); });
            TerminateIfCapturing(overrides, lastInputState);

            inputBehaviors.Add(activeCockpit.InputBehaviors, "active_cockpit");
            overrideBehaviors.Add(activeCockpit.OverrideBehaviors, "active_cockpit_override");
            activeCockpit.InputBehaviors.OnSetChanged += on_cockpit_behaviors_changed;
            activeCockpit.OverrideBehaviors.OnSetChanged += on_cockpit_behaviors_changed;
        }






        // remove all scene stuff and reset view to default
        public void NewScene(bool bAnimated, bool bResetView = true)
        {
            if (InCameraManipulation)
                return;     // not supported yet

            // disable tools, because they might refer to active selection
            if (ToolManager.HasActiveTool(ToolSide.Left))
                ToolManager.DeactivateTool(ToolSide.Left);
            if (ToolManager.HasActiveTool(ToolSide.Right))
                ToolManager.DeactivateTool(ToolSide.Right);

            Scene.ClearHistory();
            Scene.ClearSelection();
            Scene.RemoveAllSceneObjects();
            Scene.RemoveAllUIElements();
            Scene.SetCurrentTime(0);
            Scene.SelectionMask = null;

            UniqueNames.Reset();

            if (bResetView)
                ResetView(bAnimated);

            // seems like a good time for this...
            FPlatform.SuggestGarbageCollection();
        }

        public void ResetView(bool bAnimated)
        {
            Action resetAction = () => {
                Scene.SetSceneScale(1.0f);
                ActiveCamera.Manipulator().ResetSceneOrbit(Scene, true, true, true);
                // [RMS] above should already do this, but sometimes it gets confused..
                Scene.RootGameObject.SetRotation(Quaternion.identity);
                ActiveCamera.Manipulator().ResetScenePosition(scene);
                ActiveCamera.Manipulator().SceneTranslate(Scene, SceneGraphConfig.InitialSceneTranslate, true);
            };

            if (bAnimated)
                ActiveCamera.Animator().DoActionDuringDipToBlack(resetAction, 0.5f);
            else
                resetAction();
        }

        public void ScaleView(Vector3 vCenterW, float fRadiusW )
        {
            //Vector3f camTarget = ActiveCamera.GetTarget();
            //Vector3f localTarget = Scene.WorldFrame.ToFrameP(camTarget);
            Vector3f vDeltaOrig = Scene.SceneFrame.ToFrameP(vCenterW);

            ActiveCamera.Manipulator().ResetSceneOrbit(
                Scene, false, true, true);

            float fCurScale = Scene.GetSceneScale();

            Frame3f cockpitF = ActiveCockpit.GetLevelViewFrame(CoordSpace.WorldCoords);
            float fScale = 1.0f / fRadiusW;
            vDeltaOrig *= fScale;
            Frame3f deskF = cockpitF.Translated(1.2f, 2).Translated(-0.5f, 1).Translated(-vDeltaOrig);
            Scene.SceneFrame = deskF;
            Scene.SetSceneScale(fCurScale * fScale);
            Vector3f newTarget = Scene.SceneFrame.Origin + vDeltaOrig;
            ActiveCamera.SetTarget(newTarget);
        }



        // raycasts into 2D Cockpit if enabled
        public bool Find2DCockpitUIHit(Ray3f orthoEyeRay, out UIRayHit bestHit)
        {
            if (Use2DCockpit == false)
                throw new Exception("FContext.Find2DUIHit: 2D UI layer is not enabled!");

            bestHit = null;
            if (options.EnableCockpit)
                return activeCockpit.FindUIRayIntersection(orthoEyeRay, out bestHit);
            return false;
        }


        // see comment above
        public bool Find2DCockpitUIHoverHit(Ray3f orthoEyeRay, out UIRayHit bestHit)
        {
            if (Use2DCockpit == false)
                throw new Exception("FContext.Find2DUIHit: 2D UI layer is not enabled!");

            bestHit = null;
            if (options.EnableCockpit)
                return activeCockpit.FindUIHoverRayIntersection(orthoEyeRay, out bestHit);
            return false;
        }



        // raycasts into Scene and Cockpit for UIElement hits. Assumption is that cockpit is "closer" 
        // to eye than scene. This is **not strictly true**. Possibly we should explicitly
        // break this into two separate functions, so that separate Behaviors can be
        // used for Cockpit and Scene.
        // 
        // Note also that if we are using 2D Cockpit, cockpit hits are disabled in this function.
        public bool FindUIHit(Ray eyeRay, out UIRayHit bestHit)
        {
			bestHit = new UIRayHit();
			UIRayHit sceneHit = null, cockpitHit = null;

            bool bCockpitOnly = (options.EnableCockpit && activeCockpit.GrabFocus);
			if (bCockpitOnly == false && scene.FindUIRayIntersection(eyeRay, out sceneHit) ) {
				bestHit = sceneHit;
			}
			if ( Use2DCockpit == false && options.EnableCockpit 
                && activeCockpit.FindUIRayIntersection(eyeRay, out cockpitHit) ) {
				    if ( cockpitHit.fHitDist < bestHit.fHitDist )
					    bestHit = cockpitHit;
			}
			return bestHit.IsValid;
		}

        // see comment as above
        public bool FindUIHoverHit(Ray eyeRay, out UIRayHit bestHit)
        {
            bestHit = new UIRayHit();
            UIRayHit sceneHit = null, cockpitHit = null;

            bool bCockpitOnly = (options.EnableCockpit && activeCockpit.GrabFocus);
            if (bCockpitOnly == false && scene.FindUIHoverRayIntersection(eyeRay, out sceneHit)) {
                bestHit = sceneHit;
            }
            if ( Use2DCockpit == false && options.EnableCockpit 
                && activeCockpit.FindUIHoverRayIntersection(eyeRay, out cockpitHit)) {
                    if (cockpitHit.fHitDist < bestHit.fHitDist)
                        bestHit = cockpitHit;
            }
            return bestHit.IsValid;
        }


        // currently used to change cursor highlight in VR views. Perhaps the VR input Controllers
        // should do this themselves!
        public bool FindAnyRayIntersection(Ray eyeRay, out AnyRayHit anyHit)
        {
			anyHit = new AnyRayHit ();
			AnyRayHit sceneHit = null;
			UIRayHit cockpitHit = null;

            bool bCockpitOnly = (options.EnableCockpit && activeCockpit.GrabFocus);

            if (bCockpitOnly == false && scene.FindAnyRayIntersection (eyeRay, out sceneHit)) {
				anyHit = sceneHit;
			}
			if (Use2DCockpit == false && options.EnableCockpit 
                && activeCockpit.FindUIRayIntersection (eyeRay, out cockpitHit)) {
				    if (cockpitHit.fHitDist < anyHit.fHitDist)
					    anyHit = new AnyRayHit(cockpitHit);
			}
			return anyHit.IsValid;
		}



        /// <summary>
        /// Add an Action that will be run once, in the next frame, and then discarded
        /// </summary>
        public void RegisterNextFrameAction(Action F) {
            lock (nextFrameActions) {
                nextFrameActions.RegisterAction(F);
            }
        }

        /// <summary>
        /// Add an Action that will be run once, in the next frame, and then discarded
        /// </summary>
        public void RegisterNextFrameAction(Action<object> F, object data) {
            lock (nextFrameActions) {
                nextFrameActions.RegisterAction(F, data);
            }
        }



        public void RegisterEveryFrameAction(Action F) {
            lock (everyFrameActions) {
                everyFrameActions.RegisterAction(F);
            }
        }




        public bool RequestTextEntry(ITextEntryTarget target)
       {
            if ( activeTextTarget != null ) {
                activeTextTarget.OnEndTextEntry();
                activeTextTarget = null;
            }
            if (target.OnBeginTextEntry()) {
                activeTextTarget = target;
                return true;
            }
            return false;
        }
        public void ReleaseTextEntry(ITextEntryTarget target)
        {
            if (target != null) {
                if (activeTextTarget != target && activeTextTarget == null)
                    throw new Exception("Cockpit.ReleaseTextEntry: text entry was not captured!");
                if (activeTextTarget != target)
                    throw new Exception("Cockpit.ReleaseTextEntry: different ITextEntryTarget arleady active!");
            }
            activeTextTarget.OnEndTextEntry();
            activeTextTarget = null;
            return;
        }
        void completeTextEntryOnFocusChange()
        {
            if (activeTextTarget != null)
                ReleaseTextEntry(activeTextTarget);
        }
        public bool ProcessTextEntryForFrame()
        {
            if (activeTextTarget == null)
                return false;

            // [TODO] this should happen somewhere else!!!
            //      should handle repeat here (ie in the somewhere-else)

            if (Input.GetKeyUp(KeyCode.Escape)) {
                return activeTextTarget.OnEscape();
            } else if ( Input.GetKeyUp(KeyCode.Return) ) {
                return activeTextTarget.OnReturn();
            } else if (Input.GetKeyDown(KeyCode.Backspace)) {
                return activeTextTarget.OnBackspace();
            } else if (Input.GetKeyDown(KeyCode.Delete)) {
                return activeTextTarget.OnDelete();
            } else if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                return activeTextTarget.OnLeftArrow();
            } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
                return activeTextTarget.OnRightArrow();
            } else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.V)) {
                if ( GUIUtility.systemCopyBuffer.Length > 0 )
                    return activeTextTarget.OnCharacters(GUIUtility.systemCopyBuffer);
            } else if (Input.anyKeyDown) {
                if (Input.inputString.Length > 0)
                    return activeTextTarget.OnCharacters(Input.inputString);
            }
            return activeTextTarget.ConsumeAllInput();
        }

        public bool IsTextEntryActive() {
            return activeTextTarget != null;
        }
          



		bool HandleKeyboardInput() {
            // does current text-entry target want keyboard input?
            if (ProcessTextEntryForFrame() == true)
                return true;

            // does cockpit want it?
            if (options.EnableCockpit) {
                if ( activeCockpit.HandleShortcutKeys() )
                    return true;
            }

            return false;
		}



        void startup_checks()
        {
        }



	} // end SceneController

} // end namespace
