using System;
using System.Collections.Generic;
using UnityEngine;

namespace f3
{

    // [RMS] use gameObj.AddComponent<IgnoreMaterialChanges>() on 
    //   GameObject instances to prevent material from being modified by
    //   other calls to GameObjectSet
    public class IgnoreMaterialChanges : MonoBehaviour
    {

    }


    /// <summary>
    ///  GameObjectSet is a utility class that provides a more useful interface
    ///  to a set of GameObject children. Most implementations of core F3 classes
    ///  like SceneObject, SceneUIElement, etc, are derived from GameObjectSet.
    /// </summary>
	public class GameObjectSet
	{
		protected List<GameObject> vObjects;

		public GameObjectSet ()
		{
			vObjects = new List<GameObject> ();
		}

		public List<GameObject> GameObjects { 
			get { return vObjects; } 
		}

        public void ClearGameObjects(bool bDestroy = true, float fDelay = 0.0f)
        {
            if ( bDestroy ) {
                foreach (GameObject go in vObjects)
                    UnityUtil.DestroyAllGameObjects(go, fDelay);
            }
            vObjects = new List<GameObject>();
        }

		public bool HasGO(GameObject go) {
			GameObject found = vObjects.Find (x => x == go);
			return found != null;
		}

        /// <summary>
        /// Finds the first child GO named sName. Optionally searches nested children.
        /// </summary>
        public GameObject FindGOByName(string sName, bool bRecurse = true)
        {
            for (int i = 0; i < vObjects.Count; ++i) {
                if (vObjects[i].GetName() == sName)
                    return vObjects[i];
                if ( bRecurse && vObjects[i].HasChildren() ) {
                    GameObject found = vObjects[i].FindChildByName(sName, bRecurse);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }


        public virtual void AppendExistingGO(GameObject go)
        {
            vObjects.Add(go);
        }
        public virtual GameObject AppendExistingChildGO(GameObject parent, string childName)
        {
            GameObject child = parent.transform.Find(childName).gameObject;
            vObjects.Add(child);
            child.SetLayer(parent.GetLayer());
            return child;
        }

        public virtual void AppendNewGO(fGameObject go, fGameObject parent, bool bKeepPosition)
        {
            vObjects.Add(go);
            go.SetParent(parent, bKeepPosition);
            go.SetLayer(parent.GetLayer());
            foreach (GameObject child_go in go.Children()) {
                vObjects.Add(child_go);
                child_go.SetLayer(parent.GetLayer());
            }
        }


		public virtual fMeshGameObject AppendMeshGO(string name, fMesh mesh, fMaterial setMaterial, fGameObject parent, bool bCollider = true) {
            fMeshGameObject go = new fMeshGameObject(mesh, true, bCollider);
            go.EnableCollisions = false;
            go.SetMaterial(setMaterial);
            go.SetName(name);

			vObjects.Add (go);

            if (parent != null) {
                parent.AddChild(go);
                go.SetLayer(parent.GetLayer());
            }

            return go;
		}


		public virtual fGameObject AppendUnityPrimitiveGO(string name, PrimitiveType eType, Material setMaterial, GameObject parent, bool bCollider = true) {
			var gameObj = GameObject.CreatePrimitive (eType);
            if (bCollider) {
                gameObj.AddComponent(typeof(MeshCollider));
                gameObj.GetComponent<MeshCollider>().enabled = false;
            }
			gameObj.GetComponent<MeshRenderer> ().material = setMaterial;
            gameObj.SetName(name);

            vObjects.Add (gameObj);

            if (parent != null) {
                gameObj.transform.parent = parent.transform;
                gameObj.SetLayer(parent.GetLayer());
            }

			return new fGameObject(gameObj, FGOFlags.NoFlags);
		}


        public virtual void RemoveGO(GameObject go)
        {
            vObjects.Remove(go);
            go.transform.SetParent(null);
        }
        public virtual void RemoveGO(fGameObject go)
        {
            vObjects.Remove(go);
            go.SetParent(null, true);
        }


        public virtual void SetAllGOMaterials(Material m) {
            foreach (var go in vObjects) {
                if (go.GetComponent<IgnoreMaterialChanges>() == null)
                    go.GetComponent<MeshRenderer>().material = m;
            }
		}


        // use for fading, etc
        public virtual void SetAllGOAlphaMultiply(float fT)
        {
            foreach (var go in vObjects)
                go.SetAlphaMultiply(fT);
        }


        public virtual void SetAllGOLayer(int nLayer)
        {
            foreach (var go in vObjects)
                go.SetLayer(nLayer);
        }


		public virtual bool FindGORayIntersection (Ray ray, out GameObjectRayHit hit)
		{
			hit = new GameObjectRayHit();
			RaycastHit hitInfo;

            // [RMS] this keeps popping up...why??
            //if (Mathf.Abs(ray.direction.sqrMagnitude - 1.0f) > 0.001) {
            //    DebugUtil.Log(2, "FindGORayIntersection: ray direction is not normalized! {0} {1}", ray.direction, ray.direction.magnitude);
            //    return false;
            //}

			foreach (var go in vObjects) {
                Collider collider = go.GetComponent<Collider>();
                if (collider) {
                    collider.enabled = true;
                    if (collider.Raycast(ray, out hitInfo, Mathf.Infinity)) {
                        if (hitInfo.distance < hit.fHitDist) {
                            hit.fHitDist = hitInfo.distance;
                            hit.hitPos = hitInfo.point;
                            hit.hitNormal = hitInfo.normal;
                            hit.hitGO = go;
                        }
                    }
                    collider.enabled = false;
                }
			}

			return (hit.hitGO != null);
		}


		public virtual bool IsGOHit(Ray ray, GameObject go) {
			bool bHit = false;
			RaycastHit hitInfo;
            MeshCollider collider = go.GetComponent<MeshCollider>();
            if (collider) {
                collider.enabled = true;
                if (collider.Raycast(ray, out hitInfo, Mathf.Infinity))
                    bHit = true;
                collider.enabled = false;
            }
			return bHit;
		}

        public virtual GameObject FindHitGO(Ray ray)
        {
            foreach ( GameObject go in vObjects ) {
                if (IsGOHit(ray, go))
                    return go;
            }
            return null;
        }



	}
}

