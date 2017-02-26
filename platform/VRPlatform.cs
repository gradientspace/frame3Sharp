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
                    int iLeft = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Leftmost);
                    int iRight = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Rightmost);
                    return SteamVR_Controller.Input(iLeft).connected || SteamVR_Controller.Input(iRight).connected;
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
                int idx = SteamVR_Controller.GetDeviceIndex( (i == 0) ? 
                    SteamVR_Controller.DeviceRelation.Leftmost :
                    SteamVR_Controller.DeviceRelation.Rightmost );
                return SteamVR_Controller.Input(idx).hasTracking;

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
                GameObject controller = (i == 0) ? LeftViveController : RightViveController;
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
                GameObject controller = (i == 0) ? LeftViveController : RightViveController;
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








        // Vive-specific stuff. Unlike OVR, SteamVR does not provide very straightforward
        // code-level access to controllers. We need to find a MonoBehavior attached to
        // each controller, which is set up by the [CameraRig] prefab. 
        static GameObject leftViveController, rightViveController;
        static void lookup_vive_controllers()
        {
            int iLeft = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Leftmost);
            int iRight = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Rightmost);

            List<GameObject> children = new List<GameObject>(spatialCameraRig.Children());
            for (int i = 0; i < children.Count; ++i) {
                SteamVR_TrackedObject tracked = children[i].GetComponent<SteamVR_TrackedObject>();
                if (tracked == null)
                    continue;
                int idx = (int)tracked.index;
                if (idx == iLeft)
                    leftViveController = children[i];
                else if (idx == iRight)
                    rightViveController = children[i];
            }
        }

        static GameObject LeftViveController
        {
            get {
                if ( leftViveController == null && spatialCameraRig != null )
                    lookup_vive_controllers();
                return leftViveController;
            }
        }
        static GameObject RightViveController
        {
            get {
                if ( rightViveController == null && spatialCameraRig != null )
                    lookup_vive_controllers();
                return rightViveController;
            }
        }


    }
}
