using System;
using UnityEngine;
using g3;

namespace f3
{
    //
    // this Widget implements uniform scaling
    // 
    public class UniformScaleWidget : Standard3DTransformWidget
    {
        // scaling distance along axis multiplied by this value. 
        // By default click-point stays under cursor (ie direct manipulation), but 
        // you can slow it down by setting this to return a smaller value.
        // Also you can use this to compensate for global scene scaling (hence the function)
        public Func<float> ScaleMultiplierF = () => { return 1.0f; };

        //ISpaceConversion conversion;
        Camera activeCamera;        // should really not hold this, but we need it for now

        public UniformScaleWidget(Camera cam)
        {
            activeCamera = cam;
        }

        // stored frames from target used during click-drag interaction
        Frame3f scaleFrameW;     // world-space frame of target
        Frame3f cameraFrameW;    // camera-aligned frame, but centered at scaleFrameW.origin
        Vector3f startScale;     // scale value at begin capture

        // computed values during interaction
        Vector3f vInitialHitPos;     // initial hit position in frame

        public override bool BeginCapture(ITransformable target, Ray3f worldRay, UIRayHit hit)
        {
            if (target.SupportsScaling == false)
                return false;

            // save local and world frames
            scaleFrameW = target.GetLocalFrame(CoordSpace.WorldCoords);
            cameraFrameW = new Frame3f(scaleFrameW.Origin, activeCamera.transform.rotation);
            startScale = target.GetLocalScale();

            // save initial hitpos in plane
            vInitialHitPos = cameraFrameW.RayPlaneIntersection(worldRay.Origin, worldRay.Direction, 2);

            return true;
        }

        public override bool UpdateCapture(ITransformable target, Ray3f worldRay)
        {
            // ray-hit with world-space translation plane
            Vector3f planeHit = cameraFrameW.RayPlaneIntersection(worldRay.Origin, worldRay.Direction, 2);

            // construct delta in world space and project into frame coordinates
            Vector3f delta = (planeHit - vInitialHitPos);
            delta *= ScaleMultiplierF();
            //float dx = Vector3f.Dot(delta, cameraFrameW.GetAxis(0));
            float dy = Vector3f.Dot(delta, cameraFrameW.GetAxis(1));
            float scaleDelta = 1.0f + dy;
            float scaleFactor = Mathf.Clamp(scaleDelta, 0.1f, 1000.0f);

            // update target
            target.SetLocalScale(startScale * scaleFactor);

            return true;
        }

        public override bool EndCapture(ITransformable target)
        {
            return true;
        }

        public override void Disconnect()
        {
            RootGameObject.Destroy();
        }

        public override bool CheckVisibility(ref Frame3f curFrameW, ref Vector3d eyePos) {
            return true;
        }
    }
}

