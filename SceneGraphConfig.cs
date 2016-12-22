using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

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
        public static int TextRendererQueue
        {
            get { return 3100; }
        }

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


        public static string LastFileOpenPath
        {
            get { return lastFileOpenPath; }
            set { lastFileOpenPath = value; PlayerPrefs.SetString("LastFileOpenPath", value); }
        }


        public static Vector3 InitialSceneTranslate
        {
            get { return -4.0f * Vector3.up; }
        }


        public static float DefaultPivotVisualDegrees
        {
            get { return 2.0f; }
        }
        public static float DefaultAxisGizmoVisualDegrees
        {
            get { return 25.0f; }
        }
        public static float DefaultCurveVisualDegrees
        {
            get { return 0.25f; }
        }

        // determined by geometry in SpatialInputController.cs
        public static float HandTipOffset
        {
            get { return 0.1f; }
        }


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

