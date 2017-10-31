using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public class StableSurfaceFrame
    {
        public Frame3f CurrentFrame
        {
            get { return lastHitF; }
        }


        public int NormalAxis = 1;
        public int UpAxis = 2;
        public FScene TargetScene = null;




        Frame3f lastHitF;
        SceneObject lastHitObject;

        public void Initialize(Frame3f fStart)
        {
            lastHitF = fStart;
            lastHitObject = null;
        }


        public void Update(AnyRayHit hit)
        {
            // as we drag object we will align Y with hit surface normal, but
            // we also want to constrain rotation so it is stable. Hence, we are
            // going to use world or local frame of target object to stabilize
            // rotation around normal. 
            Frame3f hitF = TargetScene.SceneFrame;
            Vector3f targetAxis = hitF.GetAxis(1);
            if (hit.hitSO is SceneObject)
                hitF = hit.hitSO.GetLocalFrame(CoordSpace.WorldCoords);
            bool bUseLocal =
                (TargetScene.Context.TransformManager.ActiveFrameType == FrameType.LocalFrame);
            if (bUseLocal && hit.hitSO is SceneObject) {
                hitF = hit.hitSO.GetLocalFrame(CoordSpace.WorldCoords);
                targetAxis = hitF.GetAxis(1);
            }
            // if normal is parallel to target, this would become unstable, so use another axis
            if (Vector3f.Dot(targetAxis, hit.hitNormal) > 0.99f)
                targetAxis = hitF.GetAxis(0);

            if (lastHitObject == null || hit.hitSO != lastHitObject) {
                lastHitF = new Frame3f(hit.hitPos, hit.hitNormal, NormalAxis);
                lastHitF.ConstrainedAlignAxis(UpAxis, targetAxis, lastHitF.GetAxis(NormalAxis));
            } else {
                lastHitF.Origin = hit.hitPos;
                lastHitF.AlignAxis(NormalAxis, hit.hitNormal);
                lastHitF.ConstrainedAlignAxis(UpAxis, targetAxis, lastHitF.GetAxis(NormalAxis));
            }
            lastHitObject = hit.hitSO;
        }
    }
}
