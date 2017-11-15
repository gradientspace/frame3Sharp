using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    /// <summary>
    /// This is an interface to a 2D sub-region of a 3D surface.
    /// Using these functions we can project 3D points into the 2D
    /// boxmodel space, and back to the 3D surface. This is useful
    /// for doing boxmodel-style 2D layout on a 3D surface.
    /// </summary>
    public interface ISurfaceBoxRegion
    {
        AxisAlignedBox2f Bounds2D { get; }
        Vector2f To2DCoords(Vector3f pos);
        Frame3f From2DCoords(Vector2f pos, float fNormalOffset);
    }






    public class PlaneBoxRegion : ISurfaceBoxRegion
    {
        public Frame3f Frame;   // Z is normal
        public AxisAlignedBox2f Dimensions;

        public PlaneBoxRegion()
        {
            Frame = new Frame3f(Vector3f.Zero, -Vector3f.AxisZ);
            Dimensions = new AxisAlignedBox2f(1.0f);
        }

        public virtual AxisAlignedBox2f Bounds2D {
            get {
                return Dimensions;
            }
        }

        virtual public Vector2f To2DCoords(Vector3f pos)
        {
            Vector3f posF = Frame.ToFrameP(pos);
            return posF.xy;
        }

        virtual public Frame3f From2DCoords(Vector2f pos, float fNormalOffset)
        {
            Frame3f f = new Frame3f(Frame);
            f.Translate(pos.x * Frame.X + pos.y * Frame.Y + fNormalOffset * Frame.Z);
            return f;
        }
    }








    public class CylinderBoxRegion : ISurfaceBoxRegion
    {
        public float Radius;
        public Vector3f Origin;
        public float HorzDegreeLeft;      
        public float HorzDegreeRight;
        public float MinHeight;
        public float MaxHeight;
        public CylinderBoxRegion()
        {
            Radius = 1.0f;
            Origin = Vector3f.Zero;
            HorzDegreeLeft = 30.0f;      
            HorzDegreeRight = 30.0f;
            MinHeight = -1.0f;
            MaxHeight = 1.0f;
        }

        public virtual AxisAlignedBox2f Bounds2D
        {
            get {
                float circ = 2 * MathUtil.PIf * Radius;
                float left = -(HorzDegreeLeft / 360.0f) * circ;
                float right = (HorzDegreeRight / 360.0f) * circ;
                float cx = (left + right) * 0.5f;
                float cy = (MinHeight + MaxHeight) * 0.5f;
                return new AxisAlignedBox2f(new Vector2f(cx, cy), (right - left) / 2, (MaxHeight - MinHeight) / 2);
            }
        }

        virtual public Vector2f To2DCoords(Vector3f pos)
        {
            Vector3f dv = pos - Origin;
            float h = dv.y;
            dv.Normalize();
            float fAngleX = MathUtil.PlaneAngleSignedD(Vector3f.AxisZ, dv, 1);
            float circ = 2 * MathUtil.PIf * Radius;
            return new Vector2f((fAngleX / 360.0f) * circ, h);
        }

        virtual public Frame3f From2DCoords(Vector2f pos, float fNormalOffset)
        {
            // todo simplify this, use radians only
            float circ = 2 * MathUtil.PIf * Radius;
            float fAngleX = (pos.x / circ) * 360.0f;
            fAngleX *= MathUtil.Deg2Radf;
            float x = (float)Math.Sin(fAngleX);
            float z = (float)Math.Cos(fAngleX);
            Vector3f normal = new Vector3f(x, 0, z);
            x *= (Radius + fNormalOffset);
            z *= (Radius + fNormalOffset);
            Vector3f pos3 = new Vector3f(x, pos.y, z) + Origin;
            return new Frame3f(pos3, normal);
        }
    }





    public class SphereBoxRegion : ISurfaceBoxRegion
    {
        public float Radius;
        public Vector3f Origin;
        public float HorzDegreeLeft;      
        public float HorzDegreeRight;
        public float VertDegreeBottom;
        public float VertDegreeTop;
        public SphereBoxRegion()
        {
            Radius = 1.0f;
            Origin = Vector3f.Zero;
            HorzDegreeLeft = 45.0f;      
            HorzDegreeRight = 45.0f;
            VertDegreeBottom = 45.0f;
            VertDegreeTop = 45.0f;
        }

        virtual public AxisAlignedBox2f Bounds2D
        {
            get {
                float circ = 2 * MathUtil.PIf * Radius;
                float left = -(HorzDegreeLeft / 360.0f) * circ;
                float right = (HorzDegreeRight / 360.0f) * circ;
                float bottom = -(VertDegreeBottom / 360.0f) * circ;
                float top = (VertDegreeTop / 360.0f) * circ;
                float cx = (left + right) * 0.5f;
                float cy = (bottom + top) * 0.5f;
                return new AxisAlignedBox2f(new Vector2f(cx, cy), (right - left) / 2, (top - bottom) / 2);
            }
        }

        virtual public Vector2f To2DCoords(Vector3f pos)
        {
            float circ = 2 * MathUtil.PIf * Radius;
            Vector3f dv = pos - Origin;
            dv.Normalize();
            float fAngleX = MathUtil.PlaneAngleSignedD(Vector3f.AxisZ, dv, 1);
            float fAngleY = -MathUtil.PlaneAngleSignedD(Vector3f.AxisZ, dv, 0);
            return new Vector2f((fAngleX / 360.0f) * circ, (fAngleY / 360.0f) * circ);
        }


        virtual public Frame3f From2DCoords(Vector2f pos, float fNormalOffset)
        {
            // todo simplify this, use radians only
            float circ = 2 * MathUtil.PIf * Radius;
            float fAngleX = (pos.x / circ) * 360.0f;
            float fAngleY = (pos.y / circ) * 360.0f;
            Vector3f normal = VRUtil.DirectionFromSphereCenter(fAngleX, fAngleY);
            Vector3f pos3 = (Radius + fNormalOffset) * normal + Origin;
            return new Frame3f(pos3, normal);
        }

    }



}
