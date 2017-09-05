using System;
using System.Collections.Generic;
using g3;
using f3;
using UnityEngine;

namespace f3
{



    public class UnityCurveRenderer : CurveRendererImplementation
    {
        protected LineRenderer r;

        public virtual void initialize(fGameObject go, Colorf color)
        {
            r = ((GameObject)go).AddComponent<LineRenderer>();
            r.useWorldSpace = false;
            r.material = MaterialUtil.CreateTransparentMaterial(color);
        }

        public virtual void initialize(fGameObject go, fMaterial material, bool bSharedMat)
        {
            r = ((GameObject)go).AddComponent<LineRenderer>();
            r.useWorldSpace = false;
            if (bSharedMat)
                r.sharedMaterial = material;
            else
                r.material = material;
        }

        public virtual void update_curve(Vector3f[] Vertices)
        {
            if ( Vertices == null ) {
                r.positionCount = 0;
                return;
            }
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
        public virtual void update_width(float startWidth, float endWidth)
        {
            r.startWidth = startWidth;
            r.endWidth = endWidth;
        }
        public virtual void update_color(Colorf color)
        {
            r.startColor = r.endColor = color;
        }

        public virtual void set_corner_quality(int n)
        {
            r.numCornerVertices = n;
        }


        public virtual bool is_pixel_width() {
            return false;
        }
    }




    public class UnityPixelCurveRenderer : UnityCurveRenderer
    {
        //fGameObject lineGO;
        Vector3f center;

        public override void initialize(fGameObject go, Colorf color)
        {
            //lineGO = go;
            base.initialize(go, color);
        }

        public override void update_curve(Vector3f[] Vertices)
        {
            base.update_curve(Vertices);
            if (Vertices == null)
                center = Vector3f.Zero;
            else
                center = BoundsUtil.Bounds(Vertices, (x) => { return x; }).Center;
        }

        public override void update_width(float width)
        {
            // how to convert this width to pixels?
            //float near_plane_w = Camera.main.nearClipPlane * (float)Math.Tan(Camera.main.fieldOfView);
            //float near_pixel_w = Camera.main.pixelWidth / near_plane_w;
            float near_plane_pixel_deg = Camera.main.fieldOfView / Camera.main.pixelWidth;
            float fWidth = VRUtil.GetVRRadiusForVisualAngle(center, Camera.main.transform.position, near_plane_pixel_deg);
            r.startWidth = r.endWidth = width * fWidth;
        }

        public override bool is_pixel_width() {
            return true;
        }
    }





    public class UnityCurveRendererFactory : CurveRendererFactory
    {
        public CurveRendererImplementation Build(LineWidthType widthType)
        {
            if (widthType == LineWidthType.World)
                return new UnityCurveRenderer();
            else
                return new UnityPixelCurveRenderer();
        }
    }

}
