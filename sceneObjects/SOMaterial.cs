using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    public class SOMaterial
    {
        public string Name { get; set; }

        public enum MaterialType
        {
            StandardRGBColor,
            TransparentRGBColor,
            PerVertexColor,
            TextureMap
        };
        public MaterialType Type { get; set; }

        public Colorf RGBColor { get; set; }

        // [TODO] abstract this so we don't use unity.Texture2D
        public Texture2D MainTexture { get; set; }


        public SOMaterial()
        {
            Name = UniqueNames.GetNext("SOMaterial");
            Type = MaterialType.StandardRGBColor;
            RGBColor = Colorf.VideoWhite;
        }

        public SOMaterial Clone() {
            return new SOMaterial() {
                Name = this.Name, Type = this.Type,
                RGBColor = this.RGBColor, MainTexture = this.MainTexture
            };
        }


        public static SOMaterial CreateStandard(string sName, Colorf color) {
            return new SOMaterial() { Name = sName, Type = MaterialType.StandardRGBColor, RGBColor = color };
        }
        public static SOMaterial CreateTransparentVariant(SOMaterial m, float fAlpha)
        {
            if (m.Type != MaterialType.StandardRGBColor)
                throw new NotImplementedException("SOMaterial.CreateTransparentVariant only supports Standard materials");
            SOMaterial copy = m.Clone();
            copy.Name += "_Tr";
            copy.Type = MaterialType.TransparentRGBColor;
            copy.RGBColor = new Colorf(copy.RGBColor, fAlpha);
            return copy;
        }
    }
}
