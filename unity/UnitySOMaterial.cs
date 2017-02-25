using System;
using UnityEngine;
using g3;

namespace f3
{

    // wrap arbitrary unity Material in an SOMaterial
    public class UnitySOMaterial : SOMaterial
    {
        public Material unityMaterial;


        public UnitySOMaterial(Material wrapMaterial) : base(false)
        {
            unityMaterial = wrapMaterial;
        }

        public override string Name
        {
            get { return unityMaterial.name; }
            set { unityMaterial.name = value; }
        }

        public override MaterialType Type
        {
            get { return SOMaterial.MaterialType.Custom; }
            set { throw new NotImplementedException("UnitySOMaterial: cannot set type!"); }
        }

        public override Colorf RGBColor {
            get { return unityMaterial.color; }
            set { unityMaterial.color = value; }
        }

        public override Texture2D MainTexture
        {
            get { return unityMaterial.mainTexture as Texture2D; }
            set { unityMaterial.mainTexture = value; }
        }

        public override SOMaterial Clone() {
            Material copyM = UnityEngine.Object.Instantiate<Material>(unityMaterial);
            return new UnitySOMaterial(copyM);
        }

    }
}
