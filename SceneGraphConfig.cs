using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{
    public class SavedSettings
    {
        Dictionary<string, object> settings = new Dictionary<string, object>();
        static SavedSettings singleton;

        public static SavedSettings Get
        {
            get { if (singleton == null) singleton = new SavedSettings(); return singleton; }
        }


        public static void Save(string sName, object value)
        {
            Get.settings[sName] = value;
        }
        public static object Restore(string sName)
        {
            if (Get.settings.ContainsKey(sName))
                return Get.settings[sName];
            return null;
        }

    }


	public class SceneGraphConfig
	{
        static string lastFileOpenPath = "c:\\";

		protected SceneGraphConfig ()
		{
            ActiveDoubleClickDelay = MouseDoubleClickDelay;
        }

        public static string GeometryLayerName {
            get { return "Default"; }
        }

        // assumption is that layer with this name has been created in the Editor, because we 
        // cannot create layers in code. We will use this layer to draw overlay 3D objects (eg 3D widgets, etc)
        public static string WidgetOverlayLayerName {
			get { return "3DWidgetOverlay"; }
		}

        // same as above, overlay for HUD/cockpit
        public static string HUDLayerName
        {
            get { return "HUDOverlay"; }
        }

        // same as above, overlay for 2D UI
        public static string UILayerName
        {
            get { return "UIOverlay"; }
        }

        // same as above, overlay for cursor 
        public static string CursorLayerName
        {
            get { return "CursorOverlay"; }
        }

        // for transparent objects, unity Z-sorts by the center of each mesh, which can cause transparent
        //  things to pop behind/infront objects close to them (eg the background rect for a text field).
        //  We can tweak this by putting the text in a different z-sorting layer, which is defind by
        //  Renderer.material.renderQueue.
        // details on render queue #'s: https://docs.unity3d.com/Manual/SL-SubShaderTags.html
        //   (An alternative is to put it in a separate Layer / Sorting Layer, but this is quite heavy and
        //    we are trying to reserve that for major different layers, because there is no z-testing between them)
        // This is the default 'Transparent' renderQueue in Unity, so text will be z-ordered same as opaque elements
        public static int TextRendererQueue = 3000;


        public static string DefaultStandardMaterialPath
        {
            get { return "StandardMaterials/default_standard"; }
        }
        public static string DefaultTransparentMaterialPath {
			get { return "StandardMaterials/standard_transparent"; }
		}
        public static string DefaultUnlitTransparentMaterialPath
        {
            get { return "StandardMaterials/default_unlit_transparent"; }
        }
        public static string DefaultUnlitTextureTransparentMaterialPath
        {
            get { return "StandardMaterials/default_unlit_texture_transparent"; }
        }
        public static string DefaultUnlitTextureMaterialPath
        {
            get { return "StandardMaterials/default_unlit_texture"; }
        }
        public static string DefaultParticleMaterialPath
        {
            get { return "StandardMaterials/default_particle"; }
        }
        public static string DefaultDropShadowMaterialPath
        {
            get { return "StandardMaterials/drop_shadow_mat"; }
        }
        public static string DefaultDepthWriteOnlyMaterialPath {
            get { return "StandardMaterials/depth_write_only"; }
        }


        public static string LastFileOpenPath
        {
            get { return lastFileOpenPath; }
            set { lastFileOpenPath = value; PlayerPrefs.SetString("LastFileOpenPath", value); }
        }


        // [RMS] this is what we use for VR scenes...should not be set to this value, probably!!
        public static Vector3f InitialSceneTranslate = Vector3f.Zero;


        public static float DefaultPivotVisualDegrees = 2.0f;
        public static float DefaultAxisGizmoVisualDegrees = 25.0f;
        public static float DefaultSnapCurveVisualDegrees = 0.25f;
        public static float DefaultSceneCurveVisualDegrees = 1.0f;
        public static float DefaultSnapDistVisualDegrees = 5.0f;


        // VR "hand" cursor dimensions. You can use these to find different points on
        // the arrrows (eg tip is handFrame.Origin + VRHandTipOffset*handFrame.Z)
        public static float VRHandTipOffset = 0.1f;
        public static float VRHandArrowRadius = 0.1f;
        public static float VRHandStickLength = 0.1f;
        public static float VRHandStickWidth = 0.05f;


        public static bool EnableVisibleCameraPivot = true;


        // Default Z-offset for text relative to background rect, used in HUDLabel, HUDTextEntry, etc...
        // In 2D, this doesn't matter except that if it is too small there might be Z-fighting, and if
        // you have overlapping 2D elements it might make the text poke through.
        // In 3D, if it is too large there will be large gaps between text and widgets
        // Only used at widget creation time, so you can change it dynamically if need be.
        public static float TextLabelZOffset = -0.01f;


        // determined by geometry in SpatialInputController.cs
        public static float HandTipOffset = 0.1f;


        // set this to 0 to disable cursor-hide
        public static float MouseCursorHideTimeout {
            get { return 5.0f; }
        }

        public static float MouseDoubleClickDelay {
            get { return 0.3f; }
        }
        public static float TriggerDoubleClickDelay {
            get { return 0.4f; }
        }
        public static float ActiveDoubleClickDelay { get; set; }



        public static void ConfigureForVR() {
            // bump up text render queue. This means *all* text will be drawn on top of UI elements.
            TextRendererQueue = 3100;
        }




        public static void RestorePreferences()
        {
            if (PlayerPrefs.HasKey("LastFileOpenPath")) {
                string sDir = PlayerPrefs.GetString("LastFileOpenPath");
                if (Directory.Exists(sDir))
                    lastFileOpenPath = sDir;
            }
        }

	}
}

