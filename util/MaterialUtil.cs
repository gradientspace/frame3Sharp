using System;
using UnityEngine;

namespace f3
{
    public class MaterialUtil
    {
        protected MaterialUtil()
        {
        }


        public static Color MakeColor(Color c, float alpha) {
            return new Color(c.r, c.g, c.b, alpha);
        }

        public static Material CreateStandardMaterial(Color c) {
            Material m = new Material(Resources.Load(SceneGraphConfig.DefaultStandardMaterialPath) as Material);
            m.color = c;
            return m;
        }

        public static Material CreateStandardVertexColorMaterial(Color c)
        {
            Material m = new Material(Resources.Load<Material>("StandardMaterials/default_vertex_colored"));
            m.color = c;
            return m;
        }


        public static Material CreateTransparentMaterial(Color c)
        {
            Material m = new Material(Resources.Load(SceneGraphConfig.DefaultTransparentMaterialPath) as Material);
            m.color = c;
            return m;
        }
        public static Material CreateTransparentMaterial(Color c, float alpha) {
            Material m = new Material(Resources.Load(SceneGraphConfig.DefaultTransparentMaterialPath) as Material);
            m.color = MakeColor(c, alpha);
            return m;
        }

        public static fMaterial CreateTransparentMaterialF(Color c, float alpha)
        {
            Material m = new Material(Resources.Load(SceneGraphConfig.DefaultTransparentMaterialPath) as Material);
            m.color = MakeColor(c, alpha);
            return new fMaterial(m);
        }


        public static Material CreateFlatMaterial(Color c, float alpha = 1.0f) {
            Material m = new Material(Resources.Load(SceneGraphConfig.DefaultUnlitTransparentMaterialPath) as Material);
            m.color = MakeColor(c, c.a * alpha);
            return m;
        }

        public static Material CreateImageMaterial(string sResourcePath) {
            Material m = new Material(Resources.Load(SceneGraphConfig.DefaultUnlitTextureMaterialPath) as Material);
            m.color = Color.white;
            Texture2D tex = Resources.Load<Texture2D>(sResourcePath);
            m.mainTexture = tex;
            return m;
        }

        public static Material CreateTransparentImageMaterial(string sResourcePath)
        {
            Material m = new Material(Resources.Load(SceneGraphConfig.DefaultUnlitTextureTransparentMaterialPath) as Material);
            m.color = Color.white;
            Texture2D tex = Resources.Load<Texture2D>(sResourcePath);
            m.mainTexture = tex;
            return m;
        }


        public static Material CreateTextMeshMaterial()
        {
            Material m = new Material(Resources.Load<Material>("StandardMaterials/default_text_material"));
            return m;
        }
        public static void SetTextMeshDefaultMaterial(TextMesh textMesh)
        {
            Material newMat = CreateTextMeshMaterial();
            newMat.mainTexture = textMesh.GetComponent<Renderer>().material.mainTexture;
            textMesh.GetComponent<Renderer>().material = newMat;
        }



        public static void SetMaterial(GameObject go, Material m)
        {
            Renderer r = go.GetComponent<Renderer>();
            if ( r && r.material != m )
                r.material = m;
        }

        public static void DisableShadows(GameObject go, bool bCastOff = true, bool bReceiveOff = true)
        {
            Renderer ren = go.GetComponent<Renderer>();
            if (bCastOff)
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (bReceiveOff)
                ren.receiveShadows = false;
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

            } else {
                return MaterialUtil.CreateStandardMaterial(Color.black);
            }
        }


    }
}

