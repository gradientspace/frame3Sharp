using System;
using UnityEngine;
using g3;

namespace f3
{

    /// <summary>
    /// this Widget implements rotation around an axis.
    /// 
    /// TODO: when axis is highly perp to ray, we should use cylinder instead of plane...
    /// </summary>
    public class AxisRotationWidget : Standard3DTransformWidget
    {
		int nRotationAxis;

        public bool EnableSnapping = true;
        public float SnapIncrementDeg = 5.0f;

        public Func<Frame3f, int, float, float> AbsoluteAngleConstraintF = null;
        public Func<Frame3f, int, float, float> DeltaAngleConstraintF = null;


        public AxisRotationWidget(int nFrameAxis)
		{
			nRotationAxis = nFrameAxis;
		}

		// stored frames from target used during click-drag interaction
		Frame3f rotateFrameL;		// local-space frame 
		Frame3f rotateFrameW;		// world-space frame
		Vector3f rotateAxisW;		// world-space axis we are rotating around (redundant...)

		// computed values during interaction
		Frame3f raycastFrame;		// camera-facing plane containing translateAxisW
		float fRotateStartAngle;

		public override bool BeginCapture(ITransformable target, Ray3f worldRay, UIRayHit hit)
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

            if (EnableSnapping) {
                enable_circle_indicator(true);
            }

            return true;
		}

		public override bool UpdateCapture(ITransformable target, Ray3f worldRay)
		{
			// ray-hit with plane perpendicular to rotateAxisW
			Vector3f planeHitW = raycastFrame.RayPlaneIntersection(worldRay.Origin, worldRay.Direction, 2);

			// find angle of hitpos in 2D plane perp to rotateAxis, and compute delta-angle
			Vector3f dv = planeHitW - rotateFrameW.Origin;
			int iX = (nRotationAxis + 1) % 3;
			int iY = (nRotationAxis + 2) % 3;
			float fX = Vector3.Dot( dv, rotateFrameW.GetAxis(iX) );
			float fY = Vector3.Dot( dv, rotateFrameW.GetAxis(iY) );

			float fNewAngle = (float)Math.Atan2 (fY, fX);
            if (AbsoluteAngleConstraintF != null)
                fNewAngle = AbsoluteAngleConstraintF(rotateFrameL, nRotationAxis, fNewAngle);

            float fDeltaAngle = (fNewAngle - fRotateStartAngle);
            if (DeltaAngleConstraintF != null)
                fDeltaAngle = DeltaAngleConstraintF(rotateFrameL, nRotationAxis, fDeltaAngle);


            bool on_snap = false;
            if (EnableSnapping) {
                double dist = (planeHitW - rotateFrameW.Origin).Length;
                on_snap = Math.Abs(dist - gizmoRadiusW) < gizmoRadiusW * 0.15f;
                if (on_snap)
                    fDeltaAngle = (float)Snapping.SnapToIncrement(fDeltaAngle, SnapIncrementDeg * MathUtil.Deg2Radf);
                enable_snap_indicator(true);
                update_snap_indicator(-fDeltaAngle, on_snap);
            }            

            // construct new frame for target that is rotated around axis
            Vector3f rotateAxisL = rotateFrameL.GetAxis(nRotationAxis);
			Quaternionf q = Quaternion.AngleAxis(fDeltaAngle * Mathf.Rad2Deg, rotateAxisL );
			Frame3f newFrame = rotateFrameL;
			newFrame.Rotation = q * newFrame.Rotation;		// order matters here!

			// update target
			target.SetLocalFrame (newFrame, CoordSpace.ObjectCoords);

            if (EnableSnapping)
                update_circle_indicator(on_snap);

            return true;
		}

        public override bool EndCapture(ITransformable target)
        {
            if (EnableSnapping) {
                enable_circle_indicator(false);
                enable_snap_indicator(false);
            }

            return true;
        }


        public override void Disconnect()
        {
            if (circle_indicator != null)
                circle_indicator.Destroy();
            if (snap_indicator != null)
                circle_indicator.Destroy();
            RootGameObject.Destroy();
        }


        static double VisibilityThresh = Math.Cos(65 * MathUtil.Deg2Rad);
        public override bool CheckVisibility(ref Frame3f curFrameW, ref Vector3d eyePosW)
        {
            Vector3d axis = curFrameW.GetAxis(nRotationAxis);
            Vector3d eyevec = (eyePosW - curFrameW.Origin).Normalized;
            double dot = axis.Dot(eyevec);
            return Math.Abs(dot) > VisibilityThresh;
        }


        static Vector3d[] diagonals = new Vector3d[3] {
            (-Vector3d.AxisX+Vector3d.AxisZ).Normalized,
            (Vector3d.AxisX+Vector3d.AxisZ).Normalized,
            (Vector3d.AxisX-Vector3d.AxisZ).Normalized,
        };


        fLineSetGameObject circle_indicator = null;
        void enable_circle_indicator(bool enable)
        {
            if (enable == false && circle_indicator == null)
                return;
            if (enable && circle_indicator == null) {
                LineSet lines = new LineSet();
                lines.UseFixedNormal = true;
                lines.FixedNormal = Vector3f.AxisY;
                DCurve3 curve = new DCurve3(Polygon2d.MakeCircle(gizmoInitialRadius, 64),0,2);
                lines.Curves.Add(curve);
                lines.Width = 1.0f;
                lines.WidthType = LineWidthType.Pixel;
                lines.Segments.Add(
                    new Segment3d(Vector3d.Zero, gizmoInitialRadius * diagonals[nRotationAxis] ));
                lines.Color = Colorf.DimGrey;
                circle_indicator = new fLineSetGameObject(new GameObject(), lines, "circle");
                circle_indicator.SetLayer(FPlatform.WidgetOverlayLayer, true);

                circle_indicator.SetLocalRotation(Quaternionf.FromTo(Vector3f.AxisY, Frame3f.Identity.GetAxis(nRotationAxis)));
                RootGameObject.AddChild(circle_indicator, false);
            }
            circle_indicator.SetVisible(enable);
        }
        void update_circle_indicator(bool on_snap)
        {
            if (circle_indicator != null)
                circle_indicator.SafeUpdateLines((lines) => {
                    lines.Color.a = (on_snap) ? 1.0f : 0.5f;
                });
        }


        fLineSetGameObject snap_indicator = null;
        void enable_snap_indicator(bool enable)
        {
            if (enable == false && snap_indicator == null)
                return;
            if (enable && snap_indicator == null) {
                LineSet lines = new LineSet();
                lines.UseFixedNormal = true;
                lines.FixedNormal = Vector3f.AxisY;
                int n = 360 / (int)SnapIncrementDeg;
                int n45 = 45 / (int)SnapIncrementDeg;
                int n90 = 90 / (int)SnapIncrementDeg;
                double r = gizmoInitialRadius;
                double r2 = gizmoInitialRadius * 1.05;
                double r45 = gizmoInitialRadius * 1.10;
                double r90 = gizmoInitialRadius * 1.175;
                for ( int k = 0; k < n; ++k ) {
                    float angle = ((float)k / (float)n) * MathUtil.TwoPIf;
                    double x = Math.Cos(angle), y = Math.Sin(angle);
                    Vector3d v = new Vector3d(x, 0, y); v.Normalize();
                    double far_r = ((k + n45) % n90) == 0 ? r90 :
                        (((k + n45) % n45) == 0 ? r45 : r2);
                    lines.Segments.Add(new Segment3d(r * v, far_r * v));
                }
                lines.Width = 1.0f;
                lines.WidthType = LineWidthType.Pixel;
                lines.Color = Colorf.DimGrey;
                snap_indicator = new fLineSetGameObject(new GameObject(), lines, "indicator");
                snap_indicator.SetLayer(FPlatform.WidgetOverlayLayer, true);
                

                RootGameObject.AddChild(snap_indicator, false);
            }
            snap_indicator.SetVisible(enable);
        }
        void update_snap_indicator(float fAngle, bool on_snap)
        {
            Quaternionf planeRotation = new Quaternionf(Vector3f.AxisY, fAngle*MathUtil.Rad2Degf);
            Quaternionf alignAxisRotation = Quaternionf.FromTo(Vector3f.AxisY, Frame3f.Identity.GetAxis(nRotationAxis));
            snap_indicator.SetLocalRotation(alignAxisRotation * planeRotation);

            snap_indicator.SafeUpdateLines((lines) => {
                lines.Color.a = (on_snap) ? 1.0f : 0.15f;
            });
        }


    }
}

