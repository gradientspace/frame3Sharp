using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public enum HUDShapeType
    {
        Disc,
        Rectangle,
        RoundRect
    }


    public struct HUDShape
    {
        public HUDShapeType Type;

        // for disc, roundrect
        public float Radius;
        public int Slices;

        // for rectangle, roundrect
        public float Width;
        public float Height;
        public bool UseUVSubRegion;

        // can override roundrect corners with this
        public int RoundRectSharpCorners;

        public Vector2f Size {
            get {
                if (Type == HUDShapeType.Disc)
                    return new Vector2f(2 * Radius, 2 * Radius);
                else
                    return new Vector2f(Width, Height);
            }
        }

        public float EffectiveRadius()
        {
            if (Type == HUDShapeType.Disc)
                return Radius;
            else
                return (float)Math.Sqrt(Width * Width + Height * Height);
        }


        //[System.Obsolete("HUDShape() is deprecated, will change to struct in future, use explicit constructor.")]
        //public HUDShape()
        //{
        //    Type = HUDShapeType.Disc;
        //    Radius = 0.1f;
        //    Slices = 32;
        //    Width = 01f;
        //    Height = 0.1f;
        //    UseUVSubRegion = true;
        //}


        public HUDShape(HUDShapeType type, float radius)
        {
            Type = type;
            Radius = radius;
            Slices = 32;
            Width = Height = 2*radius;
            UseUVSubRegion = true;
            RoundRectSharpCorners = 0;
        }

        public HUDShape(HUDShapeType type, float width, float height)
        {
            Type = type;
            Radius = width * 0.1f;
            Slices = 4;
            Width = width; Height = height;
            UseUVSubRegion = true;
            RoundRectSharpCorners = 0;
        }
        public HUDShape(HUDShapeType type, float width, float height, float radius, int slices, bool useUVSubRegion)
        {
            Type = type;
            Radius = radius;
            Slices = slices;
            Width = width;
            Height = height;
            UseUVSubRegion = useUVSubRegion;
            RoundRectSharpCorners = 0;
        }

        // for struct conversion
        public static readonly HUDShape Default = new HUDShape(HUDShapeType.Disc, 1.0f, 1.0f, 0.1f, 32, true);
    }


}
