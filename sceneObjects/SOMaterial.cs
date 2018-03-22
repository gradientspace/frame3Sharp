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

            StandardMesh,

            Custom
        };
        public virtual MaterialType Type { get; set; }

        public virtual Colorf RGBColor { get; set; }

        // [TODO] abstract this so we don't use unity.Texture2D
        public virtual Texture2D MainTexture { get; set; }


        public enum CullingModes
        {
            None = 0,
            FrontFace = 1,
            BackFace = 2
        }

        /// <summary>
        /// Backface culling mode.
        /// Currently only supported on PerVertexColor and FlatShadedPerVertexColor types!
        /// </summary>
        public CullingModes CullingMode { get; set; }

        /// <summary>
        /// Integer value added to Unity Material.renderQueue 
        /// Can use to nudge objects up/down in rendering order (sometimes helps w/ transparency)
        /// </summary>
        public virtual int RenderQueueShift { get; set; }


        /// <summary>
        /// This function will be called when instantiating the SOMaterial as the platform-specific
        /// material type (eg in unity, called when generating a UnityEngine.Material)
        /// In Unity, the object will be a UnityEngine.Material
        /// </summary>
        public Action<object> MaterialCustomizerF = null;


        [Flags]
        public enum HintFlags
        {
            None = 0,
            UseTransparentPass = 1
        }
        public HintFlags Hints { get; set; }


        public SOMaterial()
        {
            Name = UniqueNames.GetNext("SOMaterial");
            Type = MaterialType.StandardRGBColor;
            RGBColor = Colorf.VideoWhite;
            CullingMode = CullingModes.None;
            Hints = HintFlags.None;
        }

        // in some subclasses we don't want to do default constructor...
        protected SOMaterial(bool do_nothing)
        {
        }

        public virtual SOMaterial Clone() {
            return new SOMaterial() {
                Name = this.Name, Type = this.Type,
                RGBColor = this.RGBColor, MainTexture = this.MainTexture,
                CullingMode = this.CullingMode, RenderQueueShift = this.RenderQueueShift,
                Hints = this.Hints
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
            copy.CullingMode = CullingModes.BackFace;
            return copy;
        }

        public static SOMaterial CreateFlatShaded(string sName, Colorf color)
        {
            return new SOMaterial() { Name = sName, Type = MaterialType.FlatShadedPerVertexColor, RGBColor = color };
        }

        public static SOMaterial CreateMesh(string sName, Colorf color) {
            return new SOMeshMaterial() { Name = sName, RGBColor = color };
        }
    }



}
