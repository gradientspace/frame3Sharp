using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{

    public class fTextGameObject : fGameObject
    {
        Vector2f size;
        fText textObj;

        public fTextGameObject(GameObject go, fText textObj, Vector2f size)
            : base(go, FGOFlags.EnablePreRender)
        {
            this.size = size;
            this.textObj = textObj;
        }

        public fText TextObject
        {
            get { return textObj; }
        }

        public Vector2f GetSize()
        {
            return size;
        }

        public void SetHeight(float fNewHeight)
        {
            // doesn't support textmeshpro text...

            TextMesh tm = go.GetComponent<TextMesh>();
            tm.transform.localScale = Vector3f.One;
            Vector2f size = UnityUtil.EstimateTextMeshDimensions(tm);
            float fScaleH = fNewHeight / size.y;
            tm.transform.localScale = new Vector3(fScaleH, fScaleH, fScaleH);
            size = new Vector2f(fScaleH * size.x, fNewHeight);
        }

        public void SetText(string sText)
        {
            if (textObj.GetText() != sText)
                textObj.SetText(sText);
        }

        public override void SetColor(Colorf color)
        {
            textObj.SetColor(color);
        }
    }







    public class fTextAreaGameObject : fGameObject
    {
        Vector2f size;
        fText textObj;

        public fTextAreaGameObject(GameObject go, fText textObj, Vector2f size)
            : base(go, FGOFlags.EnablePreRender)
        {
            this.size = size;
            this.textObj = textObj;
        }

        public fText TextObject
        {
            get { return textObj; }
        }

        public Vector2f GetSize()
        {
            return size;
        }

        //public void SetHeight(float fNewHeight)
        //{
        //    // doesn't support textmeshpro text...

        //    TextMesh tm = go.GetComponent<TextMesh>();
        //    tm.transform.localScale = Vector3f.One;
        //    Vector2f size = UnityUtil.EstimateTextMeshDimensions(tm);
        //    float fScaleH = fNewHeight / size.y;
        //    tm.transform.localScale = new Vector3(fScaleH, fScaleH, fScaleH);
        //    size = new Vector2f(fScaleH * size.x, fNewHeight);
        //}

        public string GetText()
        {
            return textObj.GetText();
        }
        public void SetText(string sText)
        {
            if (textObj.GetText() != sText)
                textObj.SetText(sText);
        }

        public override void SetColor(Colorf color)
        {
            textObj.SetColor(color);
        }
    }



}

