using System;
using System.Collections.Generic;
using g3;
using UnityEngine;

namespace f3
{
    public static class VRPlatform
    {
        // TODO:
        //   - make it possible to hide/show Vive controller geometry



        // By default the "direction" of the vive controller is up, like its a sword.
        // We generally want it to point forward, like the Rift controllers. This angle
        // seams reasonable but could be made configurable...
        public static float RiftControllerRotationAngle = 0.0f;
        public static float ViveControllerRotationAngle = 60.0f;


        public enum Device
        {
            NoVRDevice, OculusRift, HTCVive
        }

        public static Device CurrentVRDevice
        {
            //get { return VRDevice.NoVRDevice; }
            //get { return VRDevice.OculusRift; }
            get { return Device.HTCVive; }
        }



        static GameObject spatialCameraRig = null;
        public static void SetSpatialRig(GameObject root)
        {
            spatialCameraRig = root;
        }




        public static bool HaveActiveSpatialInput
        {
            get {
                if ( CurrentVRDevice == Device.OculusRift ) {
                    return OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch) ||
                            OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch);
                } else if ( CurrentVRDevice == Device.HTCVive ) {
                    return LeftViveControllerDevice.connected || RightViveControllerDevice.connected;
                } else {
                    return false;
                }
            }
        }



        // 0 = left, 1 = right
        public static bool IsSpatialDeviceTracked(int i)
        {
            if ( CurrentVRDevice == Device.OculusRift ) {
                OVRInput.Controller controller = (i == 0) ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                return OVRInput.GetControllerPositionTracked(controller);

            } else if ( CurrentVRDevice == Device.HTCVive ) {
                var device = (i == 0) ? LeftViveControllerDevice : RightViveControllerDevice;
                return device.hasTracking;

            } else {
                return false;
            }
        }



        public static Vector3f GetLocalControllerPosition(int i)
        {
            if ( CurrentVRDevice == Device.OculusRift ) {
                OVRInput.Controller controller = (i == 0) ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                return OVRInput.GetLocalControllerPosition(controller);

            } else if ( CurrentVRDevice == Device.HTCVive ) {
                GameObject controller = (i == 0) ? LeftViveControllerGO : RightViveControllerGO;
                if (controller == null)
                    return Vector3f.Zero;
                SteamVR_TrackedObject tracker = controller.GetComponent<SteamVR_TrackedObject>();
                return tracker.transform.localPosition;

            } else {
                return Vector3f.Zero;
            }
        }


        public static Quaternionf GetLocalControllerRotation(int i)
        {
            if ( CurrentVRDevice == Device.OculusRift ) {
                OVRInput.Controller controller = (i == 0) ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                Quaternionf rotation = OVRInput.GetLocalControllerRotation(controller);
                if ( RiftControllerRotationAngle != 0 )
                    rotation = Quaternionf.AxisAngleD(rotation.AxisX, RiftControllerRotationAngle) * rotation;
                return rotation;

            } else if ( CurrentVRDevice == Device.HTCVive ) {
                GameObject controller = (i == 0) ? LeftViveControllerGO : RightViveControllerGO;
                if (controller == null)
                    return Quaternionf.Identity;
                SteamVR_TrackedObject tracker = controller.GetComponent<SteamVR_TrackedObject>();
                Quaternionf rotation = tracker.transform.localRotation;
                if ( ViveControllerRotationAngle != 0 )
                    rotation = Quaternionf.AxisAngleD(rotation.AxisX, ViveControllerRotationAngle) * rotation;
                return rotation;

            } else {
                return Quaternionf.Identity;
            }
        }




        static InputTrigger leftTrigger;
        public static InputTrigger LeftTrigger
        {
            get {
                if ( leftTrigger == null ) {
                    if (CurrentVRDevice == Device.OculusRift) {
                        leftTrigger = new InputTrigger(() => {
                            return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
                        }, 0.9f, 0.1f);
                    } else if (CurrentVRDevice == Device.HTCVive) {
                        leftTrigger = new InputTrigger(() => {
                            Vector2f v = LeftViveControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
                            return v.x;
                        }, 0.8f, 0.1f);
                    } else {
                        leftTrigger = InputTrigger.NoTrigger;
                    }
                }
                return leftTrigger;
            }
        }



        static InputTrigger rightTrigger;
        public static InputTrigger RightTrigger
        {
            get {
                if ( rightTrigger == null ) {
                    if (CurrentVRDevice == Device.OculusRift) {
                        rightTrigger = new InputTrigger(() => {
                            return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
                        }, 0.9f, 0.1f);
                    } else if (CurrentVRDevice == Device.HTCVive) {
                        rightTrigger = new InputTrigger(() => {
                            Vector2f v = RightViveControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
                            return v.x;
                        }, 0.8f, 0.1f);
                    } else {
                        rightTrigger = InputTrigger.NoTrigger;
                    }
                }
                return rightTrigger;
            }
        }





        static InputTrigger leftSecondaryTrigger;
        public static InputTrigger LeftSecondaryTrigger
        {
            get {
                if ( leftSecondaryTrigger == null ) {
                    if (CurrentVRDevice == Device.OculusRift) {
                        leftSecondaryTrigger = new InputTrigger(() => {
                            return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);
                        }, 0.9f, 0.1f);
                    } else if (CurrentVRDevice == Device.HTCVive) {
                        leftSecondaryTrigger = new InputTrigger(() => {
                            bool bDown = LeftViveControllerDevice.GetPress(SteamVR_Controller.ButtonMask.Grip);
                            return (bDown) ? 1.0f : 0.0f;
                        }, 0.9f, 0.1f);
                    } else {
                        leftSecondaryTrigger = InputTrigger.NoTrigger;
                    }
                }
                return leftSecondaryTrigger;
            }
        }




        static InputTrigger rightSecondaryTrigger;
        public static InputTrigger RightSecondaryTrigger
        {
            get {
                if ( rightSecondaryTrigger == null ) {
                    if (CurrentVRDevice == Device.OculusRift) {
                        leftSecondaryTrigger = new InputTrigger(() => {
                            return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
                        }, 0.9f, 0.1f);
                    } else if (CurrentVRDevice == Device.HTCVive) {
                        rightSecondaryTrigger = new InputTrigger(() => {
                            bool bDown = RightViveControllerDevice.GetPress(SteamVR_Controller.ButtonMask.Grip);
                            return (bDown) ? 1.0f : 0.0f;
                        }, 0.9f, 0.1f);
                    } else {
                        rightSecondaryTrigger = InputTrigger.NoTrigger;
                    }
                }
                return rightSecondaryTrigger;
            }
        }




        // Vive-specific stuff. Unlike OVR, SteamVR does not provide very straightforward
        // code-level access to controllers. We need to find a MonoBehavior attached to
        // each controller, which is set up by the [CameraRig] prefab. 
        // 
        // Also, in SteamVR the left/right relationship of controllers will change if the
        // user switches hands. This messes up all sorts of stuff if we are looking it up
        // dynamically. So, cache it first time and stick with it.
        static GameObject leftViveController, rightViveController;
        static int iLeftViveDeviceIdx = -1, iRightViveDeviceIdx = -1;
        static void lookup_vive_controllers()
        {
            if (spatialCameraRig == null)
                return;
            if (leftViveController != null && rightViveController != null)
                return;

            iLeftViveDeviceIdx = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Leftmost);
            iRightViveDeviceIdx = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Rightmost);

            List<GameObject> children = new List<GameObject>(spatialCameraRig.Children());
            for (int i = 0; i < children.Count; ++i) {
                SteamVR_TrackedObject tracked = children[i].GetComponent<SteamVR_TrackedObject>();
                if (tracked == null)
                    continue;
                int idx = (int)tracked.index;
                if (idx == iLeftViveDeviceIdx)
                    leftViveController = children[i];
                else if (idx == iRightViveDeviceIdx)
                    rightViveController = children[i];
            }
        }

        static GameObject LeftViveControllerGO {
            get {
                lookup_vive_controllers();
                return leftViveController;
            }
        }
        static GameObject RightViveControllerGO {
            get {
                lookup_vive_controllers();
                return rightViveController;
            }
        }

        static SteamVR_Controller.Device LeftViveControllerDevice {
            get {
                lookup_vive_controllers();
                return SteamVR_Controller.Input(iLeftViveDeviceIdx);
            }
        }
        static SteamVR_Controller.Device RightViveControllerDevice {
            get {
                lookup_vive_controllers();
                return SteamVR_Controller.Input(iRightViveDeviceIdx);
            }
        }

    }
}
