using System;
using UnityEngine;
using g3;

namespace f3
{
	//
	// this Widget implements rotation around an axis
	//
	public class AxisRotationWidget : Standard3DWidget
    {
		int nRotationAxis;

		public AxisRotationWidget(int nFrameAxis)
		{
			nRotationAxis = nFrameAxis;
		}

		// stored frames from target used during click-drag interaction
		Frame3f rotateFrameL;		// local-space frame 
		Frame3f rotateFrameW;		// world-space frame
		Vector3 rotateAxisW;		// world-space axis we are rotating around (redundant...)

		// computed values during interaction
		Frame3f raycastFrame;		// camera-facing plane containing translateAxisW
		float fRotateStartAngle;

		public override bool BeginCapture(ITransformable target, Ray worldRay, UIRayHit hit)
		{
			// save local and world frames
			rotateFrameL = target.GetLocalFrame (CoordSpace.ObjectCoords);
			rotateFrameW = target.GetLocalFrame (CoordSpace.WorldCoords);
			rotateAxisW = rotateFrameW.GetAxis (nRotationAxis);

			// save angle of hitpos in 2D plane perp to rotateAxis, so we can find delta-angle later
			Vector3f vWorldHitPos = hit.hitPos;
			Vector3f dv = vWorldHitPos - rotateFrameW.Origin;
			int iX = (nRotationAxis + 1) % 3;
			int iY = (nRotationAxis + 2) % 3;
			float fX = Vector3f.Dot( dv, rotateFrameW.GetAxis(iX) );
			float fY = Vector3f.Dot( dv, rotateFrameW.GetAxis(iY) );
			fRotateStartAngle = (float)Math.Atan2 (fY, fX);

			// construct plane we will ray-intersect with in UpdateCapture()
			raycastFrame = new Frame3f( vWorldHitPos, rotateAxisW );

			return true;
		}

		public override bool UpdateCapture(ITransformable target, Ray worldRay)
		{
			// ray-hit with plane perpendicular to rotateAxisW
			Vector3f planeHit = raycastFrame.RayPlaneIntersection (worldRay.origin, worldRay.direction, 2);

			// find angle of hitpos in 2D plane perp to rotateAxis, and compute delta-angle
			Vector3f dv = planeHit - rotateFrameW.Origin;
			int iX = (nRotationAxis + 1) % 3;
			int iY = (nRotationAxis + 2) % 3;
			float fX = Vector3.Dot( dv, rotateFrameW.GetAxis(iX) );
			float fY = Vector3.Dot( dv, rotateFrameW.GetAxis(iY) );
			float fNewAngle = (float)Math.Atan2 (fY, fX);
			float fDeltaAngle = (fNewAngle - fRotateStartAngle);

			// construct new frame for target that is rotated around axis
			Vector3f rotateAxisL = rotateFrameL.GetAxis(nRotationAxis);
			Quaternionf q = Quaternion.AngleAxis(fDeltaAngle * Mathf.Rad2Deg, rotateAxisL );
			Frame3f newFrame = rotateFrameL;
			newFrame.Rotation = q * newFrame.Rotation;		// order matters here!

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

