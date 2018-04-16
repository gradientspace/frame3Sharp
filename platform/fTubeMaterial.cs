using System;
using UnityEngine;

namespace f3
{
    /// <summary>
    /// wrapper around fMaterial specifically for f3 TubeShader.
    /// (Currently does not enforce this, though...)
    /// </summary>
    public class fTubeMaterial : fMaterial
    {
        public fTubeMaterial(Material m) : base(m)
        {
        }

        public float Radius {
            get { return GetFloat("_Radius"); }
            set { SetFloat("_Radius", value); }
        }

        public int Slices {
            get { return GetInt("_TubeN"); }
            set { SetInt("_TubeN", value); }
        }

        public float Emission {
            get { return GetFloat("_Emission"); }
            set { SetFloat("_Emission", value); }
        }

        public bool EnablePerVertexColors {
            get {
                return GetFloat("_PerVertexColors") == 0 ? false : true;
            }
            set {
                SetFloat("_PerVertexColors", (value) ? 1.0f : 0.0f);
            }
        }

        public bool EnableLighting {
            get {
                return GetFloat("_DiffuseLighting") == 0 ? false : true;
            }
            set {
                SetFloat("_DiffuseLighting", (value) ? 1.0f : 0.0f);
            }
        }

    }
}
