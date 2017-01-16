using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using UnityEngine;

namespace f3
{
    public class FPlatform
    {
        static public int GetLayerID(string sLayerName)
        {
            return LayerMask.NameToLayer(sLayerName);
        }
        static public int WidgetOverlayLayer { get { return GetLayerID(SceneGraphConfig.WidgetOverlayLayerName); } }
        static public int HUDLayer { get { return GetLayerID(SceneGraphConfig.HUDLayerName); } }
        static public int UILayer { get { return GetLayerID(SceneGraphConfig.UILayerName); } }
        static public int CursorLayer { get { return GetLayerID(SceneGraphConfig.CursorLayerName); } }


        static public float RealTime()
        {
            return Time.realtimeSinceStartup;
        }

        static private int frame_counter = 0;
        static public int FrameCounter()
        {
            return frame_counter;
        }
        static internal void IncrementFrameCounter()
        {
            frame_counter++;
        }


        static public bool IsUsingVR()
        {
            return UnityEngine.VR.VRSettings.enabled;
            //return UnityEngine.VR.VRSettings.isDeviceActive;
        }


        static public bool IsTouchDevice()
        {
#if UNITY_EDITOR
            if ( UnityEditor.EditorApplication.isRemoteConnected )
                return true;
#endif
            return (Application.platform == RuntimePlatform.IPhonePlayer
                        || Application.platform == RuntimePlatform.Android );
        }



        static public System.IntPtr GetWindowHandle()
        {
#if UNITY_STANDALONE_WIN
            return GetActiveWindow();
#else
            return System.IntPtr.Zero;
#endif
        }



        //! Show an open-file dialog and with the provided file types. 
        //! Returns path to selected file, or null if Cancel is clicked.
        //! Uses system file dialog if available, otherwise Mono cross-platform dialog
        static public string GetOpenFileName(string sDialogTitle, string sInitialPathAndFile, 
                string[] filterPatterns, string sPatternDesc)
        {
#if UNITY_STANDALONE_WIN
            IntPtr p = tinyfd_openFileDialog(sDialogTitle, sInitialPathAndFile, 
                filterPatterns.Length, filterPatterns, sPatternDesc, 0);
            if (p == IntPtr.Zero)
                return null;
            else
                return stringFromChar(p);
#else
            // [TODO] implement
            return null;
#endif
        }




        //! Show an open-file dialog and with the provided file types. 
        //! Returns path to selected file, or null if Cancel is clicked.
        //! Uses system file dialog if available, otherwise Mono cross-platform dialog
        static public string GetSaveFileName(string sDialogTitle, string sInitialPathAndFile, 
                string[] filterPatterns, string sPatternDesc)
        {
#if UNITY_STANDALONE_WIN
            IntPtr p = tinyfd_saveFileDialog(sDialogTitle, sInitialPathAndFile, 
                filterPatterns.Length, filterPatterns, sPatternDesc);
            if (p == IntPtr.Zero)
                return null;
            else
                return stringFromChar(p);
#else
            // [TODO] implement
            return null;
#endif
        }





        // platform-specific interop functions

#if UNITY_STANDALONE_WIN
        [DllImport("user32.dll")]
        private static extern System.IntPtr GetActiveWindow();

        [DllImport("tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_inputBox(string aTitle, string aMessage, string aDefaultInput);
        //IntPtr lulu = tinyfd_inputBox("input box", "gimme a string", "lolo");

        [DllImport("tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_openFileDialog(string aTitle, string aDefaultPathAndFile, int aNumOfFilterPatterns, string[] aFilterPatterns, string aSingleFilterDescription, int aAllowMultipleSelects);
        //IntPtr p = tinyfd_openFileDialog("select a mesh file", "c:\\scratch\\", 2, new string[] { "*.stl", "*.obj" }, "mesh files", 0);

        [DllImport("tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_saveFileDialog(string aTitle, string aDefaultPathAndFile, int aNumOfFilterPatterns, string[] aFilterPatterns, string aSingleFilterDescription);
        //IntPtr p = tinyfd_openFileDialog("select a mesh file", "c:\\scratch\\default.stl", 2, new string[] { "*.stl", "*.obj" }, "mesh files", 0);

#endif


        private static string stringFromChar(IntPtr ptr) {
            return Marshal.PtrToStringAnsi(ptr);
        }



    }


}
