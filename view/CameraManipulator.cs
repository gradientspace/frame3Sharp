using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{

    public struct CameraState
    {
        public Vector3 camPosition;
        public Quaternion camRotation;
        public Vector3 scenePosition;
        public Quaternion sceneRotation;
        public Vector3 target;
        public float turntable_x, turntable_y;
    }


    // [TODO] this should just know about current scene, then we can
    //   get rid of all the goddamn arguments!!


    public class CameraManipulator : MonoBehaviour
    {
        Camera getCamera()
        {
            return gameObject.GetComponent<Camera>();
        }

        // camera stuff
        public float[] turntable_angles = { 0.0f, 0.0f };

        public CameraManipulator()
        {
        }


        public void SceneTumble(FScene scene, Camera cam, float dx, float dy)
        {
            Vector3 up = cam.gameObject.transform.up;
            Vector3 right = cam.gameObject.transform.right;

            //Vector3 curOrigin = scene.RootGameObject.transform.position;
            Vector3 curOrigin = cam.GetTarget();

            scene.RootGameObject.transform.RotateAround(curOrigin, up, dx);
            scene.RootGameObject.transform.RotateAround(curOrigin, right, dy);
        }


        public void SceneOrbit(FScene scene, Camera cam, float dAzimuth, float dAltitude)
        {
            //Vector3 sceneOrigin = scene.RootGameObject.transform.position;
            Vector3 rotTarget = cam.GetTarget();

            // [RMS] Orbiting around the Target point is kind of a weird concept when
            //   you are moving the scene and not the camera. Basically we want to rotate
            //   the scene around the target, but not incrementally - based on a pair of
            //   spherical angles. So, we have to return to a 'reference' state each update.
            //   Simplest way to do that is to "un-rotate" by the angles before the delta...
            //   (???)

            scene.RootGameObject.transform.RotateAround(rotTarget, Vector3.right, -turntable_angles[1]);
            scene.RootGameObject.transform.RotateAround(rotTarget, Vector3.up, -turntable_angles[0]);

            turntable_angles[0] -= dAzimuth;
            turntable_angles[1] += dAltitude;
            turntable_angles[1] = Mathf.Clamp(turntable_angles[1], -89.9f, 89.9f);

            scene.RootGameObject.transform.RotateAround(rotTarget, Vector3.up, turntable_angles[0]);
            scene.RootGameObject.transform.RotateAround(rotTarget, Vector3.right, turntable_angles[1]);
        }


        public void ResetSceneOrbit(FScene scene, bool bAzimuth, bool bAltitude, bool bApply = true)
        {
            Vector3 rotTarget = getCamera().GetTarget();
            if ( bApply ) {
                scene.RootGameObject.transform.RotateAround(rotTarget, Vector3.right, -turntable_angles[1]);
                scene.RootGameObject.transform.RotateAround(rotTarget, Vector3.up, -turntable_angles[0]);
            }
            if (bAzimuth == true)
                turntable_angles[0] = 0.0f;
            if (bAltitude == true)
                turntable_angles[1] = 0.0f;
            if ( bApply ) {
                scene.RootGameObject.transform.RotateAround(rotTarget, Vector3.up, turntable_angles[0]);
                scene.RootGameObject.transform.RotateAround(rotTarget, Vector3.right, turntable_angles[1]);
            }
        }



        public void ResetScenePosition(FScene scene)
        {
            scene.RootGameObject.transform.position = Vector3.zero;
            Vector3 targetPos = Vector3.zero;
            getCamera().SetTarget(targetPos);
        }


        public void SceneTranslate(FScene scene, Vector3 translate)
        {
            scene.RootGameObject.transform.position += translate;
        }

        public void ScenePan(FScene scene, Camera cam, float dx, float dy)
        {
            float fScale = scene.GetSceneScale();

            Vector3 right = cam.gameObject.transform.right;
            Vector3 up = cam.gameObject.transform.up;
            Vector3 delta = dx * fScale * right + dy * fScale * up;
            Vector3 newPos = scene.RootGameObject.transform.position + delta;
            scene.RootGameObject.transform.position = newPos;
        }

        public void SceneZoom(FScene scene, Camera cam, float dz)
        {
            if (dz == 0.0f)
                return;

            float fScale = scene.GetSceneScale();

            //Vector3 fw = cam.gameObject.transform.forward;
            Vector3 fw = cam.GetTarget() - cam.transform.position;
            float fTargetDist = fw.magnitude;
            fw.Normalize();

            float fMinTargetDist = 0.1f;
            if (dz > 0 && fTargetDist - dz < fMinTargetDist)
                dz = fTargetDist - fMinTargetDist;

            Vector3 delta = dz * fScale * fw;
            scene.RootGameObject.transform.position -= delta;
            cam.SetTarget(cam.GetTarget() - delta);
        }


        public void ScenePanFocus(Camera camera, Vector3 focusPoint)
        {
            CameraAnimator animator = camera.gameObject.GetComponent<CameraAnimator>();
            if (animator == null)
                return;                 // [TODO] do non-animated version?
            animator.PanFocus(focusPoint);
        }





        public CameraState GetCurrentState(FScene scene)
        {
            CameraState s = new CameraState();
            s.camPosition = getCamera().transform.position;
            s.camRotation = getCamera().transform.rotation;
            s.scenePosition = scene.RootGameObject.transform.position;
            s.sceneRotation = scene.RootGameObject.transform.rotation;
            s.target = getCamera().GetTarget();
            s.turntable_x = turntable_angles[0];
            s.turntable_y = turntable_angles[1];
            return s;
        }

        // restores scene values & cam target (but not cam position)
        public void SetCurrentSceneState(FScene scene, CameraState s)
        {
            scene.RootGameObject.transform.position = s.scenePosition;
            scene.RootGameObject.transform.rotation = s.sceneRotation;
            getCamera().SetTarget(s.target);
            turntable_angles[0] = s.turntable_x;
            turntable_angles[1] = s.turntable_y;
        }



        public class RateControlInfo
        {
            public float lastTime;
            public float maxSpeed;
            public float maxAccel;
            public float angleMultipler;
            public float curSpeedX, curSpeedY;
            public Vector2 startPos;
            public float rampUpRadius;
            public float deadZoneRadius;
            public bool StayLevel;
            public RateControlInfo(Vector2 startPos)
            {
                lastTime = FPlatform.RealTime();
                maxSpeed = 5.0f;
                maxAccel = 10.5f;
                angleMultipler = 4.0f;
                curSpeedX = curSpeedY = 0.0f;
                rampUpRadius = 10.0f;
                deadZoneRadius = 1.0f;
                StayLevel = true;
                this.startPos = startPos;
            }
        }


        float rc_update_speed(float delta_x, float rampUpRadius, float deadZoneRadius, float maxAccel, float maxSpeed, float dt, float curSpeed)
        {
            float ty = g3.MathUtil.LinearRampT(rampUpRadius, deadZoneRadius, delta_x);
            float target_vel = ty * maxSpeed;

            // this doesn't make any sense!
            float accel = g3.MathUtil.RangeClamp((target_vel - curSpeed), maxAccel);

            return g3.MathUtil.RangeClamp(curSpeed + dt * accel, target_vel);
        }



        public void SceneRateControlledFly(FScene scene, Camera cam, Vector2 curPos, RateControlInfo rc )
        {
            float dt = (FPlatform.RealTime() - rc.lastTime);
            rc.lastTime = FPlatform.RealTime();

            float delta_y = (curPos.y - rc.startPos.y);
            rc.curSpeedY =
                rc_update_speed(delta_y, rc.rampUpRadius, rc.deadZoneRadius, rc.maxAccel, rc.maxSpeed, dt, rc.curSpeedY);
            float delta_x = (curPos.x - rc.startPos.x);
            rc.curSpeedX =
                rc_update_speed(delta_x, rc.rampUpRadius, rc.deadZoneRadius, rc.maxAccel, rc.maxSpeed, dt, rc.curSpeedX);


            Vector3 forward, up;
            if (rc.StayLevel) {
                forward = new Vector3(cam.transform.forward.x, 0.0f, cam.transform.forward.z);
                forward.Normalize();
                up = Vector3.up;
            } else {
                forward = cam.gameObject.transform.forward;
                up = cam.gameObject.transform.up;
            }
            Vector3 curScenePos = scene.RootGameObject.transform.position;
            Vector3 newScenePos = curScenePos - dt * rc.curSpeedY * forward;
            scene.RootGameObject.transform.position = newScenePos;
            scene.RootGameObject.transform.RotateAround(
                cam.transform.position, up, -dt * rc.curSpeedX * rc.angleMultipler);
        }



        public void SceneRateControlledZoom(FScene scene, Camera cam, Vector2 curPos, RateControlInfo rc)
        {
            float dt = (FPlatform.RealTime() - rc.lastTime);
            rc.lastTime = FPlatform.RealTime();

            float delta_y = (curPos.y - rc.startPos.y);
            rc.curSpeedY =
                rc_update_speed(delta_y, rc.rampUpRadius, rc.deadZoneRadius, rc.maxAccel, rc.maxSpeed, dt, rc.curSpeedY);
            float delta_x = (curPos.x - rc.startPos.x);
            rc.curSpeedX =
                rc_update_speed(delta_x, rc.rampUpRadius, rc.deadZoneRadius, rc.maxAccel, rc.maxSpeed, dt, rc.curSpeedX);


            Vector3 forward, up;
            if (rc.StayLevel) {
                forward = new Vector3(cam.transform.forward.x, 0.0f, cam.transform.forward.z);
                forward.Normalize();
                up = Vector3.up;
            } else {
                forward = cam.gameObject.transform.forward;
                up = cam.gameObject.transform.up;
            }
            Vector3 right = Vector3.Cross(up, forward);
            Vector3 curScenePos = scene.RootGameObject.transform.position;
            Vector3 newScenePos = curScenePos - dt * (rc.curSpeedY * forward + rc.curSpeedX * right);
            scene.RootGameObject.transform.position = newScenePos;
        }


        // in egocentric camera system the "eye" is moving up/down, which means the scene moves in opposite direction
        public void SceneRateControlledEgogentricPan(FScene scene, Camera cam, Vector2 curPos, RateControlInfo rc)
        {
            float dt = (FPlatform.RealTime() - rc.lastTime);
            rc.lastTime = FPlatform.RealTime();

            float delta_y = (curPos.y - rc.startPos.y);
            rc.curSpeedY =
                rc_update_speed(delta_y, rc.rampUpRadius, rc.deadZoneRadius, rc.maxAccel, rc.maxSpeed, dt, rc.curSpeedY);
            float delta_x = (curPos.x - rc.startPos.x);
            rc.curSpeedX =
                rc_update_speed(delta_x, rc.rampUpRadius, rc.deadZoneRadius, rc.maxAccel, rc.maxSpeed, dt, rc.curSpeedX);


            Vector3 forward, up;
            if (rc.StayLevel) {
                forward = new Vector3(cam.transform.forward.x, 0.0f, cam.transform.forward.z);
                forward.Normalize();
                up = Vector3.up;
            } else {
                forward = cam.gameObject.transform.forward;
                up = cam.gameObject.transform.up;
            }
            Vector3 right = Vector3.Cross(up, forward);
            Vector3 curScenePos = scene.RootGameObject.transform.position;
            Vector3 newScenePos = curScenePos - dt * (rc.curSpeedY * up + rc.curSpeedX * right);
            scene.RootGameObject.transform.position = newScenePos;
        }




    }
}
