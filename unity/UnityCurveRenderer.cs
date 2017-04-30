using System;
using System.Collections.Generic;
using g3;
using f3;
using UnityEngine;

namespace f3
{



    public class UnityCurveRenderer : CurveRendererImplementation
    {
        LineRenderer r;

        public virtual void initialize(fGameObject go, Colorf color)
        {
            r = ((GameObject)go).AddComponent<LineRenderer>();
            r.useWorldSpace = false;
            r.material = MaterialUtil.CreateTransparentMaterial(color);
        }

        public virtual void update_curve(Vector3f[] Vertices)
        {
            if (r.positionCount != Vertices.Length)
                r.positionCount = Vertices.Length;
            for (int i = 0; i < Vertices.Length; ++i)
                r.SetPosition(i, Vertices[i]);
        }
        public virtual void update_num_points(int N)
        {
            if (r.positionCount != N)
                r.positionCount = N;
        }
        public virtual void update_position(int i, Vector3f v)
        {
            r.SetPosition(i, v);
        }
        public virtual void update_width(float width)
        {
            r.startWidth = r.endWidth = width;
        }
        public virtual void update_color(Colorf color)
        {
            r.startColor = r.endColor = color;
        }
    }




    public class UnityCurveRendererFactory : CurveRendererFactory
    {
        public CurveRendererImplementation Build()
        {
            return new UnityCurveRenderer();
        }
    }

}
