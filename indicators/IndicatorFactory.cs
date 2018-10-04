using System;
using g3;

namespace f3
{
    /// <summary>
    /// IndicatorFactory is an interface used by various Tools to create indicators in
    /// a structured way. This allows clients of the Tool to customize the indicators
    /// without having to subclass/modify the Tool, while also not requiring that we
    /// expose every possible parameter.
    /// </summary>
    public interface IndicatorFactory
    {
        SphereIndicator MakeSphereIndicator(
            int id, string name,
            fDimension Radius,
            Func<Frame3f> SceneFrameF,
            Func<Colorf> ColorF,
            Func<bool> VisibleF );

        SectionPlaneIndicator MakeSectionPlaneIndicator(
            int id, string name,
            fDimension Width,
            Func<Frame3f> SceneFrameF,
            Func<Colorf> ColorF,
            Func<bool> VisibleF );

    }


    /// <summary>
    /// Default IndicatorFactory implementation
    /// </summary>
    public class StandardIndicatorFactory : IndicatorFactory
    {
        public virtual SphereIndicator MakeSphereIndicator(
            int id, string name,
            fDimension Radius,
            Func<Frame3f> SceneFrameF,
            Func<Colorf> ColorF,
            Func<bool> VisibleF
            )
        {
            SphereIndicator indicator = new SphereIndicator() {
                SceneFrameF = SceneFrameF,
                Radius = Radius,
                ColorF = ColorF,
                VisibleF = VisibleF
            };
            return indicator;
        }


        public virtual SectionPlaneIndicator MakeSectionPlaneIndicator(
            int id, string name,
            fDimension Width,
            Func<Frame3f> SceneFrameF,
            Func<Colorf> ColorF,
            Func<bool> VisibleF)
        {
            SectionPlaneIndicator indicator = new SectionPlaneIndicator() {
                Width = Width,
                SceneFrameF = SceneFrameF,
                ColorF = ColorF,
                VisibleF = VisibleF
            };
            return indicator;
        }





    }
}
