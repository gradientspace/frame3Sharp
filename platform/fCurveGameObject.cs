using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{

    public interface CurveRendererImplementation
    {
        void initialize(fGameObject go, Colorf color);
        void initialize(fGameObject go, fMaterial material, bool bSharedMaterial);
        void update_curve(Vector3f[] Vertices);
        void update_num_points(int N);
        void update_position(int i, Vector3f v);
        void update_width(float width);
        void update_width(float startWidth, float endWidth);
        void update_color(Colorf color);
        void set_corner_quality(int n);
        bool is_pixel_width();      // alternative is world-width
    }
    public interface CurveRendererFactory
    {
        CurveRendererImplementation Build(LineWidthType widthType);
    }



    public class fCurveGameObject : fGameObject
    {
        float width = 0.05f;
        Colorf color = Colorf.Black;

        CurveRendererImplementation renderer;

        public fCurveGameObject(GameObject go, CurveRendererImplementation curveRenderer)
            : base(go, FGOFlags.EnablePreRender)
        {
            renderer = curveRenderer;
            if (renderer.is_pixel_width())
                width = 1.0f;
        }

        /// <summary>
        /// Is the curve width specified in world or pixel coordinates? 
        /// This is a proprety of the CurveRenderer, cannot be changed.
        /// </summary>
        public LineWidthType WidthType
        {
            get { return renderer.is_pixel_width() ? LineWidthType.Pixel : LineWidthType.World; }
        }

        public void SetLineWidth(float fWidth)
        {
            update(fWidth, color);
        }
        public float GetLineWidth() { return width; }

        public void SetLineWidth(float fStartWidth, float fEndWidth)
        {
            update(fStartWidth, fEndWidth, color);
        }

        public override void SetColor(Colorf newColor)
        {
            update(width, newColor);
        }
        public Colorf GetColor() { return color; }



        public enum CornerQuality
        {
            Minimal, Moderate, High
        }
        public void SetCornerQuality(CornerQuality q)
        {
            if (q == CornerQuality.Minimal)
                renderer.set_corner_quality(0);
            else if (q == CornerQuality.Moderate)
                renderer.set_corner_quality(3);
            else if (q == CornerQuality.High)
                renderer.set_corner_quality(8);
        }


        protected void update(float newWidth, Colorf newColor)
        {
            if (width != newWidth) {
                width = newWidth;
                renderer.update_width(width);
            }
            if (color != newColor) {
                color = newColor;
                renderer.update_color(color);
                base.SetColor(color);       // material overrides line renderer??
            }
        }
        protected void update(float newStartWidth, float newEndWidth, Colorf newColor)
        {
            update(newStartWidth, newColor);
            renderer.update_width(newStartWidth, newEndWidth);
        }

        protected void update_curve(Vector3f[] Vertices)
        {
            renderer.update_curve(Vertices);
        }

        protected void update_num_points(int N)
        {
            renderer.update_num_points(N);
        }

        protected void update_position(int i, Vector3f v)
        {
            renderer.update_position(i, v);
        }

    }





    public class fLineGameObject : fCurveGameObject
    {
        Vector3f start, end;

        public fLineGameObject(GameObject go, CurveRendererImplementation curveRenderer)
            : base(go, curveRenderer)
        {
            LineRenderer r = GetComponent<LineRenderer>();
            r.positionCount = 2;
        }


        public void SetStart(Vector3f s)
        {
            if (start != s) {
                start = s;
                LineRenderer r = GetComponent<LineRenderer>();
                r.SetPosition(0, start);
            }
        }
        public Vector3f GetStart() { return start; }


        public void SetEnd(Vector3f e)
        {
            if (end != e) {
                end = e;
                LineRenderer r = GetComponent<LineRenderer>();
                r.SetPosition(1, end);
            }
        }
        public Vector3f GetEnd() { return end; }
    }





    public class fPolylineGameObject : fCurveGameObject
    {
        Vector3f[] Vertices;
        bool bVertsValid;

        public fPolylineGameObject(GameObject go, CurveRendererImplementation curveRenderer)
            : base(go, curveRenderer)
        {
        }


        public void SetVertices(List<Vector3f> vertices)
        {
            Vertices = vertices.ToArray();
            bVertsValid = false;
        }

        public void SetVertices(Vector3f[] vertices, bool bCopy = true)
        {
            if (bCopy) {
                if (Vertices == null || Vertices.Length != vertices.Length)
                    Vertices = new Vector3f[vertices.Length];
                Array.Copy(vertices, Vertices, vertices.Length);
            } else
                Vertices = vertices;
            bVertsValid = false;
        }

        public override void PreRender()
        {
            if (bVertsValid)
                return;
            update_curve(Vertices);
            bVertsValid = true;
        }
    }





    public class fCircleGameObject : fCurveGameObject
    {
        float radius = 1.0f;
        int steps = 32;
        bool bCircleValid = false;

        public fCircleGameObject(GameObject go, CurveRendererImplementation curveRenderer)
            : base(go, curveRenderer)
        {
        }


        public void SetRadius(float fRadius)
        {
            if (radius != fRadius) {
                radius = fRadius;
                bCircleValid = false;
            }
        }
        public float GetRadius() { return radius; }


        public void SetSteps(int nSteps)
        {
            if (steps != nSteps) {
                steps = nSteps;
                bCircleValid = false;
            }
        }
        public int GetSteps() { return steps; }

        public override void PreRender()
        {
            if (bCircleValid)
                return;

            update_num_points(steps + 1);
            float twopi = (float)(2 * Math.PI);
            for (int i = 0; i <= steps; ++i) {
                float t = (float)i / (float)steps;
                float a = t * twopi;
                float x = radius * (float)Math.Cos(a);
                float y = radius * (float)Math.Sin(a);
                update_position(i, new Vector3f(x, 0, y));
            }

            bCircleValid = true;
        }
    }



}

