using System;
using UnityEngine;
using System.Collections.Generic;
using g3;


namespace f3
{


    public class AxisTransformGizmoBuilder : ITransformGizmoBuilder
    {
        public bool SupportsMultipleObjects { get { return true; } }

        public ITransformGizmo Build(FScene scene, List<SceneObject> targets)
        {
            var g = new AxisTransformGizmo();
            g.Create(scene, targets);
            return g;
        }
    }


    [Flags]
    public enum AxisGizmoFlags
    {
        AxisTranslateX = 1,
        AxisTranslateY = 1<<2,
        AxisTranslateZ = 1<<3,
        PlaneTranslateX = 1<<4,
        PlaneTranslateY = 1<<5,
        PlaneTranslateZ = 1<<6,
        AxisRotateX = 1<<7,
        AxisRotateY = 1<<8,
        AxisRotateZ = 1<<9,
        UniformScale = 1<<24,

        AxisTranslations = AxisTranslateX | AxisTranslateY | AxisTranslateZ,
        PlaneTranslations = PlaneTranslateX | PlaneTranslateY | PlaneTranslateZ,
        AxisRotations = AxisRotateX | AxisRotateY | AxisRotateZ,

        All = AxisTranslations | PlaneTranslations | AxisRotations | UniformScale
    }


    public class AxisTransformGizmo : GameObjectSet, ITransformGizmo
    {
        public static readonly string DefaultName = "axis_transform";


		fGameObject gizmo;
		fGameObject translate_x, translate_y, translate_z;
		fGameObject rotate_x, rotate_y, rotate_z;
		fGameObject translate_xy, translate_xz, translate_yz;
        fGameObject uniform_scale;
        Bounds gizmoGeomBounds;

        TransientGroupSO internalGroupSO;

        SceneObject frameSourceSO;
        TransientXFormSO internalXFormSO;

        SceneUIParent parent;
		FScene parentScene;
        List<SceneObject> targets;
		ITransformWrapper targetWrapper;

		Dictionary<GameObject, Standard3DWidget> Widgets;
        Standard3DWidget activeWidget;
        Standard3DWidget hoverWidget;

        bool is_interactive = true;

        Material xMaterial, yMaterial, zMaterial;
        Material xHoverMaterial, yHoverMaterial, zHoverMaterial;
        Material allMaterial, allHoverMaterial;

		FrameType eCurrentFrameMode;
		public FrameType CurrentFrameMode {
			get { return eCurrentFrameMode; }
			set {
				eCurrentFrameMode = value;
				SetActiveFrame (eCurrentFrameMode);
			}
		}
        public bool SupportsFrameMode { get { return true; } }


        AxisGizmoFlags eEnabledWidgets = AxisGizmoFlags.All;
        public AxisGizmoFlags ActiveWidgets {
            get { return eEnabledWidgets; }
            set { eEnabledWidgets = value; update_active(); }
        }


        //bool EnableDebugLogging;

        public AxisTransformGizmo ()
		{
			Widgets = new Dictionary<GameObject, Standard3DWidget> ();
			//EnableDebugLogging = false;
		}
        ~AxisTransformGizmo()
        {
            Debug.Assert(internalGroupSO == null);
            Debug.Assert(internalXFormSO == null);
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



		public fGameObject RootGameObject {
			get { return gizmo; }
		}
        public List<SceneObject> Targets
        {
            get { return targets; }
            set { Debug.Assert(false, "not implemented!"); }
        }
        public FScene Scene {
            get { return parentScene; }
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

		public virtual void Disconnect() {

            // we could get this while we are in an active capture, if selection
            // changes. in that case we need to terminate gracefully.
            if (activeWidget != null)
                EndCapture(null);

            if (targetWrapper != null)
                targetWrapper.Target.OnTransformModified -= onTransformModified;

            // if we created an internal group SO, get rid of it
            if (internalGroupSO != null) {
                internalGroupSO.RemoveAllChildren();
                parentScene.RemoveSceneObject(internalGroupSO, true);
                internalGroupSO = null;
            }
            // same for xform
            if (internalXFormSO != null) {
                internalXFormSO.DisconnectTarget();
                parentScene.RemoveSceneObject(internalXFormSO, true);
                internalXFormSO = null;
            }

            FUtil.SafeSendEvent(OnDisconnected, this, EventArgs.Empty);
        }
        public event EventHandler OnDisconnected;


        public virtual bool IsVisible {
            get { return RootGameObject.IsVisible(); }
            set { RootGameObject.SetVisible(value);}
        }

        public virtual bool IsInteractive {
            get { return is_interactive; }
            set { is_interactive = value; }
        }

        // [TODO] why isn't this in GameObjectSet?
        virtual public void SetLayer(int nLayer) {
            foreach (var go in GameObjects)
                go.SetLayer(nLayer);
        }


        // called on per-frame Update()
        virtual public void PreRender() {
            gizmo.Show();

            float fScaling = VRUtil.GetVRRadiusForVisualAngle(
               gizmo.GetPosition(),
               parentScene.ActiveCamera.GetPosition(),
               SceneGraphConfig.DefaultAxisGizmoVisualDegrees);
            fScaling /= parentScene.GetSceneScale();
            float fGeomDim = gizmoGeomBounds.size.magnitude;
            fScaling /= fGeomDim;
            gizmo.SetLocalScale( new Vector3f(fScaling) );
        }

        public virtual void Create(FScene parentScene, List<SceneObject> targets) {
			this.parentScene = parentScene;
			this.targets = targets;

			gizmo = new GameObject("TransformGizmo");

			float fAlpha = 0.5f;
			xMaterial = MaterialUtil.CreateTransparentMaterial (Color.red, fAlpha);
			yMaterial = MaterialUtil.CreateTransparentMaterial (Color.green, fAlpha);
			zMaterial = MaterialUtil.CreateTransparentMaterial (Color.blue, fAlpha);
            xHoverMaterial = MaterialUtil.CreateStandardMaterial(Color.red);
            yHoverMaterial = MaterialUtil.CreateStandardMaterial(Color.green);
            zHoverMaterial = MaterialUtil.CreateStandardMaterial(Color.blue);
            allMaterial = MaterialUtil.CreateTransparentMaterial(Color.white, fAlpha);
            allHoverMaterial = MaterialUtil.CreateStandardMaterial(Color.white);

            translate_x = AppendMeshGO ("x_translate", 
				FResources.LoadMesh("transform_gizmo/axis_translate_x"),
				xMaterial, gizmo);
            Widgets[translate_x] = new AxisTranslationWidget(0) {
                RootGameObject = translate_x, StandardMaterial = xMaterial, HoverMaterial = xHoverMaterial,
                TranslationScaleF = () => { return 1.0f / parentScene.GetSceneScale(); }
            };
			translate_y = AppendMeshGO ("y_translate", 
				FResources.LoadMesh("transform_gizmo/axis_translate_y"),
				yMaterial, gizmo);
			Widgets [translate_y] = new AxisTranslationWidget(1) {
                RootGameObject = translate_y, StandardMaterial = yMaterial, HoverMaterial = yHoverMaterial,
                TranslationScaleF = () => { return 1.0f / parentScene.GetSceneScale(); }
            };
            translate_z = AppendMeshGO ("z_translate", 
				FResources.LoadMesh("transform_gizmo/axis_translate_z"),
				zMaterial, gizmo);	
			Widgets [translate_z] = new AxisTranslationWidget(2) {
                RootGameObject = translate_z, StandardMaterial = zMaterial, HoverMaterial = zHoverMaterial,
                TranslationScaleF = () => { return 1.0f / parentScene.GetSceneScale(); }
            };


            rotate_x = AppendMeshGO ("x_rotate",
				FResources.LoadMesh("transform_gizmo/axisrotate_x"),
				xMaterial, gizmo);
			Widgets [rotate_x] = new AxisRotationWidget(0) {
                RootGameObject = rotate_x, StandardMaterial = xMaterial, HoverMaterial = xHoverMaterial };
            rotate_y = AppendMeshGO ("y_rotate",
				FResources.LoadMesh("transform_gizmo/axisrotate_y"),
				yMaterial, gizmo);
			Widgets [rotate_y] = new AxisRotationWidget(1) {
                RootGameObject = rotate_y, StandardMaterial = yMaterial, HoverMaterial = yHoverMaterial };
            rotate_z = AppendMeshGO ("z_rotate",
				FResources.LoadMesh("transform_gizmo/axisrotate_z"),
				zMaterial, gizmo);			
			Widgets [rotate_z] = new AxisRotationWidget(2) {
                RootGameObject = rotate_z, StandardMaterial = zMaterial, HoverMaterial = zHoverMaterial };


            // plane translation widgets
            translate_xy = AppendMeshGO ("xy_translate",
				FResources.LoadMesh("transform_gizmo/plane_translate_xy"),
				zMaterial, gizmo);
			Widgets [translate_xy] = new PlaneTranslationWidget(2) {
                RootGameObject = translate_xy, StandardMaterial = zMaterial, HoverMaterial = zHoverMaterial,
                TranslationScaleF = () => { return 1.0f / parentScene.GetSceneScale(); }
            };
            translate_xz = AppendMeshGO ("xz_translate",
				FResources.LoadMesh("transform_gizmo/plane_translate_xz"),
				yMaterial, gizmo);
			Widgets [translate_xz] = new PlaneTranslationWidget(1) {
                RootGameObject = translate_xz, StandardMaterial = yMaterial, HoverMaterial = yHoverMaterial,
                TranslationScaleF = () => { return 1.0f / parentScene.GetSceneScale(); }
            };
            translate_yz = AppendMeshGO ("yz_translate",
				FResources.LoadMesh("transform_gizmo/plane_translate_yz"),
				xMaterial, gizmo);
			Widgets [translate_yz] = new PlaneTranslationWidget(0) {
                RootGameObject = translate_yz, StandardMaterial = xMaterial, HoverMaterial = xHoverMaterial,
                TranslationScaleF = () => { return 1.0f / parentScene.GetSceneScale(); }
            };


            uniform_scale = AppendMeshGO("uniform_scale",
                FResources.LoadMesh("transform_gizmo/uniform_scale"), allMaterial, gizmo);
            Widgets[uniform_scale] = new UniformScaleWidget(parentScene.ActiveCamera) {
                RootGameObject = uniform_scale, StandardMaterial = allMaterial, HoverMaterial = allHoverMaterial,
                ScaleMultiplierF = () => { return 1.0f / parentScene.GetSceneScale(); }
            };

            gizmoGeomBounds = UnityUtil.GetGeometryBoundingBox( new List<GameObject>()
                { translate_x,translate_y,translate_z,rotate_x,rotate_y,rotate_z,translate_xy,translate_xz,translate_yz,uniform_scale} );

            // disable shadows on widget components
            foreach ( var go in GameObjects )
                MaterialUtil.DisableShadows(go);

            update_active();

            eCurrentFrameMode = FrameType.LocalFrame;
			SetActiveFrame (eCurrentFrameMode);

            SetLayer(FPlatform.WidgetOverlayLayer);

            // seems like possibly this geometry will be shown this frame, before PreRender()
            // is called, which means that on next frame the geometry will pop. 
            // So we hide here and show in PreRender
            gizmo.Hide();
        }



        // configure gizmo - constructs necessary ITransformWrapper to provide
        // desired gizmo behavior. You can modify behavior in subclasses by
        // overriding InitializeTransformWrapper (but you must 
        void SetActiveFrame(FrameType eFrame) {

            // disconect existing wrapper
            if (targetWrapper != null)
                targetWrapper.Target.OnTransformModified -= onTransformModified;

            // if we have multiple targets, we construct a transient SO to
            // act as a parent (stored as internalGroupSO)
            if ( targets.Count > 1 && internalGroupSO == null ) {
                internalGroupSO = new TransientGroupSO();
                internalGroupSO.Create();
                parentScene.AddSceneObject(internalGroupSO);
                internalGroupSO.AddChildren(targets);
            }
            SceneObject useSO = (targets.Count == 1) ? targets[0] : internalGroupSO;

            // construct the wrapper
            targetWrapper = InitializeTransformWrapper(useSO, eFrame);

            //connect up to it
            targetWrapper.Target.OnTransformModified += onTransformModified;
            onTransformModified(null);

            // configure gizmo
            if (uniform_scale != null)
                uniform_scale.SetVisible( targetWrapper.SupportsScaling && (eEnabledWidgets & AxisGizmoFlags.UniformScale) != 0);
		}


        // you can override this to modify behavior. Note that this default
        // implementation currently uses some internal members for the relative-xform case
        virtual protected ITransformWrapper InitializeTransformWrapper(SceneObject useSO, FrameType eFrame)
        {
            if (frameSourceSO != null) {
                internalXFormSO = new TransientXFormSO();
                internalXFormSO.Create();
                parentScene.AddSceneObject(internalXFormSO);
                internalXFormSO.ConnectTarget(frameSourceSO, useSO);
                return new PassThroughWrapper(internalXFormSO);

            } else  if (eFrame == FrameType.LocalFrame) {
				return new PassThroughWrapper (useSO);

			} else {
				return new SceneFrameWrapper(parentScene, useSO);
			}

        }




        public bool SupportsReferenceObject { get { return true;  } }
        public void SetReferenceObject(SceneObject sourceSO)
        {
            if (sourceSO != null && frameSourceSO == sourceSO)
                return;     // ignore repeats as this is kind of expensive

            if (internalXFormSO != null) {
                internalXFormSO.DisconnectTarget();
                parentScene.RemoveSceneObject(internalXFormSO, true);
                internalXFormSO = null;
            }

            frameSourceSO = sourceSO;
            SetActiveFrame(eCurrentFrameMode);
        }




        virtual protected void onTransformModified(SceneObject so)
        {
            // keep widget synced with object frame of target
            Frame3f widgetFrame = targetWrapper.GetLocalFrame(CoordSpace.ObjectCoords);
            gizmo.SetLocalPosition( widgetFrame.Origin );
            gizmo.SetLocalRotation( widgetFrame.Rotation );
        }


        void update_active()
        {
            if (translate_x != null) translate_x.SetActive((eEnabledWidgets & AxisGizmoFlags.AxisTranslateX) != 0);
            if (translate_y != null) translate_y.SetActive((eEnabledWidgets & AxisGizmoFlags.AxisTranslateY) != 0);
            if (translate_z != null) translate_z.SetActive((eEnabledWidgets & AxisGizmoFlags.AxisTranslateZ) != 0);

            if (rotate_x != null) rotate_x.SetActive((eEnabledWidgets & AxisGizmoFlags.AxisRotateX) != 0 );
            if (rotate_y != null) rotate_y.SetActive((eEnabledWidgets & AxisGizmoFlags.AxisRotateY) != 0);
            if (rotate_z != null) rotate_z.SetActive((eEnabledWidgets & AxisGizmoFlags.AxisRotateZ) != 0);

            if (translate_yz != null) translate_yz.SetActive((eEnabledWidgets & AxisGizmoFlags.PlaneTranslateX) != 0);
            if (translate_xz != null) translate_xz.SetActive((eEnabledWidgets & AxisGizmoFlags.PlaneTranslateY) != 0);
            if (translate_xy != null) translate_xy.SetActive((eEnabledWidgets & AxisGizmoFlags.PlaneTranslateZ) != 0);

            if (uniform_scale != null) uniform_scale.SetActive((eEnabledWidgets & AxisGizmoFlags.UniformScale) != 0);
        }




        // subwidget access
        public AxisRotationWidget GetAxisRotationWidget(int axis) {
            if (axis == 0)
                return (rotate_x == null) ? null : Widgets[rotate_x] as AxisRotationWidget;
            else if ( axis == 1 )
                return (rotate_y == null) ? null : Widgets[rotate_y] as AxisRotationWidget;
            else if (axis == 2)
                return (rotate_z == null) ? null : Widgets[rotate_z] as AxisRotationWidget;
            throw new ArgumentOutOfRangeException("AxisTransformGizmo.RotationWidget: invalid axis index " + axis.ToString());
        }
        public AxisTranslationWidget GetAxisTranslationWidget(int axis) {
            if (axis == 0)
                return (translate_x == null) ? null : Widgets[translate_x] as AxisTranslationWidget;
            else if (axis == 1)
                return (translate_y == null) ? null : Widgets[translate_y] as AxisTranslationWidget;
            else if (axis == 2)
                return (translate_z == null) ? null : Widgets[translate_z] as AxisTranslationWidget;
            throw new ArgumentOutOfRangeException("AxisTransformGizmo.AxisTranslationWidget: invalid axis index " + axis.ToString());
        }
        public PlaneTranslationWidget GetPlaneTranslationWidget(int axis) {
            if (axis == 0)
                return (translate_yz == null) ? null : Widgets[translate_yz] as PlaneTranslationWidget;
            else if (axis == 1)
                return (translate_xz == null) ? null : Widgets[translate_xz] as PlaneTranslationWidget;
            else if (axis == 2)
                return (translate_xy == null) ? null : Widgets[translate_xy] as PlaneTranslationWidget;
            throw new ArgumentOutOfRangeException("AxisTransformGizmo.PlaneTranslationWidget: invalid axis index " + axis.ToString());
        }
        public UniformScaleWidget GetUniformScaleWidget()
        {
            return (uniform_scale == null) ? null : Widgets[uniform_scale] as UniformScaleWidget;
        }






        public bool FindRayIntersection(Ray3f ray, out UIRayHit hit)
		{
			hit = null;
			GameObjectRayHit hitg = null;
			if (is_interactive && FindGORayIntersection(ray, out hitg)) {
				if (hitg.hitGO != null) {
					hit = new UIRayHit (hitg, this);
					return true;
				}
			}
			return false;
		}
        public bool FindHoverRayIntersection(Ray3f ray, out UIRayHit hit)
        {
            return FindRayIntersection(ray, out hit);
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
			if (Widgets.ContainsKey (e.hit.hitGO)) {
                Standard3DWidget w = Widgets [e.hit.hitGO];
				if (w.BeginCapture (targetWrapper, e.ray, e.hit.toUIHit() )) {
                    MaterialUtil.SetMaterial(w.RootGameObject, w.HoverMaterial);
                    targetWrapper.BeginTransformation ();
					activeWidget = w;
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
                activeWidget.UpdateCapture (targetWrapper, e.ray);
				return true;
			}
			return false;
		}

        virtual public bool EndCapture(InputEvent e)
		{
			if (activeWidget != null) {
                MaterialUtil.SetMaterial(activeWidget.RootGameObject, activeWidget.StandardMaterial);

                // tell wrapper we are done with capture, so it should bake transform/etc
                bool bModified = targetWrapper.DoneTransformation ();
                if (bModified) {
                    // update gizmo
                    onTransformModified(null);
                    // allow client/subclass to add any other change records
                    OnTransformInteractionEnd();
                    // gizmos drop change events by default
                    Scene.History.PushInteractionCheckpoint();
                }

				activeWidget = null;
			}
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

    }
}

 