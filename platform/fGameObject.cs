using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{

    //
    // fGameObject wraps a GameObject for frame3Sharp. The idea is that eventually we
    //  will be able to "replace" GameObject with something else, ie non-Unity stuff.
    //
    // implicit cast operators allow transparent conversion between GameObject and fGameObject
    //
    public class fGameObject
    {
        GameObject go;


        public fGameObject(GameObject go)
        {
            this.go = go;
        }


        public void SetName(string name)
        {
            go.name = name;
        }
        public string GetName()
        {
            return go.name;
        }

        public void SetLayer(int layer)
        {
            go.layer = layer;
        }
        public int GetLayer()
        {
            return go.layer;
        }

        public bool HasChildren()
        {
            return go.transform.childCount > 0;
        }
        public System.Collections.IEnumerable Children()
        {
            for (int k = 0; k < go.transform.childCount; ++k)
                yield return go.transform.GetChild(k).gameObject;
        }


        public Mesh GetMesh()
        {
            return go.GetComponent<MeshFilter>().mesh;
        }
        public Mesh GetSharedMesh()
        {
            return go.GetComponent<MeshFilter>().sharedMesh;
        }
        public void SetMesh(Mesh m)
        {
            go.GetComponent<MeshFilter>().mesh = m;
        }
        public void SetSharedMesh(Mesh m)
        {
            go.GetComponent<MeshFilter>().sharedMesh = m;
        }

        public void SetMaterial(fMaterial mat)
        {
            go.GetComponent<Renderer>().material = mat;
        }
        public fMaterial GetMaterial()
        {
            return new fMaterial(go.GetComponent<Renderer>().material);
        }


        public void SetParent(fGameObject parentGO, bool bKeepWorldPosition = false)
        {
            if (parentGO == null)
                go.transform.parent = null;
            else
                go.transform.SetParent(((GameObject)parentGO).transform, bKeepWorldPosition);
        }



        public void SetPosition(Vector3f vPosition)
        {
            go.transform.position = vPosition;
        }
        public Vector3f GetPosition()
        {
            return go.transform.position;
        }

        public void SetLocalPosition(Vector3f vPosition)
        {
            go.transform.localPosition = vPosition;
        }
        public Vector3f GetLocalPosition()
        {
            return go.transform.localPosition;
        }

        public void SetLocalScale(Vector3f vScale)
        {
            go.transform.localScale = vScale;
        }
        public void SetLocalScale(float fScale)
        {
            go.transform.localScale = fScale * Vector3f.One; 
        }
        public Vector3f GetLocalScale()
        {
            return go.transform.localScale;
        }




        public static implicit operator UnityEngine.GameObject(fGameObject go)
        {
            return go.go;
        }
        public static implicit operator fGameObject(UnityEngine.GameObject go)
        {
            return new fGameObject(go);
        }
    }
}
