using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    /// <summary>
    /// wrapper around fMaterial specifically for f3 StandardMeshShader.
    /// (Currently does not enforce this, though...)
    /// </summary>
    public class fMeshMaterial : fMaterial
    {
        public fMeshMaterial(UnityEngine.Material m) : base(m)
        {
        }

        public void InitializeFromSOMaterial(SOMeshMaterial mat)
        {
            EnableWireframe = mat.EnableWireframe;
        }


        public bool EnableWireframe {
            get {
                return GetFloat("__Wireframe") == 0 ? false : true;
            }
            set {
                if (value) {
                    SetFloat("_Wireframe", 1.0f);
                    unityMat.EnableKeyword("_ENABLE_WIREFRAME");
                } else {
                    SetFloat("_Wireframe", 0.0f);
                    unityMat.DisableKeyword("_ENABLE_WIREFRAME");
                }
            }
        }
    }
}
