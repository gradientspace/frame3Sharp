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
        Rectangle
    }

    public class HUDShape
    {
        public HUDShapeType Type{ get; set; }

        // for disc
        public float Radius { get; set; }
        public int Slices { get; set; }

        // for rectangle
        public float Width { get; set; }
        public float Height { get; set; }
        public bool UseUVSubRegion { get; set; }



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



        public HUDShape()
        {
            Type = HUDShapeType.Disc;
            Radius = 0.1f;
            Slices = 32;

            Width = Height = 0.1f;
            UseUVSubRegion = true;
        }
    }
}
