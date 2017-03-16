using System;
using UnityEngine;
using System.Collections.Generic;
using g3;


namespace f3
{


    public class AxisTransformGizmoBuilder : ITransformGizmoBuilder
    {
        public bool SupportsMultipleObjects { get { return true; } }

        public ITransformGizmo Build(FScene scene, List<TransformableSO> targets)
        {
            var g = new AxisTransformGizmo();
            g.Create(scene, targets);
            return g;
        }
    }

    public class AxisTransformGizmo : GameObjectSet, ITransformGizmo
    {
		GameObject gizmo;
		GameObject x, y, z;
		GameObject rotate_x, rotate_y, rotate_z;
		GameObject translate_xy, translate_xz, translate_yz;
        GameObject uniform_scale;
        Bounds gizmoGeomBounds;

        TransientGroupSO internalGroupSO;

        TransformableSO frameSourceSO;
        TransientXFormSO internalXFormSO;

        SceneUIParent parent;
		FScene parentScene;
        List<TransformableSO> targets;
		ITransformWrapper targetWrapper;

		Dictionary<GameObject, Standard3DWidget> Widgets;
        Standard3DWidget activeWidget;
        Standard3DWidget hoverWidget;

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



		public GameObject RootGameObject {
			get { return gizmo; }
		}
        public List<TransformableSO> Targets
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
                internalXFormSO.Disconnect();
                parentScene.RemoveSceneObject(internalXFormSO, true);
                internalXFormSO = null;
            }
        }

        public virtual bool IsVisible {
            get { return RootGameObject.IsVisible(); }
            set { RootGameObject.SetVisible(value);}
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
               gizmo.transform.position,
               parentScene.ActiveCamera.GetPosition(),
               SceneGraphConfig.DefaultAxisGizmoVisualDegrees);
            fScaling /= parentScene.GetSceneScale();
            float fGeomDim = gizmoGeomBounds.size.magnitude;
            fScaling /= fGeomDim;
            gizmo.transform.localScale = new Vector3(fScaling, fScaling, fScaling);
        }

        public void Create(FScene parentScene, List<TransformableSO> targets) {
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

            x = AppendMeshGO ("x_translate", 
				(Mesh)Resources.Load ("transform_gizmo/axis_translate_x", typeof(Mesh)),
				xMaterial, gizmo);
            Widgets[x] = new AxisTranslationWidget(0) {
                RootGameObject = x, StandardMaterial = xMaterial, HoverMaterial = xHoverMaterial,
                TranslationScaleF = () => { return 1.0f / parentScene.GetSceneScale(); }
            };
			y = AppendMeshGO ("y_translate", 
				(Mesh)Resources.Load ("transform_gizmo/axis_translate_y", typeof(Mesh)),
				yMaterial, gizmo);
			Widgets [y] = new AxisTranslationWidget(1) {
                RootGameObject = y, StandardMaterial = yMaterial, HoverMaterial = yHoverMaterial,
                TranslationScaleF = () => { return 1.0f / parentScene.GetSceneScale(); }
            };
            z = AppendMeshGO ("z_translate", 
				(Mesh)Resources.Load ("transform_gizmo/axis_translate_z", typeof(Mesh)),
				zMaterial, gizmo);	
			Widgets [z] = new AxisTranslationWidget(2) {
                RootGameObject = z, StandardMaterial = zMaterial, HoverMaterial = zHoverMaterial,
                TranslationScaleF = () => { return 1.0f / parentScene.GetSceneScale(); }
            };


            rotate_x = AppendMeshGO ("x_rotate",
				(Mesh)Resources.Load ("transform_gizmo/axisrotate_x", typeof(Mesh)),
				xMaterial, gizmo);
			Widgets [rotate_x] = new AxisRotationWidget(0) {
                RootGameObject = rotate_x, StandardMaterial = xMaterial, HoverMaterial = xHoverMaterial };
            rotate_y = AppendMeshGO ("y_rotate",
				(Mesh)Resources.Load ("transform_gizmo/axisrotate_y", typeof(Mesh)),
				yMaterial, gizmo);
			Widgets [rotate_y] = new AxisRotationWidget(1) {
                RootGameObject = rotate_y, StandardMaterial = yMaterial, HoverMaterial = yHoverMaterial };
            rotate_z = AppendMeshGO ("z_rotate",
				(Mesh)Resources.Load ("transform_gizmo/axisrotate_z", typeof(Mesh)),
				zMaterial, gizmo);			
			Widgets [rotate_z] = new AxisRotationWidget(2) {
                RootGameObject = rotate_z, StandardMaterial = zMaterial, HoverMaterial = zHoverMaterial };


            // plane translation widgets
            translate_xy = AppendMeshGO ("xy_translate",
				(Mesh)Resources.Load ("transform_gizmo/plane_translate_xy", typeof(Mesh)),
				zMaterial, gizmo);
			Widgets [translate_xy] = new PlaneTranslationWidget(2) {
                RootGameObject = translate_xy, StandardMaterial = zMaterial, HoverMaterial = zHoverMaterial,
                TranslationScaleF = () => { return 1.0f / parentScene.GetSceneScale(); }
            };
            translate_xz = AppendMeshGO ("xz_translate",
				(Mesh)Resources.Load ("transform_gizmo/plane_translate_xz", typeof(Mesh)),
				yMaterial, gizmo);
			Widgets [translate_xz] = new PlaneTranslationWidget(1) {
                RootGameObject = translate_xz, StandardMaterial = yMaterial, HoverMaterial = yHoverMaterial,
                TranslationScaleF = () => { return 1.0f / parentScene.GetSceneScale(); }
            };
            translate_yz = AppendMeshGO ("yz_translate",
				(Mesh)Resources.Load ("transform_gizmo/plane_translate_yz", typeof(Mesh)),
				xMaterial, gizmo);
			Widgets [translate_yz] = new PlaneTranslationWidget(0) {
                RootGameObject = translate_yz, StandardMaterial = xMaterial, HoverMaterial = xHoverMaterial,
                TranslationScaleF = () => { return 1.0f / parentScene.GetSceneScale(); }
            };


            uniform_scale = AppendMeshGO("uniform_scale",
                Resources.Load<Mesh>("transform_gizmo/uniform_scale"), allMaterial, gizmo);
            Widgets[uniform_scale] = new UniformScaleWidget(parentScene.ActiveCamera) {
                RootGameObject = uniform_scale, StandardMaterial = allMaterial, HoverMaterial = allHoverMaterial,
                ScaleMultiplierF = () => { return 1.0f / parentScene.GetSceneScale(); }
            };

            gizmoGeomBounds = UnityUtil.GetGeometryBoundingBox( new List<GameObject>()
                { x,y,z,rotate_x,rotate_y,rotate_z,translate_xy,translate_xz,translate_yz,uniform_scale} );

            // disable shadows on widget components
            foreach ( var go in GameObjects )
                MaterialUtil.DisableShadows(go);

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
            TransformableSO useSO = (targets.Count == 1) ? targets[0] : internalGroupSO;

            // construct the wrapper
            targetWrapper = InitializeTransformWrapper(useSO, eFrame);

            //connect up to it
            targetWrapper.Target.OnTransformModified += onTransformModified;
            onTransformModified(null);

            // configure gizmo
            uniform_scale.SetVisible(targetWrapper.SupportsScaling);
		}


        // you can override this to modify behavior. Note that this default
        // implementation currently uses some internal members for the relative-xform case
        virtual protected ITransformWrapper InitializeTransformWrapper(TransformableSO useSO, FrameType eFrame)
        {
            if (frameSourceSO != null) {
                internalXFormSO = new TransientXFormSO();
                internalXFormSO.Create();
                parentScene.AddSceneObject(internalXFormSO);
                internalXFormSO.Connect(frameSourceSO, useSO);
                return new PassThroughWrapper(internalXFormSO);

            } else  if (eFrame == FrameType.LocalFrame) {
				return new PassThroughWrapper (useSO);

			} else {
				return new SceneFrameWrapper(parentScene, useSO);
			}

        }




        public bool SupportsReferenceObject { get { return true;  } }
        public void SetReferenceObject(TransformableSO sourceSO)
        {
            if (sourceSO != null && frameSourceSO == sourceSO)
                return;     // ignore repeats as this is kind of expensive

            if (internalXFormSO != null) {
                internalXFormSO.Disconnect();
                parentScene.RemoveSceneObject(internalXFormSO, true);
                internalXFormSO = null;
            }

            frameSourceSO = sourceSO;
            SetActiveFrame(eCurrentFrameMode);
        }




        void onTransformModified(TransformableSO so)
        {
            // keep widget synced with object frame of target
            Frame3f widgetFrame = targetWrapper.GetLocalFrame(CoordSpace.ObjectCoords);
            gizmo.transform.localPosition = widgetFrame.Origin;
            gizmo.transform.localRotation = widgetFrame.Rotation;
        }







		public bool FindRayIntersection (Ray ray, out UIRayHit hit)
		{
			hit = null;
			GameObjectRayHit hitg = null;
			if (FindGORayIntersection (ray, out hitg)) {
				if (hitg.hitGO != null) {
					hit = new UIRayHit (hitg, this);
					return true;
				}
			}
			return false;
		}
        public bool FindHoverRayIntersection(Ray ray, out UIRayHit hit)
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
            get { return true; }
        }
        public virtual void UpdateHover(Ray ray, UIRayHit hit)
        {
            if (hoverWidget != null)
                EndHover(ray);
            if (Widgets.ContainsKey(hit.hitGO)) {
                hoverWidget = Widgets[hit.hitGO];
                MaterialUtil.SetMaterial(hoverWidget.RootGameObject, hoverWidget.HoverMaterial);
            }
        }
        public virtual void EndHover(Ray ray)
        {
            if (hoverWidget != null) {
                MaterialUtil.SetMaterial(hoverWidget.RootGameObject, hoverWidget.StandardMaterial);
                hoverWidget = null;
            }
        }


    }
}

 