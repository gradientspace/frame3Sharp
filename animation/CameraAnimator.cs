using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DG.Tweening;
using g3;

namespace f3
{
    public class CameraAnimator : MonoBehaviour
    {
        public fCamera UseCamera { get; set; }
        public FScene UseScene { get; set; }

        public bool ShowTargetDuringAnimations = true;

        public Vector3f CameraTarget
        {
            get { return UseCamera.GetTarget();  }
            set { UseCamera.SetTarget(value); }
        }

        fGameObject fadeObject { get; set; }

        public CameraAnimator()
        {
        }

        public void Start()
        {
            fadeObject = new fGameObject( GameObject.CreatePrimitive(PrimitiveType.Sphere), FGOFlags.NoFlags );
            //fadeObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            fadeObject.SetMaterial(MaterialUtil.CreateFlatMaterial(Color.black, 0.0f), true);
            fadeObject.SetName("fade_sphere");
            UnityUtil.ReverseMeshOrientation(fadeObject.GetMesh());
            fadeObject.SetParent(UseCamera.GameObject(), false);
            fadeObject.SetLayer(FPlatform.HUDLayer);
        }

        
        // should not use this anymore...
        public void PanFocus(Vector3f focusPoint, CoordSpace eSpace = CoordSpace.WorldCoords, float duration = 0.5f)
        {
            Vector3f focusPointW = (eSpace == CoordSpace.WorldCoords) ? focusPoint : UseScene.ToWorldP(focusPoint);

            // figure out the pan that we would apply to camera, then apply the delta to the scene
            Vector3f curPos = UseCamera.GetPosition();
            Vector3f curDir = UseCamera.GetWorldFrame().Z;
            float fDist = Vector3.Dot((focusPointW - curPos), curDir);
            Vector3f newPos = focusPointW - fDist * curDir;
            Vector3f delta = curPos - newPos;

            StartCoroutine(
                SmoothTranslate(UseScene.RootGameObject.GetPosition() + delta, duration));
            StartCoroutine(
                SmoothMoveTarget(focusPointW + delta, duration / 10.0f));
        }



        /// <summary>
        /// Animate camera so that focusPoint moves to center of camera
        /// Camera target is also set to focusPoint
        /// </summary>
        public void AnimatePanFocus(Vector3f focusPoint, CoordSpace eSpace, float duration)
        {
            if (duration > 0 && ShowTargetDuringAnimations)
                UseCamera.SetTargetVisible(true);

            Vector3f focusPointS = (eSpace == CoordSpace.WorldCoords) ? UseScene.ToSceneP(focusPoint) : focusPoint;
            Vector3f startFocusS = UseScene.ToSceneP(UseCamera.GetTarget());
            Action<float> tweenF = (t) => {
                Vector3f newTargetS = Vector3f.Lerp(startFocusS, focusPointS, t);
                UseCamera.Manipulator().PanFocusOnScenePoint(UseScene, UseCamera, newTargetS);
            };

            if (duration > 0) {
                TweenAnimator anim = new TweenAnimator(tweenF, duration) {
                    OnCompletedF = () => { UseCamera.SetTargetVisible(false); }
                };
                UseScene.ObjectAnimator.Register(anim);
            } else
                tweenF(1.0f);
        }



        /// <summary>
        /// Animate camera so that focusPoint moves to center of camera, at distance distanceW along cam.Forward
        /// Camera target is also set to focusPoint
        /// </summary>
        public void AnimatePanZoomFocus(Vector3f focusPoint, CoordSpace eSpace, float distanceW, float duration)
        {
            if (duration > 0 && ShowTargetDuringAnimations)
                UseCamera.SetTargetVisible(true);

            Vector3f focusPointS = (eSpace == CoordSpace.WorldCoords) ? UseScene.ToSceneP(focusPoint) : focusPoint;
            Vector3f startFocusS = UseScene.ToSceneP(UseCamera.GetTarget());
            float startDistW = UseCamera.GetPosition().Distance(UseCamera.GetTarget());

            Action<float> tweenF = (t) => {
                float smooth_t = MathUtil.WyvillRise01(t);
                Vector3f newTargetS = Vector3f.Lerp(startFocusS, focusPointS, smooth_t);
                UseCamera.Manipulator().PanFocusOnScenePoint(UseScene, UseCamera, newTargetS);

                float curDist = UseCamera.GetPosition().Distance(UseCamera.GetTarget());
                float toDist = MathUtil.Lerp(startDistW, distanceW, smooth_t);
                float dolly = toDist - curDist;
                UseCamera.Manipulator().SceneZoom(UseScene, UseCamera, -dolly);
            };

            if (duration > 0) {
                TweenAnimator anim = new TweenAnimator(tweenF, duration) {
                    OnCompletedF = () => { UseCamera.SetTargetVisible(false); }
                };
                UseScene.ObjectAnimator.Register(anim);
            } else
                tweenF(1.0f);
        }


        /// <summary>
        /// Animate camera so that centerPt moves to center of camera, and width is visible.
        /// Camera target is also set to centerPt
        /// </summary>
        public void AnimateFitWidthToView(Vector3f centerPt, float width, CoordSpace eSpace, float duration)
        {
            if (eSpace != CoordSpace.WorldCoords)
                width = UseScene.ToWorldDimension(width);
            float fFitDistW = UseCamera.Manipulator().GetFitWidthCameraDistance(width);
            Vector3f focusPointW = (eSpace == CoordSpace.WorldCoords) ? centerPt : UseScene.ToWorldP(centerPt);
            AnimatePanZoomFocus(focusPointW, CoordSpace.WorldCoords, fFitDistW, duration);
        }


        /// <summary>
        /// Animate camera so that centerPt moves to center of camera, and height is visible.
        /// Camera target is also set to centerPt
        /// </summary>
        public void AnimateFitHeightToView(Vector3f centerPt, float height, CoordSpace eSpace, float duration)
        {
            if (eSpace != CoordSpace.WorldCoords)
                height = UseScene.ToWorldDimension(height);
            float fFitDistW = UseCamera.Manipulator().GetFitHeightCameraDistance(height);
            Vector3f focusPointW = (eSpace == CoordSpace.WorldCoords) ? centerPt : UseScene.ToWorldP(centerPt);
            AnimatePanZoomFocus(focusPointW, CoordSpace.WorldCoords, fFitDistW, duration);
        }



        // set view position and target location explicitly, during a dip-to-black transition
        public void Teleport(Vector3f vMoveToLocation, Vector3f vNewTargetLocation)
        {
            // figure out the pan that we would apply to camera, then apply the delta to the scene
            Vector3f curPos = UseCamera.GetPosition();
            Vector3f newPos = vMoveToLocation;
            Vector3f delta = curPos - newPos;

            StartCoroutine(
                SmoothDipToBlack(0.75f));
            StartCoroutine(
                SmoothTranslate(UseScene.RootGameObject.GetPosition() + delta, 0.75f));
            StartCoroutine(
                SmoothMoveTarget(vNewTargetLocation+delta, 0.1f));
        }


        public void AnimateOrbitTo(float toAzimuth, float toAltitude, float duration = 0.25f)
        {
            StartCoroutine(
                SmoothOrbitTo(toAzimuth, toAltitude, duration));
        }


        /// <summary>
        /// Turntable-rotate to set azimuth/altitude, while also re-centering camera on target at given distance.
        /// </summary>
        public void AnimateOrbitZoomFocusTo(float toAzimuth, float toAltitude, float toDistance, Vector3f toTargetS, float duration = 0.25f)
        {
            if (duration > 0 && ShowTargetDuringAnimations)
                UseCamera.SetTargetVisible(true);

            Vector3f startTargetS = UseScene.ToSceneP(UseCamera.GetTarget());
            float startAltitude = UseCamera.Manipulator().TurntableAltitudeD;
            float startAzimuth = UseCamera.Manipulator().TurntableAzimuthD;

            Action<float> tweenF = (t) => {
                Vector3f newTargetS = Vector3f.Lerp(startTargetS, toTargetS, t);
                //Vector3f newTargetW = UseScene.ToWorldP(newTargetS);
                //UseCamera.Manipulator().ScenePanFocus(UseScene, UseCamera, newTargetW, false);
                UseCamera.Manipulator().PanFocusOnScenePoint(UseScene, UseCamera, newTargetS);

                float alt = MathUtil.Lerp(startAltitude, toAltitude, t);
                float az = MathUtil.Lerp(startAzimuth, toAzimuth, t);
                UseCamera.Manipulator().SceneOrbit(UseScene, UseCamera, az, alt, true);

                float curDist = UseCamera.GetPosition().Distance(UseCamera.GetTarget());
                float toDist = MathUtil.SmoothInterp(curDist, toDistance, t);
                float dolly = toDist - curDist;
                UseCamera.Manipulator().SceneZoom(UseScene, UseCamera, -dolly);
            };

            if (duration > 0) {
                TweenAnimator anim = new TweenAnimator(tweenF, duration) {
                    OnCompletedF = () => { UseCamera.SetTargetVisible(false); }
                };
                UseScene.ObjectAnimator.Register(anim);
            } else
                tweenF(1.0f);
        }





        /// <summary>
        /// Tumble scene to given orientation, around current target point
        /// [TODO] currently this does some weird stuff, because distance from target varies...
        /// </summary>
        public void AnimateTumbleTo(Quaternionf toOrientation, float duration = 0.25f)
        {
            if (duration > 0 && ShowTargetDuringAnimations)
                UseCamera.SetTargetVisible(true);

            Vector3f startTargetS = UseScene.ToSceneP(UseCamera.GetTarget());
            Frame3f startF = UseScene.SceneFrame;

            Action<float> tweenF = (t) => {
                // update rotation
                Quaternionf rot = Quaternion.Slerp(startF.Rotation, toOrientation, t);
                UseScene.SceneFrame = new Frame3f(startF.Origin, rot);

                // stay on target
                UseCamera.Manipulator().PanFocusOnScenePoint(UseScene, UseCamera, startTargetS);
            };

            if (duration > 0) {
                TweenAnimator anim = new TweenAnimator(tweenF, duration) {
                    OnCompletedF = () => { UseCamera.SetTargetVisible(false); }
                };
                UseScene.ObjectAnimator.Register(anim);
            } else
                tweenF(1.0f);
        }



        /// <summary>
        /// Tumble scene to given orientation, while also re-centering camera on target at given distance.
        /// </summary>
        public void AnimateTumbleZoomFocusTo(Quaternionf toOrientation, float toDistance, Vector3f toTargetS, float duration = 0.25f)
        {
            if (duration > 0 && ShowTargetDuringAnimations)
                UseCamera.SetTargetVisible(true);

            Vector3f startTargetS = UseScene.ToSceneP(UseCamera.GetTarget());
            Frame3f startF = UseScene.SceneFrame;

            Action<float> tweenF = (t) => {
                Vector3f newTargetS = Vector3f.Lerp(startTargetS, toTargetS, t);
                UseCamera.Manipulator().PanFocusOnScenePoint(UseScene, UseCamera, newTargetS);

                Quaternionf rot = Quaternion.Slerp(startF.Rotation, toOrientation, t);
                UseScene.RootGameObject.SetLocalRotation(rot);

                float curDist = UseCamera.GetPosition().Distance(UseCamera.GetTarget());
                float toDist = MathUtil.SmoothInterp(curDist, toDistance, t);
                float dolly = toDist - curDist;
                UseCamera.Manipulator().SceneZoom(UseScene, UseCamera, -dolly);
            };

            if (duration > 0) {
                TweenAnimator anim = new TweenAnimator(tweenF, duration) {
                    OnCompletedF = () => { UseCamera.SetTargetVisible(false); }
                };
                UseScene.ObjectAnimator.Register(anim);
            } else
                tweenF(1.0f);
        }







        // set the view position and target location explicitly while also resetting the
        //  scene to be level (ie scene up is y axis), during a dip-to-black transition.
        //  Assumes that moveto and newtarget are lying in an xz-plane...
        public void Teleport_Level(Vector3f vMoveToLocation, Vector3f vNewTargetLocation)
        {
            StartCoroutine(
                Teleport_Level_Helper(vMoveToLocation, vNewTargetLocation, vNewTargetLocation, 0.0f, 0.75f));
        }
        public void Teleport_Level(Vector3f vMoveToLocation, Vector3f vNewTargetLocation, Vector3f vPivotAround, float fLevelRotateAngle = 0)
        {
            StartCoroutine(
                Teleport_Level_Helper(vMoveToLocation, vNewTargetLocation, vPivotAround, fLevelRotateAngle, 0.75f));
        }
        IEnumerator Teleport_Level_Helper(Vector3f vMoveToLocation, Vector3f vNewTargetLocation, Vector3f vPivotAround, float fLevelRotateAngle, float duration)
        {
            yield return null;
            Sequence mySequence = DOTween.Sequence();
            mySequence.Append(
                ((Material)fadeObject.GetMaterial()).DOFade(1.0f, duration / 3.0f).OnComplete(() => {
                    fGameObject sceneGO = UseScene.RootGameObject;

                    // set target to new target location explicitly, then reset orbit altitude.
                    // now we are level with ground but not at location
                    CameraTarget = vPivotAround;
                    UseCamera.Manipulator().ResetSceneOrbit(UseScene, false, true, true);

                    // add planar rotation if we need it
                    if (fLevelRotateAngle != 0)
                        UseCamera.Manipulator().SceneOrbit(UseScene, UseCamera, fLevelRotateAngle, 0.0f);

                    // figure out the pan that we would apply to camera, opposite is pan to scene
                    Vector3f delta = UseCamera.GetPosition() - vMoveToLocation;
                    sceneGO.SetPosition(sceneGO.GetPosition() + delta);
                    // also have to shift scene target pos
                    CameraTarget = vNewTargetLocation + delta;

                    UseCamera.SetTargetVisible(true);
                }));
            mySequence.AppendInterval(duration / 3.0f);
            mySequence.Append(
                ((Material)fadeObject.GetMaterial()).DOFade(0.0f, duration / 3.0f));

            // add a delay before we hide target
            mySequence.AppendInterval(1.0f);
            mySequence.OnComplete(() => {
                UseCamera.SetTargetVisible(false);
            });

        }



        IEnumerator SmoothTranslateRotate(Vector3 toPosition, Quaternion toOrientation, float duration)
        {
            yield return null;
            UseCamera.SetTargetVisible(true);
            ((GameObject)UseScene.RootGameObject).transform.DOMove(toPosition, duration).OnComplete(
                () => { UseCamera.SetTargetVisible(false); });
            ((GameObject)UseScene.RootGameObject).transform.DORotateQuaternion(toOrientation, duration);
        }

        IEnumerator SmoothTranslate(Vector3 to, float duration)
        {
            yield return null;
            UseCamera.SetTargetVisible(true);
            ((GameObject)UseScene.RootGameObject).transform.DOMove(to, duration).OnComplete(
                () => { UseCamera.SetTargetVisible(false); });
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
                ((Material)fadeObject.GetMaterial()).DOFade(1.0f, duration / 4.0f));
            mySequence.AppendInterval(duration / 2.0f);
            mySequence.Append(
                ((Material)fadeObject.GetMaterial()).DOFade(0.0f, duration / 4.0f));
        }



        IEnumerator SmoothOrbitTo(float azimuth, float altitude, float duration)
        {
            yield return null;

            var manip = UseCamera.Manipulator();
            Vector2f target = new Vector2f(azimuth, altitude);

            DOTween.To(() => { return new Vector2f(manip.TurntableAzimuthD, manip.TurntableAltitudeD); },
                (v) => { manip.SceneOrbit(UseScene, UseCamera, v.x, v.y, true); },
                target, duration);
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
                ((Material)fadeObject.GetMaterial()).DOFade(1.0f, duration / 3.0f).OnComplete(() => {
                    // once we have faded we can do action
                    fadeAction();
                }));
            // wait for 1/3
            mySequence.AppendInterval(duration / 3.0f);
            // fade back in
            mySequence.Append(
                ((Material)fadeObject.GetMaterial()).DOFade(0.0f, duration / 3.0f));
        }



    }
}
