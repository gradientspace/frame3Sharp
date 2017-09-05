using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    //
    // fMaterial wraps a Material for frame3Sharp. The idea is that eventually we
    //  will be able to "replace" Material with something else, ie non-Unity stuff.
    //
    // implicit cast operators allow transparent conversion from fMaterial to Material
    //
    public class fMaterial
    {
        Material unitymaterial;


        public fMaterial(UnityEngine.Material m)
        {
            unitymaterial = m;
        }

        public Colorf color
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
}
