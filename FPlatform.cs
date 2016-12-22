using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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


    }



}
