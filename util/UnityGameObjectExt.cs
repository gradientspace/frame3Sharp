using System;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{
    public class CustomAlphaMultiply : MonoBehaviour
    {
        public virtual void SetAlphaMultiply(float fT) { }
    }


    public enum RenderStage
    {
        OpaqueObjects,
        TransparentObjects
    }



    public static class UnityExtensions
    {
        public static void SetName(this GameObject go, string name) {
            go.name = name;
        }
        public static string GetName(this GameObject go) {
            return go.name;
        }

        public static void SetLayer(this GameObject go, int layer, bool bSetOnChildren = false) {
            go.layer = layer;
            if (bSetOnChildren) {
                for (int k = 0; k < go.transform.childCount; ++k)
                    go.transform.GetChild(k).gameObject.SetLayer(layer, true);
            }
        }
        public static int GetLayer(this GameObject go) {
            return go.layer;
        }

        public static bool HasChildren(this GameObject go) {
            return go.transform.childCount > 0;
        }
        public static IEnumerable<GameObject> Children(this GameObject go) {
            for (int k = 0; k < go.transform.childCount; ++k)
                yield return go.transform.GetChild(k).gameObject;
        }

        public static void AddChild(this GameObject go, GameObject child, bool bKeepWorldPosition)
        {
            child.transform.SetParent(go.transform, bKeepWorldPosition);
        }
        public static void RemoveChild(this GameObject go, GameObject child, bool bKeepWorldPosition = true)
        {
            child.transform.SetParent(null, bKeepWorldPosition);
        }
        public static void RemoveFromParent(this GameObject go, bool bKeepWorldPosition = true)
        {
            go.transform.SetParent(null, bKeepWorldPosition);
        }

        public static GameObject FindChildByName(this GameObject go, string sName, bool bRecurse)
        {
            foreach (GameObject childGO in go.Children()) {
                if (childGO.name == sName)
                    return childGO;
                if ( bRecurse && childGO.HasChildren() ) {
                    GameObject found = childGO.FindChildByName(sName, bRecurse);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        public static bool IsSameOrChild(this GameObject go, GameObject testGO)
        {
            if (go == testGO)
                return true;
            foreach ( GameObject childGO in go.Children() ) { 
                if (childGO.IsSameOrChild(testGO))
                    return true;
            }
            return false;
        }


        public static Mesh GetMesh(this GameObject go)
        {
            var filter = go.GetComponent<MeshFilter>();
            return (filter != null) ? filter.mesh : null;
        }
        public static Mesh GetSharedMesh(this GameObject go)
        {
            var filter = go.GetComponent<MeshFilter>();
            return (filter != null) ? filter.sharedMesh: null;
        }
        public static void SetMesh(this GameObject go, Mesh m, bool bUpdateCollider = false) {
            go.GetComponent<MeshFilter>().mesh = m;
            if ( bUpdateCollider ) {
                MeshCollider c = go.GetComponent<MeshCollider>();
                if (c != null)
                    c.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            }
        }
        public static void SetSharedMesh(this GameObject go, Mesh m, bool bUpdateCollider = false) {
            go.GetComponent<MeshFilter>().sharedMesh = m;
            if ( bUpdateCollider ) {
                MeshCollider c = go.GetComponent<MeshCollider>();
                if (c != null)
                    c.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            }
        }

        public static void SetMaterial(this GameObject go, fMaterial mat, bool bShared = false) {
            if ( bShared )
                go.GetComponent<Renderer>().sharedMaterial = mat;
            else
                go.GetComponent<Renderer>().material = mat;
        }
        public static fMaterial GetMaterial(this GameObject go) {
            Renderer r = go.GetComponent<Renderer>();
            return (r != null) ? new fMaterial(r.material) : null;
        }

        public static void SetColor(this GameObject go, Colorf color)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
                r.material.SetColor(color);
        }
        public static Colorf GetColor(this GameObject go)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
                return r.material.color;
            return Colorf.White;
        }

        // [RMS] assumes all shaders have parameter _AlphaScale available (!)
        public static void SetAlphaMultiply(this GameObject go, float fScale)
        {
            // [RMS] assume if we have custom handling we shouldn't do default handling?
            CustomAlphaMultiply c = go.GetComponent<CustomAlphaMultiply>();
            if (c != null) {
                c.SetAlphaMultiply(fScale);
            } else {
                Renderer r = go.GetComponent<Renderer>();
                if (r != null) {
                    r.material.SetFloat("_AlphaScale", fScale);
                }
            }
        }


        public static void Destroy(this GameObject go)
        {
            if (go != null) {
                GameObject.Destroy(go);
            }
        }


        public static void Hide(this GameObject go)
        {
            if (go.activeSelf == true)
                go.SetActive(false);
        }
        public static void Show(this GameObject go)
        {
            if (go.activeSelf == false)
                go.SetActive(true);
        }
        public static void SetVisible(this GameObject go, bool bVisible)
        {
            if (bVisible) Show(go); else Hide(go);
        }
        public static bool IsVisible(this GameObject go)
        {
            return (go.activeSelf == true);
        }


        public static void EnableCollider(this GameObject go, bool bEnable = true)
        {
            MeshCollider c = go.GetComponent<MeshCollider>();
            if (c != null) {
                if (go.activeSelf == false) {
                    go.SetActive(true);
                    c.enabled = bEnable;
                    go.SetActive(false);
                } else {
                    c.enabled = bEnable;
                }
            }
        }
        public static void DisableCollider(this GameObject go) {
            go.EnableCollider(false);
        }


        public static Vector3f GetLocalScale(this GameObject go)
        {
            return go.transform.localScale;
        }
        public static void SetLocalScale(this GameObject go, Vector3f scale)
        {
            go.transform.localScale = scale;
        }

        public static Vector3f GetLocalPosition(this GameObject go)
        {
            return go.transform.localPosition;
        }
        public static void SetLocalPosition(this GameObject go, Vector3f position)
        {
            go.transform.localPosition = position;
        }




        /*
         * Camera extensions
         */


        public static void SetName(this Camera cam, string name) {
            cam.name = name;
        }
        public static string GetName(this Camera cam) {
            return cam.name;
        }



        /*
         * Material extensions
         */


        public static void SetName(this Material mat, string name) {
            mat.name = name;
        }
        public static string GetName(this Material mat) {
            return mat.name;
        }

        public static void SetColor(this Material mat, Colorf color)
        {
            mat.color = color;
        }
        public static Colorf GetColor(this Material mat)
        {
            return new Colorf(mat.color);
        }

        public static void SetRenderStage(this Material mat, RenderStage eStage)
        {
            if (eStage == RenderStage.OpaqueObjects)
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            else
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }


    }

}
