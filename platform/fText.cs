using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;
using g3;

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
                    (text_component as TextMeshPro).text = text;
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
                    (text_component as TextMeshPro).color = color;
                    break;
            }
        }



        public Vector2f GetCursorPosition(int iPos)
        {
            // not supporting unity TextMesh
            // this might do it: http://answers.unity3d.com/questions/31622/is-it-possible-to-find-the-width-of-a-text-mesh.html

            TextMeshPro textmesh = (text_component as TextMeshPro);

            if (iPos == 0)
                return new Vector2f(0, 0);

            float fWidth = textmesh.textInfo.characterInfo[iPos-1].xAdvance;
            fWidth *= textmesh.transform.localScale[0];
            return new Vector2f(fWidth, 0);
        }

    }
}
