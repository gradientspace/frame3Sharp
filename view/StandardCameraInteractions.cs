using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    // default maya alt+left/right/middle hotkeys
    public class MayaCameraHotkeys : ICameraInteraction
    {
        public float MouseOrbitSpeed = 10.0f;
        public float MousePanSpeed = 0.5f;
        public float MouseZoomSpeed = 0.5f;

        public float GamepadOrbitSpeed = 2.0f;
        public float GamepadPanSpeed = 0.2f;
        public float GamepadZoomSpeed = 0.2f;


        public MayaCameraHotkeys()
        {
        }

        public CameraInteractionState CheckCameraControls(InputState input)
        {
            if (Input.GetKeyDown(KeyCode.LeftAlt) || 
                InputExtension.Get.GamepadLeftShoulder.Pressed || InputExtension.Get.GamepadRightShoulder.Pressed)
                return CameraInteractionState.BeginCameraAction;
            else if (Input.GetKeyUp(KeyCode.LeftAlt) 
                        || (InputExtension.Get.GamepadLeftShoulder.Released && InputExtension.Get.GamepadRightShoulder.Down == false)
                        || (InputExtension.Get.GamepadRightShoulder.Released && InputExtension.Get.GamepadLeftShoulder.Down == false ) )
                return CameraInteractionState.EndCameraAction;
            else
                return CameraInteractionState.Ignore;
        }

        public void DoCameraControl(FScene scene, fCamera mainCamera, InputState input)
        {
            Vector2f mouseDelta = InputExtension.Get.Mouse.PositionDelta;
            Vector2f stick1 = InputExtension.Get.GamepadLeftStick.Position;
            Vector2f stick2 = InputExtension.Get.GamepadRightStick.Position;
            float dx = mouseDelta.x + stick1.x;
            float dy = mouseDelta.y + stick1.y;
            float dx2 = stick2.x;
            float dy2 = stick2.y;

            if (Input.GetMouseButton(0)) {
                mainCamera.Manipulator().SceneOrbit(scene, mainCamera, MouseOrbitSpeed * dx, MouseOrbitSpeed * dy);

            } else if (Input.GetMouseButton(1)) {
                mainCamera.Manipulator().SceneZoom(scene, mainCamera, -MouseZoomSpeed * dy);
                //mainCamera.Manipulator().ScenePan(scene, mainCamera, 0.05f * dx, 0);

            } else if (Input.GetMouseButton(2)) {
                mainCamera.Manipulator().ScenePan(scene, mainCamera, MousePanSpeed * dx, MousePanSpeed * dy);

            } else if (InputExtension.Get.GamepadRightShoulder.Down) {
                mainCamera.Manipulator().SceneZoom(scene, mainCamera, GamepadZoomSpeed * dy);
                mainCamera.Manipulator().ScenePan(scene, mainCamera, (-0.5f*GamepadPanSpeed * dx) + (-GamepadPanSpeed * dx2), -GamepadPanSpeed * dy2);

            } else if (InputExtension.Get.GamepadLeftShoulder.Down) {
                mainCamera.Manipulator().SceneOrbit(scene, mainCamera, GamepadOrbitSpeed * dx, GamepadOrbitSpeed * dy);
                mainCamera.Manipulator().ScenePan(scene, mainCamera, -GamepadPanSpeed * dx2, -GamepadPanSpeed * dy2);
            }
        }

    }











    // default maya alt+left/right/middle hotkeys
    // plus, alt+shift+lmb=pan and alt+ctrl+lmb=zoom
    public class MayaExtCameraHotkeys : ICameraInteraction
    {
        public float MouseOrbitSpeed = 10.0f;
        public float MousePanSpeed = 0.5f;
        public float MouseZoomSpeed = 0.5f;
        public bool UseAdaptive = false;

        public float GamepadOrbitSpeed = 2.0f;
        public float GamepadPanSpeed = 0.2f;
        public float GamepadZoomSpeed = 0.2f;


        public MayaExtCameraHotkeys()
        {
        }

        public CameraInteractionState CheckCameraControls(InputState input)
        {
            if (Input.GetKeyDown(KeyCode.LeftAlt) ||
                InputExtension.Get.GamepadLeftShoulder.Pressed || InputExtension.Get.GamepadRightShoulder.Pressed)
                return CameraInteractionState.BeginCameraAction;
            else if (Input.GetKeyUp(KeyCode.LeftAlt)
                        || (InputExtension.Get.GamepadLeftShoulder.Released && InputExtension.Get.GamepadRightShoulder.Down == false)
                        || (InputExtension.Get.GamepadRightShoulder.Released && InputExtension.Get.GamepadLeftShoulder.Down == false))
                return CameraInteractionState.EndCameraAction;
            else
                return CameraInteractionState.Ignore;
        }

        public void DoCameraControl(FScene scene, fCamera mainCamera, InputState input)
        {
            Vector2f mouseDelta = InputExtension.Get.Mouse.PositionDelta;
            Vector2f stick1 = InputExtension.Get.GamepadLeftStick.Position;
            Vector2f stick2 = InputExtension.Get.GamepadRightStick.Position;
            float dx = mouseDelta.x + stick1.x;
            float dy = mouseDelta.y + stick1.y;
            float dx2 = stick2.x;
            float dy2 = stick2.y;

            if (Input.GetMouseButton(0)) {
                if (input.bShiftKeyDown) {
                    if (UseAdaptive)
                        mainCamera.Manipulator().SceneAdaptivePan(scene, mainCamera, MousePanSpeed * dx, MousePanSpeed * dy);
                    else
                        mainCamera.Manipulator().ScenePan(scene, mainCamera, MousePanSpeed * dx, MousePanSpeed * dy);
                } else if (input.bCtrlKeyDown || input.bCmdKeyDown) {
                    if ( UseAdaptive )
                        mainCamera.Manipulator().SceneAdaptiveZoom(scene, mainCamera, -MouseZoomSpeed * dy);
                    else
                        mainCamera.Manipulator().SceneZoom(scene, mainCamera, -MouseZoomSpeed * dy);
                } else {
                    mainCamera.Manipulator().SceneOrbit(scene, mainCamera, MouseOrbitSpeed * dx, MouseOrbitSpeed * dy);
                }

            } else if (Input.GetMouseButton(1)) {
                if (UseAdaptive)
                    mainCamera.Manipulator().SceneAdaptiveZoom(scene, mainCamera, -MouseZoomSpeed * dy);
                else
                    mainCamera.Manipulator().SceneZoom(scene, mainCamera, -MouseZoomSpeed * dy);

            } else if (Input.GetMouseButton(2)) {
                if (UseAdaptive)
                    mainCamera.Manipulator().SceneAdaptivePan(scene, mainCamera, MousePanSpeed * dx, MousePanSpeed * dy);
                else
                    mainCamera.Manipulator().ScenePan(scene, mainCamera, MousePanSpeed * dx, MousePanSpeed * dy);

            } else if (InputExtension.Get.GamepadRightShoulder.Down) {
                mainCamera.Manipulator().SceneZoom(scene, mainCamera, GamepadZoomSpeed * dy);
                mainCamera.Manipulator().ScenePan(scene, mainCamera, (-0.5f * GamepadPanSpeed * dx) + (-GamepadPanSpeed * dx2), -GamepadPanSpeed * dy2);

            } else if (InputExtension.Get.GamepadLeftShoulder.Down) {
                mainCamera.Manipulator().SceneOrbit(scene, mainCamera, GamepadOrbitSpeed * dx, GamepadOrbitSpeed * dy);
                mainCamera.Manipulator().ScenePan(scene, mainCamera, -GamepadPanSpeed * dx2, -GamepadPanSpeed * dy2);
            }
        }

    }








    // default maya alt+left/right/middle hotkeys
    public class RateControlledEgocentricCamera : ICameraInteraction
    {
        bool bInAction;
        bool bUsingMouse, bUsingGamepad;

        CameraManipulator.RateControlInfo rcInfo;
        CameraManipulator.RateControlInfo rcInfo2;
        Vector2 curPos2D, secondPos2D;
        VRMouseCursorController.AutoUnfreezer unfreezer;

        public RateControlledEgocentricCamera()
        {
            bInAction = false;
            bUsingMouse = bUsingGamepad = false;
        }

        public CameraInteractionState CheckCameraControls(InputState input)
        {
            if (Input.GetKeyDown(KeyCode.LeftAlt)) {
                return CameraInteractionState.BeginCameraAction;
            } else if (InputExtension.Get.GamepadLeftShoulder.Pressed || InputExtension.Get.GamepadRightShoulder.Pressed) {

                if (unfreezer == null && FContext.ActiveContext_HACK.MouseController is VRMouseCursorController )
                    unfreezer = (FContext.ActiveContext_HACK.MouseController as VRMouseCursorController).RequestFreezeCursor();

                return CameraInteractionState.BeginCameraAction;
            } else if (Input.GetKeyUp(KeyCode.LeftAlt)
                         || (InputExtension.Get.GamepadLeftShoulder.Released && InputExtension.Get.GamepadRightShoulder.Down == false)
                         || (InputExtension.Get.GamepadRightShoulder.Released && InputExtension.Get.GamepadLeftShoulder.Down == false)) {
                end_camera_action();

                if ( unfreezer != null ) {
                    unfreezer.Unfreeze();
                    unfreezer = null;
                }

                return CameraInteractionState.EndCameraAction;
            } else
                return CameraInteractionState.Ignore;
        }


        void end_camera_action()
        {
            if (bInAction) {
                rcInfo = null;
                bInAction = false;
            }
            bUsingMouse = bUsingGamepad = false;
        }


        public void DoCameraControl(FScene scene, fCamera mainCamera, InputState input)
        {
            if (bInAction == false) {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) {
                    curPos2D = new Vector2(0, 0);
                    rcInfo = new CameraManipulator.RateControlInfo(curPos2D);
                    bInAction = bUsingMouse = true;
                } else if (InputExtension.Get.GamepadRightShoulder.Down || InputExtension.Get.GamepadLeftShoulder.Down) {
                    curPos2D = secondPos2D = new Vector2(0, 0);
                    rcInfo = new CameraManipulator.RateControlInfo(curPos2D);
                    rcInfo2 = new CameraManipulator.RateControlInfo(secondPos2D);
                    bInAction = bUsingGamepad = true;
                }
            }

            if ( bInAction && bUsingMouse ) {
                Vector2f mouseDelta = InputExtension.Get.Mouse.PositionDelta;
                curPos2D.x += mouseDelta.x;
                curPos2D.y += mouseDelta.y;

                if (Input.GetMouseButton(0)) {
                    mainCamera.Manipulator().SceneRateControlledFly(scene, mainCamera, curPos2D, rcInfo);
                } else if (Input.GetMouseButton(1)) {
                    mainCamera.Manipulator().SceneRateControlledZoom(scene, mainCamera, curPos2D, rcInfo);
                } else if (Input.GetMouseButton(2)) {
                    mainCamera.Manipulator().SceneRateControlledEgogentricPan(scene, mainCamera, curPos2D, rcInfo);
                }
            }


            if ( bInAction && bUsingGamepad ) {
                Vector2f stick1 = InputExtension.Get.GamepadLeftStick.Position;
                Vector2f stick2 = InputExtension.Get.GamepadRightStick.Position;
                float dx = stick1.x;
                float dy = stick1.y;
                float dx2 = stick2.x;
                float dy2 = stick2.y;
                curPos2D.x += dx; 
                curPos2D.y += dy;
                secondPos2D.x += dx2;
                secondPos2D.y += dy2;
                float use_t = 3.0f;     // 5 == hard stop, 1 == bit too soft
                curPos2D.x = MathUtil.SignedClamp(curPos2D.x, rcInfo.rampUpRadius);
                curPos2D.x = Mathf.Lerp(curPos2D.x, 0, use_t * Time.deltaTime);
                curPos2D.y = MathUtil.SignedClamp(curPos2D.y, rcInfo.rampUpRadius);
                curPos2D.y = Mathf.Lerp(curPos2D.y, 0, use_t * Time.deltaTime);
                secondPos2D.x = MathUtil.SignedClamp(secondPos2D.x, rcInfo.rampUpRadius);
                secondPos2D.x = Mathf.Lerp(secondPos2D.x, 0, use_t * Time.deltaTime);
                secondPos2D.y = MathUtil.SignedClamp(secondPos2D.y, rcInfo.rampUpRadius);
                secondPos2D.y = Mathf.Lerp(secondPos2D.y, 0, use_t * Time.deltaTime);

                if (InputExtension.Get.GamepadRightShoulder.Down) {
                    mainCamera.Manipulator().SceneRateControlledZoom(scene, mainCamera, curPos2D, rcInfo);
                    secondPos2D[0] = 0;
                    mainCamera.Manipulator().SceneRateControlledEgogentricPan(scene, mainCamera, secondPos2D, rcInfo2);
                } else if (InputExtension.Get.GamepadLeftShoulder.Down) {
                    mainCamera.Manipulator().SceneRateControlledFly(scene, mainCamera, curPos2D, rcInfo);
                    mainCamera.Manipulator().SceneRateControlledEgogentricPan(scene, mainCamera, secondPos2D, rcInfo2);
                }

            }
        }
        
    }

}
