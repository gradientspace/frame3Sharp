//force TMPRo support
//#define G3_ENABLE_TEXT_MESH_PRO

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;


#if G3_ENABLE_TEXT_MESH_PRO
using TMPro;
#endif


namespace f3
{
    public enum TextType
    {
        UnityTextMesh,
        TextMeshPro
    }

    public class fText
    {
        object text_component;
        TextType eType;

        public fText(object component, TextType type)
        {
            text_component = component;
            eType = type;
        }

        public void SetText(string text)
        {
            switch (eType) {
                case TextType.UnityTextMesh:
                    (text_component as TextMesh).text = text;
                    break;
                case TextType.TextMeshPro:
#if G3_ENABLE_TEXT_MESH_PRO
                    (text_component as TextMeshPro).text = text;
#endif
                    break;
            }
        }


        public void SetColor(Colorf color)
        {
            switch (eType) {
                case TextType.UnityTextMesh:
                    (text_component as TextMesh).color = color;
                    break;
                case TextType.TextMeshPro:
#if G3_ENABLE_TEXT_MESH_PRO
                    (text_component as TextMeshPro).color = color;
#endif
                    break;
            }
        }



        public Vector2f GetCursorPosition(int iPos)
        {
            // not supporting unity TextMesh
            // this might do it: http://answers.unity3d.com/questions/31622/is-it-possible-to-find-the-width-of-a-text-mesh.html


#if G3_ENABLE_TEXT_MESH_PRO
            TextMeshPro textmesh = (text_component as TextMeshPro);

            if (iPos == 0)
                return new Vector2f(0, 0);

            float fWidth = textmesh.textInfo.characterInfo[iPos-1].xAdvance;
            fWidth *= textmesh.transform.localScale[0];
            return new Vector2f(fWidth, 0);
#else
            throw new NotImplementedException("Sorry no TMPro!");
#endif
        }

    }






    public static class TextMeshProUtil
    {
#if G3_ENABLE_TEXT_MESH_PRO
        public static bool HaveTextMeshPro { get { return true; } }

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

#else
        public static bool HaveTextMeshPro { get { return false; } }

        public static fTextGameObject CreateTextMeshProGO(
            string sName, string sText,
            Colorf textColor, float fTextHeight,
            BoxPosition textOrigin = BoxPosition.Center,
            float fOffsetZ = -0.01f)
        {
            throw new NotImplementedException("you need to #define G3_ENABLE_TEXT_MESH_PRO to use TextMeshPro (!)");
        }

#endif



    }


}
