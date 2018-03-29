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







    // wrap unity fMeshMaterial (which wraps unity Material) in an SOMaterial
    public class UnitySOMeshMaterial : SOMeshMaterial
    {
        public fMeshMaterial meshMaterial;

        public UnitySOMeshMaterial(fMeshMaterial wrapMaterial) : base(false)
        {
            meshMaterial = wrapMaterial;
        }

        public override string Name {
            get { return meshMaterial.name; }
            set { meshMaterial.name = value; }
        }

        public override MaterialType Type {
            get { return SOMaterial.MaterialType.Custom; }
            set { throw new NotImplementedException("UnitySOMeshMaterial: cannot set type!"); }
        }

        public override Colorf RGBColor {
            get { return meshMaterial.color; }
            set { meshMaterial.color = value; }
        }

        public override Texture2D MainTexture {
            get { return meshMaterial.mainTexture as Texture2D; }
            set { meshMaterial.mainTexture = value; }
        }

        public override bool EnableWireframe {
            get { return meshMaterial.EnableWireframe; }
            set { meshMaterial.EnableWireframe = value; }
        }

        public override ClipPlaneModes ClipPlaneMode {
            get { return (ClipPlaneModes)(int)meshMaterial.ClipPlaneMode; }
            set { meshMaterial.ClipPlaneMode = (fMeshMaterial.ClipPlaneModes)(int)value; }
        }

        public override Frame3f ClipPlanePos {
            get { return meshMaterial.ClipPlanePos; }
            set { meshMaterial.ClipPlanePos = value; }
        }


        public override SOMaterial Clone()
        {
            Material copyM = UnityEngine.Object.Instantiate<Material>(meshMaterial);
            fMeshMaterial meshM = new fMeshMaterial(copyM);
            return new UnitySOMeshMaterial(meshM);
        }

    }




}
