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
        protected Material unityMat;

        public fMaterial(UnityEngine.Material m)
        {
            unityMat = m;
        }

        public virtual string name {
            get { return unityMat.name; }
            set { unityMat.name = value; }
        }

        public virtual Colorf color {
            get { return unityMat.color; }
            set { unityMat.color = value; }
        }

        public virtual Texture mainTexture {
            get { return unityMat.mainTexture; }
            set { unityMat.mainTexture = value; }
        }

        public virtual int renderQueue {
            get { return unityMat.renderQueue; }
            set { unityMat.renderQueue = value; }
        }


        public virtual void SetInt(string identifier, int value) {
            unityMat.SetInt(identifier, value);
        }
        public virtual int GetInt(string identifier) {
            return unityMat.GetInt(identifier);
        }

        public virtual void SetFloat(string identifier, float value) {
            unityMat.SetFloat(identifier, value);
        }
        public virtual float GetFloat(string identifier) {
            return unityMat.GetFloat(identifier);
        }

        public virtual void SetVector(string identifier, Vector4f value) {
            unityMat.SetVector(identifier, value);
        }
        public virtual Vector4f GetVector(string identifier) {
            return unityMat.GetVector(identifier);
        }


        public static implicit operator UnityEngine.Material(fMaterial mat)
        {
            return mat.unityMat;
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
            get { return unityMat.color; }
            set {
                bool alpha_change = (value.a == 1.0f && unityMat.color.a != 1.0f) ||
                                    (value.a != 1.0f && unityMat.color.a == 1.0f);
                unityMat.color = value;
                if (alpha_change) {
                    DebugUtil.Log(2, "changing alpha to {0}", value.a);
                    if (value.a == 1) {
                        MaterialUtil.SetupMaterialWithBlendMode(unityMat, MaterialUtil.BlendMode.Opaque);
                        unityMat.renderQueue = OpaqueRenderQueue;
                    } else {
                        MaterialUtil.SetupMaterialWithBlendMode(unityMat, MaterialUtil.BlendMode.Transparent);
                        unityMat.renderQueue = TransparentRenderQueue;
                    }
                }
            }
        }

    }

}
