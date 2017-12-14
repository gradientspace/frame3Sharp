using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using g3;

using UnityEngine;

namespace f3
{
    public class FPlatform
    {
        static public int GetLayerID(string sLayerName)
        {
            return LayerMask.NameToLayer(sLayerName);
        }
        static public int GeometryLayer { get { return GetLayerID(SceneGraphConfig.GeometryLayerName); } }
        static public int WidgetOverlayLayer { get { return GetLayerID(SceneGraphConfig.WidgetOverlayLayerName); } }
        static public int HUDLayer { get { return GetLayerID(SceneGraphConfig.HUDLayerName); } }
        static public int UILayer { get { return GetLayerID(SceneGraphConfig.UILayerName); } }
        static public int CursorLayer { get { return GetLayerID(SceneGraphConfig.CursorLayerName); } }


        // these should probably be accessors? Initialized by CameraTracking right now...
        static public fCamera MainCamera;
        static public fCamera WidgetCamera;
        static public fCamera HUDCamera;
        static public fCamera OrthoUICamera;
        static public fCamera CursorCamera;



        // argh unity can't even return paths to background thread?!?
        static string app_dataPath = null;
        static string persistent_dataPath = null;
        static string temp_dataPath = null;

        /// <summary>
        /// returns path to xyz_Data\ in builds, and path to Assets\ in editor
        /// </summary>
        public static string GameDataFolderPath() {
            if (app_dataPath == null)
                app_dataPath = Path.GetFullPath(Application.dataPath);
            return app_dataPath;
        }

        public static string PersistentDataPath() {
            if (persistent_dataPath == null)
                persistent_dataPath = Path.GetFullPath(Application.persistentDataPath);
            return persistent_dataPath;
        }

        public static string TemporaryDataPath() {
            if (temp_dataPath == null)
                temp_dataPath = Path.GetFullPath(Application.temporaryCachePath);
            return temp_dataPath;
        }

        public static string GameExecutablePath() {
            return Path.GetFullPath(Path.Combine(GameDataFolderPath(), ".."));
        }


        private static float _last_realtime = 0;
        /// <summary> Return clock time since start of program. Can only be called from main thread !! </summary>
        static public float RealTime()
        {
            _last_realtime = Time.realtimeSinceStartup;
            return _last_realtime;
        }
        /// <summary> Return last requested RealTime() - can be called from any thread, but may be out-of-date</summary>
        static public float SafeRealTime()
        {
            return _last_realtime;
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


        static public int ScreenWidth {
            get { return Screen.width; }
        }
        static public int ScreenHeight {
            get { return Screen.height; }
        }
        static public float ScreenDPI {
            get { return (Screen.dpi == 0) ? 96 : Screen.dpi; }
        }

        // multiplier on Cockpit.GetPixelScale() applied when *not* running in editor.
        static public float PixelScaleFactor = 1.0f;

        // multiplier on Cockpit.GetPixelScale() applied when running in editor.
        static public float EditorPixelScaleFactor = 0.5f;



        // argh unity does not have a window resize event built-in ?!??
        static private int window_width = -1, window_height = -1;
        //static private int startup_height = -1;
        static public bool IsWindowResized()
        {
            if ( window_width == -1 || window_height == -1 ) {
                window_height = Screen.height;
                window_width = Screen.width;
                //startup_height = window_height;
                return false;
            }
            if ( window_height != Screen.height || window_width != Screen.width ) {
                window_height = Screen.height;
                window_width = Screen.width;
                return true;
            }
            return false;
        }

        // [RMS] Cockpit uses this to clamp screen-size used for PixelScale. Set to
        // larger min / smaller max to prevent crazy huge/tiny UI.
        static Interval1i valid_screen_dim_range = new Interval1i(512, 8096);
        static public Interval1i ValidScreenDimensionRange
        {
            get { return valid_screen_dim_range; }
            set { valid_screen_dim_range = value; }
        }


        static private bool _in_unity_editor = Application.isEditor;
        static public bool InUnityEditor()
        {
            return _in_unity_editor;
        }


        [Flags]
        public enum fDeviceType {
            WindowsDesktop = 1,
            WindowsVR = 2,
            OSXDesktop = 4,
            IPhone = 8,
            IPad = 16,
            AndroidPhone = 32
        }

        static public fDeviceType GetDeviceType()
        {
            #if UNITY_IOS 
                switch( UnityEngine.iOS.Device.generation ) {
                    case UnityEngine.iOS.DeviceGeneration.iPad1Gen:
                    case UnityEngine.iOS.DeviceGeneration.iPad2Gen:
                    case UnityEngine.iOS.DeviceGeneration.iPad3Gen:
                    case UnityEngine.iOS.DeviceGeneration.iPad4Gen:
                    //case UnityEngine.iOS.DeviceGeneration.iPad5Gen:  // same as iPadAir1 ?
                    case UnityEngine.iOS.DeviceGeneration.iPadAir1:
                    case UnityEngine.iOS.DeviceGeneration.iPadAir2:
                    case UnityEngine.iOS.DeviceGeneration.iPadMini1Gen:
                    case UnityEngine.iOS.DeviceGeneration.iPadMini2Gen:
                    case UnityEngine.iOS.DeviceGeneration.iPadMini3Gen:
                    case UnityEngine.iOS.DeviceGeneration.iPadMini4Gen:
                    case UnityEngine.iOS.DeviceGeneration.iPadPro1Gen:
                    case UnityEngine.iOS.DeviceGeneration.iPadPro10Inch1Gen:
                    case UnityEngine.iOS.DeviceGeneration.iPadUnknown:
                        return fDeviceType.IPad;
                    default:
                        return fDeviceType.IPhone;
                }
            #elif UNITY_ANDROID
                return fDeviceType.AndroidPhone;
            #elif UNITY_STANDALONE_WIN
                return IsUsingVR() ? fDeviceType.WindowsVR : fDeviceType.WindowsDesktop;
            #elif UNITY_STANDALONE_OSX
                return fDeviceType.OSXDesktop;
            #else
              throw new NotSupportedException("FPlatform.GetDeviceType: unknown device type");
            #endif
        }
        public static bool IsMobile() {
            return (GetDeviceType() & (fDeviceType.IPad | fDeviceType.IPhone | fDeviceType.AndroidPhone)) != 0 ;
        }



        static public bool IsUsingVR()
        {
            return UnityEngine.XR.XRSettings.enabled;
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



        static Thread _main_thread;
        static public void InitializeMainThreadID()
        {
            _main_thread = Thread.CurrentThread;

            // for some stupid reason Unity cannot return these paths in background threads, so we have to cache
            GameDataFolderPath();
            PersistentDataPath();
            TemporaryDataPath();
        }

        static public bool InMainThread()
        {
            if (_main_thread == null)
                throw new Exception("FPlatform.InMainThread: Must call InitializeMainThreadID() first!!");
            return Thread.CurrentThread == _main_thread;
        }





        public interface CoroutineExecutor
        {
            void StartAnonymousCoroutine(IEnumerator routine);
        }

        /// <summary>
        /// Global object you can use at any time to start a Coroutine. Generally set to
        /// active BaseSceneConfig
        /// </summary>
        static public CoroutineExecutor CoroutineExec {
            get { return coroutine_exec; }
            set { coroutine_exec = value; }
        }
        static CoroutineExecutor coroutine_exec;





        static public System.IntPtr GetWindowHandle()
        {
#if UNITY_STANDALONE_WIN
            return GetActiveWindow();
#else
            return System.IntPtr.Zero;
#endif
        }



        // background threads should kill themselves if this ever becomes true...
        static public bool ShutdownBackgroundThreadsOnQuit = false;


        static public void QuitApplication() {
            Cursor.lockState = CursorLockMode.None;
            ShutdownBackgroundThreadsOnQuit = true;
            Application.Quit();
        }


        static public void SuggestGarbageCollection()
        {
            // [RMS] collective wisdom is that the GC is smart enough that we should never do this, 
            //  except if we *know* many objects just became invalid. Like, when we clear the scene.
            //  Comment out the call to disable this behavior.
            GC.Collect();
        }


        static public bool ShowingExternalPopup = false;



        /// <summary>
        /// Show an open-file dialog and with the provided file types. 
        /// Returns path to selected file, or null if Cancel is clicked.
        /// Uses system file dialog, via tinyfiledialogs.
        /// filterPatterns specified like this: new string[] { "*.stl", "*.obj" }
        /// Note that tinyfiledialogs does not support multiple save-types in save dialog
        /// </summary>
        static public string GetOpenFileName(string sDialogTitle, string sInitialPathAndFile, 
                string[] filterPatterns, string sPatternDesc)
        {
            ShowingExternalPopup = true;

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
            // tinyfd changes CWD (?), and this makes Unity unhappy
            string curDirectory = Directory.GetCurrentDirectory();

            IntPtr p = tinyfd_openFileDialog(sDialogTitle, sInitialPathAndFile, 
                filterPatterns.Length, filterPatterns, sPatternDesc, 0);

            ShowingExternalPopup = false;
            try {
                Directory.SetCurrentDirectory(curDirectory);
            } catch (Exception) {
                // [RMS] sometimes this results in an exception? I am confused...
            }

            if (p == IntPtr.Zero)
                return null;

            string s = stringFromChar(p);
            return s;
#else
            // [TODO] implement
            return null;
#endif
        }




        /// <summary>
        /// Show a save-file dialog and with the provided file types. 
        /// Returns path to selected file, or null if Cancel is clicked.
        /// Uses system file dialog, via tinyfiledialogs.
        /// filterPatterns specified like this: new string[] { "*.stl", "*.obj" }
        /// Note that tinyfiledialogs does not support multiple save-types in save dialog
        /// </summary>
        static public string GetSaveFileName(string sDialogTitle, string sInitialPathAndFile, 
                string[] filterPatterns, string sPatternDesc)
        {
            ShowingExternalPopup = true;

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
            // tinyfd changes CWD (?), and this makes Unity unhappy
            string curDirectory = Directory.GetCurrentDirectory();

            IntPtr p = tinyfd_saveFileDialog(sDialogTitle, sInitialPathAndFile, 
                filterPatterns.Length, filterPatterns, sPatternDesc);

            ShowingExternalPopup = false;
            try {
                Directory.SetCurrentDirectory(curDirectory);
            } catch ( Exception ) {
                // [RMS] sometimes this results in an exception? I am confused...
            }

            if (p == IntPtr.Zero)
                return null;

            string s = stringFromChar(p);
            return s;
#else
            // [TODO] implement
            return null;
#endif
        }





        // platform-specific interop functions

#if UNITY_STANDALONE_WIN
        [DllImport("user32.dll")]
        private static extern System.IntPtr GetActiveWindow();
#endif
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)

        // NOTE: tinyfiledialogs is compiled with a flag that means it should output UTF8.
        // However, seems like it only works if I interpret result with Marshal.PtrToStringAnsi()... ??

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
