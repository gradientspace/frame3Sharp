using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DG.Tweening;

namespace f3
{
    public class CameraAnimator : MonoBehaviour
    {
        public Camera UseCamera { get; set; }
        public FScene UseScene { get; set; }

        public Vector3 CameraTarget
        {
            get { return UseCamera.GetTarget();  }
            set { UseCamera.SetTarget(value); }
        }

        GameObject fadeObject { get; set; }

        public CameraAnimator()
        {
        }

        public void Start()
        {
            fadeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //fadeObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            fadeObject.GetComponent<MeshRenderer>().material = MaterialUtil.CreateFlatMaterial(Color.black, 0.0f);
            fadeObject.SetName("fade_sphere");
            UnityUtil.ReverseMeshOrientation(fadeObject.GetMesh());
            fadeObject.transform.SetParent(UseCamera.transform, false);
            fadeObject.SetLayer(FPlatform.HUDLayer);
        }

        public void PanFocus(Vector3 v)
        {
            // figure out the pan that we would apply to camera, then apply the delta to the scene
            Vector3 curPos = UseCamera.transform.position;
            Vector3 curDir = UseCamera.transform.forward;
            float fDist = Vector3.Dot((v - curPos), curDir);
            Vector3 newPos = v - fDist * curDir;
            Vector3 delta = curPos - newPos;

            StartCoroutine(
                SmoothTranslate(UseScene.RootGameObject.transform.position + delta, 0.5f));
            StartCoroutine(
                SmoothMoveTarget(v+delta, 0.1f));
        }


        // set view position and target location explicitly, during a dip-to-black transition
        public void Teleport(Vector3 vMoveToLocation, Vector3 vNewTargetLocation)
        {
            // figure out the pan that we would apply to camera, then apply the delta to the scene
            Vector3 curPos = UseCamera.transform.position;
            Vector3 newPos = vMoveToLocation;
            Vector3 delta = curPos - newPos;

            StartCoroutine(
                SmoothDipToBlack(0.75f));
            StartCoroutine(
                SmoothTranslate(UseScene.RootGameObject.transform.position + delta, 0.75f));
            StartCoroutine(
                SmoothMoveTarget(vNewTargetLocation+delta, 0.1f));
        }



        // set the view position and target location explicitly while also resetting the
        //  scene to be level (ie scene up is y axis), during a dip-to-black transition.
        //  Assumes that moveto and newtarget are lying in an xz-plane...
        public void Teleport_Level(Vector3 vMoveToLocation, Vector3 vNewTargetLocation)
        {
            StartCoroutine(
                Teleport_Level_Helper(vMoveToLocation, vNewTargetLocation, vNewTargetLocation, 0.0f, 0.75f));
        }
        public void Teleport_Level(Vector3 vMoveToLocation, Vector3 vNewTargetLocation, Vector3 vPivotAround, float fLevelRotateAngle = 0)
        {
            StartCoroutine(
                Teleport_Level_Helper(vMoveToLocation, vNewTargetLocation, vPivotAround, fLevelRotateAngle, 0.75f));
        }
        IEnumerator Teleport_Level_Helper(Vector3 vMoveToLocation, Vector3 vNewTargetLocation, Vector3 vPivotAround, float fLevelRotateAngle, float duration)
        {
            yield return null;
            Sequence mySequence = DOTween.Sequence();
            mySequence.Append(
                fadeObject.GetComponent<MeshRenderer>().material.DOFade(1.0f, duration / 3.0f).OnComplete(() => {
                    GameObject sceneGO = UseScene.RootGameObject;

                    // set target to new target location explicitly, then reset orbit altitude.
                    // now we are level with ground but not at location
                    CameraTarget = vPivotAround;
                    UseCamera.Manipulator().ResetSceneOrbit(UseScene, false, true, true);

                    // add planar rotation if we need it
                    if (fLevelRotateAngle != 0)
                        UseCamera.Manipulator().SceneOrbit(UseScene, UseCamera, fLevelRotateAngle, 0.0f);

                    // figure out the pan that we would apply to camera, opposite is pan to scene
                    Vector3 delta = UseCamera.transform.position - vMoveToLocation;
                    sceneGO.transform.position += delta;
                    // also have to shift scene target pos
                    CameraTarget = vNewTargetLocation + delta;

                    UseCamera.gameObject.GetComponent<CameraTarget>().ShowTarget = true;
                }));
            mySequence.AppendInterval(duration / 3.0f);
            mySequence.Append(
                fadeObject.GetComponent<MeshRenderer>().material.DOFade(0.0f, duration / 3.0f));

            // add a delay before we hide target
            mySequence.AppendInterval(1.0f);
            mySequence.OnComplete(() => {
                UseCamera.gameObject.GetComponent<CameraTarget>().ShowTarget = false;
            });

        }



        IEnumerator SmoothTranslateRotate(Vector3 toPosition, Quaternion toOrientation, float duration)
        {
            yield return null;
            UseCamera.gameObject.GetComponent<CameraTarget>().ShowTarget = true;
            UseScene.RootGameObject.transform.DOMove(toPosition, duration).OnComplete(
                () => { UseCamera.gameObject.GetComponent<CameraTarget>().ShowTarget = false; });
            UseScene.RootGameObject.transform.DORotateQuaternion(toOrientation, duration);
        }

        IEnumerator SmoothTranslate(Vector3 to, float duration)
        {
            yield return null;
            UseCamera.gameObject.GetComponent<CameraTarget>().ShowTarget = true;
            UseScene.RootGameObject.transform.DOMove(to, duration).OnComplete(
                () => { UseCamera.gameObject.GetComponent<CameraTarget>().ShowTarget = false; });
        }
        IEnumerator SmoothMoveTarget(Vector3 to, float duration)
        {
            yield return null;
            DOTween.To(() => CameraTarget, x => CameraTarget = x, to, duration);
        }


        IEnumerator SmoothDipToBlack(float duration)
        {
            yield return null;
            Sequence mySequence = DOTween.Sequence();
            mySequence.Append(
                fadeObject.GetComponent<MeshRenderer>().material.DOFade(1.0f, duration / 4.0f));
            mySequence.AppendInterval(duration / 2.0f);
            mySequence.Append(
                fadeObject.GetComponent<MeshRenderer>().material.DOFade(0.0f, duration / 4.0f));
        }






        public void DoActionDuringDipToBlack(Action fadeAction, float fDuration)
        {
            StartCoroutine(FadeTransitionHelper(fadeAction, fDuration));
        }


        IEnumerator FadeTransitionHelper(Action fadeAction, float duration)
        {
            yield return null;
            Sequence mySequence = DOTween.Sequence();
            mySequence.Append(
                fadeObject.GetComponent<MeshRenderer>().material.DOFade(1.0f, duration / 3.0f).OnComplete(() => {
                    // once we have faded we can do action
                    fadeAction();
                }));
            // wait for 1/3
            mySequence.AppendInterval(duration / 3.0f);
            // fade back in
            mySequence.Append(
                fadeObject.GetComponent<MeshRenderer>().material.DOFade(0.0f, duration / 3.0f));
        }



    }
}
