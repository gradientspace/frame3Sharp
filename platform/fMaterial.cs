using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    /// <summary>
    /// fMaterial wraps a Material for frame3Sharp. The idea is that eventually we
    ///  will be able to "replace" Material with something else, ie non-Unity stuff.
    ///
    /// implicit cast operators allow transparent conversion from fMaterial to Material    
    /// </summary>
    public class fMaterial
    {
        protected Material unitymaterial;


        public fMaterial(UnityEngine.Material m)
        {
            unitymaterial = m;
        }

        public virtual Colorf color
        {
            get { return unitymaterial.color; }
            set { unitymaterial.color = value; }
        }

        public int renderQueue {
            get { return unitymaterial.renderQueue; }
            set { unitymaterial.renderQueue = value; }
        }

        public static implicit operator UnityEngine.Material(fMaterial mat)
        {
            return mat.unitymaterial;
        }
        public static implicit operator fMaterial(UnityEngine.Material mat)
        {
            return (mat != null) ? new fMaterial(mat) : null;
        }
    }




    /// <summary>
    /// This is a specialization of fMaterial that (should) automatically switch
    /// between opaque and transparent rendering modes depending on alpha value.
    /// In Unity this is non-trivial because you can't just change renderQueue,
    /// also requires various shader flags.
    /// (Possibly it is best to initialize this w/ StandardShader)
    /// </summary>
    public class fDynamicTransparencyMaterial : fMaterial
    {
        public int OpaqueRenderQueue = 1000;
        public int TransparentRenderQueue = 3000;

        public fDynamicTransparencyMaterial(UnityEngine.Material m) : base(m)
        {
        }

        public override Colorf color {
            get { return unitymaterial.color; }
            set {
                bool alpha_change = (value.a == 1.0f && unitymaterial.color.a != 1.0f) ||
                                    (value.a != 1.0f && unitymaterial.color.a == 1.0f);
                unitymaterial.color = value;
                if (alpha_change) {
                    DebugUtil.Log(2, "changing alpha to {0}", value.a);
                    if (value.a == 1) {
                        MaterialUtil.SetupMaterialWithBlendMode(unitymaterial, MaterialUtil.BlendMode.Opaque);
                        unitymaterial.renderQueue = OpaqueRenderQueue;
                    } else {
                        MaterialUtil.SetupMaterialWithBlendMode(unitymaterial, MaterialUtil.BlendMode.Transparent);
                        unitymaterial.renderQueue = TransparentRenderQueue;
                    }
                }
            }
        }

    }

}
