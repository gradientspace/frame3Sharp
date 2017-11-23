using UnityEngine;
using System;
using System.Collections;
using g3;


namespace f3 {

	public class VRMouseCursorController : ICursorController
    {

		Camera camera;
		FContext context;

		public GameObject Cursor { get; set; }
		public float CursorVisualAngleInDegrees { get; set; }
		public Material CursorDefaultMaterial { get; set; }
		public Material CursorHitMaterial { get; set; }
		public Material CursorCapturingMaterial { get; set; }

		public Vector3 CurrentCursorPosWorld;
		public Vector3 CurrentCursorRaySourceWorld;

        public Ray3f CurrentCursorWorldRay()
        {
			Vector3f camPos = CurrentCursorRaySourceWorld;
			Vector3f cursorPos = CurrentCursorPosWorld;
			Ray3f ray = new Ray3f(camPos, (cursorPos - camPos).Normalized);
            if (Math.Abs(ray.Direction.LengthSquared - 1) > 0.001f)
                ray = new Ray3f(camPos, Vector3f.AxisY);
			return ray;
        }

        public bool HasSecondPosition { get { return false; } }
        public Ray3f SecondWorldRay() {
            throw new NotImplementedException("VRMouseCursorController.SecondWorldRay: not supported!");
        }


        public Ray3f CurrentCursorOrthoRay()
        {
            throw new NotImplementedException("VRMouseCursorController.CurrentCursorUIRay: UI layer not supported in VR!");
        }


		GameObject xformObject;			// [RMS] this is an internal GO we use basically just for a transform
										//  Actually a plane that stays in front of eye.

		Vector3 vCursorPlaneOrigin;
		Vector3 vCursorPlaneRight;
		Vector3 vCursorPlaneUp;
		Vector3 vRaySourcePosition;
        float fCursorSpeedNormalization;
        bool bWasInCaptureFreeze;

		float fCurPlaneX;
		float fCurPlaneY;
		Vector3 vPlaneCursorPos;
		Vector3 vSceneCursorPos;

        Mesh standardCursorMesh;
        Mesh activeToolCursorMesh;


        float lastMouseEventTime;
        bool mouseInactiveState;
        public bool MouseInactive { get { return mouseInactiveState; } }

        // to use this, see RequestFreezeCursor() 
        bool bFreezeCursor = false;


        public VRMouseCursorController(Camera viewCam, FContext context) {
			camera = viewCam;
			this.context = context;
		}

		// Use this for initialization
		public void Start () {
            fCursorSpeedNormalization = 1.0f;
            fCurPlaneX = 0;
			fCurPlaneY = 0;
			vPlaneCursorPos = Vector3.zero;
			vSceneCursorPos = vPlaneCursorPos;

            CursorDefaultMaterial = MaterialUtil.CreateTransparentMaterial (Color.grey, 0.6f);
			//CursorHitMaterial = MaterialUtil.CreateTransparentMaterial (Color.yellow, 0.8f);
            CursorHitMaterial = MaterialUtil.CreateStandardMaterial(Color.yellow);
            CursorCapturingMaterial = MaterialUtil.CreateTransparentMaterial (Color.yellow, 0.75f);

			CursorVisualAngleInDegrees = 1.5f;

            standardCursorMesh = MeshGenerators.Create3DArrow(1.0f, 1.0f, 1.0f, 0.5f, 16);
            UnityUtil.TranslateMesh(standardCursorMesh, 0, -2.0f, 0);
            activeToolCursorMesh = MeshGenerators.Create3DArrow(1.0f, 1.0f, 1.0f, 1.0f, 16);
            UnityUtil.TranslateMesh(activeToolCursorMesh, 0, -2.0f, 0);

            Cursor = UnityUtil.CreateMeshGO("cursor", standardCursorMesh, CursorDefaultMaterial);
            Cursor.SetSharedMesh(standardCursorMesh);
            Cursor.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            Cursor.transform.localRotation = Quaternion.AngleAxis(45.0f, new Vector3(1, 0, 1).normalized);
            MaterialUtil.DisableShadows(Cursor);

            xformObject = GameObject.CreatePrimitive (PrimitiveType.Plane);
			xformObject.SetName("cursor_plane");
            MaterialUtil.DisableShadows(xformObject);
            xformObject.GetComponent<MeshRenderer>().material 
                = MaterialUtil.CreateTransparentMaterial (Color.cyan, 0.2f);
            xformObject.GetComponent<MeshRenderer>().enabled = false;

            lastMouseEventTime = FPlatform.RealTime();
            mouseInactiveState = false;
        }

		// FixedUpdate is called before any Update
		public void Update () {

            if (bFreezeCursor)
                return;

            // if we are in capture we freeze the cursor plane
            if (context.InCaptureMouse == false) {
                Vector3 camPos = camera.gameObject.transform.position;
				Vector3 forward = camera.gameObject.transform.forward;

                // orient Y-up plane so that it is in front of eye, perp to camera direction
                float fCursorDepth = 10.0f;
                fCursorSpeedNormalization = 1.0f;
                if (context.ActiveCockpit != null && context.ActiveCockpit.DefaultCursorDepth > 0) {
                    fCursorDepth = context.ActiveCockpit.DefaultCursorDepth;
                    // cursor speed will change depending on cursor plane distance, unless we normalize
                    fCursorSpeedNormalization *= (fCursorDepth / 10.0f);
                }
                xformObject.transform.position = camPos + fCursorDepth * forward;	
				xformObject.transform.LookAt (camera.gameObject.transform);
				xformObject.transform.RotateAround (xformObject.transform.position, xformObject.transform.right, 90);

				// that plane is the plane the mouse cursor moves on
				this.vCursorPlaneOrigin = xformObject.transform.position;
				this.vCursorPlaneRight = xformObject.transform.right;
				this.vCursorPlaneUp = xformObject.transform.forward;        // because we rotated? weird...
				this.vRaySourcePosition = camera.transform.position;

                // if we were capturing, then plane was frozen and when we stop capturing, if
                //  head moved, the cursor will pop to a new position (because it is stored in
                //  local plane coords). So raycast through old cursor to hit new plane and figure
                //  out new local coords (fCurPlaneX, fCurPlaneY)
                if (bWasInCaptureFreeze) {
                    Frame3f newF = new Frame3f(vCursorPlaneOrigin, this.camera.transform.forward);
                    Vector3 vPlaneHit = newF.RayPlaneIntersection(this.vRaySourcePosition,
                        (vPlaneCursorPos - vRaySourcePosition).normalized, 2);
                    fCurPlaneX = Vector3.Dot((vPlaneHit - vCursorPlaneOrigin), vCursorPlaneRight);
                    fCurPlaneY = Vector3.Dot((vPlaneHit - vCursorPlaneOrigin), vCursorPlaneUp);
                    bWasInCaptureFreeze = false;
                }

			} else {
                bWasInCaptureFreeze = true;
            }

            Vector2f mousePos = InputExtension.Get.Mouse.PositionDelta;
            Vector2f leftStick = InputExtension.Get.GamepadLeftStick.Position;
            float fX = mousePos.x + leftStick.x;
            float fY = mousePos.y + leftStick.y;

            // auto-hide cursor if it doesn't move for a while
            if (fX == 0 && fY == 0 && SceneGraphConfig.MouseCursorHideTimeout > 0) {
                if ((FPlatform.RealTime() - lastMouseEventTime) > SceneGraphConfig.MouseCursorHideTimeout) {
                    Cursor.SetVisible(false);
                    mouseInactiveState = true;
                }
                if (mouseInactiveState)
                    return;
            } else {
                lastMouseEventTime = FPlatform.RealTime();
                if (mouseInactiveState)
                    Cursor.SetVisible(true);
                mouseInactiveState = false;
            }

            // update cursor location
            fCurPlaneX -= 0.3f * fX * fCursorSpeedNormalization;
            fCurPlaneY -= 0.3f * fY * fCursorSpeedNormalization;
			vPlaneCursorPos = 
				vCursorPlaneOrigin + fCurPlaneX * vCursorPlaneRight + fCurPlaneY * vCursorPlaneUp;
			vSceneCursorPos = vPlaneCursorPos;

            // if cursor gets outside of viewpoint it is almost impossible to get it back.
            // So, if it goes too far out of view (45 deg here), we snap it back to the origin
            if (context.InCameraManipulation == false && context.InCaptureMouse == false) {
                float fAngle = Vector3.Angle((vPlaneCursorPos - camera.transform.position).normalized, camera.transform.forward);
                if (fAngle > 50.0f) {
                    fCurPlaneX = fCurPlaneY = 0;
                    vPlaneCursorPos =
                        vCursorPlaneOrigin + fCurPlaneX * vCursorPlaneRight + fCurPlaneY * vCursorPlaneUp;
                    vSceneCursorPos = vPlaneCursorPos;
                }
            }


            bool bHit = false;
            // [RMS] boundsHit cursor orientation could be useful for things where you are picking a point
            //   on the ground plane (eg like drawing contours). Not sure how to toggle that though.
            //   Just disabling for now...
            //bool bIsBoundsHit = false;
			if (context != null) {
				Ray r = new Ray (camera.transform.position, (vPlaneCursorPos - camera.transform.position).normalized);
				AnyRayHit hit = null;
                if (context.FindAnyRayIntersection(r, out hit)) {
                    vSceneCursorPos = hit.hitPos;
                    bHit = true;
                } else {
                    GameObjectRayHit ghit = null;
                    if (context.GetScene().FindWorldBoundsHit(r, out ghit)) {
                        vSceneCursorPos = ghit.hitPos;
                        //bIsBoundsHit = true;
                    }
                }
			}

			this.CurrentCursorPosWorld = vPlaneCursorPos;
			this.CurrentCursorRaySourceWorld = this.vRaySourcePosition;

            Vector3 vEyeToPos = (vPlaneCursorPos - camera.transform.position).normalized;
            //if (bIsBoundsHit) {
            //    Vector3 rotAxis = (vEyeToPos + camera.transform.right).normalized;
            //    Cursor.transform.localRotation = Quaternion.AngleAxis(180.0f-45.0f, rotAxis);
            //} else {
                Quaternion rotAlignUp = Quaternion.FromToRotation(Vector3.up, camera.transform.up);
                Vector3 rotAxis = (vEyeToPos + camera.transform.right).normalized;
                Cursor.transform.localRotation = Quaternion.AngleAxis(45.0f, rotAxis) * rotAlignUp;
            //}

            Cursor.transform.position = vSceneCursorPos;
			if (context.InCaptureMouse)
				Cursor.GetComponent<MeshRenderer> ().material = CursorCapturingMaterial;
			else if (bHit)
				Cursor.GetComponent<MeshRenderer> ().material = CursorHitMaterial;
			else
				Cursor.GetComponent<MeshRenderer> ().material = CursorDefaultMaterial;

            Cursor.SetLayer(FPlatform.CursorLayer);

            // maintain a consistent visual size for 3D cursor sphere
            float fScaling = VRUtil.GetVRRadiusForVisualAngle(vSceneCursorPos, camera.transform.position, CursorVisualAngleInDegrees);
			Cursor.transform.localScale = new Vector3 (fScaling, fScaling, fScaling);

            // update cursor
            Mesh useMesh = context.ToolManager.HasActiveTool(ToolSide.Right) ? activeToolCursorMesh : standardCursorMesh;
            if ( Cursor.GetSharedMesh() != useMesh ) {
                Cursor.SetSharedMesh(useMesh);
            }

        }


        public void ResetCursorToCenter()
        {
            fCurPlaneX = 0;
            fCurPlaneY = 0;
        }


        public void HideCursor() {
            if (mouseInactiveState == false) {
                Cursor.SetVisible(false);
                mouseInactiveState = true;
            }
        }
        public void ShowCursor()
        {
            Cursor.Show();
            mouseInactiveState = false;
            lastMouseEventTime = FPlatform.RealTime();
        }



        public class AutoUnfreezer {
            Action unfreeze;
            public AutoUnfreezer(Action unfreezer) { this.unfreeze = unfreezer; }
            public void Unfreeze() { unfreeze(); unfreeze = null; }
            ~AutoUnfreezer() { if ( unfreeze != null) unfreeze(); }
        }
        void unfreeze_cursor() {
            bFreezeCursor = false;
            ShowCursor();
        }
        public AutoUnfreezer RequestFreezeCursor()
        {
            if (bFreezeCursor == false) {
                bFreezeCursor = true;
                HideCursor();
                return new AutoUnfreezer(() => { unfreeze_cursor(); });
            } else {
                throw new System.Exception("MouseCursorController.RequestFreeze but already frozen");
            }
        }

	}

}