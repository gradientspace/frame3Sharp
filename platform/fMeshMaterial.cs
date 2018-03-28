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
            ClipPlaneMode = (ClipPlaneModes)(int)mat.ClipPlaneMode;
        }


        public bool EnableWireframe {
            get {
                return GetFloat("_Wireframe") == 0 ? false : true;
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


        public enum ClipPlaneModes
        {
            NoClip = 0,
            Clip = 1,
            ClipAndFill = 2
        }

        public ClipPlaneModes ClipPlaneMode {
            get {
                return (ClipPlaneModes)(int)GetFloat("_EnableClipPlane");
            }
            set {
                float f = MathUtil.Clamp((int)value, 0, 2);
                SetFloat("_EnableClipPlane", f);
            }
        }

    }
}
