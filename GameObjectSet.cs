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


    //
    // [RMS] this is a bit hacky utility class that allows us to 
    //   create various standard GO types, ray-hit test them, etc.
    //   Used by SceneObject, SceneUIElement, and so on
    //
	public class GameObjectSet
	{
		public List<GameObject> vObjects;

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

        public virtual void AppendExistingGO(GameObject go)
        {
            vObjects.Add(go);
        }
        public virtual GameObject AppendExistingChildGO(GameObject parent, string childName)
        {
            GameObject child = parent.transform.FindChild(childName).gameObject;
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


		public virtual GameObject AppendMeshGO(string name, Mesh mesh, Material setMaterial, GameObject parent, bool bCollider = true) {
            // [TODO] replace with UnityUtil.CreateMeshGO ??
            var gameObj = new GameObject (name);
            gameObj.AddComponent<MeshFilter>();
            gameObj.SetMesh(mesh);
            if (bCollider) {
                gameObj.AddComponent(typeof(MeshCollider));
                gameObj.GetComponent<MeshCollider>().enabled = false;
            }
			(gameObj.AddComponent (typeof(MeshRenderer)) as MeshRenderer).material = setMaterial;

			vObjects.Add (gameObj);

            gameObj.transform.parent = parent.transform;
            gameObj.SetLayer(parent.GetLayer());

			return gameObj;
		}


		public virtual GameObject AppendUnityPrimitiveGO(string name, PrimitiveType eType, Material setMaterial, GameObject parent, bool bCollider = true) {
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

			return gameObj;
		}


        public virtual void RemoveGO(GameObject go)
        {
            vObjects.Remove(go);
            go.transform.SetParent(null);
        }


        public virtual void SetAllGOMaterials(Material m) {
            foreach (var go in vObjects) {
                if (go.GetComponent<IgnoreMaterialChanges>() == null)
                    go.GetComponent<MeshRenderer>().material = m;
            }
		}

        // [RMS] assumes all shaders have parameter _AlphaScale available (!)
        public virtual void SetAllGOAlphaMultiply(float fT)
        {
            foreach (var go in vObjects) 
                go.GetComponent<Renderer>().material.SetFloat("_AlphaScale", fT);
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

