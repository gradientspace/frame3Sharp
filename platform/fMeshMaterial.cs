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



        Frame3f clip_plane_pos = Frame3f.Identity;
        public Frame3f ClipPlanePos {
            get {
                Vector4f v = GetVector("_ClipPlaneEquation");
                Vector3f z = new Vector3f(v.x, v.y, v.z);
                Vector3f o = v.w * z;
                return new Frame3f(o, z);
            }
            set {
                Vector3f z = value.Z, o = value.Origin;
                SetVector("_ClipPlaneEquation", new Vector4f(z.x, z.y, z.z, z.Dot(o)));

            }
        }

    }
}
