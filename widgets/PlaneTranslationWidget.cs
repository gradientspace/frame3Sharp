using System;
using UnityEngine;
using g3;

namespace f3
{
	//
	// this Widget implements translation constrained to a plane
	// 
	public class PlaneTranslationWidget : Standard3DWidget
    {
        // scaling distance along axis multiplied by this value. 
        // By default click-point stays under cursor (ie direct manipulation), but 
        // you can slow it down by setting this to return a smaller value.
        // Also you can use this to compensate for global scene scaling (hence the function)
        public Func<float> TranslationScaleF = () => { return 1.0f; };


        public Func<Frame3f, int, float, float> DeltaDistanceConstraintF = null;


        int nTranslationPlaneNormal;

		public PlaneTranslationWidget(int nNormalAxis)
		{
			nTranslationPlaneNormal = nNormalAxis;
		}

		// stored frames from target used during click-drag interaction
		Frame3f translateFrameL;		// local-space frame
		Frame3f translateFrameW;		// world-space frame

		// computed values during interaction
		Vector3f vInitialHitPos;		// initial hit position in frame

		public override bool BeginCapture(ITransformable target, Ray3f worldRay, UIRayHit hit)
		{
			// save local and world frames
			translateFrameL = target.GetLocalFrame (CoordSpace.ObjectCoords);
			translateFrameW = target.GetLocalFrame (CoordSpace.WorldCoords);

			// save initial hitpos in translation plane
			vInitialHitPos = translateFrameW.RayPlaneIntersection(worldRay.Origin, worldRay.Direction, nTranslationPlaneNormal);

			return true;
		}

		public override bool UpdateCapture(ITransformable target, Ray3f worldRay)
		{
			// ray-hit with world-space translation plane
			Vector3f planeHit = translateFrameW.RayPlaneIntersection(worldRay.Origin, worldRay.Direction, nTranslationPlaneNormal);
			int e0 = (nTranslationPlaneNormal + 1) % 3;
			int e1 = (nTranslationPlaneNormal + 2) % 3;

			// construct delta in world space and project into frame coordinates
			Vector3f delta = (planeHit - vInitialHitPos);
            delta *= TranslationScaleF();
			float dx = Vector3f.Dot (delta, translateFrameW.GetAxis (e0));
			float dy = Vector3f.Dot (delta, translateFrameW.GetAxis (e1));
            if ( DeltaDistanceConstraintF != null ) {
                dx = DeltaDistanceConstraintF(translateFrameL, e0, dx);
                dy = DeltaDistanceConstraintF(translateFrameL, e1, dy);
            }

            // construct new local frame translated along plane axes
            Frame3f newFrame = translateFrameL;
			newFrame.Origin += dx*translateFrameL.GetAxis(e0) + dy*translateFrameL.GetAxis(e1);

			// update target
			target.SetLocalFrame (newFrame, CoordSpace.ObjectCoords);

			return true;
		}

        public override bool EndCapture(ITransformable target)
        {
            return true;
        }
    }
}

