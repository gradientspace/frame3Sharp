using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using g3;

namespace f3
{

    public enum SceneChangeType
    {
        Added, Removed, Modified
    }

    public delegate void SceneModifiedHandler(object sender, SceneObject so, SceneChangeType type);
	public delegate void SceneSelectionChangedHandler(object sender, EventArgs e);
    public delegate void TimeChangedHandler(object sender, EventArgs e);


	public class FScene : SceneUIParent, SOParent
	{
        ChangeHistory history;
        public ChangeHistory History { get { return history; } }

        public SORegistry TypeRegistry { get; set; }

        public SOMaterial DefaultSOMaterial { get; set; }
        public SOMaterial DefaultCurveSOMaterial { get; set; }
        public SOMaterial NewSOMaterial { get; set; }
        public SOMaterial TransparentNewSOMaterial { get; set; }
        public SOMaterial DefaultMeshSOMaterial { get; set; }
        public SOMaterial PivotSOMaterial { get; set; }
        public SOMaterial FrameSOMaterial { get; set; }

        public Material SelectedMaterial { get; set; }
        public Material FrameMaterial { get; set; }
        public Material PivotMaterial { get; set; }

        public SOType defaultPrimitiveType;

        public GenericAnimator ObjectAnimator { get; set; }

        FContext context;

        GameObject sceneRoot;
        GameObject scene_objects;


		List<SceneObject> vObjects;
		List<SceneObject> vSelected;
		List<SceneUIElement> vUIElements;
		List<GameObject> vBoundsObjects;


        // [RMS] deleted objects that are not destroyed are parented to this GO,
        //   which remains under sceneo. Assumption is these will not be visible.
        //   This allows us to "save" SOs for undo/redo
        GameObject deleted_objects;
        List<SceneObject> vDeleted;


        // animation
        double currentTime = 0;


        // camera stuff
        public float[] turntable_angles = { 0.0f, 0.0f };

		public FScene(FContext context)
		{
            this.context = context;

            history = new ChangeHistory();
            TypeRegistry = new SORegistry();

            vObjects = new List<SceneObject> ();
			vSelected = new List<SceneObject> ();
			vUIElements = new List<SceneUIElement> ();
			vBoundsObjects = new List<GameObject> ();
            ObjectAnimator = new GenericAnimator();

			sceneRoot = new GameObject ("Scene");
            // for animation playbacks
            sceneRoot.AddComponent<SceneAnimator>().Scene = this;        
            sceneRoot.AddComponent<UnityPerFrameAnimationBehavior>().Animator = ObjectAnimator;                

            scene_objects = new GameObject("scene_objects");
            UnityUtil.AddChild(sceneRoot, scene_objects, false);

            deleted_objects = new GameObject("deleted_objects");
            UnityUtil.AddChild(sceneRoot, deleted_objects, false);
            vDeleted = new List<SceneObject>();

            // initialize materials
            DefaultSOMaterial = new SOMaterial() {
                Name = "DefaultSO",
                Type = SOMaterial.MaterialType.StandardRGBColor, RGBColor = ColorUtil.StandardBeige
            };
            DefaultCurveSOMaterial = new SOMaterial() {
                Name = "DefaultCurveSO",
                Type = SOMaterial.MaterialType.UnlitRGBColor, RGBColor = Colorf.DarkSlateGrey
            };
            DefaultMeshSOMaterial = new SOMaterial() {
                Name = "DefaultMeshSO",
                Type = SOMaterial.MaterialType.PerVertexColor, RGBColor = Colorf.White
            };
            NewSOMaterial = new SOMaterial() {
                Name = "NewSO",
                Type = SOMaterial.MaterialType.StandardRGBColor, RGBColor = Colorf.CornflowerBlue
            };
            TransparentNewSOMaterial = new SOMaterial() {
                Name = "NewSO",
                Type = SOMaterial.MaterialType.TransparentRGBColor, RGBColor = new Colorf(Colorf.CornflowerBlue, 0.5f)
            };
            PivotSOMaterial = new SOMaterial() {
                Name = "PivotSO",
                Type = SOMaterial.MaterialType.TransparentRGBColor, RGBColor = ColorUtil.PivotYellow.SetAlpha(0.75f)
            };
            FrameSOMaterial = new SOMaterial() {
                Name = "PivotFrame",
                Type = SOMaterial.MaterialType.StandardRGBColor, RGBColor = ColorUtil.DarkGrey
            };

            SelectedMaterial = MaterialUtil.CreateStandardMaterial( ColorUtil.SelectionGold );
            FrameMaterial = MaterialUtil.CreateStandardMaterial(ColorUtil.DarkGrey);
            PivotMaterial = MaterialUtil.ToUnityMaterial(PivotSOMaterial);

            defaultPrimitiveType = SOTypes.Cylinder;
        }

        public FContext Context {
            get { return this.context; }
        }
        public fCamera ActiveCamera {
            get { return this.context.ActiveCamera; }
        }

        public GameObject RootGameObject {
			get { return sceneRoot; }
		}


        public double CurrentTime
        {
            get { return currentTime; }
            set { SetCurrentTime(value); }
        }
        public void SetCurrentTime(double time)
        {
            if (currentTime != time) {
                foreach (SceneObject so in vObjects)
                    so.SetCurrentTime(time);
                currentTime = time;
                FUtil.SafeSendAnyEvent(TimeChangedEvent, this, null);
            }
        }

        public SceneAnimator AnimationController {
            get { return sceneRoot.GetComponent<SceneAnimator>(); }
        }


        public event TimeChangedHandler TimeChangedEvent;
		public event SceneSelectionChangedHandler SelectionChangedEvent;
        public event SceneModifiedHandler ChangedEvent;

		protected virtual void OnSelectionChanged(EventArgs e) {
            FUtil.SafeSendEvent(SelectionChangedEvent, this, e);
		}
		protected virtual void OnSceneChanged(SceneObject so, SceneChangeType type) {
            FUtil.SafeSendAnyEvent(ChangedEvent, this, so, type);
		}



        public SOType DefaultPrimitiveType
        {
            get { return defaultPrimitiveType; }
            set { if (value.hasTag(SOType.TagPrimitive) == false)
                    throw new InvalidOperationException("Scene.DefaultPrimitiveType: tried to set to type " + value.identifier + " which is not a Primitive type!");
                  defaultPrimitiveType = value;
            }
        }



        public AxisAlignedBox3f GetBoundingBox(bool bIncludeBoundsObjects)
        {
            AxisAlignedBox3f b = UnityUtil.InvalidBounds;

            foreach ( SceneObject so in SceneObjects ) {
                b.Contain(so.GetTransformedBoundingBox());
            }
            if (b == UnityUtil.InvalidBounds || bIncludeBoundsObjects)
                UnityUtil.Combine(b, UnityUtil.GetBoundingBox(BoundsObjects));
            if (b == UnityUtil.InvalidBounds) {
                b.Contain(Vector3.zero);
                b.Expand(1.0f);
            }
            return b;
        }



        public List<GameObject> BoundsObjects
        {
            get { return vBoundsObjects; }
        }
        public void AddWorldBoundsObject(GameObject obj)
		{
			vBoundsObjects.Add (obj);
			obj.transform.SetParent (scene_objects.transform, false);
		}
        public void RemoveWorldBoundsObject(GameObject obj)
        {
            obj.transform.SetParent(null);
            vBoundsObjects.Remove(obj);
        }


        public List<SceneObject> SceneObjects { 
			get { return vObjects; }
		}

        // add new SO to scene
        public void AddSceneObject(SceneObject so, bool bUseExistingWorldPos = false)
		{
            DebugUtil.Log(1, "[Scene.AddSceneObject] adding {0}", so.Name);

            vObjects.Add(so);
            so.SetScene(this);
            so.RootGameObject.transform.SetParent(scene_objects.transform, bUseExistingWorldPos);
            so.Parent = this;
            so.SetCurrentTime(currentTime);

            OnSceneChanged(so, SceneChangeType.Added);
        }

        // this removes so from a SO/parent hierarchy and parents to Scene instead
        public void ReparentSceneObject(SceneObject so, bool bKeepPosition = true)
        {
            so.RootGameObject.transform.SetParent(null);
            so.RootGameObject.transform.SetParent(scene_objects.transform, bKeepPosition);
            so.Parent = this;
        }

        // grouping support. currently does not properly handle undo!
        public void AddSceneObjectToParentSO(SceneObject so, SceneObject parent)
        {
            if (parent is SOParent == false)
                DebugUtil.Error("FSCene.AddSceneObjectToParentSO: parent does not implement SOParent interface!");
            vObjects.Remove(so);
            parent.RootGameObject.AddChild(so.RootGameObject, true);
            so.Parent = parent as SOParent;
        }
        public void RemoveSceneObjectFromParentSO(SceneObject so)
        {
            if ( vObjects.Contains(so) == false )
                vObjects.Add(so);
            so.RootGameObject.transform.SetParent(null);
            so.RootGameObject.transform.SetParent(scene_objects.transform, true);
            so.Parent = this;
        }


        // remove SO from scene. 
        // bDestroy means that SO is actually deleted, otherwise just added 
        // to internal Deleted set, so it can be recovered by undo.
        public void RemoveSceneObject(SceneObject so, bool bDestroy)
        {
            DebugUtil.Log(1, "[Scene.AddSceneObject] removing {0} (destroy: {1})", so.Name, bDestroy);

            if (vSelected.Contains(so)) {
                Deselect(so);
            }
            vObjects.Remove(so);
            OnSceneChanged(so, SceneChangeType.Removed);

            if (so.RootGameObject != null) {
                if (bDestroy) {
                    SceneUtil.DestroySO(so);
                } else {
                    // add to deleted set
                    vDeleted.Add(so);
                    UnityUtil.AddChild(deleted_objects, so.RootGameObject, true);
                    so.RootGameObject.SetVisible(false);
                }
            }
        }
        public void RemoveAllSceneObjects()
        {
            while (vObjects.Count > 0)
                RemoveSceneObject(vObjects[0], true);
        }

        public bool HasDeletedSceneObject(SceneObject so)
        {
            return (vDeleted.Find((x) => x == so) != null);
        }
        public void RestoreDeletedSceneObject(SceneObject so)
        {
            if (vDeleted.Find((x) => x == so) == null)
                return;
            vDeleted.Remove(so);
            vObjects.Add(so);
            so.RootGameObject.SetVisible(true);
            UnityUtil.AddChild(scene_objects, so.RootGameObject, true);
            so.SetCurrentTime(currentTime);
        }
        public void CullDeletedSceneObject(SceneObject so)
        {
            if (vDeleted.Find((x) => x == so) == null)
                return;
            vDeleted.Remove(so);
            so.RootGameObject.transform.parent = null;
            UnityEngine.Object.Destroy(so.RootGameObject);
        }


        public ReadOnlyCollection<SceneObject> Selected {
			get { return vSelected.AsReadOnly(); }
		}

		public bool IsSelected(SceneObject s) {
			var found = vSelected.Find (x => x == s);
			return (found != null);
		}

		public bool Select(SceneObject s, bool bReplace) {
			if (!IsSelected (s)) {
                if (bReplace)
                    ClearSelection();

				vSelected.Add (s);
                s.PushOverrideMaterial(SelectedMaterial);

                OnSelectionChanged(EventArgs.Empty);

				return true;
			}
			return false;
		}

		public void Deselect(SceneObject s) {
            s.PopOverrideMaterial();        // assume we only pushed once!
			vSelected.Remove (s);
			OnSelectionChanged (EventArgs.Empty);
		}

		public void ClearSelection() {
            foreach (var v in vSelected)
                v.PopOverrideMaterial();
			vSelected = new List<SceneObject> ();
			OnSelectionChanged (EventArgs.Empty);
		}


        public List<SceneObject> Find(Func<SceneObject, bool> filter)
        {
            return SceneObjects.FindAll( (x) => { return filter(x); } );
        }





        public List<SceneUIElement> UIElements { 
			get { return vUIElements; }
		}

		public void AddUIElement(SceneUIElement e, bool bIsInLocalFrame = true) {
			vUIElements.Add (e);
            e.Parent = this;
			if (e.RootGameObject != null) {
				// assume gizmo transform is set to a local transform, so we want to apply current scene transform
				e.RootGameObject.transform.SetParent (sceneRoot.transform, (bIsInLocalFrame == false));
			}
		}

		public void RemoveUIElement(SceneUIElement e, bool bDestroy) {
            e.Disconnect();
            e.Parent = null;
			vUIElements.Remove (e);
			if ( e.RootGameObject != null && bDestroy) {
				e.RootGameObject.transform.parent = null;
				UnityEngine.Object.Destroy (e.RootGameObject);
			}
		}

        public void RemoveAllUIElements()
        {
            while (vUIElements.Count > 0)
                RemoveUIElement(vUIElements[0], true);
        }



        public void PreRender()
        {
            foreach (var ui in vUIElements)
                ui.PreRender();
            foreach (var so in vObjects)
                so.PreRender();
        }




		public bool FindUIRayIntersection(Ray ray, out UIRayHit hit) {
            return HUDUtil.FindNearestRayIntersection(vUIElements, ray, out hit);
        }

        public bool FindUIHoverRayIntersection(Ray ray, out UIRayHit hit) {
            return HUDUtil.FindNearestHoverRayIntersection(vUIElements, ray, out hit);
        }

        public bool FindSORayIntersection(Ray ray, out SORayHit hit, Func<SceneObject, bool> filter = null) {
            return HUDUtil.FindNearestRayIntersection(SceneObjects, ray, out hit, filter);
        }

        public bool FindSORayIntersection_PivotPriority(Ray ray, out SORayHit hit, Func<SceneObject, bool> filter = null)
        {
            bool bHitPivot = HUDUtil.FindNearestRayIntersection(SceneObjects, ray, out hit, (s) => { return s is PivotSO; });
            if (bHitPivot)
                return true;
            return HUDUtil.FindNearestRayIntersection(SceneObjects, ray, out hit, filter);
        }


        // does not test bounds!
        // [TODO] this is going to be weird... need to test bounds, I think
        public bool FindAnyRayIntersection(Ray ray, out AnyRayHit hit) {
			hit = null;

			UIRayHit bestUIHit = null;
			SORayHit bestSOHit = null;

			foreach (var ui in vUIElements) {
				UIRayHit uiHit;
				if (ui.FindRayIntersection (ray, out uiHit)) {
					if (bestUIHit == null || uiHit.fHitDist < bestUIHit.fHitDist)
						bestUIHit = uiHit;
				}
			}
			foreach (var so in vObjects) {
				SORayHit objHit;
				if (so.FindRayIntersection (ray, out objHit)) {
					if (bestSOHit == null || objHit.fHitDist < bestSOHit.fHitDist)
						bestSOHit = objHit;
				}
			}
			if (bestUIHit != null) {
				if (bestSOHit == null || bestSOHit.fHitDist > bestUIHit.fHitDist)
					hit = new AnyRayHit (bestUIHit);
				else
					hit = new AnyRayHit (bestSOHit);
			} else if (bestSOHit != null)
				hit = new AnyRayHit (bestSOHit);

			return (hit != null);
		}




		public bool FindWorldBoundsHit(Ray ray, out GameObjectRayHit hit) {
			hit = null;
			foreach (var go in this.vBoundsObjects) {
				GameObjectRayHit myHit = null;
				if (UnityUtil.FindGORayIntersection (ray, go, out myHit)) {
					if (hit == null || myHit.fHitDist < hit.fHitDist)
						hit = myHit;
				}
			}
			return (hit != null);		
		}


        // tests SceneObjects and Bounds
        public bool FindSceneRayIntersection(Ray ray, out AnyRayHit hit, Func<SceneObject, bool> sofilter = null)
        {
            hit = null;

            SORayHit bestSOHit = null;
            GameObjectRayHit bestBoundsHit = null;

            bool bHitSO = FindSORayIntersection(ray, out bestSOHit, sofilter);
            bool bHitBounds = FindWorldBoundsHit(ray, out bestBoundsHit);
            if ( bHitSO && bHitBounds ) {
                if ( bestSOHit.fHitDist < bestBoundsHit.fHitDist )
                    hit = new AnyRayHit(bestSOHit);
                else
                    hit =new AnyRayHit(bestBoundsHit, HitType.BoundsObjectHit);
            } else if (bHitSO) {
                hit = new AnyRayHit(bestSOHit);
            } else if ( bHitBounds ) {
                hit = new AnyRayHit(bestBoundsHit, HitType.BoundsObjectHit);
            }
            return (hit != null);
        }



        // SOParent interface
		public virtual Frame3f GetLocalFrame(CoordSpace eSpace)
        {
            return SceneFrame;
        }
        public virtual Vector3f GetLocalScale()
        {
            return new Vector3f(GetSceneScale());
        }



        // Scene RootGameObject is a top-level GO, so World and Object coords are the same!
        public Frame3f SceneFrame {
            get { return UnityUtil.GetGameObjectFrame(this.RootGameObject, CoordSpace.WorldCoords); }
            set { UnityUtil.SetGameObjectFrame(this.RootGameObject, value, CoordSpace.WorldCoords); }
        }


        // this is for supporting global scaling of scene. Causes lots of complications, though...

        public float GetSceneScale() {
            return RootGameObject.GetLocalScale()[0];
        }
        public void SetSceneScale(float f) {
            RootGameObject.SetLocalScale(f * Vector3f.One);
        }

        public float ToWorldDimension(float fScene) {
            return fScene * GetSceneScale();
        }
        public float ToSceneDimension(float fWorld) {
            return fWorld / GetSceneScale();
        }

        public Vector3f ToWorldP(Vector3f ptScene) {
            return SceneFrame.FromFrameP( GetSceneScale() * ptScene);
        }
        public Vector3f ToSceneP(Vector3f ptWorld) {
            return SceneFrame.ToFrameP(ptWorld) / GetSceneScale();
        }
        public Vector3d ToWorldP(Vector3d ptScene) {
            return (Vector3d)SceneFrame.FromFrameP( GetSceneScale() * (Vector3f)ptScene );
        }
        public Vector3d ToSceneP(Vector3d ptWorld) {
            return (Vector3d)SceneFrame.ToFrameP((Vector3f)ptWorld) / GetSceneScale();
        }

        public Vector3f ToWorldN(Vector3f normalScene)
        {
            return SceneFrame.FromFrameV(normalScene);
        }
        public Vector3f ToSceneN(Vector3f normalWorld)
        {
            return SceneFrame.ToFrameV(normalWorld);
        }

        public Ray3f ToWorldRay(Ray3f sceneRay)
        {
            return new Ray3f(ToWorldP(sceneRay.Origin), ToWorldN(sceneRay.Direction));
        }
        public Ray3f ToSceneRay(Ray3f worldRay)
        {
            return new Ray3f(ToSceneP(worldRay.Origin), ToSceneN(worldRay.Direction));
        }


        public Frame3f ToWorldFrame(Frame3f fSceneFrame) {
            return SceneFrame.FromFrame( fSceneFrame.Scaled(GetSceneScale())  );
        }
        public Frame3f ToSceneFrame(Frame3f fWorldFrame)
        {
            return SceneFrame.ToFrame(fWorldFrame).Scaled(1.0f / GetSceneScale());
        }


        //public Vector3f TransferPoint(SceneObject fromSO, SceneObject toSO, Vector3f point)
        //{
        //}

    }
}

