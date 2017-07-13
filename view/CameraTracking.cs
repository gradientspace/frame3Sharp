using System;
using System.Collections.Generic;
using UnityEngine;
using g3;


namespace f3 {


    // define useful extension methods for unity Camera class
    public static class ExtCamera
    {
        // extension methods to Camera
        public static Vector3f GetTarget(this UnityEngine.Camera c)
        {
            CameraTarget t = c.gameObject.GetComponent<CameraTarget>();
            return t.TargetPoint;
        }
        public static void SetTarget(this UnityEngine.Camera c, Vector3f newTarget)
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

        public Vector3f TargetPoint;

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
            targetGO.transform.localScale = fScaling * Vector3f.One; 

            if ( ShowTarget && SceneGraphConfig.EnableVisibleCameraPivot ) {
                Material setMaterial = hiddenMaterial;

                // raycast into scene and if we hit ball before we hit anything else, render
                // it darker red, to give some sense of inside/outside
                if (this.context != null) {
                    Vector3f camPos = this.context.ActiveCamera.GetPosition();
                    float fDistSqr = TargetPoint.DistanceSquared(camPos);
                    Ray3f ray_t = new Ray3f(camPos, (TargetPoint - camPos).Normalized);
                    AnyRayHit hit;
                    if (this.context.Scene.FindSceneRayIntersection(ray_t, out hit) == false)
                        setMaterial = visibleMaterial;
                    else if ( hit.fHitDist*hit.fHitDist*1.005f > fDistSqr )
                        setMaterial = visibleMaterial;
                }

                MaterialUtil.SetMaterial(targetGO, setMaterial);
                targetGO.Show();
            } else {
                targetGO.Hide();
            }

        }

    }






    public class CameraTracking {

		public CameraTracking() {
		}

		fCamera mainCamera;
		fCamera widgetCamera;
        fCamera hudCamera;
        fCamera uiCamera;
        fCamera cursorCamera;
        FContext controller;


        public fCamera MainCamera {
            get { return mainCamera;  }
        }
        public fCamera OrthoUICamera {
            get { return uiCamera; }
        }


		// Use this for initialization
		public void Initialize (FContext controller) {
            this.controller = controller;

            // find main camera
            GameObject[] mainCameras = GameObject.FindGameObjectsWithTag("MainCamera");
            if ( mainCameras.Length == 0 ) {
                throw new MissingComponentException("CameraTracking.Initialize: could not find camera with tag MainCamera");
            }
            var mainCameraObj = mainCameras[0];
            if ( mainCameras.Length > 1 ) {
                DebugUtil.Log(2, "CameraTracking.Initialize: there are multiple objects with tag MainCamera. Using the one named " + mainCameraObj.GetName());
            }
			mainCamera = new fCamera(mainCameraObj.GetComponent<Camera> () as Camera);

            // on Vive the MainCamera will have some child cameras that are a problem, 
            // so get rid of them
            if ( gs.VRPlatform.CurrentVRDevice == gs.VRPlatform.Device.HTCVive ) {
                List<GameObject> children = new List<GameObject>(mainCameraObj.Children());
                foreach (var child in children) {
                    mainCameraObj.RemoveChild(child);
                    GameObject.Destroy(child);
                }
            }

            List<Camera> newCameras = new List<Camera>();

            Vector3f mainPos = mainCamera.GetPosition();
            Quaternionf mainRot = mainCamera.GetRotation();

            // create camera for 3D widgets layer
            widgetCamera = new fCamera( Camera.Instantiate((Camera)mainCamera, mainPos, mainRot) );
			widgetCamera.SetName("WidgetCamera");
            newCameras.Add(widgetCamera);

            // create camera for HUD layer
            hudCamera = new fCamera( Camera.Instantiate((Camera)mainCamera, mainPos, mainRot));
            hudCamera.SetName("HUDCamera");
            newCameras.Add(hudCamera);

            // create camera for UI
            uiCamera = new fCamera( Camera.Instantiate((Camera)mainCamera, mainPos, mainRot));
            uiCamera.SetName("UICamera");
            ((Camera)uiCamera).orthographic = true;
            ((Camera)uiCamera).orthographicSize = 0.5f;
            newCameras.Add(uiCamera);

            // create camera for cursor
            cursorCamera = new fCamera( Camera.Instantiate((Camera)mainCamera, mainPos, mainRot));
            cursorCamera.SetName("CursorCamera");
            newCameras.Add(cursorCamera);

            // configure these cameras
            //   - must disable audio listener if it exists
            //   - do depth clear so we can draw on top of other layers
            foreach ( Camera cam in newCameras ) {
                AudioListener listener = cam.GetComponent<AudioListener>();
                if (listener != null)
                    listener.enabled = false;

                cam.clearFlags = CameraClearFlags.Depth;

                cam.tag = "Untagged";
            }


            // set up camera masks

            // this camera only renders 3DWidgetOverlay layer, and mainCam does not!
            int nWidgetLayer = FPlatform.WidgetOverlayLayer;
            int nHUDLayer = FPlatform.HUDLayer;
            int nUILayer = FPlatform.UILayer;
            int nCursorLayer = FPlatform.CursorLayer;

            ((Camera)widgetCamera).cullingMask = (1 << nWidgetLayer);
            ((Camera)hudCamera).cullingMask = (1 << nHUDLayer);
            ((Camera)uiCamera).cullingMask = (1 << nUILayer);
            ((Camera)cursorCamera).cullingMask = (1 << nCursorLayer);

            ((Camera)mainCamera).cullingMask &= ~(1 << nWidgetLayer);
            ((Camera)mainCamera).cullingMask &= ~(1 << nHUDLayer);
            ((Camera)mainCamera).cullingMask &= ~(1 << nUILayer);
            ((Camera)mainCamera).cullingMask &= ~(1 << nCursorLayer);

            // attach camera animation object to main camera
            CameraAnimator anim = mainCamera.AddComponent<CameraAnimator>();
            anim.UseCamera = mainCamera;
            anim.UseScene = this.controller.Scene;

            // add target point to camera
            CameraTarget target = mainCamera.AddComponent<CameraTarget>();
            target.TargetPoint = new Vector3f(0.0f, mainCamera.GetPosition().y, 0.0f);
            target.context = this.controller;

            // add camera manipulator to camera
            // TODO: this does not need to be a monobehavior...
            var manipulator = mainCamera.AddComponent<CameraManipulator>();
            manipulator.Camera = mainCamera;


            // initialize FPlatform
            FPlatform.MainCamera = mainCamera;
            FPlatform.WidgetCamera = widgetCamera;
            FPlatform.HUDCamera = hudCamera;
            FPlatform.OrthoUICamera = uiCamera;
            FPlatform.CursorCamera = cursorCamera;
        }
		

	}

}