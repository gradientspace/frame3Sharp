using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using g3;

namespace f3
{

    /// <summary>
    /// Extension of SOMaterial specifically for f3 StandardMeshShader,
    /// which has a bunch of configurable features. 
    /// </summary>
    public class SOMeshMaterial : SOMaterial
    {
        public virtual bool EnableWireframe { get; set; }


        public SOMeshMaterial()
        {
            Name = UniqueNames.GetNext("SOMeshMaterial");
            Type = MaterialType.StandardMesh;
            RGBColor = Colorf.VideoWhite;
            CullingMode = CullingModes.None;
            Hints = HintFlags.None;
            EnableWireframe = false;
        }

        protected SOMeshMaterial(bool do_nothing) : base(do_nothing)
        {
        }

        public override SOMaterial Clone()
        {
            return new SOMeshMaterial() {
                Name = this.Name, Type = this.Type,
                RGBColor = this.RGBColor, MainTexture = this.MainTexture,
                CullingMode = this.CullingMode,
                RenderQueueShift = this.RenderQueueShift,
                Hints = this.Hints,
                EnableWireframe = this.EnableWireframe
            };
        }

    }



}
