using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{
    /// <summary>
    /// Unity by default tries to render at as high a framerate as possible (game engines!).
    /// This script watches for mouse and keyboard events, and if there are not any for
    /// a while, it incrementally decreases the framerate to reduce CPU load
    /// 
    /// [TODO] apparently could disable the camera to disable rendering, w/o having to
    /// reduce input latency so much (is definitely noticeable even at 15fps). 
    /// However when I do this, the other cameras do not come back (possibly because Camera.main
    /// is not defined until this script runs?)
    /// </summary>
    public class UnityIdler : MonoBehaviour
    {
        public double InitialIdleDelay = 15;
        public double InitialIdleFramerate = 15;
        public double DeepIdleDelay = 30;
        public double DeepIdleFramerate = 5; 


        Vector3 last_mouse_pos;
        double last_move_time = 0;


        enum IdleStages
        {
            NoIdle, InitialIdle, DeepIdle
        }
        IdleStages IdleStage = IdleStages.NoIdle;


        int start_framerate = 0;
        int start_vsync = 0;
        bool initialized = false;

        // [RMS] disabling main camera allows Update loop to continue, but
        //  it doesn't come back cleanly...
        //Camera mainCamera;

        public void Update()
        {
            if ( initialized == false ) {
                start_framerate = Application.targetFrameRate;
                start_vsync = QualitySettings.vSyncCount;
                initialized = true;
            }

            Vector3 new_mouse_pos = Input.mousePosition;

            // cancel idle if mouse moved
            if ( (new_mouse_pos-last_mouse_pos).magnitude > 0.0001 || Input.anyKeyDown ) {
                last_mouse_pos = new_mouse_pos;
                last_move_time = Time.realtimeSinceStartup;
                if (IdleStage != IdleStages.NoIdle) {
                    to_active_framerate();
                    IdleStage = IdleStages.NoIdle;
                }
                return;
            }

            double time_delta = Time.realtimeSinceStartup - last_move_time;

            // fall into successively deeper idle states
            if (time_delta > InitialIdleDelay && IdleStage == IdleStages.NoIdle) { 
                Application.targetFrameRate = 15;
                QualitySettings.vSyncCount = 0;
                //mainCamera = Camera.main;
                //mainCamera.enabled = false;
                IdleStage = IdleStages.InitialIdle;

            } 
            if (time_delta > DeepIdleDelay && IdleStage == IdleStages.InitialIdle) {
                Application.targetFrameRate = 5;
                QualitySettings.vSyncCount = 0;
                //mainCamera = Camera.main;
                //mainCamera.enabled = false;
                IdleStage = IdleStages.DeepIdle;
            }
        }



        public void OnApplicationFocus(bool focus)
        {
            if (IdleStage != IdleStages.NoIdle) {
                to_active_framerate();
                IdleStage = IdleStages.NoIdle;
                last_move_time = Time.realtimeSinceStartup;
            }
        }



        void to_active_framerate()
        {
            Application.targetFrameRate = start_framerate;
            QualitySettings.vSyncCount = start_vsync;
            //mainCamera.enabled = true;
        }

    }
}
