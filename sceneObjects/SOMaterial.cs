using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;      // required from Texture2D
using g3;

namespace f3
{
    public class SOMaterial
    {
        public virtual string Name { get; set; }

        public enum MaterialType
        {
            StandardRGBColor,
            TransparentRGBColor,
            PerVertexColor,
            TextureMap,
            UnlitRGBColor,
            DepthWriteOnly,
            FlatShadedPerVertexColor,

            Custom
        };
        public virtual MaterialType Type { get; set; }

        public virtual Colorf RGBColor { get; set; }

        // [TODO] abstract this so we don't use unity.Texture2D
        public virtual Texture2D MainTexture { get; set; }


        public virtual int RenderQueueShift { get; set; }


        public SOMaterial()
        {
            Name = UniqueNames.GetNext("SOMaterial");
            Type = MaterialType.StandardRGBColor;
            RGBColor = Colorf.VideoWhite;
        }

        // in some subclasses we don't want to do default constructor...
        protected SOMaterial(bool do_nothing)
        {
        }

        public virtual SOMaterial Clone() {
            return new SOMaterial() {
                Name = this.Name, Type = this.Type,
                RGBColor = this.RGBColor, MainTexture = this.MainTexture
            };
        }


        public fMaterial ToFMaterial()
        {
            return MaterialUtil.ToMaterialf(this);
        }


        public static SOMaterial CreateStandard(string sName, Colorf color) {
            return new SOMaterial() { Name = sName, Type = MaterialType.StandardRGBColor, RGBColor = color };
        }
        public static SOMaterial CreateTransparent(string sName, Colorf color) {
            return new SOMaterial() { Name = sName, Type = MaterialType.TransparentRGBColor, RGBColor = color };
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
