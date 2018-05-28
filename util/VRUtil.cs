using System;
using g3;

namespace f3
{
	public class VRUtil
	{
		protected VRUtil ()
		{
		}


        public static float PlaneAngle(Vector3f a, Vector3f b, int nPlaneNormalIdx = 1)
        {
            a[nPlaneNormalIdx] = b[nPlaneNormalIdx] = 0.0f;
            a.Normalize();
            b.Normalize();
            return Vector3f.AngleD(a, b);
        }
        public static float PlaneAngleSigned(Vector3f vFrom, Vector3f vTo, int nPlaneNormalIdx = 1)
        {
            vFrom[nPlaneNormalIdx] = vTo[nPlaneNormalIdx] = 0.0f;
            vFrom.Normalize();
            vTo.Normalize();
            float fSign = Math.Sign(Vector3f.Cross(vFrom, vTo)[nPlaneNormalIdx]);
            float fAngle = fSign * Vector3f.AngleD(vFrom, vTo);
            return fAngle;
        }
        public static float PlaneAngleSigned(Vector3f vFrom, Vector3f vTo, Vector3f planeN)
        {
            vFrom = vFrom - Vector3f.Dot(vFrom, planeN) * planeN;
            vTo = vTo - Vector3f.Dot(vTo, planeN) * planeN;
            vFrom.Normalize();
            vTo.Normalize();
            Vector3f c = Vector3f.Cross(vFrom, vTo);
            float fSign = Math.Sign(Vector3f.Dot(c, planeN));
            float fAngle = fSign * Vector3f.AngleD(vFrom, vTo);
            return fAngle;
        }

        public static Vector3f AngleLerp(Vector3f a, Vector3f b, float t)
        {
            // [TODO] should be doing a rotation!!
            Vector3f v = (1.0f - t) * a + (t) * b;
            return v.Normalized;
        }

        public static float ArcLengthDeg(float fRadius, float fAngleSpanDeg)
        {
            return (float)(fRadius * fAngleSpanDeg * (Math.PI / 180.0));
        }


        /// <summary>
        /// Return a world-space width that corresponds to the given visual angle at the given distance,
        /// in a VR environment (ie where angle is physical and not connected to pixels at all)
        /// </summary>
        public static float GetVRRadiusForVisualAngle(Vector3f vWorldPos, Vector3f vEyePos, float fAngleInDegrees)
		{
            float r = (vWorldPos - vEyePos).Length;
			double a = fAngleInDegrees * (Math.PI/180.0);
			float c = 2.0f * r * (float)Math.Sin (a / 2.0);
			return c;
		}



        /// <summary>
        /// Return a world-space width that corresponds to the given visual angle at the given distance.
        /// This is done based on an estimate of the visual angle of the window, which we get by
        /// using a standard angular-unit-per-pixel value from CSS, scaled by DPI.
        /// Note: this makes lots of assumptions, in particular assumes object is at center of screen...
        /// </summary>
        public static float Get2DRadiusForVisualAngle(Vector3f vWorldPos, Vector3f vEyePos, float fAngleInDegrees)
        {
            double angle_per_pix_deg = 0.0213;   // https://www.w3.org/TR/CSS21/syndata.html#length-units
            angle_per_pix_deg *= (96.0 / FPlatform.ScreenDPI);
            // compute visual angle of window, in width
            double screen_angle_deg = FPlatform.ScreenWidth * angle_per_pix_deg;
            screen_angle_deg = Math.Min(screen_angle_deg, 45);

            // compute 'width' of scene at given distance, for windows angle (we could get this from frustum...?)
            double depth = (vWorldPos - vEyePos).Length;
            double view_width_at_depth = depth * Math.Tan(screen_angle_deg * 0.5 * MathUtil.Deg2Rad);

            // combine to get per-degree width, which we can just multiply by desired angle
            double width_per_deg = view_width_at_depth / screen_angle_deg;
            return (float)(width_per_deg * fAngleInDegrees);
        }


        /// <summary>
        /// Return a world-space width that corresponds to the given visual angle at the given distance.
        /// Automatically switches between VR and 2D calculations as appropriate.
        /// </summary>
        public static float GetRadiusForVisualAngle(Vector3f vWorldPos, Vector3f vEyePos, float fAngleInDegrees)
        {
            if (FPlatform.IsUsingVR())
                return GetVRRadiusForVisualAngle(vWorldPos, vEyePos, fAngleInDegrees);
            else
                return Get2DRadiusForVisualAngle(vWorldPos, vEyePos, fAngleInDegrees);
        }




        // horz angle is [-180,180] where negative is to the left
        // vert angle is [-90,90] where negative is down
        public static Vector3f DirectionFromSphereCenter(float fAngleHorzDeg, float fAngleVertDeg)
        {
			float fTheta = (MathUtil.Deg2Radf * fAngleHorzDeg);
			float fPhi = (MathUtil.PIf/2.0f) - (MathUtil.Deg2Radf * fAngleVertDeg);
			float z = (float)( Math.Cos (fTheta) * Math.Sin (fPhi) );
			float x = (float)( Math.Sin (fTheta) * Math.Sin (fPhi) );
			float y = (float)Math.Cos (fPhi);
            return new Vector3f(x, y, z).Normalized;
        }

		public static Ray3f MakeRayFromSphereCenter(float fAngleHorzDeg, float fAngleVertDeg) 
		{
            return new Ray3f(Vector3f.Zero, DirectionFromSphereCenter(fAngleHorzDeg, fAngleVertDeg));
		}


        // returns radius of circle created by slicing through sphere at given Y-height
        // from http://mathworld.wolfram.com/SphericalCap.html
        public static float SphereSliceRadius(float fSphereR, float fSliceY)
        {
            fSliceY = Math.Abs(fSliceY);
            float h = fSphereR - fSliceY;
            float a = (float)Math.Sqrt(h * (2.0 * fSphereR - h));
            return a;
        }

        // returns approximate # of steps of given width that fit along sphere sliced at given Y-height
        // vert angle is [-90,90] where negative is down
        public static float HorizontalStepAngle(float fSphereR, float fVertAngle, float fStepWidth)
        {
            float fSliceY = fSphereR * (float)Math.Sin(MathUtil.Deg2Radf * fVertAngle);
            float fYR = SphereSliceRadius(fSphereR, fSliceY);

            float fStepSpanRad = 2.0f * (float)Math.Asin(fStepWidth / (2.0f * fYR));
            float fStepSpanDeg = fStepSpanRad * MathUtil.Rad2Degf;
            return fStepSpanDeg;
        }





        // try to compute a stable ray/plane intersection. when origin is at < +/- an altitude threshold,
        // use ray/line distance with view-perp line, instead of actual intersection
        public static Vector3f SafeRayPlaneIntersection(Ray3f ray, Vector3f forwardDir,
            Vector3f planeOrigin, Vector3f planeNormal, float fAngleThresh = 10.0f)
        {
            // determine if we are in unstable condition
            float fOriginAngleDeg = (float)Math.Abs(
                planeNormal.AngleD((planeOrigin - ray.Origin).Normalized));
            bool bOriginUnstable = Math.Abs(fOriginAngleDeg - 90.0f) < fAngleThresh;

            // we use ray/plane intersect if stable
            Frame3f planeF = new Frame3f(planeOrigin, planeNormal);
            Vector3f planeHit = planeF.RayPlaneIntersection(ray.Origin, ray.Direction, 2);
            if (bOriginUnstable == false) {
                return planeHit;
            }

            // if unstable, find "right" direction in plane (ie perp to forward dir, which
            //  may just be ray dir), then intersection is ray/axis closest point
            Vector3f fwDirInPlane = (forwardDir - planeNormal.Dot(forwardDir)).Normalized;
            Vector3f perpDirInPlane = Quaternionf.AxisAngleD(planeNormal, 90.0f) * fwDirInPlane;
            float fAxisDist = (float)DistLine3Ray3.MinDistanceLineParam(ray, new Line3d(planeOrigin, perpDirInPlane));
            return planeOrigin + fAxisDist * perpDirInPlane;
        }



        // figuring out a decent line width is tricky. Want to be responsive to camera
        //  pos, so line doesn't get super-thick when zoomed in. So we want to measure
        //  screen-space radius. But off-screen vertices are a problem. So, only consider
        //  vertices within a level, pointing-forward view cone (can't be actual view cone
        //  because then line thickness changes as you turn head!). 
        //
        //  Also sub-sample verts for efficiency. Probably we don't need to do this
        //  every frame...but how to distribute?
        //
        //  ***returns 0*** if we couldn't find any points in view cone
        public static float EstimateStableCurveWidth(FScene scene, 
            Frame3f curveFrameS, DCurve3 curve, float fVisualAngleDeg )
        {
            // do computations in Scene coords..."safest"?

            Vector3f camPos = scene.ActiveCamera.GetPosition();
            Vector3f camForward = scene.ActiveCamera.Forward();
            // use level-forward
            camForward[1] = 0; camForward.Normalize();

            camPos = scene.ToSceneP(camPos);
            camForward = scene.ToSceneN(camForward);

            const float ViewConeDotThresh = 0.707106f;   // 45 degrees
            int nSubSampleInc = Math.Max(2, curve.VertexCount / 10);

            float rSum = 0; int iSum = 0;
            for (int k = 0; k < curve.VertexCount; k += nSubSampleInc) {
                Vector3f vS = (Vector3f)curve.GetVertex(k);
                vS = curveFrameS.FromFrameP(vS);
                Vector3f dv = (vS - camPos).Normalized;
                if (dv.Dot(camForward) < ViewConeDotThresh)
                    continue;

                float r = VRUtil.GetVRRadiusForVisualAngle(vS, camPos, fVisualAngleDeg);
                rSum += r; iSum++;
            }
            return (rSum == 0) ? 0 :  scene.ToWorldDimension( rSum / (float)iSum );
        }


    }
}

