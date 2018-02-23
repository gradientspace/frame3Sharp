using System;
using g3;

namespace f3
{
    public interface HUDSurface
    {
        void Place(HUDStandardItem hudItem, float dx, float dy);
    }

    public class HUDSphere : HUDSurface
    {
        public float Radius { get; set; }

        public HUDSphere()
        {
            Radius = 1.0f;
        }

        public void Place(HUDStandardItem hudItem, float dx, float dy)
        {
            Frame3f initFrame = hudItem.GetObjectFrame();
            Frame3f hudFrame = HUDUtil.GetSphereFrame(Radius, dx, dy);
            hudItem.SetObjectFrame(
                initFrame.Translated(hudFrame.Origin)
                .Rotated(Quaternionf.FromTo(initFrame.Z, hudFrame.Z)));
        }
    }



    public class HUDCylinder : HUDSurface
    {
        public float Radius { get; set; }
        public bool VerticalCoordIsAngle { get; set; }

        public HUDCylinder()
        {
            Radius = 1.0f;
            VerticalCoordIsAngle = true;
        }

        public void Place(HUDStandardItem hudItem, float dx, float dy)
        {
            Frame3f initFrame = hudItem.GetObjectFrame();
            Frame3f hudFrame = VerticalCoordIsAngle ?
                HUDUtil.GetCylinderFrameFromAngles(Radius, dx, dy) :
                HUDUtil.GetCylinderFrameFromAngleHeight(Radius, dx, dy);
            hudItem.SetObjectFrame(
                initFrame.Translated(hudFrame.Origin)
                .Rotated(Quaternionf.FromTo(initFrame.Z, hudFrame.Z)));
        }
    }

}
