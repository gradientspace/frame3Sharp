using System;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{
    public class SceneUtil
    {
        // used to indicate that this object is transient
        static public string InvalidUUID = "77777777-7777-7777-7777-777777777777";



        public static bool FindNearestRayIntersection(IEnumerable<SceneObject> vSceneObjects, Ray ray, out SORayHit hit) {
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
        public static bool FindNearestRayIntersection(IEnumerable<SceneObject> vSceneObjects, Func<SceneObject,bool> filter, Ray ray, out SORayHit hit)
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


        // stupid but convenient
        public static bool FindNearestRayIntersection(IEnumerable<TransformableSO> vSceneObjects, Ray ray, out SORayHit hit) {
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




        // descends parent/child SO hierarchy and finds the set of topmost non-temporary SOs
        public static void FindAllPersistentTransformableChildren(SceneObject vParent, List<TransformableSO> children)
        {
            if ( (vParent is IParentSO) == false )
                return;
            foreach ( SceneObject so in (vParent as IParentSO).GetChildren() ) {
                if ((so is TransformableSO) == false)
                    continue;
                if (so.IsTemporary)
                    FindAllPersistentTransformableChildren(so, children);
                else
                    children.Add(so as TransformableSO);
            }
        }


        public static Frame3f GetSOLocalFrame(SceneObject so, CoordSpace eSpace)
        {
            if (eSpace == CoordSpace.SceneCoords) {
                Frame3f sceneW = UnityUtil.GetGameObjectFrame(so.GetScene().RootGameObject, CoordSpace.WorldCoords);
                Frame3f objW = UnityUtil.GetGameObjectFrame(so.RootGameObject, CoordSpace.WorldCoords);
                Frame3f result = sceneW.ToFrame(objW);
                // world coords have scene scale applied, we don't want that in scene coords
                if (so.GetScene().GetSceneScale() != 1.0f)
                    result = result.Scaled(1.0f / so.GetScene().GetSceneScale());
                return result;
            } else
                return UnityUtil.GetGameObjectFrame(so.RootGameObject, eSpace);
        }

        public static void SetSOLocalFrame(SceneObject so, CoordSpace eSpace, Frame3f newFrame)
        {
            if (eSpace == CoordSpace.SceneCoords) {
                // scene frames should not be scaled by scene scale, but we want to set as world
                // coords, so we need to apply it now
                if (so.GetScene().GetSceneScale() != 1.0f)
                    newFrame = newFrame.Scaled(so.GetScene().GetSceneScale());
                Frame3f sceneW = UnityUtil.GetGameObjectFrame(so.GetScene().RootGameObject, CoordSpace.WorldCoords);
                Frame3f objW = sceneW.FromFrame(newFrame);
                UnityUtil.SetGameObjectFrame(so.RootGameObject, objW, CoordSpace.WorldCoords);
            } else
                UnityUtil.SetGameObjectFrame(so.RootGameObject, newFrame, eSpace);
        }



        public static void TranslateInFrame(TransformableSO so, Vector3f translate, CoordSpace eSpace = CoordSpace.ObjectCoords)
        {
            Frame3f f = so.GetLocalFrame(eSpace);
            f.Origin += translate;
            so.SetLocalFrame(f, eSpace);
        }


        public static Bounds GetLocalBoundingBox(IEnumerable<SceneObject> vObjects) {
            int i = 0;
            AxisAlignedBox3f b = AxisAlignedBox3f.Infinite;
            foreach ( SceneObject so in vObjects ) {
                if (i == 0)
                    b = so.GetLocalBoundingBox();
                else
                    b.Contain(so.GetLocalBoundingBox());
            }
            return b;
        }




        public static void CombineSO(DMeshSO so1, DMeshSO so2)
        {
            FScene scene = so1.GetScene();
            if (scene.IsSelected(so1))
                scene.Deselect(so1);
            if (scene.IsSelected(so2))
                scene.Deselect(so2);

            Frame3f f1 = so1.GetLocalFrame(CoordSpace.ObjectCoords);
            Vector3f scale1 = so1.GetLocalScale();
            Frame3f f2 = so2.GetLocalFrame(CoordSpace.ObjectCoords);
            Vector3f scale2 = so2.GetLocalScale();

            DMesh3 mesh1 = so1.Mesh;

            DMesh3 mesh2 = so2.Mesh;
            foreach ( int vid in mesh2.VertexIndices() ) {

                // convert point in mesh2 to scene coords
                Vector3f v2 = (Vector3f)mesh2.GetVertex(vid);
                v2 *= scale2;
                Vector3f v2s = f2.FromFrameP(v2);

                // transfer that scene coord into local coords of mesh1
                Vector3f v2in1 = f1.ToFrameP(v2s);
                v2in1 /= scale1;
                mesh2.SetVertex(vid, v2in1);

                if (mesh1.HasVertexNormals && mesh2.HasVertexNormals) {
                    Vector3f n = mesh2.GetVertexNormal(vid);
                    Vector3f ns = f2.FromFrameV(n);
                    Vector3f ns2 = f1.ToFrameV(ns);
                    mesh2.SetVertexNormal(vid, ns2);
                }
            }

            MeshEditor editor = new MeshEditor(mesh1);
            editor.AppendMesh(mesh2);

            so1.NotifyMeshEdited();

            // [TODO] change record!

            scene.RemoveSceneObject(so2, true);
        }




        public static void DestroySO(SceneObject so)
        {
            so.RootGameObject.transform.parent = null;
            so.SetScene(null);
            UnityEngine.Object.Destroy(so.RootGameObject);
        }

    }



    public class RayHit 
	{
		public Vector3 hitPos;
        public Vector3 hitNormal;
		public float fHitDist;

		public RayHit() {
			fHitDist = Mathf.Infinity;
		}

		public bool IsValid {
			get { return fHitDist < Mathf.Infinity; }
		}
	}

	public class GameObjectRayHit : RayHit
	{
		public GameObject hitGO;
	}



	public class SORayHit : GameObjectRayHit 
	{
		public SceneObject hitSO;

		public SORayHit() {
		}
		public SORayHit(GameObjectRayHit init, SceneObject so) {
			hitPos = init.hitPos;
            hitNormal = init.hitNormal;
			fHitDist = init.fHitDist;
			hitGO = init.hitGO;
			hitSO = so;
		}
        public SORayHit(AnyRayHit init) {
            Debug.Assert(init.eType == HitType.SceneObjectHit);
            hitPos = init.hitPos;
            hitNormal = init.hitNormal;
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
            fHitDist = init.fHitDist;
			hitGO = init.hitGO;
			hitUI = ui;
		}
        public UIRayHit(AnyRayHit init)
        {
            Debug.Assert(init.eType == HitType.SceneUIElementHit);
            hitPos = init.hitPos;
            hitNormal = init.hitNormal;
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
            fHitDist = init.fHitDist;
			hitGO = init.hitGO;
			eType = HitType.SceneObjectHit;
			hitSO = so;
		}
		public AnyRayHit(SORayHit init) {
			hitPos = init.hitPos;
            hitNormal = init.hitNormal;
            fHitDist = init.fHitDist;
			hitGO = init.hitGO;
			eType = HitType.SceneObjectHit;
			hitSO = init.hitSO;
		}
		public AnyRayHit(GameObjectRayHit init, SceneUIElement ui) {
			hitPos = init.hitPos;
            hitNormal = init.hitNormal;
            fHitDist = init.fHitDist;
			hitGO = init.hitGO;
			eType = HitType.SceneUIElementHit;
			hitUI = ui;
		}
		public AnyRayHit(UIRayHit init) {
			hitPos = init.hitPos;
            hitNormal = init.hitNormal;
            fHitDist = init.fHitDist;
			hitGO = init.hitGO;
			eType = HitType.SceneUIElementHit;
			hitUI = init.hitUI;
		}
        public AnyRayHit(GameObjectRayHit init, HitType eType)
        {
            Debug.Assert(eType == HitType.BoundsObjectHit);
            hitPos = init.hitPos;
            hitNormal = init.hitNormal;
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

