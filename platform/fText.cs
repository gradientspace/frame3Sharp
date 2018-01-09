//force TMPRo support
//#define F3_ENABLE_TEXT_MESH_PRO

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;


// [RMS] argh. accidentally used G3 here long ago...
#if G3_ENABLE_TEXT_MESH_PRO || F3_ENABLE_TEXT_MESH_PRO
using TMPro;
#endif


namespace f3
{
    public enum TextType
    {
        UnityTextMesh,
        TextMeshPro
    }



    public enum TextOverflowMode
    {
        Ignore, Truncate, Ellipses, Clipped
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
#if G3_ENABLE_TEXT_MESH_PRO || F3_ENABLE_TEXT_MESH_PRO
                    (text_component as TextMeshPro).text = text;
#endif
                    break;
            }
        }

        public string GetText()
        {
            switch(eType) {
                default:
                case TextType.UnityTextMesh:
                    return (text_component as TextMesh).text;
                case TextType.TextMeshPro:
#if G3_ENABLE_TEXT_MESH_PRO || F3_ENABLE_TEXT_MESH_PRO
                    return (text_component as TextMeshPro).text;
#else
                    return null;
#endif
            }
        }


        public void SetColor(Colorf color)
        {
            switch (eType) {
                case TextType.UnityTextMesh:
                    (text_component as TextMesh).color = color;
                    break;
                case TextType.TextMeshPro:
#if G3_ENABLE_TEXT_MESH_PRO || F3_ENABLE_TEXT_MESH_PRO
                    (text_component as TextMeshPro).color = color;
#endif
                    break;
            }
        }


        public void SetHeight(float fNewHeight)
        {
            switch (eType) {
                case TextType.UnityTextMesh: {
                        TextMesh tm = (text_component as TextMesh);
                        tm.transform.localScale = Vector3f.One;
                        Vector2f size = UnityUtil.EstimateTextMeshDimensions(tm);
                        float fScaleH = fNewHeight / size.y;
                        tm.transform.localScale = new Vector3(fScaleH, fScaleH, fScaleH);
                        size = new Vector2f(fScaleH * size.x, fNewHeight);
                    }
                break;

                case TextType.TextMeshPro:
#if G3_ENABLE_TEXT_MESH_PRO || F3_ENABLE_TEXT_MESH_PRO
                    (text_component as TextMeshProExt).SetTextSizeFromHeight(fNewHeight);
#endif
                    break;
            }
        }



        public void SetFixedWidth(float fWidth)
        {
            switch (eType) {
                case TextType.UnityTextMesh:
                    DebugUtil.Log(2,"Unity fText.SetFixedWidth not implemented!");
                    break;

                case TextType.TextMeshPro:
#if G3_ENABLE_TEXT_MESH_PRO || F3_ENABLE_TEXT_MESH_PRO
                    (text_component as TextMeshProExt).SetFixedWidth(fWidth);
#endif
                    break;
            }
        }


        public void SetOverflowMode(TextOverflowMode eMode)
        {
            switch (eType) {
                case TextType.UnityTextMesh:
                    DebugUtil.Log(2,"Unity fText.SetOverflowMode not implemented!");
                    break;

                case TextType.TextMeshPro:
#if G3_ENABLE_TEXT_MESH_PRO || F3_ENABLE_TEXT_MESH_PRO
                    (text_component as TextMeshProExt).SetOverflowMode(eMode);
#endif
                    break;
            }
        }




        public Vector2f GetCursorPosition(int iPos)
        {
            // not supporting unity TextMesh
            // this might do it: http://answers.unity3d.com/questions/31622/is-it-possible-to-find-the-width-of-a-text-mesh.html


#if G3_ENABLE_TEXT_MESH_PRO || F3_ENABLE_TEXT_MESH_PRO
            TextMeshPro textmesh = (text_component as TextMeshPro);

            if (iPos == 0)
                return new Vector2f(0, 0);

            // [RMS] if we call this after changing text, but before Text component has done an Update(),
            //   then the results will be nonsense and we may be out-of-bounds on the characterInfo array.
            //   Not sure what to do about this, so just clamping for now...
            iPos = MathUtil.Clamp(iPos - 1, 0, textmesh.textInfo.characterInfo.Length-1);
            float fWidth = textmesh.textInfo.characterInfo[iPos].xAdvance;

            // Ugh because of same problem as above, correct xAdvance may not be available on iPos.
            // Seems like value will be 0 in these cases? If so we can force a mesh update and then
            // the right values seem to be available
            if ( fWidth == 0 && iPos > 0) {
                textmesh.ForceMeshUpdate();
                fWidth = textmesh.textInfo.characterInfo[iPos].xAdvance;
            }

            fWidth *= textmesh.transform.localScale[0];
            return new Vector2f(fWidth, 0);
#else
            throw new NotImplementedException("Sorry no TMPro!");
#endif
        }

    }





    public class TextMeshProAlphaMultiply : CustomAlphaMultiply
    {
        public float start_alpha = float.MaxValue;
        public override void SetAlphaMultiply(float fT)
        {
#if G3_ENABLE_TEXT_MESH_PRO || F3_ENABLE_TEXT_MESH_PRO
            TextMeshPro tm = this.gameObject.GetComponent<TextMeshPro>();
            if (start_alpha == float.MaxValue)
                start_alpha = tm.alpha;
            tm.alpha = fT; // * start_alpha;
#else
            throw new NotImplementedException();
#endif
        }
    }






#if G3_ENABLE_TEXT_MESH_PRO || F3_ENABLE_TEXT_MESH_PRO
    public class TextMeshProExt : TextMeshPro
    {
        // [RMS] this is a magic number we compute in CreateTextMeshProGO
        public float fontSizeYScale;


        public float GetTextScaleForHeight(float fTextHeight)
        {
            return fTextHeight * fontSizeYScale;
        }

        public void SetTextSizeFromHeight(float fTextHeight)
        {
            float fScaleH = GetTextScaleForHeight(fTextHeight);
            this.transform.localScale = fScaleH * Vector3f.One;
        }

        public void SetFixedWidth(float fWidth)
        {
            if (this.autoSizeTextContainer == true)
                this.autoSizeTextContainer = false;
            fWidth /= this.transform.localScale.x;
            this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fWidth);
        }

        public void SetFixedHeight(float fHeight)
        {
            if (this.autoSizeTextContainer == true )
                this.autoSizeTextContainer = false;
            fHeight /= this.transform.localScale.y;
            this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fHeight);
        }

        public void SetOverflowMode(TextOverflowMode eMode)
        {
            if (this.autoSizeTextContainer == true)
                throw new Exception("TextMeshProExt.SetOverflowMode: cannot set overflow mode if text container is auto-sized. Call SetFixedWidth first.");
            if (eMode == TextOverflowMode.Clipped)
                this.overflowMode = TextOverflowModes.Masking;
            else if (eMode == TextOverflowMode.Truncate)
                this.overflowMode = TextOverflowModes.Truncate;
            else if (eMode == TextOverflowMode.Ellipses)
                this.overflowMode = TextOverflowModes.Ellipsis;
            else 
                this.overflowMode = TextOverflowModes.Overflow;
        }
    }
#endif



    public static class TextMeshProUtil
    {
#if G3_ENABLE_TEXT_MESH_PRO || F3_ENABLE_TEXT_MESH_PRO
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
            TextMeshProExt tm = textGO.AddComponent<TextMeshProExt>();
            //tm.isOrthographic = false;
            tm.alignment = TextAlignmentOptions.TopLeft;
            tm.enableWordWrapping = false;
            tm.autoSizeTextContainer = true;
            tm.fontSize = 16;
            tm.text = sText;
            tm.color = textColor;
            // ignore material changes when we add to GameObjectSet
            textGO.AddComponent<IgnoreMaterialChanges>();
            textGO.AddComponent<TextMeshProAlphaMultiply>();

            // use our textmesh material instead
            //MaterialUtil.SetTextMeshDefaultMaterial(tm);

            // convert TextContainerAnchor (which refers to TextContainer, that was deprecated) to
            // pivot point, which we will set on rectTransform
            Vector2f pivot = GetTextMeshProPivot(TextContainerAnchors.TopLeft);
            if (textOrigin == BoxPosition.Center) {
                pivot = GetTextMeshProPivot(TextContainerAnchors.Middle);
                tm.alignment = TextAlignmentOptions.Center;
            } else if (textOrigin == BoxPosition.BottomLeft) {
                pivot = GetTextMeshProPivot(TextContainerAnchors.BottomLeft);
                tm.alignment = TextAlignmentOptions.BottomLeft;
            } else if (textOrigin == BoxPosition.TopRight) {
                pivot = GetTextMeshProPivot(TextContainerAnchors.TopRight);
                tm.alignment = TextAlignmentOptions.TopRight;
            } else if (textOrigin == BoxPosition.BottomRight) {
                pivot = GetTextMeshProPivot(TextContainerAnchors.BottomRight);
                tm.alignment = TextAlignmentOptions.BottomRight;
            } else if (textOrigin == BoxPosition.CenterLeft) {
                pivot = GetTextMeshProPivot(TextContainerAnchors.Left);
                tm.alignment = TextAlignmentOptions.Left;
            } else if (textOrigin == BoxPosition.CenterRight) {
                pivot = GetTextMeshProPivot(TextContainerAnchors.Right);
                tm.alignment = TextAlignmentOptions.Right;
            } else if (textOrigin == BoxPosition.CenterTop) {
                pivot = GetTextMeshProPivot(TextContainerAnchors.Top);
                tm.alignment = TextAlignmentOptions.Top;
            } else if (textOrigin == BoxPosition.CenterBottom) {
                pivot = GetTextMeshProPivot(TextContainerAnchors.Bottom);
                tm.alignment = TextAlignmentOptions.Bottom;
            }
            tm.rectTransform.pivot = pivot;

            tm.ForceMeshUpdate();

            // read out bounds so we can know size (does this matter? why does fTextGO have size field?)
            AxisAlignedBox3f bounds = tm.bounds;
            Vector2f size = new Vector2f(bounds.Width, bounds.Height);

            tm.fontSizeYScale = GetYScale(tm);
            tm.SetTextSizeFromHeight(fTextHeight);
            float fScale = tm.GetTextScaleForHeight(fTextHeight);
            float fTextWidth = fScale * size.x;

            // set rendering queue (?)
            textGO.GetComponent<Renderer>().material.renderQueue = SceneGraphConfig.TextRendererQueue;

            fTextGameObject go = new fTextGameObject(textGO, new fText(tm, TextType.TextMeshPro),
                new Vector2f(fTextWidth, fTextHeight));
            if (fOffsetZ != 0) {
                Vector3f pos = go.GetLocalPosition();
                pos.z += fOffsetZ;
                go.SetLocalPosition(pos);
            }
            return go;
        }






        public static fTextAreaGameObject CreateTextAreaGO(
            string sName, string sText, 
            Colorf textColor, float fTextHeight, 
            Vector2f areaDimensions,
            HorizontalAlignment alignment = HorizontalAlignment.Left,
            BoxPosition textOrigin = BoxPosition.TopLeft,
            float fOffsetZ = -0.01f)
        {
            GameObject textGO = new GameObject(sName);
            TextMeshProExt tm = textGO.AddComponent<TextMeshProExt>();
            //tm.isOrthographic = false;
            switch ( alignment ) {
                case HorizontalAlignment.Left:
                    tm.alignment = TextAlignmentOptions.TopLeft; break;
                case HorizontalAlignment.Center:
                    tm.alignment = TextAlignmentOptions.Center; break;
                case HorizontalAlignment.Right:
                    tm.alignment = TextAlignmentOptions.TopRight; break;
            }
            tm.enableWordWrapping = true;
            tm.autoSizeTextContainer = false;
            tm.fontSize = 16;
            tm.text = sText;
            tm.color = textColor;
            // ignore material changes when we add to GameObjectSet
            textGO.AddComponent<IgnoreMaterialChanges>();
            textGO.AddComponent<TextMeshProAlphaMultiply>();
            // use our textmesh material instead
            //MaterialUtil.SetTextMeshDefaultMaterial(tm);

            // convert TextContainerAnchor (which refers to TextContainer, that was deprecated) to
            // pivot point, which we will set on rectTransform
            Vector2f pivot = GetTextMeshProPivot(TextContainerAnchors.TopLeft);
            if (textOrigin != BoxPosition.TopLeft)
                throw new Exception("fTextAreaGameObject: only TopLeft text origin is supported?");
            tm.rectTransform.pivot = pivot;

            tm.ForceMeshUpdate();

            tm.fontSizeYScale = GetYScale(tm);
            tm.SetTextSizeFromHeight(fTextHeight);

            tm.SetFixedWidth(areaDimensions.x);
            tm.SetFixedHeight(areaDimensions.y);
            
            textGO.GetComponent<Renderer>().material.renderQueue = SceneGraphConfig.TextRendererQueue;

            return new fTextAreaGameObject(textGO, new fText(tm, TextType.TextMeshPro), areaDimensions);
        }


        public static float GetYScale(TextMeshPro tm)
        {
            // We want to scale text to hit our target height, but if we scale by size.y
            // then the scaling will vary by text height (eg "m" will get same height as "My").
            // However: 1) size.y varies with tm.fontSize, but it's not clear how. 
            //          2) fontInfo.LineHeight tells us the height we want but doesn't change w/ tm.fontSize
            // I tried a few values and the relationship is linear. It is in the ballpark
            // of just being 10x...actually closer to 11x. No other values in fontInfo have a nice
            // round-number relationship. But this value is probably font-dependent!!

            float t = tm.fontSize / tm.font.fontInfo.LineHeight;
            float magic_k = 10.929f;        // [RMS] solve-for-x given a few different fontSize values
            float font_size_y = magic_k * t;
            return 1.0f / font_size_y;
        }



        static Vector2f GetTextMeshProPivot(TextContainerAnchors anchor)
        {
            switch (anchor) {
                case TextContainerAnchors.TopLeft:
                    return new Vector2f(0, 1);
                case TextContainerAnchors.Top:
                    return new Vector2f(0.5f, 1);
                case TextContainerAnchors.TopRight:
                    return new Vector2f(1, 1);
                case TextContainerAnchors.Left:
                    return new Vector2f(0, 0.5f);
                case TextContainerAnchors.Middle:
                    return new Vector2f(0.5f, 0.5f);
                case TextContainerAnchors.Right:
                    return new Vector2f(1, 0.5f);
                case TextContainerAnchors.BottomLeft:
                    return new Vector2f(0, 0);
                case TextContainerAnchors.Bottom:
                    return new Vector2f(0.5f, 0);
                case TextContainerAnchors.BottomRight:
                    return new Vector2f(1, 0);
            }
            return Vector2f.Zero;
        }


#else
        public static bool HaveTextMeshPro { get { return false; } }

        public static fTextGameObject CreateTextMeshProGO(
            string sName, string sText, 
            Colorf textColor, float fTextHeight, 
            BoxPosition textOrigin = BoxPosition.Center, 
            float fOffsetZ = -0.01f)
        {
            throw new NotImplementedException("you need to #define F3_ENABLE_TEXT_MESH_PRO to use TextMeshPro (!)");
        }


        public static fTextAreaGameObject CreateTextAreaGO(
            string sName, string sText, 
            Colorf textColor, float fTextHeight, 
            Vector2f areaDimensions,
            HorizontalAlignment alignment = HorizontalAlignment.Left,
            BoxPosition textOrigin = BoxPosition.Center,
            float fOffsetZ = -0.01f)
        {
            throw new NotImplementedException("you need to #define F3_ENABLE_TEXT_MESH_PRO to use TextMeshPro (!)");
        }

#endif



    }


}
