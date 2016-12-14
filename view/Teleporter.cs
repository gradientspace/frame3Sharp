using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{
    public class Teleporter
    {
        // straight teleport:
        //   camera points at hit point, we back off and set target at hit point
        static public void TeleportTowards(Camera camera, FScene scene, Vector3 targetPoint, float fPullback)
        {
            fPullback *= scene.GetSceneScale();
            Vector3 vCurCamFW = camera.transform.forward;
            Vector3 vNewCamPos = targetPoint - fPullback * vCurCamFW;
            Vector3 vNewTargetPos = vNewCamPos + fPullback * vCurCamFW;
            camera.GetComponent<CameraAnimator>().Teleport(vNewCamPos, vNewTargetPos);
        }


        // level teleport:
        //   first we level the scene (ie scene up is y axis), then camera points
        //   at ray hit point, pulled back along cam direction projected into xz plane
        static public void TeleportTowards_Level(Camera camera, FScene scene, Vector3 targetPoint, float fPullback)
        {
            fPullback *= scene.GetSceneScale();
            Vector3 vCurCamFW = camera.transform.forward;
            Vector3 vCurCamFWXZ = new Vector3(vCurCamFW[0], 0.0f, vCurCamFW[2]).normalized;
            Vector3 vNewCamPos = targetPoint - fPullback * vCurCamFWXZ;
            Vector3 vNewTargetPos = vNewCamPos + fPullback * vCurCamFWXZ;
            camera.GetComponent<CameraAnimator>().Teleport_Level(vNewCamPos, vNewTargetPos);
        }


        // vertical-offset level teleport
        //   assumption is that target point is on ground or ceiling, and we want to offset vertically.
        //   To do that we have to first reset orbit around the hit point. 
        //   One small problem here is that the output target pos is really arbitrary. perhaps we should
        //   cast another ray into the scene in the forward direction?
        static public void Teleport_VerticalNormalOffset(Camera camera, FScene scene, 
            Vector3 targetPoint, Vector3 targetNormal, float fNewTargetDist)
        {
            fNewTargetDist *= scene.GetSceneScale();

            Vector3 vCurCamFW = camera.transform.forward;
            Vector3 vCurCamFWXZ = new Vector3(vCurCamFW[0], 0.0f, vCurCamFW[2]).normalized;
            Vector3 vSceneUp = scene.RootGameObject.transform.up;

            // when we are offsetting up we use the DefaultHeightInM value to set eye level,
            // when offsetting down we use half that distance (arbitrary)
            float fUp = Vector3.Dot(targetNormal, vSceneUp) > 0 ? 1.0f : -0.5f;
            float fHeight = camera.transform.position[1] * scene.GetSceneScale();
            Vector3 vNewCamPos = targetPoint + fUp * fHeight * Vector3.up;
            Vector3 vNewTargetPos = vNewCamPos + fNewTargetDist * vCurCamFWXZ;
            camera.GetComponent<CameraAnimator>().Teleport_Level(vNewCamPos, vNewTargetPos, targetPoint);
        }


        //   we orbit view to be level, then again so that the normal of the hit point points towards
        //   the camera. Then we pull back along that direction. Unless we want to face away,
        //   then we rotate an extra 180 degrees, and set the target out by the pullback distance
        static public void Teleport_LevelNormalOffset(Camera camera, FScene scene, 
            Vector3 targetPoint, Vector3 targetNormal, float fPullback, bool bFaceAwayFromSurface = false)
        {
            fPullback *= scene.GetSceneScale();

            Vector3 vCurCamFW = camera.transform.forward;
            Vector3 vCurCamFWXZ = new Vector3(vCurCamFW[0], 0.0f, vCurCamFW[2]).normalized;
            Vector3 vHitNormalXZ = new Vector3(targetNormal[0], 0.0f, targetNormal[2]).normalized;

            float fFace = bFaceAwayFromSurface ? -1.0f : 1.0f;
            float fRotate = Vector3.Angle(-vCurCamFWXZ, fFace * vHitNormalXZ);
            float fSign = Vector3.Cross(vCurCamFWXZ, fFace * vHitNormalXZ)[1] > 0 ? -1 : 1;
            fRotate *= fSign;
            // now we use cam instead of normal here because we rotated normal to face cam!
            Vector3 vNewCamPos = targetPoint - fFace * fPullback * vCurCamFWXZ;        
            Vector3 vNewTargetPos = (bFaceAwayFromSurface) ? vNewCamPos + fPullback * vCurCamFWXZ : targetPoint;
            camera.GetComponent<CameraAnimator>().Teleport_Level(vNewCamPos, vNewTargetPos, targetPoint, fRotate);
        }


        // use normal angle w/ up vector to decide if we should offset vertically or horizontally 
        //   from hit point (threshold hardcoded to 45 degrees right now)
        static public void Teleport_Normal_Auto(Camera camera, FScene scene,
            Vector3 targetPoint, Vector3 targetNormal, float fPullback)
        {
            float fDot = Mathf.Abs(Vector3.Dot(targetNormal, scene.RootGameObject.transform.up));
            float fAngle = Mathf.Acos(fDot) * Mathf.Rad2Deg;
            if (fAngle > 45.0f)
                Teleport_LevelNormalOffset(camera, scene, targetPoint, targetNormal, fPullback, false);
            else
                Teleport_VerticalNormalOffset(camera, scene, targetPoint, targetNormal, fPullback);
        }

    }
}
