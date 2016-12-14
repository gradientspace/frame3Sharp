using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{

    public class InputTrigger
    {
        float fPrevValue = 0, fCurValue = 0;
        Func<float> valueF;
        float fValueUpThresh, fValueDownThresh;

        bool in_pressed;
        bool pressed_this_frame;
        bool released_this_frame;

        public InputTrigger(Func<float> valueFunction, float fDownThresh = 0.98f, float fUpThresh = 0.1f) {
            valueF = valueFunction;
            fValueDownThresh = fDownThresh;
            fValueUpThresh = fUpThresh;
            fPrevValue = fCurValue = 0;
            in_pressed = false;
        }

        public void Update() {
            fPrevValue = fCurValue;
            fCurValue = valueF();

            pressed_this_frame = released_this_frame = false;
            if ( in_pressed == false && fPrevValue < fValueDownThresh && fCurValue >= fValueDownThresh ) {
                in_pressed = true;
                pressed_this_frame = true;
            } else if ( in_pressed == true && fPrevValue > fValueUpThresh && fCurValue <= fValueUpThresh ) {
                in_pressed = false;
                released_this_frame = true;
            } 

        }

        public bool Pressed
            { get { return pressed_this_frame; } }
        public bool Down
            { get { return in_pressed; } }
        public bool Released
            { get { return released_this_frame; } }
    }




    public class InputExtension
    {
        static InputExtension Singleton = null;
        public static InputExtension Get {
            get { if (Singleton == null) Singleton = new InputExtension(); return Singleton; }
        }

        public InputTrigger GamepadLeft;
        public InputTrigger GamepadRight;

        public InputTrigger OculusLeftTrigger;
        public InputTrigger OculusRightTrigger;

        public InputTrigger OculusLeftShoulder;
        public InputTrigger OculusRightShoulder;


        public void Start()
        {
            GamepadLeft = new InputTrigger(() => { 
                return Input.GetAxis("LeftTrigger");
            });
            GamepadRight = new InputTrigger(() => {
                return Input.GetAxis("RightTrigger");
            });
            OculusLeftTrigger = new InputTrigger(() => {
                return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
            }, 0.9f, 0.1f);
            OculusRightTrigger = new InputTrigger(() => {
                return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
            }, 0.9f, 0.1f);
            OculusLeftShoulder = new InputTrigger(() => {
                return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);
            }, 0.9f, 0.1f);
            OculusRightShoulder = new InputTrigger(() => {
                return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
            }, 0.9f, 0.1f);
        }

        // call this from a MonoBehavior Update before you use the Input extension methods
        public void Update()
        {
            GamepadLeft.Update();
            GamepadRight.Update();
            OculusLeftTrigger.Update();
            OculusRightTrigger.Update();
            OculusLeftShoulder.Update();
            OculusRightShoulder.Update();
        }


    }



}
