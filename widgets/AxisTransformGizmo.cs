using System;
using System.Collections.Generic;
using g3;


namespace f3
{


    public class AxisTransformGizmoBuilder : ITransformGizmoBuilder
    {
        public virtual bool SupportsMultipleObjects { get { return true; } }
        public IAxisGizmoWidgetFactory Factory = null;

        public float TranslateSpeed = 1.0f;
        public float ScaleSpeed = 1.0f;
        public bool DynamicVisibilityFiltering = true;
        public bool EnableRotationSnapping = true;
        public float RotationSnapStepSizeDeg = 5.0f;
        public float GizmoVisualDegrees = 0.0f;     // ignored unless non-zero
        public int GizmoLayer = -1;                 // ignored unless >= 0

        public virtual ITransformGizmo Build(FScene scene, List<SceneObject> targets)
        {
            var g = create_gizmo();
            g.ScaleSpeed = this.ScaleSpeed;
            g.TranslateSpeed = this.TranslateSpeed;
            g.DynamicVisibilityFiltering = this.DynamicVisibilityFiltering;
            g.EnableRotationSnapping = this.EnableRotationSnapping;
            g.RotationSnapStepSizeDeg = this.RotationSnapStepSizeDeg;
            g.GizmoVisualDegrees = this.GizmoVisualDegrees;
            g.GizmoLayer = this.GizmoLayer;
            g.Create(scene, targets);
            return g;
        }

        protected virtual AxisTransformGizmo create_gizmo() {
            return new AxisTransformGizmo(Factory);
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



    public interface IAxisGizmoWidgetFactory
    {
        bool Supports(AxisGizmoFlags widget);

        bool UniqueColorPerWidget { get; }
        fMaterial MakeMaterial(AxisGizmoFlags widget);
        fMaterial MakeHoverMaterial(AxisGizmoFlags widget);

        fMesh MakeGeometry(AxisGizmoFlags widget); 
    }



    public abstract class Standard3DTransformWidget : Standard3DWidget
    {
        protected float gizmoInitialRadius = 1.0f;
        public virtual void SetGizmoInitialRadius(float gizmoRadius) {
            this.gizmoInitialRadius = gizmoRadius;
        }

        protected float gizmoRadiusW = 1.0f;
        public virtual void UpdateGizmoWorldSize(float gizmoRadiusW) {
            this.gizmoRadiusW = gizmoRadiusW;
        }

        public abstract bool CheckVisibility(ref Frame3f curFrameW, ref Vector3d eyePosW);
    }


    /// <summary>
    /// 3D transformation gizmo
    ///    - axis translate and rotate
    ///    - planar translate
    ///    - uniform scale
    ///    
    /// [TODO] 
    /// </summary>
    public class AxisTransformGizmo : GameObjectSet, ITransformGizmo
    {
        public static readonly string DefaultName = "axis_transform";

        public IAxisGizmoWidgetFactory Factory;

        fGameObject root;
		fGameObject translate_x, translate_y, translate_z;
		fGameObject rotate_x, rotate_y, rotate_z;
		fGameObject translate_xy, translate_xz, translate_yz;
        fGameObject uniform_scale;
        AxisAlignedBox3f gizmoGeomBounds;
        float initialGizmoRadius;

        TransientGroupSO internalGroupSO;

        SceneObject frameSourceSO;
        TransientXFormSO internalXFormSO;

        SceneUIParent parent;
		FScene parentScene;
        List<SceneObject> targets;
		ITransformWrapper targetWrapper;

		Dictionary<fGameObject, Standard3DTransformWidget> Widgets;
        List<fGameObject> enabledWidgetGOs = new List<fGameObject>();
        Standard3DTransformWidget activeWidget;
        Standard3DTransformWidget hoverWidget;

        bool is_interactive = true;

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

        public float ScaleSpeed = 1.0f;
        public float TranslateSpeed = 1.0f;
        public bool DynamicVisibilityFiltering = true;
        public bool EnableRotationSnapping = true;
        public float RotationSnapStepSizeDeg = 5.0f;
        public float GizmoVisualDegrees = 0.0f;        // if 0, we use SceneGraphConfig.DefaultAxisGizmoVisualDegrees
        public int GizmoLayer = -1;                    // if -1, we use FPlatform.WidgetOverlayLayer

        // if false, transform changes will not be emitted
        public bool EmitChanges = true;

        //bool EnableDebugLogging;

        public AxisTransformGizmo(IAxisGizmoWidgetFactory widgetFactory = null)
		{
			Widgets = new Dictionary<fGameObject, Standard3DTransformWidget> ();
            Factory = (widgetFactory != null) ? widgetFactory : new DefaultAxisGizmoWidgetFactory();
            //EnableDebugLogging = false;
        }
        ~AxisTransformGizmo()
        {
            Util.gDevAssert(internalGroupSO == null);
            Util.gDevAssert(internalXFormSO == null);
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
			get { return root; }
		}
        public List<SceneObject> Targets
        {
            get { return targets; }
            set { Util.gDevAssert(false, "not implemented!"); }
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
            foreach (var w in Widgets)
                w.Value.Disconnect();
            Widgets.Clear();

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

        virtual public void SetLayer(int nLayer) {
            SetAllGOLayer(nLayer);
        }


        // called on per-frame Update()
        virtual public void PreRender() {
            root.Show();

            float useDegrees = (GizmoVisualDegrees > 0) ? GizmoVisualDegrees : SceneGraphConfig.DefaultAxisGizmoVisualDegrees;
            float fWorldSize = VRUtil.GetVRRadiusForVisualAngle(
               root.GetPosition(),
               parentScene.ActiveCamera.GetPosition(),
               useDegrees);
            float fSceneSize = fWorldSize / parentScene.GetSceneScale();
            float fGeomScale = fSceneSize / initialGizmoRadius;
            root.SetLocalScale( new Vector3f(fGeomScale) );

            foreach (var widget in Widgets)
                widget.Value.UpdateGizmoWorldSize(fWorldSize);

            if (DynamicVisibilityFiltering && targetWrapper != null) {
                Frame3f frameW = targetWrapper.GetLocalFrame(CoordSpace.WorldCoords);
                Vector3d camPosW = parentScene.ActiveCamera.GetPosition();
                foreach (var go in enabledWidgetGOs) {
                    Standard3DTransformWidget widget = Widgets[go];
                    bool visible = widget.CheckVisibility(ref frameW, ref camPosW);
                    go.SetVisible(visible);
                }
            }

        }

        public virtual void Create(FScene parentScene, List<SceneObject> targets) {
			this.parentScene = parentScene;
			this.targets = targets;

			root = GameObjectFactory.CreateParentGO("TransformGizmo");

            var xMaterial = Factory.MakeMaterial(AxisGizmoFlags.AxisTranslateX);
            var xHoverMaterial = Factory.MakeHoverMaterial(AxisGizmoFlags.AxisTranslateX);
            var yMaterial = Factory.MakeMaterial(AxisGizmoFlags.AxisTranslateY);
            var yHoverMaterial = Factory.MakeHoverMaterial(AxisGizmoFlags.AxisTranslateY);
            var zMaterial = Factory.MakeMaterial(AxisGizmoFlags.AxisTranslateZ);
            var zHoverMaterial = Factory.MakeHoverMaterial(AxisGizmoFlags.AxisTranslateZ);

            if ( Factory.Supports(AxisGizmoFlags.AxisTranslateX) )
                translate_x = append_widget(AxisGizmoFlags.AxisTranslateX, 0, "x_translate", xMaterial, xHoverMaterial);
            if (Factory.Supports(AxisGizmoFlags.AxisTranslateY))
                translate_y = append_widget(AxisGizmoFlags.AxisTranslateY, 1, "y_translate", yMaterial, yHoverMaterial);
            if (Factory.Supports(AxisGizmoFlags.AxisTranslateZ))
                translate_z = append_widget(AxisGizmoFlags.AxisTranslateZ, 2, "z_translate", zMaterial, zHoverMaterial);

            if (Factory.Supports(AxisGizmoFlags.AxisRotateX))
                rotate_x = append_widget(AxisGizmoFlags.AxisRotateX, 0, "x_rotate", xMaterial, xHoverMaterial);
            if (Factory.Supports(AxisGizmoFlags.AxisRotateY))
                rotate_y = append_widget(AxisGizmoFlags.AxisRotateY, 1, "y_rotate", yMaterial, yHoverMaterial);
            if (Factory.Supports(AxisGizmoFlags.AxisRotateZ))
                rotate_z = append_widget(AxisGizmoFlags.AxisRotateZ, 2, "z_rotate", zMaterial, zHoverMaterial);

            if (Factory.Supports(AxisGizmoFlags.PlaneTranslateX))
                translate_yz = append_widget(AxisGizmoFlags.PlaneTranslateX, 0, "yz_translate", xMaterial, xHoverMaterial);
            if (Factory.Supports(AxisGizmoFlags.PlaneTranslateY))
                translate_xz = append_widget(AxisGizmoFlags.PlaneTranslateY, 1, "xz_translate", yMaterial, yHoverMaterial);
            if (Factory.Supports(AxisGizmoFlags.PlaneTranslateZ))
                translate_xy = append_widget(AxisGizmoFlags.PlaneTranslateZ, 2, "xy_translate", zMaterial, zHoverMaterial);

            if (Factory.Supports(AxisGizmoFlags.UniformScale))
                uniform_scale = append_widget(AxisGizmoFlags.UniformScale, 0, "uniform_scale", null, null);

            gizmoGeomBounds = UnityUtil.GetGeometryBoundingBox(root, true);
            gizmoGeomBounds.Contain(Vector3d.Zero);
            initialGizmoRadius = gizmoGeomBounds.MaxDim;
            foreach (var widget in Widgets)
                widget.Value.SetGizmoInitialRadius(initialGizmoRadius);

            // disable shadows on widget components
            foreach ( var go in GameObjects )
                MaterialUtil.DisableShadows(go);

            update_active();

            eCurrentFrameMode = FrameType.LocalFrame;
			SetActiveFrame (eCurrentFrameMode);

            SetLayer( (GizmoLayer == -1) ? FPlatform.WidgetOverlayLayer : GizmoLayer );

            // seems like possibly this geometry will be shown this frame, before PreRender()
            // is called, which means that on next frame the geometry will pop. 
            // So we hide here and show in PreRender
            root.Hide();
        }


        virtual protected fGameObject append_widget(AxisGizmoFlags widgetType, int nAxis, string name, 
            fMaterial material, fMaterial hoverMaterial)
        {
            var useMaterial = (Factory.UniqueColorPerWidget || material == null) 
                ? Factory.MakeMaterial(widgetType) : material;
            var useHoverMaterial = (Factory.UniqueColorPerWidget || hoverMaterial == null) 
                ? Factory.MakeHoverMaterial(widgetType) : hoverMaterial;
            var go = AppendMeshGO(name, Factory.MakeGeometry(widgetType), useMaterial, RootGameObject, true);

            Standard3DTransformWidget widget = null;
            switch (widgetType) {
                case AxisGizmoFlags.AxisTranslateX:
                case AxisGizmoFlags.AxisTranslateY:
                case AxisGizmoFlags.AxisTranslateZ:
                    widget = new AxisTranslationWidget(nAxis) {
                        RootGameObject = go, StandardMaterial = useMaterial, HoverMaterial = useHoverMaterial,
                        TranslationScaleF = () => { return TranslateSpeed / parentScene.GetSceneScale(); }
                    };
                    break;

                case AxisGizmoFlags.AxisRotateX:
                case AxisGizmoFlags.AxisRotateY:
                case AxisGizmoFlags.AxisRotateZ:
                    widget = new AxisRotationWidget(nAxis) {
                        RootGameObject = go, StandardMaterial = useMaterial, HoverMaterial = useHoverMaterial,
                        EnableSnapping = EnableRotationSnapping,
                        SnapIncrementDeg = RotationSnapStepSizeDeg
                    };
                    break;

                case AxisGizmoFlags.PlaneTranslateX:
                case AxisGizmoFlags.PlaneTranslateY:
                case AxisGizmoFlags.PlaneTranslateZ:
                    widget = new PlaneTranslationWidget(nAxis) {
                        RootGameObject = go, StandardMaterial = useMaterial, HoverMaterial = useHoverMaterial,
                        TranslationScaleF = () => { return TranslateSpeed / parentScene.GetSceneScale(); }
                    };
                    break;

                case AxisGizmoFlags.UniformScale:
                    widget = new UniformScaleWidget(parentScene.ActiveCamera) {
                        RootGameObject = go, StandardMaterial = useMaterial, HoverMaterial = useHoverMaterial,
                        ScaleMultiplierF = () => { return ScaleSpeed / parentScene.GetSceneScale(); }
                    };
                    break;

                default:
                    throw new Exception("DefaultAxisGizmoWidgetFactory.MakeHoverMaterial: invalid widget type " + widget.ToString());
            }

            Widgets[go] = widget;
            return go;
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
            update_active();
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
            root.SetLocalPosition( widgetFrame.Origin );
            root.SetLocalRotation( widgetFrame.Rotation );
        }


        void update_active()
        {
            foreach (var go in Widgets.Keys)
                go.SetVisible(false);
            enabledWidgetGOs.Clear();

            if (translate_x != null && (eEnabledWidgets & AxisGizmoFlags.AxisTranslateX) != 0)  enabledWidgetGOs.Add(translate_x);
            if (translate_y != null && (eEnabledWidgets & AxisGizmoFlags.AxisTranslateY) != 0)  enabledWidgetGOs.Add(translate_y);
            if (translate_z != null && (eEnabledWidgets & AxisGizmoFlags.AxisTranslateZ) != 0)  enabledWidgetGOs.Add(translate_z);

            if (rotate_x != null && (eEnabledWidgets & AxisGizmoFlags.AxisRotateX) != 0) enabledWidgetGOs.Add(rotate_x);
            if (rotate_y != null && (eEnabledWidgets & AxisGizmoFlags.AxisRotateY) != 0) enabledWidgetGOs.Add(rotate_y);
            if (rotate_z != null && (eEnabledWidgets & AxisGizmoFlags.AxisRotateZ) != 0) enabledWidgetGOs.Add(rotate_z);

            if (translate_yz != null && (eEnabledWidgets & AxisGizmoFlags.PlaneTranslateX) != 0) enabledWidgetGOs.Add(translate_yz);
            if (translate_xz != null && (eEnabledWidgets & AxisGizmoFlags.PlaneTranslateY) != 0) enabledWidgetGOs.Add(translate_xz);
            if (translate_xy != null && (eEnabledWidgets & AxisGizmoFlags.PlaneTranslateZ) != 0) enabledWidgetGOs.Add(translate_xy);

            if (uniform_scale != null) {
                if ( (eEnabledWidgets & AxisGizmoFlags.UniformScale) != 0)
                    enabledWidgetGOs.Add(uniform_scale);
                if (targetWrapper != null && targetWrapper.SupportsScaling == false)
                    enabledWidgetGOs.Remove(uniform_scale);
            }

            foreach (var widget in enabledWidgetGOs)
                widget.SetVisible(true);
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
                Standard3DTransformWidget w = Widgets [e.hit.hitGO];
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
                activeWidget.EndCapture(targetWrapper);
                MaterialUtil.SetMaterial(activeWidget.RootGameObject, activeWidget.StandardMaterial);

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






    /// <summary>
    /// Default material/geometry provider for axis transformation gizmo
    /// </summary>
    public class DefaultAxisGizmoWidgetFactory : IAxisGizmoWidgetFactory
    {
        public float Alpha = 0.5f;
        public int OverrideRenderQueue = -1;

        public virtual bool Supports(AxisGizmoFlags widget) {
            return true;
        }

        public virtual bool UniqueColorPerWidget {
            get { return false; }
        }

        static fMaterial XMaterial, YMaterial, ZMaterial, AllMaterial;

        public virtual fMaterial MakeMaterial(AxisGizmoFlags widget)
        {
            switch (widget) {
                case AxisGizmoFlags.AxisRotateX:
                case AxisGizmoFlags.AxisTranslateX:
                case AxisGizmoFlags.PlaneTranslateX:
                    if (XMaterial == null) {
                        XMaterial = MaterialUtil.CreateTransparentMaterial(Colorf.VideoRed, Alpha);
                        if (OverrideRenderQueue != -1)
                            XMaterial.renderQueue = OverrideRenderQueue;
                    }
                    return XMaterial;

                case AxisGizmoFlags.AxisRotateY:
                case AxisGizmoFlags.AxisTranslateY:
                case AxisGizmoFlags.PlaneTranslateY:
                    if (YMaterial == null) {
                        YMaterial = MaterialUtil.CreateTransparentMaterial(Colorf.VideoGreen, Alpha);
                        if (OverrideRenderQueue != -1)
                            YMaterial.renderQueue = OverrideRenderQueue;
                    }
                    return YMaterial;

                case AxisGizmoFlags.AxisRotateZ:
                case AxisGizmoFlags.AxisTranslateZ:
                case AxisGizmoFlags.PlaneTranslateZ:
                    if (ZMaterial == null) { 
                        ZMaterial = MaterialUtil.CreateTransparentMaterial(Colorf.VideoBlue, Alpha);
                        if (OverrideRenderQueue != -1)
                            ZMaterial.renderQueue = OverrideRenderQueue;
                    }
                    return ZMaterial;

                case AxisGizmoFlags.UniformScale:
                    if (AllMaterial == null) {
                        AllMaterial = MaterialUtil.CreateTransparentMaterial(Colorf.VideoWhite, Alpha);
                        if (OverrideRenderQueue != -1)
                            AllMaterial.renderQueue = OverrideRenderQueue;
                    }
                    return AllMaterial;

                default:
                    throw new Exception("DefaultAxisGizmoWidgetFactory.MakeMaterial: invalid widget type " + widget.ToString());
            }
        }


        static fMaterial XHover, YHover, ZHover, AllHover;


        public virtual fMaterial MakeHoverMaterial(AxisGizmoFlags widget)
        {
            switch (widget) {
                case AxisGizmoFlags.AxisRotateX:
                case AxisGizmoFlags.AxisTranslateX:
                case AxisGizmoFlags.PlaneTranslateX:
                    if (XHover == null) {
                        XHover = MaterialUtil.CreateTransparentMaterial(Colorf.VideoRed);
                        if (OverrideRenderQueue != -1)
                            XHover.renderQueue = OverrideRenderQueue;
                    }
                    return XHover;

                case AxisGizmoFlags.AxisRotateY:
                case AxisGizmoFlags.AxisTranslateY:
                case AxisGizmoFlags.PlaneTranslateY:
                    if (YHover == null) {
                        YHover = MaterialUtil.CreateTransparentMaterial(Colorf.VideoGreen);
                        if (OverrideRenderQueue != -1)
                            YHover.renderQueue = OverrideRenderQueue;
                    }
                    return YHover;

                case AxisGizmoFlags.AxisRotateZ:
                case AxisGizmoFlags.AxisTranslateZ:
                case AxisGizmoFlags.PlaneTranslateZ:
                    if (ZHover == null) {
                        ZHover = MaterialUtil.CreateTransparentMaterial(Colorf.VideoBlue);
                        if (OverrideRenderQueue != -1)
                            ZHover.renderQueue = OverrideRenderQueue;
                    }
                    return ZHover;

                case AxisGizmoFlags.UniformScale:
                    if (AllHover == null) {
                        AllHover = MaterialUtil.CreateTransparentMaterial(Colorf.VideoWhite);
                        if (OverrideRenderQueue != -1)
                            AllHover.renderQueue = OverrideRenderQueue;
                    }
                    return AllHover;

                default:
                    throw new Exception("DefaultAxisGizmoWidgetFactory.MakeHoverMaterial: invalid widget type " + widget.ToString());
            }
        }


        static fMesh AxisTranslateX, AxisTranslateY, AxisTranslateZ;
        static fMesh AxisRotateX, AxisRotateY, AxisRotateZ;
        static fMesh PlaneTranslateX, PlaneTranslateY, PlaneTranslateZ;
        static fMesh UniformScale;


        public virtual fMesh MakeGeometry(AxisGizmoFlags widget)
        {
            switch (widget) {
                case AxisGizmoFlags.AxisTranslateX:
                    if (AxisTranslateX == null)
                        AxisTranslateX = FResources.LoadMesh("transform_gizmo/axis_translate_x");
                    return AxisTranslateX;
                case AxisGizmoFlags.AxisTranslateY:
                    if (AxisTranslateY == null)
                        AxisTranslateY = FResources.LoadMesh("transform_gizmo/axis_translate_y");
                    return AxisTranslateY;
                case AxisGizmoFlags.AxisTranslateZ:
                    if (AxisTranslateZ == null)
                        AxisTranslateZ = FResources.LoadMesh("transform_gizmo/axis_translate_z");
                    return AxisTranslateZ;


                case AxisGizmoFlags.AxisRotateX:
                    if (AxisRotateX == null)
                        AxisRotateX = FResources.LoadMesh("transform_gizmo/axisrotate_x");
                    return AxisRotateX;
                case AxisGizmoFlags.AxisRotateY:
                    if (AxisRotateY == null)
                        AxisRotateY = FResources.LoadMesh("transform_gizmo/axisrotate_y");
                    return AxisRotateY;
                case AxisGizmoFlags.AxisRotateZ:
                    if (AxisRotateZ == null)
                        AxisRotateZ = FResources.LoadMesh("transform_gizmo/axisrotate_z");
                    return AxisRotateZ;


                case AxisGizmoFlags.PlaneTranslateX:
                    if (PlaneTranslateX == null)
                        PlaneTranslateX = FResources.LoadMesh("transform_gizmo/plane_translate_yz");
                    return PlaneTranslateX;
                case AxisGizmoFlags.PlaneTranslateY:
                    if (PlaneTranslateY == null)
                        PlaneTranslateY = FResources.LoadMesh("transform_gizmo/plane_translate_xz");
                    return PlaneTranslateY;
                case AxisGizmoFlags.PlaneTranslateZ:
                    if (PlaneTranslateZ == null)
                        PlaneTranslateZ = FResources.LoadMesh("transform_gizmo/plane_translate_xy");
                    return PlaneTranslateZ;


                case AxisGizmoFlags.UniformScale:
                    if (UniformScale == null)
                        UniformScale = FResources.LoadMesh("transform_gizmo/uniform_scale");
                    return UniformScale;


                default:
                    throw new Exception("DefaultAxisGizmoWidgetFactory.MakeHoverMaterial: invalid widget type " + widget.ToString());
            }
        }
    }

}

 