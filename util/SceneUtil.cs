using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    public class SceneUtil
    {
        // used to indicate that this object is transient
        static public string InvalidUUID = "77777777-7777-7777-7777-777777777777";



        public static bool FindNearestRayIntersection(IEnumerable<SceneObject> vSceneObjects, Ray3f ray, out SORayHit hit) {
            hit = null;
            foreach (var so in vSceneObjects) {
                SORayHit soHit;
                if (so.FindRayIntersection(ray, out soHit)) {
                    if (hit == null || soHit.fHitDist < hit.fHitDist)
                        hit = soHit;
                }
            }
            return (hit != null);
        }
        public static bool FindNearestRayIntersection(IEnumerable<SceneObject> vSceneObjects, Func<SceneObject,bool> filter, Ray3f ray, out SORayHit hit)
        {
            hit = null;
            foreach (var so in vSceneObjects) {
                if (filter(so)) {
                    SORayHit soHit;
                    if (so.FindRayIntersection(ray, out soHit)) {
                        if (hit == null || soHit.fHitDist < hit.fHitDist)
                            hit = soHit;
                    }
                }
            }
            return (hit != null);
        }





        public static bool FindNearestPoint(IEnumerable<SceneObject> vSceneObjects, Vector3d point, double maxDist, out SORayHit nearest, CoordSpace eSpace = CoordSpace.WorldCoords) {
            nearest = null;
            foreach (var so in vSceneObjects) {
                if (so is SpatialQueryableSO) {
                    SpatialQueryableSO spatialSO = so as SpatialQueryableSO;
                    if (spatialSO.SupportsNearestQuery) {
                        SORayHit soNearest;
                        if (spatialSO.FindNearest(point, maxDist, out soNearest, eSpace)) {
                            if (nearest == null || soNearest.fHitDist < nearest.fHitDist)
                                nearest = soNearest;
                        }
                    }
                }
            }
            return (nearest != null);
        }



        // descends parent/child SO hierarchy and finds the set of topmost non-temporary SOs
        public static void FindAllPersistentTransformableChildren(SceneObject vParent, List<SceneObject> children)
        {
            if ( (vParent is SOCollection) == false )
                return;
            foreach ( SceneObject so in (vParent as SOCollection).GetChildren() ) {
                if (so.IsTemporary)
                    FindAllPersistentTransformableChildren(so, children);
                else
                    children.Add(so);
            }
        }


        public static Frame3f GetSOLocalFrame(SceneObject so, CoordSpace eSpace)
        {
            if (eSpace == CoordSpace.SceneCoords) {
                // new code maps object frame up to scene
                // [TODO] this is not the most efficient approach! can at least get the object
                //   frame directly, and avoid first local-to-obj xform
                Frame3f objF = Frame3f.Identity;
                Frame3f result = SceneTransforms.ObjectToScene(so, objF);

                // [RMS] old code that mapped up to world, and then down to scene
                //   Problem with this code is that it is unstable - if scene-to-world xform changes,
                //   then scene frame will numerically change. Which is a problem.
                //Frame3f sceneW = UnityUtil.GetGameObjectFrame(so.GetScene().RootGameObject, CoordSpace.WorldCoords);
                //Frame3f objW = UnityUtil.GetGameObjectFrame(so.RootGameObject, CoordSpace.WorldCoords);
                //Frame3f result = sceneW.ToFrame(objW);
                //// world coords have scene scale applied, we don't want that in scene coords
                //if (so.GetScene().GetSceneScale() != 1.0f)
                //    result = result.Scaled(1.0f / so.GetScene().GetSceneScale());

                return result;
            } else
                return UnityUtil.GetGameObjectFrame(so.RootGameObject, eSpace);
        }

        public static void SetSOLocalFrame(SceneObject so, CoordSpace eSpace, Frame3f newFrame)
        {
            if (eSpace == CoordSpace.SceneCoords) {
                // scene frames should not be scaled by scene scale, but we want to set as world
                // coords, so we need to apply it now
                //if (so.GetScene().GetSceneScale() != 1.0f)
                //    newFrame = newFrame.Scaled(so.GetScene().GetSceneScale());
                //Frame3f sceneW = UnityUtil.GetGameObjectFrame(so.GetScene().RootGameObject, CoordSpace.WorldCoords);
                //Frame3f objW = sceneW.FromFrame(newFrame);
                Frame3f objW = so.GetScene().ToWorldFrame(newFrame);
                UnityUtil.SetGameObjectFrame(so.RootGameObject, objW, CoordSpace.WorldCoords);
            } else
                UnityUtil.SetGameObjectFrame(so.RootGameObject, newFrame, eSpace);
        }



        public static void TranslateInFrame(SceneObject so, Vector3f translate, CoordSpace eSpace = CoordSpace.ObjectCoords)
        {
            Frame3f f = so.GetLocalFrame(eSpace);
            f.Origin += translate;
            so.SetLocalFrame(f, eSpace);
        }


        public static GOWrapperSO CombineAnySOs(SceneObject s1, SceneObject s2, bool bDeleteExisting = true)
        {
            FScene scene = s1.GetScene();

            if (scene.IsSelected(s1))
                scene.Deselect(s1);
            if (scene.IsSelected(s2))
                scene.Deselect(s2);

            fGameObject parentGO = GameObjectFactory.CreateParentGO("combined");
            GOWrapperSO.AppendSOGeometry(parentGO, s1, true);
            GOWrapperSO.AppendSOGeometry(parentGO, s2, true);

            GOWrapperSO wrapperSO = new GOWrapperSO() { AllowMaterialChanges = false };
            wrapperSO.Create(parentGO);

            if ( bDeleteExisting ) {
                scene.RemoveSceneObject(s1, false);
                scene.RemoveSceneObject(s2, false);
            }

            scene.AddSceneObject(wrapperSO, false);

            return wrapperSO;
        }




        public static GroupSO CreateGroupSO(SceneObject so1, SceneObject so2)
        {
            FScene scene = so1.GetScene();
            if (scene.IsSelected(so1))
                scene.Deselect(so1);
            if (scene.IsSelected(so2))
                scene.Deselect(so2);

            GroupSO group = new GroupSO();
            group.Create();

            scene.AddSceneObject(group);

            group.AddChild(so1);
            group.AddChild(so2);

            return group;
        }



        public static void AppendMeshSO(DMeshSO appendTo, DMeshSO append)
        {
            FScene scene = appendTo.GetScene();
            if (scene.IsSelected(appendTo))
                scene.Deselect(appendTo);
            if (scene.IsSelected(append))
                scene.Deselect(append);

            Frame3f f1 = appendTo.GetLocalFrame(CoordSpace.ObjectCoords);
            Vector3f scale1 = appendTo.GetLocalScale();
            Frame3f f2 = append.GetLocalFrame(CoordSpace.ObjectCoords);
            Vector3f scale2 = append.GetLocalScale();

            bool mesh1HasVtxNormals = appendTo.Mesh.HasVertexNormals;

            DMesh3 mesh2 = append.Mesh;
            foreach ( int vid in mesh2.VertexIndices() ) {

                // convert point in mesh2 to scene coords
                Vector3f v2 = (Vector3f)mesh2.GetVertex(vid);
                v2 *= scale2;
                Vector3f v2s = f2.FromFrameP(v2);

                // transfer that scene coord into local coords of mesh1
                Vector3f v2in1 = f1.ToFrameP(v2s);
                v2in1 /= scale1;
                mesh2.SetVertex(vid, v2in1);

                if (mesh1HasVtxNormals && mesh2.HasVertexNormals) {
                    Vector3f n = mesh2.GetVertexNormal(vid);
                    Vector3f ns = f2.FromFrameV(n);
                    Vector3f ns2 = f1.ToFrameV(ns);
                    mesh2.SetVertexNormal(vid, ns2);
                }
            }

            appendTo.EditAndUpdateMesh( (mesh1) => {
                MeshEditor editor = new MeshEditor(mesh1);
                editor.AppendMesh(mesh2);
            }, GeometryEditTypes.ArbitraryEdit);

            // [TODO] change record!

            scene.RemoveSceneObject(append, false);
        }




        // not sure where these should go...
        public static void SetVisible(SceneObject so, bool bVisible)
        {
            if ( so.RootGameObject.IsVisible() != bVisible ) {
                if (bVisible)
                    Show(so);
                else
                    Hide(so);
            }
        }
        public static void Show(SceneObject so)
        {
            so.RootGameObject.Show();
            if ( so is SOCollection ) {
                foreach (SceneObject childso in (so as SOCollection).GetChildren())
                    Show(childso);
            }
        }
        public static void Hide(SceneObject so)
        {
            so.RootGameObject.Hide();
            if ( so is SOCollection ) {
                foreach (SceneObject childso in (so as SOCollection).GetChildren())
                    Hide(childso);
            }
        }
        public static bool IsVisible(SceneObject so)
        {
            return so.RootGameObject.IsVisible();
        }
        public static List<T> FindObjectsOfType<T>(IEnumerable<SceneObject> objects, bool bVisibleOnly = false) where T : class 
        {
            List<T> result = new List<T>();
            foreach ( var obj in objects ) {
                if (obj is T) {
                    if (bVisibleOnly == false || IsVisible(obj))
                        result.Add(obj as T);
                }
            }
            return result;
        }


        public static void DestroySO(SceneObject so)
        {
            so.RootGameObject.SetParent(null);
            so.SetScene(null);
            so.RootGameObject.Destroy();
        }



        public static bool IsSelectionMatch(FScene scene, Type type, int count)
        {
            var c = scene.Selected;
            if (c.Count != count)
                return false;
            foreach ( var o in c ) {
                if ( ! type.IsAssignableFrom(o.GetType()) )
                    return false;
            }
            return true;
        }
        public static bool IsSelectionMatch(FScene scene, params Type[] typeList)
        {
            var c = scene.Selected;
            if (c.Count != typeList.Length)
                return false;
            int k = 0;
            foreach (var o in c) {
                if ( ! typeList[k++].IsAssignableFrom(o.GetType()) )
                    return false;
            }
            return true;
        }


    }



    public class RayHit 
	{
		public Vector3f hitPos;
        public Vector3f hitNormal;
        public int hitIndex;
		public float fHitDist;

		public RayHit() {
            fHitDist = float.PositiveInfinity;
            hitIndex = -1;
        }

		public bool IsValid {
			get { return fHitDist < float.PositiveInfinity; }
		}
	}

	public class GameObjectRayHit : RayHit
	{
		public fGameObject hitGO;
	}



	public class SORayHit : GameObjectRayHit 
	{
		public SceneObject hitSO;

		public SORayHit() {
		}
		public SORayHit(GameObjectRayHit init, SceneObject so) {
			hitPos = init.hitPos;
            hitNormal = init.hitNormal;
            hitIndex = init.hitIndex;
			fHitDist = init.fHitDist;
			hitGO = init.hitGO;
			hitSO = so;
		}
        public SORayHit(AnyRayHit init) {
            Util.gDevAssert(init.eType == HitType.SceneObjectHit);
            hitPos = init.hitPos;
            hitNormal = init.hitNormal;
            hitIndex = init.hitIndex;
            fHitDist = init.fHitDist;
            hitGO = init.hitGO;
            hitSO = init.hitSO;
        }
    }


	public class UIRayHit : GameObjectRayHit 
	{
		public SceneUIElement hitUI;

		public UIRayHit() {
		}
		public UIRayHit(GameObjectRayHit init, SceneUIElement ui) {
			hitPos = init.hitPos;
            hitNormal = init.hitNormal;
            hitIndex = init.hitIndex;
            fHitDist = init.fHitDist;
			hitGO = init.hitGO;
			hitUI = ui;
		}
        public UIRayHit(AnyRayHit init)
        {
            Util.gDevAssert(init.eType == HitType.SceneUIElementHit);
            hitPos = init.hitPos;
            hitNormal = init.hitNormal;
            hitIndex = init.hitIndex;
            fHitDist = init.fHitDist;
            hitGO = init.hitGO;
            hitUI = init.hitUI;
        }
    }



	public enum HitType {
		SceneObjectHit,
		SceneUIElementHit,
        BoundsObjectHit
	}

	public class AnyRayHit : GameObjectRayHit 
	{
		public HitType eType;

		public SceneObject hitSO;
		public SceneUIElement hitUI;

		public AnyRayHit() {
		}
		public AnyRayHit(GameObjectRayHit init, SceneObject so) {
			hitPos = init.hitPos;
            hitNormal = init.hitNormal;
            hitIndex = init.hitIndex;
            fHitDist = init.fHitDist;
			hitGO = init.hitGO;
			eType = HitType.SceneObjectHit;
			hitSO = so;
		}
		public AnyRayHit(SORayHit init) {
			hitPos = init.hitPos;
            hitNormal = init.hitNormal;
            hitIndex = init.hitIndex;
            fHitDist = init.fHitDist;
			hitGO = init.hitGO;
			eType = HitType.SceneObjectHit;
			hitSO = init.hitSO;
		}
		public AnyRayHit(GameObjectRayHit init, SceneUIElement ui) {
			hitPos = init.hitPos;
            hitNormal = init.hitNormal;
            hitIndex = init.hitIndex;
            fHitDist = init.fHitDist;
			hitGO = init.hitGO;
			eType = HitType.SceneUIElementHit;
			hitUI = ui;
		}
		public AnyRayHit(UIRayHit init) {
			hitPos = init.hitPos;
            hitNormal = init.hitNormal;
            hitIndex = init.hitIndex;
            fHitDist = init.fHitDist;
			hitGO = init.hitGO;
			eType = HitType.SceneUIElementHit;
			hitUI = init.hitUI;
		}
        public AnyRayHit(GameObjectRayHit init, HitType eType)
        {
            Util.gDevAssert(eType == HitType.BoundsObjectHit);
            hitPos = init.hitPos;
            hitNormal = init.hitNormal;
            hitIndex = init.hitIndex;
            fHitDist = init.fHitDist;
            hitGO = init.hitGO;
            this.eType = eType;
        }
        public UIRayHit toUIHit() {
            return new UIRayHit(this);
        }
        public SORayHit toSOHit() {
            return new SORayHit(this);
        }
    }


}

