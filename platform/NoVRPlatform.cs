#if F3_NO_VR_SUPPORT

using System;
using UnityEngine;

namespace gs {

// This has the same API as gsUnityVR.VRPlatform, but just returns dummy values
public static class VRPlatform {

	// this is really more about the controllers...
	public enum Device
	{
		NoVRDevice, GenericVRDevice, OculusRift, HTCVive
	}
	public static Device CurrentVRDevice {
		get { return Device.NoVRDevice; }
	}
	public static bool VREnabled {
		get { return false; }
        set { }  // ignore!
	}
	public static bool HaveActiveSpatialInput {
		get { return false; }
	}

 	public static bool Initialize(GameObject SpatialCameraRig)
	{	
		throw new Exception("Called VRPlatform.Initialize with no VR Support");
	}

        public static bool IsLeftControllerTracked {
            get { return false;  }
        }
        public static bool IsRightControllerTracked {
            get { return false;  }
        }
        public static bool IsSpatialDeviceTracked(int i) {
			return false;
		}

        public static Vector3 LeftControllerPosition {
            get { return Vector3.zero; }
        }
        public static Vector3 RightControllerPosition {
            get { return Vector3.zero; }
        }
        public static Vector3 GetLocalControllerPosition(int i)
        {
			return Vector3.zero;
		}

        public static Quaternion LeftControllerRotation {
            get { return Quaternion.identity; }
        }
        public static Quaternion RightControllerRotation {
            get { return Quaternion.identity; }
        }
        public static Quaternion GetLocalControllerRotation(int i)
        {
			return Quaternion.identity;
		}

		public static float LeftTrigger {
			get { return 0.0f; }
		}
		public static float RightTrigger {
			get { return 0.0f; }
		}

		public static float LeftSecondaryTrigger {
			get { return 0.0f; }
		}
		public static float RightSecondaryTrigger {
			get { return 0.0f; }
		}

		public static bool LeftGripButton {
			get { return false; }
		}
		public static bool RightGripButton {
			get { return false; }
		}

		public static Vector2 LeftStickPosition
        {
			get { return Vector2.zero; }
		}
		public static Vector2 RightStickPosition
        {
			get { return Vector2.zero; }
		}		


		public static bool LeftStickTouching
        {
			get { return false; }
		}
		public static bool LeftStickPressed
        {
			get { return false; }
		}
		public static bool LeftStickDown
        {
			get { return false; }
		}
		public static bool LeftStickReleased
        {
			get { return false; }
		}		

		
		public static bool RightStickTouching
        {
			get { return false; }
		}
		public static bool RightStickPressed
        {
			get { return false; }
		}
		public static bool RightStickDown
        {
			get { return false; }
		}
		public static bool RightStickReleased
        {
			get { return false; }
		}	



		public static bool LeftMenuButtonPressed
        {
			get { return false; }
		}
		public static bool LeftMenuButtonDown
        {
			get { return false; }
		}
		public static bool LeftMenuButtonReleased
        {
			get { return false; }
		}	

		public static bool RightMenuButtonPressed
        {
			get { return false; }
		}
		public static bool RightMenuButtonDown
        {
			get { return false; }
		}
		public static bool RightMenuButtonReleased
        {
			get { return false; }
		}	



		public static bool AButtonPressed
        {
			get { return false; }
		}
		public static bool AButtonDown
        {
			get { return false; }
		}
		public static bool AButtonReleased
        {
			get { return false; }
		}	


		public static bool BButtonPressed
        {
			get { return false; }
		}
		public static bool BButtonDown
        {
			get { return false; }
		}
		public static bool BButtonReleased
        {
			get { return false; }
		}	


		public static bool XButtonPressed
        {
			get { return false; }
		}
		public static bool XButtonDown
        {
			get { return false; }
		}
		public static bool XButtonReleased
        {
			get { return false; }
		}	


		public static bool YButtonPressed
        {
			get { return false; }
		}
		public static bool YButtonDown
        {
			get { return false; }
		}
		public static bool YButtonReleased
        {
			get { return false; }
		}					


		public static GameObject AutoConfigureVR()
		{
			throw new Exception("Called VRPlatform.AutoConfigureVR with no VR support");
		}

}


}

#endif