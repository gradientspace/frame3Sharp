using System;
using UnityEngine;
using g3;

namespace f3
{
    public class MaterialUtil
    {
        protected MaterialUtil()
        {
        }


        public static Colorf MakeColor(Colorf c, float alpha) {
            return new Colorf(c.r, c.g, c.b, alpha);
        }

        public static Material CreateStandardMaterial(Colorf c) {
            Material m = SafeLoadMaterial(SceneGraphConfig.DefaultStandardMaterialPath);
            m.color = c;
            return m;
        }
        public static fMaterial CreateStandardMaterialF(Colorf c) {
            Material m = SafeLoadMaterial(SceneGraphConfig.DefaultStandardMaterialPath);
            m.color = c;
            return new fMaterial(m);
        }


        public static Material CreateStandardVertexColorMaterial(Colorf c)
        {
            Material m = SafeLoadMaterial("StandardMaterials/default_vertex_colored");
            m.color = c;
            return m;
        }
        public static fMaterial CreateStandardVertexColorMaterialF(Colorf c)
        {
            Material m = SafeLoadMaterial("StandardMaterials/default_vertex_colored");
            m.color = c;
            return new fMaterial(m);
        }


        public static Material CreateTransparentMaterial(Colorf c)
        {
            Material m = SafeLoadMaterial(SceneGraphConfig.DefaultTransparentMaterialPath);
            m.color = c;
            return m;
        }
        public static fMaterial CreateTransparentMaterialF(Colorf c)
        {
            Material m = SafeLoadMaterial(SceneGraphConfig.DefaultTransparentMaterialPath);
            m.color = c;
            return new fMaterial(m);
        }

        public static Material CreateTransparentMaterial(Colorf c, float alpha) {
            Material m = SafeLoadMaterial(SceneGraphConfig.DefaultTransparentMaterialPath);
            m.color = MakeColor(c, alpha);
            return m;
        }
        public static fMaterial CreateTransparentMaterialF(Colorf c, float alpha)
        {
            Material m = SafeLoadMaterial(SceneGraphConfig.DefaultTransparentMaterialPath);
            m.color = MakeColor(c, alpha);
            return new fMaterial(m);
        }


        public static Material CreateFlatMaterial(Colorf c, float alpha = 1.0f) {
            Material m = SafeLoadMaterial(SceneGraphConfig.DefaultUnlitTransparentMaterialPath);
            m.color = MakeColor(c, c.a * alpha);
            return m;
        }
        public static fMaterial CreateFlatMaterialF(Colorf c, float alpha = 1.0f) {
            Material m = SafeLoadMaterial(SceneGraphConfig.DefaultUnlitTransparentMaterialPath);
            m.color = MakeColor(c, c.a * alpha);
            return new fMaterial(m);
        }

        public static Material CreateImageMaterial(string sResourcePath) {
            Material m = SafeLoadMaterial(SceneGraphConfig.DefaultUnlitTextureMaterialPath);
            m.color = Colorf.White;
            Texture2D tex = SafeLoadTexture2D(sResourcePath);
            m.mainTexture = tex;
            return m;
        }

        public static Material CreateTransparentImageMaterial(string sResourcePath)
        {
            Material m = SafeLoadMaterial(SceneGraphConfig.DefaultUnlitTextureTransparentMaterialPath);
            m.color = Colorf.White;
            Texture2D tex = SafeLoadTexture2D(sResourcePath);
            m.mainTexture = tex;
            return m;
        }
        public static fMaterial CreateTransparentImageMaterialF(string sResourcePath) {
            return new fMaterial(CreateTransparentImageMaterial(sResourcePath));
        }


        public static fMaterial CreateParticlesMaterial() {
            Material m = SafeLoadMaterial(SceneGraphConfig.DefaultParticleMaterialPath);
            return new fMaterial(m);
        }


        public static fMaterial CreateDropShadowMaterial(Colorf c, float w, float h, float falloff)
        {
            Material m = SafeLoadMaterial(SceneGraphConfig.DefaultDropShadowMaterialPath);
            m.SetFloat("_FalloffWidth", falloff);
            m.SetVector("_Center", new Vector4(0, 0, 0, 0));
            m.SetVector("_Extents", new Vector4(w/2, h/2, 0, 0));
            m.color = c;
            return new fMaterial(m);
        }


        public static Material CreateTextMeshMaterial()
        {
            Material m = SafeLoadMaterial("StandardMaterials/default_text_material");
            return m;
        }
        public static void SetTextMeshDefaultMaterial(TextMesh textMesh)
        {
            Material newMat = CreateTextMeshMaterial();
            newMat.mainTexture = textMesh.GetComponent<Renderer>().material.mainTexture;
            textMesh.GetComponent<Renderer>().material = newMat;
        }



        public static Material SafeLoadMaterial(string sPath)
        {
            Material mat = null;
            try {
                Material loaded = Resources.Load<Material>(sPath);
                mat = new Material(loaded);
            } catch (Exception e) {
                DebugUtil.Log(2, "MaterialUtil.SafeLoadMaterial: exception: " + e.Message);
                mat = new Material(Shader.Find("Standard"));
                mat.color = Color.red;
            }
            return mat;
        }


        public static Texture2D SafeLoadTexture2D(string sPath)
        {
            Texture2D tex = null;
            try {
                Texture2D loaded = Resources.Load<Texture2D>(sPath);
                if (loaded == null)
                    throw new Exception("Texture " + sPath + " not found!!");
                tex = loaded;
                //tex = Texture2D.Instantiate<Texture2D>(loaded);
            } catch ( Exception e) {
                DebugUtil.Log(2, "MaterialUtil.SafeLoadTexture2D: exception: " + e.Message);
                tex = Texture2D.Instantiate<Texture2D>(Texture2D.whiteTexture);
            }
            return tex;
        }


        public static void SetMaterial(GameObject go, Material m)
        {
            Renderer r = go.GetComponent<Renderer>();
            if ( r && r.material != m )
                r.material = m;
        }
        public static Material GetMaterial(GameObject go)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r)
                return r.material;
            return null;
        }


        public static void DisableShadows(GameObject go, bool bCastOff = true, bool bReceiveOff = true, bool bRecursive = true)
        {
            Renderer ren = go.GetComponent<Renderer>();
            if (ren != null) {
                if (bCastOff)
                    ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (bReceiveOff)
                    ren.receiveShadows = false;
            }
            if ( bRecursive ) {
                foreach (var childgo in go.Children())
                    DisableShadows(childgo, bCastOff, bRecursive, bRecursive);
            }
        }

        public static void SetIgnoreMaterialChanges(fGameObject go)
        {
            ((GameObject)go).AddComponent<IgnoreMaterialChanges>();
        }


        // [RMS] code to change Material mode setting at runtime 
        //   (ie the "Rendering Mode" drop-down in Material properties panel)
        // source: https://forum.unity3d.com/threads/standard-material-shader-ignoring-setfloat-property-_mode.344557/

        public enum BlendMode
        {
            Opaque,
            Cutout,
            Fade,        // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
        }

        static public void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
        {
            switch (blendMode) {
                case BlendMode.Opaque:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = -1;
                    break;
                case BlendMode.Cutout:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 2450;
                    break;
                case BlendMode.Fade:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                    break;
                case BlendMode.Transparent:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                    break;
            }
        }



        public static SOMaterial FromUnityMaterial(Material unityMat)
        {
            SOMaterial soMat = new SOMaterial();
            soMat.Name = unityMat.name;
            soMat.RGBColor = unityMat.color;

            string tag = unityMat.GetTag("RenderType", true);
            bool bTransparent = (unityMat.color.a < 1 || tag == "Transparent");

            if ( bTransparent ) {
                soMat.Type = SOMaterial.MaterialType.TransparentRGBColor;
            } else {
                soMat.Type = SOMaterial.MaterialType.StandardRGBColor;              
            }

            return soMat;
        }




        public static Material ToUnityMaterial(SOMaterial m)
        {
            if (m.Type == SOMaterial.MaterialType.TextureMap) {
                Material unityMat = new Material(Shader.Find("Standard"));
                unityMat.SetName(m.Name);
                unityMat.color = m.RGBColor;
                //if (m.Alpha < 1.0f)
                //    MaterialUtil.SetupMaterialWithBlendMode(unityMat, MaterialUtil.BlendMode.Transparent);
                unityMat.mainTexture = m.MainTexture;
                return unityMat;

            } else if (m.Type == SOMaterial.MaterialType.PerVertexColor) {
                return MaterialUtil.CreateStandardVertexColorMaterial(m.RGBColor);

            } else if (m.Type == SOMaterial.MaterialType.TransparentRGBColor) {
                return MaterialUtil.CreateTransparentMaterial(m.RGBColor);

            } else if (m.Type == SOMaterial.MaterialType.StandardRGBColor) {
                return MaterialUtil.CreateStandardMaterial(m.RGBColor);

            } else if (m.Type == SOMaterial.MaterialType.UnlitRGBColor) {
                return MaterialUtil.CreateFlatMaterial(m.RGBColor);

            } else if ( m is UnitySOMaterial ) {
                return (m as UnitySOMaterial).unityMaterial;

            } else {
                return MaterialUtil.CreateStandardMaterial(Color.black);
            }
        }



        public static fMaterial ToMaterialf(SOMaterial m)
        {
            return new fMaterial(ToUnityMaterial(m));
        }

    }
}

