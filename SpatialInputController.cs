using UnityEngine;
using System.Collections;
using g3;
using gs;

namespace f3 {

	public class SpatialInputController {

        GameObject spatialCamRig;
        Camera camera;
		FContext context;

        public bool SpatialInputActive { get; set; }
        bool spatialInputInitialized;

        public float CursorVisualAngleInDegrees { get; set; }

        public class SpatialDevice
        {
            public GameObject Hand { get; set; }
            public GameObject Cursor { get; set; }
            public GameObject Laser { get; set; }
            public LineRenderer LaserRen { get; set; }

            public Material CursorDefaultMaterial { get; set; }
            public Material CursorHitMaterial { get; set; }
            public Material CursorClickableMaterial { get; set; }
            public Material CursorCapturingMaterial { get; set; }

            public Material HandMaterial { get; set; }

            public bool CursorActive { get; set; }

            public Frame3f AbsoluteHandFrame { get; set; }
            public Frame3f SmoothedHandFrame { get; set; }

            public Ray CursorRay { get; set; }
            public Vector3 RayHitPos { get; set; }

            public GameObject HandIcon { get; set; }
        }
        public SpatialDevice Left;
        public SpatialDevice Right;


		public SpatialInputController(GameObject spatialCamRig, Camera viewCam, FContext context) {
            this.spatialCamRig = spatialCamRig;
            this.camera = viewCam;
			this.context = context;
		}

		// Use this for initialization
		public void Start () {
            spatialInputInitialized = false;
            SpatialInputActive = false;
        }


        public bool CheckForSpatialInputActive()
        {
            if (spatialCamRig == null)
                return false;

            bool bTracking = VRPlatform.HaveActiveSpatialInput;
            if (bTracking) {
                SpatialInputActive = true;
                if (spatialInputInitialized == false)
                    InitializeSpatialInput();
            } else {
                SpatialInputActive = false;
            }
            return SpatialInputActive;
        }


        Quaternion handGeomRotation = Quaternion.FromToRotation(Vector3.up, Vector3.forward);


        Mesh standardCursorMesh;
        Mesh activeToolCursorMesh;

        void InitializeSpatialInput()
        {
            Left = new SpatialDevice();
            Right = new SpatialDevice();

            Left.CursorDefaultMaterial = MaterialUtil.CreateTransparentMaterial(ColorUtil.ForestGreen, 0.6f);
            Left.CursorHitMaterial = MaterialUtil.CreateStandardMaterial(ColorUtil.ForestGreen);
            Left.CursorClickableMaterial = MaterialUtil.CreateStandardMaterial(ColorUtil.SelectionGold);
            Left.CursorCapturingMaterial = MaterialUtil.CreateTransparentMaterial(ColorUtil.SelectionGold, 0.75f);
            Left.HandMaterial = MaterialUtil.CreateTransparentMaterial(ColorUtil.ForestGreen, 0.3f);

            Right.CursorDefaultMaterial = MaterialUtil.CreateTransparentMaterial(Colorf.DarkRed, 0.6f);
            Right.CursorHitMaterial = MaterialUtil.CreateStandardMaterial(Colorf.VideoRed);
            Right.CursorClickableMaterial = MaterialUtil.CreateStandardMaterial(ColorUtil.PivotYellow);
            Right.CursorCapturingMaterial = MaterialUtil.CreateTransparentMaterial(ColorUtil.PivotYellow, 0.75f);
            Right.HandMaterial = MaterialUtil.CreateTransparentMaterial(ColorUtil.CgRed, 0.3f);

            CursorVisualAngleInDegrees = 1.5f;

            standardCursorMesh = MeshGenerators.Create3DArrow(1.0f, 1.0f, 1.0f, 0.5f, 16);
            UnityUtil.TranslateMesh(standardCursorMesh, 0, -2.0f, 0);
            activeToolCursorMesh = MeshGenerators.Create3DArrow(1.0f, 1.0f, 1.0f, 1.0f, 16);
            UnityUtil.TranslateMesh(activeToolCursorMesh, 0, -2.0f, 0);

            Left.Cursor = UnityUtil.CreateMeshGO("left_cursor", standardCursorMesh, Left.CursorDefaultMaterial);
            Left.Cursor.transform.localScale = 0.3f * Vector3.one; 
            Left.Cursor.transform.localRotation = Quaternion.AngleAxis(45.0f, new Vector3(1, 0, 1).normalized);
            MaterialUtil.DisableShadows(Left.Cursor);
            Left.Cursor.SetLayer(FPlatform.CursorLayer);
            Left.SmoothedHandFrame = Frame3f.Identity;

            var leftHandMesh = MeshGenerators.Create3DArrow(SceneGraphConfig.VRHandTipOffset, SceneGraphConfig.VRHandArrowRadius,
                SceneGraphConfig.VRHandStickLength, SceneGraphConfig.VRHandStickWidth, 16);
            Left.Hand = UnityUtil.CreateMeshGO( "left_hand", leftHandMesh, Left.HandMaterial);
            UnityUtil.TranslateMesh(leftHandMesh, 0, -SceneGraphConfig.VRHandStickLength, 0);
            Left.Hand.transform.rotation = Quaternion.FromToRotation(Vector3.up, Vector3.forward);
            Left.Hand.SetLayer(FPlatform.HUDLayer);

            Left.Laser = new GameObject("left_laser");
            Left.LaserRen = Left.Laser.AddComponent<LineRenderer>();
            Left.LaserRen.SetPositions( new Vector3[2] { Vector3.zero, 100*Vector3.up }  );
            Left.LaserRen.startWidth = Left.LaserRen.endWidth = 0.01f;
            Left.LaserRen.material = MaterialUtil.CreateFlatMaterial(ColorUtil.ForestGreen, 0.2f);
            Left.Laser.SetLayer(FPlatform.CursorLayer);
            Left.Laser.transform.parent = Left.Cursor.transform;

            Right.Cursor = UnityUtil.CreateMeshGO("right_cursor", standardCursorMesh, Right.CursorDefaultMaterial);
            Right.Cursor.transform.localScale = 0.3f * Vector3.one; 
            Right.Cursor.transform.localRotation = Quaternion.AngleAxis(45.0f, new Vector3(1, 0, 1).normalized);
            MaterialUtil.DisableShadows(Right.Cursor);
            Right.Cursor.SetLayer(FPlatform.CursorLayer);
            Right.SmoothedHandFrame = Frame3f.Identity;

            var rightHandMesh = MeshGenerators.Create3DArrow(SceneGraphConfig.VRHandTipOffset, SceneGraphConfig.VRHandArrowRadius,
                SceneGraphConfig.VRHandStickLength, SceneGraphConfig.VRHandStickWidth, 16);
            Right.Hand = UnityUtil.CreateMeshGO("right_hand", rightHandMesh, Right.HandMaterial);
            UnityUtil.TranslateMesh(rightHandMesh, 0, -SceneGraphConfig.VRHandStickLength, 0);
            Right.Hand.transform.rotation = Quaternion.FromToRotation(Vector3.up, Vector3.forward);
            Right.Hand.SetLayer(FPlatform.HUDLayer);

            Right.Laser = new GameObject("right_laser");
            Right.LaserRen = Right.Laser.AddComponent<LineRenderer>();
            Right.LaserRen.SetPositions(new Vector3[2] { Vector3.zero, 100 * Vector3.up });
            Right.LaserRen.startWidth = Right.LaserRen.endWidth = 0.01f;
            Right.LaserRen.material = MaterialUtil.CreateFlatMaterial(ColorUtil.CgRed, 0.2f);
            Right.Laser.SetLayer(FPlatform.CursorLayer);
            Right.Laser.transform.parent = Right.Cursor.transform;

            spatialInputInitialized = true;
        }




        // FixedUpdate is called before any Update
        public void Update () {
            if (CheckForSpatialInputActive() == false)
                return;

            Vector3 rootPos = spatialCamRig.transform.position;

            SpatialDevice[] hands = { Left, Right };
            for (int i = 0; i < 2; ++i) {
                SpatialDevice h = hands[i];

                h.CursorActive = VRPlatform.IsSpatialDeviceTracked(i);
                if (h.CursorActive) {
                    h.Hand.Show();
                    h.Cursor.Show();

                    Vector3 handPos = VRPlatform.GetLocalControllerPosition(i);
                    Quaternion handRot = VRPlatform.GetLocalControllerRotation(i);

                    h.AbsoluteHandFrame = new Frame3f(rootPos + handPos, handRot);

                    float fPositionT = 0.2f;
                    float fRotationT = 0.2f;
                    //float fPositionT = 1.0f;
                    //float fRotationT = 1.0f;

                    if (h.SmoothedHandFrame.Origin != Vector3f.Zero) {
                        Vector3 new_origin = 
                            Vector3.Lerp(h.SmoothedHandFrame.Origin, h.AbsoluteHandFrame.Origin, fPositionT);
                        Quaternion new_rotation =
                            Quaternion.Slerp(h.SmoothedHandFrame.Rotation, h.AbsoluteHandFrame.Rotation, fRotationT);
                        h.SmoothedHandFrame = new Frame3f(new_origin, new_rotation);
                    } else 
                        h.SmoothedHandFrame = h.AbsoluteHandFrame;

                    h.Hand.transform.position = h.SmoothedHandFrame.Origin;
                    h.Hand.transform.rotation = h.SmoothedHandFrame.Rotation * (Quaternionf)handGeomRotation;

                    h.CursorRay = new Ray(h.SmoothedHandFrame.Origin, 
                        (h.SmoothedHandFrame.Rotation * Vector3.forward).Normalized );

                    if (Mathf.Abs(h.CursorRay.direction.sqrMagnitude - 1.0f) > 0.001f) {
                        DebugUtil.Log(2, "SpatialInputController.Update - invlaid cursor ray! rotation was {0}", h.SmoothedHandFrame.Rotation);
                        h.CursorRay = new Ray(h.SmoothedHandFrame.Origin, Vector3.up);
                    }

                    // raycast into scene to see if we hit object, UI, bounds, etc. 
                    bool bHit = false;
                    bool bHitGizmo = false;
                    if (context != null) {
                        // want to hit-test active gizmo first, because that has hit-priority
                        if ( context.TransformManager.HaveActiveGizmo ) {
                            UIRayHit uiHit = null;
                            if ( context.TransformManager.ActiveGizmo.FindRayIntersection(h.CursorRay, out uiHit) ) {
                                h.RayHitPos = uiHit.hitPos;
                                bHit = true;
                                bHitGizmo = true;
                            }
                        }
                        // next we tested scene
                        if (bHit == false) {
                            AnyRayHit hit = null;
                            if (context.FindAnyRayIntersection(h.CursorRay, out hit)) {
                                h.RayHitPos = hit.hitPos;
                                bHit = true;
                            }
                        }
                        // finally test worldbounds
                        if (bHit == false) { 
                            GameObjectRayHit ghit = null;
                            if (context.GetScene().FindWorldBoundsHit(h.CursorRay, out ghit)) {
                                h.RayHitPos = ghit.hitPos;
                            }
                        }
                    }

                    // if not, plane cursor on view-perp plane centered at last hit pos,
                    // otherwise it will be stuck/disappear
                    if ( bHit == false ) {
                        Frame3f f = new Frame3f(h.RayHitPos, camera.transform.forward);
                        h.RayHitPos = f.RayPlaneIntersection(h.CursorRay.origin, h.CursorRay.direction, 2);
                    }

                    h.Cursor.transform.position = h.RayHitPos;
                    //if (scene.InCapture)
                    //    MaterialUtil.SetMaterial(h.Cursor, h.CursorCapturingMaterial);
                    //else
                    if ( bHitGizmo )
                        MaterialUtil.SetMaterial(h.Cursor, h.CursorClickableMaterial);
                    else if (bHit)
                        MaterialUtil.SetMaterial(h.Cursor, h.CursorHitMaterial);
                    else
                        MaterialUtil.SetMaterial(h.Cursor, h.CursorDefaultMaterial);

                    // maintain a consistent visual size for 3D cursor sphere
                    float fScaling = VRUtil.GetVRRadiusForVisualAngle(h.RayHitPos, camera.transform.position, CursorVisualAngleInDegrees);
                    h.Cursor.transform.localScale = fScaling * Vector3.one;

                    // orient cursor so it is tilted like a 2D cursor, but per-hand
                    Vector3 cursor_right = Vector3.Cross(camera.transform.up, h.CursorRay.direction);
                    Vector3 cursor_fw = Vector3.Cross(cursor_right, camera.transform.up);
                    float rotSign = (h == Right) ? 1.0f : -1.0f;
                    Vector3 pointDir = (camera.transform.up + cursor_fw - 0.5f * rotSign * cursor_right).normalized;
                    h.Cursor.transform.localRotation = Quaternion.FromToRotation(Vector3.up, pointDir);

                    // update laser line
                    if ( h.Laser != null ) {
                        float hDist = (h.RayHitPos - h.CursorRay.origin).magnitude;
                        Vector3 p0 = h.RayHitPos - 0.9f * hDist * h.CursorRay.direction;
                        Vector3 p1 = h.RayHitPos + 100.0f*h.CursorRay.direction;
                        float r0 = VRUtil.GetVRRadiusForVisualAngle(p0, camera.transform.position, 0.5f);
                        h.LaserRen.SetPosition(0, p0);
                        h.LaserRen.SetPosition(1, p1);
                        h.LaserRen.startWidth = h.LaserRen.endWidth = r0;
                    }

                    // udpate cursor
                    Mesh useMesh = context.ToolManager.HasActiveTool(i) ? activeToolCursorMesh : standardCursorMesh;
                    if (h.Cursor.GetSharedMesh() != useMesh) {
                        h.Cursor.SetSharedMesh(useMesh);
                    }


                } else {
                    h.Hand.Hide();
                    h.Cursor.Hide();
                }
            }
		}



        public void ClearHandIcon(int iSide)
        {
            SpatialDevice h = (iSide == 0) ? Left : Right;
            if (h.HandIcon != null) {
                h.HandIcon.transform.parent = null;
                h.HandIcon.Destroy();
            }
        }

        public void SetHandIcon(Mesh m, int iSide)
        {
            SpatialDevice h = (iSide == 0) ? Left : Right;
            if (h.HandIcon != null) {
                h.HandIcon.transform.parent = null;
                h.HandIcon.Destroy();
            }

            h.HandIcon = 
                UnityUtil.CreateMeshGO("hand_icon", m, MaterialUtil.CreateStandardVertexColorMaterial(Color.white), false);
            float s = SceneGraphConfig.VRHandArrowRadius;
            h.HandIcon.transform.localScale = s * 0.3f * Vector3.one;
            h.HandIcon.transform.localPosition = new Vector3(0.0f, -0.3f*s, -0.3f*s);
            h.HandIcon.transform.SetParent(h.Hand.transform, false);
            h.HandIcon.SetLayer(FPlatform.CursorLayer);

        }


    }

}