using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;
using TMPro;

namespace f3
{
   public static class GameObjectFactory
    {
        public static fGameObject CreateParentGO(string sName)
        {
            GameObject go = new GameObject(sName);
            return new fGameObject(go);
        }


        static void initialize_meshgo(GameObject go, fMesh mesh, bool bCollider)
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


        public static fMeshGameObject CreateMeshGO(string sName, Mesh mesh = null, bool bCollider = false)
        {
            GameObject go = new GameObject(sName);
            initialize_meshgo(go, new fMesh(mesh), bCollider);
            return new fMeshGameObject(go);
        }
        public static fMeshGameObject CreateMeshGO(string sName, fMesh mesh, bool bCollider = false)
        {
            GameObject go = new GameObject(sName);
            initialize_meshgo(go, mesh, bCollider);
            return new fMeshGameObject(go);
        }

        // unit rectangle lying in plane
        public static fRectangleGameObject CreateRectangleGO(string sName, float fWidth, float fHeight, Colorf color, bool bCollider)
        {
            fMaterial mat = MaterialUtil.CreateFlatMaterialF(color);
            return CreateRectangleGO(sName, fWidth, fHeight, mat, false, bCollider);
        }
        public static fRectangleGameObject CreateRectangleGO(string sName, float fWidth, float fHeight, fMaterial useMaterial, bool bShareMaterial, bool bCollider)
        {
            GameObject go = new GameObject(sName);
            Mesh rectMesh = UnityUtil.GetPrimitiveMesh(PrimitiveType.Quad);
            UnityUtil.RotateMesh(rectMesh, Quaternionf.AxisAngleD(Vector3f.AxisX, 90), Vector3f.Zero);
            initialize_meshgo(go, new fMesh(rectMesh), bCollider);
            go.SetMaterial(useMaterial, bShareMaterial);
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
            initialize_meshgo(go, new fMesh(triMesh), bCollider);
            go.SetMaterial(MaterialUtil.CreateFlatMaterialF(color));
            return new fTriangleGameObject(go, fWidth, fHeight);
        }


        // disc with radius=1, lying in plane, centered at (0,0)
        public static fDiscGameObject CreateDiscGO(string sName, float fRadius, Colorf color, bool bCollider)
        {
            GameObject go = new GameObject(sName);
            Mesh discMesh = PrimitiveCache.GetPrimitiveMesh(fPrimitiveType.Disc);
            initialize_meshgo(go, new fMesh(discMesh), bCollider);
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



        public static fTextGameObject CreateTextMeshGO(
            string sName, string sText, 
            Colorf textColor, float fTextHeight, 
            BoxPosition textOrigin = BoxPosition.Center, 
            float fOffsetZ = -0.1f)
        {
            GameObject textGO = new GameObject(sName);
            TextMesh tm = textGO.AddComponent<TextMesh>();
            tm.text = sText;
            tm.color = textColor;
            tm.fontSize = 50;
            tm.offsetZ = fOffsetZ;
            tm.alignment = TextAlignment.Left;
            // ignore material changes when we add to GameObjectSet
            textGO.AddComponent<IgnoreMaterialChanges>();
            // use our textmesh material instead
            MaterialUtil.SetTextMeshDefaultMaterial(tm);

            Vector2f size = UnityUtil.EstimateTextMeshDimensions(tm);
            float fScaleH = fTextHeight / size.y;
            tm.transform.localScale = new Vector3(fScaleH, fScaleH, fScaleH);
            float fTextWidth = fScaleH * size.x;

            // by default text origin is top-left
            if ( textOrigin == BoxPosition.Center )
                tm.transform.Translate(-fTextWidth / 2.0f, fTextHeight / 2.0f, 0);
            else if ( textOrigin == BoxPosition.BottomLeft )
                tm.transform.Translate(0, fTextHeight, 0);
            else if ( textOrigin == BoxPosition.TopRight )
                tm.transform.Translate(-fTextWidth, 0, 0);
            else if ( textOrigin == BoxPosition.BottomRight )
                tm.transform.Translate(-fTextWidth, fTextHeight, 0);
            else if ( textOrigin == BoxPosition.CenterLeft )
                tm.transform.Translate(0, fTextHeight/2.0f, 0);
            else if ( textOrigin == BoxPosition.CenterRight )
                tm.transform.Translate(-fTextWidth, fTextHeight/2.0f, 0);
            else if ( textOrigin == BoxPosition.CenterTop )
                tm.transform.Translate(-fTextWidth / 2.0f, 0, 0);
            else if ( textOrigin == BoxPosition.CenterBottom )
                tm.transform.Translate(-fTextWidth / 2.0f, fTextHeight, 0);

            textGO.GetComponent<Renderer>().material.renderQueue = SceneGraphConfig.TextRendererQueue;

            return new fTextGameObject(textGO, new fText(tm, TextType.UnityTextMesh),
                new Vector2f(fTextWidth, fTextHeight) );
        }


        // [TODO] currently only allows for left-justified text.
        // Can support center/right, but the translate block needs to be rewritten
        // (can we generalize as target-center of 2D bbox??
        public static fTextGameObject CreateTextMeshProGO(
            string sName, string sText, 
            Colorf textColor, float fTextHeight, 
            BoxPosition textOrigin = BoxPosition.Center, 
            float fOffsetZ = -0.01f)
        {
            GameObject textGO = new GameObject(sName);
            TextMeshPro tm = textGO.AddComponent<TextMeshPro>();
            //tm.isOrthographic = false;
            tm.alignment = TextAlignmentOptions.TopLeft;
            tm.enableWordWrapping = false;
            tm.autoSizeTextContainer = true;
            tm.fontSize = 16;
            tm.text = sText;
            tm.color = textColor;
            // ignore material changes when we add to GameObjectSet
            textGO.AddComponent<IgnoreMaterialChanges>();
            // use our textmesh material instead
            //MaterialUtil.SetTextMeshDefaultMaterial(tm);

            TextContainer container = textGO.GetComponent<TextContainer>();
            container.isAutoFitting = false;
            container.anchorPosition = TextContainerAnchors.TopLeft;

            tm.ForceMeshUpdate();

            // set container width and height to just contain text
            AxisAlignedBox3f bounds = tm.bounds;
            Vector2f size = new Vector2f(bounds.Width, bounds.Height);
            container.width = size.x + 1;
            container.height = size.y + 1;

            // Now we want to scale text to hit our target height, but if we scale by size.y
            // then the scaling will vary by text height (eg "m" will get same height as "My").
            // However: 1) size.y varies with tm.fontSize, but it's not clear how. 
            //          2) fontInfo.LineHeight tells us the height we want but doesn't change w/ tm.fontSize
            // I tried a few values and the relationship is linear. It is in the ballpark
            // of just being 10x...actually closer to 11x. No other values in fontInfo have a nice
            // round-number relationship. But this value is probably font-dependent!!
            float t = tm.fontSize / tm.font.fontInfo.LineHeight;
            float magic_k = 10.929f;        // [RMS] solve-for-x given a few different fontSize values
            float font_size_y = magic_k * t;
            float fScaleH = fTextHeight / font_size_y;

            tm.transform.localScale = new Vector3(fScaleH, fScaleH, fScaleH);
            float fTextWidth = fScaleH * size.x;

            // by default text origin is top-left
            if ( textOrigin == BoxPosition.Center )
                tm.transform.Translate(-fTextWidth / 2.0f, fTextHeight / 2.0f, fOffsetZ);
            else if ( textOrigin == BoxPosition.BottomLeft )
                tm.transform.Translate(0, fTextHeight, fOffsetZ);
            else if ( textOrigin == BoxPosition.TopRight )
                tm.transform.Translate(-fTextWidth, 0, fOffsetZ);
            else if ( textOrigin == BoxPosition.BottomRight )
                tm.transform.Translate(-fTextWidth, fTextHeight, fOffsetZ);
            else if ( textOrigin == BoxPosition.CenterLeft )
                tm.transform.Translate(0, fTextHeight/2.0f, fOffsetZ);
            else if ( textOrigin == BoxPosition.CenterRight )
                tm.transform.Translate(-fTextWidth, fTextHeight/2.0f, fOffsetZ);
            else if ( textOrigin == BoxPosition.CenterTop )
                tm.transform.Translate(-fTextWidth / 2.0f, 0, fOffsetZ);
            else if ( textOrigin == BoxPosition.CenterBottom )
                tm.transform.Translate(-fTextWidth / 2.0f, fTextHeight, fOffsetZ);

            textGO.GetComponent<Renderer>().material.renderQueue = SceneGraphConfig.TextRendererQueue;

            return new fTextGameObject(textGO, new fText(tm, TextType.TextMeshPro),
                new Vector2f(fTextWidth, fTextHeight) );
        }



        public static void DestroyGO(fGameObject go) {
            UnityEngine.GameObject.Destroy(go);
        }

    }

}
