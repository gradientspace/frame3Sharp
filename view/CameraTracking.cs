using UnityEngine;
using System.Collections;


namespace f3 {


    // define useful extension methods for unity Camera class
    public static class ExtCamera
    {
        // extension methods to Camera
        public static Vector3 GetTarget(this UnityEngine.Camera c)
        {
            CameraTarget t = c.gameObject.GetComponent<CameraTarget>();
            return t.TargetPoint;
        }
        public static void SetTarget(this UnityEngine.Camera c, Vector3 newTarget)
        {
            CameraTarget t = c.gameObject.GetComponent<CameraTarget>();
            t.TargetPoint = newTarget;
        }


        // we attach this below
        public static CameraManipulator Manipulator(this UnityEngine.Camera c)
        {
            return c.gameObject.GetComponent<CameraManipulator>();
        }
    }






    public class CameraTarget : MonoBehaviour
    {
        public FContext context;

        public Vector3 TargetPoint;

        public GameObject targetGO;
        public bool ShowTarget;

        Material visibleMaterial, hiddenMaterial;

        public void Start()
        {
            ShowTarget = false;

            targetGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visibleMaterial = MaterialUtil.CreateTransparentMaterial(Color.red, 0.8f);
            hiddenMaterial = MaterialUtil.CreateTransparentMaterial(Color.red, 0.4f);
            targetGO.GetComponent<MeshRenderer>().material = hiddenMaterial;
            targetGO.SetLayer(FPlatform.WidgetOverlayLayer);
            MaterialUtil.DisableShadows(targetGO);
            targetGO.SetName("camera_target");
        }

        public void Update()
        {
            targetGO.transform.position = TargetPoint;
            float fScaling = VRUtil.GetVRRadiusForVisualAngle(TargetPoint, gameObject.transform.position, 1.0f);
            targetGO.transform.localScale = new Vector3(fScaling, fScaling, fScaling);

            if ( ShowTarget ) {
                Material setMaterial = hiddenMaterial;

                // raycast into scene and if we hit ball before we hit anything else, render
                // it darker red, to give some sense of inside/outside
                if (this.context != null) {
                    Vector3 camPos = this.context.ActiveCamera.GetPosition();
                    float fDistSqr = (TargetPoint - camPos).sqrMagnitude;
                    Ray ray_t = new Ray(camPos, (TargetPoint - camPos).normalized);
                    AnyRayHit hit;
                    if (this.context.Scene.FindSceneRayIntersection(ray_t, out hit) == false)
                        setMaterial = visibleMaterial;
                    else if ( hit.fHitDist*hit.fHitDist*1.005f > fDistSqr )
                        setMaterial = visibleMaterial;
                }

                MaterialUtil.SetMaterial(targetGO, setMaterial);
                UnityUtil.Show(targetGO);
            } else {
                UnityUtil.Hide(targetGO);
            }

        }

    }






    public class CameraTracking {

		public CameraTracking() {
		}

		Camera mainCamera;
		Camera widgetCamera;
        Camera hudCamera;
        Camera uiCamera;
        Camera cursorCamera;
        FContext controller;


        public Camera MainCamera {
            get { return mainCamera;  }
        }
        public Camera OrthoUICamera {
            get { return uiCamera; }
        }


		// Use this for initialization
		public void Initialize (FContext controller) {
            this.controller = controller;

			// find main camera
			var mainCameraObj = GameObject.FindWithTag ("MainCamera");
            if ( mainCameraObj == null ) {
                throw new MissingComponentException("[CameraTracking.Initialize] could not find camera with tag MainCamera");
            }
			mainCamera = mainCameraObj.GetComponent<Camera> () as Camera;

            // create camera for 3D widgets layer
            widgetCamera = Camera.Instantiate (mainCamera);
			widgetCamera.SetName("WidgetCamera");
            widgetCamera.transform.position = mainCamera.transform.position;
            widgetCamera.transform.rotation = mainCamera.transform.rotation;

            // create camera for HUD layer
            hudCamera = Camera.Instantiate(mainCamera);
            hudCamera.SetName("HUDCamera");
            hudCamera.transform.position = mainCamera.transform.position;
            hudCamera.transform.rotation = mainCamera.transform.rotation;

            // create camera for UI
            uiCamera = Camera.Instantiate(mainCamera);
            uiCamera.SetName("UICamera");
            uiCamera.transform.position = mainCamera.transform.position;
            uiCamera.transform.rotation = mainCamera.transform.rotation;
            uiCamera.orthographic = true;
            uiCamera.orthographicSize = 0.5f;

            // create camera for cursor
            cursorCamera = Camera.Instantiate(mainCamera);
            cursorCamera.SetName("CursorCamera");
            cursorCamera.transform.position = mainCamera.transform.position;
            cursorCamera.transform.rotation = mainCamera.transform.rotation;

            // configure these cameras
            //   - must disable audio listener
            //   - do depth clear so we can draw on top of other layers
            (widgetCamera.GetComponent<AudioListener> () as AudioListener).enabled = false;
			widgetCamera.clearFlags = CameraClearFlags.Depth;
            (hudCamera.GetComponent<AudioListener>() as AudioListener).enabled = false;
            hudCamera.clearFlags = CameraClearFlags.Depth;
            (uiCamera.GetComponent<AudioListener>() as AudioListener).enabled = false;
            uiCamera.clearFlags = CameraClearFlags.Depth;
            (cursorCamera.GetComponent<AudioListener>() as AudioListener).enabled = false;
            cursorCamera.clearFlags = CameraClearFlags.Depth;


            // set up camera masks

            // this camera only renders 3DWidgetOverlay layer, and mainCam does not!
            int nWidgetLayer = FPlatform.WidgetOverlayLayer;
            int nHUDLayer = FPlatform.HUDLayer;
            int nUILayer = FPlatform.UILayer;
            int nCursorLayer = FPlatform.CursorLayer;

            widgetCamera.cullingMask = (1 << nWidgetLayer);
            hudCamera.cullingMask = (1 << nHUDLayer);
            uiCamera.cullingMask = (1 << nUILayer);
            cursorCamera.cullingMask = (1 << nCursorLayer);

            mainCamera.cullingMask &= ~(1 << nWidgetLayer);
            mainCamera.cullingMask &= ~(1 << nHUDLayer);
            mainCamera.cullingMask &= ~(1 << nUILayer);
            mainCamera.cullingMask &= ~(1 << nCursorLayer);

            // attach camera animation object to main camera
            CameraAnimator anim = mainCamera.gameObject.AddComponent<CameraAnimator>();
            anim.UseCamera = mainCamera;
            anim.UseScene = this.controller.Scene;

            // add target point to camera
            CameraTarget target = mainCamera.gameObject.AddComponent<CameraTarget>();
            target.TargetPoint = new Vector3(
                0.0f, mainCamera.transform.position[1], 0.0f);
            target.context = this.controller;

            // add camera manipulator to camera
            mainCamera.gameObject.AddComponent<CameraManipulator>();
        }
		

	}

}