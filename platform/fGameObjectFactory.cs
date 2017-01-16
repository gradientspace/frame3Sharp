using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
   public static class GameObjectFactory
    {
        public static fGameObject CreateParentGO(string sName)
        {
            GameObject go = new GameObject(sName);
            return new fGameObject(go);
        }


        static void initialize_meshgo(GameObject go, Mesh mesh, bool bCollider)
        {
            go.AddComponent<MeshFilter>();
            if ( mesh != null )
                go.SetMesh(mesh);
            go.AddComponent<MeshRenderer>();
            if (bCollider) {
                var collider = go.AddComponent<MeshCollider>();
                collider.enabled = false;
            }
        }


        public static fGameObject CreateMeshGO(string sName, Mesh mesh = null, bool bCollider = false)
        {
            GameObject go = new GameObject(sName);
            initialize_meshgo(go, mesh, bCollider);
            return new fGameObject(go);
        }

        // unit rectangle lying in plane
        public static fRectangleGameObject CreateRectangleGO(string sName, float fWidth, float fHeight, Colorf color, bool bCollider)
        {
            GameObject go = new GameObject(sName);
            Mesh rectMesh = UnityUtil.GetPrimitiveMesh(PrimitiveType.Quad);
            UnityUtil.RotateMesh(rectMesh, Quaternionf.AxisAngleD(Vector3f.AxisX, 90), Vector3f.Zero);
            initialize_meshgo(go, rectMesh, bCollider);
            go.SetMaterial(MaterialUtil.CreateFlatMaterialF(color));
            return new fRectangleGameObject(go, fWidth, fHeight);
        }

        // equilateral triangle lying in plane centered at (0,0) 
        // with height = 1 (ie vertical extent is [-0.5,0.5], so base width is 2/sqrt(3)
        public static fTriangleGameObject CreateTriangleGO(string sName, float fWidth, float fHeight, Colorf color, bool bCollider)
        {
            GameObject go = new GameObject(sName);
            Mesh triMesh = new Mesh();
            float h = 1;
            float w = (float)(2 / Math.Sqrt(3));
            //float h = (float)(Math.Sqrt(3) / 2);      // width=1 instead (expose this as parameter?)
            //float w = 1;
            triMesh.vertices = new Vector3[3] {
                new Vector3(-w/2, 0.0f, -h/2), new Vector3(w/2, 0.0f, -h/2), new Vector3(0, 0, h/2) };
            triMesh.triangles = new int[3] { 0, 2, 1 };
            initialize_meshgo(go, triMesh, bCollider);
            go.SetMaterial(MaterialUtil.CreateFlatMaterialF(color));
            return new fTriangleGameObject(go, fWidth, fHeight);
        }


        // disc with radius=1, lying in plane, centered at (0,0)
        public static fDiscGameObject CreateDiscGO(string sName, float fRadius, Colorf color, bool bCollider)
        {
            GameObject go = new GameObject(sName);
            Mesh discMesh = PrimitiveCache.GetPrimitiveMesh(fPrimitiveType.Disc);
            initialize_meshgo(go, discMesh, bCollider);
            go.SetMaterial(MaterialUtil.CreateFlatMaterialF(color));
            return new fDiscGameObject(go, fRadius);
        }


        public static fLineGameObject CreateLineGO(string sName, Colorf color, float fLineWidth)
        {
            GameObject go = new GameObject(sName);
            LineRenderer r = go.AddComponent<LineRenderer>();
            r.useWorldSpace = false;
            r.material = MaterialUtil.CreateTransparentMaterial(Color.black, 0.75f);
            fLineGameObject lgo = new fLineGameObject(go);
            lgo.SetColor(color);
            lgo.SetLineWidth(fLineWidth);
            return lgo;
        }



        public static fCircleGameObject CreateCircleGO(string sName, float fRadius, Colorf color, float fLineWidth)
        {
            GameObject go = new GameObject(sName);
            LineRenderer r = go.AddComponent<LineRenderer>();
            r.useWorldSpace = false;
            r.material = MaterialUtil.CreateTransparentMaterial(Color.black, 0.75f);
            fCircleGameObject fgo = new fCircleGameObject(go);
            fgo.SetColor(color);
            fgo.SetLineWidth(fLineWidth);
            fgo.SetSteps(32);
            fgo.SetRadius(fRadius);
            return fgo;
        }



        public static fTextGameObject CreateTextMeshGO(string sName, string sText, Colorf textColor, float fTextHeight)
        {
            GameObject textGO = new GameObject(sName);
            TextMesh tm = textGO.AddComponent<TextMesh>();
            tm.text = sText;
            tm.color = textColor;
            tm.fontSize = 50;
            tm.offsetZ = -0.25f;
            tm.alignment = TextAlignment.Left;
            // ignore material changes when we add to GameObjectSet
            textGO.AddComponent<IgnoreMaterialChanges>();
            // use our textmesh material instead
            MaterialUtil.SetTextMeshDefaultMaterial(tm);

            Vector2 size = UnityUtil.EstimateTextMeshDimensions(tm);
            float fScaleH = fTextHeight / size[1];
            tm.transform.localScale = new Vector3(fScaleH, fScaleH, fScaleH);
            //tm.transform.Translate(-Width / 2.0f, fTextHeight / 2.0f, 0.0f);

            textGO.GetComponent<Renderer>().material.renderQueue = SceneGraphConfig.TextRendererQueue;

            return new fTextGameObject(textGO);
        }


        public static void DestroyGO(fGameObject go) {
            UnityEngine.GameObject.Destroy(go);
        }

    }

}
