using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    public class Teleporter
    {
        public static float DefaultHeightInM = 1.6f;   // 1.6m ~= 5.5 feet


        // straight teleport:
        //   camera points at hit point, we back off and set target at hit point
        static public void TeleportTowards(fCamera camera, FScene scene, Vector3f targetPoint, float fPullback)
        {
            fPullback *= scene.GetSceneScale();
            Vector3f vCurCamFW = camera.GetWorldFrame().Z;
            Vector3f vNewCamPos = targetPoint - fPullback * vCurCamFW;
            Vector3f vNewTargetPos = vNewCamPos + fPullback * vCurCamFW;
            camera.Animator().Teleport(vNewCamPos, vNewTargetPos);
        }


        // level teleport:
        //   first we level the scene (ie scene up is y axis), then camera points
        //   at ray hit point, pulled back along cam direction projected into xz plane
        static public void TeleportTowards_Level(fCamera camera, FScene scene, Vector3f targetPoint, float fPullback)
        {
            fPullback *= scene.GetSceneScale();
            Vector3f vCurCamFW = camera.GetWorldFrame().Z;
            Vector3f vCurCamFWXZ = new Vector3f(vCurCamFW[0], 0.0f, vCurCamFW[2]).Normalized;
            Vector3f vNewCamPos = targetPoint - fPullback * vCurCamFWXZ;
            Vector3f vNewTargetPos = vNewCamPos + fPullback * vCurCamFWXZ;
            camera.Animator().Teleport_Level(vNewCamPos, vNewTargetPos);
        }


        // vertical-offset level teleport
        //   assumption is that target point is on ground or ceiling, and we want to offset vertically.
        //   To do that we have to first reset orbit around the hit point. 
        //   One small problem here is that the output target pos is really arbitrary. perhaps we should
        //   cast another ray into the scene in the forward direction?
        static public void Teleport_VerticalNormalOffset(fCamera camera, FScene scene, 
            Vector3f targetPoint, Vector3f targetNormal, float fNewTargetDist)
        {
            fNewTargetDist *= scene.GetSceneScale();

            Vector3f vCurCamFW = camera.GetWorldFrame().Z;
            Vector3f vCurCamFWXZ = new Vector3f(vCurCamFW[0], 0.0f, vCurCamFW[2]).Normalized;
            Vector3f vSceneUp = scene.RootGameObject.GetWorldFrame().Y;

            // when we are offsetting up we use the DefaultHeightInM value to set eye level,
            // when offsetting down we use half that distance (arbitrary)
            float fUp = Vector3f.Dot(targetNormal, vSceneUp) > 0 ? 1.0f : -0.5f;
            // [RMS] camera height does not work because OVRCameraRig sets eye-level to be origin!!
            //float fHeight = camera.GetPosition()[1] * scene.GetSceneScale();
            float fHeight = DefaultHeightInM * scene.GetSceneScale();
            Vector3f vNewCamPos = targetPoint + fUp * fHeight * Vector3f.AxisY;
            Vector3f vNewTargetPos = vNewCamPos + fNewTargetDist * vCurCamFWXZ;
            camera.Animator().Teleport_Level(vNewCamPos, vNewTargetPos, targetPoint);
        }


        //   we orbit view to be level, then again so that the normal of the hit point points towards
        //   the camera. Then we pull back along that direction. Unless we want to face away,
        //   then we rotate an extra 180 degrees, and set the target out by the pullback distance
        static public void Teleport_LevelNormalOffset(fCamera camera, FScene scene, 
            Vector3f targetPoint, Vector3f targetNormal, float fPullback, bool bFaceAwayFromSurface = false)
        {
            fPullback *= scene.GetSceneScale();

            Vector3f vCurCamFW = camera.GetWorldFrame().Z;
            Vector3f vCurCamFWXZ = new Vector3f(vCurCamFW[0], 0.0f, vCurCamFW[2]).Normalized;
            Vector3f vHitNormalXZ = new Vector3f(targetNormal[0], 0.0f, targetNormal[2]).Normalized;

            float fFace = bFaceAwayFromSurface ? -1.0f : 1.0f;
            float fRotate = Vector3f.AngleD(-vCurCamFWXZ, fFace * vHitNormalXZ);
            float fSign = Vector3f.Cross(vCurCamFWXZ, fFace * vHitNormalXZ)[1] > 0 ? -1 : 1;
            fRotate *= fSign;
            // now we use cam instead of normal here because we rotated normal to face cam!
            Vector3f vNewCamPos = targetPoint - fFace * fPullback * vCurCamFWXZ;        
            Vector3f vNewTargetPos = (bFaceAwayFromSurface) ? vNewCamPos + fPullback * vCurCamFWXZ : targetPoint;
            camera.Animator().Teleport_Level(vNewCamPos, vNewTargetPos, targetPoint, fRotate);
        }


        // use normal angle w/ up vector to decide if we should offset vertically or horizontally 
        //   from hit point (threshold hardcoded to 45 degrees right now)
        static public void Teleport_Normal_Auto(fCamera camera, FScene scene,
            Vector3f targetPoint, Vector3f targetNormal, float fPullback)
        {
            float fDot = Mathf.Abs(Vector3f.Dot(targetNormal, scene.RootGameObject.GetWorldFrame().Y));
            float fAngle = Mathf.Acos(fDot) * Mathf.Rad2Deg;
            if (fAngle > 45.0f)
                Teleport_LevelNormalOffset(camera, scene, targetPoint, targetNormal, fPullback, false);
            else
                Teleport_VerticalNormalOffset(camera, scene, targetPoint, targetNormal, fPullback);
        }

    }
}
