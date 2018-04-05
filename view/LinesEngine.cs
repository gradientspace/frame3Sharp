using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;


namespace f3
{

    /// <summary>
    /// A LineSet is a set of segments and curves (open and loops) that 
    /// we want to draw with the same settings. 
    /// (Expect this class to expand in future)
    /// </summary>
    public class LineSet
    {
        public List<Segment3d> Segments = new List<Segment3d>();
        public List<DCurve3> Curves = new List<DCurve3>();

        public float Width = 1.0f;
        public LineWidthType WidthType = LineWidthType.Pixel;

        public bool UseFixedNormal = false;
        public Vector3f FixedNormal = Vector3f.AxisY;

        public Colorf Color = Colorf.Black;
        public bool DepthTest = true;
    }




    /// <summary>
    /// LineRenderingManager handles drawing LineSet instances. The idea is that
    /// this will provide the 'best available' line rendering, client will not 
    /// have to worry about what method is used to do the drawing. 
    /// 
    /// Currently based on fLineSetGameObject instances, which provide the LineSet
    /// instance, and also a transform. 
    /// 
    /// </summary>
    public static class LineRenderingManager
    {
        static Dictionary<int, UnityLineRender_GL> GlobalLineRenderers = new Dictionary<int, UnityLineRender_GL>();


        public static void Initialize()
        {
            var main_ren = FPlatform.MainCamera.AddComponent<UnityLineRender_GL>();
            GlobalLineRenderers[FPlatform.GeometryLayer] = main_ren;

            var widget_ren = FPlatform.WidgetCamera.AddComponent<UnityLineRender_GL>();
            GlobalLineRenderers[FPlatform.WidgetOverlayLayer] = widget_ren;

        }

        public static void AddLineSet(fLineSetGameObject source)
        {
            int layer = source.GetLayer();
            GlobalLineRenderers[layer].AddLineSet(source, source.Lines);
        }

        public static void ChangeLayer(fLineSetGameObject source, int fromLayer, int toLayer)
        {
            GlobalLineRenderers[fromLayer].RemoveLineSet(source);
            GlobalLineRenderers[toLayer].AddLineSet(source, source.Lines);
        }

        public static void RemoveLineSet(fLineSetGameObject source)
        {
            int layer = source.GetLayer();
            GlobalLineRenderers[layer].RemoveLineSet(source);
        }

    }


}
